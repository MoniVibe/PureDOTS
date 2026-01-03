#!/usr/bin/env bash
set -u -o pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TRI_ROOT="${TRI_ROOT:-$(cd "${SCRIPT_DIR}/../../.." && pwd)}"
TRI_STATE_DIR="${TRI_STATE_DIR:-${TRI_ROOT}/.tri/state}"
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

tri_ops() {
  "$PYTHON_BIN" "$SCRIPT_DIR/tri_ops.py" "$@"
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
}

mkdir -p "$TRI_RUNS_DIR"
tri_ops init >/dev/null 2>&1 || true

cycle=0
while true; do
  cycle=$((cycle + 1))
  tri_ops gc_stale_leases >/dev/null 2>&1 || true

  if lock_active; then
    tri_ops heartbeat --agent wsl --phase "locked" --current-task "offline" --cycle "$cycle" >/dev/null 2>&1 || true
    run_offline_hook
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
  sleep "$POLL_SECS"
done
