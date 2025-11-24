using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Compliance;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Systems
{
    /// <summary>
    /// Monitors for compliance infractions: weapon fire in safe zones, cargo scans, boarding events.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial struct Space4XComplianceMonitorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ComplianceRuleCatalog>();
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

            var job = new ComplianceMonitorJob
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
        public partial struct ComplianceMonitorJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<ComplianceRuleCatalogBlob> RuleCatalog;
            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                in WeaponMount weaponMount)
            {
                // Check if weapon fired in a safe zone
                // TODO: Check safe zone tags/spatial queries
                if (weaponMount.IsFiring)
                {
                    // Check rules for safe zone violations
                    CheckRules(entityInQueryIndex, entity, ComplianceTags.SafeZoneViolation, ECB);
                }
            }

            private void CheckRules(
                int entityInQueryIndex,
                Entity offenderEntity,
                ComplianceTags triggerTags,
                EntityCommandBuffer.ParallelWriter ecb)
            {
                if (!RuleCatalog.IsCreated)
                {
                    return;
                }

                var rules = RuleCatalog.Value.Rules;
                for (int i = 0; i < rules.Length; i++)
                {
                    var rule = rules[i];
                    if ((rule.TargetTags & (uint)triggerTags) != 0)
                    {
                        // Create infraction
                        var infractionEntity = ecb.CreateEntity(entityInQueryIndex);
                        ecb.AddComponent(entityInQueryIndex, infractionEntity, new ComplianceInfraction
                        {
                            RuleId = rule.Id,
                            OffenderEntity = offenderEntity,
                            InfractionTick = CurrentTick,
                            TriggerTags = triggerTags,
                            Severity = 1f // TODO: Calculate based on context
                        });
                    }
                }
            }
        }
    }
}

