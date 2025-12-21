using PureDOTS.Runtime.Communication;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Communication
{
    /// <summary>
    /// Ensures comm endpoints have the required buffers for attempts and receipts.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PerceptionSystemGroup), OrderFirst = true)]
    public partial struct CommunicationEndpointBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CommEndpoint>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (endpoint, entity) in SystemAPI.Query<RefRO<CommEndpoint>>().WithEntityAccess())
            {
                if (!state.EntityManager.HasBuffer<CommAttempt>(entity))
                {
                    ecb.AddBuffer<CommAttempt>(entity);
                }

                if (!state.EntityManager.HasBuffer<CommReceipt>(entity))
                {
                    ecb.AddBuffer<CommReceipt>(entity);
                }
            }
        }
    }
}
