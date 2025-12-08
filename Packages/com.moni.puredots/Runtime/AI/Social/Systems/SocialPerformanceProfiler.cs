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
        private FixedString64Bytes _trustAverageKey;
        private FixedString64Bytes _reputationAverageKey;
        private FixedString64Bytes _moraleAverageKey;
        private FixedString64Bytes _cooperationCountKey;
        private FixedString64Bytes _socialMessageCountKey;

        protected override void OnCreate()
        {
            _lastUpdateTime = 0f;
            RequireForUpdate<TelemetryStream>();

            // Managed initialization keeps Burst paths free of FixedString static constructors.
            _trustAverageKey = new FixedString64Bytes("Social.Trust.Average");
            _reputationAverageKey = new FixedString64Bytes("Social.Reputation.Average");
            _moraleAverageKey = new FixedString64Bytes("Social.Morale.Average");
            _cooperationCountKey = new FixedString64Bytes("Social.Cooperation.Count");
            _socialMessageCountKey = new FixedString64Bytes("Social.Message.Count");
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return; // Temporal batching
            }

            var writer = TelemetryHub.AsParallelWriter();

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

                Enqueue(writer, _trustAverageKey, trustSum / trustCount, TelemetryMetricUnit.Ratio);
                Enqueue(writer, _reputationAverageKey, reputationSum / trustCount, TelemetryMetricUnit.Ratio);
                Enqueue(writer, _cooperationCountKey, cooperationCount, TelemetryMetricUnit.Count);
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

                Enqueue(writer, _moraleAverageKey, moraleSum / moraleCount, TelemetryMetricUnit.Ratio);
            }

            var messageQuery = GetEntityQuery(typeof(SocialMessage));
            var messageCount = messageQuery.CalculateEntityCount();
            Enqueue(writer, _socialMessageCountKey, messageCount, TelemetryMetricUnit.Count);

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

    private static void Enqueue(NativeQueue<TelemetryMetric>.ParallelWriter writer, in FixedString64Bytes key, float value, TelemetryMetricUnit unit)
    {
        if (writer.Equals(default))
        {
            TelemetryHub.Enqueue(new TelemetryMetric { Key = key, Value = value, Unit = unit });
        }
        else
        {
            writer.Enqueue(new TelemetryMetric { Key = key, Value = value, Unit = unit });
        }
    }
}

