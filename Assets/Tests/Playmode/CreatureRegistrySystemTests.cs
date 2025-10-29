using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace PureDOTS.Tests
{
    public class CreatureRegistrySystemTests
    {
        [Test]
        public void CreatureRegistrySystem_PopulatesEntries()
        {
            using var world = new World("CreatureRegistrySystemTests");
            var entityManager = world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(entityManager);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                Tick = 2,
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

            var creatureEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(creatureEntity, new CreatureId { Value = 101 });
            entityManager.AddComponentData(creatureEntity, new CreatureAttributes
            {
                TypeId = (FixedString64Bytes)"Wolf",
                ThreatLevel = 3.5f,
                Flags = CreatureStatusFlags.Hostile
            });
            entityManager.AddComponentData(creatureEntity, LocalTransform.FromPositionRotationScale(new float3(-6f, 0f, 4f), quaternion.identity, 1f));
            entityManager.AddComponent<SpatialIndexedTag>(creatureEntity);

            world.UpdateSystem<CreatureRegistrySystem>();
            world.UpdateSystem<RegistryDirectorySystem>();

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CreatureRegistry>()).GetSingletonEntity();
            var registry = entityManager.GetComponentData<CreatureRegistry>(registryEntity);
            Assert.AreEqual(1, registry.TotalCreatures);
            Assert.AreEqual(3.5f, registry.TotalThreatScore, 0.0001f);
            Assert.AreEqual(0, registry.SpatialResolvedCount);
            Assert.AreEqual(1, registry.SpatialFallbackCount);
            Assert.AreEqual(0, registry.SpatialUnmappedCount);
            Assert.AreEqual(0u, registry.LastSpatialVersion);

            var entries = entityManager.GetBuffer<CreatureRegistryEntry>(registryEntity);
            Assert.AreEqual(1, entries.Length);
            var entry = entries[0];
            Assert.AreEqual(creatureEntity, entry.CreatureEntity);
            Assert.AreEqual(101, entry.CreatureId);
            Assert.AreEqual((FixedString64Bytes)"Wolf", entry.TypeId);
            Assert.AreEqual(3.5f, entry.ThreatLevel, 0.0001f);
            Assert.AreEqual(CreatureStatusFlags.Hostile, entry.Flags);
            Assert.GreaterOrEqual(entry.CellId, 0);
            Assert.AreEqual(0u, entry.SpatialVersion);

            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(RegistryKind.Creature, metadata.Kind);
            Assert.AreEqual(1, metadata.EntryCount);
            Assert.IsTrue(metadata.Continuity.HasSpatialData);
            Assert.AreEqual(0u, metadata.Continuity.SpatialVersion);
            Assert.AreEqual(0, metadata.Continuity.SpatialResolvedCount);
            Assert.AreEqual(1, metadata.Continuity.SpatialFallbackCount);
        }
    }
}
