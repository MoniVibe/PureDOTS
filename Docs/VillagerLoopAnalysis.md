# Villager Loop Analysis & Vegetation Scaffolding

**Agent Alpha Report** - Villager System Tightening & Vegetation Lifecycle Scaffolding

## Executive Summary

Completed comprehensive analysis of villager system sequencing, identified and fixed one critical ordering issue, and scaffolded complete vegetation lifecycle components with baker stubs.

## Villager Loop Analysis

### Current System Sequence (Fixed)

1. **VillagerNeedsSystem** - Updates hunger/energy/health
   - [OK] Rewind guard: checks `rewindState.Mode != RewindMode.Record`
   - [OK] Cold path: excludes `PlaybackGuardTag` entities
   - [TODO] Consider hot/cold split optimization

2. **VillagerStatusSystem** - Calculates availability/mood
   - [OK] Rewind guard present
   - [OK] Correctly ordered after NeedsSystem

3. **VillagerJobAssignmentSystem** - Assigns worksites
   - [OK] Rewind guard present
   - [OK] Cold path excludes `PlaybackGuardTag`
   - [OK] Correctly ordered after StatusSystem

4. **VillagerAISystem** - Evaluates goals and decides behavior
   - [OK] Rewind guard present
   - [FIXED] Now correctly ordered after JobAssignmentSystem and before TargetingSystem
   - [OK] Previously was incorrectly only after NeedsSystem
   - [OK] Cold path excludes `PlaybackGuardTag`

5. **VillagerTargetingSystem** - Resolves target entities to positions
   - [OK] Rewind guard present
   - [OK] Correctly ordered after JobAssignmentSystem

6. **VillagerMovementSystem** - Updates positions
   - [OK] Rewind guard present
   - [OK] Correctly ordered after TargetingSystem
   - [OK] Deterministic movement calculations

### Findings

**Strengths:**
- All systems have proper rewind guards
- All systems exclude `PlaybackGuardTag` entities appropriately
- Dependency tracking via `state.Dependency` is consistent
- System logic is deterministic and rewind-safe

**Issues Fixed:**
- VillagerAISystem was incorrectly sequenced: it needs job assignments completed before evaluating goals
- Added explicit `[UpdateAfter(typeof(VillagerJobAssignmentSystem))]` and `[UpdateBefore(typeof(VillagerTargetingSystem))]` attributes

**TODOs Added:**
- Consider creating a dedicated system for PlaybackGuardTag filtering to optimize hot/cold splits
- All systems already have good cold path handling; no critical hot/cold split issues identified

## Vegetation Lifecycle Scaffolding

### Components Created

**Runtime Components** (`VegetationComponents.cs`):
- `VegetationId` - Species identification
- `VegetationLifecycle` - Stage management (seedling → mature → dead)
- `VegetationHealth` - Health, water, light, soil quality
- `VegetationProduction` - Resource yield configuration
- `VegetationConsumption` - Water/nutrient consumption rates
- `VegetationReproduction` - Spreading and reproduction behavior
- `VegetationSeasonal` - Seasonal effects and climate sensitivity
- Tag components: Mature, ReadyToHarvest, Dead, Dying, Decayable
- Buffers: SeedDrop (tracking dropped seeds), HistoryEvent (growth milestones)

**Authoring** (`VegetationAuthoring.cs`):
- Comprehensive `VegetationAuthoring` MonoBehaviour with inspector fields
- Complete `VegetationBaker` that converts to ECS runtime components
- Conditional tag assignment based on initial lifecycle stage
- Production, consumption, reproduction, and seasonal configuration

### Integration Points

Vegetation components are designed to integrate with:
- **Villager Gathering Systems**: Production component provides harvestable resources
- **Resource System**: ResourceTypeId maps to resource types
- **Time/Rewind System**: HistoryEvent buffer for deterministic replay
- **Environment Systems**: Health component tracks environmental needs

### Implementation Status

1. **VegetationGrowthSystem** - [COMPLETE] Updates lifecycle stages over time using data-driven species catalog
   - Uses `VegetationSpeciesCatalog` blob for stage durations
   - Supports multiple species with different growth rates
   - Safety checks for missing catalog singleton
   - Comprehensive test suite with multi-species validation
2. **VegetationHealthSystem** - [COMPLETE] Processes environmental effects on vegetation health
   - Compares environment state to species thresholds from catalog blob
   - Calculates deficits for water, light, soil, pollution, and wind
   - Applies health regeneration or damage based on environmental conditions
   - Adds `VegetationStressedTag` and `VegetationDyingTag` flags
   - Updates lifecycle stage to Dying when health drops below threshold
   - Runs before growth system to ensure health affects lifecycle
3. **VegetationReproductionSystem** - Handle spreading and new growth
4. **VegetationHarvestSystem** - Allow villagers to gather resources
5. **VegetationDecaySystem** - Handle dead/dying vegetation cleanup
6. **VegetationSeasonalSystem** - Apply seasonal multipliers to growth/production

### System Group Assignment

Vegetation systems belong in `VegetationSystemGroup` which runs after `FixedStepSimulationSystemGroup`, allowing them to consume physics and fixed-step updates.

## Files Modified

1. `Docs/SystemOrdering/SystemSchedule.md` - Added analysis notes and TODOs
2. `Assets/Scripts/PureDOTS/Systems/VillagerAISystem.cs` - Fixed sequencing attributes
3. `Assets/Scripts/PureDOTS/Runtime/VegetationComponents.cs` - New vegetation components
4. `Assets/Scripts/PureDOTS/Authoring/VegetationAuthoring.cs` - New vegetation authoring

## Next Steps

1. Implement vegetation growth system to update lifecycle stages
2. Add environmental impact systems (water, sunlight, soil)
3. Connect vegetation production to resource gathering systems
4. Create vegetation spawning and management systems
5. Add vegetation decay/cleanup systems

