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
    /// Updates the sunlight grid based on climate time-of-day, seasonal variation, and simple vegetation occlusion.
    /// Produces deterministic direct/ambient light values that other environment systems can consume.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(ClimateStateUpdateSystem))]
    public partial struct SunlightGridUpdateSystem : ISystem
    {
        const uint kUpdateStrideTicks = 5u;
        private TimeAwareController _timeAware;
        private EntityQuery _vegetationQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<SunlightGrid>();
            state.RequireForUpdate<ClimateState>();

            _vegetationQuery = SystemAPI.QueryBuilder()
                .WithAll<VegetationLifecycle, LocalTransform>()
                .WithNone<VegetationDeadTag>()
                .Build();

            _timeAware = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState))
            {
                return;
            }

            if (!_timeAware.TryBegin(timeState, rewindState, out var context))
            {
                return;
            }

            var currentTick = context.Time.Tick;
            var sunlightEntity = SystemAPI.GetSingletonEntity<SunlightGrid>();
            var sunlightGrid = SystemAPI.GetComponent<SunlightGrid>(sunlightEntity);

            if (sunlightGrid.LastUpdateTick != uint.MaxValue)
            {
                var ticksSince = EnvironmentEffectUtility.TickDelta(currentTick, sunlightGrid.LastUpdateTick);
                if (ticksSince < kUpdateStrideTicks)
                {
                    return;
                }
            }

            if (!SystemAPI.HasBuffer<SunlightGridRuntimeSample>(sunlightEntity))
            {
                return;
            }

            var runtimeBuffer = SystemAPI.GetBuffer<SunlightGridRuntimeSample>(sunlightEntity);
            if (runtimeBuffer.Length == 0)
            {
                sunlightGrid.LastUpdateTick = currentTick;
                SystemAPI.SetComponent(sunlightEntity, sunlightGrid);
                return;
            }

            var climate = SystemAPI.GetSingleton<ClimateState>();
            CalculateSunlight(climate, out var sunDirection, out var directLight, out var ambientLight, out var sunScalar);

            var samples = runtimeBuffer.Reinterpret<SunlightSample>().AsNativeArray();

            var baselineJob = new SunlightBaselineJob
            {
                Samples = samples,
                DirectLight = directLight,
                AmbientLight = ambientLight
            };
            state.Dependency = baselineJob.ScheduleParallel(samples.Length, 128, state.Dependency);

            var occluderCounts = new NativeArray<int>(samples.Length, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            if (!_vegetationQuery.IsEmptyIgnoreFilter)
            {
                foreach (var (lifecycle, transform) in SystemAPI.Query<RefRO<VegetationLifecycle>, RefRO<LocalTransform>>().WithNone<VegetationDeadTag>())
                {
                    var stage = lifecycle.ValueRO.CurrentStage;
                    if (stage < VegetationLifecycle.LifecycleStage.Mature)
                    {
                        continue;
                    }

                    if (!EnvironmentGridMath.TryWorldToCell(sunlightGrid.Metadata, transform.ValueRO.Position, out var cell, out _))
                    {
                        continue;
                    }

                    var index = EnvironmentGridMath.GetCellIndex(sunlightGrid.Metadata, cell);
                    occluderCounts[index] = math.min(occluderCounts[index] + 1, ushort.MaxValue);
                }
            }

            var occlusionJob = new ApplyOcclusionJob
            {
                Samples = samples,
                OccluderCounts = occluderCounts,
                DirectPenaltyPerOccluder = 0.08f,
                AmbientBoostPerOccluder = 1.5f
            };
            state.Dependency = occlusionJob.ScheduleParallel(samples.Length, 128, state.Dependency);
            occluderCounts.Dispose(state.Dependency);

            sunlightGrid.SunDirection = sunDirection;
            sunlightGrid.SunIntensity = sunScalar;
            sunlightGrid.LastUpdateTick = currentTick;
            SystemAPI.SetComponent(sunlightEntity, sunlightGrid);
        }

        private static void CalculateSunlight(in PureDOTS.Environment.ClimateState climate, out float3 direction, out float direct, out float ambient, out float scalar)
        {
            var hours = math.clamp(climate.TimeOfDayHours, 0f, 24f);
            var dayProgress = hours / 24f;
            var dayAngle = dayProgress * math.PI * 2f;

            var sinElevation = math.sin(dayAngle - math.PI / 2f);
            var clampedSin = math.clamp(sinElevation, -1f, 1f);
            var sunHeight = math.max(0f, clampedSin);
            var elevationAngle = math.asin(clampedSin);
            var cosElevation = math.cos(elevationAngle);

            if (sunHeight <= 0.0001f)
            {
                direction = new float3(0f, -1f, 0f);
            }
            else
            {
                var azimuth = dayAngle + math.PI;
                var dir = new float3(
                    math.cos(azimuth) * cosElevation,
                    -sunHeight,
                    math.sin(azimuth) * cosElevation);
                direction = math.normalizesafe(dir, new float3(0f, -1f, 0f));
            }

            var cloudFactor = math.saturate(climate.CloudCover / 100f);
            var seasonScale = climate.CurrentSeason switch
            {
                Season.Summer => 1.1f,
                Season.Winter => 0.7f,
                _ => 0.9f
            };

            var baseDirect = math.pow(sunHeight, 0.65f) * 100f * seasonScale;
            var directLight = math.clamp(baseDirect * (1f - 0.7f * cloudFactor), 0f, 100f);

            var ambientBase = math.lerp(8f, 35f, cloudFactor);
            var ambientLight = math.clamp(ambientBase + directLight * 0.2f, 5f, 60f);
            if (sunHeight <= 0.0001f)
            {
                ambientLight = math.clamp(ambientBase * 0.5f, 5f, 25f);
            }

            direct = directLight;
            ambient = ambientLight;
            scalar = math.saturate(directLight / 100f);
        }

        [BurstCompile]
        private struct SunlightBaselineJob : IJobFor
        {
            public NativeArray<SunlightSample> Samples;
            public float DirectLight;
            public float AmbientLight;

            public void Execute(int index)
            {
                Samples[index] = new SunlightSample
                {
                    DirectLight = DirectLight,
                    AmbientLight = AmbientLight,
                    OccluderCount = 0
                };
            }
        }

        [BurstCompile]
        private struct ApplyOcclusionJob : IJobFor
        {
            public NativeArray<SunlightSample> Samples;
            [ReadOnly] public NativeArray<int> OccluderCounts;
            public float DirectPenaltyPerOccluder;
            public float AmbientBoostPerOccluder;

            public void Execute(int index)
            {
                var sample = Samples[index];
                var count = math.max(0, OccluderCounts[index]);
                sample.OccluderCount = (ushort)math.min(count, ushort.MaxValue);

                var directPenalty = 1f - math.saturate(count * DirectPenaltyPerOccluder);
                sample.DirectLight = math.max(0f, sample.DirectLight * directPenalty);

                var ambient = sample.AmbientLight + count * AmbientBoostPerOccluder;
                sample.AmbientLight = math.clamp(ambient, 5f, 100f);

                Samples[index] = sample;
            }
        }
    }
}
