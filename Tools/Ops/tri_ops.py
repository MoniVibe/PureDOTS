#!/usr/bin/env python3
import argparse
import json
import os
import socket
import sys
import uuid
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Optional


def utc_now() -> datetime:
    return datetime.now(timezone.utc).replace(microsecond=0)


def utc_iso(dt: Optional[datetime] = None) -> str:
    if dt is None:
        dt = utc_now()
    return dt.strftime("%Y-%m-%dT%H:%M:%SZ")


def parse_utc(value: Optional[str]) -> Optional[datetime]:
    if not value:
        return None
    try:
        if value.endswith("Z"):
            value = value[:-1] + "+00:00"
        return datetime.fromisoformat(value).astimezone(timezone.utc)
    except Exception:
        return None


def atomic_write_json(path: Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp_path = path.with_name(f".{path.name}.tmp")
    payload = json.dumps(data, ensure_ascii=True, separators=(",", ":"), sort_keys=False)
    with open(tmp_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(payload)
        handle.flush()
        os.fsync(handle.fileno())
    os.replace(tmp_path, path)


def read_json(path: Path) -> Optional[dict]:
    try:
        with open(path, "r", encoding="utf-8") as handle:
            return json.load(handle)
    except Exception:
        return None


def get_state_dir(args: argparse.Namespace) -> Path:
    state_dir = args.state_dir or os.environ.get("TRI_STATE_DIR")
    if not state_dir:
        sys.stderr.write("TRI_STATE_DIR is required (or use --state-dir)\n")
        sys.exit(2)
    return Path(state_dir)


def ensure_ops_dirs(state_dir: Path) -> Path:
    ops_dir = state_dir / "ops"
    for sub in ("heartbeats", "requests", "claims", "results", "locks", "archive/requests", "archive/claims"):
        (ops_dir / sub).mkdir(parents=True, exist_ok=True)
    (state_dir / "builds" / "inbox").mkdir(parents=True, exist_ok=True)
    (state_dir / "builds" / "inbox_archive").mkdir(parents=True, exist_ok=True)
    (state_dir / "builds").mkdir(parents=True, exist_ok=True)
    (state_dir / "runs").mkdir(parents=True, exist_ok=True)
    return ops_dir


def lease_expiry(lease_seconds: int) -> datetime:
    return utc_now() + timedelta(seconds=lease_seconds)


def is_expired(expires_utc: Optional[str]) -> bool:
    parsed = parse_utc(expires_utc)
    if not parsed:
        return True
    return utc_now() > parsed


def priority_value(value) -> int:
    if value is None:
        return 0
    if isinstance(value, (int, float)):
        return int(value)
    text = str(value).strip().lower()
    if text.isdigit():
        return int(text)
    mapping = {
        "tier0": 100,
        "tier1": 80,
        "tier2": 60,
        "high": 50,
        "normal": 10,
        "medium": 10,
        "low": 0,
        "task": 5,
    }
    return mapping.get(text, 0)


def cmd_init(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ensure_ops_dirs(state_dir)
    return 0


def cmd_heartbeat(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    agent = args.agent
    data = {
        "agent": agent,
        "host": args.host or socket.gethostname(),
        "pid": os.getpid(),
        "cycle": args.cycle,
        "phase": args.phase,
        "currentTask": args.current_task,
        "utc": utc_iso(),
        "version": args.version,
    }
    atomic_write_json(ops_dir / "heartbeats" / f"{agent}.json", data)
    return 0


def cmd_request_rebuild(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    req_id = args.id or str(uuid.uuid4())
    projects = args.project or []
    if args.projects:
        projects += [item.strip() for item in args.projects.split(",") if item.strip()]
    if not projects:
        sys.stderr.write("request_rebuild requires at least one project\n")
        return 2
    data = {
        "id": req_id,
        "type": args.type,
        "projects": projects,
        "reason": args.reason,
        "requested_by": args.requested_by,
        "utc": utc_iso(),
        "priority": args.priority,
    }
    if args.desired_build_commit:
        data["desired_build_commit"] = args.desired_build_commit
    if args.notes:
        data["notes"] = args.notes
    atomic_write_json(ops_dir / "requests" / f"{req_id}.json", data)
    sys.stdout.write(req_id + "\n")
    return 0


def cmd_claim_next(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    requests_dir = ops_dir / "requests"
    claims_dir = ops_dir / "claims"

    items = []
    for path in requests_dir.glob("*.json"):
        req = read_json(path) or {}
        req_id = req.get("id") or path.stem
        if not req_id:
            continue
        if req_id != path.stem:
            req_id = path.stem
        req_utc = parse_utc(req.get("utc"))
        if not req_utc:
            req_utc = datetime.fromtimestamp(path.stat().st_mtime, tz=timezone.utc)
        items.append((-priority_value(req.get("priority")), req_utc, path, req_id, req))

    items.sort()
    for _, _, path, req_id, req in items:
        claim_file = claims_dir / f"{req_id}.json"
        if claim_file.exists():
            existing = read_json(claim_file)
            if existing and not is_expired(existing.get("lease_expires_utc")):
                continue
        lease_seconds = args.lease_seconds
        expires = lease_expiry(lease_seconds)
        claim = {
            "id": req_id,
            "claimed_by": args.agent,
            "utc": utc_iso(),
            "lease_seconds": lease_seconds,
            "lease_expires_utc": utc_iso(expires),
        }
        atomic_write_json(claim_file, claim)
        if args.json:
            sys.stdout.write(json.dumps({"id": req_id, "request": req}, separators=(",", ":")) + "\n")
        else:
            sys.stdout.write(req_id + "\n")
        return 0

    return 2


def cmd_renew_claim(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    claim_file = ops_dir / "claims" / f"{args.id}.json"
    existing = read_json(claim_file)
    if existing and not args.force:
        if existing.get("claimed_by") != args.agent:
            sys.stderr.write("claim owner mismatch\n")
            return 3
    lease_seconds = args.lease_seconds
    expires = lease_expiry(lease_seconds)
    claim = {
        "id": args.id,
        "claimed_by": args.agent,
        "utc": utc_iso(),
        "lease_seconds": lease_seconds,
        "lease_expires_utc": utc_iso(expires),
    }
    atomic_write_json(claim_file, claim)
    return 0


def cmd_lock_build(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    lock_file = ops_dir / "locks" / "build.lock"
    if lock_file.exists():
        existing = read_json(lock_file)
        if existing and not is_expired(existing.get("lease_expires_utc")):
            if existing.get("owner") == args.owner and existing.get("request_id") == args.request_id:
                pass
            elif not args.force:
                sys.stderr.write("build lock is held by another owner\n")
                return 3
    lease_seconds = args.lease_seconds
    expires = lease_expiry(lease_seconds)
    lock = {
        "owner": args.owner,
        "request_id": args.request_id,
        "utc": utc_iso(),
        "lease_seconds": lease_seconds,
        "lease_expires_utc": utc_iso(expires),
    }
    atomic_write_json(lock_file, lock)
    return 0


def cmd_renew_lock(args: argparse.Namespace) -> int:
    return cmd_lock_build(args)


def cmd_unlock_build(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    lock_file = ops_dir / "locks" / "build.lock"
    if not lock_file.exists():
        return 0
    existing = read_json(lock_file)
    if existing and not args.force:
        if existing.get("owner") != args.owner:
            sys.stderr.write("build lock owner mismatch\n")
            return 3
        if args.request_id and existing.get("request_id") != args.request_id:
            sys.stderr.write("build lock request mismatch\n")
            return 3
    try:
        lock_file.unlink()
    except FileNotFoundError:
        pass
    return 0


def cmd_lock_status(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    lock_file = ops_dir / "locks" / "build.lock"
    existing = read_json(lock_file) if lock_file.exists() else None
    if existing and not is_expired(existing.get("lease_expires_utc")):
        if args.json:
            sys.stdout.write(json.dumps(existing, separators=(",", ":")) + "\n")
        return 0
    return 1


def cmd_gc_stale_leases(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    removed = {"locks": 0, "claims": 0}
    lock_file = ops_dir / "locks" / "build.lock"
    existing = read_json(lock_file) if lock_file.exists() else None
    if existing and is_expired(existing.get("lease_expires_utc")):
        try:
            lock_file.unlink()
            removed["locks"] += 1
        except FileNotFoundError:
            pass
    if args.prune_claims:
        for claim_file in (ops_dir / "claims").glob("*.json"):
            claim = read_json(claim_file)
            if claim and is_expired(claim.get("lease_expires_utc")):
                try:
                    claim_file.unlink()
                    removed["claims"] += 1
                except FileNotFoundError:
                    pass
    if args.json:
        sys.stdout.write(json.dumps(removed, separators=(",", ":")) + "\n")
    return 0


def cmd_write_result(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    ops_dir = ensure_ops_dirs(state_dir)
    data = {
        "id": args.id,
        "status": args.status,
        "utc": utc_iso(),
        "published_build_path": args.published_build_path,
        "build_commit": args.build_commit,
        "logs": args.log or [],
    }
    if args.error:
        data["error"] = args.error
    atomic_write_json(ops_dir / "results" / f"{args.id}.json", data)
    return 0


def cmd_current_build(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    project = args.project.lower()
    path = state_dir / "builds" / f"current_{project}.json"
    data = read_json(path)
    if not data:
        return 2
    if args.field:
        value = data.get(args.field)
        if value is None:
            return 2
        sys.stdout.write(str(value) + "\n")
        return 0
    sys.stdout.write(json.dumps(data, separators=(",", ":")) + "\n")
    return 0


def cmd_write_current(args: argparse.Namespace) -> int:
    state_dir = get_state_dir(args)
    project = args.project.lower()
    data = {
        "project": project,
        "path": args.path,
        "executable": args.executable,
        "build_commit": args.build_commit,
        "utc": utc_iso(),
        "build_id": args.build_id,
        "request_id": args.request_id,
    }
    if args.notes:
        data["notes"] = args.notes
    atomic_write_json(state_dir / "builds" / f"current_{project}.json", data)
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="tri_ops")
    parser.add_argument("--state-dir", help="Override TRI_STATE_DIR")
    subparsers = parser.add_subparsers(dest="command", required=True)

    init_cmd = subparsers.add_parser("init")
    init_cmd.set_defaults(func=cmd_init)

    heartbeat = subparsers.add_parser("heartbeat")
    heartbeat.add_argument("--agent", required=True)
    heartbeat.add_argument("--phase", required=True)
    heartbeat.add_argument("--current-task", default="")
    heartbeat.add_argument("--cycle", type=int, default=0)
    heartbeat.add_argument("--version", default="1")
    heartbeat.add_argument("--host")
    heartbeat.set_defaults(func=cmd_heartbeat)

    request = subparsers.add_parser("request_rebuild")
    request.add_argument("--id")
    request.add_argument("--type", default="rebuild")
    request.add_argument("--project", action="append")
    request.add_argument("--projects")
    request.add_argument("--reason", default="")
    request.add_argument("--requested-by", required=True)
    request.add_argument("--priority", default="normal")
    request.add_argument("--desired-build-commit")
    request.add_argument("--notes")
    request.set_defaults(func=cmd_request_rebuild)

    claim_next = subparsers.add_parser("claim_next")
    claim_next.add_argument("--agent", required=True)
    claim_next.add_argument("--lease-seconds", type=int, default=900)
    claim_next.add_argument("--json", action="store_true")
    claim_next.set_defaults(func=cmd_claim_next)

    renew_claim = subparsers.add_parser("renew_claim")
    renew_claim.add_argument("--id", required=True)
    renew_claim.add_argument("--agent", required=True)
    renew_claim.add_argument("--lease-seconds", type=int, default=900)
    renew_claim.add_argument("--force", action="store_true")
    renew_claim.set_defaults(func=cmd_renew_claim)

    write_result = subparsers.add_parser("write_result")
    write_result.add_argument("--id", required=True)
    write_result.add_argument("--status", required=True)
    write_result.add_argument("--published-build-path", required=True)
    write_result.add_argument("--build-commit", required=True)
    write_result.add_argument("--log", action="append")
    write_result.add_argument("--error")
    write_result.set_defaults(func=cmd_write_result)

    lock_build = subparsers.add_parser("lock_build")
    lock_build.add_argument("--owner", required=True)
    lock_build.add_argument("--request-id", required=True)
    lock_build.add_argument("--lease-seconds", type=int, default=900)
    lock_build.add_argument("--force", action="store_true")
    lock_build.set_defaults(func=cmd_lock_build)

    renew_lock = subparsers.add_parser("renew_lock")
    renew_lock.add_argument("--owner", required=True)
    renew_lock.add_argument("--request-id", required=True)
    renew_lock.add_argument("--lease-seconds", type=int, default=900)
    renew_lock.add_argument("--force", action="store_true")
    renew_lock.set_defaults(func=cmd_renew_lock)

    unlock_build = subparsers.add_parser("unlock_build")
    unlock_build.add_argument("--owner", required=True)
    unlock_build.add_argument("--request-id")
    unlock_build.add_argument("--force", action="store_true")
    unlock_build.set_defaults(func=cmd_unlock_build)

    lock_status = subparsers.add_parser("lock_status")
    lock_status.add_argument("--json", action="store_true")
    lock_status.set_defaults(func=cmd_lock_status)

    gc = subparsers.add_parser("gc_stale_leases")
    gc.add_argument("--prune-claims", action="store_true")
    gc.add_argument("--json", action="store_true")
    gc.set_defaults(func=cmd_gc_stale_leases)

    current_build = subparsers.add_parser("current_build")
    current_build.add_argument("--project", required=True)
    current_build.add_argument("--field")
    current_build.set_defaults(func=cmd_current_build)

    write_current = subparsers.add_parser("write_current")
    write_current.add_argument("--project", required=True)
    write_current.add_argument("--path", required=True)
    write_current.add_argument("--executable", required=True)
    write_current.add_argument("--build-commit", required=True)
    write_current.add_argument("--build-id", required=True)
    write_current.add_argument("--request-id", required=True)
    write_current.add_argument("--notes")
    write_current.set_defaults(func=cmd_write_current)

    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
