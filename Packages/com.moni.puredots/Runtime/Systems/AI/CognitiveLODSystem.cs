using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Cognitive LOD assignment and management system.
    /// Assigns LOD based on distance, importance, and CPU load.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(SpatialSystemGroup))]
    public partial struct CognitiveLODSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Update LOD assignments
            var lodQuery = state.GetEntityQuery(typeof(CognitiveLOD), typeof(LocalTransform));
            
            if (lodQuery.IsEmpty)
            {
                return;
            }

            // Get camera/player position for distance calculation
            var cameraPos = float3.zero; // Would get from camera system
            var cpuLoad = 0.5f; // Would get from profiler

            var job = new UpdateLODJob
            {
                CameraPosition = cameraPos,
                CPULoadFactor = cpuLoad,
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(lodQuery, state.Dependency);

            // Update LOD state distribution
            UpdateLODDistribution(ref state, tickState.Tick);
        }

        [BurstCompile]
        private void UpdateLODDistribution(ref SystemState state, uint currentTick)
        {
            if (!SystemAPI.TryGetSingletonEntity<CognitiveLODState>(out var stateEntity))
            {
                return;
            }

            var lodQuery = state.GetEntityQuery(typeof(CognitiveLOD));
            var lodArray = lodQuery.ToComponentDataArray<CognitiveLOD>(Allocator.Temp);

            var distribution = new CognitiveLODState
            {
                HighCount = 0,
                MediumCount = 0,
                LowCount = 0,
                SleepCount = 0,
                LastDistributionTick = currentTick
            };

            for (int i = 0; i < lodArray.Length; i++)
            {
                switch (lodArray[i].Detail)
                {
                    case CognitiveDetail.High:
                        distribution.HighCount++;
                        break;
                    case CognitiveDetail.Medium:
                        distribution.MediumCount++;
                        break;
                    case CognitiveDetail.Low:
                        distribution.LowCount++;
                        break;
                    case CognitiveDetail.Sleep:
                        distribution.SleepCount++;
                        break;
                }
            }

            SystemAPI.SetComponent(stateEntity, distribution);
            lodArray.Dispose();
        }

        [BurstCompile]
        private partial struct UpdateLODJob : IJobEntity
        {
            public float3 CameraPosition;
            public float CPULoadFactor;
            public uint CurrentTick;

            public void Execute(
                ref CognitiveLOD lod,
                in LocalTransform transform)
            {
                // Calculate distance score (closer = higher)
                var distance = math.distance(transform.Position, CameraPosition);
                lod.DistanceScore = 1f / (1f + distance * 0.1f); // Normalize

                // Update CPU load factor
                lod.CPULoadFactor = CPULoadFactor;

                // Determine LOD based on distance, importance, and CPU load
                var combinedScore = lod.DistanceScore * 0.5f + lod.ImportanceScore * 0.5f;

                // Adjust for CPU load (higher load = lower LOD)
                var cpuPenalty = CPULoadFactor * 0.3f;
                combinedScore = math.max(0f, combinedScore - cpuPenalty);

                // Assign LOD
                if (combinedScore > 0.7f && CPULoadFactor < 0.8f)
                {
                    lod.Detail = CognitiveDetail.High;
                }
                else if (combinedScore > 0.4f && CPULoadFactor < 0.9f)
                {
                    lod.Detail = CognitiveDetail.Medium;
                }
                else if (combinedScore > 0.1f)
                {
                    lod.Detail = CognitiveDetail.Low;
                }
                else
                {
                    lod.Detail = CognitiveDetail.Sleep;
                }

                lod.LastLODUpdateTick = CurrentTick;
            }
        }
    }
}

