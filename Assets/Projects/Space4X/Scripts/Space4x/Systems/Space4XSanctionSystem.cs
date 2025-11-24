using PureDOTS.Runtime.Compliance;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Systems
{
    /// <summary>
    /// Applies sanctions based on infractions: fines, rep hits, interdictions, bounty flags.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(Space4XComplianceMonitorSystem))]
    public partial struct Space4XSanctionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ComplianceInfraction>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<ComplianceRuleCatalog>(out var ruleCatalog))
            {
                return;
            }

            var currentTick = timeState.Tick;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            var job = new SanctionApplicationJob
            {
                RuleCatalog = ruleCatalog.Catalog,
                CurrentTick = currentTick,
                ECB = ecbParallel
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct SanctionApplicationJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<ComplianceRuleCatalogBlob> RuleCatalog;
            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                in ComplianceInfraction infraction)
            {
                // Find rule
                if (!TryFindRule(RuleCatalog, infraction.RuleId, out var rule))
                {
                    ECB.DestroyEntity(entityInQueryIndex, entity);
                    return;
                }

                // Apply sanction based on enforcement level
                var enforcementLevel = (EnforcementLevel)rule.Enforcement;
                var sanctionMagnitude = rule.Magnitude * infraction.Severity;

                var sanction = new ComplianceSanction
                {
                    RuleId = rule.Id,
                    Level = enforcementLevel,
                    FineAmount = enforcementLevel == EnforcementLevel.Fine ? sanctionMagnitude : 0f,
                    ReputationHit = enforcementLevel == EnforcementLevel.ReputationHit ? sanctionMagnitude : 0f,
                    IsBountyFlagged = enforcementLevel == EnforcementLevel.Bounty,
                    SanctionTick = CurrentTick
                };

                // Add sanction to offender
                if (infraction.OffenderEntity != Entity.Null)
                {
                    ECB.AddComponent(entityInQueryIndex, infraction.OffenderEntity, sanction);
                }

                // Destroy infraction entity
                ECB.DestroyEntity(entityInQueryIndex, entity);
            }

            private bool TryFindRule(
                BlobAssetReference<ComplianceRuleCatalogBlob> catalog,
                FixedString32Bytes ruleId,
                out ComplianceRule rule)
            {
                rule = default;
                if (!catalog.IsCreated)
                {
                    return false;
                }

                var rules = catalog.Value.Rules;
                for (int i = 0; i < rules.Length; i++)
                {
                    if (rules[i].Id.Equals(ruleId))
                    {
                        rule = rules[i];
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

