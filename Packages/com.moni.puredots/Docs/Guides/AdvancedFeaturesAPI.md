# Advanced Features API Reference

**Last Updated**: 2025-01-27

Quick reference for API signatures and component structures.

---

## Scheduling

### JobDependencyAttribute
```csharp
[JobDependency(typeof(DependencySystem), DependencyType.After)]
public partial struct MySystem : ISystem { }
```

### SystemBudget
```csharp
public struct SystemBudget : IComponentData
{
    public float CostMs;      // Estimated cost per execution
    public byte Priority;      // 0-255, higher = more important
    public float MaxMs;        // Max allowed execution time
}
```

---

## Time & Profiling

### TelemetryStream (Extended)
```csharp
public struct TelemetryStream : IComponentData
{
    public uint Version;
    public uint LastTick;
    public float RealTimeMs;           // Real time elapsed
    public float SimTimeMs;            // Simulation time elapsed
    public float CompressionFactor;    // ΔSim / ΔReal
    public float DriftMs;              // Drift across worlds
}
```

### TimeCoordinator
```csharp
public struct TimeCoordinator : IComponentData
{
    public float TimeScale;    // 1.0 = normal, 10.0 = 10x speed
    public byte WorldId;       // World identifier (0-255)
    public bool IsFrozen;      // Whether time is frozen
}
```

---

## AI Decision Recording

### DecisionEvent
```csharp
public struct DecisionEvent
{
    public ulong Agent;        // Agent GUID
    public byte Type;          // Decision type enum
    public float Utility;      // Utility score
    public uint Tick;          // Tick when decision was made
    public FixedBytes64 Context;
}
```

### DecisionEventBufferHelper
```csharp
// Add event
DecisionEventBufferHelper.AddEvent(ref buffer, ref registry, decision);

// Get event at tick
bool found = DecisionEventBufferHelper.TryGetEventAtTick(
    buffer, registry, tick, out DecisionEvent evt);

// Clear old events
DecisionEventBufferHelper.ClearEventsBeforeTick(ref buffer, ref registry, minTick);
```

### AIDecisionDebugAPI
```csharp
// Get decisions at tick
bool found = AIDecisionDebugAPI.TryGetAgentDecisionsAtTick(
    entityManager, registryEntity, agentGuid, tick, out events);

// Get decisions in range
AIDecisionDebugAPI.GetAgentDecisionsInRange(
    entityManager, registryEntity, agentGuid, startTick, endTick, out events);

// Scrub decisions
AIDecisionDebugAPI.ScrubAgentDecisions(
    entityManager, registryEntity, agentGuid, currentTick, deltaTicks, out events);
```

---

## Math Kernel

### MathKernel (Static Methods)
```csharp
// Vector ops
float3 NormalizeSafe(in float3 v, float epsilon = 1e-8f)
float Distance(in float3 a, in float3 b)
float3 Lerp(in float3 a, in float3 b, float t)
float3 Slerp(in float3 a, in float3 b, float t)

// Quaternion ops
quaternion QuaternionFromEuler(float3 euler)
quaternion QuaternionLookRotation(in float3 forward, in float3 up)
quaternion QuaternionSlerp(in quaternion a, in quaternion b, float t)

// Interpolation
float SmoothStep(float edge0, float edge1, float x)
float EaseInOut(float t)

// Noise (deterministic)
float Noise1D(float x, uint seed = 0)
float Noise2D(float2 p, uint seed = 0)
float Noise3D(float3 p, uint seed = 0)

// Random (deterministic)
uint NextRandom(ref uint state)
float RandomFloat(ref uint state)
float RandomFloatRange(ref uint state, float min, float max)
int RandomIntRange(ref uint state, int min, int max)
```

### MathConstants
```csharp
// Gravitational
public const float GravitationalConstant;
public const float EarthGravity;
public const float StandardGravity;

// Atmospheric
public const float StandardAtmosphericPressure;
public const float AirDensitySeaLevel;
public const float GasConstant;
public const float LapseRate;

// Orbital
public const float AstronomicalUnit;
public const float SolarMass;
public const float EarthMass;
```

---

## Replay Framework

### ReplayService
```csharp
public struct ReplayService
{
    void WriteCommand(uint tick, byte commandType, NativeArray<byte> commandData);
    void WriteTickHash(uint tick, ulong hash);
    bool TryGetTickHash(uint tick, out ulong hash);
    NativeArray<byte> GetCommandLog();
}
```

### ReplayJumpAPI
```csharp
bool JumpToTick(ref SystemState state, uint targetTick, in ReplayMetadata metadata);
uint GetCurrentTick(ref SystemState state);
bool ValidateReplay(NativeHashMap<uint, ulong> tickHashes, in ReplayMetadata metadata);
```

---

## Spatial Queries

### SpatialQueryService
```csharp
public struct SpatialQueryService
{
    void RegisterEntity(int3 cellCoords, Entity entity);
    void UnregisterEntity(int3 cellCoords, Entity entity);
    void QueryCell(int3 cellCoords, ref NativeList<Entity> results);
    void QueryAABB(in AABB bounds, ref NativeList<Entity> results);
}
```

### SpatialDomainHelper
```csharp
void RegisterDomain(EntityManager entityManager, Entity managerEntity,
    FixedString64Bytes domainName, float cellSize);
bool TryFindDomain(EntityManager entityManager, FixedString64Bytes domainName,
    out Entity domainEntity);
```

---

## Tuning Profiles

### TuningProfileLoader
```csharp
BlobAssetReference<TuningProfileBlob> LoadFromJson(string jsonPath);
void HotReloadProfile(EntityManager entityManager, Entity profileEntity, string jsonPath);
```

### TuningProfileBlob
```csharp
public struct TuningProfileBlob
{
    public BlobString ProfileName;
    public BlobString Domain;
    public BlobArray<TuningParameter> Parameters;
}

public struct TuningParameter
{
    public BlobString Name;
    public float Value;
    public byte Type; // 0=float, 1=int, 2=bool
}
```

---

## Physics

### KeplerianOrbit
```csharp
void CalculateOrbitalState(
    float semiMajorAxis, float eccentricity, float inclination,
    float argumentOfPeriapsis, float longitudeOfAscendingNode,
    float meanAnomaly, float gravitationalParameter,
    out float3 position, out float3 velocity);
```

### RK2Integration
```csharp
void Integrate(in float3 position, in float3 velocity, in float3 acceleration,
    float deltaTime, out float3 newPosition, out float3 newVelocity);
void IntegrateRotation(in quaternion rotation, in float3 angularVelocity,
    float deltaTime, out quaternion newRotation);
```

---

## World Bus

### WorldBusRouter
```csharp
void RouteMessages(ref WorldBus bus, byte currentWorldId, ref DynamicBuffer<WorldMessage> targetBuffer);
void SendMessage(ref WorldBus bus, byte sourceWorld, byte targetWorld,
    FixedBytes64 payload, uint tick, byte messageType);
```

### WorldMessage
```csharp
public struct WorldMessage : IBufferElementData
{
    public byte SourceWorld;
    public byte TargetWorld;
    public FixedBytes64 Payload;
    public uint Tick;
    public byte MessageType;
}
```

---

## Binary Serialization

### BinarySerialization
```csharp
void Serialize<T>(in T value, ref NativeList<byte> output) where T : unmanaged;
bool Deserialize<T>(NativeArray<byte> input, int offset, out T value) where T : unmanaged;
void EnsureLittleEndian(ref NativeArray<byte> data);
```

---

## Performance Benchmarks

### BenchmarkMetrics
```csharp
public struct BenchmarkMetrics
{
    void RecordSystemGroupTime(FixedString64Bytes groupName, float ms);
    NativeHashMap<FixedString64Bytes, float> MeanMsPerGroup;
    NativeHashMap<FixedString64Bytes, float> MaxMsPerGroup;
    NativeHashMap<FixedString64Bytes, long> MemoryAllocations;
    NativeHashMap<FixedString64Bytes, int> BurstJobCounts;
    NativeHashMap<FixedString64Bytes, int> EntityCountsPerArchetype;
}
```

### BaselineComparison
```csharp
bool CompareMetrics(in BenchmarkMetrics current, in BenchmarkMetrics baseline,
    float regressionThreshold, out NativeList<FixedString128Bytes> regressions);
```

### PerfRunner
```csharp
// CLI entry point
public static void Run();
// Usage: Unity -batchmode -executeMethod PureDOTS.Runtime.Devtools.PerfRunner.Run --ticks 10000
```

---

## Component Structures

### DecisionEventRegistry
```csharp
public struct DecisionEventRegistry : IComponentData
{
    public int WriteIndex;
    public int Capacity;
    public int Count;
}
```

### ReplayMetadata
```csharp
public struct ReplayMetadata : IComponentData
{
    public uint StartTick;
    public uint EndTick;
    public ulong Hash;
    public int CommandCount;
    public uint Version;
    public FixedString64Bytes PhysicsProfile;
    public FixedString64Bytes AIProfile;
    public FixedString64Bytes EconomyProfile;
}
```

### WorldBusState
```csharp
public struct WorldBusState : IComponentData
{
    public byte WorldId;
    public int MessageCount;
}
```

### WorldPartition
```csharp
public struct WorldPartition : IComponentData
{
    public int3 CellCoords;
    public byte AuthorityId;
    public bool IsLocal;
}
```

