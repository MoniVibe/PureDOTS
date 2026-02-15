using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Systems.AI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Scenarios
{
    public struct AITierCadenceMicroAgent : IComponentData
    {
        public AILODTier Tier;
    }

    public struct AITierCadenceMicroState : IComponentData
    {
        public BlobAssetReference<AIUtilityArchetypeBlob> UtilityBlob;
        public uint Digest;
        public int Tier0EvalCount;
        public int Tier1EvalCount;
        public int Tier2EvalCount;
        public int Tier3EvalCount;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ScenarioEntityBootstrapSystem))]
    public partial struct AITierCadenceMicroScenarioSystem : ISystem
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.ai.tiercadence.micro");
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

            if (state.EntityManager.HasComponent<AITierCadenceMicroState>(scenarioEntity))
            {
                state.Enabled = false;
                return;
            }

            if (SystemAPI.HasSingleton<MindCadenceSettings>())
            {
                var cadence = SystemAPI.GetSingletonRW<MindCadenceSettings>();
                cadence.ValueRW.EvaluationCadenceTicks = 1;
            }

            var utilityBlob = CreateUtilityBlob();
            SeedTier(ref state, utilityBlob, AILODTier.Tier0_Full, -6f);
            SeedTier(ref state, utilityBlob, AILODTier.Tier1_Reduced, -2f);
            SeedTier(ref state, utilityBlob, AILODTier.Tier2_EventDriven, 2f);
            SeedTier(ref state, utilityBlob, AILODTier.Tier3_Aggregate, 6f);

            state.EntityManager.AddComponentData(scenarioEntity, new AITierCadenceMicroState
            {
                UtilityBlob = utilityBlob,
                Digest = 2166136261u,
                Tier0EvalCount = 0,
                Tier1EvalCount = 0,
                Tier2EvalCount = 0,
                Tier3EvalCount = 0
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
                !state.EntityManager.HasComponent<AITierCadenceMicroState>(scenarioEntity))
            {
                return;
            }

            var scenarioState = state.EntityManager.GetComponentData<AITierCadenceMicroState>(scenarioEntity);
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
                if (singleton.Value != Entity.Null && state.EntityManager.Exists(singleton.Value))
                {
                    return singleton.Value;
                }
            }

            var query = SystemAPI.QueryBuilder().WithAll<ScenarioInfo>().Build();
            return query.IsEmptyIgnoreFilter ? Entity.Null : query.GetSingletonEntity();
        }

        private static BlobAssetReference<AIUtilityArchetypeBlob> CreateUtilityBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AIUtilityArchetypeBlob>();
            var actions = builder.Allocate(ref root.Actions, 1);

            ref var action0 = ref actions[0];
            action0.BiasMask = AIUtilityBiasMask.None;
            var factors = builder.Allocate(ref action0.Factors, 1);
            factors[0] = new AIUtilityCurveBlob
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

        private static void SeedTier(
            ref SystemState state,
            BlobAssetReference<AIUtilityArchetypeBlob> utilityBlob,
            AILODTier tier,
            float x)
        {
            for (var i = 0; i < AgentsPerTier; i++)
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(x, 0f, i * 0.35f)));
                state.EntityManager.AddComponentData(entity, new AIBehaviourArchetype { UtilityBlob = utilityBlob });
                state.EntityManager.AddComponentData(entity, new AIUtilityState
                {
                    BestActionIndex = 0,
                    BestScore = 0f,
                    LastEvaluationTick = 0u
                });
                state.EntityManager.AddComponentData(entity, new AISensorConfig
                {
                    UpdateInterval = 999f,
                    Range = 20f,
                    MaxResults = 1,
                    QueryOptions = 0,
                    PrimaryCategory = AISensorCategory.None,
                    SecondaryCategory = AISensorCategory.None
                });
                state.EntityManager.AddComponentData(entity, new AIFidelityTier
                {
                    Tier = tier,
                    LastChangeTick = 0u,
                    ReasonMask = 0
                });
                state.EntityManager.AddComponentData(entity, new AITierCadenceMicroAgent { Tier = tier });

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
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AITaskResolutionSystem))]
    public partial struct AITierCadenceMicroMetricsSystem : ISystem
    {
        private static readonly FixedString64Bytes TargetScenarioId = new FixedString64Bytes("scenario.ai.tiercadence.micro");
        private static readonly FixedString64Bytes Tier0EvalCountKey = new FixedString64Bytes("ai.tiercadence.tier0.eval_count");
        private static readonly FixedString64Bytes Tier1EvalCountKey = new FixedString64Bytes("ai.tiercadence.tier1.eval_count");
        private static readonly FixedString64Bytes Tier2EvalCountKey = new FixedString64Bytes("ai.tiercadence.tier2.eval_count");
        private static readonly FixedString64Bytes Tier3EvalCountKey = new FixedString64Bytes("ai.tiercadence.tier3.eval_count");
        private static readonly FixedString64Bytes DigestKey = new FixedString64Bytes("ai.tiercadence.digest");

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            state.RequireForUpdate<TimeState>();
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
            if (scenarioEntity == Entity.Null || !state.EntityManager.HasComponent<AITierCadenceMicroState>(scenarioEntity))
            {
                return;
            }

            var time = SystemAPI.GetSingleton<TimeState>();
            var stateData = state.EntityManager.GetComponentData<AITierCadenceMicroState>(scenarioEntity);

            var tickTier0 = 0;
            var tickTier1 = 0;
            var tickTier2 = 0;
            var tickTier3 = 0;
            uint tickFold = 0u;

            foreach (var (utility, marker, entity) in SystemAPI.Query<RefRO<AIUtilityState>, RefRO<AITierCadenceMicroAgent>>().WithEntityAccess())
            {
                if (utility.ValueRO.LastEvaluationTick != time.Tick)
                {
                    continue;
                }

                switch (marker.ValueRO.Tier)
                {
                    case AILODTier.Tier0_Full:
                        tickTier0++;
                        break;
                    case AILODTier.Tier1_Reduced:
                        tickTier1++;
                        break;
                    case AILODTier.Tier2_EventDriven:
                        tickTier2++;
                        break;
                    case AILODTier.Tier3_Aggregate:
                        tickTier3++;
                        break;
                }

                var eventHash = math.hash(new uint4(
                    time.Tick,
                    (uint)entity.Index,
                    (uint)marker.ValueRO.Tier,
                    utility.ValueRO.LastEvaluationTick));
                tickFold += eventHash;
                tickFold ^= eventHash * 16777619u;
            }

            stateData.Tier0EvalCount += tickTier0;
            stateData.Tier1EvalCount += tickTier1;
            stateData.Tier2EvalCount += tickTier2;
            stateData.Tier3EvalCount += tickTier3;
            stateData.Digest = math.hash(new uint4(
                stateData.Digest,
                tickFold,
                (uint)(stateData.Tier0EvalCount ^ (stateData.Tier1EvalCount << 8)),
                (uint)(stateData.Tier2EvalCount ^ (stateData.Tier3EvalCount << 8))));
            if (stateData.Digest == 0u)
            {
                stateData.Digest = 1u;
            }

            state.EntityManager.SetComponentData(scenarioEntity, stateData);

            var metricLookup = SystemAPI.GetBufferLookup<ScenarioMetricSample>(false);
            metricLookup.Update(ref state);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier0EvalCountKey, stateData.Tier0EvalCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier1EvalCountKey, stateData.Tier1EvalCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier2EvalCountKey, stateData.Tier2EvalCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, Tier3EvalCountKey, stateData.Tier3EvalCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, DigestKey, stateData.Digest);
        }

        private Entity ResolveScenarioEntity(ref SystemState state)
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
