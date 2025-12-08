using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Singleton component that stores world metrics (entity counts, frame time) for debug display.
    /// Updated once per second by WorldMetricsSystem.
    /// </summary>
    public struct WorldMetrics : IComponentData
    {
        public int VillagerCount;
        public int ResourceCount;
        public int StorehouseCount;
        public int MiningVesselCount;
        public int CarrierCount;
        public int AsteroidCount;
        public float AverageFrameTimeMs;
        public uint LastUpdateTick;
    }
}


