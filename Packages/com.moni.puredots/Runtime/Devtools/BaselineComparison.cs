using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Devtools
{
    /// <summary>
    /// Compares current metrics vs baseline and auto-flags regressions.
    /// </summary>
    [BurstCompile]
    public static class BaselineComparison
    {
        /// <summary>
        /// Compares metrics against baseline and returns regression flags.
        /// </summary>
        [BurstCompile]
        public static bool CompareMetrics(
            in BenchmarkMetrics current,
            in BenchmarkMetrics baseline,
            float regressionThreshold,
            out NativeList<FixedString128Bytes> regressions)
        {
            regressions = new NativeList<FixedString128Bytes>(16, Allocator.Temp);
            bool hasRegressions = false;

            // Compare system group times
            var groupNames = new NativeList<FixedString64Bytes>(32, Allocator.Temp);
            foreach (var kvp in current.MeanMsPerGroup)
            {
                groupNames.Add(kvp.Key);
            }

            for (int i = 0; i < groupNames.Length; i++)
            {
                var groupName = groupNames[i];
                if (current.MeanMsPerGroup.TryGetValue(groupName, out float currentMs) &&
                    baseline.MeanMsPerGroup.TryGetValue(groupName, out float baselineMs))
                {
                    float regression = (currentMs - baselineMs) / baselineMs;
                    if (regression > regressionThreshold)
                    {
                        var message = new FixedString128Bytes();
                        message.Append("Regression in ");
                        message.Append(groupName);
                        message.Append(": ");
                        message.Append(regression * 100f);
                        message.Append("% slower");
                        regressions.Add(message);
                        hasRegressions = true;
                    }
                }
            }

            groupNames.Dispose();
            return hasRegressions;
        }
    }
}

