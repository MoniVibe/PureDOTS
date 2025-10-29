using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Spatial;

namespace PureDOTS.Tests
{
    public class BandRegistrySystemTests
    {
        [Test]
        public void BandRegistrySystem_PopulatesEntries()
        {
            using var world = new World("BandRegistrySystemTests");
            var entityManager = world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(entityManager);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                Tick = 10,
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

            var bandEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(bandEntity, new BandId { Value = 3 });
            entityManager.AddComponentData(bandEntity, new BandStats
            {
                MemberCount = 25,
                Morale = 0.85f,
                Flags = BandStatusFlags.Engaged
            });
            entityManager.AddComponentData(bandEntity, LocalTransform.FromPositionRotationScale(new float3(12f, 0f, -4f), quaternion.identity, 1f));
            entityManager.AddComponent<SpatialIndexedTag>(bandEntity);

            world.UpdateSystem<BandRegistrySystem>();
            world.UpdateSystem<RegistryDirectorySystem>();

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<BandRegistry>()).GetSingletonEntity();
            var registry = entityManager.GetComponentData<BandRegistry>(registryEntity);
            Assert.AreEqual(1, registry.TotalBands);
            Assert.AreEqual(25, registry.TotalMembers);
            Assert.AreEqual(10u, registry.LastUpdateTick);
            Assert.AreEqual(0u, registry.LastSpatialVersion);
            Assert.AreEqual(0, registry.SpatialResolvedCount);
            Assert.AreEqual(0, registry.SpatialFallbackCount);
            Assert.AreEqual(0, registry.SpatialUnmappedCount);

            var entries = entityManager.GetBuffer<BandRegistryEntry>(registryEntity);
            Assert.AreEqual(1, entries.Length);
            var entry = entries[0];
            Assert.AreEqual(bandEntity, entry.BandEntity);
            Assert.AreEqual(3, entry.BandId);
            Assert.AreEqual(25, entry.MemberCount);
            Assert.AreEqual(0.85f, entry.Morale, 0.0001f);
            Assert.AreEqual(BandStatusFlags.Engaged, entry.Flags);
            Assert.AreEqual(-1, entry.CellId);
            Assert.AreEqual(0u, entry.SpatialVersion);
            Assert.AreEqual(new float3(12f, 0f, -4f), entry.Position);

            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(RegistryKind.Band, metadata.Kind);
            Assert.AreEqual(1, metadata.EntryCount);
            Assert.AreEqual(10u, metadata.LastUpdateTick);
            Assert.AreEqual(1u, metadata.Version);
            Assert.IsFalse(metadata.Continuity.HasSpatialData);
            Assert.IsFalse(metadata.Continuity.RequiresSpatialSync);
        }
    }
}
