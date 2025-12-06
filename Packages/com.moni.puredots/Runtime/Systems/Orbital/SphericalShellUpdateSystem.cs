using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Components.Orbital;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Orbital
{
    /// <summary>
    /// Updates shell membership for entities based on distance from galactic center.
    /// Core shell: 1 Hz update, high resolution
    /// Inner shell: 0.1 Hz, medium resolution
    /// Outer shell: 0.01 Hz, low resolution
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(LinearVelocityIntegrationSystem))]
    public partial struct SphericalShellUpdateSystem : ISystem
    {
        private const float CoreRadius = 1000f;      // Core shell inner radius (meters)
        private const float InnerRadius = 10000f;   // Inner shell inner radius (meters)
        private const float OuterRadius = 100000f;   // Outer shell inner radius (meters)

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShellMembership>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            uint currentTick = tickTimeState.Tick;
            float deltaTime = tickTimeState.FixedDeltaTime;

            var job = new UpdateShellMembershipJob
            {
                CurrentTick = currentTick,
                CoreRadius = CoreRadius,
                InnerRadius = InnerRadius,
                OuterRadius = OuterRadius
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct UpdateShellMembershipJob : IJobEntity
        {
            public uint CurrentTick;
            public float CoreRadius;
            public float InnerRadius;
            public float OuterRadius;

            public void Execute(ref ShellMembership shell, in SixDoFState sixDoF)
            {
                // Compute distance from origin (galactic center)
                double distance = math.length((double3)sixDoF.Position);

                // Determine shell based on distance
                int newShellIndex;
                double innerRadius;
                double outerRadius;
                float updateFrequency;

                if (distance < CoreRadius)
                {
                    newShellIndex = (int)ShellType.Core;
                    innerRadius = 0.0;
                    outerRadius = CoreRadius;
                    updateFrequency = 1.0f; // 1 Hz
                }
                else if (distance < InnerRadius)
                {
                    newShellIndex = (int)ShellType.Inner;
                    innerRadius = CoreRadius;
                    outerRadius = InnerRadius;
                    updateFrequency = 0.1f; // 0.1 Hz
                }
                else if (distance < OuterRadius)
                {
                    newShellIndex = (int)ShellType.Outer;
                    innerRadius = InnerRadius;
                    outerRadius = OuterRadius;
                    updateFrequency = 0.01f; // 0.01 Hz
                }
                else
                {
                    // Beyond outer shell - still assign to outer
                    newShellIndex = (int)ShellType.Outer;
                    innerRadius = OuterRadius;
                    outerRadius = double.MaxValue;
                    updateFrequency = 0.01f;
                }

                // Check if update is needed based on frequency
                uint ticksSinceUpdate = CurrentTick - shell.LastUpdateTick;
                float ticksPerUpdate = 60.0f / updateFrequency; // Assuming 60 Hz base tick rate

                if (shell.ShellIndex != newShellIndex || ticksSinceUpdate >= (uint)ticksPerUpdate)
                {
                    shell.ShellIndex = newShellIndex;
                    shell.InnerRadius = innerRadius;
                    shell.OuterRadius = outerRadius;
                    shell.UpdateFrequency = updateFrequency;
                    shell.LastUpdateTick = CurrentTick;
                }
            }
        }
    }
}

