using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Resolves semantic keys into concrete variant keys using the active theme.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ResolveRenderVariantSystem : SystemBase
    {
        private EntityQuery _resolveQuery;
        private EntityQuery _missingVariantQuery;
        private EntityQuery _missingThemeOverrideQuery;
        private EntityQuery _semanticChangeQuery;
        private EntityQuery _themeOverrideChangeQuery;
        private EntityQuery _variantOverrideChangeQuery;
        private EntityQuery _renderKeyChangeQuery;
        private ushort _lastThemeId;
        private uint _lastCatalogVersion;

        protected override void OnCreate()
        {
            _resolveQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderSemanticKey>(),
                    ComponentType.ReadWrite<RenderVariantKey>(),
                    ComponentType.ReadOnly<RenderThemeOverride>()
                }
            });

            _missingVariantQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RenderSemanticKey>() },
                None = new[] { ComponentType.ReadOnly<RenderVariantKey>() }
            });

            _semanticChangeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderSemanticKey>(),
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<RenderThemeOverride>()
                }
            });
            _semanticChangeQuery.AddChangedVersionFilter(ComponentType.ReadOnly<RenderSemanticKey>());

            _themeOverrideChangeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderSemanticKey>(),
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<RenderThemeOverride>()
                }
            });
            _themeOverrideChangeQuery.AddChangedVersionFilter(ComponentType.ReadOnly<RenderThemeOverride>());

            _variantOverrideChangeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderSemanticKey>(),
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<RenderVariantOverride>()
                }
            });
            _variantOverrideChangeQuery.AddChangedVersionFilter(ComponentType.ReadOnly<RenderVariantOverride>());

            _renderKeyChangeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderSemanticKey>(),
                    ComponentType.ReadOnly<RenderVariantKey>(),
                    ComponentType.ReadOnly<RenderKey>()
                }
            });
            _renderKeyChangeQuery.AddChangedVersionFilter(ComponentType.ReadOnly<RenderKey>());

            RequireForUpdate<RenderPresentationCatalog>();
            RequireForUpdate<ActiveRenderTheme>();
        }

        protected override void OnUpdate()
        {
            var catalog = SystemAPI.GetSingleton<RenderPresentationCatalog>();
            if (!catalog.Blob.IsCreated)
                return;

            if (!_missingVariantQuery.IsEmptyIgnoreFilter)
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                foreach (var (semanticKey, entity) in SystemAPI.Query<RefRO<RenderSemanticKey>>().WithNone<RenderVariantKey>().WithEntityAccess())
                {
                    ecb.AddComponent(entity, new RenderVariantKey { Value = 0 });
                }
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }

            var theme = SystemAPI.GetSingleton<ActiveRenderTheme>();
            var catalogVersion = SystemAPI.TryGetSingleton<RenderCatalogVersion>(out var version)
                ? version.Value
                : 0u;

            var themeChanged = theme.ThemeId != _lastThemeId;
            var catalogChanged = catalogVersion != _lastCatalogVersion;
            var semanticChanged = !_semanticChangeQuery.IsEmptyIgnoreFilter;
            var themeOverrideChanged = !_themeOverrideChangeQuery.IsEmptyIgnoreFilter;
            var variantOverrideChanged = !_variantOverrideChangeQuery.IsEmptyIgnoreFilter;
            var lodChanged = !_renderKeyChangeQuery.IsEmptyIgnoreFilter;

            if (!(themeChanged || catalogChanged || semanticChanged || themeOverrideChanged || variantOverrideChanged || lodChanged))
                return;

            var renderKeyLookup = GetComponentLookup<RenderKey>(true);
            var variantOverrideLookup = GetComponentLookup<RenderVariantOverride>(true);

            var job = new ResolveRenderVariantJob
            {
                Catalog = catalog.Blob,
                RenderKeyLookup = renderKeyLookup,
                RenderVariantOverrideLookup = variantOverrideLookup,
                ActiveThemeId = theme.ThemeId
            };

            Dependency = job.ScheduleParallel(_resolveQuery, Dependency);

            _lastThemeId = theme.ThemeId;
            _lastCatalogVersion = catalogVersion;
        }

        [BurstCompile]
        private partial struct ResolveRenderVariantJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<RenderPresentationCatalogBlob> Catalog;
            [ReadOnly] public ComponentLookup<RenderKey> RenderKeyLookup;
            [ReadOnly] public ComponentLookup<RenderVariantOverride> RenderVariantOverrideLookup;
            public ushort ActiveThemeId;

            private int ResolveThemeIndex(ushort themeId)
            {
                ref var value = ref Catalog.Value;
                if (themeId < value.ThemeIndexLookup.Length)
                {
                    var candidate = value.ThemeIndexLookup[themeId];
                    if (candidate >= 0 && candidate < value.Themes.Length)
                        return candidate;
                }

                var fallback = math.min((int)value.DefaultThemeIndex, math.max(value.Themes.Length - 1, 0));
                return fallback;
            }

            public void Execute(
                Entity entity,
                RefRW<RenderVariantKey> variantKey,
                RefRO<RenderSemanticKey> semanticKey,
                RefRO<RenderThemeOverride> themeOverride,
                EnabledRefRO<RenderThemeOverride> themeOverrideEnabled)
            {
                if (TryApplyOverride(entity, variantKey))
                {
                    return;
                }

                ref var catalog = ref Catalog.Value;
                var themeOverrideValue = themeOverride.ValueRO.Value;
                var themeId = themeOverrideEnabled.ValueRO ? themeOverrideValue : ActiveThemeId;

                var themeIndex = ResolveThemeIndex(themeId);
                ref var themeRow = ref catalog.Themes[themeIndex];
                var semantic = math.clamp(semanticKey.ValueRO.Value, 0, catalog.SemanticKeyCount - 1);
                var lod = ResolveLod(entity);
                var lodCount = math.max(1, catalog.LodCount);
                var flatIndex = math.clamp(lod, 0, lodCount - 1) * catalog.SemanticKeyCount + semantic;
                ref var variantIndices = ref themeRow.VariantIndices;
                flatIndex = math.clamp(flatIndex, 0, variantIndices.Length - 1);
                var resolvedVariant = variantIndices[flatIndex];
                resolvedVariant = math.clamp(resolvedVariant, 0, catalog.Variants.Length - 1);

                if (variantKey.ValueRO.Value != resolvedVariant)
                {
                    variantKey.ValueRW.Value = resolvedVariant;
                }
            }

            private int ResolveLod(Entity entity)
            {
                if (RenderKeyLookup.HasComponent(entity))
                {
                    return RenderKeyLookup[entity].LOD;
                }
                return 0;
            }

            private bool TryApplyOverride(Entity entity, RefRW<RenderVariantKey> variantKey)
            {
                if (!RenderVariantOverrideLookup.HasComponent(entity) || !RenderVariantOverrideLookup.IsComponentEnabled(entity))
                {
                    return false;
                }

                var overrideValue = RenderVariantOverrideLookup[entity].Value;
                if (variantKey.ValueRO.Value != overrideValue)
                {
                    variantKey.ValueRW.Value = overrideValue;
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Maps resolved variant keys onto enableable presenter components without structural changes.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(ResolveRenderVariantSystem))]
    public partial class RenderVariantResolveSystem : SystemBase
    {
        private EntityQuery _missingResolvedQuery;
        private EntityQuery _missingSpritePresenterQuery;
        private EntityQuery _missingMeshPresenterQuery;
        private EntityQuery _missingDebugPresenterQuery;
        private EntityQuery _missingThemeOverrideQuery;
        private uint _lastCatalogVersion;

        protected override void OnCreate()
        {
            RequireForUpdate<RenderPresentationCatalog>();
            var keyQuery = GetEntityQuery(ComponentType.ReadOnly<RenderVariantKey>());
            RequireForUpdate(keyQuery);

            _missingResolvedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RenderVariantKey>() },
                None = new[] { ComponentType.ReadOnly<RenderVariantResolved>() }
            });

            _missingSpritePresenterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RenderVariantKey>() },
                None = new[] { ComponentType.ReadOnly<SpritePresenter>() }
            });

            _missingMeshPresenterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RenderVariantKey>() },
                None = new[] { ComponentType.ReadOnly<MeshPresenter>() }
            });

            _missingDebugPresenterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RenderVariantKey>() },
                None = new[] { ComponentType.ReadOnly<DebugPresenter>() }
            });

            _missingThemeOverrideQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RenderVariantKey>() },
                None = new[] { ComponentType.ReadOnly<RenderThemeOverride>() }
            });
        }

        protected override void OnUpdate()
        {
            var catalog = SystemAPI.GetSingleton<RenderPresentationCatalog>();
            if (!catalog.Blob.IsCreated)
                return;

            var hasCatalogVersion = SystemAPI.TryGetSingleton<RenderCatalogVersion>(out var catalogVersion);
            var catalogChanged = hasCatalogVersion && catalogVersion.Value != _lastCatalogVersion;

            EnsureComponentDataImmediate(_missingResolvedQuery, new RenderVariantResolved
            {
                LastKey = new RenderVariantKey(-1),
                LastKind = RenderPresenterKind.None,
                LastDefIndex = -1
            });
            EnsureEnableableComponentImmediate(_missingThemeOverrideQuery, new RenderThemeOverride { Value = 0 });
            EnsureEnableableComponentImmediate(_missingSpritePresenterQuery, new SpritePresenter { DefIndex = RenderPresentationConstants.UnassignedPresenterDefIndex });
            EnsureEnableableComponentImmediate(_missingMeshPresenterQuery, new MeshPresenter { DefIndex = RenderPresentationConstants.UnassignedPresenterDefIndex });
            EnsureEnableableComponentImmediate(_missingDebugPresenterQuery, new DebugPresenter { DefIndex = RenderPresentationConstants.UnassignedPresenterDefIndex });

            var ecb = new EntityCommandBuffer(WorldUpdateAllocator);
            var catalogBlob = catalog.Blob;

            if (catalogChanged)
            {
                foreach (var (key, resolved, sprite, mesh, debugPresenter, entity) in SystemAPI
                             .Query<RefRO<RenderVariantKey>, RefRW<RenderVariantResolved>, RefRW<SpritePresenter>, RefRW<MeshPresenter>, RefRW<DebugPresenter>>()
                             .WithEntityAccess())
                {
                    ResolveVariantForEntity(entity, key, resolved, sprite, mesh, debugPresenter, catalogBlob, ecb, true);
                }
            }
            else
            {
                foreach (var (key, resolved, sprite, mesh, debugPresenter, entity) in SystemAPI
                             .Query<RefRO<RenderVariantKey>, RefRW<RenderVariantResolved>, RefRW<SpritePresenter>, RefRW<MeshPresenter>, RefRW<DebugPresenter>>()
                             .WithEntityAccess()
                             .WithChangeFilter<RenderVariantKey>())
                {
                    ResolveVariantForEntity(entity, key, resolved, sprite, mesh, debugPresenter, catalogBlob, ecb, false);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();

            if (hasCatalogVersion)
            {
                _lastCatalogVersion = catalogVersion.Value;
            }
        }

        private void ResolveVariantForEntity(
            Entity entity,
            RefRO<RenderVariantKey> key,
            RefRW<RenderVariantResolved> resolved,
            RefRW<SpritePresenter> sprite,
            RefRW<MeshPresenter> mesh,
            RefRW<DebugPresenter> debugPresenter,
            BlobAssetReference<RenderPresentationCatalogBlob> catalogBlob,
            EntityCommandBuffer ecb,
            bool forceRefresh)
        {
            var currentKey = key.ValueRO;
            var cached = resolved.ValueRO;
            if (!forceRefresh && cached.LastKey.Equals(currentKey))
                return;

            var spriteTarget = (ushort)0;
            var meshTarget = (ushort)0;
            var debugTarget = (ushort)0;

            if (TryResolve(catalogBlob, currentKey, out var record))
            {
                spriteTarget = ResolvePresenterDefIndex(record, RenderPresenterMask.Sprite);
                meshTarget = ResolvePresenterDefIndex(record, RenderPresenterMask.Mesh);
                debugTarget = ResolvePresenterDefIndex(record, RenderPresenterMask.Debug);

                resolved.ValueRW.LastKind = ResolvePrimaryKind(record.Mask);
                resolved.ValueRW.LastDefIndex = record.DefIndex;
            }
            else
            {
                resolved.ValueRW.LastKind = RenderPresenterKind.None;
                resolved.ValueRW.LastDefIndex = -1;
            }

            ApplyPresenterDefIndex(entity, sprite.ValueRO.DefIndex, spriteTarget, RenderPresenterKind.Sprite, ecb);
            ApplyPresenterDefIndex(entity, mesh.ValueRO.DefIndex, meshTarget, RenderPresenterKind.Mesh, ecb);
            ApplyPresenterDefIndex(entity, debugPresenter.ValueRO.DefIndex, debugTarget, RenderPresenterKind.Debug, ecb);

            resolved.ValueRW.LastKey = currentKey;
        }

        private static void ApplyPresenterDefIndex(
            Entity entity,
            ushort currentValue,
            ushort targetValue,
            RenderPresenterKind presenterKind,
            EntityCommandBuffer ecb)
        {
            if (currentValue == targetValue)
                return;

            switch (presenterKind)
            {
                case RenderPresenterKind.Sprite:
                    ecb.SetComponent(entity, new SpritePresenter { DefIndex = targetValue });
                    break;
                case RenderPresenterKind.Mesh:
                    ecb.SetComponent(entity, new MeshPresenter { DefIndex = targetValue });
                    break;
                case RenderPresenterKind.Debug:
                    ecb.SetComponent(entity, new DebugPresenter { DefIndex = targetValue });
                    break;
            }
        }

        private static ushort ResolvePresenterDefIndex(in RenderResolveRecord record, RenderPresenterMask targetMask)
        {
            return (record.Mask & targetMask) != 0
                ? PackPresenterIndex(record.DefIndex)
                : (ushort)0;
        }

        private static RenderPresenterKind ResolvePrimaryKind(RenderPresenterMask mask)
        {
            if ((mask & RenderPresenterMask.Mesh) != 0)
                return RenderPresenterKind.Mesh;
            if ((mask & RenderPresenterMask.Sprite) != 0)
                return RenderPresenterKind.Sprite;
            if ((mask & RenderPresenterMask.Debug) != 0)
                return RenderPresenterKind.Debug;
            return RenderPresenterKind.None;
        }

        private void EnsureComponentDataImmediate<T>(EntityQuery query, in T value)
            where T : unmanaged, IComponentData
        {
            if (query.IsEmptyIgnoreFilter)
                return;

            using var entities = query.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                if (EntityManager.HasComponent<T>(entity))
                {
                    EntityManager.SetComponentData(entity, value);
                }
                else
                {
                    EntityManager.AddComponentData(entity, value);
                }
            }
        }

        private void EnsureEnableableComponentImmediate<T>(EntityQuery query, in T value)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            if (query.IsEmptyIgnoreFilter)
                return;

            using var entities = query.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                if (EntityManager.HasComponent<T>(entity))
                {
                    EntityManager.SetComponentData(entity, value);
                }
                else
                {
                    EntityManager.AddComponentData(entity, value);
                }
                EntityManager.SetComponentEnabled<T>(entity, false);
            }
        }

        private static ushort PackPresenterIndex(int defIndex)
        {
            const int maxIndex = RenderPresentationConstants.UnassignedPresenterDefIndex - 1;
            return (ushort)math.clamp(defIndex, 0, maxIndex);
        }

        private static bool TryResolve(BlobAssetReference<RenderPresentationCatalogBlob> catalogRef, RenderVariantKey key, out RenderResolveRecord record)
        {
            record = default;
            if (!catalogRef.IsCreated)
                return false;

            ref var catalog = ref catalogRef.Value;
            if (catalog.Variants.Length == 0)
                return false;

            var resolvedIndex = math.clamp(key.Value, 0, catalog.Variants.Length - 1);
            var variant = catalog.Variants[resolvedIndex];
            if (variant.PresenterMask == RenderPresenterMask.None)
                return false;

            record = new RenderResolveRecord
            {
                Mask = variant.PresenterMask,
                DefIndex = resolvedIndex
            };
            return true;
        }

        private struct RenderResolveRecord
        {
            public RenderPresenterMask Mask;
            public int DefIndex;
        }
    }
}
