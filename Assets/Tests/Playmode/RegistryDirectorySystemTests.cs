using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Tests
{
    public partial class RegistryDirectorySystemTests
    {
        [Test]
        public void RegistryDirectorySystem_BuildsDirectoryFromMetadata()
        {
            using var world = new World("RegistryDirectoryTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();
            world.UpdateSystem<RegistryDirectorySystem>();

            var directoryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryDirectory>()).GetSingletonEntity();
            var directory = entityManager.GetComponentData<RegistryDirectory>(directoryEntity);
            var entries = entityManager.GetBuffer<RegistryDirectoryEntry>(directoryEntity);

            Assert.Greater(entries.Length, 0, "Directory should collect seeded registries.");

            Assert.IsTrue(entries.TryGetHandle(RegistryKind.Resource, out _), "Resource registry handle missing.");
            Assert.IsTrue(entries.TryGetHandle(RegistryKind.Storehouse, out _), "Storehouse registry handle missing.");
            Assert.Greater(directory.Version, 0u);
        }

        [Test]
        public void RegistryDirectorySystem_UpdatesWhenMetadataChanges()
        {
            using var world = new World("RegistryDirectoryUpdateTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();
            world.UpdateSystem<RegistryDirectorySystem>();

            var directoryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryDirectory>()).GetSingletonEntity();
            var directoryInitial = entityManager.GetComponentData<RegistryDirectory>(directoryEntity);

            // Mutate resource registry metadata to force a directory refresh.
            var resourceEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var metadata = entityManager.GetComponentData<RegistryMetadata>(resourceEntity);
            metadata.MarkUpdated(metadata.EntryCount, metadata.LastUpdateTick + 1u, RegistryContinuitySnapshot.WithoutSpatialData());
            entityManager.SetComponentData(resourceEntity, metadata);

            // Advance time tick so the directory records a new update tick.
            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick++;
            entityManager.SetComponentData(timeEntity, timeState);

            world.UpdateSystem<RegistryDirectorySystem>();

            var directoryUpdated = entityManager.GetComponentData<RegistryDirectory>(directoryEntity);
            var entries = entityManager.GetBuffer<RegistryDirectoryEntry>(directoryEntity);

            Assert.Greater(directoryUpdated.Version, directoryInitial.Version, "Directory version did not increment.");
            Assert.AreNotEqual(directoryUpdated.AggregateHash, directoryInitial.AggregateHash, "Aggregate hash did not change after metadata mutation.");
            Assert.IsTrue(entries.TryGetHandle(RegistryKind.Resource, out var handle));
            Assert.AreEqual(resourceEntity, handle.RegistryEntity);
        }

        [Test]
        public void RegistryDirectoryLookup_ResolvesRegistryBuffer()
        {
            using var world = new World("RegistryDirectoryLookupTest");

            world.UpdateSystem<CoreSingletonBootstrapSystem>();
            world.UpdateSystem<RegistryDirectorySystem>();

            RegistryLookupTestSystem.Reset();
            world.UpdateSystem<RegistryLookupTestSystem>();

            Assert.IsTrue(RegistryLookupTestSystem.LastResult, "Lookup should succeed once directory is initialised.");
            Assert.GreaterOrEqual(RegistryLookupTestSystem.BufferLength, 0, "Buffer length should be reported even if empty.");
        }

        public partial struct RegistryLookupTestSystem : ISystem
        {
            public static bool LastResult;
            public static int BufferLength;
            private BufferLookup<ResourceRegistryEntry> _resourceEntriesLookup;
            private EntityQuery _resourceRegistryQuery;

            public static void Reset()
            {
                LastResult = false;
                BufferLength = -1;
            }

            public void OnCreate(ref SystemState state)
            {
                state.RequireForUpdate<RegistryDirectory>();
                state.RequireForUpdate<ResourceRegistry>();

                _resourceEntriesLookup = state.GetBufferLookup<ResourceRegistryEntry>(isReadOnly: true);
                _resourceRegistryQuery = state.GetEntityQuery(ComponentType.ReadOnly<ResourceRegistry>(), ComponentType.ReadOnly<ResourceRegistryEntry>());
                state.RequireForUpdate(_resourceRegistryQuery);
            }

            public void OnUpdate(ref SystemState state)
            {
                _resourceEntriesLookup.Update(ref state);

                if (_resourceRegistryQuery.IsEmptyIgnoreFilter)
                {
                    LastResult = false;
                    BufferLength = -1;
                }
                else
                {
                    var registryEntity = _resourceRegistryQuery.GetSingletonEntity();
                    LastResult = _resourceEntriesLookup.TryGetBuffer(registryEntity, out var buffer);
                    BufferLength = LastResult ? buffer.Length : -1;
                }

                state.Enabled = false;
            }
        }
    }
}
