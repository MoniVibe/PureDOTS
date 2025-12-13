# Guilds – Shared Spine Feature

**Status**: Tier-1 Implementation  
**Category**: Social / Economy / Factions  
**Scope**: Shared PureDOTS module used by both Godgame and Space4X

## Overview

The Guilds Shared Spine provides a unified, moddable guild abstraction that works for both Godgame (craft/profession guilds, economic guilds, spiritual orders, shadow guilds, rebel factions) and Space4X (mega-corps, trade leagues, mercenary companies, religious orders, research alliances). The system integrates tightly with the Aggregate & Individual Dynamics spine and Motivation system.

## Key Features

- **Unified Abstraction**: One guild system works for bottom-up (charter) and top-down (spawn) formation
- **Aggregate Integration**: Guilds use `AggregateIdentity`, `AggregateStats`, and `AmbientGroupConditions` from the Aggregate spine
- **Motivation Integration**: Guilds have their own `MotivationDrive`/`MotivationSlot` for group ambitions
- **Data-Driven**: All guild types, actions, and governance rules defined in moddable catalogs
- **Cross-Game**: Same core system, different content catalogs for Godgame vs Space4X

## Core Components

### Guild Components (`PureDOTS.Runtime.Guild`)

- `GuildId`: Short numeric ID for registries
- `GuildWealth`: Aggregate wealth metrics + optional pooled treasury
- `GuildKnowledge`: Learned bonuses and progression
- `GuildStrike`, `GuildRiot`, `GuildCoup`: Hot state components (only present when active)

### Guild Catalogs

- `GuildTypeSpec`: Defines guild types (Heroes, Merchants, Rebels, etc.)
  - Recruitment scoring rules
  - Default governance
  - Alignment preferences
  - Behavior flags (can declare strikes, coups, war, etc.)
- `GuildActionSpec`: Defines actions guilds can take
  - Preconditions (alignment, governance, relations)
  - Cost & risk hints
  - AI strategy tags
- `GuildGovernanceSpec`: Defines governance rules
  - Voting rules, term lengths
  - Coup thresholds

## Systems

### Core Systems (`PureDOTS.Systems.Guild`)

- `GuildAggregateAdapterSystem`: Bridges existing `Guild` entities to generic aggregate system
- `GuildCharterFormationSystem`: Handles bottom-up guild formation via charter signatures
- `GuildSpawnSystem`: Spawns archetypal guilds based on world conditions (top-down)

### Game-Specific Systems

- `GodgameGuildCharterSystem`: Godgame-specific charter formation logic (education checks, fees, signature motivation)
- `Space4XGuildSpawnSystem`: Space4X-specific spawn logic (mega-corps, religious orders, research alliances)

## Integration Points

### Aggregate System

- Guilds use `AggregateIdentity`, `AggregateStats`, `AmbientGroupConditions` from Aggregate spine
- `GroupMembership` replaces old `GuildMembership` (unified pattern)
- `GuildAggregateAdapter` bridges existing `Guild` entities to aggregate system
- `AggregateStatsRecalculationSystem` automatically updates guild stats from members

### Motivation System

- Guilds have `MotivationDrive`/`MotivationSlot` for group ambitions
- `AggregateStats` and `AmbientGroupConditions` influence which ambitions guilds pick
- Guild actions are driven by `MotivationIntent` (from Motivation system)

### Village/Band/Business Integration

- Villages: Have `GuildEmbassy` entries from multiple guilds; village politics consider guild pressure
- Bands: Some bands are arm of a guild (Heroes' expedition, Rebel militia); `BandAggregateAdapter` can link to `GuildAggregateAdapter`
- Businesses: Guilds can own businesses; coordinate prices, supply, strikes

## Authoring Assets

### ScriptableObjects (`PureDOTS.Authoring.Guild`)

- `GuildTypeSpecAsset`: Author individual guild type specifications
- `GuildTypeCatalogAsset`: Hold multiple `GuildTypeSpecAsset` references with merging support
- `GuildActionSpecAsset`: Author individual guild action specifications
- `GuildActionCatalogAsset`: Hold multiple `GuildActionSpecAsset` references with merging support
- `GuildGovernanceSpecAsset`: Author individual governance specifications
- `GuildGovernanceCatalogAsset`: Hold multiple `GuildGovernanceSpecAsset` references with merging support

## Bootstrap Systems

### Godgame (`Godgame.Guild`)

- `GodgameGuildBootstrap`: Creates `GuildConfigState`, `GuildActionConfigState`, `GuildGovernanceConfigState` singletons with Godgame-specific catalogs

### Space4X (`Space4X.Guild`)

- `Space4XGuildBootstrap`: Creates singletons with Space4X-specific catalogs (mega-corps, trade leagues, etc.)

## Tier-2 Features (Future)

- `GuildGovernanceSystem`: Apply governance spec to drive democratic votes, authoritarian decrees, meritocratic promotions
- `GuildFactionSystem`: Analyze member alignments/outlooks; spawn `GuildFaction` entries when splits appear
- `GuildEconomicActionSystem`: Execute economic actions (price fixing, monopolies, buyouts, migration, franchising)
- `GuildPoliticalActionSystem`: Execute political actions (strikes, demonstrations, lobbying, coups)
- `GuildWarActionSystem`: Execute military actions (guild warfare, mercenary contracts, raids)
- `GuildCrisisResponseSystem`: Respond to world crises (world-boss hunting, apocalypse response, defense of villages)

## Usage Example

```csharp
// Create a guild type spec asset
var heroesGuildSpec = ScriptableObject.CreateInstance<GuildTypeSpecAsset>();
heroesGuildSpec.TypeId = 1;
heroesGuildSpec.Label = "Heroes' Guild";
heroesGuildSpec.DefaultGovernance = GuildLeadership.GovernanceType.Meritocratic;
heroesGuildSpec.CanDeclareWar = true;

// Add to catalog
var catalog = ScriptableObject.CreateInstance<GuildTypeCatalogAsset>();
catalog.TypeSpecs = new[] { heroesGuildSpec };

// Build blob asset
var blob = catalog.BuildBlobAsset();

// Initialize in bootstrap
var configState = new GuildConfigState
{
    Catalog = blob,
    FormationCheckFrequency = 300
};
```

## Notes

- **Burst Compatibility**: All systems use `[BurstCompile]`, `ISystem`, `in` parameters for struct access
- **Rewind Safety**: All systems respect `RewindState` (only mutate in Record mode)
- **Namespace**: `PureDOTS.Runtime.Guild` for components, `PureDOTS.Systems.Guild` for systems
- **Modding**: Catalogs support additive merging (base + mods), JSON loading from StreamingAssets
- **Integration Pattern**: Adapter components bridge existing Guild entities to generic aggregate system (preserves existing code)
- **Membership Pattern**: `GroupMembership` replaces `GuildMembership` for unified aggregate membership tracking


















