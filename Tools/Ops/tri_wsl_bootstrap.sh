#!/usr/bin/env bash
set -u -o pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
STATE_LOCKDOWN="${SCRIPT_DIR}/tri_state_lockdown.sh"

require_workspace() {
  local root="$1"
  if [ ! -d "$root/space4x" ] || [ ! -d "$root/godgame" ] || [ ! -d "$root/Tools" ]; then
    echo "TRI_ROOT must point to a workspace containing: space4x, godgame, Tools"
    exit 2
  fi
}

if [ -x "$STATE_LOCKDOWN" ]; then
  "$STATE_LOCKDOWN" || true
fi

if [ -z "${TRI_ROOT:-}" ]; then
  if [ -d "/mnt/c/dev/Tri/space4x" ] && [ -d "/mnt/c/dev/Tri/godgame" ] && [ -d "/mnt/c/dev/Tri/Tools" ]; then
    TRI_ROOT="/mnt/c/dev/Tri"
  else
    echo "TRI_ROOT must be set; required folders: space4x, godgame, Tools"
    exit 2
  fi
fi

require_workspace "$TRI_ROOT"

TRI_STATE_DIR="${TRI_STATE_DIR:-/mnt/c/dev/Tri/.tri/state}"
export TRI_ROOT
export TRI_STATE_DIR

TRI_RUNS_DIR="${TRI_RUNS_DIR:-${TRI_STATE_DIR}/runs/$(date +%Y-%m-%d)}"
export TRI_RUNS_DIR

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

mkdir -p "$TRI_RUNS_DIR"
"$PYTHON_BIN" "$SCRIPT_DIR/tri_ops.py" init

exec "$SCRIPT_DIR/tri_wsl_runner.sh"
