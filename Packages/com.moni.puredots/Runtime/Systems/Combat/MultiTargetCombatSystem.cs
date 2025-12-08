using PureDOTS.Runtime.Focus;
using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Divides focus cost by packet size, processes all targets in burst job.
    /// Efficient multi-target resolution (e.g., 4 imprisoned + 1 mind-controlled).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(TargetPacketSystem))]
    public partial struct MultiTargetCombatSystem : ISystem
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

            var job = new ProcessMultiTargetCombatJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessMultiTargetCombatJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                ref FocusState focus,
                in MultiTargetBehaviorTag tag,
                DynamicBuffer<HitBuffer> targets,
                DynamicBuffer<ActionComposition> actions)
            {
                if (targets.Length == 0)
                    return;

                float baseFocusCost = 20f;
                float focusCostPerTarget = baseFocusCost / math.max(targets.Length, 1f);

                if (focus.Current < focusCostPerTarget)
                    return;

                focus.Current = math.max(0f, focus.Current - focusCostPerTarget);

                for (int i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                }

                targets.Clear();
            }
        }
    }
}
