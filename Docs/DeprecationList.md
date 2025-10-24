# PureDOTS Deprecation List

This document tracks hybrid scripts and patterns slated for replacement with pure DOTS implementations. Per the project vision, authoring components (MonoBehaviour + Baker) are **intentional and acceptable** for converting GameObject data to ECS. This list focuses on runtime hybrid patterns that violate the pure DOTS architecture.

## Design Principles

- **Authoring Components**: MonoBehaviour + Baker classes are the intended pattern for SubScene conversion. These bridge Unity Editor/Inspector data to ECS components at bake time.
- **Runtime Hybrid Scripts**: MonoBehaviour components that run alongside ECS systems violate the pure DOTS architecture and should be replaced.
- **Editor Tools**: Editor-only scripts are acceptable for tooling and don't require deprecation.

## Deprecated Items

### 1. Runtime Debugging UI (`DotsDebugHUD`)

**File**: `Assets/Scripts/PureDOTS/Debug/DotsDebugHUD.cs`

**Status**: Deprecated with scaffolded replacement

**Reason**: This MonoBehaviour queries the ECS world at runtime using `World.DefaultGameObjectInjectionWorld` and `EntityQuery` APIs. While functional, it violates the pure DOTS principle by running simulation queries from GameObject/MonoBehaviour space.

**Current Implementation**:
- Queries `TimeState`, `RewindState`, `VillagerId`, `StorehouseInventory` singletons and components
- Displays on-screen HUD via `OnGUI()`
- Provides toggles for different debug panels

**Replacement Implementation**:
- ✅ Created `DebugDisplayData` singleton component (`Assets/Scripts/PureDOTS/Runtime/DebugComponents.cs`)
- ✅ Implemented `DebugDisplaySystem` (`Assets/Scripts/PureDOTS/Systems/DebugDisplaySystem.cs`) in `PresentationSystemGroup`
- ✅ System seeds singleton in `OnCreate` and populates all debug metrics:
  - Time state (tick, paused flag, speed multiplier)
  - Rewind state (mode, playback tick)
  - Villager counts (via cached query)
  - Storehouse resource totals (sum of all inventories)
- ✅ Uses cached queries for performance, Burst-compiled for determinism
- ✅ Added comprehensive test suite (`DebugDisplaySystemTests.cs`)
- ✅ Created optional Unity UI presentation bridge (`DebugDisplayReader.cs` + Canvas prefab)
- ✅ Added DOTS command buffer for HUD toggling (`DebugCommand` + `DebugCommandAuthoring`)
- ✅ Added optional keyboard input handler (`DebugInputHandler.cs`) for designer convenience

**Priority**: Medium (debugging is useful during development, but not simulation-critical)

**Estimated Effort**: Complete

**Blockers**: None

---

## Usage Guide for Designers

### Setting Up the Debug HUD (Optional)

The debug display system is opt-in and can be added to any playmode build:

1. **Create a Canvas GameObject** in your scene (if not already present)
   - Right-click in Hierarchy → UI → Canvas
   - Set Canvas to "Screen Space - Overlay"

2. **Add DebugDisplayReader Component**
   - Select the Canvas GameObject
   - Add Component → Debug Display Reader
   - Optionally assign UI Text components for specific fields

3. **Add Debug Input Handler** (optional, for keyboard shortcuts)
   - Add Component → Debug Input Handler
   - Customize keyboard shortcuts (default: F1 = toggle, F2 = show, F3 = hide)

4. **Create Debug Command Buffer** (if you want DOTS systems to toggle HUD)
   - Create empty GameObject in scene
   - Add Component → Debug Command Authoring
   - Component will create command buffer at bake time

### Keyboard Shortcuts (with DebugInputHandler enabled)
- **F1**: Toggle HUD visibility
- **F2**: Show HUD
- **F3**: Hide HUD

### API Usage from DOTS Systems

To toggle the HUD from a DOTS system:

```csharp
// Get the debug command entity
var query = SystemAPI.QueryBuilder()
    .WithAll<DebugCommandSingletonTag>()
    .Build();
    
if (!query.IsEmptyIgnoreFilter)
{
    var entity = query.GetSingletonEntity();
    var commands = SystemAPI.GetBufferRW<DebugCommand>(entity);
    commands.Add(new DebugCommand { Type = DebugCommand.CommandType.ToggleHUD });
}
```

### UI Layout Example

```
Canvas
├── Debug Display Reader (component)
├── Debug Input Handler (component)
└── Panel (optional, for styling)
    ├── TimeStateText (Text component)
    ├── RewindStateText (Text component)
    ├── VillagerCountText (Text component)
    └── ResourceTotalText (Text component)
```

---

## Replacement Patterns

### Pure DOTS Debug System

**Architecture**:
```csharp
// Runtime component for debug data
public struct DebugDisplayData : IComponentData
{
    public FixedString128Bytes TimeState;
    public FixedString128Bytes RewindState;
    public int VillagerCount;
    public float TotalResourcesStored;
    // ... other debug metrics
}

// System that updates debug data
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct DebugDisplaySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var debugEntity = SystemAPI.GetSingletonEntity<DebugDisplayData>();
        var debugData = SystemAPI.GetComponentRW<DebugDisplayData>(debugEntity);
        
        // Query and aggregate data from ECS systems
        var timeState = SystemAPI.GetSingleton<TimeState>();
        var villagerCount = SystemAPI.QueryBuilder()
            .WithAll<VillagerId>()
            .Build()
            .CalculateEntityCount();
        
        // Update debug display singleton
        debugData.ValueRW.TimeState = /* formatted string */;
        debugData.ValueRW.VillagerCount = villagerCount;
    }
}
```

**Presentation Bridge** (if needed):
- MonoBehaviour reads `DebugDisplayData` singleton via `World.DefaultGameObjectInjectionWorld`
- Updates Canvas/UI elements
- Minimal GameObject dependency, no simulation logic

---

## Legacy Migration Candidates

### Potential Future Deprecations

When porting from the legacy `godgame` repository, watch for these patterns:

1. **Service Locators**: Any `WorldServices`, `RegistrySystems`, or singleton managers that provide lookup services
   - Replace with ECS queries, singleton components, or buffer-based registries

2. **MonoBehaviour Controllers**: Runtime controllers that manage ECS entities via `EntityManager`
   - Replace with pure DOTS systems
   - Move logic to `ISystem` or `SystemBase` implementations

3. **Hybrid Adapters**: Components that bridge MonoBehaviour events to ECS commands
   - Replace with command buffers and pure DOTS input systems
   - Use existing `TimeControlCommand` buffer pattern as reference

4. **Presentation Dependencies**: Systems that invoke MonoBehaviour/GameObject methods during simulation
   - Split into hot simulation archetypes and cold presentation archetypes
   - Use companion components or separate worlds for visuals

---

## Authoring Components (Not Deprecated)

The following MonoBehaviour authoring components are **intentional and should remain**:

- `TimeControlsAuthoring` + `TimeControlsBaker`
- `SimulationAuthoring` (TimeSettingsAuthoring, HistorySettingsAuthoring) + Bakers
- `PureDotsConfigAuthoring` + `PureDotsConfigBaker`
- `ResourceSourceAuthoring` + `ResourceSourceBaker`
- `StorehouseAuthoring` + `StorehouseBaker`
- `ResourceChunkAuthoring` + `ResourceChunkBaker`
- `ConstructionSiteAuthoring` + `ConstructionSiteBaker`
- `VillagerAuthoring` + `VillagerBaker`
- `VillagerSpawnerAuthoring` + `VillagerSpawnerBaker`
- `VegetationAuthoring` + `VegetationBaker`

These follow the canonical Unity Entities SubScene authoring pattern and are essential for designer/artist workflows.

---

## Editor Tools (Not Deprecated)

- `PureDotsTestMenu` - Editor-only menu for running tests

---

## Completion Criteria

For each deprecated item:
- [x] Pure DOTS replacement fully implemented
- [x] Original deprecated script marked with `[Obsolete]` attribute
- [x] Documentation updated to reference new pattern
- [x] Test suite created and validated
- [x] UI presentation bridge created (optional)
- [ ] Legacy code removed after migration period

---

## Migration Timeline

| Item | Target Quarter | Assignee | Status |
|------|---------------|----------|--------|
| DotsDebugHUD → Pure DOTS Debug System | Q1 2025 | TBD | Complete - Production Ready with UI Bridge |

---

Last Updated: 2025-01-XX

