# Celestial Mechanics + Shadow Pipeline (Contract)

**Status**: Active (shared PureDOTS contract)  
**Scope**: PureDOTS (shared) → Space4X + Godgame  
**Goal**: Keep day/night, spatial orbit pose, and sunlight/shadow sampling separate but composable.

---

## 1) Two Orbit Layers (Keep Them Separate)

### A) Day/Night Orbit (time-of-day cycle)
- **Components**: `OrbitParameters`, `OrbitState`, `TimeOfDayState`, `TimeOfDayConfig`
- **Systems**: `OrbitAdvanceSystem` → `TimeOfDaySystem`
- **Output**: normalized time-of-day + `SunlightFactor`
- **Purpose**: deterministic day/night cycle used by environment + gameplay

### B) Spatial Orbit Pose (system-map placement)
- **Components**: `Celestial.OrbitalState`, `CelestialOrbitPose`
- **System**: `CelestialOrbitPoseSystem`
- **Output**: system-space position + forward/up (optional)
- **Purpose**: Space4X system map placement; Godgame can ignore pose or use for skybox

**Rule**: Do not overload the day/night orbit to drive spatial placement. Spatial pose is separate.

---

## 2) Sunlight Pipeline (Canonical Flow)

```
StarLuminosity → StarSolarYield → SunlightState.GlobalIntensity
      ↓
TimeOfDayState (per planet/world)
      ↓
SunlightGrid (per-cell sunlight, occlusion-aware)
      ↓
Vegetation / exposure / gameplay sampling
```

### Tier 1 (global)
- `SunlightDistributionSystem` writes `SunlightState.GlobalIntensity`
- Global intensity is deterministic and rewind-safe

### Tier 2 (spatial grid)
- `SunlightGridUpdateSystem` builds per-cell sunlight samples
- `SunlightGrid` is the authoritative spatial sunlight channel
- Vegetation and exposure systems **sample the grid**, not the global scalar

---

## 3) Occlusion / Shadowing (Upgrade Path)

### MVP (already implemented)
- `TerrainQueryFacade.IsSolid` returns true for solid volume
- `SunlightGridUpdateSystem` zeroes light in solid cells
- Result: caves/tunnels are dark, surface cells are lit

### Next (volume-aware)
- Swap the solid query to real voxel/diggable terrain volume
- Keep voxel data in local volume space
- Use `VolumeWorldToLocal` + `VolumeOrigin` to convert world → volume-local

### Later (large-body occlusion)
- Add analytic occluders for planets/asteroids (system-map shadows)
- Shadow factors can be cached per sector/region, not per-entity raycasts

---

## 4) Game Adoption Rules

### Godgame
- Spawn one planetoid entity with day/night components + `SunlightState`
- Keep terrain flat; light sampling alone makes caves dark

### Space4X
- Spawn a star + planetoid per colony
- Use spatial orbit pose for system-map placement (optional in local scenes)
- Ships/planetoids can use local frames for voxel interiors; pose is separate

---

## 5) Determinism / Time Rules

- Orbit pose uses **TimeState.Tick** (fixed-step deterministic)
- All light updates must honor pause/rewind guards
- Presentation reads `LocalTransform` only; sim owns pose updates

