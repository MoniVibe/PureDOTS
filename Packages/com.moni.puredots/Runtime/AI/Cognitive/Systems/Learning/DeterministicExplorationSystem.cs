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
    /// Deterministic exploration system - 1Hz cognitive layer.
    /// Replaces random exploration with systematic tick-based variation.
    /// Guarantees identical discovery sequences across replays.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct DeterministicExplorationSystem : ISystem
    {
        private const float UpdateInterval = 1.0f; // 1Hz
        private const uint ExplorationPeriod = 100; // Cycle through exploration every 100 ticks
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

            // Exploration factor is computed per-agent in ProceduralLearningSystem
            // This system provides helper functions for deterministic exploration
        }

        /// <summary>
        /// Compute deterministic exploration factor for an agent.
        /// Uses tick-based variation to guarantee identical sequences across replays.
        /// </summary>
        [BurstCompile]
        public static float ComputeExplorationFactor(uint tick, Entity entity, uint period = ExplorationPeriod)
        {
            // Combine tick and entity index for deterministic but varied exploration
            uint combined = tick ^ (uint)(entity.Index * 17); // Simple hash
            return (combined % period) / (float)period; // Normalize to 0-1
        }

        /// <summary>
        /// Select exploration action based on deterministic factor.
        /// </summary>
        [BurstCompile]
        public static ActionId SelectExplorationAction(float explorationFactor, int actionCount = 8)
        {
            int actionIndex = (int)(explorationFactor * actionCount) % actionCount;
            return (ActionId)(actionIndex + 1); // Map to ActionId 1-8
        }
    }
}

