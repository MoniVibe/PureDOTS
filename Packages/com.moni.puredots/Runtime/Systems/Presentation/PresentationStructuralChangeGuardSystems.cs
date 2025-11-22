#if UNITY_EDITOR
using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Captures the structural change baseline after BeginPresentationEntityCommandBufferSystem playback.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginPresentationECBSystem))]
    public partial struct PresentationStructuralChangeGuardBeginSystem : ISystem
    {
        private EntityQuery _sentinelQuery;

        public void OnCreate(ref SystemState state)
        {
            _sentinelQuery = state.GetEntityQuery(ComponentType.ReadOnly<PresentationStructuralChangeSentinel>());
            EnsureSentinel(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            EnsureSentinel(ref state);
            var entity = _sentinelQuery.GetSingletonEntity();
            var sentinel = state.EntityManager.GetComponentData<PresentationStructuralChangeSentinel>(entity);
            sentinel.LastKnownOrderVersion = state.EntityManager.EntityOrderVersion;
            state.EntityManager.SetComponentData(entity, sentinel);
        }

        private static void EnsureSentinel(ref SystemState state)
        {
            var query = state.GetEntityQuery(ComponentType.ReadOnly<PresentationStructuralChangeSentinel>());
            if (!query.IsEmptyIgnoreFilter)
            {
                return;
            }

            {
                var entity = state.EntityManager.CreateEntity(typeof(PresentationStructuralChangeSentinel));
                state.EntityManager.SetComponentData(entity, new PresentationStructuralChangeSentinel
                {
                    LastKnownOrderVersion = state.EntityManager.EntityOrderVersion
                });
            }
        }
    }

    /// <summary>
    /// Throws if any presentation system performs structural changes outside ECB boundaries.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(EndPresentationECBSystem))]
    public partial struct PresentationStructuralChangeGuardEndSystem : ISystem
    {
        private EntityQuery _sentinelQuery;

        public void OnCreate(ref SystemState state)
        {
            _sentinelQuery = state.GetEntityQuery(ComponentType.ReadOnly<PresentationStructuralChangeSentinel>());
            state.RequireForUpdate(_sentinelQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var sentinelEntity = _sentinelQuery.GetSingletonEntity();
            var sentinel = state.EntityManager.GetComponentData<PresentationStructuralChangeSentinel>(sentinelEntity);
            var currentVersion = state.EntityManager.EntityOrderVersion;
            if (currentVersion != sentinel.LastKnownOrderVersion)
            {
                throw new System.InvalidOperationException(
                    "PresentationSystemGroup performed structural changes. Use Begin/EndPresentationEntityCommandBufferSystem or defer to Simulation ECB boundaries.");
            }
        }
    }
}
#endif
