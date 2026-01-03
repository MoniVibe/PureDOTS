#!/usr/bin/env bash
set -u -o pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SESSION_NAME="${TRI_OPS_TMUX_SESSION:-tri-ops}"

if command -v tmux >/dev/null 2>&1; then
  if tmux has-session -t "$SESSION_NAME" 2>/dev/null; then
    echo "tmux session '$SESSION_NAME' already running"
    exit 0
  fi
  tmux new-session -d -s "$SESSION_NAME" "$SCRIPT_DIR/tri_wsl_bootstrap.sh"
  echo "started tmux session '$SESSION_NAME'"
  exit 0
fi

LOG_DIR="${TRI_STATE_DIR:-$HOME/.tri/state}/ops"
mkdir -p "$LOG_DIR"
nohup "$SCRIPT_DIR/tri_wsl_bootstrap.sh" >"$LOG_DIR/wsl_startup.log" 2>&1 &
echo "started tri_wsl_bootstrap.sh via nohup"
