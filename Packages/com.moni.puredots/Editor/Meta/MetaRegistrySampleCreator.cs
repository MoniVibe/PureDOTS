#if UNITY_EDITOR
using System.IO;
using PureDOTS.Authoring;
using PureDOTS.Authoring.Meta;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor.Meta
{
    internal static class MetaRegistrySampleCreator
    {
        private const string SampleRoot = "Assets/Samples/PureDOTS/MetaRegistries";

        [MenuItem("PureDOTS/Samples/Create Meta Registry Samples")]
        public static void CreateSamples()
        {
            Directory.CreateDirectory(SampleRoot);

            CreateAsset(() => ScriptableObject.CreateInstance<FactionProfileAsset>(), "SampleFactionProfile.asset", asset =>
            {
                asset.factionId = 1;
                asset.displayName = "Sample Faction";
                asset.factionType = Runtime.Components.FactionType.PlayerControlled;
                asset.resourceStockpile = 2500f;
                asset.populationCount = 120;
                asset.territoryCellCount = 48;
                asset.territoryCenter = new Vector3(0f, 0f, 0f);
                asset.diplomaticStatus = Runtime.Components.DiplomaticStatusFlags.Allied;
            });

            CreateAsset(() => ScriptableObject.CreateInstance<ClimateHazardProfileAsset>(), "SampleClimateHazardProfile.asset", asset =>
            {
                asset.hazardName = "Sample Storm";
                asset.hazardType = Runtime.Components.ClimateHazardType.Storm;
                asset.currentIntensity = 0.35f;
                asset.maxIntensity = 0.9f;
                asset.radius = 20f;
                asset.durationTicks = 900u;
                asset.affectedChannels = Runtime.Components.EnvironmentChannelMask.Moisture | Runtime.Components.EnvironmentChannelMask.Wind;
            });

            CreateAsset(() => ScriptableObject.CreateInstance<AreaEffectProfileAsset>(), "SampleAreaEffectProfile.asset", asset =>
            {
                asset.effectName = "Inspiration Aura";
                asset.effectType = Runtime.Components.AreaEffectType.Buff;
                asset.currentStrength = 1.2f;
                asset.maxStrength = 1.5f;
                asset.radius = 10f;
                asset.affectedTargets = Runtime.Components.AreaEffectTargetMask.Villagers;
            });

            CreateAsset(() => ScriptableObject.CreateInstance<CultureProfileAsset>(), "SampleCultureProfile.asset", asset =>
            {
                asset.cultureId = 1;
                asset.cultureName = "Sample Culture";
                asset.cultureType = Runtime.Components.CultureType.Religious;
                asset.currentAlignment = 0.25f;
                asset.baseAlignment = 0.1f;
                asset.memberCount = 320;
                asset.alignmentFlags = Runtime.Components.CultureAlignmentFlags.Stable;
                asset.description = "Demonstrates how culture alignment data maps into the registry.";
            });

            CreateSceneSpawnSample();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("PureDOTS Samples", $"Meta registry sample assets created under {SampleRoot}.", "OK");
        }

        private static void CreateSceneSpawnSample()
        {
            CreateAsset(() => ScriptableObject.CreateInstance<SceneSpawnProfileAsset>(), "MetaRegistrySceneSpawnProfile.asset", asset =>
            {
                asset.seed = 12345u;
                asset.entries.Add(new SceneSpawnEntryDefinition
                {
                    category = Runtime.Components.SceneSpawnCategory.Faction,
                    prefab = null,
                    count = 1,
                    payloadId = "Faction:Sample",
                    payloadValue = 0f,
                    placement = Runtime.Components.SpawnPlacementMode.Point
                });

                asset.entries.Add(new SceneSpawnEntryDefinition
                {
                    category = Runtime.Components.SceneSpawnCategory.ClimateHazard,
                    prefab = null,
                    count = 1,
                    payloadId = "Hazard:Storm",
                    payloadValue = 0f,
                    placement = Runtime.Components.SpawnPlacementMode.Point
                });

                asset.entries.Add(new SceneSpawnEntryDefinition
                {
                    category = Runtime.Components.SceneSpawnCategory.AreaEffect,
                    prefab = null,
                    count = 1,
                    payloadId = "Effect:Aura",
                    payloadValue = 0f,
                    placement = Runtime.Components.SpawnPlacementMode.Point
                });

                asset.entries.Add(new SceneSpawnEntryDefinition
                {
                    category = Runtime.Components.SceneSpawnCategory.Culture,
                    prefab = null,
                    count = 1,
                    payloadId = "Culture:Sample",
                    payloadValue = 0f,
                    placement = Runtime.Components.SpawnPlacementMode.Point
                });
            });
        }

        private static void CreateAsset<T>(System.Func<T> factory, string fileName, System.Action<T> configure)
            where T : ScriptableObject
        {
            var path = Path.Combine(SampleRoot, fileName).Replace('\\', '/');
            var asset = factory();
            configure?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
#endif


