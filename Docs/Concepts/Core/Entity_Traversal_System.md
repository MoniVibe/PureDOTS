# Entity Traversal System

**Status:** Draft  
**Category:** Core / Movement / Navigation  
**Applies To:** Godgame, Space4X, shared PureDOTS  
**Created:** 2025-01-XX  
**Last Updated:** 2025-01-XX

---

## Purpose

This document defines a **traversal graph system** layered on top of standard ground navigation. The system enables entities to use **jump, climb, crawl, squeeze, and drop** as special traversal edges, making unreachable places reachable and creating meaningful size-based connectivity differences.

**Core Principle:** Treat traversal actions (jump/climb/crawl/squeeze/drop) as **edges in a traversal graph** rather than special-case movement modes. This provides connectivity changes, size gating, and good performance through sparse link checks.

---

## Core Concept

### Traversal as Graph Edges

**Walk Edges:**
- Cheap, always available
- Standard ground navigation
- No special requirements

**Traversal Edges:**
- **JumpLink:** Jump across gaps, up to ledges
- **ClimbLink:** Climb ladders, vines, cliff faces
- **CrawlLink:** Crawl through low passages
- **SqueezeLink:** Squeeze through tight spaces (often small radius + crawl)
- **DropLink:** Drop down from heights

Each traversal edge has:
- **Requirements:** Capabilities needed (size, mobility, equipment)
- **Cost:** Time/energy/risk for pathfinding decisions
- **Execution Recipe:** How to perform the traversal (arc, spline, corridor)

### Benefits

**Connectivity Changes:**
- Unreachable places become reachable
- Small entities can access areas large entities cannot
- Creates tactical routes and escape paths

**Size Gating:**
- Small folk use holes, big folk can't
- Meaningful size differences affect gameplay
- World readability (players learn "that's a goblin hole")

**Performance:**
- Links are sparse (only where needed)
- Checks are simple (fit + capable)
- Doesn't affect every entity (selective application)

---

## Entity Requirements

### Body Dimensions

Entities need **body dimension data** to determine if they can fit through traversal links:

```csharp
public struct BodyDimensions : IComponentData
{
    /// <summary>Radius or shoulder width proxy (for squeeze clearance)</summary>
    public float Radius;
    
    /// <summary>Standing height (for crawl/clearance checks)</summary>
    public float StandingHeight;
    
    /// <summary>Optional: Crouch height (or derive as multiplier, e.g., 0.7x standing)</summary>
    public float CrouchHeight;
    
    /// <summary>Optional: Crawl height (or derive as multiplier, e.g., 0.4x standing)</summary>
    public float CrawlHeight;
}
```

**Derivation Rules:**
- If `CrouchHeight` not provided: `CrouchHeight = StandingHeight * 0.7f`
- If `CrawlHeight` not provided: `CrawlHeight = StandingHeight * 0.4f`
- `Radius` typically derived from entity size/archetype

### Mobility Capabilities

Entities need **mobility capability data** to determine what traversal actions they can perform:

```csharp
public struct MobilityCaps : IComponentData
{
    /// <summary>Maximum horizontal jump distance</summary>
    public float MaxJumpDistance;
    
    /// <summary>Maximum vertical jump height (upward)</summary>
    public float MaxJumpUp;
    
    /// <summary>Maximum safe drop height (downward landing tolerance)</summary>
    public float MaxDropDown;
    
    /// <summary>Can this entity climb? (ladders, vines, cliff faces)</summary>
    public bool CanClimb;
    
    /// <summary>Climbing speed multiplier (0-1, typically slower than walking)</summary>
    public float ClimbSpeed;
    
    /// <summary>Can this entity crawl? (low passages, tunnels)</summary>
    public bool CanCrawl;
    
    /// <summary>Crawling speed multiplier (0-1, typically much slower)</summary>
    public float CrawlSpeedMultiplier;
    
    /// <summary>Can this entity squeeze? (tight spaces, often synonymous with small radius + crawl)</summary>
    public bool CanSqueeze;
}
```

**Squeeze Logic:**
- `CanSqueeze` is often synonymous with `(Radius < threshold) && CanCrawl`
- Squeeze = minimum clearance radius/height + slow + one-at-a-time reservation
- Small entities (goblins, children) typically have `CanSqueeze = true`

---

## World Representation

### Traversal Links

Traversal links are **data entities** (or buffers on nav nodes) that represent special traversal opportunities in the world.

**Link Structure:**
```csharp
public struct TraversalLink : IComponentData
{
    public TraversalType Type;  // Jump/Climb/Crawl/Squeeze/Drop
    
    public float3 StartPosition;  // Or node index reference
    public float3 EndPosition;    // Or node index reference
    
    /// <summary>Maximum radius that can use this link</summary>
    public float MaxRadius;
    
    /// <summary>Maximum height (in required stance) that can use this link</summary>
    public float MaxHeight;
    
    /// <summary>Required stance: Standing, Crouching, Crawling</summary>
    public Stance RequiredStance;
    
    /// <summary>Requirements: flags/tags (needs hands, needs climbable surface, etc.)</summary>
    public TraversalRequirements Requirements;
    
    /// <summary>Pathfinding cost (time/energy/risk)</summary>
    public float Cost;
    
    /// <summary>Execution parameters (type-specific)</summary>
    public TraversalExecutionParams ExecutionParams;
}

public enum TraversalType : byte
{
    Jump = 0,
    Climb = 1,
    Crawl = 2,
    Squeeze = 3,
    Drop = 4
}

public enum Stance : byte
{
    Standing = 0,
    Crouching = 1,
    Crawling = 2
}

public struct TraversalRequirements : IComponentData
{
    public bool NeedsHands;           // For climbing
    public bool NeedsClimbableSurface; // For climbing
    public bool NeedsSmallSize;       // For squeeze
    // ... other requirements
}

public struct TraversalExecutionParams : IComponentData
{
    // Type-specific execution data
    // Jump: arc height, takeoff/landing normals, snap tolerances
    // Climb: spline/ladder endpoints, handholds (optional)
    // Crawl/Squeeze: corridor path, speed multiplier, single-file reservation
}
```

### Link Clearance

**Clearance Requirements:**
- **MaxRadius:** Maximum entity radius that can use this link
- **MaxHeight:** Maximum entity height (in required stance) that can use this link
- **RequiredStance:** What stance the entity must be in (Standing/Crouching/Crawling)

**Clearance Checks:**
- Entity fits if: `agent.Radius <= link.MaxRadius && agent.Height(link.RequiredStance) <= link.MaxHeight`
- Example: Small goblin (radius 0.3, height 0.8) can use squeeze link (max radius 0.4, max height 1.0, stance: crawling)
- Example: Large warrior (radius 0.6, height 1.8) cannot use same squeeze link

### Execution Parameters

**Jump Execution:**
- Arc height (parabola or bezier curve)
- Takeoff/landing normals (surface alignment)
- "Snap" tolerances (landing position correction)
- Fixed-time arc from Start to End

**Climb Execution:**
- Spline or ladder axis path
- Optional handholds (for complex climbs)
- Rotation to align with surface normal
- Speed based on `ClimbSpeed` multiplier

**Crawl/Squeeze Execution:**
- Corridor path (spline or waypoints)
- Speed multiplier (typically 0.3-0.5x walking speed)
- Single-file reservation (only one entity at a time)
- Optional stance change (logical, not necessarily collider resizing)

**Drop Execution:**
- Safe drop height check
- Landing tolerance
- Optional damage if drop too far

---

## Authoring & Baking

### Hand-Authoring (Phase 1-3)

**For smoke scenes and initial implementation, hand-author traversal links:**

**JumpMarkers:**
- Place A ↔ B markers across chasms
- Define arc parameters, clearance requirements
- Visual gizmos showing allowed size bands

**Hole/Tunnel Volumes:**
- Define crawl/squeeze corridors
- Set clearance (max radius, max height)
- Mark as single-file or multi-file

**Climb Volumes:**
- Ladders, vines, cliff faces
- Define climb path (spline or axis)
- Set requirements (needs hands, climbable surface)

**Authoring Tools:**
- **Link Gizmos:** In-editor visualization showing:
  - Allowed size bands (radius/height ranges)
  - Required stance
  - Why an agent can't use it (debug reason codes)
  - Execution path preview

### Auto-Baking (Phase 4, Optional)

**Later enhancement:**
- Ledge detection on terrain (auto-create jump links)
- Gap detection (auto-create jump links across gaps)
- Tunnel detection (auto-create crawl/squeeze links)
- **Still keep manual overrides** for designer control

---

## Pathfinding Integration

### Traversability Checks

When pathfinding, edges are traversable if:

**1. Fits:**
```
agent.Radius <= link.MaxRadius
agent.Height(link.RequiredStance) <= link.MaxHeight
```

**2. Capable:**
- **JumpLink:** `distance <= agent.MaxJumpDistance && height <= agent.MaxJumpUp`
- **ClimbLink:** `agent.CanClimb == true`
- **CrawlLink:** `agent.CanCrawl == true` (or can crouch)
- **SqueezeLink:** Usually both `fit` + `agent.CanCrawl` (or `agent.CanSqueeze`)

**3. Preferable:**
- Use A* cost = `walk_distance + traversal_cost`
- Traversal cost should be **high enough** that entities only use it when it matters
- Prevents everyone from bunny-hopping everywhere
- Example: Jump cost = 2.0x walk distance, Climb cost = 3.0x walk distance

### Pathfinding Algorithm

**A* with Traversal Edges:**
1. Standard ground navigation (walk edges) - cheap, always available
2. Traversal edges (jump/climb/crawl/squeeze) - expensive, conditional
3. Pathfinding considers both, prefers walk when possible
4. Uses traversal only when necessary to reach destination

**Flow-Field Optimization:**
- Keep crowds on normal nav (walk edges only)
- Do traversal only for "agents that matter":
  - Heroes, bands, special units
  - Scouts, thieves, goblins (small entities)
  - Scripted world moments (raiders crossing ravine)
- Best performance/feel compromise

---

## Execution: Deterministic Motion

### Kinematic Motion (Not Physics)

To achieve the **Baldur's Gate 3 "vibe,"** traversal execution should be **scripted kinematic motion** (parametric), not physically simulated chaos.

**Jump Execution:**
- Fixed-time arc from Start to End (parabola or bezier)
- Landing snap tolerance (snap to ground if within threshold)
- Deterministic timing, predictable results

**Climb Execution:**
- Move along spline or ladder axis
- Optionally rotate to align with surface normal
- Speed based on `ClimbSpeed` multiplier
- Deterministic path, no physics jitter

**Crawl/Squeeze Execution:**
- Move along corridor path (spline or waypoints)
- Reduce speed by `CrawlSpeedMultiplier`
- Optionally change stance (purely logical)
- Single-file reservation prevents collisions

### Collider Resizing (Optional)

**When to Resize:**
- **Don't resize by default** - expensive and tricky in DOTS physics
- For "meaningful size," you can get 90% of gameplay by:
  - Enforcing clearance at planning time (link gating)
  - Enforcing one-at-a-time reservation inside squeeze corridors
  - Optionally using simplified trigger volumes

**When You Need Real Resizing:**
- Only if you want emergent crawling under arbitrary obstacles
- Not needed for authored tunnels/holes
- Adds complexity and performance cost

---

## Meaningful Size Gameplay

### What Size Affects

**Connectivity:**
- Small entities can reach places others can't
- Creates tactical advantages (escape routes, ambush positions)
- World readability (players learn "that's a goblin hole")

**Tactical Routes:**
- Escape holes (small entities can flee)
- Ambush tunnels (small entities can surprise)
- Hidden passages (size-gated access)

**World Readability:**
- Visual language: holes look like "goblin-sized"
- Players learn size-based navigation rules
- Clear feedback when entity can't fit

### Authoring Support

**Strong Visual Language:**
- Holes with clear size indicators
- Link gizmos showing allowed size bands
- Debug visualization of why entities can't use links

**Link Gizmos Should Show:**
- Allowed size bands (radius/height ranges)
- Required stance (standing/crouching/crawling)
- Why an agent can't use it (debug reason codes):
  - "Too large (radius 0.6 > max 0.4)"
  - "Too tall (height 1.8 > max 1.0)"
  - "Cannot climb (CanClimb = false)"
  - "Cannot crawl (CanCrawl = false)"

---

## Rollout Plan (Pragmatic Phases)

### Phase 1: JumpLinks Only

**Scope:**
- Hand-authored jump points across chasms
- A* supports traversal edges
- Deterministic arc execution
- Basic clearance checks (radius/height)

**Entities:**
- Heroes, bands, special units only
- Not for every villager (performance)

**Deliverables:**
- Jump link authoring tools
- A* traversal edge support
- Jump execution system
- Basic clearance gating

### Phase 2: Squeeze/Crawl

**Scope:**
- Tunnel volumes with clearance + reservations
- Small races get real "access privilege"
- Single-file reservation system
- Crawl execution (corridor movement)

**Entities:**
- Small entities (goblins, children, scouts)
- Heroes and special units
- Not for crowds (performance)

**Deliverables:**
- Crawl/squeeze link authoring
- Single-file reservation system
- Crawl execution system
- Size-based connectivity gameplay

### Phase 3: Climb

**Scope:**
- Ladder/vine/cliff climb volumes
- Climb execution (spline movement)
- Optional stamina/cost
- Surface alignment

**Entities:**
- Heroes, bands, special units
- Entities with `CanClimb = true`
- Not for crowds (performance)

**Deliverables:**
- Climb link authoring
- Climb execution system
- Surface alignment
- Optional stamina system

### Phase 4: Auto-Baking (Optional)

**Scope:**
- Ledge/gap detection on terrain
- Auto-generate jump links
- Tunnel detection
- **Still keep manual overrides** for designer control

**Deliverables:**
- Auto-baking tools
- Terrain analysis
- Manual override system
- Validation tools

---

## Performance Considerations

### Key Warning

**Don't give traversal to every villager by default.** It will:
- **Explode pathfinding branching:** Every entity considers traversal edges, massive search space
- **Create visual noise:** "Why is everyone hopping?" - unrealistic behavior
- **Kill crowd performance targets:** Too expensive for large populations

### Selective Application

**Use traversal for:**
- **Heroes / Bands / Creatures:** Important entities that need tactical movement
- **Special Villagers:** Scouts, thieves, goblins (small entities with unique capabilities)
- **Scripted World Moments:** Raiders crossing ravine, special events

**Don't use for:**
- **Crowds:** Standard ground nav only
- **Generic Villagers:** Unless they're special (scout, thief, etc.)
- **Background Entities:** Performance optimization

### Performance Optimizations

**Sparse Links:**
- Only create links where needed (not everywhere)
- Links are expensive to check, keep them sparse

**Selective Pathfinding:**
- Flow-fields for crowds (no traversal)
- A* with traversal for special entities only

**Caching:**
- Cache traversability checks per entity type
- Pre-compute which links each entity type can use

**LOD System:**
- Simple traversal for distant entities
- Full traversal only for nearby/important entities

---

## LOD Fidelity and Reachability

### Clean Rule (Reachability Is Constant)

If a place is only reachable via traversal links, agents that cannot traverse those links cannot reach it.
Do not allow "magical climbing when LOD'd" unless a real alternate traversal exists (ladder, rope, elevator, etc.).

### Same Path, Different Fidelity

**LOD0 (near camera / important agents):**
- Simulate every hop: takeoff -> arc -> land on each platform node.

**LOD1 (far but active):**
- Still follow the same intermediate platform nodes.
- Skip expensive physics/probes; move along a precomputed arc over a fixed time per hop.

**LOD2 (very far / background):**
- Validate traversal (caps + clearance + link existence).
- Book "in traversal" for T seconds (based on hop count / distances).
- Place at the destination node when the timer completes.

**Visibility Upgrade Rule:**
- If the agent becomes visible mid-traversal, bump LOD2 -> LOD1/0 and play remaining hops.
- Never pop/teleport in view.

### Practical Content Rule

If you want non-jumpers to still access a region, give them a real alternate edge:
- ClimbLink (ladder/vines)
- Slope/ramp path
- Assist mechanics (rope dropped by a climber, bridge built, miracle interaction)

Otherwise, mark it as "requires Jump traversal" and let pathfinding return unreachable.

---

## Implementation Notes

### PureDOTS Integration

**Components:**
- `BodyDimensions : IComponentData` - Entity size data
- `MobilityCaps : IComponentData` - Entity capability data
- `TraversalLink : IComponentData` - World traversal link data
- `TraversalExecutionState : IComponentData` - Runtime execution state

**Systems:**
- `TraversalPathfindingSystem` - A* with traversal edges
- `TraversalExecutionSystem` - Kinematic motion execution
- `TraversalReservationSystem` - Single-file corridor management
- `TraversalClearanceSystem` - Fit/capability checks

**Authoring:**
- `TraversalLinkAuthoring` - MonoBehaviour for hand-authoring
- `TraversalVolumeAuthoring` - Volumes for crawl/squeeze
- Editor gizmos for visualization

### Game-Specific Adaptations

**Godgame:**
- Villagers use traversal for tactical movement
- Goblins/small creatures get squeeze access
- Heroes use all traversal types

**Space4X:**
- Crew members use traversal for ship interior navigation
- Different size classes (human, alien, robot)
- Zero-g traversal (different mechanics)

---

## Related Documentation

- **Navigation Systems:** `Concepts/Core/Locomotion_System.md`
- **Entity Stats:** `Concepts/Core/Entity_Stats_And_Archetypes_Canonical.md`
- **Forces System:** `Concepts/Core/General_Forces_System.md`

---

## Open Questions

1. **Collider Resizing:** When is real collider resizing needed vs logical stance changes?
2. **Stamina System:** Should traversal actions consume stamina/focus? (Phase 3 consideration)
3. **Multi-Entity Coordination:** How to handle bands/groups using traversal together?
4. **Animation Integration:** How to trigger traversal animations from execution system?
5. **Failure Handling:** What happens when traversal execution fails? (entity stuck, fall damage, etc.)

---

**For Designers:** Use traversal links to create meaningful size-based gameplay. Small entities should have tactical advantages through access to areas others can't reach.

**For Implementers:** Start with Phase 1 (JumpLinks only) for heroes/special units. Don't enable for crowds until performance is validated.

**For Artists:** Create clear visual language for traversal opportunities. Holes should look "goblin-sized," ledges should be obviously climbable.

---

**Last Updated:** 2025-01-XX  
**Status:** Draft - Ready for implementation planning and Phase 1 rollout

