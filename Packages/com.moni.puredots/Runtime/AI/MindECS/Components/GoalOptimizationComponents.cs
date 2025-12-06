using Unity.Collections;
using Unity.Entities;
using PureDOTS.Environment;

namespace PureDOTS.AI.MindECS
{
    /// <summary>
    /// Behavior caching component for O(1) lookups of last action per context.
    /// </summary>
    public struct GoalCache : IComponentData
    {
        public FixedString64Bytes LastContextKey;
        public byte LastActionIndex;
        public uint LastEvaluationTick;
    }

    /// <summary>
    /// Spatial desire grid for cell-based desire evaluation.
    /// </summary>
    public struct SpatialDesireGrid : IComponentData
    {
        public EnvironmentGridMetadata Metadata;
        public BlobAssetReference<DesireGridBlob> Blob; // Per-cell desire scores
    }

    /// <summary>
    /// Blob payload for desire grid.
    /// </summary>
    public struct DesireGridBlob
    {
        public BlobArray<float> DesireScores;
    }

    /// <summary>
    /// Environmental preference component for agent-biome interaction.
    /// </summary>
    public struct EnvironmentalPreference : IComponentData
    {
        public half PreferredTemperature;
        public half PreferredMoisture;
        public half PreferredLight;
        public half HumidityPenalty; // Desert fauna
        public half LightPenalty; // Cave flora
    }

    /// <summary>
    /// Ambition goal types for agent cognitive interaction.
    /// </summary>
    public enum AmbitionType : byte
    {
        None = 0,
        ExpandFarm = 1,
        SeekShade = 2,
        Migrate = 3
    }

    /// <summary>
    /// Ambition goal component for agent cognitive interaction with biome.
    /// </summary>
    public struct AmbitionGoal : IComponentData
    {
        public AmbitionType Type;
        public float3 TargetZone;
        public float Priority;
        public uint IssuedTick;
    }
}

