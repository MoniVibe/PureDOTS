# Megastructures System (Shared Contract)

**Status**: Draft (shared PureDOTS contract)  
**Scope**: PureDOTS core + Space4X primary adoption; Godgame may reuse for mega-wonders  
**Goal**: Represent vast structures with minimal simulation entities and streamed, presentation-heavy detail.

---

## 0) Scale & Zoom Alignment

Megastructures must honor the shared scale/zoom contract (no scene splits, anchored proxies, camera-relative presentation at extreme scales). Presentation can swap proxies, but simulation pose is authoritative.

**Rule**: Interiors are streamed cells in structure-local space; root pose defines world placement. Presentation handles tier swaps and floating origin.

---

## 1) Core Representation: Root + Streamed Cells

### Root Entity (always loaded)
- `MegastructureRoot` (id, kind, seed, faction/ownership)
- `LocalTransform` (structure pose)
- `AABB`/bounds data (overall extent)
- `MegastructureState` (Constructed / UnderConstruction / Ruined / Cleared)
- High-level sims (optional): integrity, power, atmosphere
- `MegastructureStreamGridRef` (stream grid / cell index blob)

### Streamed Cells (loaded near player/active agents)
Each cell is keyed by `cellCoord` in structure-local space:
- `MegastructureCell` (root ref, cellCoord, state)
- Traversal: `NavChunk` / `PortalRef` buffers
- Occupancy: solid/void + walkable surfaces
- Gameplay hooks: spawners, loot, hazards, doors, triggers
- Presentation: render-only instances (PresentationSystemGroup)
**Rule**: The root is authoritative gameplay state. Cells are streamed for interaction/presentation, not authority.

---

## 2) Unified Mobility via Pose Chain

Every megastructure resolves its pose through a parent chain:
- **Inert**: no parent, fixed pose.
- **Orbital**: parent = star/planet; pose updated by orbit mechanics.
- **Embedded**: parent = asteroid/planetoid; local pose fixed or slowly shifting.
- **Mobile**: root also has vessel movement components (thrust/turn), still can orbit.

**Rule**: Simulation pose is authoritative; presentation never moves entities, only renders them.

---

## 2) Interiors & Cavities (Solid/Void Volume)

### Option A: Voxel/Occupancy Chunks (fastest for morphing)
- Chunked solid mask + material/deposit IDs
- Cavities = empty voxels inside hull volume
- Supports cutting/destruction/digging
- Requires meshing + streaming

### Option B: Modular Kit + Implicit Collision (best art fidelity)
- Authored modules (corridors, hangars, rings)
- Simple collision/occlusion proxies per module
- Destruction swaps modules to damaged variants

**Recommendation**:
- Space4X built megastructures → Option B (modules)
- Ruins/dungeons or diggable variants → Option A (voxels)

---

## 3) Navigation & Exploration

**Hierarchy**:
- Portal graph (cheap): rooms/zones/cells as nodes, doors/vents as edges
- Local steering within a room/cell (bounded movement, waypoint following)
- Optional traversal links (vents, breaches, crawl spaces)

**Goal**: avoid global 3D A*; use graph + local steering.

---

## 4) State Machines (Constructed vs Ruined)

### Constructed
- Root holds `ConstructionQueue` + progress
- Cells/modules spawn only when built (or scaffold variants)
- Power/atmo/security emerge from placed modules

### Ruined / Dungeon
- Root seeds a layout graph (deterministic by seed)
- Cells generate blocked portals, hazards, loot, faction presence
- "Cleared" flips portal locks/hazards; updates root state

**Rule**: Constructed and Ruined are the same structure type; only generator/state differ.

---

## 5) Rendering: Vast but Few Entities

- Simulation entities: root + streamed cells + interactables
- Render detail: Entities Graphics instances driven by per-cell instance buffers
- Occlusion: interior meshes + optional camera cut volumes

---

## 6) Interior Entry (No Discontinuity)

- Keep exterior proxy visible at macro scales.
- Stream nearby interior cells when the player boards/enters.
- Shift camera focus inside while exterior remains as proxy.

**Rule**: no scene swaps; focus + streaming + proxy swaps only.

---

## 7) First Slice (Ship-Ready)

1. `MegastructureRoot` + cell streaming (near player only)  
2. Authored ring-station modules (Option B)  
3. Portal graph + waypoint routing across zones  
4. "Ruined" variant (blocked portals + hazards + loot)

---

## 8) Ownership & Adoption

- **PureDOTS**: core data contracts, streaming rules, portal graph basics
- **Space4X**: default implementation + presentation + authoring (primary)
- **Godgame**: optional reuse for mega-wonders (shared contract, game-specific presentation)
