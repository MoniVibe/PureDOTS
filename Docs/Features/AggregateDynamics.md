# Aggregate & Individual Dynamics

**Status**: Tier-1 Implementation Complete  
**Category**: Social Simulation / Group Dynamics  
**Scope**: Shared PureDOTS module used by Godgame (villages/guilds/bands) and Space4X (fleets/crews/empires)

---

## Overview

The Aggregate & Individual Dynamics system provides a game-agnostic framework for bidirectional influence between individuals and groups. Individuals shape group averages through their traits, and groups create ambient conditions that bias individual behavior.

### Key Features

- **Bidirectional Influence**: Individuals → group averages, groups → ambient pressure on individuals
- **Data-Driven Aggregation**: Moddable rules for how traits aggregate into ambient conditions
- **Cascade Effects**: Composition changes trigger recalculation and ambient updates
- **Motivation Integration**: Aggregates have their own group-level ambitions via Motivation system
- **Adapter Pattern**: Bridges existing game entities (Village, Band, Fleet) to generic aggregate system

---

## Core Components

### Individual Components

#### `MoralProfile` (Extended)
Located in `PureDOTS.Runtime.Motivation.MotivationComponents`.

Extended with social traits:
- `byte Initiative` (0-100): Likelihood to take initiative
- `byte Ambition` (0-100): Intensity of goal pursuit
- `float DesireStatus` (0-1): Desire for status/recognition
- `float DesireWealth` (0-1): Desire for material resources
- `float DesirePower` (0-1): Desire for influence
- `float DesireKnowledge` (0-1): Desire for understanding

Existing alignment traits (CorruptPure, ChaoticLawful, EvilGood, VengefulForgiving, CravenBold, MightMagic) remain unchanged.

#### `GroupMembership`
Located in `PureDOTS.Runtime.Aggregate.GroupMembershipComponents`.

Links individuals to groups:
- `Entity Group`: Primary group entity (village, guild, fleet, etc.)
- `byte Role`: Data-driven role index (0 = member, 1 = leader, etc.)

### Aggregate Components

#### `AggregateIdentity`
Located in `PureDOTS.Runtime.Aggregate.AggregateComponents`.

Identifies aggregate entities:
- `ushort TypeId`: Data-driven type ID (Village, Guild, Fleet, etc.)
- `uint Seed`: Seed for per-group randomization (names, quirks)

#### `AggregateStats`
Located in `PureDOTS.Runtime.Aggregate.AggregateComponents`.

Statistics averaged from member traits:
- Averaged MoralProfile traits (Initiative, VengefulForgiving, BoldCraven, etc.)
- Desire coverage (StatusCoverage, WealthCoverage, PowerCoverage, KnowledgeCoverage)
- `int MemberCount`: Number of members
- `uint LastRecalcTick`: Last recalculation tick

#### `AmbientGroupConditions`
Located in `PureDOTS.Runtime.Aggregate.AmbientConditionsComponents`.

Ambient conditions derived from aggregate stats:
- Pressures: AmbientCourage, AmbientCaution, AmbientAnger, AmbientCompassion, AmbientDrive
- Expectations: ExpectationLoyalty, ExpectationConformity, ToleranceForOutliers
- `uint LastUpdateTick`: Last update tick

### Configuration

#### `AggregateConfigCatalog`
Located in `PureDOTS.Runtime.Aggregate.AggregateConfigCatalog`.

Blob asset catalog containing:
- `AggregateTypeConfig[]`: Configurations per aggregate type
- `AggregateAggregationRule[]`: Rules mapping source traits → target metrics

#### `AggregateConfigState`
Singleton component holding:
- `BlobAssetReference<AggregateConfigCatalog> Catalog`: Global catalog reference
- `uint AmbientUpdateFrequency`: Frequency of ambient condition updates in ticks

---

## Systems

### Core Systems

#### `GroupMembershipChangeSystem`
**Purpose**: Detect when group composition changes and mark aggregates as dirty.

**Behavior**:
- Watches `GroupMembership` component
- Marks affected groups with `AggregateStatsDirtyTag`
- Respects RewindState (only processes in Record mode)

#### `AggregateStatsRecalculationSystem`
**Purpose**: Recompute group averages when dirty.

**Behavior**:
- Queries groups with `AggregateStatsDirtyTag`
- Averages `MoralProfile` traits from all members
- Writes to `AggregateStats`
- Removes dirty tag

**Optimization**: For large worlds, consider incremental aggregation (maintain running sums) in Tier-2.

#### `AmbientConditionsUpdateSystem`
**Purpose**: Convert `AggregateStats` into `AmbientGroupConditions` using data-driven rules.

**Behavior**:
- Runs at configurable frequency (default: every 100 ticks)
- Fetches `AggregateTypeConfig` via `AggregateIdentity.TypeId`
- Applies aggregation rules to derive ambient metrics
- Respects RewindState

#### `IndividualAmbientResponseSystem`
**Purpose**: Apply ambient group pressure to individuals.

**Behavior**:
- Runs at configurable frequency
- Compares individual traits to group ambient conditions
- Applies slow drifts (does NOT overwrite traits)
- Can modify other components (morale, stress) via game-specific systems

### Adapter Systems

#### `BandAggregateAdapterSystem` (PureDOTS)
Bridges `PureDOTS.Runtime.Bands.Band` entities to generic aggregate system.

#### `GodgameVillageAggregateAdapterSystem`
Bridges `Godgame.Villages.Village` entities to generic aggregate system.

#### `Space4XFleetAggregateAdapterSystem`
Bridges `Space4X.Registry.Space4XFleet` entities to generic aggregate system.

### Bootstrap Systems

#### `AggregateMotivationBootstrapSystem`
Ensures aggregate entities have motivation components initialized (MotivationDrive, MotivationSlot buffer, MotivationIntent, LegacyPoints).

---

## Integration Points

### Motivation System

Aggregate entities have their own `MotivationDrive`/`MotivationSlot` components for group-level ambitions. `AggregateStats` and `AmbientGroupConditions` influence which ambitions groups pick (via `MotivationSpecs` with alignment requirements).

### Game-Specific Integration

**Godgame**:
- Villagers use `GroupMembership` to villages, guilds, bands
- Guild membership shapes "career culture"
- Village membership shapes ambient morals

**Space4X**:
- Crew members belong to ships/fleets
- Fleet's `AmbientGroupConditions` influence mutiny chances, response to harsh captains

---

## Modding

### Adding New Aggregate Types

1. Create `AggregateTypeConfigAsset` ScriptableObject
2. Define aggregation rules (source traits → target metrics)
3. Set composition change threshold
4. Add to `AggregateConfigCatalogAsset`
5. Create adapter system (if needed) to bridge game entity to aggregate system

### Custom Aggregation Rules

Rules are defined in `AggregateTypeConfigAsset`:
- Source trait: One of `AggregateSourceTrait` enum values
- Target metric: One of `AggregateTargetMetric` enum values
- Weight: Contribution multiplier

Example: BoldCraven → AmbientCourage with weight 1.0

---

## Usage Examples

### Creating an Aggregate Entity

```csharp
// Via adapter system (automatic)
// BandAggregateAdapterSystem creates aggregate for Band entities

// Or manually:
var aggregateEntity = ecb.CreateEntity();
ecb.AddComponent(aggregateEntity, new AggregateIdentity { TypeId = VillageTypeId, Seed = 123 });
ecb.AddComponent(aggregateEntity, new AggregateStats { /* ... */ });
ecb.AddComponent(aggregateEntity, new AmbientGroupConditions { /* ... */ });
```

### Linking Individuals to Groups

```csharp
// Add GroupMembership to individual
ecb.AddComponent(individualEntity, new GroupMembership 
{ 
    Group = aggregateEntity, 
    Role = 0 // member
});

// Mark aggregate as dirty for recalculation
ecb.AddComponent<AggregateStatsDirtyTag>(aggregateEntity);
```

### Reading Ambient Conditions

```csharp
// In game-specific system
if (SystemAPI.HasComponent<AmbientGroupConditions>(groupEntity))
{
    var ambient = SystemAPI.GetComponent<AmbientGroupConditions>(groupEntity);
    // Use ambient.AmbientCourage, ambient.ExpectationLoyalty, etc.
}
```

---

## Tier-2 Features (Future)

- **Composition Change History**: Buffer tracking member changes over time
- **Cascade System**: Trigger events for significant composition/ambient shifts
- **Incremental Aggregation**: Maintain running sums for large worlds
- **Per-Game Response Systems**: Custom misalignment handling (mutiny, guild schisms, reforms)

---

## Performance Considerations

- **Update Frequency**: Ambient conditions update at configurable frequency (default: 100 ticks)
- **Dirty Tagging**: Only recalculates stats when composition changes
- **Simple Averaging**: Tier-1 uses simple averaging; Tier-2 can add incremental aggregation
- **Burst Compatibility**: All systems are Burst-compiled for performance

---

## Related Systems

- **Motivation & Legacy**: Group ambitions and legacy points
- **Villager/Crew Simulation**: Individual behavior systems
- **Environment & Economy**: Aggregate stats influenced by environment, economic systems use desire coverage

