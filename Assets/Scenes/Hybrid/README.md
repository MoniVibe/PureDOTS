# Hybrid Showcase Scene Stub

This folder houses the `HybridShowcase.unity` scene and any supporting sub-scenes.  The Unity editor should contain:

1. A root GameObject with `HybridControlToggleAuthoring` so mode switching is available in play mode.
2. A pair of `SubScene` GameObjects (`Godgame SubScene`, `Space4X SubScene`) authored with the appropriate prefabs.
3. Optional presentation/UI GameObjects that visualize the current control mode.

The project scripts added in `PureDOTS/Packages/com.moni.puredots/Runtime/Hybrid` handle the runtime toggle logic.  Designers only need to populate the subscenes and hook up the UI events.



