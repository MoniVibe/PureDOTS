# Streaming Cells – Current Status (Agent C)

## What’s implemented now
- Activation/deactivation by window: `CellStreamingSystem` toggles `SimulationCell.IsActive` based on `CellStreamingWindow` (XZ AABB) with hysteresis.
- In-memory deterministic “serialization”: `CellSerializationSystem` disables/enables agents per cell and updates `CellStreamingState`.
- Window updater: `CellStreamingWindowUpdateSystem` copies from `CellStreamingWindowTarget` to `CellStreamingWindow` each tick.
- Config/telemetry:
  - `CellStreamingConfig` { CellSize, Hysteresis, EstimatedAgentBytes }
  - `CellStreamingWindow`, `CellStreamingWindowTarget`
  - `CellStreamingMetrics` (active/serialized cells & agents, approx bytes)
- Buffer hygiene: `CellAgentBufferMaintenanceSystem` prunes destroyed agent references each tick.
- Helper utilities: `CellAgentBufferHelpers` provide add/remove/move helpers to keep buffers in sync on spawn/move.
- Debug/visibility: `CellStreamingDebugDrawSystem` draws window bounds and cell centers (Editor/dev only).
- Snapshot store scaffold: `CellSnapshotStore` records per-cell metadata (currently agent count) as a step toward EntityScene serialization.

## Known gaps / risks
- Persistence: uses `Disabled` toggling; snapshot store only tracks counts; no EntityScene-backed snapshotting yet (no disk/off-memory persistence).
- Memory accounting: approx bytes is a heuristic only.
- Agent membership: `CellAgentBuffer` is pruned for destroyed entities, but not auto-updated for move/spawn.
- Input feed: no live camera/player writer wired to `CellStreamingWindowTarget`.
- Debug/visibility: no gizmo/debug-draw to inspect windows/cell states.

## Next actions (Agent C)
1) Swap `Disabled` toggling for EntityScene-backed snapshotting; keep deterministic order and update `CellStreamingState`.
2) Add real memory accounting (chunk size or stream size) to `CellStreamingMetrics`.
3) Maintain `CellAgentBuffer` on spawn/move (helper API now provided); already prunes destroyed agents.
4) Wire `CellStreamingWindowTarget` from camera/player each frame (debug draw exists for window/cells).
5) Minimal demo scene showing enter/leave window transitions and metrics.
