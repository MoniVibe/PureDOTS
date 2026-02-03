using PureDOTS.Runtime.Knowledge;
using PureDOTS.Runtime.Modularity;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Modularity
{
    /// <summary>
    /// Ensures entities tagged with <see cref="GroupKnowledgeModuleTag"/> have knowledge fact buffers.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial struct GroupKnowledgeFactBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GroupKnowledgeModuleTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<GroupKnowledgeModuleTag>>().WithEntityAccess())
            {
                if (!em.HasBuffer<KnowledgeFact>(entity))
                {
                    ecb.AddBuffer<KnowledgeFact>(entity);
                }

                if (!em.HasBuffer<KnowledgeFactRequest>(entity))
                {
                    ecb.AddBuffer<KnowledgeFactRequest>(entity);
                }
            }

            ecb.Playback(em);
        }
    }
}
