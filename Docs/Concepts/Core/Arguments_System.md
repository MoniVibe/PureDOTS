# Arguments System

**Status:** Concept
**Category:** Core - Social Decision Protocol
**Scope:** Cross-Project (Godgame: Villager/Tactical Decisions, Space4X: Crew/Fleet Decisions)
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Implement arguments as an event-driven, small-N interaction system that resolves conflicting intents/orders or high-stakes choices through bounded decision protocols, producing selected options, relationship deltas, and potential escalation events.

**Secondary Goals:**
- Avoid "everyone debates everyone" complexity (O(P·O) bounded, not O(N²))
- Integrate with existing communication, relations, and governance systems
- Enable strategic decision-making (tactics, policies, plans) through social interaction
- Support relationship negotiation and escalation paths
- Scale efficiently using session budgets and LOD

**Key Principle:** Arguments are NOT a new AI layer—they're special interactions that produce decisions. Only leaders/advisors/proxies argue; mass units follow the chosen plan.

---

## System Overview

### Key Insight

**Arguments are bounded decision protocols that run only when:**
- Two or more entities have conflicting intents/orders
- A high-stakes choice is needed (tactics pivot, siege plan, policy change)
- A relationship negotiation is triggered (any "quarrel" is a social decision)

**What Arguments Produce:**
1. **Selected Option:** Plan/policy chosen by group
2. **Relationship Deltas:** Relations updated based on outcome
3. **Escalation Events:** Potential conflict/violence if resolution fails

**Not General Chatter:**
- Arguments are NOT generic comm back-and-forth
- They're single session entities with deadlines
- Scope limited to decision-makers (leaders, advisors, proxies)
- Mass units follow the chosen plan (no participation)

---

## Data Model (Cheap, Fixed-Size)

### ArgumentSession Entity

**Session Entity Components:**

```csharp
// Session metadata
public struct ArgumentSession : IComponentData
{
    public FixedString64Bytes TopicId;      // "combat_tactic", "siege_plan", "policy_change"
    public ArgumentStage Stage;             // Initializing, Preferences, Resolving, Concluded
    public uint StartTick;                  // When session started
    public uint DeadlineTick;               // When session must conclude
    public byte Round;                      // Current round (0-4 max)
    public byte MaxRounds;                  // Maximum rounds before forced resolution
    public Entity Initiator;                // Entity that triggered the argument
}

public enum ArgumentStage : byte
{
    Initializing = 0,      // Gathering participants
    Preferences = 1,       // Computing utilities
    Resolving = 2,         // Computing group support
    Concluded = 3          // Outcome determined
}

// Participant buffer (≤ 8-16 participants)
[InternalBufferCapacity(16)]
public struct ArgumentParticipant : IBufferElementData
{
    public Entity Entity;                   // Participant entity
    public ArgumentRole Role;               // Leader, Advisor, Proxy
    public float Influence;                 // 0-1: weight in group decision
    public float Temperament;               // 0-1: aggression/calmness
    public float RiskTolerance;             // 0-1: willingness to take risks
    public byte AuthorityRank;              // 0-255: hierarchical authority
    // Trust to other participants (sparse, only store significant relationships)
    public NativeArray<float> TrustToOthers; // Optional: computed from EntityRelation
}

public enum ArgumentRole : byte
{
    Leader = 0,            // Primary decision-maker
    Advisor = 1,           // Provides input, no final authority
    Proxy = 2              // Represents a group (village, crew, faction)
}

// Option buffer (≤ 4-12 options)
[InternalBufferCapacity(12)]
public struct ArgumentOption : IBufferElementData
{
    public FixedString64Bytes OptionId;     // "flank", "spearhead", "hold", "withdraw"
    public ArgumentOptionTags Tags;         // Categorization flags
    public float Cost;                      // Resource/capability cost
    public float Risk;                      // 0-1: risk level
    public uint PreconditionsMask;          // Bitflags for required conditions
}

[Flags]
public enum ArgumentOptionTags : uint
{
    None = 0,
    Aggressive = 1 << 0,       // Offensive tactic
    Defensive = 1 << 1,        // Defensive tactic
    Risky = 1 << 2,            // High risk
    Safe = 1 << 3,             // Low risk
    Expensive = 1 << 4,        // High cost
    Cheap = 1 << 5,            // Low cost
    Fast = 1 << 6,             // Quick execution
    Slow = 1 << 7,             // Takes time
    RequiresAuthority = 1 << 8 // Needs high authority
}

// Optional proposal buffer (participants propose options)
[InternalBufferCapacity(8)]
public struct ArgumentProposal : IBufferElementData
{
    public Entity From;                      // Who proposed
    public FixedString64Bytes OptionId;      // Proposed option
    public float Confidence;                 // 0-1: how confident in proposal
}
```

### Argument State Tracking

```csharp
// Participant preferences (computed each round)
public struct ArgumentParticipantPreferences : IBufferElementData
{
    public Entity Participant;
    public BlobAssetReference<ArgumentPreferenceBlob> Preferences; // OptionId → Utility mapping
}

// Group support scores (computed from preferences)
public struct ArgumentSupportScores : IComponentData
{
    public BlobAssetReference<ArgumentSupportBlob> Scores; // OptionId → Support score
}

// Outcome determination
public struct ArgumentOutcome : IComponentData
{
    public FixedString64Bytes WinningOptionId;
    public float WinningMargin;              // 0-1: how decisive (higher = more consensus)
    public ArgumentResolutionType ResolutionType; // How it was resolved
    public bool Escalated;                   // Did it escalate to violence?
}

public enum ArgumentResolutionType : byte
{
    Concede = 0,             // Large margin + high trust → concede gracefully
    Agree = 1,               // Consensus reached
    AgreeToDisagree = 2,     // Small margin + low authority coupling
    Adjourn = 3,             // High uncertainty + deadline not urgent
    AuthorityOverride = 4,   // Authority forces decision
    Violence = 5             // Escalation to conflict
}
```

---

## How Arguments Run (O(P·O), Bounded)

### 1. Trigger (Rare Events Only)

**Trigger Conditions:**

```csharp
public struct ArgumentTriggerSystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        // Check for conflicting intents/orders
        foreach (var (intentA, entityA) in SystemAPI.Query<RefRO<EntityIntent>>().WithEntityAccess())
        {
            // Find entities with conflicting intents
            foreach (var (intentB, entityB) in SystemAPI.Query<RefRO<EntityIntent>>().WithEntityAccess())
            {
                if (entityA == entityB) continue;
                
                // Check if intents conflict
                float conflictScore = ComputeConflictScore(intentA.ValueRO, intentB.ValueRO);
                float stakeScore = ComputeStakeScore(entityA, entityB);
                
                // Check if participants are in contact (spatial proximity or comm link)
                bool inContact = CheckContact(entityA, entityB);
                
                // Create session only if thresholds met
                if (conflictScore > ConflictThreshold && 
                    stakeScore > StakeThreshold && 
                    inContact)
                {
                    CreateArgumentSession(entityA, entityB, conflictScore, stakeScore);
                }
            }
        }
        
        // Check for high-stakes choices (tactics, policies, plans)
        CheckHighStakesDecisions();
    }
    
    float ComputeConflictScore(EntityIntent intentA, EntityIntent intentB)
    {
        // Intent modes conflict (e.g., Attack vs Flee)
        if (intentA.Mode == IntentMode.Attack && intentB.Mode == IntentMode.Flee)
            return 1.0f;
        
        // Target conflicts (same target, different actions)
        if (intentA.TargetEntity == intentB.TargetEntity && intentA.Mode != intentB.Mode)
            return 0.8f;
        
        return 0f;
    }
    
    float ComputeStakeScore(Entity a, Entity b)
    {
        // Higher stakes = more important entities (leaders, large groups)
        float influenceA = GetInfluence(a);
        float influenceB = GetInfluence(b);
        return (influenceA + influenceB) * 0.5f;
    }
    
    bool CheckContact(Entity a, Entity b)
    {
        // Spatial proximity (within communication range)
        var posA = SystemAPI.GetComponent<LocalTransform>(a).Position;
        var posB = SystemAPI.GetComponent<LocalTransform>(b).Position;
        float distance = math.distance(posA, posB);
        
        if (distance < CommunicationRange)
            return true;
        
        // Or comm link exists (for remote arguments)
        return HasCommLink(a, b);
    }
}
```

**Session Budget (Scalability):**

```csharp
public struct ArgumentSessionBudget : IComponentData
{
    public byte MaxSessionsPerRegion;      // Default: 4 per region/ship per tick
    public byte ActiveSessionCount;        // Current active sessions
}
```

### 2. Preferences (Local Utility)

**Each Participant Computes Utility Per Option:**

```csharp
public struct ArgumentPreferenceSystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        foreach (var (session, participants, options) in SystemAPI.Query<
            RefRO<ArgumentSession>,
            DynamicBuffer<ArgumentParticipant>,
            DynamicBuffer<ArgumentOption>>())
        {
            if (session.ValueRO.Stage != ArgumentStage.Preferences)
                continue;
            
            // For each participant, compute utility for each option
            for (int pIdx = 0; pIdx < participants.Length; pIdx++)
            {
                var participant = participants[pIdx];
                var entity = participant.Entity;
                
                // Get participant's outlook/alignment (affects preferences)
                var alignment = SystemAPI.GetComponentRO<VillagerAlignment>(entity);
                var outlook0 = SystemAPI.GetComponentRO<VillagerOutlook0>(entity);
                var behavior = SystemAPI.GetComponentRO<VillagerBehavior>(entity);
                
                // Compute utility for each option
                var utilities = new NativeList<float>(Allocator.Temp);
                for (int oIdx = 0; oIdx < options.Length; oIdx++)
                {
                    var option = options[oIdx];
                    float utility = ComputeParticipantUtility(
                        participant,
                        option,
                        alignment.ValueRO,
                        outlook0.ValueRO,
                        behavior.ValueRO);
                    utilities.Add(utility);
                }
                
                // Store preferences (softmax for probability distribution)
                StoreParticipantPreferences(session.GetEntity(), entity, utilities);
            }
        }
    }
    
    float ComputeParticipantUtility(
        ArgumentParticipant participant,
        ArgumentOption option,
        VillagerAlignment alignment,
        VillagerOutlook0 outlook,
        VillagerBehavior behavior)
    {
        // Utility formula: U_i(o) = w_i · f(o, context) + b_i(relationships) - λ_i · risk(o)
        
        // w_i: outlook-based preference weight
        float outlookWeight = ComputeOutlookWeight(option, outlook);
        
        // f(o, context): context-dependent function
        float contextValue = ComputeContextValue(option);
        
        // b_i(relationships): relationship bonus/penalty
        float relationshipBonus = ComputeRelationshipBonus(participant, option);
        
        // λ_i: risk tolerance (from temperament)
        float riskPenalty = participant.RiskTolerance * option.Risk;
        
        float utility = outlookWeight * contextValue + relationshipBonus - riskPenalty;
        return utility;
    }
    
    float ComputeOutlookWeight(ArgumentOption option, VillagerOutlook0 outlook)
    {
        // Alignment affects option preference
        // Example: Lawful entities prefer structured options, Chaotic prefer flexible
        float weight = 1.0f;
        
        if (option.Tags.HasFlag(ArgumentOptionTags.RequiresAuthority))
        {
            // Lawful entities favor authority-based options
            // (outlook.OrderAxis would increase weight, but simplified here)
            weight *= 1.2f;
        }
        
        return weight;
    }
    
    float ComputeRelationshipBonus(ArgumentParticipant participant, ArgumentOption option)
    {
        // If option was proposed by trusted entity, add bonus
        // If option conflicts with trusted entity's preference, subtract
        // Simplified: use average trust to other participants
        float avgTrust = ComputeAverageTrust(participant);
        return avgTrust * 0.1f; // Small bonus from trust
    }
}
```

### 3. Group Resolution (Fast)

**Compute Group Support:**

```csharp
public struct ArgumentResolutionSystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        foreach (var (session, participants, options) in SystemAPI.Query<
            RefRO<ArgumentSession>,
            DynamicBuffer<ArgumentParticipant>,
            DynamicBuffer<ArgumentOption>>())
        {
            if (session.ValueRO.Stage != ArgumentStage.Resolving)
                continue;
            
            // Compute group support for each option
            var supportScores = new NativeList<float>(Allocator.Temp);
            
            for (int oIdx = 0; oIdx < options.Length; oIdx++)
            {
                float support = 0f;
                
                // S(o) = Σ_i Influence_i · softmax_i(U_i(o))
                for (int pIdx = 0; pIdx < participants.Length; pIdx++)
                {
                    var participant = participants[pIdx];
                    float utility = GetParticipantUtility(participant, oIdx);
                    float softmaxProb = ComputeSoftmax(participant, utility, options.Length);
                    
                    support += participant.Influence * softmaxProb;
                }
                
                supportScores.Add(support);
            }
            
            // Pick winner (deterministic argmax for sim stability)
            int winnerIdx = FindArgMax(supportScores);
            var winnerOption = options[winnerIdx];
            float winningMargin = ComputeWinningMargin(supportScores, winnerIdx);
            
            // Determine resolution type
            var resolutionType = DetermineResolutionType(
                winningMargin,
                participants,
                session.ValueRO);
            
            // Apply outcome
            ApplyArgumentOutcome(session.GetEntity(), winnerOption, resolutionType, winningMargin);
        }
    }
    
    float ComputeSoftmax(ArgumentParticipant participant, float utility, int optionCount)
    {
        // Simplified softmax (normalize utilities to probabilities)
        // In practice, use temperature parameter for variability
        float temperature = 1.0f; // Lower = more decisive, Higher = more random
        float expUtil = math.exp(utility / temperature);
        
        // Normalize (simplified: assume all options computed)
        return expUtil / optionCount; // Simplified, actual softmax requires sum of all exp utilities
    }
    
    ArgumentResolutionType DetermineResolutionType(
        float winningMargin,
        DynamicBuffer<ArgumentParticipant> participants,
        ArgumentSession session)
    {
        // Resolution type mapping based on:
        // - winning margin (how decisive)
        // - authority gap
        // - trust/cohesion
        // - escalation propensity
        
        float avgTrust = ComputeAverageTrust(participants);
        float authorityGap = ComputeAuthorityGap(participants);
        float hostility = ComputeHostility(participants);
        
        // Concede: large margin + high trust
        if (winningMargin > 0.7f && avgTrust > 0.6f)
            return ArgumentResolutionType.Concede;
        
        // Agree: consensus reached
        if (winningMargin > 0.5f)
            return ArgumentResolutionType.Agree;
        
        // Agree to disagree: small margin + low authority coupling
        if (winningMargin < 0.3f && authorityGap < 0.3f)
            return ArgumentResolutionType.AgreeToDisagree;
        
        // Adjourn: high uncertainty + deadline not urgent
        if (winningMargin < 0.2f && session.DeadlineTick - session.StartTick > 100)
            return ArgumentResolutionType.Adjourn;
        
        // Violence: high hostility + high risk tolerance + low trust + no authority restraint
        if (hostility > 0.7f && avgTrust < 0.3f && authorityGap < 0.2f)
        {
            // Emit escalation event
            EmitEscalationEvent(session);
            return ArgumentResolutionType.Violence;
        }
        
        // Default: authority override (if high authority present)
        if (authorityGap > 0.5f)
            return ArgumentResolutionType.AuthorityOverride;
        
        return ArgumentResolutionType.Agree; // Fallback
    }
}
```

### 4. Outcome Application

**Apply Selected Option + Relationship Deltas:**

```csharp
public struct ArgumentOutcomeSystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        foreach (var (session, outcome, participants) in SystemAPI.Query<
            RefRO<ArgumentSession>,
            RefRO<ArgumentOutcome>,
            DynamicBuffer<ArgumentParticipant>>())
        {
            if (session.ValueRO.Stage != ArgumentStage.Concluded)
                continue;
            
            // 1. Apply selected option to entities (update intents/orders)
            ApplySelectedOption(session.ValueRO, outcome.ValueRO);
            
            // 2. Emit relationship delta events
            EmitRelationshipEvents(session.ValueRO, outcome.ValueRO, participants);
            
            // 3. Emit escalation events if violence occurred
            if (outcome.ValueRO.Escalated)
            {
                EmitEscalationEvent(session.ValueRO);
            }
            
            // 4. Cleanup session (mark for destruction)
            MarkSessionForCleanup(session.GetEntity());
        }
    }
    
    void ApplySelectedOption(ArgumentSession session, ArgumentOutcome outcome)
    {
        // Update participant entities with chosen option
        // Example: Set EntityIntent to match chosen tactic
        foreach (var participant in GetParticipants(session))
        {
            var intent = SystemAPI.GetComponentRW<EntityIntent>(participant.Entity);
            intent.ValueRW.Mode = MapOptionToIntentMode(outcome.WinningOptionId);
            intent.ValueRW.IntentSetTick = session.StartTick;
        }
    }
    
    void EmitRelationshipEvents(ArgumentSession session, ArgumentOutcome outcome, 
        DynamicBuffer<ArgumentParticipant> participants)
    {
        // Emit SocialEvent for each participant interaction
        for (int i = 0; i < participants.Length; i++)
        {
            for (int j = i + 1; j < participants.Length; j++)
            {
                var participantA = participants[i];
                var participantB = participants[j];
                
                // Determine relationship change based on outcome
                InteractionOutcome interactionOutcome = DetermineInteractionOutcome(
                    outcome, participantA, participantB);
                
                // Emit RecordInteractionRequest (existing system)
                var interactionReq = new RecordInteractionRequest
                {
                    EntityA = participantA.Entity,
                    EntityB = participantB.Entity,
                    Outcome = interactionOutcome,
                    IntensityChange = ComputeIntensityChange(outcome, participantA, participantB)
                };
                
                // Post to entity (existing system handles this)
                PostInteractionRequest(participantA.Entity, interactionReq);
            }
        }
    }
    
    InteractionOutcome DetermineInteractionOutcome(
        ArgumentOutcome outcome,
        ArgumentParticipant participantA,
        ArgumentParticipant participantB)
    {
        // Map resolution type to interaction outcome
        switch (outcome.ResolutionType)
        {
            case ArgumentResolutionType.Concede:
                return InteractionOutcome.Positive; // Graceful concession
            case ArgumentResolutionType.Agree:
                return InteractionOutcome.Neutral;  // Consensus
            case ArgumentResolutionType.AgreeToDisagree:
                return InteractionOutcome.Neutral;  // No hard feelings
            case ArgumentResolutionType.Violence:
                return InteractionOutcome.Hostile;  // Conflict
            case ArgumentResolutionType.AuthorityOverride:
                // Overridden participant feels negative
                if (participantA.AuthorityRank < participantB.AuthorityRank)
                    return InteractionOutcome.Negative;
                return InteractionOutcome.Neutral;
            default:
                return InteractionOutcome.Neutral;
        }
    }
}
```

---

## Scalability (Non-Negotiables)

### Budget Constraints

```csharp
public struct ArgumentSessionBudget : IComponentData
{
    public byte MaxSessionsPerRegion;      // Default: 4 per region/ship per tick
    public byte ActiveSessionCount;
    public uint LastBudgetResetTick;
}

// Enforce budget in trigger system
if (budget.ActiveSessionCount >= budget.MaxSessionsPerRegion)
{
    // Skip creating new session (can queue for next tick)
    return;
}
```

### Scope Limitation

**Only Leaders/Advisors/Proxies Argue:**
- Leaders: Primary decision-makers (squad leaders, village chiefs, ship captains)
- Advisors: Provide input (strategists, counselors, officers)
- Proxies: Represent groups (village representatives, faction delegates)

**Mass Units Follow Plan:**
- Rank-and-file entities do NOT participate in arguments
- They receive the chosen option via orders/intents
- No computational cost for non-decision-makers

### Simulation LOD

**Far Sessions (Tier 2-3):**
- Resolve in 1 step (instant resolution)
- Skip preference computation rounds
- Direct to outcome based on authority/influence

**Near Camera (Tier 0-1):**
- Play 2-4 rounds for flavor/UI
- Full preference computation
- Visual representation of argument progress

```csharp
public enum ArgumentLOD : byte
{
    Full = 0,       // Near camera: multiple rounds, full computation
    Reduced = 1,    // Mid-distance: 1-2 rounds
    Instant = 2     // Far: single-step resolution
}

public ArgumentLOD GetSessionLOD(Entity sessionEntity)
{
    // Query camera distance, determine LOD tier
    float distance = GetDistanceToCamera(sessionEntity);
    if (distance < NearDistance) return ArgumentLOD.Full;
    if (distance < MidDistance) return ArgumentLOD.Reduced;
    return ArgumentLOD.Instant;
}
```

### No Chatter Storms

**Arguments ≠ General Communication:**
- Arguments are bounded sessions with deadlines
- Not continuous back-and-forth
- Single resolution event, then cleanup
- Budget prevents explosion

---

## Integrations

### Communication System

**Session Requires Valid Comm Link:**

```csharp
bool CanCreateArgumentSession(Entity a, Entity b)
{
    // Check spatial proximity (direct communication)
    float distance = math.distance(GetPosition(a), GetPosition(b));
    if (distance < CommunicationRange)
        return true;
    
    // Or check for comm link (remote arguments via radio/psionics)
    return HasCommLink(a, b);
}

// Jamming can force adjourn or default doctrine
void CheckJamming(Entity sessionEntity)
{
    if (IsJammed(sessionEntity))
    {
        // Force adjourn or use default doctrine
        var session = SystemAPI.GetComponentRW<ArgumentSession>(sessionEntity);
        session.ValueRW.Stage = ArgumentStage.Concluded;
        
        var outcome = SystemAPI.GetComponentRW<ArgumentOutcome>(sessionEntity);
        outcome.ValueRW.ResolutionType = ArgumentResolutionType.Adjourn;
        outcome.ValueRW.WinningOptionId = GetDefaultDoctrine(sessionEntity);
    }
}
```

### Reactions/Relations System

**Each Session Emits Social Events:**

```csharp
// Social events emitted by arguments
public enum ArgumentSocialEventType : byte
{
    Argued = 0,             // Participants argued
    Conceded = 1,           // Participant conceded gracefully
    Insulted = 2,           // Participant was insulted
    Threatened = 3,         // Participant was threatened
    Overruled = 4,          // Participant was overruled by authority
    WonArgument = 5,        // Participant's option won
    LostArgument = 6        // Participant's option lost
}

// Events update relations via existing RecordInteractionRequest system
// (See ArgumentOutcomeSystem.EmitRelationshipEvents)
```

### Governance/Authority

**Authority Can Override:**

```csharp
void ApplyAuthorityOverride(Entity sessionEntity, Entity authorityEntity)
{
    // Authority forces decision
    var session = SystemAPI.GetComponentRW<ArgumentSession>(sessionEntity);
    var participants = SystemAPI.GetBuffer<ArgumentParticipant>(sessionEntity);
    var options = SystemAPI.GetBuffer<ArgumentOption>(sessionEntity);
    
    // Find authority's preferred option
    var authorityPref = GetAuthorityPreference(authorityEntity, options);
    
    // Force outcome
    var outcome = SystemAPI.GetComponentRW<ArgumentOutcome>(sessionEntity);
    outcome.ValueRW.WinningOptionId = authorityPref;
    outcome.ValueRW.ResolutionType = ArgumentResolutionType.AuthorityOverride;
    outcome.ValueRW.WinningMargin = 1.0f; // Decisive
    
    // Repeated overrides increase grievance → future arguments/mutiny
    IncrementGrievance(sessionEntity, authorityEntity);
}
```

**Grievance System (Future):**

```csharp
public struct AuthorityGrievance : IBufferElementData
{
    public Entity AuthorityEntity;
    public byte GrievanceLevel;      // 0-255: accumulated grievances
    public uint LastOverrideTick;
}

// High grievance → triggers future arguments or mutiny events
```

---

## MVP Slice (Combat Tactic Selection)

### Implementation Scope

**Topic: Combat Tactic Selection**

**Options:**
- `"flank"` - Flanking maneuver (aggressive, risky)
- `"spearhead"` - Direct assault (aggressive, high cost)
- `"hold"` - Defensive position (defensive, safe)
- `"withdraw"` - Retreat (defensive, safe, low risk)

**Participants:**
- Squad leader (Leader role, high influence, high authority)
- 1-3 advisors (Advisor role, moderate influence, low authority)

**Outputs:**
- Chosen tactic (selected option)
- 1 relationship delta event (between leader and advisors)

### Simplified Implementation

```csharp
// Trigger: Detect conflicting combat intents
void CheckCombatArgumentTrigger(Entity squadLeader, DynamicBuffer<EntityIntent> intents)
{
    // Find advisors with conflicting intents
    var leaderIntent = GetIntent(squadLeader);
    
    foreach (var advisor in GetAdvisors(squadLeader))
    {
        var advisorIntent = GetIntent(advisor);
        
        if (Conflicts(leaderIntent, advisorIntent))
        {
            CreateCombatArgumentSession(squadLeader, advisor);
        }
    }
}

void CreateCombatArgumentSession(Entity leader, Entity advisor)
{
    var sessionEntity = EntityManager.CreateEntity();
    
    // Create session
    EntityManager.AddComponent<ArgumentSession>(sessionEntity);
    var session = new ArgumentSession
    {
        TopicId = "combat_tactic",
        Stage = ArgumentStage.Initializing,
        StartTick = CurrentTick,
        DeadlineTick = CurrentTick + 60, // 1 second at 60 Hz
        MaxRounds = 2 // MVP: 2 rounds max
    };
    EntityManager.SetComponentData(sessionEntity, session);
    
    // Add participants
    var participants = EntityManager.AddBuffer<ArgumentParticipant>(sessionEntity);
    participants.Add(new ArgumentParticipant
    {
        Entity = leader,
        Role = ArgumentRole.Leader,
        Influence = 0.7f,
        AuthorityRank = 200,
        RiskTolerance = GetRiskTolerance(leader),
        Temperament = GetTemperament(leader)
    });
    participants.Add(new ArgumentParticipant
    {
        Entity = advisor,
        Role = ArgumentRole.Advisor,
        Influence = 0.3f,
        AuthorityRank = 100,
        RiskTolerance = GetRiskTolerance(advisor),
        Temperament = GetTemperament(advisor)
    });
    
    // Add options
    var options = EntityManager.AddBuffer<ArgumentOption>(sessionEntity);
    options.Add(new ArgumentOption { OptionId = "flank", Tags = ArgumentOptionTags.Aggressive | ArgumentOptionTags.Risky, Risk = 0.7f });
    options.Add(new ArgumentOption { OptionId = "spearhead", Tags = ArgumentOptionTags.Aggressive, Risk = 0.8f, Cost = 0.6f });
    options.Add(new ArgumentOption { OptionId = "hold", Tags = ArgumentOptionTags.Defensive | ArgumentOptionTags.Safe, Risk = 0.2f });
    options.Add(new ArgumentOption { OptionId = "withdraw", Tags = ArgumentOptionTags.Defensive | ArgumentOptionTags.Safe, Risk = 0.1f });
    
    // Add outcome component
    EntityManager.AddComponent<ArgumentOutcome>(sessionEntity);
}
```

### Expected Behavior

**Example Flow:**

1. **Trigger:** Squad leader wants to "flank", advisor wants to "hold"
2. **Session Created:** Leader + advisor as participants, 4 options available
3. **Preferences:** Each computes utility (leader prefers aggressive options, advisor prefers defensive)
4. **Resolution:** Group support computed (leader's influence > advisor's)
5. **Outcome:** "flank" wins (high influence, good margin)
6. **Relationship Event:** Emit `RecordInteractionRequest` (Positive if advisor conceded, Negative if overruled)
7. **Apply:** Set squad's combat intent to "flank"

---

## Related Documentation

- **Communication System:** `Docs/Concepts/Core/Communication_And_Language_System.md` - Comm link requirements
- **Entity Relations:** `Docs/Concepts/Implemented/Villagers/Entity_Relations_And_Interactions.md` - Relationship deltas
- **Entity Intent:** `Packages/com.moni.puredots/Runtime/Runtime/Interrupts/InterruptComponents.cs` - Intent system
- **Alignment/Outlook:** `Docs/Concepts/Entity_Agnostic_Design.md` - Personality traits
- **Governance:** `Docs/Concepts/Politics/Leadership_And_Succession.md` - Authority systems

---

**For Implementers:** Focus on trigger conditions, preference computation, group resolution, and outcome application  
**For Designers:** Focus on option definitions, relationship outcomes, and escalation paths

