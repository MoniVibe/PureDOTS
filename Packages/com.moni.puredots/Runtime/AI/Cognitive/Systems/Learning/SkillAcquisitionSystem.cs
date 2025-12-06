using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Learning
{
    /// <summary>
    /// Skill acquisition system - 1Hz cognitive layer.
    /// Compresses procedural memories into macro-actions after repeated success.
    /// Example: [Push Box] + [Climb] + [Jump] → "EscapePit"
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct SkillAcquisitionSystem : ISystem
    {
        private const float UpdateInterval = 1.0f; // 1Hz
        private const int SuccessThreshold = 5; // Number of successes required to create macro-action
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

            var job = new SkillAcquisitionJob
            {
                CurrentTick = tickTime.Tick,
                SuccessThreshold = SuccessThreshold
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct SkillAcquisitionJob : IJobEntity
        {
            public uint CurrentTick;
            public int SuccessThreshold;

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex,
                ref ProceduralMemory memory)
            {
                // Check if we have a successful action chain that should become a macro-action
                // Look for sequences of actions with high success scores
                if (memory.TriedActions.Length < 3)
                {
                    return; // Need at least 3 actions for a macro-action
                }

                // Find action sequences with consistently high success scores
                int successCount = 0;
                for (int i = 0; i < memory.SuccessScores.Length; i++)
                {
                    if (memory.SuccessScores[i] > 0.7f) // High success threshold
                    {
                        successCount++;
                    }
                }

                // If we have enough successful actions, create a macro-action
                if (successCount >= SuccessThreshold && memory.SuccessChainCount < 10)
                {
                    // Example: Create "EscapePit" macro-action if we have Push + Climb + Jump sequence
                    bool hasPush = false;
                    bool hasClimb = false;
                    bool hasJump = false;

                    for (int i = 0; i < memory.TriedActions.Length; i++)
                    {
                        var action = memory.TriedActions[i];
                        if (action == ActionId.Push) hasPush = true;
                        if (action == ActionId.Climb) hasClimb = true;
                        if (action == ActionId.Jump) hasJump = true;
                    }

                    if (hasPush && hasClimb && hasJump)
                    {
                        // Add EscapePit macro-action to memory
                        // In full implementation, would create BehaviorNode component
                        memory.SuccessChainCount++;
                    }
                }
            }
        }
    }
}

