# M4 Phase 1 Audit: Throw Token Spawn

**Date:** 2025-01-XX  
**Status:** Implementation Complete, Issues Identified  
**Phase:** M4 Phase 1 - Throw Token Spawn

## Overview

Phase 1 successfully implements throw token spawning for miracles, integrating with existing Divine Hand throw mechanics. Tokens spawn with physics components and launch velocity calculated from aim direction, charge, and spec parameters.

## What Works

1. **Spec Extension**: `MiracleSpec` correctly extended with throw parameters (`ThrowSpeedBase`, `ThrowSpeedChargeMultiplier`, `ThrowArcBoost`, `ThrowCollisionRadius`)
2. **Authoring**: Baker correctly validates and serializes throw parameters with sensible defaults
3. **Token Components**: `MiracleToken` and `MiracleOnImpact` components properly defined
4. **Velocity Calculation**: `ComputeThrowVelocity` correctly applies charge scaling and arc boost
5. **Integration**: Tokens use existing `BeingThrown` + `PhysicsVelocity` pattern

## Critical Issues

### 1. Physics Velocity Overwrite (HIGH PRIORITY)

**Problem:** `PhysicsBodyBootstrapSystem` runs in `InitializationSystemGroup` (before gameplay) and adds `PhysicsVelocity` with zero velocity to entities with `RequiresPhysics` but no `PhysicsCollider`. Our activation system adds `PhysicsVelocity` immediately, but bootstrap will overwrite it on the next frame.

**Impact:** Tokens may spawn with zero velocity instead of calculated launch velocity.

**Location:** `MiracleActivationSystem.cs` lines 335-339

**Solution Options:**
- **Option A (Recommended):** Don't add `PhysicsVelocity` in activation. Let bootstrap add it, then use a deferred system (`MiracleTokenVelocitySystem`) that runs after bootstrap to set velocity.
- **Option B:** Add `PhysicsCollider` ourselves in activation (but this duplicates bootstrap logic).
- **Option C:** Use `PhysicsVelocity` component lookup and update after bootstrap runs (requires system ordering).

**Recommendation:** Implement Option A - create `MiracleTokenVelocitySystem` that runs after `PhysicsBodyBootstrapSystem` to set initial velocity.

### 2. Missing Flight Time Tracking System (HIGH PRIORITY)

**Problem:** `MiracleOnImpact` component has `FlightTime` and `MaxFlightTime` fields, but no system updates them or handles timeout.

**Impact:** Tokens will never timeout, potentially causing memory leaks and orphaned entities.

**Location:** No system exists

**Solution:** Create `MiracleTokenFlightSystem` that:
- Updates `FlightTime` each frame
- Destroys tokens when `FlightTime >= MaxFlightTime`
- Updates `BeingThrown.TimeSinceThrow` for consistency

### 3. Missing Impact Detection System (HIGH PRIORITY)

**Problem:** `MiracleOnImpact.HasImpacted` is never set, and no system handles collision events to trigger impact.

**Impact:** Tokens will never trigger impact effects, defeating the purpose of throw mechanics.

**Location:** No system exists

**Solution:** Create `MiracleTokenImpactSystem` that:
- Listens to physics collision events (via `PhysicsCollisionEventElement` buffer)
- Sets `HasImpacted = 1` on first collision
- Triggers impact effect (spawn `MiracleEffectNew` or call game-specific handler)
- Destroys token entity after impact

### 4. Spawn Position May Be Incorrect (MEDIUM PRIORITY)

**Problem:** Token spawns at `request.TargetPoint`, which is the target location, not the hand/cursor position.

**Impact:** Tokens may spawn at incorrect locations (e.g., far away from player).

**Location:** `MiracleActivationSystem.cs` line 280

**Current Code:**
```csharp
float3 spawnPosition = request.TargetPoint; // Or hand position
```

**Solution:** Use hand/cursor position instead:
```csharp
float3 spawnPosition = hasHandInput ? handInput.CursorWorldPosition : request.TargetPoint;
```

### 5. Physics Gravity Factor (MEDIUM PRIORITY)

**Problem:** Bootstrap sets `PhysicsGravityFactor = 0` (kinematic), but tokens might need gravity for realistic arcs.

**Impact:** Tokens may not follow realistic ballistic trajectories.

**Location:** `PhysicsBodyBootstrapSystem.cs` line 94

**Solution Options:**
- **Option A:** Keep kinematic (no gravity) - tokens use pure velocity, arc comes from initial Y component.
- **Option B:** Make tokens dynamic with gravity - requires changing bootstrap behavior or adding gravity after bootstrap.

**Recommendation:** Option A for now (matches existing throw pattern), but document for future enhancement.

### 6. Arc Calculation Assumes Z-Forward (LOW PRIORITY)

**Problem:** `ComputeThrowVelocity` uses `aimDir.z` assuming Z is forward, but coordinate system may vary.

**Impact:** Arc boost may not work correctly if coordinate system differs.

**Location:** `MiracleActivationSystem.cs` line 446

**Current Code:**
```csharp
float forwardComponent = math.max(0f, aimDir.z); // Assuming Z is forward
```

**Solution:** Verify coordinate system or use `math.length(new float2(aimDir.x, aimDir.z))` for horizontal component.

## Minor Issues

### 7. Missing Default Values Documentation

**Problem:** Default throw parameter values (20 m/s, 1.5x multiplier, 5 m/s arc, 0.5m radius) are only in authoring, not documented.

**Solution:** Add XML comments to `MiracleSpec` fields documenting defaults.

### 8. No Validation for Throw-Only Miracles

**Problem:** If a miracle has `AllowedDispenseModes = Throw` only, but `ThrowSpeedBase = 0`, token will spawn with zero speed.

**Solution:** Add validation in baker or activation system to ensure `ThrowSpeedBase > 0` when throw mode is allowed.

### 9. Owner Entity Validation Missing

**Problem:** `MiracleToken.Owner` is set but never validated. If owner is destroyed, token becomes orphaned.

**Impact:** Orphaned tokens may cause issues in impact detection.

**Solution:** Add owner validation in flight/impact systems (similar to `MiracleSustainedTickSystem`).

## Integration Points

### Working Correctly

1. **Physics Bootstrap**: `RequiresPhysics` + `PhysicsInteractionConfig` correctly trigger bootstrap
2. **BeingThrown**: Component added correctly for flight tracking
3. **Charge Integration**: Velocity correctly uses `normalizedCharge` from `MiracleChargeState`
4. **Spec Lookup**: `MiracleSpec` correctly read from catalog

### Needs Verification

1. **System Ordering**: Verify `MiracleActivationSystem` runs before physics bootstrap (it does - activation is in GameplaySystemGroup, bootstrap is in InitializationSystemGroup)
2. **Collision Events**: Verify `RaisesCollisionEvents` flag actually triggers event buffers (needs testing)
3. **Coordinate System**: Verify `AimDirection` coordinate system matches arc calculation assumptions

## Testing Checklist

- [ ] Tokens spawn with correct position (hand/cursor, not target)
- [ ] Tokens have correct launch velocity (not zero)
- [ ] Tokens follow ballistic trajectory (with/without gravity)
- [ ] Tokens timeout after `MaxFlightTime`
- [ ] Tokens trigger impact on collision
- [ ] Charge scaling affects throw speed correctly
- [ ] Arc boost works correctly for different aim angles
- [ ] Multiple tokens can exist simultaneously
- [ ] Owner destruction doesn't cause crashes
- [ ] Physics bootstrap doesn't overwrite velocity

## Recommendations for Phase 2

Before proceeding to Phase 2 (Impact Effects), fix:

1. **CRITICAL:** Implement `MiracleTokenVelocitySystem` to handle velocity after bootstrap
2. **CRITICAL:** Implement `MiracleTokenFlightSystem` for flight time tracking and timeout
3. **CRITICAL:** Implement `MiracleTokenImpactSystem` for collision detection and impact triggering
4. **MEDIUM:** Fix spawn position to use hand/cursor instead of target point
5. **LOW:** Verify coordinate system assumptions

## Files Modified

- `Runtime/Miracles/MiracleCatalogComponents.cs` - Added throw parameters
- `Authoring/Miracles/MiracleCatalogAuthoring.cs` - Added authoring fields and baker logic
- `Runtime/Miracles/MiracleTokenComponents.cs` - Created token components
- `Systems/Miracles/MiracleActivationSystem.cs` - Added token spawn logic

## Files Needed (Not Created)

- `Systems/Miracles/MiracleTokenVelocitySystem.cs` - Set velocity after bootstrap
- `Systems/Miracles/MiracleTokenFlightSystem.cs` - Track flight time and handle timeout
- `Systems/Miracles/MiracleTokenImpactSystem.cs` - Handle collision events and trigger impact

