# AI Optimization Methodologies

**Status:** Concept - Design Patterns
**Category:** Core - AI Scalability & Efficiency
**Scope:** Cross-Project (PureDOTS Foundation)
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Document high-value, low-cost AI optimization patterns that enable millions of agents to remain "alive" and reactive without constant expensive cognition, tailored to PureDOTS architecture with sensors/comms/intents.

**Secondary Goals:**
- Reduce polling overhead through attention/interrupt-driven updates
- Enable believable behavior without expensive decision-making (schedules, commitment)
- Support uncertainty and memory through confidence-aware decisions and local caches
- Enable group intelligence without per-unit computation (doctrine/SOP)
- Create emergent work cycles (maintenance/degradation)
- Enable lightweight social dynamics (micro-social events)
- Provide rare authored moments (storylets)
- Scale efficiently through simulation LOD

**Key Principle:** Keep agents reactive and believable while minimizing computational cost. Most agents should be idle most of the time, only "thinking hard" when something changes.

---

## 1. Attention + Interrupts (Stop Constant Polling)

### Core Concept

**Agents only "think hard" when something changes.** Instead of constant polling, agents react to perception deltas, comm receipts, need thresholds, and local hazards via interrupts.

### Integration with PureDOTS

**Existing Systems:**
- `InterruptComponents` (InterruptType, EntityIntent, InterruptPriority)
- `AISensorUpdateSystem` (perception updates)
- Communication system (comm receipts)

**Enhancement Pattern:**

```csharp
// Interrupt-driven AI updates (instead of constant polling)
public struct AIAttentionState : IComponentData
{
    public byte AttentionLevel;        // 0 = idle, 255 = fully attentive
    public uint LastAttentionTick;     // When attention last triggered
    public float AttentionDecayRate;   // How fast attention fades
}

// Interrupts trigger attention
public enum AttentionTrigger : byte
{
    PerceptionDelta = 0,       // Sensor reading changed significantly
    CommReceived = 1,          // Message/command received
    NeedThreshold = 2,         // Hunger/energy hit critical level
    LocalHazard = 3,           // Threat detected nearby
    OrderReceived = 4          // Command from authority
}

// Lightweight interrupt handler
void OnInterrupt(Entity agent, Interrupt interrupt)
{
    // Bump attention level
    var attention = GetComponentRW<AIAttentionState>(agent);
    attention.ValueRW.AttentionLevel = 255; // Full attention
    attention.ValueRW.LastAttentionTick = CurrentTick;
    
    // Trigger intent update or replan
    if (interrupt.Priority >= InterruptPriority.High)
    {
        RequestReplan(agent);
    }
    else
    {
        UpdateIntent(agent, interrupt);
    }
}
```

**Benefits:**
- Millions of agents can remain mostly idle
- Only active agents consume CPU
- Reactive behavior without constant polling
- Scales linearly with active agents (not total agents)

**Implementation Priority:** High (immediate scalability win)

---

## 2. Schedules and Duty Rosters (Life Without Expensive Cognition)

### Core Concept

**Tiny "daily/shift" scheduler produces believable behavior without decision-making.** Entities follow predictable schedules based on roles and time-of-day, reducing need for complex AI evaluation.

### Integration with PureDOTS

**Existing Systems:**
- `EntityRoutine`, `RoutineSchedule`, `DayPhase`, `RoutineActivity`
- `VillagerShiftSchedulingSystem`
- Time-of-day service

**Enhancement Pattern:**

```csharp
// Schedule profiles (authoring data)
public struct ScheduleProfile : IComponentData
{
    // Blob reference to schedule configuration
    public BlobAssetReference<ScheduleProfileBlob> Profile;
}

// Duty roles (affect schedule selection)
public enum DutyRole : byte
{
    Hauler = 0,       // Transport/logistics
    Guard = 1,        // Security/patrol
    Builder = 2,      // Construction/maintenance
    Medic = 3,        // Healthcare/repair
    Engineer = 4,     // Technical/systems
    Cook = 5,         // Food preparation
    Trader = 6,       // Commerce
    Researcher = 7    // Knowledge/tech
}

// Schedule applies to routine system
void ApplyScheduleProfile(Entity entity, DutyRole role, ScheduleProfile profile)
{
    // Schedule profile maps DayPhase → RoutineActivity
    // Example: Guard role → Patrol at Night, Rest at Dawn
    var routine = GetComponentRW<EntityRoutine>(entity);
    var schedule = profile.Profile.Value;
    
    var currentPhase = GetCurrentDayPhase();
    routine.ValueRO.ScheduledActivity = schedule.GetActivity(currentPhase, role);
}
```

**Benefits:**
- Believable daily cycles without AI decisions
- Works in villages (Godgame) and crews (Space4X)
- Watch shifts, maintenance windows emerge naturally
- Zero decision-making cost for routine activities

**Implementation Priority:** Medium (builds on existing routine system)

---

## 3. Commitment + Anti-Thrashing (Agents Stop Flip-Flopping)

### Core Concept

**Once an intent is chosen, it has "inertia".** Commitment timers, abort costs, and replan cooldowns prevent agents from constantly changing their minds.

### Integration with PureDOTS

**Existing Systems:**
- `EntityIntent` (IntentMode, IntentSetTick)
- `AISteeringSystem` (movement execution)
- Arguments system (decision protocols)

**Enhancement Pattern:**

```csharp
public struct IntentCommitment : IComponentData
{
    public float CommitmentTimer;      // How long to stick with current intent
    public float AbortCost;            // Cost (energy/morale) to change intent
    public float ReplanCooldown;       // Time before next replan allowed
    public uint IntentLockTick;        // When intent was locked
}

// Anti-thrashing logic
bool CanChangeIntent(Entity agent, EntityIntent newIntent, IntentCommitment commitment)
{
    // Check cooldown
    if (CurrentTick - commitment.IntentLockTick < commitment.ReplanCooldown)
        return false;
    
    // Check commitment timer
    if (commitment.CommitmentTimer > 0)
        return false;
    
    // Check abort cost (can afford to change?)
    var needs = GetComponentRO<VillagerNeeds>(agent);
    if (commitment.AbortCost > needs.ValueRO.Energy)
        return false;
    
    return true;
}

// Apply commitment after intent change
void LockIntent(Entity agent, EntityIntent intent)
{
    var commitment = GetComponentRW<IntentCommitment>(agent);
    commitment.ValueRO.IntentLockTick = CurrentTick;
    commitment.ValueRO.CommitmentTimer = GetCommitmentDuration(intent.Mode);
}
```

**Affects:**
- Movement (don't change direction constantly)
- Hauling (complete task before switching)
- Combat roles (stick with assigned role)
- Conversations/arguments (commit to decision)

**Benefits:**
- More believable behavior (less jittery)
- Reduced replanning overhead
- Strategic commitment decisions (can't afford to abort = stuck)

**Implementation Priority:** Medium (improves behavior quality)

---

## 4. Confidence-Aware Decisions (Uncertainty Makes Behavior Believable)

### Core Concept

**Store confidence on beliefs, messages, perceptions, paths.** Low confidence triggers clarification, scouting, asking board, waiting, or safe-defaults. High confidence enables decisive action.

### Integration with PureDOTS

**Existing Systems:**
- `AISensorReading` (DetectionConfidence already exists)
- Communication system (miscommunication + jamming)
- Intel/visibility systems

**Enhancement Pattern:**

```csharp
// Confidence tracks on various data
public struct BeliefConfidence : IBufferElementData
{
    public FixedString64Bytes BeliefId;    // "enemy_location", "resource_deposit"
    public float Confidence;               // 0-1: how certain
    public uint LastUpdateTick;            // When confidence last updated
    public float DecayRate;                // How fast confidence fades
}

// Confidence-aware decision logic
IntentMode DecideWithConfidence(Entity agent, BeliefConfidence belief)
{
    if (belief.Confidence < 0.3f)
    {
        // Low confidence → clarify/scout
        return IntentMode.Patrol; // Scout the area
    }
    else if (belief.Confidence < 0.6f)
    {
        // Medium confidence → ask others / wait
        if (HasNearbyAllies(agent))
            return IntentMode.Socialize; // Ask for information
        else
            return IntentMode.Idle; // Wait for better intel
    }
    else
    {
        // High confidence → act decisively
        return IntentMode.Attack; // Confident about target
    }
}

// Communication affects confidence
void OnCommReceived(Entity receiver, CommunicationAttempt comm)
{
    // Miscommunication reduces confidence
    var confidence = GetOrCreateBeliefConfidence(receiver, comm.Topic);
    confidence.Confidence *= comm.Clarity; // Lower clarity = lower confidence
    
    // Jamming prevents confidence updates
    if (IsJammed(receiver))
    {
        confidence.Confidence *= 0.5f; // Jamming reduces confidence
    }
}
```

**Benefits:**
- Believable uncertainty (agents act cautious when unsure)
- Plugs directly into miscommunication + jamming design
- Emergent scouting behavior (low confidence → seek information)
- Safe defaults prevent risky decisions with poor intel

**Implementation Priority:** High (enhances existing systems naturally)

---

## 5. Local Memory Caches (Cheap "I Remember Where")

### Core Concept

**Don't query the world; query a small per-agent cache.** Known locations decay over time and verify when near, enabling scouting, rumors, patrol loops, and "lost track" moments.

### Integration with PureDOTS

**Existing Systems:**
- Spatial queries (expensive for frequent lookups)
- Sensor readings (temporary, not persistent)

**Enhancement Pattern:**

```csharp
[InternalBufferCapacity(16)]
public struct KnownLocationCache : IBufferElementData
{
    public FixedString64Bytes LocationType;  // "resource_pile", "enemy_camp", "safe_cover"
    public float3 Position;                  // Remembered position
    public float Confidence;                 // 0-1: how reliable (decays over time)
    public uint LastSeenTick;                // When last observed
    public uint CacheCreationTick;           // When cached
}

// Cache management
void UpdateMemoryCache(Entity agent)
{
    var cache = GetBufferRW<KnownLocationCache>(agent);
    
    for (int i = cache.Length - 1; i >= 0; i--)
    {
        var entry = cache[i];
        float age = (CurrentTick - entry.LastSeenTick) / (float)TicksPerDay;
        
        // Decay confidence over time
        entry.Confidence *= math.pow(0.9f, age); // 10% decay per day
        
        // Verify when near (update or remove if wrong)
        float distance = math.distance(GetPosition(agent), entry.Position);
        if (distance < VerificationRange)
        {
            if (!VerifyLocation(entry))
            {
                cache.RemoveAt(i); // Location no longer valid
                continue;
            }
            else
            {
                entry.LastSeenTick = CurrentTick; // Refresh
                entry.Confidence = 1.0f; // Verified
            }
        }
        
        // Remove if confidence too low
        if (entry.Confidence < 0.1f)
        {
            cache.RemoveAt(i);
        }
        else
        {
            cache[i] = entry;
        }
    }
}

// Use cache instead of spatial query
float3? GetRememberedLocation(Entity agent, FixedString64Bytes locationType)
{
    var cache = GetBufferRO<KnownLocationCache>(agent);
    foreach (var entry in cache)
    {
        if (entry.LocationType.Equals(locationType) && entry.Confidence > 0.5f)
        {
            return entry.Position;
        }
    }
    return null; // Don't remember
}
```

**Benefits:**
- Cheap lookups (no spatial queries)
- Enables scouting (remember enemy locations)
- Rumors (share cached locations via communication)
- Patrol loops (remember patrol points)
- "Lost track" moments (confidence decays, agent forgets)

**Implementation Priority:** Medium (nice-to-have optimization)

---

## 6. Doctrine / SOP for Groups (Macro Intelligence Without Per-Unit Brains)

### Core Concept

**Groups run simple policies; individuals follow roles.** Squad doctrine defines behavior (aggressive/defensive, flank preference, ROE strictness). Leader computes plan; units execute with minimal local logic.

### Integration with PureDOTS

**Existing Systems:**
- Group behavior components (`GroupTag`, `GroupMember`, `GroupFormation`)
- Arguments system (tactical decisions)
- Individual combat intent

**Enhancement Pattern:**

```csharp
// Doctrine/SOP configuration
public struct SquadDoctrine : IComponentData
{
    public DoctrineAggressiveness Aggressiveness;  // Aggressive, Defensive, Cautious
    public DoctrineFlankPreference FlankPreference; // PreferFlanking, PreferDirect, Adaptive
    public float ROEStrictness;                    // 0-1: how strictly follow rules of engagement
    public DoctrineFormation FormationPreference;  // Tight, Loose, Adaptive
}

public enum DoctrineAggressiveness : byte
{
    Aggressive = 0,   // Push forward, engage first
    Balanced = 1,     // Respond to threats
    Defensive = 2,    // Hold position, counter-attack
    Cautious = 3      // Avoid engagement, retreat easily
}

// Role assignment (leader decides, units follow)
public enum CombatRole : byte
{
    Spearhead = 0,    // Front line, aggressive
    Flanker = 1,      // Side attack, mobile
    Support = 2,      // Rear, ranged/healer
    Reserve = 3       // Held back, reactive
}

// Leader computes plan from doctrine
void ComputeGroupPlan(Entity groupLeader, SquadDoctrine doctrine)
{
    // Leader evaluates situation (once, not per-unit)
    var threatLevel = EvaluateThreatLevel(groupLeader);
    var terrain = EvaluateTerrain(groupLeader);
    
    // Doctrine influences plan
    IntentMode groupIntent = DetermineGroupIntent(doctrine, threatLevel, terrain);
    CombatRole[] roleAssignments = AssignRoles(doctrine, groupIntent);
    
    // Assign roles to members (units execute, don't decide)
    var members = GetBufferRW<GroupMember>(groupLeader);
    for (int i = 0; i < members.Length; i++)
    {
        var member = members[i];
        var unitIntent = GetComponentRW<EntityIntent>(member.Entity);
        unitIntent.ValueRW.Mode = MapRoleToIntent(roleAssignments[i]);
    }
}
```

**Benefits:**
- Group intelligence without per-unit computation
- Leader does thinking, units execute
- Doctrine drives emergent tactics
- Scales to large groups (one decision, many executors)

**Implementation Priority:** High (enables large-scale combat)

---

## 7. Maintenance + Degradation Loops (Emergent Work, Zero Scripting)

### Core Concept

**Things slowly break; agents create/consume tasks naturally.** Buildings/modules get wear, failure risk, and service intervals. Creates believable "always something to do" cycles without scripting.

### Integration with PureDOTS

**Existing Systems:**
- Job/task systems
- Entity health/degradation
- Resource production

**Enhancement Pattern:**

```csharp
// Degradation component
public struct StructuralDegradation : IComponentData
{
    public float Wear;                  // 0-1: accumulated wear
    public float FailureRisk;           // 0-1: chance of failure per tick
    public float ServiceInterval;       // Recommended service interval (days)
    public uint LastServiceTick;        // When last serviced
    public DegradationType Type;        // Mechanical, Structural, Electronic, etc.
}

public enum DegradationType : byte
{
    Mechanical = 0,     // Moving parts, wear
    Structural = 1,     // Load-bearing, stress
    Electronic = 2,     // Systems, corrosion
    Organic = 3         // Biological, decay
}

// Degradation system (runs periodically, not every tick)
void UpdateDegradation(Entity structure)
{
    var degradation = GetComponentRW<StructuralDegradation>(structure);
    float age = (CurrentTick - degradation.ValueRO.LastServiceTick) / (float)TicksPerDay;
    
    // Accumulate wear over time
    degradation.ValueRW.Wear += age * DegradationRate;
    degradation.ValueRW.Wear = math.clamp(degradation.ValueRW.Wear, 0f, 1f);
    
    // Failure risk increases with wear
    degradation.ValueRO.FailureRisk = degradation.ValueRO.Wear * BaseFailureRate;
    
    // Create maintenance task if needed
    if (degradation.ValueRO.Wear > MaintenanceThreshold)
    {
        CreateMaintenanceTask(structure);
    }
}

// Agents naturally consume maintenance tasks (via job system)
void CreateMaintenanceTask(Entity structure)
{
    // Post task to job registry (existing system)
    var task = new MaintenanceTask
    {
        TargetEntity = structure,
        TaskType = TaskType.Repair,
        Priority = CalculatePriority(structure)
    };
    PostTaskToRegistry(task);
}
```

**Benefits:**
- Emergent work cycles (no scripting needed)
- Always something to do (repair, restock, clean, refuel)
- Believable maintenance needs
- Agents naturally find work

**Implementation Priority:** Medium (nice-to-have realism)

---

## 8. Micro-Social Layer (Lightweight Relations Without Chat)

### Core Concept

**Small event reactions drive relationships over time.** Events (helped me, stole from me, obeyed/refused order, saved me, insulted me) emit relation deltas and small behavior biases (trust, cooperation likelihood).

### Integration with PureDOTS

**Existing Systems:**
- `EntityRelation` (Intensity, Trust, Familiarity)
- `RecordInteractionRequest` (interaction outcomes)
- Arguments system (social events)

**Enhancement Pattern:**

```csharp
// Micro-social events (lightweight, frequent)
public enum MicroSocialEvent : byte
{
    HelpedMe = 0,           // Positive: shared resource, assisted
    StoleFromMe = 1,        // Negative: took resource, betrayed
    ObeyedOrder = 2,        // Positive: followed command
    RefusedOrder = 3,       // Negative: disobeyed
    SavedMe = 4,            // Positive: rescue, protection
    InsultedMe = 5,         // Negative: verbal attack
    SharedIntel = 6,        // Positive: information sharing
    BetrayedTrust = 7,      // Negative: broke confidence
    WorkedTogether = 8,     // Positive: collaboration
    Competed = 9            // Neutral: rivalry
}

// Emit micro-social event
void EmitMicroSocialEvent(Entity actor, Entity target, MicroSocialEvent eventType)
{
    // Compute relation delta (small, incremental)
    sbyte intensityDelta = GetEventIntensityDelta(eventType);
    
    // Update relation via existing system
    var interactionReq = new RecordInteractionRequest
    {
        EntityA = actor,
        EntityB = target,
        Outcome = MapEventToOutcome(eventType),
        IntensityChange = intensityDelta
    };
    PostInteractionRequest(actor, interactionReq);
    
    // Update behavior bias (trust, cooperation)
    UpdateBehaviorBias(actor, target, eventType);
}

// Behavior bias (affects future decisions)
public struct SocialBias : IBufferElementData
{
    public Entity TargetEntity;
    public float TrustModifier;          // -1 to +1: affects cooperation likelihood
    public float CooperationLikelihood;  // 0-1: chance to help/cooperate
}
```

**Benefits:**
- Lightweight (no complex conversation system)
- Drives relationships over time
- Affects behavior (trust → cooperation)
- Works with existing relation system

**Implementation Priority:** High (enhances existing relations naturally)

---

## 9. Storylets (Rare, Authored "Beats" Triggered by State)

### Core Concept

**Instead of simulating deep drama constantly, trigger occasional scenes.** Condition-based triggers (low morale + scarcity + rivalry) → argument session / policy shift. Keeps CPU low, produces memorable moments.

### Integration with PureDOTS

**Existing Systems:**
- Arguments system (decision protocols)
- Entity relations (rivalry detection)
- Needs/morale systems

**Enhancement Pattern:**

```csharp
// Storylet definition (authoring data)
public struct Storylet : IComponentData
{
    public FixedString64Bytes StoryletId;      // "rivalry_argument", "scarcity_crisis"
    public StoryletTriggerConditions Conditions; // When to trigger
    public StoryletOutcome Outcome;            // What happens
    public float TriggerProbability;           // 0-1: chance when conditions met
    public uint CooldownTicks;                 // Min time between triggers
}

// Trigger conditions (state-based)
public struct StoryletTriggerConditions
{
    public float MinMorale;           // Trigger if morale below this
    public float MinScarcity;         // Trigger if resources below this
    public float MinRivalry;          // Trigger if relations below this
    public bool RequiresProximity;    // Must be nearby
    public bool RequiresAuthority;    // Must have authority present
}

// Storylet system (runs periodically, checks conditions)
void CheckStoryletTriggers()
{
    foreach (var storylet in GetAllStorylets())
    {
        // Check conditions
        if (ConditionsMet(storylet.Conditions))
        {
            // Roll probability
            if (Random.NextFloat() < storylet.TriggerProbability)
            {
                // Trigger storylet
                ExecuteStorylet(storylet);
            }
        }
    }
}

// Execute storylet (triggers argument session, policy shift, etc.)
void ExecuteStorylet(Storylet storylet)
{
    switch (storylet.Outcome.Type)
    {
        case StoryletOutcomeType.ArgumentSession:
            CreateArgumentSession(storylet.Outcome.Participants, storylet.Outcome.Topic);
            break;
        case StoryletOutcomeType.PolicyShift:
            TriggerPolicyChange(storylet.Outcome.Target);
            break;
        case StoryletOutcomeType.RelationshipEvent:
            EmitRelationshipEvent(storylet.Outcome.Participants);
            break;
    }
}
```

**Benefits:**
- Low CPU (rare triggers, not constant simulation)
- Memorable moments (authored scenes)
- Emergent narratives (conditions → scenes)
- Works with arguments system

**Implementation Priority:** Low (nice-to-have polish)

---

## 10. Simulation LOD for AI (The Big Scalability Lever)

### Core Concept

**Not every entity runs the same brain rate.** Tier 0 (near camera): full AI. Tier 1: reduced update cadence. Tier 2: event-driven only. Tier 3: aggregate-only (rates and ledgers).

### Integration with PureDOTS

**Existing Systems:**
- Simulation LOD framework (`Simulation_LOD_And_Environment_Fields.md`)
- Camera distance queries
- Aggregation systems

**Enhancement Pattern:**

```csharp
public enum AILODTier : byte
{
    Tier0_Full = 0,           // Near camera: full AI, every tick
    Tier1_Reduced = 1,        // Mid-distance: reduced cadence (every N ticks)
    Tier2_EventDriven = 2,    // Far: event-driven only (interrupts, needs)
    Tier3_Aggregate = 3       // Offscreen: aggregate simulation (rates/ledgers)
}

// LOD assignment (based on camera distance)
AILODTier GetAILOD(Entity agent)
{
    float distance = GetDistanceToCamera(agent);
    
    if (distance < NearDistance)
        return AILODTier.Tier0_Full;
    else if (distance < MidDistance)
        return AILODTier.Tier1_Reduced;
    else if (distance < FarDistance)
        return AILODTier.Tier2_EventDriven;
    else
        return AILODTier.Tier3_Aggregate;
}

// Update cadence per tier
void UpdateAIWithLOD(Entity agent, AILODTier tier)
{
    switch (tier)
    {
        case AILODTier.Tier0_Full:
            // Full AI: sensors, scoring, steering, tasks
            UpdateSensors(agent);
            UpdateScoring(agent);
            UpdateSteering(agent);
            UpdateTasks(agent);
            break;
            
        case AILODTier.Tier1_Reduced:
            // Reduced cadence: update every 4 ticks
            if (CurrentTick % 4 == 0)
            {
                UpdateSensors(agent);
                UpdateScoring(agent);
            }
            UpdateSteering(agent); // Movement still smooth
            break;
            
        case AILODTier.Tier2_EventDriven:
            // Event-driven: only on interrupts/needs
            if (HasActiveInterrupt(agent) || NeedsCritical(agent))
            {
                UpdateAI(agent); // Full update only when needed
            }
            break;
            
        case AILODTier.Tier3_Aggregate:
            // Aggregate: no individual AI, use aggregate rates
            // (handled by aggregate systems, not individual AI)
            break;
    }
}
```

**Benefits:**
- Massive scalability (millions of agents feasible)
- Most agents in Tier 2/3 (event-driven or aggregate)
- Only Tier 0 agents consume significant CPU
- Smooth transitions between tiers

**Implementation Priority:** Critical (required for scale)

---

## Integration Summary

### Priority Order (Implementation)

1. **Simulation LOD (Critical):** Required for scale
2. **Attention + Interrupts (High):** Immediate scalability win
3. **Confidence-Aware Decisions (High):** Enhances existing systems
4. **Doctrine/SOP (High):** Enables large-scale combat
5. **Micro-Social Layer (High):** Enhances existing relations
6. **Commitment + Anti-Thrashing (Medium):** Improves behavior quality
7. **Schedules/Rosters (Medium):** Builds on existing routine system
8. **Local Memory Caches (Medium):** Nice-to-have optimization
9. **Maintenance/Degradation (Medium):** Nice-to-have realism
10. **Storylets (Low):** Nice-to-have polish

### Synergies

- **Attention + LOD:** Event-driven updates work at all LOD tiers
- **Confidence + Memory:** Cache confidence decays over time
- **Doctrine + Arguments:** Doctrine influences argument outcomes
- **Schedules + Commitment:** Schedules reduce need for intent changes
- **Micro-Social + Relations:** Events drive relation changes
- **Maintenance + Schedules:** Maintenance fits into duty rosters

---

## Related Documentation

- **AI Behavior Modules:** `Docs/Mechanics/AIBehaviorModules.md` - Core AI framework
- **Simulation LOD:** `Docs/Concepts/Core/Simulation_LOD_And_Environment_Fields.md` - LOD foundation
- **Arguments System:** `Docs/Concepts/Core/Arguments_System.md` - Decision protocols
- **Entity Relations:** `Docs/Concepts/Implemented/Villagers/Entity_Relations_And_Interactions.md` - Social dynamics
- **Routine System:** `Packages/com.moni.puredots/Runtime/Runtime/AI/Routine/RoutineComponents.cs` - Schedule foundation

---

**For Implementers:** Focus on LOD, attention/interrupts, and confidence first (biggest scalability wins)  
**For Designers:** Focus on doctrine/SOP, micro-social, and storylets (most visible behavior improvements)

