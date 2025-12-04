# Power Network Integration Guide

This document describes how to integrate the power network system with game-specific systems.

## Godgame Integration

### Building Power Consumption

**Location**: `Godgame/Assets/Scripts/Godgame/...`

**Steps**:
1. Add `PowerConsumerDefId` field to building archetype definitions
2. In production/farming systems, read `PowerConsumerState.Supplied` and scale outputs:
   ```csharp
   var powerFactor = consumerState.Supplied / consumerDef.BaseDemand;
   var effectiveOutput = baseOutput * math.clamp(powerFactor, 0f, 1f);
   ```
3. Feed `AggregatePowerState.BlackoutLevel` into `VillageAmbientHappiness` modifiers:
   ```csharp
   var blackoutPenalty = aggregatePowerState.BlackoutLevel * -10f; // Example modifier
   ```
4. Village growth gates: require minimum `Coverage` for size progression (Hamlet → Village → Town)

### Systems to Modify

- `VillageSpatialGrowth`: Check `AggregatePowerState.Coverage` before allowing growth
- Production systems: Scale outputs by `PowerConsumerState.Supplied / BaseDemand`
- `VillageAmbientHappiness`: Apply blackout penalties

## Space4x Integration

### Platform Power Scaling

**Location**: `Space4x/Assets/Scripts/Space4x/...`

**Steps**:
1. Attach `AggregatePowerState` to platform entities (ships/stations)
2. Scale combat stats based on `Coverage`:
   - Engine thrust: `thrust *= Coverage`
   - Shield recharge: `rechargeRate *= Coverage`
   - Weapon cycle time: `cycleTime /= Coverage` (faster when well-powered)
   - Hangar throughput: `throughput *= Coverage`
   - Research lab output: `researchRate *= Coverage`
3. Module systems: Reactors become `PowerSourceState` entities
4. Combat systems: Read `Coverage` for readiness calculations

### Systems to Modify

- Combat systems: Scale engine/shield/weapon stats by `AggregatePowerState.Coverage`
- Module systems: Convert reactor modules to `PowerSourceState` entities
- Research systems: Scale research output by power surplus
- Life support: Scale crew survival/morale by power availability

## Example Integration Code

### Godgame Production Scaling

```csharp
// In production system
if (HasComponent<PowerConsumerState>(buildingEntity))
{
    var consumerState = GetComponent<PowerConsumerState>(buildingEntity);
    var consumerDef = GetConsumerDef(consumerState.ConsumerDefId);
    var powerFactor = consumerState.Supplied / consumerDef.BaseDemand;
    productionRate *= math.clamp(powerFactor, 0f, 1f);
}
```

### Space4x Combat Scaling

```csharp
// In combat system
if (HasComponent<AggregatePowerState>(shipEntity))
{
    var powerState = GetComponent<AggregatePowerState>(shipEntity);
    engineThrust *= powerState.Coverage;
    shieldRechargeRate *= powerState.Coverage;
    weaponCycleTime /= math.max(0.1f, powerState.Coverage);
}
```

