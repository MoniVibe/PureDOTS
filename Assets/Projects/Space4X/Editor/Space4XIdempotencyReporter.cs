#if UNITY_EDITOR && INCLUDE_SPACE4X_IN_PUREDOTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Space4X.Editor
{
    /// <summary>
    /// Reports idempotency hashes for prefab maker runs.
    /// Ensures two consecutive runs on same catalogs produce identical outputs.
    /// </summary>
    public static class Space4XIdempotencyReporter
    {
        private const string ReportsDirectory = "projects/space4x/reports";
        private const string HashReportFile = "idempotency_hashes.json";

        [System.Serializable]
        public class IdempotencyReport
        {
            public string timestamp;
            public Dictionary<string, string> assetHashes = new();
            public string bindingsMinimalHash;
            public string bindingsFancyHash;
        }

        public static void ReportHash(string assetPath, string hash)
        {
            var report = LoadOrCreateReport();
            report.assetHashes[assetPath] = hash;
            SaveReport(report);
        }

        public static void ReportBindingsHash(bool isMinimal, string hash)
        {
            var report = LoadOrCreateReport();
            if (isMinimal)
            {
                report.bindingsMinimalHash = hash;
            }
            else
            {
                report.bindingsFancyHash = hash;
            }
            SaveReport(report);
        }

        public static bool ValidateIdempotency(out string errorMessage)
        {
            errorMessage = null;
            var reportPath = Path.Combine(Application.dataPath, "..", ReportsDirectory, HashReportFile);
            
            if (!File.Exists(reportPath))
            {
                errorMessage = "No previous report found. This is the first run.";
                return true; // First run is always valid
            }

            var previousReport = JsonUtility.FromJson<IdempotencyReport>(File.ReadAllText(reportPath));
            var currentReport = LoadOrCreateReport();

            // Compare hashes
            var mismatches = new List<string>();
            foreach (var kvp in currentReport.assetHashes)
            {
                if (previousReport.assetHashes.TryGetValue(kvp.Key, out var previousHash))
                {
                    if (previousHash != kvp.Value)
                    {
                        mismatches.Add($"{kvp.Key}: hash changed from {previousHash} to {kvp.Value}");
                    }
                }
            }

            if (mismatches.Count > 0)
            {
                errorMessage = $"Idempotency violation detected:\n{string.Join("\n", mismatches)}";
                return false;
            }

            return true;
        }

        public static string ComputeAssetHash(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                return null;
            }

            var content = File.ReadAllText(assetPath);
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private static IdempotencyReport LoadOrCreateReport()
        {
            var reportPath = Path.Combine(Application.dataPath, "..", ReportsDirectory, HashReportFile);
            if (File.Exists(reportPath))
            {
                try
                {
                    return JsonUtility.FromJson<IdempotencyReport>(File.ReadAllText(reportPath));
                }
                catch
                {
                    // If parsing fails, create new report
                }
            }

            return new IdempotencyReport
            {
                timestamp = DateTime.UtcNow.ToString("O"),
                assetHashes = new Dictionary<string, string>()
            };
        }

        private static void SaveReport(IdempotencyReport report)
        {
            report.timestamp = DateTime.UtcNow.ToString("O");
            var directory = Path.Combine(Application.dataPath, "..", ReportsDirectory);
            Directory.CreateDirectory(directory);
            var reportPath = Path.Combine(directory, HashReportFile);
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, prettyPrint: true));
        }

        public static void ClearReport()
        {
            var reportPath = Path.Combine(Application.dataPath, "..", ReportsDirectory, HashReportFile);
            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }
        }
    }
}
#endif
