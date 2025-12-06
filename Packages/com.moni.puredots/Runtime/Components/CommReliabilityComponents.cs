using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Communication reliability component for per-agent/channel reliability.
    /// Reliability value ranges from 0-1 (0 = complete failure, 1 = perfect).
    /// </summary>
    public struct CommReliability : IComponentData
    {
        public float Reliability;           // Per-agent/channel reliability (0-1)
        public float LossRate;              // Message loss rate (0-1)
        public float Jitter;                // Message jitter in seconds
        public float SignalDecay;           // Signal decay rate (0-1)
        public uint LastUpdateTick;         // When reliability was last updated
    }

    /// <summary>
    /// Noise profile containing loss rate, jitter, and signal decay parameters.
    /// Used for stress-testing coordination stability.
    /// </summary>
    public struct NoiseProfile : IComponentData
    {
        public float LossRate;              // Base loss rate (0-1)
        public float JitterMean;            // Mean jitter in seconds
        public float JitterStdDev;          // Standard deviation of jitter
        public float SignalDecayRate;       // Signal decay rate per unit distance (0-1)
        public float MaxDistance;          // Maximum communication distance
        public uint ProfileId;              // Unique identifier for this profile
    }

    /// <summary>
    /// Communication channel state tracking message loss and latency.
    /// </summary>
    public struct CommChannelState : IComponentData
    {
        public int MessagesSent;            // Total messages sent
        public int MessagesReceived;       // Total messages received
        public int MessagesLost;            // Total messages lost
        public float AverageLatency;        // Average message latency in seconds
        public float MaxLatency;            // Maximum observed latency
        public uint LastMessageTick;        // Tick of last message
    }
}

