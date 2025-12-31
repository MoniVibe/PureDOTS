# Git Practices (WSL vs Windows)

Purpose: prevent cross-OS git corruption, locks, invalid filenames, and auth churn.

## WSL side (/home/oni/Tri)
- Work only in `/home/oni/Tri`; do not run git in `/mnt/c` for these repos.
- Keep WSL changes limited to logic and non-Assets files; `Assets/` and `.meta` stay on Windows.
- Check repo status before and after tasks to catch unexpected dirtiness.
- Use SSH remotes and a stable key in `~/.ssh`.
- Prefer rebase when integrating `origin/main`; finish the rebase promptly.
- If git stalls, confirm no git process is running before removing any `.git/*lock` files.

## Windows side (C:\dev\Tri)
- Use Windows for `Assets/`, `.meta`, scenes, and editor wiring; avoid WSL git in that tree.
- Keep `core.protectNTFS` and `core.protectHFS` enabled to avoid invalid paths.
- Ensure you have write access to `.git` (fix ACLs before running git if needed).
- Use SSH remotes and a consistent key; avoid switching auth methods mid-task.
- Prefer rebase when integrating `origin/main` unless a merge is explicitly required.

## Cross-OS guardrails
- Do not run simultaneous git operations in the same repo from both OSes.
- Do not share `Library/` across OSes.
- Keep `Packages/manifest.json` and `Packages/packages-lock.json` in sync when logic changes.
- Avoid filenames that are illegal on Windows (trailing spaces, control chars).
- If a WSL task needs `Assets/`, log a handoff and switch to Windows for the edit.

## Recovery notes
- Divergent history: fetch then rebase `origin/main`.
- Stale lock: remove `.git/index.lock` only after confirming no git process is active.
- Permission errors: fix ACLs on the repo root and `.git` before retrying git.
- Invalid path errors: re-enable protect flags and fix filenames on Windows.
