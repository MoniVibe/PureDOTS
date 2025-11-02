# Camera Rendering Background Only - Fix Guide

## Problem
Camera only renders background color (solid color screen), no entities visible.

## Root Causes

### 1. Entities Missing Rendering Components
In DOTS/ECS, entities need specific rendering components to be visible:
- `MaterialMeshInfo` - Specifies mesh and material
- `RenderBounds` - Bounding box for culling
- `WorldRenderBounds` - World-space bounds
- `RenderFilterSettings` - Layer, shadow, etc. settings
- `RenderMeshArray` (shared component) - Contains meshes and materials

**Solution:** Ensure entities are baked from GameObjects with MeshRenderer/MeshFilter OR add rendering components at runtime.

### 2. Camera Settings Issues
- Camera position/orientation wrong
- Camera culling mask excludes entity layers
- Camera far/near planes incorrect
- Camera not pointing at entities

### 3. SubScene Not Loaded
If entities are in a SubScene, the SubScene must be loaded and baked.

## Quick Fixes

### Fix 1: Verify Camera Position and Rotation
Camera should be positioned to see entities:
- Position: (0, 15, -20) 
- Rotation: (60, 0, 0) - Looking down at XZ plane
- Forward vector should point toward where entities are

### Fix 2: Check Camera Culling Mask
1. Select Main Camera
2. In Inspector, check **Culling Mask**
3. Ensure it includes the layer where entities are rendered (usually "Default" or "Everything")

### Fix 3: Verify Entities Have Rendering Components
Entities need to be baked from GameObjects with:
- `MeshRenderer` component
- `MeshFilter` component
- Proper material assigned

OR entities need runtime rendering components added via `RenderMeshUtility.AddComponents`.

### Fix 4: Check SubScene Status
1. Verify SubScene is **closed** (not open) during Play mode
2. SubScene should show as "Baked" or "Live Link" in Hierarchy
3. Entities should appear in Entity Debugger (Window → Entities → Hierarchy)

### Fix 5: Verify Render Pipeline Asset
1. Edit → Project Settings → Graphics
2. Verify **Scriptable Render Pipeline Settings** is assigned
3. Should have a URP Asset assigned

## Camera Configuration Checklist

- [ ] Camera Position: (0, 15, -20) or appropriate for scene
- [ ] Camera Rotation: Looking toward entities (typically (60, 0, 0) for top-down)
- [ ] Camera Near Plane: 0.3 (not too close)
- [ ] Camera Far Plane: 1000 (not too far)
- [ ] Camera Field of View: 60 (for perspective) or appropriate Orthographic Size
- [ ] Camera Culling Mask: Includes entity layers (usually "Everything")
- [ ] Camera Clear Flags: Skybox or Solid Color (both work)
- [ ] Camera Depth: 0 (if multiple cameras, ensure Main Camera has lowest depth)

## Entity Rendering Checklist

- [ ] Entities exist (check Entity Debugger: Window → Entities → Hierarchy)
- [ ] Entities have `LocalToWorld` component (for transforms)
- [ ] Entities have `MaterialMeshInfo` component (for mesh/material selection)
- [ ] Entities have `RenderBounds` component (for culling)
- [ ] Entities have `RenderMeshArray` shared component (contains meshes/materials)
- [ ] Entities are in visible layers (check `RenderFilterSettings` layer)

## Debugging Steps

1. **Check Entity Count:**
   - Window → Entities → Hierarchy
   - Verify entities exist
   - If no entities, SubScene might not be loaded/baked

2. **Check Camera View:**
   - Select Main Camera
   - Scene view → Game view button (bottom-right)
   - Verify camera frustum (gizmo) shows correct view
   - Check if frustum overlaps entity positions

3. **Check Rendering Components:**
   - Window → Entities → Hierarchy
   - Select an entity
   - Inspector should show rendering components
   - If missing, entities aren't properly baked

4. **Check Console:**
   - Look for rendering errors
   - Look for missing component errors
   - Look for SubScene loading errors

## Common Issues

### Issue: Camera Looking Wrong Direction
**Symptom:** Background color visible, nothing else
**Fix:** 
- Check camera rotation
- Set rotation to (60, 0, 0) for top-down view
- Or calculate rotation to look at entity positions

### Issue: Entities Not Baked
**Symptom:** No entities in Entity Debugger
**Fix:**
- Ensure SubScene is closed (not open)
- Re-bake SubScene (right-click → Reimport SubScene)
- Check GameObject authoring components exist

### Issue: Entities Missing Rendering Components
**Symptom:** Entities exist but don't render
**Fix:**
- Ensure GameObjects have MeshRenderer/MeshFilter
- Ensure materials are assigned
- Re-bake SubScene
- OR add rendering components at runtime

### Issue: Camera Culling Entities
**Symptom:** Entities exist but aren't visible
**Fix:**
- Check camera culling mask includes entity layers
- Check camera far/near planes
- Check entities are within camera frustum

## Test Script

Create a simple test to verify entities render:

```csharp
// Add this to a MonoBehaviour in main scene
void Start()
{
    var world = World.DefaultGameObjectInjectionWorld;
    var entityManager = world.EntityManager;
    
    // Count entities with rendering components
    var renderQuery = entityManager.CreateEntityQuery(
        ComponentType.ReadOnly<MaterialMeshInfo>(),
        ComponentType.ReadOnly<LocalToWorld>()
    );
    
    Debug.Log($"Entities with rendering: {renderQuery.CalculateEntityCount()}");
    
    // Count all entities
    var allQuery = entityManager.CreateEntityQuery(typeof(LocalToWorld));
    Debug.Log($"Total entities: {allQuery.CalculateEntityCount()}");
}
```

## Expected Camera Setup for Mining Demo

- **Position:** (0, 15, -20) - Above and behind scene center
- **Rotation:** (60, 0, 0) - Looking down at 60-degree angle
- **Field of View:** 60 degrees
- **Near Plane:** 0.3
- **Far Plane:** 1000
- **Clear Flags:** Solid Color (skybox also works)
- **Culling Mask:** Everything

This setup should show entities in the XZ plane (ground level) when looking from above.





