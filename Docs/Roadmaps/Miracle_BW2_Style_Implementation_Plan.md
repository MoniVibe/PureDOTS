# Miracle System: Black & White 2 Style Implementation Plan

**Status:** Planning  
**Category:** Core Gameplay / Systems  
**Scope:** PureDOTS Framework + Godgame Integration  
**Created:** 2025-01-21

---

## Overview

Implementation plan to achieve Black & White 2–style miracles (feel + UX + systemic behavior) using the existing Catalog → ActivationRequest → Effect entity architecture.

**Target Behavior:** Quick selection, clear preview, hold-to-charge, release-to-throw, sustained channeling, cancel gesture, and worship-site shortcuts—implemented as generic systems, not scripted miracle one-offs.

---

## Target Behavior Spec (The "B&W2 Feel")

### Casting Loop
1. **Select miracle** (hotbar / radial menu)
2. **Aim** (world reticle + radius preview + valid/invalid feedback)
3. **Choose dispense mode**
   - **Throw**: spawn a token/projectile and launch it (arc preview optional)
   - **Sustained**: channel an area effect that persists while held (drains resource / locks input state)
4. **Charge / tiering**: Hold to increase tier/intensity (size, duration, strength)
5. **Cancel**: "Shake to cancel" gesture (fast mouse deltas) + right-click cancel fallback
6. **Shortcuts**: Worship site context cast or bound slots (nearby cast boost / reduced cost / faster access)

---

## Core Architecture (Existing)

**Already Implemented:**
- ✅ `MiracleCatalog` (BlobAsset), specs, authoring
- ✅ `MiracleActivationRequest` buffer → `MiracleActivationSystem`
- ✅ `MiracleCooldown` buffer + updater
- ✅ Basic effect spawning for a few miracles
- ✅ Input bridge + caster state

**Main Missing Pieces:**
- Targeting/preview
- Sustained tick loop
- Throw token flight & impact
- Cost enforcement
- Delivery variants

---

## Milestones (PR-Sized), in Dependency Order

### M0 — Lock the Miracle Runtime Contract

**Goal:** Every miracle, regardless of type, goes through the same runtime envelope.

**Tasks:**
1. Add/confirm data types:
   - `MiracleDeliveryType` (enum): `Instant`, `Projectile`, `SustainedArea`, `Beacon`, `Chain`
   - `MiracleTargetingMode` (enum): `Point`, `Area`, `Entity`, `GroundOnly`, `FriendlyOnly`, `EnemyOnly`
   - `MiracleChargeModel` (enum): `None`, `HoldToTier`, `HoldToContinuous`

2. Extend `MiracleSpec` to include:
   - `DeliveryType`
   - `ChargeModel`, `TierCount` and tier parameters (or curves)
   - `BaseDuration`, `BaseRadius`, `BaseStrength`
   - `CostUpfront`, `CostPerSecond` (even if not enforced immediately)

**Acceptance Criteria:**
- Catalog can express "Fireball projectile", "Rain sustained area", "Heal instant/area"
- No game-specific logic in the spec types (PureDOTS-only)
- All existing miracles migrate to new spec structure

**Files to Modify:**
- `Runtime/Miracles/MiracleCatalogComponents.cs` - Add enums and extend `MiracleSpec`
- `Authoring/Miracles/MiracleCatalogAuthoring.cs` - Update authoring UI
- Update existing miracle catalog assets

**Estimated Effort:** 1 PR

---

### M1 — Unified Targeting + Preview Pipeline

**Goal:** B&W2-like aiming feedback independent of specific miracles.

**Systems to Create:**
1. **`MiracleTargetingSystem`**
   - Reads `MiracleCasterState` + current selection + mouse ray
   - Computes `MiracleTargetSolution` (point/entity, radius, validity flags, reason codes)
   - Deterministic tie-breakers (distance, then stable id)

2. **`MiraclePreviewSystem`** (presentation hook)
   - Consumes `MiracleTargetSolution`
   - Shows reticle, radius ring, arc preview (optional later)

**Key Rule:**
- Activation is only allowed when `MiracleTargetSolution.IsValid`

**Components:**
```csharp
public struct MiracleTargetSolution : IComponentData
{
    public float3 TargetPoint;
    public Entity TargetEntity; // If entity-targeting
    public float Radius;
    public byte IsValid;
    public byte ValidityReason; // Enum: None, OutOfRange, InvalidTarget, InsufficientResource, etc.
    public float3 PreviewArcStart; // For throw preview
    public float3 PreviewArcEnd;
}
```

**Acceptance Criteria:**
- Every miracle uses the same targeting evaluation (only spec changes behavior)
- You can swap selected miracle and preview updates instantly
- Invalid states show clear feedback (red reticle, reason text)
- Deterministic targeting (same input = same target)

**Files to Create:**
- `Runtime/Miracles/MiracleTargetingComponents.cs`
- `Systems/Miracles/MiracleTargetingSystem.cs`
- `Systems/Miracles/MiraclePreviewSystem.cs` (presentation layer)

**Estimated Effort:** 1-2 PRs

---

### M2 — Charge/Tier Mechanics

**Goal:** Holding the cast button produces a meaningful "power-up" feel.

**Components to Add:**
```csharp
public struct MiracleChargeState : IComponentData
{
    public float Charge01; // 0-1 normalized charge
    public float HeldTime; // Seconds held
    public byte TierIndex; // Current tier (0 = uncharged, 1-N = tiers)
    public byte IsCharging; // 0/1 flag
}
```

**Behavior:**
- `HoldToTier`: tier steps at thresholds (Tier 1/2/3…)
- `HoldToContinuous`: continuous scaling (radius/strength)
- Charge affects at least radius + strength in spawned runtime effect
- Cancel resets charge cleanly

**Systems:**
- `MiracleChargeSystem` - Updates charge state based on input hold
- Charge curve handling (can be spec-driven: linear, ease-in, etc.)

**Acceptance Criteria:**
- Charge affects at least radius + strength in spawned runtime effect
- Cancel resets charge cleanly
- Visual feedback shows charge progress (UI/presentation)
- Charge time scales appropriately per miracle tier

**Files to Create:**
- `Runtime/Miracles/MiracleChargeComponents.cs`
- `Systems/Miracles/MiracleChargeSystem.cs`

**Files to Modify:**
- `MiracleActivationSystem` - Apply charge to spawned effects
- Input bridge - Track hold state

**Estimated Effort:** 1 PR

---

### M3 — Sustained Miracles: Real Channeling Loop

**Goal:** Sustained miracles actually apply continuously and can be stopped/canceled.

**Runtime Entity Model:**
When activated in sustained mode, spawn:
- `MiracleEffect` entity with:
  - `Owner`, `MiracleId`, `TargetPoint`/`Radius`
  - `Intensity` (from charge), `RemainingDuration` (optional), `IsChanneling`
- Optionally a separate `MiracleEmitter` entity anchored to target for clean updates

**Systems:**
1. **`MiracleSustainedTickSystem`**
   - Applies effect each tick (rain spawns wetness, heal pulses, veil applies modifiers, etc.)
   - Respects `LocalTimeScale` (if time bubbles exist)
   - Queries targets within radius each tick

2. **`MiracleChannelStopSystem`**
   - Stops on input release, cancel gesture, or resource depletion
   - Cleans up effect entities

**Integration:**
- `MiracleActivationSystem` spawns sustained effect when `DispenseMode == Sustained`
- `MiracleCasterState.IsChanneling` tracks active channel
- Input bridge detects release → triggers stop

**Acceptance Criteria:**
- Holding the cast button sustains the effect; releasing stops it immediately
- Time scaling affects sustained progression correctly
- Resource depletion stops channeling gracefully
- Multiple sustained miracles can run simultaneously (if allowed by design)

**Files to Create:**
- `Runtime/Miracles/MiracleSustainedComponents.cs`
- `Systems/Miracles/MiracleSustainedTickSystem.cs`
- `Systems/Miracles/MiracleChannelStopSystem.cs`

**Files to Modify:**
- `MiracleActivationSystem` - Handle sustained spawn differently
- `Godgame_MiracleInputBridgeSystem` - Detect release for stop

**Estimated Effort:** 1-2 PRs

---

### M4 — Throw Miracles: Token Flight + Impact

**Goal:** "Throwable miracle" feels like B&W2: charge, arc, impact, explosion/area effect.

**Token Model:**
On activation in throw mode:
- Spawn a `MiracleToken` entity with:
  - `Owner`, `MiracleId`, `Intensity`, `Radius`, etc.
  - `PhysicsVelocity` initial from aim + charge
  - `MiracleOnImpact` component

**Systems:**
1. **`MiracleTokenSpawnSystem`** (from `ActivationRequest`)
   - Creates token entity with physics
   - Applies initial velocity from charge + aim direction

2. **`MiracleTokenFlightSystem`** (mostly physics)
   - Updates token position/rotation
   - Optional: arc preview during flight

3. **`MiracleImpactSystem`**
   - On collision, despawn token and spawn a `MiracleEffect` (often `Instant`/`AreaExplosion`)
   - Determines impact point and normal

**Integration Choice:**
- If Divine Hand throw pipeline is authoritative, feed throw tokens through the same "throw verb" rather than duplicating throw physics logic

**Components:**
```csharp
public struct MiracleToken : IComponentData
{
    public MiracleId Id;
    public Entity Owner;
    public float Intensity;
    public float Radius;
    public float3 LaunchVelocity;
}

public struct MiracleOnImpact : IComponentData
{
    public float ExplosionRadius;
    public byte HasImpacted; // Set to 1 on collision
}
```

**Acceptance Criteria:**
- Throw casting works end-to-end: token spawns, moves, impacts, creates effect
- "Nothing throws" gap is gone for miracles
- Arc preview shows trajectory (optional but recommended)
- Impact detection is reliable (collision or ground check)

**Files to Create:**
- `Runtime/Miracles/MiracleTokenComponents.cs`
- `Systems/Miracles/MiracleTokenSpawnSystem.cs`
- `Systems/Miracles/MiracleTokenFlightSystem.cs`
- `Systems/Miracles/MiracleImpactSystem.cs`

**Files to Modify:**
- `MiracleActivationSystem` - Spawn token instead of effect for throw mode
- Consider integration with `DivineHandSystem` throw mechanics

**Estimated Effort:** 2 PRs

---

### M5 — Cost Model: Prayer/Mana

**Goal:** Miracles are resource-gated like B&W2, not only cooldown-gated.

**Minimal Viable Resource Loop:**

**Components:**
```csharp
public struct PrayerPool : IComponentData
{
    public float Current;
    public float Max;
    public float RegenPerSecond;
}

public struct PrayerIncomeState : IComponentData
{
    public float IncomePerSecond; // From worship sites, etc.
}
```

**Systems:**
1. **`PrayerIncomeSystem`** (stub)
   - Simple regen or "worship generates income"
   - Updates `PrayerPool.Current` over time

2. **`MiracleCostSystem`**
   - Upfront cost on activation
   - Per-second drain for sustained channeling
   - If insufficient: prevent activation or stop channel

**Integration:**
- `MiracleActivationSystem` checks cost before spawning
- `MiracleSustainedTickSystem` drains per tick
- `MiracleChannelStopSystem` stops on depletion

**Acceptance Criteria:**
- Sustained miracles drain over time; they shut off when depleted
- Throw/instant miracles fail gracefully if insufficient
- Cost display in UI (preview shows cost, insufficient = red)
- Prayer regen visible/audible feedback

**Files to Create:**
- `Runtime/Miracles/PrayerComponents.cs`
- `Systems/Miracles/PrayerIncomeSystem.cs`
- `Systems/Miracles/MiracleCostSystem.cs`

**Files to Modify:**
- `MiracleActivationSystem` - Validate cost before activation
- `MiracleSustainedTickSystem` - Drain per tick
- `MiracleTargetingSystem` - Include cost in validity check

**Estimated Effort:** 1-2 PRs

---

### M6 — Worship-Site Shortcuts + Binding

**Goal:** B&W2-like "cast from worship context" without special-casing miracles.

**Data:**
```csharp
public struct WorshipSite : IComponentData
{
    public float InfluenceRadius;
    public float CostDiscount; // 0-1 multiplier
    public float ChargeSpeedBonus; // 0-1 multiplier
    public float RadiusBonus; // 0-1 multiplier
}

[InternalBufferCapacity(4)]
public struct WorshipSiteBindings : IBufferElementData
{
    public byte SlotIndex;
    public MiracleId BoundMiracleId;
}
```

**Systems:**
1. **`WorshipSiteDetectionSystem`**
   - Finds nearest active site to player hand
   - Updates `MiracleCasterState` with active site reference

2. **`WorshipShortcutCastSystem`**
   - Hotkeys / radial from site
   - Applies cost modifiers as pure data modifiers at activation time

**Integration:**
- `MiracleTargetingSystem` checks for nearby worship site
- `MiracleCostSystem` applies discounts from site
- UI shows worship site shortcuts when nearby

**Acceptance Criteria:**
- Standing near a worship site enables quick casting/binding
- Works for any miracle via the same activation flow
- Cost modifiers applied transparently (no special cases)
- Binding UI allows player to assign miracles to slots

**Files to Create:**
- `Runtime/Miracles/WorshipSiteComponents.cs`
- `Systems/Miracles/WorshipSiteDetectionSystem.cs`
- `Systems/Miracles/WorshipShortcutCastSystem.cs`

**Files to Modify:**
- `MiracleCostSystem` - Apply site discounts
- `MiracleChargeSystem` - Apply charge speed bonus
- UI systems - Show worship site shortcuts

**Estimated Effort:** 1-2 PRs

---

### M7 — Delivery Variants (Beacon / Chain / Explosion)

**Goal:** Implement "multiple delivery methods" generically so new miracles don't need bespoke pipelines.

**Generic Pattern:**
- `MiracleDeliveryType` drives which runtime entity graph spawns
- **Beacon**: spawns an anchor entity that periodically triggers sub-effects
- **Chain**: on impact/tick, finds next valid targets in radius (deterministic ordering)
- **Explosion**: an instant area pulse at target point

**Components:**
```csharp
public struct MiracleBeacon : IComponentData
{
    public Entity AnchorEntity;
    public float TickInterval;
    public float RemainingDuration;
    public int MaxTicks;
}

public struct MiracleChainState : IComponentData
{
    public int MaxJumps;
    public int JumpsRemaining;
    public Entity LastTarget;
    public float ChainRange;
}
```

**Systems:**
- `MiracleBeaconSystem` - Periodic effect application from anchor
- `MiracleChainSystem` - Finds next target, applies effect, decrements jumps
- `MiracleExplosionSystem` - Instant radial effect (can reuse existing area logic)

**Acceptance Criteria:**
- A single miracle can support multiple dispense modes and delivery types via spec flags
- Beacon effects follow anchor entity movement
- Chain targeting is deterministic (same targets always hit in same order)
- Explosion effects are instant and radial

**Files to Create:**
- `Runtime/Miracles/MiracleDeliveryComponents.cs`
- `Systems/Miracles/MiracleBeaconSystem.cs`
- `Systems/Miracles/MiracleChainSystem.cs`
- `Systems/Miracles/MiracleExplosionSystem.cs`

**Files to Modify:**
- `MiracleActivationSystem` - Spawn appropriate delivery entity based on `DeliveryType`

**Estimated Effort:** 2-3 PRs (optional but matches design doc)

---

### M8 — UX Polish: Radial Menu + Shake-to-Cancel + Audio Hooks

**Goal:** Perceived B&W2 quality.

**Shake-to-Cancel:**
- Implement in input bridge: detect rapid alternating mouse deltas beyond threshold within N ms → set `CancelTriggered`
- Threshold: 3+ direction changes within 0.5 seconds
- Visual feedback: UI shake/vibration, red X flash, cancel sound

**Radial Menu:**
- Pure UI, but must output only: `SelectedMiracleId`, `DispenseMode`, and optionally `QueuedCast`
- No gameplay logic inside UI
- Middle mouse button or hold key + mouse move activation

**Audio Hooks:**
- Charge sound (pitch scales with charge)
- Cast sound (varies by miracle type)
- Impact sound (for throw miracles)
- Cancel sound (whoosh)

**Acceptance Criteria:**
- Cancel works reliably and never causes double-cast
- Radial selection has no gameplay logic inside UI
- Audio feedback is clear and responsive
- Shake detection doesn't false-positive during normal play

**Files to Create:**
- `Systems/Miracles/MiracleShakeDetectionSystem.cs` (or integrate into input bridge)
- UI systems for radial menu (presentation layer)

**Files to Modify:**
- `Godgame_MiracleInputBridgeSystem` - Add shake detection
- Presentation systems - Add audio hooks

**Estimated Effort:** 1 PR (optional)

---

## Testing Plan

**Must-do if you want "no hardcoding":**

### ScenarioRunner / PlayMode Tests

1. **Sustained Channel Test**
   - Hold for N ticks → effect applied each tick → release stops → total applied equals expected within tolerance

2. **Throw Token Test**
   - Cast throw → token spawns → impacts → effect spawns at impact → cooldown set

3. **Cost Enforcement Test**
   - Insufficient prayer prevents activation; sustained stops when drained

4. **Determinism Replay**
   - Record per-tick input + target solution + activation requests; replay produces identical outcomes

5. **Charge/Tier Test**
   - Hold for X seconds → verify tier progression → verify effect scales correctly

6. **Worship Site Test**
   - Stand near site → verify shortcuts available → verify cost discounts applied

**Test Files to Create:**
- `Tests/Playmode/MiracleSustainedChannelTests.cs`
- `Tests/Playmode/MiracleThrowTokenTests.cs`
- `Tests/Playmode/MiracleCostEnforcementTests.cs`
- `Tests/Playmode/MiracleDeterminismTests.cs`

---

## Implementation Notes (Avoid Common Traps)

1. **Keep miracle-specific logic behind "effect systems"** keyed by `MiracleId`, but keep casting/delivery/targeting 100% generic.

2. **Never let UI directly spawn effects;** UI only changes selection/intent.

3. **Ensure all targeting tie-breakers are deterministic** (distance, then stable id).

4. **Make Sustained and Throw share the same upstream pipeline;** they only diverge at delivery spawn.

5. **Namespace hygiene:** All new components in `PureDOTS.Runtime.Miracles` namespace (PureDOTS framework) or `Godgame.Miracles` (game-specific).

6. **Time-aware:** All systems must respect `TimeState` and `RewindState` for determinism.

7. **Burst-compatible:** Keep hot paths burst-compiled; only presentation systems can use managed code.

8. **Component data only:** No managed references in miracle components (use `Entity` references, `FixedString` for text).

---

## Dependencies Between Milestones

```
M0 (Runtime Contract)
  ↓
M1 (Targeting/Preview) ──┐
  ↓                       │
M2 (Charge/Tier) ────────┤
  ↓                       │
M3 (Sustained) ──────────┼──→ All depend on M0
  ↓                       │
M4 (Throw) ──────────────┤
  ↓                       │
M5 (Cost) ───────────────┤
  ↓                       │
M6 (Worship Sites) ───────┘
  ↓
M7 (Delivery Variants) [Optional]
  ↓
M8 (UX Polish) [Optional]
```

---

## Success Metrics

**Functional:**
- All 4 MVP miracles (Rain, TemporalVeil, Fire, Heal) support both Sustained and Throw modes
- Targeting preview works for all miracles
- Cost system prevents spam without feeling restrictive
- Worship sites provide meaningful shortcuts

**Performance:**
- Sustained miracles tick at 60fps without frame drops
- Throw token physics doesn't impact frame time
- Targeting queries are efficient (spatial hashing)

**Quality:**
- Cancel gesture feels responsive (<100ms detection)
- Charge feedback is clear and satisfying
- Throw arc preview is accurate

---

## Related Documentation

- `Docs/Concepts/Miracles/Miracle_System_Vision.md` - Design vision
- `Docs/Concepts/UI_UX/Miracle_UI_System.md` - UI design
- `Docs/Mechanics/MiraclesAndAbilities.md` - Framework overview
- `Docs/Mechanics/MiracleFramework.md` - Command framework

---

**Last Updated:** 2025-01-21  
**Owner:** Systems Team



