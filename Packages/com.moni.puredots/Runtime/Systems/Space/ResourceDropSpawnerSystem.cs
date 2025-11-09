using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DropOnlyHarvestDepositSystem))]
    public partial struct ResourceDropSpawnerSystem : ISystem
    {
        private EntityArchetype _pileArchetype;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ResourceDropConfig>();
            _pileArchetype = state.EntityManager.CreateArchetype(
                typeof(ResourcePile),
                typeof(ResourcePileMeta));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (loopState, dropConfig, transform) in SystemAPI
                         .Query<RefRW<MiningLoopState>, RefRW<ResourceDropConfig>, RefRO<LocalTransform>>()
                         .WithAll<DropOnlyHarvesterTag>())
            {
                if (loopState.ValueRO.Phase != MiningLoopPhase.Harvesting)
                {
                    continue;
                }

                dropConfig.ValueRW.TimeSinceLastDrop += SystemAPI.Time.DeltaTime;
                if (dropConfig.ValueRW.TimeSinceLastDrop < dropConfig.ValueRO.DropIntervalSeconds)
                {
                    continue;
                }

                dropConfig.ValueRW.TimeSinceLastDrop = 0f;
                var pileEntity = ecb.CreateEntity(_pileArchetype);
                var position = transform.ValueRO.Position;
                var jitterDir = math.normalize(new float3(Noise(position.xy * 1.1f), Noise(position.yz * 1.3f), Noise(position.xz * 1.7f)));
                var jitter = jitterDir * dropConfig.ValueRO.DropRadiusMeters;
                var amount = math.min(dropConfig.ValueRO.MaxStack, loopState.ValueRO.CurrentCargo + dropConfig.ValueRO.DropIntervalSeconds * 0.1f);
                ecb.SetComponent(pileEntity, new ResourcePile
                {
                    Amount = amount,
                    Position = position + jitter
                });
                ecb.SetComponent(pileEntity, new ResourcePileMeta
                {
                    ResourceTypeId = dropConfig.ValueRO.ResourceTypeId,
                    DecaySeconds = dropConfig.ValueRO.DecaySeconds,
                    MaxCapacity = dropConfig.ValueRO.MaxStack
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private static float Noise(float2 v)
        {
            return (math.hash(v) & 0xFFFF) / 65535f - 0.5f;
        }
    }
}
