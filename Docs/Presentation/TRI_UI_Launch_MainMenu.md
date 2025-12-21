# TRI — Shared Launch + Main Menu Stubs (PureDOTS)
**Status:** Implemented stubs (logic + authoring), UI wiring pending  
**Last Updated:** 2025-12-21

This is the shared (game-agnostic) launch/menu foundation for **Godgame** and **Space4X**.

Goal: unify “main menu / settings / new game / difficulty / custom game / world-gen request” as **PureDOTS data + systems**, with game-specific UI rendering on top.

---

## What shipped

### Runtime components (PureDOTS)
- `PureDOTS.Runtime.Launch.LaunchRootTag` + `TriGame` + `LaunchMenuState`
- `UserSettings` (audio/UI stubs)
- `NewGameDraft` + presets (`DifficultyPreset`, `DensityPreset`, `WorldGenSizePreset`)
- `LaunchCommand` buffer (UI writes commands; ECS consumes)
- `WorldGenRequest` + `WorldGenStatus` (world-gen handshake)
- `UseWorldGenStubTag` (opt-in stub worldgen)

### Runtime systems (PureDOTS.Systems)
- `PureDOTS.Systems.Launch.LaunchMenuSystem` (state machine; inert unless `LaunchRootTag` exists)
- `PureDOTS.Systems.Launch.WorldGenStubSystem` (Requested → Completed immediately; only when `UseWorldGenStubTag` exists)

### Authoring (PureDOTS.Authoring)
- `PureDOTS.Authoring.Launch.LaunchRootAuthoring` (bakes the launch root entity + defaults)
- `NewGamePresetDef` (custom game authoring asset)
- `UserSettingsDef` (settings authoring asset)

---

## How to wire it into a menu scene (either game)

1. Create a new scene (e.g. `MainMenu.unity`) in the **game repo** (not in PureDOTS).
2. Add an empty GameObject named `LaunchRoot`.
3. Add component `PureDOTS.Authoring.Launch.LaunchRootAuthoring`.
4. Set:
   - `Game Id`: `Godgame` or `Space4X`
   - `Start Screen`: `MainMenu`
   - Optional `Default New Game Preset` (create via `Create > PureDOTS > Launch > New Game Preset`)
   - Optional `Default User Settings` (create via `Create > PureDOTS > Launch > User Settings`)
   - `Use World Gen Stub`: true (temporary dev path)

This bakes a single entity that the launch systems target.

---

## How UI drives it (UGUI or UI Toolkit)

UI code should:
1. Modify drafts (`NewGameDraft`, `UserSettings`) on the launch root entity.
2. Append a `LaunchCommand` to the `DynamicBuffer<LaunchCommand>` on that same entity.

Minimal example (pseudo-code):
```csharp
// Get the LaunchRoot entity via query for LaunchRootTag
// Update draft components directly…
entityManager.SetComponentData(root, draft);
// …then enqueue a command
var commands = entityManager.GetBuffer<LaunchCommand>(root);
commands.Add(new LaunchCommand { Type = LaunchCommandType.StartNewGame });
```

World generation implementations (game-side) should consume `WorldGenRequest` and update `WorldGenStatus` (and eventually load/spawn the actual world). For now, `UseWorldGenStubTag` makes it complete immediately and flips the menu state to `InGame`.

---

## Time controls (shared)

The launch root also accepts time-control commands via the same `LaunchCommand` buffer:
- `TogglePause`, `Pause`, `Resume`
- `SlowMo`, `FastForward`, `SpeedNormal`, `SetSpeed`
- `StepTicks`
- `RewindToggle`, `StartRewind`, `StopRewind`, `ScrubToTick`

These are bridged into the PureDOTS time spine through `PureDOTS.Systems.Launch.LaunchTimeControlSystem`.

---

## Next steps (when you’re ready)
- Godgame UI: build a card-shell menu UI that edits `UserSettings` / `NewGameDraft` and shows `WorldGenStatus`.
- Space4X UI: same, plus later scenario selection + boot args wiring.
- Replace `WorldGenStubSystem` usage by omitting `UseWorldGenStubTag` and adding a real game-specific worldgen system that:
  - creates/clears the world,
  - seeds RNG,
  - spawns initial entities / loads bootstrap scene, and
  - transitions `LaunchMenuState` to `InGame`.
