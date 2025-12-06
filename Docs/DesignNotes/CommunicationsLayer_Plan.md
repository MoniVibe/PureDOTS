# Causality-Aware Communications Layer - Implementation Plan

## Architecture Overview

Three-layer ECS communication system:
- **Body ECS** (Unity Entities): Physical carriers (birds, wagons, comm-ships) - 60 Hz
- **Comms ECS** (Unity Entities): Signal propagation (radio, tachyon beams) - 1 Hz  
- **Aggregate ECS** (Unity Entities): Information routing (fleets, HQs, spy networks) - 0.1 Hz

Integration with existing:
- **Mind ECS** (DefaultEcs): Reads KnowledgeState for decision-making
- **Body→Mind Sync**: KnowledgeState updates flow via AgentSyncBus

---

## Phase 1: Core Components (PureDOTS)

### 1.1 KnowledgeState Component
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/KnowledgeComponents.cs`

```csharp
public struct KnowledgeState : IComponentData
{
    public ulong LastEventId;      // Sequence number
    public float TimestampKnown;   // Local time of awareness
    public uint SourceId;          // Who told them (Entity index or GUID hash)
    public byte Confidence;        // 0-255 certainty
}
```

### 1.2 Message Component
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/MessageComponents.cs`

```csharp
public struct Message : IComponentData
{
    public int Origin;              // Entity index
    public int Destination;         // Entity index (-1 = broadcast)
    public float3 Position;         // Current position
    public float3 Velocity;         // Movement vector
    public float SignalStrength;    // 0-1, decays over time/distance
    public ulong EventId;          // WorldEvent sequence
    public float TimeOfArrival;     // Scheduled arrival time
    public byte MessageType;        // Enum: Runner, Radio, Spy, etc.
}

public enum MessageType : byte
{
    Runner = 0,      // 10-40 km/h, medium reliability
    Radio = 1,       // 0.1-1.0c, high reliability
    Tachyon = 2,     // Near-instant, high reliability
    Spy = 3,         // Variable speed, low reliability
    Wagon = 4        // Slow, medium reliability
}
```

### 1.3 WorldEvent Buffer
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/EventComponents.cs`

```csharp
[InternalBufferCapacity(16)]
public struct WorldEvent : IBufferElementData
{
    public ulong Id;                // Unique event ID (tick-based)
    public float3 Position;         // Event location
    public byte Type;               // EventType enum
    public float Timestamp;         // When it occurred
    public uint OriginEntity;       // Who/what caused it
}

public enum EventType : byte
{
    FleetDestroyed = 0,
    FortDestroyed = 1,
    ResourceDepleted = 2,
    EnemySpotted = 3,
    SupplyArrived = 4,
    // ... game-specific events
}
```

### 1.4 Message Pool Component
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/MessagePoolComponents.cs`

```csharp
public struct MessagePool : IComponentData
{
    // Managed wrapper holds NativeQueue<Entity> Free
    // Created via managed system, accessed via singleton
}

// Singleton tracking pool state
public struct MessagePoolState : IComponentData
{
    public int PoolSize;            // Current pool size
    public int FreeCount;            // Available messages
    public int ActiveCount;          // In-flight messages
}
```

---

## Phase 2: Communication Systems (PureDOTS)

### 2.1 EventSystem
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/Systems/EventSystem.cs`

- Emits WorldEvent when game events occur (fleet destroyed, fort destroyed)
- Burst-compiled, writes to singleton buffer
- Event ID = (tick << 32) | entityIndex (deterministic)

### 2.2 CommSourceSystem  
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/Systems/CommSourceSystem.cs`

- Converts WorldEvents into Message entities per comm channel
- Queries entities with CommSource component (radio towers, runners, spy networks)
- Creates messages from pool, sets origin/destination/velocity
- Runs in Body ECS (60 Hz) for physical carriers

### 2.3 CommsPropagationSystem
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/Systems/CommsPropagationSystem.cs`

- Updates Message entities each tick:
  - Moves position += velocity * dt
  - Applies signal decay: `signalStrength *= exp(-decayRate * dt)`
  - Checks arrival: `distance(Message.Position, Destination) < threshold`
- On arrival: queues KnowledgeUpdateEvent
- Runs in Comms ECS (1 Hz) for signals

### 2.4 KnowledgeUpdateSystem
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/Systems/KnowledgeUpdateSystem.cs`

- Processes KnowledgeUpdateEvents
- Updates KnowledgeState only if:
  - `incomingEventId > currentKnowledge.LastEventId`
  - `confidence > threshold`
- Writes to recipient entity's KnowledgeState component
- Runs in Aggregate ECS (0.1 Hz) for strategic updates

---

## Phase 3: Communication Graph (PureDOTS)

### 3.1 Cluster Communication Graph
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/ClusterCommsComponents.cs`

```csharp
public struct ClusterCommsBuffer : IBufferElementData
{
    public int TargetCluster;
    public float Delay;             // Propagation delay
    public float Reliability;       // 0-1 success probability
}

public struct ClusterCommsNode : IComponentData
{
    public int ClusterId;
    public float3 CenterPosition;
    public float Range;
}
```

**System**: `ClusterCommsBuildSystem`
- Aggregates entities within comm range into clusters
- Builds ClusterCommsBuffer links between clusters
- Reduces O(n²) routing to O(k) per cluster
- Runs in Aggregate ECS (0.1 Hz)

---

## Phase 4: Advanced Features (PureDOTS)

### 4.1 Signal Interference System
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/Systems/SignalInterferenceSystem.cs`

```csharp
public struct SignalInterferenceField : IComponentData
{
    public float JammingDensity;    // Local interference intensity
    public float3 Position;
    public float Range;
}
```

- Precomputed grid of jamming density
- Messages apply: `signalStrength *= exp(-jammingDensity * dt)`
- Spies use "anti-field" probability to intercept/modify

### 4.2 Information Decay & Falsification
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/Systems/KnowledgeDecaySystem.cs`

- Confidence decays: `confidence = exp(-distance / range) * reliability`
- Spies inject fake messages with modified SourceId
- Recipients use confidence-weighted logic

### 4.3 Time-Delay Scheduling
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/Systems/MessageSchedulerSystem.cs`

- Messages store `TimeOfArrival`
- Priority queue (NativeHeap) sorted by arrival time
- Systems advance simulation time, trigger events when `time >= arrival`
- O(log n) with priority queue vs O(n) per-frame checks

---

## Phase 5: Integration with Existing Systems

### 5.1 Mind ECS Integration
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Bridges/KnowledgeSyncSystem.cs`

- Reads KnowledgeState from Body ECS
- Syncs to Mind ECS via AgentSyncBus (existing bridge)
- Mind ECS decision systems query KnowledgeState for local awareness

### 5.2 Aggregate ECS Decision Layer
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/AI/Aggregate/KnowledgeAwareSystems.cs`

- Goal evaluation systems use local KnowledgeState
- Example: `if (knowledge.Confidence < 0.5f) stay put; else moveTo(reportedEnemyPos);`
- Natural misinformation behavior without AI scripting

### 5.3 Supply Chain Integration
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Logistics/KnowledgeAwareLogisticsSystem.cs`

- Supply entities query KnowledgeSnapshot
- `if (!knowledge.HasEvent(FleetDestroyed)) proceedTo(fleetPos); else reroute();`
- Knowledge flows: Fleets → Supply depots → Factories (Aggregate ECS path)

---

## Phase 6: Performance Optimizations

### 6.1 Message Pooling
- Pre-allocate 128-512 message entities
- Re-enqueue after delivery/expiration
- No destruction → fully Burst-safe, deterministic

### 6.2 Knowledge Diffusion Field (Optional)
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Comms/Systems/KnowledgeDiffusionSystem.cs`

- For large populations, skip message entities
- Treat information as scalar field diffusing through space
- `K_new = K + α * laplacian(K)`
- Used for planetary-scale rumor spreading

### 6.3 Latency-Driven Strategic Behavior
**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/AI/Aggregate/QuorumSystem.cs`

- Aggregate ECS schedules reactions only after quorum confirmation
- `if (ConfirmedReports(eventId) >= MinReports) enqueueStrategicResponse();`
- Strategic inertia emerges automatically

---

## Implementation Order

1. **Core Components** (Phase 1) - Foundation
2. **Basic Systems** (Phase 2.1-2.4) - Event → Message → Knowledge flow
3. **Message Pooling** (Phase 6.1) - Performance
4. **Cluster Graph** (Phase 3) - Scalability
5. **Integration** (Phase 5) - Connect to existing systems
6. **Advanced Features** (Phase 4, 6.2-6.3) - Polish

---

## File Structure

```
PureDOTS/Packages/com.moni.puredots/Runtime/Comms/
├── KnowledgeComponents.cs          # KnowledgeState
├── MessageComponents.cs             # Message, MessageType
├── EventComponents.cs                # WorldEvent, EventType
├── MessagePoolComponents.cs          # MessagePool, MessagePoolState
├── ClusterCommsComponents.cs         # ClusterCommsBuffer, ClusterCommsNode
├── Systems/
│   ├── EventSystem.cs                # Emits WorldEvents
│   ├── CommSourceSystem.cs           # Converts events to messages
│   ├── CommsPropagationSystem.cs     # Moves messages, applies decay
│   ├── KnowledgeUpdateSystem.cs      # Updates KnowledgeState
│   ├── ClusterCommsBuildSystem.cs    # Builds cluster graph
│   ├── SignalInterferenceSystem.cs   # Jamming/interference
│   ├── KnowledgeDecaySystem.cs       # Confidence decay
│   ├── MessageSchedulerSystem.cs     # Time-delay scheduling
│   └── KnowledgeDiffusionSystem.cs   # Optional field diffusion
└── Bridges/
    └── KnowledgeSyncSystem.cs        # Body→Mind knowledge sync
```

---

## Determinism & Replay

- All messages seeded from world tick + origin GUID
- Information web replays identically under deterministic scheduling
- Late arrivals, false info, decay are all reproducible
- Message IDs: `(tick << 32) | entityIndex` (deterministic)

---

## Performance Targets

- Message propagation: < 0.5ms per 1000 messages
- Knowledge updates: < 0.2ms per 1000 updates
- Cluster graph build: < 1ms per rebuild (0.1 Hz)
- Total comms overhead: < 2ms per frame (aggregated)

---

## Testing Strategy

1. **Unit Tests**: Message creation, propagation math, confidence decay
2. **Integration Tests**: Event → Message → Knowledge flow
3. **Determinism Tests**: Replay identical runs with same seed
4. **Performance Tests**: 10k messages, 1k knowledge states, cluster graph

