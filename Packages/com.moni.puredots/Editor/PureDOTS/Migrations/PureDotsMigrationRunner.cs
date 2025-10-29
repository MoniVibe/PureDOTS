#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor.Migrations
{
    internal static class PureDotsMigrationRunner
    {
        private const string MenuRoot = "PureDOTS/Migrations/";

        [MenuItem(MenuRoot + "Run All Migrations (Apply)", priority = 10)]
        public static void RunAllApply()
        {
            RunMigrations(applyChanges: true);
        }

        [MenuItem(MenuRoot + "Run All Migrations (Dry Run)", priority = 11)]
        public static void RunAllDryRun()
        {
            RunMigrations(applyChanges: false);
        }

        /// <summary>
        /// Command-line entry point: -executeMethod PureDOTS.Editor.Migrations.PureDotsMigrationRunner.RunAllFromCommandLine
        /// Optional arguments: "--apply" (default dry run).
        /// </summary>
        public static void RunAllFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            var apply = args.Any(a => string.Equals(a, "--apply", StringComparison.OrdinalIgnoreCase));

            var report = RunMigrations(apply);

            if (report.HasErrors)
            {
                EditorApplication.Exit(1);
            }

            EditorApplication.Exit(0);
        }

        public static PureDotsMigrationReport RunMigrations(bool applyChanges)
        {
            var report = new PureDotsMigrationReport();
            var context = new PureDotsMigrationContext(applyChanges, report);

            var migrations = LoadMigrations()
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Id)
                .ToList();

            if (migrations.Count == 0)
            {
                report.Info("No migrations registered.");
                return report;
            }

            report.Info($"Running {migrations.Count} migration(s) (apply={applyChanges})");

            foreach (var migration in migrations)
            {
                report.Info($"Running migration {migration.Id}..." + (string.IsNullOrEmpty(migration.Description) ? string.Empty : $" {migration.Description}"));
                migration.Execute(context);
            }

            if (applyChanges && context.ModifiedAssets.Count > 0)
            {
                AssetDatabase.SaveAssets();
                report.Info($"Saved {context.ModifiedAssets.Count} asset(s).");
            }

            report.Info("Migration run complete.");
            return report;
        }

        private static IEnumerable<PureDotsMigration> LoadMigrations()
        {
            var type = typeof(PureDotsMigration);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => SafeGetTypes(a, type))
                .Select(t => (PureDotsMigration)Activator.CreateInstance(t));
        }

        private static IEnumerable<Type> SafeGetTypes(System.Reflection.Assembly assembly, Type baseType)
        {
            try
            {
                return assembly.GetTypes().Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t));
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Debug.LogWarning(loaderException.Message);
                }

                return Enumerable.Empty<Type>();
            }
        }
    }
}
#endif

