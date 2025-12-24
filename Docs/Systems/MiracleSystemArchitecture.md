# Miracle System Architecture

**Status:** Active  
**Category:** System Documentation  
**Last Updated:** 2025-01-XX

## Overview

The Miracle System provides a data-driven framework for player-cast divine powers with deterministic area effects, inspired by Black & White 2. The system supports charge/tier mechanics (M2) and sustained channeling (M3), with full configurability via catalog assets.

## System Execution Order

All miracle systems run in `MiracleEffectSystemGroup`, which executes within `GameplaySystemGroup` after `ResourceSystemGroup`.

### OrderFirst Systems (run first, order among themselves matters):

1. **MiracleChargeSystem** - Updates charge state while input is held
   - Runs before `MiracleTargetingSystem` to ensure charge is available for preview

2. **MiracleTargetingSystem** - Computes targeting solutions for selected miracles
   - Runs before `MiracleRequestCreationSystem` to provide target data
   - Runs before `MiracleActivationSystem` for validation

3. **MiracleDetectabilityBootstrapSystem** - Initializes detectability state

4. **MiracleRequestCreationSystem** - Converts runtime state to activation requests
   - Runs after `MiracleTargetingSystem` to read target solutions
   - Runs before `MiracleActivationSystem` to create requests

5. **MiracleActivationSystem** - Consumes requests and spawns effect entities
   - Runs after request creation to process requests

### Regular Order Systems:

6. **MiracleSustainedTickSystem** - Applies sustained effects each tick
   - Runs after `MiracleActivationSystem` to process newly spawned sustained effects

7. **MiracleChannelStopSystem** - Stops channeling when input released
   - Runs after `MiracleSustainedTickSystem` to check stop conditions

8. **MiracleCooldownSystem** - Updates cooldown timers
   - Runs after `MiracleActivationSystem` to update cooldowns after activation

9. **MiracleEffectLifetimeSystem** - Manages effect lifetimes

10. **MiracleSystem** - Legacy system (backward compatibility)

11. **GodSiphonSystem** - Handles divine hand siphoning

## Component Dependencies

### Core Components

- **MiracleConfigState** (Singleton) - Provides access to `MiracleCatalogBlob`
- **MiracleRuntimeStateNew** - Per-caster runtime state (selected ID, activation flags)
- **MiracleChargeState** - Per-caster charge tracking (charge level, held time, tier)
- **MiracleChannelState** - Per-caster channeling state (active effect entity, channel ID)
- **MiracleTargetSolution** - Computed targeting solution (target point, radius, validity)
- **MiracleActivationRequest** (Buffer) - Activation requests consumed by activation system
- **MiracleCooldown** (Buffer) - Per-miracle cooldown tracking

### Tracking Components

- **MiracleRequestCreationState** - Tracks previous activation state for edge detection
- **MiracleChargeTrackingState** - Tracks previous selected ID for charge reset on switch

### Effect Components

- **MiracleSustainedEffect** - Component on sustained effect entities
- **MiracleEffectNew** - Component on instant/throw effect entities

## Data Flow

### Activation Flow

```
Input System (Godgame_MiracleInputBridgeSystem)
  ↓
MiracleRuntimeStateNew (IsActivating, IsSustained, SelectedId)
  ↓
MiracleTargetingSystem
  ↓
MiracleTargetSolution (TargetPoint, Radius, IsValid)
  ↓
MiracleRequestCreationSystem (edge detection: 0→1 transition)
  ↓
MiracleActivationRequest (Id, TargetPoint, TargetRadius, DispenseMode)
  ↓
MiracleActivationSystem (cooldown check, spawn effect)
  ↓
MiracleEffectNew / MiracleSustainedEffect
```

### Charge Flow

```
MiracleRuntimeStateNew (IsActivating, IsSustained)
  ↓
MiracleChargeSystem (updates charge state)
  ↓
MiracleChargeState (Charge01, HeldTime, TierIndex)
  ↓
MiracleTargetingSystem (applies charge to preview radius)
  ↓
MiracleActivationSystem (applies charge to effect intensity/radius)
```

### Sustained Channeling Flow

```
MiracleRuntimeStateNew (IsSustained = 1)
  ↓
MiracleRequestCreationSystem (creates Sustained request)
  ↓
MiracleActivationSystem (spawns MiracleSustainedEffect entity)
  ↓
MiracleChannelState (tracks active effect entity)
  ↓
MiracleSustainedTickSystem (applies effects each tick)
  ↓
MiracleChannelStopSystem (stops when IsSustained = 0)
```

## Integration Points

### Input Integration

- **Godgame_MiracleInputBridgeSystem** - Bridges `MiracleInput` singleton to `MiracleRuntimeStateNew`
- Updates `IsActivating`, `IsSustained`, and `SelectedId` based on input
- Runs in `HandSystemGroup` (different group, cross-group ordering configured at group level)

### Authoring Integration

- **DivineHandAuthoring** - Initializes all miracle components on hand entities
- **MiracleCatalogAuthoring** - Bakes `MiracleCatalogBlob` from ScriptableObject asset
- All components initialized with safe defaults (zeroed states, null entities)

### Presentation Integration

- **MiracleChargeDisplaySystem** - Copies `MiracleChargeState` to `MiracleChargeDisplayData` for UI
- **MiraclePreviewSystem** - Updates `MiraclePreviewData` for visual preview
- Runs in `PresentationSystemGroup` (separate from simulation)

## Component Lifecycle

### Hand Entity Initialization

1. `DivineHandAuthoring` bakes hand entity
2. All miracle components added with default values:
   - `MiracleRuntimeStateNew` (SelectedId=None, IsActivating=0, IsSustained=0)
   - `MiracleChargeState` (all zeros)
   - `MiracleChannelState` (ActiveEffectEntity=Null)
   - `MiracleTargetSolution` (default invalid state)
   - `MiracleRequestCreationState` (PreviousIsActivating=0, PreviousIsSustained=0)
   - `MiracleChargeTrackingState` (PreviousSelectedId=None)

### Activation Lifecycle

1. Input updates `MiracleRuntimeStateNew.IsActivating` or `IsSustained`
2. `MiracleRequestCreationSystem` detects edge (0→1 transition)
3. Creates `MiracleActivationRequest` if target is valid
4. `MiracleActivationSystem` consumes request:
   - Validates cooldown
   - Validates dispense mode compatibility
   - Computes charged radius/intensity
   - Spawns effect entity (`MiracleEffectNew` or `MiracleSustainedEffect`)
   - Updates cooldown
   - Resets charge state

### Sustained Channeling Lifecycle

1. `MiracleActivationSystem` spawns `MiracleSustainedEffect` entity
2. Updates caster's `MiracleChannelState` with effect entity reference
3. `MiracleSustainedTickSystem` applies effects each tick:
   - Updates target point to follow cursor
   - Delegates to game-specific effect systems
4. `MiracleChannelStopSystem` stops channeling when:
   - `IsSustained` becomes 0, OR
   - `SustainedCastHeld` becomes 0
5. Destroys effect entity and resets channel state

## Edge Cases and Gotchas

### Charge Reset

- Charge resets when:
  - Selected miracle becomes `None`
  - Spec not found in catalog
  - Charge model is `None`
  - Selected miracle changes (different ID)
  - Charging signal transitions from true → false
- Charge does NOT reset when:
  - Same miracle remains selected
  - Player switches back to previously selected miracle (resets because ID changed)

### Request Creation

- Requests are only created on edge detection (0→1 transition)
- Prevents duplicate requests every frame
- Sustained requests created when `IsSustained` transitions to true
- Throw requests created when `IsActivating` transitions to true

### Channel State

- Only one sustained channel per caster (checked in activation system)
- Channel state persists after stop (fixed in M3 audit)
- Orphaned effects cleaned up if owner destroyed (fixed in M3 audit)

### Cooldown Management

- Cooldown buffer created on first activation
- Entries added after ECB playback (handles new buffer creation)
- Cooldown validated before activation
- Charges decremented on activation

## System Dependencies

### Required Singletons

- `MiracleConfigState` - Catalog access (required by most systems)
- `TimeState` - Time management (required by activation/cooldown systems)
- `RewindState` - Rewind support (required by activation/cooldown systems)

### Component Dependencies

- `MiracleRuntimeStateNew` - Required for charge, targeting, request creation
- `MiracleTargetSolution` - Required for request creation, activation validation
- `DivineHandInput` - Required for targeting (cursor position)
- `MiracleSlotDefinition` (Buffer) - Optional, for slot-based selection

## Future Considerations

### M4 (Not Yet Implemented)

- Prayer cost enforcement
- Resource depletion checks
- Multiplayer support (PlayerIndex in requests)

### M5 (Not Yet Implemented)

- Resource depletion for sustained channels
- Prayer pool integration
- Cost validation

## Related Documentation

- `Docs/Mechanics/MiracleFramework.md` - High-level framework overview
- `Docs/Archive/TODO/MiraclesFramework_TODO.md` - Historical TODO items
- Component files in `Runtime/Runtime/Miracles/` - Component definitions
- System files in `Runtime/Systems/Miracles/` - System implementations

