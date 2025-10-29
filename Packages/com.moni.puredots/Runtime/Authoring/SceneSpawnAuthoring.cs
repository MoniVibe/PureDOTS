using System;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring
{
    [DisallowMultipleComponent]
    public sealed class SceneSpawnAuthoring : MonoBehaviour
    {
        public SceneSpawnProfileAsset profile;
        [Tooltip("Override the profile seed for this instance. Zero keeps the profile value.")]
        public uint seedOverride;
        [Tooltip("Global seed offset applied in addition to per-entry offsets.")]
        public uint seedOffset;
    }

    public sealed class SceneSpawnAuthoringBaker : Baker<SceneSpawnAuthoring>
    {
        public override void Bake(SceneSpawnAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            if (authoring.profile == null)
            {
                Debug.LogWarning("SceneSpawnAuthoring requires a SceneSpawnProfileAsset reference.", authoring);
                AddComponent(entity, new SceneSpawnController
                {
                    Seed = math.max(1u, authoring.seedOverride == 0 ? 1u : authoring.seedOverride),
                    Flags = 0
                });
                AddBuffer<SceneSpawnRequest>(entity);
                AddBuffer<SceneSpawnPoint>(entity);
                return;
            }

            uint baseSeed = authoring.seedOverride != 0
                ? authoring.seedOverride
                : authoring.profile.seed;
            baseSeed = math.max(1u, baseSeed);

            AddComponent(entity, new SceneSpawnController
            {
                Seed = baseSeed,
                Flags = 0
            });

            var requestBuffer = AddBuffer<SceneSpawnRequest>(entity);
            var pointBuffer = AddBuffer<SceneSpawnPoint>(entity);

            var entries = authoring.profile.entries;
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var definition = entries[i];
                if (definition.prefab == null)
                {
                    Debug.LogWarning($"SceneSpawnProfile '{authoring.profile.name}' entry {i} missing prefab reference.", authoring);
                    continue;
                }

                var prefabEntity = GetEntity(definition.prefab, TransformUsageFlags.Dynamic);
                if (prefabEntity == Entity.Null)
                {
                    Debug.LogWarning($"SceneSpawnProfile '{authoring.profile.name}' entry {i} prefab failed conversion.", authoring);
                    continue;
                }

                int customPointStart = -1;
                int customPointCount = 0;
                if (definition.placement == SpawnPlacementMode.CustomPoints && definition.customPoints != null && definition.customPoints.Count > 0)
                {
                    customPointStart = pointBuffer.Length;
                    customPointCount = definition.customPoints.Count;
                    foreach (var point in definition.customPoints)
                    {
                        pointBuffer.Add(new SceneSpawnPoint
                        {
                            LocalPoint = new float3(point.x, point.y, point.z)
                        });
                    }
                }

                var payload = default(FixedString64Bytes);
                if (!string.IsNullOrWhiteSpace(definition.payloadId))
                {
                    payload = new FixedString64Bytes(definition.payloadId.Trim());
                }

                var gridDimensions = new int2(
                    math.max(1, definition.gridDimensions.x),
                    math.max(1, definition.gridDimensions.y));

                var gridSpacing = new float2(
                    math.max(0.01f, definition.gridSpacing.x),
                    math.max(0.01f, definition.gridSpacing.y));

                var heightRange = new float2(definition.heightRange.x, definition.heightRange.y);
                if (heightRange.x > heightRange.y)
                {
                    heightRange = heightRange.yx;
                }

                uint entrySeedOffset = definition.GetSeedOffset((uint)i) ^ authoring.seedOffset;

                requestBuffer.Add(new SceneSpawnRequest
                {
                    Category = definition.category,
                    Prefab = prefabEntity,
                    Count = math.max(1, definition.count),
                    Placement = definition.placement,
                    Rotation = definition.rotation,
                    Offset = new float3(definition.localOffset.x, definition.localOffset.y, definition.localOffset.z),
                    Radius = math.max(0f, definition.radius),
                    InnerRadius = math.clamp(definition.innerRadius, 0f, math.max(0f, definition.radius)),
                    GridDimensions = gridDimensions,
                    GridSpacing = gridSpacing,
                    HeightRange = heightRange,
                    FixedYawDegrees = definition.fixedYawDegrees,
                    PayloadId = payload,
                    PayloadValue = definition.payloadValue,
                    SeedOffset = entrySeedOffset,
                    CustomPointStart = customPointStart,
                    CustomPointCount = customPointCount
                });
            }
        }
    }
}
