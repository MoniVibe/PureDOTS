using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Cognitive
{
    /// <summary>
    /// Grudge entry storing negative experiences per culture.
    /// Uses exponential weighting: grudge[culture] = 1 - exp(-Anger * negativeEvents)
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct GrudgeEntry : IBufferElementData
    {
        /// <summary>Culture ID this grudge is against</summary>
        public ushort CultureId;

        /// <summary>Grudge value (0-1, where 1 = maximum grudge)</summary>
        public float GrudgeValue;

        /// <summary>Count of negative events contributing to this grudge</summary>
        public int NegativeEventCount;

        /// <summary>Last tick when grudge was updated</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Prejudice profile controlling grudge decay and forgiveness.
    /// Species-specific flags control decay behavior (e.g., dwarves never forget).
    /// </summary>
    public struct PrejudiceProfile : IComponentData
    {
        /// <summary>Decay rate per tick (0 = never forget, 1 = instant decay)</summary>
        public float DecayRate;

        /// <summary>Forgiveness factor (0-1): reduces grudge over time</summary>
        public float ForgivenessFactor;

        /// <summary>Flag: if true, decay is clamped to 0 (never forget grudges)</summary>
        public bool NeverForget;

        /// <summary>Last tick when prejudice was updated</summary>
        public uint LastUpdateTick;
    }
}

