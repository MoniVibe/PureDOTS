using Space4X.Combat;
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
    /// Detects danger sources within perception range using spatial queries.
    /// Populates DetectedDanger buffer for entities with DangerPerception.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct DangerDetectionSystem : ISystem
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
            var dangerSourceLookup = SystemAPI.GetComponentLookup<DangerSource>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            dangerSourceLookup.Update(ref state);
            transformLookup.Update(ref state);

            // Detect dangers
            new DetectDangersJob
            {
                DangerSourceLookup = dangerSourceLookup,
                TransformLookup = transformLookup,
                CurrentTick = currentTick,
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct DetectDangersJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<DangerSource> DangerSourceLookup;

            [ReadOnly]
            public ComponentLookup<LocalTransform> TransformLookup;

            public uint CurrentTick;
            public float DeltaTime;

            void Execute(
                Entity entity,
                in DangerPerception perception,
                in LocalTransform transform,
                ref DynamicBuffer<DetectedDanger> detectedDangers)
            {
                // Clear old detections (older than perception reaction time)
                for (int i = detectedDangers.Length - 1; i >= 0; i--)
                {
                    var detection = detectedDangers[i];
                    if (detection.DetectedTick + (uint)(perception.ReactionTime * 60f) < CurrentTick)
                    {
                        detectedDangers.RemoveAt(i);
                    }
                }

                // TODO: In a full implementation, use spatial queries (e.g., Unity Physics OverlapSphere)
                // to find DangerSource entities within perception range
                // For now, this is a placeholder that would need integration with spatial systems

                // Example structure for spatial query:
                // 1. Query all entities with DangerSource component within perceptionRange
                // 2. For each danger source:
                //    - Calculate predicted impact position (projectile trajectory, AOE center, etc.)
                //    - Calculate time to impact
                //    - Add to DetectedDanger buffer if within perception range and level
            }
        }
    }
}
