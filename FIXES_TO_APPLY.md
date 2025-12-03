# Compilation Error Fixes - Step-by-Step Guide

**IMPORTANT**: This workspace is PureDOTS (the shared framework). Game-specific fixes must be applied in the actual game project directories:
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame`
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x`

The `Assets/Projects/Godgame` folder in PureDOTS workspace is likely artifacts, not the real project code.

This document provides concrete fixes for each error bucket. Apply fixes in order as later errors often resolve as side-effects.

## Bucket 1: PureDOTS Camera Physics ✅ COMPLETE

**Status**: Already fixed. Stubs exist in `Packages/com.moni.puredots/Runtime/Physics/CameraPhysicsStubs.cs`

The camera controller uses Unity's `Physics.Raycast` directly, so no changes needed.

---

## Bucket 2: DOTS SourceGen / Jobs Errors

### 2.1 Split ApplyLODTintJob into 3 Jobs

**Error**: `SGJE0008: You have defined 3 Execute() method(s) in ApplyLODTintJob`

**Fix**: Find the file containing `ApplyLODTintJob` (likely in `Godgame_LODVisualizationSystem.cs` or similar) and split it:

```csharp
// OLD (WRONG):
public partial struct ApplyLODTintJob : IJobEntity
{
    void Execute(ref VillagerVisualState visualState, in PresentationLODState lod) { ... }
    void Execute(ref ResourceChunkVisualState visualState, in PresentationLODState lod) { ... }
    void Execute(ref VillageCenterVisualState visualState, in PresentationLODState lod) { ... }
}

// NEW (CORRECT):
[BurstCompile]
public partial struct ApplyVillagerLODTintJob : IJobEntity
{
    void Execute(ref VillagerVisualState visualState, in PresentationLODState lod) { /* ... */ }
}

[BurstCompile]
public partial struct ApplyResourceChunkLODTintJob : IJobEntity
{
    void Execute(ref ResourceChunkVisualState visualState, in PresentationLODState lod) { /* ... */ }
}

[BurstCompile]
public partial struct ApplyVillageCenterLODTintJob : IJobEntity
{
    void Execute(ref VillageCenterVisualState visualState, in PresentationLODState lod) { /* ... */ }
}
```

Then schedule all three jobs in the system's `OnUpdate`.

### 2.2 Ensure Visual State Components are IComponentData

**Error**: `SGJE0010: IJobEntity.Execute() parameter 'visualState' of type VillagerVisualState is not supported`

**Fix**: Ensure these components are defined as `struct : IComponentData` in the **Godgame project** (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`):

Create or update `Assets/Scripts/Godgame/Presentation/PresentationTagComponents.cs` in the Godgame project:

```csharp
using Unity.Entities;

namespace Godgame.Presentation
{
    public enum VillagerVisualStateId : byte
    {
        Idle, Walking, Gathering, Building, Combat, Resting
    }

    public struct VillagerVisualState : IComponentData
    {
        public VillagerVisualStateId Value;
    }

    public struct ResourceChunkVisualState : IComponentData
    {
        public byte StateId; // or enum if you have one
    }

    public struct VillageCenterVisualState : IComponentData
    {
        public byte StateId; // or enum if you have one
    }

    public struct PresentationLODState : IComponentData
    {
        public byte LODLevel; // 0 = highest detail, higher = lower detail
    }
}
```

### 2.3 Fix VegetationPresentationTag

**Error**: `SGQC001: WithAll<VegetationPresentationTag>() is not supported`

**Fix**: Make it a proper component in the **Godgame project** (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`):

Add to `Assets/Scripts/Godgame/Presentation/PresentationTagComponents.cs`:

```csharp
namespace Godgame.Presentation
{
    public struct VegetationPresentationTag : IComponentData { }
}
```

---

## Bucket 3: Namespace / Naming Issues

### 3.1 Remove `using Bakings;`

**Error**: `error CS0246: The type or namespace name 'Bakings' could not be found`

**Fix**: Remove `using Bakings;` from these files in the **Godgame project** (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`):
- `Assets/Scripts/Godgame/.../BiomeTerrainAgent.cs`
- `Assets/Scripts/Godgame/.../BiomeTerrainBindingAuthoring.cs`
- `Assets/Scripts/Godgame/.../GroundTileAuthoring.cs`
- `Assets/Scripts/Godgame/.../GroundTileSystems.cs`
- `Assets/Scripts/Godgame/.../FaunaAmbientSpawnSystem.cs`
- `Assets/Scripts/Godgame/.../MiraclePresentationAuthoring.cs`
- `Assets/Scripts/Godgame/.../MiraclePresentationSystem.cs`

After removing, delete generated code folder:
```
SystemGenerator/Unity.Entities.SourceGen.SystemGenerator.SystemGenerator/Temp/GeneratedCode/
```

Unity will regenerate without the `using Bakings;` reference.

### 3.2 Fix Godgame.Debug Collision

**Error**: `CS0101: The namespace 'Godgame' already contains a definition for 'Debug'`

**Status**: Already fixed! `GodgameTelemetryHUD.cs` uses `namespace Godgame.Debugging`, not `Godgame.Debug`.

If you find other files using `namespace Godgame.Debug`, rename to `Godgame.Debugging` or `GodgameDebug`.

### 3.3 Fix PresentationSystemGroup Ambiguity ✅ COMPLETE

**Status**: Already fixed! `GodgameRegistryBridgeSystem.cs` has:
```csharp
using PresentationSystemGroup = PureDOTS.Systems.PresentationSystemGroup;
```

If other files have this error, add the same alias.

---

## Bucket 4: Godgame Runtime vs Presentation Assembly Layering

### 4.1 Ensure Presentation Components are in Correct Assembly

**Fix**: Create/verify `PresentationTagComponents.cs` in the **Godgame project** (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`) at `Assets/Scripts/Godgame/Presentation/PresentationTagComponents.cs`:

```csharp
using Unity.Entities;

namespace Godgame.Presentation
{
    // LOD Components
    public struct PresentationLODState : IComponentData
    {
        public byte LODLevel;
    }

    // Visual State Components
    public enum VillagerVisualStateId : byte
    {
        Idle, Walking, Gathering, Building, Combat, Resting
    }

    public struct VillagerVisualState : IComponentData
    {
        public VillagerVisualStateId Value;
    }

    public struct ResourceChunkVisualState : IComponentData
    {
        public byte StateId;
    }

    public struct VillageCenterVisualState : IComponentData
    {
        public byte StateId;
    }

    // Biome Presentation
    public struct BiomePresentationData : IComponentData
    {
        public float MoistureLevel;
        public byte BiomeTypeId;
    }

    // Presentation Config
    public struct PresentationConfig : IComponentData
    {
        public float LODDistance0;
        public float LODDistance1;
        public float LODDistance2;
    }

    // Tags
    public struct VillagerPresentationTag : IComponentData { }
    public struct ResourceChunkPresentationTag : IComponentData { }
    public struct VillageCenterPresentationTag : IComponentData { }
    public struct VegetationPresentationTag : IComponentData { }
}
```

Ensure this file is in the `Godgame.Presentation` asmdef (or create one if missing).

### 4.2 Move Presentation Systems to Presentation Assembly

**Fix**: In the **Godgame project** (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`), move these systems to `Godgame.Presentation` asmdef:
- `Godgame_VillagerPresentationSystem`
- `Godgame_ResourceChunkPresentationSystem`
- `Godgame_VillagePresentationSystem`
- `Godgame_LODVisualizationSystem`
- `Godgame_DensityVisualizationSystem`
- `Godgame_PathfindingDebugSystem`
- `MiraclePresentationSystem` (visual part)
- `GroundTilePresentation` / `BiomeTerrain` visuals
- `Godgame_AutoPerformanceAdjustmentSystem`
- `Godgame_PerformanceValidationSystem`
- `Godgame_PresentationMetricsSystem`

Ensure `Godgame.Runtime` asmdef does NOT reference `Godgame.Presentation` asmdef.

### 4.3 SwappablePresentation Types

**Status**: Should exist in the **Godgame project** (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`). If missing, create:
- `SwappablePresentationBinding` - `Assets/Scripts/Godgame/Presentation/SwappablePresentationBinding.cs`
- `SwappablePresentationDirtyTag` - Same file
- `SwappablePresentationBindingAuthoring` - `Assets/Scripts/Godgame/Presentation/Authoring/SwappablePresentationBindingAuthoring.cs`

---

## Bucket 5: PureDOTS Time / Misc References

### 5.1 Fix TimeControlCommand Namespace

**Error**: `error CS0234: The type or namespace name 'Time' does not exist in the namespace 'PureDOTS.Systems'`

**Fix**: `TimeControlCommand` is in `PureDOTS.Runtime.Components` namespace, not `PureDOTS.Systems.Time`.

**Location**: `Packages/com.moni.puredots/Runtime/Runtime/TimeControlComponents.cs`

**Fix**: In any file using `TimeControlCommand` (in either PureDOTS or game projects), change:
```csharp
// OLD (WRONG):
using PureDOTS.Systems.Time;

// NEW (CORRECT):
using PureDOTS.Runtime.Components;
```

Then use `TimeControlCommand` directly (it's in the `PureDOTS.Runtime.Components` namespace).

**Note**: If the file is in the Godgame project, apply this fix there. If it's in PureDOTS, apply it here.

---

## Quick Checklist

- [x] Camera physics stubs exist
- [ ] Split ApplyLODTintJob (when file accessible)
- [ ] Ensure visual state components are IComponentData (when files accessible)
- [ ] Fix VegetationPresentationTag (when file accessible)
- [ ] Remove `using Bakings;` (when files accessible)
- [x] Godgame.Debug already uses Godgame.Debugging
- [x] PresentationSystemGroup already aliased
- [ ] Create PresentationTagComponents.cs in Godgame.Presentation
- [ ] Move presentation systems to Presentation asmdef
- [x] SwappablePresentation types exist
- [ ] Fix TimeControlCommand namespace references (when files accessible)

