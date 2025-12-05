# System Heat Tier Guidelines

## Overview

Every system must be classified into one of three heat tiers: **Hot**, **Warm**, or **Cold**. This classification determines update frequency, computational budget, and allowed operations.

## Heat Tier Definitions

### Hot Path

**Definition**: Runs every tick on many entities. Must be tiny, branch-light, data-tight.

**Characteristics**:
- **Frequency**: Every simulation tick
- **Entities**: Many (all active entities)
- **Operations**: O(1) reads of pre-computed scalars
- **Allowed**: Simple math, component reads, cached values
- **Forbidden**: Allocations, pathfinding, graph traversals, N² operations, expensive calculations

**Examples**:
- Movement: Apply velocity to position
- Steering: Follow already-computed paths
- Damage application: Apply already-chosen damage
- Awareness reads: Read AwarenessSnapshot flags
- Job execution: Follow current job step

**Budget**: No throttling needed (O(N) simple operations)

**Code Pattern**:
```csharp
[UpdateInGroup(typeof(HotPathSystemGroup))]
public partial struct HotSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Only read pre-computed values
        foreach (var (snapshot, transform) in 
            SystemAPI.Query<RefRO<AwarenessSnapshot>, RefRW<LocalTransform>>())
        {
            // Simple reads and math only
            if (snapshot.ValueRO.ThreatLevel > 0.5f)
            {
                // Apply movement based on cached threat direction
            }
        }
    }
}
```

### Warm Path

**Definition**: Runs regularly but on fewer entities or with throttling. Can afford more logic.

**Characteristics**:
- **Frequency**: Every N ticks (5-100 ticks), staggered per entity
- **Entities**: Fewer (important entities, groups, or sampled)
- **Operations**: Moderate calculations, aggregations, local pathfinding
- **Allowed**: Local A* searches, utility scoring, sampling, group-level work
- **Forbidden**: Global scans, expensive graph operations, unbounded iterations

**Examples**:
- Local pathfinding: A* on small grid
- Target selection: Evaluate nearby candidates
- Loyalty aggregation: Sample N members, aggregate
- Job reassignment: Evaluate when thresholds crossed
- Perception updates: LOS checks per group

**Budget**: Hard caps (e.g., MaxLocalPathQueriesPerTick = 50)

**Code Pattern**:
```csharp
[UpdateInGroup(typeof(WarmPathSystemGroup))]
public partial struct WarmSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
        var counters = SystemAPI.GetSingletonRW<UniversalPerformanceCounters>();
        
        int processed = 0;
        foreach (var (cadence, entity) in 
            SystemAPI.Query<RefRO<UpdateCadence>>()
            .WithEntityAccess())
        {
            // Check cadence
            if (!UpdateCadenceHelpers.ShouldUpdate(timeState.Tick, cadence.ValueRO))
                continue;
            
            // Check budget
            if (processed >= budget.MaxWarmOperationsPerTick)
                break;
            
            // Do moderate work (pathfinding, aggregation, etc.)
            processed++;
        }
        
        counters.ValueRW.WarmOperationsThisTick += processed;
    }
}
```

### Cold Path

**Definition**: Runs rarely, or on small sets, or amortized. Can afford heavy logic/branches.

**Characteristics**:
- **Frequency**: Event-driven or long intervals (50-200+ ticks)
- **Entities**: Small sets (important entities, hubs, or event-triggered)
- **Operations**: Heavy calculations, graph building, strategic planning
- **Allowed**: Global pathfinding, graph operations, complex evaluations, batching
- **Forbidden**: Running every tick, processing all entities

**Examples**:
- Strategic route planning: Multi-modal pathfinding
- Graph building: RegionGraph, TransitGraph rebuilds
- Political decisions: Alliance/sanction evaluations
- Trade route building: Global price equilibrium
- History archiving: Recompression, analytics

**Budget**: Very small caps (e.g., MaxStrategicRoutePlansPerTick = 5)

**Code Pattern**:
```csharp
[UpdateInGroup(typeof(ColdPathSystemGroup))]
public partial struct ColdSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
        var counters = SystemAPI.GetSingletonRW<UniversalPerformanceCounters>();
        
        // Only process if event-driven trigger or long interval
        if (timeState.Tick % 100 != 0 && !HasDirtyEvents())
            return;
        
        int processed = 0;
        foreach (var eventEntity in GetEventEntities())
        {
            if (processed >= budget.MaxColdOperationsPerTick)
                break;
            
            // Do heavy work (graph building, strategic planning, etc.)
            processed++;
        }
        
        counters.ValueRW.ColdOperationsThisTick += processed;
    }
}
```

## Domain-Specific Guidelines

### Perception & Knowledge

**Hot**:
- Read AwarenessSnapshot (enemy flags, threat level, alarm state)
- Read KnownFact (nearest enemy, threat direction)
- No raycasts, no distance calculations

**Warm**:
- LOS/vision/hearing checks per group or sensor anchor
- Update shared awareness buffer
- Spatial hashing for "scan" queries
- Every 20-100 ticks, staggered per group

**Cold**:
- Big map visibility recalculations (fog-of-war)
- Global detection networks
- Rebuilding spatial indexes
- Event-driven or every 200+ ticks

### Combat & Damage

**Hot**:
- Apply already-chosen damage (subtract HP, apply modifiers)
- Check death, apply animations
- Light cooldown countdowns
- No target search, no ability evaluation

**Warm**:
- Target selection for units "ready to act" (initiative threshold)
- Small local neighbor set (spatial grid/cell lists)
- Ability/special action selection (bounded options)
- Cap on re-evaluation frequency (every 5-20 ticks)

**Cold**:
- Large battle simulations at empire scale
- Unit template balance, auto-tuning
- War theatre outcome calculations
- Event-driven or every 500+ ticks

### AI Brain Layers

**Reflex (Hot)**:
- Dodge incoming projectile
- Step back from cliff
- Break collision
- Triggered by events or very cheap checks
- Every tick when triggered

**Tactical (Warm)**:
- Per-unit or per-group: Choose stance, retarget, use ability, reposition
- Every few ticks (5-20) or when context changes
- Budget: MaxTacticalDecisionsPerTick

**Operational (Cold-ish)**:
- Per band/army/fleet: Where to patrol, which town to besiege, which front to reinforce
- Every tens/hundreds of ticks (50-200), plus event-driven

**Strategic (Cold)**:
- Per faction/empire: Declare war, set war goals, allocate fronts, major projects
- Very infrequent (200+ ticks) or event-driven

### Jobs & Schedules

**Hot**:
- Follow current job step (move to work location)
- Perform job anim/loop with simple timers
- No job re-selection every tick

**Warm**:
- Work/sleep/eat schedule evaluation (only when thresholds cross)
- Reassign jobs when work done/workplace changed/skills changed
- Every 20-100 ticks, staggered per villager

**Cold**:
- Large-scale job market balancing
- Guild apprentice allocation
- Workforce reallocation across village/colony
- Policy-driven reorganization
- Event-driven or every 500+ ticks

### World Sim (Weather, Fire, Disease, Ecology, Power)

**Hot**:
- Apply fire damage to entities in burning cells
- Apply current weather modifiers to accuracy/movement
- Apply current power coverage to production
- Read snapshots only, no calculations

**Warm**:
- Update cell-level state (fire spread, disease spread, pollution, flood levels)
- Power grid coverage and outages
- Work in chunks, update only "active" cells
- Use "next update tick" per chunk
- Every 10-50 ticks, staggered per chunk

**Cold**:
- Global climate shifts (seasonal changes, long-term terraforming)
- Re-seeding weather patterns
- Recalculation of large-scale connectivity
- Event-driven or every 200+ ticks

### Relations & Social

**Hot**:
- Read LoyaltyState (ToBand, ToFaction, BetrayalRisk)
- Read OrgStandingSnapshot (Attitude, Trust, Fear)
- Read PersonalRelation buffer (bounded 8-16 entries)
- No graph traversals, no relation calculations

**Warm**:
- Aggregate loyalty from lower-level data
- Update OrgStandingSnapshot from OrgRelation edges
- Update PersonalRelation buffer after direct interactions
- Every 20-100 ticks, staggered per group

**Cold**:
- Create/destroy OrgRelation edges (sparse graph)
- Long-term drift in inter-org relations
- Deep political/economic evaluations
- Event-driven or every 100-200 ticks

### Navigation

**Hot**:
- Follow already-computed paths (next waypoint)
- Simple steering/formation behavior
- Apply velocity to position
- No pathfinding calls

**Warm**:
- Local pathfinding within a region (short A* on small grid)
- Group-level "where are we going next?" decisions
- Simple replan when path is blocked
- Budget: MaxLocalPathQueriesPerTick

**Cold**:
- Long-range multi-modal route planning
- Graph building & updates (RegionGraph, TransitGraph)
- Strategic decisions ("invade here", "move supply line there")
- Budget: MaxStrategicRoutePlansPerTick

### Time, Rewind & Logging

**Hot**:
- Read current TickTimeState
- Systems respect RewindState.Mode and early-out on Playback
- Write into small ring buffers for critical fast histories only

**Warm**:
- History recording for important entities/domains (sample rate based on importance)
- Logs/events for narrative or debugging
- Budget: MaxHistoryRecordsPerTick

**Cold**:
- Full history recompression/archiving
- Expensive rewinds/scrubbing with UI
- Rebuilding derived histories (graphs, analytics)
- Event-driven or every 1000+ ticks

## Budget Recommendations

### Hot Path
- **No throttling needed**: O(N) simple operations are acceptable
- **Monitor**: Track total entities processed per tick
- **Warning threshold**: If processing >100K entities/tick, consider optimization

### Warm Path
- **Per-domain budgets**: 10-100 operations per tick per domain
- **Examples**:
  - MaxLocalPathQueriesPerTick: 50
  - MaxPerceptionChecksPerTick: 20
  - MaxJobReassignmentsPerTick: 15
  - MaxCombatOperationsPerTick: 30
- **Warning threshold**: If consistently hitting budget, increase cadence or optimize

### Cold Path
- **Very small budgets**: 1-10 operations per tick per domain
- **Examples**:
  - MaxStrategicRoutePlansPerTick: 5
  - MaxPoliticalDecisionsPerTick: 5
  - MaxGraphRebuildsPerTick: 2
- **Warning threshold**: If consistently hitting budget, increase intervals or batch more

## Common Anti-Patterns

### 1. Hot Path Doing Warm Work

**Bad**:
```csharp
// Hot path doing pathfinding every tick
foreach (var entity in entities)
{
    var path = FindPath(entity.Position, target.Position); // ❌ Expensive!
}
```

**Good**:
```csharp
// Hot path reads pre-computed path
foreach (var (path, entity) in SystemAPI.Query<RefRO<NavPath>>())
{
    var nextWaypoint = path.ValueRO.Waypoints[path.ValueRO.CurrentIndex]; // ✅ Read only
}
```

### 2. No Budget Enforcement

**Bad**:
```csharp
// Processing unlimited entities
foreach (var entity in allEntities) // ❌ No limit!
{
    DoExpensiveWork(entity);
}
```

**Good**:
```csharp
// Respecting budget
var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
int processed = 0;
foreach (var entity in entities)
{
    if (processed >= budget.MaxOperationsPerTick) break; // ✅ Budget enforced
    DoExpensiveWork(entity);
    processed++;
}
```

### 3. No Staggering

**Bad**:
```csharp
// All entities update on same tick
if (timeState.Tick % 20 == 0) // ❌ All update together!
{
    foreach (var entity in allEntities)
        Update(entity);
}
```

**Good**:
```csharp
// Staggered updates
foreach (var (cadence, entity) in SystemAPI.Query<RefRO<UpdateCadence>>())
{
    if (UpdateCadenceHelpers.ShouldUpdate(timeState.Tick, cadence.ValueRO)) // ✅ Staggered
        Update(entity);
}
```

### 4. N² Operations

**Bad**:
```csharp
// Checking all pairs
for (int i = 0; i < entities.Length; i++) // ❌ N²!
{
    for (int j = i + 1; j < entities.Length; j++)
    {
        CheckRelation(entities[i], entities[j]);
    }
}
```

**Good**:
```csharp
// Sparse graph (only stored relations)
foreach (var relation in SystemAPI.Query<RefRO<OrgRelation>>()) // ✅ Only stored pairs
{
    ProcessRelation(relation.ValueRO.OrgA, relation.ValueRO.OrgB);
}
```

### 5. No LOD

**Bad**:
```csharp
// Same detail for all entities
foreach (var entity in allEntities) // ❌ No LOD!
{
    DoExpensiveDetailedWork(entity);
}
```

**Good**:
```csharp
// LOD-based detail
foreach (var (importance, entity) in SystemAPI.Query<RefRO<AIImportance>>())
{
    if (importance.ValueRO.Level == 0) // Hero: full detail
        DoExpensiveDetailedWork(entity);
    else if (importance.ValueRO.Level <= 2) // Important/Normal: moderate detail
        DoModerateWork(entity);
    else // Background: simple heuristic
        DoSimpleWork(entity);
}
```

## Classification Decision Tree

1. **Does it run every tick on many entities?**
   - Yes → **Hot**: Must be O(1) reads only
   - No → Continue

2. **Does it do moderate calculations (pathfinding, aggregation, sampling)?**
   - Yes → **Warm**: Needs budgets and staggering
   - No → Continue

3. **Does it do heavy work (graph building, strategic planning, global operations)?**
   - Yes → **Cold**: Event-driven or long intervals
   - No → Re-evaluate: Might be misclassified

4. **Is it event-driven or runs rarely?**
   - Yes → **Cold**
   - No → Re-evaluate classification

## Conclusion

Proper heat tier classification is critical for performance. When in doubt:
- **Hot**: Only if it MUST run every tick and is O(1) reads
- **Warm**: If it can be throttled/staggered and does moderate work
- **Cold**: If it can be event-driven or run infrequently

Remember: It's better to be conservative and classify as Warm/Cold than to incorrectly classify as Hot and cause frame time issues.

