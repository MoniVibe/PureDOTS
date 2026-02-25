using PureDOTS.Input;
using PureDOTS.Runtime.InputKernel;
using PureDOTS.Runtime.Movement;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Ensures InputKernel singleton state and baseline ownership/intent components exist.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial struct InputKernelBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            EnsureRootSingleton(entityManager);
            EnsureOwnershipForControlledEntities(entityManager);
            EnsureLocomotionIntentForMovementOwnedEntities(entityManager);
        }

        private static void EnsureRootSingleton(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<InputKernelRootTag>());
            if (query.IsEmptyIgnoreFilter)
            {
                var root = entityManager.CreateEntity(
                    typeof(InputKernelRootTag),
                    typeof(InputKernelState),
                    typeof(InputKernelDiagnostics));
                entityManager.SetComponentData(root, new InputKernelState
                {
                    Tick = 0u,
                    Revision = 1u
                });
                entityManager.SetComponentData(root, new InputKernelDiagnostics
                {
                    LastTick = 0u,
                    EntitiesSanitized = 0u,
                    TotalSanitized = 0u
                });
                return;
            }

            var singleton = query.GetSingletonEntity();
            if (!entityManager.HasComponent<InputKernelState>(singleton))
            {
                entityManager.AddComponentData(singleton, new InputKernelState
                {
                    Tick = 0u,
                    Revision = 1u
                });
            }

            if (!entityManager.HasComponent<InputKernelDiagnostics>(singleton))
            {
                entityManager.AddComponentData(singleton, new InputKernelDiagnostics
                {
                    LastTick = 0u,
                    EntitiesSanitized = 0u,
                    TotalSanitized = 0u
                });
            }
        }

        private static void EnsureOwnershipForControlledEntities(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ControlledBy>(),
                ComponentType.Exclude<InputKernelOwnership>());
            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            using var entities = query.ToEntityArray(Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var controlledBy = entityManager.GetComponentData<ControlledBy>(entity);
                entityManager.AddComponentData(entity, new InputKernelOwnership
                {
                    PlayerId = controlledBy.PlayerId
                });
            }
        }

        private static void EnsureLocomotionIntentForMovementOwnedEntities(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MovementKernelOwned>(),
                ComponentType.Exclude<InputKernelLocomotionIntent>());
            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            using var entities = query.ToEntityArray(Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                entityManager.AddComponentData(entities[i], InputKernelLocomotionIntent.Disabled);
            }
        }
    }
}
