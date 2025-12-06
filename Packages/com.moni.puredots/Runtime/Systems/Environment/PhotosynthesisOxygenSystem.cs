using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Systems.Environment;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Photosynthesis-Oxygen loop system.
    /// Vegetation creates oxygen → supports fauna → fuels fire → reduces oxygen.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(ClimateFeedbackSystem))]
    public partial struct PhotosynthesisOxygenSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (timeState.IsPaused)
            {
                return;
            }

            // Check temporal LOD
            var lodConfig = SystemAPI.HasSingleton<TemporalLODConfig>()
                ? SystemAPI.GetSingleton<TemporalLODConfig>()
                : new TemporalLODConfig { ClimateFeedbackDivisor = 5 };

            if (!TemporalLODHelpers.ShouldUpdate(timeState.Tick, lodConfig.ClimateFeedbackDivisor))
            {
                return;
            }

            // Query mature vegetation for oxygen production
            var vegetationQuery = SystemAPI.QueryBuilder()
                .WithAll<VegetationLifecycle, VegetationHealth>()
                .WithAny<VegetationMatureTag>()
                .WithNone<VegetationDeadTag>()
                .Build();

            if (vegetationQuery.IsEmpty)
            {
                return;
            }

            // Calculate oxygen production from vegetation
            // oxygen += photosynthesis - combustion
            // This would update an oxygen grid or feed into climate feedback
            // For now, this is a placeholder structure
        }
    }
}

