using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Combat.Targeting;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Selects targets for entities based on their targeting strategy.
    /// Queries spatial grid for potential targets and scores them.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial struct TargetSelectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;

            // Get spatial grid if available
            bool hasSpatialGrid = SystemAPI.TryGetSingleton<SpatialGridState>(out var gridState);

            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var healthLookup = SystemAPI.GetComponentLookup<Health>(true);
            var factionLookup = SystemAPI.GetComponentLookup<FactionId>(true);
            var highValueLookup = SystemAPI.GetComponentLookup<HighValueTargetTag>(true);

            new TargetSelectionJob
            {
                CurrentTick = currentTick,
                TransformLookup = transformLookup,
                HealthLookup = healthLookup,
                FactionLookup = factionLookup,
                HighValueLookup = highValueLookup
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct TargetSelectionJob : IJobEntity
        {
            public uint CurrentTick;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<Health> HealthLookup;
            [ReadOnly] public ComponentLookup<FactionId> FactionLookup;
            [ReadOnly] public ComponentLookup<HighValueTargetTag> HighValueLookup;

            void Execute(
                Entity entity,
                ref TargetPriority priority,
                ref DynamicBuffer<PotentialTarget> potentialTargets,
                in DynamicBuffer<ThreatSource> threatSources,
                in LocalTransform transform)
            {
                // Check if we should keep current target (target lock)
                if (priority.CurrentTarget != Entity.Null &&
                    priority.Strategy != TargetingStrategy.PlayerAssigned)
                {
                    uint ticksSinceSelected = CurrentTick - priority.TargetSelectedTick;
                    if (ticksSinceSelected < priority.TargetLockDuration && priority.AllowAutoSwitch)
                    {
                        // Still in target lock period - validate target still exists
                        if (TransformLookup.HasComponent(priority.CurrentTarget))
                        {
                            return; // Keep current target
                        }
                    }
                }

                // Player-assigned targets don't auto-switch
                if (priority.Strategy == TargetingStrategy.PlayerAssigned &&
                    priority.CurrentTarget != Entity.Null)
                {
                    // Validate target still exists
                    if (TransformLookup.HasComponent(priority.CurrentTarget))
                    {
                        return;
                    }
                    // Target gone - clear it
                    priority.CurrentTarget = Entity.Null;
                    return;
                }

                // Score potential targets
                if (potentialTargets.Length == 0)
                {
                    priority.CurrentTarget = Entity.Null;
                    priority.ThreatScore = 0f;
                    return;
                }

                // Update threat scores from threat sources
                for (int i = 0; i < potentialTargets.Length; i++)
                {
                    var target = potentialTargets[i];

                    // Check if this target is in our threat sources
                    for (int j = 0; j < threatSources.Length; j++)
                    {
                        if (threatSources[j].Source == target.Target)
                        {
                            target.ThreatScore += threatSources[j].ThreatAmount;
                            target.IsAttackingUs = true;
                            break;
                        }
                    }

                    // Check if high-value target
                    if (HighValueLookup.HasComponent(target.Target))
                    {
                        target.IsHighValue = true;
                        var hvt = HighValueLookup[target.Target];
                        target.ThreatScore += hvt.PriorityModifier;
                    }

                    // Update health percentage
                    if (HealthLookup.HasComponent(target.Target))
                    {
                        var health = HealthLookup[target.Target];
                        target.HealthPercent = health.MaxHealth > 0 ? health.Current / health.MaxHealth : 0f;
                    }

                    potentialTargets[i] = target;
                }

                // Select best target based on strategy
                Entity bestTarget = TargetSelectionHelpers.SelectBest(
                    potentialTargets,
                    priority.Strategy,
                    CurrentTick + (uint)entity.Index);

                if (bestTarget != priority.CurrentTarget)
                {
                    priority.CurrentTarget = bestTarget;
                    priority.TargetSelectedTick = CurrentTick;

                    // Update threat score from selected target
                    for (int i = 0; i < potentialTargets.Length; i++)
                    {
                        if (potentialTargets[i].Target == bestTarget)
                        {
                            priority.ThreatScore = potentialTargets[i].ThreatScore;
                            break;
                        }
                    }
                }

                priority.LastEngagedTick = CurrentTick;
            }
        }
    }

    /// <summary>
    /// Updates threat sources based on incoming damage.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(TargetSelectionSystem))]
    public partial struct ThreatUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            float deltaTime = timeState.DeltaTime;
            uint currentTick = timeState.Tick;

            // Get default config
            TargetSelectionConfig config;
            if (SystemAPI.TryGetSingleton<TargetSelectionConfig>(out var configSingleton))
            {
                config = configSingleton;
            }
            else
            {
                config = new TargetSelectionConfig
                {
                    ThreatDecayRate = 5f,
                    DamageThreatMultiplier = 1f
                };
            }

            new ThreatUpdateJob
            {
                DeltaTime = deltaTime,
                CurrentTick = currentTick,
                ThreatDecayRate = config.ThreatDecayRate,
                DamageThreatMultiplier = config.DamageThreatMultiplier
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ThreatUpdateJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;
            public float ThreatDecayRate;
            public float DamageThreatMultiplier;

            void Execute(
                ref DynamicBuffer<ThreatSource> threatSources,
                in DynamicBuffer<DamageEvent> damageEvents)
            {
                // Add threat from damage events
                for (int i = 0; i < damageEvents.Length; i++)
                {
                    var dmg = damageEvents[i];
                    if (dmg.SourceEntity == Entity.Null)
                    {
                        continue;
                    }

                    // Find or create threat source entry
                    bool found = false;
                    for (int j = 0; j < threatSources.Length; j++)
                    {
                        if (threatSources[j].Source == dmg.SourceEntity)
                        {
                            var source = threatSources[j];
                            source.ThreatAmount += dmg.RawDamage * DamageThreatMultiplier;
                            source.LastThreatTick = CurrentTick;
                            threatSources[j] = source;
                            found = true;
                            break;
                        }
                    }

                    if (!found && threatSources.Length < threatSources.Capacity)
                    {
                        threatSources.Add(new ThreatSource
                        {
                            Source = dmg.SourceEntity,
                            ThreatAmount = dmg.RawDamage * DamageThreatMultiplier,
                            LastThreatTick = CurrentTick
                        });
                    }
                }

                // Decay threat over time
                for (int i = threatSources.Length - 1; i >= 0; i--)
                {
                    var source = threatSources[i];
                    source.ThreatAmount -= ThreatDecayRate * DeltaTime;

                    if (source.ThreatAmount <= 0f)
                    {
                        threatSources.RemoveAt(i);
                    }
                    else
                    {
                        threatSources[i] = source;
                    }
                }
            }
        }
    }
}

