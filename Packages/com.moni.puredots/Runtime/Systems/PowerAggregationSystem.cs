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
    /// Aggregates power production/consumption per zone, pushing deltas upward once per tick.
    /// Implements hierarchical aggregation: Entity → Zone → Region → Global.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PowerSystemGroup))]
    public partial struct PowerAggregationSystem : ISystem
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

            // Aggregate entity power into zones
            var zoneStates = new NativeHashMap<int, PowerZoneState>(64, Allocator.TempJob);
            
            foreach (var (entityPower, zone, entity) in SystemAPI.Query<
                         RefRO<EntityPower>,
                         RefRO<PowerZone>>()
                         .WithEntityAccess())
            {
                var power = entityPower.ValueRO;
                var zoneId = zone.ValueRO.ZoneId;

                if (!zoneStates.TryGetValue(zoneId, out var zoneState))
                {
                    zoneState = new PowerZoneState
                    {
                        TotalProduction = 0,
                        TotalConsumption = 0,
                        NetPower = 0,
                        LastUpdateTick = currentTick
                    };
                }

                zoneState.TotalProduction = FixedPointMath.Add(zoneState.TotalProduction, power.ProductionRate);
                zoneState.TotalConsumption = FixedPointMath.Add(zoneState.TotalConsumption, power.ConsumptionRate);
                zoneState.NetPower = FixedPointMath.Subtract(zoneState.TotalProduction, zoneState.TotalConsumption);
                zoneState.LastUpdateTick = currentTick;

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
                foreach (var (zone, entity) in SystemAPI.Query<RefRO<PowerZone>>()
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
                    ecb.AddComponent(zoneEntity, new PowerZone { ZoneId = zoneId });
                }

                ecb.SetComponent(zoneEntity, zoneState);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            zoneStates.Dispose();
        }
    }
}

