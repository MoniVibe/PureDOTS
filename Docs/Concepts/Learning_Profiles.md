# Learning Profiles and Adaptive Behavior (Game-Agnostic)

**Status**: Concept (captured from interrupted session)  
**Last Updated**: 2025-12-05  
**Audience**: Systems design (PureDOTS), game teams wiring stimuli and behaviors  
**Scope**: DOTS 1.4, Burst-safe, Unity Input System, C#9, deterministic sim

## Core Principles

- Same stimulus, inverted reactions: Profile axes drive opposite responses (evil/chaotic may enjoy screams, pure/good are horrified). Preferences are per-axis, not absolute.
- Profiles are continuous: Moral (evil↔good), Order (chaotic↔lawful), Purity (corrupt↔pure), Might/Magic, plus behavior levers (warlike/peaceful, vengeful/forgiving, materialist/spiritualist). Use Order for cooperative vs competitive to avoid lever bloat.
- Learning over lifespan: Entities start blank; through experiences and teaching they learn patterns, form habits, optimize strategies, and update beliefs.
- Game-agnostic mapping: Games decide `StressorTypeId` → preference bindings (screams, torture audio, laughter, birds, gravitational stress, prayer, rebellion, etc.).

## Profile-Driven Perception (Stimuli ↔ Preference)

- Inputs: `StressorTypeId` (game-defined stimulus), base stress value.
- Modifiers: Per-profile preference values (e.g., DeathStimuliPreference, OrderlinessPreference) derived from alignment/behavior levers.
- Example (screams):
  - Evil chaotic: preference clamps to strong negative → stress relief, seeks suffering.
  - Pure good peaceful: preference clamps positive → high stress, possible break.
  - Lawful evil nuance: enjoys “justified” suffering (legal execution) but dislikes chaotic murder (orderliness preference dominates).

## Learning and Knowledge Model (DOTS-Safe Sketch)

These components are game-agnostic containers; games define enums/IDs and meaning. Keep fields blittable; no managed references.

```csharp
// Procedural knowledge flags and counters (habits/strategies, not XP levels)
public struct ProceduralKnowledge : IComponentData
{
    public byte ConstructionHaulingOptimized; // 0-255 habit strength
    public byte CooperationStrategyLevel;     // learned cooperation/defection patterns
    public byte SocialTimingMastery;          // uses mood/morale windows
    public byte DealMakingOptimization;       // negotiation timing/targets
    public uint LifetimeExperiencePoints;     // generic lifetime XP
    public uint AgeTicks;                     // age in ticks for learning curves
}

[InternalBufferCapacity(16)]
public struct LearnedKnowledge : IBufferElementData
{
    public short KnowledgeTypeId;   // game-defined (language, culture norm, resource, tactic)
    public byte Confidence;         // 0-255
    public uint LearnedTick;
    public uint LastReinforcedTick;
    public Entity AssociatedEntity; // teacher/source/owner
    public uint EncodedData;        // packed payload (game-defined)
}

public struct HabitFormation : IComponentData
{
    public short PatternId;         // game-defined behavior pattern
    public ushort RepetitionCount;  // attempts
    public byte SuccessRate;        // 0-255
    public uint HabitFormedTick;    // when it became automatic
    public byte IsHabit;            // 1 = automatic behavior
}

[InternalBufferCapacity(4)]
public struct LanguageProficiency : IBufferElementData
{
    public short LanguageId;        // culture ID
    public byte ProficiencyLevel;   // 0 none, 128 conversational, 255 native
    public byte LearningProgress;   // incremental progress
    public byte LearningMotivation; // xenophilic/xenophobic influence, opportunity bonus
    public uint LearningStartTick;
}

[InternalBufferCapacity(8)]
public struct SocialObservation : IBufferElementData
{
    public Entity TargetEntity;
    public byte ObservationType;    // e.g., MoodCorrelation, MoraleImpact, StressResponse
    public byte DetectedPattern;    // game-defined encoded pattern
    public byte PatternConfidence;  // 0-255
    public byte SampleSize;         // observation count
    public uint LastObservedTick;
}

public struct MetaLearning : IComponentData
{
    public byte StressComfortUnderstanding;   // knows stress/comfort causality
    public byte MoodMechanicsUnderstanding;   // when to approach
    public byte ReciprocityUnderstanding;     // help↔gratitude patterns (lawful excels)
    public byte CollectiveActionUnderstanding;// shared stressor → guild formation
    public uint LastMetaLearningTick;
}
```

### Habit and Pattern Formation

- Wisdom gates how fast patterns become habits; high wisdom drops repetition thresholds.
- Chaotic vs lawful shapes cooperation: lawful learns “ask for help, honor reciprocity”; chaotic learns “defect when convenient, exploit trust.”
- Intelligence accelerates pattern detection (meta-learning from observations).

### Memory/Lessons Integration

1) Observation → `SocialObservation` / `LearnedKnowledge` entry.  
2) Repetition + success → `HabitFormation` flips to habit (flag in `ProceduralKnowledge`).  
3) Lessons feed back into preferences and behaviors (e.g., construction sounds become comforting to materialists, stressful to egalitarians).

## Behavioral Outcomes and Examples (Outstanding Concepts)

- Risky paths: Wise/chaotic/brave entities may ignore roads to save time despite danger; dumb/craven stick to lit roads, avoid night travel.
- Fleet prep: Captains/sensor/weapons officers learn a culture favors stealth/cloaks/ECW → focus checks increase, retrofit fleets in that territory or on hostile intel.
- Damage typing: Band leaders learn a race is weak to a damage type → stock that damage, recruit accordingly before tasks/goals involving them.
- Counterplay: Armies learn enemy ambush/tactics and adapt counters; guilds track allies’ and rivals’ deals and react (ally, hedge, sabotage, exploit).
- Social revelation: Monogamous entity learns mate cheats; discovers family substance abuse or illegitimacy → reacts per culture levers (purity, order, morality).
- Animal opportunism: Animals learn an unsecured storehouse/food pile or undefended colony and exploit it until countered.
- Sensory inversion (baseline example): Screams relieve stress for evil/chaotic; create extreme stress for pure/good peaceful. Lawful evil only relieved when suffering is “justified.”
- Language/opportunity: Xenophilic trader learns rich culture language for access; xenophobic refuses or learns slowly, preferring intermediaries.
- Social timing: Diplomat learns morale/stress windows before deals; optimizes approach timing, raising success rates.

## Processing and Performance Notes

- Sparse updates: Only process learning when novel stimuli or attempts occur; skip idle ticks.
- LOD: VIP entities learn continuously; background entities learn slowly or in batches.
- Buffer hygiene: Cap `LearnedKnowledge`/`SocialObservation`, decay low-confidence entries; habits cache behaviors as byte flags to avoid per-tick churn.
- Determinism: Use fixed tick time and seeded RNG; avoid managed allocations/strings in Burst paths; keep all structs blittable.

## Open Items / Design Hooks

- Define game-specific IDs: `StressorTypeId`, `KnowledgeTypeId`, `PatternId`, `ObservationType`, `LanguageId` per game domain.
- Map profile levers to preference weights and clamp policy (e.g., -128..+127) in a shared conversion table.
- Decide habit thresholds per attribute (Wisdom reduces repetitions; Intelligence/Order modifies confidence gain/decay).
- Author tests: episodic → semantic → habit pipelines; verify inverse reactions for opposite profiles.

## Log

- 2025-12-05: Captured interrupted conceptualization transcript — profile-based stimuli inversion, lifespan learning/knowledge model, cooperation vs competition via Order lever, and outstanding scenario examples (roads risk, stealthy cultures, damage prep, ambush adaptation, guild deal awareness, betrayal/vice discovery, animal opportunism).
