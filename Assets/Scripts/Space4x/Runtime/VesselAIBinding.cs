using Space4X.Runtime;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Runtime
{
    /// <summary>
    /// Binding that maps shared AI action indices to vessel goals.
    /// Similar to VillagerAIUtilityBinding but for vessels.
    /// </summary>
    public struct VesselAIUtilityBinding : IComponentData
    {
        public FixedList32Bytes<VesselAIState.Goal> Goals;
    }
}

