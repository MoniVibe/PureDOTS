using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Individual;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Spatial;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Systems.Scenarios
{
    public struct AIBiasProofAgentTag : IComponentData
    {
        public byte GroupId;
    }

    public struct AIBiasProofLastEvalTick : IComponentData
    {
        public uint Value;
    }

    public struct AIBiasProofCounters : IComponentData
    {
        public uint GroupAAction0Chosen;
        public uint GroupBAction1Chosen;
        public uint GroupATotalDecisions;
        public uint GroupBTotalDecisions;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class AIBiasProofScenarioBootstrapSystem : SystemBase
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.puredots.ai_biasproof.micro");

        private EntityQuery _agentQuery;
        private BlobAssetReference<AIUtilityArchetypeBlob> _utilityBlob;

        protected override void OnCreate()
        {
            RequireForUpdate<ScenarioInfo>();
            _agentQuery = GetEntityQuery(ComponentType.ReadOnly<AIBiasProofAgentTag>());
        }

        protected override void OnUpdate()
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            if (!scenarioInfo.ScenarioId.Equals(TargetScenarioId))
            {
                Enabled = false;
                return;
            }

            if (!_agentQuery.IsEmptyIgnoreFilter)
            {
                Enabled = false;
                return;
            }

            if (SystemAPI.TryGetSingletonRW<MindCadenceSettings>(out var cadence))
            {
                var value = cadence.ValueRW;
                value.SensorCadenceTicks = 1u;
                value.EvaluationCadenceTicks = 1u;
                cadence.ValueRW = value;
            }

            _utilityBlob = CreateBiasProofUtilityBlob();

            SpawnGroup(
                groupId: 0,
                count: 32,
                aggressionBias: 1.8f,
                socialBias: 0.2f);

            SpawnGroup(
                groupId: 1,
                count: 32,
                aggressionBias: 0.2f,
                socialBias: 1.8f);

            if (!SystemAPI.HasSingleton<AIBiasProofCounters>())
            {
                var counterEntity = EntityManager.CreateEntity(typeof(AIBiasProofCounters));
                EntityManager.SetComponentData(counterEntity, default(AIBiasProofCounters));
            }

            Debug.Log("[AIBiasProof] Seeded scenario.puredots.ai_biasproof.micro with 64 agents (A=32, B=32)");
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            if (_utilityBlob.IsCreated)
            {
                _utilityBlob.Dispose();
            }
        }

        private void SpawnGroup(byte groupId, int count, float aggressionBias, float socialBias)
        {
            for (int i = 0; i < count; i++)
            {
                var entity = EntityManager.CreateEntity(
                    typeof(LocalTransform),
                    typeof(AISensorConfig),
                    typeof(AISensorState),
                    typeof(AIUtilityState),
                    typeof(AIBehaviourArchetype),
                    typeof(BehaviorTuning),
                    typeof(AIBiasProofAgentTag),
                    typeof(AIBiasProofLastEvalTick));

                EntityManager.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
                EntityManager.SetComponentData(entity, new AISensorConfig
                {
                    UpdateInterval = 0f,
                    Range = 10f,
                    MaxResults = 2,
                    QueryOptions = SpatialQueryOptions.IgnoreSelf | SpatialQueryOptions.RequireDeterministicSorting,
                    PrimaryCategory = AISensorCategory.None,
                    SecondaryCategory = AISensorCategory.None
                });
                EntityManager.SetComponentData(entity, new AISensorState
                {
                    Elapsed = 0f,
                    LastSampleTick = 0
                });
                EntityManager.SetComponentData(entity, new AIUtilityState
                {
                    BestActionIndex = 0,
                    BestScore = 0f,
                    LastEvaluationTick = 0
                });
                EntityManager.SetComponentData(entity, new AIBehaviourArchetype
                {
                    UtilityBlob = _utilityBlob
                });
                EntityManager.SetComponentData(entity, new BehaviorTuning
                {
                    AggressionBias = aggressionBias,
                    SocialBias = socialBias,
                    GreedBias = 1f,
                    CuriosityBias = 1f,
                    ObedienceBias = 1f
                });
                EntityManager.SetComponentData(entity, new AIBiasProofAgentTag
                {
                    GroupId = groupId
                });
                EntityManager.SetComponentData(entity, new AIBiasProofLastEvalTick
                {
                    Value = 0
                });

                var sensorReadings = EntityManager.AddBuffer<AISensorReading>(entity);
                sensorReadings.Add(new AISensorReading
                {
                    Target = Entity.Null,
                    DistanceSq = 0f,
                    NormalizedScore = 1f,
                    CellId = -1,
                    SpatialVersion = 0,
                    Category = AISensorCategory.Custom0
                });
                sensorReadings.Add(new AISensorReading
                {
                    Target = Entity.Null,
                    DistanceSq = 0f,
                    NormalizedScore = 1f,
                    CellId = -1,
                    SpatialVersion = 0,
                    Category = AISensorCategory.Custom0
                });

                EntityManager.AddBuffer<AIActionState>(entity);
            }
        }

        private static BlobAssetReference<AIUtilityArchetypeBlob> CreateBiasProofUtilityBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AIUtilityArchetypeBlob>();
            var actions = builder.Allocate(ref root.Actions, 2);

            ref var action0 = ref actions[0];
            action0.BiasMask = AIUtilityBiasMask.Aggression;
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
            action1.BiasMask = AIUtilityBiasMask.Social;
            var factors1 = builder.Allocate(ref action1.Factors, 1);
            factors1[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 1,
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            var blob = builder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct AIBiasProofMetricsSystem : ISystem
    {
        private FixedString64Bytes _targetScenarioId;
        private FixedString64Bytes _groupAAction0Key;
        private FixedString64Bytes _groupBAction1Key;
        private FixedString64Bytes _groupATotalKey;
        private FixedString64Bytes _groupBTotalKey;
        private FixedString64Bytes _totalDecisionsKey;
        private FixedString64Bytes _monotonicityKey;

        private EntityQuery _counterQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            _counterQuery = state.GetEntityQuery(ComponentType.ReadOnly<AIBiasProofCounters>());

            _targetScenarioId = new FixedString64Bytes("scenario.puredots.ai_biasproof.micro");
            _groupAAction0Key = new FixedString64Bytes("ai.biasproof.groupA.action0_chosen_count");
            _groupBAction1Key = new FixedString64Bytes("ai.biasproof.groupB.action1_chosen_count");
            _groupATotalKey = new FixedString64Bytes("ai.biasproof.groupA.total_decisions");
            _groupBTotalKey = new FixedString64Bytes("ai.biasproof.groupB.total_decisions");
            _totalDecisionsKey = new FixedString64Bytes("ai.biasproof.total_decisions");
            _monotonicityKey = new FixedString64Bytes("ai.biasproof.monotonicity");
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioInfo>(out var scenarioInfo) || !scenarioInfo.ScenarioId.Equals(_targetScenarioId))
            {
                return;
            }

            var counterEntity = EnsureCounterEntity(ref state);
            var counters = state.EntityManager.GetComponentData<AIBiasProofCounters>(counterEntity);

            foreach (var (utilityState, groupTag, lastEvalTick) in SystemAPI.Query<RefRO<AIUtilityState>, RefRO<AIBiasProofAgentTag>, RefRW<AIBiasProofLastEvalTick>>())
            {
                var evaluatedTick = utilityState.ValueRO.LastEvaluationTick;
                if (evaluatedTick == 0 || evaluatedTick <= lastEvalTick.ValueRO.Value)
                {
                    continue;
                }

                if (groupTag.ValueRO.GroupId == 0)
                {
                    counters.GroupATotalDecisions++;
                    if (utilityState.ValueRO.BestActionIndex == 0)
                    {
                        counters.GroupAAction0Chosen++;
                    }
                }
                else
                {
                    counters.GroupBTotalDecisions++;
                    if (utilityState.ValueRO.BestActionIndex == 1)
                    {
                        counters.GroupBAction1Chosen++;
                    }
                }

                lastEvalTick.ValueRW.Value = evaluatedTick;
            }

            state.EntityManager.SetComponentData(counterEntity, counters);

            var totalDecisions = counters.GroupATotalDecisions + counters.GroupBTotalDecisions;
            var monotonicityPass =
                counters.GroupAAction0Chosen > 0u &&
                counters.GroupBAction1Chosen > 0u &&
                counters.GroupAAction0Chosen <= counters.GroupATotalDecisions &&
                counters.GroupBAction1Chosen <= counters.GroupBTotalDecisions
                    ? 1.0
                    : 0.0;

            ScenarioMetricsUtility.SetMetric(state.EntityManager, _groupAAction0Key, counters.GroupAAction0Chosen);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, _groupBAction1Key, counters.GroupBAction1Chosen);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, _groupATotalKey, counters.GroupATotalDecisions);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, _groupBTotalKey, counters.GroupBTotalDecisions);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, _totalDecisionsKey, totalDecisions);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, _monotonicityKey, monotonicityPass);
        }

        private Entity EnsureCounterEntity(ref SystemState state)
        {
            if (!_counterQuery.IsEmptyIgnoreFilter)
            {
                return _counterQuery.GetSingletonEntity();
            }

            var entity = state.EntityManager.CreateEntity(typeof(AIBiasProofCounters));
            state.EntityManager.SetComponentData(entity, default(AIBiasProofCounters));
            return entity;
        }
    }
}
