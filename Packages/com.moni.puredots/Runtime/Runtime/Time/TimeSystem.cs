using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Baseline time driver that advances RewindState tick according to mode.
    /// External systems set Mode/TargetTick/PendingStepTicks; this system mutates CurrentTick/TargetTick.
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct TimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!Application.isPlaying)
                return;

            if (!SystemAPI.TryGetSingletonRW<RewindState>(out var rewind))
                return;

            ref var rs = ref rewind.ValueRW;

            switch (rs.Mode)
            {
                case RewindMode.Play:
                    rs.CurrentTick++;
                    rs.TargetTick = rs.CurrentTick;
                    break;

                case RewindMode.Paused:
                    // no tick advance
                    break;

                case RewindMode.Step:
                    if (rs.PendingStepTicks > 0)
                    {
                        rs.CurrentTick++;
                        rs.TargetTick = rs.CurrentTick;
                        rs.PendingStepTicks--;
                    }
                    else
                    {
                        rs.Mode = RewindMode.Paused;
                    }
                    break;

                case RewindMode.Rewind:
                    if (rs.CurrentTick > 0)
                    {
                        rs.CurrentTick--;
                    }
                    else
                    {
                        rs.Mode = RewindMode.Paused;
                    }
                    break;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}



