# Extension Request: Export SpatialPartitionAuthoring Type

**Status**: `[RESOLVED]`  
**Submitted**: 2025-11-27  
**Game Project**: Space4X  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Space4X's scene setup wizard (`Space4XSceneSetupMenu.cs`) provides editor menu commands to configure scenes for gameplay. One feature attempts to set up spatial partitioning via `PureDOTS.Runtime.Spatial.SpatialPartitionAuthoring`, but this type either:

1. Doesn't exist in PureDOTS
2. Exists in a different namespace
3. Exists but isn't public/exported

This blocks the scene setup menu's spatial configuration feature.

---

## Proposed Solution

**Extension Type**: New Component Export

**Details:**

Create and export `SpatialPartitionAuthoring` in `PureDOTS.Runtime.Spatial` namespace:

```csharp
namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Authoring component for spatial partition configuration.
    /// Attach to a GameObject in the subscene to configure spatial queries.
    /// </summary>
    public class SpatialPartitionAuthoring : MonoBehaviour
    {
        [Header("Grid Configuration")]
        public float CellSize = 10f;
        public int GridWidth = 100;
        public int GridHeight = 100;
        public int GridDepth = 100;
        
        [Header("Query Settings")]
        public int MaxEntitiesPerCell = 64;
        public bool EnableDynamicResize = false;
        
        public class Baker : Baker<SpatialPartitionAuthoring>
        {
            public override void Bake(SpatialPartitionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpatialPartitionConfig
                {
                    CellSize = authoring.CellSize,
                    GridWidth = authoring.GridWidth,
                    GridHeight = authoring.GridHeight,
                    GridDepth = authoring.GridDepth,
                    MaxEntitiesPerCell = authoring.MaxEntitiesPerCell,
                    EnableDynamicResize = authoring.EnableDynamicResize
                });
            }
        }
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New file: `Packages/com.moni.puredots/Runtime/Authoring/Spatial/SpatialPartitionAuthoring.cs`
- May need: `SpatialPartitionConfig` component if not already defined

**Breaking Changes:**
- No - this is a new addition

---

## Example Usage

```csharp
// In Space4X scene setup menu
using PureDOTS.Runtime.Spatial;

[MenuItem("Space4X/Setup/Add Spatial Partition")]
static void AddSpatialPartition()
{
    var go = new GameObject("SpatialPartition");
    var authoring = go.AddComponent<SpatialPartitionAuthoring>();
    authoring.CellSize = 50f;  // Space game scale
    authoring.GridWidth = 200;
    authoring.GridHeight = 200;
    authoring.GridDepth = 200;
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Space4X creates own spatial authoring
  - **Rejected**: Would duplicate PureDOTS spatial infrastructure
  - **Rejected**: Wouldn't integrate with PureDOTS spatial queries

- **Alternative 2**: Configure spatial via code only (no authoring)
  - **Rejected**: Less designer-friendly
  - **Rejected**: Harder to tune per-scene

---

## Implementation Notes

**If type exists in different namespace:**
- Please document correct namespace
- Space4X will update its using statements

**If type is intentionally internal:**
- Please confirm
- Space4X will implement local spatial setup

**Current Workaround:**
Space4X has commented out spatial setup code in `Space4XSceneSetupMenu.cs` (lines 198, 232).

**Error Reference:**
```
Assets\Scripts\Space4x\Editor\Space4XSceneSetupMenu.cs(198,86): error CS0234: 
The type or namespace name 'SpatialPartitionAuthoring' does not exist in the namespace 
'PureDOTS.Runtime.Spatial'
```

---

## Review Notes

**Reviewer**: Automated  
**Review Date**: 2025-11-27  
**Decision**: Already Resolved  
**Notes**: 

`SpatialPartitionAuthoring` already exists in `PureDOTS.Authoring` namespace at:
`Packages/com.moni.puredots/Runtime/Authoring/SpatialPartitionProfile.cs` (line 242)

**Resolution**: Space4X should update its `using` statement from:
```csharp
using PureDOTS.Runtime.Spatial;
```
to:
```csharp
using PureDOTS.Authoring;
```

The component works with `SpatialPartitionProfile` ScriptableObject assets and provides full spatial grid configuration with gizmo support. 

