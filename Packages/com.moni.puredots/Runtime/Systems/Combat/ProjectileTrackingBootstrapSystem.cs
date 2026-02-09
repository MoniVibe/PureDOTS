using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Ensures a projectile tracking hub exists for headless/early iteration runs.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ProjectileTrackingBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<ProjectileTrackingHub>())
            {
                state.Enabled = false;
                return;
            }

            var entity = state.EntityManager.CreateEntity(
                typeof(ProjectileTrackingHub),
                typeof(ProjectileTrackingConfig),
                typeof(ProjectileTrackingCounters));

            state.EntityManager.SetComponentData(entity, new ProjectileTrackingConfig
            {
                MaxEvents = 4096,
                ClearEachFrame = 1
            });

            state.EntityManager.SetComponentData(entity, new ProjectileTrackingCounters
            {
                NextId = 1
            });

            state.EntityManager.AddBuffer<ProjectileTrackingEvent>(entity);
            state.EntityManager.AddBuffer<ProjectileTrackingAmmoCounter>(entity);
            state.Enabled = false;
        }
    }
}
