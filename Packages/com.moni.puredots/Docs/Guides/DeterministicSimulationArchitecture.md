# Deterministic Simulation Architecture Overview

## Quick Reference

This document provides a quick reference for the 12 core principles implemented for deterministic, scalable simulation. See individual guides for detailed usage.

## 1. Deterministic & Reversible Calculations

**Utilities:**
- `DeterministicRandom.CreateFromTickAndEntity(tick, entity)` - Seeded RNG
- `FixedPointMath` - Fixed-point math for economy/power
- `TickTimeState.FixedDeltaTime` - Use for simulation time deltas

**Guide:** [DeterminismGuide.md](DeterminismGuide.md)

## 2. Hot/Cold Data Separation

**Components:**
- `VillagerPresentation`, `VillagePresentation` - Cold presentation data
- `VillagerLore`, `VillageLore` - Cold lore data
- `PresentationCompanionRef`, `LoreCompanionRef` - Links to companion entities
- `SimToPresentationMessage` - Message buffer for sim→presentation sync

**Systems:**
- `PresentationBridgeSystem` - Writes messages from simulation
- `SimPresentationValidationSystem` - Validates boundaries

**Guide:** [HotColdSeparationGuide.md](HotColdSeparationGuide.md)

## 3. Dirty Flags & Partitioned Ticks

**Components:**
- `PeriodicTickComponent` - Update every N ticks
- `PeriodicTickHelper.ShouldUpdate()` - Check if update needed

**Patterns:**
- `WithChangeFilter<T>()` - Only process changed entities
- Periodic ticks: Needs (1), AI (10), Pathfinding (5), Economy (60)

**Guide:** [PerformanceOptimizationGuide.md](PerformanceOptimizationGuide.md)

## 4. Hierarchical Aggregation

**Components:**
- `PowerZone`, `PowerZoneState`, `EntityPower` - Power aggregation
- `EconomyZone`, `EconomyZoneState`, `EntityEconomy` - Economy aggregation

**Systems:**
- `PowerAggregationSystem` - Aggregates power per zone
- `EconomyAggregationSystem` - Aggregates economy per zone

**Guide:** [PerformanceOptimizationGuide.md](PerformanceOptimizationGuide.md)

## 5. Flowfield Pathfinding

**Components:**
- `FlowfieldGrid` - Shared component per zone
- `FlowfieldCell` - Flow vectors per cell
- `PathCacheBlob` - Cached path data

**Systems:**
- `FlowfieldGenerationSystem` - Generates flowfields per zone

**Guide:** [FlowfieldPathfindingGuide.md](FlowfieldPathfindingGuide.md)

## 6. Spatial Load Balancing

**Components:**
- `RegionProfile` - Region cost profiling

**Systems:**
- `SpatialLoadBalancerSystem` - Profiles and rebalances threads

**Usage:** Thread ownership linked to `SpatialGridResidency.CellId` ranges.

## 7. Presentation Separation

**Systems:**
- `PresentationBridgeSystem` - Sim→presentation message bridge
- `SimPresentationValidationSystem` - Boundary validation

**Pattern:** `PresentationSystemGroup` runs asynchronously, never blocks simulation.

**Guide:** [HotColdSeparationGuide.md](HotColdSeparationGuide.md)

## 8. System Profiling

**Components:**
- `SystemMetrics` - Per-system performance metrics
- `MemoryMetrics` - Memory fragmentation metrics

**Systems:**
- `SystemMetricsCollector` - Collects and exports metrics every 5s
- `MemoryMetricsSystem` - Tracks chunk reuse rate
- `ChunkReuseSystem` - Flushes inactive chunks every 10 min

**Guide:** [SystemProfilingGuide.md](SystemProfilingGuide.md)

## 9. Modding API

**Components:**
- `ModdingEventBus` - Event bus singleton
- `ModdingEvent` - Event buffer
- `ModdingCatalogEntry` - Catalog registration buffer

**Systems:**
- `ModdingCatalogSystem` - Processes catalog entries at boot

**Guide:** [ModdingAPIGuide.md](ModdingAPIGuide.md)

## 10. Memory Fragmentation Control

**Components:**
- `MemoryMetrics` - Tracks chunk reuse rate (>90% target)

**Systems:**
- `MemoryMetricsSystem` - Updates metrics every tick
- `ChunkReuseSystem` - Flushes inactive chunks every 10 min

**Guide:** [SystemProfilingGuide.md](SystemProfilingGuide.md)

## 11. Deterministic Debugging

**Components:**
- `TickHashState` - Singleton for hash tracking
- `TickHashEntry` - Hash per tick buffer
- `RandomSeedPerTick` - Random seed per tick buffer

**Systems:**
- `TickHashSystem` - Computes hash per tick

**Guide:** [DeterministicDebuggingGuide.md](DeterministicDebuggingGuide.md)

## 12. Simulation LOD

**Components:**
- `LODComponent` - LOD level and update stride
- `LODConfig` - Distance thresholds and update strides

**Systems:**
- `SimulationLODSystem` - Assigns LOD levels based on distance

**Guide:** [PerformanceOptimizationGuide.md](PerformanceOptimizationGuide.md)

## Integration Checklist

When creating new systems:

1. **Determinism:**
   - [ ] Use `DeterministicRandom.CreateFromTickAndEntity()`
   - [ ] Use `TickTimeState.FixedDeltaTime` (never `SystemAPI.Time.DeltaTime`)
   - [ ] Use `FixedPointMath` for economy/power

2. **Hot/Cold:**
   - [ ] Separate hot (simulation) and cold (presentation) data
   - [ ] Use `SimToPresentationMessage` buffer for sync
   - [ ] Never mutate hot components from presentation

3. **Performance:**
   - [ ] Add `WithChangeFilter<T>()` where appropriate
   - [ ] Use `PeriodicTickComponent` for non-critical updates
   - [ ] Assign entities to zones for aggregation
   - [ ] Add `LODComponent` for distance-based LOD

4. **Profiling:**
   - [ ] Add `SystemMetrics` tracking
   - [ ] Monitor `MemoryMetrics` for chunk reuse

5. **Modding:**
   - [ ] Use `ModdingEventBus` for mod communication
   - [ ] Register catalogs at boot time

6. **Debugging:**
   - [ ] Check `TickHashEntry` for determinism validation
   - [ ] Use `RandomSeedPerTick` for replay

## File Locations

**Core Utilities:**
- `Runtime/Core/DeterministicRandom.cs`
- `Runtime/Core/FixedPointMath.cs`

**Components:**
- `Runtime/Components/ColdDataComponents.cs`
- `Runtime/Components/PeriodicTickComponent.cs`
- `Runtime/Components/PowerZoneComponents.cs`
- `Runtime/Components/EconomyZoneComponents.cs`
- `Runtime/Components/FlowfieldComponents.cs`
- `Runtime/Components/LODComponent.cs`
- `Runtime/Components/SystemMetricsComponent.cs`
- `Runtime/Components/MemoryMetricsComponent.cs`

**Systems:**
- `Runtime/Systems/PresentationBridgeSystem.cs`
- `Runtime/Systems/PowerAggregationSystem.cs`
- `Runtime/Systems/EconomyAggregationSystem.cs`
- `Runtime/Systems/FlowfieldGenerationSystem.cs`
- `Runtime/Systems/SpatialLoadBalancerSystem.cs`
- `Runtime/Systems/SystemMetricsCollector.cs`
- `Runtime/Systems/MemoryMetricsSystem.cs`
- `Runtime/Systems/ChunkReuseSystem.cs`
- `Runtime/Systems/TickHashSystem.cs`
- `Runtime/Systems/SimulationLODSystem.cs`
- `Runtime/Systems/ModdingCatalogSystem.cs`

**Modding:**
- `Runtime/Modding/ModdingEventBus.cs`

