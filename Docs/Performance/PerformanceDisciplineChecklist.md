# Performance Discipline Checklist

## Overview

Every new feature/system must pass through this checklist to ensure it follows the hot/warm/cold performance discipline. This prevents N² operations, frame time spikes, and ensures scalability to millions of entities.

## Pre-Development Questions

Before implementing any new system, answer these questions:

### 1. What are the hot-path reads?

**Question**: Which 2-4 numbers/scalars does AI/logic need to read every tick?

**Examples:**
- Navigation: Current waypoint position, distance to goal
- Combat: Current HP, nearest enemy distance
- Perception: Threat level, nearest enemy position
- Jobs: Current job step, work location

**Guideline**: Hot path should only read pre-computed scalars. No calculations, no graph traversals, no N² checks.

### 2. Can I precompute them in a warm system?

**Question**: Can these hot-path values be pre-computed and cached?

**Examples:**
- LoyaltyState: Pre-computed loyalty values (ToBand, ToFaction, BetrayalRisk)
- AwarenessSnapshot: Pre-computed "what I currently know" (enemy flags, threat level)
- JobStateSnapshot: Current job step, work location

**Guideline**: If values change infrequently, compute them in warm systems and cache them for hot path consumption.

### 3. What events can I hook instead of polling?

**Question**: Can I make this event-driven instead of checking every tick?

**Examples:**
- Job reassignment: Trigger when work done/workplace destroyed, not every tick
- Relation updates: Trigger on war/alliance events, not continuous polling
- Power updates: Trigger when network changes, not every tick

**Guideline**: Prefer event-driven updates over polling. Use dirty flags, event entities, or change detection.

### 4. Can I make this group-level instead of per-unit?

**Question**: Can group anchors do the heavy work, with individuals consuming cheap results?

**Examples:**
- Perception: Squad leader does LOS checks, shares awareness with group
- Navigation: Group leader plans route, members follow
- Combat: Group evaluates targets, individuals use cached results

**Guideline**: Group-level computation dramatically reduces N² operations. Only named heroes/special units get individual computation.

### 5. What are my budgets and cadences?

**Question**: How many operations per tick? How often per entity/org?

**Examples:**
- MaxPerceptionChecksPerTick: 20 checks/tick
- MaxJobReassignmentsPerTick: 15 reassignments/tick
- UpdateCadence: Every 20 ticks for normal entities, every 5 for important

**Guideline**: 
- Hot: Every tick, no throttling
- Warm: Throttled (K operations/tick), staggered (every N ticks)
- Cold: Event-driven or long intervals (50-200 ticks)

### 6. What's the LOD story?

**Question**: How does this behave for background vs important entities?

**Examples:**
- Importance 0 (Hero): Every tick, full detail
- Importance 1 (Important): Every 5 ticks, detailed
- Importance 2 (Normal): Every 20 ticks, moderate detail
- Importance 3 (Background): Every 100 ticks, coarse/approximate

**Guideline**: Use AIImportance component to scale update frequency and detail level. Background entities use simplified heuristics.

### 7. What's the N² trap here?

**Question**: Am I ever touching "all pairs" of anything?

**Examples:**
- ❌ Bad: Every tick, check every entity against every other entity
- ✅ Good: Only check entities in same spatial cell, or use sparse relation graph
- ❌ Bad: Full scene scan for targets
- ✅ Good: Spatial grid lookup, bounded candidate list

**Guideline**: 
- Use spatial hashing/grids for proximity queries
- Use sparse graphs (only store salient pairs)
- Use sampling (aggregate from N members, not all)
- Cap candidate lists (20-100 max)

## Domain-Specific Checklists

### Perception & Knowledge

- [ ] Hot path reads AwarenessSnapshot (enemy flags, threat level) only
- [ ] LOS checks done per group/sensor anchor, not per entity
- [ ] Awareness shared via group buffer
- [ ] Spatial hashing used for "scan" queries
- [ ] Budget: MaxPerceptionChecksPerTick enforced
- [ ] Staggered updates per group (every N ticks)

### Combat & Damage

- [ ] Hot path applies already-chosen damage only
- [ ] Target selection only for units "ready to act" (initiative threshold)
- [ ] Small local neighbor set (spatial grid), not full scene scan
- [ ] Ability selection with bounded options (fixed number)
- [ ] Cap on re-evaluation frequency (every N ticks)
- [ ] Budget: MaxCombatOperationsPerTick enforced

### AI Brain Layers

- [ ] Reflex layer (HOT): Event-driven, very cheap checks
- [ ] Tactical layer (WARM): Every few ticks, when context changes
- [ ] Operational layer (COLD-ish): Every tens/hundreds of ticks
- [ ] Strategic layer (COLD): Very infrequent, event-driven
- [ ] Each layer respects appropriate budgets and cadences

### Jobs & Schedules

- [ ] Hot path follows current job step only (no re-selection)
- [ ] Schedule evaluation only when time-of-day/thresholds cross
- [ ] Job reassignment only when work done/workplace changed
- [ ] Staggered updates per villager
- [ ] Budget: MaxJobReassignmentsPerTick enforced
- [ ] Large-scale balancing in cold path

### World Sim (Weather, Fire, Disease, Ecology, Power)

- [ ] Hot path reads snapshots only (fire damage, weather modifiers)
- [ ] Cell updates in chunks, only "active" cells
- [ ] Use "next update tick" per chunk for staggering
- [ ] Only simulate where something is happening
- [ ] Global climate shifts in cold path
- [ ] Budget: MaxCellUpdatesPerTick enforced

### Relations & Social

- [ ] Hot path reads LoyaltyState/OrgStandingSnapshot only
- [ ] Personal relations bounded (8-16 entries max)
- [ ] Sparse org relation graph (only active pairs)
- [ ] Event-driven updates, not every tick
- [ ] Slow decay (every 100-200 ticks)
- [ ] Budget: MaxRelationEventsPerTick enforced

### Navigation

- [ ] Hot path follows existing paths only (no pathfinding)
- [ ] Local pathfinding throttled (MaxLocalPathQueriesPerTick)
- [ ] Strategic planning in cold path
- [ ] Group-level path planning, individuals follow
- [ ] Staggered updates per entity

### Time, Rewind & Logging

- [ ] Hot path reads RewindStateSnapshot only
- [ ] History recording with sample rates based on importance
- [ ] Not every tick for everyone
- [ ] Expensive rewinds/archiving in cold path
- [ ] Budget: MaxHistoryRecordsPerTick enforced

### Narrative & Situation Triggers

- [ ] Hot path consumes already-fired beats only
- [ ] Situation updates every M ticks, not every tick
- [ ] Few conditions per situation, not scanning entire world
- [ ] Trigger discovery uses precomputed scores
- [ ] Region-based or subscribed triggers, not entity list scan

### Power & Infrastructure

- [ ] Hot path reads PowerStateSnapshot only
- [ ] Power flow solve periodically, not every tick
- [ ] Network rebuilding event-driven
- [ ] Route optimization in cold path

## Enforcement Guidelines

### Code Review Checklist

When reviewing code, verify:

1. **Hot path systems**:
   - [ ] No allocations
   - [ ] No pathfinding/graph traversals
   - [ ] No N² operations
   - [ ] Only reads pre-computed scalars
   - [ ] Branch-light, data-tight

2. **Warm path systems**:
   - [ ] Respects budget (K operations/tick)
   - [ ] Uses UpdateCadence for staggering
   - [ ] Samples instead of full iteration
   - [ ] Group-level when possible

3. **Cold path systems**:
   - [ ] Event-driven or long intervals
   - [ ] Sparse graph maintenance
   - [ ] Batched operations
   - [ ] Only for important entities/hubs

### Testing Checklist

- [ ] Performance tests with 10K/100K/1M entities
- [ ] Verify no N² operations in hot path
- [ ] Monitor graph sizes and edge counts
- [ ] Test budget enforcement
- [ ] Verify staggered updates spread load
- [ ] Test LOD scaling (importance 0-3)

### Common Anti-Patterns to Avoid

1. **Every-tick polling**: Checking conditions every tick when events would work
2. **Full scene scans**: Iterating all entities instead of spatial grids
3. **N² relations**: Tracking all pairwise relations instead of sparse graphs
4. **No budgets**: Unlimited operations per tick
5. **No staggering**: All entities update on same tick
6. **No LOD**: Same detail level for all entities
7. **Hot path calculations**: Computing values in hot path instead of caching
8. **Per-unit heavy work**: Individual entities doing expensive operations

## Examples

### Good Example: Perception System

**Hot Path** (every tick):
- Read AwarenessSnapshot: `NearestEnemyDistance`, `ThreatLevel`, `AlarmState`
- Use for AI decisions (fight/flee)

**Warm Path** (every 20 ticks, per group):
- Squad leader does LOS checks
- Updates shared awareness buffer
- Respects MaxPerceptionChecksPerTick budget

**Cold Path** (event-driven):
- Fog-of-war recalculation
- Global detection network updates

### Bad Example: Naive Perception

**Every tick, per entity**:
- Raycast to all entities in scene
- Calculate distances to all potential targets
- Evaluate threat for all entities

**Problems**:
- N² operations (every entity checks every other entity)
- Expensive raycasts every tick
- No budgets or staggering
- No group-level optimization

## Conclusion

Following this checklist ensures all systems scale to millions of entities without frame time degradation. The discipline is simple: hot path reads scalars, warm path computes them, cold path maintains graphs. Budgets, cadences, and LOD ensure work is distributed and prioritized.

