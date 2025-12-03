# Documentation Organization Guide

**Created**: 2025-11-27
**Purpose**: Define clear boundaries between PureDOTS framework documentation and game-specific documentation

---

## Organization Principles

### 1. PureDOTS Framework Documentation
**Location**: `Packages/com.moni.puredots/Documentation/`

**Criteria for PureDOTS docs**:
- ✅ Game-agnostic, reusable across multiple games
- ✅ Framework-level architecture and patterns
- ✅ Generic systems that can be configured per-game
- ✅ Infrastructure (registries, rewind, time management, etc.)
- ✅ Technical patterns (DOTS best practices, Burst optimization, etc.)
- ✅ Cross-game extension points (tags, enums, interfaces)

**Structure**:
```
Packages/com.moni.puredots/Documentation/
├── DesignNotes/          # Framework system designs
├── ExtensionRequests/    # Game → Framework requests
├── Integration/          # How games integrate with framework
└── Patterns/             # Reusable technical patterns
```

### 2. Game-Specific Documentation
**Locations**:
- `Assets/Projects/Godgame/Docs/`
- `Assets/Projects/Space4X/Docs/`

**Criteria for game-specific docs**:
- ✅ Game-specific mechanics and content
- ✅ Specific implementations using PureDOTS
- ✅ Game design concepts unique to that game
- ✅ Game-specific systems that extend the framework

**Structure** (per game):
```
Assets/Projects/{GameName}/Docs/
├── Concepts/      # High-level game design concepts
├── Mechanics/     # Detailed mechanic specifications
└── Systems/       # Game-specific system implementations
```

---

## Categorization Decision Tree

### Is it PureDOTS Framework?

**YES if**:
1. The system works across multiple games (Godgame + Space4X + future games)
2. It defines generic components/interfaces that games configure
3. It's an architecture pattern or infrastructure
4. Games would request it as an "extension" to the framework
5. It uses generic tags/IDs rather than game-specific types

**NO if (move to game project)**:
1. It's specific to one game's theme/lore
2. It uses hardcoded game-specific types (e.g., "Radical", "Villager", "Captain")
3. It's a game design concept rather than a technical system
4. Only one game would ever use it

---

## Doc-by-Doc Categorization

### PureDOTS Framework Docs
*Location*: `Packages/com.moni.puredots/Documentation/DesignNotes/`

#### Architecture & Patterns
- `DataOrientedPractices.md` - DOTS patterns and best practices
- `RewindPatterns.md` - Time rewind architecture
- `HistoryBufferPatterns.md` - Temporal data patterns
- `ThreadingAndScheduling.md` - Job system patterns
- `SoA_Expectations.md` - Struct of Arrays patterns
- `SystemExecutionOrder.md` - System scheduling patterns
- `SystemIntegration.md` - System integration patterns
- `NeutralityLinting.md` - Game-agnosticism enforcement

#### Core Framework Systems
- `RegistryDomainPlan.md` - Registry architecture
- `RegistryContinuityContracts.md` - Registry integrity
- `RegistryHotColdSplits.md` - Registry optimization
- `RegistryLifecycle.md` - Registry lifecycle
- `MetaRegistryRoadmap.md` - Registry evolution
- `TimeComponents.md` (if exists) - Time management
- `StateMachineFramework.md` - Generic state machines

#### Generic Game Systems (Configurable)
- `GuildCurriculumSystem.md` - Generic guild/teaching (works for both games)
- `AnchoredCharactersSystem.md` - Character persistence (cross-game)
- `CelestialMechanicsAndShadowSystem.md` - Light/shadow mechanics (cross-game)
- `BorderPatrolAmbushSystem.md` - Patrol/ambush (game-agnostic core)
- `BuffSystem.md` - Buff/debuff system (generic)
- `SkillProgressionSystem.md` - Generic skill progression
- `HeritageAndKnowledgeSystem.md` - Knowledge transmission (generic)
- `MartialMasterySystem.md` - Combat skill progression (generic)
- `AbilityAutoCastSystem.md` - Generic ability system
- `CraftingQualitySystem.md` - Quality mechanics (generic)
- `EconomySystem.md` - Generic economy patterns
- `FactionAndGuildSystem.md` - Generic aggregates
- `QuestAndAdventureSystem.md` - Generic questing
- `EventSystemConcepts.md` - Event architecture

#### Spatial & Pathfinding
- `SpatialPartitioning.md` - Spatial data structures
- `SpatialServicesConcepts.md` - Spatial services
- `SpatialBrushAndSelection.md` - Spatial tools
- `FlowFieldPathfinding.md` - Pathfinding patterns
- `UniversalNavigationSystem.md` - Navigation abstraction

#### Resource & Production
- `ResourceAuthoringAndConsumption.md` - Resource system
- `ResourceQualityAndProcessing.md` - Quality mechanics
- `ResourceRegistryPlan.md` - Resource registry
- `ProductionChains.md` - Production mechanics

#### Presentation & Tooling
- `PresentationBridgeContracts.md` - Sim→Presentation interface
- `PresentationGuidelines.md` - Presentation patterns
- `FoundationalSettingsSandbox.md` - Runtime tweaking system
- `RuntimeDeveloperSandbox.md` - Developer tools
- `ScenarioNarratives.md` - Scenario system
- `SchedulerAndQueueing.md` - Task scheduling
- `MetricEngine.md` - Telemetry system

#### Specific Domains (but generic within domain)
- `EnvironmentalEffects.md` - Weather/environment patterns
- `PerceptionSystem.md` - Perception/sensor patterns
- `VFXPoolingPlan.md` - VFX management
- `VegetationLifecycleAndChunks.md` - Vegetation system
- `VegetationAssets.md` - Vegetation authoring

### Godgame-Specific Docs
*Location*: `Assets/Projects/Godgame/Docs/Systems/`

#### Radical/Rebellion Systems
- `RadicalAggregatesSystem.md` - Radicalization mechanics (Godgame-specific)
- `RadicalMovementExamples.md` - Radical examples
- `RadicalResponseStrategies.md` - Response to radicals
- `RadicalSystemQuickReference.md` - Radical quick ref

#### Villager/God Mechanics
- `RainMiraclesAndHand.md` - Divine intervention (Godgame-specific)
- `VillagerJobs_DOTS.md` - Villager job system (if Godgame-specific)
- `SociopoliticalDynamics.md` - Social systems (likely Godgame)
- `MobileSettlementSystem.md` - Settlement mechanics (if Godgame)
- `NarrativeSituations.md` - Narrative system (if Godgame-specific)

#### Godgame Infrastructure
- `EliteCrisisSystem.md` - Crisis mechanics (if Godgame-specific)
- `IndustrialSectorSystem.md` - Industry (if Godgame-specific)

### Space4X-Specific Docs
*Location*: `Assets/Projects/Space4X/Docs/Systems/`

#### Camera & Rendering
- `CameraIntegrationArchitecture.md` - Space4X camera (if specific to Space4X)
- `CameraIntegrationSuggestions.md` - Camera patterns

#### Space4X Mechanics
- *(Will be identified as game develops specific systems)*

### Cross-Project Concepts
*Location*: `Docs/` (root level, applies to both games)

- `CONCEPT_CAPTURE_METHODS.md` - Documentation methodology
- `PUREDOTS_INTEGRATION_SPEC.md` - Integration specification
- `NEW_PROJECT_QUICKSTART.md` - New project setup
- `ORIENTATION_SUMMARY.md` - Project overview

### Special Cases / Need Review
These need deeper analysis to categorize:

- `LastLightVR_ConceptTranscript.md` - LastLightVR game (separate project?)
- `ProbabilisticSimulation_OutsideAnchorInfluence.md` - Framework or game?
- `SpawnerIntegration.md` - Framework or game-specific?
- `SystemCentralization.md` - Architecture pattern
- `SystemIntegration_BaselineAudit.md` - Audit doc

---

## Migration Process

### Phase 1: Create Folder Structure ✅
- `Packages/com.moni.puredots/Documentation/DesignNotes/`
- `Assets/Projects/Godgame/Docs/Systems/`
- `Assets/Projects/Godgame/Docs/Concepts/`
- `Assets/Projects/Space4X/Docs/Systems/`
- `Assets/Projects/Space4X/Docs/Concepts/`

### Phase 2: Move Framework Docs
- Move PureDOTS framework docs to `Packages/com.moni.puredots/Documentation/DesignNotes/`
- Update cross-references in moved docs

### Phase 3: Move Game-Specific Docs
- Move Godgame docs to `Assets/Projects/Godgame/Docs/Systems/`
- Move Space4X docs to `Assets/Projects/Space4X/Docs/Systems/`
- Update cross-references

### Phase 4: Create Index Files
- Create `README.md` or `INDEX.md` in each documentation folder
- List all docs with brief descriptions
- Link to related docs

### Phase 5: Update Main Documentation
- Update root `Docs/INDEX.md` with new structure
- Update `TRI_PROJECT_BRIEFING.md` with doc locations

---

## Guidelines for Future Documentation

### When Creating New Docs

**Ask yourself**:
1. Would this system work in a different game?
   - YES → PureDOTS framework
   - NO → Game-specific

2. Does it define generic components/interfaces?
   - YES → PureDOTS framework
   - NO → Probably game-specific

3. Does it use game-specific terminology (Radicals, Villagers, Captains)?
   - YES → Game-specific
   - NO → Could be framework

4. Would another game team request this as a framework extension?
   - YES → PureDOTS framework
   - NO → Game-specific

### Naming Conventions

**PureDOTS Framework**:
- Use generic names: `AggregateSystem.md`, not `VillagerGroupSystem.md`
- Focus on patterns: `BuffSystem.md`, not `VillagerMoodSystem.md`
- Emphasize reusability: `KnowledgeTransmission.md`, not `VillagerLearning.md`

**Game-Specific**:
- Use game-specific names: `RadicalAggregatesSystem.md` (Godgame)
- Reference game mechanics: `RainMiraclesAndHand.md` (Godgame)
- Specific implementations: `Space4XCameraSystem.md`

---

## See Also

- [CONCEPT_CAPTURE_METHODS.md](CONCEPT_CAPTURE_METHODS.md) - Documentation methodology
- [PUREDOTS_INTEGRATION_SPEC.md](PUREDOTS_INTEGRATION_SPEC.md) - How games integrate with PureDOTS
- [TRI_PROJECT_BRIEFING.md](../TRI_PROJECT_BRIEFING.md) - Tri-project architecture overview

---

**Maintainer**: Tri-Project Documentation Team
**Last Updated**: 2025-11-27
