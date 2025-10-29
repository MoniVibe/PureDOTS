using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using Unity.Entities;

namespace PureDOTS.Tests
{
    public class RegistryInstrumentationSystemTests
    {
        [Test]
        public void RegistryInstrumentationSystem_PopulatesSamplesAndCounts()
        {
            using var world = new World("RegistryInstrumentationTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 900u;
            entityManager.SetComponentData(timeEntity, timeState);

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            metadata.MarkUpdated(12, timeState.Tick, RegistryContinuitySnapshot.WithSpatialData(3u, requireSync: true));
            entityManager.SetComponentData(registryEntity, metadata);

            entityManager.SetComponentData(registryEntity, new RegistryHealth
            {
                HealthLevel = RegistryHealthLevel.Warning,
                StaleEntryCount = 0,
                StaleEntryRatio = 0f,
                SpatialVersionDelta = 2u,
                TicksSinceLastUpdate = 5u,
                DirectoryVersionDelta = 0u,
                TotalEntryCount = 12,
                LastHealthCheckTick = timeState.Tick,
                FailureFlags = RegistryHealthFlags.UpdateFrequencyWarning
            });

            world.UpdateSystem<RegistryDirectorySystem>();
            world.UpdateSystem<RegistryInstrumentationSystem>();

            var directoryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryDirectory>()).GetSingletonEntity();
            var instrumentation = entityManager.GetComponentData<RegistryInstrumentationState>(directoryEntity);

            Assert.AreEqual(1, instrumentation.SampleCount);
            Assert.AreEqual(0, instrumentation.HealthyCount);
            Assert.AreEqual(1, instrumentation.WarningCount);
            Assert.AreEqual(0, instrumentation.CriticalCount);
            Assert.AreEqual(0, instrumentation.FailureCount);
            Assert.GreaterOrEqual(instrumentation.Version, 1u);
            Assert.AreEqual(timeState.Tick, instrumentation.LastUpdateTick);

            var samples = entityManager.GetBuffer<RegistryInstrumentationSample>(directoryEntity);
            Assert.AreEqual(1, samples.Length);
            var sample = samples[0];
            Assert.AreEqual(metadata.Label, sample.Label);
            Assert.AreEqual(RegistryHealthLevel.Warning, sample.HealthLevel);
            Assert.AreEqual(RegistryHealthFlags.UpdateFrequencyWarning, sample.HealthFlags);
            Assert.AreEqual(12, sample.EntryCount);
            Assert.AreEqual(metadata.Version, sample.Version);
            Assert.AreEqual(metadata.LastUpdateTick, sample.LastUpdateTick);
            Assert.AreEqual(3u, sample.SpatialVersion);
            Assert.AreEqual(2u, sample.SpatialVersionDelta);
        }
    }
}
