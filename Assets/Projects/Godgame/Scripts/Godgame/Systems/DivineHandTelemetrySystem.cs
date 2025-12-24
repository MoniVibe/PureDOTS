using Godgame.Runtime;
using Godgame.Systems;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Hand;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Systems
{
    /// <summary>
    /// Emits lightweight telemetry about the current Divine Hand state so downstream HUD/analytics can react without bespoke hooks.
    /// Runs after <see cref="DivineHandSystem"/> inside <see cref="HandSystemGroup"/>.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(HandSystemGroup))]
    [UpdateAfter(typeof(DivineHandSystem))]
    public partial struct DivineHandTelemetrySystem : ISystem
    {
        private EntityQuery _handQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _handQuery = SystemAPI.QueryBuilder()
                .WithAll<PureDOTS.Runtime.Hand.HandState>()
                .WithAll<HandInteractionState>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_handQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            {
                return;
            }

            var accumulator = new NativeReference<HandTelemetryAccumulator>(Allocator.TempJob);
            accumulator.Value = default;

            var sampleJob = new SampleHandStateJob
            {
                Accumulator = accumulator
            };

            var handle = sampleJob.Schedule(_handQuery, state.Dependency);
            state.Dependency = handle;
            state.Dependency.Complete();

            var totals = accumulator.Value;
            accumulator.Dispose();

            WriteTelemetry(ref state, telemetryEntity, in totals);
        }

        private static void WriteTelemetry(ref SystemState state, Entity telemetryEntity, in HandTelemetryAccumulator totals)
        {
            if (totals.TotalHands == 0)
            {
                return;
            }

            var buffer = state.EntityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
            buffer.AddMetric("puredots.hand.count", totals.TotalHands);
            buffer.AddMetric("puredots.hand.state.empty", totals.EmptyCount);
            buffer.AddMetric("puredots.hand.state.holding", totals.HoldingCount);
            buffer.AddMetric("puredots.hand.state.dragging", totals.DraggingCount);
            buffer.AddMetric("puredots.hand.state.slingshot", totals.SlingshotAimCount);
            buffer.AddMetric("puredots.hand.state.dumping", totals.DumpingCount);

            float avgHeld = totals.TotalHands > 0 ? (float)totals.TotalHeldAmount / totals.TotalHands : 0f;
            float avgChargeSeconds = totals.TotalHands > 0 ? totals.TotalChargeSeconds / totals.TotalHands : 0f;

            buffer.AddMetric("puredots.hand.avgHeldAmount", avgHeld);
            buffer.AddMetric("puredots.hand.avgChargeMs", avgChargeSeconds * 1000f, TelemetryMetricUnit.DurationMilliseconds);
        }

        [BurstCompile]
        private partial struct SampleHandStateJob : IJobEntity
        {
            public NativeReference<HandTelemetryAccumulator> Accumulator;

            public void Execute(in PureDOTS.Runtime.Hand.HandState handState, in HandInteractionState interaction)
            {
                var acc = Accumulator.Value;
                acc.TotalHands++;
                
                var mappedState = MapToGodgameState(handState.CurrentState);
                switch (mappedState)
                {
                    case Godgame.Runtime.HandState.Empty:
                        acc.EmptyCount++;
                        break;
                    case Godgame.Runtime.HandState.Holding:
                        acc.HoldingCount++;
                        break;
                    case Godgame.Runtime.HandState.Dragging:
                        acc.DraggingCount++;
                        break;
                    case Godgame.Runtime.HandState.SlingshotAim:
                        acc.SlingshotAimCount++;
                        break;
                    case Godgame.Runtime.HandState.Dumping:
                        acc.DumpingCount++;
                        break;
                }

                acc.TotalHeldAmount += math.max(interaction.HeldAmount, 0);
                acc.TotalChargeSeconds += math.max(handState.ChargeTimer, 0f);

                Accumulator.Value = acc;
            }

            private static Godgame.Runtime.HandState MapToGodgameState(PureDOTS.Runtime.Hand.HandStateType state)
            {
                return state switch
                {
                    PureDOTS.Runtime.Hand.HandStateType.Holding => Godgame.Runtime.HandState.Holding,
                    PureDOTS.Runtime.Hand.HandStateType.Siphoning => Godgame.Runtime.HandState.Dragging,
                    PureDOTS.Runtime.Hand.HandStateType.Charging => Godgame.Runtime.HandState.SlingshotAim,
                    PureDOTS.Runtime.Hand.HandStateType.Dumping => Godgame.Runtime.HandState.Dumping,
                    _ => Godgame.Runtime.HandState.Empty
                };
            }
        }

        private struct HandTelemetryAccumulator
        {
            public int TotalHands;
            public int EmptyCount;
            public int HoldingCount;
            public int DraggingCount;
            public int SlingshotAimCount;
            public int DumpingCount;
            public int TotalHeldAmount;
            public float TotalChargeSeconds;
        }
    }
}
