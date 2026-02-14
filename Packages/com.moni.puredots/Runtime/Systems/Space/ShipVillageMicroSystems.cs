using PureDOTS.Runtime;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Space;
using PureDOTS.Runtime.Telemetry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Systems.Space
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ShipVillageScenarioBootstrapSystem : ISystem
    {
        private FixedString64Bytes _targetScenarioId;
        private EntityQuery _shipRootQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            _targetScenarioId = new FixedString64Bytes("scenario.space4x.ship_micro.01");
            _shipRootQuery = state.GetEntityQuery(ComponentType.ReadOnly<ShipRootTag>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            if (!scenarioInfo.ScenarioId.Equals(_targetScenarioId))
            {
                state.Enabled = false;
                return;
            }

            EnsureScaleMetricsConfig(ref state);

            if (!_shipRootQuery.IsEmptyIgnoreFilter)
            {
                state.Enabled = false;
                return;
            }

            const int seatCount = 8;
            const int crewCount = 48;

            var shipRoot = state.EntityManager.CreateEntity(
                typeof(ShipRootTag),
                typeof(ShipId),
                typeof(ShipDesignRef),
                typeof(ShipOrder),
                typeof(ShipIntent),
                typeof(ShipOrderCadence),
                typeof(ShipCommsLink));

            state.EntityManager.SetComponentData(shipRoot, new ShipId { Value = 1 });
            state.EntityManager.SetComponentData(shipRoot, new ShipDesignRef { Value = 9001 });
            state.EntityManager.SetComponentData(shipRoot, new ShipOrder
            {
                Type = ShipOrderType.HoldCourse,
                State = ShipOrderState.Idle,
                Sequence = 0,
                IssuedTick = 0
            });
            state.EntityManager.SetComponentData(shipRoot, new ShipIntent
            {
                Readiness = 0f,
                Coordination = 0f,
                CanExecute = 0,
                LastAppliedTick = 0
            });
            state.EntityManager.SetComponentData(shipRoot, new ShipOrderCadence
            {
                NextInjectTick = 1,
                InjectEveryTicks = 120
            });

            var commsEntity = state.EntityManager.CreateEntity(typeof(ShipCommsTag), typeof(ShipCommsRuntime));
            state.EntityManager.SetComponentData(commsEntity, new ShipCommsRuntime
            {
                TotalEvents = 0,
                EventsSinceTranscript = 0,
                LastTranscriptTick = 0,
                LastEventCode = 0,
                LastFromRole = SeatRoleKind.Captain,
                LastToRole = SeatRoleKind.Captain
            });
            state.EntityManager.AddBuffer<CommsEvent>(commsEntity);

            state.EntityManager.SetComponentData(shipRoot, new ShipCommsLink
            {
                CommsEntity = commsEntity
            });

            var seats = new NativeArray<Entity>(seatCount, Allocator.Temp);
            var seatCrewCounts = new NativeArray<int>(seatCount, Allocator.Temp);
            var seatSkillSums = new NativeArray<float>(seatCount, Allocator.Temp);

            for (int i = 0; i < seatCount; i++)
            {
                var seat = state.EntityManager.CreateEntity(
                    typeof(SeatRole),
                    typeof(SeatAssignment),
                    typeof(SeatState),
                    typeof(SeatIntent));

                seats[i] = seat;
                state.EntityManager.SetComponentData(seat, new SeatRole { Value = GetSeatRole(i) });
                state.EntityManager.SetComponentData(seat, new SeatAssignment { Ship = shipRoot });
                state.EntityManager.SetComponentData(seat, new SeatState
                {
                    Manned = 0,
                    CrewAssigned = 0,
                    Efficiency = 0f,
                    Readiness = 0f,
                    LastDecisionTick = 0
                });
                state.EntityManager.SetComponentData(seat, new SeatIntent
                {
                    ReadinessDelta = 0f,
                    CoordinationDelta = 0f,
                    ConfirmedOrder = 0
                });
            }

            for (int i = 0; i < crewCount; i++)
            {
                int seatIndex = i % seatCount;
                float skill = 0.55f + ((i * 17) % 30) * 0.01f;
                float fatigue = 0.07f + ((i * 11) % 12) * 0.0125f;
                float stress = 0.05f + ((i * 5) % 9) * 0.015f;

                var crew = state.EntityManager.CreateEntity(
                    typeof(CrewId),
                    typeof(CrewProfileRef),
                    typeof(CrewState),
                    typeof(CrewSeatAssignment));

                state.EntityManager.SetComponentData(crew, new CrewId { Value = i + 1 });
                state.EntityManager.SetComponentData(crew, new CrewProfileRef { Value = 1000 + (i % 16) });
                state.EntityManager.SetComponentData(crew, new CrewState
                {
                    Stress = stress,
                    Fatigue = fatigue,
                    Skill = skill
                });
                state.EntityManager.SetComponentData(crew, new CrewSeatAssignment
                {
                    Seat = seats[seatIndex]
                });

                seatCrewCounts[seatIndex]++;
                seatSkillSums[seatIndex] += skill;
            }

            for (int i = 0; i < seatCount; i++)
            {
                var stateData = state.EntityManager.GetComponentData<SeatState>(seats[i]);
                int assigned = seatCrewCounts[i];
                float avgSkill = assigned > 0 ? seatSkillSums[i] / assigned : 0f;
                float readiness = math.saturate(avgSkill - 0.2f);

                stateData.Manned = assigned > 0 ? (byte)1 : (byte)0;
                stateData.CrewAssigned = (byte)math.clamp(assigned, 0, 255);
                stateData.Efficiency = readiness;
                stateData.Readiness = readiness;
                stateData.LastDecisionTick = 0;
                state.EntityManager.SetComponentData(seats[i], stateData);
            }

            seats.Dispose();
            seatCrewCounts.Dispose();
            seatSkillSums.Dispose();

            UnityEngine.Debug.Log("[ShipVillageMicro] Seeded scenario.space4x.ship_micro.01 (ships=1 seats=8 crew=48)");
            state.Enabled = false;
        }

        private void EnsureScaleMetricsConfig(ref SystemState state)
        {
            var config = new ScaleTestMetricsConfig
            {
                SampleInterval = 1,
                LogInterval = 50,
                CollectSystemTimings = 0,
                CollectMemoryStats = 1,
                TargetTickTimeMs = 16.67f,
                TargetMemoryMB = 512f,
                EnableLODDebug = 0,
                EnableAggregateDebug = 0
            };

            if (SystemAPI.TryGetSingletonRW<ScaleTestMetricsConfig>(out var existing))
            {
                existing.ValueRW = config;
                return;
            }

            var configEntity = state.EntityManager.CreateEntity(typeof(ScaleTestMetricsConfig));
            state.EntityManager.SetComponentData(configEntity, config);
        }

        private static SeatRoleKind GetSeatRole(int index)
        {
            return index switch
            {
                0 => SeatRoleKind.Captain,
                1 => SeatRoleKind.Navigation,
                2 => SeatRoleKind.Sensors,
                3 => SeatRoleKind.Weapons,
                4 => SeatRoleKind.Logistics,
                5 => SeatRoleKind.Engineering,
                6 => SeatRoleKind.Weapons,
                7 => SeatRoleKind.Sensors,
                _ => SeatRoleKind.Logistics
            };
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ShipVillageOrderInjectSystem : ISystem
    {
        private FixedString64Bytes _targetScenarioId;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            _targetScenarioId = new FixedString64Bytes("scenario.space4x.ship_micro.01");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioInfo>(out var scenarioInfo) || !scenarioInfo.ScenarioId.Equals(_targetScenarioId))
            {
                return;
            }

            uint tick = 0;
            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickTimeState))
            {
                tick = tickTimeState.Tick;
            }
            else if (SystemAPI.TryGetSingleton<ScenarioTick>(out var scenarioTick))
            {
                tick = scenarioTick.Value;
            }
            var commsLookup = SystemAPI.GetBufferLookup<CommsEvent>(false);
            commsLookup.Update(ref state);

            foreach (var (order, cadence, commsLink) in SystemAPI.Query<RefRW<ShipOrder>, RefRW<ShipOrderCadence>, RefRO<ShipCommsLink>>())
            {
                if (tick < cadence.ValueRO.NextInjectTick)
                {
                    continue;
                }

                uint nextSequence = order.ValueRO.Sequence + 1;
                var nextOrderType = (ShipOrderType)((nextSequence - 1u) % 3u);

                order.ValueRW.Type = nextOrderType;
                order.ValueRW.State = ShipOrderState.Issued;
                order.ValueRW.Sequence = nextSequence;
                order.ValueRW.IssuedTick = tick;

                cadence.ValueRW.NextInjectTick = tick + math.max(1u, cadence.ValueRO.InjectEveryTicks);

                if (!commsLookup.HasBuffer(commsLink.ValueRO.CommsEntity))
                {
                    continue;
                }

                var comms = commsLookup[commsLink.ValueRO.CommsEntity];
                if (comms.Length >= 24)
                {
                    continue;
                }

                comms.Add(new CommsEvent
                {
                    Tick = tick,
                    FromRole = SeatRoleKind.Captain,
                    ToRole = SeatRoleKind.Navigation,
                    EventCode = 1,
                    Payload = (float)nextOrderType
                });
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ShipVillageOrderInjectSystem))]
    public partial struct ShipVillageSeatDecideSystem : ISystem
    {
        private FixedString64Bytes _targetScenarioId;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            _targetScenarioId = new FixedString64Bytes("scenario.space4x.ship_micro.01");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioInfo>(out var scenarioInfo) || !scenarioInfo.ScenarioId.Equals(_targetScenarioId))
            {
                return;
            }

            uint tick = 0;
            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickTimeState))
            {
                tick = tickTimeState.Tick;
            }
            else if (SystemAPI.TryGetSingleton<ScenarioTick>(out var scenarioTick))
            {
                tick = scenarioTick.Value;
            }
            if (tick % 10u != 0u)
            {
                return;
            }

            var shipOrderLookup = SystemAPI.GetComponentLookup<ShipOrder>(true);
            shipOrderLookup.Update(ref state);

            var commsLinkLookup = SystemAPI.GetComponentLookup<ShipCommsLink>(true);
            commsLinkLookup.Update(ref state);

            var commsLookup = SystemAPI.GetBufferLookup<CommsEvent>(false);
            commsLookup.Update(ref state);

            foreach (var (seatState, seatIntent, role, assignment, seatEntity) in SystemAPI.Query<RefRW<SeatState>, RefRW<SeatIntent>, RefRO<SeatRole>, RefRO<SeatAssignment>>().WithEntityAccess())
            {
                int crewCount = 0;
                float skillSum = 0f;
                float fatigueSum = 0f;
                float stressSum = 0f;

                foreach (var (crewState, crewSeat) in SystemAPI.Query<RefRO<CrewState>, RefRO<CrewSeatAssignment>>())
                {
                    if (crewSeat.ValueRO.Seat != seatEntity)
                    {
                        continue;
                    }

                    crewCount++;
                    skillSum += crewState.ValueRO.Skill;
                    fatigueSum += crewState.ValueRO.Fatigue;
                    stressSum += crewState.ValueRO.Stress;
                }

                seatState.ValueRW.Manned = crewCount > 0 ? (byte)1 : (byte)0;
                seatState.ValueRW.CrewAssigned = (byte)math.clamp(crewCount, 0, 255);

                if (crewCount == 0)
                {
                    seatState.ValueRW.Efficiency = 0f;
                    seatState.ValueRW.Readiness = 0f;
                    seatState.ValueRW.LastDecisionTick = tick;
                    seatIntent.ValueRW.ReadinessDelta = 0f;
                    seatIntent.ValueRW.CoordinationDelta = 0f;
                    seatIntent.ValueRW.ConfirmedOrder = 0;
                    continue;
                }

                float avgSkill = skillSum / crewCount;
                float avgFatigue = fatigueSum / crewCount;
                float avgStress = stressSum / crewCount;
                float readiness = math.saturate(avgSkill - (avgFatigue * 0.5f) - (avgStress * 0.35f));
                float efficiency = math.saturate(avgSkill - (avgFatigue * 0.2f));

                seatState.ValueRW.Efficiency = efficiency;
                seatState.ValueRW.Readiness = readiness;
                seatState.ValueRW.LastDecisionTick = tick;

                float roleWeight = ShipVillageMicroSystemHelpers.GetRoleCoordinationWeight(role.ValueRO.Value);
                seatIntent.ValueRW.ReadinessDelta = readiness * roleWeight;
                seatIntent.ValueRW.CoordinationDelta = readiness * roleWeight * 0.5f;

                bool hasActiveOrder =
                    shipOrderLookup.HasComponent(assignment.ValueRO.Ship) &&
                    (shipOrderLookup[assignment.ValueRO.Ship].State == ShipOrderState.Issued ||
                     shipOrderLookup[assignment.ValueRO.Ship].State == ShipOrderState.Executing);

                bool confirmed = hasActiveOrder && readiness >= 0.45f;
                seatIntent.ValueRW.ConfirmedOrder = confirmed ? (byte)1 : (byte)0;
                if (!confirmed)
                {
                    continue;
                }

                if (!commsLinkLookup.HasComponent(assignment.ValueRO.Ship))
                {
                    continue;
                }

                var commsEntity = commsLinkLookup[assignment.ValueRO.Ship].CommsEntity;
                if (!commsLookup.HasBuffer(commsEntity))
                {
                    continue;
                }

                var comms = commsLookup[commsEntity];
                if (comms.Length >= 24)
                {
                    continue;
                }

                comms.Add(new CommsEvent
                {
                    Tick = tick,
                    FromRole = role.ValueRO.Value,
                    ToRole = SeatRoleKind.Captain,
                    EventCode = 2,
                    Payload = readiness
                });
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ShipVillageSeatDecideSystem))]
    public partial struct ShipVillageIntentApplySystem : ISystem
    {
        private FixedString64Bytes _targetScenarioId;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            _targetScenarioId = new FixedString64Bytes("scenario.space4x.ship_micro.01");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioInfo>(out var scenarioInfo) || !scenarioInfo.ScenarioId.Equals(_targetScenarioId))
            {
                return;
            }

            uint tick = 0;
            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickTimeState))
            {
                tick = tickTimeState.Tick;
            }
            else if (SystemAPI.TryGetSingleton<ScenarioTick>(out var scenarioTick))
            {
                tick = scenarioTick.Value;
            }
            var commsLookup = SystemAPI.GetBufferLookup<CommsEvent>(false);
            commsLookup.Update(ref state);

            foreach (var (shipIntent, shipOrder, commsLink, shipEntity) in SystemAPI.Query<RefRW<ShipIntent>, RefRW<ShipOrder>, RefRO<ShipCommsLink>>().WithEntityAccess())
            {
                int seatCount = 0;
                int confirmations = 0;
                float readinessSum = 0f;
                float coordinationSum = 0f;

                foreach (var (seatState, seatIntent, assignment) in SystemAPI.Query<RefRO<SeatState>, RefRO<SeatIntent>, RefRO<SeatAssignment>>())
                {
                    if (assignment.ValueRO.Ship != shipEntity)
                    {
                        continue;
                    }

                    seatCount++;
                    readinessSum += seatState.ValueRO.Readiness;
                    coordinationSum += seatIntent.ValueRO.CoordinationDelta;
                    confirmations += seatIntent.ValueRO.ConfirmedOrder;
                }

                if (seatCount == 0)
                {
                    continue;
                }

                shipIntent.ValueRW.Readiness = readinessSum / seatCount;
                shipIntent.ValueRW.Coordination = math.saturate(coordinationSum / seatCount);
                shipIntent.ValueRW.CanExecute = confirmations >= math.max(2, seatCount / 2) ? (byte)1 : (byte)0;
                shipIntent.ValueRW.LastAppliedTick = tick;

                var previousState = shipOrder.ValueRO.State;
                if (shipOrder.ValueRO.State == ShipOrderState.Issued && shipIntent.ValueRO.CanExecute == 1)
                {
                    shipOrder.ValueRW.State = ShipOrderState.Executing;
                }
                else if (shipOrder.ValueRO.State == ShipOrderState.Executing && shipIntent.ValueRO.Readiness >= 0.72f && tick >= shipOrder.ValueRO.IssuedTick + 40u)
                {
                    shipOrder.ValueRW.State = ShipOrderState.Complete;
                }
                else if (shipOrder.ValueRO.State == ShipOrderState.Complete && tick >= shipOrder.ValueRO.IssuedTick + 90u)
                {
                    shipOrder.ValueRW.State = ShipOrderState.Idle;
                }

                if (shipOrder.ValueRO.State == previousState || !commsLookup.HasBuffer(commsLink.ValueRO.CommsEntity))
                {
                    continue;
                }

                var comms = commsLookup[commsLink.ValueRO.CommsEntity];
                if (comms.Length >= 24)
                {
                    continue;
                }

                byte code = shipOrder.ValueRO.State switch
                {
                    ShipOrderState.Executing => (byte)3,
                    ShipOrderState.Complete => (byte)4,
                    ShipOrderState.Idle => (byte)5,
                    _ => (byte)0
                };

                if (code == 0)
                {
                    continue;
                }

                comms.Add(new CommsEvent
                {
                    Tick = tick,
                    FromRole = SeatRoleKind.Captain,
                    ToRole = SeatRoleKind.Weapons,
                    EventCode = code,
                    Payload = shipIntent.ValueRO.Readiness
                });
            }
        }
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ShipVillageCommsDrainSystem : ISystem
    {
        private FixedString64Bytes _targetScenarioId;
        private FixedString64Bytes _eventsMetricKey;
        private FixedString64Bytes _seatReadinessMetricKey;
        private FixedString64Bytes _orderStateMetricKey;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            _targetScenarioId = new FixedString64Bytes("scenario.space4x.ship_micro.01");
            _eventsMetricKey = new FixedString64Bytes("ship.micro.events.count");
            _seatReadinessMetricKey = new FixedString64Bytes("ship.micro.seat.readiness");
            _orderStateMetricKey = new FixedString64Bytes("ship.micro.order.state");
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioInfo>(out var scenarioInfo) || !scenarioInfo.ScenarioId.Equals(_targetScenarioId))
            {
                return;
            }

            uint tick = 0;
            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickTimeState))
            {
                tick = tickTimeState.Tick;
            }
            else if (SystemAPI.TryGetSingleton<ScenarioTick>(out var scenarioTick))
            {
                tick = scenarioTick.Value;
            }
            var commsLookup = SystemAPI.GetBufferLookup<CommsEvent>(false);
            commsLookup.Update(ref state);

            var commsRuntimeLookup = SystemAPI.GetComponentLookup<ShipCommsRuntime>(false);
            commsRuntimeLookup.Update(ref state);

            foreach (var (shipId, order, shipIntent, commsLink, shipEntity) in SystemAPI.Query<RefRO<ShipId>, RefRO<ShipOrder>, RefRO<ShipIntent>, RefRO<ShipCommsLink>>().WithAll<ShipRootTag>().WithEntityAccess())
            {
                if (!commsRuntimeLookup.HasComponent(commsLink.ValueRO.CommsEntity) || !commsLookup.HasBuffer(commsLink.ValueRO.CommsEntity))
                {
                    continue;
                }

                var runtime = commsRuntimeLookup[commsLink.ValueRO.CommsEntity];
                var events = commsLookup[commsLink.ValueRO.CommsEntity];
                for (int i = 0; i < events.Length; i++)
                {
                    var evt = events[i];
                    runtime.TotalEvents += 1u;
                    runtime.EventsSinceTranscript += 1u;
                    runtime.LastEventCode = evt.EventCode;
                    runtime.LastFromRole = evt.FromRole;
                    runtime.LastToRole = evt.ToRole;
                }
                events.Clear();

                int seatCount = 0;
                float seatReadinessTotal = 0f;
                foreach (var (seatState, assignment) in SystemAPI.Query<RefRO<SeatState>, RefRO<SeatAssignment>>())
                {
                    if (assignment.ValueRO.Ship != shipEntity)
                    {
                        continue;
                    }

                    seatCount++;
                    seatReadinessTotal += seatState.ValueRO.Readiness;
                }

                float seatReadiness = seatCount == 0 ? 0f : seatReadinessTotal / seatCount;
                ScenarioMetricsUtility.SetMetric(state.EntityManager, _eventsMetricKey, runtime.TotalEvents);
                ScenarioMetricsUtility.SetMetric(state.EntityManager, _seatReadinessMetricKey, seatReadiness);
                ScenarioMetricsUtility.SetMetric(state.EntityManager, _orderStateMetricKey, (byte)order.ValueRO.State);

                if (tick >= runtime.LastTranscriptTick + 50u)
                {
                    UnityEngine.Debug.Log($"[ShipVillageMicroMetrics] ship.micro.events.count={runtime.TotalEvents} ship.micro.seat.readiness={seatReadiness:0.00} ship.micro.order.state={(byte)order.ValueRO.State}");
                    UnityEngine.Debug.Log($"[ShipVillageMicro] tick={tick} ship={shipId.ValueRO.Value} order={order.ValueRO.Type}/{order.ValueRO.State} readiness={shipIntent.ValueRO.Readiness:0.00} seats={seatReadiness:0.00} eventsWindow={runtime.EventsSinceTranscript} eventsTotal={runtime.TotalEvents} last={runtime.LastFromRole}>{runtime.LastToRole}#{runtime.LastEventCode}");
                    runtime.EventsSinceTranscript = 0;
                    runtime.LastTranscriptTick = tick;
                }

                commsRuntimeLookup[commsLink.ValueRO.CommsEntity] = runtime;
            }
        }
    }

    internal static class ShipVillageMicroSystemHelpers
    {
        public static float GetRoleCoordinationWeight(SeatRoleKind role)
        {
            return role switch
            {
                SeatRoleKind.Captain => 1.00f,
                SeatRoleKind.Navigation => 0.80f,
                SeatRoleKind.Sensors => 0.75f,
                SeatRoleKind.Weapons => 0.70f,
                SeatRoleKind.Logistics => 0.65f,
                SeatRoleKind.Engineering => 0.68f,
                _ => 0.5f
            };
        }
    }
}
