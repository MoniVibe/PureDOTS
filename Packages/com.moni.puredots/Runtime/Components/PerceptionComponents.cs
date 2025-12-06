using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Compressed feature vector output from PerceptionFusionSystem.
    /// Unifies sensor data (vision, sound, smell, radar) into shared semantic buffers.
    /// </summary>
    public struct PerceptionFeatureVector : IBufferElementData
    {
        public float VisionScore;           // Vision-based detection score (0-1)
        public float SoundScore;            // Sound-based detection score (0-1)
        public float SmellScore;            // Smell-based detection score (0-1)
        public float RadarScore;            // Radar-based detection score (0-1)
        public float3 SourcePosition;       // Position of detected source
        public float Confidence;            // Overall confidence (0-1)
        public uint DetectionTick;          // When detection occurred
    }

    /// <summary>
    /// Sensor weights for perception fusion.
    /// Mind ECS interprets features via profile weights ("smell bias", "radar trust").
    /// </summary>
    public struct SensorWeights : IComponentData
    {
        public float VisionWeight;          // Weight for vision sensor (0-1)
        public float SoundWeight;           // Weight for sound sensor (0-1)
        public float SmellWeight;           // Weight for smell sensor (0-1)
        public float RadarWeight;           // Weight for radar sensor (0-1)
        public float SmellBias;             // Bias towards smell detection (0-1)
        public float RadarTrust;             // Trust level for radar (0-1)
    }

    /// <summary>
    /// Perception fusion state tracking fusion operations.
    /// </summary>
    public struct PerceptionFusionState : IComponentData
    {
        public uint LastFusionTick;        // When fusion was last performed
        public int FeatureCount;            // Number of features in current vector
        public float FusionCost;             // CPU cost of last fusion (ms)
    }
}

