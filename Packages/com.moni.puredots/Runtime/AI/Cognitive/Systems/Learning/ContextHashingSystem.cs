using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Learning
{
    /// <summary>
    /// Context hashing system - 1Hz cognitive layer.
    /// Computes context hash from terrain + obstacle + goal for procedural learning.
    /// Uses Hamming distance for generalization.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct ContextHashingSystem : ISystem
    {
        private const float UpdateInterval = 1.0f; // 1Hz
        private float _lastUpdateTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            _lastUpdateTime = 0f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record && rewind.Mode != RewindMode.CatchUp)
            {
                return;
            }

            var tickTime = SystemAPI.GetSingleton<TickTimeState>();
            if (tickTime.IsPaused)
            {
                return;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            _lastUpdateTime = currentTime;

            var job = new ContextHashingJob
            {
                CurrentTick = tickTime.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ContextHashingJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex,
                in LocalTransform transform,
                ref ContextHash contextHash)
            {
                // Compute context from environment (simplified - would sample terrain/obstacle data)
                // For now, use position-based heuristics
                
                TerrainType terrainType = InferTerrainType(transform.Position);
                ObstacleTag obstacleTag = ObstacleTag.None; // Would be detected by other systems
                GoalType goalType = GoalType.None; // Would come from goal system

                // Compute hash
                byte hash = ContextHashHelper.ComputeHash(terrainType, obstacleTag, goalType);

                contextHash.TerrainType = terrainType;
                contextHash.ObstacleTag = obstacleTag;
                contextHash.GoalType = goalType;
                contextHash.Hash = hash;
                contextHash.LastComputedTick = CurrentTick;
            }

            private TerrainType InferTerrainType(float3 position)
            {
                // Simplified terrain inference based on Y position
                // In full implementation, would sample terrain grid
                if (position.y < -5f)
                {
                    return TerrainType.Pit;
                }
                else if (position.y > 10f)
                {
                    return TerrainType.Mountainous;
                }
                else if (position.y > 5f)
                {
                    return TerrainType.Hilly;
                }
                else
                {
                    return TerrainType.Flat;
                }
            }
        }
    }
}

