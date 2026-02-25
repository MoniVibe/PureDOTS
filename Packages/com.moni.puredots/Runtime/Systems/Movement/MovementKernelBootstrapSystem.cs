using PureDOTS.Runtime.Movement;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Movement
{
    /// <summary>
    /// Bootstraps movement kernel guard singleton and ensures owned entities carry MovementKernelPose.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial struct MovementKernelBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            EnsureGuardSingleton(entityManager);
            EnsureMovementTaggedEntitiesAreOwned(entityManager);
            EnsureOwnedEntitiesHaveKernelPose(entityManager);
        }

        private static void EnsureGuardSingleton(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MovementKernelGuardConfig>());
            if (!query.IsEmptyIgnoreFilter)
            {
                var singleton = query.GetSingletonEntity();
                if (!entityManager.HasComponent<MovementKernelGuardStats>(singleton))
                {
                    entityManager.AddComponentData(singleton, new MovementKernelGuardStats());
                }

                if (!entityManager.HasBuffer<MovementKernelViolation>(singleton))
                {
                    entityManager.AddBuffer<MovementKernelViolation>(singleton);
                }

                return;
            }

            var created = entityManager.CreateEntity(
                typeof(MovementKernelGuardConfig),
                typeof(MovementKernelGuardStats));
            entityManager.SetComponentData(created, MovementKernelGuardConfig.Default);
            entityManager.SetComponentData(created, new MovementKernelGuardStats());
            entityManager.AddBuffer<MovementKernelViolation>(created);
        }

        private static void EnsureOwnedEntitiesHaveKernelPose(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MovementKernelOwned>(),
                ComponentType.Exclude<MovementKernelPose>());

            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var pose = new MovementKernelPose
                {
                    Position = default,
                    Rotation = quaternion.identity,
                    Scale = 1f,
                    CapturedTick = 0
                };

                if (entityManager.HasComponent<LocalTransform>(entity))
                {
                    var transform = entityManager.GetComponentData<LocalTransform>(entity);
                    pose.Position = transform.Position;
                    pose.Rotation = transform.Rotation;
                    pose.Scale = transform.Scale;
                }

                entityManager.AddComponentData(entity, pose);
            }
        }

        private static void EnsureMovementTaggedEntitiesAreOwned(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<GroundMovementTag>(),
                    ComponentType.ReadOnly<FlyingMovementTag>(),
                    ComponentType.ReadOnly<SpaceMovementTag>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<MovementKernelOwned>()
                }
            });

            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            entityManager.AddComponent<MovementKernelOwned>(query);
        }
    }
}
