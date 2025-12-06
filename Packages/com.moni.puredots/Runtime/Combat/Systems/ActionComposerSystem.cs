using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Blends atomic actions into sequences, evaluates up to N composites per entity per tick.
    /// Procedural action composition system.
    /// Performance: Pooled buffers, tiered tick rates for advanced behaviors.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(BehaviorCostSystem))]
    public partial struct ActionComposerSystem : ISystem
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

            // Baseline behaviors run every tick (60Hz), advanced every 2 ticks (30Hz)
            // This is handled per-entity in the job based on tier

            var job = new ComposeActionsJob
            {
                CurrentTick = timeState.Tick,
                DeltaTime = timeState.FixedDeltaTime
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ComposeActionsJob : IJobEntity
        {
            public uint CurrentTick;
            public float DeltaTime;

            void Execute(
                in BehaviorTierState tierState,
                DynamicBuffer<ActionComposition> compositions)
            {
                // Tiered tick rate: advanced behaviors update less frequently
                // Baseline: every tick, Learned/Mastered: every 2 ticks
                bool shouldUpdate = tierState.Tier == BehaviorTier.Baseline || (CurrentTick % 2 == 0);
                if (!shouldUpdate)
                    return;

                // Clear expired compositions (pool reuse)
                for (int i = compositions.Length - 1; i >= 0; i--)
                {
                    var comp = compositions[i];
                    if (comp.StartTime + comp.Duration < CurrentTick * DeltaTime)
                    {
                        compositions.RemoveAt(i);
                    }
                }

                // Compose new actions based on behavior tier
                // Simplified: baseline behaviors compose simple actions
                // In full implementation, would query BehaviorCatalog blob for action sequences

                const int maxCompositions = 8;
                if (compositions.Length >= maxCompositions)
                    return;

                // Example: if tier allows, compose a multi-action sequence
                if (tierState.Tier >= BehaviorTier.Learned && compositions.Length == 0)
                {
                    // Compose a strafe-shoot sequence
                    var dashAction = new ActionComposition
                    {
                        Action = AtomicAction.Dash,
                        StartTime = CurrentTick * DeltaTime,
                        Duration = 0.2f,
                        Direction = new float3(1f, 0f, 0f) // Simplified
                    };
                    compositions.Add(dashAction);

                    var fireAction = new ActionComposition
                    {
                        Action = AtomicAction.Fire,
                        StartTime = CurrentTick * DeltaTime + 0.1f,
                        Duration = 0.15f,
                        Direction = new float3(1f, 0f, 0f)
                    };
                    compositions.Add(fireAction);
                }
            }
        }
    }
}

