# LightSource Registry Recipe

**Category:** `puredots-infra-system`  
**Feature Name:** LightSource Registry  
**Contracts Touched:** None (PureDOTS-only infrastructure, no shared data exposed)  
**Determinism:** Required (rewind-safe, registry state restored on rewind)

---

## Context & Intent

**What problem does this solve?**  
Provides efficient spatial queries for light sources (torches, lamps, celestial bodies) used by both Godgame (villager pathfinding, mood systems) and Space4x (solar panel efficiency, visibility calculations). Registry pattern allows O(1) lookup of all light sources without scanning entities each frame.

**Which games/projects are affected?**  
- PureDOTS: Core registry system (singleton + buffer pattern)
- Godgame: Uses registry for villager AI (prefer lit paths, avoid darkness)
- Space4x: Uses registry for solar panel placement and visibility calculations

**Skip sections that don't apply:**  
- [x] No contracts needed (PureDOTS-only infra, no shared data structures)
- [x] No game adapters required (games query registry directly via PureDOTS API)
- [ ] ScenarioRunner test (optional - registry is deterministic but simple enough to skip)

---

## Step 1: Extract Shared Invariants

**Questions answered:**
- **Shared data:** Registry of light source entities with positions and intensity
- **Rewind-safe:** Registry state rebuilt each tick (deterministic)
- **Presentation:** Light rendering, VFX (stays in game projects)
- **Producers:** Entities with `LightSourceConfig` component automatically register
- **Consumers:** Game systems query registry buffer for spatial queries

**Universal invariants:**
- Singleton registry entity with metadata (total count, last update tick)
- Buffer of entries (Entity, Position, Intensity, Range)
- System rebuilds registry each tick from entities with `LightSourceConfig`
- Deterministic ordering (by entity index for consistency)

**Game-specific variations:**
- Godgame: Uses registry for villager pathfinding (prefer lit areas)
- Space4x: Uses registry for solar panel efficiency calculations (distance to stars)

---

## Step 2: Define Contracts

**Skip:** PureDOTS-only infrastructure. No shared data structures exposed to games.

**Note:** Games query the registry buffer directly via PureDOTS API. No contracts needed.

---

## Step 3: Implement PureDOTS Spine

### 3.1 Components & Buffers

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Environment/LightSourceComponents.cs`

**Created:**
- `LightSourceConfig` component - Configuration (Intensity, Range, LightType enum)
- `LightSourceRegistry` component - Singleton metadata (TotalLights, LastUpdateTick)
- `LightSourceRegistryEntry` buffer - Entry per light source (Entity, Position, Intensity, Range)

**Code structure:**
```csharp
namespace PureDOTS.Runtime.Environment
{
    public struct LightSourceConfig : IComponentData
    {
        public float Intensity;      // 0-1 brightness
        public float Range;          // Effective radius
        public LightType Type;        // Torch, Lamp, Celestial, etc.
    }

    public struct LightSourceRegistry : IComponentData
    {
        public int TotalLights;
        public uint LastUpdateTick;
    }

    [InternalBufferCapacity(64)]
    public struct LightSourceRegistryEntry : IBufferElementData
    {
        public Entity SourceEntity;
        public float3 Position;
        public float Intensity;
        public float Range;
        public LightType Type;
    }

    public enum LightType : byte
    {
        Torch = 0,
        Lamp = 1,
        Celestial = 2,
        Ambient = 3
    }
}
```

### 3.2 Systems

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/LightSourceRegistrySystem.cs`

**Created:**
- `LightSourceRegistrySystem` - Rebuilds registry each tick from entities with `LightSourceConfig`

**System pattern:**
```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct LightSourceRegistrySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return; // Only rebuild in Record mode

        var registryEntity = SystemAPI.GetSingletonEntity<LightSourceRegistry>();
        var registry = SystemAPI.GetComponentRW<LightSourceRegistry>(registryEntity);
        var entries = state.EntityManager.GetBuffer<LightSourceRegistryEntry>(registryEntity);
        
        entries.Clear();

        // Query all entities with LightSourceConfig
        foreach (var (config, transform, entity) in
            SystemAPI.Query<RefRO<LightSourceConfig>, RefRO<LocalTransform>>()
                .WithEntityAccess())
        {
            entries.Add(new LightSourceRegistryEntry
            {
                SourceEntity = entity,
                Position = transform.ValueRO.Position,
                Intensity = config.ValueRO.Intensity,
                Range = config.ValueRO.Range,
                Type = config.ValueRO.Type
            });
        }

        // Update metadata
        registry.ValueRW.TotalLights = entries.Length;
        registry.ValueRW.LastUpdateTick = SystemAPI.GetSingleton<TimeState>().Tick;
    }
}
```

### 3.3 Authoring

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/LightSourceAuthoring.cs`

**Created:**
- `LightSourceAuthoring` - MonoBehaviour with configurable Intensity, Range, Type

**Code structure:**
```csharp
public class LightSourceAuthoring : MonoBehaviour
{
    [Range(0f, 1f)]
    public float Intensity = 1f;
    
    public float Range = 10f;
    
    public LightType Type = LightType.Lamp;

    public class Baker : Baker<LightSourceAuthoring>
    {
        public override void Bake(LightSourceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new LightSourceConfig
            {
                Intensity = authoring.Intensity,
                Range = authoring.Range,
                Type = authoring.Type
            });
        }
    }
}
```

**Bootstrap:** Registry singleton created by `CoreSingletonBootstrapSystem` (standard pattern for all registries).

---

## Step 4: Wire Game Adapters

**Skip:** Games query registry directly via PureDOTS API. No adapters needed.

**Usage pattern in games:**
```csharp
// In Godgame or Space4x system
var registryEntity = SystemAPI.GetSingletonEntity<LightSourceRegistry>();
var entries = SystemAPI.GetBuffer<LightSourceRegistryEntry>(registryEntity);

foreach (var entry in entries)
{
    // Use entry.Position, entry.Intensity, entry.Range for game logic
}
```

---

## Step 5: Determinism & ScenarioRunner

**Skip:** Registry is deterministic (rebuilds each tick) but simple enough that ScenarioRunner test is optional. Registry pattern is well-tested via existing registry tests.

**Note:** If determinism verification is needed, add a simple test that verifies registry entries match entity count after N ticks.

---

## Step 6: Integration & Testing Notes

**Files created/modified:**
- Components: `PureDOTS/.../Runtime/Runtime/Environment/LightSourceComponents.cs`
- Systems: `PureDOTS/.../Runtime/Systems/Environment/LightSourceRegistrySystem.cs`
- Authoring: `PureDOTS/.../Runtime/Authoring/LightSourceAuthoring.cs`
- Bootstrap: `PureDOTS/.../Runtime/Systems/CoreSingletonBootstrapSystem.cs` (add registry singleton creation)

**Integration points:**
- Uses existing `TimeState` and `RewindState` singletons
- Follows standard registry pattern (singleton + buffer)
- Integrates with spatial grid (optional - for efficient spatial queries)

**Testing checklist:**
- [x] Registry singleton created on bootstrap
- [x] System rebuilds registry each tick
- [x] Entries match entities with `LightSourceConfig`
- [x] Rewind safety verified (registry state restored)
- [ ] (Optional) ScenarioRunner test for determinism

---

## Key Differences from Cross-Project Mechanic Recipe

**What's different:**
- **No contracts:** PureDOTS-only infrastructure doesn't expose shared data structures
- **No adapters:** Games query registry directly, no translation layer needed
- **Simpler:** Single system rebuilds registry, no intake/execution/cleanup pattern
- **No ScenarioRunner:** Optional for simple deterministic systems

**What's the same:**
- Same high-level loop: Concept → Implementation → Testing
- Same determinism requirements (rewind-safe)
- Same file structure (components, systems, authoring)

---

## See Also

- `[PureDOTS/Docs/Recipes/Recipe_Template.md](Recipe_Template.md)` - Template for creating new recipes
- `[PureDOTS/Docs/Recipes/README.md](README.md)` - Recipe catalog
- `[PureDOTS/Docs/INTEGRATION_GUIDE.md](../INTEGRATION_GUIDE.md)` - Registry pattern documentation

