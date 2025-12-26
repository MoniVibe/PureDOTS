# Authoring & Prefabs Recipe

**Category:** `authoring-pattern` (applies to all feature types)  
**Feature Name:** Authoring Components, Bakers, Prefabs, and SubScenes  
**Contracts Touched:** N/A (authoring is pre-runtime)  
**Determinism:** Required (baking must be deterministic, prefabs must work with ScenarioRunner)

---

## Context & Intent

**What problem does this solve?**  
Standardizes how to create authoring components, bakers, prefabs, and SubScenes in PureDOTS DOTS 1.4 environment. Ensures consistent patterns for:
- Converting MonoBehaviour authoring to ECS components
- Setting up prefabs that work with registries and ScenarioRunner
- Organizing SubScenes for deterministic conversion
- Extending base authoring with game-specific configs

**Which games/projects are affected?**  
- PureDOTS: Base authoring components (`PureDOTS/Runtime/Authoring/`)
- Godgame: Game-specific authoring (`Godgame/Assets/Scripts/Godgame/Authoring/`)
- Space4x: Game-specific authoring (`Space4x/Assets/Scripts/Space4x/Authoring/`)

**Skip sections that don't apply:**  
- [ ] All sections apply (authoring is foundational)

---

## Step 1: When to Use Authoring vs Runtime-Only

**Use Authoring (MonoBehaviour + Baker) when:**
- Entity needs **initial configuration** set by designers (e.g., launcher cooldown, villager job type)
- Entity needs **transform data** from scene/prefab (position, rotation, scale)
- Entity needs **ScriptableObject references** (resource catalogs, config assets)
- Entity should be **placeable in scenes/prefabs** by designers

**Use Runtime-Only Components when:**
- Component is **purely runtime state** (e.g., `LauncherState`, `VillagerNeeds`)
- Component is **added by systems** (e.g., `LaunchedProjectileTag`, registry entries)
- Component is **computed at runtime** (e.g., spatial grid residency, collision events)

**Pattern:**
```csharp
// ✅ GOOD: Config set at authoring time
public struct LauncherConfig : IComponentData { ... }  // Added by Baker

// ✅ GOOD: State initialized at authoring, updated at runtime
public struct LauncherState : IComponentData { ... }   // Added by Baker, modified by systems

// ❌ BAD: Runtime-only tag added by systems
public struct LaunchedProjectileTag : IComponentData { ... }  // Added by LaunchExecutionSystem, NOT Baker
```

---

## Step 2: Base Authoring Component Pattern

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/`

**Structure:**
```csharp
using PureDOTS.Runtime.<Feature>;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Authoring
{
    /// <summary>
    /// Base authoring component for <feature> entities.
    /// Games should extend this or use it directly for basic <feature>s.
    /// </summary>
    public class <Feature>Authoring : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Description")]
        [Range(min, max)]  // Use Range for numeric fields
        public int ConfigValue = defaultValue;

        public class Baker : Baker<<Feature>Authoring>
        {
            public override void Bake(<Feature>Authoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 1. Add tag component (if needed)
                AddComponent<<Feature>Tag>(entity);

                // 2. Add config component (bake-time settings)
                AddComponent(entity, new <Feature>Config
                {
                    ConfigValue = Mathf.Clamp(authoring.ConfigValue, min, max),
                    // ... other config fields
                });

                // 3. Add runtime state (initialized to defaults)
                AddComponent(entity, new <Feature>State
                {
                    // Initialize to safe defaults
                    LastUpdateTick = 0,
                    Version = 0
                });

                // 4. Add buffers (if needed)
                AddBuffer<<Feature>Request>(entity);
                AddBuffer<<Feature>Entry>(entity);
            }
        }
    }
}
```

**TransformUsageFlags Guidelines:**
- `TransformUsageFlags.Dynamic` - Entity moves/rotates at runtime (most gameplay entities)
- `TransformUsageFlags.Static` - Entity never moves (terrain, buildings)
- `TransformUsageFlags.None` - No transform needed (singletons, pure data entities)

**Example:** See `PureDOTS/.../Runtime/Authoring/LaunchAuthoring.cs`

---

## Step 3: Game-Specific Authoring Extension

**Location:** `<Game>/Assets/Scripts/<Game>/Authoring/`

**Pattern:** Extend base authoring, add game-specific config/tags

```csharp
using PureDOTS.Authoring;
using PureDOTS.Runtime.<Feature>;
using Unity.Entities;
using UnityEngine;

namespace <Game>.Authoring
{
    /// <summary>
    /// <Game>-specific <feature> authoring.
    /// Extends the base <feature> with <Game>-specific settings.
    /// </summary>
    public class <Game><Feature>Authoring : MonoBehaviour
    {
        [Header("Base Settings")]
        // Reuse base config fields (or reference base authoring component)
        public int BaseConfigValue = defaultValue;

        [Header("<Game> Settings")]
        // Game-specific fields
        public float GameSpecificValue = defaultValue;

        public class Baker : Baker<<Game><Feature>Authoring>
        {
            public override void Bake(<Game><Feature>Authoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 1. Add base PureDOTS components (same as base authoring)
                AddComponent<<Feature>Tag>(entity);
                AddComponent(entity, new <Feature>Config { ... });
                AddComponent(entity, new <Feature>State { ... });
                AddBuffer<<Feature>Request>(entity);

                // 2. Add game-specific components
                AddComponent<<Game><Feature>Tag>(entity);
                AddComponent(entity, new <Game><Feature>Config
                {
                    GameSpecificValue = authoring.GameSpecificValue
                });
            }
        }
    }
}
```

**Example:** See `Godgame/Assets/Scripts/Godgame/Authoring/GodgameSlingshotAuthoring.cs`

---

## Step 4: Prefab Setup

**Location:** `<Game>/Assets/Prefabs/` or `PureDOTS/Assets/PureDOTS/Prefabs/`

**Prefab Structure:**
1. **Root GameObject** - Contains authoring component(s)
2. **Child GameObjects** - For visual representation (optional, presentation-only)
3. **No MonoBehaviour scripts** on children (use DOTS components via authoring)

**Prefab Workflow:**
1. Create GameObject in scene
2. Add authoring component(s)
3. Configure fields in inspector
4. Drag to Prefab folder to create prefab
5. Use prefab in SubScenes or instantiate in scenes

**Prefab Best Practices:**
- Keep prefabs **minimal** - authoring components + transform only
- Visual representation (meshes, sprites) can be children but won't be converted
- Use **nested prefabs** for complex hierarchies (e.g., launcher + payload prefab)
- Prefabs should be **self-contained** - all required authoring components on root

**Example Prefabs:**
- `PureDOTS/Assets/PureDOTS/Prefabs/ResourceNode.prefab`
- `PureDOTS/Assets/PureDOTS/Prefabs/Villager.prefab`
- `PureDOTS/Assets/PureDOTS/Prefabs/Storehouse.prefab`

---

## Step 5: SubScene Organization

**Location:** `<Game>/Assets/Scenes/` or `PureDOTS/Assets/Scenes/`

**SubScene Structure:**
1. **Bootstrap GameObject** - Contains config authoring (`PureDotsConfigAuthoring`, `SpatialPartitionAuthoring`)
2. **Gameplay Entities** - Prefabs/GameObjects with feature authoring components
3. **Keep in SubScene** - All DOTS entities should be in SubScenes for deterministic conversion

**SubScene Setup:**
```
Main Scene (Unity Scene)
├── Camera (presentation)
├── Lighting (presentation)
└── GameplaySubScene (SubScene asset)
    ├── Bootstrap (PureDotsConfigAuthoring, SpatialPartitionAuthoring)
    ├── LauncherPrefab (LauncherAuthoring)
    ├── VillagerPrefab (VillagerAuthoring)
    └── ResourceNodePrefab (ResourceSourceAuthoring)
```

**SubScene Best Practices:**
- **One SubScene per gameplay domain** (e.g., `GodgameBootstrapSubScene`, `Space4XRegistryDemo_SubScene`)
- **Bootstrap configs** in SubScene (not main scene) for deterministic conversion
- **Presentation** (cameras, lights, UI) stays in main scene
- **SubScenes enable/disable** for loading/unloading gameplay domains

**Example SubScenes:**
- `Godgame/Assets/Scenes/GodgameBootstrapSubScene.unity`
- `Space4x/Assets/Scenes/legacy/Space4XRegistryDemo_SubScene.unity`

---

## Step 6: ScriptableObject Config Assets

**When to use:** Shared configuration data referenced by multiple authoring components (e.g., resource catalogs, time configs, spatial profiles)

**Location:** `<Game>/Assets/<Game>/Config/` or `PureDOTS/Assets/PureDOTS/Config/`

**Pattern:**
```csharp
// ScriptableObject asset
[CreateAssetMenu(fileName = "MyConfig", menuName = "PureDOTS/MyConfig")]
public class MyConfig : ScriptableObject
{
    public int ConfigValue;
    public float AnotherValue;
}

// Authoring component references it
public class MyAuthoring : MonoBehaviour
{
    public MyConfig ConfigAsset;  // Assign in inspector

    public class Baker : Baker<MyAuthoring>
    {
        public override void Bake(MyAuthoring authoring)
        {
            // Access config asset data
            var configValue = authoring.ConfigAsset.ConfigValue;
            // ... use in component data
        }
    }
}
```

**Common Config Assets:**
- `PureDotsRuntimeConfig` - Time step, history settings
- `ResourceTypeCatalog` - Resource type definitions
- `SpatialPartitionProfile` - Spatial grid configuration

**Example:** See `Space4x/Assets/Space4X/Config/` for config asset setup

---

## Step 7: Registry Integration

**When authoring entities that should appear in registries:**

**Add registry tag during baking:**
```csharp
public class VillagerAuthoring : MonoBehaviour
{
    public class Baker : Baker<VillagerAuthoring>
    {
        public override void Bake(VillagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Add registry component (systems will pick up automatically)
            AddComponent<VillagerId>(entity);
            AddComponent<VillagerConfig>(entity);
            
            // Registry system will add entity to VillagerRegistry buffer automatically
        }
    }
}
```

**For spatial registries, add spatial tag:**
```csharp
// Add spatial indexing for registry bridge
AddComponent<SpatialIndexedTag>(entity);
AddComponent(entity, new SpatialGridResidency { CellId = -1 });  // System will update
```

**Example:** See `Space4x/Docs/Guides/Space4X/SpatialAndMiracleIntegration.md`

---

## Step 8: Testing Authoring

**EditMode Tests:**
```csharp
[Test]
public void Authoring_BakesCorrectly()
{
    // Create GameObject with authoring
    var go = new GameObject("Test");
    var authoring = go.AddComponent<MyAuthoring>();
    authoring.ConfigValue = 42;

    // Convert to entity
    var world = new World("Test");
    var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(go, world);

    // Verify components
    Assert.IsTrue(world.EntityManager.HasComponent<MyConfig>(entity));
    var config = world.EntityManager.GetComponentData<MyConfig>(entity);
    Assert.AreEqual(42, config.ConfigValue);
}
```

**PlayMode Tests:**
- Load SubScene in test scene
- Verify entities exist with correct components
- Verify registries populate correctly

---

## Common Patterns & Pitfalls

### Pattern: Extending Base Authoring

**✅ GOOD:** Extend base authoring, add game-specific components
```csharp
// Base: PureDOTS.Authoring.LauncherAuthoring
// Game: Godgame.Authoring.GodgameSlingshotAuthoring extends it
```

**❌ BAD:** Copy-paste base authoring code
```csharp
// Don't duplicate base authoring logic
```

### Pattern: Config vs State

**✅ GOOD:** Config = bake-time settings, State = runtime values
```csharp
public struct LauncherConfig : IComponentData { ... }  // Set by Baker
public struct LauncherState : IComponentData { ... }   // Modified by systems
```

**❌ BAD:** Mixing config and state
```csharp
// Don't put runtime state in config component
```

### Pattern: TransformUsageFlags

**✅ GOOD:** Use `Dynamic` for moving entities, `Static` for buildings
```csharp
GetEntity(TransformUsageFlags.Dynamic);   // Moves at runtime
GetEntity(TransformUsageFlags.Static);    // Never moves
```

**❌ BAD:** Using wrong flags
```csharp
// Don't use Dynamic for static terrain
// Don't use Static for moving entities
```

### Pitfall: Missing Transform

**Problem:** Entity needs transform but authoring doesn't provide it
```csharp
// ❌ BAD: No LocalTransform added
var entity = GetEntity(TransformUsageFlags.None);
AddComponent<MyComponent>(entity);  // Entity has no position!
```

**Solution:** Use appropriate TransformUsageFlags
```csharp
// ✅ GOOD: TransformUsageFlags.Dynamic adds LocalTransform automatically
var entity = GetEntity(TransformUsageFlags.Dynamic);
```

### Pitfall: Non-Deterministic Baking

**Problem:** Using random or time-based values in Baker
```csharp
// ❌ BAD: Non-deterministic
AddComponent(entity, new MyConfig { Value = Random.Range(0, 100) });
```

**Solution:** Use deterministic seeds or config values
```csharp
// ✅ GOOD: Deterministic
AddComponent(entity, new MyConfig { Value = authoring.ConfigValue });
```

---

## File Locations Summary

| What | Where |
|------|-------|
| Base Authoring | `PureDOTS/.../Runtime/Authoring/` |
| Game Authoring | `<Game>/Assets/Scripts/<Game>/Authoring/` |
| Prefabs | `<Game>/Assets/Prefabs/` or `PureDOTS/Assets/PureDOTS/Prefabs/` |
| SubScenes | `<Game>/Assets/Scenes/` |
| Config Assets | `<Game>/Assets/<Game>/Config/` or `PureDOTS/Assets/PureDOTS/Config/` |
| Authoring Tests | `<Game>/Assets/Scripts/<Game>/Tests/` or `PureDOTS/Assets/Tests/EditMode/` |

---

## Integration Checklist

**When creating new authoring:**
- [ ] Decide: Authoring vs runtime-only component?
- [ ] Choose TransformUsageFlags (Dynamic/Static/None)
- [ ] Separate Config (bake-time) from State (runtime)
- [ ] Add registry tags if entity should appear in registries
- [ ] Add spatial tags if entity needs spatial indexing
- [ ] Create prefab if entity is reusable
- [ ] Add to SubScene for deterministic conversion
- [ ] Write EditMode test for baking correctness

---

## See Also

- `[PureDOTS/Docs/Recipes/Recipe_Template.md](Recipe_Template.md)` - Feature implementation template
- `[PureDOTS/Docs/INTEGRATION_GUIDE.md](../INTEGRATION_GUIDE.md)` - Integration patterns
- `[Space4x/Docs/Guides/Space4X/SpatialAndMiracleIntegration.md](../../../Space4x/Docs/Guides/Space4X/SpatialAndMiracleIntegration.md)` - Spatial integration guide
- Unity DOTS 1.4 Documentation - Baker API, TransformUsageFlags, SubScenes






