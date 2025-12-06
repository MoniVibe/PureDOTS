using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Focus;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Consumes focus/stamina on behavior activation and downgrades tier if insufficient.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(BehaviorGatingSystem))]
    public partial struct BehaviorCostSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            if (timeState.IsPaused)
                return;

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var job = new ApplyBehaviorCostJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ApplyBehaviorCostJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                ref BehaviorTierState tierState,
                ref FocusState focus,
                ref StaminaState stamina,
                DynamicBuffer<ActionComposition> actions)
            {
                if (actions.Length == 0)
                    return;

                // Get modifier if present, otherwise use defaults
                BehaviorModifier modifier = new BehaviorModifier
                {
                    FocusCostMultiplier = 1f,
                    StaminaCostMultiplier = 1f,
                    LearningRateMultiplier = 1f
                };

                // Calculate total costs from active actions
                // In a full implementation, this would look up behavior costs from catalog
                float totalFocusCost = 0f;
                float totalStaminaCost = 0f;

                // Simplified: assume each action has base costs
                // Real implementation would query BehaviorCatalog blob
                for (int i = 0; i < actions.Length; i++)
                {
                    // Base costs per action type (simplified)
                    switch (actions[i].Action)
                    {
                        case AtomicAction.Dash:
                            totalStaminaCost += 10f;
                            break;
                        case AtomicAction.Swing:
                            totalStaminaCost += 5f;
                            totalFocusCost += 2f;
                            break;
                        case AtomicAction.Parry:
                            totalStaminaCost += 3f;
                            totalFocusCost += 5f;
                            break;
                        case AtomicAction.Cast:
                            totalFocusCost += 15f;
                            break;
                    }
                }

                // Apply modifiers (would read from BehaviorModifier component if present)
                totalFocusCost *= modifier.FocusCostMultiplier;
                totalStaminaCost *= modifier.StaminaCostMultiplier;

                // Check if sufficient resources
                bool canAfford = focus.Current >= totalFocusCost && stamina.Current >= totalStaminaCost;

                if (!canAfford)
                {
                    // Downgrade to baseline tier if insufficient resources
                    if (tierState.Tier > BehaviorTier.Baseline)
                    {
                        tierState.Tier = BehaviorTier.Baseline;
                    }
                    return;
                }

                // Consume resources
                focus.Current = math.max(0f, focus.Current - totalFocusCost);
                stamina.Current = math.max(0f, stamina.Current - totalStaminaCost);

                // Check focus threshold for tier downgrade
                if (focus.Current < focus.SoftThreshold && tierState.Tier > BehaviorTier.Baseline)
                {
                    tierState.Tier = BehaviorTier.Baseline;
                }
            }
        }
    }
}
