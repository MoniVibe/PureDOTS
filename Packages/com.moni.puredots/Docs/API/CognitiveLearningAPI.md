# Cognitive Learning Framework - API Reference

## Components

### ExperienceComponents

#### `ExperienceEvent : IBufferElementData`
Raw experience event stored in buffer.

**Fields:**
- `ExperienceType Type` - Type of experience (Combat, Trade, Betrayal, etc.)
- `Entity Source` - Entity that caused this experience
- `Entity Context` - Context entity (village, fleet, location)
- `float Outcome` - +1 success, -1 failure, 0 neutral
- `ushort CultureId` - Culture/faction ID of the source
- `uint Tick` - Tick when experience occurred

**Usage:**
```csharp
var buffer = SystemAPI.GetBuffer<ExperienceEvent>(entity);
buffer.Add(new ExperienceEvent { /* ... */ });
```

#### `MemoryProfile : IComponentData`
Controls how an entity learns and retains experiences.

**Fields:**
- `float LearningRate` - Learning rate multiplier (scaled by wisdom/intelligence)
- `float Retention` - Retention factor (0-1): how long memories last
- `float Bias` - Base predisposition toward Source type
- `uint LastUpdateTick` - Last tick when memory was updated

**Default Values:**
- `LearningRate`: 0.1f
- `Retention`: 0.95f
- `Bias`: 0f

### EmotionComponents

#### `EmotionState : IComponentData`
Emotional state vector (updated in MindECS, synced to Body ECS).

**Fields:**
- `float Anger` - Anger level (0-1)
- `float Trust` - Trust level (0-1)
- `float Fear` - Fear level (0-1)
- `float Pride` - Pride level (0-1)
- `uint LastUpdateTick` - Last tick when emotions were updated

**Modulation Rules:**
- `LearningRate *= (1 + Pride - Fear)`
- `Bias[culture] += Anger * 0.1f`

#### `EmotionModulator : IComponentData`
Emotion-derived modifiers for learning and decision-making (computed in Body ECS).

**Fields:**
- `float LearningRateMultiplier` - Learning rate multiplier: `1 + Pride - Fear` (clamped 0.1-2.0)
- `float BiasAdjustment` - Bias adjustment per culture: `Anger * 0.1f`
- `float ConfidenceModifier` - Decision confidence modifier (affects utility scoring)
- `uint LastUpdateTick` - Last tick when modulator was computed

### SkillComponents

#### `SkillProfile : IComponentData`
Procedural knowledge as weighted proficiency vectors (normalized 0-1).

**Fields:**
- `float CastingSkill` - Spell casting skill (0-1)
- `float DualCastingAptitude` - Dual-hand casting aptitude (0-1)
- `float MeleeSkill` - Melee combat skill (0-1)
- `float StrategicThinking` - Strategic thinking ability (0-1)
- `uint LastUpdateTick` - Last tick when skills were updated

**Learning Rules:**
- Observational: `ΔSkill = 0.1× * Outcome * LearningRate`
- Practice-based: `ΔSkill *= Stamina` or `Focus` multiplier
- Dual-hand gate: `Finesse × Aptitude > 0.5f`

#### `SkillLearningState : IComponentData`
Tracks learning state and plateau detection.

**Fields:**
- `int CastingExperienceCount` - Experience counter for casting
- `int DualCastingExperienceCount` - Experience counter for dual casting
- `int MeleeExperienceCount` - Experience counter for melee
- `int StrategicExperienceCount` - Experience counter for strategic thinking
- `uint LastUpdateTick` - Last tick when learning state was updated
- `float PlateauThreshold` - Freeze updates when `ΔSkill < ε` (default: 0.001f)
- `bool IsPlateaued` - Flag indicating if skills have plateaued

### CultureComponents

#### `CultureProfile : IComponentData`
Culture profile storing aggregate traits and reputation.

**Fields:**
- `ushort Id` - Culture ID
- `float Aggression` - Aggression level (0-1)
- `float Trustworthiness` - Trustworthiness level (0-1)
- `float MagicStyle` - Magic style preference (0-1)
- `float Reputation` - Global social weight/reputation (0-1)
- `uint LastUpdateTick` - Last tick when profile was updated

#### `CultureBelief : IBufferElementData`
Belief vector per culture (stored on leaders/captains).

**Fields:**
- `ushort CultureId` - Culture ID this belief is about
- `float BeliefValue` - Belief value: `lerp(belief, observedTrait, LearningRate)`
- `float Confidence` - Confidence in this belief (0-1)
- `uint LastUpdateTick` - Last tick when belief was updated

**Update Rule:**
```csharp
belief[culture] = lerp(belief, observedTrait, LearningRate)
```

#### `CultureMemoryGraph : IComponentData` (Singleton)
Shared culture memory graph singleton.

**Fields:**
- `int CultureCount` - Total number of cultures tracked
- `uint LastUpdateTick` - Last tick when graph was updated

### GrudgeComponents

#### `GrudgeEntry : IBufferElementData`
Grudge entry storing negative experiences per culture.

**Fields:**
- `ushort CultureId` - Culture ID this grudge is against
- `float GrudgeValue` - Grudge value (0-1): `1 - exp(-Anger * negativeEvents)`
- `int NegativeEventCount` - Count of negative events
- `uint LastUpdateTick` - Last tick when grudge was updated

**Exponential Weighting:**
```csharp
grudge[culture] = 1 - exp(-Anger * negativeEvents)
```

#### `PrejudiceProfile : IComponentData`
Controls grudge decay and forgiveness.

**Fields:**
- `float DecayRate` - Decay rate per tick (0 = never forget, 1 = instant decay)
- `float ForgivenessFactor` - Forgiveness factor (0-1): reduces grudge over time
- `bool NeverForget` - If true, decay is clamped to 0 (dwarves: true)
- `uint LastUpdateTick` - Last tick when prejudice was updated

**Species-Specific:**
- Dwarves: `DecayRate = 0`, `NeverForget = true`
- Others: Exponential decay with forgiveness factors

## Systems

### Body ECS Systems (Deterministic, Burst-Compiled)

#### `ExperienceSyncSystem`
Collects `ExperienceEvent` buffers and sends to MindECS via `AgentSyncBus`.

**Update Frequency:** 100ms (Body→Mind sync interval)

**Query:** Entities with `ExperienceEvent` buffer and `AgentSyncId`

**Usage:** Automatically runs, no manual interaction needed.

#### `LearningSyncSystem`
Receives learning updates from MindECS and applies modifiers to Body ECS.

**Update Frequency:** 250ms (Mind→Body sync interval)

**Query:** Entities with `AgentSyncId` and learning components (`EmotionModulator`, `SkillProfile`, `CultureBelief`)

**Usage:** Automatically runs, modifiers available after sync.

#### `CultureMemoryGraphSystem`
Maintains culture memory graph singleton and aggregates culture profiles.

**Update Frequency:** Every tick (after `OrgRelationEventImpactSystem`)

**Dependencies:** `OrgRelation`, `OrgPersona`

**Usage:** Automatically maintains `CultureMemoryGraph` singleton.

#### `CultureBeliefUpdateSystem`
Updates culture belief vectors on leaders/captains.

**Update Frequency:** Every tick (after `CultureMemoryGraphSystem`)

**Query:** Entities with `CultureBelief` buffer and `MemoryProfile`

**Usage:** Automatically updates beliefs based on observed culture profiles.

#### `GrudgeUpdateSystem`
Updates grudge entries based on negative experiences.

**Update Frequency:** Every tick

**Query:** Entities with `GrudgeEntry` buffer, `PrejudiceProfile`, and `EmotionState`

**Usage:** Automatically updates grudges from `ExperienceEvent` buffers.

#### `FocusLearningSystem`
Extends Focus system with learning limits.

**Update Frequency:** Every tick (after `FocusUpdateSystem`)

**Query:** Entities with `FocusState`, `SkillLearningState`, and `MemoryProfile`

**Static Helpers:**
- `CalculateEffectiveLearningRate(float baseRate, float focusCurrent, float focusMax)` - Returns learning rate multiplier
- `DrainFocusForLearning(ref FocusState, float cost)` - Drains focus for learning
- `DrainFocusForObservation(ref FocusState, float cost)` - Drains focus for observation
- `DrainFocusForPlanning(ref FocusState, float cost)` - Drains focus for planning

#### `AdaptiveBehaviorSystem`
Applies memory-based modifiers to AI utility scoring.

**Update Frequency:** Every tick (after `AIUtilityScoringSystem`)

**Query:** Entities with `AIUtilityState` and learning components

**Usage:** Automatically applies memory/emotion/grudge modifiers to utility scores.

### MindECS Systems (Non-Deterministic, Managed Code)

#### `ExperienceProcessingSystem`
Processes experiences, applies decay, updates knowledge base weights.

**Update Frequency:** 10 seconds (batched updates)

**Query:** Entities with `CognitiveMemory` and `AgentGuid`

**Processing:**
- Applies decay: `memory.Value *= Retention`
- Updates bias: `bias[culture] += Outcome * LearningRate`
- Compresses old memories into histograms

#### `EmotionalLearningSystem`
Updates emotions from interactions, modulates learning.

**Update Frequency:** 4 Hz (0.25s intervals)

**Query:** Entities with `CognitiveMemory`, `PersonalityProfile`, and `AgentGuid`

**Processing:**
- Updates `EmotionState` from `InteractionDigests`
- Modulates learning: `LearningRate *= (1 + Pride - Fear)`
- Updates bias: `Bias[culture] += Anger * 0.1f`

#### `SkillLearningSystem`
Updates skills via observational and practice-based learning.

**Update Frequency:** 1 Hz (1s intervals)

**Query:** Entities with `CognitiveMemory` and `AgentGuid`

**Processing:**
- Observational: `ΔSkill = 0.1× * Outcome * LearningRate`
- Practice-based: `ΔSkill *= Stamina` or `Focus` multiplier
- Plateau detection: Freezes updates when `ΔSkill < 0.001f`

#### `PerceptionLearningSystem`
Updates perception skills via recursive Bayesian updates.

**Update Frequency:** 2 Hz (0.5s intervals)

**Query:** Entities with `CognitiveMemory` and `AgentGuid`

**Processing:**
- Bayesian update: `posterior = (prior * likelihood) / normalization`
- Skill improves with experience (entities "guess better")

#### `AggregateLearningSystem`
Meta-learning for aggregate entities (fleets, bands, empires).

**Update Frequency:** 5 seconds

**Query:** Entities with `CognitiveMemory` and `AgentGuid` (aggregates only)

**Processing:**
- Maintains statistical models of subordinate performance
- Average success rate vs opponent factions (weighted by recency)
- Shared via "Doctrine Buffs" to subordinates

#### `MemoryCompressionSystem`
Compresses memories into histograms.

**Update Frequency:** 10 seconds (batched)

**Query:** Entities with `CognitiveMemory` and `AgentGuid`

**Processing:**
- Keeps last 64 distinct entities interacted with
- Older memories merge into aggregate "racial" or "factional" impressions
- Histograms stored per culture, per tactic

#### `LearningValidationSystem`
Tracks experience counters, detects plateaus, freezes updates.

**Update Frequency:** 5 seconds

**Query:** Entities with `CognitiveMemory` and `AgentGuid`

**Processing:**
- Tracks experience counters per skill/memory
- When stable (`ΔSkill < 0.001f`), freezes updates
- Models skill plateauing and reduces CPU load

## Enums

### `ExperienceType : ushort`
Experience event types.

**Values:**
- `None = 0`
- `Combat = 1`
- `Trade = 2`
- `Betrayal = 3`
- `Miracle = 4`
- `Spell = 5`
- `Ambush = 6`
- `Help = 7`
- `Social = 8`
- `Custom0 = 240` (for extensions)

## Integration Points

### Recording Experiences

```csharp
// In any system that handles interactions
var buffer = SystemAPI.GetBuffer<ExperienceEvent>(entity);
buffer.Add(new ExperienceEvent
{
    Type = ExperienceType.Combat,
    Source = sourceEntity,
    Context = contextEntity,
    Outcome = outcome, // -1 to +1
    CultureId = sourceCultureId,
    Tick = currentTick
});
```

### Reading Modifiers

```csharp
// Emotion modifiers
if (SystemAPI.HasComponent<EmotionModulator>(entity))
{
    var modulator = SystemAPI.GetComponent<EmotionModulator>(entity);
    var effectiveRate = baseRate * modulator.LearningRateMultiplier;
    var adjustedScore = baseScore * (1f + modulator.ConfidenceModifier);
}

// Skill modifiers
if (SystemAPI.HasComponent<SkillProfile>(entity))
{
    var skills = SystemAPI.GetComponent<SkillProfile>(entity);
    var successRate = baseRate * skills.CastingSkill;
    var canDualCast = (finesse * skills.DualCastingAptitude) > 0.5f;
}
```

### Checking Grudges

```csharp
if (SystemAPI.HasBuffer<GrudgeEntry>(entity))
{
    var grudges = SystemAPI.GetBuffer<GrudgeEntry>(entity);
    foreach (var grudge in grudges)
    {
        if (grudge.CultureId == targetCulture && grudge.GrudgeValue > 0.7f)
        {
            // Strong grudge: avoid or attack
        }
    }
}
```

## Performance Notes

- **Batch Updates**: Experience processing runs every 10 seconds
- **Memory Compression**: Keeps last 64 entities, compresses older memories
- **Plateau Detection**: Frozen skills skip update jobs
- **Sync Intervals**: 100ms Body→Mind, 250ms Mind→Body
- **Focus Limits**: Learning drains focus, preventing infinite learning

## See Also

- [Integration Guide](CognitiveLearningFramework.md) - Usage examples and patterns
- Component Definitions: `Runtime/Runtime/Cognitive/`
- System Implementations: `Runtime/AI/MindECS/Systems/` and `Runtime/Systems/`

