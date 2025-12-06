using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Environment sample component updated in parallel via chunk lookup.
    /// Feeds into AI desirability curves (comfort, growth, morale).
    /// </summary>
    public struct EnvironmentSample : IComponentData
    {
        public half Temperature;
        public half Moisture;
        public half Oxygen;
        public half Light;
        public half SoilFertility;
        public uint LastSampleTick;
    }
}

