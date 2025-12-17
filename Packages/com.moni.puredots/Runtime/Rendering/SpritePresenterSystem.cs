using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Applies sprite/impostor presenter data so variants with RenderPresenterMask.Sprite can render.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(ResolveRenderVariantSystem))]
    [UpdateBefore(typeof(Unity.Rendering.EntitiesGraphicsSystem))]
    public partial struct SpritePresenterSystem : ISystem
    {
        private EntityQuery _applyAllQuery;
        private EntityQuery _applyChangedQuery;
        private uint _lastCatalogVersion;
        private EntityQuery _missingMaterialMeshQuery;
        private EntityQuery _missingRenderBoundsQuery;
        private EntityQuery _missingWorldBoundsQuery;
        private EntityQuery _missingChunkBoundsQuery;

        public void OnCreate(ref SystemState state)
        {
            _applyAllQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<SpritePresenter>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadWrite<MaterialMeshInfo>(),
                    ComponentType.ReadWrite<RenderBounds>(),
                    ComponentType.ReadWrite<WorldRenderBounds>(),
                    ComponentType.ReadWrite<ChunkWorldRenderBounds>()
                }
            });

            _applyChangedQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<SpritePresenter>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadWrite<MaterialMeshInfo>(),
                    ComponentType.ReadWrite<RenderBounds>(),
                    ComponentType.ReadWrite<WorldRenderBounds>(),
                    ComponentType.ReadWrite<ChunkWorldRenderBounds>()
                }
            });
            _applyChangedQuery.AddChangedVersionFilter(ComponentType.ReadOnly<SpritePresenter>());

            _missingMaterialMeshQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<SpritePresenter>()
                },
                None = new[] { ComponentType.ReadOnly<MaterialMeshInfo>() }
            });

            _missingRenderBoundsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<SpritePresenter>()
                },
                None = new[] { ComponentType.ReadOnly<RenderBounds>() }
            });

            _missingWorldBoundsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<SpritePresenter>()
                },
                None = new[] { ComponentType.ReadOnly<WorldRenderBounds>() }
            });

            _missingChunkBoundsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<SpritePresenter>()
                },
                None = new[] { ComponentType.ReadOnly<ChunkWorldRenderBounds>() }
            });

            state.RequireForUpdate<RenderPresentationCatalog>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out RenderPresentationCatalog catalog) || !catalog.Blob.IsCreated)
                return;

            if (!SystemAPI.TryGetSingleton<RenderCatalogVersion>(out var catalogVersion))
            {
                return;
            }

            EnsureCoreComponents(ref state);

            var catalogChanged = catalogVersion.Value != _lastCatalogVersion;
            var query = catalogChanged ? _applyAllQuery : _applyChangedQuery;
            if (query.IsEmptyIgnoreFilter)
            {
                if (catalogChanged)
                {
                    _lastCatalogVersion = catalogVersion.Value;
                }
                return;
            }

            var renderMeshArray = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(catalog.RenderMeshArrayEntity);
            var meshCount = renderMeshArray.MeshReferences?.Length ?? 0;
            var materialCount = renderMeshArray.MaterialReferences?.Length ?? 0;
            if (meshCount == 0 || materialCount == 0)
            {
                return;
            }

            var job = new ApplySpritePresenterJob
            {
                Catalog = catalog.Blob,
                MeshCount = meshCount,
                MaterialCount = materialCount,
                SpritePresenterHandle = state.GetComponentTypeHandle<SpritePresenter>(true),
                TransformHandle = state.GetComponentTypeHandle<LocalTransform>(true),
                MaterialMeshHandle = state.GetComponentTypeHandle<MaterialMeshInfo>(),
                RenderBoundsHandle = state.GetComponentTypeHandle<RenderBounds>(),
                WorldBoundsHandle = state.GetComponentTypeHandle<WorldRenderBounds>(),
                ChunkBoundsHandle = state.GetComponentTypeHandle<ChunkWorldRenderBounds>()
            };

            state.Dependency = job.ScheduleParallel(query, state.Dependency);
            _lastCatalogVersion = catalogVersion.Value;
        }

        [BurstCompile]
        private struct ApplySpritePresenterJob : IJobChunk
        {
            [ReadOnly] public BlobAssetReference<RenderPresentationCatalogBlob> Catalog;
            public int MeshCount;
            public int MaterialCount;

            [ReadOnly] public ComponentTypeHandle<SpritePresenter> SpritePresenterHandle;
            [ReadOnly] public ComponentTypeHandle<LocalTransform> TransformHandle;
            public ComponentTypeHandle<MaterialMeshInfo> MaterialMeshHandle;
            public ComponentTypeHandle<RenderBounds> RenderBoundsHandle;
            public ComponentTypeHandle<WorldRenderBounds> WorldBoundsHandle;
            public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkBoundsHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var spritePresenters = chunk.GetNativeArray(ref SpritePresenterHandle);
                var transforms = chunk.GetNativeArray(ref TransformHandle);
                var materialMeshes = chunk.GetNativeArray(ref MaterialMeshHandle);
                var renderBounds = chunk.GetNativeArray(ref RenderBoundsHandle);
                var worldBounds = chunk.GetNativeArray(ref WorldBoundsHandle);
                var chunkBounds = chunk.GetNativeArray(ref ChunkBoundsHandle);

                ref var catalog = ref Catalog.Value;
                if (catalog.Variants.Length == 0)
                    return;

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var presenterIndex = spritePresenters[i].DefIndex;
                    if (presenterIndex == RenderPresentationConstants.UnassignedPresenterDefIndex)
                        continue;

                    var defIndex = math.clamp((int)presenterIndex, 0, catalog.Variants.Length - 1);
                    ref var variant = ref catalog.Variants[defIndex];
                    if ((variant.PresenterMask & RenderPresenterMask.Sprite) == 0)
                        continue;

                    var matIndex = math.clamp((int)variant.MaterialIndex, 0, math.max(MaterialCount - 1, 0));
                    var meshIndex = math.clamp((int)variant.MeshIndex, 0, math.max(MeshCount - 1, 0));

                    materialMeshes[i] = MaterialMeshInfo.FromRenderMeshArrayIndices((ushort)matIndex, (ushort)meshIndex, variant.SubMesh);

                    var transform = transforms[i];
                    var scaledCenter = variant.BoundsCenter * transform.Scale;
                    var worldCenter = transform.Position + math.rotate(transform.Rotation, scaledCenter);
                    var scaledExtents = variant.BoundsExtents * transform.Scale;

                    var localBounds = new AABB
                    {
                        Center = variant.BoundsCenter,
                        Extents = variant.BoundsExtents
                    };

                    var worldAabb = new AABB
                    {
                        Center = worldCenter,
                        Extents = scaledExtents
                    };

                    renderBounds[i] = new RenderBounds { Value = localBounds };
                    worldBounds[i] = new WorldRenderBounds { Value = worldAabb };
                    chunkBounds[i] = new ChunkWorldRenderBounds { Value = worldAabb };
                }
            }
        }

        private void EnsureCoreComponents(ref SystemState state)
        {
            EnsureComponentIfMissing(ref state, _missingMaterialMeshQuery,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 0));
            EnsureComponentIfMissing(ref state, _missingRenderBoundsQuery, new RenderBounds
            {
                Value = new AABB { Center = float3.zero, Extents = new float3(0.5f) }
            });
            EnsureComponentIfMissing(ref state, _missingWorldBoundsQuery, new WorldRenderBounds
            {
                Value = new AABB { Center = float3.zero, Extents = new float3(0.5f) }
            });
            EnsureComponentIfMissing(ref state, _missingChunkBoundsQuery, new ChunkWorldRenderBounds
            {
                Value = new AABB { Center = float3.zero, Extents = new float3(0.5f) }
            });
        }

        private static void EnsureComponentIfMissing<TComponent>(ref SystemState state, EntityQuery query, in TComponent value)
            where TComponent : unmanaged, IComponentData
        {
            if (query.IsEmptyIgnoreFilter)
                return;

            var entityManager = state.EntityManager;
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<TComponent>(entity))
                {
                    entityManager.SetComponentData(entity, value);
                }
                else
                {
                    entityManager.AddComponentData(entity, value);
                }
            }
        }
    }
}
