# Documentation Consolidation - Quick Reference

**Date**: 2025-12-01
**Full Details**: See [DOCUMENTATION_CONSOLIDATION_PROPOSAL.md](DOCUMENTATION_CONSOLIDATION_PROPOSAL.md)

---

## TL;DR

**Current State**: 72 files in root Docs/ (too cluttered)
**Target**: ~30 focused docs + organized subfolders
**Time**: 3 weeks part-time (~15 hours total)

---

## Key Problems Identified

1. ❌ **72 loose files** in Docs/ root (should be ~30)
2. ❌ **Large temp files** (conole.md = 206KB of console logs)
3. ❌ **Overlapping summaries** (10+ implementation/progress reports)
4. ❌ **Missing best practices** for DOTS 1.4.x, C# 9, Unity Input System
5. ❌ **Scattered TODOs** (hard to find active work)

---

## Immediate Actions (This Week)

### 1. Delete Temp Files (~5 min)
```bash
# These appear to be console dumps/temp notes
rm Docs/conole.md          # 206KB console output
rm Docs/consoleerror.md    # 64KB error dump
rm Docs/pass.md            # 11KB unclear notes
```

### 2. Create Archive Folders (~5 min)
```bash
mkdir -p Docs/Archive/Progress/2025
mkdir -p Docs/Archive/Fixes/2025
mkdir -p Docs/Archive/Setup/2025
mkdir -p Docs/Archive/CompletedWork/2025
mkdir -p Docs/Archive/Analysis/2025
mkdir -p Docs/Tools
mkdir -p Docs/BestPractices
```

### 3. Archive Progress Reports (~30 min)
Move to `Docs/Archive/Progress/2025/`:
- Implementation_*.md (5 files)
- Progress*.md (2 files)
- *_Summary.md (10+ implementation summaries)
- Readiness_Assessment.md
- ROADMAP_STATUS.md

### 4. Archive Fix Documentation (~30 min)
Move to `Docs/Archive/Fixes/2025/`:
- Camera_*_Fix.md (3 files)
- Mining_Demo_*.md (6 files)
- *_Troubleshooting.md (3 files)
- Console_*.md (error analysis files)
- Render/SubScene fix docs

### 5. Move Tool Documentation (~10 min)
Move to `Docs/Tools/`:
- MCP_VFX_Graph_Tools.md (39KB)
- VFX_HELPER_FIXES.md
- WSL_Unity_MCP_Relay.md

---

## New Documentation Needed (Next 2 Weeks)

### Critical: Best Practices Guides

Create `Docs/BestPractices/` with:

1. **DOTS_1_4_Patterns.md** (~2 hours)
   - ISystem vs SystemBase
   - SystemAPI.Query patterns
   - Aspect-oriented queries
   - Baker improvements
   - Source generator gotchas

2. **CSharp9_Features.md** (~1.5 hours)
   - Records for DTOs
   - Init-only setters
   - Pattern matching in systems
   - Target-typed new
   - Function pointers (Burst-compatible)

3. **UnityInputSystem_ECS.md** (~2 hours)
   - Input System integration architecture
   - Command component pattern
   - Deterministic input handling
   - Multiplayer patterns
   - Testing input

4. **BurstOptimization.md** (~1.5 hours)
   - Burst checklist
   - SIMD vectorization
   - Common errors & fixes
   - Burst Inspector workflow

5. **JobSystemPatterns.md** (~1 hour)
   - Job type selection
   - ParallelWriter usage
   - Dependency optimization
   - Safety system tips

6. **MemoryLayoutOptimization.md** (~1.5 hours)
   - Hot/medium/cold component sizing
   - Cache-line awareness
   - Companion entity pattern
   - Archetype design

7. **EntityCommandBuffers.md** (~1 hour)
   - When to use ECB vs EntityManager
   - Playback timing
   - ParallelWriter patterns
   - Debugging tips

8. **ComponentDesignPatterns.md** (~1 hour)
   - Component sizing guidelines
   - Tag vs data components
   - Shared components
   - Enable/Disable patterns

9. **DeterminismChecklist.md** (~1 hour)
   - Fixed timestep requirements
   - RNG seeding
   - Float precision
   - Rewind-safe patterns

10. **README.md** (~30 min)
    - Best practices index
    - Navigation to all guides

**Total Time**: ~12 hours

---

## TODO Consolidation (Week 3)

### Current Mess
- Multiple TODO files with overlapping content
- No clear active vs backlog distinction
- Completed items not archived

### Proposed Structure

**Keep 3 TODO files:**
1. `PureDOTS_TODO.md` (root) - **Sprint work only** (2-3 weeks)
2. `Docs/TODO/BACKLOG.md` - **Consolidated backlog** by domain
3. Domain-specific (keep focused): SystemIntegration, Climate, Villager, etc.

**Archive completed** → `Docs/Archive/CompletedWork/{Year}/`

**Time**: ~2-3 hours

---

## File Count Reduction

| Location | Before | After | Reduction |
|----------|--------|-------|-----------|
| Docs/ root | 72 | ~30 | -42 files |
| Docs/Archive/ | 0 | ~40 | +40 (organized) |
| Docs/BestPractices/ | 0 | 10 | +10 (new) |
| Docs/Tools/ | 0 | 3 | +3 (moved) |

**Net Result**: Root folder 58% cleaner, historical context preserved

---

## Benefits

### For Developers
✅ **Clear navigation** - Find docs in seconds, not minutes
✅ **Best practices at hand** - Stop asking "how do I...?"
✅ **Active work visible** - Sprint TODO vs long-term backlog
✅ **Implementation-ready** - DOTS 1.4.x, C# 9, Input System guides

### For New Team Members
✅ **Faster onboarding** - Clear path from Getting Started → Best Practices
✅ **Pattern library** - See how experienced devs solve problems
✅ **Historical context** - Understand why decisions were made (in archive)

### For Project Health
✅ **Maintainable** - Clear policies for where things go
✅ **Searchable** - Organized by topic, not chronology
✅ **Up-to-date** - Quarterly review process prevents staleness

---

## Quick Win: Start Today (1 hour)

```bash
# 1. Create archive structure (5 min)
mkdir -p Docs/Archive/{Progress,Fixes,Setup,CompletedWork,Analysis}/2025
mkdir -p Docs/Tools
mkdir -p Docs/BestPractices

# 2. Delete obvious temp files (1 min)
rm Docs/conole.md Docs/consoleerror.md Docs/pass.md

# 3. Move tool docs (2 min)
mv Docs/MCP_VFX_Graph_Tools.md Docs/Tools/
mv Docs/VFX_HELPER_FIXES.md Docs/Tools/
mv Docs/WSL_Unity_MCP_Relay.md Docs/Tools/

# 4. Archive 5 biggest progress reports (10 min)
mv Docs/Implementation_*.md Docs/Archive/Progress/2025/
mv Docs/Progress*.md Docs/Archive/Progress/2025/
mv Docs/Readiness_Assessment.md Docs/Archive/Progress/2025/

# 5. Archive camera fix docs (5 min)
mv Docs/Camera_*_Fix.md Docs/Archive/Fixes/2025/
mv Docs/Camera_Debugging_Guide.md Docs/Archive/Fixes/2025/

# 6. Archive mining demo docs (5 min)
mv Docs/Mining_Demo_*.md Docs/Archive/Fixes/2025/
mv Docs/Mining_Loops_Demo_Status.md Docs/Archive/Fixes/2025/

# 7. Update INDEX.md references (30 min)
# Open INDEX.md and update links to archived files
# Add links to new Tools/ and BestPractices/ folders
```

**Result**: Root folder goes from 72 → ~50 files in 1 hour

---

## Maintenance Going Forward

### New Document Policy
- Add to **subfolder**, not root (unless high-level)
- Update **INDEX.md** immediately
- Link from **relevant READMEs**

### Progress Tracking
- Use **git commits** for daily progress
- Write **monthly summaries** if needed
- **Archive summaries** after 6 months

### TODO Management
- **Sprint** (PureDOTS_TODO.md): 2-3 weeks of work
- **Backlog** (TODO/BACKLOG.md): Everything else
- **Archive** when completed

### Best Practices Updates
- **Review quarterly** (with Unity/DOTS updates)
- **Update** when versions change
- **Incorporate** team learnings

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Breaking links | Search-replace moved files, validate before commit |
| Losing history | Archive with headers, don't delete |
| Stale best practices | Quarterly review calendar |
| Team resistance | Roll out incrementally, get feedback |

---

## Next Steps

1. ✅ **Review proposal** with team (you're here!)
2. ⬜ **Agree on archive strategy** (what to keep/delete)
3. ⬜ **Schedule Week 1 cleanup** (pick low-impact week)
4. ⬜ **Assign best practices authoring** (domain experts)
5. ⬜ **Set quarterly review** (calendar reminder)

---

## Questions to Resolve

1. **Archive policy**: Keep all progress reports or delete ones >1 year old?
2. **Best practices priority**: Which guides most urgent? (I recommend: DOTS 1.4 → Input System → C# 9)
3. **TODO consolidation**: Merge domain TODOs into BACKLOG.md or keep separate?
4. **Review schedule**: Quarterly? Semi-annual?

---

## Estimated Time Investment

| Phase | Time | When |
|-------|------|------|
| Week 1: Cleanup | 2-3 hours | This week |
| Week 2: Best Practices | 8-10 hours | Next week |
| Week 3: TODO Consolidation | 2-3 hours | Week after |
| **Total** | **12-16 hours** | **3 weeks** |

**ROI**: Saves every developer 5-10 minutes per day searching for docs = **2-4 hours/week saved per dev**

---

## Full Details

See [DOCUMENTATION_CONSOLIDATION_PROPOSAL.md](DOCUMENTATION_CONSOLIDATION_PROPOSAL.md) for:
- Complete file lists (what to archive/delete)
- Detailed outlines for all 10 best practice guides
- Archive file format template
- Maintenance guidelines
- Full implementation plan

---

*Maintainer: PureDOTS Documentation Team*
*Created: 2025-12-01*
