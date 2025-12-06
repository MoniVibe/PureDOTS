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
    /// Mean-field drift system for rogue objects.
    /// Instead of full integration for billions of rogue bodies, simulates a single
    /// mean-field vector field representing galactic potential: Φ(r) = GM_total / (r + ε)
    /// Entities sample acceleration from this field, avoiding per-body mutual forces.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SphericalHarmonicGravitySystem))]
    [UpdateBefore(typeof(LinearVelocityIntegrationSystem))]
    public partial struct MeanFieldDriftSystem : ISystem
    {
        private const double Epsilon = 1e-6; // Small value to prevent division by zero

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

            // Get total galactic mass (would be stored in a singleton in full implementation)
            // For now, use a constant
            const double TotalGalacticMass = 1e42; // kg (example value)
            const double G = 6.67430e-11; // Gravitational constant
            double GM_total = G * TotalGalacticMass;

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            float deltaTime = tickTimeState.FixedDeltaTime;

            var job = new ComputeMeanFieldAccelerationJob
            {
                GM_total = GM_total,
                Epsilon = Epsilon,
                DeltaTime = deltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ComputeMeanFieldAccelerationJob : IJobEntity
        {
            public double GM_total;
            public double Epsilon;
            public float DeltaTime;

            public void Execute(ref SixDoFState sixDoF)
            {
                // Mean-field potential: Φ(r) = GM_total / (r + ε)
                // Acceleration: a = -∇Φ = GM_total * rHat / (r + ε)²
                double3 position = (double3)sixDoF.Position;
                double r = math.length(position);

                if (r < 1e-6)
                {
                    return; // At origin, no acceleration
                }

                double3 rHat = position / r;
                double rPlusEpsilon = r + Epsilon;
                double rPlusEpsilonSquared = rPlusEpsilon * rPlusEpsilon;

                // Acceleration = GM_total * rHat / (r + ε)²
                double3 acceleration = (GM_total / rPlusEpsilonSquared) * rHat;

                // Apply to linear velocity (accumulate with other forces)
                // In full implementation, this would be accumulated with spherical harmonic gravity
                float3 accelFloat = (float3)acceleration;
                sixDoF.LinearVelocity += accelFloat * DeltaTime;
            }
        }
    }
}

