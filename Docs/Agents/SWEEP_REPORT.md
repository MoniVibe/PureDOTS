# Doc Sweep Report: Desktop vs Laptop (unity_clean)

**Sweep date:** 2026-02-14  
**Scope:** `C:\dev\unity_clean` on both machines  
**SSH laptop:** shonh@25.29.69.246

---

## DESKTOP (local)

### Top-level structure
- `.tri`, `godgame`, `headlessrebuildtool`, `puredots`, `space4x`, `Tools` (junction to headlessrebuildtool)
- **No** `directive.md`, `STATUS.md`, `recurring.md`, `recurringerrors.md` at workspace root

### AGENTS.md
| Path | Size | Git |
|------|------|-----|
| puredots/AGENTS.md | 5383 B | tracked |
| space4x/AGENTS.md | 5383 B | tracked |
| godgame/AGENTS.md | 5383 B | tracked |

### Docs/AGENTS.md (nested quickstarts)
| Path | Size |
|------|------|
| godgame/Docs/AGENTS.md | 1483 B |
| space4x/Docs/AGENTS.md | 1654 B |

### Key orientation/headless docs
| Path | Location |
|------|----------|
| TRI_PROJECT_BRIEFING.md | godgame/, space4x/ (root), puredots/Docs/Archive/ |
| headless_runbook.md | puredots/Docs/Headless/, headlessrebuildtool/, space4x/Docs/Headless/, godgame/Docs/Headless/, Tools/ |
| headlesstasks.md | puredots/Docs/Headless/, headlessrebuildtool/, Tools/ |
| recurring.md | puredots/Docs/Headless/ |
| recurringerrors.md | puredots/Docs/Headless/ |

### Docs/Agents/
- **Did not exist** prior to sweep; created for SWEEP_REPORT.md and SLOTS.md

### Canonical direction set (NORTH_STAR, DEMO_SLICE, etc.)
- **Not present** in unity_clean desktop; these exist in sibling `Tri` workspace

---

## LAPTOP (SSH)

### Top-level structure
- Same repos: godgame, headlessrebuildtool, puredots, space4x
- **Additional root docs (desktop does not have):**
  - `directive.md` (637 B) — canonical ops binder
  - `recurring.md` (230 B)
  - `recurringerrors.md` (214 B)
  - `advisory.md` (8295 B)
  - `MORNING_PROTOCOL.md`, `SETUP_REPORT.md`, `TODAY_AGENDA.md`, `skills.md`, `orbit.md`
  - Many `buildbox_*` dirs, `_worktrees`, `CodexBridge`, `_buildbox`, etc.

### Laptop-only / local-only classification
| File/Dir | Recommendation |
|----------|----------------|
| directive.md | **Promote to repo** — canonical ops binder; should be in repo or a pointer |
| recurring.md, recurringerrors.md | **Promote or sync** — known pitfalls; puredots/Docs/Headless/ has copies |
| advisory.md | **Keep local** or archive — machine-specific advisories |
| MORNING_PROTOCOL.md, TODAY_AGENDA.md, SETUP_REPORT.md | **Keep local** — session/status |
| skills.md, orbit.md | **Classify** — may be reference; list for review |
| buildbox_* dirs | **Local-only** — run artifacts, do not commit |
| _tmp_* | **Local-only** — temp files |

### AGENTS.md
- Same layout as desktop: puredots/, space4x/, godgame/ root AGENTS.md (project-first, 5383 B each)

---

## Differences (Desktop vs Laptop)

| Item | Desktop | Laptop |
|------|---------|--------|
| directive.md at root | ❌ | ✅ 637 B |
| recurring.md at root | ❌ | ✅ 230 B |
| recurringerrors.md at root | ❌ | ✅ 214 B |
| Root-level session/status docs | ❌ | ✅ (MORNING_PROTOCOL, TODAY_AGENDA, etc.) |
| buildbox_* / _worktrees | minimal | Many |
| Docs/Agents/SLOTS.md | created by sweep | to be added via PR |

---

## Recommended actions

1. **AGENTS.md** — Normalize to ROLE-FIRST in all three repos (done in this PR)
2. **SLOTS.md** — Add to puredots, space4x, godgame (done)
3. **directive.md** — If not in repo: add tombstone at root pointing to canonical location (e.g. `../Tri/directive.md` or promote into puredots/Docs/)
4. **recurring/recurringerrors** — Canonical location: `puredots/Docs/Headless/`. Root copies on laptop: treat as local; add pointer from root if needed
5. **Canonical docs (NORTH_STAR etc.)** — Missing in unity_clean. AGENTS.md references them; fallback: TRI_PROJECT_BRIEFING + Docs/Headless/*

---

## Statement

No validations were run. Bunker owns green.
