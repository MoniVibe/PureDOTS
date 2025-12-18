# PureDOTS Documentation Index

## Documentation Organization

Documentation is organized by responsibility:
- **Framework Docs**: `PureDOTS/Docs/` - PureDOTS framework documentation
- **Game Docs**: `Godgame/Docs/` and `Space4x/Docs/` - Game-specific documentation
- **Root**: `TRI_PROJECT_BRIEFING.md` - Project overview

---

## Entry Points
- `Docs/INTEGRATION_GUIDE.md` – **quick reference for PureDOTS integration** (how to interface with PureDOTS)
- `Docs/PUREDOTS_INTEGRATION_SPEC.md` – detailed extension procedures (for extending PureDOTS)
- `TRI_PROJECT_BRIEFING.md` (root) – project overview and coding patterns
- `Docs/INDEX.md` (this file) – documentation navigation

## New Project Setup
- `TRI_PROJECT_BRIEFING.md` (root) – project overview and structure
- `Docs/INTEGRATION_GUIDE.md` – PureDOTS integration reference
- `Docs/DesignNotes/LastLightVR/` – LastLightVR concept documents (examples)

## Truth Sources
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`
- `Docs/TruthSources/PlatformPerformance_TruthSource.md`

## Best Practices ⭐ NEW

**Implementation-friendly guides for DOTS 1.4.x, C# 9, Unity Input System, and performance optimization:**

- [BestPractices/README.md](BestPractices/README.md) - Best practices index and navigation
- [BestPractices/PROJECT_SEPARATION.md](BestPractices/PROJECT_SEPARATION.md) ⚠️ **READ FIRST** - PureDOTS vs Space4X vs Godgame separation
- [BestPractices/DOTS_1_4_Patterns.md](BestPractices/DOTS_1_4_Patterns.md) - DOTS 1.4.x implementation patterns
- [BestPractices/CSharp9_Features.md](BestPractices/CSharp9_Features.md) - C# 9 features for DOTS development
- [BestPractices/UnityInputSystem_ECS.md](BestPractices/UnityInputSystem_ECS.md) - Input System integration with ECS
- [BestPractices/BurstOptimization.md](BestPractices/BurstOptimization.md) - Burst compilation and optimization
- [BestPractices/JobSystemPatterns.md](BestPractices/JobSystemPatterns.md) - Job system best practices
- [BestPractices/MemoryLayoutOptimization.md](BestPractices/MemoryLayoutOptimization.md) - Cache-friendly component design
- [BestPractices/EntityCommandBuffers.md](BestPractices/EntityCommandBuffers.md) - ECB patterns and timing
- [BestPractices/ComponentDesignPatterns.md](BestPractices/ComponentDesignPatterns.md) - Component sizing and design
- [BestPractices/DeterminismChecklist.md](BestPractices/DeterminismChecklist.md) - Deterministic code checklist

See also: [FoundationGuidelines.md](FoundationGuidelines.md) - Core coding standards (P0-P17 patterns)


## Core Physics & Forces ⚡ NEW

**Runtime-flexible force system** for spatial, temporal, mass, and grid-based forces:
- [Concepts/Core/General_Forces_System.md](Concepts/Core/General_Forces_System.md) - **Technical specification** for Burst-friendly forces
- [Concepts/Core/Forces_Integration_Guide.md](Concepts/Core/Forces_Integration_Guide.md) - **Integration examples** for Godgame and Space4X
- [Concepts/Core/Multi_Force_Interactions.md](Concepts/Core/Multi_Force_Interactions.md) - **Emergent behaviors** from multiple simultaneous forces
- Features: Gravity wells, wind zones, vortex fields, temporal distortion, heightmap collision
- Runtime-configurable: Toggle fields, modify strength, change spatial grid at runtime
- Optimized: Layer masking, spatial partitioning, LOD system
- Deterministic: Full rewind support, same inputs → same outputs

## Social Dynamics & Reactions 🤝 NEW

**Personality-driven reaction system** where same events produce different outcomes:
- [Concepts/Core/Reactions_And_Relations_System.md](Concepts/Core/Reactions_And_Relations_System.md) - **Complete social dynamics system**
- [Concepts/Core/Relation_Bonuses_System.md](Concepts/Core/Relation_Bonuses_System.md) - **Strategic diplomacy through relation bonuses**
- Features: Event perception, reaction computation, relation changes, grudges, debts
- Context-dependent bonuses: Relations provide different bonuses based on alignment/outlook combinations
- Hate-driven bonuses: Intelligent entities gain production/military bonuses when threatened by hostile relations
- Personality-driven: Alignment and behavior traits modify reactions
- Emergent diplomacy: Warlike villages ally, peaceful villages shun aggressors, materialists cooperate with spiritualists
- Cascading consequences: Simple insults escalate to wars, gifts build alliances
- Aggregate reactions: Villages/fleets react as collectives based on member values
- Deterministic: Same personality + same event = same reaction

## Tactical Combat ⚔️ NEW

**Position-based combat system** where firing arcs and bay management create tactical depth:
- [Concepts/Core/Bay_And_Platform_Combat.md](Concepts/Core/Bay_And_Platform_Combat.md) - **Bay and platform combat system**
- Features: Combat positions (bays/platforms), firing arcs, line of sight, coordinated fire
- Space4X: Carriers open hangar bays to deploy mechs/titans attacking from the hull
- Godgame: Ships use broadside platforms where crews man cannons with specific firing arcs
- Bay states: Opening/closing transitions create vulnerability windows
- Arc management: Positioning matters (crossing-the-T, flanking, arc coverage)
- Damage progression: Bay health affects combat capability
- Parent-child relationships: Carriers/ships/fortresses empower occupants
- Coordinated volleys: Simultaneous firing from all positions for bonus damage
- Deterministic: Same positioning + same targets = same results

## Power & Energy Systems ⚡🔋 NEW

**Consistent power economy** with generation losses, battery discharge, and buffer requirements:
- [Concepts/Core/Power_And_Battery_System.md](Concepts/Core/Power_And_Battery_System.md) - **Complete power generation and storage system**
- Power generation: Reactors produce steady power with efficiency losses (70-99% based on tech)
- Distribution losses: Transmission resistance loses 2-15% power as heat
- Battery self-discharge: Passive drain at fixed rate (0.005-0.1%/sec based on tech)
- Power bank requirements: Weapons and shields require local buffers for burst operation
- Battery degradation: Charge cycles reduce capacity and efficiency over time
- Tech unlocks: Better reactors, superconductors, quantum batteries
- Strategic depth: Power management in combat (reactor damage, battery depletion, surge timing)
- Works for: Ships, mechs, buildings, stations, magic systems (mana as energy)
- Deterministic: Consistent power flow calculations

## Cross-Game Mechanics

**NEW**: Cross-game mechanics with thematic variations for both Godgame and Space4X:
- `Docs/Mechanics/` - Game mechanics that work across projects
  - [Miracles and Abilities](Mechanics/MiraclesAndAbilities.md) - Player powers with variable delivery and intensity
  - [Underground Spaces](Mechanics/UndergroundSpaces.md) - Excavatable caverns, undercities, hidden bases
  - [Floating Islands & Rogue Orbiters](Mechanics/FloatingIslandsAndRogueOrbiters.md) - Temporary exploration zones
  - [Special Days & Events](Mechanics/SpecialDaysAndEvents.md) - Holidays, blood moons, celestial events
  - [Instance Portals](Mechanics/InstancePortals.md) - Procedural dungeons and challenge zones
  - [Runewords & Synergies](Mechanics/RunewordsAndSynergies.md) - Diablo 2-style itemization combos
  - [Entertainment & Performers](Mechanics/EntertainmentAndPerformers.md) - Musicians, dancers, bards providing morale
  - [Wonder Construction](Mechanics/WonderConstruction.md) - Multi-stage monument building with professional workers
  - [Limb & Organ Grafting](Mechanics/LimbAndOrganGrafting.md) ⚠️ - Body horror grafting system (Mature 17+)
  - [Memories & Lessons](Mechanics/MemoriesAndLessons.md) - Cultural preservation and context-triggered buffs
  - [Consciousness Transference](Mechanics/ConsciousnessTransference.md) ⚠️ - Psychic inheritance, possession, neural override (Mature 17+)
  - [Death Continuity & Undead Origins](Mechanics/DeathContinuityAndUndeadOrigins.md) ⚠️ - Undead from actual corpses, spirit continuity (Mature 17+)
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
- [Architecture/BOUNDARY_CLASSIFICATION.md](Architecture/BOUNDARY_CLASSIFICATION.md) ⚠️ **NEW** - Complete component catalog (PureDOTS vs Game Layer)
- [Architecture/BOUNDARY_CONTRACT.md](Architecture/BOUNDARY_CONTRACT.md) ⚠️ **NEW** - Boundary rules and decision framework
- [Architecture/MIGRATION_PLAN.md](Architecture/MIGRATION_PLAN.md) ⚠️ **NEW** - Step-by-step fixes for boundary violations

## AI Documentation
- `Docs/Guides/AI_Integration_Guide.md` - Comprehensive guide for integrating entities with AI pipeline (includes advanced behavior recipes)
- `Docs/AI_Gap_Audit.md` - Gap analysis and catalog of remaining AI work
- `Docs/AI_Backlog.md` - Prioritized implementation backlog (AI-001 through AI-008)
- `Docs/AI_Validation_Plan.md` - Testing and metrics framework for AI systems
- `Docs/Archive/Progress/2025/AI_Readiness_Implementation_Summary.md` - Summary of completed AI integration work (archived)

## Guides & Authoring
- `Docs/PUREDOTS_INTEGRATION_SPEC.md` - **Canonical integration & extension specification** (start here for interfacing with PureDOTS)
- `Docs/FoundationGuidelines.md` - **Core coding standards** (P0-P17 patterns, critical DOTS rules)
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

## Tools & Utilities

**Tool-specific documentation:**
- `Docs/Tools/MCP_VFX_Graph_Tools.md` - MCP VFX Graph tools
- `Docs/Tools/VFX_HELPER_FIXES.md` - VFX helper fixes
- `Docs/Tools/WSL_Unity_MCP_Relay.md` - WSL Unity MCP relay

## Archive

**Historical documentation organized by category:**

- `Docs/Archive/Progress/2025/` - Progress reports and implementation summaries
- `Docs/Archive/Fixes/2025/` - Fixed issues and troubleshooting guides
- `Docs/Archive/Setup/2025/` - One-off setup guides and migration docs
- `Docs/Archive/Analysis/2025/` - Analysis documents and design notes
- `Docs/Archive/AgentPrompts/2025/` - Agent prompt files
- `Docs/Archive/CompletedWork/2025/` - Completed TODO items

**Archive Policy:** Documents are archived after 6 months or when superseded. Archive headers indicate reason and current reference.

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
