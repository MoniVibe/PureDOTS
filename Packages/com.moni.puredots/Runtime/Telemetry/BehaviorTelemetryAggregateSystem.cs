using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Telemetry
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BehaviorTelemetryAggregateSystem : ISystem
    {
        private ComponentLookup<BehaviorTelemetryConfig> _configLookup;
        private BufferLookup<BehaviorTelemetryRecord> _recordLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _configLookup = state.GetComponentLookup<BehaviorTelemetryConfig>(true);
            _recordLookup = state.GetBufferLookup<BehaviorTelemetryRecord>();

            state.RequireForUpdate<BehaviorTelemetryConfig>();
            state.RequireForUpdate<BehaviorTelemetryState>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var configEntity = SystemAPI.GetSingletonEntity<BehaviorTelemetryConfig>();
            var config = _configLookup[configEntity];
            var cadence = math.max(1, config.AggregateCadenceTicks);
            if (timeState.Tick % (uint)cadence != 0)
            {
                return;
            }

            _recordLookup.Update(ref state);
            var telemetryEntity = SystemAPI.GetSingletonEntity<BehaviorTelemetryState>();
            var buffer = _recordLookup[telemetryEntity];

            AggregateHazard(ref state, buffer, timeState.Tick);
            AggregateGather(ref state, buffer, timeState.Tick);
        }

        private void AggregateHazard(ref SystemState state, DynamicBuffer<BehaviorTelemetryRecord> buffer, uint tick)
        {
            foreach (var telemetry in SystemAPI.Query<RefRW<HazardDodgeTelemetry>>())
            {
                var value = telemetry.ValueRW;
                if (value.RaycastHitsInterval > 0)
                {
                    buffer.Add(new BehaviorTelemetryRecord
                    {
                        Tick = tick,
                        Behavior = BehaviorId.HazardDodge,
                        Kind = BehaviorTelemetryRecordKind.Metric,
                        MetricOrInvariantId = (ushort)BehaviorMetricId.HazardRaycastHits,
                        ValueA = value.RaycastHitsInterval,
                        Passed = 1
                    });
                }

                if (value.AvoidanceTransitionsInterval > 0)
                {
                    buffer.Add(new BehaviorTelemetryRecord
                    {
                        Tick = tick,
                        Behavior = BehaviorId.HazardDodge,
                        Kind = BehaviorTelemetryRecordKind.Metric,
                        MetricOrInvariantId = (ushort)BehaviorMetricId.HazardAvoidanceTransitions,
                        ValueA = value.AvoidanceTransitionsInterval,
                        Passed = 1
                    });
                }

                buffer.Add(new BehaviorTelemetryRecord
                {
                    Tick = tick,
                    Behavior = BehaviorId.HazardDodge,
                    Kind = BehaviorTelemetryRecordKind.Metric,
                    MetricOrInvariantId = (ushort)BehaviorMetricId.HazardDodgeDistanceMm,
                    ValueA = value.DodgeDistanceMmInterval,
                    Passed = 1
                });

                telemetry.ValueRW = new HazardDodgeTelemetry
                {
                    WasAvoidingLastTick = value.WasAvoidingLastTick
                };
            }
        }

        private void AggregateGather(ref SystemState state, DynamicBuffer<BehaviorTelemetryRecord> buffer, uint tick)
        {
            foreach (var telemetry in SystemAPI.Query<RefRW<GatherDeliverTelemetry>>())
            {
                var value = telemetry.ValueRW;
                buffer.Add(new BehaviorTelemetryRecord
                {
                    Tick = tick,
                    Behavior = BehaviorId.GatherDeliver,
                    Kind = BehaviorTelemetryRecordKind.Metric,
                    MetricOrInvariantId = (ushort)BehaviorMetricId.GatherMinedMilli,
                    ValueA = value.MinedAmountMilliInterval,
                    Passed = 1
                });

                buffer.Add(new BehaviorTelemetryRecord
                {
                    Tick = tick,
                    Behavior = BehaviorId.GatherDeliver,
                    Kind = BehaviorTelemetryRecordKind.Metric,
                    MetricOrInvariantId = (ushort)BehaviorMetricId.GatherDepositedMilli,
                    ValueA = value.DepositedAmountMilliInterval,
                    Passed = 1
                });

                buffer.Add(new BehaviorTelemetryRecord
                {
                    Tick = tick,
                    Behavior = BehaviorId.GatherDeliver,
                    Kind = BehaviorTelemetryRecordKind.Metric,
                    MetricOrInvariantId = (ushort)BehaviorMetricId.GatherCarrierCargoMilli,
                    ValueA = value.CarrierCargoMilliSnapshot,
                    Passed = 1
                });

                telemetry.ValueRW.MinedAmountMilliInterval = 0;
                telemetry.ValueRW.DepositedAmountMilliInterval = 0;
                telemetry.ValueRW.StuckTicksInterval = 0;
            }
        }
    }
}
