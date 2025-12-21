# Ritual Magic System

## Overview

Universal ritual system for sustained coordinated actions - not limited to magic users. Warriors chant war hymns, bards sing epics, captains deliver rallying speeches, mages cast sustained spells, and ships coordinate siege beams. Rituals progress through phases with varying intensity, each phase requiring checks that can pass, fail, or critically succeed/fail. Completing all phases grants bonuses. Failed phases may switch rituals to closure/mending alternatives. Rituals can be chained together and customized.

**Key Principles**:
- **Universal system**: Not just magic - war chants, epic songs, rallying speeches, work songs, ship formations
- **Phase-based progression**: Rituals split into 2-8 phases with varying intensity curves
- **Phase checks**: Each phase can fail, pass, or critically succeed/fail
- **Phase retention**: Bonuses from earlier phases carry forward with diminishing returns
- **Completion bonuses**: Successfully finishing all phases grants powerful rewards
- **Ritual switching**: Failed phases can trigger alternative closure/mending rituals
- **Ritual chaining**: String multiple rituals together for combo effects
- **Ritual crafting**: Customize phases, intensity curves, and outcomes
- **Focus-fueled**: Rituals require baseline focus to maintain, more focus = more power
- **Concentration checks**: Damage, stuns, silences, knockbacks test concentration
- **Alignment consequences**: Chaotic rituals backlash harder than lawful when interrupted
- **Cooperative**: Multiple participants amplify power (uses cooperation system)
- **Cross-game**: War chants (Godgame), psionic rituals (Space4X), ship formations (both)
- **Deterministic**: Same focus + same checks = same outcome

---

## Core Ritual Components

### Ritual Definition

```csharp
public struct Ritual : IComponentData
{
    public Entity PrimaryCaster;
    public FixedString64Bytes RitualName;
    public RitualType Type;
    public RitualAlignment Alignment;
    public RitualPhase CurrentPhase;

    // Focus and intensity
    public float BaselineFocusRequired;     // Minimum focus to maintain
    public float CurrentFocusInvestment;    // Total focus being channeled
    public float RitualIntensity;           // 0.0 to 10.0+ (scales with focus)

    // Concentration
    public float ConcentrationLevel;        // 0.0 to 1.0 (current concentration)
    public float ConcentrationDrain;        // Per second drain
    public float ConcentrationThreshold;    // Below this, ritual fails

    // Duration and area
    public float Duration;                  // How long ritual has been active
    public float MaxDuration;               // Optional time limit
    public float EffectRadius;              // Area of effect
    public float3 CenterPosition;           // Ritual location

    // State
    public bool IsActive;
    public bool IsBeingDisrupted;
    public uint StartedTick;
}

public enum RitualType : byte
{
    // Protective rituals
    Barrier = 0,            // Energy barrier blocking passage/damage
    Shield = 1,             // Damage absorption field
    Ward = 2,               // Protection from specific damage type
    Sanctuary = 3,          // Area denies hostile magic

    // Offensive rituals
    Blizzard = 10,          // Freezing storm dealing cold damage
    LightningStorm = 11,    // Electric storm with random strikes
    Flamestorm = 12,        // Fire vortex burning area
    AcidRain = 13,          // Corrosive precipitation
    VoidRift = 14,          // Tears reality, chaotic damage

    // Support rituals
    HealingRain = 20,       // Area healing over time
    BlessingAura = 21,      // Stat buffs in area
    ManaWell = 22,          // Regenerates mana for allies
    HasteDomain = 23,       // Speed boost area
    FortificationField = 24, // Defense boost area

    // Control rituals
    SlowField = 30,         // Movement speed reduction
    SilenceZone = 31,       // Prevents spellcasting
    GravityWell = 32,       // Pulls entities toward center
    ChaosDomain = 33,       // Random effects in area
    TimeDistortion = 34,    // Slows/accelerates time

    // Summoning rituals
    SummonElemental = 40,   // Calls elemental creature
    GateOpening = 41,       // Opens portal
    SpiritBinding = 42,     // Binds spirits to area

    // Space4X ship rituals
    SiegeBeam = 50,         // Focused energy beam between ships
    RepairCloud = 51,       // Nanite repair field
    JamField = 52,          // Electronic/psionic jamming
    ShieldLink = 53,        // Linked shields between ships
    PowerTransfer = 54,     // Transfer energy between ships
    SensorWeb = 55,         // Shared sensor network

    // Non-magic rituals (warriors, bards, captains)
    WarChant = 60,          // Warriors chanting for combat bonuses
    BattleHymn = 61,        // Religious war song for morale
    EpicSong = 62,          // Bard's epic tale (buffs, morale, inspiration)
    RallySpeech = 63,       // Captain's rousing speech
    WorkSong = 64,          // Laborers singing to increase productivity
    MourningSong = 65,      // Funeral dirge (debuffs enemies, buffs allies)
    VictorySong = 66,       // Post-battle celebration (massive morale)
    MarchCadence = 67,      // Marching rhythm (movement speed, cohesion)
    ShantySong = 68         // Sailors' work song (ship speed, crew morale)
}

public enum RitualAlignment : byte
{
    Lawful = 0,         // Ordered, predictable, safe interruption
    Neutral = 1,        // Balanced
    Chaotic = 2         // Unstable, dangerous interruption
}

public enum RitualPhase : byte
{
    Preparation = 0,    // Setting up ritual
    Invocation = 1,     // Initial casting
    Channeling = 2,     // Maintaining effect
    Intensifying = 3,   // Increasing power
    Stable = 4,         // Sustained at constant level
    Degrading = 5,      // Losing power
    Interrupted = 6,    // Disrupted
    Backlash = 7,       // Dangerous failure
    Completion = 8      // Clean shutdown
}
```

---

## Phase-Based Ritual System

Rituals progress through multiple phases, each with varying intensity and requiring phase checks.

### Phase Structure

```csharp
public struct RitualPhaseProgression : IComponentData
{
    public uint TotalPhases;                // 2 to 8 phases
    public uint CurrentPhaseIndex;          // Which phase (0-based)
    public float PhaseProgress;             // 0.0 to 1.0 within current phase
    public float TotalProgress;             // Overall ritual progress
    public bool AllPhasesCompleted;         // Successfully finished all phases
}

[InternalBufferCapacity(8)]
public struct RitualPhaseDefinition : IBufferElementData
{
    public uint PhaseIndex;
    public FixedString64Bytes PhaseName;    // "Invocation", "Climax", "Resolution"
    public float PhaseDuration;             // Seconds to complete phase
    public float PhaseIntensityMultiplier;  // Intensity during this phase
    public float FocusCostMultiplier;       // Focus drain during this phase
    public float EffectMagnitude;           // Effect strength during phase

    // Phase requirements
    public float RequiredConcentration;     // Min concentration to enter phase
    public float RequiredSkill;             // Min skill to attempt phase
    public bool RequiresVerbal;             // Must speak/chant
    public bool RequiresSomatic;            // Must perform gestures

    // Phase check difficulty
    public PhaseCheckDifficulty Difficulty;
    public float PassThreshold;             // Skill check to pass (0.0 to 1.0)
    public float CriticalThreshold;         // Skill check to crit (0.9+ typically)
}

public enum PhaseCheckDifficulty : byte
{
    Trivial = 0,        // 0.1 threshold
    Easy = 1,           // 0.3 threshold
    Moderate = 2,       // 0.5 threshold
    Hard = 3,           // 0.7 threshold
    VeryHard = 4,       // 0.85 threshold
    NearImpossible = 5  // 0.95 threshold
}

public struct PhaseIntensityCurve : IComponentData
{
    public IntensityCurveType CurveType;
    public float[] PhaseIntensities;        // Intensity multiplier per phase

    // Common curve patterns
    public bool UsePresetCurve;
    public PresetCurve PresetType;
}

public enum PresetCurve : byte
{
    Flat = 0,               // All phases equal intensity (1.0, 1.0, 1.0, 1.0)
    BuildingCrescendo = 1,  // Increasing (0.5, 0.7, 0.9, 1.2)
    PeakMiddle = 2,         // Peak in middle (0.6, 1.5, 1.5, 0.6)
    FrontLoaded = 3,        // Strong start (1.5, 1.0, 0.7, 0.5)
    FinaleExplosion = 4,    // Massive finale (0.5, 0.6, 0.8, 2.0)
    WavePattern = 5,        // Oscillating (1.0, 0.5, 1.5, 0.7, 1.2)
    Chaotic = 6             // Random per execution
}

// Example: Epic Song with 4 phases
// Phase 1 "Opening Verse": 10 sec, 0.6× intensity
// Phase 2 "Rising Action": 15 sec, 1.0× intensity
// Phase 3 "Climax": 20 sec, 1.8× intensity
// Phase 4 "Resolution": 10 sec, 0.8× intensity
```

### Phase Checks

Each phase transition requires a skill check with pass/fail/critical outcomes:

```csharp
public struct PhaseCheck : IComponentData
{
    public Entity Performer;
    public uint PhaseIndex;
    public float SkillRoll;                 // 0.0 to 1.0 random roll
    public float SkillModifier;             // From performer's skill
    public float ConcentrationModifier;     // From current concentration
    public float CooperationBonus;          // From group cohesion
    public float TotalSkillValue;           // Combined check value

    public PhaseCheckResult Result;
    public float ResultMagnitude;           // How well/poorly it went
}

public enum PhaseCheckResult : byte
{
    CriticalFailure = 0,    // Catastrophic (roll ≤ 0.05)
    Failure = 1,            // Failed (roll < threshold)
    Pass = 2,               // Succeeded (roll ≥ threshold)
    CriticalSuccess = 3     // Exceptional (roll ≥ critical threshold)
}

[BurstCompile]
public partial struct PhaseCheckSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (check, phaseProgression, phaseDef) in SystemAPI.Query<
            RefRW<PhaseCheck>,
            RefRO<RitualPhaseProgression>,
            DynamicBuffer<RitualPhaseDefinition>>())
        {
            var currentPhase = phaseDef[(int)phaseProgression.ValueRO.CurrentPhaseIndex];

            // Generate random roll
            check.ValueRW.SkillRoll = UnityEngine.Random.value;

            // Calculate total skill value
            check.ValueRW.TotalSkillValue =
                (check.ValueRO.SkillModifier * 0.5f) +              // 50% from skill
                (check.ValueRO.ConcentrationModifier * 0.3f) +      // 30% from concentration
                (check.ValueRO.CooperationBonus * 0.2f);            // 20% from group

            // Determine result
            if (check.ValueRO.SkillRoll <= 0.05f)
            {
                // Critical failure (5% chance regardless of skill)
                check.ValueRW.Result = PhaseCheckResult.CriticalFailure;
                check.ValueRW.ResultMagnitude = 0f;
            }
            else if (check.ValueRO.TotalSkillValue >= currentPhase.CriticalThreshold &&
                     check.ValueRO.SkillRoll >= 0.9f)
            {
                // Critical success (need high skill AND good roll)
                check.ValueRW.Result = PhaseCheckResult.CriticalSuccess;
                check.ValueRW.ResultMagnitude = 1.5f; // 150% effectiveness
            }
            else if (check.ValueRO.TotalSkillValue >= currentPhase.PassThreshold)
            {
                // Regular pass
                check.ValueRW.Result = PhaseCheckResult.Pass;
                check.ValueRW.ResultMagnitude = 1.0f; // 100% effectiveness
            }
            else
            {
                // Regular failure
                check.ValueRW.Result = PhaseCheckResult.Failure;
                check.ValueRW.ResultMagnitude = 0.5f; // 50% effectiveness (partial)
            }

            // Apply result
            ApplyPhaseCheckResult(check.ValueRO, phaseProgression.ValueRO, currentPhase);
        }
    }
}
```

### Phase Check Consequences

```csharp
public struct PhaseCheckConsequences
{
    // Critical failure consequences
    public static void OnCriticalFailure(in PhaseCheck check, ref Ritual ritual)
    {
        // Ritual usually ends with backlash
        ritual.CurrentPhase = RitualPhase.Backlash;

        // Possible outcomes:
        // - War chant: Morale penalty instead of bonus (-20% morale for 60 sec)
        // - Epic song: Audience boos, reputation loss
        // - Magic ritual: Backlash damage (alignment-dependent)
        // - Rally speech: Troops lose confidence in leader

        // Some rituals switch to closure rituals
        if (CanSwitchToClosureRitual(ritual))
        {
            SwitchToClosureRitual(ref ritual);
        }
    }

    // Failure consequences
    public static void OnFailure(in PhaseCheck check, ref Ritual ritual)
    {
        // Ritual ends but not catastrophically
        ritual.CurrentPhase = RitualPhase.Interrupted;

        // Partial benefits retained
        // - Bonuses from completed phases persist at 50% strength
        // - No backlash (clean shutdown)
        // - Can attempt again after cooldown
    }

    // Pass consequences
    public static void OnPass(in PhaseCheck check, ref RitualPhaseProgression progression)
    {
        // Advance to next phase
        progression.CurrentPhaseIndex++;
        progression.PhaseProgress = 0f;

        // Bonuses from this phase carry forward
        // (see Phase Retention section)
    }

    // Critical success consequences
    public static void OnCriticalSuccess(in PhaseCheck check, ref Ritual ritual)
    {
        // Exceptional performance
        // - 150% effectiveness for this phase
        // - Bonus retention increased to 80% (vs 60% normal)
        // - Reduced focus cost for next phase (-20%)
        // - Increased concentration regeneration (+50%)
        // - Audience/participants inspired (+morale)
    }
}
```

### Phase Retention

Bonuses from earlier phases carry forward with diminishing returns:

```csharp
public struct PhaseRetention : IComponentData
{
    public float RetentionRate;             // How much carries forward (0.4 to 0.8)
    public float AccumulatedBonus;          // Sum of retained bonuses
    public uint PhasesCompleted;            // Number of successful phases
}

[InternalBufferCapacity(8)]
public struct RetainedPhaseBonus : IBufferElementData
{
    public uint SourcePhaseIndex;
    public float OriginalMagnitude;
    public float RetainedMagnitude;         // Decays over time/phases
    public BonusType Type;
}

public enum BonusType : byte
{
    AttackBonus = 0,
    DefenseBonus = 1,
    MoraleBonus = 2,
    SpeedBonus = 3,
    DamageBonus = 4,
    HealingBonus = 5,
    SkillBonus = 6
}

// Example: Epic Song (4 phases)
// Phase 1 completed: +10% morale
// Phase 2 completed: +15% attack
//   - Phase 1 bonus retained at 60%: +6% morale
//   - Current: +15% attack, +6% morale
// Phase 3 completed: +25% damage
//   - Phase 1 bonus retained at 36% (0.6²): +3.6% morale
//   - Phase 2 bonus retained at 60%: +9% attack
//   - Current: +25% damage, +9% attack, +3.6% morale
// Phase 4 completed (ALL PHASES BONUS):
//   - All bonuses locked at current retention
//   - Completion bonus added: +50% duration on all bonuses
```

### Completion Bonuses

Successfully completing all phases grants powerful rewards:

```csharp
public struct RitualCompletionBonus : IComponentData
{
    public CompletionBonusType Type;
    public float BonusMagnitude;
    public float BonusDuration;
    public bool IsPermanent;                // Some bonuses last forever
}

public enum CompletionBonusType : byte
{
    // Magnitude bonuses
    DoubleEffectiveness = 0,    // 2× all effects
    ExtendedDuration = 1,       // +100% duration
    EnhancedRetention = 2,      // Bonuses don't decay

    // Unique unlocks
    TemporaryImmunity = 10,     // Immune to fear, stuns (war chant)
    InspiredState = 11,         // +50% all stats (epic song)
    UnbreakableMorale = 12,     // Cannot rout (battle hymn)
    MassResurrection = 13,      // Revive dead allies (healing ritual)

    // Meta bonuses
    ReducedCooldown = 20,       // -50% cooldown on next ritual
    FreeRecast = 21,            // Can immediately cast again
    ChainBonus = 22,            // Next ritual in chain gets +50% power
    PermanentBuff = 23          // Some effect becomes permanent
}

// Example completion bonuses:
// War Chant (all 3 phases): Unbreakable morale + immune to fear for 120 sec
// Epic Song (all 4 phases): +50% all stats + inspired state for 180 sec
// Healing Rain (all 5 phases): Mass resurrection (revive all dead within 50m)
// Void Rift (all 6 phases): Reality tear becomes permanent portal
```

---

## Ritual Switching and Chaining

### Ritual Switching

Failed phases can trigger alternative closure/mending rituals:

```csharp
public struct RitualSwitchConfig : IComponentData
{
    public bool AllowSwitching;
    public FixedString64Bytes ClosureRitualName;    // Safe shutdown ritual
    public FixedString64Bytes MendingRitualName;    // Recovery ritual
    public float SwitchPenalty;                     // Cost to switch (focus)
}

public struct RitualSwitch : IComponentData
{
    public Entity OriginalRitual;
    public Entity NewRitual;
    public SwitchReason Reason;
    public float TransitionTime;            // Seconds to complete switch
    public bool IsTransitioning;
}

public enum SwitchReason : byte
{
    PhaseFailure = 0,           // Failed phase check
    CriticalFailure = 1,        // Critical fumble
    InterruptionAvoidance = 2,  // Switching to prevent backlash
    TacticalDecision = 3        // Performer chose to switch
}

// Example switches:
// Void Rift (failed Phase 3) → Reality Mending (closure)
// Battle Hymn (critical fail) → Calming Chant (mending)
// Epic Song (interrupted) → Bardic Apology (closure, humorous)
// Summoning (failed) → Banishment Chant (mending, sends back)
```

### Ritual Chaining

String multiple rituals together for combo effects:

```csharp
public struct RitualChain : IComponentData
{
    public FixedString64Bytes ChainName;
    public uint TotalRitualsInChain;
    public uint CurrentRitualIndex;
    public bool ChainActive;
    public float ChainBonus;                // Accumulates with each ritual
}

[InternalBufferCapacity(8)]
public struct ChainedRitual : IBufferElementData
{
    public RitualType Type;
    public uint Position;                   // Order in chain
    public float ChainBonusMultiplier;      // Bonus if chained
    public bool MustCompleteAllPhases;      // Required for chain?
}

public struct ChainCompletionBonus : IComponentData
{
    public float BonusMagnitude;            // +100% per ritual in chain
    public float BonusDuration;
    public ChainSynergyType Synergy;
}

public enum ChainSynergyType : byte
{
    Additive = 0,       // Effects stack additively
    Multiplicative = 1, // Effects multiply together
    Transformative = 2, // New unique effect created
    Resonant = 3        // All rituals resonate (special outcome)
}

// Example chains:
// War Chant → Battle Hymn → Victory Song
//   - Each ritual adds +30% morale
//   - Completing chain: +150% morale total (multiplicative)
//   - Synergy: Unstoppable Fervor (cannot be demoralized for 300 sec)

// Barrier → Shield → Ward → Sanctuary
//   - Layered defenses
//   - Completing chain: Impenetrable Fortress (blocks all damage types)

// Blizzard → Lightning Storm → Flamestorm
//   - Elemental chaos
//   - Synergy: Elemental Apocalypse (all elements simultaneously)
```

### Ritual Crafting

Customize rituals by defining phases, intensity, and outcomes:

```csharp
public struct CraftedRitual : IComponentData
{
    public FixedString64Bytes CustomName;
    public Entity Crafter;                  // Who designed this ritual
    public uint PhaseCount;                 // 2 to 8 phases
    public IntensityCurveType IntensityCurve;
    public RitualAlignment Alignment;
    public float CraftingQuality;           // 0.0 to 1.0 (affects reliability)
}

[InternalBufferCapacity(8)]
public struct CraftedPhase : IBufferElementData
{
    public FixedString64Bytes PhaseName;
    public float Duration;
    public float IntensityMultiplier;
    public PhaseCheckDifficulty Difficulty;

    // Effect selection
    public EffectType Effect;
    public float EffectMagnitude;
}

public struct RitualCraftingSystem : IComponentData
{
    public float CrafterSkill;              // Ritual design skill
    public uint RitualsDesigned;            // Experience count
    public bool CanCraftChains;             // Unlock: chain rituals
    public bool CanCustomizeIntensity;      // Unlock: custom curves
}

// Example crafted ritual: "Storm of Blades" (warrior ritual)
// Phase 1 "Oath of Steel": 5 sec, 0.5× intensity, +10% attack
// Phase 2 "Dance of Death": 10 sec, 1.2× intensity, +30% attack
// Phase 3 "Blade Tempest": 15 sec, 2.0× intensity, +80% attack + whirlwind
// Completion bonus: Blade Master state (attacks hit all nearby enemies for 60 sec)
```

---

### Concentration Mechanics

```csharp
public struct RitualConcentration : IComponentData
{
    public float BaseConcentration;         // Starting concentration
    public float CurrentConcentration;      // Current level
    public float ConcentrationRegen;        // Recovery per second
    public float ConcentrationDrain;        // Base drain per second

    // Stat modifiers
    public float MentalFortitude;           // Willpower/discipline (0.0 to 1.0)
    public float FocusCapacity;             // Max focus available
    public float ConcentrationSkill;        // Skill at maintaining concentration

    // Disruption resistance
    public float DisruptionResistance;      // Resist interruption (0.0 to 1.0)
    public float DamageDisruptionMod;       // How much damage disrupts
    public float StunDisruptionMod;         // How much stuns disrupt
    public float KnockbackDisruptionMod;    // How much knockback disrupts
}

public struct ConcentrationCheck : IComponentData
{
    public DisruptionSource Source;
    public float Magnitude;                 // Strength of disruption
    public float ResistanceRoll;            // Stat-based resistance
    public bool Succeeded;                  // Did they maintain concentration?
    public float ConcentrationLoss;         // How much concentration lost
}

public enum DisruptionSource : byte
{
    Damage = 0,
    Stun = 1,
    Silence = 2,            // Only affects verbal rituals
    Knockback = 3,
    ForcedMovement = 4,
    ManaDepletion = 5,
    FocusLoss = 6,
    Fear = 7,
    Confusion = 8,
    Death = 9               // Caster death
}

[BurstCompile]
public partial struct ConcentrationCheckSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (check, concentration, ritual) in SystemAPI.Query<
            RefRW<ConcentrationCheck>,
            RefRW<RitualConcentration>,
            RefRW<Ritual>>())
        {
            // Calculate disruption severity
            float baseSeverity = CalculateBaseSeverity(check.ValueRO.Source, check.ValueRO.Magnitude);

            // Apply source-specific modifiers
            float modifiedSeverity = check.ValueRO.Source switch
            {
                DisruptionSource.Damage => baseSeverity * concentration.ValueRO.DamageDisruptionMod,
                DisruptionSource.Stun => baseSeverity * concentration.ValueRO.StunDisruptionMod,
                DisruptionSource.Knockback => baseSeverity * concentration.ValueRO.KnockbackDisruptionMod,
                DisruptionSource.Silence => ritual.ValueRO.RequiresVerbal ? baseSeverity * 2.0f : 0f,
                DisruptionSource.Death => 1.0f, // Always fail on death
                _ => baseSeverity
            };

            // Roll resistance check
            float resistanceRoll = concentration.ValueRO.MentalFortitude *
                                  concentration.ValueRO.ConcentrationSkill *
                                  concentration.ValueRO.DisruptionResistance;

            check.ValueRW.ResistanceRoll = resistanceRoll;

            // Compare severity vs resistance
            if (resistanceRoll >= modifiedSeverity)
            {
                // Resisted disruption
                check.ValueRW.Succeeded = true;
                check.ValueRW.ConcentrationLoss = modifiedSeverity * 0.1f; // Minor loss
            }
            else
            {
                // Failed concentration check
                check.ValueRW.Succeeded = false;
                float failureMargin = modifiedSeverity - resistanceRoll;
                check.ValueRW.ConcentrationLoss = 0.2f + (failureMargin * 0.5f); // Significant loss
            }

            // Apply concentration loss
            concentration.ValueRW.CurrentConcentration -= check.ValueRO.ConcentrationLoss;

            // Check if ritual fails
            if (concentration.ValueRO.CurrentConcentration < ritual.ValueRO.ConcentrationThreshold)
            {
                ritual.ValueRW.CurrentPhase = RitualPhase.Interrupted;
                ritual.ValueRW.IsBeingDisrupted = true;
            }
        }
    }

    private float CalculateBaseSeverity(DisruptionSource source, float magnitude)
    {
        return source switch
        {
            DisruptionSource.Damage => magnitude * 0.01f,      // 100 damage = 1.0 severity
            DisruptionSource.Stun => magnitude * 0.5f,         // Duration-based
            DisruptionSource.Knockback => magnitude * 0.02f,   // Distance-based
            DisruptionSource.Silence => 0.8f,                  // High severity
            DisruptionSource.Fear => 0.6f,
            DisruptionSource.Death => 1.0f,                    // Total disruption
            _ => magnitude * 0.1f
        };
    }
}
```

### Focus-Based Intensity

```csharp
public struct RitualIntensityScaling : IComponentData
{
    public float MinimumIntensity;          // Intensity at baseline focus
    public float MaximumIntensity;          // Intensity at maximum focus
    public float IntensityPerFocus;         // How much each focus adds
    public float CurrentIntensity;          // Actual intensity

    // Scaling curves
    public IntensityScalingType ScalingType;
}

public enum IntensityScalingType : byte
{
    Linear = 0,         // 1:1 focus to intensity
    Exponential = 1,    // Accelerating returns
    Logarithmic = 2,    // Diminishing returns
    Stepped = 3         // Discrete intensity tiers
}

[BurstCompile]
public partial struct RitualIntensitySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (ritual, intensity) in SystemAPI.Query<
            RefRW<Ritual>,
            RefRW<RitualIntensityScaling>>())
        {
            // Calculate intensity from focus investment
            float focusRatio = ritual.ValueRO.CurrentFocusInvestment /
                              ritual.ValueRO.BaselineFocusRequired;

            float calculatedIntensity = intensity.ValueRO.ScalingType switch
            {
                IntensityScalingType.Linear =>
                    intensity.ValueRO.MinimumIntensity +
                    (focusRatio * intensity.ValueRO.IntensityPerFocus),

                IntensityScalingType.Exponential =>
                    intensity.ValueRO.MinimumIntensity *
                    math.pow(focusRatio, 1.5f),

                IntensityScalingType.Logarithmic =>
                    intensity.ValueRO.MinimumIntensity +
                    (math.log10(focusRatio + 1f) * intensity.ValueRO.MaximumIntensity),

                IntensityScalingType.Stepped =>
                    intensity.ValueRO.MinimumIntensity *
                    math.floor(focusRatio),

                _ => intensity.ValueRO.MinimumIntensity
            };

            intensity.ValueRW.CurrentIntensity = math.clamp(
                calculatedIntensity,
                intensity.ValueRO.MinimumIntensity,
                intensity.ValueRO.MaximumIntensity
            );

            ritual.ValueRW.RitualIntensity = intensity.ValueRO.CurrentIntensity;
        }
    }
}
```

---

## Ritual Types and Effects

### Barrier Ritual

```csharp
public struct BarrierRitual : IComponentData
{
    public float BarrierStrength;           // Hit points of barrier
    public float BarrierRegenRate;          // HP regen per second
    public float MaxBarrierStrength;
    public bool BlocksPhysical;
    public bool BlocksMagical;
    public bool BlocksProjectiles;
    public float DamageAbsorption;          // % damage absorbed
}

// Intensity scaling:
// Intensity 1.0: 500 HP barrier, blocks 50% damage
// Intensity 5.0: 2,500 HP barrier, blocks 75% damage
// Intensity 10.0: 5,000 HP barrier, blocks 90% damage
```

### Storm Rituals

```csharp
public struct StormRitual : IComponentData
{
    public StormType Type;
    public float TickRate;                  // Damage ticks per second
    public float DamagePerTick;             // Scaled by intensity
    public float StrikeChance;              // Chance of lightning strike
    public float StrikeRadius;
    public float MovementSpeedPenalty;      // Slow entities in storm
    public float AccuracyPenalty;           // Reduce accuracy in storm
}

public enum StormType : byte
{
    Blizzard = 0,       // Cold damage + slow
    Lightning = 1,      // Electric damage + stun chance
    Flame = 2,          // Fire damage + burning
    Acid = 3,           // Corrosive damage + armor reduction
    Void = 4            // Chaos damage + random effects
}

// Blizzard intensity scaling:
// Intensity 1.0: 10 cold dmg/sec, -20% movement
// Intensity 5.0: 50 cold dmg/sec, -60% movement, 20% freeze chance
// Intensity 10.0: 100 cold dmg/sec, -90% movement, 50% freeze chance

// Lightning storm intensity scaling:
// Intensity 1.0: 5 dmg/sec ambient, 100 dmg strikes every 5 sec
// Intensity 5.0: 25 dmg/sec ambient, 500 dmg strikes every 2 sec
// Intensity 10.0: 50 dmg/sec ambient, 1000 dmg strikes every 1 sec
```

### Healing Rain Ritual

```csharp
public struct HealingRainRitual : IComponentData
{
    public float HealingPerTick;            // HP restored per tick
    public float TickRate;                  // Heals per second
    public float ManaRestorationRate;       // Optional mana regen
    public float StatusClearChance;         // Chance to remove debuffs
    public bool AffectsAllies;
    public bool AffectsEnemies;             // Some rituals heal everyone
}

// Intensity scaling:
// Intensity 1.0: 5 HP/sec, no mana, 10% status clear
// Intensity 5.0: 25 HP/sec, 2 mana/sec, 30% status clear
// Intensity 10.0: 50 HP/sec, 5 mana/sec, 60% status clear, resurrect dead (1% chance)
```

### Aura/Buff Rituals

```csharp
public struct AuraRitual : IComponentData
{
    public AuraType Type;
    public float BonusMagnitude;            // Scales with intensity
    public bool StacksWithOtherAuras;
    public float ApplicationRadius;
}

public enum AuraType : byte
{
    Blessing = 0,       // +stats
    Haste = 1,          // +speed
    Fortification = 2,  // +defense
    Precision = 3,      // +accuracy
    Vigor = 4,          // +health regen
    Clarity = 5,        // +mana regen
    Fury = 6            // +damage
}

// Blessing aura intensity scaling:
// Intensity 1.0: +10% all stats
// Intensity 5.0: +50% all stats
// Intensity 10.0: +100% all stats, immune to fear
```

### Control Rituals

```csharp
public struct ControlRitual : IComponentData
{
    public ControlType Type;
    public float EffectMagnitude;
    public float ResistanceReduction;       // Reduces target resistance
}

public enum ControlType : byte
{
    Slow = 0,           // Movement speed reduction
    Silence = 1,        // Prevent spellcasting
    Gravity = 2,        // Pull toward center
    Chaos = 3,          // Random effects
    TimeWarp = 4        // Alter time flow
}

// Gravity well intensity scaling:
// Intensity 1.0: 10 m/s² pull, 10m radius
// Intensity 5.0: 50 m/s² pull, 25m radius
// Intensity 10.0: 100 m/s² pull (escape impossible), 50m radius
```

---

## Ritual Interruption and Backlash

### Interruption Consequences

```csharp
public struct RitualBacklash : IComponentData
{
    public RitualAlignment Alignment;
    public float RitualIntensity;
    public BacklashSeverity Severity;
    public float BacklashDamage;
    public BacklashEffect[] Effects;
}

public enum BacklashSeverity : byte
{
    Minor = 0,          // Small energy release
    Moderate = 1,       // Noticeable consequences
    Major = 2,          // Dangerous explosion
    Catastrophic = 3    // Reality-warping disaster
}

public struct BacklashEffect
{
    public BacklashEffectType Type;
    public float Magnitude;
    public float Radius;
    public uint Duration;
}

public enum BacklashEffectType : byte
{
    // Energy release
    EnergyExplosion = 0,        // Damage burst
    ManaVortex = 1,             // Drains mana from area
    ChainLightning = 2,         // Arcs between nearby entities

    // Environmental
    RealityTear = 10,           // Unstable space
    ElementalSpill = 11,        // Element spreads uncontrolled
    CursedGround = 12,          // Area becomes hazardous

    // Entity effects
    MagicBurn = 20,             // Damage over time to casters
    ManaAddiction = 21,         // Casters lose max mana
    Madness = 22,               // Temporary insanity
    Petrification = 23,         // Turned to stone

    // Summoning accidents
    UnintendedSummon = 30,      // Random creature appears
    PortalLeak = 31,            // Portal opens uncontrolled
    PossessionRisk = 32         // Spirits attempt possession
}

[BurstCompile]
public partial struct RitualBacklashSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (backlash, ritual) in SystemAPI.Query<
            RefRW<RitualBacklash>,
            RefRO<Ritual>>())
        {
            if (ritual.ValueRO.CurrentPhase != RitualPhase.Interrupted)
                continue;

            // Calculate backlash severity based on alignment and intensity
            backlash.ValueRW.Severity = CalculateBacklashSeverity(
                ritual.ValueRO.Alignment,
                ritual.ValueRO.RitualIntensity
            );

            // Lawful rituals: Predictable, safe shutdown
            if (ritual.ValueRO.Alignment == RitualAlignment.Lawful)
            {
                // Minor energy release only
                backlash.ValueRW.BacklashDamage = ritual.ValueRO.RitualIntensity * 10f;
                CreateSafeBacklashEffects(ref backlash.ValueRW, ritual.ValueRO);
            }
            // Neutral rituals: Moderate consequences
            else if (ritual.ValueRO.Alignment == RitualAlignment.Neutral)
            {
                backlash.ValueRW.BacklashDamage = ritual.ValueRO.RitualIntensity * 50f;
                CreateModerateBacklashEffects(ref backlash.ValueRW, ritual.ValueRO);
            }
            // Chaotic rituals: DANGEROUS
            else if (ritual.ValueRO.Alignment == RitualAlignment.Chaotic)
            {
                backlash.ValueRW.BacklashDamage = ritual.ValueRO.RitualIntensity * 200f;
                CreateChaoticBacklashEffects(ref backlash.ValueRW, ritual.ValueRO);
            }

            // Apply backlash effects
            ExecuteBacklash(backlash.ValueRO, ritual.ValueRO);
        }
    }

    private BacklashSeverity CalculateBacklashSeverity(
        RitualAlignment alignment,
        float intensity)
    {
        float severityScore = intensity;

        // Alignment modifier
        if (alignment == RitualAlignment.Chaotic)
            severityScore *= 2.0f;
        else if (alignment == RitualAlignment.Lawful)
            severityScore *= 0.3f;

        if (severityScore < 2.0f)
            return BacklashSeverity.Minor;
        else if (severityScore < 5.0f)
            return BacklashSeverity.Moderate;
        else if (severityScore < 10.0f)
            return BacklashSeverity.Major;
        else
            return BacklashSeverity.Catastrophic;
    }

    private void CreateChaoticBacklashEffects(
        ref RitualBacklash backlash,
        in Ritual ritual)
    {
        // Chaotic rituals have unpredictable, dangerous backlash
        // Examples:
        // - Interrupted Void Rift: Reality tear, random teleportation, summon void creatures
        // - Interrupted Chaos Domain: Mass madness, random polymorphing
        // - Interrupted Gate Opening: Uncontrolled portal spewing demons

        // Random number of effects (1-5)
        int effectCount = UnityEngine.Random.Range(1, 6);

        // Possible chaotic effects (randomized)
        var possibleEffects = new[]
        {
            BacklashEffectType.RealityTear,
            BacklashEffectType.UnintendedSummon,
            BacklashEffectType.Madness,
            BacklashEffectType.PortalLeak,
            BacklashEffectType.ChainLightning
        };

        // Apply random effects
        for (int i = 0; i < effectCount; i++)
        {
            var effect = possibleEffects[UnityEngine.Random.Range(0, possibleEffects.Length)];
            ApplyBacklashEffect(ritual, effect, ritual.RitualIntensity);
        }
    }
}
```

### Alignment-Based Backlash Examples

```csharp
// Example 1: Lawful Barrier (Intensity 5.0) interrupted
// Backlash severity: Minor
// Effects:
// - 50 damage to caster (5.0 × 10)
// - Energy dissipates harmlessly
// - No lasting effects

// Example 2: Neutral Lightning Storm (Intensity 5.0) interrupted
// Backlash severity: Moderate
// Effects:
// - 250 damage to caster (5.0 × 50)
// - Chain lightning hits nearby entities (3 targets, 150 dmg each)
// - Area becomes electrically charged for 30 seconds

// Example 3: Chaotic Void Rift (Intensity 8.0) interrupted
// Backlash severity: Major
// Effects:
// - 1,600 damage to caster (8.0 × 200)
// - Reality tear opens (20m radius)
// - Summons 1d6 void creatures (hostile to everyone)
// - Random teleportation of entities within 30m
// - Caster gains temporary madness (-50% accuracy, confused AI)
// - Cursed ground persists for 5 minutes (5 void dmg/sec)

// Example 4: Chaotic Gate Opening (Intensity 10.0) interrupted
// Backlash severity: Catastrophic
// Effects:
// - 2,000 damage to caster (10.0 × 200)
// - Uncontrolled portal opens (50m radius)
// - Portal leaks demons/aberrations continuously for 10 minutes
// - Reality distortion field (physics behave erratically)
// - All nearby casters lose 50% max mana permanently
// - Area becomes permanently cursed (cannot be removed without powerful cleansing)
// - Possible caster death + possession by summoned entity
```

---

## Space4X Ship Rituals

### Siege Beam Cooperation

Multiple ships focus energy beams on a single target:

```csharp
public struct SiegeBeam : IComponentData
{
    public Entity TargetShip;
    public float BeamPower;                 // Total power from all ships
    public float DamagePerSecond;
    public float ArmorPenetration;
    public uint ParticipatingShips;
    public bool RequiresPsionicCrew;
}

[InternalBufferCapacity(8)]
public struct SiegeBeamParticipant : IBufferElementData
{
    public Entity ShipEntity;
    public float PowerContribution;         // Energy per second
    public float BeamModuleEfficiency;      // Module quality
    public bool HasPsionicOperator;
}

// Intensity scaling (from cooperation):
// 2 ships, 60% cohesion: 1,000 DPS combined
// 5 ships, 80% cohesion: 3,200 DPS combined (synergy bonus)
// 10 ships, 90% cohesion: 8,100 DPS combined (massive synergy)

// Interruption: If lead ship disrupted (damage, EMP, etc.):
// - Lawful alignment (standard siege beam): Clean shutdown, no backlash
// - Neutral alignment (psionic-enhanced): Energy feedback (200 damage to all ships)
// - Chaotic alignment (void-powered): Beam explodes (1000 damage to all ships + target)
```

### Repair Cloud Ritual

Ships emit nanite clouds for distributed repair:

```csharp
public struct RepairCloud : IComponentData
{
    public float CloudRadius;
    public float RepairRatePerSecond;       // HP restored
    public float NaniteEfficiency;
    public uint ShipsInCloud;
    public bool RequiresNaniteModules;
}

// Intensity scaling:
// Intensity 1.0: 50 HP/sec, 500m radius
// Intensity 5.0: 250 HP/sec, 1500m radius
// Intensity 10.0: 500 HP/sec, 3000m radius, repairs disabled systems

// Interruption: Nanites go rogue
// - Minor: Nanites dissipate harmlessly
// - Moderate: Nanites damage ships instead of repairing (50 dmg/sec for 10 sec)
// - Major: Nanite swarm becomes hostile entity
```

### Jam Field Ritual

Electronic/psionic jamming prevents sensors and targeting:

```csharp
public struct JamField : IComponentData
{
    public JamType Type;
    public float JamStrength;               // Intensity of jamming
    public float EffectRadius;
    public float SensorReduction;           // % sensor effectiveness reduced
    public float AccuracyPenalty;           // Targeting penalty
}

public enum JamType : byte
{
    Electronic = 0,     // Radio/radar jamming
    Psionic = 1,        // Mental interference
    Gravitational = 2,  // Spacetime distortion
    Quantum = 3         // Probability manipulation
}

// Intensity scaling:
// Intensity 1.0: -30% sensors, -20% accuracy, 1000m radius
// Intensity 5.0: -70% sensors, -50% accuracy, 3000m radius
// Intensity 10.0: -95% sensors, -80% accuracy, 5000m radius, blinds AI

// Interruption consequences:
// - Electronic: Clean shutdown (lawful)
// - Psionic: Feedback to psionics (moderate, 500 psionic damage)
// - Gravitational: Spacetime ripple (moderate, random movement)
// - Quantum: Probability cascade (chaotic, random effects)
```

### Shield Link Ritual

Multiple ships link shields into unified defensive field:

```csharp
public struct ShieldLink : IComponentData
{
    public float CombinedShieldStrength;    // Total HP from all shields
    public float ShieldRegenRate;
    public float DamageDistribution;        // How evenly damage spreads
    public uint LinkedShips;
}

[InternalBufferCapacity(8)]
public struct LinkedShield : IBufferElementData
{
    public Entity ShipEntity;
    public float ShieldContribution;        // Shield HP contributed
    public float PowerDrain;                // Energy cost
}

// Intensity scaling:
// 3 ships, cohesion 70%: 3,000 combined shields, 90% damage distribution
// 5 ships, cohesion 85%: 6,500 combined shields, 95% damage distribution
// 10 ships, cohesion 95%: 15,000 combined shields, 99% damage distribution

// Interruption: Shield overload
// - If one ship dies: Shields collapse, energy feedback (300 dmg to all ships)
// - If interrupted intentionally: Clean shutdown over 3 seconds
// - If disrupted by EMP: Chain reaction, all shields offline for 30 sec
```

---

## Ritual Cooperation Integration

Rituals use the cooperation system for multi-caster bonuses:

```csharp
public struct RitualCooperation : IComponentData
{
    public Entity RitualEntity;
    public Entity CooperationEntity;        // Links to Cooperation component
    public float CohesionBonus;             // From cooperation cohesion
    public float PowerAmplification;        // Intensity multiplier
}

// Formula:
// SingleCasterIntensity = FocusInvestment / BaselineFocus
// CooperativeCasterIntensity = (TotalFocus / BaselineFocus) × CohesionMultiplier
// CohesionMultiplier = 1.0 + (Cohesion × 2.0) // Up to 3× at perfect cohesion

// Example: 3 mages casting Blizzard ritual
// Mage A: 100 focus
// Mage B: 80 focus
// Mage C: 60 focus
// Total: 240 focus
// Baseline: 50 focus
// Cohesion: 0.75

// Single caster intensity: 100 / 50 = 2.0
// Cooperative intensity: (240 / 50) × (1 + 0.75 × 2.0) = 4.8 × 2.5 = 12.0

// Result: 3 mages with good cohesion create intensity 12.0 ritual
// (vs intensity 2.0 if cast alone)
// Blizzard now does 120 cold dmg/sec in 60m radius with 70% freeze chance
```

---

## Phase-Based Ritual Examples

### Example 1: Bard's Epic Song (4 Phases)

```csharp
// Bard performing "The Ballad of Heroes Past"
var epicSong = new Ritual
{
    Type = RitualType.EpicSong,
    Alignment = RitualAlignment.Lawful
};

var progression = new RitualPhaseProgression
{
    TotalPhases = 4,
    CurrentPhaseIndex = 0
};

// Phase 1: "Opening Verse" (10 seconds)
// Difficulty: Easy (0.3 threshold)
// Effect: +10% morale
// Bard skill: 0.7, Concentration: 0.8, Cohesion: 0.6
// Check: (0.7 × 0.5) + (0.8 × 0.3) + (0.6 × 0.2) = 0.35 + 0.24 + 0.12 = 0.71
// Roll: 0.45 → PASS (0.71 > 0.3)
// Result: +10% morale, advance to Phase 2

// Phase 2: "Rising Action" (15 seconds)
// Difficulty: Moderate (0.5 threshold)
// Effect: +15% attack
// Check: 0.71 (same calculation)
// Roll: 0.62 → PASS (0.71 > 0.5)
// Result: +15% attack, Phase 1 bonus retained at 60% (+6% morale)
// Current bonuses: +15% attack, +6% morale

// Phase 3: "Climax" (20 seconds, 1.8× intensity)
// Difficulty: Hard (0.7 threshold)
// Effect: +25% damage
// Check: 0.71
// Roll: 0.73 → PASS (0.71 ≥ 0.7, barely)
// Result: +25% damage
// Phase 1 retained at 36%: +3.6% morale
// Phase 2 retained at 60%: +9% attack
// Current bonuses: +25% damage, +9% attack, +3.6% morale

// Phase 4: "Resolution" (10 seconds)
// Difficulty: Moderate (0.5 threshold)
// Check: 0.71
// Roll: 0.92 → CRITICAL SUCCESS! (0.92 ≥ 0.9 and 0.71 > 0.5)
// Result: ALL PHASES COMPLETED + CRITICAL FINALE
// - All bonuses locked (don't decay)
// - Completion bonus: Inspired State (+50% all stats for 180 sec)
// - Critical bonus: +50% duration on all bonuses (270 sec total)

// Final effect:
// +25% damage, +9% attack, +3.6% morale
// + Inspired State: +50% all stats
// Duration: 270 seconds
// Audience morale: +100% (epic performance)
```

### Example 2: Warrior's War Chant with Phase Failure

```csharp
// Warrior leading "Hymn of Iron"
var warChant = new Ritual
{
    Type = RitualType.WarChant,
    Alignment = RitualAlignment.Neutral
};

// Phase 1: "Call to Arms" (5 seconds)
// Effect: +15% attack
// Warrior skill: 0.6, Concentration: 0.7, Cohesion: 0.8 (unit cohesion)
// Check: (0.6 × 0.5) + (0.7 × 0.3) + (0.8 × 0.2) = 0.67
// Roll: 0.55 → PASS
// Result: +15% attack

// Phase 2: "Oath of Valor" (10 seconds)
// Effect: +20% defense
// Check: 0.67
// Roll: 0.71 → PASS
// Result: +20% defense, Phase 1 retained at 60% (+9% attack)
// Current: +20% defense, +9% attack

// Phase 3: "Warcry" (15 seconds, 2.0× intensity)
// Effect: +40% morale, immune to fear
// Warrior takes arrow (100 damage) mid-chant
// Concentration drops: 0.7 → 0.4
// Check: (0.6 × 0.5) + (0.4 × 0.3) + (0.8 × 0.2) = 0.58
// Difficulty: VeryHard (0.85 threshold)
// Roll: 0.62 → FAILURE (0.58 < 0.85)

// Failure consequence:
// - Ritual ends (not catastrophic, just interrupted)
// - Bonuses from completed phases retained at 50% strength:
//   Phase 1: 15% × 0.5 = 7.5% attack
//   Phase 2: 20% × 0.5 = 10% defense
// - Duration: 60 seconds (reduced from normal 120)
// - No completion bonus
// - Can attempt again after 30 second cooldown

// Outcome: Partial success, unit gets modest bonuses
```

### Example 3: Ritual Chain - Warrior's Crescendo

```csharp
// Experienced warrior chains 3 rituals
var chain = new RitualChain
{
    ChainName = "Path of the Champion",
    TotalRitualsInChain = 3,
    ChainBonus = 0f // Accumulates
};

// Ritual 1: War Chant (3 phases, all completed)
// Bonuses: +15% attack, +20% defense, +25% morale
// Chain bonus: +30% effectiveness
// Actual bonuses: +19.5% attack, +26% defense, +32.5% morale
// ChainBonus accumulates: 0.3

// Ritual 2: Battle Hymn (4 phases, all completed)
// Bonuses: +20% damage, +30% morale, immune to fear
// Chain bonus: +60% effectiveness (0.3 from Ritual 1, +0.3 from this)
// Actual bonuses: +32% damage, +48% morale, immune to fear
// ChainBonus accumulates: 0.6
// Previous ritual bonuses retained at 70% (chaining bonus):
//   +13.65% attack, +18.2% defense, +22.75% morale (from War Chant)

// Ritual 3: Victory Song (5 phases, all completed)
// Bonuses: +40% all stats, +100% morale, unbreakable will
// Chain bonus: +90% effectiveness (0.6 + 0.3)
// Actual bonuses: +76% all stats, +190% morale, unbreakable will
// ChainBonus: 0.9

// CHAIN COMPLETION BONUS (all 3 completed):
// Synergy: Unstoppable Fervor (multiplicative)
// - All stat bonuses multiply together
// - Cannot be demoralized, feared, stunned, or routed
// - Morale locked at maximum (300%)
// - Duration: 300 seconds
// - Legend buff: This performance becomes a legendary tale
//   (other warriors can learn this chain from this warrior)

// Result: God-tier warrior for 5 minutes
```

### Example 4: Ritual Switch - Failed Summoning

```csharp
// Mage attempting "Gate to Elemental Plane"
var summoning = new Ritual
{
    Type = RitualType.GateOpening,
    Alignment = RitualAlignment.Chaotic // Dangerous
};

// Phase 1: "Circle of Binding" - PASS
// Phase 2: "Planar Attunement" - PASS
// Phase 3: "Gateway Opening" - CRITICAL FAILURE (roll: 0.03)

// Critical failure consequence:
// - Normal outcome: Catastrophic backlash (1000+ damage, uncontrolled portal)
// - But this mage has RitualSwitchConfig enabled

var switchConfig = new RitualSwitchConfig
{
    AllowSwitching = true,
    ClosureRitualName = "Emergency Sealing",
    SwitchPenalty = 50f // 50 focus cost
};

// Automatic switch triggered:
// - Original ritual: Gate Opening (failed at Phase 3)
// - Switch to: Emergency Sealing (3 phases)
// - Transition time: 2 seconds
// - Cost: 50 focus (mage has 120, can afford)

// Emergency Sealing ritual:
// Phase 1: "Containment" (3 sec) - PASS
//   - Prevents portal from fully opening
//   - Damage reduced: 1000 → 200
// Phase 2: "Banishment" (5 sec) - PASS
//   - Sends back partially-summoned entities
//   - No uncontrolled summons
// Phase 3: "Ward Reinforcement" (4 sec) - PASS
//   - Seals planar breach
//   - Prevents future summons at this location for 1 hour

// Result: Disaster averted
// - Mage takes 200 damage (vs 1000)
// - No hostile summons
// - Area temporarily warded
// - Mage gains experience in ritual recovery
// - Can attempt summoning again (elsewhere, after cooldown)
```

### Example 5: Custom Crafted Ritual

```csharp
// Master warrior designs personal ritual
var crafter = new RitualCraftingSystem
{
    CrafterSkill = 0.9f,
    RitualsDesigned = 47,
    CanCraftChains = true,
    CanCustomizeIntensity = true
};

var customRitual = new CraftedRitual
{
    CustomName = "Tempest of a Thousand Cuts",
    Crafter = warriorEntity,
    PhaseCount = 3,
    IntensityCurve = IntensityCurveType.FinaleExplosion,
    Alignment = RitualAlignment.Neutral,
    CraftingQuality = 0.85f
};

// Phase 1: "Blade Meditation" (8 sec, 0.5× intensity)
// Effect: +10% accuracy, +5% attack speed
// Difficulty: Easy (crafter made it accessible)
// Visual: Warrior enters focused stance, blade glows

// Phase 2: "Whirlwind Form" (12 sec, 0.8× intensity)
// Effect: +25% attack speed, attacks hit 2 nearby enemies
// Difficulty: Moderate
// Visual: Warrior spins, blade becomes blur

// Phase 3: "Tempest Unleashed" (10 sec, 3.0× intensity!!!)
// Effect: +100% attack speed, attacks hit ALL nearby enemies (10m radius)
//         Each hit has 30% chance to proc additional strike
// Difficulty: Hard
// Visual: Warrior becomes living storm of blades

// Completion bonus (if all phases finished):
// - Blade Dancer state (60 sec)
// - Immune to interruption
// - Movement speed +50%
// - Leaves damaging vortex trail
// - Final strike deals 500% damage AoE

// Crafter customizations:
// - Chose FinaleExplosion curve (weak start, explosive finale)
// - Made Phase 1 easy so he can reliably start it in combat
// - Made Phase 3 extremely powerful but risky (3.0× intensity)
// - Balanced difficulty so failure at Phase 3 isn't catastrophic
// - Can be chained with other warrior rituals

// This ritual becomes part of this warrior's signature style
// Other warriors can learn it if they witness it performed successfully
```

---

## Example Ritual Scenarios

### Scenario 1: Lawful Barrier Interrupted

```csharp
var barrier = new Ritual
{
    Type = RitualType.Barrier,
    Alignment = RitualAlignment.Lawful,
    RitualIntensity = 6.0f,
    CurrentFocusInvestment = 300f,
    BaselineFocusRequired = 50f
};

// Caster takes 150 damage from arrow
var check = new ConcentrationCheck
{
    Source = DisruptionSource.Damage,
    Magnitude = 150f
};

// Concentration check:
// Base severity: 150 × 0.01 = 1.5
// Caster stats: Mental fortitude 0.7, Concentration skill 0.8, Resistance 0.6
// Resistance roll: 0.7 × 0.8 × 0.6 = 0.336

// Result: 1.5 > 0.336 → FAILED
// Concentration loss: 0.2 + (1.5 - 0.336) × 0.5 = 0.782
// Current concentration: 0.8 - 0.782 = 0.018

// Falls below threshold (0.3) → Ritual interrupted

// Backlash (Lawful):
// Severity: Minor (6.0 intensity × 0.3 = 1.8 score)
// Damage: 6.0 × 10 = 60 damage to caster
// Effect: Energy dissipates harmlessly, barrier fades
// No lasting consequences
```

### Scenario 2: Chaotic Void Rift Interrupted

```csharp
var voidRift = new Ritual
{
    Type = RitualType.VoidRift,
    Alignment = RitualAlignment.Chaotic,
    RitualIntensity = 9.0f,
    CurrentFocusInvestment = 450f,
    BaselineFocusRequired = 50f
};

// Caster gets stunned for 2 seconds
var check = new ConcentrationCheck
{
    Source = DisruptionSource.Stun,
    Magnitude = 2.0f
};

// Concentration check:
// Base severity: 2.0 × 0.5 = 1.0
// Modified severity: 1.0 × 2.0 (stun modifier) = 2.0
// Resistance roll: 0.8 × 0.9 × 0.7 = 0.504

// Result: 2.0 > 0.504 → FAILED catastrophically

// Backlash (Chaotic):
// Severity: Catastrophic (9.0 × 2.0 = 18.0 score)
// Damage: 9.0 × 200 = 1,800 damage to caster (likely fatal)
// Effects (randomized):
//   1. Reality Tear (40m radius, spacetime unstable for 300 sec)
//   2. Unintended Summon (3d6 void horrors, hostile to all)
//   3. Madness (all entities within 50m, -60% accuracy, 60 sec)
//   4. Portal Leak (void energy spills, 20 dmg/sec in area)
//   5. Chain Lightning (arcs to 8 nearest entities, 800 dmg each)

// Result: TPK risk, entire battlefield becomes chaos zone
```

### Scenario 3: Cooperative Healing Rain (High Cohesion)

```csharp
// 4 druids casting Healing Rain together
var healingRain = new Ritual
{
    Type = RitualType.HealingRain,
    Alignment = RitualAlignment.Lawful,
    CurrentFocusInvestment = 400f,  // 100 each
    BaselineFocusRequired = 50f
};

var cooperation = new Cooperation
{
    Type = CooperationType.RitualCasting,
    ParticipantCount = 4
};

var cohesion = new CooperationCohesion
{
    CurrentCohesion = 0.85f         // High cohesion (druid circle)
};

// Intensity calculation:
// Base: 400 / 50 = 8.0
// Cohesion multiplier: 1 + (0.85 × 2.0) = 2.7
// Final intensity: 8.0 × 2.7 = 21.6 (!!!)

// Effects at intensity 21.6:
// - 108 HP/sec healing (50 base × 2.16)
// - 10.8 mana/sec restoration
// - 90% status effect clear chance
// - 5% resurrection chance for dead allies
// - 80m radius

// One druid takes 300 damage (arrow volley)
// Concentration check:
// Resistance: 0.75 × 0.85 × 0.7 = 0.446
// Severity: 3.0 (300 × 0.01)
// Result: FAILED

// But ritual continues!
// - Remaining 3 druids maintain ritual
// - Intensity drops to: (300 / 50) × 2.5 = 15.0
// - Still powerful (75 HP/sec), ritual stable
// - Injured druid gets healed by the ritual they helped create
```

### Scenario 4: Space4X Siege Beam Cooperation

```csharp
// 6 ships cooperating on siege beam
var siegeBeam = new Ritual
{
    Type = RitualType.SiegeBeam,
    Alignment = RitualAlignment.Neutral,
    CurrentFocusInvestment = 6000f, // 1000 energy each ship
    RitualIntensity = 7.5f
};

var cooperation = new Cooperation
{
    Type = CooperationType.SiegeBeam,
    ParticipantCount = 6
};

var cohesion = new CooperationCohesion
{
    CurrentCohesion = 0.72f         // Moderate cohesion (fleet coordination)
};

// Beam power: 6000 × (1 + 0.72 × 1.5) = 12,480 DPS

// Lead ship takes critical hit (500 damage to power core)
// Concentration check:
// Ship AI resistance: 0.6 × 0.7 × 0.5 = 0.21
// Damage severity: 500 × 0.01 = 5.0
// Result: CATASTROPHIC FAILURE

// Backlash (Neutral alignment):
// Severity: Major (7.5 × 1.0 = 7.5 score)
// Damage: 7.5 × 50 = 375 damage to all 6 ships
// Effect: Energy feedback explosion
//   - 375 shield damage to each participating ship
//   - Power systems overloaded (offline 15 sec)
//   - Targeting disrupted (30% accuracy for 30 sec)
//   - Target ship survives (beam interrupted)

// Lesson: Don't focus siege beam if you're about to get hit
```

---

## Integration with Other Systems

**Cooperation System**: Multi-caster rituals use cooperation cohesion for power amplification

**Combat Mechanics**: Damage/knockback trigger concentration checks

**Communication System**: Verbal rituals silenced by silence effects

**Patience System**: Impatient entities struggle to maintain long rituals

**Formations**: Formation cohesion can support ritual cohesion (disciplined troops maintain focus better)

**Magic/Mana System**: Focus and mana fuel ritual intensity

**Sandbox Modding**: All ritual parameters runtime-moddable (intensity scaling, backlash severity, etc.)

---

## Performance Targets

```
Ritual Intensity Calc:    <0.1ms per ritual
Concentration Check:      <0.05ms per check
Backlash Calculation:     <0.2ms per interruption
Storm Tick Updates:       <0.3ms per storm (100 entities affected)
Aura Application:         <0.15ms per aura (50 entities)
Ship Cooperation:         <0.2ms per ritual (10 ships)
────────────────────────────────────────
Total (50 active rituals): <40ms per frame
```

---

## Summary

**Universal System**: Not just magic - warriors chant hymns, bards sing epics, captains rally troops, workers sing labor songs, ships coordinate formations

**Ritual Types**:
- **Magic**: Barriers, storms (blizzard/lightning/flame/acid/void), healing rains, auras, control fields, summoning
- **Non-magic**: War chants, battle hymns, epic songs, rally speeches, work songs, march cadences, shanties
- **Space4X ships**: Siege beams, repair clouds, jam fields, shield links, sensor webs

**Phase-Based Progression**: Rituals split into 2-8 phases with varying intensity curves (flat, crescendo, peak middle, finale explosion, wave pattern, chaotic)

**Phase Checks**: Each phase requires skill check (50% skill + 30% concentration + 20% cooperation)
- **Critical Failure** (≤5% roll): Catastrophic backlash, possible ritual switch to closure/mending
- **Failure** (<threshold): Ritual ends, bonuses retained at 50%, no backlash
- **Pass** (≥threshold): Advance to next phase, bonuses carry forward at 60%
- **Critical Success** (≥90% roll + high skill): 150% effectiveness, 80% retention, bonus inspiration

**Phase Retention**: Bonuses from earlier phases persist with diminishing returns (60% base, compounds: 0.6, 0.36, 0.216...)

**Completion Bonuses**: Successfully finishing all phases grants powerful rewards (double effectiveness, inspired state, unbreakable morale, mass resurrection, permanent buffs)

**Ritual Switching**: Failed phases can trigger alternative closure/mending rituals (prevents catastrophic backlash, costs focus, automatic if configured)

**Ritual Chaining**: String multiple rituals together for combo effects
- Chain bonus accumulates (+30% per ritual)
- Previous ritual bonuses retained at 70% (vs 60% normal)
- Completion synergies: Additive, multiplicative, transformative, resonant
- Example: War Chant → Battle Hymn → Victory Song = Unstoppable Fervor (300% morale, immune to all demoralization)

**Ritual Crafting**: Master performers design custom rituals
- Define phases (2-8), intensity curves, difficulties
- Choose effects and magnitudes per phase
- Balance risk vs reward (easy start, explosive finale)
- Becomes signature style, teachable to others

**Focus Mechanics**: Baseline focus required, more focus = higher intensity (1.5× to 3× scaling with cooperation)

**Concentration**: Checked against damage, stuns, silences, knockbacks; resistance = Mental Fortitude × Skill × Resistance stat

**Intensity Scaling**: Linear, exponential, logarithmic, or stepped progression from focus investment

**Alignment Consequences**:
- **Lawful**: Safe interruption (10× intensity damage, no lasting effects)
- **Neutral**: Moderate backlash (50× intensity damage, temporary effects)
- **Chaotic**: DANGEROUS (200× intensity damage, catastrophic effects, reality tears, unintended summons)

**Example Phase Outcomes**:
- **Bard epic song** (4 phases, all completed + critical finale): +50% all stats for 270 sec, audience inspired
- **Warrior war chant** (Phase 3 failed after 2 successes): 7.5% attack, 10% defense for 60 sec (partial success)
- **Warrior ritual chain** (3 rituals completed): +76% all stats, +190% morale, unbreakable will, legendary status for 300 sec
- **Failed summoning** (switched to emergency sealing): 200 damage vs 1,000, no summons, area warded

**Key Insights**:
1. Non-magic users have equal access to powerful rituals through performance, chanting, and leadership
2. Phase-based system creates dramatic progression with rising/falling intensity
3. Failure isn't binary - partial completion still grants partial benefits
4. Chaining rituals can create god-tier temporary states
5. Ritual switching provides safety net for dangerous chaotic rituals
6. Custom crafted rituals become signature fighting/performance styles
