# Villager Systems Parity & Expansion TODO

> **Generalisation Guideline**: Treat “villagers” as one archetype in a reusable AI framework. AI modules (sensing, decision-making, steering, task assignment) must work for any entity type by swapping archetype data; avoid villager-only code in the meta runtime.

## Goal
- Rebuild villager simulation to handle 100k–1M agents with BW2-inspired behaviour: needs, jobs, alignment, combat readiness, and creature interactions.
- Ensure deterministic, rewind-safe DOTS systems with clear SoA data layout, pooled allocations, and spatial/grid integration.
- Provide designers with configurable levers (ScriptableObjects + blobs) to tune villager archetypes, jobs, and schedules.
- Stay aligned with `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and glue work in `Docs/TODO/SystemIntegration_TODO.md`.

## Plain-Language Primer
- Villagers are autonomous agents. They need food, rest, morale, jobs, and respond to player miracles/creature commands.
- In DOTS we model them as components & buffers (data), and systems (logic) running in groups.
- The registry + spatial grid + time/replay services supply data; villagers in turn feed registries and presentation.

## Alignment With Vision
- **Deterministic Core**: all villager behaviour must be rewindable; no hidden state outside DOTS data.
- **Scalability Discipline**: support >1M agents via SoA layout, pooled buffers, jobified systems, spatial queries.
- **Flexibility by Configuration**: villager archetypes, job priorities, alignments, and schedules defined in assets.
- **Observability & Automation**: hooks for debugging, metrics, automated tests.

## Architecture Outline
1. **Data Model**  
   - Components: `VillagerState`, `VillagerNeeds`, `VillagerInventory`, `VillagerAlignment`, `VillagerMood`, `VillagerJobTicket`, `VillagerTarget` (SoA).  
   - Buffers: `VillagerJobHistory`, `VillagerNeedsHistory`, `VillagerCommandBuffer`.  
   - Blobs: `VillagerArchetypeCatalog`, `JobDefinitionCatalog`, `NeedDecayCurves`.  
   - Tags: `VillagerIdleTag`, `VillagerWorkingTag`, `VillagerCombatTag`, `VillagerPlayerPriorityTag`.
2. **System Groups**  
   - `VillagerNeedsSystemGroup` (energy/hunger/thirst)  
   - `VillagerMoodSystem` (morale, alignment lean)  
   - `VillagerJobSystemGroup` (assignment, execution, delivery)  
   - `VillagerAISystemGroup` (pathing decisions, spatial awareness)  
   - `VillagerCommandSystemGroup` (player/creature overrides)  
   - `VillagerHistorySystem` (record events for rewind & analytics).
3. **Integration Points**  
   - Registries (resource/storehouse)  
   - Spatial grid & navmesh (path targets)  
   - Divine hand / miracles (interruption, pickup)  
   - Time engine (jobs & needs bound to ticks)  
   - Presentation (HUD badges, animation bridges).

## Dependencies & Shared Infrastructure
- Consume environment grid cadence (moisture/temperature/wind/sunlight + sampling helpers) from `EnvironmentSystemGroup`; never roll bespoke samplers.
- Use spatial registry utilities (`RegistryUtilities.cs`) + spatial grid query helpers for all proximity lookups (villagers, haulers, wagons, miracles).
- Plug villager behaviour into shared `AISystemGroup` modules (sensors, utility scoring, steering, task emitter) via `AISensorConfig`, `AIBehaviourArchetype`, and `VillagerAIUtilityBinding`.
- Integrate with central hand/RMB router (`HandCameraInputRouter`, `HandInteractionState`) to respect shared interaction priorities and rewind guards.

## Workstreams & Tasks

### 0. Requirements Reconnaissance
- [ ] Audit current villager systems (`VillagerNeedsSystem`, `VillagerStatusSystem`, `VillagerJobAssignmentSystem`, `VillagerAISystem`, `VillagerMovementSystem`) for data layout, performance, and determinism gaps.
- [ ] Document legacy truth-source expectations: `VillagerTruth.md`, `Villagers_Jobs.md`, `VillagerState.md`, RMBS truth on player priority.
- [ ] Profile existing loops to identify hotspots (job assignment, pathing, inventory updates).
- [ ] Catalogue future features: alignment shifts, armies, creature training, prayer/tribute contributions.
- [ ] Review rewinding behaviour: confirm current systems respect `RewindState` and record necessary history.

### 1. Data Layout & Assets
- [ ] Design SoA component structs (float3 arrays, ints, bools) aligned to 16-byte boundaries; minimize bool/byte fragmentation.
- [ ] Build `VillagerArchetypeCatalog` ScriptableObject + blob with base stats (needs decay, job weights, loyalty, alignment lean).
- [ ] Define `JobDefinitionCatalog` with job durations, resource costs, rewards, skill requirements.
- [ ] Define `VillagerNeedCurve` assets (AnimationCurve -> blob) for hunger/energy/mood thresholds.
- [ ] Extend authoring/baker to convert new assets into runtime data.
- [ ] Add tags/flags for special roles (soldier, craftsman, priest) to support future modules.
- [ ] **Add spatial awareness components** in `VillagerSensorComponents.cs`:
  - `VillagerSpatialSensor` - cached nearby entities by category (villagers, threats, resources)
  - `VillagerSteeringState` - local avoidance vectors and obstacle detection state
- [ ] Ensure villager sensor and needs systems consume shared environment grid cadence (read moisture/temperature/wind once `EnvironmentSystemGroup` baseline jobs exist).
- [ ] Adopt shared pooling for command buffers/history entries; document SoA compliance beyond base components.
- [ ] **Add pathfinding components** in `FlowFieldComponents.cs`:
  - `FlowFieldConfig` - grid resolution, update frequency, cost weights
  - `FlowFieldData` - blob asset storing direction vectors and costs per cell
  - `FlowFieldRequest` - buffer element for villagers requesting paths to goals
  - `FlowFieldFollower` - component for agents following a specific field layer
- [ ] Configure reusable AI behaviour modules (sensor, scoring, steering, task selection) through archetype-specific data and marker components.
- [ ] Document how other entity types (ships, drones, NPCs) plug into the same AI modules via configuration.

### 2. Core Systems Refactoring
- [ ] Rewrite `VillagerNeedsSystem` as Burst job with SoA data, using pooled command buffers for state transitions.
- [ ] Introduce `VillagerMoodSystem` computing morale, alignment shift, effect of miracles/creature actions.
- [ ] Rework `VillagerJobAssignmentSystem` to use spatial grid + registries to find nearest suitable job target.
- [ ] Refactor `VillagerJobExecutionSystem` to support modular job behaviours (gather, build, worship, combat).
- [ ] Implement `VillagerCommandSystem` to process player/creature commands (priority overrides, recruitment).
- [ ] Ensure all systems bail appropriately during playback/catch-up (rewind guard).
- [ ] Integrate `VillagerHistorySystem` for deterministic logging of job start/end, need events, morale shifts.
- [ ] **Implement `VillagerSensorUpdateSystem`** running after spatial grid rebuild:
  - Query spatial grid for entities within sensor range (15-30m)
  - Populate sensor buffers: nearby villagers, threats, resources
  - Cache results for multiple ticks (update every 5-10 ticks) to reduce cost
  - Integrate with VillagerAISystem for threat detection and flee behavior
- [ ] **Implement `VillagerLocalSteeringSystem`** for obstacle avoidance:
  - Query nearby villagers (2-5m radius) from spatial grid
  - Apply separation force to avoid clustering (Reynolds steering)
  - Detect static obstacles via grid cell occupancy
  - Adjust movement direction while maintaining target heading

### 3. Needs & Schedule
- [ ] Create job/need priority scheduler (e.g., utility score + cooldown) per villager archetype.
- [ ] Implement shift schedules (day/night) using `TimeOfDay` service; allow config overrides by alignment/culture.
- [ ] Provide fallback behaviours (idle, roam, worship) when needs are satisfied.
- [ ] Hook into prayers/tribute economy (increase alignment, expand influence ring).
- [ ] Define shared AI behaviour modules (sensing, scoring, steering, task resolution) that other projects can reuse; keep villager-specific data in archetype configs.
- [ ] Document patterns for combining generic AI systems with specialised behaviour via marker components.

### 4. Inventory & Economy
- [ ] Rebuild `VillagerInventory` buffer to track multiple resource slots, weight/capacity, and reserved tickets.
- [ ] Ensure inventory interacts with resource/storehouse registries via pooled commands.
- [ ] Add deterministic consumption (food from storehouse) tied to needs; integrate with resource economy.
- [ ] Prepare extension for army equipment (weapons, armor) later.

### 5. Alignment & Morale
- [ ] Implement alignment state derived from actions (helpful vs. aggressive) and creature influence.
- [ ] Add morale effects (panic, productivity modifiers) based on needs, alignment, environment events.
- [ ] Feed alignment/morale metrics into time/tribute, city attractiveness.
- [ ] Provide API for presentation (UI badges) and for AI to react to morale shifts.

### 6. Interaction & Interrupts
- [ ] Define interrupt pipeline: player hand pickup, miracles, disasters, combat threats.
- [ ] Coordinate with RMB router to ensure villager actions yield to higher priority (per truth source).
- [ ] Support graceful interruption/resume of jobs with deterministic state restore.
- [ ] Integrate with `SceneSpawnSystem` for dynamic villager spawning by role.

### 7. Movement & Pathing Integration
- [ ] Abstract path requests so villager systems can plug into future nav solutions (Unity DOTS Nav, custom flow fields).
- [ ] For now, ensure positions sync with placeholder movement (LocalTransform) while deferring full nav graph.
- [ ] Provide hook for spatial grid / nav service to return path targets and avoid collisions.
- [ ] Prepare for crowd-simulation improvements (flocking, lane formation) later.
- [ ] **Implement Flow Field Pathfinding** for scalability to 100k+ agents:
  - Create `FlowFieldBuildSystem` generating flow fields using Dijkstra/Fast Marching in Burst jobs
  - Use spatial grid to identify obstacles and goal positions for flow field generation
  - Support multiple flow field layers by goal type (resources, storehouses, safety zones)
  - Update flow fields periodically (every 30-60 ticks), not per-agent, for performance
  - Implement `FlowFieldFollowSystem` where villagers sample local cell direction and steer
  - Combine flow field direction with local avoidance for smooth crowd movement
  - Add lazy updates: only rebuild flow fields when goals/obstacles change significantly
  - Enable layer sharing: multiple villagers with same goal share one flow field
  - Plan hierarchical approach: macro-grid for long distance, micro-grid for local detail
- [ ] **Ensure pathfinding rewind compatibility**:
  - Flow field generation must be deterministic (sorted entity iteration, fixed cost calc)
  - Skip spatial grid and flow field updates during playback mode
  - Rebuild flow fields fully during catch-up mode
  - Add snapshot strategy if flow field rebuild becomes too expensive (measure first)

### 8. Testing & Benchmarks
- [ ] Unit tests for needs decay, job assignment correctness, inventory transfers, alignment changes.
- [ ] Playmode tests verifying gather-deliver loop, alignment shifts, scheduled behaviours, rewind parity.
- [ ] Stress tests with 100k, 500k, 1M villagers measuring frame time per system and verifying zero GC allocations.
- [ ] Determinism tests: two runs, identical commands -> same villager states, job history, inventory totals.
- [ ] Integration tests with registries/spatial grid ensuring combined workload stays under target budgets.
- [ ] **Spatial sensor and pathfinding tests** in `VillagerSensorTests.cs` and `FlowFieldTests.cs`:
  - Verify sensor accuracy: correct entities within range, no false positives/negatives
  - Test flow field generation correctness: direction vectors point toward goals
  - Validate flow field determinism: identical inputs produce identical flow fields
  - Ensure sensor results sorted deterministically by entity index
- [ ] **Pathfinding rewind determinism tests** in `SpatialPathfindingRewindTests.cs`:
  - Record 100 ticks → rewind to tick 50 → verify flow fields match original
  - Test playback mode: sensor and flow field queries return consistent results
  - Test catch-up mode: flow fields rebuild correctly during fast-forward
- [ ] **Performance targets by agent count**:
  - 10k villagers: Spatial sensors update every tick (~0.5ms per frame)
  - 50k villagers: Sensors every 5 ticks, local steering (<2ms per frame)
  - 100k villagers: Flow fields every 30 ticks, sensor caching (<5ms per frame)
  - 1M villagers (future): Hierarchical grid, GPU-accelerated flow fields
- [ ] Measure and validate query performance vs. linear scans to quantify speedup.

### 9. Tooling & Observability
- [ ] Extend debug HUD to display villager counts per state/job/alignment, average needs, morale ranges.
- [ ] Add timeline overlays showing job events and need spikes (ties into history system).
- [ ] Provide in-editor inspector for per-system metrics (e.g., `VillagerJobInspector` to list current assignments).
- [ ] Hook into analytics/logging to export simulation snapshots for balancing.
- [ ] **Add pathfinding debug visualization**:
  - Scene gizmo drawer showing flow field direction vectors and costs per cell
  - Runtime overlay for sensor ranges (toggled via debug menu)
  - Visual indicators for local steering forces and obstacle avoidance
  - Flow field layer selector to visualize different goal types

### 10. Documentation & Designer Workflow
- [ ] Update `Docs/DesignNotes/VillagerJobs_DOTS.md` with new architecture, assets, tuning guidelines.
- [ ] Create `Docs/Guides/VillagerAuthoring.md` explaining how to author archetypes, job definitions, schedules.
- [ ] Document integration points with other systems (registry, spatial, hand/miracles) so designers know dependencies.
- [ ] Log progress and major decisions in `Docs/Progress.md`.
- [ ] **Add pathfinding documentation** in `Docs/DesignNotes/FlowFieldPathfinding.md`:
  - Explain flow field approach and why it scales better than A* for crowds
  - Document flow field configuration parameters and tuning guidelines
  - Describe sensor system and how it integrates with AI behaviors
  - Provide examples of local steering and obstacle avoidance
  - Include performance targets and optimization strategies

## Open Questions
- How many villager archetypes and cultures need to be supported at launch?
- What level of AI sophistication is required (GOAP vs. utility vs. behaviour tree) to match BW2 feel?
- How do creature interactions (training, approval) tie into villager morale and job assignment?
- Do we need a persistent history beyond rewind (analytics, story triggers)?
- How do we handle nav/pathing until a full DOTS nav solution is integrated?
- **Pathfinding-specific questions**:
  - Should flow fields be stored in blob assets or transient NativeArrays?
  - How many flow field layers can we realistically maintain (5? 10? 20?)?
  - Should sensors cache results per villager or per cell (shared)?
  - Do we need height layers for 3D movement (flying miracles, terrain elevation)?
  - What's the optimal grid resolution for flow fields (1m? 2m? 5m cells)?
  - When should we trigger flow field rebuilds vs. using stale data?

## Dependencies & Links
- Depends on registry rewrite for resource/storehouse lookups.
- Consumes spatial grid queries for proximity decisions.
- Shares presentation data with companion bridge systems.
- Interacts with time control/time-of-day services for scheduling.
- Must align with divine hand/miracle interrupt rules.
- **Requires spatial grid implementation** (see `SpatialServices_TODO.md`) for:
  - Sensor system to query nearby entities efficiently
  - Flow field generation to identify goals and obstacles
  - Job assignment optimization via radius queries
- **Links to pathfinding plan** (see `sp.plan.md`) for detailed architecture and implementation stages.

## Next Steps & Implementation Order
1. Recon & profiling (task list 0) to baseline current behaviour and bottlenecks.
2. Finalize data layout and asset definitions; update authoring/bakers.
3. Refactor needs + job assignment loops to SoA with new data model.
4. Integrate spatial grid and registries for efficient target selection.
5. Layer in mood/alignment systems and command overrides.
6. Expand inventory/economy and schedule behaviours.
7. **Implement pathfinding in stages** (see `sp.plan.md` for details):
   - Stage 1: Spatial sensors (1-2 weeks) - Add sensor component, integrate with AI
   - Stage 2: Job query optimization (1 week) - Use spatial radius queries
   - Stage 3: Local steering (1-2 weeks) - Implement Reynolds behaviors
   - Stage 4: Flow field MVP (2-3 weeks) - Single layer, Burst-compiled generation
   - Stage 5: Multi-layer flow fields (2 weeks) - Multiple goal types
   - Stage 6: Polish & optimization (1-2 weeks) - Debug viz, tests, profiling
8. Build tooling/tests, run stress benchmarks, iterate until performance targets met.
9. Update documentation and tag completion in global TODOs.

Keep this file updated as tasks progress; surface blockers and design decisions so the team stays aligned.
