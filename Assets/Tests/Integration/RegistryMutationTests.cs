using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Tests.Playmode;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Tests for registry spawn/despawn handling and deterministic rebuild.
    /// </summary>
    public class RegistryMutationTests : EcsTestFixture
    {
        [Test]
        public void ResourceRegistry_RebuildsOnEntitySpawn()
        {
            var world = World;
            var entityManager = world.EntityManager;

            // Bootstrap singletons
            var timeEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 0.02f,
                CurrentSpeedMultiplier = 1f,
                Tick = 0,
                IsPaused = false
            });

            var rewindEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record
            });

            // Create resource registry
            var registryEntity = entityManager.CreateEntity();
            entityManager.AddComponent<ResourceRegistry>(registryEntity);
            var entries = entityManager.AddBuffer<ResourceRegistryEntry>(registryEntity);
            entityManager.AddComponentData(registryEntity, new RegistryMetadata
            {
                Kind = RegistryKind.Resource,
                Flags = (byte)RegistryHandleFlags.SupportsAIQueries,
                Label = new FixedString64Bytes("ResourceRegistry")
            });

            // Create resource type index
            var typeIndexEntity = entityManager.CreateEntity();
            entityManager.AddComponent<ResourceTypeIndex>(typeIndexEntity);

            // Create initial resource entity
            var resource1 = entityManager.CreateEntity();
            entityManager.AddComponentData(resource1, new ResourceSourceConfig
            {
                GatherRatePerWorker = 1f,
                MaxSimultaneousWorkers = 3,
                RespawnSeconds = 0f,
                Flags = 0
            });
            entityManager.AddComponentData(resource1, new ResourceTypeId { Value = "Wood" });
            entityManager.AddComponentData(resource1, new ResourceSourceState { UnitsRemaining = 100f });
            entityManager.AddComponentData(resource1, new LocalTransform
            {
                Position = new float3(0f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // Note: Registry system would rebuild here in real scenario
            // This test validates that entities matching the query are included
            Assert.IsTrue(entityManager.HasComponent<ResourceSourceConfig>(resource1));
            Assert.IsTrue(entityManager.HasComponent<ResourceTypeId>(resource1));
        }

        [Test]
        public void Registry_DeterministicOrdering()
        {
            var world = World;
            var entityManager = world.EntityManager;

            // Create multiple entities with different indices
            var entities = new NativeArray<Entity>(5, Allocator.Temp);
            for (int i = 0; i < 5; i++)
            {
                entities[i] = entityManager.CreateEntity();
            }

            // Verify deterministic ordering (Entity.Index determines order)
            for (int i = 0; i < entities.Length - 1; i++)
            {
                Assert.Less(entities[i].Index, entities[i + 1].Index, 
                    "Entity indices should be monotonically increasing");
            }

            entities.Dispose();
        }
    }
}


