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
    /// Integrates linear velocity for 6-DoF objects.
    /// Part of hierarchical decoupling - runs separately from angular integration.
    /// Uses symplectic Euler for stability over long timesteps.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LinearVelocityIntegrationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SixDoFState>();
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
            float deltaTime = tickTimeState.FixedDeltaTime;

            var job = new LinearVelocityIntegrationJob
            {
                DeltaTime = deltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct LinearVelocityIntegrationJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(ref SixDoFState sixDoF)
            {
                // Symplectic Euler: Position += LinearVelocity * dt
                sixDoF.Position += sixDoF.LinearVelocity * DeltaTime;
            }
        }
    }
}

