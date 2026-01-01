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

## Entity Stats & Archetypes ⭐ NEW
- [Concepts/Core/Entity_Stats_And_Archetypes_Canonical.md](Concepts/Core/Entity_Stats_And_Archetypes_Canonical.md) – **canonical** stat layers (core vs domain) + archetype schema (tags/weights/policies)
- [Concepts/Core/Archetypical_Theme_Mapping.md](Concepts/Core/Archetypical_Theme_Mapping.md) – **archetypical theme mapping** for alignment/outlook/behavior combinations (visual aesthetics, cultural expressions, presentation theming)
- [Concepts/Core/Entity_Traversal_System.md](Concepts/Core/Entity_Traversal_System.md) – **traversal graph system** for jump/climb/crawl/squeeze/drop (size-gated connectivity, deterministic execution)
- [Concepts/Core/Tech_Tree_Flavors.md](Concepts/Core/Tech_Tree_Flavors.md) – **tech tree flavors** for cross-family, divergent, convergent, and outlook-specific techs (weapons, spells, modules, facilities, implants)
- [Concepts/Core/Scenery_Desirability_And_Taxation.md](Concepts/Core/Scenery_Desirability_And_Taxation.md) – **scenery, desirability, and taxation systems** (flora/fauna-based scenery, housing preferences, behavior-driven taxation, segregation policies)

## UI / Presentation
- [Presentation/TRI_UI_Launch_MainMenu.md](Presentation/TRI_UI_Launch_MainMenu.md) – shared launch/main menu + worldgen + time-controls contract
- [Presentation/TRI_UI_BehaviorCloud_Widget.md](Presentation/TRI_UI_BehaviorCloud_Widget.md) – behavior cloud “card shell + RenderTexture” spec

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
- [Concepts/Core/Locomotion_System.md](Concepts/Core/Locomotion_System.md) - **Multi-modal movement system** with directionality and mode switching
- Features: Gravity wells, wind zones, vortex fields, temporal distortion, heightmap collision
- Locomotion modes: Walking, running, flying, hovering, gliding, swimming, climbing, and more
- Directionality: Mono-directional, bi-directional, planar omni, volumetric omni, custom patterns
- Runtime mode switching: Context-aware selection based on terrain, stamina, altitude, and constraints
- Resource costs: Stamina and energy consumption per locomotion mode
- Cross-game support: Same system for villagers, creatures, ships, mechs, spacecraft
- Runtime-configurable: Toggle fields, modify strength, change spatial grid at runtime
- Optimized: Layer masking, spatial partitioning, LOD system, Burst-compiled
- Deterministic: Full rewind support, same inputs → same outputs

## Social Dynamics & Reactions 🤝 NEW

**Personality-driven reaction system** where same events produce different outcomes:
- [Concepts/Core/Reactions_And_Relations_System.md](Concepts/Core/Reactions_And_Relations_System.md) - **Complete social dynamics system**
- [Concepts/Core/Relation_Bonuses_System.md](Concepts/Core/Relation_Bonuses_System.md) - **Strategic diplomacy through relation bonuses**
- [Concepts/Core/Communication_And_Language_System.md](Concepts/Core/Communication_And_Language_System.md) - **Communication, languages, and spell casting**
- [Concepts/Core/Reputation_System.md](Concepts/Core/Reputation_System.md) - **Reputation, trust, and guild membership systems**
- [Concepts/Core/Dialogue_Content_System.md](Concepts/Core/Dialogue_Content_System.md) - **Dialogue topics, deception, influence, and cooperation**
- [Concepts/Core/Entity_Cooperation_System.md](Concepts/Core/Entity_Cooperation_System.md) - **Multi-domain entity cooperation and mutual care**
- Cooperation types: Magic pooling, ritual casting, coordinated volleys, collaborative research/crafting, crew coordination, music ensembles
- Cohesion mechanics: Skills (40%) + Relations (30%) + Communication (20%) + Experience (10%) determines cooperation quality
- Efficiency scaling: 1.5× to 3× effectiveness with perfect cohesion depending on cooperation type
- Magic cooperation: Mages channel mana to powerful allies, increases cast speed and efficiency from one source
- Combat cooperation: Archers/bombardiers coordinate fire, phalanx cohesion (400 warriors as one)
- Production cooperation: Researchers/craftsmen collaborate, quality depends on skills and cohesion
- Facility cooperation: Officers, captains, pilots + operators + hangar crews coordinate using comms/sensors
- Social cooperation: Band members, companions develop shared sign languages
- Mutual care: Mechanics care about pilots surviving, pilots care about mechanics sleeping/eating (morale +26%, performance +17%)
- Multi-layer cooperation: Strategic → Operational → Tactical → Individual cascading bonuses
- Cross-game: Magic circles (Godgame), flight deck crews (Space4X), both use same framework
- Features: Event perception, reaction computation, relation changes, grudges, debts
- Dialogue topics: Social, tactical, strategic, personal, transactional (greetings, romance, familial, neighborly, outsider, teaching/learning)
- Profile sharing: Entities reveal/lie about identity, profession, guild, skills, intentions
- Deception mechanics: Charisma (lying) vs Insight (detecting lies), difficulty-based checks
- Influence systems: Intimidation (demoralize -80%), rally (inspire +80%), persuasion
- Memory tapping: Entities tap shared memories (family, home, legacy, patriotism, glory) for temporary bonuses using focus
- Shared memory scaling: More participants = stronger bonuses (diminishing returns), collective focus pool = longer duration
- Stronghold rallying: High charisma leaders rally entire fortresses with structured speeches (Opening → Climax → Closing)
- Speech analysis: Quality based on structure, memory invocations, emotional impact, charisma amplification
- Focus resource: Collective pool from all participants, consumed per second, determines rally duration
- Commands: Authority-based compliance (subordinates obey based on rank, relation, urgency)
- Requests/demands: Help requests (relation-based), coercive demands (threat-backed)
- Cooperation: Party formation, tactical callouts, coordinated combat
- Context-aware responses: Same statement, different outcomes based on relations/reputation/personality/relationship context
- Reputation domains: Trading, combat, diplomacy, magic, crafting, leadership (separate scores)
- Action-based reputation: Honest deals (+rep), swindles (-rep), heroism (+rep), atrocities (-rep)
- Gossip & spread: Witnesses share information, reputation spreads organically (60% impact for second-hand)
- Individual & aggregate: Both single entities and groups (guilds, factions, settlements) have reputations
- Guild signs: Secret gestures for membership proof, magical door access, teachable to trusted allies
- Emergency transfer: Dying members can teach guild signs to refugees for safe house access
- Trust learning: Entities remember who lied (deception history), adjust future trust accordingly
- Reputation effects: Unlocks guild access, trade discounts, quest availability, teaching opportunities
- Slow change: Trust hard to build, easy to lose, slow to rebuild (reputation decay over time)
- Aggregate reputation: Groups with bad reputations struggle to find trading partners
- Communication hierarchy: Native language → Known languages → General signs (universal gestures)
- Language system: Multiple languages with proficiency levels, learning progression, teaching
- General signs: Universal gestures for basic communication (prone to miscommunication 30-60%)
- Intent & deception: Share true intentions or disguise them, detection based on insight vs deception skill
- Spell languages: Verbal incantations require specific languages (Ancient Arcane, Elvish, etc.)
- Spell signs: Gesture-based magic using hand/limb movements (somatic components)
- Miscommunication gameplay: Language barriers create emergent narratives, relation penalties
- Cross-game: NPC dialogue (Godgame), alien diplomacy (Space4X), AI communication (synthetic languages)
- Context-dependent bonuses: Relations provide different bonuses based on alignment/outlook combinations
- Hate-driven bonuses: Intelligent entities gain production/military bonuses when threatened by hostile relations
- Personality-driven: Alignment and behavior traits modify reactions
- Emergent diplomacy: Warlike villages ally, peaceful villages shun aggressors, materialists cooperate with spiritualists
- Cascading consequences: Simple insults escalate to wars, gifts build alliances
- Aggregate reactions: Villages/fleets react as collectives based on member values
- Deterministic: Same personality + same event = same reaction

## Tactical Combat ⚔️ NEW

**Position-based combat system** where firing arcs and bay management create tactical depth:
- [Concepts/Core/Combat_Mechanics_Core.md](Concepts/Core/Combat_Mechanics_Core.md) - **Core combat mechanics** (accuracy, damage, knockback, stability)
- [Concepts/Core/Bay_And_Platform_Combat.md](Concepts/Core/Bay_And_Platform_Combat.md) - **Bay and platform combat system**
- [Concepts/Core/Mobile_Cities_And_Land_Carriers.md](Concepts/Core/Mobile_Cities_And_Land_Carriers.md) - **Mobile cities and land carriers**
- [Concepts/Core/Tactical_Commands_And_Formations.md](Concepts/Core/Tactical_Commands_And_Formations.md) - **Tactical commands and formation system**
- Tactical commands: Follow, attack, defend, stay, formations, retreat through communication system
- Command compliance: Authority + relations + personality + priority determine obedience
- Formation types: Phalanx, shield wall, wedge, testudo, skirmish, square, line, column (infantry), line ahead/abreast (naval)
- Formation training: Entities must learn formations through drilling, proficiency 0% to 100%
- Formation cohesion: Proficiency (50%) + Relations (30%) + Position Adherence (20%) determines integrity
- Formation bonuses: Phalanx (+100% defense, immune to flanking/charges, 50% speed), Wedge (+60% attack, -20% defense), Testudo (+200% defense, 30% speed)
- Bonuses scale with cohesion: 50% cohesion = 50% of bonuses (phalanx 100% defense becomes +50%)
- Breaking conditions: Casualties, morale collapse, leader death, flanking attacks, disruption, cohesion below threshold
- Formation strength example: Novice phalanx (30% proficiency, 40% relations) = 50% cohesion vs Veteran phalanx (90% proficiency, 80% relations) = 95% cohesion
- Communication-based: Commands use language/clarity system, can be misunderstood, hand signals vs verbal orders
- Accuracy disruption: Damage and knockback reduce accuracy, offset by physical strength and focus usage
- Stability system: (Strength + log(Mass)) / 2 + FocusUsage determines disruption resistance (0-95% dampening)
- Damage type modifiers: Physical 1.0×, Lightning 1.5×, Psychic 1.8×, Void 2.0× disruption
- Recovery: Entities regain accuracy over time (decay rate configurable per entity)
- Features: Combat positions (bays/platforms), firing arcs, line of sight, coordinated fire
- Space4X: Carriers open hangar bays to deploy mechs/titans attacking from the hull
- Godgame: Ships use broadside platforms where crews man cannons with specific firing arcs
- Mobile cities: Tech progression enables cities on treads, walker limbs, hover, or anti-grav platforms
- Land carriers: Advanced mobile cities deploy units from combat bays (mechs, vehicles, troops, artillery)
- Cross-game parallel: Godgame land carriers behave like Space4X ship carriers but on terrain
- Locomotion variety: Treads, walkers (bipedal/quad/hex/octo), hover, anti-gravity, hybrid
- Strategic uses: Nomadic civilizations, siege platforms, mobile production, resource harvesting while moving
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

## Magic & Ritual Systems 🔮⚡ NEW

**Focus-fueled sustained magic** where intensity scales with investment and interruption causes dangerous backlash:
- [Concepts/Core/Ritual_Magic_System.md](Concepts/Core/Ritual_Magic_System.md) - **Complete ritual magic system**
- Ritual types: Barriers, shields, storms (blizzard/lightning/flame/acid/void), healing rains, auras, control fields, summoning
- Focus-based intensity: Baseline focus to maintain, more focus = more power (linear/exponential/logarithmic/stepped scaling)
- Concentration mechanics: Must maintain concentration, fueled by focus, checked by mental stats
- Disruption sources: Damage, stuns, silences (verbal rituals), knockbacks, forced movement, mana depletion, fear
- Concentration checks: Mental Fortitude × Skill × Resistance vs Disruption Severity
- Stat resistance: Willpower, discipline, concentration skill resist interruption
- Alignment consequences: Lawful (safe, 10× damage), Neutral (moderate, 50× damage), Chaotic (DANGEROUS, 200× damage + catastrophic effects)
- Backlash severity: Minor (energy dissipates) → Moderate (temporary effects) → Major (dangerous explosion) → Catastrophic (reality-warping disaster)
- Chaotic ritual interruption: Reality tears, unintended summons, mass madness, portal leaks, chain lightning, permanent curses
- Cooperative rituals: Multi-caster bonuses (up to 3× intensity at perfect cohesion), uses cooperation system
- Intensity examples: Blizzard 1.0 (10 dmg/sec, -20% speed) → 10.0 (100 dmg/sec, -90% speed, 50% freeze)
- Space4X ship rituals: Siege beams (focused energy), repair clouds (nanite healing), jam fields (sensor disruption), shield links (unified defense)
- Ship cooperation: Multiple ships pool power for massive effects, interrupted beams cause energy feedback
- Example consequences: Void rift interrupted at 9.0 intensity = 1,800 damage + reality tear + summon 3d6 void horrors + mass madness + portal leak
- Cross-game: Magical rituals (Godgame), psionic/tech rituals (Space4X), both use same framework

## Environmental Resources & Corruption 🔮☢️ NEW

**Tiberium-like crystal growth** with exponential propagation and environmental consequences:
- [Concepts/Core/Catalytic_Crystal_System.md](Concepts/Core/Catalytic_Crystal_System.md) - **Complete catalytic crystal system**
- Crystal types: Green (10× energy), Blue (15× energy, slow growth), Red (8× energy, explosive), Purple (20× energy, extreme mutations)
- Advanced types: Nexus (accelerates nearby growth), Reactive (explodes when damaged), Psionic (affects minds)
- Self-propagating: Exponential growth curves, spreads 5-350 m²/hour depending on stage
- Energy density: 100-200 kWh/kg (10-20× better than coal), massive power source
- Flora corruption stages: Healthy → Wilting (10-30%) → Infested (30-60%) → Mutated (60-90%) → Crystalline (90-100%, death)
- Fauna mutations: Minor → Moderate → Major → Abomination (extremely dangerous)
- Mutation types: Crystalline hide, extra limbs, radioactive bite, enhanced senses, regenerative healing, size increase
- Terrain corruption: Soil toxicity, water contamination, structural collapse, crystal veins through bedrock
- Radiation mechanics: Accumulated dose (Sieverts), lethal at 50+ Sv, protective equipment required
- Growth stages: Seedling (<10kg) → Sprouting (10-100kg) → Mature (100-1000kg) → Blooming (1000-10000kg) → Overgrown (>10000kg) → Critical (unstable)
- Bloom events: 10× growth rate, creates mutant abominations, devastates biomes
- Detonations: Explosive crystals create 5,000 kg TNT equivalent blasts, chain reactions
- Harvesting methods: Manual (safe, 90% recovery), explosives (dangerous, 20% recovery), controlled burn (0% recovery, prevents spread)
- Containment: Sonic fences (60% reduction), stasis fields (100% reduction), nanite swarms (active removal), underground isolation
- Excision strategies: Explosive removal, thermal excision, biological agents, teleportation, underground containment
- Strategic dilemma: Exploit for massive energy vs protect environment from corruption
- Cross-game: Crystal fields threatening villages (Godgame), asteroid infestations, space station breaches, planetary corruption (Space4X)
- Deterministic: Same growth conditions → same spread patterns

## UI/UX & Information Systems 📖🖱️ NEW

**Paradox-style hierarchical tooltips** that make the game self-documenting:
- [Concepts/Core/Hierarchical_Tooltip_System.md](Concepts/Core/Hierarchical_Tooltip_System.md) - **Complete tooltip and information architecture**
- Three-tier information hierarchy: Primary tooltip → Sub-tooltips → Deep explanations
- Hover mechanics: 3-second grace period, MMB pin, Shift+Hover shortcuts
- Game-specific implementations: Villagers (Godgame), Ships/Colonies (Space4X)
- Cross-system integration: Shows forces, reactions, combat positions, power banks, relations
- Performance optimized: Pooling, lazy loading, spatial culling, text caching
- Color-coded highlights: Blue (stats), Green (bonuses), Red (penalties), Orange (mechanics)
- Accessibility: Colorblind support, font scaling, screen reader compatible
- Self-documenting: Players learn mechanics by exploring tooltips (no wiki required)
- Consistent cross-game: Same UX patterns in both Godgame and Space4X

## Data Visualization & Overlays 🗺️📊 NEW

**Interactive overlay system** for visualizing and manipulating world data:
- [Concepts/Core/Overlay_Visualization_System.md](Concepts/Core/Overlay_Visualization_System.md) - **Complete overlay system**
- Resource overlay: Distribution, quality, rarity, depletion tracking with heat maps
- Climate overlay: Temperature, moisture, wind patterns, biome visualization and modification
- Moisture grid: Rainfall, aquifers, water flow, irrigation coverage, drought risk
- Entity traffic: Movement patterns, congestion detection, pathfinding bottlenecks
- Aggregate entities: Faction positions, group status, centroid markers, territory boundaries
- Pollution overlay (future): Air/water/soil contamination, spread vectors, cleanup zones
- Multi-layer composition: Blend multiple overlays with opacity and blend modes
- Interactive tooltips: Contextual information, action buttons, modification costs
- Selection mechanics: Click tiles/deposits/groups, area select, zoom-to-detail, comparison mode
- Cross-game variants: Godgame (2D terrain, miracles) vs Space4X (3D sectors, terraforming)
- Performance: Burst-compiled generation (<2ms), GPU instanced rendering, 10K+ tiles with 3 overlays (<5ms)
- Accessibility: 4 colorblind modes, pattern overlays, text labels, scalable icons, high contrast

## Sandbox Mode & Runtime Modding 🎮🔧 NEW

**Garry's Mod-like creative freedom** - Everything is opt-in, moddable, and runtime-adjustable:
- [Concepts/Core/Sandbox_Mode_And_Runtime_Modding.md](Concepts/Core/Sandbox_Mode_And_Runtime_Modding.md) - **Complete modding and sandbox system**
- [Concepts/Core/Held_Entity_Modification.md](Concepts/Core/Held_Entity_Modification.md) - **Debug tool for modifying held entities** (alignments, outlooks, behaviors, archetypes)
- Feature toggles: All gameplay systems opt-in (combat, social, economic, environmental, AI)
- Runtime parameters: Every value exposed for live modification (damage multipliers, patience rates, production speeds)
- Hot-reload: Apply changes without restart (parameter changes <0.1ms, feature toggles <10ms, full reload <200ms)
- Mod profiles: Save/load configurations, preset profiles (Vanilla, Hardcore, Peaceful, Roleplay, Speed Run, Chaos)
- Sandbox tools: In-game tweaker UI with sliders, search/filter, real-time preview
- Console commands: Text-based power user interface (set/get/reset parameters, spawn entities, load profiles)
- Modding API: C# hooks for external mods (parameter callbacks, event hooks, entity spawning)
- Entity customization: Spawn entities with custom stats, modify existing entities in real-time
- Held entity modification: Modify alignments, outlooks, behaviors, archetypes of entities while held (debug flag gated)
- Persistent configs: Save/load/export/import configurations across sessions
- Cross-game support: Same modding system for Godgame and Space4X with contextualized parameters
- Performance: Optimized hot-reload, batch parameter changes, component enablement for toggles
- Accessibility: Players choose complexity level, can enable/disable any feature at any time

## World Generation & Procedural Systems 🌍🎲 NEW

**Procedural pantheons and factions** - Every playthrough generates unique gods/forces with rerolled behaviors:
- [Concepts/Core/Procedural_World_Generation_System.md](Concepts/Core/Procedural_World_Generation_System.md) - **Complete procedural generation system**
- God/force archetypes: Templates with alignment bounds, personality weights, domain definitions
- Procedural generation: Seed-based randomization creates unique personalities each playthrough
- Player configuration: Pre-generation settings (pantheon size, composition, difficulty, rivalry guarantees)
- Cross-game compatibility: Godgame gods and Space4X cosmic forces use same framework
- Deterministic: Same seed → same pantheon (rewind-friendly)
- Pantheon validation: Ensures balance (rival pairs, alignment distribution, narrative coherence)
- Integration: Generated gods interface with forces, reactions, worship, tooltips, and miracles
- Space4X variant: Cosmic forces control sectors, grant tech bonuses, provide artifacts
- Replayability: No two playthroughs have identical pantheons/factions
- Technical: Burst-compiled generation (<100ms), blob asset archetypes, minimal runtime overhead

## Divine Systems (Godgame) ⚡👑 NEW

**Rival gods control nature** - Diplomacy with pantheon unlocks permanent powers:
- [Concepts/Core/Rival_Gods_Diplomacy_System.md](Concepts/Core/Rival_Gods_Diplomacy_System.md) - **Complete divine politics system**
- Pantheon: 9 primary gods control time, growth, wind, fire, water, fortune, earth, life, death
- Mana as debt/credit: Separate mana balance with each god, worship points fuel transactions
- Worship economy: Villagers generate worship, temples direct distribution, strategic allocation puzzle
- God relations: Interloper → Tolerated → Accepted → Allied → Conjoined (permanent unlock)
- Miracle costs: Relations affect mana (0× at Conjoined, 2× at Nemesis)
- God absorption: Conjoined status grants FREE miracles forever, redirects worship to other gods
- Competing interests: Life Cluster vs Destruction Cluster, Fire vs Water, Life vs Death, Order vs Chaos
- Divine interference: Nemesis gods sabotage player (wildfires, disasters, curses, divine smites)
- Conjoined powers: FREE miracles in domain forever, passive bonuses, ultimate abilities
- Opposing pair mastery: Conjoin rivals (Fire + Water) for synergy powers (Steam domain)
- Strategic paths: Life Cluster (helpful), Destruction Cluster (wrathful), Balance (diplomatic), Ultimate (opposing pairs)
- Integration: Uses reactions system for god responses, forces for interference, alignment for compatibility, procedural generation for pantheon creation

## Experimental Features ⚠️🧪 NEW

**High-risk, optional features** that may be cut due to UX/accessibility concerns:
- [Concepts/Experimental/Chaos_Entity_System.md](Concepts/Experimental/Chaos_Entity_System.md) - **Chaos Entity / Trickster meta-antagonist**
- Fourth-wall breaking: Meta-enemy that manipulates UI, controls, and player perception
- Opt-in only: Never enabled by default, requires explicit player consent with warnings
- Safety features: 5-second warnings, ESC to cancel, settings toggle, blackout conditions
- Reward system: Tolerating chaos grants unique unlocks, banishment quest for ultimate power
- Two variants: Fourth-wall breaking (risky) vs In-World Trickster God (safer)
- Space4X variant: Quantum Anomaly that creates spatial/temporal interference
- Accessibility warnings: Not recommended for motion/photosensitivity or motor difficulties
- Telemetry tracked: Cancel rates, disable rates, event tolerance to measure reception
- May be cut: Extensive playtesting required before committing to implementation
- Alternative approaches: Chaos as difficulty modifier, gameplay mechanic, or narrative device

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

## AI Documentation & Telemetry 🤖📊 NEW
- [Concepts/Core/AI_Telemetry_System.md](Concepts/Core/AI_Telemetry_System.md) - **Complete AI training telemetry** for headless builds
- `Docs/Guides/AI_Integration_Guide.md` - Comprehensive guide for integrating entities with AI pipeline (includes advanced behavior recipes)
- `Docs/AI_Gap_Audit.md` - Gap analysis and catalog of remaining AI work
- `Docs/AI_Backlog.md` - Prioritized implementation backlog (AI-001 through AI-008)
- `Docs/AI_Validation_Plan.md` - Testing and metrics framework for AI systems
- `Docs/Archive/Progress/2025/AI_Readiness_Implementation_Summary.md` - Summary of completed AI integration work (archived)

**AI Telemetry Features** (headless training builds):
- World state snapshots: Complete simulation state at decision boundaries (population, resources, combat, diplomacy, progress)
- Entity observations: Per-entity state for fine-grained AI perception (position, health, morale, behavior, social state)
- Social graph snapshots: Relationship network (relations, trust, reputation, communication clarity, recent interactions)
- Action outcome attribution: Track AI decisions from input → execution → outcome with reward calculation
- Combat engagement tracking: Battle analysis (positioning quality, rally usage, damage efficiency, coordination score)
- Social action tracking: Dialogue outcomes (deception success, relation changes, reputation impact, memory tapping)
- Economic decision tracking: ROI analysis (predicted vs actual costs, payback time, victory contribution)
- Emergent behavior detection: Novel strategy identification (unusual tactics, exploits, hybrid approaches, novelty score)
- Strategy clustering: Playstyle identification (aggression, economy, diplomacy, tech weights + win rates)
- Training progress metrics: RL convergence tracking (episode rewards, policy loss, exploration rate, win rate)
- A/B testing framework: Head-to-head policy comparison with statistical significance
- System-specific telemetry: Force usage, locomotion quality, memory tap effectiveness, bay management, deception success
- Export formats: CSV, NDJSON, Binary Burst, TFRecord, NPZ (TensorFlow/PyTorch integration)
- Performance targets: <20ms overhead @ 10Hz export, 50-500MB per episode (compressed)
- Training scale: Supports 10K-1M episode training runs with full deterministic replay

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
