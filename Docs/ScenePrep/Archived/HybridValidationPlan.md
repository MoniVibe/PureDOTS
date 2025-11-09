# Hybrid Showcase Validation Plan

Use this checklist after wiring the scene to confirm both games behave correctly when running side by side.

## Editor Validation

- [ ] Run `Window ▸ Entities ▸ Bake And Convert` with both subscenes open; ensure no conversion errors.
- [ ] Verify `HybridControlToggleAuthoring` updates HUD/UI when `F9` is pressed.
- [ ] Confirm that switching to `Space4XOnly` mode disables Godgame divine hand movement (cursor idle, villagers still simulate).
- [ ] Confirm that switching to `GodgameOnly` mode freezes the Space4X RTS camera while divine hand works.
- [ ] Switch back to `Dual` mode and confirm both control schemes operate simultaneously.

## Gameplay Validation

- [ ] Villagers gather from resource nodes and deposit into storehouses without entering the Space4X sandbox.
- [ ] Space4X miners travel to asteroid nodes, collect ore, and return to the carrier.
- [ ] Presentation registry spawns visuals for both factions (meshes loaded, materials correct).
- [ ] Performance check: frame time remains under target (e.g., 16.6 ms) when both loops run.

## Regression Safeguards

- [ ] Record a short play-session clip for documentation.
- [ ] Update `Docs/ScenePrep/HybridShowcaseChecklist.md` with completion state and link evidence.
- [ ] Note outstanding issues (if any) in `PureDOTS/Docs/Progress.md`.


