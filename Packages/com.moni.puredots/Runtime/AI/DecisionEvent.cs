using Unity.Entities;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Records an AI decision event for introspection, analytics, and replay scrubbing.
    /// </summary>
    public struct DecisionEvent
    {
        /// <summary>
        /// Agent identifier (GUID or entity index).
        /// </summary>
        public ulong Agent;

        /// <summary>
        /// Decision type (enum value as byte).
        /// </summary>
        public byte Type;

        /// <summary>
        /// Utility score of the decision.
        /// </summary>
        public float Utility;

        /// <summary>
        /// Tick when the decision was made.
        /// </summary>
        public uint Tick;

        /// <summary>
        /// Additional context data (64 bytes for extensibility).
        /// </summary>
        public Unity.Collections.FixedBytes64 Context;

        public DecisionEvent(ulong agent, byte type, float utility, uint tick)
        {
            Agent = agent;
            Type = type;
            Utility = utility;
            Tick = tick;
            Context = default;
        }
    }
}

