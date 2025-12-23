#if UNITY_EDITOR && INCLUDE_SPACE4X_IN_PUREDOTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PureDOTS.Authoring.Combat;
using PureDOTS.Authoring.Resource;
using UnityEditor;
using UnityEngine;

namespace Space4X.Editor
{
    /// <summary>
    /// Generates coverage heatmap after Prefab Maker runs.
    /// Reports % of catalog IDs with valid prefab/binding.
    /// </summary>
    public static class Space4XCoverageReporter
    {
        private const string ReportsDirectory = "projects/space4x/reports";
        private const string CoverageReportFile = "coverage_report.json";

        [System.Serializable]
        public class CoverageReport
        {
            public string timestamp;
            public Dictionary<string, CategoryCoverage> categories = new();
        }

        [System.Serializable]
        public class CategoryCoverage
        {
            public int totalIds;
            public int validPrefabs;
            public int validBindings;
            public float prefabCoveragePercent;
            public float bindingCoveragePercent;
            public List<string> missingPrefabs = new();
            public List<string> missingBindings = new();
        }

        public static void GenerateReport()
        {
            var report = new CoverageReport
            {
                timestamp = DateTime.UtcNow.ToString("O"),
                categories = new Dictionary<string, CategoryCoverage>()
            };

            // Check weapon coverage
            report.categories["weapons"] = CheckWeaponCoverage();
            
            // Check projectile coverage
            report.categories["projectiles"] = CheckProjectileCoverage();
            
            // Check hull coverage
            report.categories["hulls"] = CheckHullCoverage();
            
            // Check station coverage
            report.categories["stations"] = CheckStationCoverage();
            
            // Check resource coverage
            report.categories["resources"] = CheckResourceCoverage();

            SaveReport(report);
            Debug.Log($"[Space4XCoverageReporter] Coverage report generated: {GetSummary(report)}");
        }

        private static CategoryCoverage CheckWeaponCoverage()
        {
            var coverage = new CategoryCoverage();
            
            // Find all weapon catalogs
            var weaponCatalogs = AssetDatabase.FindAssets("t:WeaponCatalogAsset");
            var totalIds = new HashSet<string>();
            var validBindings = new HashSet<string>();

            foreach (var guid in weaponCatalogs)
            {
                var asset = AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset?.Entries == null) continue;

                foreach (var entry in asset.Entries)
                {
                    var id = string.IsNullOrWhiteSpace(entry.WeaponId) ? $"weapon.{entry.Class}" : entry.WeaponId.ToLowerInvariant();
                    totalIds.Add(id);
                    
                    // Check if binding exists
                    if (HasBinding("weapons", id))
                    {
                        validBindings.Add(id);
                    }
                    else
                    {
                        coverage.missingBindings.Add(id);
                    }
                }
            }

            coverage.totalIds = totalIds.Count;
            coverage.validBindings = validBindings.Count;
            coverage.bindingCoveragePercent = coverage.totalIds > 0 
                ? (coverage.validBindings / (float)coverage.totalIds) * 100f 
                : 0f;

            return coverage;
        }

        private static CategoryCoverage CheckProjectileCoverage()
        {
            var coverage = new CategoryCoverage();
            
            // Similar logic for projectiles
            var projectileCatalogs = AssetDatabase.FindAssets("t:ProjectileCatalogAsset");
            var totalIds = new HashSet<string>();
            var validBindings = new HashSet<string>();

            foreach (var guid in projectileCatalogs)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ProjectileCatalogAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset?.Entries == null) continue;

                foreach (var entry in asset.Entries)
                {
                    var id = string.IsNullOrWhiteSpace(entry.ProjectileId) ? $"projectile.{entry.Kind}" : entry.ProjectileId.ToLowerInvariant();
                    totalIds.Add(id);
                    
                    if (HasBinding("projectiles", id))
                    {
                        validBindings.Add(id);
                    }
                    else
                    {
                        coverage.missingBindings.Add(id);
                    }
                }
            }

            coverage.totalIds = totalIds.Count;
            coverage.validBindings = validBindings.Count;
            coverage.bindingCoveragePercent = coverage.totalIds > 0 
                ? (coverage.validBindings / (float)coverage.totalIds) * 100f 
                : 0f;

            return coverage;
        }

        private static CategoryCoverage CheckHullCoverage()
        {
            // Placeholder
            return new CategoryCoverage { totalIds = 0, validPrefabs = 0, validBindings = 0 };
        }

        private static CategoryCoverage CheckStationCoverage()
        {
            // Placeholder
            return new CategoryCoverage { totalIds = 0, validPrefabs = 0, validBindings = 0 };
        }

        private static CategoryCoverage CheckResourceCoverage()
        {
            // Placeholder
            return new CategoryCoverage { totalIds = 0, validPrefabs = 0, validBindings = 0 };
        }

        private static bool HasBinding(string category, string id)
        {
            // Check if binding exists in current binding set
            var bindings = Space4XBindingLoader.CurrentBindings;
            if (bindings == null) return false;

            return category switch
            {
                "weapons" => bindings.weapons?.Any(w => w.id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? false,
                "projectiles" => bindings.projectiles?.Any(p => p.id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? false,
                _ => false
            };
        }

        private static string GetSummary(CoverageReport report)
        {
            var lines = new List<string>();
            foreach (var kvp in report.categories)
            {
                var cat = kvp.Value;
                lines.Add($"{kvp.Key}: {cat.bindingCoveragePercent:F1}% bindings ({cat.validBindings}/{cat.totalIds})");
            }
            return string.Join(", ", lines);
        }

        private static void SaveReport(CoverageReport report)
        {
            var directory = Path.Combine(Application.dataPath, "..", ReportsDirectory);
            Directory.CreateDirectory(directory);
            var reportPath = Path.Combine(directory, CoverageReportFile);
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, prettyPrint: true));
        }
    }
}
#endif
