#if UNITY_EDITOR || DEVELOPMENT_BUILD
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Dev-only guard that reports missing presentation components once per archetype.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial struct RenderPresentationValidationSystem : ISystem
    {
        private const int ValidationWarmupFrames = 8;
        private const int PersistentMissingFrameThreshold = 120;
        private const int MaxReportsPerCategory = 8;

        private EntityQuery _missingSemanticQuery;
        private EntityQuery _missingPresenterQuery;
        private NativeParallelHashSet<ulong> _reportedSemantic;
        private NativeParallelHashSet<ulong> _reportedPresenter;
        private EntityTypeHandle _entityTypeHandle;
        private int _presentationReadyFrames;
        private int _missingSemanticFrames;
        private int _missingPresenterFrames;
        private int _reportedSemanticCount;
        private int _reportedPresenterCount;

        public void OnCreate(ref SystemState state)
        {
            const EntityQueryOptions queryOptions = EntityQueryOptions.IgnoreComponentEnabledState;

            _missingSemanticQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MeshPresenter>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<RenderSemanticKey>()
                },
                Options = queryOptions
            });

            _missingPresenterQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderSemanticKey>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<MeshPresenter>(),
                    ComponentType.ReadOnly<SpritePresenter>(),
                    ComponentType.ReadOnly<DebugPresenter>(),
                    ComponentType.ReadOnly<TracerPresenter>()
                },
                Options = queryOptions
            });

            _reportedSemantic = new NativeParallelHashSet<ulong>(64, Allocator.Persistent);
            _reportedPresenter = new NativeParallelHashSet<ulong>(64, Allocator.Persistent);
            _entityTypeHandle = state.GetEntityTypeHandle();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_reportedSemantic.IsCreated)
                _reportedSemantic.Dispose();
            if (_reportedPresenter.IsCreated)
                _reportedPresenter.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<PresentationReady>())
            {
                _presentationReadyFrames = 0;
                _missingSemanticFrames = 0;
                _missingPresenterFrames = 0;
                return;
            }

            if (_presentationReadyFrames < ValidationWarmupFrames)
            {
                _presentationReadyFrames++;
                return;
            }

            _entityTypeHandle.Update(ref state);
            if (_missingSemanticQuery.IsEmptyIgnoreFilter)
            {
                _missingSemanticFrames = 0;
            }
            else
            {
                _missingSemanticFrames++;
                if (_missingSemanticFrames >= PersistentMissingFrameThreshold)
                {
                    ReportOnce(ref state, _missingSemanticQuery, ref _reportedSemantic,
                        ref _reportedSemanticCount,
                        "[RenderPresentationValidation] Entity is missing RenderSemanticKey but has a presenter component.");
                }
            }

            if (_missingPresenterQuery.IsEmptyIgnoreFilter)
            {
                _missingPresenterFrames = 0;
            }
            else
            {
                _missingPresenterFrames++;
                if (_missingPresenterFrames >= PersistentMissingFrameThreshold)
                {
                    ReportOnce(ref state, _missingPresenterQuery, ref _reportedPresenter,
                        ref _reportedPresenterCount,
                        "[RenderPresentationValidation] Entity has RenderSemanticKey but no presenter component (Mesh/Sprite/Tracer/Debug).");
                }
            }
        }

        private void ReportOnce(
            ref SystemState state,
            EntityQuery query,
            ref NativeParallelHashSet<ulong> reportedSet,
            ref int reportedCount,
            string message)
        {
            if (query.IsEmptyIgnoreFilter)
                return;

            if (reportedCount >= MaxReportsPerCategory)
                return;

            using var chunks = query.ToArchetypeChunkArray(Allocator.Temp);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var entities = chunk.GetNativeArray(_entityTypeHandle);
                if (entities.Length == 0)
                    continue;

                var key = ComputeKey(entities[0]);
                if (reportedSet.Contains(key))
                    continue;

                reportedSet.Add(key);
                reportedCount++;
                var sample = entities[0];
                var details = string.Empty;
                if (state.EntityManager.HasComponent<RenderSemanticKey>(sample))
                {
                    details += $" SemanticKey={state.EntityManager.GetComponentData<RenderSemanticKey>(sample).Value}";
                }
                if (state.EntityManager.HasComponent<RenderKey>(sample))
                {
                    details += $" RenderKey={state.EntityManager.GetComponentData<RenderKey>(sample).ArchetypeId}";
                }
                var strictValidation = Application.isBatchMode
                    || global::System.Environment.GetEnvironmentVariable("PUREDOTS_STRICT_RENDER_VALIDATION") == "1";
                if (strictValidation)
                {
                    Debug.LogError($"{message} Example entity: {sample}.{details}");
                }
                else
                {
                    Debug.LogWarning($"{message} Example entity: {sample}.{details}");
                }

                if (reportedCount >= MaxReportsPerCategory)
                {
                    Debug.LogWarning("[RenderPresentationValidation] Additional validation errors suppressed for this category.");
                    break;
                }
            }
        }

        private static ulong ComputeKey(Entity entity)
        {
            var index = (ulong)(uint)entity.Index;
            var version = (ulong)(uint)entity.Version;
            return (index << 32) | version;
        }
    }
}
#endif
