# Complex Entity Memory Model

## Overview

This document defines the canonical memory-efficient representation for "complex entities" (carriers with crews, guilds, colonies, stations, etc.) targeting ~10 million entities-of-record with only a fraction being "hot" (actively simulated in detail).

## Design Principles

1. **Hot entities stay tiny**: Core entity-of-record uses minimal components (< 128 bytes)
2. **Avoid archetype explosion**: Use enableable components for optional modules instead of separate archetypes
3. **Externalize heavy data**: Crew rosters, deep knowledge graphs, and variable-size data live in pooled blob stores
4. **Expand on demand**: Operational and narrative detail components activate only when needed (bubble, focus, combat, docking, inspection)
5. **Conservation guarantees**: Expansions maintain deterministic rollups/aggregates; mass/crew/cap values are conserved

## Entity-of-Record (Canonical Minimum)

Every complex entity has this minimal component set:

### Core Identity
- `ComplexEntityIdentity`: Stable ID, entity type enum, creation tick
- `ComplexEntityCoreAxes`: Packed hot-path values (position, velocity, mass, capacity, current load, health)

### Core Axes Structure
```csharp
public struct ComplexEntityCoreAxes : IComponentData
{
    // Spatial (12 bytes)
    public float3 Position;
    
    // Motion (12 bytes)
    public float3 Velocity;
    
    // Physical (8 bytes)
    public float Mass;
    public float Capacity;
    
    // State (8 bytes)
    public float CurrentLoad;
    public float Health;
    
    // Flags (4 bytes)
    public uint Flags; // bitfield: operational, narrative, etc.
    
    // Total: ~44 bytes (well under 128-byte cache line)
}
```

**Budget**: ~44 bytes per entity-of-record × 10M = ~440 MB

## Expansion Strategy

### Operational Expansion (Enableable Components)

Activated when entity enters:
- **Active bubble**: Within player viewport/camera frustum
- **Player focus**: Selected or being inspected
- **Combat**: Engaged in combat or within combat range
- **Docking**: Docking/undocking operations active

**Components**:
- `ComplexEntityOperationalState` (enableable): Detailed movement, targeting, AI state
- `ComplexEntityCrewHandle`: Reference to crew roster in pooled blob store
- `ComplexEntitySparseAxesBuffer`: Rare/optional axes (power budget, module states, etc.)

**Budget**: ~200 bytes per operational entity × 10K max operational = ~2 MB

### Narrative/Detail Expansion (Blob/Pool Data)

Activated when:
- **Inspection**: Player opens detail panel
- **Retinue**: Entity has named crew/officers/captain
- **Narrative events**: Entity participates in story beats

**Components**:
- `ComplexEntityNarrativeDetail` (enableable): References to narrative blob assets
- `ComplexEntityCrewHandle`: Expanded crew roster with names, traits, relationships
- External blob stores: Crew rosters, knowledge graphs, relationship matrices

**Budget**: Variable, but typically < 1K per narrative entity × 1K max narrative = ~1 MB

## Data Structures

### Fixed-Size Packed Core Axes (Hot)

All hot-path reads use `ComplexEntityCoreAxes`:
- Position, velocity for movement systems
- Mass, capacity for logistics systems
- Health for combat systems
- Current load for resource management

**Access Pattern**: Direct component read, no indirection, cache-friendly.

### Sparse Axes Buffer (Rare)

`ComplexEntitySparseAxesBuffer` stores optional axes that don't fit in core:
- Power budget breakdown
- Module-specific states
- Specialized capability flags

**Access Pattern**: Buffer iteration when needed, typically only for operational entities.

### Crew Roster Pool

Crew rosters stored in pooled blob assets keyed by stable entity ID:

```csharp
public struct CrewRosterBlob
{
    public BlobArray<CrewMemberBlob> Members;
    public BlobArray<CrewRoleBlob> Roles;
    public BlobArray<CrewRelationshipBlob> Relationships;
}

public struct ComplexEntityCrewHandle : IComponentData
{
    public BlobAssetReference<CrewRosterBlob> Roster;
    public uint LastUpdateTick;
}
```

**Access Pattern**: Lookup by `ComplexEntityIdentity.StableId`, shared blob assets reduce memory overhead.

## Trigger Conditions

### Active Bubble
- Entity within camera frustum or viewport bounds
- System: `ComplexEntityBubbleActivationSystem` (runs every N ticks, spatial query)

### Player Focus
- Entity selected via UI or player interaction
- Component: `FocusTargetTag` (added by input/interaction systems)

### Combat
- Entity engaged in combat (has `CombatReadyTag` or `InCombatTag`)
- System: `ComplexEntityCombatActivationSystem` (monitors combat state)

### Docking
- Entity performing docking/undocking operations
- Component: `DockingActiveTag` (added by docking systems)

### Inspection
- Player opens detail panel for entity
- Component: `InspectionRequest` (added by UI systems)

## Conservation and Determinism

### Rollup/Aggregate Rules

When operational/narrative components are disabled:
1. **Mass conservation**: `ComplexEntityCoreAxes.Mass` includes crew mass (pre-aggregated)
2. **Capacity conservation**: `ComplexEntityCoreAxes.Capacity` includes crew capacity (pre-aggregated)
3. **Crew count**: Stored as aggregate in core axes flags (bitfield or packed count)

### Deterministic Transitions

Expansion/contraction must be deterministic:
- Activation based on deterministic triggers (spatial queries, tick-based checks)
- Rollup values computed deterministically from expanded state
- No floating-point drift: use fixed-point or integer math for aggregates

### Conversion Rules

**Expand to Operational**:
1. Enable `ComplexEntityOperationalState`
2. Load crew roster from pool (if not already loaded)
3. Populate sparse axes buffer from core axes
4. Initialize operational state from core axes

**Collapse from Operational**:
1. Rollup operational state to core axes
2. Store crew roster back to pool (if modified)
3. Disable `ComplexEntityOperationalState`
4. Clear sparse axes buffer (or keep minimal state)

## Memory Budgets

### Per Entity-of-Record
- Core identity: ~16 bytes
- Core axes: ~44 bytes
- Metadata/tags: ~8 bytes
- **Total**: ~68 bytes per entity

**10M entities**: ~680 MB

### Per Operational Entity
- Core (above): ~68 bytes
- Operational state: ~120 bytes
- Crew handle: ~8 bytes
- Sparse axes buffer: ~64 bytes (average)
- **Total**: ~260 bytes per operational entity

**10K operational**: ~2.6 MB

### Per Narrative Entity
- Core (above): ~68 bytes
- Narrative detail: ~16 bytes
- Expanded crew handle: ~8 bytes
- Blob references: ~16 bytes
- **Total**: ~108 bytes per narrative entity

**1K narrative**: ~108 KB

### Blob Stores (Shared)
- Crew rosters pool: ~50 MB (shared across all entities)
- Knowledge graphs: ~10 MB (shared)
- Relationship matrices: ~5 MB (shared)

**Total shared**: ~65 MB

### Grand Total
- 10M entities-of-record: ~680 MB
- 10K operational: ~2.6 MB
- 1K narrative: ~108 KB
- Shared blob stores: ~65 MB
- **Total**: ~748 MB (well within target for 10M entities)

## Expected Max Operational Count

Based on typical gameplay:
- **Active bubble**: ~1K entities (viewport + nearby)
- **Player focus**: ~10 entities (selected/inspected)
- **Combat**: ~500 entities (engaged or nearby)
- **Docking**: ~100 entities (docking operations)

**Total operational**: ~1.6K entities (conservative estimate, allows up to 10K)

## Feature Flag Integration

The complex entity system is controlled by `SimulationFeatureFlags`:
- `ComplexEntitiesEnabled`: Master switch for complex entity system
- `ComplexEntityOperationalExpansionEnabled`: Enable operational expansion
- `ComplexEntityNarrativeExpansionEnabled`: Enable narrative expansion

Systems check flags before activating expansions.

## Implementation Notes

### Archetype Stability
- Use enableable components (`IEnableableComponent`) to avoid archetype changes
- Core archetype remains stable regardless of expansion state
- Only narrative detail blob references may cause archetype changes (rare)

### System Ordering
1. `ComplexEntityActivationSystem`: Determines which entities should be operational
2. `ComplexEntityOperationalStateSystem`: Updates operational state for enabled entities
3. `ComplexEntityNarrativeDetailSystem`: Handles narrative detail expansion/contraction
4. `ComplexEntityCrewPoolSystem`: Manages crew roster pool lifecycle

### Performance Considerations
- Activation checks run at reduced cadence (every N ticks)
- Spatial queries use spatial hashing/grids
- Crew pool lookups use stable ID hash tables
- Blob assets are immutable and thread-safe

## Future Extensions

- **Multi-level expansion**: Support multiple expansion tiers (minimal → operational → full detail)
- **Predictive expansion**: Pre-expand entities likely to become operational
- **Compression**: Compress inactive crew rosters/knowledge graphs
- **Streaming**: Stream narrative detail from disk when needed
