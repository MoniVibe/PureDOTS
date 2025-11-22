using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Scenarios
{
    /// <summary>
    /// Scenario metadata injected into worlds spun up by the ScenarioRunner executor.
    /// </summary>
    public struct ScenarioInfo : IComponentData
    {
        public FixedString64Bytes ScenarioId;
        public uint Seed;
        public int RunTicks;
    }

    /// <summary>
    /// Scenario entity count request (as authored in scenario JSON).
    /// Consumers (game projects) can read this buffer to seed spawns.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct ScenarioEntityCountElement : IBufferElementData
    {
        public FixedString64Bytes RegistryId;
        public int Count;
    }
}
