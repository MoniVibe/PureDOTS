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
    }
}
