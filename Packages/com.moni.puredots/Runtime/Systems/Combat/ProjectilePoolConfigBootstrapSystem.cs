using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Ensures a projectile pool config exists for headless/early iteration runs.
    /// Creates a minimal prefab entity when none is authored.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ProjectilePoolConfigBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<ProjectilePoolConfig>())
            {
                state.Enabled = false;
                return;
            }

            var entityManager = state.EntityManager;

            var prefab = entityManager.CreateEntity();
            entityManager.AddComponent<Prefab>(prefab);
            entityManager.AddComponentData(prefab, LocalTransform.FromPosition(float3.zero));
            entityManager.AddComponentData(prefab, default(ProjectileEntity));
            entityManager.AddComponent<ProjectileActive>(prefab);
            entityManager.AddComponent<ProjectileRecycleTag>(prefab);
            entityManager.AddBuffer<ProjectileHitResult>(prefab);
            entityManager.SetComponentEnabled<ProjectileActive>(prefab, false);
            entityManager.SetComponentEnabled<ProjectileRecycleTag>(prefab, false);

            var configEntity = entityManager.CreateEntity(typeof(ProjectilePoolConfig));
            entityManager.SetComponentData(configEntity, new ProjectilePoolConfig
            {
                Prefab = prefab,
                Capacity = 256
            });

            state.Enabled = false;
        }
    }
}
