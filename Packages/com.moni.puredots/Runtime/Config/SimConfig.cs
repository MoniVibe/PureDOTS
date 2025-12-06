using System;
using UnityEngine;

namespace PureDOTS.Config
{
    /// <summary>
    /// Simulation configuration loaded from JSON.
    /// </summary>
    [Serializable]
    public struct SimConfig
    {
        public float FixedDeltaTime;
        public float MindTickRate;
        public float AggregateTickRate;
        public int RewindBufferSeconds;

        public static SimConfig Default => new SimConfig
        {
            FixedDeltaTime = 0.0166667f,
            MindTickRate = 1.0f,
            AggregateTickRate = 5.0f,
            RewindBufferSeconds = 300
        };
    }
}

