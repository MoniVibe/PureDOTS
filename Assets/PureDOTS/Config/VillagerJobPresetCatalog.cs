using System.Collections.Generic;
using UnityEngine;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Config
{
    [CreateAssetMenu(fileName = "VillagerJobPresetCatalog", menuName = "PureDOTS/Villager Job Preset Catalog", order = 0)]
    public sealed class VillagerJobPresetCatalog : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/PureDOTS/Config/VillagerJobPresetCatalog.asset";

        [SerializeField]
        private VillagerJobPresetDefinition[] _presets = s_defaultPresets;

        public IReadOnlyList<VillagerJobPresetDefinition> Presets => _presets;

        public bool TryGetPreset(string id, out VillagerJobPresetDefinition preset)
        {
            if (!string.IsNullOrEmpty(id) && _presets != null)
            {
                for (int i = 0; i < _presets.Length; i++)
                {
                    if (string.Equals(_presets[i].Id, id, System.StringComparison.OrdinalIgnoreCase))
                    {
                        preset = _presets[i];
                        return true;
                    }
                }
            }

            preset = default;
            return false;
        }

        public VillagerJobPresetDefinition GetPresetByIndex(int index)
        {
            if (_presets == null || _presets.Length == 0)
            {
                return s_defaultPresets[index % s_defaultPresets.Length];
            }

            var safeIndex = Mathf.Clamp(index, 0, _presets.Length - 1);
            return _presets[safeIndex];
        }

#if UNITY_EDITOR
        public static VillagerJobPresetCatalog LoadAsset()
        {
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<VillagerJobPresetCatalog>(DefaultAssetPath);
            if (catalog != null)
            {
                return catalog;
            }

            var guids = UnityEditor.AssetDatabase.FindAssets("t:PureDOTS.Config.VillagerJobPresetCatalog");
            if (guids != null && guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<VillagerJobPresetCatalog>(path);
            }

            return catalog;
        }
#endif

        public static IReadOnlyList<VillagerJobPresetDefinition> DefaultPresets => s_defaultPresets;

        private static readonly VillagerJobPresetDefinition[] s_defaultPresets =
        {
            new VillagerJobPresetDefinition
            {
                Id = "gatherer_standard",
                JobType = VillagerJob.JobType.Gatherer,
                BaseSpeed = 3f,
                InitialHunger = 40f,
                InitialEnergy = 85f,
                InitialMorale = 75f
            },
            new VillagerJobPresetDefinition
            {
                Id = "gatherer_sprinter",
                JobType = VillagerJob.JobType.Gatherer,
                BaseSpeed = 3.5f,
                InitialHunger = 35f,
                InitialEnergy = 90f,
                InitialMorale = 80f
            },
            new VillagerJobPresetDefinition
            {
                Id = "gatherer_stoic",
                JobType = VillagerJob.JobType.Gatherer,
                BaseSpeed = 2.8f,
                InitialHunger = 30f,
                InitialEnergy = 95f,
                InitialMorale = 85f
            }
        };
    }

    [System.Serializable]
    public struct VillagerJobPresetDefinition
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private VillagerJob.JobType jobType;

        [SerializeField]
        private float baseSpeed;

        [SerializeField]
        private float initialHunger;

        [SerializeField]
        private float initialEnergy;

        [SerializeField]
        private float initialMorale;

        public string Id
        {
            get => string.IsNullOrWhiteSpace(id) ? "unnamed" : id.Trim();
            set => id = value;
        }

        public VillagerJob.JobType JobType
        {
            get => jobType;
            set => jobType = value;
        }

        public float BaseSpeed
        {
            get => baseSpeed;
            set => baseSpeed = value;
        }

        public float InitialHunger
        {
            get => initialHunger;
            set => initialHunger = value;
        }

        public float InitialEnergy
        {
            get => initialEnergy;
            set => initialEnergy = value;
        }

        public float InitialMorale
        {
            get => initialMorale;
            set => initialMorale = value;
        }
    }
}









