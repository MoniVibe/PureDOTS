using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Population pressure loop system.
    /// Overpopulation → resource decline → migration intent.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(ClimateFeedbackSystem))]
    public partial struct PopulationPressureSystem : ISystem
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

            // Query villagers/agents for population density
            // Calculate resource availability per capita
            // Generate migration intents when overpopulation detected
            // This would feed into AI goal evaluation
            // For now, this is a placeholder structure
        }
    }
}

