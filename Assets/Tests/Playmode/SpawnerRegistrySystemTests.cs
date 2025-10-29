using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using PureDOTS.Runtime.Spatial;

namespace PureDOTS.Tests
{
    public class SpawnerRegistrySystemTests
    {
        [Test]
        public void SpawnerRegistrySystem_PopulatesEntries()
        {
            using var world = new World("SpawnerRegistrySystemTests");
            var entityManager = world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(entityManager);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                Tick = 20,
                IsPaused = false,
                FixedDeltaTime = 0.016f,
                CurrentSpeedMultiplier = 1f
            });

            var rewindEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                PlaybackTick = 0
            });

            var spawnerEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(spawnerEntity, new SpawnerId { Value = 55 });
            entityManager.AddComponentData(spawnerEntity, new SpawnerConfig
            {
                SpawnTypeId = (FixedString64Bytes)"Villager",
                OwnerFaction = Entity.Null,
                Capacity = 5,
                CooldownSeconds = 30f
            });
            entityManager.AddComponentData(spawnerEntity, new SpawnerState
            {
                ActiveSpawnCount = 2,
                RemainingCooldown = 10f,
                Flags = SpawnerStatusFlags.Active
            });
            entityManager.AddComponentData(spawnerEntity, LocalTransform.FromPositionRotationScale(new float3(3f, 0f, 7f), quaternion.identity, 1f));
            entityManager.AddComponent<SpatialIndexedTag>(spawnerEntity);

            world.UpdateSystem<SpawnerRegistrySystem>();
            world.UpdateSystem<RegistryDirectorySystem>();

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnerRegistry>()).GetSingletonEntity();
            var registry = entityManager.GetComponentData<SpawnerRegistry>(registryEntity);
            Assert.AreEqual(1, registry.TotalSpawners);
            Assert.AreEqual(1, registry.ActiveSpawnerCount);

            var entries = entityManager.GetBuffer<SpawnerRegistryEntry>(registryEntity);
            Assert.AreEqual(1, entries.Length);
            var entry = entries[0];
            Assert.AreEqual(spawnerEntity, entry.SpawnerEntity);
            Assert.AreEqual((FixedString64Bytes)"Villager", entry.SpawnerTypeId);
            Assert.AreEqual(2, entry.ActiveSpawnCount);
            Assert.AreEqual(5, entry.Capacity);
            Assert.AreEqual(10f, entry.RemainingCooldown, 0.0001f);
            Assert.AreEqual(SpawnerStatusFlags.Active, entry.Flags);

            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(RegistryKind.Spawner, metadata.Kind);
            Assert.AreEqual(1, metadata.EntryCount);
        }
    }
}
