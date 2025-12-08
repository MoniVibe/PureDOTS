using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Registry
{
    /// <summary>
    /// Tracks unique archetype hashes and updates ArchetypeRegistry metrics.
    /// Note: This does not change archetype creation; it only counts/versions for reporting.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct ArchetypeRegistrySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Ensure registry singleton exists
            if (!SystemAPI.HasSingleton<ArchetypeRegistry>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new ArchetypeRegistry
                {
                    Version = 0,
                    ArchetypeCount = 0,
                    FragmentationScore = 0
                });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registryEntity = SystemAPI.GetSingletonEntity<ArchetypeRegistry>();
            var registry = SystemAPI.GetComponentRW<ArchetypeRegistry>(registryEntity);

            var unique = new NativeParallelHashSet<ulong>(128, state.WorldUpdateAllocator);
            foreach (var hash in SystemAPI.Query<RefRO<ArchetypeHash>>())
            {
                unique.Add(hash.ValueRO.ComponentBitmask);
            }

            registry.ValueRW.ArchetypeCount = (uint)unique.Count();
            registry.ValueRW.FragmentationScore = registry.ValueRW.ArchetypeCount; // placeholder: lower is better
            registry.ValueRW.Version++;
        }
    }
}
