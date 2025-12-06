using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.AI.Social.Systems
{
    /// <summary>
    /// Social performance profiler for telemetry tracking.
    /// Tracks trust, reputation, and morale averages for debugging and performance monitoring.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(MotivationSystem))]
    [BurstCompile]
    public partial struct SocialPerformanceProfiler : ISystem
    {
        private static readonly FixedString64Bytes TrustAverageKey = "Social.Trust.Average";
        private static readonly FixedString64Bytes ReputationAverageKey = "Social.Reputation.Average";
        private static readonly FixedString64Bytes MoraleAverageKey = "Social.Morale.Average";
        private static readonly FixedString64Bytes CooperationCountKey = "Social.Cooperation.Count";
        private static readonly FixedString64Bytes SocialMessageCountKey = "Social.Message.Count";

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TelemetryStream>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Telemetry collection handled in managed wrapper
        }
    }

    /// <summary>
    /// Managed wrapper for SocialPerformanceProfiler that collects telemetry metrics.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(MotivationSystem))]
    public sealed partial class SocialPerformanceProfilerManaged : SystemBase
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 1.0f; // 1 Hz telemetry updates

        protected override void OnCreate()
        {
            _lastUpdateTime = 0f;
            RequireForUpdate<TelemetryStream>();
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return; // Temporal batching
            }

            // Get telemetry stream entity
            if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            {
                return;
            }

            if (!SystemAPI.HasBuffer<TelemetryMetric>(telemetryEntity))
            {
                return;
            }

            var telemetryBuffer = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);

            // Calculate averages from social components using jobs with parallel reduction
            var trustQuery = GetEntityQuery(typeof(SocialKnowledge), typeof(SocialRelationship));
            var trustCount = trustQuery.CalculateEntityCount();
            
            if (trustCount > 0)
            {
                using var trustSums = new NativeArray<float>(trustCount, Allocator.TempJob);
                using var reputationSums = new NativeArray<float>(trustCount, Allocator.TempJob);
                using var cooperationCounts = new NativeArray<int>(trustCount, Allocator.TempJob);

                var trustJob = new CollectTrustMetricsJob
                {
                    TrustSums = trustSums,
                    ReputationSums = reputationSums,
                    CooperationCounts = cooperationCounts
                };
                trustJob.ScheduleParallel(trustQuery, Dependency).Complete();

                // Reduce sums
                float trustSum = 0f, reputationSum = 0f;
                int cooperationCount = 0;
                for (int i = 0; i < trustCount; i++)
                {
                    trustSum += trustSums[i];
                    reputationSum += reputationSums[i];
                    cooperationCount += cooperationCounts[i];
                }

                telemetryBuffer.AddMetric(SocialPerformanceProfiler.TrustAverageKey, trustSum / trustCount, TelemetryMetricUnit.Ratio);
                telemetryBuffer.AddMetric(SocialPerformanceProfiler.ReputationAverageKey, reputationSum / trustCount, TelemetryMetricUnit.Ratio);
                telemetryBuffer.AddMetric(SocialPerformanceProfiler.CooperationCountKey, cooperationCount, TelemetryMetricUnit.Count);
            }

            var moraleQuery = GetEntityQuery(typeof(Motivation));
            var moraleCount = moraleQuery.CalculateEntityCount();
            
            if (moraleCount > 0)
            {
                using var moraleSums = new NativeArray<float>(moraleCount, Allocator.TempJob);
                var moraleJob = new CollectMoraleMetricsJob { MoraleSums = moraleSums };
                moraleJob.ScheduleParallel(moraleQuery, Dependency).Complete();

                float moraleSum = 0f;
                for (int i = 0; i < moraleCount; i++)
                {
                    moraleSum += moraleSums[i];
                }

                telemetryBuffer.AddMetric(SocialPerformanceProfiler.MoraleAverageKey, moraleSum / moraleCount, TelemetryMetricUnit.Ratio);
            }

            var messageQuery = GetEntityQuery(typeof(SocialMessage));
            var messageCount = messageQuery.CalculateEntityCount();
            telemetryBuffer.AddMetric(SocialPerformanceProfiler.SocialMessageCountKey, messageCount, TelemetryMetricUnit.Count);

            _lastUpdateTime = currentTime;
        }
    }

    [BurstCompile]
    private partial struct CollectTrustMetricsJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float> TrustSums;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> ReputationSums;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> CooperationCounts;

        public void Execute([EntityIndexInQuery] int index, in SocialKnowledge knowledge, in DynamicBuffer<SocialRelationship> relationships)
        {
            TrustSums[index] = knowledge.BaseTrust;
            ReputationSums[index] = knowledge.BaseReputation;
            CooperationCounts[index] = relationships.Length;
        }
    }

    [BurstCompile]
    private partial struct CollectMoraleMetricsJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float> MoraleSums;

        public void Execute([EntityIndexInQuery] int index, in Motivation motivation)
        {
            MoraleSums[index] = motivation.Morale;
        }
    }
    }
}

