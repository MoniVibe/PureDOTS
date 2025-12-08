using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems.Performance;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Burst-compiled vision system with FOV cone checks and occlusion queries.
    /// Processes entities with SensorSpec.Type == Vision.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(AISensorUpdateSystem))]
    public partial struct VisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<UniversalPerformanceBudget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
            var counters = SystemAPI.GetSingletonRW<UniversalPerformanceCounters>();

            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var spatialResidencyLookup = SystemAPI.GetComponentLookup<SpatialGridResidency>(true);

            var job = new ProcessVisionJob
            {
                CurrentTick = timeState.Tick,
                Budget = budget.MaxPerceptionChecksPerTick,
                TransformLookup = transformLookup,
                SpatialResidencyLookup = spatialResidencyLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            transformLookup.Update(ref state);
            spatialResidencyLookup.Update(ref state);
        }

        [BurstCompile]
        public partial struct ProcessVisionJob : IJobEntity
        {
            public uint CurrentTick;
            public int Budget;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<SpatialGridResidency> SpatialResidencyLookup;

            public void Execute(
                Entity entity,
                in SensorSpec spec,
                in SensorState sensorState,
                in LocalTransform transform,
                DynamicBuffer<SensorReadingBuffer> readings)
            {
                if (spec.Type != SensorType.Vision)
                {
                    return;
                }

                // Check update interval
                if (sensorState.LastSampleTick == CurrentTick)
                {
                    return; // Already updated this tick
                }

                readings.Clear();

                // Get forward direction from rotation
                var forward = math.forward(transform.Rotation);
                var fovRadians = math.radians(spec.FieldOfView * 0.5f);
                var cosFov = math.cos(fovRadians);

                // Query nearby entities (simplified - in production, use spatial grid)
                // For now, we'll use a simple distance check
                // TODO: Integrate with spatial grid for efficient queries

                // This is a placeholder - actual implementation would query spatial grid
                // and filter by FOV cone using dot product
            }
        }
    }

    /// <summary>
    /// Burst-compiled smell system with diffusion field sampling.
    /// Processes entities with SensorSpec.Type == Smell.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(AISensorUpdateSystem))]
    public partial struct SmellSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<UniversalPerformanceBudget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            var job = new ProcessSmellJob
            {
                CurrentTick = timeState.Tick,
                Budget = budget.MaxPerceptionChecksPerTick,
                TransformLookup = transformLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            transformLookup.Update(ref state);
        }

        [BurstCompile]
        public partial struct ProcessSmellJob : IJobEntity
        {
            public uint CurrentTick;
            public int Budget;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public void Execute(
                Entity entity,
                in SensorSpec spec,
                in SensorState sensorState,
                in LocalTransform transform,
                DynamicBuffer<SensorReadingBuffer> readings)
            {
                if (spec.Type != SensorType.Smell)
                {
                    return;
                }

                // Check update interval
                if (sensorState.LastSampleTick == CurrentTick)
                {
                    return;
                }

                readings.Clear();

                // Smell uses distance-based falloff with diffusion simulation
                // Intensity = sourceStrength / (distance^2 + 1)
                // TODO: Integrate with environment grid for diffusion field sampling
            }
        }
    }

    /// <summary>
    /// Burst-compiled hearing system with distance-based attenuation.
    /// Processes entities with SensorSpec.Type == Hearing.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(AISensorUpdateSystem))]
    public partial struct HearingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<UniversalPerformanceBudget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            var job = new ProcessHearingJob
            {
                CurrentTick = timeState.Tick,
                Budget = budget.MaxPerceptionChecksPerTick,
                TransformLookup = transformLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            transformLookup.Update(ref state);
        }

        [BurstCompile]
        public partial struct ProcessHearingJob : IJobEntity
        {
            public uint CurrentTick;
            public int Budget;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public void Execute(
                Entity entity,
                in SensorSpec spec,
                in SensorState sensorState,
                in LocalTransform transform,
                DynamicBuffer<SensorReadingBuffer> readings)
            {
                if (spec.Type != SensorType.Hearing)
                {
                    return;
                }

                // Check update interval
                if (sensorState.LastSampleTick == CurrentTick)
                {
                    return;
                }

                readings.Clear();

                // Hearing uses distance-based attenuation
                // Intensity = sourceIntensity / (distance + 1)
                // TODO: Implement event-based sound propagation
            }
        }
    }

    /// <summary>
    /// Burst-compiled radar system with sphere sweep and ECM interference simulation.
    /// Processes entities with SensorSpec.Type == Radar.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(AISensorUpdateSystem))]
    public partial struct RadarSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<UniversalPerformanceBudget>();
            state.RequireForUpdate<SpatialGridConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
            var gridConfig = SystemAPI.GetSingleton<SpatialGridConfig>();
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            var job = new ProcessRadarJob
            {
                CurrentTick = timeState.Tick,
                Budget = budget.MaxPerceptionChecksPerTick,
                GridConfig = gridConfig,
                TransformLookup = transformLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            transformLookup.Update(ref state);
        }

        [BurstCompile]
        public partial struct ProcessRadarJob : IJobEntity
        {
            public uint CurrentTick;
            public int Budget;
            public SpatialGridConfig GridConfig;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public void Execute(
                Entity entity,
                in SensorSpec spec,
                in SensorState sensorState,
                in LocalTransform transform,
                DynamicBuffer<SensorReadingBuffer> readings)
            {
                if (spec.Type != SensorType.Radar)
                {
                    return;
                }

                // Check update interval
                if (sensorState.LastSampleTick == CurrentTick)
                {
                    return;
                }

                readings.Clear();

                // Radar uses sphere sweep via spatial grid
                // ECM interference reduces confidence: confidence *= (1 - ecmStrength)
                // TODO: Integrate with spatial grid for sphere sweep queries
            }
        }
    }
}

