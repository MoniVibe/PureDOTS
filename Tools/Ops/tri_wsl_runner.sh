#!/usr/bin/env bash
set -u -o pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
STATE_LOCKDOWN="${SCRIPT_DIR}/tri_state_lockdown.sh"
DOCS_SYNC="${SCRIPT_DIR}/tri_docs_sync.sh"

TRI_ROOT="${TRI_ROOT:-/mnt/c/dev/Tri}"
TRI_STATE_DIR="${TRI_STATE_DIR:-/mnt/c/dev/Tri/.tri/state}"
export TRI_ROOT
export TRI_STATE_DIR

if [ -f "$STATE_LOCKDOWN" ]; then
  bash "$STATE_LOCKDOWN" || true
fi

TRI_RUNS_DIR="${TRI_RUNS_DIR:-${TRI_STATE_DIR}/runs/$(date +%Y-%m-%d)}"

PYTHON_BIN="${PYTHON_BIN:-}"
if [ -z "$PYTHON_BIN" ]; then
  if command -v python3 >/dev/null 2>&1; then
    PYTHON_BIN=python3
  elif command -v python >/dev/null 2>&1; then
    PYTHON_BIN=python
  else
    echo "python not found"
    exit 2
  fi
fi

HEARTBEAT_SECS="${TRI_OPS_HEARTBEAT_SECONDS:-45}"
POLL_SECS="${TRI_OPS_POLL_SECONDS:-30}"
PS_STALE_SECS="${TRI_PS_STALE_SECONDS:-120}"
DOCS_CANON_DIR="${TRI_ROOT}/puredots/Docs/Headless"
HEADLESSTASKS_PATH="${TRI_HEADLESSTASKS_PATH:-${DOCS_CANON_DIR}/headlesstasks.md}"
REC_ERRORS_PATH="${TRI_RECURRING_ERRORS_PATH:-${DOCS_CANON_DIR}/recurringerrors.md}"
BLOCKER_STATE_FILE="${TRI_STATE_DIR}/ops/wsl_blocker_state"

tri_ops() {
  "$PYTHON_BIN" "$SCRIPT_DIR/tri_ops.py" "$@"
}

work_done=0
mark_work_done() {
  work_done=1
}

utc_to_epoch() {
  local utc="${1:-}"
  if [ -z "$utc" ]; then
    echo -1
    return
  fi
  local epoch
  epoch="$(date -u -d "$utc" +%s 2>/dev/null || true)"
  if [ -z "$epoch" ]; then
    echo -1
    return
  fi
  echo "$epoch"
}

json_field() {
  local file="$1"
  local field="$2"
  "$PYTHON_BIN" - "$file" "$field" <<'PY'
import json
import sys

path = sys.argv[1]
field = sys.argv[2]
try:
    with open(path, "r", encoding="utf-8") as handle:
        data = json.load(handle)
    value = data.get(field)
    if value is None:
        sys.exit(1)
    sys.stdout.write(str(value))
except Exception:
    sys.exit(1)
PY
}

ps_heartbeat_utc() {
  local hb="${TRI_STATE_DIR}/ops/heartbeats/ps.json"
  if [ ! -f "$hb" ]; then
    return
  fi
  json_field "$hb" "utc" 2>/dev/null || true
}

ps_heartbeat_epoch() {
  local utc
  utc="$(ps_heartbeat_utc || true)"
  utc_to_epoch "$utc"
}

ps_heartbeat_is_stale() {
  local hb_epoch
  hb_epoch="$(ps_heartbeat_epoch)"
  if [ "$hb_epoch" -le 0 ]; then
    return 0
  fi
  local now
  now="$(date -u +%s)"
  if [ $((now - hb_epoch)) -gt "$PS_STALE_SECS" ]; then
    return 0
  fi
  return 1
}

lock_status_label() {
  local lock_file="${TRI_STATE_DIR}/ops/locks/build.lock"
  if [ ! -f "$lock_file" ]; then
    echo "none"
    return
  fi
  local expires
  expires="$(json_field "$lock_file" "lease_expires_utc" 2>/dev/null || true)"
  if [ -z "$expires" ]; then
    echo "stale"
    return
  fi
  local exp_epoch now_epoch
  exp_epoch="$(utc_to_epoch "$expires")"
  now_epoch="$(date -u +%s)"
  if [ "$exp_epoch" -le "$now_epoch" ]; then
    echo "stale"
  else
    echo "active"
  fi
}

current_build_id() {
  local project="$1"
  local path="${TRI_STATE_DIR}/builds/current_${project}.json"
  if [ ! -f "$path" ]; then
    return
  fi
  json_field "$path" "build_id" 2>/dev/null || true
}

update_state_paths() {
  TRI_RUNS_DIR="${TRI_STATE_DIR}/runs/$(date +%Y-%m-%d)"
  export TRI_RUNS_DIR
  BLOCKER_STATE_FILE="${TRI_STATE_DIR}/ops/wsl_blocker_state"
}

write_cycle_header() {
  local summary="${TRI_RUNS_DIR}/summary.md"
  local now lock_label space_id god_id ps_utc
  now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  ps_utc="$(ps_heartbeat_utc || true)"
  lock_label="$(lock_status_label)"
  space_id="$(current_build_id "space4x" || true)"
  god_id="$(current_build_id "godgame" || true)"
  printf '[%s] state=%s ps_utc=%s lock=%s space4x=%s godgame=%s\n' \
    "$now" \
    "$TRI_STATE_DIR" \
    "${ps_utc:-missing}" \
    "$lock_label" \
    "${space_id:-missing}" \
    "${god_id:-missing}" >>"$summary"
}

ensure_recurring_errors_file() {
  local path="$1"
  if [ -f "$path" ]; then
    return
  fi
  mkdir -p "$(dirname "$path")"
  printf "# recurringerrors\n\n" >"$path"
}

append_recurring_error() {
  local message="$1"
  ensure_recurring_errors_file "$REC_ERRORS_PATH"
  printf -- '- UTC: %s | %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$message" >>"$REC_ERRORS_PATH"
}

log_blocker_once() {
  local key="$1"
  local message="$2"
  local now_epoch
  now_epoch="$(date -u +%s)"
  if [ -f "$BLOCKER_STATE_FILE" ]; then
    local last_key last_epoch
    IFS='|' read -r last_key last_epoch <"$BLOCKER_STATE_FILE" || true
    if [ "$key" = "${last_key:-}" ] && [ $((now_epoch - ${last_epoch:-0})) -lt 3600 ]; then
      mark_work_done
      return
    fi
  fi
  mkdir -p "$(dirname "$BLOCKER_STATE_FILE")"
  printf '%s|%s\n' "$key" "$now_epoch" >"$BLOCKER_STATE_FILE"
  append_recurring_error "$message"
  if [ -f "$HEADLESSTASKS_PATH" ]; then
    {
      printf -- "- UTC: %s\n" "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
      printf -- "- Agent: wsl-runner\n"
      printf -- "- Project: Ops\n"
      printf -- "- Task: OPS_BLOCKER\n"
      printf -- "- Scenario: N/A\n"
      printf -- "- Baseline: N/A\n"
      printf -- "- Threshold: N/A\n"
      printf -- "- Action: %s\n" "$message"
      printf -- "- Result: Blocked\n"
      printf -- "- Notes: %s\n" "$message"
    } >>"$HEADLESSTASKS_PATH"
  fi
  mark_work_done
}

find_latest_build_dir() {
  local project="$1"
  local builds_dir="${TRI_STATE_DIR}/builds/${project}"
  local exe_name=""
  case "$project" in
    space4x) exe_name="Space4X_Headless.x86_64" ;;
    godgame) exe_name="Godgame_Headless.x86_64" ;;
    *) return 1 ;;
  esac
  if [ ! -d "$builds_dir" ]; then
    return 1
  fi
  local latest
  latest="$(find "$builds_dir" -maxdepth 2 -type f -name "$exe_name" -printf '%T@ %p\n' 2>/dev/null | sort -nr | head -n 1 | awk '{print $2}')"
  if [ -z "$latest" ]; then
    return 1
  fi
  echo "$(dirname "$latest")|$latest"
}

seed_current_build() {
  local project="$1"
  local found build_dir executable build_id build_commit
  found="$(find_latest_build_dir "$project" || true)"
  if [ -z "$found" ]; then
    return 1
  fi
  build_dir="${found%%|*}"
  executable="${found#*|}"
  build_id="$(basename "$build_dir")"
  build_commit="${build_id##*_}"
  if [ -z "$build_commit" ]; then
    build_commit="$build_id"
  fi
  tri_ops write_current \
    --project "$project" \
    --path "$build_dir" \
    --executable "$executable" \
    --build-commit "$build_commit" \
    --build-id "$build_id" \
    --request-id "seeded" \
    --notes "seeded_from_existing_builds" >/dev/null 2>&1 || return 1
  mark_work_done
  return 0
}

has_pending_publish_check() {
  local project="$1"
  "$PYTHON_BIN" - "$TRI_STATE_DIR/ops/requests" "$project" <<'PY'
import json
import sys
from pathlib import Path

req_dir = Path(sys.argv[1])
project = sys.argv[2]

for path in req_dir.glob("*.json"):
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        continue
    if data.get("type") != "publish_check":
        continue
    projects = data.get("projects") or []
    if project in projects:
        sys.exit(0)
sys.exit(1)
PY
}

maybe_request_publish_check() {
  local project="$1"
  local reason="$2"
  if ps_heartbeat_is_stale; then
    log_blocker_once "ps_stale_${project}" "$reason (ps heartbeat stale)"
    return
  fi
  if has_pending_publish_check "$project"; then
    return
  fi
  tri_ops request_rebuild \
    --type publish_check \
    --requested-by wsl \
    --projects "$project" \
    --desired-build-commit origin/main \
    --reason "$reason" \
    --priority high >/dev/null 2>&1 || true
  mark_work_done
}

ensure_current_build() {
  local project="$1"
  if tri_ops current_build --project "$project" >/dev/null 2>&1; then
    return 0
  fi
  if seed_current_build "$project"; then
    return 0
  fi
  maybe_request_publish_check "$project" "missing current pointer; no builds to seed"
  return 1
}

lock_active() {
  tri_ops lock_status >/dev/null 2>&1
}

heartbeat_loop_pid=""

start_heartbeat_loop() {
  local phase="$1"
  local task="$2"
  local cycle="$3"
  stop_heartbeat_loop
  (
    while true; do
      tri_ops heartbeat --agent wsl --phase "$phase" --current-task "$task" --cycle "$cycle" >/dev/null 2>&1 || true
      sleep "$HEARTBEAT_SECS"
    done
  ) &
  heartbeat_loop_pid=$!
}

stop_heartbeat_loop() {
  if [ -n "$heartbeat_loop_pid" ]; then
    kill "$heartbeat_loop_pid" >/dev/null 2>&1 || true
    wait "$heartbeat_loop_pid" 2>/dev/null || true
    heartbeat_loop_pid=""
  fi
}

run_offline_hook() {
  if [ -z "${TRI_WSL_OFFLINE_HOOK:-}" ]; then
    return
  fi
  if [ -x "$TRI_WSL_OFFLINE_HOOK" ]; then
    "$TRI_WSL_OFFLINE_HOOK" || true
  else
    bash "$TRI_WSL_OFFLINE_HOOK" || true
  fi
}

run_project() {
  local project="$1"
  local cycle="$2"
  local cycle_dir="$3"
  local runner=""
  local scenario=""

  case "$project" in
    space4x)
      runner="${TRI_SPACE4X_RUNNER:-$TRI_ROOT/Tools/run_space4x_headless.sh}"
      scenario="${TRI_SPACE4X_SCENARIO:-$TRI_ROOT/space4x/Assets/Scenarios/space4x_smoke.json}"
      ;;
    godgame)
      runner="${TRI_GODGAME_RUNNER:-$TRI_ROOT/Tools/run_godgame_headless.sh}"
      scenario="${TRI_GODGAME_SCENARIO:-$TRI_ROOT/godgame/Assets/Scenarios/Godgame/godgame_smoke.json}"
      ;;
    *)
      return
      ;;
  esac

  if lock_active; then
    return
  fi

  local executable
  if ! ensure_current_build "$project"; then
    tri_ops heartbeat --agent wsl --phase "skip_no_build" --current-task "$project" --cycle "$cycle" >/dev/null 2>&1 || true
    return
  fi
  executable="$(tri_ops current_build --project "$project" --field executable 2>/dev/null || true)"
  if [ -z "$executable" ]; then
    tri_ops heartbeat --agent wsl --phase "skip_no_build" --current-task "$project" --cycle "$cycle" >/dev/null 2>&1 || true
    return
  fi
  if [ -f "$executable" ] && [ ! -x "$executable" ]; then
    chmod +x "$executable" 2>/dev/null || true
  fi

  if [ ! -f "$runner" ]; then
    tri_ops heartbeat --agent wsl --phase "skip_missing_runner" --current-task "$project" --cycle "$cycle" >/dev/null 2>&1 || true
    return
  fi

  if lock_active; then
    return
  fi

  local project_dir="${cycle_dir}/${project}"
  mkdir -p "$project_dir"
  local telemetry_dir="${project_dir}/telemetry"
  mkdir -p "$telemetry_dir"
  local report_path="${project_dir}/report.json"
  local log_path="${project_dir}/stdout.log"

  start_heartbeat_loop "running_${project}" "$project" "$cycle"

  if [ -x "$runner" ]; then
    EXECUTABLE_PATH="$executable" \
      SCENARIO_PATH="$scenario" \
      REPORT_PATH="$report_path" \
      TELEMETRY_DIR="$telemetry_dir" \
      "$runner" >"$log_path" 2>&1
  else
    EXECUTABLE_PATH="$executable" \
      SCENARIO_PATH="$scenario" \
      REPORT_PATH="$report_path" \
      TELEMETRY_DIR="$telemetry_dir" \
      bash "$runner" >"$log_path" 2>&1
  fi
  local exit_code=$?

  stop_heartbeat_loop
  tri_ops heartbeat --agent wsl --phase "done_${project}" --current-task "exit=$exit_code" --cycle "$cycle" >/dev/null 2>&1 || true
  mark_work_done
}

update_state_paths
mkdir -p "$TRI_RUNS_DIR"
tri_ops init >/dev/null 2>&1 || true

cycle=0
while true; do
  cycle=$((cycle + 1))
  work_done=0

  if [ -f "$DOCS_SYNC" ]; then
    bash "$DOCS_SYNC" start >/dev/null 2>&1 || true
  fi

  update_state_paths
  mkdir -p "$TRI_RUNS_DIR"
  tri_ops init >/dev/null 2>&1 || true
  write_cycle_header

  tri_ops gc_stale_leases --prune-claims >/dev/null 2>&1 || true
  lock_label="$(lock_status_label)"

  if [ "$lock_label" = "active" ]; then
    if ps_heartbeat_is_stale; then
      log_blocker_once "stuck_lock" "build.lock active with stale ps heartbeat; attempted gc"
    fi
    tri_ops heartbeat --agent wsl --phase "locked" --current-task "offline" --cycle "$cycle" >/dev/null 2>&1 || true
    run_offline_hook
    if [ "$work_done" -eq 0 ]; then
      log_blocker_once "locked_cycle" "cycle skipped due to active build.lock"
    fi
    if [ -f "$DOCS_SYNC" ]; then
      bash "$DOCS_SYNC" end >/dev/null 2>&1 || true
      bash "$DOCS_SYNC" start >/dev/null 2>&1 || true
    fi
    sleep "$POLL_SECS"
    continue
  fi

  stamp="$(date +%Y%m%d_%H%M%S)"
  cycle_dir="${TRI_RUNS_DIR}/cycle_${stamp}"
  mkdir -p "$cycle_dir"

  tri_ops heartbeat --agent wsl --phase "cycle_start" --current-task "$cycle_dir" --cycle "$cycle" >/dev/null 2>&1 || true

  run_project "space4x" "$cycle" "$cycle_dir"
  run_project "godgame" "$cycle" "$cycle_dir"

  tri_ops heartbeat --agent wsl --phase "cycle_end" --current-task "$cycle_dir" --cycle "$cycle" >/dev/null 2>&1 || true

  if [ "$work_done" -eq 0 ]; then
    log_blocker_once "no_work" "cycle produced no runs or requests; attempted recovery and logged state"
  fi

  if [ -f "$DOCS_SYNC" ]; then
    bash "$DOCS_SYNC" end >/dev/null 2>&1 || true
    bash "$DOCS_SYNC" start >/dev/null 2>&1 || true
  fi

  sleep "$POLL_SECS"
done
