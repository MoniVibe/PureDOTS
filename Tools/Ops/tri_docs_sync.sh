#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-both}"
TRI_ROOT="${TRI_ROOT:-/mnt/c/dev/Tri}"
CANON_DIR="${TRI_ROOT}/puredots/Docs/Headless"
MIRROR_DIR="${TRI_ROOT}"

ensure_file() {
  local path="$1"
  local title="$2"
  if [ ! -f "$path" ]; then
    mkdir -p "$(dirname "$path")"
    printf "# %s\n\n" "$title" >"$path"
  fi
}

copy_if_newer() {
  local src="$1"
  local dst="$2"
  if [ ! -f "$src" ]; then
    return
  fi
  if [ ! -f "$dst" ] || [ "$src" -nt "$dst" ]; then
    mkdir -p "$(dirname "$dst")"
    cp -f "$src" "$dst"
  fi
}

sync_start() {
  ensure_file "${CANON_DIR}/headlesstasks.md" "Headless Tasks"
  ensure_file "${CANON_DIR}/recurring.md" "recurring"
  ensure_file "${CANON_DIR}/recurringerrors.md" "recurringerrors"

  copy_if_newer "${CANON_DIR}/headlesstasks.md" "${MIRROR_DIR}/headlesstasks.md"
  copy_if_newer "${CANON_DIR}/recurring.md" "${MIRROR_DIR}/recurring.md"
  copy_if_newer "${CANON_DIR}/recurringerrors.md" "${MIRROR_DIR}/recurringerrors.md"
}

sync_end() {
  copy_if_newer "${MIRROR_DIR}/headlesstasks.md" "${CANON_DIR}/headlesstasks.md"
  copy_if_newer "${MIRROR_DIR}/recurring.md" "${CANON_DIR}/recurring.md"
  copy_if_newer "${MIRROR_DIR}/recurringerrors.md" "${CANON_DIR}/recurringerrors.md"
}

case "$MODE" in
  start)
    sync_start
    ;;
  end)
    sync_end
    ;;
  both)
    sync_start
    sync_end
    ;;
  *)
    echo "usage: tri_docs_sync.sh [start|end|both]"
    exit 2
    ;;
esac
