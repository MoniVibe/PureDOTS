using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Advanced
{
    /// <summary>
    /// Observational learning system - 1Hz cognitive layer.
    /// Allows agents to copy others' high-score action chains.
    /// Enables procedural knowledge diffusion through populations.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct ObservationalLearningSystem : ISystem
    {
        private const float UpdateInterval = 1.0f; // 1Hz
        private const float ObservationRange = 5.0f; // Range to observe other agents
        private const float ObservationalReinforcementMultiplier = 0.25f; // Reduced reinforcement for observed actions
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

            // Observational learning happens when agents witness successful actions
            // This system provides helper functions for recording observed actions
        }

        /// <summary>
        /// Record an observed action with reduced reinforcement.
        /// Called when an agent witnesses another agent's successful action.
        /// </summary>
        [BurstCompile]
        public static void RecordObservedAction(
            ref ProceduralMemory memory,
            byte contextHash,
            ActionId observedAction,
            float observedOutcome, // 0.0 = failure, 1.0 = success
            float learningRate,
            float wisdomMultiplier = 1.0f) // Higher wisdom accelerates learning
        {
            // Apply observational reinforcement multiplier
            float adjustedOutcome = observedOutcome * ObservationalReinforcementMultiplier * wisdomMultiplier;

            // Use existing reinforcement function with adjusted outcome
            ProceduralMemoryReinforcementSystem.ReinforceAction(
                ref memory,
                contextHash,
                observedAction,
                adjustedOutcome,
                learningRate);
        }
    }
}

