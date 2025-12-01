# Extension Request: World State & Transformation System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Godgame (Space4X could use for galactic events)  
**Priority**: P3  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/WorldState/WorldStateComponents.cs` - WorldStateType, WorldState, ApocalypseTrigger, BiomeTransformation, TransformationWave, CounterApocalypseSpawn, SpawnTableOverride
- `Packages/com.moni.puredots/Runtime/Runtime/WorldState/WorldStateHelpers.cs` - Static helpers for apocalypse triggers, wave expansion, counter-faction spawning

---

## Use Case

World-scale transformations are needed for:

**Godgame:**
- Apocalypse states (Demon Hellscape, Undead Wasteland, etc.)
- World boss victory conditions
- Biome conversion cascades
- Counter-apocalypse spawning
- "Rebirth" cycles

**Space4X (Potential):**
- Galactic-scale events (supernova chains, warp storms)
- Faction total victory states
- Sector-wide transformations

---

## Proposed Components

```csharp
// === World State ===
public enum WorldStateType : byte
{
    Normal = 0,
    DemonHellscape = 1,
    UndeadWasteland = 2,
    FrozenEternity = 3,
    VerdantOvergrowth = 4,
    CelestialDominion = 5,
    VoidCorruption = 6
    // Extensible
}

public struct WorldState : IComponentData
{
    public WorldStateType CurrentState;
    public WorldStateType PreviousState;
    public uint StateChangedTick;
    public float TransitionProgress;     // 0-1 during transitions
    public Entity DominantFaction;       // Who caused this state
}

// === Victory Conditions ===
public struct ApocalypseTrigger : IComponentData
{
    public Entity FactionEntity;         // World boss faction
    public float TerritoryControl;       // 0-1 percent of world
    public uint CivilizationsDestroyed;
    public uint UnchallengeTicks;        // How long unchallenged
    public bool TriggerReady;            // Met victory conditions
}

public struct ApocalypseTriggerConfig : IComponentData
{
    public float TerritoryThreshold;     // 0.8 = 80% control
    public uint UnchallengeDuration;     // Ticks to wait
    public bool RequireAllCivilizationsDestroyed;
}

// === Biome Transformation ===
public struct BiomeTransformation : IBufferElementData
{
    public Entity BiomeEntity;
    public byte OriginalBiomeType;
    public byte TargetBiomeType;
    public float TransformProgress;      // 0-1
    public uint StartTick;
}

public struct TransformationWave : IComponentData
{
    public float3 EpicenterPosition;
    public float CurrentRadius;
    public float ExpansionRate;          // Units per tick
    public float MaxRadius;
    public WorldStateType TransformTo;
}

// === Counter-Apocalypse ===
public struct CounterApocalypseSpawn : IComponentData
{
    public WorldStateType ApocalypseType;
    public FixedString32Bytes CounterFaction; // "Angels" vs demons
    public float SpawnProbability;       // Per check interval
    public uint MinTicksBeforeSpawn;     // Grace period
    public bool HasSpawned;
}

// === Spawn Table Swap ===
public struct SpawnTableOverride : IComponentData
{
    public WorldStateType ForState;
    public FixedString64Bytes SpawnTableId; // Different creatures/resources
    public bool OverrideActive;
}

// === Configuration ===
public struct WorldTransformConfig : IComponentData
{
    public uint BiomeTransformDuration;  // Ticks per biome
    public float WaveExpansionRate;
    public uint CounterSpawnCheckInterval;
    public bool AllowRebirthCycles;      // Can world return to normal?
}
```

### New Systems
- `ApocalypseTriggerSystem` - Monitors victory conditions
- `WorldStateTransitionSystem` - Handles state changes
- `BiomeTransformationSystem` - Converts biomes over time
- `TransformationWaveSystem` - Spreads transformation from epicenter
- `CounterApocalypseSystem` - Spawns opposing forces
- `SpawnTableSwapSystem` - Changes what spawns based on state

---

## Example Usage

```csharp
// === World boss achieves victory ===
var trigger = EntityManager.GetComponentData<ApocalypseTrigger>(demonBoss);
if (trigger.TerritoryControl >= 0.8f)
{
    // ApocalypseTriggerSystem sets trigger.TriggerReady = true
    // WorldStateTransitionSystem changes state to DemonHellscape
}

// === Biome conversion cascade ===
// TransformationWaveSystem expands from demon fortress
// Normal biomes become "Scorched", "Lava", "Ash"
// Vegetation dies, structures corrupt

// === Counter-apocalypse spawns ===
// After 1 game-year, CounterApocalypseSystem rolls for angel invasion
// Celestial portals open, angelic forces arrive to purge demons
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/WorldState/` directory
- Integration: Biome system, spawn system, faction system

**Breaking Changes:** None - new system

---

## Review Notes

*(PureDOTS team use)*

