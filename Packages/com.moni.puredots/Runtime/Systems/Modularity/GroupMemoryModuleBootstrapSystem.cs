using PureDOTS.Runtime.Memory;
using PureDOTS.Runtime.Modularity;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Modularity
{
    /// <summary>
    /// Ensures entities tagged with <see cref="GroupMemoryModuleTag"/> have memory buffers.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial struct GroupMemoryModuleBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GroupMemoryModuleTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<GroupMemoryModuleTag>>().WithEntityAccess())
            {
                if (!em.HasBuffer<MemoryEntry>(entity))
                {
                    ecb.AddBuffer<MemoryEntry>(entity);
                }

                if (!em.HasBuffer<MemoryAddRequest>(entity))
                {
                    ecb.AddBuffer<MemoryAddRequest>(entity);
                }
            }

            ecb.Playback(em);
        }
    }
}
