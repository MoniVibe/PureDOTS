#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor.Migrations
{
    internal abstract class PureDotsMigration
    {
        /// <summary>
        /// Unique identifier (e.g., date-based "2025-10-26-schema-survey").
        /// Used for logging and ordering.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// Optional human-readable description.
        /// </summary>
        public virtual string Description => string.Empty;

        /// <summary>
        /// Order relative to other migrations. Lower values run first.
        /// </summary>
        public virtual int Order => 0;

        internal void Execute(PureDotsMigrationContext context)
        {
            try
            {
                Run(context);
            }
            catch (Exception ex)
            {
                context.Report.Error($"{Id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Implement migration logic. Use context.Report for logging and context.MarkModified when mutating assets.
        /// </summary>
        protected abstract void Run(PureDotsMigrationContext context);

        protected static IEnumerable<T> LoadAssets<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }

        internal static string GetAssetPath(UnityEngine.Object asset)
        {
            return asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
        }
    }

    internal sealed class PureDotsMigrationContext
    {
        private readonly List<UnityEngine.Object> _modified = new();

        internal PureDotsMigrationContext(bool applyChanges, PureDotsMigrationReport report)
        {
            ApplyChanges = applyChanges;
            Report = report;
        }

        public bool ApplyChanges { get; }
        public PureDotsMigrationReport Report { get; }
        public IReadOnlyList<UnityEngine.Object> ModifiedAssets => _modified;

        public void MarkModified(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            if (!_modified.Contains(asset))
            {
                _modified.Add(asset);
            }

            if (ApplyChanges)
            {
                EditorUtility.SetDirty(asset);
            }
        }

        public void LogDryRun(UnityEngine.Object asset, string message)
        {
            if (asset == null)
            {
                Report.Info(message);
                return;
            }

            var path = PureDotsMigration.GetAssetPath(asset);
            Report.Info($"[DryRun] {message} ({path})");
        }
    }

    internal sealed class PureDotsMigrationReport
    {
        private readonly List<string> _messages = new();

        public IEnumerable<string> Messages => _messages;
        public bool HasErrors { get; private set; }

        public void Info(string message)
        {
            _messages.Add($"INFO: {message}");
            Debug.Log(message);
        }

        public void Warning(string message)
        {
            _messages.Add($"WARN: {message}");
            Debug.LogWarning(message);
        }

        public void Error(string message)
        {
            _messages.Add($"ERROR: {message}");
            HasErrors = true;
            Debug.LogError(message);
        }
    }
}
#endif

