using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
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
                bool shouldUpdate = tierState.Tier == BehaviorTier.Baseline || (CurrentTick % 2 == 0);
                if (!shouldUpdate)
                    return;

                for (int i = compositions.Length - 1; i >= 0; i--)
                {
                    var comp = compositions[i];
                    if (comp.StartTime + comp.Duration < CurrentTick * DeltaTime)
                    {
                        compositions.RemoveAt(i);
                    }
                }

                const int maxCompositions = 8;
                if (compositions.Length >= maxCompositions)
                    return;

                if (tierState.Tier >= BehaviorTier.Learned && compositions.Length == 0)
                {
                    var dashAction = new ActionComposition
                    {
                        Action = AtomicAction.Dash,
                        StartTime = CurrentTick * DeltaTime,
                        Duration = 0.2f,
                        Direction = new float3(1f, 0f, 0f)
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
