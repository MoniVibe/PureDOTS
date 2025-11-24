using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Presentation;
using PureDOTS.Systems;
using Space4X.Presentation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space4X.Presentation
{
    /// <summary>
    /// Adapter system that reads binding JSON and emits presentation spawn requests.
    /// Uses request buffers only; no structural changes except Begin/End ECB playback.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct Space4XPresentationAdapterSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WeaponMount>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Load bindings if not already loaded
            if (Space4XBindingLoader.CurrentBindings == null)
            {
                Space4XBindingLoader.LoadBindings(Space4XBindingLoader.IsMinimal);
            }

            if (!SystemAPI.TryGetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>(out var ecbSingleton))
            {
                return;
            }

            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Process weapon muzzle FX requests
            var weaponJob = new WeaponPresentationJob
            {
                ECB = ecb.AsParallelWriter()
            };
            state.Dependency = weaponJob.ScheduleParallel(state.Dependency);

            // Process projectile tracer/impact requests
            var projectileJob = new ProjectilePresentationJob
            {
                ECB = ecb.AsParallelWriter()
            };
            state.Dependency = projectileJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct WeaponPresentationJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                in WeaponMount weaponMount,
                in TurretState turretState,
                in LocalTransform transform)
            {
                // Get binding for this weapon
                // Note: In Burst, we can't access Space4XBindingLoader directly
                // This would need to be done via a blob asset or component lookup
                // For now, this is a placeholder structure
                
                // Would emit presentation spawn request for muzzle FX
                // ECB.AddComponent(entityInQueryIndex, entity, new PresentationSpawnRequest { ... });
            }
        }

        [BurstCompile]
        public partial struct ProjectilePresentationJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                in ProjectileEntity projectile,
                in LocalTransform transform)
            {
                // Get binding for this projectile
                // Would emit presentation spawn request for tracer/beam/impact FX
                // ECB.AddComponent(entityInQueryIndex, entity, new PresentationSpawnRequest { ... });
            }
        }
    }
}

