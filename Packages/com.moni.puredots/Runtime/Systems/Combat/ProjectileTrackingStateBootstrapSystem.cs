using PureDOTS.Runtime.Combat;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
    [UpdateInGroup(typeof(CombatSystemGroup), OrderFirst = true)]
    public partial struct ProjectileTrackingStateBootstrapSystem : ISystem
    {
        private EntityQuery _missingTrackingQuery;

        public void OnCreate(ref SystemState state)
        {
            _missingTrackingQuery = SystemAPI.QueryBuilder()
                .WithAll<ProjectileEntity>()
                .WithNone<ProjectileTrackingState>()
                .Build();

            state.RequireForUpdate(_missingTrackingQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_missingTrackingQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entityManager = state.EntityManager;
            using var entities = _missingTrackingQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!entityManager.HasComponent<ProjectileTrackingState>(entity))
                {
                    entityManager.AddComponentData(entity, default(ProjectileTrackingState));
                }
            }
        }
    }
}
