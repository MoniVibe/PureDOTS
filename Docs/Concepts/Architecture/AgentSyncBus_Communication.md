# AgentSyncBus Communication Framework

**Status:** Design Document  
**Category:** Core Architecture  
**Scope:** PureDOTS Foundation Layer  
**Created:** 2025-12-07  
**Last Updated:** 2025-12-07

---

## Purpose

`AgentSyncBus` is the **single communication channel** between Body/Mind/Aggregate ECS worlds. It provides batched, delta-compressed message queues with GUID-based identity. All cross-world communication must go through the bus; direct world queries are forbidden.

**Key Principle:** Burst code collects data in native containers; managed systems enqueue into bus. Never access the bus from Burst-compiled code.

---

## Core Concept: Message Bus Architecture

**Bus Authority:** Created and owned by `AgentSyncBridgeCoordinator` (managed only)  
**Identity Model:** All messages carry `AgentGuid`; no `Entity` handles cross layers  
**Cadence:** Body→Mind sync ~100 ms; Mind→Body sync ~250 ms (configurable but must be consistent per session)  
**Compression:** Delta-compressed batches to keep sync cost < 3 ms/frame  
**Access Pattern:** Burst code writes to temp native containers; managed wrapper enqueues into bus queues

---

## Message Types & Queues

| Message | Purpose | Direction | Queue Notes |
|---------|---------|-----------|-------------|
| `MindToBodyMessage` | Intents from cognition to simulation | Mind → Body | Batched per Mind→Body interval |
| `BodyToMindMessage` | State updates from simulation | Body → Mind | Batched per Body→Mind interval; delta-compressed |
| `Percept` | Sensor readings | Body → Mind | Batched with Body→Mind |
| `LimbCommand` | Limb activations | Mind → Body | Batched per Mind→Body interval |
| `AggregateIntentMessage` | Group-level intents | Aggregate → Mind | Batched per Aggregate tick |
| `ConsensusVoteMessage` | Voting payloads | Any → Aggregate/Mind | Batched; resolved to `ConsensusOutcomeMessage` |
| `ConsensusOutcomeMessage` | Resolved votes | Aggregate/Mind → Agents | Batched; consumed by cognitive systems |

---

## Message Structure

### Base Message Pattern

All messages follow this pattern:

```csharp
public struct MindToBodyMessage : IComponentData
{
    public AgentGuid AgentGuid;      // Cross-world identity
    public byte IntentType;          // Game-defined enum
    public float3 TargetPosition;   // Intent target
    public uint TickNumber;          // Timestamp for ordering
    public byte DeltaFlags;           // Which fields changed (for compression)
}
```

### AgentGuid (Cross-World Identity)

```csharp
public struct AgentGuid : IEquatable<AgentGuid>
{
    public ulong High;
    public ulong Low;
    
    // Burst-safe GUID wrapper
    // Used across all three worlds as the only cross-layer identifier
}
```

### Delta Compression

Body→Mind messages use delta flags per field; unchanged fields are skipped:

```csharp
[Flags]
public enum BodyToMindDeltaFlags : byte
{
    None = 0,
    PositionChanged = 1 << 0,
    HealthChanged = 1 << 1,
    VelocityChanged = 1 << 2,
    // ... more flags
}
```

---

## Access Patterns

### Getting the Bus

```csharp
// Managed systems only
var coordinator = World.DefaultGameObjectInjectionWorld
    .GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
var bus = coordinator.GetBus();
```

### Enqueueing from Managed Systems

```csharp
// Burst job collects data
[BurstCompile]
public void CollectStateData(
    NativeList<BodyToMindMessage> tempMessages,
    // ... other params
)
{
    // Write to temp native list
    tempMessages.Add(new BodyToMindMessage { ... });
}

// Managed wrapper enqueues
public void OnUpdate(ref SystemState state)
{
    var tempMessages = new NativeList<BodyToMindMessage>(Allocator.Temp);
    
    // Run Burst job to collect data
    var job = new CollectStateDataJob { TempMessages = tempMessages };
    job.ScheduleParallel(query, state.Dependency).Complete();
    
    // Enqueue from managed code
    var bus = GetBus();
    foreach (var msg in tempMessages)
    {
        bus.EnqueueBodyToMindMessage(msg);
    }
    
    tempMessages.Dispose();
}
```

### Reading Batches

```csharp
// Managed systems consume batches
public void OnUpdate(ref SystemState state)
{
    var bus = GetBus();
    var batch = bus.DequeueMindToBodyBatch(Allocator.Temp);
    
    foreach (var msg in batch)
    {
        // Process message
        ApplyIntent(msg);
    }
    
    batch.Dispose();
}
```

---

## Lifecycle & Ordering

### System Execution Order

Bridge systems run in `SimulationSystemGroup` in this order:

1. **`AgentMappingSystem`** - Links Body `AgentSyncId` to Mind/Aggregate indices
2. **`BodyToMindSyncSystem`** - Gathers state/percepts, enqueues into bus (managed wrapper)
3. **`MindToBodySyncSystem`** - Pulls cognitive outputs, enqueues intents/commands
4. **`IntentResolutionSystem`** - Applies resolved intents to Body ECS

### Initialization Flow

1. **Bootstrap:** `AgentSyncBridgeCoordinator` constructs the bus during world bootstrap
2. **Mapping:** `AgentMappingSystem` links Body `AgentSyncId` to Mind/Aggregate indices
3. **Body→Mind:** `BodyToMindSyncSystem` gathers state/percepts, enqueues into bus
4. **Mind→Body:** `MindToBodySyncSystem` pulls cognitive outputs, enqueues intents
5. **Intent Resolution:** `IntentResolutionSystem` applies resolved intents to Body ECS

---

## Delta Compression & Ordering

- **Delta flags:** Body→Mind uses delta flags per field; unchanged fields are skipped
- **Message ordering:** Messages are ordered by `TickNumber` inside each batch; do not rely on global ordering across queues
- **Tick monotonicity:** Keep `TickNumber` monotonic per direction; bridge systems stamp ticks if producer omits them

---

## Extension Rules (New Message Types)

When adding a new message type:

1. **Define struct** with `AgentGuid` + payload + `TickNumber` (and any flags for delta semantics)
2. **Add queue & APIs** to `AgentSyncBus` (`EnqueueX`, `DequeueXBatch`, count property)
3. **Bridge integration:** Update relevant bridge system to produce/consume the new batch and wire into cadence
4. **Performance check:** Profile bus cost after adding; respect < 3 ms/frame target

### Example: Adding a New Message Type

```csharp
// 1. Define message struct
public struct CustomMessage : IComponentData
{
    public AgentGuid AgentGuid;
    public byte CustomData;
    public uint TickNumber;
}

// 2. Add to AgentSyncBus
public class AgentSyncBus
{
    private NativeQueue<CustomMessage> _customQueue;
    
    public void EnqueueCustomMessage(CustomMessage msg)
    {
        _customQueue.Enqueue(msg);
    }
    
    public NativeList<CustomMessage> DequeueCustomBatch(Allocator allocator)
    {
        var batch = new NativeList<CustomMessage>(allocator);
        while (_customQueue.TryDequeue(out var msg))
        {
            batch.Add(msg);
        }
        return batch;
    }
}

// 3. Wire into bridge system
public partial struct CustomSyncSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var bus = GetBus();
        var batch = bus.DequeueCustomBatch(Allocator.Temp);
        // Process batch...
        batch.Dispose();
    }
}
```

---

## Performance Targets

- **Sync overhead:** < 3 ms/frame for all bus processing
- **Message batching:** Batch messages per sync interval (100ms Body→Mind, 250ms Mind→Body)
- **Delta compression:** Only sync changed fields to reduce message volume
- **Memory:** Efficient GUID storage, batched message queues

---

## Troubleshooting Quick Checks

- **Bus null?** Ensure `AgentSyncBridgeCoordinator` exists in the active world
- **Missing Mind link?** Verify `AgentSyncId.MindEntityIndex >= 0` after mapping
- **Messages not arriving?** Confirm producer enqueues from managed code and cadence intervals are not paused
- **Burst errors?** Ensure Burst code only writes to temp containers; enqueuing must happen in managed systems

---

## Anti-Patterns (Do Not Do)

- Accessing `AgentSyncBus` inside Burst jobs (collect data in jobs, enqueue in managed systems)
- Passing managed references or UnityEngine objects through bus messages
- Using `Entity` identifiers across layers (always use `AgentGuid`)
- Direct world queries across layers (use bus messages only)
- Skipping delta compression for high-frequency messages

---

## Integration Requirements

### For PureDOTS Developers

1. **Create bus in bootstrap** - `AgentSyncBridgeCoordinator` creates bus during world initialization
2. **Implement bridge systems** - Create `BodyToMindSyncSystem`, `MindToBodySyncSystem`, `AgentMappingSystem`
3. **Respect access patterns** - Burst collects, managed enqueues
4. **Profile sync cost** - Keep total bus processing under 3 ms/frame

### For Game-Specific Developers

1. **Enqueue from managed code** - Never access bus from Burst-compiled systems
2. **Use GUID identity** - All messages must carry `AgentGuid`
3. **Batch messages** - Collect multiple messages per sync interval
4. **Respect cadence** - Don't enqueue more frequently than sync intervals

---

## Related Documentation

- **Three Pillar ECS Architecture:** `Docs/Architecture/ThreePillarECS_Architecture.md` - Layer responsibilities and communication rules
- **Multi-ECS Integration Guide:** `Docs/Guides/MultiECS_Integration_Guide.md` - Integration cookbook and system ordering
- **Foundation Guidelines:** `Docs/FoundationGuidelines.md` - Burst and determinism constraints (P0–P25 patterns)
- **AgentSyncBus Specification:** `Docs/Architecture/AgentSyncBus_Specification.md` - Complete API reference

---

**For Implementers:** The bus is managed-only. Burst code collects data in native containers; managed systems enqueue. All messages use `AgentGuid` for identity. Keep sync cost under 3 ms/frame.

**For Designers:** Think of the bus as a message queue between worlds. Messages are batched and delta-compressed. Never query across worlds directly; always use the bus.

---

**Last Updated:** 2025-12-07  
**Status:** Design Document - Foundation Layer Architecture

