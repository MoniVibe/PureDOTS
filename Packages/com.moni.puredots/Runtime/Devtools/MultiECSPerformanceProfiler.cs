using System.Collections.Generic;
using System.Text;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Runtime.Devtools
{
    /// <summary>
    /// Tracks sync costs, tick rates, and memory usage for multi-ECS architecture.
    /// Validates < 3ms/frame sync cost target.
    /// </summary>
    public class MultiECSPerformanceProfiler
    {
        private static readonly ProfilerMarker SyncMarker = new("MultiECS.Sync");
        private static readonly ProfilerMarker MindToBodyMarker = new("MultiECS.MindToBody");
        private static readonly ProfilerMarker BodyToMindMarker = new("MultiECS.BodyToMind");

        private struct SyncMetrics
        {
            public float TotalSyncTimeMs;
            public float MindToBodyTimeMs;
            public float BodyToMindTimeMs;
            public int MindToBodyMessageCount;
            public int BodyToMindMessageCount;
            public uint TickNumber;
        }

        private readonly Queue<SyncMetrics> _recentMetrics;
        private const int MaxMetricsHistory = 100;
        private float _lastReportTime;
        private const float ReportInterval = 1f; // Report every second

        public MultiECSPerformanceProfiler()
        {
            _recentMetrics = new Queue<SyncMetrics>();
            _lastReportTime = 0f;
        }

        public void RecordSyncMetrics(
            float mindToBodyTimeMs,
            float bodyToMindTimeMs,
            int mindToBodyCount,
            int bodyToMindCount,
            uint tickNumber)
        {
            var metrics = new SyncMetrics
            {
                TotalSyncTimeMs = mindToBodyTimeMs + bodyToMindTimeMs,
                MindToBodyTimeMs = mindToBodyTimeMs,
                BodyToMindTimeMs = bodyToMindTimeMs,
                MindToBodyMessageCount = mindToBodyCount,
                BodyToMindMessageCount = bodyToMindCount,
                TickNumber = tickNumber
            };

            _recentMetrics.Enqueue(metrics);
            while (_recentMetrics.Count > MaxMetricsHistory)
            {
                _recentMetrics.Dequeue();
            }
        }

        public void Update(float currentTime)
        {
            if (currentTime - _lastReportTime >= ReportInterval)
            {
                ReportMetrics();
                _lastReportTime = currentTime;
            }
        }

        private void ReportMetrics()
        {
            if (_recentMetrics.Count == 0)
            {
                return;
            }

            float totalSyncAvg = 0f;
            float mindToBodyAvg = 0f;
            float bodyToMindAvg = 0f;
            int totalMindToBody = 0;
            int totalBodyToMind = 0;
            float maxSyncTime = 0f;

            foreach (var metric in _recentMetrics)
            {
                totalSyncAvg += metric.TotalSyncTimeMs;
                mindToBodyAvg += metric.MindToBodyTimeMs;
                bodyToMindAvg += metric.BodyToMindTimeMs;
                totalMindToBody += metric.MindToBodyMessageCount;
                totalBodyToMind += metric.BodyToMindMessageCount;
                if (metric.TotalSyncTimeMs > maxSyncTime)
                {
                    maxSyncTime = metric.TotalSyncTimeMs;
                }
            }

            int count = _recentMetrics.Count;
            totalSyncAvg /= count;
            mindToBodyAvg /= count;
            bodyToMindAvg /= count;

            var report = new StringBuilder();
            report.AppendLine("[MultiECS Performance]");
            report.AppendLine($"  Avg Sync Time: {totalSyncAvg:F3}ms (target: < 3ms)");
            report.AppendLine($"  Max Sync Time: {maxSyncTime:F3}ms");
            report.AppendLine($"  Mind→Body: {mindToBodyAvg:F3}ms ({totalMindToBody / count} msgs/update)");
            report.AppendLine($"  Body→Mind: {bodyToMindAvg:F3}ms ({totalBodyToMind / count} msgs/update)");

            if (totalSyncAvg > 3f)
            {
                report.AppendLine($"  ⚠️ WARNING: Sync cost exceeds target (3ms)");
            }
            else
            {
                report.AppendLine($"  ✓ Sync cost within target");
            }

            UnityDebug.Log(report.ToString());
        }

        public string GetMetricsReport()
        {
            if (_recentMetrics.Count == 0)
            {
                return "No metrics collected yet.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"multiECS\": {");

            float totalSyncAvg = 0f;
            float mindToBodyAvg = 0f;
            float bodyToMindAvg = 0f;
            int totalMindToBody = 0;
            int totalBodyToMind = 0;
            float maxSyncTime = 0f;

            foreach (var metric in _recentMetrics)
            {
                totalSyncAvg += metric.TotalSyncTimeMs;
                mindToBodyAvg += metric.MindToBodyTimeMs;
                bodyToMindAvg += metric.BodyToMindTimeMs;
                totalMindToBody += metric.MindToBodyMessageCount;
                totalBodyToMind += metric.BodyToMindMessageCount;
                if (metric.TotalSyncTimeMs > maxSyncTime)
                {
                    maxSyncTime = metric.TotalSyncTimeMs;
                }
            }

            int count = _recentMetrics.Count;
            totalSyncAvg /= count;
            mindToBodyAvg /= count;
            bodyToMindAvg /= count;

            sb.AppendLine($"    \"avgSyncTimeMs\": {totalSyncAvg:F3},");
            sb.AppendLine($"    \"maxSyncTimeMs\": {maxSyncTime:F3},");
            sb.AppendLine($"    \"avgMindToBodyMs\": {mindToBodyAvg:F3},");
            sb.AppendLine($"    \"avgBodyToMindMs\": {bodyToMindAvg:F3},");
            sb.AppendLine($"    \"avgMindToBodyMessages\": {totalMindToBody / count},");
            sb.AppendLine($"    \"avgBodyToMindMessages\": {totalBodyToMind / count},");
            sb.AppendLine($"    \"samples\": {count},");
            sb.AppendLine($"    \"withinBudget\": {totalSyncAvg <= 3f}");
            sb.AppendLine("  }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public void Clear()
        {
            _recentMetrics.Clear();
        }
    }
}

