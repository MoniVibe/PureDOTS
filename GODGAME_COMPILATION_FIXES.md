# Godgame Compilation Fixes - Step-by-Step Guide

**Location**: Apply these fixes in the **Godgame project** directory:
`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`

**NOT** in PureDOTS workspace!

---

## Problem Summary

The compiler can't see presentation components (`VillagerVisualState`, `PresentationLODState`, etc.) because:
1. Components are missing or in wrong assembly
2. `Godgame.Presentation.Authoring` namespace is missing
3. Systems using presentation types are in wrong assembly (`Godgame.Runtime` instead of `Godgame.Presentation`)
4. IJobEntity jobs have incorrect configuration

---

## Fix 2.1: Ensure Godgame.Presentation Components & Authoring Exist

### A. Centralize Presentation Components

**File**: `Assets/Scripts/Godgame/Presentation/PresentationTagComponents.cs`

Create or update this file with:

```csharp
using Unity.Entities;

namespace Godgame.Presentation
{
    public enum PresentationLOD : byte
    {
        LOD0_Full,
        LOD1_Mid,
        LOD2_Far,
    }

    public struct PresentationLODState : IComponentData
    {
        public PresentationLOD Value;
    }

    public enum VillagerVisualStateId : byte
    {
        Idle,
        Walking,
        Gathering,
        Carrying,
        Throwing,
        MiracleAffected,
    }

    public struct VillagerVisualState : IComponentData
    {
        public VillagerVisualStateId Value;
    }

    public enum ResourceChunkVisualStateId : byte
    {
        Normal,
        Carried,
        Shredded,
    }

    public struct ResourceChunkVisualState : IComponentData
    {
        public ResourceChunkVisualStateId Value;
    }

    public enum VillageCenterVisualStateId : byte
    {
        Normal,
        Prosperous,
        Starving,
        UnderMiracle,
        Crisis,
    }

    public struct VillageCenterVisualState : IComponentData
    {
        public VillageCenterVisualStateId Value;
    }

    public struct VegetationPresentationTag : IComponentData { }

    public struct BiomePresentationData : IComponentData
    {
        public float Fertility;
        public float Moisture;
        public float VegetationHealth;
    }

    public struct SwappablePresentationDirtyTag : IComponentData { }

    public struct SwappablePresentationBinding : IComponentData
    {
        public Entity Prefab;
        public int VariantIndex;
    }

    public struct PresentationConfig : IComponentData
    {
        public float LOD0Distance;
        public float LOD1Distance;
        public float LOD2Distance;
        // Add your density/budget fields here
    }
}
```

**Critical**: This file MUST be compiled into the `Godgame.Presentation` asmdef.
- In Unity, select the file → Inspector → verify it shows `Godgame.Presentation` assembly

### B. Create Authoring Namespace & Types

**Create folder**: `Assets/Scripts/Godgame/Presentation/Authoring/`

**File 1**: `Assets/Scripts/Godgame/Presentation/Authoring/SwappablePresentationBindingAuthoring.cs`

```csharp
using Unity.Entities;
using UnityEngine;

namespace Godgame.Presentation.Authoring
{
    public class SwappablePresentationBindingAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public int VariantIndex;

        public class Baker : Baker<SwappablePresentationBindingAuthoring>
        {
            public override void Bake(SwappablePresentationBindingAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new SwappablePresentationBinding
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Renderable),
                    VariantIndex = authoring.VariantIndex
                });
                AddComponent<SwappablePresentationDirtyTag>(entity);
            }
        }
    }
}
```

**File 2**: `Assets/Scripts/Godgame/Presentation/Authoring/PresentationConfigAuthoring.cs`

```csharp
using Unity.Entities;
using UnityEngine;

namespace Godgame.Presentation.Authoring
{
    public class PresentationConfigAuthoring : MonoBehaviour
    {
        public PresentationConfig Config;

        public class Baker : Baker<PresentationConfigAuthoring>
        {
            public override void Bake(PresentationConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.Config);
            }
        }
    }
}
```

**Critical**: Both files must be in `Godgame.Presentation` asmdef (same folder tree as `PresentationTagComponents.cs`)

---

## Fix 2.2: Move Presentation Systems to Godgame.Presentation Assembly

### Systems to Move

Ensure these scripts are compiled into `Godgame.Presentation` assembly:

1. `Assets/Scripts/Godgame/Presentation/Godgame_VillagerPresentationSystem.cs`
2. `Assets/Scripts/Godgame/Presentation/Godgame_ResourceChunkPresentationSystem.cs`
3. `Assets/Scripts/Godgame/Presentation/Godgame_VillagePresentationSystem.cs`
4. `Assets/Scripts/Godgame/Presentation/Godgame_PresentationMetricsSystem.cs`
5. `Assets/Scripts/Godgame/Presentation/Debug/Godgame_LODVisualizationSystem.cs`
6. `Assets/Scripts/Godgame/Presentation/Debug/Godgame_DensityVisualizationSystem.cs`
7. `Assets/Scripts/Godgame/Presentation/Debug/Godgame_PathfindingDebugSystem.cs`
8. `Assets/Scripts/Godgame/Presentation/Performance/Godgame_AutoPerformanceAdjustmentSystem.cs`
9. `Assets/Scripts/Godgame/Presentation/Performance/Godgame_PerformanceValidationSystem.cs`
10. `Assets/Scripts/Godgame/Presentation/Biomes/BiomeTerrainBindingAuthoring.cs`
11. `Assets/Scripts/Godgame/Presentation/Demo/GroundTileSystems.cs`
12. `Assets/Scripts/Godgame/Presentation/Miracles/MiraclePresentationSystem.cs`
13. `Assets/Scripts/Godgame/Presentation/Miracles/Godgame_RegionMiracleSystem.cs` (if presentation-only)

### How to Fix

**Option A: Physical Move (Recommended)**
- Move files physically under `Assets/Scripts/Godgame/Presentation/` (or subfolders)
- Ensure `Godgame.Presentation.asmdef` exists at `Assets/Scripts/Godgame/Presentation/`
- Ensure NO `Godgame.Runtime.asmdef` exists above them in folder tree

**Option B: Change Assembly Definition**
- Select each file → Inspector → change Assembly Definition to `Godgame.Presentation`

### Add Required Using Directives

In each moved script, ensure you have:

```csharp
using Godgame.Presentation;
```

For scripts that need authoring types:

```csharp
using Godgame.Presentation.Authoring;
```

### Verify Assembly Layout

After moving, SourceGen output should be under:
```
SystemGenerator/Unity.Entities.SourceGen.SystemGenerator.SystemGenerator/Temp/GeneratedCode/Godgame.Presentation/...
```

NOT under `Godgame.Runtime/...`

---

## Fix 2.3: Clean Up IJobEntity SourceGen Issues

### A. Remove InternalCompilerQueryAndHandleData References

**Files to check**:
- `Assets/Scripts/Godgame/Presentation/Debug/Godgame_LODVisualizationSystem.cs`
- `Assets/Scripts/Godgame/Presentation/Miracles/Godgame_MiracleInputBridgeSystem.cs`

**Search for**: `InternalCompilerQueryAndHandleData`

**Delete**: Any lines referencing `ApplyXJob.InternalCompilerQueryAndHandleData`

**Correct Job Pattern**:

```csharp
[BurstCompile]
public partial struct ApplyVillagerLODTintJob : IJobEntity
{
    void Execute(ref VillagerVisualState visualState, in PresentationLODState lodState)
    {
        // tint logic here
    }
}
```

**Schedule Normally**:

```csharp
protected override void OnUpdate()
{
    new ApplyVillagerLODTintJob { /* fields */ }.ScheduleParallel();
}
```

**Apply Same Pattern To**:
- `ApplyChunkLODTintJob`
- `ApplyVillageCenterLODTintJob`
- `ApplyMiracleTintJob`

### B. Ensure Parameters are Component Types

Now that all components are `struct : IComponentData` in `Godgame.Presentation`:
- `VillagerVisualState`
- `ResourceChunkVisualState`
- `VillageCenterVisualState`
- `PresentationLODState`
- `VegetationPresentationTag`
- `BiomePresentationData`

And jobs are in the same assembly (`Godgame.Presentation`), the `SGJE0010` and `SGQC001` errors should disappear.

### C. Regenerate SourceGen Code

**After moving scripts & fixing jobs**:

1. **Delete** the generated code folder:
   ```
   SystemGenerator/Unity.Entities.SourceGen.SystemGenerator.SystemGenerator/Temp/GeneratedCode/
   ```

2. **Let Unity recompile** - it will regenerate `GeneratedCode` based on new assembly layout

3. **Verify** generated files now reference correct types from `Godgame.Presentation`

---

## Quick Checklist

### PureDOTS ✅
- [x] Open `ScaleTestEditorMenu.cs` and comment out `ScenarioRunnerEntryPoints` reference
- [x] Rebuild PureDOTS; confirm no errors in `com.moni.puredots`

### Godgame (Apply in Godgame Project Directory)

- [ ] Ensure `PresentationTagComponents.cs` defines all required components in `Godgame.Presentation` namespace
- [ ] Verify `PresentationTagComponents.cs` is in `Godgame.Presentation` asmdef
- [ ] Create `SwappablePresentationBindingAuthoring` in `Godgame.Presentation.Authoring` namespace
- [ ] Create `PresentationConfigAuthoring` in `Godgame.Presentation.Authoring` namespace
- [ ] Move all presentation/debug/perf systems into `Godgame.Presentation` assembly
- [ ] Add `using Godgame.Presentation;` to files that need it
- [ ] Add `using Godgame.Presentation.Authoring;` to authoring files
- [ ] Remove all `Apply*Job.InternalCompilerQueryAndHandleData` references
- [ ] Ensure IJobEntity jobs use correct component types
- [ ] Delete `SystemGenerator/.../GeneratedCode/` and let Unity regenerate

---

## Notes

- **Critical**: All fixes must be applied in the **Godgame project directory**, NOT in PureDOTS workspace
- The `Godgame.Runtime` asmdef should **NOT** reference `Godgame.Presentation` for clean layering
- Presentation systems can reference Runtime, but Runtime should not reference Presentation
- After fixes, SourceGen should generate code in `Godgame.Presentation` assembly, not `Godgame.Runtime`

