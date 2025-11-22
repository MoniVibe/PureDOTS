using PureDOTS.Runtime.Alignment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Ships;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Ships
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CoreSingletonBootstrapSystem))]
    public partial struct CrewAggregationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CrewAggregate>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (SystemAPI.GetSingleton<RewindState>().Mode != RewindMode.Record)
            {
                return;
            }

            var thresholds = SystemAPI.TryGetSingleton<ComplianceThresholds>(out var singletonThresholds)
                ? singletonThresholds
                : ComplianceThresholds.CreateDefault();

            foreach (var (crew, entity) in SystemAPI.Query<RefRW<CrewAggregate>>().WithEntityAccess())
            {
                if (!state.EntityManager.HasComponent<CrewCompliance>(entity))
                {
                    state.EntityManager.AddComponent<CrewCompliance>(entity);
                }

                if (!state.EntityManager.HasBuffer<CrewAlignmentSample>(entity))
                {
                    state.EntityManager.AddBuffer<CrewAlignmentSample>(entity);
                }

                if (!state.EntityManager.HasBuffer<ComplianceAlert>(entity))
                {
                    state.EntityManager.AddBuffer<ComplianceAlert>(entity);
                }

                var samples = state.EntityManager.GetBuffer<CrewAlignmentSample>(entity);
                var alerts = state.EntityManager.GetBuffer<ComplianceAlert>(entity);
                var compliance = SystemAPI.GetComponent<CrewCompliance>(entity);
                alerts.Clear();

                float loyaltySum = 0f;
                float suspicionSum = 0f;
                float fanaticismSum = 0f;
                var count = 0;
                var missingData = (byte)0;
                DoctrineId doctrine = compliance.Doctrine;
                AffiliationId affiliation = compliance.Affiliation;

                if (state.EntityManager.HasComponent<DoctrineRef>(entity))
                {
                    doctrine = state.EntityManager.GetComponentData<DoctrineRef>(entity).Id;
                }

                for (int i = 0; i < samples.Length; i++)
                {
                    var sample = samples[i];
                    if (sample.Affiliation.Value.Length == 0)
                    {
                        missingData = 1;
                        continue;
                    }

                    loyaltySum += math.clamp(sample.Loyalty, 0f, 1f);
                    suspicionSum += math.max(0f, sample.Suspicion);
                    fanaticismSum += math.max(0f, sample.Fanaticism);
                    count++;
                    affiliation = sample.Affiliation;
                    if (doctrine.Value.Length == 0 && sample.Doctrine.Value.Length > 0)
                    {
                        doctrine = sample.Doctrine;
                    }
                }

                var avgLoyalty = count > 0 ? loyaltySum / count : 0f;
                var avgSuspicion = count > 0 ? suspicionSum / count : 0f;
                var avgFanaticism = count > 0 ? fanaticismSum / count : 0f;
                var delta = avgSuspicion - compliance.AverageSuspicion;

                var status = ComplianceStatus.Nominal;
                if (missingData != 0 || count == 0 || doctrine.Value.Length == 0)
                {
                    status = ComplianceStatus.Warning;
                }

                if (avgLoyalty <= thresholds.LoyaltyBreach || delta >= thresholds.SuspicionDeltaBreach)
                {
                    status = ComplianceStatus.Breach;
                }
                else if (avgLoyalty <= thresholds.LoyaltyWarning || delta >= thresholds.SuspicionDeltaWarning)
                {
                    status = (ComplianceStatus)math.max((int)status, (int)ComplianceStatus.Warning);
                }

                compliance.Affiliation = affiliation;
                compliance.Doctrine = doctrine;
                compliance.AverageLoyalty = avgLoyalty;
                compliance.AverageSuspicion = avgSuspicion;
                compliance.AverageFanaticism = avgFanaticism;
                compliance.SuspicionDelta = delta;
                compliance.Status = status;
                compliance.MissingData = missingData;
                compliance.LastUpdateTick = timeState.Tick;
                SystemAPI.SetComponent(entity, compliance);

                if (status != ComplianceStatus.Nominal)
                {
                    alerts.Add(new ComplianceAlert
                    {
                        Affiliation = affiliation,
                        Doctrine = doctrine,
                        Status = status,
                        SuspicionDelta = delta,
                        Loyalty = avgLoyalty,
                        Suspicion = avgSuspicion,
                        MissingData = missingData
                    });
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
