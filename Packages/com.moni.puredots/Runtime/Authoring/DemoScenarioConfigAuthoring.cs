using Unity.Entities;
using UnityEngine;
using PureDOTS.Runtime;

namespace PureDOTS.Authoring
{
    [DisallowMultipleComponent]
    public sealed class DemoScenarioConfigAuthoring : MonoBehaviour
    {
        [Tooltip("Demo scenario definition asset.")]
        public DemoScenarioDef scenarioDef;
    }

    public sealed class DemoScenarioConfigBaker : Baker<DemoScenarioConfigAuthoring>
    {
        public override void Bake(DemoScenarioConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            if (authoring.scenarioDef == null)
            {
                Debug.LogWarning("[DemoScenarioConfigBaker] No scenarioDef assigned. Using defaults.", authoring);
                AddComponent(entity, new DemoScenarioConfig
                {
                    EnableGodgame = true,
                    EnableSpace4x = true,
                    GodgameSeed = 12345u,
                    Space4xSeed = 67890u,
                    VillageCount = 1,
                    VillagersPerVillage = 3,
                    CarrierCount = 1,
                    AsteroidCount = 2,
                    StartingBandCount = 0,
                    Difficulty = 0.5f,
                    Density = 0.5f
                });
                return;
            }

            var def = authoring.scenarioDef;
            AddComponent(entity, new DemoScenarioConfig
            {
                EnableGodgame = def.EnableGodgame,
                EnableSpace4x = def.EnableSpace4x,
                GodgameSeed = def.GodgameSeed > 0 ? def.GodgameSeed : 12345u,
                Space4xSeed = def.Space4xSeed > 0 ? def.Space4xSeed : 67890u,
                VillageCount = Mathf.Max(0, def.VillageCount),
                VillagersPerVillage = Mathf.Max(1, def.VillagersPerVillage),
                CarrierCount = Mathf.Max(0, def.CarrierCount),
                AsteroidCount = Mathf.Max(0, def.AsteroidCount),
                StartingBandCount = Mathf.Max(0, def.StartingBandCount),
                Difficulty = Mathf.Clamp01(def.Difficulty),
                Density = Mathf.Clamp01(def.Density)
            });
        }
    }
}

