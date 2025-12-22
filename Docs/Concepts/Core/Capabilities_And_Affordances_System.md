# Capabilities and Affordances System

**Status:** Concept
**Category:** Core - Movement & Interaction
**Scope:** Cross-Project (PureDOTS Foundation)
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Enable entities to interact with the world based on their anatomy/capabilities (limbs/stats), while the world exposes what can be done (affordances), and navigation integrates special movement types (climb/jump/swim/fly/dig) rather than just walking.

**Secondary Goals:**
- Anatomy-driven capabilities (stats + limbs determine what's possible)
- World-authored affordances (ladders, ledges, climb surfaces explicitly defined)
- Navigation graph with traversal edges (special movement types as graph edges)
- Minimal action primitives (intent-based, not micromovement)
- Self-aware rerouting (avoid risky edges without heavy planning)
- Medium-driven modes (same code works in Godgame + Space4X)
- Performance guardrails (no per-agent surface queries, dirty recompute, LOD)

**Key Principle:** Limbs/stats decide what you can do, the world exposes what can be done here, navigation picks a route that includes traversal actions, and execution consumes stamina + applies failure risk.

---

## 1. Anatomy as Data (Not Bespoke Code)

### Core Concept

**Anatomy is data-driven, not hardcoded.** Stats, limbs, and status flags determine capabilities. Missing limbs reduce capabilities (e.g., one arm halves climb grade).

### Component Structure

```csharp
// Per entity: stats (existing system integration)
public struct EntityStats : IComponentData
{
    public float Strength;      // Physical power
    public float Physique;      // Endurance, health
    public float Finesse;       // Dexterity, precision
    public float Will;          // Mental strength
    public float Wisdom;        // Knowledge, awareness
    // ... other stats
}

// Limb state (existing: LimbState buffer)
[InternalBufferCapacity(8)]
public struct LimbState : IBufferElementData
{
    public LimbType Limb;                    // Arm, Leg, Wing, Fin, Tail, Tool
    public float Manipulation;               // 0-1: fine motor control
    public float Force;                      // 0-1: strength capability
    public float Grip;                       // 0-1: grasping/holding ability
    public float Health01;                   // 0-1: limb health
    public float Stamina01;                  // 0-1: limb stamina (local)
    public LimbStatusFlags StatusFlags;      // Broken, Numb, Bleeding, Encumbered, Augmented
    public AugmentMask Augment;              // Gecko pads, magboots, fins, claws
}

public enum LimbType : byte
{
    Arm = 0,
    Leg = 1,
    Wing = 2,
    Fin = 3,
    Tail = 4,
    Tool = 5,
    Head = 6
}

[Flags]
public enum LimbStatusFlags : byte
{
    None = 0,
    Broken = 1 << 0,         // Cannot use limb
    Numb = 1 << 1,           // Reduced capability
    Bleeding = 1 << 2,       // Drains stamina
    Encumbered = 1 << 3,     // Carrying heavy load
    Augmented = 1 << 4       // Has augment (gecko pads, etc.)
}

[Flags]
public enum AugmentMask : uint
{
    None = 0,
    GeckoPads = 1 << 0,      // Climb smooth surfaces
    Magboots = 1 << 1,       // Stick to metal (zero-G)
    Fins = 1 << 2,           // Improved swimming
    Claws = 1 << 3,          // Climb without hands
    Wings = 1 << 4,          // Flight capability
    Propulsion = 1 << 5      // Thrusters (Space4X)
}
```

### Derived Capabilities (Computed When Dirty)

```csharp
// Mobility capability cache (computed from anatomy)
public struct MobilityCapability : IComponentData
{
    // Jump capabilities
    public float MaxJumpHeight;              // Maximum vertical jump distance
    public float MaxJumpGap;                 // Maximum horizontal jump distance
    
    // Climb capabilities
    public float ClimbGrade;                 // 0-N: maximum climbable angle/grade
    public float ClimbSpeed;                 // Speed while climbing
    
    // Swim capabilities
    public float SwimSpeed;                  // Swimming speed
    public float DiveRate;                   // Vertical dive speed
    public float BuoyancyBias;               // -1 (sinks) to +1 (floats)
    
    // Flight capabilities
    public float FlyThrust;                  // Thrust force (if wings/thrusters)
    public float GlideRatio;                 // Glide efficiency (wings present)
    public float FlightStaminaRate;          // Stamina consumption rate
    
    // Dig capabilities
    public float DigRate;                    // Digging speed
    public uint DigToolMask;                 // Bitmask for required tools
    
    // Failure risk
    public float FailureRiskMultiplier;      // From fatigue/status/injury
}

// Dirty flag for recomputation
public struct MobilityCapabilityDirty : IComponentData { }

// Capability computation system
public struct MobilityCapabilitySystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        // Only recompute when dirty (stats/limbs/status changed)
        foreach (var (stats, limbs, entity) in SystemAPI.Query<
            RefRO<EntityStats>,
            DynamicBuffer<LimbState>>().WithEntityAccess())
        {
            if (!HasComponent<MobilityCapabilityDirty>(entity))
                continue;
            
            var capability = ComputeCapability(stats.ValueRO, limbs);
            SetComponent(entity, capability);
            RemoveComponent<MobilityCapabilityDirty>(entity);
        }
    }
    
    MobilityCapability ComputeCapability(EntityStats stats, DynamicBuffer<LimbState> limbs)
    {
        var cap = new MobilityCapability();
        
        // Count functional limbs
        int armCount = CountFunctionalLimbs(limbs, LimbType.Arm);
        int legCount = CountFunctionalLimbs(limbs, LimbType.Leg);
        bool hasWings = HasLimbType(limbs, LimbType.Wing);
        bool hasFins = HasLimbType(limbs, LimbType.Fin);
        
        // Compute climb grade (requires arms or claws)
        float climbGradeBase = armCount * 1.0f; // Each arm adds 1.0 grade
        if (HasAugment(limbs, AugmentMask.Claws))
            climbGradeBase += 2.0f; // Claws enable climbing without arms
        if (HasAugment(limbs, AugmentMask.GeckoPads))
            climbGradeBase += 3.0f; // Gecko pads enable smooth surfaces
        
        cap.ClimbGrade = climbGradeBase * (stats.Finesse * 0.5f + 0.5f);
        cap.ClimbSpeed = legCount * 0.5f * stats.Strength;
        
        // Compute jump (requires legs)
        cap.MaxJumpHeight = legCount * 0.5f * stats.Strength;
        cap.MaxJumpGap = legCount * 0.7f * stats.Finesse;
        
        // Compute swim (requires limbs + fins help)
        float swimBase = (armCount + legCount) * 0.3f;
        if (hasFins || HasAugment(limbs, AugmentMask.Fins))
            swimBase *= 1.5f;
        cap.SwimSpeed = swimBase * stats.Strength;
        cap.BuoyancyBias = ComputeBuoyancy(stats, limbs);
        
        // Compute flight (requires wings or thrusters)
        if (hasWings || HasAugment(limbs, AugmentMask.Wings))
        {
            cap.FlyThrust = stats.Strength * 0.8f;
            cap.GlideRatio = 5.0f; // Wings provide gliding
        }
        if (HasAugment(limbs, AugmentMask.Propulsion))
        {
            cap.FlyThrust = stats.Strength * 1.2f; // Thrusters more powerful
        }
        cap.FlightStaminaRate = 0.1f; // Stamina consumption
        
        // Compute dig (requires tools or claws)
        if (HasLimbType(limbs, LimbType.Tool) || HasAugment(limbs, AugmentMask.Claws))
            cap.DigRate = stats.Strength * 0.5f;
        
        // Failure risk from fatigue/injury
        float avgHealth = ComputeAverageLimbHealth(limbs);
        float fatiguePenalty = 1.0f - stats.Stamina01; // Lower stamina = higher risk
        cap.FailureRiskMultiplier = (2.0f - avgHealth) * (1.0f + fatiguePenalty);
        
        return cap;
    }
}

// Key rule: Missing limbs reduce capabilities
// Example: One arm halves climb grade; zero arms → climb grade 0 (unless claws/augment)
```

---

## 2. World Exposes Affordances

### Core Concept

**Don't raycast every surface for every agent.** Bake/author "what can be done" as explicit affordances on world entities and navigation features.

### Affordance Component

```csharp
// Affordance on world entities / nav features
public struct WorldAffordance : IComponentData
{
    public AffordanceType Type;              // Ladder, Ledge, ClimbSurface, Vault, etc.
    public float3 LocalAnchorStart;          // Start position (local to entity)
    public float3 LocalAnchorEnd;            // End position (local to entity)
    public AffordanceRequirement Requirement; // What's needed to use this
    public AffordanceCostHints CostHints;    // Time, stamina, noise, visibility
}

public enum AffordanceType : byte
{
    Ladder = 0,              // Vertical ladder (up/down)
    Ledge = 1,               // Horizontal ledge (jump up)
    ClimbSurface = 2,        // Climbable wall/surface
    Vault = 3,               // Vault over obstacle
    JumpGap = 4,             // Gap to jump across
    SwimVolume = 5,          // Water volume (swim entry)
    DiveVolume = 6,          // Deep water (dive entry)
    Diggable = 7,            // Diggable terrain
    PushOffSurface = 8,      // Surface to push off (zero-G)
    Portal = 9,              // Teleport/transport (future)
    FlyVolume = 10           // Air volume (fly entry)
}

public struct AffordanceRequirement
{
    public float MinClimbGrade;              // Required climb capability
    public float MinGrip;                    // Required grip strength
    public float MinJumpHeight;              // Required jump height
    public float MinSwim;                    // Required swim capability
    public uint ToolMask;                    // Required tools (bitmask)
    public MediumType MediumMask;            // Required medium (Air, Liquid, Vacuum)
}

public struct AffordanceCostHints
{
    public float TimeMultiplier;             // How long it takes (relative to normal)
    public float StaminaMultiplier;          // Stamina cost multiplier
    public float Noise;                      // 0-1: noise generated
    public float Visibility;                 // 0-1: visibility/stealth impact
}
```

### Benefits

**"Self-aware agents find ladders" automatic:** Ladders are explicit affordances, so agents automatically detect and use them during navigation.

**No per-agent surface queries:** Affordances are baked/indexed, not computed per-agent.

---

## 3. Navigation Includes Traversal Actions

### Core Concept

**Build a navigation graph that supports special edges.** Normal edges are walk/ground; special edges (off-mesh links) represent climb, jump, vault, ladder, swim, fly, dig actions.

### Navigation Graph Extension

```csharp
// Navigation edge with traversal type
public struct NavEdge : IBufferElementData
{
    public int FromNode;
    public int ToNode;
    public EdgeType Type;                    // Normal or special traversal
    public float Cost;                       // Base cost (time + stamina)
    public TraversalRequirement Requirement; // Capability requirements
}

public enum EdgeType : byte
{
    Walk = 0,                // Normal ground movement
    Climb = 1,               // Climb surface
    Jump = 2,                // Jump gap
    Vault = 3,               // Vault obstacle
    LadderUp = 4,            // Ladder (up)
    LadderDown = 5,          // Ladder (down)
    Swim = 6,                // Swim through volume
    Dive = 7,                // Dive through volume
    Fly = 8,                 // Fly transition
    Dig = 9,                 // Dig through terrain
    PushOff = 10             // Push off surface (zero-G)
}

public struct TraversalRequirement
{
    public float MinClimbGrade;              // Required capability
    public float MinJumpHeight;
    public float MinSwim;
    public uint ToolMask;
    public MediumType MediumMask;
}

// Edge cost computation
float ComputeEdgeCost(NavEdge edge, MobilityCapability capability)
{
    float baseCost = edge.Cost;
    
    // Add risk penalty for difficult traversals
    float capabilityGap = ComputeCapabilityGap(edge.Requirement, capability);
    float failProb = ComputeFailureProbability(capabilityGap, capability.FailureRiskMultiplier);
    float riskPenalty = failProb * FailPenaltyCost;
    
    // Optionally allow "risky" edges if agent is desperate
    float expectedCost = baseCost + riskPenalty;
    
    return expectedCost;
}

// Planning rule: Filter impossible edges
bool CanTraverseEdge(NavEdge edge, MobilityCapability capability)
{
    // Check requirements
    if (edge.Requirement.MinClimbGrade > capability.ClimbGrade)
        return false;
    if (edge.Requirement.MinJumpHeight > capability.MaxJumpHeight)
        return false;
    if (edge.Requirement.MinSwim > capability.SwimSpeed)
        return false;
    if ((edge.Requirement.ToolMask & capability.DigToolMask) == 0)
        return false;
    
    return true;
}
```

### Pathfinding Integration

```csharp
// Pathfinding includes special edges
void FindPathWithTraversal(Entity agent, float3 start, float3 goal)
{
    var capability = GetComponentRO<MobilityCapability>(agent);
    var navGraph = GetSingletonRO<NavGraph>();
    
    // Filter edges by capability
    var validEdges = FilterEdgesByCapability(navGraph.Edges, capability.ValueRO);
    
    // Run A* with special edges included
    var path = AStarPathfind(start, goal, validEdges);
    
    // Path includes traversal actions:
    // [Walk, Walk, Jump, Walk, Climb, Walk, LadderUp, Walk]
}
```

**Result:** Low-finesse agents avoid parkour routes while high-finesse take shortcuts.

---

## 4. Execution: Minimal Action Primitives

### Core Concept

**AI outputs intent, not micromovement.** Keep primitives minimal: MoveTo, TraverseEdge, Interact, Recover.

### Action Primitives

```csharp
// Minimal action primitives
public enum TraversalAction : byte
{
    MoveTo = 0,              // Uses path (normal movement)
    TraverseEdge = 1,        // Climb/jump/vault/ladder/swim/fly/dig
    Interact = 2,            // Open hatch, grab rung, place tool
    Recover = 3              // Rest to regain stamina
}

// Traversal execution state
public struct TraversalExecution : IComponentData
{
    public TraversalAction CurrentAction;
    public int CurrentEdgeId;                // Edge being traversed
    public TraversalPhase Phase;             // Entry, Executing, Exit
    public float Progress;                   // 0-1: progress through traversal
    public uint StartTick;                   // When traversal started
}

public enum TraversalPhase : byte
{
    Entry = 0,               // Positioning at anchor
    Executing = 1,           // Performing traversal
    Exit = 2                 // Completing traversal
}

// Traversal has entry conditions, tick cost, success/fail check
bool CanStartTraversal(Entity agent, NavEdge edge)
{
    // Entry condition: at anchor, correct pose
    float3 anchorPos = GetEdgeAnchor(edge, FromNode);
    float distance = math.distance(GetPosition(agent), anchorPos);
    if (distance > AnchorTolerance)
        return false;
    
    // Check pose/alignment (simplified)
    return true;
}

// Tick cost (stamina drain)
void UpdateTraversalExecution(Entity agent, ref TraversalExecution exec, NavEdge edge)
{
    // Consume stamina per tick
    var stamina = GetComponentRW<Stamina>(agent);
    float staminaCost = GetTraversalStaminaCost(edge.Type) * DeltaTime;
    stamina.ValueRO.Current -= staminaCost;
    
    // Update progress
    exec.Progress += DeltaTime / GetTraversalDuration(edge.Type);
    
    // Check success/fail (cheap function, no physics unless Tier0)
    if (exec.Progress >= 1.0f)
    {
        CompleteTraversal(agent, exec, edge);
    }
    else if (CheckFailure(agent, edge))
    {
        HandleTraversalFailure(agent, exec, edge);
    }
}
```

### Failure Model (Cheap + Believable)

```csharp
// Failure probability (logistic curve)
float ComputeFailureProbability(TraversalRequirement req, MobilityCapability cap, Entity agent)
{
    float capabilityGap = ComputeCapabilityGap(req, cap);
    
    // Logistic curve: pFail = sigmoid((Req - Capability) * k) * fatigueMult * injuryMult
    float k = 2.0f; // Steepness parameter
    float baseFailProb = 1.0f / (1.0f + math.exp(-k * capabilityGap));
    
    var stats = GetComponentRO<EntityStats>(agent);
    float fatigueMult = 1.0f + (1.0f - stats.Stamina01) * 0.5f;
    float injuryMult = cap.FailureRiskMultiplier;
    
    float failProb = baseFailProb * fatigueMult * injuryMult;
    return math.clamp(failProb, 0f, 1f);
}

// Failure outcome (by severity)
void HandleTraversalFailure(Entity agent, TraversalExecution exec, NavEdge edge)
{
    float severity = Random.NextFloat();
    
    if (severity < 0.3f)
    {
        // Stall: cannot proceed, must recover
        exec.Phase = TraversalPhase.Entry;
        exec.Progress = 0f;
        ConsumeStamina(agent, StallStaminaCost);
    }
    else if (severity < 0.7f)
    {
        // Slip: fall back, minor damage
        AbortTraversal(agent, exec);
        ApplyDamage(agent, MinorDamage);
    }
    else
    {
        // Fall: serious damage
        AbortTraversal(agent, exec);
        ApplyDamage(agent, SeriousDamage);
        // Trigger fall animation/physics (Tier0 only)
    }
    
    // "Self-aware" entities raise perceived risk after failures
    UpdatePerceivedRisk(agent, edge, +0.2f);
}
```

---

## 5. Self-Aware Rerouting (Without Heavy Planning)

### Core Concept

**Simple meta-policy enables smart routing without GOAP explosion.** Agents avoid risky edges if alternatives exist, blacklist failed edge types, and replan when affordances are detected.

### Meta-Policy

```csharp
// Perceived risk cache (lightweight)
[InternalBufferCapacity(16)]
public struct PerceivedEdgeRisk : IBufferElementData
{
    public int EdgeId;                       // Edge identifier
    public float PerceivedRisk;              // 0-1: how risky this edge seems
    public uint LastFailureTick;             // When last failed (for cooldown)
}

// Self-aware rerouting policy
bool ShouldUseEdge(Entity agent, NavEdge edge, float alternativeCostMultiplier)
{
    var perceivedRisk = GetPerceivedRisk(agent, edge);
    var capability = GetComponentRO<MobilityCapability>(agent);
    
    // Policy 1: If risk > threshold and alternative exists within X% cost → avoid
    if (perceivedRisk > RiskAvoidanceThreshold)
    {
        if (HasAlternativePath(agent, edge, alternativeCostMultiplier))
        {
            return false; // Avoid risky edge
        }
    }
    
    // Policy 2: If attempted and failed once → blacklist for cooldown
    var riskEntry = GetPerceivedRiskEntry(agent, edge);
    if (riskEntry.LastFailureTick > 0)
    {
        uint cooldown = 300; // 5 seconds at 60 Hz
        if (CurrentTick - riskEntry.LastFailureTick < cooldown)
        {
            return false; // Still in cooldown
        }
    }
    
    // Policy 3: Check if capability sufficient (actual risk)
    float actualRisk = ComputeFailureProbability(edge.Requirement, capability.ValueRO, agent);
    if (actualRisk > ActualRiskThreshold)
    {
        return false; // Too risky
    }
    
    return true; // Safe to use
}

// Replan when affordances detected
void OnAffordanceDetected(Entity agent, WorldAffordance affordance)
{
    // If comms/sensors indicate "ladder nearby" → replan with that target
    if (affordance.Type == AffordanceType.Ladder)
    {
        // Replan path to include ladder
        float3 ladderPos = GetAffordancePosition(affordance);
        RequestReplan(agent, ladderPos);
    }
}
```

**Result:** Smart routing without GOAP explosion. Agents naturally avoid risky routes and adapt to discovered affordances.

---

## 6. Medium-Driven Modes (Same Code Works in Godgame + Space4X)

### Core Concept

**At movement update, sample environment and resolve locomotion mode.** Gravity vector, medium type, and currents determine mode automatically.

### Integration with Environment Field

```csharp
// Medium-driven mode resolution (existing pattern from Simulation_LOD doc)
public enum LocomotionMode : byte
{
    Grounded = 0,            // On solid surface
    Swimming = 1,            // In liquid
    Floating = 2,            // Zero-G (vacuum or microgravity)
    Flying = 3,              // Aerial flight
    Climbing = 4,            // On vertical surface
    Digging = 5              // Through terrain
}

// Mode resolution system
void ResolveLocomotionMode(Entity agent)
{
    float3 position = GetPosition(agent);
    var env = EnvironmentField.Query(position); // Existing API
    
    // Resolve mode from environment
    LocomotionMode mode;
    if (env.MediumType == MediumType.Liquid)
    {
        mode = LocomotionMode.Swimming;
    }
    else if (env.MediumType == MediumType.Vacuum || env.GravityVector.magnitude < 0.1f)
    {
        mode = LocomotionMode.Floating; // Zero-G
    }
    else if (IsOnVerticalSurface(agent))
    {
        mode = LocomotionMode.Climbing;
    }
    else if (IsDigging(agent))
    {
        mode = LocomotionMode.Digging;
    }
    else if (IsFlying(agent))
    {
        mode = LocomotionMode.Flying;
    }
    else
    {
        mode = LocomotionMode.Grounded;
    }
    
    // Apply mode to movement system
    var locomotion = GetComponentRW<ActiveLocomotion>(agent);
    locomotion.ValueRW.CurrentMode = mode;
}
```

**Benefits:**
- Underwater → swim/dive, buoyancy + drag (automatically)
- Zero-G → push-off or thrusters (if present)
- Inside ship vs outside hull changes behavior automatically
- Same code works for Godgame (terrestrial) and Space4X (space/ships)

---

## 7. Performance Guardrails (Non-Negotiable)

### Core Constraints

**1. No Per-Agent Surface Queries:**
- Affordances + nav edges are baked/indexed
- Agents query affordance registry, not raycast surfaces

**2. Dirty Recompute:**
- `MobilityCapability` only updates when stats/limbs/status change
- Mark dirty on: stat change, limb damage, augment change, status change

**3. Simulation LOD:**

```csharp
// Tier0 (near camera / boarding): richer contact checks, optional impulses
if (LODTier == Tier0)
{
    // Full physics checks
    bool success = PhysicsCheckTraversal(agent, edge);
}

// Tier1+: kinematic traversal + probability model only
else
{
    // Probability-based success/fail
    float failProb = ComputeFailureProbability(...);
    bool success = Random.NextFloat() > failProb;
}
```

**4. Group Planning:**
- Formations request one path
- Members follow slots (no individual pathfinding)

```csharp
// Group pathfinding (single path for formation)
void ComputeGroupPath(Entity formationLeader, float3 goal)
{
    var path = FindPath(formationLeader, goal);
    
    // Members follow formation slots
    var members = GetFormationMembers(formationLeader);
    for (int i = 0; i < members.Length; i++)
    {
        float3 slotOffset = GetFormationSlotOffset(i);
        // Members navigate to path + slotOffset (simple offset, no pathfinding)
    }
}
```

---

## 8. Practical Incremental Build Order

### Phase 1: Ground Move + Jump (MVP)

**Single special edge: jump gap**
- Implement `MobilityCapability` with jump calculations
- Add `JumpGap` affordance type
- Navigation graph includes jump edges
- Execution: `TraverseEdge(Jump)`

### Phase 2: Ladders

**Easy affordance, discrete edges**
- Add `Ladder` affordance type
- Navigation graph includes ladder edges (up/down)
- Execution: `TraverseEdge(LadderUp/Down)`

### Phase 3: Swim + Dive Volumes

**Medium-driven mode integration**
- Add `SwimVolume`, `DiveVolume` affordances
- Environment field integration (MediumType.Liquid)
- Execution: `TraverseEdge(Swim/Dive)`

### Phase 4: Climb Surfaces

**Grade-based climbing**
- Add `ClimbSurface` affordance type
- Grade-based requirement matching
- Execution: `TraverseEdge(Climb)`

### Phase 5: Fly/Glide

**Stamina-thrust model**
- Add `FlyVolume` affordance
- Flight capability computation (wings/thrusters)
- Execution: `TraverseEdge(Fly)`

### Phase 6: Dig

**Terrain edit commands + diggable affordances**
- Add `Diggable` affordance type
- Terrain modification integration
- Execution: `TraverseEdge(Dig)`

---

## Integration Summary

### Existing Systems Enhanced

- **LimbState:** Enhanced with Manipulation/Force/Grip and AugmentMask
- **LocomotionSystem:** Extended with traversal actions and medium-driven modes
- **Navigation/Pathfinding:** Extended with special edges and capability filtering
- **Environment Field:** Used for medium-driven mode resolution

### New Systems Added

- **MobilityCapability:** Derived capabilities from anatomy
- **WorldAffordance:** World-authored interaction points
- **TraversalExecution:** Action primitive execution
- **PerceivedEdgeRisk:** Self-aware rerouting cache

---

## Related Documentation

- **Locomotion System:** `Docs/Concepts/Core/Locomotion_System.md` - Movement modes
- **Simulation LOD:** `Docs/Concepts/Core/Simulation_LOD_And_Environment_Fields.md` - Medium-driven modes
- **Limb System:** `Packages/com.moni.puredots/Runtime/Combat/Components.cs` - LimbState
- **Navigation:** `Packages/com.moni.puredots/Documentation/DesignNotes/UniversalNavigationSystem.md` - Pathfinding

---

**For Implementers:** Focus on MobilityCapability computation, affordance indexing, and navigation graph extension with special edges  
**For Designers:** Focus on affordance authoring, capability requirements, and failure model tuning

