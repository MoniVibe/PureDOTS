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

- `ComplexEntityIdentity`: **ulong StableId**, entity type enum, creation tick
- `ComplexEntityCoreAxes`: **fixed-size packed** hot-path values (quantized pose + fixed-point aggregates + flags)

### Core Axes Structure

```csharp
public struct ComplexEntityCoreAxes : IComponentData
{
    // Spatial (16 bytes)
    public int3 Cell;          // coarse spatial cell (game-defined size)
    public ushort LocalX;      // 0..65535 within cell (typically X)
    public ushort LocalY;      // 0..65535 within cell (typically Z)

    // Motion (8 bytes, quantized)
    public short VelX;         // planar vel (game-defined scale)
    public short VelY;
    public ushort HeadingQ;    // 0..65535 maps to 0..2π
    public ushort HealthQ;     // 0..65535 maps to 0..1

    // Aggregates (12 bytes, fixed-point)
    public uint MassQ;         // fixed-point (includes crew mass when collapsed)
    public uint CapacityQ;     // fixed-point (includes crew capacity when collapsed)
    public uint LoadQ;         // fixed-point

    // Flags & small aggregates (8 bytes)
    public uint Flags;         // operational/narrative/dirty bits
    public ushort CrewCount;   // cold-state aggregate (roster is externalized)
    public ushort Reserved0;

    // Total: 40 bytes
}
```

**Budget (components only)**: ~56 bytes per entity-of-record (16B identity + 40B axes) × 10M ≈ **560 MB**

Notes:

- This excludes ECS chunk overhead and allocator fragmentation; treat **~600–750 MB** as a more realistic envelope for 10M records.
- Fixed-point + quantized pose avoids “float drift” in rollups and keeps cold-state truth deterministic.

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

**Budget (typical)**: ~80–160 bytes *extra* per operational entity (state + handle + small sparse buffer)  
**10K operational**: ~0.8–1.6 MB plus buffer spillover (rare)

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

- Quantized pose (cell + local) for spatial activation and coarse queries
- Fixed-point aggregates for logistics/health/rollups

**Access Pattern**: Direct component read, no indirection, cache-friendly.

### Sparse Axes Buffer (Rare)

`ComplexEntitySparseAxesBuffer` stores optional axes that don't fit in core:

- Power budget breakdown
- Module-specific states
- Specialized capability flags

**Access Pattern**: Buffer iteration when needed, typically only for operational entities.

### Crew Roster Pool

Crew rosters stored in pooled blob assets keyed by stable entity ID (ulong):

```csharp
public struct CrewRosterBlob
{
    public BlobArray<CrewMemberBlob> Members;
    public BlobArray<CrewRoleBlob> Roles;
    public BlobArray<CrewRelationshipBlob> Relationships;
}

public struct ComplexEntityCrewHandle : IComponentData
{
    public ulong OwnerStableId;
    public BlobAssetReference<CrewRosterBlob> Roster;
    public uint LastUpdateTick;
}
```

**Access Pattern**: Pool lookup by `ComplexEntityIdentity.StableId`; hot entities cache the `Roster` pointer in `ComplexEntityCrewHandle`.

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

1. **Mass conservation**: `ComplexEntityCoreAxes.MassQ` includes crew mass (pre-aggregated)
2. **Capacity conservation**: `ComplexEntityCoreAxes.CapacityQ` includes crew capacity (pre-aggregated)
3. **Crew count**: Stored as aggregate in `ComplexEntityCoreAxes.CrewCount`

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

- `ComplexEntityIdentity`: 16 bytes
- `ComplexEntityCoreAxes`: 40 bytes
- **Total (components only)**: 56 bytes

**10M entities (components only)**: ~560 MB  
**10M entities (realistic, incl. chunk overhead)**: ~600–750 MB

### Per Operational Entity

- Core (above): 56 bytes
- `ComplexEntityOperationalState` (enabled): ~32 bytes
- `ComplexEntityCrewHandle`: ~24 bytes
- `ComplexEntitySparseAxesBuffer` internal capacity (2 elements): ~24 bytes
- **Total (typical)**: ~136 bytes per operational entity (plus rare buffer spillover)

**10K operational**: ~1.36 MB (plus spillover)

### Per Narrative Entity

- Core (above): ~56 bytes
- Narrative detail: ~16 bytes
- Expanded crew handle: ~8 bytes
- Blob references: ~16 bytes
- **Total**: ~96 bytes per narrative entity (plus blob payloads)

**1K narrative**: ~108 KB

### Blob Stores (Shared)

- Crew rosters pool: ~50 MB (shared across all entities)
- Knowledge graphs: ~10 MB (shared)
- Relationship matrices: ~5 MB (shared)

**Total shared**: ~65 MB

### Grand Total

- 10M entities-of-record (realistic): ~600–750 MB
- 10K operational: ~1.36 MB (plus spillover)
- 1K narrative: ~108 KB
- Shared blob stores: ~65 MB
- **Total**: ~670–820 MB (depends on chunk overhead + shared stores)

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

- Core archetype is **minimal** (identity + packed axes).
- Operational/narrative expansions are applied only to the hot subset (adding/removing components is allowed because hot-count is bounded).
- Enableable components are used to toggle *within* the hot subset without re-archetyping every frame.

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
