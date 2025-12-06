using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// BlobAsset structure for perception profiles.
    /// Stores fusion profiles for different agent types.
    /// </summary>
    public struct PerceptionProfileBlob
    {
        public BlobString ProfileId;        // Profile identifier
        public float VisionWeight;          // Weight for vision sensor
        public float SoundWeight;           // Weight for sound sensor
        public float SmellWeight;           // Weight for smell sensor
        public float RadarWeight;           // Weight for radar sensor
        public float SmellBias;             // Bias towards smell detection
        public float RadarTrust;             // Trust level for radar
        public float FusionThreshold;       // Minimum confidence for fusion
    }

    /// <summary>
    /// Catalog of perception profiles.
    /// </summary>
    public struct PerceptionProfileCatalogBlob
    {
        public BlobArray<PerceptionProfileBlob> Profiles;
    }
}

