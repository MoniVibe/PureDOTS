# Burst Coverage for legacy-Relevant Systems

**Last Updated:** 2025-01-27  
**Purpose:** Track Burst compilation status for all hot-path systems used in legacy scenes.

---

## legacy-Relevant Hot-Path Systems

### Core Time & Rewind Systems

| System | Burst Status | Group | legacy Usage | Notes |
|--------|--------------|-------|------------|-------|
| `TimeTickSystem` | ✅ Burst | TimeSystemGroup | All demos | Core tick advancement |
| `TimeStepSystem` | ✅ Burst | TimeSystemGroup | All demos | Fixed timestep gating |
| `RewindCoordinatorSystem` | ✅ Burst | TimeSystemGroup | RewindSandbox | Rewind state management |
| `TickSnapshotLogSystem` | ✅ Burst | LateSimulationSystemGroup | RewindSandbox | Snapshot recording |
| `HistorySettingsConfigSystem` | ⚠️ No Burst | TimeSystemGroup | All demos | Bootstrap only (acceptable) |

**Status:** ✅ Core time systems are Burst-compiled. Bootstrap systems intentionally non-Burst.

---

### Resource Systems

| System | Burst Status | Group | legacy Usage | Notes |
|--------|--------------|-------|------------|-------|
| `ResourceGatheringSystem` | ✅ Burst | ResourceSystemGroup | PureDotsTemplate | Hot path - gathering logic |
| `ResourceDepositSystem` | ✅ Burst | ResourceSystemGroup | PureDotsTemplate | Hot path - deposit logic |
| `ResourceProcessingSystem` | ✅ Burst | ResourceSystemGroup | MiningDemo | Hot path - processing |
| `ResourceRegistrySystem` | ✅ Burst | ResourceSystemGroup | All demos | Registry updates |
| `ResourceReservationBootstrapSystem` | ⚠️ No Burst | ResourceSystemGroup | All demos | Bootstrap only (acceptable) |
| `RegistryContinuityValidationSystem` | ⚠️ No Burst | ResourceSystemGroup | All demos | Validation only (acceptable) |
| `RegistryHealthSystem` | ⚠️ No Burst | ResourceSystemGroup | All demos | Telemetry only (acceptable) |

**Status:** ✅ Hot-path resource systems are Burst-compiled. Validation/telemetry systems are intentionally non-Burst.

---

### Villager Systems

| System | Burst Status | Group | legacy Usage | Notes |
|--------|--------------|-------|------------|-------|
| `VillagerAISystem` | ✅ Burst | VillagerSystemGroup | PureDotsTemplate | Hot path - AI decisions |
| `VillagerMovementSystem` | ✅ Burst | VillagerSystemGroup | PureDotsTemplate | Hot path - movement |
| `VillagerNeedsSystem` | ✅ Burst | VillagerSystemGroup | PureDotsTemplate | Hot path - needs update |
| `VillagerStatusSystem` | ✅ Burst | VillagerSystemGroup | PureDotsTemplate | Hot path - status update |
| `VillagerTargetingSystem` | ✅ Burst | VillagerSystemGroup | PureDotsTemplate | Hot path - target selection |
| `VillagerJobSystems` | ✅ Burst | VillagerSystemGroup | PureDotsTemplate | Hot path - job execution |
| `VillagerAutonomousActionSystem` | ✅ Burst | VillagerSystemGroup | PureDotsTemplate | Hot path - autonomous actions |

**Status:** ✅ All villager hot-path systems are Burst-compiled.

---

### Spatial Systems

| System | Burst Status | Group | legacy Usage | Notes |
|--------|--------------|-------|------------|-------|
| `SpatialGridBuildSystem` | ✅ Burst | SpatialSystemGroup | All demos | Hot path - grid updates |
| `SpatialGridSnapshotSystem` | ✅ Burst | SpatialSystemGroup | RewindSandbox | Hot path - snapshot |
| `SpatialProviderRegistrySystem` | ✅ Burst | SpatialSystemGroup | All demos | Hot path - provider updates |
| `SpatialGridDirtyTrackingSystem` | ✅ Burst | SpatialSystemGroup | All demos | Hot path - dirty tracking |
| `RegistrySpatialSyncSystem` | ⚠️ No Burst | SpatialSystemGroup | All demos | Sync only (acceptable) |

**Status:** ✅ Hot-path spatial systems are Burst-compiled. Sync systems are intentionally non-Burst.

---

### Mining & Hauling Systems (Space4X)

| System | Burst Status | Group | legacy Usage | Notes |
|--------|--------------|-------|------------|-------|
| `MiningLoopSystem` | ✅ Burst | SpaceSystemGroup | Space4XMineLoop | Hot path - mining logic |
| `HaulingLoopSystem` | ✅ Burst | SpaceSystemGroup | Space4XMineLoop | Hot path - hauling logic |
| `HaulingJobAssignmentSystem` | ✅ Burst | SpaceSystemGroup | Space4XMineLoop | Hot path - job assignment |
| `HaulingJobPrioritySystem` | ✅ Burst | SpaceSystemGroup | Space4XMineLoop | Hot path - priority calculation |
| `ResourcePileSystem` | ✅ Burst | SpaceSystemGroup | Space4XMineLoop | Hot path - pile updates |
| `ResourcePileMovementSystem` | ✅ Burst | SpaceSystemGroup | Space4XMineLoop | Hot path - movement |

**Status:** ✅ All mining/hauling hot-path systems are Burst-compiled.

---

### Spawner Systems

| System | Burst Status | Group | legacy Usage | Notes |
|--------|--------------|-------|------------|-------|
| `SceneSpawnSystem` | ✅ Burst | SimulationSystemGroup | SpawnerDemoScene | Hot path - deterministic spawning |
| `SpawnerLifecycleSystem` | ✅ Burst | SimulationSystemGroup | SpawnerDemoScene | Hot path - lifecycle management |

**Status:** ✅ Spawner systems are Burst-compiled and deterministic.

---

### Presentation Systems

| System | Burst Status | Group | legacy Usage | Notes |
|--------|--------------|-------|------------|-------|
| `PresentationBridgePlaybackSystem` | ⚠️ No Burst | PresentationSystemGroup | All demos | Requires MonoBehaviour bridge (acceptable) |
| `PresentationSpawnSystem` | ⚠️ No Burst | SimulationSystemGroup | All demos | Structural changes (acceptable) |
| `PresentationRecycleSystem` | ⚠️ No Burst | SimulationSystemGroup | All demos | Structural changes (acceptable) |
| `PresentationHandleSyncSystem` | ✅ Burst | PresentationSystemGroup | All demos | Hot path - handle sync |

**Status:** ✅ Presentation systems correctly exclude Burst where structural changes or MonoBehaviour bridges are required. Hot-path sync systems are Burst-compiled.

---

### Environment Systems

| System | Burst Status | Group | legacy Usage | Notes |
|--------|--------------|-------|------------|-------|
| `MoistureGridSystems` | ✅ Burst | EnvironmentSystemGroup | EnvironmentDemo | Hot path - moisture updates |
| `MoistureEvaporationSystem` | ✅ Burst | EnvironmentSystemGroup | EnvironmentDemo | Hot path - evaporation |
| `TemperatureUpdateSystem` | ✅ Burst | EnvironmentSystemGroup | EnvironmentDemo | Hot path - temperature |

**Status:** ✅ Environment hot-path systems are Burst-compiled.

---

## Summary

### Burst Coverage by Category

| Category | Hot-Path Systems | Burst-Compiled | Coverage |
|----------|------------------|----------------|----------|
| Time & Rewind | 4 | 4 | 100% |
| Resources | 4 | 4 | 100% |
| Villagers | 7 | 7 | 100% |
| Spatial | 4 | 4 | 100% |
| Mining/Hauling | 6 | 6 | 100% |
| Spawners | 2 | 2 | 100% |
| Presentation | 1 | 1 | 100% (hot paths only) |
| Environment | 3 | 3 | 100% |

**Overall Hot-Path Burst Coverage:** ✅ **100%** (31/31 hot-path systems)

---

## Non-Burst Systems (Acceptable)

The following systems are intentionally non-Burst for valid reasons:

1. **Bootstrap Systems:** Run once at startup, not in hot paths
   - `HistorySettingsConfigSystem`
   - `ResourceReservationBootstrapSystem`
   - Various bootstrap systems

2. **Validation/Telemetry Systems:** Run infrequently, may use managed APIs
   - `RegistryContinuityValidationSystem`
   - `RegistryHealthSystem`
   - `RegistrySpatialSyncSystem`

3. **Presentation Bridge Systems:** Require MonoBehaviour bridges or structural changes
   - `PresentationBridgePlaybackSystem` (requires MonoBehaviour bridge)
   - `PresentationSpawnSystem` (structural changes)
   - `PresentationRecycleSystem` (structural changes)

4. **Rewind Guard Systems:** May use managed APIs for safety checks
   - `EnvironmentRewindGuardSystem`
   - `RewindModeRoutingSystem`

**Status:** ✅ All non-Burst systems are in cold paths or have valid reasons for exclusion.

---

## legacy Readiness Assessment

**Burst Compliance:** ✅ **READY**

- All hot-path systems used in demos are Burst-compiled
- Non-Burst systems are in cold paths or have valid exclusions
- No performance-critical systems are missing Burst compilation

**Recommendations:**
- ✅ No changes needed for legacy readiness
- Monitor Burst compilation warnings in CI
- Validate Burst compilation in release builds

---

## Validation

To verify Burst coverage:

1. **CI Validation:** Run `CI/run_burst_compile.ps1` (if available)
2. **Editor Validation:** Enable Burst compilation in Project Settings
3. **Build Validation:** Test release builds with Burst enabled

**Last Validated:** 2025-01-27 (based on audit data)

