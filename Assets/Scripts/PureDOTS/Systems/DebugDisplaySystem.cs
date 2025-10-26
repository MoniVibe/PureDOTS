using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Pooling;
using PureDOTS.Runtime.Registry;
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
        private ComponentLookup<RegistryMetadata> _registryMetadataLookup;
        private BufferLookup<RegistryDirectoryEntry> _registryDirectoryLookup;

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

            _registryMetadataLookup = state.GetComponentLookup<RegistryMetadata>(isReadOnly: true);
            _registryDirectoryLookup = state.GetBufferLookup<RegistryDirectoryEntry>(isReadOnly: true);
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

            int resourceRegistries = 0;
            int resourceEntries = 0;
            int storehouseRegistries = 0;
            int storehouseEntries = 0;
            int villagerRegistries = 0;
            int villagerEntries = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                debugData.RegisteredRegistryCount++;

                if (_registryMetadataLookup.HasComponent(entry.Handle.RegistryEntity))
                {
                    var metadata = _registryMetadataLookup[entry.Handle.RegistryEntity];
                    debugData.RegisteredEntryCount += metadata.EntryCount;

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
                    }
                }
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

            debugData.RegistryStateText = text;
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

            key = "villagers.count";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.VillagerCount, Unit = TelemetryMetricUnit.Count });

            key = "resources.total";
            buffer.Add(new TelemetryMetric { Key = key, Value = debugData.TotalResourcesStored, Unit = TelemetryMetricUnit.Count });

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
