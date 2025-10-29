using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public class RegistryHealthSystemTests
    {
        [Test]
        public void RegistryHealthSystem_FlagsStaleEntriesAsWarning()
        {
            using var world = new World("RegistryHealthWarningTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();

            // Advance simulation tick to control entry age.
            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 1000;
            entityManager.SetComponentData(timeEntity, timeState);

            // Configure monitoring thresholds for easier assertions.
            var monitoringEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryHealthMonitoring>()).GetSingletonEntity();
            var monitoring = entityManager.GetComponentData<RegistryHealthMonitoring>(monitoringEntity);
            monitoring.MinCheckIntervalTicks = 0;
            monitoring.LogWarnings = false;
            monitoring.EmitTelemetry = false;
            entityManager.SetComponentData(monitoringEntity, monitoring);

            var thresholds = entityManager.GetComponentData<RegistryHealthThresholds>(monitoringEntity);
            thresholds.MaxStaleTickAge = 10;
            thresholds.StaleEntryWarningRatio = 0.25f;
            thresholds.StaleEntryCriticalRatio = 0.75f;
            thresholds.SpatialVersionMismatchWarning = 100;
            thresholds.SpatialVersionMismatchCritical = 200;
            thresholds.MinUpdateFrequencyTicks = 0;
            thresholds.DirectoryVersionMismatchWarning = 1000;
            entityManager.SetComponentData(monitoringEntity, thresholds);

            // Populate resource registry with entries, making half of them stale.
            var resourceEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var resourceBuffer = entityManager.GetBuffer<ResourceRegistryEntry>(resourceEntity);
            resourceBuffer.Clear();

            var staleSourceA = entityManager.CreateEntity();
            var staleSourceB = entityManager.CreateEntity();
            var freshSourceA = entityManager.CreateEntity();
            var freshSourceB = entityManager.CreateEntity();

            resourceBuffer.Add(new ResourceRegistryEntry
            {
                ResourceTypeIndex = 0,
                SourceEntity = staleSourceA,
                Position = float3.zero,
                UnitsRemaining = 100f,
                ActiveTickets = 0,
                ClaimFlags = 0,
                LastMutationTick = 980u,
                CellId = 0,
                SpatialVersion = 1u
            });

            resourceBuffer.Add(new ResourceRegistryEntry
            {
                ResourceTypeIndex = 0,
                SourceEntity = staleSourceB,
                Position = new float3(1f, 0f, 0f),
                UnitsRemaining = 80f,
                ActiveTickets = 0,
                ClaimFlags = 0,
                LastMutationTick = 979u,
                CellId = 1,
                SpatialVersion = 1u
            });

            resourceBuffer.Add(new ResourceRegistryEntry
            {
                ResourceTypeIndex = 0,
                SourceEntity = freshSourceA,
                Position = new float3(2f, 0f, 0f),
                UnitsRemaining = 90f,
                ActiveTickets = 0,
                ClaimFlags = 0,
                LastMutationTick = 995u,
                CellId = 2,
                SpatialVersion = 1u
            });

            resourceBuffer.Add(new ResourceRegistryEntry
            {
                ResourceTypeIndex = 0,
                SourceEntity = freshSourceB,
                Position = new float3(3f, 0f, 0f),
                UnitsRemaining = 70f,
                ActiveTickets = 0,
                ClaimFlags = 0,
                LastMutationTick = 996u,
                CellId = 3,
                SpatialVersion = 1u
            });

            var metadata = entityManager.GetComponentData<RegistryMetadata>(resourceEntity);
            metadata.MarkUpdated(resourceBuffer.Length, 990u, RegistryContinuitySnapshot.WithoutSpatialData());
            entityManager.SetComponentData(resourceEntity, metadata);

            var resourceRegistry = entityManager.GetComponentData<ResourceRegistry>(resourceEntity);
            resourceRegistry.TotalResources = resourceBuffer.Length;
            resourceRegistry.LastUpdateTick = metadata.LastUpdateTick;
            resourceRegistry.LastSpatialVersion = 1u;
            resourceRegistry.SpatialResolvedCount = resourceBuffer.Length;
            resourceRegistry.SpatialFallbackCount = 0;
            resourceRegistry.SpatialUnmappedCount = 0;
            entityManager.SetComponentData(resourceEntity, resourceRegistry);

            // Ensure directory reflects latest metadata before running health system.
            world.UpdateSystem<RegistryDirectorySystem>();

            world.UpdateSystem<RegistryHealthSystem>();

            var health = entityManager.GetComponentData<RegistryHealth>(resourceEntity);
            Assert.AreEqual(RegistryHealthLevel.Warning, health.HealthLevel, "Expected warning level when 50% of entries are stale.");
            Assert.AreEqual(2, health.StaleEntryCount);
            Assert.AreEqual(0.5f, health.StaleEntryRatio, 0.001f);
            Assert.AreEqual(10u, health.TicksSinceLastUpdate);

            var monitoringAfter = entityManager.GetComponentData<RegistryHealthMonitoring>(monitoringEntity);
            Assert.AreEqual(RegistryHealthLevel.Warning, monitoringAfter.WorstHealthLevel);
            Assert.AreEqual(1, monitoringAfter.UnhealthyRegistryCount);
            Assert.AreEqual(timeState.Tick, monitoringAfter.LastCheckTick);
        }

        [Test]
        public void RegistryHealthSystem_FailsWhenSpatialContinuityMissing()
        {
            using var world = new World("RegistryHealthContinuityFailureTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 120;
            entityManager.SetComponentData(timeEntity, timeState);

            var monitoringEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryHealthMonitoring>()).GetSingletonEntity();
            var monitoring = entityManager.GetComponentData<RegistryHealthMonitoring>(monitoringEntity);
            monitoring.MinCheckIntervalTicks = 0;
            monitoring.LogWarnings = false;
            monitoring.EmitTelemetry = false;
            entityManager.SetComponentData(monitoringEntity, monitoring);

            var thresholds = entityManager.GetComponentData<RegistryHealthThresholds>(monitoringEntity);
            thresholds.MaxStaleTickAge = 1000;
            thresholds.StaleEntryWarningRatio = 1f;
            thresholds.StaleEntryCriticalRatio = 1f;
            thresholds.SpatialVersionMismatchWarning = 100;
            thresholds.SpatialVersionMismatchCritical = 200;
            thresholds.MinUpdateFrequencyTicks = 0;
            thresholds.DirectoryVersionMismatchWarning = 1000;
            entityManager.SetComponentData(monitoringEntity, thresholds);

            var syncEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistrySpatialSyncState>()).GetSingletonEntity();
            entityManager.SetComponentData(syncEntity, new RegistrySpatialSyncState
            {
                SpatialVersion = 5u,
                LastPublishedTick = timeState.Tick - 10u,
                HasSpatialDataFlag = 1
            });

            var resourceEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var metadata = entityManager.GetComponentData<RegistryMetadata>(resourceEntity);
            metadata.MarkUpdated(0, timeState.Tick, RegistryContinuitySnapshot.WithoutSpatialData(requireSync: true));
            entityManager.SetComponentData(resourceEntity, metadata);

            world.UpdateSystem<RegistryDirectorySystem>();
            world.UpdateSystem<RegistryHealthSystem>();

            var health = entityManager.GetComponentData<RegistryHealth>(resourceEntity);
            Assert.AreEqual(RegistryHealthLevel.Failure, health.HealthLevel);
            Assert.AreNotEqual(0, health.FailureFlags & RegistryHealthFlags.SpatialContinuityMissing);
        }

        [Test]
        public void RegistryHealthSystem_ReportsHealthyWhenEntriesFresh()
        {
            using var world = new World("RegistryHealthHealthyTest");
            var entityManager = world.EntityManager;

            world.UpdateSystem<CoreSingletonBootstrapSystem>();

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 200;
            entityManager.SetComponentData(timeEntity, timeState);

            var monitoringEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryHealthMonitoring>()).GetSingletonEntity();
            var monitoring = entityManager.GetComponentData<RegistryHealthMonitoring>(monitoringEntity);
            monitoring.MinCheckIntervalTicks = 0;
            monitoring.LogWarnings = false;
            monitoring.EmitTelemetry = false;
            entityManager.SetComponentData(monitoringEntity, monitoring);

            var thresholds = entityManager.GetComponentData<RegistryHealthThresholds>(monitoringEntity);
            thresholds.MaxStaleTickAge = 50;
            thresholds.StaleEntryWarningRatio = 0.5f;
            thresholds.StaleEntryCriticalRatio = 0.9f;
            thresholds.MinUpdateFrequencyTicks = 0;
            thresholds.DirectoryVersionMismatchWarning = 1000;
            thresholds.SpatialVersionMismatchWarning = 100;
            thresholds.SpatialVersionMismatchCritical = 200;
            entityManager.SetComponentData(monitoringEntity, thresholds);

            var resourceEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var resourceBuffer = entityManager.GetBuffer<ResourceRegistryEntry>(resourceEntity);
            resourceBuffer.Clear();

            var sourceA = entityManager.CreateEntity();
            var sourceB = entityManager.CreateEntity();

            resourceBuffer.Add(new ResourceRegistryEntry
            {
                ResourceTypeIndex = 0,
                SourceEntity = sourceA,
                Position = float3.zero,
                UnitsRemaining = 25f,
                ActiveTickets = 0,
                ClaimFlags = 0,
                LastMutationTick = 195u,
                CellId = 0,
                SpatialVersion = 5u
            });

            resourceBuffer.Add(new ResourceRegistryEntry
            {
                ResourceTypeIndex = 0,
                SourceEntity = sourceB,
                Position = new float3(1f, 0f, 0f),
                UnitsRemaining = 40f,
                ActiveTickets = 0,
                ClaimFlags = 0,
                LastMutationTick = 198u,
                CellId = 1,
                SpatialVersion = 5u
            });

            var metadata = entityManager.GetComponentData<RegistryMetadata>(resourceEntity);
            metadata.MarkUpdated(resourceBuffer.Length, 199u, RegistryContinuitySnapshot.WithoutSpatialData());
            entityManager.SetComponentData(resourceEntity, metadata);

            var resourceRegistry = entityManager.GetComponentData<ResourceRegistry>(resourceEntity);
            resourceRegistry.TotalResources = resourceBuffer.Length;
            resourceRegistry.LastUpdateTick = metadata.LastUpdateTick;
            resourceRegistry.LastSpatialVersion = 5u;
            resourceRegistry.SpatialResolvedCount = resourceBuffer.Length;
            resourceRegistry.SpatialFallbackCount = 0;
            resourceRegistry.SpatialUnmappedCount = 0;
            entityManager.SetComponentData(resourceEntity, resourceRegistry);

            world.UpdateSystem<RegistryDirectorySystem>();
            world.UpdateSystem<RegistryHealthSystem>();

            var health = entityManager.GetComponentData<RegistryHealth>(resourceEntity);
            Assert.AreEqual(RegistryHealthLevel.Healthy, health.HealthLevel);
            Assert.AreEqual(0, health.StaleEntryCount);
            Assert.AreEqual(0f, health.StaleEntryRatio, 0.001f);

            var monitoringAfter = entityManager.GetComponentData<RegistryHealthMonitoring>(monitoringEntity);
            Assert.AreEqual(RegistryHealthLevel.Healthy, monitoringAfter.WorstHealthLevel);
            Assert.AreEqual(0, monitoringAfter.UnhealthyRegistryCount);
        }
    }
}
