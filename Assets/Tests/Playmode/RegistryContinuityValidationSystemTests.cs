using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using Unity.Entities;

namespace PureDOTS.Tests
{
    public class RegistryContinuityValidationSystemTests
    {
        [Test]
        public void ContinuityValidation_RaisesWarningOnSpatialDrift()
        {
            using var world = new World("ContinuityWarningTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 200;
            entityManager.SetComponentData(timeEntity, timeState);

            var thresholdsEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryHealthMonitoring>()).GetSingletonEntity();
            var thresholds = entityManager.GetComponentData<RegistryHealthThresholds>(thresholdsEntity);
            thresholds.SpatialVersionMismatchWarning = 1;
            thresholds.SpatialVersionMismatchCritical = 10;
            entityManager.SetComponentData(thresholdsEntity, thresholds);

            var syncEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistrySpatialSyncState>()).GetSingletonEntity();
            entityManager.SetComponentData(syncEntity, new RegistrySpatialSyncState
            {
                SpatialVersion = 7u,
                LastPublishedTick = timeState.Tick,
                HasSpatialDataFlag = 1
            });

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            metadata.MarkUpdated(0, timeState.Tick, RegistryContinuitySnapshot.WithSpatialData(5u, requireSync: true));
            entityManager.SetComponentData(registryEntity, metadata);

            world.UpdateSystem<RegistryDirectorySystem>();
            world.UpdateSystem<RegistryContinuityValidationSystem>();

            var continuityState = entityManager.GetComponentData<RegistryContinuityState>(syncEntity);
            Assert.AreEqual(1, continuityState.WarningCount);
            Assert.AreEqual(0, continuityState.FailureCount);

            var alerts = entityManager.GetBuffer<RegistryContinuityAlert>(syncEntity);
            Assert.AreEqual(1, alerts.Length);
            var alert = alerts[0];
            Assert.AreEqual(RegistryContinuityStatus.Warning, alert.Status);
            Assert.AreEqual(2u, alert.Delta);
        }

        [Test]
        public void ContinuityValidation_RaisesFailureOnCriticalDrift()
        {
            using var world = new World("ContinuityFailureTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 500;
            entityManager.SetComponentData(timeEntity, timeState);

            var thresholdsEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryHealthMonitoring>()).GetSingletonEntity();
            var thresholds = entityManager.GetComponentData<RegistryHealthThresholds>(thresholdsEntity);
            thresholds.SpatialVersionMismatchWarning = 2;
            thresholds.SpatialVersionMismatchCritical = 4;
            entityManager.SetComponentData(thresholdsEntity, thresholds);

            var syncEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistrySpatialSyncState>()).GetSingletonEntity();
            entityManager.SetComponentData(syncEntity, new RegistrySpatialSyncState
            {
                SpatialVersion = 20u,
                LastPublishedTick = timeState.Tick,
                HasSpatialDataFlag = 1
            });

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            metadata.MarkUpdated(0, timeState.Tick, RegistryContinuitySnapshot.WithSpatialData(12u, requireSync: true));
            entityManager.SetComponentData(registryEntity, metadata);

            world.UpdateSystem<RegistryDirectorySystem>();
            world.UpdateSystem<RegistryContinuityValidationSystem>();

            var continuityState = entityManager.GetComponentData<RegistryContinuityState>(syncEntity);
            Assert.AreEqual(0, continuityState.WarningCount);
            Assert.AreEqual(1, continuityState.FailureCount);

            var alerts = entityManager.GetBuffer<RegistryContinuityAlert>(syncEntity);
            Assert.AreEqual(1, alerts.Length);
            var alert = alerts[0];
            Assert.AreEqual(RegistryContinuityStatus.Failure, alert.Status);
            Assert.AreEqual(8u, alert.Delta);
            Assert.AreNotEqual(0, alert.Flags & RegistryHealthFlags.SpatialMismatchCritical);
        }

        [Test]
        public void ContinuityValidation_FailsWhenSpatialSnapshotMissing()
        {
            using var world = new World("ContinuityMissingTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 42;
            entityManager.SetComponentData(timeEntity, timeState);

            var syncEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistrySpatialSyncState>()).GetSingletonEntity();
            entityManager.SetComponentData(syncEntity, new RegistrySpatialSyncState
            {
                SpatialVersion = 9u,
                LastPublishedTick = timeState.Tick,
                HasSpatialDataFlag = 1
            });

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            metadata.MarkUpdated(0, timeState.Tick, RegistryContinuitySnapshot.WithoutSpatialData(requireSync: true));
            entityManager.SetComponentData(registryEntity, metadata);

            world.UpdateSystem<RegistryDirectorySystem>();
            world.UpdateSystem<RegistryContinuityValidationSystem>();

            var continuityState = entityManager.GetComponentData<RegistryContinuityState>(syncEntity);
            Assert.AreEqual(0, continuityState.WarningCount);
            Assert.AreEqual(1, continuityState.FailureCount);

            var alerts = entityManager.GetBuffer<RegistryContinuityAlert>(syncEntity);
            Assert.AreEqual(1, alerts.Length);
            var alert = alerts[0];
            Assert.AreEqual(RegistryContinuityStatus.Failure, alert.Status);
            Assert.AreNotEqual(0, alert.Flags & RegistryHealthFlags.SpatialContinuityMissing);
        }

        [Test]
        public void ContinuityValidation_EvaluatesAllSpatialRegistries()
        {
            using var world = new World("ContinuityAllRegistriesTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 1000;
            entityManager.SetComponentData(timeEntity, timeState);

            var thresholdsEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryHealthMonitoring>()).GetSingletonEntity();
            var thresholds = entityManager.GetComponentData<RegistryHealthThresholds>(thresholdsEntity);
            thresholds.SpatialVersionMismatchWarning = 1;
            thresholds.SpatialVersionMismatchCritical = 2;
            entityManager.SetComponentData(thresholdsEntity, thresholds);

            var syncEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistrySpatialSyncState>()).GetSingletonEntity();
            entityManager.SetComponentData(syncEntity, new RegistrySpatialSyncState
            {
                SpatialVersion = 50u,
                LastPublishedTick = timeState.Tick,
                HasSpatialDataFlag = 1
            });

            var directoryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryDirectory>()).GetSingletonEntity();
            var directory = entityManager.GetBuffer<RegistryDirectoryEntry>(directoryEntity);

            var targetedRegistries = 0;
            for (var i = 0; i < directory.Length; i++)
            {
                var registryEntity = directory[i].Handle.RegistryEntity;
                if (!entityManager.HasComponent<RegistryMetadata>(registryEntity))
                {
                    continue;
                }

                var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
                if (!metadata.SupportsSpatialQueries)
                {
                    continue;
                }

                metadata.MarkUpdated(
                    entryCount: metadata.EntryCount,
                    tick: timeState.Tick,
                    continuity: RegistryContinuitySnapshot.WithSpatialData(10u, requireSync: true));
                entityManager.SetComponentData(registryEntity, metadata);
                targetedRegistries++;
            }

            Assert.Greater(targetedRegistries, 0, "Expected at least one registry that requires spatial continuity.");

            world.UpdateSystem<RegistryDirectorySystem>();
            world.UpdateSystem<RegistryContinuityValidationSystem>();

            var continuityState = entityManager.GetComponentData<RegistryContinuityState>(syncEntity);
            var alerts = entityManager.GetBuffer<RegistryContinuityAlert>(syncEntity);

            Assert.AreEqual(targetedRegistries, alerts.Length, "Each spatial registry should produce a continuity alert when drifting.");
            Assert.AreEqual(0, continuityState.WarningCount, "Drift this large should register as failures, not warnings.");
            Assert.AreEqual(targetedRegistries, continuityState.FailureCount, "Every spatial registry was forced to drift, so all should fail continuity checks.");
        }
    }
}
