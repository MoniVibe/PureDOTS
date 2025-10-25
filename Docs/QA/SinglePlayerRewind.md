# Single-Player Rewind QA Checklist

Maintains confidence that record / catch-up / playback behaviour matches the TruthSource specification. Run the following steps whenever we touch time systems, villager jobs, or registry services.

## Prerequisites
- Scene: `Scenes/Validation/RewindSandbox.unity` (or equivalent test scene with at least one villager, a resource node, and a storehouse).
- Prefab: ensure `RewindTimelineDebug` MonoBehaviour is active in the scene so mode + tick are visible in play mode.
- Enable the debug HUD (from `DebugDisplayReader`) to watch registry totals.

## Smoke Test
1. Enter Play Mode. Confirm the on-screen overlay reports `Mode: Record` and `Tick` increments.
2. Let the simulation run for **~10 seconds**. Ensure the villager gathers, carries, and deposits resources:
   - `VillagerJobTicket` reports an active ticket ID.
   - Storehouse totals rise accordingly.
3. Press the configured rewind input (default `Time/RewindHold`). Verify:
   - Overlay mode flips to `Playback` immediately.
   - Playback tick decreases smoothly; HUD totals rewind (villager unloads, storehouse total drops).
4. Continue holding rewind until at least 5 seconds are rewound, then release.
   - Mode switches to `CatchUp` for a brief period, then back to `Record`.
   - Villager resumes the job loop without duplicate tickets or negative inventory.

## Detailed Assertions
| Area | Expectation |
|------|-------------|
| Villager Jobs | `VillagerJobHistorySample` buffers contain samples at configured stride. During rewind, job phase and ticket ID snap to historical values. |
| Resource Registry | `ResourceRegistryEntry.UnitsRemaining` returns to pre-gather amounts when rewinding. |
| Storehouse Registry | `StorehouseRegistryEntry.TotalStored` as well as the per-resource `TypeSummaries` revert exactly. No residual reservations remain after returning to `Record`. |
| Commands & Events | `VillagerJobEventStream` does not grow during playback/catch-up. |

## Edge Cases
1. **Zero-length playback**: Tap the rewind control quickly. Mode should switch to `Playback` then immediately `CatchUp` without visual jitter.
2. **Rewind after storehouse fills**: Let the storehouse reach capacity, then rewind past the point it overflowed. Ensure the villager resumes correctly and capacity tallies reset.
3. **Catch-up stress**: Hold rewind to the very beginning, release, and confirm `CatchUpSimulationSystemGroup` processes multiple ticks without exploding the HUD or registries (watch for batched tick warnings).

## Failure Logging
- Any mismatch between HUD totals and registry buffers: file under “Rewind Registry Drift” with captured console output.
- Villager job resumes in an incorrect phase or loses ticket ID: log “Job Phase Desync” with timestamp.
- Playback tick moves in the wrong direction or sticks: log “Playback Tick Stalled”.

## Automation Notes
While the above is manual, we plan to convert the smoke test into a PlayMode test suite that drives rewind via the TimeControl command buffer and snapshots registry state before/after. Track status in `PureDOTS_TODO.md` under “Rewind QA Automation”.
