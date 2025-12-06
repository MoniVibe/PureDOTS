using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Spatial;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Aggregates resource flows per zone, computing trade routes at zone level.
    /// Implements hierarchical aggregation: Entity → Zone → Region → Global.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ResourceSystemGroup))]
    public partial struct EconomyAggregationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickTimeState.Tick;

            // Aggregate entity economy into zones
            var zoneStates = new NativeHashMap<int, EconomyZoneState>(64, Allocator.TempJob);
            
            foreach (var (entityEcon, zone, entity) in SystemAPI.Query<
                         RefRO<EntityEconomy>,
                         RefRO<EconomyZone>>()
                         .WithEntityAccess())
            {
                var econ = entityEcon.ValueRO;
                var zoneId = zone.ValueRO.ZoneId;

                if (!zoneStates.TryGetValue(zoneId, out var zoneState))
                {
                    zoneState = new EconomyZoneState
                    {
                        LastUpdateTick = currentTick
                    };
                }

                // Aggregate production and consumption per resource type
                for (int i = 0; i < econ.ProductionRates.Length && i < zoneState.ProductionPerType.Length; i++)
                {
                    var prod = zoneState.ProductionPerType[i];
                    zoneState.ProductionPerType[i] = FixedPointMath.Add(prod, econ.ProductionRates[i]);
                }

                for (int i = 0; i < econ.ConsumptionRates.Length && i < zoneState.ConsumptionPerType.Length; i++)
                {
                    var cons = zoneState.ConsumptionPerType[i];
                    zoneState.ConsumptionPerType[i] = FixedPointMath.Add(cons, econ.ConsumptionRates[i]);
                }

                zoneState.LastUpdateTick = currentTick;
                zoneStates[zoneId] = zoneState;
            }

            // Compute trade balance (production - consumption) per zone
            foreach (var kvp in zoneStates)
            {
                var zoneId = kvp.Key;
                var zoneState = kvp.Value;

                for (int i = 0; i < zoneState.ProductionPerType.Length && i < zoneState.ConsumptionPerType.Length && i < zoneState.TradeBalancePerType.Length; i++)
                {
                    var prod = zoneState.ProductionPerType[i];
                    var cons = zoneState.ConsumptionPerType[i];
                    zoneState.TradeBalancePerType[i] = FixedPointMath.Subtract(prod, cons);
                }

                zoneStates[zoneId] = zoneState;
            }

            // Write zone states to zone entities (create if needed)
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            foreach (var kvp in zoneStates)
            {
                var zoneId = kvp.Key;
                var zoneState = kvp.Value;

                // Find or create zone entity
                Entity zoneEntity = Entity.Null;
                foreach (var (zone, entity) in SystemAPI.Query<RefRO<EconomyZone>>()
                             .WithEntityAccess())
                {
                    if (zone.ValueRO.ZoneId == zoneId)
                    {
                        zoneEntity = entity;
                        break;
                    }
                }

                if (zoneEntity == Entity.Null)
                {
                    zoneEntity = state.EntityManager.CreateEntity();
                    ecb.AddComponent(zoneEntity, new EconomyZone { ZoneId = zoneId });
                }

                ecb.SetComponent(zoneEntity, zoneState);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            zoneStates.Dispose();
        }
    }
}

