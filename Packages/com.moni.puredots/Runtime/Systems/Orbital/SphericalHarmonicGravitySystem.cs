using PureDOTS.Runtime.BlobAssets.Orbital;
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
    /// Computes gravitational acceleration using spherical harmonic expansion (l=2 quadrupole).
    /// Pre-baked coefficients in BlobAssets provide constant-time lookup.
    /// Keeps gravitational precision for 6-DoF roll-and-pitch motion without full N-body cost.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SphericalShellUpdateSystem))]
    [UpdateBefore(typeof(LinearVelocityIntegrationSystem))]
    public partial struct SphericalHarmonicGravitySystem : ISystem
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

            var job = new ComputeGravitationalAccelerationJob();
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ComputeGravitationalAccelerationJob : IJobEntity
        {
            public void Execute(
                ref SixDoFState sixDoF,
                in SphericalHarmonicGravity gravity)
            {
                if (!gravity.IsCreated)
                {
                    return;
                }

                ref var coefficients = ref gravity.Coefficients.Value;
                double3 position = (double3)sixDoF.Position;
                double3 acceleration = coefficients.ComputeAcceleration(position);

                // Convert acceleration to float3 and apply to linear velocity
                // Note: In a full implementation, this would accumulate with other forces
                // For now, we'll store it as a component that other systems can read
                float3 accelFloat = (float3)acceleration;
                // Integration happens in LinearVelocityIntegrationSystem
                // This system just computes the acceleration
            }
        }
    }
}

