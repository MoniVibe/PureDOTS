# Extension Request: Space4X Stat Simulation Support

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Space4X (with Godgame applicability)  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Space4X requires PureDOTS engine features to fully simulate its stat-driven progression system. Individual entities (captains, officers, crew) have rich stat profiles that influence gameplay systems.

**Space4X Stats:**
- **IndividualStats**: Command, Tactics, Logistics, Diplomacy, Engineering, Resolve (0-100)
- **PhysiqueFinesseWill**: Physical attributes with inclinations
- **ExpertiseEntry Buffer**: CarrierCommand, Espionage, Logistics, Psionic, Beastmastery tiers
- **ServiceTrait Buffer**: ReactorWhisperer, StrikeWingMentor, TacticalSavant, etc.

**Godgame Overlap**: Similar stat structure for villagers (Strength, Agility, Will, profession skills)

---

## Required Features

### 1. Scenario Runner Stat Seeding (P1)

**Requirement**: Ability to seed entity stats from scenario JSON.

**Current Gap**: ScenarioRunner spawns entities but doesn't support stat initialization.

**Proposed API**:

```json
{
  "entities": {
    "captain_01": {
      "archetype": "Space4X.Captain",
      "stats": {
        "command": 75,
        "tactics": 60,
        "logistics": 50,
        "diplomacy": 80,
        "engineering": 45,
        "resolve": 70
      },
      "physique": {
        "physique": 65,
        "finesse": 70,
        "will": 55
      },
      "expertise": [
        {"type": "CarrierCommand", "tier": 5},
        {"type": "Logistics", "tier": 3}
      ],
      "traits": ["ReactorWhisperer", "TacticalSavant"]
    }
  }
}
```

**Implementation:**
- Extend `ScenarioRunner` spawner to accept stat dictionaries
- Map JSON keys to component fields
- Support buffer initialization (expertise, traits)

---

### 2. Registry Continuity for Stat Progression (P1)

**Requirement**: Stat progression must be rewind-compatible.

**Current Gap**: XP pools and stat modifications need deterministic replay.

**Proposed Components:**

```csharp
/// <summary>
/// Records stat changes for rewind replay.
/// </summary>
[InternalBufferCapacity(16)]
public struct StatHistorySample : IBufferElementData
{
    public uint Tick;
    public half Command;
    public half Tactics;
    public half Logistics;
    public half Diplomacy;
    public half Engineering;
    public half Resolve;
    public float GeneralXP;
}

/// <summary>
/// Command log entry for XP changes.
/// </summary>
public struct StatXPCommandLogEntry : IBufferElementData, ICommandLogEntry
{
    public uint Tick;
    public Entity TargetEntity;
    public StatType StatType;
    public float XPAmount;
    public StatXPChangeType ChangeType;
}

public enum StatXPChangeType : byte
{
    Gain, Spend, Reset, Transfer
}
```

---

### 3. Stat Aggregation Queries (P2)

**Requirement**: Efficiently query aggregate stats across entity groups.

**Current Gap**: No built-in support for fleet/group stat aggregation.

**Proposed Solution:**

```csharp
/// <summary>
/// Aggregated stats for a group (fleet, colony, etc.)
/// </summary>
public struct GroupStatAggregate : IComponentData
{
    public half AvgCommand;
    public half MaxCommand;
    public half MinCommand;
    public half AvgTactics;
    public half MaxTactics;
    public half MinTactics;
    // ... other stats
    public byte MemberCount;
}

/// <summary>
/// System that updates group aggregates periodically.
/// </summary>
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct StatAggregationSystem : ISystem
{
    // Updates GroupStatAggregate from member stats
    // Configurable update frequency to manage performance
}
```

---

### 4. Stat Influence Telemetry (P2)

**Requirement**: Track how stats affect gameplay outcomes.

**Proposed Telemetry Keys:**
```
// Influence metrics
space4x.stats.commandInfluence.formationRadius
space4x.stats.tacticsInfluence.targetingAccuracy
space4x.stats.logisticsInfluence.transferSpeed
space4x.stats.engineeringInfluence.repairSpeed
space4x.stats.resolveInfluence.engagementTime

// Modifier tracking
space4x.stats.modifiers.{entityId}.{statType}.base
space4x.stats.modifiers.{entityId}.{statType}.modified
space4x.stats.modifiers.{entityId}.{statType}.source
```

---

### 5. Rewind Determinism Guarantees (P1)

**Requirement**: Stat calculations must replay identically.

**Implementation Notes:**
- Use `half` types for stats (already done) to reduce precision issues
- Ensure stat lookups are deterministic (entity order, component access)
- Apply stat modifiers in consistent order
- Log stat calculations to command log for verification

---

## Impact Assessment

**Files/Systems Affected:**
- Extend: `ScenarioRunner` for stat seeding
- New: `StatAggregationSystem` for group queries
- Extend: Registry snapshot to include stat state
- Extend: Telemetry system for stat influence metrics

**Breaking Changes:** None - additive features

---

## Example Usage

```csharp
// === Scenario: Seed captain with specific stats ===
// In scenario JSON:
{
  "captain": {
    "stats": {"command": 80, "tactics": 70},
    "expertise": [{"type": "CarrierCommand", "tier": 6}]
  }
}

// === System: Query group stats ===
var fleetStats = EntityManager.GetComponentData<GroupStatAggregate>(fleetEntity);
float avgCommand = (float)fleetStats.AvgCommand;
float commandBonus = avgCommand * 0.01f; // 1% per command point

// === Telemetry: Track stat influence ===
TelemetryStream.Emit("space4x.stats.commandInfluence.formationRadius", 
    baseRadius * (1f + commandBonus));

// === Rewind: Verify stat determinism ===
var history = EntityManager.GetBuffer<StatHistorySample>(captainEntity);
// History can be compared across rewind passes to verify determinism
```

---

## Priority Summary

| Feature | Priority | Blocking |
|---------|----------|----------|
| Scenario Stat Seeding | P1 | Yes - testing |
| Registry Continuity | P1 | Yes - rewind |
| Rewind Guarantees | P1 | Yes - determinism |
| Stat Aggregation | P2 | No |
| Stat Telemetry | P2 | No |

---

## Reference

**Detailed Spec**: `Space4X/Docs/PureDOTS_Request_Space4xStats.md`

**Space4X Stat Components**:
- `Assets/Scripts/Space4x/Registry/ModuleDataSchemas.cs`
- `Assets/Scripts/Space4x/Systems/AI/*`

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

