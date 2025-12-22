using NUnit.Framework;
using PureDOTS.Runtime.Bands;
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
                TargetTick = 0,
                TickDuration = 0.016f,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            });
            entityManager.SetComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = 0,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });

            var bandEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(bandEntity, new BandId { Value = 3 });
            entityManager.AddComponentData(bandEntity, new BandStats
            {
                MemberCount = 25,
                AverageDiscipline = 60f,
                Morale = 75f,
                Cohesion = 70f,
                Fatigue = 0.1f,
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
            Assert.AreEqual(75f, registry.AverageMorale, 0.0001f);
            Assert.AreEqual(70f, registry.AverageCohesion, 0.0001f);
            Assert.AreEqual(60f, registry.AverageDiscipline, 0.0001f);

            var entries = entityManager.GetBuffer<BandRegistryEntry>(registryEntity);
            Assert.AreEqual(1, entries.Length);
            var entry = entries[0];
            Assert.AreEqual(bandEntity, entry.BandEntity);
            Assert.AreEqual(3, entry.BandId);
            Assert.AreEqual(25, entry.MemberCount);
            Assert.AreEqual(75f, entry.Morale, 0.0001f);
            Assert.AreEqual(70f, entry.Cohesion, 0.0001f);
            Assert.AreEqual(60f, entry.AverageDiscipline, 0.0001f);
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
