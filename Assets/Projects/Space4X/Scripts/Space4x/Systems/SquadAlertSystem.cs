using Space4X.Combat;
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
    /// Propagates danger alerts from leaders/officers to squad members.
    /// Requires leadership level threshold (CommandSkill) to send alerts.
    /// Squad members receive alerts even without direct perception.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(DangerDetectionSystem))]
    public partial struct SquadAlertSystem : ISystem
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
            var dangerPerceptionLookup = SystemAPI.GetComponentLookup<DangerPerception>(true);
            var individualStatsLookup = SystemAPI.GetComponentLookup<IndividualStats>(true);
            var crewExpertiseLookup = SystemAPI.GetComponentLookup<CrewExpertise>(true);
            var translationLookup = SystemAPI.GetComponentLookup<Translation>(true);
            var detectedDangerLookup = SystemAPI.GetBufferLookup<DetectedDanger>(true);
            dangerPerceptionLookup.Update(ref state);
            individualStatsLookup.Update(ref state);
            crewExpertiseLookup.Update(ref state);
            translationLookup.Update(ref state);
            detectedDangerLookup.Update(ref state);

            // Propagate alerts from leaders
            new PropagateSquadAlertsJob
            {
                DangerPerceptionLookup = dangerPerceptionLookup,
                IndividualStatsLookup = individualStatsLookup,
                CrewExpertiseLookup = crewExpertiseLookup,
                TranslationLookup = translationLookup,
                DetectedDangerLookup = detectedDangerLookup,
                CurrentTick = currentTick
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct PropagateSquadAlertsJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<DangerPerception> DangerPerceptionLookup;

            [ReadOnly]
            public ComponentLookup<IndividualStats> IndividualStatsLookup;

            [ReadOnly]
            public ComponentLookup<CrewExpertise> CrewExpertiseLookup;

            [ReadOnly]
            public ComponentLookup<Translation> TranslationLookup;

            [ReadOnly]
            public BufferLookup<DetectedDanger> DetectedDangerLookup;

            public uint CurrentTick;

            void Execute(
                Entity leaderEntity,
                in DangerPerception perception,
                in Translation leaderTranslation,
                in DynamicBuffer<DetectedDanger> detectedDangers,
                ref DynamicBuffer<DangerAlert> alerts,
                in IndividualStats stats,
                in CrewExpertise expertise)
            {
                // Check if this entity can send alerts (has AlertSquad flag and sufficient leadership)
                if ((perception.EnabledResponses & DangerResponseFlags.AlertSquad) == 0)
                {
                    return; // Cannot send alerts
                }

                // Check leadership threshold (CommandSkill from IndividualStats or CrewExpertise)
                byte commandSkill = 0;
                if (IndividualStatsLookup.HasComponent(leaderEntity))
                {
                    commandSkill = (byte)math.clamp(IndividualStatsLookup[leaderEntity].Command, 0, 255);
                }
                else if (CrewExpertiseLookup.HasComponent(leaderEntity))
                {
                    commandSkill = CrewExpertiseLookup[leaderEntity].CommandSkill;
                }

                // Require minimum CommandSkill to send alerts (e.g., 50/100)
                const byte minCommandSkill = 50;
                if (commandSkill < minCommandSkill)
                {
                    return; // Leadership too low
                }

                // Alert range scales with CommandSkill
                float alertRange = perception.PerceptionRange * (1.0f + commandSkill / 200.0f); // Up to 1.5x range

                // For each detected danger, propagate alert to nearby entities
                for (int i = 0; i < detectedDangers.Length; i++)
                {
                    var danger = detectedDangers[i];

                    // TODO: In a full implementation, use spatial queries to find nearby squad members
                    // For now, this is a placeholder that would need integration with spatial systems
                    // and proper squad membership components

                    // Example structure:
                    // 1. Query entities within alertRange that are squad members
                    // 2. For each squad member:
                    //    - Add DangerAlert to their buffer
                    //    - Alert includes danger info and alerting entity
                }
            }
        }
    }
}

