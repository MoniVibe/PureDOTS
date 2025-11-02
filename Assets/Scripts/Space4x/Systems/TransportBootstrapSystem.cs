using PureDOTS.Runtime.Registry;
using Space4X.Runtime.Transport;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Systems
{
    /// <summary>
    /// Creates transport registries for game-specific transport units (miner vessels, haulers, freighters, wagons).
    /// Runs after CoreSingletonBootstrapSystem to ensure framework registries are created first.
    /// </summary>
    [UpdateInGroup(typeof(PureDOTS.Systems.TimeSystemGroup))]
    [UpdateAfter(typeof(PureDOTS.Systems.CoreSingletonBootstrapSystem))]
    public partial class TransportBootstrapSystem : SystemBase
    {
        protected override void OnCreate()
        {
            // Don't require anything - we'll create registries if they don't exist
        }

        protected override void OnUpdate()
        {
            var entityManager = EntityManager;

            // Create transport registries using Custom registry kind since they're game-specific
            EnsureRegistry<MinerVesselRegistry, MinerVesselRegistryEntry>(entityManager, RegistryKind.Custom, new FixedString64Bytes("MinerVesselRegistry"), RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries);
            EnsureRegistry<HaulerRegistry, HaulerRegistryEntry>(entityManager, RegistryKind.Custom, new FixedString64Bytes("HaulerRegistry"), RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries);
            EnsureRegistry<FreighterRegistry, FreighterRegistryEntry>(entityManager, RegistryKind.Custom, new FixedString64Bytes("FreighterRegistry"), RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries);
            EnsureRegistry<WagonRegistry, WagonRegistryEntry>(entityManager, RegistryKind.Custom, new FixedString64Bytes("WagonRegistry"), RegistryHandleFlags.SupportsSpatialQueries | RegistryHandleFlags.SupportsAIQueries | RegistryHandleFlags.SupportsPathfinding);

            // Disable system after first run
            Enabled = false;
        }

        private static void EnsureRegistry<TRegistry, TEntry>(EntityManager entityManager, RegistryKind kind, FixedString64Bytes label, RegistryHandleFlags flags)
            where TRegistry : unmanaged, IComponentData
            where TEntry : unmanaged, IBufferElementData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TRegistry>());
            Entity registryEntity;

            if (query.IsEmptyIgnoreFilter)
            {
                registryEntity = entityManager.CreateEntity(typeof(TRegistry));
            }
            else
            {
                registryEntity = query.GetSingletonEntity();
            }

            if (!entityManager.HasBuffer<TEntry>(registryEntity))
            {
                entityManager.AddBuffer<TEntry>(registryEntity);
            }

            if (!entityManager.HasComponent<RegistryMetadata>(registryEntity))
            {
                var metadata = new RegistryMetadata();
                metadata.Initialise(kind, 0, flags, label);
                entityManager.AddComponentData(registryEntity, metadata);
            }
            else
            {
                var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
                if (metadata.Kind == RegistryKind.Unknown && metadata.Version == 0 && metadata.EntryCount == 0)
                {
                    metadata.Initialise(kind, metadata.ArchetypeId, flags, label);
                    entityManager.SetComponentData(registryEntity, metadata);
                }
            }

            if (!entityManager.HasComponent<RegistryHealth>(registryEntity))
            {
                entityManager.AddComponentData(registryEntity, default(RegistryHealth));
            }
        }
    }
}

