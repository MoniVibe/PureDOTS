#!/usr/bin/env bash
set -u -o pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TRI_ROOT="${TRI_ROOT:-$(cd "${SCRIPT_DIR}/../../.." && pwd)}"

DEFAULT_STATE_DIR="/home/oni/Tri/.tri/state"
if [ ! -d "/home/oni/Tri" ]; then
  DEFAULT_STATE_DIR="${TRI_ROOT}/.tri/state"
fi

TRI_STATE_DIR="${TRI_STATE_DIR:-$DEFAULT_STATE_DIR}"
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
