using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Parallel environment sampling system.
    /// Updates EnvironmentSample component on entities via chunk lookup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(ClimateFeedbackSystem))]
    public partial struct EnvironmentSamplingSystem : ISystem
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

            var currentTick = timeState.Tick;

            // Query entities that need environment sampling
            var entityQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform>()
                .WithAny<EnvironmentSample>()
                .Build();

            if (entityQuery.IsEmpty)
            {
                return;
            }

            // Get environment grids
            var moistureGrid = SystemAPI.HasSingleton<MoistureGrid>() ? SystemAPI.GetSingleton<MoistureGrid>() : default;
            var temperatureGrid = SystemAPI.HasSingleton<TemperatureGrid>() ? SystemAPI.GetSingleton<TemperatureGrid>() : default;
            var sunlightGrid = SystemAPI.HasSingleton<SunlightGrid>() ? SystemAPI.GetSingleton<SunlightGrid>() : default;

            var sampler = new EnvironmentSampler(ref state);

            var job = new SampleEnvironmentJob
            {
                MoistureGrid = moistureGrid,
                TemperatureGrid = temperatureGrid,
                SunlightGrid = sunlightGrid,
                CurrentTick = currentTick
            };

            state.Dependency = job.ScheduleParallel(entityQuery, state.Dependency);
        }

        [BurstCompile]
        public partial struct SampleEnvironmentJob : IJobEntity
        {
            public MoistureGrid MoistureGrid;
            public TemperatureGrid TemperatureGrid;
            public SunlightGrid SunlightGrid;
            public uint CurrentTick;

            public void Execute(ref EnvironmentSample sample, in LocalTransform transform)
            {
                var worldPos = transform.Position;

                // Sample temperature
                if (TemperatureGrid.IsCreated)
                {
                    sample.Temperature = (half)TemperatureGrid.SampleBilinear(worldPos, 20f);
                }
                else
                {
                    sample.Temperature = (half)20f;
                }

                // Sample moisture
                if (MoistureGrid.IsCreated)
                {
                    sample.Moisture = (half)MoistureGrid.SampleBilinear(worldPos, 50f);
                }
                else
                {
                    sample.Moisture = (half)50f;
                }

                // Sample light
                if (SunlightGrid.IsCreated)
                {
                    var sunlight = SunlightGrid.SampleBilinear(worldPos, default);
                    sample.Light = (half)(sunlight.DirectLight + sunlight.AmbientLight);
                }
                else
                {
                    sample.Light = (half)100f;
                }

                // Oxygen and soil fertility would be sampled from additional grids
                // For now, use defaults
                sample.Oxygen = (half)21f; // Atmospheric oxygen percentage
                sample.SoilFertility = (half)50f; // Default fertility

                sample.LastSampleTick = CurrentTick;
            }
        }
    }
}

