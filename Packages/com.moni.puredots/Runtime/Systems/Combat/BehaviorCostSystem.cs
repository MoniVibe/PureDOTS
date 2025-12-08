using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Focus;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
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

                BehaviorModifier modifier = new BehaviorModifier
                {
                    FocusCostMultiplier = 1f,
                    StaminaCostMultiplier = 1f,
                    LearningRateMultiplier = 1f
                };

                float totalFocusCost = 0f;
                float totalStaminaCost = 0f;

                for (int i = 0; i < actions.Length; i++)
                {
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

                totalFocusCost *= modifier.FocusCostMultiplier;
                totalStaminaCost *= modifier.StaminaCostMultiplier;

                bool canAfford = focus.Current >= totalFocusCost && stamina.Current >= totalStaminaCost;

                if (!canAfford)
                {
                    if (tierState.Tier > BehaviorTier.Baseline)
                    {
                        tierState.Tier = BehaviorTier.Baseline;
                    }
                    return;
                }

                focus.Current = math.max(0f, focus.Current - totalFocusCost);
                stamina.Current = math.max(0f, stamina.Current - totalStaminaCost);

                if (focus.Current < focus.SoftThreshold && tierState.Tier > BehaviorTier.Baseline)
                {
                    tierState.Tier = BehaviorTier.Baseline;
                }
            }
        }
    }
}
