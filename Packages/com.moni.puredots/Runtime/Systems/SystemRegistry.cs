#nullable enable
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Central registry for DOTS systems so we can deterministically control world creation.
    /// </summary>
    public static partial class SystemRegistry
    {
        private const string EnvironmentProfileKey = "PURE_DOTS_BOOTSTRAP_PROFILE";

        private static readonly Dictionary<string, BootstrapWorldProfile> s_profiles = new();
        private static readonly List<Type> s_profileScratch = new(1024);
        private static readonly HashSet<Type> s_seenTypes = new();
        private static string? s_overrideProfileId;
        private static bool s_initialized;

        static SystemRegistry()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (s_initialized)
                return;

            RegisterBuiltInProfiles();
            s_initialized = true;
        }

        public static void RegisterProfile(BootstrapWorldProfile profile, bool overwrite = false)
        {
            if (s_profiles.ContainsKey(profile.Id) && !overwrite)
                throw new InvalidOperationException($"Bootstrap profile '{profile.Id}' already registered.");

            s_profiles[profile.Id] = profile;
        }

        public static bool TryGetProfile(string id, out BootstrapWorldProfile profile)
        {
            Initialize();
            return s_profiles.TryGetValue(id, out profile);
        }

        public static IEnumerable<BootstrapWorldProfile> GetProfiles()
        {
            Initialize();
            return s_profiles.Values;
        }

        public static void OverrideActiveProfile(string? profileId)
        {
            s_overrideProfileId = profileId;
        }

        public static BootstrapWorldProfile ResolveActiveProfile()
        {
            Initialize();

            if (!string.IsNullOrWhiteSpace(s_overrideProfileId) && s_profiles.TryGetValue(s_overrideProfileId, out var overrideProfile))
            {
                return overrideProfile;
            }

            var envProfile = System.Environment.GetEnvironmentVariable(EnvironmentProfileKey);
            if (!string.IsNullOrWhiteSpace(envProfile) && s_profiles.TryGetValue(envProfile, out var envResolved))
            {
                return envResolved;
            }

            if (Application.isBatchMode && s_profiles.TryGetValue(BuiltinProfiles.HeadlessId, out var headlessProfile))
            {
                return headlessProfile;
            }

            if (s_profiles.TryGetValue(BuiltinProfiles.GameWorldId, out var gameProfile))
            {
                return gameProfile;
            }

            return s_profiles[BuiltinProfiles.DefaultId];
        }

        public static IReadOnlyList<Type> GetSystems(in BootstrapWorldProfile profile)
        {
            Initialize();

            s_profileScratch.Clear();
            s_seenTypes.Clear();

            var systems = DefaultWorldInitialization.GetAllSystems(profile.FilterFlags);

            foreach (var systemType in systems)
            {
                if (profile.ShouldInclude(systemType) && s_seenTypes.Add(systemType))
                {
                    s_profileScratch.Add(systemType);
                }
            }

            foreach (var include in profile.ForcedInclusions)
            {
                if (include == null || s_seenTypes.Contains(include))
                    continue;

                s_profileScratch.Add(include);
                s_seenTypes.Add(include);
            }

            return s_profileScratch.ToArray();
        }

        private static void RegisterBuiltInProfiles()
        {
            if (s_profiles.Count > 0)
                return;

            var defaultFilter = WorldSystemFilterFlags.Default
                               | WorldSystemFilterFlags.Editor
                               | WorldSystemFilterFlags.Streaming
                               | WorldSystemFilterFlags.ProcessAfterLoad
                               | WorldSystemFilterFlags.EntitySceneOptimizations;

            RegisterProfile(new BootstrapWorldProfile(
                BuiltinProfiles.DefaultId,
                "Default Simulation",
                defaultFilter));

            RegisterProfile(new BootstrapWorldProfile(
                BuiltinProfiles.HeadlessId,
                "Headless Simulation",
                defaultFilter,
                exclusions: new[] { typeof(Unity.Entities.PresentationSystemGroup) },
                additionalFilter: ShouldIncludeInHeadlessProfile));

            RegisterProfile(new BootstrapWorldProfile(
                BuiltinProfiles.ReplayId,
                "Replay / Analysis",
                defaultFilter,
                forcedInclusions: new[]
                {
                    typeof(HistorySystemGroup),
                    typeof(EnvironmentSystemGroup),
                    typeof(SpatialSystemGroup),
                    typeof(GameplaySystemGroup)
                }));

            // Demo profile: includes all default systems PLUS demo systems for testing
            RegisterProfile(new BootstrapWorldProfile(
                BuiltinProfiles.DemoId,
                "Demo World",
                defaultFilter,
                additionalFilter: type =>
                {
                    // Include demo systems that start with "PureDOTS.Demo."
                    if (type.FullName != null && type.FullName.StartsWith("PureDOTS.Demo."))
                        return true;

                    // Include Hybrid systems for demo purposes
                    if (type.FullName != null && type.FullName.StartsWith("PureDOTS.Hybrid."))
                        return true;

                    // Use default filter for all other systems
                    return true;
                }));

            RegisterProfile(new BootstrapWorldProfile(
                BuiltinProfiles.GameWorldId,
                "Game World",
                defaultFilter,
                additionalFilter: ShouldIncludeInGameWorldProfile));
        }

        private static bool ShouldIncludeInHeadlessProfile(Type type)
        {
            var fullName = type.FullName;
            if (string.IsNullOrWhiteSpace(fullName))
                return false;

            if (fullName.StartsWith("Unity.Rendering.", StringComparison.Ordinal))
                return false;
            if (fullName.StartsWith("Unity.Entities.Graphics.", StringComparison.Ordinal))
                return false;
            if (fullName.StartsWith("PureDOTS.Rendering.", StringComparison.Ordinal))
                return false;
            if (fullName.StartsWith("PureDOTS.Systems.Presentation", StringComparison.Ordinal))
                return false;
            if (fullName.StartsWith("PureDOTS.Demo.", StringComparison.Ordinal))
                return false;
            if (fullName.StartsWith("PureDOTS.Runtime.Demo.", StringComparison.Ordinal))
                return false;

            if (fullName.Contains(".Presentation", StringComparison.Ordinal))
                return false;
            if (fullName.Contains(".Demo.", StringComparison.Ordinal))
                return false;

            return true;
        }

        private static bool ShouldIncludeInGameWorldProfile(Type type)
        {
            var fullName = type.FullName;
            if (string.IsNullOrWhiteSpace(fullName))
                return false;

            if (fullName.StartsWith("PureDOTS.Demo.", StringComparison.Ordinal))
                return false;
            if (fullName.StartsWith("PureDOTS.Runtime.Demo.", StringComparison.Ordinal))
                return false;
            if (fullName.StartsWith("PureDOTS.Systems.Hybrid.", StringComparison.Ordinal))
                return false;

            if (fullName.Contains(".Demo.", StringComparison.Ordinal))
                return false;

            if (fullName == "PureDOTS.Runtime.Economy.Production.ProductionRecipeBootstrapSystem")
                return false;
            if (fullName == "PureDOTS.Runtime.Economy.Wealth.WealthTierSpecBootstrapSystem")
                return false;
            if (fullName == "PureDOTS.Runtime.Economy.Resources.ItemSpecBootstrapSystem")
                return false;
            if (fullName == "PureDOTS.Systems.PresentationBindingSampleBootstrapSystem")
                return false;

            if (fullName == "PureDOTS.Rendering.RenderSanitySystem")
                return false;

            return true;
        }

        public static class BuiltinProfiles
        {
            public const string DefaultId = "default";
            public const string HeadlessId = "headless";
            public const string ReplayId = "replay";
            public const string GameWorldId = "gameworld";
            public const string DemoId = "demo";

            public static BootstrapWorldProfile Default => s_profiles[DefaultId];
            public static BootstrapWorldProfile Headless => s_profiles[HeadlessId];
            public static BootstrapWorldProfile Replay => s_profiles[ReplayId];
            public static BootstrapWorldProfile GameWorld => s_profiles[GameWorldId];
            public static BootstrapWorldProfile Demo => s_profiles[DemoId];
        }
    }
}
