#if UNITY_EDITOR
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring
{
    [DisallowMultipleComponent]
    public sealed class RainMiracleAuthoring : MonoBehaviour
    {
        public GameObject rainCloudPrefab;
        [Min(1)] public int cloudCount = 3;
        [Min(0f)] public float spawnRadius = 12f;
        [Min(0f)] public float spawnHeightOffset = 18f;
        [Range(0f, 360f)] public float spawnSpreadAngle = 90f;
        public uint seed = 1;
    }

    public sealed class RainMiracleBaker : Baker<RainMiracleAuthoring>
    {
        public override void Bake(RainMiracleAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);

            Entity cloudPrefab = Entity.Null;
            if (authoring.rainCloudPrefab != null)
            {
                cloudPrefab = GetEntity(authoring.rainCloudPrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);
            }

            if (cloudPrefab == Entity.Null)
            {
                Debug.LogWarning("RainMiracleAuthoring requires a rain cloud prefab to spawn.", authoring);
            }

            AddComponent(entity, new RainMiracleConfig
            {
                RainCloudPrefab = cloudPrefab,
                CloudCount = authoring.cloudCount,
                SpawnRadius = authoring.spawnRadius,
                SpawnHeightOffset = authoring.spawnHeightOffset,
                SpawnSpreadAngle = authoring.spawnSpreadAngle,
                Seed = math.max(authoring.seed, 1u)
            });
        }
    }
}
#endif
