# Reactions and Relations System

**Status:** Design Document
**Category:** Core Social Dynamics
**Scope:** PureDOTS Foundation Layer
**Created:** 2025-12-18

---

## Purpose

Defines how entities **react differently to the same events** based on their personality, alignment, and behavior traits. Reactions drive **relation changes**, creating a believable world where context and personality matter.

**Core Concept:** The same action has different consequences depending on who experiences it. A warning shot can intimidate a peaceful captain or provoke a warlike one. A successful raid earns respect from warlike villages but disgust from peaceful ones.

**Key Principle:** Reactions are **computed, not scripted**. Entities evaluate events through the lens of their alignment and behavior, producing authentic, emergent responses.

---

## Architecture Overview

```
EVENT → PERCEPTION → REACTION → RELATION CHANGE → CONSEQUENCES

1. EVENT
   Something happens in world (war, gift, insult, warning shot, etc.)

2. PERCEPTION
   Entity perceives event based on:
   - Alignment (Moral, Order, Purity)
   - Behavior (Vengeful/Forgiving, Bold/Craven)
   - Current relations with involved parties
   - Cultural context

3. REACTION
   Entity generates emotional/behavioral response:
   - Gratitude, anger, fear, respect, disgust, admiration, etc.
   - Intensity based on personality match

4. RELATION CHANGE
   Reaction modifies relations with involved entities:
   - Increase/decrease relation value
   - Add grudges, debts, alliances

5. CONSEQUENCES
   Relation changes trigger behavioral responses:
   - Retaliation, gift-giving, alliance formation, etc.
```

---

## Component Architecture

### 1. Event Components

Events are standardized messages that entities can react to:

```csharp
/// <summary>
/// Base event that entities can perceive and react to
/// </summary>
public struct SocialEvent : IComponentData
{
    /// <summary>
    /// Type of event (war, gift, insult, etc.)
    /// </summary>
    public SocialEventTypeId EventType;

    /// <summary>
    /// Entity that initiated the event (actor)
    /// </summary>
    public Entity Instigator;

    /// <summary>
    /// Entity that is the target of the event (recipient)
    /// </summary>
    public Entity Target;

    /// <summary>
    /// Additional entities involved (witnesses, victims, etc.)
    /// </summary>
    public Entity AffectedParty;

    /// <summary>
    /// Event intensity/magnitude (0-1 normalized)
    /// </summary>
    public float Magnitude;

    /// <summary>
    /// Location where event occurred
    /// </summary>
    public float3 Location;

    /// <summary>
    /// Tick when event occurred
    /// </summary>
    public uint EventTick;

    /// <summary>
    /// Event-specific context data (blob reference)
    /// </summary>
    public BlobAssetReference<SocialEventContextBlob> Context;
}

public enum SocialEventTypeId : ushort
{
    // Conflict
    WarDeclared,
    RaidLaunched,
    RaidSuccessful,
    RaidFailed,
    BattleWon,
    BattleLost,
    TerritoryConquered,

    // Diplomacy
    AllianceOffered,
    AllianceAccepted,
    AllianceRejected,
    AllianceBroken,
    TreatyProposed,
    TreatlySigned,
    TreatyViolated,

    // Trade
    TradeOffered,
    TradeAccepted,
    TradeRejected,
    GiftGiven,
    GiftReceived,
    DebtIncurred,
    DebtRepaid,

    // Social
    InsultDelivered,
    ComplimentGiven,
    ThanksSaid,
    ApologyOffered,
    AssistanceProvided,
    AssistanceRequested,

    // Space4X Specific
    WarningShot,
    HullScan,
    CargoScan,
    TerritoryCrossing,
    ResourceClaim,
    StationConstruction,

    // Godgame Specific
    MiracleWitnessed,
    DisasterSurvived,
    ResourceShared,
    BuildingConstructed,

    // Generic
    Unknown = 0xFFFF
}

/// <summary>
/// Event context data (variable per event type)
/// </summary>
public struct SocialEventContextBlob
{
    /// <summary>
    /// Resource amounts (for trades, gifts, etc.)
    /// </summary>
    public BlobArray<float> ResourceValues;

    /// <summary>
    /// Entity references (for alliances, witnesses, etc.)
    /// </summary>
    public BlobArray<Entity> InvolvedEntities;

    /// <summary>
    /// Success/failure outcome
    /// </summary>
    public bool WasSuccessful;

    /// <summary>
    /// Casualties/losses
    /// </summary>
    public int Casualties;
}
```

### 2. Reaction Components

Reactions are computed responses to events:

```csharp
/// <summary>
/// Entity's reaction to a perceived event
/// </summary>
public struct SocialReaction : IComponentData
{
    /// <summary>
    /// Reference to the event being reacted to
    /// </summary>
    public Entity EventEntity;

    /// <summary>
    /// Type of emotional reaction
    /// </summary>
    public ReactionTypeId ReactionType;

    /// <summary>
    /// Intensity of reaction (-1 to +1, negative = hostile, positive = friendly)
    /// </summary>
    public float Intensity;

    /// <summary>
    /// Who this reaction is directed at (usually event instigator)
    /// </summary>
    public Entity ReactedToEntity;

    /// <summary>
    /// Whether this reaction has been processed into relation changes
    /// </summary>
    public bool IsProcessed;

    /// <summary>
    /// Tick when reaction occurred
    /// </summary>
    public uint ReactionTick;
}

public enum ReactionTypeId : byte
{
    // Positive
    Gratitude,
    Admiration,
    Respect,
    Trust,
    Joy,
    Relief,
    Pride,

    // Negative
    Anger,
    Disgust,
    Fear,
    Contempt,
    Envy,
    Shame,

    // Neutral
    Indifference,
    Curiosity,
    Surprise,

    // Meta
    None = 0xFF
}
```

### 3. Relation Components

Relations track ongoing relationships between entities:

```csharp
/// <summary>
/// Relation from one entity to another (directional)
/// </summary>
public struct EntityRelation : IComponentData
{
    /// <summary>
    /// Entity this relation points to
    /// </summary>
    public Entity TargetEntity;

    /// <summary>
    /// Relation value (-100 to +100)
    /// -100 = sworn enemies, 0 = neutral, +100 = closest allies
    /// </summary>
    public int RelationValue;

    /// <summary>
    /// Relation category based on value
    /// </summary>
    public RelationCategory Category;

    /// <summary>
    /// Last tick this relation was modified
    /// </summary>
    public uint LastModifiedTick;
}

public enum RelationCategory : byte
{
    Enemy = 0,      // -100 to -50
    Hostile = 1,    // -50 to -10
    Unfriendly = 2, // -10 to 0
    Neutral = 3,    // 0 to 10
    Friendly = 4,   // 10 to 50
    Allied = 5      // 50 to 100
}

/// <summary>
/// Buffer of all relations an entity has
/// </summary>
public struct EntityRelationBuffer : IBufferElementData
{
    public Entity TargetEntity;
    public int RelationValue;
    public RelationCategory Category;
}

/// <summary>
/// Grudge: Remembered offense that decays over time
/// </summary>
public struct EntityGrudge : IBufferElementData
{
    /// <summary>
    /// Entity this grudge is against
    /// </summary>
    public Entity TargetEntity;

    /// <summary>
    /// What offense was committed
    /// </summary>
    public SocialEventTypeId OffenseType;

    /// <summary>
    /// Severity of offense (affects decay rate)
    /// </summary>
    public float Severity;

    /// <summary>
    /// Tick when grudge was formed
    /// </summary>
    public uint FormationTick;

    /// <summary>
    /// Current strength (decays over time based on Vengeful trait)
    /// </summary>
    public float Strength;
}

/// <summary>
/// Debt: Remembered favor owed or owing
/// </summary>
public struct EntityDebt : IBufferElementData
{
    /// <summary>
    /// Entity debt is owed to/from
    /// </summary>
    public Entity TargetEntity;

    /// <summary>
    /// Type of debt
    /// </summary>
    public DebtType Type;

    /// <summary>
    /// Magnitude of debt (resource value, lives saved, etc.)
    /// </summary>
    public float Magnitude;

    /// <summary>
    /// Tick when debt was incurred
    /// </summary>
    public uint IncurredTick;
}

public enum DebtType : byte
{
    OweFavor,      // This entity owes them
    OwedFavor,     // They owe this entity
    LifeDebt,      // Saved their life
    BloodDebt      // Killed their kin
}
```

---

## Reaction Computation System

### Core Formula

```csharp
[BurstCompile]
public static float ComputeReactionIntensity(
    SocialEvent socialEvent,
    VillagerAlignment alignment,
    VillagerBehavior behavior,
    EntityRelation existingRelation)
{
    // 1. Base intensity from event magnitude
    float baseIntensity = socialEvent.Magnitude;

    // 2. Alignment modifier: Does this event align with their values?
    float alignmentModifier = ComputeAlignmentModifier(
        socialEvent.EventType,
        alignment);

    // 3. Behavior modifier: Does their personality amplify/dampen reaction?
    float behaviorModifier = ComputeBehaviorModifier(
        socialEvent.EventType,
        behavior);

    // 4. Relation modifier: How do they already feel about the actor?
    float relationModifier = ComputeRelationModifier(
        existingRelation.RelationValue);

    // 5. Combine modifiers
    float finalIntensity = baseIntensity * alignmentModifier * behaviorModifier * relationModifier;

    return math.clamp(finalIntensity, -1f, 1f);
}

[BurstCompile]
static float ComputeAlignmentModifier(
    SocialEventTypeId eventType,
    VillagerAlignment alignment)
{
    // Events have alignment implications
    // Example: RaidSuccessful

    switch (eventType)
    {
        case SocialEventTypeId.RaidSuccessful:
            // Low Moral (ruthless) = positive reaction to successful aggression
            // High Moral (compassionate) = negative reaction to violence
            float moralReaction = math.lerp(1.5f, -1.5f, alignment.MoralAxis);

            // Low Order (chaotic) = enjoys disruption
            // High Order (lawful) = dislikes chaos
            float orderReaction = math.lerp(1.2f, -1.2f, alignment.OrderAxis);

            return (moralReaction + orderReaction) * 0.5f;

        case SocialEventTypeId.GiftGiven:
            // High Moral = appreciates generosity more
            // Purity doesn't care much
            return math.lerp(0.5f, 1.5f, alignment.MoralAxis);

        case SocialEventTypeId.TreatyViolated:
            // High Order = very offended by broken contracts
            // Low Order = expects betrayal
            return math.lerp(0.3f, 2.0f, alignment.OrderAxis);

        // ... more cases
        default:
            return 1.0f;
    }
}

[BurstCompile]
static float ComputeBehaviorModifier(
    SocialEventTypeId eventType,
    VillagerBehavior behavior)
{
    switch (eventType)
    {
        case SocialEventTypeId.InsultDelivered:
            // Vengeful = amplifies negative reaction to insult
            // Forgiving = dampens negative reaction
            return math.lerp(0.5f, 2.0f, behavior.Vengeful);

        case SocialEventTypeId.WarningShot:
            // Bold = may be provoked (positive modifier to aggression)
            // Craven = intimidated (negative modifier, leads to submission)
            return math.lerp(-1.5f, 1.5f, behavior.Bold);

        // ... more cases
        default:
            return 1.0f;
    }
}

[BurstCompile]
static float ComputeRelationModifier(int currentRelation)
{
    // Existing relations bias reactions
    // Friends interpret actions more charitably
    // Enemies interpret actions more suspiciously

    // Normalize relation to -1 to +1
    float normalizedRelation = currentRelation / 100f;

    // Positive events: Friends appreciate more (+50%)
    // Negative events: Enemies react more extremely (+50%)

    return 1.0f + (math.abs(normalizedRelation) * 0.5f);
}
```

---

## Reaction Processing System

```csharp
/// <summary>
/// System that generates reactions to perceived events
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SocialSystemsGroup))]
public partial struct GenerateReactionsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // For each unprocessed event
        foreach (var (socialEvent, eventEntity) in
            SystemAPI.Query<RefRO<SocialEvent>>()
                .WithNone<ProcessedEventTag>()
                .WithEntityAccess())
        {
            // Find all entities that should perceive this event
            var perceivers = FindPerceivers(ref state, socialEvent.ValueRO);

            foreach (var perceiver in perceivers)
            {
                // Get perceiver's personality
                var alignment = state.EntityManager.GetComponentData<VillagerAlignment>(perceiver);
                var behavior = state.EntityManager.GetComponentData<VillagerBehavior>(perceiver);

                // Get existing relation to event instigator
                var relationBuffer = state.EntityManager.GetBuffer<EntityRelationBuffer>(perceiver);
                var existingRelation = FindRelation(relationBuffer, socialEvent.ValueRO.Instigator);

                // Compute reaction
                float intensity = ComputeReactionIntensity(
                    socialEvent.ValueRO,
                    alignment,
                    behavior,
                    existingRelation);

                // Determine reaction type based on event and intensity
                ReactionTypeId reactionType = DetermineReactionType(
                    socialEvent.ValueRO.EventType,
                    intensity);

                // Create reaction entity
                Entity reactionEntity = ecb.CreateEntity();
                ecb.AddComponent(reactionEntity, new SocialReaction
                {
                    EventEntity = eventEntity,
                    ReactionType = reactionType,
                    Intensity = intensity,
                    ReactedToEntity = socialEvent.ValueRO.Instigator,
                    IsProcessed = false,
                    ReactionTick = state.WorldUnmanaged.Time.ElapsedTime
                });

                // Store reaction reference on perceiver
                if (!state.EntityManager.HasBuffer<PendingReactionBuffer>(perceiver))
                {
                    ecb.AddBuffer<PendingReactionBuffer>(perceiver);
                }
                ecb.AppendToBuffer(perceiver, new PendingReactionBuffer
                {
                    ReactionEntity = reactionEntity
                });
            }

            // Mark event as processed
            ecb.AddComponent<ProcessedEventTag>(eventEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    static NativeList<Entity> FindPerceivers(ref SystemState state, SocialEvent socialEvent)
    {
        var perceivers = new NativeList<Entity>(Unity.Collections.Allocator.Temp);

        // Target always perceives
        perceivers.Add(socialEvent.Target);

        // Instigator perceives (for self-reflection)
        if (socialEvent.Instigator != Entity.Null)
            perceivers.Add(socialEvent.Instigator);

        // Affected party perceives
        if (socialEvent.AffectedParty != Entity.Null)
            perceivers.Add(socialEvent.AffectedParty);

        // Find witnesses (entities within perception radius)
        var spatialGrid = SystemAPI.GetSingleton<SpatialGridState>();
        var nearbyEntities = SpatialGridUtility.QueryRadius(
            spatialGrid,
            socialEvent.Location,
            radius: 50f);

        foreach (var entity in nearbyEntities)
        {
            // Only entities with alignment/behavior can react
            if (state.EntityManager.HasComponent<VillagerAlignment>(entity))
            {
                perceivers.Add(entity);
            }
        }

        return perceivers;
    }

    static EntityRelation FindRelation(
        DynamicBuffer<EntityRelationBuffer> relations,
        Entity target)
    {
        for (int i = 0; i < relations.Length; i++)
        {
            if (relations[i].TargetEntity == target)
            {
                return new EntityRelation
                {
                    TargetEntity = target,
                    RelationValue = relations[i].RelationValue,
                    Category = relations[i].Category
                };
            }
        }

        // No existing relation = neutral
        return new EntityRelation
        {
            TargetEntity = target,
            RelationValue = 0,
            Category = RelationCategory.Neutral
        };
    }

    static ReactionTypeId DetermineReactionType(
        SocialEventTypeId eventType,
        float intensity)
    {
        // Map event type + intensity to reaction emotion

        if (intensity > 0.7f)
        {
            return eventType switch
            {
                SocialEventTypeId.GiftGiven => ReactionTypeId.Gratitude,
                SocialEventTypeId.RaidSuccessful => ReactionTypeId.Admiration,
                SocialEventTypeId.AssistanceProvided => ReactionTypeId.Relief,
                _ => ReactionTypeId.Joy
            };
        }
        else if (intensity < -0.7f)
        {
            return eventType switch
            {
                SocialEventTypeId.InsultDelivered => ReactionTypeId.Anger,
                SocialEventTypeId.TreatyViolated => ReactionTypeId.Disgust,
                SocialEventTypeId.RaidLaunched => ReactionTypeId.Fear,
                _ => ReactionTypeId.Anger
            };
        }
        else if (math.abs(intensity) < 0.2f)
        {
            return ReactionTypeId.Indifference;
        }
        else
        {
            return intensity > 0 ? ReactionTypeId.Respect : ReactionTypeId.Contempt;
        }
    }
}

public struct ProcessedEventTag : IComponentData { }

public struct PendingReactionBuffer : IBufferElementData
{
    public Entity ReactionEntity;
}
```

---

## Relation Change System

```csharp
/// <summary>
/// System that converts reactions into relation changes
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SocialSystemsGroup))]
[UpdateAfter(typeof(GenerateReactionsSystem))]
public partial struct ApplyReactionToRelationsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // For each unprocessed reaction
        foreach (var (reaction, reactionEntity) in
            SystemAPI.Query<RefRW<SocialReaction>>()
                .WithNone<ProcessedEventTag>()
                .WithEntityAccess())
        {
            if (reaction.ValueRO.IsProcessed)
                continue;

            // Find the entity that had this reaction
            Entity reactor = FindReactor(ref state, reactionEntity);
            if (reactor == Entity.Null)
                continue;

            // Get their relation buffer
            var relationBuffer = state.EntityManager.GetBuffer<EntityRelationBuffer>(reactor);

            // Calculate relation change
            int relationDelta = CalculateRelationDelta(reaction.ValueRO);

            // Apply to existing relation or create new one
            bool found = false;
            for (int i = 0; i < relationBuffer.Length; i++)
            {
                if (relationBuffer[i].TargetEntity == reaction.ValueRO.ReactedToEntity)
                {
                    var relation = relationBuffer[i];
                    relation.RelationValue = math.clamp(
                        relation.RelationValue + relationDelta,
                        -100, 100);
                    relation.Category = GetRelationCategory(relation.RelationValue);
                    relationBuffer[i] = relation;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Create new relation
                relationBuffer.Add(new EntityRelationBuffer
                {
                    TargetEntity = reaction.ValueRO.ReactedToEntity,
                    RelationValue = relationDelta,
                    Category = GetRelationCategory(relationDelta)
                });
            }

            // Handle grudges for negative reactions
            if (reaction.ValueRO.Intensity < -0.5f)
            {
                AddGrudge(ref state, ecb, reactor, reaction.ValueRO);
            }

            // Handle debts for positive reactions
            if (reaction.ValueRO.Intensity > 0.7f &&
                reaction.ValueRO.ReactionType == ReactionTypeId.Gratitude)
            {
                AddDebt(ref state, ecb, reactor, reaction.ValueRO);
            }

            // Mark reaction as processed
            reaction.ValueRW.IsProcessed = true;
            ecb.AddComponent<ProcessedEventTag>(reactionEntity);

            // Emit telemetry
            EmitReactionTelemetry(ref state, reactor, reaction.ValueRO, relationDelta);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    static int CalculateRelationDelta(SocialReaction reaction)
    {
        // Base delta from intensity
        int baseDelta = (int)(reaction.Intensity * 20f);  // -20 to +20

        // Scale by reaction type
        float typeMultiplier = reaction.ReactionType switch
        {
            ReactionTypeId.Gratitude => 1.5f,
            ReactionTypeId.Anger => 1.5f,
            ReactionTypeId.Disgust => 2.0f,
            ReactionTypeId.Admiration => 1.3f,
            ReactionTypeId.Fear => 1.2f,
            ReactionTypeId.Indifference => 0.1f,
            _ => 1.0f
        };

        return (int)(baseDelta * typeMultiplier);
    }

    static RelationCategory GetRelationCategory(int value)
    {
        return value switch
        {
            < -50 => RelationCategory.Enemy,
            < -10 => RelationCategory.Hostile,
            < 0 => RelationCategory.Unfriendly,
            < 10 => RelationCategory.Neutral,
            < 50 => RelationCategory.Friendly,
            _ => RelationCategory.Allied
        };
    }

    static void AddGrudge(
        ref SystemState state,
        EntityCommandBuffer ecb,
        Entity reactor,
        SocialReaction reaction)
    {
        if (!state.EntityManager.HasBuffer<EntityGrudge>(reactor))
        {
            ecb.AddBuffer<EntityGrudge>(reactor);
        }

        var grudgeBuffer = state.EntityManager.GetBuffer<EntityGrudge>(reactor);

        // Check if grudge already exists
        for (int i = 0; i < grudgeBuffer.Length; i++)
        {
            if (grudgeBuffer[i].TargetEntity == reaction.ReactedToEntity)
            {
                // Strengthen existing grudge
                var grudge = grudgeBuffer[i];
                grudge.Strength = math.min(grudge.Strength + math.abs(reaction.Intensity), 1f);
                grudgeBuffer[i] = grudge;
                return;
            }
        }

        // Add new grudge
        var socialEvent = state.EntityManager.GetComponentData<SocialEvent>(reaction.EventEntity);
        grudgeBuffer.Add(new EntityGrudge
        {
            TargetEntity = reaction.ReactedToEntity,
            OffenseType = socialEvent.EventType,
            Severity = math.abs(reaction.Intensity),
            FormationTick = reaction.ReactionTick,
            Strength = math.abs(reaction.Intensity)
        });
    }

    static void AddDebt(
        ref SystemState state,
        EntityCommandBuffer ecb,
        Entity reactor,
        SocialReaction reaction)
    {
        if (!state.EntityManager.HasBuffer<EntityDebt>(reactor))
        {
            ecb.AddBuffer<EntityDebt>(reactor);
        }

        var debtBuffer = state.EntityManager.GetBuffer<EntityDebt>(reactor);

        debtBuffer.Add(new EntityDebt
        {
            TargetEntity = reaction.ReactedToEntity,
            Type = DebtType.OweFavor,
            Magnitude = reaction.Intensity,
            IncurredTick = reaction.ReactionTick
        });
    }

    static Entity FindReactor(ref SystemState state, Entity reactionEntity)
    {
        // Find entity that has this reaction in their pending buffer
        foreach (var (buffer, entity) in
            SystemAPI.Query<DynamicBuffer<PendingReactionBuffer>>()
                .WithEntityAccess())
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ReactionEntity == reactionEntity)
                    return entity;
            }
        }
        return Entity.Null;
    }

    static void EmitReactionTelemetry(
        ref SystemState state,
        Entity reactor,
        SocialReaction reaction,
        int relationDelta)
    {
        var telemetryStream = SystemAPI.GetSingleton<TelemetryStream>();

        telemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Social,
            Name = "Reaction_Generated",
            Value = 1
        });

        telemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Social,
            Name = "Relation_Changed",
            Value = relationDelta
        });
    }
}
```

---

## Example Scenarios

### Example 1: Warlike vs Peaceful Villages (Godgame)

```csharp
// SCENARIO: Village A successfully raids Village B

// Create raid event
Entity raidEvent = em.CreateEntity();
em.AddComponentData(raidEvent, new SocialEvent
{
    EventType = SocialEventTypeId.RaidSuccessful,
    Instigator = villageA,
    Target = villageB,
    Magnitude = 0.8f,  // Significant raid
    Location = villageBPosition
});

// VILLAGE C (Warlike neighbor) perceives the event
VillageC.Alignment = {
    MoralAxis = 0.2f,   // Low moral (ruthless)
    OrderAxis = 0.3f,   // Low order (chaotic)
    PurityAxis = 0.5f
};

// Reaction computation:
// baseIntensity = 0.8
// alignmentModifier = lerp(1.5, -1.5, 0.2) = +1.08 (likes violence)
// behaviorModifier = 1.0
// relationModifier = 1.0
// finalIntensity = 0.8 * 1.08 = +0.86

// Result: ADMIRATION (positive reaction to successful aggression)
// Relation change: +17 toward Village A
// New relation: Village C now views Village A more favorably

// VILLAGE D (Peaceful neighbor) perceives the same event
VillageD.Alignment = {
    MoralAxis = 0.9f,   // High moral (compassionate)
    OrderAxis = 0.8f,   // High order (lawful)
    PurityAxis = 0.7f
};

// Reaction computation:
// baseIntensity = 0.8
// alignmentModifier = lerp(1.5, -1.5, 0.9) = -1.32 (hates violence)
// behaviorModifier = 1.0
// relationModifier = 1.0
// finalIntensity = 0.8 * -1.32 = -1.0 (clamped)

// Result: DISGUST (negative reaction to aggression)
// Relation change: -30 toward Village A
// New relation: Village D now views Village A as hostile
// Grudge formed: "RaidSuccessful" offense, severity 1.0
```

**Visual Result:** Same event, opposite reactions. Warlike villages grow closer to aggressive neighbors, peaceful villages shun them. **Creates faction formation naturally.**

---

### Example 2: Warning Shot Reactions (Space4X)

```csharp
// SCENARIO: Player ship fires warning shot at unknown vessel

// Create warning shot event
Entity warningShotEvent = em.CreateEntity();
em.AddComponentData(warningShotEvent, new SocialEvent
{
    EventType = SocialEventTypeId.WarningShot,
    Instigator = playerShip,
    Target = unknownVessel,
    Magnitude = 0.6f,
    Location = unknownVesselPosition
});

// CAPTAIN A (Peaceful merchant)
CaptainA.Behavior = {
    Bold = 0.2f,      // Craven (timid)
    Vengeful = 0.3f   // Forgiving
};

// Reaction computation:
// baseIntensity = 0.6
// behaviorModifier = lerp(-1.5, 1.5, 0.2) = -1.1 (intimidated by aggression)
// finalIntensity = 0.6 * -1.1 = -0.66

// Result: FEAR (intimidated, will comply)
// Relation change: -13 toward player
// Action: Captain transmits compliance, changes course

// CAPTAIN B (Warlike pirate)
CaptainB.Behavior = {
    Bold = 0.9f,      // Bold (aggressive)
    Vengeful = 0.8f   // Vengeful
};

// Reaction computation:
// baseIntensity = 0.6
// behaviorModifier = lerp(-1.5, 1.5, 0.9) = +1.2 (provoked by threat)
// finalIntensity = 0.6 * 1.2 = +0.72

// Result: ANGER (provoked, will retaliate!)
// Relation change: -21 toward player
// Grudge formed: "WarningShot" offense, severity 0.72
// Action: Captain opens fire in response
```

**Tactical Result:** Warning shots are risky. Peaceful targets comply, warlike targets attack. **Player must know their enemy.**

---

### Example 3: Gift Giving (Both Games)

```csharp
// SCENARIO: Entity A gives gift to Entity B

Entity giftEvent = em.CreateEntity();
em.AddComponentData(giftEvent, new SocialEvent
{
    EventType = SocialEventTypeId.GiftGiven,
    Instigator = entityA,
    Target = entityB,
    Magnitude = 0.7f  // Valuable gift
});

// RECIPIENT 1: High moral (appreciates generosity)
Recipient1.Alignment = { MoralAxis = 0.9f };
// finalIntensity = 0.7 * lerp(0.5, 1.5, 0.9) = 0.7 * 1.4 = +0.98
// Reaction: GRATITUDE (very appreciative)
// Relation change: +20
// Debt incurred: OweFavor, magnitude 0.98

// RECIPIENT 2: Low moral (suspicious of motives)
Recipient2.Alignment = { MoralAxis = 0.2f };
// finalIntensity = 0.7 * lerp(0.5, 1.5, 0.2) = 0.7 * 0.7 = +0.49
// Reaction: RESPECT (appreciates, but wary)
// Relation change: +10
// No debt (not appreciative enough)

// RECIPIENT 3: Already friends (high existing relation)
Recipient3.RelationValue = +70;
// relationModifier = 1.0 + (0.7 * 0.5) = 1.35
// finalIntensity = 0.7 * 1.4 * 1.35 = +1.32 (clamped to 1.0)
// Reaction: JOY (friends appreciate gifts more!)
// Relation change: +30
// Debt incurred: OweFavor, magnitude 1.0
```

**Social Result:** Same gift, different appreciation levels. Gifts build stronger bonds with those who value generosity. **Rewarding matching values.**

---

### Example 4: Treaty Violation Reactions (Space4X)

```csharp
// SCENARIO: Faction A violates treaty with Faction B

Entity treatyViolationEvent = em.CreateEntity();
em.AddComponentData(treatyViolationEvent, new SocialEvent
{
    EventType = SocialEventTypeId.TreatyViolated,
    Instigator = factionA,
    Target = factionB,
    Magnitude = 0.9f  // Severe violation
});

// FACTION C (High Order - Values contracts)
FactionC.Alignment = { OrderAxis = 0.9f };
// alignmentModifier = lerp(0.3, 2.0, 0.9) = 1.87 (very offended by broken contracts)
// finalIntensity = 0.9 * 1.87 = -1.68 (clamped to -1.0)
// Reaction: DISGUST
// Relation change: -40 toward Faction A
// Grudge formed: "TreatyViolated", severity 1.0
// Consequence: Faction C ends all treaties with Faction A

// FACTION D (Low Order - Expects betrayal)
FactionD.Alignment = { OrderAxis = 0.1f };
// alignmentModifier = lerp(0.3, 2.0, 0.1) = 0.47 (expects this)
// finalIntensity = 0.9 * 0.47 = -0.42
// Reaction: INDIFFERENCE (shrugs, "What did you expect?")
// Relation change: -5 (minimal impact)
// Consequence: Business as usual
```

**Diplomatic Result:** Treaty violations have **wildly different consequences** depending on who witnesses them. Lawful factions take it seriously, chaotic factions expect it. **Reputation matters.**

---

### Example 5: Villager Thanks (Godgame)

```csharp
// SCENARIO: Villager A thanks Villager B for help

Entity thanksEvent = em.CreateEntity();
em.AddComponentData(thanksEvent, new SocialEvent
{
    EventType = SocialEventTypeId.ThanksSaid,
    Instigator = villagerA,
    Target = villagerB,
    Magnitude = 0.4f  // Simple thanks
});

// RECIPIENT 1: High moral (values gratitude)
RecipientVillager1.Alignment = { MoralAxis = 0.8f };
// finalIntensity = 0.4 * lerp(0.5, 1.5, 0.8) = 0.4 * 1.3 = +0.52
// Reaction: JOY (appreciates acknowledgment)
// Relation change: +10

// RECIPIENT 2: Low moral (doesn't care)
RecipientVillager2.Alignment = { MoralAxis = 0.2f };
// finalIntensity = 0.4 * lerp(0.5, 1.5, 0.2) = 0.4 * 0.7 = +0.28
// Reaction: INDIFFERENCE (whatever)
// Relation change: +2

// VILLAGE (observing from aggregate)
Village.Alignment = { MoralAxis = 0.7f };
// finalIntensity = 0.4 * 1.2 = +0.48
// Reaction: RESPECT (likes polite villagers)
// Relation change: +9
// Consequence: Village looks favorably on both villagers
```

**Social Result:** Simple thanks has **bigger impact on those who value politeness**. Creates natural social hierarchies based on shared values. **Micro-interactions matter.**

---

## Aggregate Reactions

### Villages/Fleets React as Collectives

Aggregates compute reactions based on **member averages**:

```csharp
[BurstCompile]
partial struct AggregateReactionSystem : IJobEntity
{
    void Execute(
        Entity aggregateEntity,
        ref VillagerAlignment aggregateAlignment,  // Computed from members
        in AggregateMembers members)
    {
        // Aggregate alignment is average of members
        // This means aggregate reactions naturally reflect population values

        // Example: Village with 60% warlike, 40% peaceful
        // - Warlike members: MoralAxis = 0.3 avg
        // - Peaceful members: MoralAxis = 0.8 avg
        // - Village aggregate: MoralAxis = 0.3*0.6 + 0.8*0.4 = 0.5
        // Result: Village has moderate reaction to aggression
    }
}
```

**Emergent Behavior:**
- Homogeneous villages have **strong, unified reactions**
- Diverse villages have **moderate, mixed reactions**
- As population shifts, village personality changes
- **Demographics drive diplomacy**

---

## Grudge Decay System

Grudges fade over time based on **Vengeful trait**:

```csharp
[BurstCompile]
public partial struct GrudgeDecaySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        uint currentTick = (uint)state.WorldUnmanaged.Time.ElapsedTime;

        foreach (var (grudgeBuffer, behavior) in
            SystemAPI.Query<DynamicBuffer<EntityGrudge>, RefRO<VillagerBehavior>>())
        {
            for (int i = grudgeBuffer.Length - 1; i >= 0; i--)
            {
                var grudge = grudgeBuffer[i];

                // Calculate decay rate based on Vengeful trait
                // High Vengeful (0.9) = slow decay (0.1% per second)
                // Low Vengeful (0.1) = fast decay (0.9% per second)
                float decayRate = math.lerp(0.009f, 0.001f, behavior.ValueRO.Vengeful);

                // Apply decay
                grudge.Strength -= decayRate * deltaTime;

                // Remove grudge if strength drops below threshold
                if (grudge.Strength <= 0f)
                {
                    grudgeBuffer.RemoveAt(i);
                }
                else
                {
                    grudgeBuffer[i] = grudge;
                }
            }
        }
    }
}
```

**Result:**
- **Vengeful entities** hold grudges for years (realistic vendettas)
- **Forgiving entities** forget quickly (peace is possible)
- **Creates dynamic social landscape** - old wounds heal or fester

---

## Cascading Reactions

Reactions can trigger **chain reactions**:

```csharp
// Example: Insult → Anger → Retaliation → Escalation

// 1. Initial insult
Entity insultEvent = CreateEvent(SocialEventTypeId.InsultDelivered, villagerA, villagerB);
// VillagerB reacts with ANGER (intensity -0.8)

// 2. VillagerB retaliates
Entity retaliationEvent = CreateEvent(SocialEventTypeId.InsultDelivered, villagerB, villagerA);
// VillagerA reacts with ANGER (intensity -0.9, stronger because they're Vengeful)

// 3. VillagerA escalates to violence
Entity attackEvent = CreateEvent(SocialEventTypeId.BattleWon, villagerA, villagerB);
// VillagerB reacts with FEAR (intensity -1.0)
// Village witnesses, reacts with DISGUST toward VillagerA

// 4. Village intervenes
Entity banishmentEvent = CreateEvent(SocialEventTypeId.Banishment, village, villagerA);
// VillagerA reacts with ANGER toward village
// Becomes outcast, forms band with similar outcasts

// Result: Simple insult cascades into faction formation
```

**Emergent Storytelling:** Social dynamics create **natural narratives** without scripting.

---

## Presentation Integration

### Visual Feedback for Reactions

```csharp
// Godgame: Show reaction animations
[BurstCompile]
partial struct VillagerReactionPresentationSystem : IJobEntity
{
    void Execute(
        ref GodgameVillagerPresentation presentation,
        in DynamicBuffer<PendingReactionBuffer> reactions)
    {
        if (reactions.Length == 0)
            return;

        // Get most recent/intense reaction
        var latestReaction = GetMostIntenseReaction(reactions);

        // Set animation based on reaction type
        presentation.EmotionAnimation = latestReaction.ReactionType switch
        {
            ReactionTypeId.Gratitude => VillagerAnimation.ThankYou,
            ReactionTypeId.Anger => VillagerAnimation.Angry,
            ReactionTypeId.Fear => VillagerAnimation.Cowering,
            ReactionTypeId.Joy => VillagerAnimation.Celebrate,
            ReactionTypeId.Disgust => VillagerAnimation.Disgusted,
            _ => VillagerAnimation.Idle
        };

        // Emotion bubble duration
        presentation.EmotionBubbleDuration = math.abs(latestReaction.Intensity) * 3f;

        // Facial expression intensity
        presentation.ExpressionIntensity = math.abs(latestReaction.Intensity);
    }
}

// Space4X: Show diplomatic UI feedback
[BurstCompile]
partial struct DiplomaticReactionUISystem : IJobEntity
{
    void Execute(
        ref Space4XFactionPresentation presentation,
        in DynamicBuffer<PendingReactionBuffer> reactions)
    {
        if (reactions.Length == 0)
            return;

        var latestReaction = GetMostIntenseReaction(reactions);

        // Update diplomatic status indicator
        presentation.DiplomaticStance = latestReaction.ReactionType switch
        {
            ReactionTypeId.Gratitude => DiplomaticStance.Grateful,
            ReactionTypeId.Anger => DiplomaticStance.Hostile,
            ReactionTypeId.Fear => DiplomaticStance.Intimidated,
            ReactionTypeId.Respect => DiplomaticStance.Respectful,
            ReactionTypeId.Contempt => DiplomaticStance.Dismissive,
            _ => DiplomaticStance.Neutral
        };

        // Highlight in UI
        presentation.ShowDiplomaticAlert = math.abs(latestReaction.Intensity) > 0.7f;
        presentation.AlertColor = latestReaction.Intensity > 0
            ? new float4(0, 1, 0, 1)  // Green for positive
            : new float4(1, 0, 0, 1);  // Red for negative
    }
}
```

---

## Integration with Existing Systems

### 1. Alignment System Integration

```csharp
// Reactions modify alignment over time (you become what you do)

[BurstCompile]
partial struct AlignmentShiftFromReactionsSystem : IJobEntity
{
    void Execute(
        ref VillagerAlignment alignment,
        in DynamicBuffer<PendingReactionBuffer> reactions,
        in VillagerBehavior behavior)
    {
        // Repeated reactions gradually shift alignment
        foreach (var reactionRef in reactions)
        {
            var reaction = GetReaction(reactionRef.ReactionEntity);

            // Hostile reactions shift toward low Moral (ruthless)
            if (reaction.ReactionType == ReactionTypeId.Anger ||
                reaction.ReactionType == ReactionTypeId.Disgust)
            {
                alignment.MoralAxis -= 0.0001f;  // Very gradual
            }

            // Grateful reactions shift toward high Moral (compassionate)
            if (reaction.ReactionType == ReactionTypeId.Gratitude ||
                reaction.ReactionType == ReactionTypeId.Joy)
            {
                alignment.MoralAxis += 0.0001f;
            }

            // Clamp to valid range
            alignment.MoralAxis = math.clamp(alignment.MoralAxis, 0f, 1f);
        }
    }
}
```

### 2. Initiative System Integration

```csharp
// Intense reactions trigger autonomous actions

[BurstCompile]
partial struct ReactionTriggeredInitiativeSystem : IJobEntity
{
    void Execute(
        ref VillagerInitiativeState initiative,
        in DynamicBuffer<PendingReactionBuffer> reactions)
    {
        foreach (var reactionRef in reactions)
        {
            var reaction = GetReaction(reactionRef.ReactionEntity);

            // Very intense reactions trigger immediate action
            if (math.abs(reaction.Intensity) > 0.9f)
            {
                // Force initiative to trigger now
                initiative.NextActionTick = 0;

                // Set pending action based on reaction
                initiative.PendingAction = reaction.ReactionType switch
                {
                    ReactionTypeId.Anger => InitiativeAction.Retaliate,
                    ReactionTypeId.Gratitude => InitiativeAction.ReturnFavor,
                    ReactionTypeId.Fear => InitiativeAction.Flee,
                    _ => InitiativeAction.None
                };
            }
        }
    }
}
```

### 3. Combat System Integration

```csharp
// Relations modify combat behavior

[BurstCompile]
partial struct RelationModifiedCombatSystem : IJobEntity
{
    void Execute(
        ref CombatAI combatAI,
        in DynamicBuffer<EntityRelationBuffer> relations,
        in CombatTarget target)
    {
        // Find relation to current target
        int relationValue = 0;
        for (int i = 0; i < relations.Length; i++)
        {
            if (relations[i].TargetEntity == target.TargetEntity)
            {
                relationValue = relations[i].RelationValue;
                break;
            }
        }

        // Modify combat behavior based on relations
        if (relationValue < -70)
        {
            // Sworn enemies: Fight to the death
            combatAI.RetreatThreshold = 0f;
            combatAI.AggressionMultiplier = 2.0f;
        }
        else if (relationValue < -30)
        {
            // Hostile: Standard combat
            combatAI.RetreatThreshold = 0.3f;
            combatAI.AggressionMultiplier = 1.0f;
        }
        else if (relationValue > 50)
        {
            // Allies: Defensive only, try not to kill
            combatAI.RetreatThreshold = 0.7f;
            combatAI.AggressionMultiplier = 0.3f;
            combatAI.UseLethalForce = false;
        }
    }
}
```

### 4. Trade System Integration

```csharp
// Relations modify trade prices

[BurstCompile]
partial struct RelationModifiedTradeSystem : IJobEntity
{
    void Execute(
        ref TradeOffer tradeOffer,
        in DynamicBuffer<EntityRelationBuffer> relations,
        in TradePartner partner)
    {
        // Find relation to trade partner
        int relationValue = 0;
        for (int i = 0; i < relations.Length; i++)
        {
            if (relations[i].TargetEntity == partner.PartnerEntity)
            {
                relationValue = relations[i].RelationValue;
                break;
            }
        }

        // Modify prices based on relations
        float priceMultiplier = 1.0f;

        if (relationValue > 70)
        {
            // Close allies: 20% discount
            priceMultiplier = 0.8f;
        }
        else if (relationValue > 30)
        {
            // Friends: 10% discount
            priceMultiplier = 0.9f;
        }
        else if (relationValue < -30)
        {
            // Hostile: 50% markup
            priceMultiplier = 1.5f;
        }
        else if (relationValue < -70)
        {
            // Enemies: Won't trade at all
            tradeOffer.IsValid = false;
            return;
        }

        tradeOffer.Price *= priceMultiplier;
    }
}
```

---

## Telemetry & Analytics

```csharp
[BurstCompile]
partial struct SocialTelemetrySystem : IJobEntity
{
    public TelemetryStream TelemetryStream;

    void Execute(Entity entity)
    {
        // Count reactions by type
        var reactionCounts = CountReactionsByType();

        foreach (var (type, count) in reactionCounts)
        {
            TelemetryStream.Emit(new TelemetryMetric
            {
                Category = TelemetryCategory.Social,
                Name = $"Reactions_{type}",
                Value = count
            });
        }

        // Count relation categories
        var relationCounts = CountRelationCategories();

        foreach (var (category, count) in relationCounts)
        {
            TelemetryStream.Emit(new TelemetryMetric
            {
                Category = TelemetryCategory.Social,
                Name = $"Relations_{category}",
                Value = count
            });
        }

        // Active grudges
        int activeGrudges = CountActiveGrudges();
        TelemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Social,
            Name = "Active_Grudges",
            Value = activeGrudges
        });

        // Outstanding debts
        int outstandingDebts = CountOutstandingDebts();
        TelemetryStream.Emit(new TelemetryMetric
        {
            Category = TelemetryCategory.Social,
            Name = "Outstanding_Debts",
            Value = outstandingDebts
        });
    }
}
```

---

## Performance Considerations

### Spatial Perception Radius

```csharp
// Only nearby entities perceive events
// Reduces reaction processing from O(n) to O(k) where k << n

const float PERCEPTION_RADIUS = 50f;  // Configurable

// Example: 1000 entities in world
// Event happens
// Without spatial filtering: 1000 reactions processed
// With spatial filtering: ~20 nearby entities react
// 50x reduction!
```

### Reaction Batching

```csharp
// Process reactions in batches, not per-frame

[UpdateInGroup(typeof(SocialSystemsGroup))]
[UpdateInterval(0.5f)]  // Process every 0.5 seconds
public partial struct BatchedReactionProcessingSystem : ISystem
{
    // Reactions accumulate in buffers
    // Processed in batches for efficiency
}
```

### Relation Caching

```csharp
// Cache frequently queried relations

public struct RelationCache : IComponentData
{
    /// <summary>
    /// Cached relation to most frequently interacted entity
    /// </summary>
    public Entity CachedTarget;
    public int CachedRelationValue;

    /// <summary>
    /// Tick when cache was updated
    /// </summary>
    public uint LastCacheTick;
}
```

---

## Summary

The **Reactions & Relations System** creates believable, dynamic social worlds where:

✅ **Same event, different reactions** - Personality drives perception
✅ **Emergent faction formation** - Similar entities naturally cluster
✅ **Dynamic diplomacy** - Relations change based on actions
✅ **Grudges and debts** - Past events have lasting consequences
✅ **Cascading reactions** - Social dynamics create stories
✅ **Aggregate reactions** - Villages/fleets react as collectives
✅ **Burst-compatible** - Parallel processing, deterministic
✅ **Visual feedback** - Reactions visible in presentation layer

**Game Impact:**

**Godgame:**
- Warlike villages form raiding bands
- Peaceful villages shun aggressors
- Villagers hold grudges, seek revenge
- Gift economy builds social bonds
- Divine interventions have lasting social consequences

**Space4X:**
- Warning shots have unpredictable results
- Trade embargoes based on relations
- Diplomatic incidents cascade into wars
- Faction reputations matter
- Captain personalities drive fleet behavior

**Result:** A **living, reactive world** where social dynamics emerge from individual personalities, not scripted events.

---

**Related Documentation:**
- [Entity_Agnostic_Design.md](Entity_Agnostic_Design.md) - Entities and aggregates use same components
- [General_Forces_System.md](General_Forces_System.md) - Physical forces system
- [Multi_Force_Interactions.md](Multi_Force_Interactions.md) - Emergent physical behaviors
- `VillagerAlignment` - Tri-axis alignment system
- `VillagerBehavior` - Personality traits
- `VillagerInitiativeState` - Autonomous action system

---

**Last Updated:** 2025-12-18
**Status:** Design Document - Social Dynamics
**Burst Compatible:** Yes
**Deterministic:** Yes
**Creates Emergent Storytelling:** Yes ✨
