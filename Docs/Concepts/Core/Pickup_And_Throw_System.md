# Pickup and Throw System

**Status:** Design Directive / Implementation Guide
**Category:** Core - Interaction / Physics
**Audience:** Implementers / Architects
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

**Purpose:** DOTS-friendly system for picking up → holding/priming → queued throwing → impulse retention, plus "siphon" aggregate resources (Black & White 2 style) without turning everything into expensive physics objects.

**Key Design Principle:** Most "stuff" is aggregate (piles/storehouses); only a few objects become physical during interaction. This achieves B&W2-style feel without per-unit physics simulation.

**Performance Goal:** Keep dynamic physics bodies to a small active set. Most resources stay aggregate unless actively being interacted with.

---

## Core Concepts

### 1. Two Kinds of Entities

#### A) Physical Item (Can Be Grabbed/Thrown)

**Components (typical):**
- `PickableTag` - Marks entity as pickable
- `LocalTransform` (+ `Parent` when held) - Position/rotation
- `PhysicsCollider` - Unity Physics collider for queries
- `PhysicsVelocity` - Linear and angular velocity
- `PhysicsMass` - Mass properties
- `PhysicsMassOverride` - Controls kinematic state while held
- `PhysicsGravityFactor` - Optional; can be ignored while kinematic
- `ItemPayload` - What the item "is" (resource type, quality, etc.)

**Use Case:** Rocks, tools, individual objects that can be thrown.

#### B) Aggregate Pile / Storehouse Node (Not Physical by Default)

**Components:**
- `StorehouseInventory` buffer (resourceIndex→amount, qualitySum)
- Minimal presentation (single mesh/VFX)
- No dynamic physics unless within "interaction radius" (LOD)

**Performance Win:** Most "stuff" is aggregate; only a few objects become physical during interaction.

---

## Data Model

### Pickup Components

```csharp
/// <summary>
/// Marks entity as pickable (can be grabbed and thrown).
/// </summary>
public struct PickableTag : IComponentData { }

/// <summary>
/// Indicates item is currently held by a holder.
/// </summary>
public struct HeldBy : IComponentData
{
    public Entity Holder;        // Entity holding this item
    public Entity HandAnchor;    // Hand anchor entity (for transform hierarchy)
}

/// <summary>
/// Item payload data (what the item "is").
/// </summary>
public struct ItemPayload : IComponentData
{
    public ushort ResourceTypeIndex;
    public float Amount;
    public ushort QualitySum;
    public byte TierId;
}
```

### Throw Components

```csharp
/// <summary>
/// Throw charge state (while button held).
/// </summary>
public struct ThrowCharge : IComponentData
{
    public uint StartTick;       // Tick when charging started
    public uint MaxTicks;        // Maximum charge ticks
    public float BaseImpulse;    // Base impulse strength
    public float MaxExtra;       // Maximum extra impulse from charging
}

/// <summary>
/// Queued throw command (applied at release tick).
/// </summary>
public struct QueuedThrow : IComponentData
{
    public uint ReleaseTick;     // Exact tick to apply impulse
    public float3 Impulse;       // Impulse vector (world space)
    public float3 PointLocal;    // Point to apply impulse (local space, for spin)
}

/// <summary>
/// Stack payload for aggregate resources (piles become payload items).
/// </summary>
public struct ItemPayloadStack : IComponentData
{
    public ushort ResourceTypeIndex;
    public float Amount;
    public uint QualitySum;
}
```

### Siphon Components

```csharp
/// <summary>
/// Siphon state (attracting items toward hand).
/// </summary>
public struct SiphonActive : IComponentData
{
    public float AttractionStrength;    // Impulse strength per tick
    public float GrabRadius;            // Radius to convert to held/carried
    public float MaxBudgetPerTick;      // Maximum attraction budget per tick
}

/// <summary>
/// Carried aggregate payload (data-only, no physics).
/// </summary>
public struct CarriedPayload : IComponentData
{
    public ushort ResourceTypeIndex;
    public float Amount;
    public uint QualitySum;
}
```

---

## How It Works

### 1. Pickup Flow

**Step 1: Finding Pickables**

Use Unity Physics collision queries (not per-object scans):

```csharp
// Use CollisionWorld overlap/distance queries (cheap broadphase)
var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

// OverlapAabb or OverlapBox for area queries
var overlapInput = new OverlapAabbInput
{
    Aabb = new Aabb { Min = minBounds, Max = maxBounds },
    Filter = new CollisionFilter { BelongsTo = pickableLayer, CollidesWith = handLayer }
};

var results = new NativeList<OverlapAabbHit>(Allocator.Temp);
collisionWorld.OverlapAabb(overlapInput, ref results);

// Or CalculateDistance for nearest item
var distanceInput = new PointDistanceInput
{
    Position = handPosition,
    MaxDistance = maxGrabDistance,
    Filter = new CollisionFilter { BelongsTo = pickableLayer, CollidesWith = handLayer }
};

collisionWorld.CalculateDistance(distanceInput, out var hit);
```

**Step 2: Attach to Hand (Transform Hierarchy)**

When a hand grabs an item:

```csharp
// Attach item to hand anchor using transform hierarchy
var ecb = new EntityCommandBuffer(Allocator.TempJob);

// Set parent to hand anchor
ecb.SetComponent(itemEntity, new Parent { Value = handAnchorEntity });

// Set local transform to grip offset
var gripOffset = math.mul(math.inverse(handTransform.Rotation), worldPos - handTransform.Position);
ecb.SetComponent(itemEntity, new LocalTransform 
{ 
    Position = gripOffset,
    Rotation = quaternion.identity,  // Or preserve item rotation
    Scale = 1f
});

// Add HeldBy component
ecb.AddComponent(itemEntity, new HeldBy
{
    Holder = holderEntity,
    HandAnchor = handAnchorEntity
});
```

**Step 3: Make Kinematic (Don't Fight the Solver)**

```csharp
// Make it kinematic while held
if (SystemAPI.HasComponent<PhysicsMassOverride>(itemEntity))
{
    var massOverride = SystemAPI.GetComponent<PhysicsMassOverride>(itemEntity);
    massOverride.IsKinematic = 1;
    massOverride.SetVelocityToZero = 1;  // Stop jittering
    ecb.SetComponent(itemEntity, massOverride);
}
else
{
    ecb.AddComponent(itemEntity, new PhysicsMassOverride
    {
        IsKinematic = 1,
        SetVelocityToZero = 1
    });
}

// Gravity is ignored when kinematic (no PhysicsGravityFactor needed)
```

**Optional:** Switch collision filter to not collide with holder:

```csharp
// Update collision filter to ignore holder
var collider = SystemAPI.GetComponent<PhysicsCollider>(itemEntity);
collider.Value.SetCollisionFilter(new CollisionFilter
{
    BelongsTo = collider.Value.GetCollisionFilter().BelongsTo,
    CollidesWith = collider.Value.GetCollisionFilter().CollidesWith & ~holderLayer,
    GroupIndex = collider.Value.GetCollisionFilter().GroupIndex
});
ecb.SetComponent(itemEntity, collider);
```

---

### 2. Queued Throw Flow

**Principle:** Make throwing deterministic and rewind-safe by applying one impulse at the exact release tick.

#### A) Charge System (While Button Held)

```csharp
[BurstCompile]
[UpdateInGroup(typeof(InteractionSystemGroup))]
public partial struct ThrowChargeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var timeState = SystemAPI.GetSingleton<TimeState>();
        var currentTick = timeState.Tick;

        foreach (var (heldBy, throwCharge, throwQueue, entity) in
            SystemAPI.Query<RefRO<HeldBy>, RefRW<ThrowCharge>, RefRW<QueuedThrow>>()
                .WithEntityAccess())
        {
            // Check if button is held (input system sets this)
            if (!IsButtonHeld(heldBy.ValueRO.Holder))
            {
                continue;
            }

            // Initialize charge if not started
            if (throwCharge.ValueRO.StartTick == 0)
            {
                throwCharge.ValueRW.StartTick = currentTick;
            }

            // Calculate charge progress (0..1)
            var chargeTicks = currentTick - throwCharge.ValueRO.StartTick;
            var chargeProgress = math.saturate((float)chargeTicks / throwCharge.ValueRO.MaxTicks);

            // Compute impulse (base + extra from charging)
            var impulseMagnitude = throwCharge.ValueRO.BaseImpulse + 
                                   (throwCharge.ValueRO.MaxExtra * chargeProgress);

            // Get hand velocity (for "swing matters" feel)
            var handVelocity = GetHandVelocity(heldBy.ValueRO.HandAnchor, ref state);
            var handDirection = math.normalizesafe(handVelocity);

            // Calculate throw direction (hand direction + some up vector)
            var throwDirection = math.normalize(handDirection + new float3(0, 0.3f, 0));

            // Queue throw for next tick (or current tick + 1 for determinism)
            throwQueue.ValueRW = new QueuedThrow
            {
                ReleaseTick = currentTick + 1,  // Apply on next tick
                Impulse = throwDirection * impulseMagnitude,
                PointLocal = float3.zero  // Center of mass (or offset for spin)
            };

            // Optional: Update preview arc (presentation-only, not sim state)
            UpdatePreviewArc(entity, throwQueue.ValueRO.Impulse, ref state);
        }
    }
}
```

#### B) Release System (When Tick == ReleaseTick)

```csharp
[BurstCompile]
[UpdateInGroup(typeof(InteractionSystemGroup))]
[UpdateAfter(typeof(ThrowChargeSystem))]
public partial struct ThrowReleaseSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var timeState = SystemAPI.GetSingleton<TimeState>();
        var currentTick = timeState.Tick;
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (heldBy, queuedThrow, entity) in
            SystemAPI.Query<RefRO<HeldBy>, RefRO<QueuedThrow>>()
                .WithEntityAccess())
        {
            // Check if release tick has arrived
            if (queuedThrow.ValueRO.ReleaseTick != currentTick)
            {
                continue;
            }

            // 1. Detach (remove Parent)
            ecb.RemoveComponent<Parent>(entity);
            ecb.RemoveComponent<HeldBy>(entity);

            // 2. Re-enable dynamics
            if (SystemAPI.HasComponent<PhysicsMassOverride>(entity))
            {
                var massOverride = SystemAPI.GetComponent<PhysicsMassOverride>(entity);
                massOverride.IsKinematic = 0;
                massOverride.SetVelocityToZero = 0;
                ecb.SetComponent(entity, massOverride);
            }

            // 3. Apply impulse
            var physicsVelocity = SystemAPI.GetComponent<PhysicsVelocity>(entity);
            var physicsMass = SystemAPI.GetComponent<PhysicsMass>(entity);

            // ApplyLinearImpulse for center-of-mass impulse
            physicsVelocity.ApplyLinearImpulse(physicsMass, queuedThrow.ValueRO.Impulse);

            // Or ApplyImpulse for point-based impulse (for spin)
            // var worldPoint = math.transform(SystemAPI.GetComponent<LocalToWorld>(entity).Value, queuedThrow.ValueRO.PointLocal);
            // physicsVelocity.ApplyImpulse(physicsMass, queuedThrow.ValueRO.Impulse, worldPoint, SystemAPI.GetComponent<LocalTransform>(entity).ValueRO.Position);

            ecb.SetComponent(entity, physicsVelocity);

            // 4. Remove throw components
            ecb.RemoveComponent<ThrowCharge>(entity);
            ecb.RemoveComponent<QueuedThrow>(entity);

            // Impulse retention is automatic (don't zero velocity after release)
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

**Key Points:**
- **Deterministic:** Impulse applied at exact `ReleaseTick`
- **Rewind-safe:** Single impulse application, no continuous forces
- **Impulse Retention:** Automatic as long as `SetVelocityToZero = 0` after release

**Reference:** [Unity Physics - ApplyImpulse](https://docs.unity.cn/Packages/com.unity.physics@1.0/api/Unity.Physics.PhysicsVelocity.html)

---

### 3. Siphon Flow (B&W2 Style)

**Two Modes:**

#### Mode A: Physical Siphon (Feel-Good, But Budgeted)

```csharp
[BurstCompile]
[UpdateInGroup(typeof(InteractionSystemGroup))]
public partial struct PhysicalSiphonSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var timeState = SystemAPI.GetSingleton<TimeState>();
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        foreach (var (siphonActive, handTransform, entity) in
            SystemAPI.Query<RefRO<SiphonActive>, RefRO<LocalTransform>>()
                .WithEntityAccess())
        {
            var handPos = handTransform.ValueRO.Position;
            var siphonRadius = siphonActive.ValueRO.GrabRadius * 2f;  // Query radius larger than grab radius

            // Query nearby bodies with OverlapAabb
            var overlapInput = new OverlapAabbInput
            {
                Aabb = new Aabb
                {
                    Min = handPos - siphonRadius,
                    Max = handPos + siphonRadius
                },
                Filter = new CollisionFilter
                {
                    BelongsTo = pickableLayer,
                    CollidesWith = handLayer
                }
            };

            var results = new NativeList<OverlapAabbHit>(Allocator.Temp);
            collisionWorld.OverlapAabb(overlapInput, ref results);

            var budgetUsed = 0f;
            var maxBudget = siphonActive.ValueRO.MaxBudgetPerTick;

            for (int i = 0; i < results.Length && budgetUsed < maxBudget; i++)
            {
                var hit = results[i];
                var itemEntity = hit.Entity;

                // Skip if already held
                if (SystemAPI.HasComponent<HeldBy>(itemEntity))
                {
                    continue;
                }

                // Get item position
                var itemTransform = SystemAPI.GetComponent<LocalTransform>(itemEntity);
                var itemPos = itemTransform.Position;
                var distance = math.distance(handPos, itemPos);

                // Check if within grab radius (convert to held)
                if (distance <= siphonActive.ValueRO.GrabRadius)
                {
                    // Convert to held (attach to hand)
                    AttachToHand(itemEntity, entity, ref state);
                    budgetUsed += 1f;  // Budget cost for grab
                }
                else
                {
                    // Apply attraction impulse (capped by budget)
                    var direction = math.normalize(handPos - itemPos);
                    var impulseStrength = siphonActive.ValueRO.AttractionStrength;
                    var impulse = direction * impulseStrength;

                    var itemVelocity = SystemAPI.GetComponent<PhysicsVelocity>(itemEntity);
                    var itemMass = SystemAPI.GetComponent<PhysicsMass>(itemEntity);

                    // ApplyLinearImpulse (capped)
                    var impulseCost = math.length(impulse) * 0.1f;  // Budget cost
                    if (budgetUsed + impulseCost <= maxBudget)
                    {
                        itemVelocity.ApplyLinearImpulse(itemMass, impulse);
                        SystemAPI.SetComponent(itemEntity, itemVelocity);
                        budgetUsed += impulseCost;
                    }
                }
            }

            results.Dispose();
        }
    }
}
```

#### Mode B: Aggregate Siphon (Most Scalable)

```csharp
[BurstCompile]
[UpdateInGroup(typeof(InteractionSystemGroup))]
public partial struct AggregateSiphonSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (siphonActive, handTransform, holderEntity) in
            SystemAPI.Query<RefRO<SiphonActive>, RefRO<LocalTransform>>()
                .WithEntityAccess())
        {
            var handPos = handTransform.ValueRO.Position;
            var siphonRadius = siphonActive.ValueRO.GrabRadius;

            // Query nearby storehouses/piles (spatial query, not physics)
            var storehouseRegistry = SystemAPI.GetSingleton<StorehouseRegistry>();
            var entries = SystemAPI.GetBuffer<StorehouseRegistryEntry>(
                SystemAPI.GetSingletonEntity<StorehouseRegistry>());

            var budgetUsed = 0f;
            var maxBudget = siphonActive.ValueRO.MaxBudgetPerTick;

            for (int i = 0; i < entries.Length && budgetUsed < maxBudget; i++)
            {
                var entry = entries[i];
                var distance = math.distance(handPos, entry.Position);

                if (distance > siphonRadius)
                {
                    continue;
                }

                // Withdraw from storehouse (using StorehouseApi)
                var withdrawAmount = math.min(10f, maxBudget - budgetUsed);  // Budget-limited withdrawal
                var withdrawn = StorehouseApi.TryWithdraw(
                    entry.StorehouseEntity,
                    resourceTypeIndex,  // Which resource to siphon
                    withdrawAmount,
                    ref state,
                    reservedOut: 0f);

                if (withdrawn > 0f)
                {
                    // Add to carried payload (data-only, no physics)
                    if (SystemAPI.HasComponent<CarriedPayload>(holderEntity))
                    {
                        var payload = SystemAPI.GetComponent<CarriedPayload>(holderEntity);
                        payload.Amount += withdrawn;
                        SystemAPI.SetComponent(holderEntity, payload);
                    }
                    else
                    {
                        SystemAPI.AddComponent(holderEntity, new CarriedPayload
                        {
                            ResourceTypeIndex = resourceTypeIndex,
                            Amount = withdrawn,
                            QualitySum = 0  // Quality handling
                        });
                    }

                    budgetUsed += withdrawn;

                    // Spawn flying chunk VFX (presentation-only, not sim)
                    SpawnFlyingChunkVFX(entry.Position, handPos, resourceTypeIndex, ref state);
                }
            }
        }
    }
}
```

**Benefits:**
- **Scalable:** No physics objects for aggregate resources
- **B&W2 Feel:** Visual feedback via VFX chunks
- **Budgeted:** Respects performance limits

---

### 4. Throwing Aggregate Resources

**When you "grab a pile":**

```csharp
/// <summary>
/// Converts aggregate pile to physical payload item for throwing.
/// </summary>
public static void ConvertPileToPayloadItem(
    Entity pileEntity,
    Entity holderEntity,
    ref SystemState state)
{
    // Get pile inventory
    var inventoryItems = SystemAPI.GetBuffer<StorehouseInventoryItem>(pileEntity);
    
    // Create single physical payload entity
    var payloadEntity = state.EntityManager.CreateEntity();
    
    // Aggregate all resources into single payload stack
    var totalAmount = 0f;
    var dominantResourceIndex = (ushort)0;
    var totalQualitySum = 0u;
    
    for (int i = 0; i < inventoryItems.Length; i++)
    {
        var item = inventoryItems[i];
        totalAmount += item.Amount;
        // Use first resource type (or weighted average)
        if (i == 0)
        {
            dominantResourceIndex = item.ResourceTypeIndex;  // After ResourceTypeIndex migration
        }
        totalQualitySum += (uint)(item.Amount * item.AverageQuality);  // Or QualitySum
    }
    
    // Add payload component
    state.EntityManager.AddComponent(payloadEntity, new ItemPayloadStack
    {
        ResourceTypeIndex = dominantResourceIndex,
        Amount = totalAmount,
        QualitySum = totalQualitySum
    });
    
    // Add physics components (PickableTag, PhysicsCollider, etc.)
    state.EntityManager.AddComponent<PickableTag>(payloadEntity);
    // ... add physics collider, mass, etc.
    
    // Attach to hand (same as regular pickup)
    AttachToHand(payloadEntity, holderEntity, ref state);
}
```

**On Impact:**

```csharp
/// <summary>
/// Handles payload item impact (collision with receiver or ground).
/// </summary>
public static void HandlePayloadImpact(
    Entity payloadEntity,
    Entity hitEntity,
    float3 impactPosition,
    ref SystemState state)
{
    var payload = SystemAPI.GetComponent<ItemPayloadStack>(payloadEntity);
    
    // Check if hit valid receiver (construction site/storehouse)
    if (SystemAPI.HasComponent<StorehouseConfig>(hitEntity))
    {
        // Deposit directly
        var accepted = StorehouseApi.TryDeposit(
            hitEntity,
            payload.ResourceTypeIndex,
            payload.Amount,
            ref state,
            out var rejected);
        
        // Destroy payload entity (or reduce amount if partial deposit)
        if (accepted >= payload.Amount)
        {
            state.EntityManager.DestroyEntity(payloadEntity);
        }
        else
        {
            payload.Amount -= accepted;
            SystemAPI.SetComponent(payloadEntity, payload);
        }
    }
    else
    {
        // Spawn new pile/storehouse node at impact location
        var pileEntity = CreateResourcePile(
            impactPosition,
            payload.ResourceTypeIndex,
            payload.Amount,
            payload.QualitySum,
            ref state);
        
        // Destroy payload entity
        state.EntityManager.DestroyEntity(payloadEntity);
    }
}
```

---

## DOTS-Specific Notes and Warnings

### LocalToWorld Can Be Stale

**Problem:** `LocalToWorld` can be out of date during `SimulationSystemGroup`; don't rely on it for exact sim math while the tick is running.

**Solution:**
```csharp
// ❌ WRONG: Using LocalToWorld during simulation
var worldPos = SystemAPI.GetComponent<LocalToWorld>(entity).Value.Position;

// ✅ CORRECT: Use LocalTransform or compute explicitly
var localTransform = SystemAPI.GetComponent<LocalTransform>(entity);
var parentTransform = SystemAPI.GetComponent<LocalTransform>(parentEntity);
var worldPos = math.transform(new RigidTransform(parentTransform.Rotation, parentTransform.Position), localTransform.Position);
```

**Reference:** [Unity ECS - Transform Systems](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/transforms.html)

### Keep Dynamic Physics Bodies Small

**Rule:** Keep dynamic physics bodies to a small active set. Most resources should stay aggregate unless actively being interacted with.

**LOD Strategy:**
- **Tier 0 (Active):** Items being held/thrown (full physics)
- **Tier 1 (Nearby):** Items within interaction radius (collision queries only)
- **Tier 2 (Far):** Items outside interaction radius (aggregate only, no physics)

---

## Systems Architecture

### System Groups

```csharp
// Interaction System Group (after Physics, before Presentation)
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public class InteractionSystemGroup : ComponentSystemGroup { }
```

### System Order

1. **PickupDetectionSystem** - Finds pickables via physics queries
2. **PickupAttachSystem** - Attaches items to hands (transform hierarchy + kinematic)
3. **ThrowChargeSystem** - Computes charge and queues throw
4. **ThrowReleaseSystem** - Applies impulse at release tick
5. **PhysicalSiphonSystem** - Attracts physical items (budgeted)
6. **AggregateSiphonSystem** - Withdraws from piles (budgeted)
7. **PayloadImpactSystem** - Handles payload item impacts (collision events)

---

## Performance Considerations

### Budgets

- **Siphon Budget:** Max items/impulses per tick (e.g., 10 items, 5 impulses)
- **Query Radius:** Limit overlap query radius (e.g., 5m for siphon, 2m for grab)
- **Active Physics:** Max dynamic physics bodies (e.g., 50 active items)

### LOD Strategy

```csharp
public enum ItemLODTier : byte
{
    Tier0_Active = 0,      // Being held/thrown (full physics)
    Tier1_Nearby = 1,      // Within interaction radius (queries only)
    Tier2_Far = 2,         // Outside interaction radius (aggregate only)
    Tier3_Aggregate = 3    // Always aggregate (no physics)
}
```

---

## MVP Implementation Order

### Phase 1: Basic Pickup and Throw

1. ✅ Pickable physical object → hold (kinematic) → queued throw → impulse
2. ✅ `PickableTag`, `HeldBy`, `ThrowCharge`, `QueuedThrow` components
3. ✅ `PickupAttachSystem`, `ThrowChargeSystem`, `ThrowReleaseSystem`

### Phase 2: Aggregate Integration

4. ✅ Aggregate resource pile → withdraw to carried stack → throw stack → deposit/spawn pile
5. ✅ `ItemPayloadStack`, `CarriedPayload` components
6. ✅ `ConvertPileToPayloadItem`, `HandlePayloadImpact` logic

### Phase 3: Siphon (Optional)

7. ⚠️ Siphon uses `OverlapAabb` + `ApplyImpulse` under strict budget
8. ⚠️ `SiphonActive`, `PhysicalSiphonSystem`, `AggregateSiphonSystem`

---

## Primed Throws System

**Purpose:** Clean DOTS implementation for "primed throws" that hang in the air, with queue management and release controls.

**Controls:**
- **Shift** = Put held item into the primed queue
- **Hotkey A** = Release next (FIFO)
- **Hotkey B** = Release all
- **LMB** = Launch selected primed
- **RMB** = Pick back up (cancel launch)

**Design Goals:** Cheap, rewind-friendly, deterministic.

---

### 1. Three Explicit States

**Held → Primed → Launched**

- **Held:** Item is attached to hand anchor (transform parented)
- **Primed:** Item is frozen in place (or in formation), waiting in queue
- **Launched:** Item is dynamic again and gets an impulse

**Implementation:** Use `Parent` + `LocalTransform` for attachment/formation, since the Entities transform system is built around parent/child hierarchies.

---

### 2. Freeze "Primed" Items Safely (No Drift, No Gravity)

When an item becomes Primed:

```csharp
/// <summary>
/// Freezes item in place (hangs in air, no gravity, no drift).
/// </summary>
public static void FreezePrimedItem(Entity itemEntity, ref SystemState state)
{
    // Make physics body temporarily kinematic
    if (SystemAPI.HasComponent<PhysicsMassOverride>(itemEntity))
    {
        var massOverride = SystemAPI.GetComponent<PhysicsMassOverride>(itemEntity);
        massOverride.IsKinematic = 1;           // Treated as infinite mass/inertia
        massOverride.SetVelocityToZero = 1;     // Physics velocity ignored (hangs perfectly)
        SystemAPI.SetComponent(itemEntity, massOverride);
    }
    else
    {
        SystemAPI.AddComponent(itemEntity, new PhysicsMassOverride
        {
            IsKinematic = 1,
            SetVelocityToZero = 1
        });
    }

    // Gravity is ignored while kinematic (no PhysicsGravityFactor needed)
}
```

**Optional: Make Primed Items Non-Colliding**

If you don't want primed items bumping ships/agents:

```csharp
/// <summary>
/// Temporarily disables collisions for primed item.
/// </summary>
public static void DisablePrimedCollisions(Entity itemEntity, ref SystemState state)
{
    // Change collider's CollisionFilter (only on state transitions, not every tick)
    var collider = SystemAPI.GetComponent<PhysicsCollider>(itemEntity);
    var oldFilter = collider.Value.GetCollisionFilter();
    
    var newFilter = new CollisionFilter
    {
        BelongsTo = oldFilter.BelongsTo,
        CollidesWith = 0u,  // Collide with nothing
        GroupIndex = oldFilter.GroupIndex
    };
    
    collider.Value.SetCollisionFilter(newFilter);
    SystemAPI.SetComponent(itemEntity, collider);
}
```

**Note:** Because `PhysicsCollider` holds a blob, do this only on state transitions (Prime/Release), not every tick.

---

### 3. Store the Queue on Player/Caster (Tiny Buffer)

**On the player entity, add a small dynamic buffer:**

```csharp
/// <summary>
/// Entry in the primed throw queue.
/// </summary>
public struct PrimedThrowEntry : IBufferElementData
{
    public Entity Item;          // Primed item entity
    public ushort Slot;          // Queue order + formation slot
    public float Charge01;       // Optional charge level (0-1)
    public float ImpulseMag;     // Stored "power" (impulse magnitude)
    public float3 Spin;          // Optional angular impulse
    public byte Mode;            // 0=WorldPinned, 1=OrbitAroundPlayer
}

/// <summary>
/// Component on each primed item.
/// </summary>
public struct PrimedThrow : IComponentData
{
    public Entity Owner;         // Player/caster entity
    public ushort Slot;          // Queue slot index
    public float3 LocalOffset;   // If orbiting around owner anchor
    public uint PrimedTick;      // Tick when primed
}
```

**Performance:** O(queueSize), and queueSize is naturally small (typically 1-8 items).

---

### 4. Placement: Two "Hang" Styles

#### A) Orbit/Formation Around Player (Most Intuitive)

```csharp
/// <summary>
/// Creates primed items in orbit/formation around player.
/// </summary>
public static void PrimeItemInOrbit(
    Entity itemEntity,
    Entity playerEntity,
    ushort slot,
    ref SystemState state)
{
    // Create PrimeAnchor entity under player (if not exists)
    var primeAnchor = GetOrCreatePrimeAnchor(playerEntity, ref state);
    
    // Attach item to PrimeAnchor
    SystemAPI.SetComponent(itemEntity, new Parent { Value = primeAnchor });
    
    // Place at deterministic offset by slot (ring/spiral stack)
    var offset = CalculateFormationOffset(slot);  // e.g., ring: radius * (cos(angle), sin(angle), height)
    SystemAPI.SetComponent(itemEntity, new LocalTransform
    {
        Position = offset,
        Rotation = quaternion.identity,
        Scale = 1f
    });
    
    // Add PrimedThrow component
    SystemAPI.AddComponent(itemEntity, new PrimedThrow
    {
        Owner = playerEntity,
        Slot = slot,
        LocalOffset = offset,
        PrimedTick = SystemAPI.GetSingleton<TimeState>().Tick
    });
    
    // Freeze physics
    FreezePrimedItem(itemEntity, ref state);
}
```

**Benefits:**
- Obvious what's queued
- Easy to RMB "pick back up"
- Avoids losing primed items offscreen

#### B) World-Pinned (Literally "Hang Where It Was")

```csharp
/// <summary>
/// Primes item at current world position (no parent).
/// </summary>
public static void PrimeItemWorldPinned(
    Entity itemEntity,
    Entity playerEntity,
    ushort slot,
    ref SystemState state)
{
    // No parent - item stays at world position
    if (SystemAPI.HasComponent<Parent>(itemEntity))
    {
        SystemAPI.RemoveComponent<Parent>(itemEntity);
    }
    
    // Set LocalTransform once at prime moment
    var worldPos = SystemAPI.GetComponent<LocalTransform>(itemEntity).Position;
    SystemAPI.SetComponent(itemEntity, new LocalTransform
    {
        Position = worldPos,
        Rotation = SystemAPI.GetComponent<LocalTransform>(itemEntity).Rotation,
        Scale = 1f
    });
    
    // Kinematic keeps it fixed (no gravity, no drift)
    FreezePrimedItem(itemEntity, ref state);
    
    // Add PrimedThrow component
    SystemAPI.AddComponent(itemEntity, new PrimedThrow
    {
        Owner = playerEntity,
        Slot = slot,
        LocalOffset = float3.zero,  // Not used for world-pinned
        PrimedTick = SystemAPI.GetSingleton<TimeState>().Tick
    });
}
```

**Support Both via Mode:**

```csharp
public enum PrimedThrowMode : byte
{
    WorldPinned = 0,      // Hang at world position
    OrbitAroundPlayer = 1 // Orbit around PrimeAnchor
}
```

**Tip:** Don't use `LocalToWorld` as authoritative while sim is running; it can be out of date until the transform group updates. For sim math, read `LocalTransform` (+ parent chain) instead.

---

### 5. Releasing: Apply Impulse on Demand

**On release, do these steps atomically:**

```csharp
/// <summary>
/// Releases primed item (applies impulse and unfreezes).
/// </summary>
public static void ReleasePrimedItem(
    Entity itemEntity,
    float3 aimDirection,
    ref SystemState state)
{
    var primedThrow = SystemAPI.GetComponent<PrimedThrow>(itemEntity);
    var queueEntity = primedThrow.Owner;
    var queue = SystemAPI.GetBuffer<PrimedThrowEntry>(queueEntity);
    
    // 1. Remove from queue buffer (or mark as "released")
    RemoveFromQueue(queue, itemEntity);
    
    // 2. Restore collisions (if disabled)
    RestoreCollisions(itemEntity, ref state);
    
    // 3. Unfreeze physics
    if (SystemAPI.HasComponent<PhysicsMassOverride>(itemEntity))
    {
        var massOverride = SystemAPI.GetComponent<PhysicsMassOverride>(itemEntity);
        massOverride.IsKinematic = 0;
        massOverride.SetVelocityToZero = 0;
        SystemAPI.SetComponent(itemEntity, massOverride);
    }
    
    // 4. Detach if orbiting
    if (SystemAPI.HasComponent<Parent>(itemEntity))
    {
        SystemAPI.RemoveComponent<Parent>(itemEntity);
    }
    
    // 5. Apply impulse
    var physicsVelocity = SystemAPI.GetComponent<PhysicsVelocity>(itemEntity);
    var physicsMass = SystemAPI.GetComponent<PhysicsMass>(itemEntity);
    
    // Get stored impulse magnitude from queue entry
    var impulseMag = GetStoredImpulseMagnitude(queue, primedThrow.Slot);
    
    // Compute direction from current aim (stored magnitude, current direction)
    var impulse = aimDirection * impulseMag;
    
    // ApplyLinearImpulse for straight launch
    physicsVelocity.ApplyLinearImpulse(physicsMass, impulse);
    
    // Or ApplyImpulse for spin (impulse at point)
    // var worldPoint = GetItemCenterOfMass(itemEntity, ref state);
    // physicsVelocity.ApplyImpulse(physicsMass, impulse, worldPoint, GetItemPosition(itemEntity, ref state));
    
    SystemAPI.SetComponent(itemEntity, physicsVelocity);
    
    // 6. Remove PrimedThrow component
    SystemAPI.RemoveComponent<PrimedThrow>(itemEntity);
}
```

**Release Next vs Release All:**

```csharp
/// <summary>
/// Releases next primed item (FIFO - lowest Slot).
/// </summary>
public static void ReleaseNextPrimed(Entity playerEntity, float3 aimDirection, ref SystemState state)
{
    var queue = SystemAPI.GetBuffer<PrimedThrowEntry>(playerEntity);
    if (queue.Length == 0)
    {
        return;
    }
    
    // Find lowest Slot (FIFO)
    ushort minSlot = ushort.MaxValue;
    Entity nextItem = Entity.Null;
    
    for (int i = 0; i < queue.Length; i++)
    {
        if (queue[i].Slot < minSlot)
        {
            minSlot = queue[i].Slot;
            nextItem = queue[i].Item;
        }
    }
    
    if (nextItem != Entity.Null)
    {
        ReleasePrimedItem(nextItem, aimDirection, ref state);
    }
}

/// <summary>
/// Releases all primed items (in slot order).
/// </summary>
public static void ReleaseAllPrimed(Entity playerEntity, float3 aimDirection, ref SystemState state)
{
    var queue = SystemAPI.GetBuffer<PrimedThrowEntry>(playerEntity);
    
    // Sort by slot (ascending)
    var sortedItems = new NativeList<(ushort slot, Entity item)>(queue.Length, Allocator.Temp);
    for (int i = 0; i < queue.Length; i++)
    {
        sortedItems.Add((queue[i].Slot, queue[i].Item));
    }
    sortedItems.Sort();
    
    // Release in order (limit to N per tick for stability)
    const int maxReleasesPerTick = 8;
    var releaseCount = 0;
    
    for (int i = 0; i < sortedItems.Length && releaseCount < maxReleasesPerTick; i++)
    {
        ReleasePrimedItem(sortedItems[i].item, aimDirection, ref state);
        releaseCount++;
    }
    
    sortedItems.Dispose();
}
```

**Direction and "Primed Power":**

- Store impulse magnitude at priming time
- At release time, compute direction from current aim and multiply by stored magnitude
- So "primed" means "charged and ready", but aim can change at launch

---

### 6. RMB Pick-Back (Cancel Launch)

```csharp
/// <summary>
/// Picks back up a primed item (cancels launch, returns to Held state).
/// </summary>
public static void PickBackPrimedItem(
    Entity itemEntity,
    Entity playerEntity,
    Entity handAnchor,
    ref SystemState state)
{
    // 1. Remove from queue
    var queue = SystemAPI.GetBuffer<PrimedThrowEntry>(playerEntity);
    RemoveFromQueue(queue, itemEntity);
    
    // 2. Detach from prime anchor
    if (SystemAPI.HasComponent<Parent>(itemEntity))
    {
        SystemAPI.RemoveComponent<Parent>(itemEntity);
    }
    
    // 3. Reattach to hand (Held state)
    SystemAPI.SetComponent(itemEntity, new Parent { Value = handAnchor });
    
    // Calculate grip offset
    var handTransform = SystemAPI.GetComponent<LocalTransform>(handAnchor);
    var itemWorldPos = SystemAPI.GetComponent<LocalTransform>(itemEntity).Position;
    var gripOffset = math.mul(math.inverse(handTransform.Rotation), itemWorldPos - handTransform.Position);
    
    SystemAPI.SetComponent(itemEntity, new LocalTransform
    {
        Position = gripOffset,
        Rotation = quaternion.identity,
        Scale = 1f
    });
    
    // 4. Keep kinematic while held (same mechanism as regular pickup)
    if (SystemAPI.HasComponent<PhysicsMassOverride>(itemEntity))
    {
        var massOverride = SystemAPI.GetComponent<PhysicsMassOverride>(itemEntity);
        massOverride.IsKinematic = 1;
        massOverride.SetVelocityToZero = 1;
        SystemAPI.SetComponent(itemEntity, massOverride);
    }
    
    // 5. Add HeldBy component
    SystemAPI.AddComponent(itemEntity, new HeldBy
    {
        Holder = playerEntity,
        HandAnchor = handAnchor
    });
    
    // 6. Remove PrimedThrow component
    SystemAPI.RemoveComponent<PrimedThrow>(itemEntity);
    
    // Optional: Return to inventory/aggregate storehouse instead of hand
    // ReturnToStorehouse(itemEntity, playerEntity, ref state);
}
```

**Selection:**
- Raycast to primed item (physics query)
- Or "current selected primed index" in the queue

---

### 7. Make It Rewind/Fast-Forward Safe

**Treat all inputs as commands applied at fixed tick boundaries:**

```csharp
public enum PrimedThrowCommand : byte
{
    PrimeItem,
    ReleaseNext,
    ReleaseAll,
    ReleaseSelected,
    CancelPrime
}

/// <summary>
/// Command buffer for primed throw actions (rewind-safe).
/// </summary>
public struct PrimedThrowCommandBuffer : IBufferElementData
{
    public PrimedThrowCommand Command;
    public Entity TargetItem;      // For PrimeItem, ReleaseSelected, CancelPrime
    public float ImpulseMagnitude; // For PrimeItem
    public float3 AimDirection;    // For Release commands
    public uint CommandTick;       // Tick when command was issued
}
```

**Snapshot only:**
- Queue buffer (`PrimedThrowEntry` buffer)
- Each item's primed/held state (`PrimedThrow` component)
- Physics override flags (`PhysicsMassOverride`)

Because "primed" is kinematic + transform, it replays cleanly.

---

### Minimal MVP

**Primed state uses:**
- `PhysicsMassOverride` kinematic + `SetVelocityToZero` (hangs reliably)
- Queue buffer on player + slot formation around a `PrimeAnchor` (`Parent`/`LocalTransform`)
- Release applies `ApplyLinearImpulse`/`ApplyImpulse` and unfreezes
- Optional: collider filter swap while primed

---

## Related Documentation

- **Storehouse System:** `Docs/Concepts/Core/Storehouse_System_Summary.md` - Aggregate resource storage
- **Unity Physics Integration:** `Docs/Physics/PhysicsIntegrationGuide.md` - Unity Physics usage
- **Unity ECS Transforms:** https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/transforms.html
- **Unity Physics ApplyImpulse:** https://docs.unity.cn/Packages/com.unity.physics@1.0/api/Unity.Physics.PhysicsVelocity.html
- **Hand/Anchor Components:** `Docs/Concepts/Core/Hand_Anchor_Components_Summary.md` - Hand and anchor component reference

---

**For Implementers:** Focus on Phase 1 (basic pickup/throw) first, then Phase 2 (aggregate integration)  
**For Architects:** Review LOD strategy and budget limits for scalability  
**For Designers:** Understand siphon modes (physical vs aggregate) for gameplay feel

