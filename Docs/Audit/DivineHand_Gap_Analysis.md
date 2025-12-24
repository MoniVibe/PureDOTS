# Divine Hand Capability Gap Analysis

**Status:** Complete Analysis  
**Date:** 2025-01-21  
**Scope:** Pickup, Throw (immediate/queued/slingshot), Siphon, Dump, Sustained Construction Feed

---

## Executive Summary

This document assesses the current state of divine hand interaction systems against required capabilities. The analysis covers:

- **Pickup & Throw**: Immediate and queued throws work; slingshot charge exists but release does not re-use the held entity’s impulse yet
- **Siphon**: Component structure exists, systems partially implemented
- **Dump**: Component structure exists, systems partially implemented  
- **Sustained Dump to Construction**: Not implemented

**Key Finding:** Core pickup/throw mechanics are functional but incomplete. Siphon/dump have component foundations but need system completion. Construction yard sustained feed is missing entirely. Slingshot must launch the currently held entity or miracle (no new projectile spawn), but today release never applies a charged impulse to that entity.

---

## 1. Inventory of Current Systems

### 1.1 PureDOTS Core Components

**Location:** `puredots/Packages/com.moni.puredots/Runtime/Runtime/Interaction/`

#### Pickup Components (`PickupThrowComponents.cs`)
- ✅ `Pickable` - Tag marking entities as pickable
- ✅ `HeldByPlayer` - Tracks held entity (Holder, LocalOffset, HoldStartPosition, HoldStartTime)
- ✅ `MovementSuppressed` - Suppresses movement while held
- ✅ `BeingThrown` - Marks entity in flight (InitialVelocity, TimeSinceThrow)
- ✅ `PickupState` - State machine (Empty, AboutToPick, Holding, PrimedToThrow, Queued)
- ✅ `ThrowCharge` - Optional slingshot charge data (Charge, MaxCharge, ChargeRate, IsCharging)
- ✅ `ThrowQueue` - Buffer element for throw queue
- ✅ `ThrowQueueEntry` - Queue entry (Target, Direction, Force)

**Burst Compliance:** ✅ All components are `IComponentData` or `IBufferElementData` (Burst-friendly)

#### Hand Components (`DivineHandComponents.cs`)
- ✅ `DivineHandTag` - Tag for divine hand entity
- ✅ `HandHeldTag` - Tag for held entities
- ✅ `DivineHandState` - Hand state (CursorPosition, HeldEntity, HeldResourceTypeIndex, HeldAmount, ThrowModeEnabled, etc.)
- ✅ `PickableTag` - Tag marking pickable entities

**Burst Compliance:** ✅ All components are Burst-friendly

#### Hand Interaction Components (`HandInteractionComponents.cs`)
- ✅ `HandInteractionState` - Shared interaction state (HandState, ActiveCommand, HeldAmount, Flags)
- ✅ `ResourceSiphonState` - Siphon state (HandEntity, TargetEntity, ResourceTypeIndex, SiphonRate, DumpRate, AccumulatedUnits, Flags)
- ✅ `DivineHandCommand` - Command component (Type, TargetEntity, TargetPosition, TimeSinceIssued)
- ✅ `DivineHandCommandType` enum - None, Grab, Drop, Siphon, Dump, Miracle, Cancel

**Burst Compliance:** ✅ All components are Burst-friendly

### 1.2 Godgame-Specific Components

**Location:** `godgame/Assets/Scripts/Godgame/`

#### Runtime Components (`Runtime/DivineHandComponents.cs`)
- ✅ `HandState` enum - Empty, Holding, Dragging, SlingshotAim, Dumping
- ✅ `DivineHandConfig` - Configuration (PickupRadius, MaxGrabDistance, HoldLerp, ThrowImpulse, MinChargeSeconds, MaxChargeSeconds, etc.)
- ✅ `DivineHandState` - Extended state (HeldEntity, CursorPosition, AimDirection, ChargeTimer, CooldownTimer, HeldResourceTypeIndex, HeldAmount)
- ✅ `HandQueuedThrowElement` - Buffer element for queued throws (Entity, Direction, Impulse)

**Burst Compliance:** ✅ All components are Burst-friendly

#### Interaction Components (`Runtime/Interaction/GodgameGodHandTag.cs`)
- ✅ `GodgameGodHandTag` - Tag for god hand entity
- ✅ `GodgameGodHandBootstrapSystem` - Ensures singleton god hand exists with required components

**Burst Compliance:** ✅ System is `[BurstCompile]`

### 1.3 PureDOTS Core Systems

**Location:** `puredots/Packages/com.moni.puredots/Runtime/Systems/`

#### Hand Systems (`Hand/GodHandThrowSystem.cs`)
- ⚠️ `GodHandThrowSystem` - Processes throw mode commands (toggle, queue, launch)
  - **Status:** Placeholder implementation (TODO comments for physics launch)
  - **Burst Compliance:** ❌ Not Burst-compiled (uses `GodHandCommandStreamSingleton`, non-Burst input)
  - **Functionality:** Queue management works, but launch is incomplete

#### Miracle Systems (`Miracles/GodSiphonSystem.cs`)
- ✅ `GodSiphonSystem` - Handles siphon miracle casting
  - **Status:** Functional for miracle system, not hand interaction
  - **Burst Compliance:** ✅ `[BurstCompile]`
  - **Functionality:** Siphons from ResourceDeposit, ResourceStack, VillageResources, PlatformResources

### 1.4 Godgame Interaction Systems

**Location:** `godgame/Assets/Scripts/Godgame/Systems/Interaction/`

#### Pickup System (`GodgamePickupSystem.cs`)
- ✅ `GodgamePickupSystem` - Handles pickup using RMB input and Unity.Physics raycasts
  - **Status:** Functional
  - **Burst Compliance:** ⚠️ Partially Burst (core logic Burst, input reading `[BurstDiscard]`)
  - **Functionality:** 
    - Empty → AboutToPick → Holding state transitions
    - Cursor movement threshold detection (3px)
    - Adds `HeldByPlayer` and `MovementSuppressed` components
    - Zeroes physics velocity on pickup

#### Held Follow System (`GodgameHeldFollowSystem.cs`)
- ✅ `GodgameHeldFollowSystem` - Updates held entity positions to follow hand/camera
  - **Status:** Functional
  - **Burst Compliance:** ✅ `[BurstCompile]`
  - **Functionality:**
    - Follows holder transform (camera or god hand entity)
    - Tracks movement for throw priming
    - Accumulates velocity for throw calculation
    - Zeroes physics velocity while held

#### Throw System (`GodgameThrowSystem.cs`)
- ✅ `GodgameThrowSystem` - Handles throw/drop mechanics
  - **Status:** Functional
  - **Burst Compliance:** ⚠️ Partially Burst (core logic Burst, input reading `[BurstDiscard]`)
  - **Functionality:**
    - 3-second settle timer (hold RMB for 3s to place on terrain)
    - Movement detection (Holding → PrimedToThrow)
    - RMB release handling (drop vs throw vs queue)
    - Shift+RMB = queue throw
    - Normal RMB release = immediate throw
    - Calculates throw direction from accumulated velocity or camera forward
    - Calculates throw force (BaseThrowForce + velocity magnitude * 0.5f)
    - Applies physics velocity and `BeingThrown` component

#### Throw Queue System (`GodgameThrowQueueSystem.cs`)
- ✅ `GodgameThrowQueueSystem` - Handles throw queue release
  - **Status:** Functional
  - **Burst Compliance:** ⚠️ Partially Burst (core logic Burst, input reading `[BurstDiscard]`)
  - **Functionality:**
    - Hotkey 1 = release one throw (FIFO)
    - Hotkey 2 = release all throws
    - Applies throw velocity from queue entries
    - Adds `BeingThrown` component

### 1.5 Divine Hand Systems (Godgame)

**Location:** `puredots/Assets/Projects/Godgame/Scripts/Godgame/Systems/DivineHandSystems.cs`

- ✅ `DivineHandSystem` - Main hand state machine system
  - **Status:** Functional but incomplete
  - **Burst Compliance:** ✅ `[BurstCompile]`
  - **Functionality:**
    - Handles grab (pickup) via `Intent.StartSelect`
    - Handles siphon command (`DivineHandCommandType.SiphonPile`)
    - Handles dump command (`DivineHandCommandType.DumpToStorehouse`, `DivineHandCommandType.GroundDrip`)
    - Maintains held entity transform
    - Handles release (throw/drop)
    - Handles queued throw via `TryQueueHeldEntity`
    - **Missing:** Slingshot projectile spawning (charge accumulation works, but no throw happens)

### 1.6 Input Handlers (MonoBehaviour Bridge)

**Location:** `puredots/Packages/com.moni.puredots/Runtime/Input/`

- ✅ `StorehouseDumpRmbHandler` - MonoBehaviour handler for storehouse dump
  - **Status:** Functional (raises events)
  - **Burst Compliance:** ❌ MonoBehaviour (not Burst)
  - **Functionality:** Checks if cursor over storehouse, raises RMB events

- ✅ `PileSiphonRmbHandler` - MonoBehaviour handler for pile siphon
  - **Status:** Functional (raises events)
  - **Burst Compliance:** ❌ MonoBehaviour (not Burst)
  - **Functionality:** Checks if cursor over pile, raises RMB events

### 1.7 Storehouse Systems

**Location:** `puredots/Packages/com.moni.puredots/Runtime/Systems/`

- ✅ `StorehouseInventorySystem` - Aggregates inventory state
- ✅ `StorehouseDepositProcessingSystem` - Handles deposits
- ✅ `StorehouseWithdrawalProcessingSystem` - Handles withdrawals
- ✅ `StorehouseRegistrySystem` - Maintains registry for queries
- ✅ `StorehouseApi.TryDeposit()` - API helper for deposits
- ✅ `StorehouseApi.TryWithdraw()` - API helper for withdrawals

**Burst Compliance:** ✅ All systems are `[BurstCompile]`

### 1.8 Construction Systems

**Location:** `godgame/Assets/Scripts/Godgame/Construction/`

- ✅ `ConstructionSystem` - Handles construction ghost resource payment
  - **Status:** Functional (villagers pay via tickets)
  - **Burst Compliance:** ✅ `[BurstCompile]`
  - **Functionality:** Withdraws from storehouse, tracks Paid vs Cost, converts to built entity when complete

- ✅ `ConstructionGhost` - Component tracking resource costs and payment progress
  - **Status:** Functional
  - **Burst Compliance:** ✅ Burst-friendly

**Missing:** No direct hand-to-construction intake system (hand dumps to storehouse, villagers withdraw from storehouse)

---

## 2. Requirements Mapping

### 2.1 Pickup Objects

**Requirement:** Hand can pick up pickable objects (resources, rocks, villagers, etc.)

**Components:** ✅
- `Pickable` component exists
- `PickupState` state machine exists
- `HeldByPlayer` component exists

**Systems:** ✅
- `GodgamePickupSystem` - Functional
- `GodgameHeldFollowSystem` - Functional

**Data-Driven Entity Support:** ✅
- Works with any entity that has `Pickable` component
- No hardcoded entity types

**Status:** ✅ **COMPLETE**

---

### 2.2 Immediate Throw

**Requirement:** Hand can throw held objects immediately on RMB release

**Components:** ✅
- `BeingThrown` component exists
- `PickupState` tracks throw state

**Systems:** ✅
- `GodgameThrowSystem` - Functional
  - Handles RMB release
  - Calculates throw direction and force
  - Applies physics velocity

**Data-Driven Entity Support:** ✅
- Works with any held entity
- Uses Unity Physics velocity component

**Status:** ✅ **COMPLETE**

---

### 2.3 Queued Throw

**Requirement:** Hand can queue throws (Shift+RMB release) and release them later via hotkeys

**Components:** ✅
- `ThrowQueue` buffer exists
- `ThrowQueueEntry` exists

**Systems:** ✅
- `GodgameThrowSystem.HandleQueueThrow()` - Functional
- `GodgameThrowQueueSystem` - Functional
  - Hotkey 1 = release one
  - Hotkey 2 = release all

**Data-Driven Entity Support:** ✅
- Works with any held entity
- Queue stores entity reference, direction, force

**Status:** ✅ **COMPLETE**

---

### 2.4 Slingshot Throw

**Requirement:** Hand can charge a slingshot-style throw (hold RMB while holding, aim, release) and re-launch the currently held entity or miracle token with charge-scaled impulse.

**Components:** ⚠️
- `ThrowCharge` component exists (optional)
- `DivineHandState.ChargeTimer` exists
- `DivineHandConfig.MinChargeSeconds`, `MaxChargeSeconds` exist

**Systems:** ⚠️
- `DivineHandSystem` - Charge accumulation implemented (lines 150-153)
- **Missing:** Charged impulse application to held entity on release
- **Missing:** Visual feedback (rubber band, trajectory preview)
- **Missing:** Speed calculation from charge time

**Data-Driven Entity Support:** ⚠️
- Charge system works generically
- But no re-launch happens (just state transition)

**Status:** ⚠️ **PARTIALLY COMPLETE** (charge works, throw doesn't happen)

**Gap:** Slingshot release never applies a charge-scaled impulse to the held entity/miracle. State machine transitions but velocity is not updated.

---

### 2.5 Queue Slingshot Throw

**Requirement:** Hand can queue slingshot throws (charge, then Shift+RMB to queue instead of immediate release)

**Components:** ⚠️
- `ThrowQueue` buffer exists
- But queue entries don't store charge level

**Systems:** ❌
- No system handles slingshot queue
- `GodgameThrowSystem.HandleQueueThrow()` doesn't use charge timer
- `GodgameThrowQueueSystem` doesn't apply charge-based force

**Data-Driven Entity Support:** ❌
- Not implemented

**Status:** ❌ **NOT IMPLEMENTED**

**Gap:** No way to queue a charged slingshot throw. Queue system exists but doesn't integrate with charge system.

---

### 2.6 Siphon Resources from Piles

**Requirement:** Hand can siphon resources from aggregate piles (RMB hold over pile)

**Components:** ✅
- `ResourceSiphonState` component exists
- `DivineHandCommandType.Siphon` exists
- `HandInteractionState.FlagSiphoning` exists

**Systems:** ⚠️
- `DivineHandSystem` - Handles `DivineHandCommandType.SiphonPile` (lines 201-223)
  - Accumulates units over time based on `SiphonRate`
  - Adds to `HeldAmount` up to `HeldCapacity`
  - **Missing:** Actual pile interaction (no pile component query)
  - **Missing:** Pile amount deduction
- `PileSiphonRmbHandler` - Raises events (MonoBehaviour bridge)
- **Missing:** System that processes siphon command and deducts from pile

**Data-Driven Entity Support:** ⚠️
- Command structure exists
- But no pile component integration

**Status:** ⚠️ **PARTIALLY COMPLETE** (command structure works, pile interaction missing)

**Gap:** No system queries pile entities and deducts resources. `DivineHandSystem` accumulates units but doesn't interact with pile components.

**Required Pile Components:**
- Need to identify pile component structure (likely `AggregatePile` or `ResourcePile`)
- Need system that queries pile at cursor position
- Need system that deducts from pile amount

---

### 2.7 Dump Resources to Storehouse

**Requirement:** Hand can dump held resources into storehouse (RMB hold over storehouse)

**Components:** ✅
- `DivineHandCommandType.DumpToStorehouse` exists
- `HandInteractionState.FlagDumping` exists
- `StorehouseInventory`, `StorehouseInventoryItem` buffers exist

**Systems:** ⚠️
- `DivineHandSystem` - Handles `DivineHandCommandType.DumpToStorehouse` (lines 225-257)
  - Calls `DepositToStorehouse()` helper
  - Deducts from `HeldAmount` based on `DumpRate`
  - **Status:** Functional but needs verification
- `StorehouseDumpRmbHandler` - Raises events (MonoBehaviour bridge)
- `StorehouseApi.TryDeposit()` - API helper exists

**Data-Driven Entity Support:** ✅
- Works with any storehouse entity
- Uses `StorehouseApi` which queries storehouse components

**Status:** ✅ **COMPLETE** (needs testing/verification)

---

### 2.8 Dump Resources to Construction Yard

**Requirement:** Hand can dump resources directly to construction ghost/yard (RMB hold over construction site)

**Components:** ⚠️
- `ConstructionGhost` component exists
- `DivineHandCommandType` enum doesn't have construction-specific command
- Could use `DumpToStorehouse` or add new command type

**Systems:** ❌
- No system handles direct hand-to-construction dump
- `ConstructionSystem` only handles villager ticket withdrawals from storehouse
- No construction intake system

**Data-Driven Entity Support:** ❌
- Not implemented

**Status:** ❌ **NOT IMPLEMENTED**

**Gap:** No system accepts hand dumps directly to construction. Construction only accepts villager withdrawals from storehouse.

---

### 2.9 Sustained Dump to Construction Yard

**Requirement:** Hand can sustain dump resources to construction yard (RMB hold continuously feeds resources until construction complete or hand empty)

**Components:** ❌
- No sustained dump state component
- `DivineHandCommandType` doesn't have sustained construction dump

**Systems:** ❌
- No system handles sustained construction feed
- `ConstructionSystem` doesn't accept direct hand input
- No construction intake rate limiting

**Data-Driven Entity Support:** ❌
- Not implemented

**Status:** ❌ **NOT IMPLEMENTED**

**Gap:** No sustained dump system for construction. Would need:
- New command type or extend existing dump command
- System that queries construction ghost at cursor
- System that feeds resources to construction progress (increment `ConstructionGhost.Paid`)
- Rate limiting (resources per second)
- Stop condition (construction complete or hand empty)

---

## 3. Gap Identification & Blocking Issues

### 3.1 Critical Gaps

#### Gap 1: Slingshot Charged Release
**Severity:** High  
**Location:** `puredots/Assets/Projects/Godgame/Scripts/Godgame/Systems/DivineHandSystems.cs`  
**Issue:** Charge accumulation works (lines 150-153), but release never applies the charge-scaled impulse to the held entity or miracle. State transitions but no physics impulse.  
**Blocking:** Slingshot mechanic incomplete  
**Dependencies:** None

#### Gap 2: Slingshot Queue Integration
**Severity:** Medium  
**Location:** `godgame/Assets/Scripts/Godgame/Systems/Interaction/GodgameThrowSystem.cs`  
**Issue:** Queue system exists but doesn't integrate with charge system. Queue entries don't store charge level, release doesn't apply charge-based force.  
**Blocking:** Cannot queue charged slingshot throws  
**Dependencies:** Gap 1 (slingshot throw must work first)

#### Gap 3: Pile Siphon System
**Severity:** High  
**Location:** Missing system  
**Issue:** `DivineHandSystem` accumulates units but doesn't query pile entities or deduct from pile amount. No system connects siphon command to pile components.  
**Blocking:** Cannot siphon from piles  
**Dependencies:** Need to identify pile component structure (`AggregatePile`, `ResourcePile`, etc.)

#### Gap 4: Construction Yard Direct Dump
**Severity:** Medium  
**Location:** Missing system  
**Issue:** No system accepts hand dumps directly to construction. Construction only accepts villager withdrawals from storehouse.  
**Blocking:** Cannot dump directly to construction  
**Dependencies:** `ConstructionGhost` component exists, need intake system

#### Gap 5: Sustained Construction Feed
**Severity:** Medium  
**Location:** Missing system  
**Issue:** No sustained dump system for construction. Need continuous feed until construction complete or hand empty.  
**Blocking:** Cannot sustain feed construction  
**Dependencies:** Gap 4 (direct dump must work first)

### 3.2 Burst Compliance Issues

#### Issue 1: Input Reading in Burst Systems
**Location:** `GodgamePickupSystem`, `GodgameThrowSystem`, `GodgameThrowQueueSystem`  
**Issue:** Systems use `[BurstDiscard]` for input reading (Mouse.current, Keyboard.current). This is correct but limits Burst optimization.  
**Impact:** Low (input must be non-Burst, but core logic can be Burst)  
**Recommendation:** Keep as-is (correct pattern)

#### Issue 2: GodHandThrowSystem Not Burst
**Location:** `puredots/Packages/com.moni.puredots/Runtime/Systems/Hand/GodHandThrowSystem.cs`  
**Issue:** System not `[BurstCompile]`, uses `GodHandCommandStreamSingleton` (non-Burst input stream).  
**Impact:** Low (placeholder implementation anyway)  
**Recommendation:** Complete implementation first, then consider Burst optimization

### 3.3 Namespace Hygiene

**Status:** ✅ No namespace collisions detected in hand/interaction systems

### 3.4 Data-Driven Entity Dependencies

#### Dependency 1: Pile Component Structure
**Requirement:** Need to identify pile component structure for siphon system  
**Current State:** Pile components exist (`AggregatePile`, `ResourcePile` mentioned in docs) but need verification  
**Action:** Query codebase for pile component definitions

#### Dependency 2: Construction Intake Interface
**Requirement:** Need construction intake interface (similar to `StorehouseApi.TryDeposit()`)  
**Current State:** `ConstructionGhost` component exists, but no intake API  
**Action:** Create `ConstructionApi.TryFeed()` helper

---

## 4. Actionable Recommendations

### 4.1 Priority 1: Complete Slingshot Throw

**Tasks:**
1. **Apply charge impulse in `DivineHandSystem`**
   - File: `puredots/Assets/Projects/Godgame/Scripts/Godgame/Systems/DivineHandSystems.cs`
   - Location: `ReleaseHeldEntity()` method or new `ReleaseSlingshotThrow()` method
   - Action: When `ChargeTimer >= MinChargeSeconds` and release requested, calculate speed from charge curve and apply that velocity to the currently held entity/miracle (no new projectile spawn)
   - Speed calculation: `Speed = Lerp(MinSpeed, MaxSpeed, ChargeTimer / MaxChargeSeconds)`
   - Direction: Use `AimDirection` from `DivineHandState`

2. **Add visual feedback system** (optional, can be separate)
   - File: New presentation system
   - Action: Show rubber band effect, trajectory preview arc
   - Status: Presentation layer, not blocking

**Estimated Effort:** 2-4 hours  
**Dependencies:** None  
**Burst Compliance:** Core logic can be Burst, visual feedback non-Burst

---

### 4.2 Priority 2: Implement Pile Siphon System

**Tasks:**
1. **Identify pile component structure**
   - Search for `AggregatePile`, `ResourcePile` components
   - Verify component fields (amount, resource type, etc.)

2. **Create `GodgamePileSiphonSystem`**
   - File: `godgame/Assets/Scripts/Godgame/Systems/Interaction/GodgamePileSiphonSystem.cs`
   - Action:
     - Query pile entities at cursor position (spatial query or raycast)
     - When `DivineHandCommandType.SiphonPile` active, deduct from pile amount
     - Add to hand `HeldAmount` (already handled by `DivineHandSystem`)
     - Rate limit: `SiphonRate` units per second
   - Integration: Read `DivineHandCommand` from hand entity, query pile at `TargetPosition`

3. **Update `DivineHandSystem`** (if needed)
   - Verify siphon command handling works with new pile system
   - May need to set `TargetEntity` to pile entity

**Estimated Effort:** 4-6 hours  
**Dependencies:** Pile component structure  
**Burst Compliance:** ✅ Can be fully Burst (spatial queries, component lookups)

---

### 4.3 Priority 3: Implement Construction Direct Dump

**Tasks:**
1. **Create `ConstructionApi` helper**
   - File: `godgame/Assets/Scripts/Godgame/Construction/ConstructionApi.cs`
   - Action: Similar to `StorehouseApi.TryDeposit()`
   - Method: `TryFeed(Entity constructionEntity, ushort resourceTypeIndex, float amount, ref SystemState state, out float rejected)`
   - Logic: Increment `ConstructionGhost.Paid`, return accepted amount

2. **Update `DivineHandSystem`**
   - File: `puredots/Assets/Projects/Godgame/Scripts/Godgame/Systems/DivineHandSystems.cs`
   - Action: Add new command type `DivineHandCommandType.DumpToConstruction` or extend existing dump
   - When cursor over construction ghost, set command type
   - Call `ConstructionApi.TryFeed()` in dump processing (similar to `DepositToStorehouse()`)

3. **Add construction detection to input bridge**
   - File: New `ConstructionDumpRmbHandler` or extend existing handler
   - Action: Detect cursor over construction ghost, raise command

**Estimated Effort:** 3-4 hours  
**Dependencies:** `ConstructionGhost` component (exists)  
**Burst Compliance:** ✅ Can be fully Burst

---

### 4.4 Priority 4: Implement Sustained Construction Feed

**Tasks:**
1. **Extend dump command handling**
   - File: `puredots/Assets/Projects/Godgame/Scripts/Godgame/Systems/DivineHandSystems.cs`
   - Action: When `DivineHandCommandType.DumpToConstruction` active and RMB held:
     - Each frame, feed resources at `DumpRate` per second
     - Continue until `ConstructionGhost.Paid >= Cost` or hand empty
     - Stop when RMB released or construction complete

2. **Add construction completion check**
   - File: `ConstructionApi` or `DivineHandSystem`
   - Action: Check if `ConstructionGhost.Paid >= Cost`, if so, stop feeding (construction system handles conversion)

**Estimated Effort:** 2-3 hours  
**Dependencies:** Priority 3 (direct dump must work first)  
**Burst Compliance:** ✅ Can be fully Burst

---

### 4.5 Priority 5: Implement Slingshot Queue Integration

**Tasks:**
1. **Extend `ThrowQueueEntry` to store charge level**
   - File: `puredots/Packages/com.moni.puredots/Runtime/Runtime/Interaction/PickupThrowComponents.cs`
   - Action: Add `ChargeLevel` field (0-1 normalized)

2. **Update `GodgameThrowSystem.HandleQueueThrow()`**
   - File: `godgame/Assets/Scripts/Godgame/Systems/Interaction/GodgameThrowSystem.cs`
   - Action: When queueing, store charge level from `PickupState` or `DivineHandState.ChargeTimer`

3. **Update `GodgameThrowQueueSystem.ApplyThrow()`**
   - File: `godgame/Assets/Scripts/Godgame/Systems/Interaction/GodgameThrowQueueSystem.cs`
   - Action: Calculate force from charge level: `Force = BaseForce + (ChargeLevel * MaxExtraForce)`

**Estimated Effort:** 2-3 hours  
**Dependencies:** Priority 1 (slingshot throw must work first)  
**Burst Compliance:** ✅ Can be fully Burst

---

### 4.6 Testing & Verification

**Tasks:**
1. **Unit tests for each system**
   - Pickup system: Test state transitions
   - Throw system: Test force calculation, direction
   - Queue system: Test FIFO release
   - Siphon system: Test pile deduction, rate limiting
   - Dump system: Test storehouse deposit, construction feed

2. **Integration tests**
   - Full pickup → throw flow
   - Full pickup → queue → release flow
   - Full siphon → dump flow
   - Full construction feed flow

3. **Performance profiling**
   - Verify Burst compilation
   - Profile spatial queries (pile siphon, construction detection)
   - Profile buffer operations (queue, inventory)

**Estimated Effort:** 4-6 hours  
**Dependencies:** All implementations complete

---

## 5. Summary Matrix

| Capability | Components | Systems | Data-Driven | Status |
|------------|------------|---------|-------------|--------|
| Pickup Objects | ✅ | ✅ | ✅ | ✅ Complete |
| Immediate Throw | ✅ | ✅ | ✅ | ✅ Complete |
| Queued Throw | ✅ | ✅ | ✅ | ✅ Complete |
| Slingshot Throw | ⚠️ | ⚠️ | ⚠️ | ⚠️ Partial (charge works, throw missing) |
| Queue Slingshot | ⚠️ | ❌ | ❌ | ❌ Not Implemented |
| Siphon from Piles | ✅ | ⚠️ | ⚠️ | ⚠️ Partial (command works, pile interaction missing) |
| Dump to Storehouse | ✅ | ✅ | ✅ | ✅ Complete |
| Dump to Construction | ⚠️ | ❌ | ❌ | ❌ Not Implemented |
| Sustained Construction Feed | ❌ | ❌ | ❌ | ❌ Not Implemented |

**Legend:**
- ✅ Complete and functional
- ⚠️ Partially complete (components exist, systems incomplete)
- ❌ Not implemented

---

## 6. Implementation Order

**Recommended sequence:**
1. **Slingshot Throw** (Priority 1) - Unblocks slingshot queue
2. **Pile Siphon** (Priority 2) - High-value feature
3. **Construction Direct Dump** (Priority 3) - Unblocks sustained feed
4. **Sustained Construction Feed** (Priority 4) - Depends on direct dump
5. **Slingshot Queue Integration** (Priority 5) - Depends on slingshot throw

**Total Estimated Effort:** 13-20 hours

---

## 7. Files Requiring Changes

### New Files
- `godgame/Assets/Scripts/Godgame/Systems/Interaction/GodgamePileSiphonSystem.cs`
- `godgame/Assets/Scripts/Godgame/Construction/ConstructionApi.cs`
- `godgame/Assets/Scripts/Godgame/Input/ConstructionDumpRmbHandler.cs` (optional, or extend existing)

### Modified Files
- `puredots/Assets/Projects/Godgame/Scripts/Godgame/Systems/DivineHandSystems.cs` (slingshot throw, construction dump)
- `godgame/Assets/Scripts/Godgame/Systems/Interaction/GodgameThrowSystem.cs` (slingshot queue)
- `godgame/Assets/Scripts/Godgame/Systems/Interaction/GodgameThrowQueueSystem.cs` (charge-based force)
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Interaction/PickupThrowComponents.cs` (extend ThrowQueueEntry)

---

## 8. References

- **Divine Hand State Machine:** `puredots/Docs/Mechanics/DivineHandStateMachine.md`
- **Pickup and Throw System:** `puredots/Docs/Concepts/Core/Pickup_And_Throw_System.md`
- **Slingshot Throw:** `godgame/Docs/Concepts/Interaction/Slingshot_Throw.md`
- **Slingshot Charge:** `godgame/Docs/Concepts/Interaction/Slingshot_Charge_Mechanic.md`
- **Storehouse System:** `puredots/Docs/Concepts/Core/Storehouse_System_Summary.md`
- **Aggregate Piles:** `godgame/Docs/Concepts/Implemented/Resources/Aggregate_Piles.md`

---

**End of Analysis**

