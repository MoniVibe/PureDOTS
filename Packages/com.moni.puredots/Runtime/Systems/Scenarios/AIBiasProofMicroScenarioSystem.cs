using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Individual;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems.AI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Scenarios
{
    public struct AIBiasProofMicroAgent : IComponentData
    {
        public byte Cohort;
    }

    public struct AIBiasProofMicroState : IComponentData
    {
        public BlobAssetReference<AIUtilityArchetypeBlob> UtilityBlob;
        public uint Digest;
        public int AgentCountPerCohort;
    }

    /// <summary>
    /// Seeds a deterministic micro scenario proving that utility BiasMask + BehaviorTuning
    /// changes action selection for otherwise identical agents.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ScenarioEntityBootstrapSystem))]
    public partial struct AIBiasProofMicroScenarioSystem : ISystem
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.ai.biasproof.micro");
        private const int AgentCountPerCohort = 24;

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

            if (state.EntityManager.HasComponent<AIBiasProofMicroState>(scenarioEntity))
            {
                state.Enabled = false;
                return;
            }

            var utilityBlob = CreateUtilityBlob();
            SeedCohort(ref state, utilityBlob, cohort: 0, aggressionBias: 1.85f, socialBias: 0.45f, xBase: -6f);
            SeedCohort(ref state, utilityBlob, cohort: 1, aggressionBias: 0.45f, socialBias: 1.85f, xBase: 6f);

            state.EntityManager.AddComponentData(scenarioEntity, new AIBiasProofMicroState
            {
                UtilityBlob = utilityBlob,
                Digest = 0u,
                AgentCountPerCohort = AgentCountPerCohort
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
            if (scenarioEntity == Entity.Null || !state.EntityManager.Exists(scenarioEntity) ||
                !state.EntityManager.HasComponent<AIBiasProofMicroState>(scenarioEntity))
            {
                return;
            }

            var scenarioState = state.EntityManager.GetComponentData<AIBiasProofMicroState>(scenarioEntity);
            if (scenarioState.UtilityBlob.IsCreated)
            {
                scenarioState.UtilityBlob.Dispose();
                scenarioState.UtilityBlob = default;
                state.EntityManager.SetComponentData(scenarioEntity, scenarioState);
            }
        }

        private Entity ResolveScenarioEntity(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<ScenarioEntitySingleton>(out var singleton))
            {
                var scenarioEntity = singleton.Value;
                if (scenarioEntity != Entity.Null && state.EntityManager.Exists(scenarioEntity))
                {
                    return scenarioEntity;
                }
            }

            var query = SystemAPI.QueryBuilder().WithAll<ScenarioInfo>().Build();
            return query.IsEmptyIgnoreFilter ? Entity.Null : query.GetSingletonEntity();
        }

        private static BlobAssetReference<AIUtilityArchetypeBlob> CreateUtilityBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AIUtilityArchetypeBlob>();
            var actions = builder.Allocate(ref root.Actions, 2);

            ref var action0 = ref actions[0];
            action0.BiasMask = AIUtilityBiasMask.Aggression;
            var action0Factors = builder.Allocate(ref action0.Factors, 1);
            action0Factors[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0,
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            ref var action1 = ref actions[1];
            action1.BiasMask = AIUtilityBiasMask.Social;
            var action1Factors = builder.Allocate(ref action1.Factors, 1);
            action1Factors[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0,
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            var blob = builder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static void SeedCohort(
            ref SystemState state,
            BlobAssetReference<AIUtilityArchetypeBlob> utilityBlob,
            byte cohort,
            float aggressionBias,
            float socialBias,
            float xBase)
        {
            for (var i = 0; i < AgentCountPerCohort; i++)
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(xBase, 0f, i * 0.35f)));
                state.EntityManager.AddComponentData(entity, new AIBiasProofMicroAgent { Cohort = cohort });
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
                state.EntityManager.AddComponentData(entity, new AISensorConfig
                {
                    UpdateInterval = 999f,
                    Range = 32f,
                    MaxResults = 1,
                    QueryOptions = SpatialQueryOptions.RequireDeterministicSorting,
                    PrimaryCategory = AISensorCategory.None,
                    SecondaryCategory = AISensorCategory.None
                });
                state.EntityManager.AddComponentData(entity, new BehaviorTuning
                {
                    AggressionBias = aggressionBias,
                    SocialBias = socialBias,
                    GreedBias = 1f,
                    CuriosityBias = 1f,
                    ObedienceBias = 1f
                });

                var readings = state.EntityManager.AddBuffer<AISensorReading>(entity);
                readings.Add(new AISensorReading
                {
                    Target = Entity.Null,
                    DistanceSq = 0f,
                    NormalizedScore = 1f,
                    CellId = -1,
                    SpatialVersion = 0u,
                    Category = AISensorCategory.None
                });

                state.EntityManager.AddBuffer<AIActionState>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AITaskResolutionSystem))]
    public partial struct AIBiasProofMicroMetricsSystem : ISystem
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.ai.biasproof.micro");

        private static readonly FixedString64Bytes GroupAAggressionChosenKey = new FixedString64Bytes("ai.biasproof.groupA.aggression_chosen");
        private static readonly FixedString64Bytes GroupBSocialChosenKey = new FixedString64Bytes("ai.biasproof.groupB.social_chosen");
        private static readonly FixedString64Bytes ScheduledCountKey = new FixedString64Bytes("ai.biasproof.scheduled_count");
        private static readonly FixedString64Bytes FiredCountKey = new FixedString64Bytes("ai.biasproof.fired_count");
        private static readonly FixedString64Bytes DigestKey = new FixedString64Bytes("ai.biasproof.digest");

        private ComponentLookup<AIBiasProofMicroAgent> _cohortLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            state.RequireForUpdate<TimeState>();
            _cohortLookup = state.GetComponentLookup<AIBiasProofMicroAgent>(isReadOnly: true);
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
            if (scenarioEntity == Entity.Null || !state.EntityManager.HasComponent<AIBiasProofMicroState>(scenarioEntity))
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            int groupAAggressionChosen = 0;
            int groupBSocialChosen = 0;
            int scheduledCount = 0;
            uint digestFold = 2166136261u;

            foreach (var (utility, cohort, entity) in SystemAPI.Query<RefRO<AIUtilityState>, RefRO<AIBiasProofMicroAgent>>().WithEntityAccess())
            {
                var utilityState = utility.ValueRO;
                var cohortId = cohort.ValueRO.Cohort;

                if (cohortId == 0 && utilityState.BestActionIndex == 0)
                {
                    groupAAggressionChosen++;
                }
                else if (cohortId == 1 && utilityState.BestActionIndex == 1)
                {
                    groupBSocialChosen++;
                }

                if (utilityState.LastEvaluationTick == timeState.Tick)
                {
                    scheduledCount++;
                }

                var itemDigest = math.hash(new uint4(
                    (uint)entity.Index,
                    cohortId,
                    utilityState.BestActionIndex,
                    utilityState.LastEvaluationTick));

                // Order-independent fold to keep digest deterministic across equivalent iteration orders.
                digestFold += itemDigest;
                digestFold ^= itemDigest * 16777619u;
            }

            _cohortLookup.Update(ref state);
            int firedCount = 0;
            if (SystemAPI.HasSingleton<AICommandQueueTag>())
            {
                var queueEntity = SystemAPI.GetSingletonEntity<AICommandQueueTag>();
                var commands = SystemAPI.GetBuffer<AICommand>(queueEntity);
                for (var i = 0; i < commands.Length; i++)
                {
                    if (_cohortLookup.HasComponent(commands[i].Agent))
                    {
                        firedCount++;
                    }
                }
            }

            var scenarioState = state.EntityManager.GetComponentData<AIBiasProofMicroState>(scenarioEntity);
            scenarioState.Digest = math.hash(new uint4(
                scenarioState.Digest,
                digestFold,
                timeState.Tick,
                (uint)(groupAAggressionChosen + (groupBSocialChosen << 16))));
            if (scenarioState.Digest == 0u)
            {
                scenarioState.Digest = 1u;
            }

            state.EntityManager.SetComponentData(scenarioEntity, scenarioState);

            var metricLookup = SystemAPI.GetBufferLookup<ScenarioMetricSample>(isReadOnly: false);
            metricLookup.Update(ref state);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, GroupAAggressionChosenKey, groupAAggressionChosen);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, GroupBSocialChosenKey, groupBSocialChosen);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ScheduledCountKey, scheduledCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, FiredCountKey, firedCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, DigestKey, scenarioState.Digest);
        }

        private Entity ResolveScenarioEntity(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<ScenarioEntitySingleton>(out var singleton))
            {
                var scenarioEntity = singleton.Value;
                if (scenarioEntity != Entity.Null && state.EntityManager.Exists(scenarioEntity))
                {
                    return scenarioEntity;
                }
            }

            var query = SystemAPI.QueryBuilder().WithAll<ScenarioInfo>().Build();
            return query.IsEmptyIgnoreFilter ? Entity.Null : query.GetSingletonEntity();
        }
    }
}
