using PureDOTS.Environment;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Environment
{
    /// <summary>
    /// Biome ID component. Identifies a biome entity or resolved biome type.
    /// </summary>
    public struct BiomeId : IComponentData
    {
        public FixedString32Bytes Value;
    }

    /// <summary>
    /// Biome specification reference. Points to the biome catalog blob.
    /// </summary>
    public struct BiomeSpecRef : IComponentData
    {
        public BlobAssetReference<BiomeSpecBlob> Blob;
    }

    /// <summary>
    /// Biome resolution result. Stores the resolved biome type and score.
    /// </summary>
    public struct BiomeResolved : IComponentData
    {
        public BiomeType BiomeType;
        public float Score; // Resolution score (0-1)
    }
}

