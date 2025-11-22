using PureDOTS.Runtime.Economy;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Economy
{
    /// <summary>
    /// Ensures batch pricing/config data exists alongside batch inventory.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BatchInventoryBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BatchInventory>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (inventory, entity) in SystemAPI.Query<RefRO<BatchInventory>>().WithNone<BatchPricingState>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new BatchPricingState
                {
                    LastPriceMultiplier = 1f,
                    LastUpdateTick = 0
                });
            }

            if (!SystemAPI.HasSingleton<BatchPricingConfig>())
            {
                var cfgEntity = ecb.CreateEntity();
                ecb.AddComponent(cfgEntity, BatchPricingConfig.CreateDefault());
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
