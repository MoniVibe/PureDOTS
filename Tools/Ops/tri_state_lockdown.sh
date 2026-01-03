#!/usr/bin/env bash
set -euo pipefail

CANON_TARGET="/home/oni/Tri/.tri/state"
LEGACY_LINK="/mnt/c/dev/Tri/.tri/state"

mkdir -p "$CANON_TARGET"

if [ -e "$LEGACY_LINK" ] || [ -L "$LEGACY_LINK" ]; then
  if [ -L "$LEGACY_LINK" ]; then
    target="$(readlink "$LEGACY_LINK" || true)"
    if [ "$target" != "$CANON_TARGET" ]; then
      rm -f "$LEGACY_LINK"
    fi
  else
    rm -rf "$LEGACY_LINK"
  fi
fi

mkdir -p "$(dirname "$LEGACY_LINK")"
ln -s "$CANON_TARGET" "$LEGACY_LINK"

resolved="$(readlink "$LEGACY_LINK" || true)"
if [ "$resolved" != "$CANON_TARGET" ]; then
  echo "state lockdown failed: ${LEGACY_LINK} -> ${resolved}"
  exit 2
fi

echo "state lockdown ok: ${LEGACY_LINK} -> ${CANON_TARGET}"
