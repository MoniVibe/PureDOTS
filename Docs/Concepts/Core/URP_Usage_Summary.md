# URP Usage Summary

**Status:** Current State / Architecture Summary
**Category:** Core - Rendering Pipeline
**Audience:** Architects / Technical Advisors
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

**Universal Render Pipeline (URP) is required for Entities Graphics (DOTS rendering).** All projects (PureDOTS, Space4X, Godgame) use URP as the render pipeline. Without URP assigned, Entities Graphics is disabled and no entities render.

**Key Principle:** URP is a hard dependency for DOTS entity rendering—not optional, not legacy.

---

## Why URP is Required

### Entities Graphics Dependency

**Entities Graphics requires a Scriptable Render Pipeline (SRP) to render entities:**

- Without an SRP: `EntitiesGraphicsSystem` is disabled, no entities render, camera shows only background color
- With URP: Entities Graphics initializes normally, entities with proper components become visible

**Error when URP missing:**
```
No SRP present, no compute shader support, or running with -nographics. Entities Graphics package disabled.
```

**Reference:** `puredots/Docs/Archive/Fixes/2025/Render_Pipeline_Fix.md`

### Compatibility

- **Universal Render Pipeline (URP):** ✅ Fully supported
- **High Definition Render Pipeline (HDRP):** ✅ Fully supported
- **Built-in Render Pipeline:** ❌ NOT supported (legacy, no SRP)

---

## URP Configuration

### Pipeline Assets

**Each project has URP assets configured:**

- **Pipeline Asset:** Universal Render Pipeline Asset (URP settings: quality, rendering, etc.)
- **Renderer Asset:** Universal Renderer Asset (renderer features, shaders, etc.)

**Locations:**
- `space4x/Assets/Settings/PC_Renderer.asset` - Space4X PC renderer
- `space4x/Assets/Settings/Mobile_Renderer.asset` - Space4X mobile renderer
- `puredots/Assets/Projects/Space4X/Settings/PC_Renderer.asset`
- `puredots/Assets/Projects/Space4X/Settings/Mobile_Renderer.asset`
- `puredots/Assets/Projects/Godgame/Settings/PC_Renderer.asset`
- `puredots/Assets/Projects/Godgame/Settings/Mobile_Renderer.asset`
- `godgame/Assets/Rendering/ScenarioURP_Renderer.asset`
- `space4x/Assets/Resources/Rendering/ScenarioURP_Renderer.asset`

### Assignment Requirements

**URP must be assigned in two places:**

1. **Graphics Settings:**
   - Edit > Project Settings > Graphics
   - Set "Scriptable Render Pipeline Settings" to URP Asset

2. **Quality Settings:**
   - Edit > Project Settings > Quality
   - For each quality level, set "Render Pipeline" to URP Asset

**Automated Fix Tool:**
- Unity Menu: `Space4X > Fix Render Pipeline (URP Required)`
- Creates URP assets and assigns them automatically
- **File:** `Assets/Scripts/Editor/RenderPipelineFixer.cs`

---

## Renderer Features

### Available Renderer Features

**Unity URP provides these renderer features (available in projects):**

- `DecalRendererFeature` - Decal rendering
- `FullScreenPassRendererFeature` - Fullscreen post-processing effects
- `RenderObjects` - Custom render object passes
- `ScreenSpaceAmbientOcclusion` - SSAO effect
- `ScreenSpaceShadows` - Screen-space shadow rendering

**Current Usage:**

- **FullScreenPassRendererFeature:** Present in Space4X renderer assets (disabled by default, available for gravitational lensing effect)
- **Other features:** Available but not actively configured in current renderer assets

**Reference:** `godgame/Assets/RendererFeaturesList.txt`, `space4x/Assets/Settings/PC_Renderer.asset`

### Custom Renderer Features

**Planned/Designed:**

- **Gravitational Lensing Feature:** Screen-space distortion for black holes (Tier 1: masked screen-space, Tier 2: +depth gating, Tier 3: ray-traced geodesics)
- **Reference:** `space4x/Docs/Rendering/Gravitational_Lensing_Effect.md`

**Implementation Pattern:**
- Use `ScriptableRendererFeature` + `ScriptableRenderPass`
- Request Color/Depth/Normals/Motion as needed
- Inject at chosen injection point (e.g., `BeforeRenderingPostProcessing`)

---

## Materials and Shaders

### URP Shaders Used

**Projects use standard URP shaders:**

- `Universal Render Pipeline/Lit` - Main PBR shader (most materials)
- `Universal Render Pipeline/Unlit` - Unlit shader (fallback, simpler materials)

**Code Pattern:**
```csharp
Shader.Find("Universal Render Pipeline/Lit") 
  ?? Shader.Find("Universal Render Pipeline/Unlit")
  ?? Shader.Find("Standard")  // Fallback
```

**Reference:** `godgame/Assets/Editor/FixMaterialsAndCatalogFinal.cs`, `space4x/Assets/Scripts/Space4x/Registry/Space4XMiningDebugRenderSystem.cs`

### Material Setup

**Materials are configured to use URP shaders:**

- **Godgame materials:** `Assets/Materials/Godgame/*.mat` (Villager_Orange, VillageCenter_Gray, ResourceNode_Cyan, etc.)
- **Space4X materials:** Via Render Catalog system (mesh/material arrays in shared components)

**Editor Tools:**
- `FixGodgameMaterials.cs` - Ensures materials use URP shaders
- `FixMaterialsAndCatalogFinal.cs` - Updates materials and render catalog

---

## Entities Graphics Integration

### Rendering Components

**Entities need specific components to render via Entities Graphics:**

- `MaterialMeshInfo` - Specifies mesh and material indices
- `RenderBounds` - Bounding box for culling
- `WorldRenderBounds` - World-space bounds
- `RenderFilterSettings` - Layer, shadow, etc. settings
- `RenderMeshArray` (shared component) - Contains meshes and materials arrays

**Reference:** `puredots/Docs/Archive/Fixes/2025/Camera_Background_Only_Fix.md`

### Render Catalog System (Space4X)

**Space4X uses a Render Catalog system for entity rendering:**

**Data Flow:**
```
Space4XRenderCatalogDefinition (ScriptableObject)
  ↓ [Baker: RenderCatalogBaker]
RenderPresentationCatalog (IComponentData with BlobAssetReference)
  + RenderMeshArray shared component
  ↓ [Runtime: PureDOTS.ResolveRenderVariantSystem + ApplyRenderVariantSystem]
Entities with RenderSemanticKey + presenters → MaterialMeshInfo + RenderBounds
```

**Components:**
- `RenderSemanticKey` - Entity visual archetype ID
- `RenderVariantKey` - Resolved mesh/material variant (from catalog)
- `MeshPresenter` / `SpritePresenter` / `DebugPresenter` - Enableable presenter components

**Reference:** `space4x/Docs/Rendering/Space4X_RenderCatalog_TruthSource.md`

### System Groups

**Rendering systems run in PresentationSystemGroup:**

- PureDOTS presenter systems run before `EntitiesGraphicsSystem`
- Systems update `MaterialMeshInfo`, `RenderBounds` based on catalog/state
- `EntitiesGraphicsSystem` (Unity) performs actual rendering

**Reference:** `space4x/Docs/Rendering/Space4X_RenderCatalog_TruthSource.md` (Notes section)

---

## Project-Specific Setup

### Space4X

**Rendering Architecture:**
- Render Catalog system (mesh/material catalog with archetype mapping)
- Presentation lifecycle system assigns `RenderKey`, colors, visual-state tags
- LOD system updates `RenderLODData` based on camera distance
- Multiple renderer assets (PC vs Mobile)

**Key Systems:**
- `Space4XPresentationLifecycleSystem` - Assigns presentation components
- `Space4XPresentationLODSystem` - Updates LOD based on camera
- `RenderCatalogBaker` - Bakes catalog to blob assets

**Reference:** `space4x/Docs/Rendering/Space4X_RenderCatalog_TruthSource.md`, `space4x/Assets/Space4X/Docs/SPACE4X_PRESENTATION_AND_SCALE_PLAN.md`

### Godgame

**Rendering Architecture:**
- Similar catalog pattern (GodgameRenderCatalogDefinition)
- Materials use URP shaders (configured via editor tools)
- Villager/city rendering via Entities Graphics

**Key Files:**
- `Assets/Rendering/GodgameRenderCatalog.asset`
- `Assets/Materials/Godgame/*.mat` (URP materials)

**Reference:** `godgame/Assets/Editor/FixMaterialsAndCatalogFinal.cs`

### PureDOTS (Shared)

**PureDOTS provides:**
- Core rendering infrastructure (presenter systems, catalog resolution)
- `ResolveRenderVariantSystem` - Resolves variants from catalog
- `ApplyRenderVariantSystem` - Applies rendering components to entities
- Shared rendering components/types

**Reference:** `space4x/Docs/Rendering/Space4X_RenderCatalog_TruthSource.md` (Runtime Application section)

---

## Performance Considerations

### Optimization Strategies

**For large-scale rendering (100k+ entities):**

1. **Use Entities Graphics instancing**
   - Entities Graphics handles mesh instancing automatically
   - `MaterialMeshInfo` enables batching

2. **LOD System**
   - Update `RenderLODData` based on camera distance
   - Use different meshes/materials per LOD level
   - Hide entities beyond threshold (impostors or hidden)

3. **Culling**
   - Entities Graphics handles frustum culling via `RenderBounds`
   - Update bounds efficiently (only when transform changes)

**Reference:** `puredots/Docs/Audit/DOTS_1.4_Fix_Recommendations.md` (Rendering Optimization section)

### Scalability

**Target Entity Counts:**
- **Space4X Demo_01:** ~50-100 entities (carriers, crafts, asteroids)
- **Scale Test 1:** 10k entities
- **Scale Test 2:** 100k entities
- **Scale Test 3:** 1M entities (with LOD/impostors)

**Reference:** `space4x/Assets/Space4X/Docs/SPACE4X_PRESENTATION_AND_SCALE_PLAN.md`

---

## Known Issues and Fixes

### Issue 1: URP Not Assigned

**Symptom:** "No SRP present" error, Entities Graphics disabled, no entities render.

**Solution:**
- Run automated fix: `Space4X > Fix Render Pipeline (URP Required)`
- Or manually assign URP Asset to Graphics Settings and Quality Settings

**Reference:** `puredots/Docs/Archive/Fixes/2025/Render_Pipeline_Fix.md`

### Issue 2: Entities Not Rendering (Camera Background Only)

**Root Causes:**
1. Entities missing rendering components (`MaterialMeshInfo`, `RenderBounds`)
2. Camera settings (position, culling mask, far/near planes)
3. SubScene not loaded/baked

**Solution:**
- Verify entities have proper rendering components
- Check camera position/rotation/culling mask
- Ensure SubScene is closed (baked) during Play Mode

**Reference:** `puredots/Docs/Archive/Fixes/2025/Camera_Background_Only_Fix.md`

### Issue 3: Extra Blits from Renderer Features

**Symptom:** Performance cost from renderer features (extra intermediate blits).

**Solution:**
- Check Frame Debugger to verify blit count
- Minimize renderer feature count
- Use efficient injection points

**Reference:** `space4x/Docs/Rendering/Gravitational_Lensing_Effect.md` (Key Knobs to Keep It Cheap section)

---

## Future Considerations

### HDRP Migration (If Needed)

**If switching to HDRP:**
- Same concept (SRP-based rendering)
- Use Custom Post Process (C# volume + fullscreen shader) instead of Full Screen Pass Renderer Feature
- Clear injection points: "BeforeTAA / AfterPostProcess"

**Reference:** `space4x/Docs/Rendering/Gravitational_Lensing_Effect.md` (HDRP Alternative section)

### VR Support

**Current State:**
- Entities Graphics + VR is possible but not battle-tested
- XR support has improved but still has rough edges
- Recommendation: Use Entities for simulation, consider GameObject rendering for player-facing VR

**Reference:** `puredots/Docs/DesignNotes/LastLightVR/LastLightVR_ConceptTranscript.md` (DOTS / Entities and VR section)

---

## Related Documentation

- **Render Pipeline Fix:** `puredots/Docs/Archive/Fixes/2025/Render_Pipeline_Fix.md`
- **Camera Rendering Fix:** `puredots/Docs/Archive/Fixes/2025/Camera_Background_Only_Fix.md`
- **Space4X Render Catalog:** `space4x/Docs/Rendering/Space4X_RenderCatalog_TruthSource.md`
- **Gravitational Lensing:** `space4x/Docs/Rendering/Gravitational_Lensing_Effect.md`
- **Space4X Presentation Plan:** `space4x/Assets/Space4X/Docs/SPACE4X_PRESENTATION_AND_SCALE_PLAN.md`
- **DOTS 1.4 Recommendations:** `puredots/Docs/Audit/DOTS_1.4_Fix_Recommendations.md`

---

**For Architects:** URP is a hard dependency for DOTS rendering—ensure it's assigned in Graphics/Quality settings  
**For Graphics Programmers:** Use Full Screen Pass Renderer Feature for post-processing effects, follow Entities Graphics patterns for entity rendering  
**For Technical Artists:** Materials must use URP shaders (`Universal Render Pipeline/Lit` or `/Unlit`), configure renderer features as needed

