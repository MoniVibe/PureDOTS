# Rain Clouds, Divine Hand, and Rain Miracle Overview

## Rain Cloud Entities
- `RainCloudAuthoring` (MonoBehaviour) converts rain cloud prefabs into DOTS entities with `RainCloudTag`, `RainCloudConfig`, `RainCloudState`, and `HandPickable`.
- Core config fields:
  - `BaseRadius`, `MinRadius`, `RadiusPerHeight`: determine the ground coverage radius (radius grows with altitude).
  - `MoisturePerSecond`, `MoistureFalloff`, `MoistureCapacity`: controls how much hydration is broadcast to vegetation. Capacity `0` means infinite supply.
  - `DefaultVelocity`, `DriftNoiseStrength/Frequency`, `FollowLerp`: how the cloud drifts when not held.
- `RainCloudState` tracks current velocity, moisture reserve, active radius, and age. `RainCloudMoistureHistory` buffer records per-tick application for rewind/debugging.
- Systems:
  - `RainCloudMovementSystem` (SimulationSystemGroup) advances cloud drift and updates active radius. If a cloud has `HandHeldTag`, velocity is frozen.
  - `RainCloudMoistureSystem` (VegetationSystemGroup) gathers all clouds, computes horizontal overlap with vegetation, and increments `VegetationHealth.WaterLevel`. Moisture usage feeds back into the cloud state/history buffer.

## Divine Hand Interaction
- `DivineHandAuthoring` seeds `DivineHandTag`, config, state, and input components.
- `DivineHandSystem` (HandSystemGroup) reads `DivineHandInput` to:
  - Find the nearest `HandPickable` within pickup radius when `GrabPressed`.
  - Attach/remove `HandHeldTag` and lerp the held entity to the cursor height (configurable hold offset).
  - Apply throw impulse (currently to `RainCloudState.Velocity`) when `ThrowPressed`, scaled by `ThrowCharge`.
- External gameplay/UI should set `DivineHandInput` every frame. The struct supports cursor position, aim direction, grab/throw button states, and a charge scalar.
- `HandPickable` config per entity lets authors adjust follow lerp, mass (future use), and throw multiplier.

## Rain Miracle Flow
- `RainMiracleAuthoring` defines the prefab, count, radius, height offset, and seed for a rain miracle.
- `RainMiracleCommandQueue` holds pending `RainMiracleCommand` buffer entries. `RainMiracleCommandBootstrapSystem` ensures the queue entity exists during initialization.
- `RainMiracleSystem` (HandSystemGroup) reads commands, instantiates the configured rain cloud prefab around the target position, and clears the queue. Cloud placement uses deterministic seeding, so replay/rewind remains stable.
- Rain miracles today spawn clouds only; players can then grab/throw the clouds, and vegetation receives hydration automatically. Future miracles can extend the same command queue pattern.

## Usage Checklist
1. Create a rain cloud prefab with `RainCloudAuthoring`, `PlaceholderVisualAuthoring`, and URP material for visuals/emission.
2. Place `DivineHandAuthoring` on the hand controller object (or an empty GO), wire Coplay/inputs to write `DivineHandInput`.
3. Add a `RainMiracleAuthoring` somewhere accessible (e.g., on a config singleton). From gameplay code, enqueue `RainMiracleCommand` with the desired target position.
4. Add vegetation with `VegetationAuthoring`; `RainCloudMoistureSystem` automatically boosts `VegetationHealth.WaterLevel` under active clouds.
5. Optional: use `SceneSpawnAuthoring` profiles to pre-place clouds (category `Miracle`) for playtests.

## Parity Gap Tracker (Hand & Camera)
- **Truth sources referenced**: `../godgame/truthsources/Hand_StateMachine.md`, `../godgame/truthsources/RMBtruthsource.md`, `../godgame/truthsources/Cameraimplement.md`, `../godgame/truthsources/Layers_Tags_Physics.md`.
- **Input routing**: No `RightClickRouter` equivalent; `DivineHandInputBridge` reads `Input.GetMouseButton` directly and bypasses New Input System prioritisation. `BW2StyleCameraController` still uses legacy input fallbacks, conflicting with New Input System–only requirement.
- **Layer/mask policy**: Current DOTS hand and camera code hard-code physics queries without validating against canonical `InteractionMask` / `groundMask` definitions mandated in `Layers_Tags_Physics.md`.
- **Hand state machine** *(updated)*: `DivineHandState` now tracks explicit states/timers and emits buffer-backed events via `DivineHandEventBridge`, but resource-specific guards (type locking, siphon cooldown, storehouse focus) still need scripting.
- **Resource discipline**: No enforcement of single held resource type, siphon capacity, or storehouse dump contracts. Router hysteresis/cooldowns described in RMBtruthsource §3 are absent.
- **Camera parity** *(updated)*: `BW2StyleCameraController` now locks orbit pivots, applies distance-scaled orbit sensitivity, implements grab-land plane panning, and performs collision sweeps; follow-up tuning remains for zoom-to-cursor smoothing and anchor snap parity.
- **Diagnostics/tests**: No fast tests for RMB handler priority, hand charge/cooldown, or camera movement to mirror truth-source acceptance checklists; CI hooks in Step 6 of the parity plan remain to be authored.

### Recent implementation notes
- `Assets/Scripts/PureDOTS/Runtime/DivineHandComponents.cs` defines `HandState` enums, cooldown tunables, and an event buffer consumed by `Runtime/Hand/DivineHandEventBridge.cs` for HUD/UI listeners.
- `DivineHandSystem` now manages cooldown, charge timers, and raises structured events; `DotsDebugHUD` listens via the bridge to display live hand state.
