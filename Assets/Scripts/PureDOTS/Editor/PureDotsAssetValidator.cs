#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PureDOTS.Authoring;

namespace PureDOTS.Editor
{
    internal static class PureDotsAssetValidator
    {
        private const string kMenuRoot = "PureDOTS/Validation/";

        [MenuItem(kMenuRoot + "Run Asset Validation", priority = 0)]
        public static void RunValidationMenu()
        {
            var report = ValidateAllAssets();
            LogReport(report, verbose: true);

            EditorUtility.DisplayDialog(
                "PureDOTS Asset Validation",
                BuildSummary(report),
                "OK");
        }

        [MenuItem(kMenuRoot + "Run Validation (Quiet Log)", priority = 1)]
        public static void RunValidationMenuQuiet()
        {
            var report = ValidateAllAssets();
            LogReport(report, verbose: false);

            EditorUtility.DisplayDialog(
                "PureDOTS Asset Validation",
                BuildSummary(report),
                "OK");
        }

        /// <summary>
        /// Entry point for CI / command-line validation. Invoke via
        /// -executeMethod PureDOTS.Editor.PureDotsAssetValidator.RunValidationFromCommandLine
        /// </summary>
        public static void RunValidationFromCommandLine()
        {
            var report = ValidateAllAssets();
            LogReport(report, verbose: true);

            // Exit with non-zero code if errors are present so CI fails fast.
            EditorApplication.Exit(report.HasErrors ? 1 : 0);
        }

        public static ValidationReport ValidateAllAssets()
        {
            var report = new ValidationReport();

            ValidateAssetsOfType<PureDotsRuntimeConfig>(ValidateRuntimeConfig, report);
            ValidateAssetsOfType<ResourceTypeCatalog>(ValidateResourceTypeCatalog, report);
            ValidateAssetsOfType<EnvironmentGridConfig>(ValidateEnvironmentGridConfig, report);
            ValidateAssetsOfType<SpatialPartitionProfile>(ValidateSpatialPartitionProfile, report);

            return report;
        }

        public static ValidationReport ValidateRuntimeConfig(PureDotsRuntimeConfig config)
        {
            var report = new ValidationReport();
            ValidateRuntimeConfig(config, report);
            return report;
        }

        public static ValidationReport ValidateResourceTypeCatalog(ResourceTypeCatalog catalog)
        {
            var report = new ValidationReport();
            ValidateResourceTypeCatalog(catalog, report);
            return report;
        }

        public static ValidationReport ValidateEnvironmentGridConfig(EnvironmentGridConfig config)
        {
            var report = new ValidationReport();
            ValidateEnvironmentGridConfig(config, report);
            return report;
        }

        public static ValidationReport ValidateSpatialPartitionProfile(SpatialPartitionProfile profile)
        {
            var report = new ValidationReport();
            ValidateSpatialPartitionProfile(profile, report);
            return report;
        }

        private static void ValidateRuntimeConfig(PureDotsRuntimeConfig config, ValidationReport report)
        {
            if (config == null)
            {
                return;
            }

            var time = config.Time;
            if (time.fixedDeltaTime <= 0f)
            {
                report.AddError(config, "Time.fixedDeltaTime must be greater than zero.");
            }

            if (time.defaultSpeedMultiplier <= 0f)
            {
                report.AddError(config, "Time.defaultSpeedMultiplier must be greater than zero.");
            }

            var history = config.History;
            if (history.minTicksPerSecond < 1f)
            {
                report.AddError(config, "History.minTicksPerSecond must be at least 1.");
            }

            if (history.maxTicksPerSecond < history.minTicksPerSecond)
            {
                report.AddError(config, "History.maxTicksPerSecond must be greater than or equal to minTicksPerSecond.");
            }

            if (history.defaultTicksPerSecond < history.minTicksPerSecond || history.defaultTicksPerSecond > history.maxTicksPerSecond)
            {
                report.AddWarning(config, "History.defaultTicksPerSecond should be within [min, max] bounds.");
            }

            var pooling = config.Pooling;
            if (pooling.nativeListCapacity < 4)
            {
                report.AddError(config, "Pooling.nativeListCapacity must be at least 4.");
            }

            if (pooling.nativeQueueCapacity < 4)
            {
                report.AddError(config, "Pooling.nativeQueueCapacity must be at least 4.");
            }

            if (pooling.entityPoolMaxReserve < pooling.defaultEntityPrewarmCount)
            {
                report.AddWarning(config, "Pooling.entityPoolMaxReserve is less than defaultEntityPrewarmCount; prewarmed entities may be evicted immediately.");
            }

            if (pooling.ecbPoolCapacity < 1 || pooling.ecbWriterPoolCapacity < 1)
            {
                report.AddError(config, "Pooling ECB capacities must be at least 1.");
            }

            if (config.ResourceTypes == null)
            {
                report.AddError(config, "Resource type catalog reference is missing.");
            }
            else if (config.ResourceTypes.entries.Count == 0)
            {
                report.AddWarning(config.ResourceTypes, "Resource type catalog is empty. Systems depending on resource types will have no data at runtime.");
            }
        }

        private static void ValidateResourceTypeCatalog(ResourceTypeCatalog catalog, ValidationReport report)
        {
            if (catalog == null)
            {
                return;
            }

            if (catalog.entries == null || catalog.entries.Count == 0)
            {
                report.AddWarning(catalog, "Catalog has no entries; downstream systems will treat all lookups as missing.");
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < catalog.entries.Count; i++)
            {
                var entry = catalog.entries[i];
                if (string.IsNullOrWhiteSpace(entry.id))
                {
                    report.AddError(catalog, $"Entry {i} has an empty id and will be ignored.");
                    continue;
                }

                var trimmed = entry.id.Trim();
                if (!seen.Add(trimmed))
                {
                    report.AddError(catalog, $"Duplicate resource id '{trimmed}' found. Ids must be unique (case insensitive).");
                }

                if (entry.displayColor.a <= 0f)
                {
                    report.AddWarning(catalog, $"Resource '{trimmed}' uses a display color with zero alpha; UI overlays may be invisible.");
                }
            }
        }

        private static void ValidateEnvironmentGridConfig(EnvironmentGridConfig config, ValidationReport report)
        {
            if (config == null)
            {
                return;
            }

            var channelIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { config.MoistureChannelId(), "Moisture" },
                { config.TemperatureChannelId(), "Temperature" },
                { config.SunlightChannelId(), "Sunlight" },
                { config.WindChannelId(), "Wind" },
                { config.BiomeChannelId(), "Biome" }
            };

            foreach (var kvp in channelIds)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    report.AddError(config, $"{kvp.Value} channel id cannot be empty.");
                }
            }

            var duplicates = channelIds
                .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1);

            foreach (var duplicate in duplicates)
            {
                var joined = string.Join(", ", duplicate.Select(d => d.Value));
                report.AddError(config, $"Channel id '{duplicate.Key}' is reused by: {joined}. Each grid requires a unique channel id.");
            }

            ValidateGrid("Moisture", config.Moisture, config, report, expectEnabled: true);
            ValidateGrid("Temperature", config.Temperature, config, report, expectEnabled: true);
            ValidateGrid("Sunlight", config.Sunlight, config, report, expectEnabled: true);
            ValidateGrid("Wind", config.Wind, config, report, expectEnabled: true);

            if (config.Biome.Enabled)
            {
                ValidateGrid("Biome", config.Biome, config, report, expectEnabled: false);
            }

            var sun = config.Sunlight;
            if (sun.Enabled)
            {
                // Sun direction is normalized in ToComponent; warn if the serialized value is close to zero to help artists.
                if (config.RawSunDirection().sqrMagnitude < 1e-4f)
                {
                    report.AddWarning(config, "Sunlight sun direction is almost zero; default downward direction will be used.");
                }
            }

            var windDirection = config.RawWindDirection();
            if (windDirection.sqrMagnitude < 1e-4f)
            {
                report.AddWarning(config, "Wind direction magnitude is almost zero; default forward direction will be used.");
            }
        }

        private static void ValidateGrid(string label, EnvironmentGridConfig.GridSettings settings, EnvironmentGridConfig asset, ValidationReport report, bool expectEnabled)
        {
            var metadata = settings.ToMetadata();

            if (metadata.CellCounts.x <= 0 || metadata.CellCounts.y <= 0)
            {
                report.AddError(asset, $"{label} grid has invalid resolution. Both axes must be greater than zero.");
            }

            if (metadata.CellCounts.x > 2048 || metadata.CellCounts.y > 2048)
            {
                report.AddWarning(asset, $"{label} grid resolution exceeds 2048 in at least one axis. Verify memory usage is acceptable for DOTS builds.");
            }

            if (metadata.CellSize <= 0f)
            {
                report.AddError(asset, $"{label} grid cell size must be greater than zero.");
            }

            if (expectEnabled && !settings.Enabled)
            {
                report.AddWarning(asset, $"{label} grid is disabled. Dependent systems may not function as expected.");
            }

            if (metadata.WorldMax.x <= metadata.WorldMin.x || metadata.WorldMax.z <= metadata.WorldMin.z)
            {
                report.AddError(asset, $"{label} grid world bounds are invalid. Ensure max values exceed min values on each axis.");
            }
        }

        private static void ValidateSpatialPartitionProfile(SpatialPartitionProfile profile, ValidationReport report)
        {
            if (profile == null)
            {
                return;
            }

            var min = profile.WorldMin;
            var max = profile.WorldMax;
            if (max.x <= min.x || max.y <= min.y || max.z <= min.z)
            {
                report.AddError(profile, "World bounds must define a positive volume (max must exceed min on all axes).");
            }

            if (profile.CellSize < 0.5f)
            {
                report.AddError(profile, "Cell size must be at least 0.5 to avoid unstable cell counts.");
            }
            else if (profile.CellSize > 32f)
            {
                report.AddWarning(profile, "Cell size is very large (>32). Spatial queries may become too coarse for villager/job systems.");
            }

            var config = profile.ToComponent();
            if (config.CellCounts.x * config.CellCounts.y * config.CellCounts.z > 4_000_000)
            {
                report.AddWarning(profile, "Spatial grid cell count exceeds 4 million. Verify memory/performance budgets.");
            }

            if (profile.Provider == SpatialProviderType.UniformGrid)
            {
                var extent = profile.WorldMax - profile.WorldMin;
                var cellSize = profile.CellSize;
                if (Mathf.Abs((extent.x / cellSize) - Mathf.Round(extent.x / cellSize)) > 0.01f ||
                    Mathf.Abs((extent.z / cellSize) - Mathf.Round(extent.z / cellSize)) > 0.01f)
                {
                    report.AddWarning(profile, "Uniform grid bounds are not an even multiple of cell size; edge cells may be uneven.");
                }
            }

            if (profile.HashSeed == 0 && profile.Provider == SpatialProviderType.HashedGrid)
            {
                report.AddInfo(profile, "Hash seed is 0. Consider choosing a non-zero deterministic seed to avoid accidental collisions across projects.");
            }
        }

        private static void ValidateAssetsOfType<T>(Action<T, ValidationReport> validator, ValidationReport report) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                var beforeCount = report.MessageCount;
                validator(asset, report);
                var afterCount = report.MessageCount;

                if (afterCount == beforeCount)
                {
                    report.AddInfo(asset, "Validated successfully.");
                }
            }
        }

        internal static void LogReport(ValidationReport report, bool verbose)
        {
            foreach (var message in report.Messages)
            {
                switch (message.Severity)
                {
                    case ValidationSeverity.Info:
                        if (verbose)
                        {
                            Debug.Log(message.ToLogString(), message.Context);
                        }
                        break;
                    case ValidationSeverity.Warning:
                        Debug.LogWarning(message.ToLogString(), message.Context);
                        break;
                    case ValidationSeverity.Error:
                        Debug.LogError(message.ToLogString(), message.Context);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal static string BuildSummary(ValidationReport report)
        {
            return $"Validation complete. Errors: {report.ErrorCount}, Warnings: {report.WarningCount}, Infos: {report.InfoCount}.";
        }
    }

    internal sealed class ValidationReport
    {
        private readonly List<ValidationMessage> _messages = new();

        public IReadOnlyList<ValidationMessage> Messages => _messages;
        public int MessageCount => _messages.Count;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;

        public void AddInfo(UnityEngine.Object context, string message) => AddMessage(ValidationSeverity.Info, context, message);
        public void AddWarning(UnityEngine.Object context, string message) => AddMessage(ValidationSeverity.Warning, context, message);
        public void AddError(UnityEngine.Object context, string message) => AddMessage(ValidationSeverity.Error, context, message);

        private void AddMessage(ValidationSeverity severity, UnityEngine.Object context, string message)
        {
            _messages.Add(new ValidationMessage(severity, context, message));

            switch (severity)
            {
                case ValidationSeverity.Info:
                    InfoCount++;
                    break;
                case ValidationSeverity.Warning:
                    WarningCount++;
                    break;
                case ValidationSeverity.Error:
                    ErrorCount++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }
    }

    internal readonly struct ValidationMessage
    {
        public ValidationSeverity Severity { get; }
        public UnityEngine.Object Context { get; }
        public string Message { get; }

        public ValidationMessage(ValidationSeverity severity, UnityEngine.Object context, string message)
        {
            Severity = severity;
            Context = context;
            Message = message;
        }

        public string ToLogString()
        {
            var contextName = Context != null ? Context.name : "<null>";
            var path = Context != null ? AssetDatabase.GetAssetPath(Context) : string.Empty;
            return $"[{Severity}] {contextName}: {Message}{(string.IsNullOrEmpty(path) ? string.Empty : $" (Asset: {path})")}";
        }
    }

    internal enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }
}
#endif

