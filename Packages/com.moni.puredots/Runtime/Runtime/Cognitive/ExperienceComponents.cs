using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Cognitive
{
    /// <summary>
    /// Experience event types (Combat, Trade, Betrayal, Miracle, Spell, etc.)
    /// </summary>
    public enum ExperienceType : ushort
    {
        None = 0,
        Combat = 1,
        Trade = 2,
        Betrayal = 3,
        Miracle = 4,
        Spell = 5,
        Ambush = 6,
        Help = 7,
        Social = 8,
        Custom0 = 240
    }

    /// <summary>
    /// Raw experience event stored in buffer for processing.
    /// Stores what happened, who caused it, where, and the outcome.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct ExperienceEvent : IBufferElementData
    {
        /// <summary>Type of experience (Combat, Trade, Betrayal, etc.)</summary>
        public ExperienceType Type;

        /// <summary>Entity that caused this experience (attacker, trader, etc.)</summary>
        public Entity Source;

        /// <summary>Context entity (village, fleet, location)</summary>
        public Entity Context;

        /// <summary>Outcome: +1 success, -1 failure, 0 neutral</summary>
        public float Outcome;

        /// <summary>Culture/faction ID of the source</summary>
        public ushort CultureId;

        /// <summary>Tick when this experience occurred</summary>
        public uint Tick;
    }

    /// <summary>
    /// Memory profile controlling how an entity learns and retains experiences.
    /// </summary>
    public struct MemoryProfile : IComponentData
    {
        /// <summary>Learning rate multiplier (scaled by wisdom/intelligence)</summary>
        public float LearningRate;

        /// <summary>Retention factor (0-1): how long memories last (memory.Value *= Retention per tick)</summary>
        public float Retention;

        /// <summary>Base predisposition toward Source type (bias accumulator)</summary>
        public float Bias;

        /// <summary>Last tick when memory was updated</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Compressed memory histogram entry for aggregate statistics.
    /// Used to compress older memories into per-culture, per-tactic aggregates.
    /// </summary>
    [InternalBufferCapacity(32)]
    public struct ExperienceHistogram : IBufferElementData
    {
        /// <summary>Culture ID this histogram represents</summary>
        public ushort CultureId;

        /// <summary>Experience type this histogram represents</summary>
        public ExperienceType Type;

        /// <summary>Average outcome for this culture/type combination</summary>
        public float AverageOutcome;

        /// <summary>Total count of experiences in this histogram</summary>
        public int Count;

        /// <summary>Last tick when histogram was updated</summary>
        public uint LastUpdateTick;
    }
}

