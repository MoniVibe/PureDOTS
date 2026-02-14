using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems.AI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Scenarios
{
    public struct AITierPipeMicroAgent : IComponentData
    {
        public AILODTier Tier;
    }

    public struct AITierPipeMicroState : IComponentData
    {
        public BlobAssetReference<AIUtilityArchetypeBlob> UtilityBlob;
        public uint Digest;
        public int Tier0SensorSamples;
        public int Tier1SensorSamples;
        public int Tier2SensorSamples;
        public int Tier3SensorSamples;
        public int Tier0CommandsEmitted;
        public int Tier1CommandsEmitted;
        public int Tier2CommandsEmitted;
        public int Tier3CommandsEmitted;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ScenarioEntityBootstrapSystem))]
    public partial struct AITierCadencePipelineMicroScenarioSystem : ISystem
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.ai.tierpipe.micro");
        private const int AgentsPerTier = 16;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            if (!scenarioInfo.ScenarioId.Equals(TargetScenarioId))
            {
                state.Enabled = false;
                return;
            }

            var scenarioEntity = ResolveScenarioEntity(ref state);
            if (scenarioEntity == Entity.Null)
            {
                return;
            }

            if (state.EntityManager.HasComponent<AITierPipeMicroState>(scenarioEntity))
            {
                state.Enabled = false;
                return;
            }

            EnsureCommandQueue(ref state);
            ForceGlobalCadenceToOne(ref state);

            var utilityBlob = CreateUtilityBlob();
            var stableTarget = CreateStableTarget(ref state);

            SeedTier(ref state, utilityBlob, stableTarget, AILODTier.Tier0_Full, -6f);
            SeedTier(ref state, utilityBlob, stableTarget, AILODTier.Tier1_Reduced, -2f);
            SeedTier(ref state, utilityBlob, stableTarget, AILODTier.Tier2_EventDriven, 2f);
            SeedTier(ref state, utilityBlob, stableTarget, AILODTier.Tier3_Aggregate, 6f);

            state.EntityManager.AddComponentData(scenarioEntity, new AITierPipeMicroState
            {
                UtilityBlob = utilityBlob,
                Digest = 2166136261u
            });

            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioEntitySingleton>(out var singleton))
            {
                return;
            }

            var scenarioEntity = singleton.Value;
            if (scenarioEntity == Entity.Null ||
                !state.EntityManager.Exists(scenarioEntity) ||
                !state.EntityManager.HasComponent<AITierPipeMicroState>(scenarioEntity))
            {
                return;
            }

            var data = state.EntityManager.GetComponentData<AITierPipeMicroState>(scenarioEntity);
            if (data.UtilityBlob.IsCreated)
            {
                data.UtilityBlob.Dispose();
                data.UtilityBlob = default;
                state.EntityManager.SetComponentData(scenarioEntity, data);
            }
        }

        private static void EnsureCommandQueue(ref SystemState state)
        {
            using var query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<AICommandQueueTag>());
            Entity queueEntity;
            if (query.IsEmptyIgnoreFilter)
            {
                queueEntity = state.EntityManager.CreateEntity(typeof(AICommandQueueTag));
            }
            else
            {
                queueEntity = query.GetSingletonEntity();
            }

            if (!state.EntityManager.HasBuffer<AICommand>(queueEntity))
            {
                state.EntityManager.AddBuffer<AICommand>(queueEntity);
            }
        }

        private static void ForceGlobalCadenceToOne(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<MindCadenceSettings>())
            {
                return;
            }

            var cadence = SystemAPI.GetSingletonRW<MindCadenceSettings>();
            cadence.ValueRW.SensorCadenceTicks = 1;
            cadence.ValueRW.EvaluationCadenceTicks = 1;
            cadence.ValueRW.ResolutionCadenceTicks = 1;
        }

        private static Entity CreateStableTarget(ref SystemState state)
        {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(0f, 0f, 10f)));
            return entity;
        }

        private static BlobAssetReference<AIUtilityArchetypeBlob> CreateUtilityBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AIUtilityArchetypeBlob>();
            var actions = builder.Allocate(ref root.Actions, 2);

            ref var action0 = ref actions[0];
            action0.BiasMask = AIUtilityBiasMask.None;
            var factors0 = builder.Allocate(ref action0.Factors, 1);
            factors0[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0,
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            ref var action1 = ref actions[1];
            action1.BiasMask = AIUtilityBiasMask.None;
            var factors1 = builder.Allocate(ref action1.Factors, 1);
            factors1[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0,
                Threshold = 0.2f,
                Weight = 0.5f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            var blob = builder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static void SeedTier(
            ref SystemState state,
            BlobAssetReference<AIUtilityArchetypeBlob> utilityBlob,
            Entity targetEntity,
            AILODTier tier,
            float xBase)
        {
            for (var i = 0; i < AgentsPerTier; i++)
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(xBase, 0f, i * 0.35f)));
                state.EntityManager.AddComponentData(entity, new AISensorConfig
                {
                    UpdateInterval = 0f,
                    Range = 20f,
                    MaxResults = 1,
                    QueryOptions = SpatialQueryOptions.RequireDeterministicSorting,
                    PrimaryCategory = AISensorCategory.Custom0,
                    SecondaryCategory = AISensorCategory.None
                });
                state.EntityManager.AddComponentData(entity, new AISensorState
                {
                    Elapsed = 0f,
                    LastSampleTick = 0u
                });
                state.EntityManager.AddComponentData(entity, new AIBehaviourArchetype { UtilityBlob = utilityBlob });
                state.EntityManager.AddComponentData(entity, new AIUtilityState
                {
                    BestActionIndex = 0,
                    BestScore = 0f,
                    LastEvaluationTick = 0u
                });
                state.EntityManager.AddComponentData(entity, new AITargetState
                {
                    TargetEntity = Entity.Null,
                    TargetPosition = float3.zero,
                    ActionIndex = 0,
                    Flags = 0
                });
                state.EntityManager.AddComponentData(entity, new AIFidelityTier
                {
                    Tier = tier,
                    LastChangeTick = 0u,
                    ReasonMask = 0
                });
                state.EntityManager.AddComponentData(entity, new AITierPipeMicroAgent { Tier = tier });

                state.EntityManager.AddBuffer<AISensorReading>(entity);
                state.EntityManager.AddBuffer<AIActionState>(entity);

                var perceived = state.EntityManager.AddBuffer<PerceivedEntity>(entity);
                perceived.Add(new PerceivedEntity
                {
                    TargetEntity = targetEntity,
                    DetectedChannels = PerceptionChannel.Proximity,
                    Confidence = 1f,
                    Distance = 5f,
                    Direction = math.normalizesafe(new float3(0f, 0f, 1f)),
                    FirstDetectedTick = 0u,
                    LastSeenTick = 0u,
                    ThreatLevel = 0,
                    Relationship = 0,
                    RelationKind = PerceivedRelationKind.Unknown,
                    RelationFlags = PerceivedRelationFlags.None
                });
            }
        }

        private static Entity ResolveScenarioEntity(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<ScenarioEntitySingleton>(out var singleton))
            {
                if (singleton.Value != Entity.Null && state.EntityManager.Exists(singleton.Value))
                {
                    return singleton.Value;
                }
            }

            var query = SystemAPI.QueryBuilder().WithAll<ScenarioInfo>().Build();
            return query.IsEmptyIgnoreFilter ? Entity.Null : query.GetSingletonEntity();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AITaskResolutionSystem))]
    public partial struct AITierCadencePipelineMicroMetricsSystem : ISystem
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.ai.tierpipe.micro");

        private static readonly FixedString64Bytes Tier0SensorSamplesKey = new FixedString64Bytes("ai.tierpipe.tier0.sensor_samples");
        private static readonly FixedString64Bytes Tier1SensorSamplesKey = new FixedString64Bytes("ai.tierpipe.tier1.sensor_samples");
        private static readonly FixedString64Bytes Tier2SensorSamplesKey = new FixedString64Bytes("ai.tierpipe.tier2.sensor_samples");
        private static readonly FixedString64Bytes Tier3SensorSamplesKey = new FixedString64Bytes("ai.tierpipe.tier3.sensor_samples");

        private static readonly FixedString64Bytes Tier0CommandsEmittedKey = new FixedString64Bytes("ai.tierpipe.tier0.commands_emitted");
        private static readonly FixedString64Bytes Tier1CommandsEmittedKey = new FixedString64Bytes("ai.tierpipe.tier1.commands_emitted");
        private static readonly FixedString64Bytes Tier2CommandsEmittedKey = new FixedString64Bytes("ai.tierpipe.tier2.commands_emitted");
        private static readonly FixedString64Bytes Tier3CommandsEmittedKey = new FixedString64Bytes("ai.tierpipe.tier3.commands_emitted");
        private static readonly FixedString64Bytes DigestKey = new FixedString64Bytes("ai.tierpipe.digest");

        private ComponentLookup<AITierPipeMicroAgent> _tierLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            state.RequireForUpdate<TimeState>();
            _tierLookup = state.GetComponentLookup<AITierPipeMicroAgent>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            if (!scenarioInfo.ScenarioId.Equals(TargetScenarioId))
            {
                state.Enabled = false;
                return;
            }

            var scenarioEntity = ResolveScenarioEntity(ref state);
            if (scenarioEntity == Entity.Null || !state.EntityManager.HasComponent<AITierPipeMicroState>(scenarioEntity))
            {
                return;
            }

            _tierLookup.Update(ref state);

            var time = SystemAPI.GetSingleton<TimeState>();
            var data = state.EntityManager.GetComponentData<AITierPipeMicroState>(scenarioEntity);

            var tickTier0Sensor = 0;
            var tickTier1Sensor = 0;
            var tickTier2Sensor = 0;
            var tickTier3Sensor = 0;
            var tickTier0Commands = 0;
            var tickTier1Commands = 0;
            var tickTier2Commands = 0;
            var tickTier3Commands = 0;
            uint tickFold = 0u;

            foreach (var (sensorState, agent, entity) in SystemAPI.Query<RefRO<AISensorState>, RefRO<AITierPipeMicroAgent>>().WithEntityAccess())
            {
                if (sensorState.ValueRO.LastSampleTick != time.Tick)
                {
                    continue;
                }

                switch (agent.ValueRO.Tier)
                {
                    case AILODTier.Tier0_Full:
                        tickTier0Sensor++;
                        break;
                    case AILODTier.Tier1_Reduced:
                        tickTier1Sensor++;
                        break;
                    case AILODTier.Tier2_EventDriven:
                        tickTier2Sensor++;
                        break;
                    case AILODTier.Tier3_Aggregate:
                        tickTier3Sensor++;
                        break;
                }

                tickFold ^= math.hash(new uint4(time.Tick, (uint)entity.Index, (uint)agent.ValueRO.Tier, 1u));
            }

            if (SystemAPI.HasSingleton<AICommandQueueTag>())
            {
                var queueEntity = SystemAPI.GetSingletonEntity<AICommandQueueTag>();
                var commands = SystemAPI.GetBuffer<AICommand>(queueEntity);
                for (var i = 0; i < commands.Length; i++)
                {
                    var cmd = commands[i];
                    if (!_tierLookup.HasComponent(cmd.Agent))
                    {
                        continue;
                    }

                    var tier = _tierLookup[cmd.Agent].Tier;
                    switch (tier)
                    {
                        case AILODTier.Tier0_Full:
                            tickTier0Commands++;
                            break;
                        case AILODTier.Tier1_Reduced:
                            tickTier1Commands++;
                            break;
                        case AILODTier.Tier2_EventDriven:
                            tickTier2Commands++;
                            break;
                        case AILODTier.Tier3_Aggregate:
                            tickTier3Commands++;
                            break;
                    }

                    tickFold ^= math.hash(new uint4(time.Tick, (uint)cmd.Agent.Index, (uint)tier, cmd.ActionIndex));
                }
            }

            data.Tier0SensorSamples += tickTier0Sensor;
            data.Tier1SensorSamples += tickTier1Sensor;
            data.Tier2SensorSamples += tickTier2Sensor;
            data.Tier3SensorSamples += tickTier3Sensor;

            data.Tier0CommandsEmitted += tickTier0Commands;
            data.Tier1CommandsEmitted += tickTier1Commands;
            data.Tier2CommandsEmitted += tickTier2Commands;
            data.Tier3CommandsEmitted += tickTier3Commands;

            data.Digest = math.hash(new uint4(
                data.Digest ^ tickFold,
                (uint)(data.Tier0SensorSamples + (data.Tier1SensorSamples << 1)),
                (uint)(data.Tier2SensorSamples + (data.Tier3SensorSamples << 1)),
                (uint)(data.Tier0CommandsEmitted + data.Tier1CommandsEmitted + data.Tier2CommandsEmitted + data.Tier3CommandsEmitted)));
            if (data.Digest == 0u)
            {
                data.Digest = 1u;
            }

            state.EntityManager.SetComponentData(scenarioEntity, data);

            var metricLookup = SystemAPI.GetBufferLookup<ScenarioMetricSample>(false);
            metricLookup.Update(ref state);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier0SensorSamplesKey, data.Tier0SensorSamples);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier1SensorSamplesKey, data.Tier1SensorSamples);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier2SensorSamplesKey, data.Tier2SensorSamples);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier3SensorSamplesKey, data.Tier3SensorSamples);

            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier0CommandsEmittedKey, data.Tier0CommandsEmitted);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier1CommandsEmittedKey, data.Tier1CommandsEmitted);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier2CommandsEmittedKey, data.Tier2CommandsEmitted);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier3CommandsEmittedKey, data.Tier3CommandsEmitted);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, DigestKey, data.Digest);
        }

        private static Entity ResolveScenarioEntity(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<ScenarioEntitySingleton>(out var singleton))
            {
                if (singleton.Value != Entity.Null && state.EntityManager.Exists(singleton.Value))
                {
                    return singleton.Value;
                }
            }

            var query = SystemAPI.QueryBuilder().WithAll<ScenarioInfo>().Build();
            return query.IsEmptyIgnoreFilter ? Entity.Null : query.GetSingletonEntity();
        }
    }
}
