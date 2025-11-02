<!-- Drafted October 2025 to organise the dual mining loop visualisation work -->
# Dual Mining Demo – Visual Pass Prep

## Goals
- Present villager and spaceship mining loops side by side within a single camera framing.
- Highlight parity between DOTS-driven gameplay loops (resource accumulation, deposits, idle states).
- Keep implementation DOTS-friendly: pooled rendering, burst-friendly data access, authoring simplicity.

## Scene Layout
- **Villager zone (right side)**: retain existing `Storehouse_Main` anchor; add pooled placeholder meshes for miners, resource nodes, deposit FX.
- **Space vessel zone (left side)**: reuse `Space4X` registries; position mining asteroids + carrier dock within shared grid coordinates.
- **Shared midline**: add unobtrusive divider (e.g. ground decal) to indicate split demo lanes.

## Rendering Strategy
- **Hybrid Renderer V2** via DOTS: attach `RenderMeshArray`/`MaterialMeshInfo` through bakers for both villager and vessel entities.
- **Pooled VFX/Indicators**: author two `MiningLoopVisualAuthoring` prefabs (villager/vessel) providing burst-compatible buffer data for spawn/snapping.
- **Command Stream**: schedule a late `ISystem` (`MiningLoopVisualSyncSystem`) reading registries/diagnostics and writing to a lightweight visual buffer consumed by a presentation system.

## Interaction & UI
- **Diagnostics Overlay**: extend `RuntimeConfigConsoleBehaviour` commands (`demo visuals show/hide`) to toggle visual layer.
- **HUD Labels**: author `Entities Graphics` billboard for each loop showing current throughput (read from registries).
- **Camera**: keep `Space4XCameraRenderSyncSystem`, but add cinematic framing preset (35° angle, 0.75 zoom) toggled via config var.

## Implementation Checklist
1. ~~Author two pooled visual prefabs (villager miner, vessel miner) with DOTS-compatible renderers.~~ → `Assets/Visuals/Prefabs/*MiningVisual.prefab`
2. ~~Extend registries to expose mining throughput snapshots (total/minute) for HUD.~~ → tracked via `MiningVisualManifest` metrics
3. ~~Implement `MiningLoopVisualSyncSystem` (RecordSimulation) that maps registry entries to visual spawn buffer.~~
4. ~~Presentation system consumes buffer, repositions pooled GameObjects or Graphics entities.~~
5. ~~Add runtime config switches and editor gizmos for alignment/QA.~~ (`visuals <show|hide|toggle|hud>` console command)
6. Document workflow in `Docs/Guides/DualMiningDemo.md` once visuals stable.

## Risks & Mitigations
- **Registry timing**: Ensure visual sync runs after registries; use `[UpdateAfter(typeof(StorehouseRegistrySystem))]` etc.
- **Performance**: Limit per-frame allocations; pre-size buffers using registry counts.
- **Authoring drift**: Bake authoring prefabs into sub-scenes to keep conversions deterministic.

