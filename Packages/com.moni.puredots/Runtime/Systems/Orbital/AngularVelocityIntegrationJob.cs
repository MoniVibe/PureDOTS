using System.Runtime.InteropServices;
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
    /// Integrates angular velocity for 6-DoF objects using Rodrigues' formula.
    /// Part of hierarchical decoupling - runs separately from linear integration.
    /// Prevents gimbal drift, works perfectly with Burst SIMD.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LinearVelocityIntegrationSystem))]
    public partial struct AngularVelocityIntegrationSystem : ISystem
    {
        private const int RenormalizeInterval = 100; // Re-normalize every N ticks
        private uint _tickCounter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SixDoFState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
            _tickCounter = 0;
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
            bool shouldRenormalize = (_tickCounter % RenormalizeInterval == 0);
            _tickCounter++;

            var job = new AngularVelocityIntegrationJob
            {
                DeltaTime = deltaTime,
                ShouldRenormalize = shouldRenormalize
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct AngularVelocityIntegrationJob : IJobEntity
        {
            public float DeltaTime;
            [MarshalAs(UnmanagedType.U1)]
            public bool ShouldRenormalize;

            public void Execute(ref SixDoFState sixDoF)
            {
                // Rodrigues' formula: dq = quaternion.AxisAngle(normalize(ω), length(ω) * dt)
                float3 angularVel = sixDoF.AngularVelocity;
                float angle = math.length(angularVel) * DeltaTime;

                if (angle > 1e-6f)
                {
                    float3 axis = math.normalize(angularVel);
                    quaternion dq = quaternion.AxisAngle(axis, angle);
                    sixDoF.Orientation = math.mul(dq, sixDoF.Orientation);
                }

                // Re-normalize periodically to prevent drift
                if (ShouldRenormalize)
                {
                    sixDoF.Orientation = math.normalize(sixDoF.Orientation);
                }
            }
        }
    }
}

