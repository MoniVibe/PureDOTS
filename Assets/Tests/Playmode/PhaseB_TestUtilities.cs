using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using Unity.Entities;

namespace PureDOTS.Tests.Playmode
{
    internal static class PhaseBTestUtilities
    {
        internal static Entity EnsureSpatialGrid(EntityManager entityManager, SpatialGridConfig config)
        {
            var gridEntity = EnsureSingleton(entityManager, config);

            if (!entityManager.HasComponent<SpatialGridState>(gridEntity))
            {
                entityManager.AddComponentData(gridEntity, new SpatialGridState());
            }

            if (!entityManager.HasBuffer<SpatialGridCellRange>(gridEntity))
            {
                entityManager.AddBuffer<SpatialGridCellRange>(gridEntity);
            }

            if (!entityManager.HasBuffer<SpatialGridEntry>(gridEntity))
            {
                entityManager.AddBuffer<SpatialGridEntry>(gridEntity);
            }

            if (!entityManager.HasBuffer<SpatialGridStagingEntry>(gridEntity))
            {
                entityManager.AddBuffer<SpatialGridStagingEntry>(gridEntity);
            }

            if (!entityManager.HasBuffer<SpatialGridStagingCellRange>(gridEntity))
            {
                entityManager.AddBuffer<SpatialGridStagingCellRange>(gridEntity);
            }

            EnsureRegistryDirectory(entityManager);

            return gridEntity;
        }

        internal static void EnsureSignalField(
            EntityManager entityManager,
            Entity gridEntity,
            SpatialGridConfig gridConfig,
            SignalFieldConfig? configOverride = null)
        {
            if (!entityManager.HasComponent<SignalFieldConfig>(gridEntity))
            {
                entityManager.AddComponentData(gridEntity, configOverride ?? SignalFieldConfig.Default);
            }
            else if (configOverride.HasValue)
            {
                entityManager.SetComponentData(gridEntity, configOverride.Value);
            }

            if (!entityManager.HasComponent<SignalFieldState>(gridEntity))
            {
                entityManager.AddComponentData(gridEntity, new SignalFieldState());
            }

            if (!entityManager.HasComponent<SignalPerceptionThresholds>(gridEntity))
            {
                entityManager.AddComponentData(gridEntity, SignalPerceptionThresholds.Default);
            }

            var cells = entityManager.HasBuffer<SignalFieldCell>(gridEntity)
                ? entityManager.GetBuffer<SignalFieldCell>(gridEntity)
                : entityManager.AddBuffer<SignalFieldCell>(gridEntity);

            if (cells.Length != gridConfig.CellCount)
            {
                cells.Clear();
                cells.ResizeUninitialized(gridConfig.CellCount);
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i] = default;
                }
            }
        }

        internal static Entity EnsureRegistryDirectory(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<RegistryDirectory>());
            Entity entity;
            if (query.IsEmptyIgnoreFilter)
            {
                entity = entityManager.CreateEntity(typeof(RegistryDirectory));
            }
            else
            {
                entity = query.GetSingletonEntity();
            }

            if (!entityManager.HasBuffer<RegistryDirectoryEntry>(entity))
            {
                entityManager.AddBuffer<RegistryDirectoryEntry>(entity);
            }

            if (!entityManager.HasComponent<RegistryDirectory>(entity))
            {
                entityManager.AddComponentData(entity, new RegistryDirectory());
            }

            return entity;
        }

        private static Entity EnsureSingleton<T>(EntityManager entityManager, T data) where T : unmanaged, IComponentData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            Entity entity;
            if (query.IsEmptyIgnoreFilter)
            {
                entity = entityManager.CreateEntity(typeof(T));
            }
            else
            {
                entity = query.GetSingletonEntity();
            }

            entityManager.SetComponentData(entity, data);
            return entity;
        }
    }
}
