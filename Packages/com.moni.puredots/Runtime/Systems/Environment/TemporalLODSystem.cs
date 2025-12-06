using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// System that initializes temporal LOD configuration singleton.
    /// Other systems check this config to determine update cadence.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateBefore(typeof(MoistureEvaporationSystem))]
    public partial struct TemporalLODSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ensure temporal LOD config exists with defaults
            if (!SystemAPI.HasSingleton<TemporalLODConfig>())
            {
                var config = new TemporalLODConfig
                {
                    WindCloudDivisor = 1,
                    TemperatureDivisor = 5,
                    VegetationDivisor = 20,
                    FireDivisor = 1,
                    ClimateFeedbackDivisor = 5
                };
                SystemAPI.SetSingleton(config);
            }
        }
    }

    /// <summary>
    /// Helper methods for checking temporal LOD update cadence.
    /// </summary>
    public static class TemporalLODHelpers
    {
        /// <summary>
        /// Checks if a system should update based on tick divisor.
        /// </summary>
        [BurstCompile]
        public static bool ShouldUpdate(uint currentTick, uint divisor)
        {
            if (divisor == 0) return false;
            return currentTick % divisor == 0;
        }

        /// <summary>
        /// Gets the effective tick delta accounting for temporal LOD.
        /// </summary>
        [BurstCompile]
        public static uint GetEffectiveTickDelta(uint currentTick, uint lastUpdateTick, uint divisor)
        {
            if (divisor == 0) return 0;
            if (lastUpdateTick == uint.MaxValue) return 1;
            
            var tickDelta = currentTick - lastUpdateTick;
            // Round down to nearest divisor multiple
            return (tickDelta / divisor) * divisor;
        }
    }
}

