# Hierarchical Spatial Grid Documentation Index

**Complete documentation for the multi-resolution hierarchical spatial grid system**

---

## Documentation Files

### User Guides

1. **`HierarchicalSpatialGridGuide.md`** - Comprehensive guide covering:
   - Architecture overview
   - Configuration (authoring & runtime)
   - Query APIs (legacy & SFC-based)
   - Adaptive subdivision
   - Region streaming
   - Temporal caching
   - Migration from legacy grids
   - Performance considerations
   - Best practices

2. **`HierarchicalSpatialGridQuickReference.md`** - Quick lookup for:
   - Common code snippets
   - API usage patterns
   - Component lookups
   - System execution order
   - Provider IDs

3. **`HierarchicalSpatialGridIntegration.md`** - Integration guide for agents:
   - Integration patterns
   - API compatibility notes
   - Common pitfalls
   - Testing examples
   - Performance tips

---

## Key Concepts

### Hierarchical Levels

- **L0_Galactic**: Largest scale (1 ly - 100 AU), analytic orbits
- **L1_System**: System scale (10⁶ km), coarse collisions
- **L2_Planet**: Planet scale (1-10 km), deterministic grid
- **L3_Local**: Local scale (1-100 m), fine physics

### Core APIs

- **`SpatialGridConfig`**: Grid configuration (supports hierarchical)
- **`SpatialQueryHelper`**: Query utilities (legacy & SFC)
- **`SpaceFillingCurve`**: Morton/Hilbert encoding
- **`SpatialGridMigration`**: Legacy → hierarchical migration

### Systems

- **`SpatialGridBuildSystem`**: Grid rebuild (every tick)
- **`SpatialGridRefinementSystem`**: Adaptive subdivision (0.2 Hz)
- **`SpatialGridLoadBalancerSystem`**: Work balancing (100 tick intervals)
- **`SpatialGridStreamingSystem`**: Region streaming (60 tick intervals)
- **`SpatialGridSnapshotSystem`**: Temporal cache (every tick if changed)

---

## Quick Start

1. **Read**: `HierarchicalSpatialGridGuide.md` for architecture
2. **Reference**: `HierarchicalSpatialGridQuickReference.md` for code snippets
3. **Integrate**: `HierarchicalSpatialGridIntegration.md` for patterns

---

## Source Code Documentation

All public APIs include XML documentation with:
- Usage examples
- Performance notes
- See also references
- Parameter descriptions

**Location**: `Runtime/Runtime/Spatial/` and `Runtime/Systems/Spatial/`

---

## Related Documentation

- `Docs/Guides/Authoring/EnvironmentAndSpatialValidation.md` - Authoring guide
- `Docs/Guides/GettingStarted.md` - PureDOTS overview
- `TRI_PROJECT_BRIEFING.md` - Multi-ECS architecture

---

**Last Updated**: 2025-01-XX

