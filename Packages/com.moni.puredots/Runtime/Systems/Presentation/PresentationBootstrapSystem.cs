using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct PresentationBootstrapSystem : ISystem
    {
        private EntityQuery _queueQuery;
        private EntityQuery _syncConfigQuery;
        private EntityQuery _poolStatsQuery;

        public void OnCreate(ref SystemState state)
        {
            _queueQuery = state.GetEntityQuery(ComponentType.ReadOnly<PresentationCommandQueue>());
            _syncConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<PresentationHandleSyncConfig>());
            _poolStatsQuery = state.GetEntityQuery(ComponentType.ReadOnly<PresentationPoolStats>());

            EnsureCommandQueue(ref state);
            EnsureSyncConfig(ref state);
            EnsurePoolStats(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        private void EnsureCommandQueue(ref SystemState state)
        {
            if (!_queueQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new PresentationCommandQueue());
            state.EntityManager.AddBuffer<PresentationSpawnRequest>(entity);
            state.EntityManager.AddBuffer<PresentationRecycleRequest>(entity);
        }

        private void EnsureSyncConfig(ref SystemState state)
        {
            if (!_syncConfigQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, PresentationHandleSyncConfig.Default);
        }

        private void EnsurePoolStats(ref SystemState state)
        {
            if (!_poolStatsQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new PresentationPoolStats());
        }
    }
}
