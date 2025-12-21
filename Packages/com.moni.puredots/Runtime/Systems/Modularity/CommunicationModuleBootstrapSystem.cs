using PureDOTS.Runtime.Communication;
using PureDOTS.Runtime.Modularity;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Modularity
{
    /// <summary>
    /// Ensures entities tagged with <see cref="CommunicationModuleTag"/> have comm endpoint state and buffers.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial struct CommunicationModuleBootstrapSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<CommunicationModuleTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_query.IsEmptyIgnoreFilter)
            {
                return;
            }

            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<CommunicationModuleTag>>().WithEntityAccess())
            {
                if (!em.HasComponent<CommEndpoint>(entity))
                {
                    ecb.AddComponent(entity, CommEndpoint.Default);
                }

                if (!em.HasBuffer<CommAttempt>(entity))
                {
                    ecb.AddBuffer<CommAttempt>(entity);
                }

                if (!em.HasBuffer<CommReceipt>(entity))
                {
                    ecb.AddBuffer<CommReceipt>(entity);
                }
            }

            ecb.Playback(em);
        }
    }
}
