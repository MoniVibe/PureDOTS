using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests
{
    public class ConstructionRegistrySystemTests
    {
        [Test]
        public void ConstructionRegistrySystem_PopulatesEntries()
        {
            using var world = new World("ConstructionRegistrySystemTests");
            var entityManager = world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(entityManager);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                Tick = 1,
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

            var siteEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(siteEntity, new ConstructionSiteId { Value = 7 });
            entityManager.AddComponentData(siteEntity, new ConstructionSiteProgress
            {
                RequiredProgress = 10f,
                CurrentProgress = 2f
            });
            entityManager.AddComponentData(siteEntity, new ConstructionSiteFlags { Value = 0 });
            entityManager.AddComponentData(siteEntity, LocalTransform.FromPositionRotationScale(
                new float3(5f, 0f, -2f),
                quaternion.identity,
                1f));
            entityManager.AddComponent<SpatialIndexedTag>(siteEntity);

            world.UpdateSystem<ConstructionRegistrySystem>();
            world.UpdateSystem<RegistryDirectorySystem>();
            world.UpdateSystem<RegistryInstrumentationSystem>();

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ConstructionRegistry>()).GetSingletonEntity();
            var registry = entityManager.GetComponentData<ConstructionRegistry>(registryEntity);
            Assert.AreEqual(1, registry.ActiveSiteCount);
            Assert.AreEqual(0, registry.CompletedSiteCount);
            Assert.AreEqual(1, registry.SpatialResolvedCount + registry.SpatialFallbackCount + registry.SpatialUnmappedCount);
            Assert.AreEqual(0u, registry.LastSpatialVersion);

            var entries = entityManager.GetBuffer<ConstructionRegistryEntry>(registryEntity);
            Assert.AreEqual(1, entries.Length);
            var entry = entries[0];
            Assert.AreEqual(siteEntity, entry.SiteEntity);
            Assert.AreEqual(7, entry.SiteId);
            Assert.AreEqual(10f, entry.RequiredProgress);
            Assert.AreEqual(2f, entry.CurrentProgress);
            Assert.AreEqual(0.2f, entry.NormalizedProgress, 0.0001f);
            Assert.AreEqual(0, entry.Flags);
            Assert.GreaterOrEqual(entry.CellId, 0);
            Assert.AreEqual(5f, entry.Position.x, 0.0001f);
            Assert.AreEqual(0f, entry.Position.y, 0.0001f);
            Assert.AreEqual(-2f, entry.Position.z, 0.0001f);

            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(RegistryKind.Construction, metadata.Kind);
            Assert.AreEqual(1, metadata.EntryCount);
            Assert.IsTrue(metadata.Continuity.HasSpatialData);
            Assert.AreEqual(entry.SpatialVersion, metadata.Continuity.SpatialVersion);

            var directoryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryDirectory>()).GetSingletonEntity();
            var directoryEntries = entityManager.GetBuffer<RegistryDirectoryEntry>(directoryEntity);
            var foundConstruction = false;
            for (var i = 0; i < directoryEntries.Length; i++)
            {
                var directoryEntry = directoryEntries[i];
                if (directoryEntry.Kind != RegistryKind.Construction)
                {
                    continue;
                }

                foundConstruction = true;
                Assert.AreEqual(registryEntity, directoryEntry.Handle.RegistryEntity);
                break;
            }

            Assert.IsTrue(foundConstruction, "Construction registry should be present in the registry directory.");

            var instrumentationSamples = entityManager.GetBuffer<RegistryInstrumentationSample>(directoryEntity);
            var foundSample = false;
            for (var i = 0; i < instrumentationSamples.Length; i++)
            {
                var sample = instrumentationSamples[i];
                if (sample.Handle.Kind == RegistryKind.Construction)
                {
                    foundSample = true;
                    Assert.AreEqual(1, sample.EntryCount);
                    Assert.AreEqual(metadata.Version, sample.Version);
                    break;
                }
            }

            Assert.IsTrue(foundSample, "Construction registry should produce instrumentation samples.");
        }
    }
}
