# Ops Bus Protocol (Headless Two-Agent Pipeline)

This protocol defines the shared coordination layer between the WSL headless runner and the Windows/PowerShell builder.

## TRI_STATE_DIR

Recommended placement (WSL ext4):
- WSL: `/home/oni/Tri/.tri/state`
- Windows: `\\wsl$\Ubuntu\home\oni\Tri\.tri\state`

Alternate placement (Windows):
- Windows: `C:\tri-headless-state`
- WSL: `/mnt/c/tri-headless-state`

Set `TRI_STATE_DIR` explicitly in both lanes before starting agents.

## Directory Layout

```
TRI_STATE_DIR/
  ops/
    heartbeats/
      wsl.json
      ps.json
    requests/
      <uuid>.json
    claims/
      <uuid>.json
    results/
      <uuid>.json
    locks/
      build.lock
  builds/
    current_space4x.json
    current_godgame.json
  runs/
    YYYY-MM-DD/
```

All ops files live under `TRI_STATE_DIR/ops` only.

## JSON Schemas (Required)

Heartbeats:
```json
{ "agent": "...", "host": "...", "pid": 0, "cycle": 0, "phase": "...", "currentTask": "...", "utc": "...", "version": "1" }
```

Requests:
```json
{ "id": "...", "type": "...", "projects": ["..."], "reason": "...", "requested_by": "...", "utc": "...", "priority": "...", "desired_build_commit": "...", "notes": "..." }
```

Claims:
```json
{ "id": "...", "claimed_by": "...", "utc": "...", "lease_seconds": 900, "lease_expires_utc": "..." }
```

Results:
```json
{ "id": "...", "status": "ok", "utc": "...", "published_build_path": "...", "build_commit": "...", "logs": ["..."], "error": "..." }
```

Build lock:
```json
{ "owner": "...", "request_id": "...", "utc": "...", "lease_seconds": 900, "lease_expires_utc": "..." }
```

## Atomic Write Rule (Required)

All JSON writes must be atomic:
1) write to a temp file in the same directory
2) rename/replace to the final filename

Never write JSON directly to the final path.

## Leases / TTLs (Required)

- `build.lock` and `claims/*.json` must include `lease_seconds` and `lease_expires_utc`.
- Owners renew their own leases periodically while work is in progress.
- Expired locks/claims are treated as stale and may be reclaimed.

Default lease: 900s. Renew at least every 60s during long work.

## Idempotency (Required)

- Builder may re-run a request with the same id safely.
- Published outputs must be versioned by `timestamp + commit` to avoid overwrites.
- Result files can be overwritten atomically with the latest outcome.

## Failure Recovery

- **Stale lock**: if `build.lock` is expired, it is ignored or removed and a new lock can be acquired.
- **Stale claim**: if `lease_expires_utc` is in the past, another agent may claim the request.
- **Partial publish**: leave old builds in place and publish to a new versioned folder.

## Current Build Pointers

Current build pointers are updated atomically after publish:

`builds/current_space4x.json`, `builds/current_godgame.json`:
```json
{
  "project": "space4x",
  "path": "...",
  "executable": "...",
  "build_commit": "...",
  "utc": "...",
  "build_id": "...",
  "request_id": "..."
}
```

The WSL runner uses these files to select the executable path for the next cycle.

## CLI Tooling (tri_ops)

Canonical CLI: `Tools/Ops/tri_ops.py`

Required commands:
- `heartbeat`
- `request_rebuild`
- `claim_next`
- `write_result`
- `lock_build` / `unlock_build` / `renew_lock`
- `gc_stale_leases` (optional)

## Bootstrap Scripts

- WSL runner: `Tools/Ops/tri_wsl_bootstrap.sh`
- PowerShell builder: `Tools/Ops/tri_ps_bootstrap.ps1`

Both scripts set `TRI_STATE_DIR` and keep heartbeats fresh while polling.
