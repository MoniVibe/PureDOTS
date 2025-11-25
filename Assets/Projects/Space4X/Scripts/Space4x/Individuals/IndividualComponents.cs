using PureDOTS.Runtime.Shared;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Individuals
{
    /// <summary>
    /// Individual entity ID component.
    /// </summary>
    public struct IndividualId : IComponentData
    {
        public FixedString64Bytes Value;
    }

    /// <summary>
    /// Individual stats component. Stores Command, Tactics, Logistics, Diplomacy, Engineering, Resolve (0-100 each).
    /// </summary>
    public struct IndividualStats : IComponentData
    {
        public int Command;
        public int Tactics;
        public int Logistics;
        public int Diplomacy;
        public int Engineering;
        public int Resolve;
    }

    /// <summary>
    /// Physique, Finesse, Will attributes with inclination modifiers (1-10) and general XP pool.
    /// </summary>
    public struct PhysiqueFinesseWill : IComponentData
    {
        public int Physique; // 0-100
        public int Finesse; // 0-100
        public int Will; // 0-100
        public byte PhysiqueInclination; // 1-10, scales XP gain
        public byte FinesseInclination; // 1-10, scales XP gain
        public byte WillInclination; // 1-10, scales XP gain
        public float GeneralXP; // Cross-discipline XP pool
    }

    /// <summary>
    /// Expertise entry buffer. Stores expertise type and tier (0-255).
    /// </summary>
    public struct ExpertiseEntry : IBufferElementData
    {
        public ExpertiseType Type;
        public byte Tier; // 0-255
    }

    /// <summary>
    /// Expertise types for individual entities.
    /// </summary>
    public enum ExpertiseType : byte
    {
        CarrierCommand = 0,
        Espionage = 1,
        Logistics = 2,
        Psionic = 3,
        Beastmastery = 4
    }

    /// <summary>
    /// Service trait buffer. Stores trait IDs like "ReactorWhisperer", "StrikeWingMentor", etc.
    /// </summary>
    public struct ServiceTrait : IBufferElementData
    {
        public FixedString32Bytes TraitId;
    }

    /// <summary>
    /// Preordain profile component. Defines career path focus.
    /// </summary>
    public struct PreordainProfile : IComponentData
    {
        public PreordainTrack Track;
    }

    /// <summary>
    /// Preordain track types for career progression.
    /// </summary>
    public enum PreordainTrack : byte
    {
        CombatAce = 0,
        LogisticsMaven = 1,
        DiplomaticEnvoy = 2,
        EngineeringSavant = 3
    }
}

