using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Advanced
{
    /// <summary>
    /// Predictive simulation system - 1Hz cognitive layer (optional).
    /// Runs internal dry-runs using causal graphs to simulate action effects.
    /// Picks plan with highest expected utility.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct PredictiveSimulationSystem : ISystem
    {
        private const float UpdateInterval = 1.0f; // 1Hz
        private const int MaxSimulations = 5; // Maximum number of plans to simulate
        private const float SimulationCost = 0.1f; // Focus resource cost per simulation
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

            // Predictive simulation provides helper functions for mental rehearsal
            // Actual simulation happens in planning systems that query causal graphs
        }

        /// <summary>
        /// Simulate action effect using causal graph.
        /// Returns expected utility based on causal link weights.
        /// </summary>
        [BurstCompile]
        public static float SimulateActionEffect(
            in DynamicBuffer<CausalLink> causalLinks,
            ActionId action,
            OutcomeId desiredOutcome)
        {
            ushort cause = (ushort)action;
            ushort effect = (ushort)desiredOutcome;

            return CausalChainSystem.QueryCausalLink(causalLinks, cause, effect);
        }

        /// <summary>
        /// Find best action plan by simulating multiple actions.
        /// Returns action with highest expected utility.
        /// </summary>
        [BurstCompile]
        public static ActionId FindBestPlan(
            in DynamicBuffer<CausalLink> causalLinks,
            in FixedList64Bytes<ActionId> candidateActions,
            OutcomeId desiredOutcome)
        {
            float bestUtility = 0f;
            ActionId bestAction = ActionId.None;

            for (int i = 0; i < candidateActions.Length && i < MaxSimulations; i++)
            {
                var action = candidateActions[i];
                float utility = SimulateActionEffect(causalLinks, action, desiredOutcome);

                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    bestAction = action;
                }
            }

            return bestAction;
        }
    }
}

