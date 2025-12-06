# Cognitive Learning Framework - Integration Guide

## Overview

The Cognitive Learning Framework implements procedural cognition, emotional modeling, and emergent learning for PureDOTS entities. It integrates with the existing MindECS (DefaultEcs) cognitive layer and Body ECS (Unity Entities) deterministic simulation.

## Architecture

- **Body ECS** (Unity Entities): Stores experience events, emotion modulators, skill profiles (Burst-safe, deterministic)
- **MindECS** (DefaultEcs): Processes learning, updates emotions/skills (managed code, non-deterministic)
- **Sync Bridge**: `AgentSyncBus` communicates between layers (100ms Body→Mind, 250ms Mind→Body)

## Quick Start

### 1. Adding Experience Events

When an entity has an interaction (combat, trade, betrayal, etc.), add an `ExperienceEvent` to its buffer:

```csharp
using PureDOTS.Runtime.Cognitive;
using Unity.Entities;

// In your combat/trade/interaction system
var experienceBuffer = SystemAPI.GetBuffer<ExperienceEvent>(entity);
experienceBuffer.Add(new ExperienceEvent
{
    Type = ExperienceType.Combat,
    Source = attackerEntity,
    Context = locationEntity, // Village, fleet, etc.
    Outcome = 1f, // +1 success, -1 failure, 0 neutral
    CultureId = attackerCultureId,
    Tick = currentTick
});
```

### 2. Reading Emotion Modulators

Emotion modulators affect learning and decision-making. Read them in AI systems:

```csharp
using PureDOTS.Runtime.Cognitive;

// In your AI utility scoring system
if (SystemAPI.HasComponent<EmotionModulator>(entity))
{
    var modulator = SystemAPI.GetComponent<EmotionModulator>(entity);
    
    // Apply learning rate multiplier
    var effectiveLearningRate = baseLearningRate * modulator.LearningRateMultiplier;
    
    // Apply confidence modifier to utility scores
    var adjustedScore = baseScore * (1f + modulator.ConfidenceModifier);
    
    // Apply bias adjustment (culture-specific)
    var cultureBias = baseBias + modulator.BiasAdjustment;
}
```

### 3. Using Skill Profiles

Skill profiles modify action effectiveness. Read them in combat/spell systems:

```csharp
using PureDOTS.Runtime.Cognitive;

if (SystemAPI.HasComponent<SkillProfile>(entity))
{
    var skills = SystemAPI.GetComponent<SkillProfile>(entity);
    
    // Apply casting skill to spell success rate
    var spellSuccessRate = baseSuccessRate * skills.CastingSkill;
    
    // Check dual-hand casting threshold
    var canDualCast = (finesse * skills.DualCastingAptitude) > 0.5f;
    
    // Apply melee skill to damage
    var meleeDamage = baseDamage * (0.5f + skills.MeleeSkill * 0.5f);
    
    // Apply strategic thinking to AI decision quality
    var decisionQuality = baseQuality * skills.StrategicThinking;
}
```

### 4. Culture Beliefs and Reputation

Leaders/captains store beliefs about cultures. Use them for diplomacy and tactics:

```csharp
using PureDOTS.Runtime.Cognitive;

if (SystemAPI.HasBuffer<CultureBelief>(entity))
{
    var beliefs = SystemAPI.GetBuffer<CultureBelief>(entity);
    
    foreach (var belief in beliefs)
    {
        if (belief.CultureId == targetCultureId)
        {
            // Low belief value = low trustworthiness = higher suspicion
            var trustLevel = belief.BeliefValue;
            var confidence = belief.Confidence;
            
            // Adjust diplomacy/tactics based on belief
            if (trustLevel < 0.3f && confidence > 0.7f)
            {
                // High confidence, low trust = aggressive stance
                ApplyAggressiveModifier();
            }
        }
    }
}
```

### 5. Grudges and Prejudice

Entities remember negative experiences. Check grudges to avoid known enemies:

```csharp
using PureDOTS.Runtime.Cognitive;

if (SystemAPI.HasBuffer<GrudgeEntry>(entity))
{
    var grudges = SystemAPI.GetBuffer<GrudgeEntry>(entity);
    
    foreach (var grudge in grudges)
    {
        if (grudge.CultureId == opponentCultureId)
        {
            // High grudge = avoid or attack
            if (grudge.GrudgeValue > 0.7f)
            {
                // Strong grudge: avoid this culture or attack on sight
                AvoidOrAttack(grudge.CultureId);
            }
        }
    }
}
```

## System Integration

### Adding Components to Entities

```csharp
// In authoring or bootstrap system
var entity = ecb.CreateEntity();

// Add experience buffer (auto-created, but can pre-allocate)
ecb.AddBuffer<ExperienceEvent>(entity);

// Add memory profile
ecb.AddComponent(entity, new MemoryProfile
{
    LearningRate = 0.1f, // Scaled by wisdom/intelligence
    Retention = 0.95f, // How long memories last
    Bias = 0f, // Base predisposition
    LastUpdateTick = 0
});

// Add emotion state (updated by MindECS)
ecb.AddComponent(entity, new EmotionState
{
    Anger = 0f,
    Trust = 0.5f,
    Fear = 0f,
    Pride = 0.5f,
    LastUpdateTick = 0
});

// Add emotion modulator (computed from emotion state)
ecb.AddComponent(entity, new EmotionModulator
{
    LearningRateMultiplier = 1f,
    BiasAdjustment = 0f,
    ConfidenceModifier = 0f,
    LastUpdateTick = 0
});

// Add skill profile
ecb.AddComponent(entity, new SkillProfile
{
    CastingSkill = 0.5f,
    DualCastingAptitude = 0.3f,
    MeleeSkill = 0.5f,
    StrategicThinking = 0.5f,
    LastUpdateTick = 0
});

// Add skill learning state
ecb.AddComponent(entity, new SkillLearningState
{
    CastingExperienceCount = 0,
    DualCastingExperienceCount = 0,
    MeleeExperienceCount = 0,
    StrategicExperienceCount = 0,
    LastUpdateTick = 0,
    PlateauThreshold = 0.001f,
    IsPlateaued = false
});

// Add culture beliefs (for leaders/captains)
ecb.AddBuffer<CultureBelief>(entity);

// Add grudge buffer
ecb.AddBuffer<GrudgeEntry>(entity);

// Add prejudice profile (species-specific)
ecb.AddComponent(entity, new PrejudiceProfile
{
    DecayRate = 0.01f, // Dwarves: 0 (never forget)
    ForgivenessFactor = 0.1f,
    NeverForget = false, // Dwarves: true
    LastUpdateTick = 0
});
```

### System Execution Order

The cognitive learning systems run in this order:

1. **Body ECS (Deterministic)**:
   - `ExperienceSyncSystem` - Collects experiences, sends to MindECS (100ms)
   - `GrudgeUpdateSystem` - Updates grudges from experiences
   - `CultureMemoryGraphSystem` - Updates culture profiles
   - `CultureBeliefUpdateSystem` - Updates culture beliefs
   - `FocusLearningSystem` - Applies focus limits to learning

2. **MindECS (Non-Deterministic)**:
   - `ExperienceProcessingSystem` - Processes experiences, updates memory (10s batches)
   - `EmotionalLearningSystem` - Updates emotions from interactions (4 Hz)
   - `SkillLearningSystem` - Updates skills via learning (1 Hz)
   - `PerceptionLearningSystem` - Updates perception via Bayesian learning (2 Hz)
   - `AggregateLearningSystem` - Meta-learning for aggregates (5s)
   - `MemoryCompressionSystem` - Compresses old memories (10s)
   - `LearningValidationSystem` - Detects plateaus, freezes updates (5s)

3. **Body ECS (Deterministic)**:
   - `LearningSyncSystem` - Receives learning updates, applies modifiers (250ms)
   - `AdaptiveBehaviorSystem` - Applies memory-based modifiers to AI (every tick)

## Common Patterns

### Pattern 1: Combat Experience

```csharp
// After combat resolution
void RecordCombatExperience(Entity victor, Entity loser, ushort victorCulture, ushort loserCulture, uint tick)
{
    // Victor's experience
    var victorBuffer = SystemAPI.GetBuffer<ExperienceEvent>(victor);
    victorBuffer.Add(new ExperienceEvent
    {
        Type = ExperienceType.Combat,
        Source = loser,
        Context = Entity.Null,
        Outcome = 1f, // Success
        CultureId = loserCulture,
        Tick = tick
    });
    
    // Loser's experience
    var loserBuffer = SystemAPI.GetBuffer<ExperienceEvent>(loser);
    loserBuffer.Add(new ExperienceEvent
    {
        Type = ExperienceType.Combat,
        Source = victor,
        Context = Entity.Null,
        Outcome = -1f, // Failure
        CultureId = victorCulture,
        Tick = tick
    });
}
```

### Pattern 2: Trade Experience

```csharp
void RecordTradeExperience(Entity trader, Entity partner, bool successful, ushort partnerCulture, uint tick)
{
    var buffer = SystemAPI.GetBuffer<ExperienceEvent>(trader);
    buffer.Add(new ExperienceEvent
    {
        Type = ExperienceType.Trade,
        Source = partner,
        Context = Entity.Null,
        Outcome = successful ? 0.5f : -0.3f, // Positive for success, negative for failure
        CultureId = partnerCulture,
        Tick = tick
    });
}
```

### Pattern 3: Betrayal Experience

```csharp
void RecordBetrayalExperience(Entity victim, Entity betrayer, ushort betrayerCulture, uint tick)
{
    var buffer = SystemAPI.GetBuffer<ExperienceEvent>(victim);
    buffer.Add(new ExperienceEvent
    {
        Type = ExperienceType.Betrayal,
        Source = betrayer,
        Context = Entity.Null,
        Outcome = -1f, // Strong negative
        CultureId = betrayerCulture,
        Tick = tick
    });
    
    // This will create/update a grudge entry
}
```

### Pattern 4: Applying Skill Modifiers

```csharp
float CalculateSpellSuccessRate(Entity caster, float baseSuccessRate)
{
    if (!SystemAPI.HasComponent<SkillProfile>(caster))
    {
        return baseSuccessRate;
    }
    
    var skills = SystemAPI.GetComponent<SkillProfile>(caster);
    
    // Casting skill directly affects success rate
    return baseSuccessRate * (0.5f + skills.CastingSkill * 0.5f);
}

bool CanDualCast(Entity caster, float finesse)
{
    if (!SystemAPI.HasComponent<SkillProfile>(caster))
    {
        return false;
    }
    
    var skills = SystemAPI.GetComponent<SkillProfile>(caster);
    
    // Gate dual-hand casting via Finesse × Aptitude threshold
    return (finesse * skills.DualCastingAptitude) > 0.5f;
}
```

### Pattern 5: Memory-Based Pathfinding

```csharp
float CalculatePathfindingCost(Entity entity, Entity targetLocation, ushort locationCulture)
{
    float baseCost = 1f;
    
    // Check grudges
    if (SystemAPI.HasBuffer<GrudgeEntry>(entity))
    {
        var grudges = SystemAPI.GetBuffer<GrudgeEntry>(entity);
        foreach (var grudge in grudges)
        {
            if (grudge.CultureId == locationCulture && grudge.GrudgeValue > 0.5f)
            {
                // High grudge = avoid this location (increase cost)
                baseCost += grudge.GrudgeValue * 2f;
            }
        }
    }
    
    return baseCost;
}
```

## Performance Considerations

1. **Batch Updates**: Experience processing runs every 10 seconds (batched)
2. **Memory Compression**: Old memories compressed into histograms (keeps last 64 entities)
3. **Plateau Detection**: Frozen skills skip update jobs (saves CPU)
4. **Sync Intervals**: 100ms Body→Mind, 250ms Mind→Body (configurable)
5. **Focus Limits**: Learning drains focus, preventing infinite learning

## Extending the Framework

### Adding New Experience Types

1. Add to `ExperienceType` enum:
```csharp
public enum ExperienceType : ushort
{
    // ... existing types ...
    NewInteractionType = 9
}
```

2. Record experiences with new type in your systems
3. Handle in `ExperienceProcessingSystem` if needed

### Adding New Skills

1. Add to `SkillProfile`:
```csharp
public struct SkillProfile : IComponentData
{
    // ... existing skills ...
    public float NewSkill;
}
```

2. Update in `SkillLearningSystem`
3. Apply modifiers in relevant systems

### Custom Learning Rules

Override learning behavior by:
1. Extending `ExperienceProcessingSystem`
2. Adding custom processing in your systems
3. Modifying `MemoryProfile.LearningRate` per entity

## Troubleshooting

**Q: Experiences not being processed?**
- Check `AgentSyncId.MindEntityIndex >= 0` (entity must be mapped to MindECS)
- Verify `ExperienceSyncSystem` is running (check system order)
- Check sync interval (100ms default)

**Q: Skills not updating?**
- Check `SkillLearningState.IsPlateaued` (may be frozen)
- Verify `FocusState.Current` (learning drains focus)
- Check `MemoryProfile.LearningRate` (may be too low)

**Q: Emotions not affecting behavior?**
- Verify `EmotionModulator` component exists
- Check `LearningSyncSystem` is running (250ms interval)
- Ensure `AdaptiveBehaviorSystem` reads modifiers

## References

- Component Definitions: `Runtime/Runtime/Cognitive/`
- MindECS Systems: `Runtime/AI/MindECS/Systems/`
- Sync Systems: `Runtime/Bridges/`
- Integration Examples: See combat/trade systems for experience recording patterns
- **Cognitive Stats Integration**: See `CognitiveStatsIntegrationGuide.md` for Wisdom/Intelligence stat usage

