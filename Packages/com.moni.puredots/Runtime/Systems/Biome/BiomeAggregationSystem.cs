using PureDOTS.Biome;
using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Biome
{
    /// <summary>
    /// Lightweight Biome ECS system.
    /// Updates biome entities asynchronously to main simulation.
    /// Provides summarized telemetry to other systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(ClimateFeedbackSystem))]
    public partial struct BiomeAggregationSystem : ISystem
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

            // Query biome entities
            var biomeQuery = SystemAPI.QueryBuilder()
                .WithAll<BiomeEntity, BiomeTelemetry>()
                .Build();

            if (biomeQuery.IsEmpty)
            {
                return;
            }

            // Get environment grids for sampling
            var moistureGrid = SystemAPI.HasSingleton<MoistureGrid>() ? SystemAPI.GetSingleton<MoistureGrid>() : default;
            var temperatureGrid = SystemAPI.HasSingleton<TemperatureGrid>() ? SystemAPI.GetSingleton<TemperatureGrid>() : default;

            var job = new UpdateBiomeTelemetryJob
            {
                MoistureGrid = moistureGrid,
                TemperatureGrid = temperatureGrid,
                CurrentTick = currentTick
            };

            state.Dependency = job.ScheduleParallel(biomeQuery, state.Dependency);
        }

        [BurstCompile]
        public partial struct UpdateBiomeTelemetryJob : IJobEntity
        {
            public MoistureGrid MoistureGrid;
            public TemperatureGrid TemperatureGrid;
            public uint CurrentTick;

            public void Execute(ref BiomeTelemetry telemetry, in BiomeEntity biome, in DynamicBuffer<BiomeChunkBuffer> chunks)
            {
                // Aggregate telemetry from chunks
                var totalTemp = 0f;
                var totalMoisture = 0f;
                var sampleCount = 0;

                for (int i = 0; i < chunks.Length; i++)
                {
                    var chunk = chunks[i];
                    if (!chunk.ChunkBlob.IsCreated)
                    {
                        continue;
                    }

                    ref var chunkData = ref chunk.ChunkBlob.Value;
                    var tempArray = chunkData.Temperature;
                    var moistureArray = chunkData.Moisture;

                    // Sample from chunk (simplified - would sample all cells in full implementation)
                    if (tempArray.Length > 0 && moistureArray.Length > 0)
                    {
                        var sampleIndex = sampleCount % tempArray.Length;
                        totalTemp += tempArray[sampleIndex];
                        totalMoisture += moistureArray[sampleIndex];
                        sampleCount++;
                    }
                }

                if (sampleCount > 0)
                {
                    telemetry.AverageTemperature = totalTemp / sampleCount;
                    telemetry.AverageMoisture = totalMoisture / sampleCount;
                }

                // Flora sample count would be updated from vegetation queries
                telemetry.FloraSampleCount = sampleCount;

                // Weather intensity placeholder
                telemetry.WeatherIntensity = 0.5f;

                telemetry.LastUpdateTick = CurrentTick;
            }
        }
    }
}

