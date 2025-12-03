# Documentation Reorganization Summary

**Date**: 2025-11-27
**Reason**: Split responsibility between PureDOTS framework and game-specific documentation

---

## Problem

Documentation was scattered in `Docs/DesignNotes/` without clear distinction between:
- **Framework-level** systems (reusable, game-agnostic)
- **Game-specific** mechanics (Godgame villagers, Space4X fleets)

This made it unclear:
- Where teams should look for information
- What belongs in the framework vs. games
- How to propose framework extensions

---

## Solution

Reorganized documentation by **responsibility**:

```
Before:
Docs/DesignNotes/
├── RadicalAggregatesSystem.md (Godgame-specific)
├── GuildCurriculumSystem.md (Framework)
├── CameraIntegrationArchitecture.md (Space4X-specific)
└── ... (70+ files, mixed)

After:
Packages/com.moni.puredots/Documentation/DesignNotes/
├── GuildCurriculumSystem.md (Framework)
├── AnchoredCharactersSystem.md (Framework)
├── BorderPatrolAmbushSystem.md (Framework)
└── ... (50+ framework systems)

Assets/Projects/Godgame/Docs/Systems/
├── RadicalAggregatesSystem.md
├── RainMiraclesAndHand.md
├── VillagerJobs_DOTS.md
└── ... (8 Godgame systems)

Assets/Projects/Space4X/Docs/Systems/
├── CameraIntegrationArchitecture.md
├── CameraIntegrationSuggestions.md
└── ... (2 Space4X systems, more to come)

Docs/DesignNotes/LastLightVR/
├── LASTLIGHTVR_CONCEPT_SUMMARY.md
├── LASTLIGHTVR_INITIALIZATION_PROPOSAL.md
└── LastLightVR_ConceptTranscript.md
```

---

## What Moved Where

### PureDOTS Framework Docs
**Location**: `Packages/com.moni.puredots/Documentation/DesignNotes/`

**Count**: 50+ documents

**Categories**:
1. **Architecture & Patterns** (8 docs)
   - DataOrientedPractices, RewindPatterns, HistoryBufferPatterns, ThreadingAndScheduling, SoA_Expectations, SystemExecutionOrder, SystemIntegration, NeutralityLinting

2. **Core Infrastructure** (6 docs)
   - RegistryDomainPlan, RegistryContinuityContracts, RegistryHotColdSplits, RegistryLifecycle, MetaRegistryRoadmap, StateMachineFramework

3. **Generic Game Systems** (18 docs)
   - GuildCurriculum (5 docs), AnchoredCharacters, CelestialMechanics, BorderPatrol, BuffSystem, SkillProgression, HeritageAndKnowledge, MartialMastery, AbilityAutoCast, CraftingQuality, Economy, FactionAndGuild, QuestAndAdventure, EventSystem

4. **Spatial & Pathfinding** (5 docs)
   - SpatialPartitioning, SpatialServices, SpatialBrush, FlowFieldPathfinding, UniversalNavigation

5. **Resources & Production** (4 docs)
   - ResourceAuthoring, ResourceQuality, ResourceRegistry, ProductionChains

6. **Presentation & Tooling** (6 docs)
   - PresentationBridge, PresentationGuidelines, FoundationalSettings, RuntimeDeveloperSandbox, Scheduler, MetricEngine

7. **Domain-Specific (but generic)** (5 docs)
   - EnvironmentalEffects, PerceptionSystem, VFXPooling, VegetationLifecycle, VegetationAssets

8. **Integration** (4 docs)
   - SystemIntegration_BaselineAudit, SystemCentralization, SpawnerIntegration, ScenarioNarratives

### Godgame-Specific Docs
**Location**: `Assets/Projects/Godgame/Docs/Systems/`

**Count**: 8 documents

**Systems**:
- **Radical/Rebellion**: RadicalAggregates, RadicalMovement, RadicalResponse, RadicalQuickReference
- **Divine Intervention**: RainMiraclesAndHand
- **Villagers**: VillagerJobs_DOTS, SociopoliticalDynamics
- **Settlement**: MobileSettlement, EliteCrisis, IndustrialSector, NarrativeSituations

### Space4X-Specific Docs
**Location**: `Assets/Projects/Space4X/Docs/Systems/`

**Count**: 2 documents (more to be added as game develops)

**Systems**:
- **Camera**: CameraIntegrationArchitecture, CameraIntegrationSuggestions

### Special Cases
**LastLightVR**: Moved to `Docs/DesignNotes/LastLightVR/` (future project)

---

## New Documentation Structure

### Framework Documentation
**Entry Point**: [Packages/com.moni.puredots/Documentation/README.md](../Packages/com.moni.puredots/Documentation/README.md)

**Purpose**:
- Game-agnostic systems documentation
- Architecture patterns and best practices
- How to extend the framework

**Audience**: Framework developers, game teams looking to use/extend framework

### Game Documentation
**Entry Points**:
- [Assets/Projects/Godgame/Docs/README.md](../Assets/Projects/Godgame/Docs/README.md)
- [Assets/Projects/Space4X/Docs/README.md](../Assets/Projects/Space4X/Docs/README.md)

**Purpose**:
- Game-specific mechanics and systems
- How games use and extend the framework
- Game design documentation

**Audience**: Game team members, designers, artists

### Root Documentation
**Entry Point**: [Docs/INDEX.md](INDEX.md)

**Purpose**:
- Cross-project architecture
- Integration guides
- Project-wide references

**Audience**: All teams

---

## Index Files Created

### Framework Index
[Packages/com.moni.puredots/Documentation/README.md](../Packages/com.moni.puredots/Documentation/README.md)
- Lists all framework systems by category
- Explains framework principles
- Guides for using and extending framework

### Godgame Index
[Assets/Projects/Godgame/Docs/README.md](../Assets/Projects/Godgame/Docs/README.md)
- Lists Godgame-specific systems
- Explains how Godgame uses framework
- Game design pillars and concepts

### Space4X Index
[Assets/Projects/Space4X/Docs/README.md](../Assets/Projects/Space4X/Docs/README.md)
- Lists Space4X-specific systems
- Explains how Space4X uses framework
- Game design pillars and concepts

### Organization Guide
[Docs/DOCUMENTATION_ORGANIZATION_GUIDE.md](DOCUMENTATION_ORGANIZATION_GUIDE.md)
- **Decision tree**: Is this framework or game-specific?
- **Categorization rationale**: Doc-by-doc breakdown
- **Guidelines**: How to organize future documentation

---

## Benefits

### For Framework Team
- ✅ Clear scope: What belongs in framework vs. games
- ✅ Extension requests: Games can formally request framework features
- ✅ Reusability: Easier to find and reuse generic systems

### For Game Teams
- ✅ Clear ownership: Game teams own their game-specific docs
- ✅ Independence: Can document game mechanics without framework approval
- ✅ Integration clarity: Understand what framework provides

### For New Developers
- ✅ Orientation: Know where to look for information
- ✅ Separation of concerns: Framework vs. game implementation
- ✅ Onboarding: Clear path from framework → game integration

---

## Decision Criteria

### Framework (PureDOTS)
**YES if**:
- Works across multiple games (Godgame + Space4X + future)
- Defines generic components/interfaces
- Architecture pattern or infrastructure
- Uses generic tags/IDs, not game-specific types

**Examples**:
- ✅ GuildCurriculumSystem - Teaching mechanics (generic)
- ✅ AnchoredCharactersSystem - Character persistence (cross-game)
- ✅ BorderPatrolAmbushSystem - Patrol mechanics (game-agnostic core)

### Game-Specific
**NO if**:
- Specific to one game's theme/lore
- Uses hardcoded game-specific types ("Radical", "Villager", "Captain")
- Game design concept rather than technical system
- Only one game would ever use it

**Examples**:
- ❌ RadicalAggregatesSystem - Radicalization (Godgame-specific)
- ❌ RainMiraclesAndHand - Divine intervention (Godgame lore)
- ❌ CameraIntegrationArchitecture - Space4X camera (3D space-specific)

---

## Next Steps

### For Team Members

1. **Familiarize** with new structure:
   - Framework docs: `Packages/com.moni.puredots/Documentation/`
   - Your game docs: `Assets/Projects/{YourGame}/Docs/`

2. **Update bookmarks/links** to point to new locations

3. **When creating new docs**:
   - Ask: "Is this framework or game-specific?" (use decision tree)
   - Place in appropriate folder
   - Add entry to relevant README.md

4. **Extension requests**:
   - If your game needs a framework feature, file a request in `Docs/ExtensionRequests/`
   - See [ExtensionRequests/README.md](ExtensionRequests/README.md) for process

### For Onboarding New Developers

1. Read: [Docs/ORIENTATION_SUMMARY.md](ORIENTATION_SUMMARY.md)
2. Understand structure: [Docs/DOCUMENTATION_ORGANIZATION_GUIDE.md](DOCUMENTATION_ORGANIZATION_GUIDE.md)
3. Framework overview: [Packages/com.moni.puredots/Documentation/README.md](../Packages/com.moni.puredots/Documentation/README.md)
4. Game integration: [Docs/PUREDOTS_INTEGRATION_SPEC.md](PUREDOTS_INTEGRATION_SPEC.md)

---

## Files Modified

### Created
- `Packages/com.moni.puredots/Documentation/README.md` - Framework index
- `Assets/Projects/Godgame/Docs/README.md` - Godgame index
- `Assets/Projects/Space4X/Docs/README.md` - Space4X index
- `Docs/DOCUMENTATION_ORGANIZATION_GUIDE.md` - Organization guide
- `Docs/DOCUMENTATION_REORGANIZATION_SUMMARY.md` - This file

### Modified
- `Docs/INDEX.md` - Updated to reflect new structure

### Moved
- **50+ docs** from `Docs/DesignNotes/` to `Packages/com.moni.puredots/Documentation/DesignNotes/`
- **8 docs** from `Docs/DesignNotes/` to `Assets/Projects/Godgame/Docs/Systems/`
- **2 docs** from `Docs/DesignNotes/` to `Assets/Projects/Space4X/Docs/Systems/`
- **3 docs** to `Docs/DesignNotes/LastLightVR/`

---

## Validation

### Folders Created ✅
- `Packages/com.moni.puredots/Documentation/DesignNotes/`
- `Assets/Projects/Godgame/Docs/Systems/`
- `Assets/Projects/Godgame/Docs/Concepts/`
- `Assets/Projects/Space4X/Docs/Systems/`
- `Assets/Projects/Space4X/Docs/Concepts/`
- `Docs/DesignNotes/LastLightVR/`

### Index Files Created ✅
- Framework README
- Godgame README
- Space4X README
- Organization Guide

### Docs/INDEX.md Updated ✅
- Added "Documentation Organization" section
- Updated Design Notes section with new locations
- Added links to new READMEs

---

## Questions?

See:
- [DOCUMENTATION_ORGANIZATION_GUIDE.md](DOCUMENTATION_ORGANIZATION_GUIDE.md) - Decision tree and guidelines
- [Packages/com.moni.puredots/Documentation/README.md](../Packages/com.moni.puredots/Documentation/README.md) - Framework overview
- [TRI_PROJECT_BRIEFING.md](../TRI_PROJECT_BRIEFING.md) - Tri-project architecture

---

**Reorganization Completed**: 2025-11-27
**Status**: Complete ✅
**Impact**: Minimal - all docs moved, links updated, indices created
