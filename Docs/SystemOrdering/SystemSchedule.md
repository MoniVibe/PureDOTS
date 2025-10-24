# System Ordering Overview

The Pure DOTS template uses the following custom groups and ordering rules:

## Initialization
- `TimeSystemGroup` runs first inside `InitializationSystemGroup` (orderFirst).
  - `CoreSingletonBootstrapSystem` seeds time/history/rewind singletons.
  - `TimeSettingsConfigSystem` applies authoring overrides before other time systems.
  - `HistorySettingsConfigSystem` updates history singleton after time settings.
  - `TimeTickSystem` runs after history config to advance deterministic ticks.

## Simulation
- `VillagerSystemGroup` runs after `FixedStepSimulationSystemGroup`.
  - `VillagerNeedsSystem` updates hunger/energy/health.
    - [REWIND GUARD] OK: Has rewind guard checks `rewindState.Mode != RewindMode.Record` and returns early.
    - [TODO] Consider hot/cold split: move `PlaybackGuardTag` filtering to a dedicated system.
  - `VillagerStatusSystem` adjusts availability/mood after needs.
    - [REWIND GUARD] OK: Has rewind guard.
    - [SEQUENCING] OK: Correctly ordered after `VillagerNeedsSystem`.
  - `VillagerJobAssignmentSystem` assigns worksites after status calculations.
    - [REWIND GUARD] OK: Has rewind guard.
    - [SEQUENCING] OK: Correctly ordered after `VillagerStatusSystem`.
    - [COLD PATH] OK: Excludes `PlaybackGuardTag` entities in query.
  - `VillagerAISystem` evaluates goals (after needs) and feeds movement.
    - [REWIND GUARD] OK: Has rewind guard.
    - [SEQUENCING] OK: Correctly ordered after `VillagerJobAssignmentSystem` and before `VillagerTargetingSystem`.
  - `VillagerTargetingSystem` resolves target entities to positions.
    - [REWIND GUARD] OK: Has rewind guard.
    - [SEQUENCING] OK: Correctly ordered after `VillagerJobAssignmentSystem`.
  - `VillagerMovementSystem` updates positions after targeting.
    - [REWIND GUARD] OK: Has rewind guard.
    - [SEQUENCING] OK: Correctly ordered after `VillagerTargetingSystem`.
    - [COLD PATH] OK: Movement calculations are deterministic - no additional hot/cold split needed.

- `ResourceSystemGroup` runs after `VillagerSystemGroup`.
  - `ResourceGatheringSystem` consumes worksite assignments.
  - `ResourceDepositSystem` executes after gathering.
  - `StorehouseInventorySystem` updates aggregate totals after deposits.
  - History systems and respawn management follow to capture state.

- `VegetationSystemGroup` runs after `FixedStepSimulationSystemGroup`.
  - `VegetationGrowthSystem` updates lifecycle stages (seedling -> growing -> mature -> fruiting -> dying -> dead).
    - [REWIND GUARD] OK: Has rewind guard checks `rewindState.Mode != RewindMode.Record` and returns early.
    - [SEQUENCING] OK: Ordered after `TimeTickSystem` to consume tick updates.
    - [COLD PATH] OK: Excludes `PlaybackGuardTag` and `VegetationDeadTag` entities.
    - [HISTORY] OK: Pushes `VegetationHistoryEvent` on stage transitions for deterministic replay.
  - `VegetationHealthSystem` processes environmental effects (water, light, soil, pollution) before growth.
    - [STATUS] Implemented.
    - [REWIND GUARD] OK: Has rewind guard checks `rewindState.Mode != RewindMode.Record` and returns early.
    - [SEQUENCING] OK: Runs before `VegetationGrowthSystem` to ensure health affects growth.
    - [COLD PATH] OK: Excludes `PlaybackGuardTag` and `VegetationDeadTag` entities.
    - [DATA-DRIVEN] OK: Uses species catalog blob for environmental thresholds.
    - [FLAGS] OK: Adds `VegetationStressedTag` and `VegetationDyingTag` based on health thresholds.
  - `VegetationHarvestSystem` consumes villager harvest commands and adjusts production.
    - [STATUS] Implemented.
    - [REWIND GUARD] Guards playback by returning early when `RewindState.Mode != Record`.
    - [SEQUENCING] Runs after `VegetationGrowthSystem` so lifecycle tags are up-to-date before harvesting.
    - [DATA] Reads `VegetationHarvestCommandQueue` and `VegetationSpeciesLookup`, writes `VegetationProduction`, `VegetationHistoryEvent`, and villager inventory buffers.
  - `VegetationReproductionSystem` handles spreading and emits deterministic spawn commands.
    - [STATUS] Implemented.
    - [REWIND GUARD] Enforced; the system returns when not in record mode so reproduction is skipped during playback/catch-up.
    - [SEQUENCING] Runs after harvest so production adjustments settle before new growth and before the spawn processor.
    - [DATA] Reads `VegetationReproduction`, `VegetationRandomState`, `VegetationSpeciesLookup`, `LocalTransform`; writes `VegetationHistoryEvent`, updates `ActiveOffspring`, and appends commands to `VegetationSpawnCommand` buffer.
  - `VegetationSpawnSystem` materializes queued vegetation offspring.
    - [STATUS] Implemented.
    - [REWIND GUARD] Follows the same early-out pattern; only runs while in record mode.
    - [SEQUENCING] `UpdateAfter(typeof(VegetationReproductionSystem))` so it consumes all commands generated in the same tick.
    - [DATA] Uses species blob defaults to seed new entities (lifecycle, health, reproduction) and stamps `VegetationParent` + history entries for replay.
  - `VegetationDecaySystem` cleans up dead vegetation and queues natural respawns.
    - [STATUS] Implemented.
    - [REWIND GUARD] Enforced; skips structural changes during playback.
    - [SEQUENCING] Runs after growth/health to ensure stage transitions complete before decay culls.
    - [DATA] Records death history, decrements parent `ActiveOffspring`, conditionally enqueues respawn commands (no parent) and destroys entities via ECB.

- `LateSimulationSystemGroup` (custom) is ordered last within `SimulationSystemGroup` for history/cleanup.

## Physics
- Combat and hand interaction groups are slotted between `BuildPhysicsWorld` and `ExportPhysicsWorld` via `UpdateAfter/Before` attributes.

## Rewind Routing
- `RewindCoordinatorSystem` runs early in simulation to enable/disable record, catch-up, or playback groups.

## Presentation
- `PresentationSystemGroup` runs after simulation to provide data for UI and visualization layers.
  - `DebugDisplaySystem` updates `DebugDisplayData` singleton with current time state, villager counts, and resource totals.
    - [STATUS] Fully implemented with deterministic query caching.
    - [DATA] Reads `TimeState`, `RewindState`, villager counts (via cached `VillagerId` query), storehouse totals (via `StorehouseInventory`); writes `DebugDisplayData` singleton.
    - [PERFORMANCE] Uses cached queries in system fields, Burst-compiled, no managed allocations.
    - [DEPENDENCIES] Runs after all simulation groups complete to capture final state.
    - [PRESENTATION BRIDGE] Future Unity UI elements will read `DebugDisplayData` singleton via minimal MonoBehaviour bridge.

Consult this document when adding new systems - ensure their `UpdateInGroup`, `UpdateAfter`, or `UpdateBefore` attributes align with the deterministic scheduling expectations.
