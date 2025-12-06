using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using PureDOTS.Systems.Environment;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Unified atmospheric feedback loop system.
    /// Converges all fields atomically: temperature, moisture, oxygen.
    /// Updates once per climate tick, chunk-parallel.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(FieldPropagationSystem))]
    public partial struct ClimateFeedbackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ClimateState>();
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

            var climate = SystemAPI.GetSingleton<ClimateState>();
            var gridConfig = SystemAPI.GetSingleton<EnvironmentGridConfigData>();

            ClimateProfileData profile;
            if (!SystemAPI.TryGetSingleton(out profile))
            {
                profile = ClimateProfileDefaults.Create(in gridConfig);
            }

            // Process feedback for moisture grid
            if (SystemAPI.TryGetSingleton<MoistureGrid>(out var moistureGrid))
            {
                ProcessMoistureFeedback(ref state, moistureGrid, climate, profile, timeState);
            }

            // Process feedback for temperature grid
            if (SystemAPI.TryGetSingleton<TemperatureGrid>(out var temperatureGrid))
            {
                ProcessTemperatureFeedback(ref state, temperatureGrid, climate, profile, timeState);
            }
        }

        private void ProcessMoistureFeedback(ref SystemState state, MoistureGrid grid, ClimateState climate, ClimateProfileData profile, TimeState timeState)
        {
            var gridEntity = SystemAPI.GetSingletonEntity<MoistureGrid>();
            if (!SystemAPI.HasBuffer<MoistureGridRuntimeCell>(gridEntity))
            {
                return;
            }

            var runtimeBuffer = SystemAPI.GetBuffer<MoistureGridRuntimeCell>(gridEntity);
            if (runtimeBuffer.Length == 0)
            {
                return;
            }

            var deltaSeconds = timeState.FixedDeltaTime;

            // Get sunlight for evaporation calculation
            NativeArray<SunlightSample> sunlightSamples = default;
            if (SystemAPI.TryGetSingletonEntity<SunlightGrid>(out var sunlightEntity) && SystemAPI.HasBuffer<SunlightGridRuntimeSample>(sunlightEntity))
            {
                var sunlightBuffer = SystemAPI.GetBuffer<SunlightGridRuntimeSample>(sunlightEntity);
                if (sunlightBuffer.Length == runtimeBuffer.Length)
                {
                    sunlightSamples = sunlightBuffer.Reinterpret<SunlightSample>().AsNativeArray();
                }
            }

            var job = new ClimateFeedbackJob
            {
                MoistureCells = runtimeBuffer.AsNativeArray(),
                Sunlight = sunlightSamples,
                GlobalTemperature = climate.GlobalTemperature,
                AtmosphericMoisture = climate.AtmosphericMoisture,
                EvaporationBaseRate = profile.EvaporationBaseRate,
                DeltaSeconds = deltaSeconds
            };

            state.Dependency = job.ScheduleParallel(runtimeBuffer.Length, 64, state.Dependency);
        }

        private void ProcessTemperatureFeedback(ref SystemState state, TemperatureGrid grid, ClimateState climate, ClimateProfileData profile, TimeState timeState)
        {
            if (!grid.IsCreated)
            {
                return;
            }

            // Temperature feedback: sunIrradiance - evaporationCooling
            // This is handled by ClimateStateUpdateSystem, but we can add local adjustments here
            // For now, this is a placeholder for future expansion
        }

        [BurstCompile]
        private struct ClimateFeedbackJob : IJobFor
        {
            public NativeArray<MoistureGridRuntimeCell> MoistureCells;
            [ReadOnly] public NativeArray<SunlightSample> Sunlight;
            public float GlobalTemperature;
            public float AtmosphericMoisture;
            public float EvaporationBaseRate;
            public float DeltaSeconds;

            public void Execute(int index)
            {
                var cell = MoistureCells[index];

                // Evaporation: moisture += -evaporation (negative contribution)
                var tempFactor = math.exp((GlobalTemperature - 20f) * 0.05f);
                var humidityFactor = 1f - math.clamp(AtmosphericMoisture / 200f, 0f, 1f);
                
                var shadeFactor = 1f;
                if (Sunlight.IsCreated && index < Sunlight.Length)
                {
                    var sunlight = Sunlight[index];
                    var intensityFactor = math.saturate(sunlight.DirectLight / 100f);
                    shadeFactor = math.clamp(intensityFactor, 0.2f, 1f);
                }

                var evaporationPerSecond = EvaporationBaseRate * tempFactor * humidityFactor * shadeFactor;
                var evaporation = evaporationPerSecond * DeltaSeconds;

                // Update moisture: moisture += rainfall - evaporation
                // Rainfall is handled by MoistureRainSystem, so we only subtract evaporation here
                cell.Moisture = math.clamp(cell.Moisture - evaporation, 0f, 100f);
                cell.EvaporationRate = evaporationPerSecond;

                MoistureCells[index] = cell;
            }
        }
    }
}

