using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    public enum MiracleType : byte
    {
        Rain = 0
    }

    public struct RainMiracleConfig : IComponentData
    {
        public Entity RainCloudPrefab;
        public int CloudCount;
        public float SpawnRadius;
        public float SpawnHeightOffset;
        public float SpawnSpreadAngle;
        public uint Seed;
    }

    public struct RainMiracleCommandQueue : IComponentData { }

    public struct RainMiracleCommand : IBufferElementData
    {
        public float3 Center;
        public int CloudCount;
        public float Radius;
        public float HeightOffset;
        public Entity RainCloudPrefab;
        public uint Seed;
    }
}
