#!/usr/bin/env bash
set -u -o pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

require_workspace() {
  local root="$1"
  if [ ! -d "$root/space4x" ] || [ ! -d "$root/godgame" ] || [ ! -d "$root/Tools" ]; then
    echo "TRI_ROOT must point to a workspace containing: space4x, godgame, Tools"
    exit 2
  fi
}

if [ -z "${TRI_ROOT:-}" ]; then
  if [ -d "/home/oni/Tri/space4x" ] && [ -d "/home/oni/Tri/godgame" ] && [ -d "/home/oni/Tri/Tools" ]; then
    TRI_ROOT="/home/oni/Tri"
  else
    echo "TRI_ROOT must be set; required folders: space4x, godgame, Tools"
    exit 2
  fi
fi

require_workspace "$TRI_ROOT"

if [ -z "${TRI_STATE_DIR:-}" ]; then
  if [ -d "/home/oni/Tri" ]; then
    TRI_STATE_DIR="/home/oni/Tri/.tri/state"
  else
    TRI_STATE_DIR="${TRI_ROOT}/.tri/state"
  fi
fi
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
