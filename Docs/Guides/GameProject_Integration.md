# Integrating PureDOTS into a Game Project

This guide explains how to integrate PureDOTS as a formal framework dependency into a new or existing Unity game project.

## Prerequisites

- Unity 2022.3 or later
- PureDOTS package (local or Git repository)
- Game project structure ready

## Integration Steps

### Step 1: Add PureDOTS Dependency

**Edit your game project's `Packages/manifest.json`:**

```json
{
  "dependencies": {
    "com.moni.puredots": "file:../PureDOTS/Packages/com.moni.puredots"
  }
}
```

**For Git-based distribution:**

```json
{
  "dependencies": {
    "com.moni.puredots": "https://github.com/moni/puredots.git?path=/Packages/com.moni.puredots#v1.0.0"
  }
}
```

**Unity will automatically:**
- Import the PureDOTS package
- Compile PureDOTS assemblies
- Make them available to your game code

### Step 2: Create Game Assembly Definitions

**Create `Assets/Scripts/GameName.asmdef`:**

```json
{
  "name": "GameName.Runtime",
  "rootNamespace": "GameName",
  "references": [
    "Unity.Entities",
    "Unity.Entities.Hybrid",
    "Unity.Mathematics",
    "Unity.Burst",
    "Unity.Collections",
    "Unity.Transforms",
    "Unity.InputSystem",
    "PureDOTS.Runtime",
    "PureDOTS.Systems"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

**Key Points:**
- ✅ Reference `PureDOTS.Runtime` and `PureDOTS.Systems`
- ✅ Use game-specific namespace (`GameName`, not `PureDOTS`)
- ❌ Do NOT create assemblies in `PureDOTS` namespace

### Step 3: Setup Scene Configuration

**1. Create Root Scene** (`Assets/Scenes/Main.unity`)

**2. Add PureDOTS Configuration:**
- Create GameObject named `PureDotsConfig`
- Add `PureDotsConfigAuthoring` component
- Assign `PureDotsRuntimeConfig` asset (from PureDOTS package)

**3. Add Time Controls (Optional):**
- Create GameObject named `TimeControls`
- Add `TimeControlsAuthoring` component
- Allows keyboard time control during play

**4. Create SubScene:**
- Right-click in Hierarchy → **New Sub Scene** → **Empty Scene**
- Name it `GameEntities` and save
- **Close the SubScene** (click "←" to return to root)
- Ensure SubScene component has `AutoLoadScene = true`

### Step 4: Implement Game Systems

**Example: `Assets/Scripts/Systems/GameMovementSystem.cs`**

```csharp
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace GameName.Systems
{
    /// <summary>
    /// Game-specific movement system using PureDOTS infrastructure.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TimeSystemGroup))]
    public partial struct GameMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Access PureDOTS time system
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var deltaTime = timeState.FixedDeltaTime;

            // Process game entities
            foreach (var (transform, movement) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<GameMovement>>())
            {
                // Game-specific movement logic
                transform.ValueRW.Position += movement.ValueRO.Velocity * deltaTime;
            }
        }
    }
}
```

### Step 5: Create Game Components

**Example: `Assets/Scripts/Runtime/GameComponents.cs`**

```csharp
using Unity.Entities;
using Unity.Mathematics;

namespace GameName.Runtime
{
    /// <summary>
    /// Game-specific movement component.
    /// Uses PureDOTS infrastructure but is game-specific.
    /// </summary>
    public struct GameMovement : IComponentData
    {
        public float3 Velocity;
        public float Speed;
        public float3 TargetPosition;
    }
}
```

### Step 6: Create Game Authoring

**Example: `Assets/Scripts/Authoring/GameEntityAuthoring.cs`**

```csharp
using PureDOTS.Authoring;
using GameName.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace GameName.Authoring
{
    /// <summary>
    /// Authoring component for game entities.
    /// Uses PureDOTS bakers but adds game-specific components.
    /// </summary>
    public class GameEntityAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 5f;
        public Vector3 targetPosition;
    }

    public class GameEntityBaker : Baker<GameEntityAuthoring>
    {
        public override void Bake(GameEntityAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            // Add game-specific component
            AddComponent(entity, new GameMovement
            {
                Velocity = float3.zero,
                Speed = authoring.speed,
                TargetPosition = authoring.targetPosition
            });

            // Can also add PureDOTS components if needed
            // Example: AddComponent<ResourceSourceState>(entity);
        }
    }
}
```

### Step 7: Verify Integration

**Check Console:**
- ✅ No compilation errors
- ✅ PureDOTS systems initialize (`[PureDotsWorldBootstrap] Default DOTS world initialized`)
- ✅ Game systems register correctly

**Check Entities Window:**
- ✅ Entities created from GameObjects in SubScene
- ✅ Components assigned correctly

**Test in Play Mode:**
- ✅ Systems run correctly
- ✅ PureDOTS time system works
- ✅ Game logic executes

## Assembly Structure Best Practices

### Recommended Structure

```
GameProject/
├── Assets/
│   ├── Scripts/
│   │   ├── Runtime/           # Game components
│   │   │   └── GameComponents.cs
│   │   ├── Systems/           # Game systems
│   │   │   └── GameSystems.cs
│   │   ├── Authoring/         # Game authoring
│   │   │   └── GameAuthoring.cs
│   │   └── GameName.asmdef    # Main assembly
│   │
│   └── Scenes/
│       └── Main.unity         # Root scene
│
└── Packages/
    └── manifest.json          # References PureDOTS
```

### Multiple Assemblies (Optional)

For larger projects:

```
GameProject/Assets/Scripts/
├── GameName.Runtime.asmdef    # Core game components
├── GameName.Systems.asmdef    # Game systems
├── GameName.Authoring.asmdef  # Authoring components
└── GameName.Editor.asmdef     # Editor tools
```

**All assemblies should reference PureDOTS:**

```json
{
  "references": [
    "PureDOTS.Runtime",
    "PureDOTS.Systems"
  ]
}
```

## Using PureDOTS Features

### Time System

```csharp
var timeState = SystemAPI.GetSingleton<TimeState>();
var deltaTime = timeState.FixedDeltaTime;
var currentTick = timeState.Tick;
```

### Rewind System

```csharp
var rewindState = SystemAPI.GetSingleton<RewindState>();
if (rewindState.Mode != RewindMode.Record)
{
    return; // Skip during rewind/playback
}
```

### Registry System

```csharp
// Query registry
var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
var entries = SystemAPI.GetBuffer<ResourceRegistryEntry>(registryEntity);

// Use registry entries
foreach (var entry in entries)
{
    // Process registry entry
}
```

### Spatial Grid

```csharp
// Check spatial grid
if (SystemAPI.TryGetSingleton(out SpatialGridState spatialState))
{
    // Use spatial grid
    var cellId = GetCellId(position, spatialState);
}
```

## System Ordering

**Use PureDOTS system groups:**

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TimeSystemGroup))]
public partial struct GameSystem : ISystem
{
    // Runs after time systems
}
```

**Available Groups:**
- `TimeSystemGroup` - Time management
- `VillagerSystemGroup` - Framework villager systems
- `ResourceSystemGroup` - Resource management
- `SpatialSystemGroup` - Spatial grid updates
- `SimulationSystemGroup` - General simulation

## Troubleshooting

### Package Not Found

**Issue**: Unity can't find PureDOTS package

**Fix**: 
- Verify path in `manifest.json` is correct
- Check that PureDOTS package exists at specified path
- Refresh Unity (Assets → Refresh)

### Assembly Reference Errors

**Issue**: Can't reference PureDOTS assemblies

**Fix**:
- Ensure `PureDOTS.Runtime` and `PureDOTS.Systems` are in assembly references
- Check that PureDOTS package compiled successfully
- Verify assembly names match exactly

### Systems Not Running

**Issue**: Game systems don't execute

**Fix**:
- Check that `PureDotsConfigAuthoring` exists in scene
- Verify `PureDotsRuntimeConfig` asset is assigned
- Ensure systems are in correct system groups
- Check console for initialization errors

### Namespace Conflicts

**Issue**: Ambiguous type references

**Fix**:
- Use fully qualified names: `PureDOTS.Runtime.Components.TimeState`
- Or add `using PureDOTS.Runtime.Components;`
- Ensure game uses unique namespace (`GameName`, not `PureDOTS`)

## Version Management

### Locking to Specific Version

**For Production:**

```json
{
  "dependencies": {
    "com.moni.puredots": "https://github.com/moni/puredots.git?path=/Packages/com.moni.puredots#v1.0.0"
  }
}
```

### Using Latest (Development)

```json
{
  "dependencies": {
    "com.moni.puredots": "file:../PureDOTS/Packages/com.moni.puredots"
  }
}
```

### Updating PureDOTS

1. Update version in `manifest.json`
2. Unity will reimport package
3. Review `CHANGELOG.md` for breaking changes
4. Test game thoroughly
5. Update game code if API changed

## Best Practices

1. **Never Modify PureDOTS**: Always extend, never modify framework code
2. **Use Namespaces**: Keep game code in game-specific namespaces
3. **Version Lock**: Lock to specific versions for production builds
4. **Test Updates**: Thoroughly test when updating PureDOTS version
5. **Document Dependencies**: Document which PureDOTS version your game uses
6. **Follow Conventions**: Maintain separation between framework and game code

## Example: Complete Game Project Structure

```
MyGame/
├── Assets/
│   ├── Scripts/
│   │   ├── Runtime/
│   │   │   └── MyGameComponents.cs
│   │   ├── Systems/
│   │   │   └── MyGameSystems.cs
│   │   ├── Authoring/
│   │   │   └── MyGameAuthoring.cs
│   │   └── MyGame.asmdef
│   ├── Scenes/
│   │   └── Main.unity
│   └── Prefabs/
│       └── GameEntity.prefab
│
└── Packages/
    └── manifest.json        # References PureDOTS
```

## Next Steps

After integration:
1. Create game-specific components and systems
2. Setup game scenes with SubScenes
3. Implement game logic using PureDOTS infrastructure
4. Test deterministic behavior
5. Deploy and iterate

## See Also

- `PureDOTS_As_Framework.md` - Framework architecture overview
- `LinkingExternalGameProjects.md` - Linking multiple game projects











