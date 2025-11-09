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
    public partial struct HaulingLoopSystem : ISystem
    {
        private EntityQuery _pileQuery;
        private EntityQuery _storehouseQuery;
        private ComponentLookup<ResourcePile> _pileLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<StorehouseInventory> _storehouseInventoryLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HaulingLoopState>();
            _pileQuery = state.GetEntityQuery(ComponentType.ReadWrite<ResourcePile>(), ComponentType.ReadOnly<ResourcePileMeta>());
            _storehouseQuery = state.GetEntityQuery(ComponentType.ReadWrite<StorehouseInventory>());
            _pileLookup = state.GetComponentLookup<ResourcePile>(false);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _storehouseInventoryLookup = state.GetComponentLookup<StorehouseInventory>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var piles = _pileQuery.ToEntityArray(Allocator.Temp);
            var pileData = _pileQuery.ToComponentDataArray<ResourcePile>(Allocator.Temp);
            var pileMeta = _pileQuery.ToComponentDataArray<ResourcePileMeta>(Allocator.Temp);
            var storehouses = _storehouseQuery.ToEntityArray(Allocator.Temp);
            var storehouseInventory = _storehouseQuery.ToComponentDataArray<StorehouseInventory>(Allocator.Temp);

            _pileLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _storehouseInventoryLookup.Update(ref state);

            foreach (var (loopStateRW, configRO, jobRW, transform) in SystemAPI
                         .Query<RefRW<HaulingLoopState>, RefRO<HaulingLoopConfig>, RefRW<HaulingJob>, RefRO<LocalTransform>>())
            {
                ref var loopState = ref loopStateRW.ValueRW;
                var config = configRO.ValueRO;
                ref var job = ref jobRW.ValueRW;
                var haulerPosition = transform.ValueRO.Position;

                switch (loopState.Phase)
                {
                    case HaulingLoopPhase.Idle:
                        if (job.SourceEntity == Entity.Null || !_pileLookup.HasComponent(job.SourceEntity))
                        {
                            ClearJob(ref job);
                            break;
                        }

                        loopState.Phase = HaulingLoopPhase.TravellingToPickup;
                        loopState.PhaseTimer = ComputeTravelTime(haulerPosition, _pileLookup[job.SourceEntity].Position, config.TravelSpeedMetersPerSecond);
                        break;

                    case HaulingLoopPhase.TravellingToPickup:
                        if (job.SourceEntity == Entity.Null || !_pileLookup.HasComponent(job.SourceEntity))
                        {
                            loopState.Phase = HaulingLoopPhase.Idle;
                            ClearJob(ref job);
                            break;
                        }
                        loopState.PhaseTimer -= deltaTime;
                        if (loopState.PhaseTimer <= 0f)
                        {
                            loopState.Phase = HaulingLoopPhase.Loading;
                        }
                        break;

                    case HaulingLoopPhase.Loading:
                        if (job.SourceEntity == Entity.Null || !_pileLookup.HasComponent(job.SourceEntity))
                        {
                            loopState.Phase = HaulingLoopPhase.Idle;
                            ClearJob(ref job);
                            break;
                        }
                        var needed = config.MaxCargo - loopState.CurrentCargo;
                        if (needed <= 0.01f)
                        {
                            loopState.Phase = HaulingLoopPhase.TravellingToDropoff;
                            loopState.PhaseTimer = ComputeTravelTime(haulerPosition, DestinationPosition(job), config.TravelSpeedMetersPerSecond);
                            break;
                        }

                        var pile = _pileLookup[job.SourceEntity];
                        var take = math.min(needed, math.min(config.LoadRatePerSecond * deltaTime, pile.Amount));
                        pile.Amount -= take;
                        loopState.CurrentCargo += take;
                        needed -= take;
                        _pileLookup[job.SourceEntity] = pile;

                        if (loopState.CurrentCargo >= config.MaxCargo - 0.01f)
                        {
                            loopState.Phase = HaulingLoopPhase.TravellingToDropoff;
                            loopState.PhaseTimer = ComputeTravelTime(haulerPosition, DestinationPosition(job), config.TravelSpeedMetersPerSecond);
                        }
                        break;

                    case HaulingLoopPhase.TravellingToDropoff:
                        if (job.DestinationEntity == Entity.Null || !_storehouseInventoryLookup.HasComponent(job.DestinationEntity))
                        {
                            loopState.Phase = HaulingLoopPhase.Idle;
                            ClearJob(ref job);
                            break;
                        }
                        loopState.PhaseTimer -= deltaTime;
                        if (loopState.PhaseTimer <= 0f)
                        {
                            loopState.Phase = HaulingLoopPhase.Unloading;
                        }
                        break;

                    case HaulingLoopPhase.Unloading:
                        if (job.DestinationEntity == Entity.Null || !_storehouseInventoryLookup.HasComponent(job.DestinationEntity))
                        {
                            loopState.CurrentCargo = 0f;
                            loopState.Phase = HaulingLoopPhase.Idle;
                            ClearJob(ref job);
                            break;
                        }

                        var unload = math.min(loopState.CurrentCargo, config.UnloadRatePerSecond * deltaTime);
                        loopState.CurrentCargo -= unload;
                        var inventory = _storehouseInventoryLookup[job.DestinationEntity];
                        inventory.TotalStored += unload;
                        _storehouseInventoryLookup[job.DestinationEntity] = inventory;
                        if (loopState.CurrentCargo <= 0.01f)
                        {
                            loopState.Phase = HaulingLoopPhase.Idle;
                            loopState.CurrentCargo = 0f;
                            loopState.PhaseTimer = 0f;
                            ClearJob(ref job);
                        }
                        break;
            }
            }

            if (piles.Length > 0)
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                for (int i = 0; i < piles.Length; i++)
                {
                    if (pileData[i].Amount <= 0.01f)
                    {
                        ecb.DestroyEntity(piles[i]);
                    }
                    else
                    {
                        ecb.SetComponent(piles[i], pileData[i]);
                    }
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }

            piles.Dispose();
            pileData.Dispose();
            pileMeta.Dispose();
            storehouses.Dispose();
            storehouseInventory.Dispose();
        }

        private static void ClearJob(ref HaulingJob job)
        {
            job.SourceEntity = Entity.Null;
            job.DestinationEntity = Entity.Null;
            job.RequestedAmount = 0f;
            job.Urgency = 0f;
            job.ResourceValue = 0f;
        }

        private static float ComputeTravelTime(float3 from, float3 to, float speed)
        {
            if (speed <= 0f)
            {
                return 1f;
            }
            var distance = math.length(to - from);
            return math.max(0.1f, distance / speed);
        }

        private float3 DestinationPosition(HaulingJob job)
        {
            if (job.DestinationEntity != Entity.Null && _transformLookup.HasComponent(job.DestinationEntity))
            {
                return _transformLookup[job.DestinationEntity].Position;
            }
            return float3.zero;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
