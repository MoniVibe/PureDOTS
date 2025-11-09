using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct HaulingLoopSystem : ISystem
    {
        private EntityQuery _pileQuery;
        private EntityQuery _storehouseQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HaulingLoopState>();
            _pileQuery = state.GetEntityQuery(ComponentType.ReadWrite<ResourcePile>(), ComponentType.ReadOnly<ResourcePileMeta>());
            _storehouseQuery = state.GetEntityQuery(ComponentType.ReadWrite<StorehouseInventory>());
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

            foreach (var (loopStateRW, configRO) in SystemAPI
                         .Query<RefRW<HaulingLoopState>, RefRO<HaulingLoopConfig>>())
            {
                ref var loopState = ref loopStateRW.ValueRW;
                var config = configRO.ValueRO;

                switch (loopState.Phase)
                {
                    case HaulingLoopPhase.Idle:
                        loopState.Phase = HaulingLoopPhase.TravellingToPickup;
                        loopState.PhaseTimer = 1f; // TODO: replace with nav distance
                        break;

                    case HaulingLoopPhase.TravellingToPickup:
                        loopState.PhaseTimer -= deltaTime;
                        if (loopState.PhaseTimer <= 0f)
                        {
                            loopState.Phase = HaulingLoopPhase.Loading;
                        }
                        break;

                    case HaulingLoopPhase.Loading:
                        if (piles.Length == 0)
                        {
                            break;
                        }
                        var needed = config.MaxCargo - loopState.CurrentCargo;
                        if (needed <= 0.01f)
                        {
                            loopState.Phase = HaulingLoopPhase.TravellingToDropoff;
                            loopState.PhaseTimer = 1f;
                            break;
                        }

                        for (int i = 0; i < piles.Length && needed > 0.01f; i++)
                        {
                            if (pileData[i].Amount <= 0f)
                            {
                                continue;
                            }

                            var take = math.min(needed, math.min(config.LoadRatePerSecond * deltaTime, pileData[i].Amount));
                            pileData[i].Amount -= take;
                            loopState.CurrentCargo += take;
                            needed -= take;
                        }

                        if (loopState.CurrentCargo >= config.MaxCargo - 0.01f)
                        {
                            loopState.Phase = HaulingLoopPhase.TravellingToDropoff;
                            loopState.PhaseTimer = 1f;
                        }
                        break;

                    case HaulingLoopPhase.TravellingToDropoff:
                        loopState.PhaseTimer -= deltaTime;
                        if (loopState.PhaseTimer <= 0f)
                        {
                            loopState.Phase = HaulingLoopPhase.Unloading;
                        }
                        break;

                    case HaulingLoopPhase.Unloading:
                        if (storehouses.Length == 0)
                        {
                            loopState.CurrentCargo = 0f;
                            loopState.Phase = HaulingLoopPhase.Idle;
                            break;
                        }

                        var unload = math.min(loopState.CurrentCargo, config.UnloadRatePerSecond * deltaTime);
                        loopState.CurrentCargo -= unload;
                        var inventory = storehouseInventory[0];
                        inventory.TotalStored += unload;
                        storehouseInventory[0] = inventory;
                        if (loopState.CurrentCargo <= 0.01f)
                        {
                            loopState.Phase = HaulingLoopPhase.Idle;
                            loopState.CurrentCargo = 0f;
                            loopState.PhaseTimer = 0f;
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

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
