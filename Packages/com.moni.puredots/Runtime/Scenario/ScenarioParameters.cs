using Unity.Entities;

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// Parameters for procedural scenario generation.
    /// Scaling parameters from 10K to 1M entities.
    /// </summary>
    public struct ScenarioParameters : IComponentData
    {
        /// <summary>
        /// Target entity count.
        /// </summary>
        public int EntityCount;

        /// <summary>
        /// Number of terrain chunks to generate.
        /// </summary>
        public int TerrainChunks;

        /// <summary>
        /// Number of fleets to generate.
        /// </summary>
        public int FleetCount;

        /// <summary>
        /// Number of villagers to generate.
        /// </summary>
        public int VillagerCount;

        /// <summary>
        /// Number of miracles to generate.
        /// </summary>
        public int MiracleCount;

        /// <summary>
        /// Seed for deterministic generation.
        /// </summary>
        public uint Seed;

        public ScenarioParameters(int entityCount, uint seed = 0)
        {
            EntityCount = entityCount;
            Seed = seed;
            // Distribute entities across categories
            TerrainChunks = entityCount / 100;
            FleetCount = entityCount / 1000;
            VillagerCount = entityCount / 10;
            MiracleCount = entityCount / 10000;
        }
    }
}

