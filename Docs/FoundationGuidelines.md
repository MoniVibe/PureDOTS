# PureDOTS Foundation Guidelines

**Last Updated**: 2025-01-27

This document defines core coding patterns, architectural principles, and best practices for PureDOTS development.

---

## System Group Policy

### Simulation vs Presentation Separation

PureDOTS follows a strict separation between deterministic simulation and non-deterministic presentation:

**Simulation Groups** (`SimulationSystemGroup`, `FixedStepSimulationSystemGroup`, custom sim groups):
- All deterministic game logic runs here
- Time-based, rewind-safe systems
- Physics, AI, gameplay mechanics
- **Never** mutate presentation-only data
- **Never** depend on Unity rendering/visual systems

**Presentation Groups** (`Unity.Entities.PresentationSystemGroup`):
- All visual/rendering systems run here
- Frame-time, non-deterministic systems
- Debug drawing, aggregate render summaries, view helpers
- **Only** read from simulation data
- **Never** mutate simulation state in ways that affect determinism

### Presentation System Group Rules

**Policy**: PureDOTS presentation systems must run in `Unity.Entities.PresentationSystemGroup` (either directly or via a PureDOTS child group).

**Rules for Future Systems**:

1. **If a system touches rendering, view models, camera glue, or other visual-only data, it belongs in the presentation group** (`Unity.Entities.PresentationSystemGroup` or a PureDOTS child group under it).

2. **Simulation data and deterministic logic never live in the presentation group.**

3. **Presentation systems are read-only with respect to simulation state** - they may write to presentation/metrics components but must not affect deterministic simulation.

4. **PureDOTS provides a child group** (`PureDOTS.Systems.PureDotsPresentationSystemGroup`) for logical organization, but it ultimately runs under Unity's `PresentationSystemGroup`.

**Example**:
```csharp
// PureDOTS presentation system
[UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
public partial struct AggregateRenderSummarySystem : ISystem
{
    // read-only sim data, write-only presentation/metrics data
}

// Or use PureDOTS child group for organization
[UpdateInGroup(typeof(PureDOTS.Systems.PureDotsPresentationSystemGroup))]
public partial struct RenderDensitySystem : ISystem
{
    // PureDotsPresentationSystemGroup is itself under Unity's PresentationSystemGroup
}
```

### Multi-World Usage

In game worlds (Godgame, Space4X), PureDOTS presentation helpers run in Unity's default world presentation group.

In headless/test harness worlds:
- Worlds may have a `PresentationSystemGroup` even without real rendering (for metrics/debug)
- Or provide alternative setup that respects sim/presentation separation
- PureDOTS presentation systems gracefully handle missing presentation infrastructure

---

## Component Size Guidelines

### Hot-Path Components (< 128 bytes)

Components accessed every tick in hot simulation loops should be small for cache efficiency:

- **Target**: < 128 bytes per component
- **Examples**: `VillagerNeedsHot` (16 bytes), `LocalTransform` (32 bytes), `VillagerBeliefOptimized` (6 bytes)
- **Optimization**: Use byte/short instead of int/float where precision allows
- **Avoid**: FixedString in hot paths (use byte index + lookup table instead)

### Medium-Path Components (< 256 bytes)

Components accessed frequently but not every tick:

- **Target**: < 256 bytes per component
- **Examples**: `VillagerAIState` (~64 bytes), `ResourceChunkState` (~48 bytes)
- **Consider**: Moving to companion entity if accessed infrequently

### Cold-Path Components (No strict limit)

Components accessed rarely or only on events:

- **No strict size limit**, but prefer companion entities for > 64 bytes
- **Examples**: `VillagerKnowledge` (large buffers), `VillagerColdData` (companion entity)
- **Pattern**: Use `CompanionRef` component to link main entity to cold data entity

### Companion Entity Pattern

For components > 64 bytes that are accessed infrequently:

```csharp
// Main entity (hot)
public struct VillagerId : IComponentData { ... }
public struct VillagerNeedsHot : IComponentData { ... } // < 64 bytes

// Companion entity (cold)
public struct VillagerColdDataRef : IComponentData 
{ 
    public Entity CompanionEntity; 
}

// Companion entity has:
public struct VillagerColdData : IComponentData 
{ 
    // Large data, updated on events only
}
```

---

## Burst Compatibility

### Required for Hot Paths

All systems in simulation groups must be Burst-compatible:

- Use `[BurstCompile]` attribute
- Avoid managed types (`string`, `List<T>`, etc.)
- Use `NativeArray`, `NativeList`, `FixedString` instead
- No `SystemAPI.GetSingletonRW<T>()` in Burst jobs (use `RefRW` in queries)

### FixedString Usage

- **Hot paths**: Avoid `FixedString` - use byte index + lookup table
- **Cold paths**: `FixedString64Bytes` or `FixedString128Bytes` acceptable
- **Blob assets**: Prefer `BlobString` for large string data

### Job Safety

- Use `[ReadOnly]` for components that aren't modified
- Use `EntityCommandBuffer` for structural changes in jobs
- Use `EntityCommandBuffer.ParallelWriter` for parallel jobs

---

## Determinism Requirements

### Fixed-Step Simulation

- All simulation runs at fixed time steps (`TimeState.FixedDeltaTime`)
- No frame-rate dependent logic in simulation
- Use `TimeState.CurrentTick` for time-based logic, not `Time.DeltaTime`

### Rewind Safety

- Systems must check `RewindState.Mode` before mutating state
- Use `[UpdateBefore(typeof(RewindGuardSystem))]` or `[UpdateAfter(typeof(RewindGuardSystem))]` for ordering
- Presentation systems are automatically guarded (run only during playback, not recording)

### No Randomness in Simulation

- Use deterministic RNG seeded from scenario seed
- No `UnityEngine.Random` in simulation systems
- Use `Unity.Mathematics.Random` with explicit seed

---

## Entity Creation Patterns

### Authoring → Runtime Conversion

- Use `Baker<T>` for MonoBehaviour → ECS conversion
- Bakers run in editor, create entities at authoring time
- Use `IBaker<T>` for custom baking logic

### Runtime Spawning

- Use `EntityCommandBuffer` for structural changes
- Prefer `EntityCommandBuffer.ParallelWriter` in parallel jobs
- Use `EntityManager` only in single-threaded contexts

### Companion Entity Creation

```csharp
// In baker or spawner
var mainEntity = entityManager.CreateEntity();
entityManager.AddComponentData(mainEntity, new VillagerId { ... });

var companionEntity = entityManager.CreateEntity();
entityManager.AddComponentData(companionEntity, new VillagerColdData { ... });
entityManager.AddComponentData(mainEntity, new VillagerColdDataRef 
{ 
    CompanionEntity = companionEntity 
});
```

---

## Performance Optimization Patterns

### Chunk Utilization

- Keep archetypes small (fewer components = better chunk utilization)
- Use `[ChunkIndexInQuery]` for chunk-level operations
- Consider AOSOA (Array of Structs of Arrays) for extreme performance

### Query Optimization

- Use `WithAll<T>()`, `WithAny<T>()`, `WithNone<T>()` to narrow queries
- Use `[ReadOnly]` for components that aren't modified
- Avoid `SystemAPI.Query` in hot loops - cache queries in `OnCreate`

### Memory Management

- Use `Allocator.Temp` for temporary allocations in jobs
- Use `Allocator.TempJob` for allocations that span multiple frames
- Dispose native collections explicitly or use `using` statements

---

## Documentation Standards

### XML Documentation

All public APIs must have XML documentation:

```csharp
/// <summary>
/// Brief description of what this component/system does.
/// </summary>
/// <remarks>
/// Additional details, usage examples, performance notes.
/// </remarks>
public struct MyComponent : IComponentData
{
    /// <summary>
    /// Description of this field.
    /// </summary>
    public float Value;
}
```

### Code Comments

- Use `//` for inline explanations
- Use `///` for XML documentation
- Explain **why**, not **what** (code should be self-documenting)

---

## Testing Requirements

### Unit Tests

- Test all public APIs
- Use `Unity.Entities.Testing` for ECS testing
- Test deterministic behavior with fixed seeds

### Integration Tests

- Use `ScenarioRunner` for integration tests
- Test with realistic entity counts (100s-1000s)
- Verify determinism with rewind/playback

### Performance Tests

- Use scale test scenarios (`scale_baseline_10k.json`, etc.)
- Measure tick time, memory usage
- Validate against performance budgets

---

## Versioning and Compatibility

### Breaking Changes

- Avoid breaking changes to public APIs
- Use `[Obsolete]` attribute with migration path
- Document breaking changes in release notes

### API Stability

- Public APIs in `Runtime/` are considered stable
- Internal APIs may change without notice
- Use `#if UNITY_EDITOR` for editor-only code

---

## References

- `TRI_PROJECT_BRIEFING.md` - Project overview and architecture
- `Docs/PERFORMANCE_PLAN.md` - Performance optimization guide
- `Docs/Guides/ComponentMigrationGuide.md` - Component migration patterns
- `Docs/Guides/GameIntegrationGuide.md` - Integration guide for game projects
