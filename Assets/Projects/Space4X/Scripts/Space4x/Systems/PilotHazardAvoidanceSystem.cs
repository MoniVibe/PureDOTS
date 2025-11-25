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
    /// Skill-based hazard avoidance system for pilots.
    /// Runs continuously (not just during maneuvers) to adjust path based on detected hazards.
    /// Green pilots execute path exactly; veterans adjust gradually; masters avoid fluidly.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(DangerDetectionSystem))]
    public partial struct PilotHazardAvoidanceSystem : ISystem
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
            var deltaTime = timeState.DeltaTime;

            // Update lookups
            var pilotSkillLookup = SystemAPI.GetComponentLookup<PilotSkillModifiers>(true);
            var dangerSourceLookup = SystemAPI.GetComponentLookup<DangerSource>(true);
            var translationLookup = SystemAPI.GetComponentLookup<Translation>(true);
            var detectedDangerLookup = SystemAPI.GetBufferLookup<DetectedDanger>(true);
            pilotSkillLookup.Update(ref state);
            dangerSourceLookup.Update(ref state);
            translationLookup.Update(ref state);
            detectedDangerLookup.Update(ref state);

            // Process hazard avoidance
            new ProcessHazardAvoidanceJob
            {
                PilotSkillLookup = pilotSkillLookup,
                DangerSourceLookup = dangerSourceLookup,
                TranslationLookup = translationLookup,
                DetectedDangerLookup = detectedDangerLookup,
                CurrentTick = currentTick,
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessHazardAvoidanceJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<PilotSkillModifiers> PilotSkillLookup;

            [ReadOnly]
            public ComponentLookup<DangerSource> DangerSourceLookup;

            [ReadOnly]
            public ComponentLookup<Translation> TranslationLookup;

            [ReadOnly]
            public BufferLookup<DetectedDanger> DetectedDangerLookup;

            public uint CurrentTick;
            public float DeltaTime;

            void Execute(
                Entity entity,
                ref HazardAvoidanceState avoidanceState,
                in Translation translation,
                in PilotSkillModifiers pilotSkills)
            {
                // Green pilots (low mastery): no adjustment
                if (pilotSkills.HazardAwarenessRadius <= 0f)
                {
                    avoidanceState.CurrentAdjustment = float3.zero;
                    avoidanceState.AvoidingEntity = Entity.Null;
                    avoidanceState.AvoidanceUrgency = 0f;
                    return;
                }

                // Scan for hazards within awareness radius
                // TODO: In a full implementation, use spatial queries to find DangerSource entities
                // For now, check detected dangers buffer
                if (!DetectedDangerLookup.HasBuffer(entity))
                {
                    return;
                }

                var detectedDangers = DetectedDangerLookup[entity];
                float3 avoidanceVector = float3.zero;
                float maxUrgency = 0f;
                Entity closestDanger = Entity.Null;

                for (int i = 0; i < detectedDangers.Length; i++)
                {
                    var danger = detectedDangers[i];

                    // Check if danger is within awareness radius
                    float3 dangerPos = danger.PredictedImpactPos;
                    float3 toDanger = dangerPos - translation.Value;
                    float distance = math.length(toDanger);

                    if (distance > pilotSkills.HazardAwarenessRadius)
                    {
                        continue;
                    }

                    // Calculate avoidance urgency (closer = more urgent)
                    float urgency = 1.0f - (distance / pilotSkills.HazardAwarenessRadius);
                    if (urgency > maxUrgency)
                    {
                        maxUrgency = urgency;
                        closestDanger = danger.DangerEntity;
                    }

                    // Calculate avoidance direction (away from danger)
                    float3 awayFromDanger = -math.normalize(toDanger);
                    avoidanceVector += awayFromDanger * urgency;
                }

                // Normalize and scale avoidance vector
                if (math.lengthsq(avoidanceVector) > 0.001f)
                {
                    avoidanceVector = math.normalize(avoidanceVector);
                    float adjustmentMagnitude = maxUrgency * pilotSkills.PathAdjustmentRate * DeltaTime;
                    avoidanceVector *= math.min(adjustmentMagnitude, pilotSkills.MaxDeviationAngle * math.PI / 180f);
                }

                // Update avoidance state
                avoidanceState.CurrentAdjustment = avoidanceVector;
                avoidanceState.AvoidingEntity = closestDanger;
                avoidanceState.AvoidanceUrgency = maxUrgency;

                // TODO: Apply avoidanceVector to steering/velocity inputs
                // This would integrate with the movement system to modify vessel trajectory
            }
        }
    }
}

