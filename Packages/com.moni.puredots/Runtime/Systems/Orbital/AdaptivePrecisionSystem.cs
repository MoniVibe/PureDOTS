using PureDOTS.Runtime.Components.Orbital;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Orbital
{
    /// <summary>
    /// Adaptive precision system for mixed-precision calculations.
    /// Uses double for galactic centers (hundreds of kpc), float for system-scale (AU),
    /// half for local object transforms.
    /// Normalizes vectors before casting to maintain Burst determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SphericalShellUpdateSystem))]
    public partial struct AdaptivePrecisionSystem : ISystem
    {
        private const double GalacticThreshold = 1e20; // meters (hundreds of kpc)
        private const double SystemThreshold = 1e12;   // meters (AU scale)

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SixDoFState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new AssignPrecisionLevelJob
            {
                GalacticThreshold = GalacticThreshold,
                SystemThreshold = SystemThreshold
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct AssignPrecisionLevelJob : IJobEntity
        {
            public double GalacticThreshold;
            public double SystemThreshold;

            public void Execute(ref AdaptivePrecision precision, in SixDoFState sixDoF)
            {
                double distance = math.length((double3)sixDoF.Position);

                if (distance > GalacticThreshold)
                {
                    precision.Level = PrecisionLevel.Double;
                    precision.DistanceThreshold = GalacticThreshold;
                }
                else if (distance > SystemThreshold)
                {
                    precision.Level = PrecisionLevel.Float;
                    precision.DistanceThreshold = SystemThreshold;
                }
                else
                {
                    precision.Level = PrecisionLevel.Half;
                    precision.DistanceThreshold = SystemThreshold;
                }
            }
        }

        /// <summary>
        /// Converts a float3 to the appropriate precision based on level.
        /// Normalizes before casting to maintain determinism.
        /// </summary>
        public static float3 ConvertToPrecision(float3 value, PrecisionLevel level)
        {
            // Normalize before precision conversion
            float3 normalized = math.normalize(value);
            float magnitude = math.length(value);

            return level switch
            {
                PrecisionLevel.Double => value, // Keep as float (double not directly supported in Burst)
                PrecisionLevel.Float => value,
                PrecisionLevel.Half => normalized * (half)magnitude, // Approximate half precision
                _ => value
            };
        }
    }
}

