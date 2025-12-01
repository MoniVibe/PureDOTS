# PureDOTS Documentation Index

**Last Updated**: 2025-11-27

---

## Documentation Organization

**NEW**: Documentation is now organized by responsibility:
- **Framework Docs**: `Packages/com.moni.puredots/Documentation/` - Game-agnostic systems
- **Godgame Docs**: `Assets/Projects/Godgame/Docs/` - God-game specific
- **Space4X Docs**: `Assets/Projects/Space4X/Docs/` - Space4X specific
- **Root Docs**: `Docs/` - Cross-project architecture and guides

See [DOCUMENTATION_ORGANIZATION_GUIDE.md](DOCUMENTATION_ORGANIZATION_GUIDE.md) for details.

---

## Entry Points
- `Docs/PUREDOTS_INTEGRATION_SPEC.md` – **canonical integration & extension guide** (how to interface with PureDOTS and extend it)
- `Docs/ORIENTATION_SUMMARY.md` – comprehensive codebase orientation (PureDOTS, Godgame, Space4X)
- `Docs/BehaviorAlignment_Summary.md` – **comprehensive guide to entity behaviors, alignments, outlooks, and aggregate dynamics**
- `Packages/com.moni.puredots/Documentation/PatternBible.md` – **catalog of 50+ emergent narrative patterns** (pre-implementation idea capture)
- `Docs/FOUNDATION_GAPS_QUICK_REFERENCE.md` – quick reference for immediate foundation work priorities
- `Docs/OUTSTANDING_TODOS_SUMMARY.md` – consolidated outstanding work; start here.
- `PureDOTS_TODO.md` – main project tracker.
- `Docs/INDEX.md` (this file) – navigation; see archived notes in `Docs/ScenePrep/Archived/` for retired hybrid guidance.

## New Project Setup
- `Docs/NEW_PROJECT_QUICKSTART.md` – **fast-track guide for starting new game projects**
- `Docs/CONCEPT_CAPTURE_METHODS.md` – documentation patterns across PureDOTS, Godgame, Space4X
- `Docs/DOCUMENTATION_ORGANIZATION_GUIDE.md` – **how documentation is organized between framework and games**
- `Docs/DesignNotes/LastLightVR/` – LastLightVR concept documents
  - `LASTLIGHTVR_INITIALIZATION_PROPOSAL.md` – detailed example of new project setup
  - `LASTLIGHTVR_CONCEPT_SUMMARY.md` – concept & action plan
  - `LastLightVR_ConceptTranscript.md` – raw concept transcript

## Truth Sources
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`
- `Docs/TruthSources/PlatformPerformance_TruthSource.md`

## TODO Trackers
- `PureDOTS_TODO.md`
- `Docs/TODO/SystemIntegration_TODO.md`
- `Docs/TODO/ClimateSystems_TODO.md`
- `Docs/TODO/SpatialServices_TODO.md`
- `Docs/TODO/VillagerSystems_TODO.md`
- `Docs/TODO/ResourcesFramework_TODO.md`
- `Docs/TODO/MiraclesFramework_TODO.md`
- `Docs/TODO/VegetationSystems_TODO.md`
- `Docs/TODO/DivineHandCamera_TODO.md`
- `Docs/TODO/TerraformingPrototype_TODO.md`
- `Docs/TODO/Utilities_TODO.md`
- `Docs/TODO/RegistryRewrite_TODO.md`
- `Docs/TODO/SpawnerFramework_TODO.md`
- Presentation: `Docs/TODO/PresentationBridge_TODO.md` (see architecture/guidelines below)

## Cross-Game Mechanics

**NEW**: Cross-game mechanics with thematic variations for both Godgame and Space4X:
- `Docs/Mechanics/` - Game mechanics that work across projects
  - [Miracles and Abilities](Mechanics/MiraclesAndAbilities.md) - Player powers with variable delivery and intensity
  - [Underground Spaces](Mechanics/UndergroundSpaces.md) - Excavatable caverns, undercities, hidden bases
  - [Floating Islands & Rogue Orbiters](Mechanics/FloatingIslandsAndRogueOrbiters.md) - Temporary exploration zones
  - [Special Days & Events](Mechanics/SpecialDaysAndEvents.md) - Holidays, blood moons, celestial events
  - [Instance Portals](Mechanics/InstancePortals.md) - Procedural dungeons and challenge zones
  - [Runewords & Synergies](Mechanics/RunewordsAndSynergies.md) - Diablo 2-style itemization combos
- See: [Mechanics README](Mechanics/README.md) for overview and integration notes

## Design Notes

**Framework Design Notes** (moved to `Packages/com.moni.puredots/Documentation/DesignNotes/`):
- Architecture & Patterns: DataOrientedPractices, RewindPatterns, HistoryBufferPatterns, ThreadingAndScheduling, SoA_Expectations, etc.
- Core Systems: Registry, StateMachine, Time Management
- Generic Game Systems: GuildCurriculum, AnchoredCharacters, Celestial, BorderPatrol, Buffs, Skills, DualLeadershipPattern, EnvironmentalQuestsAndLootVectors, etc.
- See: [PureDOTS Framework Documentation](../Packages/com.moni.puredots/Documentation/README.md)

**Godgame Design Notes** (moved to `Assets/Projects/Godgame/Docs/Systems/`):
- Radical/Rebellion Systems, Miracles, Villagers, Social Dynamics
- See: [Godgame Documentation](../Assets/Projects/Godgame/Docs/README.md)

**Space4X Design Notes** (moved to `Assets/Projects/Space4X/Docs/Systems/`):
- Camera Integration, Modular Hull System, Warrior Pilot Last Stand, Misc Vessels & Megastructures
- Fleet Systems (future), Colony Systems (future)
- See: [Space4X Documentation](../Assets/Projects/Space4X/Docs/README.md)

**Cross-Project Architecture** (remains in `Docs/`):
- `Docs/PresentationBridgeArchitecture.md` - Presentation architecture overview
- `Docs/Architecture/PureDOTS_As_Framework.md` - Framework contract
- `Docs/Architecture/Framework_Formalization_Summary.md`
- `Docs/Architecture/GameDOTS_Separation.md`

**Modding & User-Generated Content**:
- **Primary**: [UnifiedEditorAndSandbox.md](../Packages/com.moni.puredots/Documentation/DesignNotes/UnifiedEditorAndSandbox.md) - **Complete editor system** serving players (content), modders (gameplay), and developers (engine)
- Framework: [ModdingAndEditorFramework.md](../Packages/com.moni.puredots/Documentation/DesignNotes/ModdingAndEditorFramework.md) - Warcraft 3-style trigger system and data model
- Godgame: [CustomGameModding.md](../Assets/Projects/Godgame/Docs/Systems/CustomGameModding.md) - Custom scenarios, demon sieges, miracle puzzles
- Space4X: [CustomGameModding.md](../Assets/Projects/Space4X/Docs/Systems/CustomGameModding.md) - Fleet battles, diplomacy scenarios, exploration missions

## Architecture
- `Docs/Architecture/PureDOTS_As_Framework.md` - Framework architecture and contract
- `Docs/Architecture/Framework_Formalization_Summary.md` - Formalization overview
- `Docs/Architecture/GameDOTS_Separation.md` - Separation conventions

## AI Documentation
- `Docs/Guides/AI_Integration_Guide.md` - Comprehensive guide for integrating entities with AI pipeline (includes advanced behavior recipes)
- `Docs/AI_Gap_Audit.md` - Gap analysis and catalog of remaining AI work
- `Docs/AI_Backlog.md` - Prioritized implementation backlog (AI-001 through AI-008)
- `Docs/AI_Validation_Plan.md` - Testing and metrics framework for AI systems
- `Docs/AI_Readiness_Implementation_Summary.md` - Summary of completed AI integration work

## Guides & Authoring
- `Docs/PUREDOTS_INTEGRATION_SPEC.md` - **Canonical integration & extension specification** (start here for interfacing with PureDOTS)
- `Docs/ExtensionRequests/` - **Extension request directory** (game teams submit requests here)
  - `Docs/ExtensionRequests/README.md` - How to submit extension requests
  - `Docs/ExtensionRequests/TEMPLATE.md` - Request template
- `Docs/Integration/GAME_INTEGRATION_GUIDE.md` - Legacy integration guide (being absorbed into PUREDOTS_INTEGRATION_SPEC.md)
- `Docs/Guides/GameProject_Integration.md` - Integrating PureDOTS into game projects
- `Docs/Guides/LinkingExternalGameProjects.md` - Linking external game projects
- `Docs/Guides/SceneSetup.md`
- `Docs/Guides/Authoring/EnvironmentAndSpatialValidation.md`
- `Docs/Guides/GettingStarted.md`
- `Docs/Streaming_Content.md` - Streaming section content references and preload flow
- `Docs/Guides/PhysicsValidation.md` - Physics validation scenarios and sample parity

## QA & Testing
- `Docs/QA/IntegrationTestChecklist.md`
- `Docs/QA/PerformanceProfiles.md`
- `Docs/QA/BootstrapAudit.md` - Bootstrap infrastructure audit and validation checklist
- `Docs/QA/IL2CPP_AOT_Audit.md` - IL2CPP/AOT safety audit and preservation requirements
- `Docs/QA/TestingStrategy.md` - Testing pyramid, replay harness, soak suite, regression scenes
- `Docs/QA/TelemetryEnhancementPlan.md` - Telemetry, profiling, and performance monitoring plan

## CI & Automation
- `Docs/CI/CI_AutomationPlan.md` - CI pipeline, build automation, coverage, nightly runs

## Technical Debt
- `Docs/TECHNICAL_DEBT.md` - Comprehensive catalog of remaining technical debt items

## Future Extensions & Roadmap

### Unified Editor & Modding System
**Current**: Visual scripting (triggers), entity customization, terrain editing, three-tier permission system (Player/Modder/Developer)
**Planned Extensions** (see [UnifiedEditorAndSandbox.md](../Packages/com.moni.puredots/Documentation/DesignNotes/UnifiedEditorAndSandbox.md) for full roadmap):
- **Advanced Triggers** - Function definitions, loops, expressions
- **AI Scripting** - Visual behavior tree editor for custom AI
- **Cinematics** - Cutscene editor with camera paths and dialogue
- **Mod Marketplace** - Community ratings, featured mods, mod packs
- **Cross-Game Mods** - Share entities/systems between Godgame and Space4X
- **Live Tuning** - Real-time engine parameter adjustment in play mode

### Framework Extensions
**Current**: Core systems (Registry, Buffs, Skills, Patrol, etc.)
**Community-Requested**:
- See `Docs/ExtensionRequests/` for game team requests
- Anchored characters, guild curriculum, celestial mechanics currently in development

### Game-Specific Roadmaps
- **Godgame**: Radical systems, miracles, villager AI, social dynamics
- **Space4X**: Fleet combat, diplomacy, tech tree, colony management
- **LastLightVR**: VR mechanics, horror elements, narrative systems

---

Keep this index updated when new documents or TODOs are added.
