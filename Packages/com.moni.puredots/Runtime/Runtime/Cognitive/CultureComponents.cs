using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Cognitive
{
    /// <summary>
    /// Culture profile storing aggregate traits and reputation.
    /// Used for cultural learning and strategic adaptation.
    /// </summary>
    public struct CultureProfile : IComponentData
    {
        /// <summary>Culture ID</summary>
        public ushort Id;

        /// <summary>Aggression level (0-1)</summary>
        public float Aggression;

        /// <summary>Trustworthiness level (0-1)</summary>
        public float Trustworthiness;

        /// <summary>Magic style preference (0-1)</summary>
        public float MagicStyle;

        /// <summary>Global social weight/reputation (0-1)</summary>
        public float Reputation;

        /// <summary>Last tick when profile was updated</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Belief vector per culture stored on leaders/captains.
    /// Used for predicting enemy tactics and adjusting diplomacy.
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct CultureBelief : IBufferElementData
    {
        /// <summary>Culture ID this belief is about</summary>
        public ushort CultureId;

        /// <summary>Belief value: lerp(belief, observedTrait, LearningRate)</summary>
        public float BeliefValue;

        /// <summary>Confidence in this belief (0-1)</summary>
        public float Confidence;

        /// <summary>Last tick when belief was updated</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Shared culture memory graph singleton.
    /// Maintains aggregate statistics across all cultures.
    /// </summary>
    public struct CultureMemoryGraph : IComponentData
    {
        /// <summary>Total number of cultures tracked</summary>
        public int CultureCount;

        /// <summary>Last tick when graph was updated</summary>
        public uint LastUpdateTick;
    }
}

