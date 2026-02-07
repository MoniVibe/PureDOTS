using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Individual
{
    /// <summary>
    /// Placeholder bootstrap system for individual anatomy limbs.
    /// Allows downstream systems to order against a stable anchor.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AnatomyLimbBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
    }
}
