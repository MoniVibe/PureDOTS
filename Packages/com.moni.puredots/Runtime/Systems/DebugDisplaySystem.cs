using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Pooling;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Streaming;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Updates debug display singleton with current simulation state.
    /// Runs in presentation group to provide data for UI layers.
    /// Deterministic and Burst-safe.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct DebugDisplaySystem : ISystem
    {
        private EntityQuery _villagerQuery;
        private EntityQuery _storehouseQuery;
        private EntityQuery _sunlightQuery;
        private ComponentLookup<RegistryMetadata> _registryMetadataLookup;
        private BufferLookup<RegistryDirectoryEntry> _registryDirectoryLookup;
        private ComponentLookup<RegistryHealth> _registryHealthLookup;
        private ComponentLookup<RegistryInstrumentationState> _registryInstrumentationLookup;
        private ComponentLookup<RegistryContinuityState> _registryContinuityLookup;
        private BufferLookup<RegistryContinuityAlert> _registryContinuityAlertLookup;
        private BufferLookup<SunlightGridRuntimeSample> _sunlightRuntimeLookup;

        public void OnCreate(ref SystemState state)
        {
            // Create singleton if it doesn't exist
            if (!SystemAPI.HasSingleton<DebugDisplayData>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<DebugDisplayData>(entity);
            }

            // Cache queries for performance
            _villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<VillagerId>()
                .Build();

            _storehouseQuery = SystemAPI.QueryBuilder()
                .WithAll<StorehouseInventory>()
                .Build();

            _sunlightQuery = SystemAPI.QueryBuilder()
                .WithAll<SunlightGrid>()
                .Build();

            _registryMetadataLookup = state.GetComponentLookup<RegistryMetadata>(isReadOnly: true);
            _registryDirectoryLookup = state.GetBufferLookup<RegistryDirectoryEntry>(isReadOnly: true);
            _registryHealthLookup = state.GetComponentLookup<RegistryHealth>(isReadOnly: true);
            _registryInstrumentationLookup = state.GetComponentLookup<RegistryInstrumentationState>(isReadOnly: true);
            _registryContinuityLookup = state.GetComponentLookup<RegistryContinuityState>(isReadOnly: true);
            _registryContinuityAlertLookup = state.GetBufferLookup<RegistryContinuityAlert>(isReadOnly: true);
            _sunlightRuntimeLookup = state.GetBufferLookup<SunlightGridRuntimeSample>(isReadOnly: true);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<DebugDisplayData>())
            {
                return;
            }

            var debugData = SystemAPI.GetSingletonRW<DebugDisplayData>();
            _registryMetadataLookup.Update(ref state);
            _registryDirectoryLookup.Update(ref state);
            _registryHealthLookup.Update(ref state);
            _registryInstrumentationLookup.Update(ref state);
            _registryContinuityLookup.Update(ref state);
            _registryContinuityAlertLookup.Update(ref state);

            // Update time state
            if (SystemAPI.HasSingleton<TimeState>())
            {
                var timeState = SystemAPI.GetSingleton<TimeState>();
                debugData.ValueRW.CurrentTick = timeState.Tick;
                debugData.ValueRW.IsPaused = timeState.IsPaused;

                var text = new FixedString128Bytes();
                text.Append("Tick: ");
                text.Append(timeState.Tick);
                text.Append(" | Speed: ");
                var speedRounded = math.round(timeState.CurrentSpeedMultiplier * 100f) / 100f;
                text.Append(speedRounded);
                text.Append(" | ");
                text.Append(timeState.IsPaused ? "Paused" : "Running");
                debugData.ValueRW.TimeStateText = text;
            }

            // Update rewind state
            if (SystemAPI.HasSingleton<RewindState>())
            {
                var rewindState = SystemAPI.GetSingleton<RewindState>();

                var text = new FixedString128Bytes();
                text.Append("Mode: ");
                switch (rewindState.Mode)
                {
                    case RewindMode.Record:
                        text.Append("Record");
                        break;
                    case RewindMode.Playback:
                        text.Append("Playback");
                        break;
                    case RewindMode.CatchUp:
                        text.Append("CatchUp");
                        break;
                    default:
                        text.Append("Unknown");
                        break;
                }
                text.Append(" | Playback Tick: ");
                text.Append(rewindState.PlaybackTick);
                debugData.ValueRW.RewindStateText = text;
            }

            debugData.ValueRW.VillagerCount = _villagerQuery.CalculateEntityCount();

            float totalStored = 0f;
            foreach (var inventory in SystemAPI.Query<RefRO<StorehouseInventory>>())
            {
                totalStored += inventory.ValueRO.TotalStored;
            }
            debugData.ValueRW.TotalResourcesStored = totalStored;

            UpdateRegistryDiagnostics(ref state, ref debugData.ValueRW);
            UpdatePoolingDiagnostics(ref debugData.ValueRW);
            UpdateSpatialDiagnostics(ref state, ref debugData.ValueRW);
            UpdateSunlightDiagnostics(ref state, ref debugData.ValueRW);
            UpdateStreamingDiagnostics(ref state, ref debugData.ValueRW);
            UpdateFrameTiming(ref state, ref debugData.ValueRW);
            UpdateReplayDiagnostics(ref state, ref debugData.ValueRW);
            WriteTelemetrySnapshot(ref state, in debugData.ValueRO);
        }

        private void UpdateRegistryDiagnostics(ref SystemState state, ref DebugDisplayData debugData)
        {
            debugData.RegisteredRegistryCount = 0;
            debugData.RegisteredEntryCount = 0;
            debugData.RegistryDirectoryVersion = 0;
            debugData.RegistryDirectoryLastUpdateTick = 0;
            debugData.RegistryDirectoryAggregateHash = 0;
            debugData.RegistryStateText = default;
            debugData.RegistryHealthHeadline = default;
            debugData.RegistryHealthAlerts = default;
            debugData.RegistryWorstHealthLevel = (byte)RegistryHealthLevel.Healthy;
            debugData.RegistryHealthyCount = 0;
            debugData.RegistryWarningCount = 0;
            debugData.RegistryCriticalCount = 0;
            debugData.RegistryFailureCount = 0;
            debugData.RegistryInstrumentationVersion = 0;
            debugData.RegistryContinuityVersion = 0;
            debugData.RegistryContinuityWarningCount = 0;
            debugData.RegistryContinuityFailureCount = 0;
            debugData.RegistryContinuityAlerts = default;
            debugData.RegistryHasAlerts = false;
            debugData.ResourceSpatialResolved = 0;
            debugData.ResourceSpatialFallback = 0;
            debugData.ResourceSpatialUnmapped = 0;
            debugData.StorehouseSpatialResolved = 0;
            debugData.StorehouseSpatialFallback = 0;
            debugData.StorehouseSpatialUnmapped = 0;
            debugData.BandRegistryCount = 0;
            debugData.BandEntryCount = 0;
            debugData.BandTotalMembers = 0;
            debugData.BandSpatialResolved = 0;
            debugData.BandSpatialFallback = 0;
            debugData.BandSpatialUnmapped = 0;

            if (!SystemAPI.TryGetSingletonEntity<RegistryDirectory>(out var registryEntity))
            {
                return;
            }

            var directory = SystemAPI.GetComponentRO<RegistryDirectory>(registryEntity).ValueRO;
            debugData.RegistryDirectoryVersion = directory.Version;
            debugData.RegistryDirectoryLastUpdateTick = directory.LastUpdateTick;
            debugData.RegistryDirectoryAggregateHash = directory.AggregateHash;

            if (!_registryDirectoryLookup.HasBuffer(registryEntity))
            {
                var emptyText = new FixedString128Bytes();
                emptyText.Append("Registries: 0");
                debugData.RegistryStateText = emptyText;
                return;
            }

            var entries = _registryDirectoryLookup[registryEntity];
            var hasInstrumentation = _registryInstrumentationLookup.HasComponent(registryEntity);
            var instrumentationState = hasInstrumentation ? _registryInstrumentationLookup[registryEntity] : default;

            int resourceRegistries = 0;
            int resourceEntries = 0;
            int storehouseRegistries = 0;
            int storehouseEntries = 0;
            int villagerRegistries = 0;
            int villagerEntries = 0;
            int bandRegistries = 0;
            int bandEntries = 0;
            int healthyCount = 0;
            int warningCount = 0;
            int criticalCount = 0;
            var worstLevelValue = (int)RegistryHealthLevel.Healthy;
            var alerts = new FixedString512Bytes();

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                debugData.RegisteredRegistryCount++;

                if (_registryMetadataLookup.HasComponent(entry.Handle.RegistryEntity))
                {
                    var metadata = _registryMetadataLookup[entry.Handle.RegistryEntity];
                    debugData.RegisteredEntryCount += metadata.EntryCount;

                    if (_registryHealthLookup.HasComponent(entry.Handle.RegistryEntity))
                    {
                        var health = _registryHealthLookup[entry.Handle.RegistryEntity];
                        UpdateHealthCounters(ref healthyCount, ref warningCount, ref criticalCount, ref worstLevelValue, ref alerts, metadata.Label, health);
                    }
                    else
                    {
                        healthyCount++;
                    }

                    switch (entry.Kind)
                    {
                        case RegistryKind.Resource:
                            resourceRegistries++;
                            resourceEntries += metadata.EntryCount;
                            break;
                        case RegistryKind.Storehouse:
                            storehouseRegistries++;
                            storehouseEntries += metadata.EntryCount;
                            break;
                        case RegistryKind.Villager:
                            villagerRegistries++;
                            villagerEntries += metadata.EntryCount;
                            break;
                        case RegistryKind.Band:
                            bandRegistries++;
                            bandEntries += metadata.EntryCount;
                            break;
                    }
                }
                else
                {
                    healthyCount++;
                }
            }

            debugData.BandRegistryCount = bandRegistries;
            debugData.BandEntryCount = bandEntries;

            var displayHealthy = healthyCount;
            var displayWarning = warningCount;
            var displayCritical = criticalCount;
            var displayFailure = 0;

            if (hasInstrumentation)
            {
                displayHealthy = instrumentationState.HealthyCount;
                displayWarning = instrumentationState.WarningCount;
                displayCritical = instrumentationState.CriticalCount;
                displayFailure = instrumentationState.FailureCount;
            }

            var text = new FixedString128Bytes();
            text.Append("Registries: ");
            text.Append(debugData.RegisteredRegistryCount);
            text.Append(" | Entries: ");
            text.Append(debugData.RegisteredEntryCount);

            if (resourceRegistries > 0)
            {
                text.Append(" | Res ");
                text.Append(resourceRegistries);
                text.Append("/");
                text.Append(resourceEntries);
            }

            if (storehouseRegistries > 0)
            {
                text.Append(" | Store ");
                text.Append(storehouseRegistries);
                text.Append("/");
                text.Append(storehouseEntries);
            }

            if (villagerRegistries > 0)
            {
                text.Append(" | Vill ");
                text.Append(villagerRegistries);
                text.Append("/");
                text.Append(villagerEntries);
            }

            if (bandRegistries > 0)
            {
                text.Append(" | Band ");
                text.Append(bandRegistries);
                text.Append("/");
                text.Append(bandEntries);
            }

            text.Append(" | Health ok=");
            text.Append(displayHealthy);
            text.Append(" warn=");
            text.Append(displayWarning);
            text.Append(" crit=");
            text.Append(displayCritical);

            if (displayFailure > 0)
            {
                text.Append(" fail=");
                text.Append(displayFailure);
            }

            if (SystemAPI.TryGetSingleton<ResourceRegistry>(out var resourceRegistry))
            {
                debugData.ResourceSpatialResolved = resourceRegistry.SpatialResolvedCount;
                debugData.ResourceSpatialFallback = resourceRegistry.SpatialFallbackCount;
                debugData.ResourceSpatialUnmapped = resourceRegistry.SpatialUnmappedCount;

                if (resourceRegistry.TotalResources > 0)
                {
                    text.Append(" | ResAlign ");
                    text.Append(resourceRegistry.SpatialResolvedCount);
                    text.Append("/");
                    text.Append(resourceRegistry.TotalResources);

                    if (resourceRegistry.SpatialFallbackCount > 0)
                    {
                        text.Append(" f=");
                        text.Append(resourceRegistry.SpatialFallbackCount);
                    }

                    if (resourceRegistry.SpatialUnmappedCount > 0)
                    {
                        text.Append(" u=");
                        text.Append(resourceRegistry.SpatialUnmappedCount);
                    }
                }
            }

            if (SystemAPI.TryGetSingleton<BandRegistry>(out var bandRegistry))
            {
                debugData.BandTotalMembers = bandRegistry.TotalMembers;
                debugData.BandSpatialResolved = bandRegistry.SpatialResolvedCount;
                debugData.BandSpatialFallback = bandRegistry.SpatialFallbackCount;
                debugData.BandSpatialUnmapped = bandRegistry.SpatialUnmappedCount;

                if (bandRegistry.TotalBands > 0)
                {
                    if (bandRegistries == 0)
                    {
                        debugData.BandRegistryCount = math.max(debugData.BandRegistryCount, 1);
                        debugData.BandEntryCount = math.max(debugData.BandEntryCount, bandRegistry.TotalBands);
                        text.Append(" | Band ");
                        text.Append(debugData.BandRegistryCount);
                        text.Append("/");
                        text.Append(debugData.BandEntryCount);
                    }

                    text.Append(" | BandMembers ");
                    text.Append(bandRegistry.TotalMembers);
                    text.Append("/");
                    text.Append(bandRegistry.TotalBands);

                    if (bandRegistry.SpatialFallbackCount > 0)
                    {
                        text.Append(" f=");
                        text.Append(bandRegistry.SpatialFallbackCount);
                    }

                    if (bandRegistry.SpatialUnmappedCount > 0)
                    {
                        text.Append(" u=");
                        text.Append(bandRegistry.SpatialUnmappedCount);
                    }
                }
            }

            if (SystemAPI.TryGetSingleton<StorehouseRegistry>(out var storehouseRegistry))
            {
                debugData.StorehouseSpatialResolved = storehouseRegistry.SpatialResolvedCount;
                debugData.StorehouseSpatialFallback = storehouseRegistry.SpatialFallbackCount;
                debugData.StorehouseSpatialUnmapped = storehouseRegistry.SpatialUnmappedCount;

                if (storehouseRegistry.TotalStorehouses > 0)
                {
                    text.Append(" | StoreAlign ");
                    text.Append(storehouseRegistry.SpatialResolvedCount);
                    text.Append("/");
                    text.Append(storehouseRegistry.TotalStorehouses);

                    if (storehouseRegistry.SpatialFallbackCount > 0)
                    {
                        text.Append(" f=");
                        text.Append(storehouseRegistry.SpatialFallbackCount);
                    }

                    if (storehouseRegistry.SpatialUnmappedCount > 0)
                    {
                        text.Append(" u=");
                        text.Append(storehouseRegistry.SpatialUnmappedCount);
                    }
                }
            }

            var worstLevel = (RegistryHealthLevel)worstLevelValue;
            if (hasInstrumentation)
            {
                if (instrumentationState.FailureCount > 0)
                {
                    worstLevel = RegistryHealthLevel.Failure;
                }
                else if (instrumentationState.CriticalCount > 0)
                {
                    worstLevel = RegistryHealthLevel.Critical;
                }
                else if (instrumentationState.WarningCount > 0)
                {
                    worstLevel = RegistryHealthLevel.Warning;
                }
                else
                {
                    worstLevel = RegistryHealthLevel.Healthy;
                }
            }

            FixedString512Bytes continuityAlerts = default;
            var continuityWarningCount = 0;
            var continuityFailureCount = 0;
            uint continuityVersion = 0;

            if (SystemAPI.TryGetSingletonEntity<RegistrySpatialSyncState>(out var syncEntity))
            {
                if (_registryContinuityLookup.HasComponent(syncEntity))
                {
                    var continuity = _registryContinuityLookup[syncEntity];
                    continuityWarningCount = continuity.WarningCount;
                    continuityFailureCount = continuity.FailureCount;
                    continuityVersion = continuity.Version;
                }

                if (_registryContinuityAlertLookup.HasBuffer(syncEntity))
                {
                    var buffer = _registryContinuityAlertLookup[syncEntity];
                    if (buffer.Length > 0)
                    {
                        var syncState = SystemAPI.GetComponentRO<RegistrySpatialSyncState>(syncEntity).ValueRO;
                        continuityAlerts = BuildContinuityAlerts(buffer, syncState.SpatialVersion);
                    }
                }
            }

            if (continuityAlerts.Length > 0)
            {
                if (alerts.Length > 0)
                {
                    alerts.Append(" || ");
                }
                alerts.Append(continuityAlerts);
            }

            debugData.RegistryHealthHeadline = BuildHealthHeadline(displayHealthy, displayWarning, displayCritical + displayFailure, worstLevel);
            debugData.RegistryHealthAlerts = alerts;
            debugData.RegistryHealthyCount = displayHealthy;
            debugData.RegistryWarningCount = displayWarning;
            debugData.RegistryCriticalCount = displayCritical;
            debugData.RegistryFailureCount = displayFailure;
            debugData.RegistryWorstHealthLevel = (byte)worstLevel;
            debugData.RegistryInstrumentationVersion = hasInstrumentation ? instrumentationState.Version : 0u;
            debugData.RegistryContinuityVersion = continuityVersion;
            debugData.RegistryContinuityWarningCount = continuityWarningCount;
            debugData.RegistryContinuityFailureCount = continuityFailureCount;
            debugData.RegistryContinuityAlerts = continuityAlerts;
            debugData.RegistryHasAlerts = displayWarning > 0 || displayCritical > 0 || displayFailure > 0 || continuityWarningCount > 0 || continuityFailureCount > 0;
            debugData.RegistryStateText = text;
        }

        private static void UpdateHealthCounters(ref int healthyCount, ref int warningCount, ref int criticalCount, ref int worstLevelValue, ref FixedString512Bytes alerts, in FixedString64Bytes label, in RegistryHealth health)
        {
            var levelValue = (int)health.HealthLevel;
            worstLevelValue = math.max(worstLevelValue, levelValue);

            switch (health.HealthLevel)
            {
                case RegistryHealthLevel.Healthy:
                    healthyCount++;
                    return;
                case RegistryHealthLevel.Warning:
                    warningCount++;
                    break;
                case RegistryHealthLevel.Critical:
                case RegistryHealthLevel.Failure:
                    criticalCount++;
                    break;
                default:
                    warningCount++;
                    break;
            }

            AppendAlert(ref alerts, label, health);
        }

        private static void AppendAlert(ref FixedString512Bytes alerts, in FixedString64Bytes label, in RegistryHealth health)
        {
            if (alerts.Length > 0)
            {
                alerts.Append(" | ");
            }
            else
            {
                alerts.Append("Alerts: ");
            }

            alerts.Append(label);
            alerts.Append(": ");
            alerts.Append(GetHealthShortLabel(health.HealthLevel));

            if (health.StaleEntryCount > 0)
            {
                alerts.Append(" stale ");
                alerts.Append(health.StaleEntryCount);
                if (health.TotalEntryCount > 0)
                {
                    alerts.Append("/");
                    alerts.Append(health.TotalEntryCount);
                }
            }

            if (health.SpatialVersionDelta > 0)
            {
                alerts.Append(" Δsp=");
                alerts.Append(health.SpatialVersionDelta);
            }

            if (health.TicksSinceLastUpdate > 0)
            {
                alerts.Append(" Δt=");
                alerts.Append(health.TicksSinceLastUpdate);
            }

            if (health.DirectoryVersionDelta > 0)
            {
                alerts.Append(" Δdir=");
                alerts.Append(health.DirectoryVersionDelta);
            }
        }

        private static FixedString512Bytes BuildContinuityAlerts(DynamicBuffer<RegistryContinuityAlert> alerts, uint spatialVersion)
        {
            var text = new FixedString512Bytes();
            if (alerts.Length == 0)
            {
                return text;
            }

            text.Append("Continuity v");
            text.Append(spatialVersion);

            for (var i = 0; i < alerts.Length; i++)
            {
                var alert = alerts[i];
                text.Append(" | ");
                text.Append(alert.Label);
                text.Append(": ");
                text.Append(alert.Status == RegistryContinuityStatus.Failure ? "fail" : "warn");

                if (alert.Delta > 0)
                {
                    text.Append(" Δ=");
                    text.Append(alert.Delta);
                }

                if (alert.RegistrySpatialVersion > 0)
                {
                    text.Append(" reg=");
                    text.Append(alert.RegistrySpatialVersion);
                }

                if ((alert.Flags & RegistryHealthFlags.SpatialContinuityMissing) != 0)
                {
                    text.Append(" missing");
                }
            }

            return text;
        }

        private static FixedString512Bytes BuildHealthHeadline(int healthyCount, int warningCount, int criticalCount, RegistryHealthLevel worstLevel)
        {
            var text = new FixedString512Bytes();
            text.Append("Worst=");
            text.Append(GetHealthShortLabel(worstLevel));
            text.Append(" | ok=");
            text.Append(healthyCount);
            text.Append(" warn=");
            text.Append(warningCount);
            text.Append(" crit=");
            text.Append(criticalCount);
            return text;
        }

        private static FixedString32Bytes GetHealthShortLabel(RegistryHealthLevel level)
        {
            var label = new FixedString32Bytes();
            switch (level)
            {
                case RegistryHealthLevel.Healthy:
                    label.Append("ok");
                    break;
                case RegistryHealthLevel.Warning:
                    label.Append("warn");
                    break;
                case RegistryHealthLevel.Critical:
                    label.Append("crit");
                    break;
                case RegistryHealthLevel.Failure:
                    label.Append("fail");
                    break;
                default:
                    label.Append((int)level);
                    break;
            }

            return label;
        }

        private void UpdatePoolingDiagnostics(ref DebugDisplayData debugData)
        {
            debugData.PoolingActive = NxPoolingRuntime.IsInitialised;
            debugData.PoolingSnapshot = NxPoolingRuntime.GatherDiagnostics();

            var text = new FixedString128Bytes();

            if (!debugData.PoolingActive)
            {
                text.Append("Pooling: inactive");
                debugData.PoolingStateText = text;
                return;
            }

            ref var pooling = ref debugData.PoolingSnapshot;

            text.Append("ECB ");
            text.Append(pooling.CommandBuffersBorrowed);
            text.Append("/");
            text.Append(pooling.CommandBuffersBorrowed + pooling.CommandBuffersAvailable);

            text.Append(" | NLists ");
            text.Append(pooling.NativeListsBorrowed);
            text.Append("/");
            text.Append(pooling.NativeListsBorrowed + pooling.NativeListsAvailable);

            text.Append(" | NQueues ");
            text.Append(pooling.NativeQueuesBorrowed);
            text.Append("/");
            text.Append(pooling.NativeQueuesBorrowed + pooling.NativeQueuesAvailable);

            debugData.PoolingStateText = text;
        }

        private void UpdateSpatialDiagnostics(ref SystemState state, ref DebugDisplayData debugData)
        {
            debugData.SpatialCellCount = 0;
            debugData.SpatialIndexedEntityCount = 0;
            debugData.SpatialVersion = 0;
            debugData.SpatialLastUpdateTick = 0;
            debugData.SpatialStateText = default;
            debugData.SpatialDirtyAddCount = 0;
            debugData.SpatialDirtyUpdateCount = 0;
            debugData.SpatialDirtyRemoveCount = 0;
            debugData.SpatialLastRebuildMilliseconds = 0f;
            debugData.SpatialLastStrategy = SpatialGridRebuildStrategy.None;

            if (!SystemAPI.TryGetSingletonEntity<SpatialGridConfig>(out var gridEntity))
            {
                return;
            }

            var config = SystemAPI.GetComponentRO<SpatialGridConfig>(gridEntity).ValueRO;
            var gridState = SystemAPI.GetComponentRO<SpatialGridState>(gridEntity).ValueRO;

            debugData.SpatialCellCount = config.CellCount;
            debugData.SpatialIndexedEntityCount = gridState.TotalEntries;
            debugData.SpatialVersion = gridState.Version;
            debugData.SpatialLastUpdateTick = gridState.LastUpdateTick;
            debugData.SpatialDirtyAddCount = gridState.DirtyAddCount;
            debugData.SpatialDirtyUpdateCount = gridState.DirtyUpdateCount;
            debugData.SpatialDirtyRemoveCount = gridState.DirtyRemoveCount;
            debugData.SpatialLastRebuildMilliseconds = gridState.LastRebuildMilliseconds;
            debugData.SpatialLastStrategy = gridState.LastStrategy;

            var text = new FixedString128Bytes();
            text.Append("Spatial Cells: ");
            text.Append(config.CellCount);
            text.Append(" | Entries: ");
            text.Append(gridState.TotalEntries);
            text.Append(" | Version: ");
            text.Append(gridState.Version);
            text.Append(" @ Tick ");
            text.Append(gridState.LastUpdateTick);

            if (config.CellCount > 0)
            {
                var average = (float)gridState.TotalEntries / config.CellCount;
                text.Append(" | Avg/Cell: ");
                var rounded = math.round(average * 100f) / 100f;
                text.Append(rounded);
            }

            text.Append(" | Dirty +/");
            text.Append(gridState.DirtyAddCount);
            text.Append(",~/");
            text.Append(gridState.DirtyUpdateCount);
            text.Append(",-/");
            text.Append(gridState.DirtyRemoveCount);

            text.Append(" | Strategy: ");
            text.Append(gridState.LastStrategy.ToString());

            if (gridState.LastRebuildMilliseconds > 0f)
            {
                var rebuildRounded = math.round(gridState.LastRebuildMilliseconds * 100f) / 100f;
                text.Append(" | RebuildMs: ");
                text.Append(rebuildRounded);
            }

            debugData.SpatialStateText = text;
        }

        private void UpdateSunlightDiagnostics(ref SystemState state, ref DebugDisplayData debugData)
        {
            debugData.SunlightStateText = default;
            debugData.SunlightDirectAverage = 0f;
            debugData.SunlightAmbientAverage = 0f;
            debugData.SunlightIntensity = 0f;
            debugData.SunlightDirection = float3.zero;
            debugData.SunlightMaxOccluders = 0;
            debugData.SunlightLastUpdateTick = 0;

            if (_sunlightQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var sunlightEntity = _sunlightQuery.GetSingletonEntity();
            var sunlightGrid = SystemAPI.GetComponentRO<SunlightGrid>(sunlightEntity).ValueRO;

            debugData.SunlightIntensity = sunlightGrid.SunIntensity;
            debugData.SunlightDirection = sunlightGrid.SunDirection;
            debugData.SunlightLastUpdateTick = sunlightGrid.LastUpdateTick;

            float directSum = 0f;
            float ambientSum = 0f;
            ushort maxOccluders = 0;
            int sampleCount = 0;

            _sunlightRuntimeLookup.Update(ref state);

            if (_sunlightRuntimeLookup.TryGetBuffer(sunlightEntity, out var runtimeSamples) && runtimeSamples.Length > 0)
            {
                sampleCount = runtimeSamples.Length;
                for (var i = 0; i < runtimeSamples.Length; i++)
                {
                    var sample = runtimeSamples[i].Value;
                    directSum += sample.DirectLight;
                    ambientSum += sample.AmbientLight;
                    if (sample.OccluderCount > maxOccluders)
                    {
                        maxOccluders = sample.OccluderCount;
                    }
                }
            }
            else if (sunlightGrid.Blob.IsCreated)
            {
                ref var blobSamples = ref sunlightGrid.Blob.Value.Samples;
                sampleCount = blobSamples.Length;
                for (var i = 0; i < blobSamples.Length; i++)
                {
                    var sample = blobSamples[i];
                    directSum += sample.DirectLight;
                    ambientSum += sample.AmbientLight;
                    if (sample.OccluderCount > maxOccluders)
                    {
                        maxOccluders = sample.OccluderCount;
                    }
                }
            }

            if (sampleCount > 0)
            {
                debugData.SunlightDirectAverage = directSum / sampleCount;
                debugData.SunlightAmbientAverage = ambientSum / sampleCount;
                debugData.SunlightMaxOccluders = maxOccluders;
            }

            var directRounded = math.round(debugData.SunlightDirectAverage * 10f) * 0.1f;
            var ambientRounded = math.round(debugData.SunlightAmbientAverage * 10f) * 0.1f;
            var intensityRounded = math.round(debugData.SunlightIntensity * 100f) * 0.01f;
            var dirRounded = math.round(debugData.SunlightDirection * 100f) * 0.01f;

            var text = new FixedString128Bytes();
            text.Append("Sun ");
            text.Append(directRounded);
            text.Append("/");
            text.Append(ambientRounded);
            text.Append(" occ=");
            text.Append(debugData.SunlightMaxOccluders);
            text.Append(" I=");
            text.Append(intensityRounded);
            text.Append(" dir=");
            text.Append(dirRounded.x);
            text.Append(",");
            text.Append(dirRounded.y);
            text.Append(",");
            text.Append(dirRounded.z);
            debugData.SunlightStateText = text;
        }

        private void UpdateStreamingDiagnostics(ref SystemState state, ref DebugDisplayData debugData)
        {
            debugData.StreamingDesiredCount = 0;
            debugData.StreamingLoadedCount = 0;
            debugData.StreamingLoadingCount = 0;
            debugData.StreamingQueuedLoads = 0;
            debugData.StreamingQueuedUnloads = 0;
            debugData.StreamingPendingCommands = 0;
            debugData.StreamingActiveCooldowns = 0;
            debugData.StreamingFirstLoadTick = StreamingStatistics.TickUnset;
            debugData.StreamingFirstUnloadTick = StreamingStatistics.TickUnset;
            debugData.StreamingStateText = default;

            if (!SystemAPI.TryGetSingletonEntity<StreamingCoordinator>(out var coordinatorEntity))
            {
                return;
            }

            if (!state.EntityManager.HasComponent<StreamingStatistics>(coordinatorEntity))
            {
                return;
            }

            var stats = SystemAPI.GetComponentRO<StreamingStatistics>(coordinatorEntity).ValueRO;

            debugData.StreamingDesiredCount = stats.DesiredCount;
            debugData.StreamingLoadedCount = stats.LoadedCount;
            debugData.StreamingLoadingCount = stats.LoadingCount;
            debugData.StreamingQueuedLoads = stats.QueuedLoads;
            debugData.StreamingQueuedUnloads = stats.QueuedUnloads;
            debugData.StreamingPendingCommands = stats.PendingCommands;
            debugData.StreamingActiveCooldowns = stats.ActiveCooldowns;
            debugData.StreamingFirstLoadTick = stats.FirstLoadTick;
            debugData.StreamingFirstUnloadTick = stats.FirstUnloadTick;

            var text = new FixedString128Bytes();
            text.Append("Streaming D:");
            text.Append(stats.DesiredCount);
            text.Append(" Ld:");
            text.Append(stats.LoadedCount);
            text.Append(" Lg:");
            text.Append(stats.LoadingCount);
            text.Append(" QL:");
            text.Append(stats.QueuedLoads);
            text.Append(" QU:");
            text.Append(stats.QueuedUnloads);
            text.Append(" P:");
            text.Append(stats.PendingCommands);
            text.Append(" CD:");
            text.Append(stats.ActiveCooldowns);

            if (stats.FirstLoadTick != StreamingStatistics.TickUnset)
            {
                text.Append(" | FirstLoad:");
                text.Append(stats.FirstLoadTick);
            }

            if (stats.FirstUnloadTick != StreamingStatistics.TickUnset)
            {
                text.Append(" | FirstUnload:");
                text.Append(stats.FirstUnloadTick);
            }

            debugData.StreamingStateText = text;
        }

        private void UpdateFrameTiming(ref SystemState state, ref DebugDisplayData debugData)
        {
            debugData.FrameTimingText = default;
            debugData.FrameTimingSampleCount = 0;
            debugData.FrameTimingWorstDurationMs = 0f;
            debugData.FrameTimingWorstGroup = default;
            debugData.FrameTimingBudgetExceeded = false;
            debugData.AllocationStateText = default;
            debugData.GcCollectionsGeneration0 = 0;
            debugData.GcCollectionsGeneration1 = 0;
            debugData.GcCollectionsGeneration2 = 0;

            if (!SystemAPI.TryGetSingletonEntity<FrameTimingStream>(out var frameEntity))
            {
                return;
            }

            var samples = state.EntityManager.GetBuffer<FrameTimingSample>(frameEntity);
            var text = new FixedString128Bytes();

            for (int i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                if (sample.DurationMs <= 0f && sample.SystemCount == 0)
                {
                    continue;
                }

                if (debugData.FrameTimingSampleCount > 0)
                {
                    text.Append(" | ");
                }

                var label = FrameTimingRecorderSystem.GetGroupLabel(sample.Group);
                text.Append(label);
                text.Append(" ");
                var durationRounded = math.round(sample.DurationMs * 100f) / 100f;
                text.Append(durationRounded);
                text.Append("ms");

                if (sample.BudgetMs > 0f)
                {
                    text.Append("/");
                    text.Append(sample.BudgetMs);
                    text.Append("ms");
                }

                if ((sample.Flags & FrameTimingFlags.BudgetExceeded) != 0)
                {
                    text.Append("!");
                    debugData.FrameTimingBudgetExceeded = true;
                }

                if ((sample.Flags & FrameTimingFlags.CatchUp) != 0)
                {
                    text.Append("*");
                }

                debugData.FrameTimingSampleCount++;

                if (sample.DurationMs > debugData.FrameTimingWorstDurationMs)
                {
                    debugData.FrameTimingWorstDurationMs = sample.DurationMs;
                    debugData.FrameTimingWorstGroup = label;
                }
            }

            debugData.FrameTimingText = text;

            var allocation = SystemAPI.GetComponentRO<AllocationDiagnostics>(frameEntity).ValueRO;
            debugData.GcCollectionsGeneration0 = allocation.GcCollectionsGeneration0;
            debugData.GcCollectionsGeneration1 = allocation.GcCollectionsGeneration1;
            debugData.GcCollectionsGeneration2 = allocation.GcCollectionsGeneration2;

            var allocationText = new FixedString128Bytes();
            allocationText.Append("GC ");
            allocationText.Append(allocation.GcCollectionsGeneration0);
            allocationText.Append("/");
            allocationText.Append(allocation.GcCollectionsGeneration1);
            allocationText.Append("/");
            allocationText.Append(allocation.GcCollectionsGeneration2);
            allocationText.Append(" | Mem ");
            allocationText.Append(BytesToMegabytes(allocation.TotalAllocatedBytes));
            allocationText.Append("MB");
            allocationText.Append(" / Res ");
            allocationText.Append(BytesToMegabytes(allocation.TotalReservedBytes));
            allocationText.Append("MB");
            if (allocation.TotalUnusedReservedBytes > 0)
            {
                allocationText.Append(" (");
                allocationText.Append(BytesToMegabytes(allocation.TotalUnusedReservedBytes));
                allocationText.Append("MB unused)");
            }

            debugData.AllocationStateText = allocationText;
        }

        private void UpdateReplayDiagnostics(ref SystemState state, ref DebugDisplayData debugData)
        {
            debugData.ReplayEventCount = 0;
            debugData.ReplayStateText = default;

            if (!SystemAPI.TryGetSingletonEntity<ReplayCaptureStream>(out var replayEntity))
            {
                return;
            }

            var stream = SystemAPI.GetComponent<ReplayCaptureStream>(replayEntity);
            var events = state.EntityManager.GetBuffer<ReplayCaptureEvent>(replayEntity);
            debugData.ReplayEventCount = stream.EventCount;

            if (events.Length == 0)
            {
                if (stream.LastEventLabel.Length > 0)
                {
                    var idleText = new FixedString128Bytes();
                    idleText.Append("Replay last: ");
                    idleText.Append(stream.LastEventLabel);
                    debugData.ReplayStateText = idleText;
                }
                return;
            }

            var latest = events[events.Length - 1];
            var text = new FixedString128Bytes();
            text.Append("Replay ");
            text.Append(events.Length);
            text.Append(" @ ");
            text.Append(latest.Tick);
            text.Append(" ");
            text.Append(ReplayCaptureSystem.GetEventTypeLabel(latest.Type));
            if (latest.Label.Length > 0)
            {
                text.Append(" ");
                text.Append(latest.Label);
            }
            if (math.abs(latest.Value) > 0.0001f)
            {
                text.Append(" (");
                text.Append(latest.Value);
                text.Append(")");
            }

            debugData.ReplayStateText = text;
        }

        private static float BytesToMegabytes(long bytes)
        {
            return bytes <= 0 ? 0f : bytes / (1024f * 1024f);
        }

        private void WriteTelemetrySnapshot(ref SystemState state, in DebugDisplayData debugData)
        {
            if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            {
                return;
            }

            var telemetry = SystemAPI.GetComponentRW<TelemetryStream>(telemetryEntity);
            telemetry.ValueRW.LastTick = debugData.CurrentTick;
            telemetry.ValueRW.Version++;

            if (!state.EntityManager.HasBuffer<TelemetryMetric>(telemetryEntity))
            {
                state.EntityManager.AddBuffer<TelemetryMetric>(telemetryEntity);
            }

            var buffer = state.EntityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
            buffer.Clear();

            FixedString64Bytes key;

            key = "tick.current";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.CurrentTick, Unit = TelemetryMetricUnit.Count });

            key = "registry.count";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.RegisteredRegistryCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.entries";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.RegisteredEntryCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.health.worst";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.RegistryWorstHealthLevel, Unit = TelemetryMetricUnit.Count });

            key = "registry.health.warning";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.RegistryWarningCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.health.critical";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.RegistryCriticalCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.health.failure";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.RegistryFailureCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.continuity.warning";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.RegistryContinuityWarningCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.continuity.failure";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.RegistryContinuityFailureCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.resource.spatial.resolved";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.ResourceSpatialResolved, Unit = TelemetryMetricUnit.Count });

            key = "registry.resource.spatial.fallback";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.ResourceSpatialFallback, Unit = TelemetryMetricUnit.Count });

            key = "registry.resource.spatial.unmapped";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.ResourceSpatialUnmapped, Unit = TelemetryMetricUnit.Count });

            key = "registry.storehouse.spatial.resolved";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StorehouseSpatialResolved, Unit = TelemetryMetricUnit.Count });

            key = "registry.storehouse.spatial.fallback";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StorehouseSpatialFallback, Unit = TelemetryMetricUnit.Count });

            key = "registry.storehouse.spatial.unmapped";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StorehouseSpatialUnmapped, Unit = TelemetryMetricUnit.Count });

            key = "registry.band.count";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.BandRegistryCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.band.entries";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.BandEntryCount, Unit = TelemetryMetricUnit.Count });

            key = "registry.band.members";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.BandTotalMembers, Unit = TelemetryMetricUnit.Count });

            key = "registry.band.spatial.resolved";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.BandSpatialResolved, Unit = TelemetryMetricUnit.Count });

            key = "registry.band.spatial.fallback";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.BandSpatialFallback, Unit = TelemetryMetricUnit.Count });

            key = "registry.band.spatial.unmapped";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.BandSpatialUnmapped, Unit = TelemetryMetricUnit.Count });

            key = "villagers.count";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.VillagerCount, Unit = TelemetryMetricUnit.Count });

            key = "resources.total";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.TotalResourcesStored, Unit = TelemetryMetricUnit.Count });

            key = "spatial.cells";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.SpatialCellCount, Unit = TelemetryMetricUnit.Count });

            key = "spatial.entries";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.SpatialIndexedEntityCount, Unit = TelemetryMetricUnit.Count });

            key = "spatial.version";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.SpatialVersion, Unit = TelemetryMetricUnit.Count });

            key = "spatial.last.tick";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.SpatialLastUpdateTick, Unit = TelemetryMetricUnit.Count });

            key = "streaming.desired";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingDesiredCount, Unit = TelemetryMetricUnit.Count });

            key = "streaming.loaded";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingLoadedCount, Unit = TelemetryMetricUnit.Count });

            key = "streaming.loading";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingLoadingCount, Unit = TelemetryMetricUnit.Count });

            key = "streaming.queued.loads";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingQueuedLoads, Unit = TelemetryMetricUnit.Count });

            key = "streaming.queued.unloads";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingQueuedUnloads, Unit = TelemetryMetricUnit.Count });

            key = "streaming.pending.commands";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingPendingCommands, Unit = TelemetryMetricUnit.Count });

            key = "streaming.cooldowns.active";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingActiveCooldowns, Unit = TelemetryMetricUnit.Count });

            if (debugData.StreamingFirstLoadTick != StreamingStatistics.TickUnset)
            {
                key = "streaming.first.load.tick";
                buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingFirstLoadTick, Unit = TelemetryMetricUnit.Count });
            }

            if (debugData.StreamingFirstUnloadTick != StreamingStatistics.TickUnset)
            {
                key = "streaming.first.unload.tick";
                buffer.Add(new TelemetryMetric { Key = key, Value = debugData.StreamingFirstUnloadTick, Unit = TelemetryMetricUnit.Count });
            }

            key = "sunlight.direct.avg";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.SunlightDirectAverage, Unit = TelemetryMetricUnit.Count });

            key = "sunlight.ambient.avg";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.SunlightAmbientAverage, Unit = TelemetryMetricUnit.Count });

            key = "sunlight.intensity";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.SunlightIntensity, Unit = TelemetryMetricUnit.Count });

            key = "sunlight.occluders.max";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.SunlightMaxOccluders, Unit = TelemetryMetricUnit.Count });

            if (debugData.PoolingActive)
            {
                key = "pooling.ecb.borrowed";
                buffer.Add(new TelemetryMetric { Key = key, Value = debugData.PoolingSnapshot.CommandBuffersBorrowed, Unit = TelemetryMetricUnit.Count });

                key = "pooling.nativelist.borrowed";
                buffer.Add(new TelemetryMetric { Key = key, Value = debugData.PoolingSnapshot.NativeListsBorrowed, Unit = TelemetryMetricUnit.Count });

                key = "pooling.nativequeue.borrowed";
                buffer.Add(new TelemetryMetric { Key = key, Value = debugData.PoolingSnapshot.NativeQueuesBorrowed, Unit = TelemetryMetricUnit.Count });
            }

            if (SystemAPI.TryGetSingletonEntity<FrameTimingStream>(out var frameEntity))
            {
                var samples = state.EntityManager.GetBuffer<FrameTimingSample>(frameEntity);
                for (int i = 0; i < samples.Length; i++)
                {
                    var sample = samples[i];
                    key = FrameTimingUtility.GetMetricKey(sample.Group);
                    buffer.Add(new TelemetryMetric
                    {
                        Key = key,
                        Value = sample.DurationMs,
                        Unit = TelemetryMetricUnit.DurationMilliseconds
                    });
                }

                var allocation = SystemAPI.GetComponent<AllocationDiagnostics>(frameEntity);

                key = "memory.allocated.bytes";
                buffer.Add(new TelemetryMetric { Key = key, Value = (float)allocation.TotalAllocatedBytes, Unit = TelemetryMetricUnit.Bytes });

                key = "memory.reserved.bytes";
                buffer.Add(new TelemetryMetric { Key = key, Value = (float)allocation.TotalReservedBytes, Unit = TelemetryMetricUnit.Bytes });

                key = "memory.unused.bytes";
                buffer.Add(new TelemetryMetric { Key = key, Value = (float)allocation.TotalUnusedReservedBytes, Unit = TelemetryMetricUnit.Bytes });

                key = "gc.gen0.collections";
                buffer.Add(new TelemetryMetric { Key = key, Value = debugData.GcCollectionsGeneration0, Unit = TelemetryMetricUnit.Count });

                key = "gc.gen1.collections";
                buffer.Add(new TelemetryMetric { Key = key, Value = debugData.GcCollectionsGeneration1, Unit = TelemetryMetricUnit.Count });

                key = "gc.gen2.collections";
                buffer.Add(new TelemetryMetric { Key = key, Value = debugData.GcCollectionsGeneration2, Unit = TelemetryMetricUnit.Count });
            }

            if (SystemAPI.TryGetSingletonEntity<ReplayCaptureStream>(out var replayEntity))
            {
                var replayStream = SystemAPI.GetComponent<ReplayCaptureStream>(replayEntity);
                key = "replay.events";
                buffer.Add(new TelemetryMetric { Key = key, Value = replayStream.EventCount, Unit = TelemetryMetricUnit.Count });
            }
        }
    }
}
