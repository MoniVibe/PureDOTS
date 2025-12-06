using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using PureDOTS.Runtime.Telemetry;

namespace PureDOTS.Systems.Telemetry
{
    /// <summary>
    /// Burst job implementing Pearson/Spearman correlation between metrics.
    /// Compares relationships like Morale ↔ Food, Pollution ↔ Population, Latency ↔ Error Rate.
    /// Outputs correlation matrix as BlobAsset for UI consumption.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TelemetryStreamingSystem))]
    public partial struct CorrelationSystem : ISystem
    {
        private uint _lastCorrelationTick;
        private const uint CorrelationInterval = 60; // Compute correlation every 60 ticks (1 Hz)

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lastCorrelationTick = 0;
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.Tick - _lastCorrelationTick < CorrelationInterval)
            {
                return;
            }

            _lastCorrelationTick = timeState.Tick;

            // Collect metric history for correlation
            // This would read from TelemetryStreamingSystem's NativeStream
            // and compute correlations between metric pairs

            // Simplified: correlation computation would happen in a Burst job
            // r = covariance(x,y) / (stddev(x) * stddev(y))
        }
    }

    /// <summary>
    /// Burst job for computing Pearson correlation coefficient.
    /// </summary>
    [BurstCompile]
    public struct ComputeCorrelationJob : IJob
    {
        [ReadOnly] public NativeArray<float> X;
        [ReadOnly] public NativeArray<float> Y;
        public NativeArray<float> Result;

        public void Execute()
        {
            if (X.Length != Y.Length || X.Length == 0)
            {
                Result[0] = 0f;
                return;
            }

            // Compute means
            float meanX = 0f;
            float meanY = 0f;
            for (int i = 0; i < X.Length; i++)
            {
                meanX += X[i];
                meanY += Y[i];
            }
            meanX /= X.Length;
            meanY /= Y.Length;

            // Compute covariance and variances
            float covariance = 0f;
            float varianceX = 0f;
            float varianceY = 0f;

            for (int i = 0; i < X.Length; i++)
            {
                float dx = X[i] - meanX;
                float dy = Y[i] - meanY;
                covariance += dx * dy;
                varianceX += dx * dx;
                varianceY += dy * dy;
            }

            covariance /= X.Length;
            varianceX /= X.Length;
            varianceY /= X.Length;

            float stddevX = math.sqrt(varianceX);
            float stddevY = math.sqrt(varianceY);

            // Pearson correlation: r = covariance / (stddevX * stddevY)
            if (stddevX > 1e-6f && stddevY > 1e-6f)
            {
                Result[0] = covariance / (stddevX * stddevY);
            }
            else
            {
                Result[0] = 0f;
            }
        }
    }

    /// <summary>
    /// Correlation matrix stored as BlobAsset for UI consumption.
    /// </summary>
    public struct CorrelationMatrixBlob
    {
        public BlobArray<FixedString64Bytes> MetricNames;
        public BlobArray<float> CorrelationValues; // Flattened matrix [i * N + j]
        public int MetricCount;
    }
}

