using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Registry for spatial query domains (climate, comms, navmesh, etc.).
    /// Each domain registers itself and provides query capabilities.
    /// </summary>
    public struct SpatialDomainRegistry : IComponentData
    {
        /// <summary>
        /// Domain identifier (climate, comms, navmesh, etc.).
        /// </summary>
        public FixedString64Bytes DomainName;

        /// <summary>
        /// Cell size for this domain's spatial grid.
        /// </summary>
        public float CellSize;

        public SpatialDomainRegistry(FixedString64Bytes domainName, float cellSize)
        {
            DomainName = domainName;
            CellSize = cellSize;
        }
    }

    /// <summary>
    /// Singleton managing all registered spatial domains.
    /// </summary>
    public struct SpatialDomainManager : IComponentData
    {
        /// <summary>
        /// Number of registered domains.
        /// </summary>
        public int DomainCount;
    }

    /// <summary>
    /// Burst-safe helper for domain registration.
    /// </summary>
    [BurstCompile]
    public static class SpatialDomainHelper
    {
        [BurstCompile]
        public static void RegisterDomain(
            EntityManager entityManager,
            Entity managerEntity,
            FixedString64Bytes domainName,
            float cellSize)
        {
            var domainEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(domainEntity, new SpatialDomainRegistry(domainName, cellSize));

            // Update manager count
            if (entityManager.HasComponent<SpatialDomainManager>(managerEntity))
            {
                var manager = entityManager.GetComponentData<SpatialDomainManager>(managerEntity);
                manager.DomainCount++;
                entityManager.SetComponentData(managerEntity, manager);
            }
            else
            {
                entityManager.AddComponentData(managerEntity, new SpatialDomainManager { DomainCount = 1 });
            }
        }

        [BurstCompile]
        public static bool TryFindDomain(
            EntityManager entityManager,
            FixedString64Bytes domainName,
            out Entity domainEntity)
        {
            domainEntity = Entity.Null;

            var query = entityManager.CreateEntityQuery(typeof(SpatialDomainRegistry));
            var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var registry = entityManager.GetComponentData<SpatialDomainRegistry>(entities[i]);
                if (registry.DomainName.Equals(domainName))
                {
                    domainEntity = entities[i];
                    entities.Dispose();
                    return true;
                }
            }

            entities.Dispose();
            return false;
        }
    }
}

