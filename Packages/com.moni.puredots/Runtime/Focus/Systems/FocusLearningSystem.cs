using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Focus;

namespace PureDOTS.Runtime.Focus
{
    /// <summary>
    /// Extends Focus system with learning limits and cognitive fatigue integration.
    /// Learning drains FocusState.Current.
    /// Spell observation drains focus.
    /// Prediction/planning drains focus.
    /// Resting restores focus.
    /// Learning rate multiplier: effectiveRate = baseRate * (FocusCurrent / FocusMax)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FocusUpdateSystem))]
    public partial struct FocusLearningSystem : ISystem
    {
        private const float LearningFocusCost = 0.1f; // Focus cost per learning update
        private const float ObservationFocusCost = 0.05f; // Focus cost for spell observation
        private const float PlanningFocusCost = 0.15f; // Focus cost for prediction/planning

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            // Process entities with FocusState and learning components
            foreach (var (focusState, skillLearning, memoryProfile) in SystemAPI.Query<RefRW<FocusState>, RefRO<SkillLearningState>, RefRO<MemoryProfile>>())
            {
                var focus = focusState.ValueRW;

                // Calculate learning rate multiplier based on focus
                // effectiveRate = baseRate * (FocusCurrent / FocusMax)
                var focusRatio = focus.Max > 0f ? focus.Current / focus.Max : 0f;
                var learningRateMultiplier = math.clamp(focusRatio, 0.1f, 1f);

                // Apply focus drain for active learning
                if (skillLearning.ValueRO.LastUpdateTick == timeState.Tick)
                {
                    // Learning occurred this tick, drain focus
                    focus.Current = math.max(0f, focus.Current - LearningFocusCost);
                }

                // Update focus state
                focusState.ValueRW = focus;
            }

            // Process entities performing spell observation
            // (Would check for spell observation activity and drain focus)
            // This would be integrated with spell systems

            // Process entities performing prediction/planning
            // (Would check for planning activity and drain focus)
            // This would be integrated with AI planning systems
        }

        /// <summary>
        /// Calculate effective learning rate based on focus level.
        /// </summary>
        [BurstCompile]
        public static float CalculateEffectiveLearningRate(float baseLearningRate, float focusCurrent, float focusMax)
        {
            if (focusMax <= 0f)
            {
                return baseLearningRate * 0.1f; // Minimum 10% if no focus
            }

            var focusRatio = focusCurrent / focusMax;
            var multiplier = math.clamp(focusRatio, 0.1f, 1f);
            return baseLearningRate * multiplier;
        }

        /// <summary>
        /// Drain focus for learning activity.
        /// </summary>
        [BurstCompile]
        public static void DrainFocusForLearning(ref FocusState focusState, float cost = LearningFocusCost)
        {
            focusState.Current = math.max(0f, focusState.Current - cost);
        }

        /// <summary>
        /// Drain focus for observation activity.
        /// </summary>
        [BurstCompile]
        public static void DrainFocusForObservation(ref FocusState focusState, float cost = ObservationFocusCost)
        {
            focusState.Current = math.max(0f, focusState.Current - cost);
        }

        /// <summary>
        /// Drain focus for planning activity.
        /// </summary>
        [BurstCompile]
        public static void DrainFocusForPlanning(ref FocusState focusState, float cost = PlanningFocusCost)
        {
            focusState.Current = math.max(0f, focusState.Current - cost);
        }
    }
}

