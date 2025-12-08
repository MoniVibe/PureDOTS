using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Limb/body part type for granular health tracking.
    /// </summary>
    public enum LimbType : byte
    {
        Head,
        Torso,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg,
        Tail,
        ExtraLimb1,
        ExtraLimb2
    }

    /// <summary>
    /// Flags indicating limb modifications (augmented, mutated, grafted, severed).
    /// </summary>
    [Flags]
    public enum LimbFlags : byte
    {
        Normal      = 0,
        Augmented   = 1 << 0,
        Mutated     = 1 << 1,
        Grafted     = 1 << 2,
        Severed     = 1 << 3,
    }

    /// <summary>
    /// Health state for a specific limb/body part.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct LimbState : IBufferElementData
    {
        public LimbType Limb;
        public float MaxHP;
        public float CurrentHP;
        public LimbFlags Flags;
    }

    /// <summary>
    /// Atomic action types that can be composed into behavior sequences.
    /// </summary>
    public enum AtomicAction : byte
    {
        Dash = 0,
        Swing = 1,
        Parry = 2,
        Jump = 3,
        Fire = 4,
        Cast = 5
    }

    /// <summary>
    /// Action ID for behavior node references.
    /// </summary>
    public enum ActionId : ushort
    {
        None = 0,
        SimpleAttack = 1,
        SimpleParry = 2,
        SimpleMove = 3,
        StrafeShoot = 4,
        CounterParry = 5,
        MultiTargetDodge = 6,
        DualCast = 7
    }

    /// <summary>
    /// Behavior tier: Baseline (always available), Learned (skill unlocked), Mastered (skill + implant).
    /// </summary>
    public enum BehaviorTier : byte
    {
        Baseline = 0,
        Learned = 1,
        Mastered = 2
    }

    /// <summary>
    /// Implant tag flags for behavior gating.
    /// </summary>
    [Flags]
    public enum ImplantFlags : byte
    {
        None = 0,
        DualSynapse = 1 << 0,
        NeuralBoost = 1 << 1,
        ReflexEnhancement = 1 << 2,
        CombatImplant1 = 1 << 3,
        CombatImplant2 = 1 << 4,
        CombatImplant3 = 1 << 5,
        CombatImplant4 = 1 << 6,
        CombatImplant5 = 1 << 7
    }

    /// <summary>
    /// Behavior node definition (blob-safe).
    /// </summary>
    public struct BehaviorNode
    {
        public ushort Id;
        public float SkillReq;
        public byte ImplantTag;
        public float FocusCost;
        public float StaminaCost;
        public float BaseWeight;
        public FixedList64Bytes<ActionId> Actions;
    }

    /// <summary>
    /// Unlocked behavior ID in entity's behavior set.
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct BehaviorSet : IBufferElementData
    {
        public ushort BehaviorId;
        public uint UnlockTick;
    }

    /// <summary>
    /// Current behavior tier for an entity.
    /// </summary>
    public struct BehaviorTierState : IComponentData
    {
        public BehaviorTier Tier;
        public ushort ActiveBehaviorId;
    }

    /// <summary>
    /// Stamina state - physical endurance for combat actions.
    /// Mirrors FocusState pattern.
    /// </summary>
    public struct StaminaState : IComponentData
    {
        public float Current;          // 0..Max
        public float Max;
        public float RegenRate;        // Per tick regeneration
        public float SoftThreshold;    // Below this: performance penalties start
        public float HardThreshold;    // Below this: risk of exhaustion
    }

    /// <summary>
    /// Implant tag component for behavior gating.
    /// </summary>
    public struct ImplantTag : IComponentData
    {
        public ImplantFlags Flags;
    }

    /// <summary>
    /// Impulse event for reactive motion.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct ImpulseEvent : IBufferElementData
    {
        public float3 Force;
        public Entity Source;
        public float Magnitude;
        public uint Tick;
    }

    /// <summary>
    /// Motion reaction state for physics-based responses.
    /// </summary>
    public struct MotionReactionState : IComponentData
    {
        public float ReactionSkill;
        public bool CanMidAirParry;
    }

    /// <summary>
    /// Target packet for multi-target behaviors (max 8 targets).
    /// </summary>
    public struct TargetPacket
    {
        public FixedList64Bytes<Entity> Targets;
        public byte Count;
    }

    /// <summary>
    /// Multi-target behavior tag marker.
    /// </summary>
    public struct MultiTargetBehaviorTag : IComponentData { }

    /// <summary>
    /// Action composition buffer for procedural action blending.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct ActionComposition : IBufferElementData
    {
        public AtomicAction Action;
        public float StartTime;
        public float Duration;
        public float3 Direction;
    }

    /// <summary>
    /// Hit buffer for damage application (AoSoA-friendly).
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct HitBuffer : IBufferElementData
    {
        public Entity Target;
        public float Damage;
        public float3 HitPoint;
        public uint Tick;
    }

    /// <summary>
    /// Behavior unlock event for presentation sync.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct BehaviorUnlockEvent : IBufferElementData
    {
        public ushort BehaviorId;
        public uint UnlockTick;
    }

    /// <summary>
    /// Behavior success rate tracking for adaptive learning.
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct BehaviorSuccessRate : IBufferElementData
    {
        public ushort BehaviorId;
        public uint SuccessCount;
        public uint AttemptCount;
        public float Weight;
    }

    /// <summary>
    /// Combat learning state.
    /// </summary>
    public struct CombatLearningState : IComponentData
    {
        public float LearningRate;
        public float DecayRate;
        public uint LastDecayTick;
    }

    /// <summary>
    /// Cognitive stats (Wisdom, Finesse, Physique) for behavior modifiers.
    /// </summary>
    public struct CognitiveStats : IComponentData
    {
        public float Wisdom;
        public float Finesse;
        public float Physique;
    }

    /// <summary>
    /// Behavior modifier computed from cognitive stats.
    /// </summary>
    public struct BehaviorModifier : IComponentData
    {
        public float FocusCostMultiplier;
        public float LearningRateMultiplier;
        public float StaminaCostMultiplier;
    }

    /// <summary>
    /// Leader tag for captains/commanders.
    /// </summary>
    public struct LeaderTag : IComponentData { }

    /// <summary>
    /// Fleet command state for aggregate learning.
    /// </summary>
    public struct FleetCommandState : IComponentData
    {
        public float LearnRate;
        public BlobAssetReference<TacticSuccessRateBlob> Tactics;
    }

    /// <summary>
    /// Culture ID for tactic tracking.
    /// </summary>
    public struct CultureId : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// Behavior event for presentation synchronization.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct BehaviorEvent : IBufferElementData
    {
        public ushort BehaviorId;
        public uint StartTick;
        public float3 Position;
        public float3 Direction;
    }

    /// <summary>
    /// Presentation command queue singleton buffer.
    /// </summary>
    public struct PresentationCommandQueueTag : IComponentData { }
}

