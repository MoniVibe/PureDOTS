using PureDOTS.Rendering;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileEffectExecutionSystem))]
    [UpdateAfter(typeof(ProjectileDamageSystem))]
    public partial struct ProjectilePoolRecycleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectilePoolConfig>();
            state.RequireForUpdate<ProjectileRecycleTag>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingletonEntity<ProjectilePoolConfig>(out var poolEntity))
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;
            var currentTime = timeState.ElapsedTime;

            var entityManager = state.EntityManager;
            if (!entityManager.HasComponent<ProjectilePoolState>(poolEntity) ||
                !entityManager.HasBuffer<ProjectilePoolEntry>(poolEntity))
            {
                return;
            }

            var poolState = SystemAPI.GetComponentRW<ProjectilePoolState>(poolEntity);
            var poolBuffer = SystemAPI.GetBuffer<ProjectilePoolEntry>(poolEntity);

            var hasTrackingHub = SystemAPI.TryGetSingletonEntity<ProjectileTrackingHub>(out var trackingHubEntity);
            DynamicBuffer<ProjectileTrackingEvent> trackingEvents = default;
            ProjectileTrackingConfig trackingConfig = default;
            if (hasTrackingHub)
            {
                trackingEvents = SystemAPI.GetBuffer<ProjectileTrackingEvent>(trackingHubEntity);
                trackingConfig = SystemAPI.GetComponent<ProjectileTrackingConfig>(trackingHubEntity);
            }

            foreach (var (recycleTag, entity) in SystemAPI.Query<EnabledRefRW<ProjectileRecycleTag>>().WithEntityAccess())
            {
                if (!recycleTag.ValueRO)
                {
                    continue;
                }

                recycleTag.ValueRW = false;

                if (hasTrackingHub && entityManager.HasComponent<ProjectileTrackingState>(entity))
                {
                    var tracking = entityManager.GetComponentData<ProjectileTrackingState>(entity);
                    if (tracking.TrackingId != 0 &&
                        (trackingConfig.MaxEvents <= 0 || trackingEvents.Length < trackingConfig.MaxEvents))
                    {
                        var projectile = entityManager.HasComponent<ProjectileEntity>(entity)
                            ? entityManager.GetComponentData<ProjectileEntity>(entity)
                            : default;

                        trackingEvents.Add(new ProjectileTrackingEvent
                        {
                            Kind = ProjectileTrackingEventKind.Recycle,
                            TrackingId = tracking.TrackingId,
                            Projectile = entity,
                            Source = projectile.SourceEntity,
                            Target = projectile.TargetEntity,
                            ProjectileId = projectile.ProjectileId,
                            AmmoId = projectile.AmmoId,
                            Position = entityManager.HasComponent<LocalTransform>(entity)
                                ? entityManager.GetComponentData<LocalTransform>(entity).Position
                                : float3.zero,
                            Direction = math.normalizesafe(projectile.Velocity),
                            Tick = currentTick,
                            Time = currentTime,
                            Value = projectile.DistanceTraveled,
                            Mode = 0,
                            Result = 1
                        });

                        tracking.LastEventTick = currentTick;
                    }
                }

                if (entityManager.HasComponent<ProjectileTrackingState>(entity))
                {
                    entityManager.SetComponentData(entity, default(ProjectileTrackingState));
                }

                if (entityManager.HasComponent<ProjectileActive>(entity))
                {
                    entityManager.SetComponentEnabled<ProjectileActive>(entity, false);
                }

                if (entityManager.HasComponent<ProjectileEntity>(entity))
                {
                    entityManager.SetComponentData(entity, default(ProjectileEntity));
                }

                if (entityManager.HasBuffer<ProjectileHitResult>(entity))
                {
                    var hits = entityManager.GetBuffer<ProjectileHitResult>(entity);
                    hits.Clear();
                }

                DisablePresenter<MeshPresenter>(entityManager, entity);
                DisablePresenter<SpritePresenter>(entityManager, entity);
                DisablePresenter<DebugPresenter>(entityManager, entity);
                DisablePresenter<TracerPresenter>(entityManager, entity);

                poolBuffer.Add(new ProjectilePoolEntry { Projectile = entity });
            }

            poolState.ValueRW.Available = poolBuffer.Length;
            poolState.ValueRW.Active = math.max(0, poolState.ValueRO.Capacity - poolState.ValueRW.Available);
        }

        private static void DisablePresenter<T>(EntityManager entityManager, Entity entity)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            if (entityManager.HasComponent<T>(entity))
            {
                entityManager.SetComponentEnabled<T>(entity, false);
            }
        }
    }
}
