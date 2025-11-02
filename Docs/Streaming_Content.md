# Streaming Section Content References

`StreamingSectionContentAuthoring` lets designers attach auxiliary assets to an existing `StreamingSectionAuthoring` without writing bespoke systems. The baker produces two optional buffers:

- `StreamingSectionPrefabReference` – wraps `EntityPrefabReference` so prefabs are loaded via `SceneSystem.LoadPrefabAsync` when the section is active and released on unload.
- `StreamingSectionWeakGameObjectReference` – stores `WeakObjectReference<GameObject>` instances for lightweight assets that should be fetched through the content pipeline.

`StreamingSectionContentSystem` runs after the core loader inside `RecordSimulationSystemGroup`. It honours rewind guards (skips work outside `RewindMode.Record`) and mirrors the Entities sample flow:

1. When a section transitions to `QueuedLoad`, `Loading`, or `Loaded`, prefab references are resolved and weak objects call `LoadAsync()`.
2. When a section enters `QueuedUnload`, `Unloading`, or `Unloaded`, pending loads are released and any prefab handles are passed back to `SceneSystem.UnloadScene`.

### Authoring workflow

1. Add `StreamingSectionAuthoring` to the SubScene anchor as usual.
2. On the same GameObject, add `StreamingSectionContentAuthoring`.
3. Populate:
   - **Entity Prefabs**: drag DOTS-ready GameObjects that should be available while the section is active (they are converted into `EntityPrefabReference`).
   - **Weak GameObject Assets**: assign `WeakObjectReference<GameObject>` entries (drag a prefab into the inspector slot). These assets remain in sync with the section lifecycle.
4. Additional systems can watch the resulting buffers to instantiate or bind the warmed assets deterministically.

Because the content system only executes during the record phase, rewind playback/catch-up does not trigger extra loads.

