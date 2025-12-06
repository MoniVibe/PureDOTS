using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Configuration singleton for modifier system behavior.
    /// </summary>
    public struct ModifierConfig : IComponentData
    {
        /// <summary>
        /// Modifier tick interval in seconds (default 0.25s = 4Hz).
        /// </summary>
        public float ModifierTickInterval;

        /// <summary>
        /// Batch size for processing modifiers (default 1000).
        /// </summary>
        public int ModifierBatchSize;

        /// <summary>
        /// Cold path update interval in ticks (default 15 = ~0.25s at 60Hz).
        /// </summary>
        public uint ColdPathUpdateInterval;

        public static ModifierConfig Default => new ModifierConfig
        {
            ModifierTickInterval = 0.25f,
            ModifierBatchSize = 1000,
            ColdPathUpdateInterval = 15
        };
    }
}

