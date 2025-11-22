# Aggregate Entity Decision-Making (Shared)

## Overview

Aggregate entities (villages, bands, guilds, fleets, planets, sectors) make **collective decisions** using the same behavioral components as individuals but with emergent consensus mechanics. This document defines how aggregates compute their state from members, resolve internal conflicts, and make autonomous decisions that affect the collective.

---

## Core Concept

**Aggregates are entities composed of member entities.**

Key principles:
- **Same components** as individuals (`VillagerAlignment`, `VillagerBehavior`, `VillagerInitiativeState`)
- **State computed from members** (average alignment, dominant personality, majority vote)
- **Consensus mechanics** (internal agreement, factions, dissent handling)
- **Autonomous actions** (aggregates act independently of members, members respond to aggregate decisions)
- **Scale-independent logic** (Band of 5 or Fleet of 1000 uses same decision framework)

---

## Aggregate Entity Components

### 1. Core Aggregate Identity

```csharp
// Base aggregate marker + metadata
public struct AggregateEntity : IComponentData
{
    public AggregateType Type;
    public FixedString64Bytes Name;
    public Entity Parent;                        // Parent aggregate (if nested)
    public ushort MemberCount;                   // Cached count for performance
    public float Cohesion;                       // 0.0-1.0 (internal unity)
    public AggregateSize Size;
}

public enum AggregateType : byte
{
    // Godgame
    Village,        // Settlement of villagers
    Band,           // Combat/work group
    Guild,          // Professional association
    Dynasty,        // Family lineage
    Business,       // Trade company

    // Space4X
    Fleet,          // Ship squadron
    Planet,         // Colony population
    Sector,         // Space region control
    Corporation,    // Economic entity
    Empire          // Faction-wide aggregate

    // Universal
    Elite           // Hero party, champion group
}

public enum AggregateSize : byte
{
    Tiny,           // 1-5 members
    Small,          // 5-20 members
    Medium,         // 20-100 members
    Large,          // 100-500 members
    Huge,           // 500-5000 members
    Massive         // 5000+ members
}
```

### 2. Member Tracking

```csharp
// Buffer of member entity references
public struct AggregateMemberEntry : IBufferElementData
{
    public Entity Member;
    public MemberRole Role;                      // Leadership hierarchy
    public float Influence;                      // 0.0-1.0 (voting weight)
    public ushort JoinedTick;                    // When joined
    public byte LoyaltyLevel;                    // 0-100 (likelihood to defect)
}

public enum MemberRole : byte
{
    Member,         // Standard participant
    Veteran,        // Experienced, higher influence
    Officer,        // Middle management
    Leader,         // Decision-maker
    Founder         // Original creator (special status)
}
```

### 3. Consensus State

```csharp
// Tracks internal agreement on decisions
public struct AggregateConsensus : IComponentData
{
    public float Agreement;                      // 0.0-1.0 (how unified)
    public ConsensusMode Mode;
    public Entity DominantFaction;               // Which internal faction leads
    public ushort DissentCount;                  // Number of dissenting members
}

public enum ConsensusMode : byte
{
    Unanimous,      // All members agree (rare, high cohesion)
    Majority,       // >50% agree (standard)
    Plurality,      // Largest faction wins (split opinion)
    Dictatorial,    // Leader decides (low cohesion, authoritarian)
    Anarchy         // No consensus (paralyzed, may splinter)
}
```

### 4. Aggregate Alignment (Computed)

Aggregates use the **same** `VillagerAlignment` component as individuals, but values are **computed from members**.

```csharp
// System that updates aggregate alignment from member averages
[BurstCompile]
public partial struct AggregateAlignmentUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (aggregate, alignment, members) in
                 SystemAPI.Query<RefRO<AggregateEntity>, RefRW<VillagerAlignment>, DynamicBuffer<AggregateMemberEntry>>())
        {
            if (members.Length == 0)
            {
                // Empty aggregate, use neutral alignment
                alignment.ValueRW = new VillagerAlignment
                {
                    MoralAxis = 0f,
                    OrderAxis = 0f,
                    PurityAxis = 0f
                };
                continue;
            }

            float totalInfluence = 0f;
            float3 weightedAlignment = float3.zero;

            foreach (var member in members)
            {
                if (!SystemAPI.Exists(member.Member))
                    continue;

                var memberAlignment = SystemAPI.GetComponent<VillagerAlignment>(member.Member);
                float influence = member.Influence;

                weightedAlignment.x += memberAlignment.MoralAxis * influence;
                weightedAlignment.y += memberAlignment.OrderAxis * influence;
                weightedAlignment.z += memberAlignment.PurityAxis * influence;

                totalInfluence += influence;
            }

            if (totalInfluence > 0f)
            {
                alignment.ValueRW.MoralAxis = weightedAlignment.x / totalInfluence;
                alignment.ValueRW.OrderAxis = weightedAlignment.y / totalInfluence;
                alignment.ValueRW.PurityAxis = weightedAlignment.z / totalInfluence;
            }
        }
    }
}
```

**Alignment Drift Over Time**:
- New members joining shift aggregate alignment
- Dominant faction's alignment pulls aggregate gradually
- High cohesion = alignment change resisted (stable identity)
- Low cohesion = alignment drifts rapidly (identity crisis)

### 5. Aggregate Behavior (Computed)

Aggregates compute **dominant personality** from members.

```csharp
[BurstCompile]
public partial struct AggregateBehaviorUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (aggregate, behavior, members) in
                 SystemAPI.Query<RefRO<AggregateEntity>, RefRW<VillagerBehavior>, DynamicBuffer<AggregateMemberEntry>>())
        {
            if (members.Length == 0)
                continue;

            // Count personality distributions
            int vengefulCount = 0, forgivingCount = 0;
            int boldCount = 0, cravenCount = 0;

            foreach (var member in members)
            {
                if (!SystemAPI.Exists(member.Member))
                    continue;

                var memberBehavior = SystemAPI.GetComponent<VillagerBehavior>(member.Member);

                if (memberBehavior.VengefulVsForgiving > 0.5f)
                    vengefulCount++;
                else
                    forgivingCount++;

                if (memberBehavior.BoldVsCraven > 0.5f)
                    boldCount++;
                else
                    cravenCount++;
            }

            // Majority wins (with influence weighting)
            behavior.ValueRW.VengefulVsForgiving = vengefulCount > forgivingCount ? 0.8f : 0.2f;
            behavior.ValueRW.BoldVsCraven = boldCount > cravenCount ? 0.8f : 0.2f;

            // Modulate by cohesion (low cohesion = moderate behavior)
            float cohesion = aggregate.ValueRO.Cohesion;
            behavior.ValueRW.VengefulVsForgiving = math.lerp(0.5f, behavior.ValueRO.VengefulVsForgiving, cohesion);
            behavior.ValueRW.BoldVsCraven = math.lerp(0.5f, behavior.ValueRO.BoldVsCraven, cohesion);
        }
    }
}
```

---

## Cohesion Mechanics

**Cohesion** represents internal unity. High cohesion = strong collective identity, low cohesion = fracturing.

```csharp
[BurstCompile]
public partial struct AggregateCohesionUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (aggregate, cohesion, members, alignment) in
                 SystemAPI.Query<RefRW<AggregateEntity>, RefRO<AggregateConsensus>, DynamicBuffer<AggregateMemberEntry>, RefRO<VillagerAlignment>>())
        {
            float alignmentVariance = CalculateAlignmentVariance(members, alignment.ValueRO);
            float loyaltyAverage = CalculateLoyaltyAverage(members);
            float leadershipStrength = CalculateLeadershipStrength(members);

            // Cohesion formula
            float baseCohesion = 0.5f;
            baseCohesion += (1f - alignmentVariance) * 0.3f;    // Similar alignments = higher cohesion
            baseCohesion += loyaltyAverage * 0.3f;              // Loyal members = higher cohesion
            baseCohesion += leadershipStrength * 0.2f;          // Strong leaders = higher cohesion
            baseCohesion -= cohesion.ValueRO.DissentCount / (float)members.Length * 0.4f; // Dissent lowers cohesion

            aggregate.ValueRW.Cohesion = math.clamp(baseCohesion, 0f, 1f);
        }
    }
}
```

**Alignment Variance**:
```csharp
float CalculateAlignmentVariance(DynamicBuffer<AggregateMemberEntry> members, VillagerAlignment aggregateAlignment)
{
    float totalVariance = 0f;

    foreach (var member in members)
    {
        if (!SystemAPI.Exists(member.Member))
            continue;

        var memberAlignment = SystemAPI.GetComponent<VillagerAlignment>(member.Member);

        float variance = math.abs(memberAlignment.MoralAxis - aggregateAlignment.MoralAxis)
                       + math.abs(memberAlignment.OrderAxis - aggregateAlignment.OrderAxis)
                       + math.abs(memberAlignment.PurityAxis - aggregateAlignment.PurityAxis);

        totalVariance += variance;
    }

    return math.clamp(totalVariance / (members.Length * 6f), 0f, 1f); // Normalize to 0-1
}
```

**Cohesion Effects**:
- **High Cohesion (>0.8)**: Aggregate acts decisively, consensus = Unanimous/Majority
- **Medium Cohesion (0.4-0.8)**: Aggregate acts normally, consensus = Majority/Plurality
- **Low Cohesion (<0.4)**: Aggregate paralyzed, consensus = Anarchy, risk of splintering

---

## Consensus Decision-Making

Aggregates make decisions via **consensus voting**.

```csharp
// Decision proposal for aggregate to vote on
public struct AggregateDecisionProposal : IComponentData
{
    public DecisionType Decision;
    public Entity Target;                        // Target of decision (enemy, resource, etc.)
    public float3 Destination;                   // Location (if applicable)
    public ushort ProposedByMember;              // Who suggested this
    public ushort VotesFor;
    public ushort VotesAgainst;
    public DecisionStatus Status;
}

public enum DecisionType : byte
{
    // Universal
    MoveTo,
    Disband,

    // Combat
    Attack,
    Retreat,
    Negotiate,

    // Economic
    Trade,
    Construct,
    HireMembers,

    // Social
    FormAlliance,
    DeclareWar,
    Merge,
    Splinter
}

public enum DecisionStatus : byte
{
    Proposed,       // Awaiting votes
    Voting,         // Active vote
    Passed,         // Majority approved
    Rejected,       // Majority denied
    Vetoed          // Leader overrode
}
```

**Voting System**:
```csharp
[BurstCompile]
public partial struct AggregateVotingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (decision, aggregate, members, consensus) in
                 SystemAPI.Query<RefRW<AggregateDecisionProposal>, RefRO<AggregateEntity>, DynamicBuffer<AggregateMemberEntry>, RefRO<AggregateConsensus>>())
        {
            if (decision.ValueRO.Status != DecisionStatus.Voting)
                continue;

            decision.ValueRW.VotesFor = 0;
            decision.ValueRW.VotesAgainst = 0;

            // Each member votes based on alignment/behavior
            foreach (var member in members)
            {
                if (!SystemAPI.Exists(member.Member))
                    continue;

                bool voteYes = EvaluateMemberVote(member.Member, decision.ValueRO, aggregate.ValueRO);

                if (voteYes)
                    decision.ValueRW.VotesFor += (ushort)(member.Influence * 100);
                else
                    decision.ValueRW.VotesAgainst += (ushort)(member.Influence * 100);
            }

            // Determine outcome based on consensus mode
            switch (consensus.ValueRO.Mode)
            {
                case ConsensusMode.Unanimous:
                    decision.ValueRW.Status = decision.ValueRO.VotesAgainst == 0 ? DecisionStatus.Passed : DecisionStatus.Rejected;
                    break;

                case ConsensusMode.Majority:
                    decision.ValueRW.Status = decision.ValueRO.VotesFor > decision.ValueRO.VotesAgainst ? DecisionStatus.Passed : DecisionStatus.Rejected;
                    break;

                case ConsensusMode.Plurality:
                    // Largest faction wins (even if <50%)
                    decision.ValueRW.Status = decision.ValueRO.VotesFor > decision.ValueRO.VotesAgainst ? DecisionStatus.Passed : DecisionStatus.Rejected;
                    break;

                case ConsensusMode.Dictatorial:
                    // Only leader vote matters
                    var leader = FindLeader(members);
                    bool leaderVote = EvaluateMemberVote(leader, decision.ValueRO, aggregate.ValueRO);
                    decision.ValueRW.Status = leaderVote ? DecisionStatus.Passed : DecisionStatus.Rejected;
                    break;

                case ConsensusMode.Anarchy:
                    // No consensus possible
                    decision.ValueRW.Status = DecisionStatus.Rejected;
                    break;
            }
        }
    }
}
```

**Member Vote Evaluation** (uses alignment + behavior):
```csharp
bool EvaluateMemberVote(Entity member, AggregateDecisionProposal decision, AggregateEntity aggregate)
{
    var alignment = GetComponent<VillagerAlignment>(member);
    var behavior = GetComponent<VillagerBehavior>(member);

    switch (decision.Decision)
    {
        case DecisionType.Attack:
            // Evil + Vengeful = likely yes
            // Good + Forgiving = likely no
            float aggressionScore = (-alignment.MoralAxis + 1f) / 2f; // Evil = 1.0, Good = 0.0
            aggressionScore += behavior.VengefulVsForgiving;
            return aggressionScore > 1.0f; // Need both traits to vote yes

        case DecisionType.Retreat:
            // Craven = likely yes
            // Bold = likely no
            return behavior.BoldVsCraven < 0.3f;

        case DecisionType.FormAlliance:
            // Good + Lawful = likely yes
            // Evil + Chaotic = likely no
            float diplomacyScore = (alignment.MoralAxis + 1f) / 2f + (alignment.OrderAxis + 1f) / 2f;
            return diplomacyScore > 1.0f;

        case DecisionType.Splinter:
            // Low loyalty + misaligned = likely yes
            var memberEntry = GetMemberEntry(aggregate, member);
            float alignmentDelta = CalculateAlignmentDelta(alignment, GetComponent<VillagerAlignment>(aggregate.Entity));
            return memberEntry.LoyaltyLevel < 50 && alignmentDelta > 2.0f;

        default:
            return math.hash(new uint2((uint)member.Index, (uint)decision.Decision)) % 2 == 0; // Random
    }
}
```

---

## Splintering & Merging

Aggregates can **split** (splinter) or **combine** (merge) based on internal dynamics.

### Splintering (Fracturing)

```csharp
public struct AggregateSplinterRequest : IComponentData
{
    public SplinterReason Reason;
    public Entity SplinterLeader;                // Member who leads breakaway faction
    public ushort EstimatedSplinterSize;         // How many members will leave
}

public enum SplinterReason : byte
{
    LowCohesion,        // Cohesion <0.2 for >100 ticks
    AlignmentConflict,  // Dominant factions too different
    LeadershipDispute,  // Multiple leaders, power struggle
    ResourceShortage,   // Not enough resources for all members
    IdeologicalSplit,   // Doctrine/culture clash
    PlayerForced        // Player command to split
}
```

**Splinter Detection System**:
```csharp
[BurstCompile]
public partial struct AggregateSplinterDetectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (aggregate, cohesion, members, consensus) in
                 SystemAPI.Query<RefRW<AggregateEntity>, RefRO<AggregateConsensus>, DynamicBuffer<AggregateMemberEntry>, RefRO<VillagerAlignment>>())
        {
            // Check splintering conditions
            bool shouldSplinter = false;
            SplinterReason reason = SplinterReason.LowCohesion;

            // Condition 1: Very low cohesion
            if (aggregate.ValueRO.Cohesion < 0.2f)
            {
                shouldSplinter = true;
                reason = SplinterReason.LowCohesion;
            }

            // Condition 2: Large dissent
            if (consensus.ValueRO.DissentCount > members.Length / 2)
            {
                shouldSplinter = true;
                reason = SplinterReason.AlignmentConflict;
            }

            // Condition 3: Multiple leaders competing
            int leaderCount = CountLeaders(members);
            if (leaderCount > 1)
            {
                shouldSplinter = true;
                reason = SplinterReason.LeadershipDispute;
            }

            if (!shouldSplinter)
                continue;

            // Create splinter request
            var splinterLeader = FindStrongestDissident(members, consensus.ValueRO.Agreement);
            state.EntityManager.AddComponentData(aggregate.ValueRO.Entity, new AggregateSplinterRequest
            {
                Reason = reason,
                SplinterLeader = splinterLeader,
                EstimatedSplinterSize = (ushort)(members.Length / 2) // Estimate half will leave
            });
        }
    }
}
```

**Splinter Execution**:
```csharp
public partial struct AggregateSplinterExecutionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (splinterRequest, aggregate, members) in
                 SystemAPI.Query<RefRO<AggregateSplinterRequest>, RefRO<AggregateEntity>, DynamicBuffer<AggregateMemberEntry>>())
        {
            // Create new aggregate entity for breakaway faction
            var newAggregate = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<AggregateEntity>(newAggregate);
            state.EntityManager.AddComponent<VillagerAlignment>(newAggregate);
            state.EntityManager.AddComponent<VillagerBehavior>(newAggregate);
            state.EntityManager.AddBuffer<AggregateMemberEntry>(newAggregate);

            var newMembers = state.EntityManager.GetBuffer<AggregateMemberEntry>(newAggregate);

            // Transfer members who align with splinter leader
            var leaderAlignment = SystemAPI.GetComponent<VillagerAlignment>(splinterRequest.ValueRO.SplinterLeader);

            for (int i = members.Length - 1; i >= 0; i--)
            {
                var member = members[i];
                var memberAlignment = SystemAPI.GetComponent<VillagerAlignment>(member.Member);

                float alignmentSimilarity = CalculateAlignmentSimilarity(memberAlignment, leaderAlignment);

                // If member aligns with splinter leader, transfer them
                if (alignmentSimilarity > 0.7f || member.Member == splinterRequest.ValueRO.SplinterLeader)
                {
                    newMembers.Add(member);
                    members.RemoveAt(i);
                }
            }

            // Update cohesion for both aggregates (should increase after split)
            // Remove splinter request
            state.EntityManager.RemoveComponent<AggregateSplinterRequest>(aggregate.ValueRO.Entity);
        }
    }
}
```

### Merging (Consolidation)

```csharp
public struct AggregateMergeRequest : IComponentData
{
    public Entity OtherAggregate;                // Aggregate to merge with
    public MergeReason Reason;
    public Entity SurvivingAggregate;            // Which entity remains (other destroyed)
}

public enum MergeReason : byte
{
    ProximityAndAlignment,  // Both nearby + similar alignment
    PlayerForced,           // Player command
    Conquest,               // One absorbed other via combat
    Treaty,                 // Diplomatic merger
    ResourceSharing         // Economic alliance becomes one entity
}
```

**Merge Detection** (mutual compatibility):
```csharp
[BurstCompile]
public partial struct AggregateMergeDetectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var aggregates = SystemAPI.Query<RefRO<AggregateEntity>, RefRO<VillagerAlignment>, RefRO<LocalTransform>>().ToEntityArray(Allocator.Temp);

        for (int i = 0; i < aggregates.Length; i++)
        {
            for (int j = i + 1; j < aggregates.Length; j++)
            {
                var agg1 = aggregates[i];
                var agg2 = aggregates[j];

                var alignment1 = SystemAPI.GetComponent<VillagerAlignment>(agg1);
                var alignment2 = SystemAPI.GetComponent<VillagerAlignment>(agg2);

                float alignmentSimilarity = CalculateAlignmentSimilarity(alignment1, alignment2);

                var pos1 = SystemAPI.GetComponent<LocalTransform>(agg1).Position;
                var pos2 = SystemAPI.GetComponent<LocalTransform>(agg2).Position;
                float distance = math.distance(pos1, pos2);

                // Merge conditions: very similar alignment + very close proximity
                if (alignmentSimilarity > 0.9f && distance < 10f)
                {
                    state.EntityManager.AddComponentData(agg1, new AggregateMergeRequest
                    {
                        OtherAggregate = agg2,
                        Reason = MergeReason.ProximityAndAlignment,
                        SurvivingAggregate = agg1 // Larger one survives
                    });
                }
            }
        }
    }
}
```

---

## Leadership & Influence

Leaders have outsized influence on aggregate decisions.

```csharp
public struct AggregateLeadership : IComponentData
{
    public Entity CurrentLeader;
    public LeadershipStyle Style;
    public float LeaderInfluence;                // 0.0-1.0 (how much leader sways decisions)
    public ushort LeadershipDurationTicks;       // How long current leader has ruled
}

public enum LeadershipStyle : byte
{
    Democratic,     // Leader proposes, members vote (influence = 0.3)
    Autocratic,     // Leader decides, members comply (influence = 0.9)
    Oligarchic,     // Council of leaders (influence = 0.6)
    Anarchic        // No leader, mob rule (influence = 0.0)
}
```

**Leader Selection** (emergent):
```csharp
Entity SelectLeader(DynamicBuffer<AggregateMemberEntry> members)
{
    // Leader = highest influence member
    Entity leader = Entity.Null;
    float highestInfluence = 0f;

    foreach (var member in members)
    {
        if (member.Influence > highestInfluence)
        {
            highestInfluence = member.Influence;
            leader = member.Member;
        }
    }

    return leader;
}
```

**Influence Calculation** (earned over time):
```csharp
void UpdateMemberInfluence(ref AggregateMemberEntry member, AggregateEntity aggregate)
{
    // Base influence = 1.0 / memberCount (equal share)
    float baseInfluence = 1f / aggregate.MemberCount;

    // Modifiers
    float roleMultiplier = member.Role switch
    {
        MemberRole.Founder => 2.0f,
        MemberRole.Leader => 1.8f,
        MemberRole.Officer => 1.4f,
        MemberRole.Veteran => 1.2f,
        MemberRole.Member => 1.0f,
        _ => 1.0f
    };

    float loyaltyMultiplier = member.LoyaltyLevel / 100f; // 0-1.0
    float tenureMultiplier = math.min(1f + (member.JoinedTick / 10000f), 2f); // Max 2x for long tenure

    member.Influence = baseInfluence * roleMultiplier * loyaltyMultiplier * tenureMultiplier;
    member.Influence = math.clamp(member.Influence, 0f, 1f);
}
```

---

## Aggregate AI Integration

Aggregates use the **same AI framework** as individuals (see [AIBehaviorModules.md](AIBehaviorModules.md)).

**Aggregate Utility Evaluation**:
```csharp
void EvaluateFleetActions(
    ref AIAgent agent,
    DynamicBuffer<AISensorReading> sensors,
    DynamicBuffer<AIUtilityOption> options,
    LocalTransform transform)
{
    var fleet = GetComponent<AggregateEntity>(agent.Entity);
    var members = GetBuffer<AggregateMemberEntry>(agent.Entity);

    // Action: CoordinatedAttack (fleet attacks as unit)
    foreach (var sensor in sensors)
    {
        if (sensor.ThreatLevel < 0.3f)
            continue;

        // Calculate fleet combat power from member ships
        float fleetPower = 0f;
        foreach (var member in members)
        {
            if (HasComponent<Carrier>(member.Member))
            {
                var carrier = GetComponent<Carrier>(member.Member);
                fleetPower += GetCarrierCombatPower(carrier);
            }
        }

        float enemyPower = EstimateEnemyPower(sensor.DetectedEntity);
        float powerRatio = fleetPower / math.max(enemyPower, 0.1f);

        if (powerRatio > 1.5f)
        {
            // Fleet has advantage, attack
            options.Add(new AIUtilityOption
            {
                Action = ActionType.CoordinateAttack,
                Target = sensor.DetectedEntity,
                Destination = sensor.Position,
                UtilityScore = powerRatio / 2f,
                ConfidenceLevel = 0.9f
            });
        }
    }
}
```

**Member Execution of Aggregate Decisions**:
When aggregate decides to attack, individual members execute.

```csharp
[BurstCompile]
public partial struct AggregateOrderPropagationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Query aggregates with active decisions
        foreach (var (decision, aggregate, members) in
                 SystemAPI.Query<RefRO<AggregateDecisionProposal>, RefRO<AggregateEntity>, DynamicBuffer<AggregateMemberEntry>>())
        {
            if (decision.ValueRO.Status != DecisionStatus.Passed)
                continue;

            // Propagate decision to all members
            foreach (var member in members)
            {
                if (!SystemAPI.Exists(member.Member))
                    continue;

                // Create individual task for member
                var memberTask = SystemAPI.GetComponentRW<AITaskState>(member.Member);
                memberTask.ValueRW.CurrentAction = ConvertDecisionToAction(decision.ValueRO.Decision);
                memberTask.ValueRW.TargetEntity = decision.ValueRO.Target;
                memberTask.ValueRW.TargetPosition = decision.ValueRO.Destination;
                memberTask.ValueRW.Phase = TaskPhase.Traveling;
            }
        }
    }
}
```

---

## Nested Aggregates

Aggregates can contain other aggregates (e.g., Empire contains Planets, Planets contain Villages).

```csharp
// Example: Empire → Planet → Village hierarchy
Entity empireEntity;
AddComponent(empireEntity, new AggregateEntity
{
    Type = AggregateType.Empire,
    Parent = Entity.Null // Top-level
});

Entity planetEntity;
AddComponent(planetEntity, new AggregateEntity
{
    Type = AggregateType.Planet,
    Parent = empireEntity // Part of empire
});

Entity villageEntity;
AddComponent(villageEntity, new AggregateEntity
{
    Type = AggregateType.Village,
    Parent = planetEntity // Part of planet
});
```

**Hierarchy Rules**:
- Child aggregates inherit parent alignment (modulated by local variance)
- Parent aggregates compute state from ALL descendants (recursive)
- Decisions can cascade down (Empire declares war → all Planets mobilize → all Villages contribute)

---

## Open Questions / Design Decisions Needed

1. **Vote Weighting**: Should leaders have veto power, or just higher influence votes?
   - *Suggestion*: Dictatorial mode = veto power, Democratic mode = higher influence only

2. **Splinter Size**: Minimum members to form valid aggregate after split?
   - *Suggestion*: 2 members minimum, otherwise individuals leave as free agents

3. **Merge Identity**: When two aggregates merge, which name/identity persists?
   - *Suggestion*: Larger aggregate's name/identity, smaller dissolved

4. **Loyalty Decay**: How fast does loyalty decrease when misaligned with aggregate?
   - *Suggestion*: -1 loyalty per 100 ticks if alignment delta >2.0

5. **Consensus Transition**: Can consensus mode change dynamically (e.g., Democratic → Dictatorial under crisis)?
   - *Suggestion*: Yes - if cohesion <0.3, transition to Anarchy; if strong leader emerges, transition to Dictatorial

6. **Nested Decision Overrides**: Can parent aggregate override child aggregate's decision?
   - *Suggestion*: Yes, but reduces child cohesion by 0.2 (resentment)

7. **Member Recruitment**: How do new members join aggregate?
   - *Suggestion*: AI individuals near aggregate with similar alignment auto-join (if aggregate accepts)

8. **Expulsion**: Can aggregate vote to kick out dissenting members?
   - *Suggestion*: Yes, requires 2/3 majority vote (high bar to prevent abuse)

---

## Implementation Notes

- **AggregateAlignmentUpdateSystem** = computes aggregate alignment from members
- **AggregateBehaviorUpdateSystem** = computes aggregate personality from members
- **AggregateCohesionUpdateSystem** = calculates cohesion based on variance/loyalty
- **AggregateVotingSystem** = processes decision proposals, tallies votes
- **AggregateSplinterDetectionSystem** = detects fracturing conditions
- **AggregateSplinterExecutionSystem** = creates new aggregate from breakaway faction
- **AggregateMergeDetectionSystem** = finds compatible aggregates nearby
- **AggregateOrderPropagationSystem** = sends aggregate decisions to members
- All systems respect `RewindState.Mode` (skip during Playback)

---

## References

- **Entity Agnostic Design**: [Entity_Agnostic_Design.md](../Concepts/Entity_Agnostic_Design.md) - Foundation principles
- **AI Behavior Modules**: [AIBehaviorModules.md](AIBehaviorModules.md) - AI framework used by aggregates
- **Alignment System**: Aggregates use same alignment components as individuals
- **Band Formation**: [FactionAndGuildSystem.md](../DesignNotes/FactionAndGuildSystem.md) - Band-specific mechanics
- **Fleet Composition**: [CarrierArchitecture.md](CarrierArchitecture.md) - Fleet as carrier aggregates
- **Village Dynamics**: [VillageSpatialGrowth.md](VillageSpatialGrowth.md) - Village as villager aggregates
