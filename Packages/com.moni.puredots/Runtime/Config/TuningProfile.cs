using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Config
{
    /// <summary>
    /// BlobAsset containing tuning parameters for a specific domain (Physics, AI, Economy, etc.).
    /// Loaded from JSON and supports hot-reload.
    /// </summary>
    public struct TuningProfileBlob
    {
        public BlobString ProfileName;
        public BlobString Domain; // "Physics", "AI", "Economy", etc.
        public BlobArray<TuningParameter> Parameters;
    }

    /// <summary>
    /// A single tuning parameter with name and value.
    /// </summary>
    public struct TuningParameter
    {
        public BlobString Name;
        public float Value;
        public byte Type; // 0=float, 1=int, 2=bool
    }

    /// <summary>
    /// Component referencing a tuning profile BlobAsset.
    /// </summary>
    public struct TuningProfileRef : IComponentData
    {
        public BlobAssetReference<TuningProfileBlob> Profile;
    }

    /// <summary>
    /// Singleton tracking active tuning profiles.
    /// </summary>
    public struct TuningProfileMetadata : IComponentData
    {
        public FixedString64Bytes ActivePhysicsProfile;
        public FixedString64Bytes ActiveAIProfile;
        public FixedString64Bytes ActiveEconomyProfile;
        public uint Version; // Incremented on profile change
    }
}

