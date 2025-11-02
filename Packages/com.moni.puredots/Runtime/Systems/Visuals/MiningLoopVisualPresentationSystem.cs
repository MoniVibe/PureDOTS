using PureDOTS.Runtime.Config;
using PureDOTS.Runtime.Visuals;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Systems.Visuals
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MiningLoopVisualPresentationSystem : SystemBase
    {
        private EntityQuery _prefabQuery;
        private EntityQuery _visualQuery;
        private RuntimeConfigVar _visualToggleVar;
        private bool _visualToggleAssigned;
        private bool _loggedMissingPrefabs;

        protected override void OnCreate()
        {
            RequireForUpdate<MiningVisualManifest>();
            _prefabQuery = GetEntityQuery(ComponentType.ReadOnly<MiningVisualPrefab>());
            _visualQuery = GetEntityQuery(ComponentType.ReadOnly<MiningVisual>(), ComponentType.ReadWrite<LocalTransform>());
            RuntimeConfigRegistry.Initialize();
            _visualToggleAssigned = RuntimeConfigRegistry.TryGetVar("visuals.mining.enabled", out _visualToggleVar!);
        }

        protected override void OnUpdate()
        {
            if (_visualToggleAssigned && !_visualToggleVar.BoolValue)
            {
                CleanupAllVisuals();
                return;
            }

            if (_prefabQuery.IsEmpty)
            {
                if (!_loggedMissingPrefabs)
                {
                    Debug.LogWarning("[MiningLoopVisualPresentationSystem] No MiningVisualPrefab found. Skipping visual presentation.");
                    _loggedMissingPrefabs = true;
                }

                CleanupAllVisuals();
                return;
            }

            _loggedMissingPrefabs = false;

            var manifestEntity = SystemAPI.GetSingletonEntity<MiningVisualManifest>();
            var requestBuffer = EntityManager.GetBuffer<MiningVisualRequest>(manifestEntity);
            var requests = requestBuffer.ToNativeArray(Allocator.Temp);

            var prefabMap = GatherPrefabs();
            var entityManager = EntityManager;

            var existingMap = new NativeParallelHashMap<Entity, Entity>(_visualQuery.CalculateEntityCount(), Allocator.Temp);
            var duplicateVisuals = new NativeList<Entity>(Allocator.Temp);

            foreach (var (visual, entity) in SystemAPI.Query<MiningVisual>().WithEntityAccess())
            {
                if (visual.SourceEntity == Entity.Null)
                {
                    duplicateVisuals.Add(entity);
                    continue;
                }

                if (!existingMap.TryAdd(visual.SourceEntity, entity))
                {
                    duplicateVisuals.Add(entity);
                }
            }

            for (var i = 0; i < duplicateVisuals.Length; i++)
            {
                var duplicate = duplicateVisuals[i];
                if (duplicate != Entity.Null && entityManager.Exists(duplicate))
                {
                    entityManager.DestroyEntity(duplicate);
                }
            }

            var usedSources = new NativeParallelHashSet<Entity>(requests.Length, Allocator.Temp);

            for (var i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                if (request.SourceEntity == Entity.Null)
                {
                    continue;
                }

                if (!prefabMap.TryGetValue((byte)request.VisualType, out var prefabInfo))
                {
                    continue;
                }

                if (prefabInfo.PrefabEntity == Entity.Null)
                {
                    continue;
                }

                var targetScale = prefabInfo.BaseScale * math.max(0.1f, request.BaseScale);

                if (existingMap.TryGetValue(request.SourceEntity, out var visualEntity) && entityManager.Exists(visualEntity))
                {
                    usedSources.Add(request.SourceEntity);

                    if (!entityManager.HasComponent<LocalTransform>(visualEntity))
                    {
                        entityManager.AddComponentData(visualEntity, LocalTransform.FromPositionRotationScale(request.Position, quaternion.identity, targetScale));
                    }
                    else
                    {
                        entityManager.SetComponentData(visualEntity, LocalTransform.FromPositionRotationScale(request.Position, quaternion.identity, targetScale));
                    }
                }
                else
                {
                    usedSources.Add(request.SourceEntity);
                    var instance = entityManager.Instantiate(prefabInfo.PrefabEntity);

                    if (!entityManager.HasComponent<LocalTransform>(instance))
                    {
                        entityManager.AddComponentData(instance, LocalTransform.FromPositionRotationScale(request.Position, quaternion.identity, targetScale));
                    }
                    else
                    {
                        entityManager.SetComponentData(instance, LocalTransform.FromPositionRotationScale(request.Position, quaternion.identity, targetScale));
                    }

                    if (entityManager.HasComponent<MiningVisual>(instance))
                    {
                        entityManager.SetComponentData(instance, new MiningVisual
                        {
                            VisualType = request.VisualType,
                            SourceEntity = request.SourceEntity
                        });
                    }
                    else
                    {
                        entityManager.AddComponentData(instance, new MiningVisual
                        {
                            VisualType = request.VisualType,
                            SourceEntity = request.SourceEntity
                        });
                    }

                    existingMap[request.SourceEntity] = instance;
                }
            }

            foreach (var kvp in existingMap)
            {
                if (kvp.Key == Entity.Null)
                {
                    continue;
                }

                if (!usedSources.Contains(kvp.Key) && entityManager.Exists(kvp.Value))
                {
                    entityManager.DestroyEntity(kvp.Value);
                }
            }

            usedSources.Dispose();
            existingMap.Dispose();
            prefabMap.Dispose();
            duplicateVisuals.Dispose();
            requests.Dispose();
        }

        private void CleanupAllVisuals()
        {
            using var visuals = _visualQuery.ToEntityArray(Allocator.Temp);
            if (visuals.Length == 0)
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            for (var i = 0; i < visuals.Length; i++)
            {
                ecb.DestroyEntity(visuals[i]);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private NativeParallelHashMap<byte, PrefabInfo> GatherPrefabs()
        {
            var map = new NativeParallelHashMap<byte, PrefabInfo>(_prefabQuery.CalculateEntityCount(), Allocator.Temp);

            foreach (var (prefab, entity) in SystemAPI.Query<MiningVisualPrefab>().WithEntityAccess())
            {
                map[(byte)prefab.VisualType] = new PrefabInfo
                {
                    PrefabEntity = prefab.Prefab != Entity.Null ? prefab.Prefab : entity,
                    BaseScale = prefab.BaseScale > 0f ? prefab.BaseScale : 1f
                };
            }

            return map;
        }

        private struct PrefabInfo
        {
            public Entity PrefabEntity;
            public float BaseScale;
        }
    }
}

