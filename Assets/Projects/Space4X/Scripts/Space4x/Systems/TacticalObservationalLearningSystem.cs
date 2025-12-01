using Space4X.Knowledge;
using Space4X.Individuals;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace Space4X.Systems
{
    /// <summary>
    /// Detects maneuver executions within observation range and grants XP toward ManeuverMastery.
    /// Awards XP based on observer's LearningModifier (derived from Finesse + TacticalExperience).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct TacticalObservationalLearningSystem : ISystem
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

            // Get tactical maneuver catalog
            if (!SystemAPI.TryGetSingleton<TacticalManeuverCatalogRef>(out var maneuverCatalogRef) ||
                !maneuverCatalogRef.Blob.IsCreated)
            {
                return;
            }

            var maneuverCatalog = maneuverCatalogRef.Blob.Value;

            // Update lookups
            var tacticalStateLookup = SystemAPI.GetComponentLookup<TacticalState>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var masteryLookup = SystemAPI.GetBufferLookup<ManeuverMastery>(true);
            var crewExpertiseLookup = SystemAPI.GetComponentLookup<CrewExpertise>(true);
            tacticalStateLookup.Update(ref state);
            transformLookup.Update(ref state);
            masteryLookup.Update(ref state);
            crewExpertiseLookup.Update(ref state);

            // Process observations
            new ProcessManeuverObservationsJob
            {
                ManeuverCatalog = maneuverCatalog,
                TacticalStateLookup = tacticalStateLookup,
                TransformLookup = transformLookup,
                MasteryLookup = masteryLookup,
                CrewExpertiseLookup = crewExpertiseLookup,
                CurrentTick = currentTick
            }.ScheduleParallel();

            // Grant XP from observations
            new GrantManeuverObservationXpJob
            {
                CurrentTick = currentTick
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessManeuverObservationsJob : IJobEntity
        {
            [ReadOnly]
            public TacticalManeuverCatalogBlob ManeuverCatalog;

            [ReadOnly]
            public ComponentLookup<TacticalState> TacticalStateLookup;

            [ReadOnly]
            public ComponentLookup<LocalTransform> TransformLookup;

            [ReadOnly]
            public BufferLookup<ManeuverMastery> MasteryLookup;

            [ReadOnly]
            public ComponentLookup<CrewExpertise> CrewExpertiseLookup;

            public uint CurrentTick;

            void Execute(
                Entity observerEntity,
                in TacticalObserver observer,
                in LocalTransform observerTransform,
                ref DynamicBuffer<ObservedManeuver> observations,
                in CrewExpertise expertise)
            {
                // Find entities performing maneuvers within observation range
                // Note: This is a simplified implementation. In a full system, you'd use spatial queries
                // For now, we'll check all entities with TacticalState

                // Limit simultaneous observations
                if (observations.Length >= observer.MaxSimultaneousObserve)
                {
                    return;
                }

                // Calculate learning modifier from Finesse + TacticalExperience
                // Assuming Finesse is in PhysiqueFinesseWill component (we'll need to check)
                // For now, use CombatSkill + TacticalExperience as proxy
                float learningMod = observer.LearningModifier;
                if (learningMod <= 0f)
                {
                    // Calculate from expertise if not set
                    learningMod = (expertise.CombatSkill + expertise.TacticalExperience) / 200f; // Normalize to 0-1
                }

                // TODO: In a full implementation, use spatial queries to find nearby entities
                // For now, this is a placeholder that would need integration with spatial systems
            }
        }

        [BurstCompile]
        public partial struct GrantManeuverObservationXpJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                Entity entity,
                ref DynamicBuffer<ObservedManeuver> observations,
                ref DynamicBuffer<ManeuverMastery> masteryBuffer,
                in TacticalObserver observer,
                in CrewExpertise expertise)
            {
                // Process observed maneuvers and grant XP
                for (int i = observations.Length - 1; i >= 0; i--)
                {
                    var observation = observations[i];

                    // Find or create mastery entry
                    int masteryIndex = -1;
                    for (int j = 0; j < masteryBuffer.Length; j++)
                    {
                        if (masteryBuffer[j].ManeuverId.Equals(observation.ManeuverId))
                        {
                            masteryIndex = j;
                            break;
                        }
                    }

                    ManeuverMastery mastery;
                    if (masteryIndex < 0)
                    {
                        // Create new mastery entry
                        mastery = new ManeuverMastery
                        {
                            ManeuverId = observation.ManeuverId,
                            MasteryProgress = 0f,
                            ObservationCount = 0,
                            PracticeAttempts = 0,
                            SuccessfulExecutions = 0,
                            FailedExecutions = 0,
                            Flags = ManeuverMasteryFlags.None
                        };
                        masteryBuffer.Add(mastery);
                        masteryIndex = masteryBuffer.Length - 1;
                    }
                    else
                    {
                        mastery = masteryBuffer[masteryIndex];
                    }

                    // Grant XP based on quality and learning modifier
                    float xpGain = observation.QualityFactor * observer.LearningModifier * 0.01f; // 1% per observation at max quality
                    mastery.MasteryProgress = math.min(4.0f, mastery.MasteryProgress + xpGain);
                    mastery.ObservationCount++;

                    // Update flags based on mastery progress
                    if (mastery.MasteryProgress >= 0.20f && (mastery.Flags & ManeuverMasteryFlags.Anticipated) == 0)
                    {
                        mastery.Flags |= ManeuverMasteryFlags.Anticipated;
                    }
                    if (mastery.MasteryProgress >= 1.0f && (mastery.Flags & ManeuverMasteryFlags.Proficient) == 0)
                    {
                        mastery.Flags |= ManeuverMasteryFlags.Proficient;
                    }
                    if (mastery.MasteryProgress >= 2.0f && (mastery.Flags & ManeuverMasteryFlags.Signature) == 0)
                    {
                        mastery.Flags |= ManeuverMasteryFlags.Signature;
                    }
                    if (mastery.MasteryProgress >= 4.0f && (mastery.Flags & ManeuverMasteryFlags.Master) == 0)
                    {
                        mastery.Flags |= ManeuverMasteryFlags.Master;
                    }

                    masteryBuffer[masteryIndex] = mastery;

                    // Remove processed observation
                    observations.RemoveAt(i);
                }
            }
        }
    }
}

