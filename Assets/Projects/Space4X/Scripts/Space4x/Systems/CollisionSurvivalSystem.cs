using Space4X.Knowledge;
using Space4X.Individuals;
using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace Space4X.Systems
{
    /// <summary>
    /// Processes collision events and performs survival rolls.
    /// Calculates impact severity from relative velocity and mass, applies crew protection,
    /// and performs survival roll based on Physique + Resolve.
    /// Awards wisdom XP for survived collisions.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct CollisionSurvivalSystem : ISystem
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
            var currentTick = timeState.Tick;

            // Update lookups
            var healthLookup = SystemAPI.GetComponentLookup<Health>(true);
            var shieldLookup = SystemAPI.GetComponentLookup<Shield>(true);
            var individualStatsLookup = SystemAPI.GetComponentLookup<IndividualStats>(true);
            var physiqueLookup = SystemAPI.GetComponentLookup<PhysiqueFinesseWill>(true);
            var wisdomPoolLookup = SystemAPI.GetComponentLookup<WisdomPool>(true);
            healthLookup.Update(ref state);
            shieldLookup.Update(ref state);
            individualStatsLookup.Update(ref state);
            physiqueLookup.Update(ref state);
            wisdomPoolLookup.Update(ref state);

            // Process collision survival
            new ProcessCollisionSurvivalJob
            {
                HealthLookup = healthLookup,
                ShieldLookup = shieldLookup,
                IndividualStatsLookup = individualStatsLookup,
                PhysiqueLookup = physiqueLookup,
                WisdomPoolLookup = wisdomPoolLookup,
                CurrentTick = currentTick
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessCollisionSurvivalJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<Health> HealthLookup;

            [ReadOnly]
            public ComponentLookup<Shield> ShieldLookup;

            [ReadOnly]
            public ComponentLookup<IndividualStats> IndividualStatsLookup;

            [ReadOnly]
            public ComponentLookup<PhysiqueFinesseWill> PhysiqueLookup;

            [ReadOnly]
            public ComponentLookup<WisdomPool> WisdomPoolLookup;

            public uint CurrentTick;

            void Execute(
                Entity entity,
                ref CollisionSurvivalRoll survivalRoll,
                in DynamicBuffer<CollisionEvent> collisionEvents,
                in IndividualStats stats,
                in PhysiqueFinesseWill physique)
            {
                // Process collision events
                for (int i = 0; i < collisionEvents.Length; i++)
                {
                    var collision = collisionEvents[i];

                    // Calculate impact severity from relative velocity
                    // Higher velocity = higher severity
                    float impactSeverity = collision.RelativeVelocity / 100f; // Normalize (adjust divisor as needed)

                    // Calculate crew protection from shields and armor
                    float crewProtection = 0f;
                    if (ShieldLookup.HasComponent(entity))
                    {
                        var shield = ShieldLookup[entity];
                        // Shields absorb some impact (e.g., 50% reduction)
                        crewProtection += shield.Current * 0.5f;
                    }

                    // TODO: Add armor protection calculation
                    // Example: crewProtection += armorRating * armorProtectionFactor;

                    // Calculate survival chance: Physique + Resolve vs ImpactSeverity
                    float survivalStat = (physique.Physique + stats.Resolve) / 200f; // Normalize to 0-1
                    float survivalChance = math.saturate(survivalStat - (impactSeverity - crewProtection));

                    // Perform survival roll (deterministic based on tick + entity index)
                    // Using hash-based deterministic random
                    uint seed = (uint)(entity.Index + CurrentTick + collision.CollisionTick);
                    float roll = HashToFloat(seed);

                    SurvivalOutcome outcome;
                    if (roll < survivalChance * 0.2f)
                    {
                        outcome = SurvivalOutcome.Unscathed;
                    }
                    else if (roll < survivalChance * 0.5f)
                    {
                        outcome = SurvivalOutcome.MinorInjury;
                    }
                    else if (roll < survivalChance * 0.8f)
                    {
                        outcome = SurvivalOutcome.MajorInjury;
                    }
                    else if (roll < survivalChance)
                    {
                        outcome = SurvivalOutcome.Incapacitated;
                    }
                    else
                    {
                        outcome = SurvivalOutcome.Death;
                    }

                    // Update survival roll component
                    survivalRoll.ImpactSeverity = impactSeverity;
                    survivalRoll.CrewProtection = crewProtection;
                    survivalRoll.Outcome = outcome;
                    survivalRoll.RollTick = CurrentTick;

                    // Award wisdom XP for survived collisions (more for survived failures)
                    if (WisdomPoolLookup.HasComponent(entity))
                    {
                        var wisdomPool = WisdomPoolLookup[entity];
                        float wisdomGain = 0f;

                        if (outcome == SurvivalOutcome.Death)
                        {
                            // No XP for death
                        }
                        else if (outcome == SurvivalOutcome.Incapacitated)
                        {
                            wisdomGain = 5f; // Small XP for surviving severe impact
                        }
                        else if (outcome == SurvivalOutcome.MajorInjury)
                        {
                            wisdomGain = 10f; // Moderate XP
                        }
                        else if (outcome == SurvivalOutcome.MinorInjury)
                        {
                            wisdomGain = 15f; // Good XP
                        }
                        else if (outcome == SurvivalOutcome.Unscathed)
                        {
                            wisdomGain = 20f; // Best XP for avoiding injury
                        }

                        // Apply wisdom gain (would need to modify WisdomPool, but it's readonly here)
                        // TODO: Create WisdomGainEvent buffer or use ECB to modify WisdomPool
                    }

                    // TODO: Apply injury/death effects based on outcome
                    // - Minor/Major injury: Apply debuff or reduce stats temporarily
                    // - Incapacitated: Disable entity temporarily
                    // - Death: Trigger death system
                }
            }

            // Deterministic hash to float (0-1)
            private float HashToFloat(uint hash)
            {
                hash ^= hash >> 16;
                hash *= 0x85ebca6b;
                hash ^= hash >> 13;
                hash *= 0xc2b2ae35;
                hash ^= hash >> 16;
                return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
            }
        }
    }
}

