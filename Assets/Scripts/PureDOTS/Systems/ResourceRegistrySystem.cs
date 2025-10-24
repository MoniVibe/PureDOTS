using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Maintains a registry of all resource sources indexed by type for efficient queries.
    /// Updates singleton component and buffer with current resource state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ResourceSystemGroup), OrderFirst = true)]
    public partial struct ResourceRegistrySystem : ISystem
    {
        private EntityQuery _resourceQuery;
        private EntityQuery _registryQuery;
        private ComponentLookup<ResourceJobReservation> _reservationLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _resourceQuery = SystemAPI.QueryBuilder()
                .WithAll<ResourceSourceConfig, ResourceTypeId>()
                .Build();

            _registryQuery = SystemAPI.QueryBuilder()
                .WithAll<ResourceRegistry>()
                .Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ResourceTypeIndex>();

            _reservationLookup = state.GetComponentLookup<ResourceJobReservation>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Ensure registry singleton exists
            if (!SystemAPI.HasSingleton<ResourceRegistry>())
            {
                var createdEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<ResourceRegistry>(createdEntity);
                state.EntityManager.AddBuffer<ResourceRegistryEntry>(createdEntity);
            }

            var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
            var registry = SystemAPI.GetComponentRW<ResourceRegistry>(registryEntity);
            var entries = state.EntityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);

            entries.Clear();

            var totalResources = 0;
            var totalActiveResources = 0;

            // Get catalog for type lookups
            var catalog = SystemAPI.GetSingleton<ResourceTypeIndex>().Catalog;
            if (!catalog.IsCreated)
            {
                return;
            }

            _reservationLookup.Update(ref state);

            // Query all resource sources
            foreach (var (sourceState, resourceTypeId, transform, entity) in SystemAPI.Query<RefRO<ResourceSourceState>, RefRO<ResourceTypeId>, RefRO<LocalTransform>>()
                .WithAll<ResourceSourceConfig>()
                .WithEntityAccess())
            {
                // Lookup type index
                var typeIndex = catalog.Value.LookupIndex(resourceTypeId.ValueRO.Value);
                if (typeIndex < 0)
                {
                    continue; // Skip unknown types
                }

                var reservation = _reservationLookup.HasComponent(entity)
                    ? _reservationLookup[entity]
                    : default;

                entries.Add(new ResourceRegistryEntry
                {
                    ResourceTypeIndex = (ushort)typeIndex,
                    SourceEntity = entity,
                    Position = transform.ValueRO.Position,
                    UnitsRemaining = sourceState.ValueRO.UnitsRemaining,
                    ActiveTickets = reservation.ActiveTickets,
                    ClaimFlags = reservation.ClaimFlags,
                    LastMutationTick = reservation.LastMutationTick
                });

                totalResources++;
                if (sourceState.ValueRO.UnitsRemaining > 0f)
                {
                    totalActiveResources++;
                }
            }

            registry.ValueRW = new ResourceRegistry
            {
                TotalResources = totalResources,
                TotalActiveResources = totalActiveResources,
                LastUpdateTick = timeState.Tick
            };
        }
    }
}

