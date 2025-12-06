using PureDOTS.Environment;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Reads environmental telemetry and triggers agent desires based on comfort thresholds.
    /// Links to Mind ECS via AgentSyncBus for goal generation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    public partial struct AgentEnvironmentalDesireSystem : ISystem
    {
        private TimeAwareController _timeAware;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _timeAware = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_timeAware.TryBegin(timeState, rewindState, out _))
            {
                return;
            }

            // Update environmental telemetry for agents
            var telemetryQuery = SystemAPI.QueryBuilder()
                .WithAll<EnvironmentalTelemetry>()
                .Build();

            if (telemetryQuery.IsEmpty)
            {
                return;
            }

            // Get biome grid for sampling
            var hasBiomeGrid = SystemAPI.TryGetSingleton<BiomeGrid>(out var biomeGrid);
            var hasMoistureGrid = SystemAPI.TryGetSingleton<MoistureGrid>(out var moistureGrid);
            var hasTemperatureGrid = SystemAPI.TryGetSingleton<TemperatureGrid>(out var temperatureGrid);

            var job = new UpdateEnvironmentalTelemetryJob
            {
                CurrentTick = timeState.Tick,
                BiomeGrid = biomeGrid,
                HasBiomeGrid = hasBiomeGrid,
                MoistureGrid = moistureGrid,
                HasMoistureGrid = hasMoistureGrid,
                TemperatureGrid = temperatureGrid,
                HasTemperatureGrid = hasTemperatureGrid
            };

            state.Dependency = job.ScheduleParallel(telemetryQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct UpdateEnvironmentalTelemetryJob : IJobEntity
        {
            public uint CurrentTick;
            public BiomeGrid BiomeGrid;
            public bool HasBiomeGrid;
            public MoistureGrid MoistureGrid;
            public bool HasMoistureGrid;
            public TemperatureGrid TemperatureGrid;
            public bool HasTemperatureGrid;

            public void Execute(ref EnvironmentalTelemetry telemetry, in Unity.Transforms.LocalTransform transform)
            {
                var position = transform.Position;

                // Sample environment at agent position
                if (HasBiomeGrid)
                {
                    telemetry.CurrentBiome = BiomeGrid.SampleNearest(position);
                }

                if (HasTemperatureGrid)
                {
                    telemetry.Temperature = TemperatureGrid.SampleBilinear(position);
                }

                if (HasMoistureGrid)
                {
                    telemetry.Moisture = MoistureGrid.SampleBilinear(position);
                }

                // Compute comfort score (0-1, higher is better)
                // Comfort based on temperature (prefer 15-25°C) and moisture (prefer 40-60%)
                var tempComfort = 1f - math.abs(telemetry.Temperature - 20f) / 30f; // Optimal at 20°C
                var moistComfort = 1f - math.abs(telemetry.Moisture - 50f) / 50f; // Optimal at 50%
                telemetry.Comfort = (tempComfort + moistComfort) * 0.5f;
                telemetry.Comfort = math.clamp(telemetry.Comfort, 0f, 1f);

                telemetry.LastUpdateTick = CurrentTick;
            }
        }
    }
}

