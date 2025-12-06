using PureDOTS.Environment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Biome
{
    /// <summary>
    /// Biome entity component tracking biome type and location.
    /// </summary>
    public struct BiomeEntity : IComponentData
    {
        public BiomeType Type;
        public int2 BiomeCoord;
    }

    /// <summary>
    /// Buffer storing chunk coordinates owned by this biome.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct BiomeChunkBuffer : IBufferElementData
    {
        public int2 ChunkCoord;
        public BlobAssetReference<ClimateChunkBlob> ChunkBlob;
    }

    /// <summary>
    /// Telemetry data summarizing biome state for other systems.
    /// </summary>
    public struct BiomeTelemetry : IComponentData
    {
        public float AverageTemperature;
        public float AverageMoisture;
        public int FloraSampleCount;
        public float WeatherIntensity;
        public uint LastUpdateTick;
    }
}

