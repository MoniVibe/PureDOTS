using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Validates registry spatial continuity against the published spatial sync state and raises alerts when drift is detected.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(RegistryHealthSystem))]
    public partial struct RegistryContinuityValidationSystem : ISystem
    {
        private ComponentLookup<RegistryMetadata> _metadataLookup;
        private ComponentLookup<RegistryHealth> _healthLookup;
        private BufferLookup<RegistryDirectoryEntry> _directoryLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RegistrySpatialSyncState>();
            state.RequireForUpdate<RegistryDirectory>();

            _metadataLookup = state.GetComponentLookup<RegistryMetadata>(isReadOnly: true);
            _healthLookup = state.GetComponentLookup<RegistryHealth>(isReadOnly: true);
            _directoryLookup = state.GetBufferLookup<RegistryDirectoryEntry>(isReadOnly: true);
        }

        public void OnUpdate(ref SystemState state)
        {
            var syncEntity = SystemAPI.GetSingletonEntity<RegistrySpatialSyncState>();
            var directoryEntity = SystemAPI.GetSingletonEntity<RegistryDirectory>();

            _metadataLookup.Update(ref state);
            _healthLookup.Update(ref state);
            _directoryLookup.Update(ref state);

            var alertsBuffer = state.EntityManager.GetBuffer<RegistryContinuityAlert>(syncEntity);
            var previousAlertCount = alertsBuffer.Length;
            alertsBuffer.Clear();

            ref var continuityState = ref SystemAPI.GetComponentRW<RegistryContinuityState>(syncEntity).ValueRW;
            var previousWarnings = continuityState.WarningCount;
            var previousFailures = continuityState.FailureCount;

            var thresholds = SystemAPI.HasSingleton<RegistryHealthThresholds>()
                ? SystemAPI.GetSingleton<RegistryHealthThresholds>()
                : RegistryHealthThresholds.CreateDefaults();
            var currentTick = SystemAPI.HasSingleton<TimeState>()
                ? SystemAPI.GetSingleton<TimeState>().Tick
                : 0u;

            if (!_directoryLookup.HasBuffer(directoryEntity))
            {
                continuityState.WarningCount = 0;
                continuityState.FailureCount = 0;
                continuityState.LastCheckTick = currentTick;
                if (previousWarnings != 0 || previousFailures != 0 || previousAlertCount != 0)
                {
                    continuityState.Version++;
                }
                return;
            }

            var entries = _directoryLookup[directoryEntity];
            if (entries.Length == 0)
            {
                continuityState.WarningCount = 0;
                continuityState.FailureCount = 0;
                continuityState.LastCheckTick = currentTick;
                if (previousWarnings != 0 || previousFailures != 0 || previousAlertCount != 0)
                {
                    continuityState.Version++;
                }
                return;
            }

            var syncState = SystemAPI.GetComponentRW<RegistrySpatialSyncState>(syncEntity).ValueRO;
            var publishedSpatialVersion = syncState.SpatialVersion;
            var hasSpatialData = syncState.HasSpatialData;

            var warningCount = 0;
            var failureCount = 0;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var registryEntity = entry.Handle.RegistryEntity;
                if (!_metadataLookup.HasComponent(registryEntity))
                {
                    continue;
                }

                var metadata = _metadataLookup[registryEntity];
                var continuity = metadata.Continuity;
                var requireSpatialSync = metadata.SupportsSpatialQueries && continuity.RequiresSpatialSync;
                var hasContinuity = continuity.HasSpatialData;

                if (!requireSpatialSync)
                {
                    continue;
                }

                var registrySpatialVersion = continuity.SpatialVersion;
                var status = RegistryContinuityStatus.Nominal;
                var flags = RegistryHealthFlags.None;
                var delta = 0u;

                if (!hasContinuity)
                {
                    status = RegistryContinuityStatus.Failure;
                    flags |= RegistryHealthFlags.SpatialContinuityMissing;
                }
                else if (!hasSpatialData)
                {
                    status = RegistryContinuityStatus.Warning;
                    flags |= RegistryHealthFlags.SpatialMismatchWarning;
                }
                else
                {
                    delta = publishedSpatialVersion >= registrySpatialVersion
                        ? publishedSpatialVersion - registrySpatialVersion
                        : registrySpatialVersion - publishedSpatialVersion;

                    if (thresholds.SpatialVersionMismatchCritical > 0u &&
                        delta >= thresholds.SpatialVersionMismatchCritical)
                    {
                        status = RegistryContinuityStatus.Failure;
                        flags |= RegistryHealthFlags.SpatialMismatchCritical;
                    }
                    else if (thresholds.SpatialVersionMismatchWarning > 0u &&
                             delta >= thresholds.SpatialVersionMismatchWarning)
                    {
                        status = RegistryContinuityStatus.Warning;
                        flags |= RegistryHealthFlags.SpatialMismatchWarning;
                    }
                }

                if (status == RegistryContinuityStatus.Nominal)
                {
                    continue;
                }

                if (_healthLookup.HasComponent(registryEntity))
                {
                    flags |= _healthLookup[registryEntity].FailureFlags;
                }

                if (status == RegistryContinuityStatus.Failure)
                {
                    failureCount++;
                }
                else
                {
                    warningCount++;
                }

                var handle = entry.Handle.WithVersion(metadata.Version);
                alertsBuffer.Add(new RegistryContinuityAlert
                {
                    Handle = handle,
                    Status = status,
                    SpatialVersion = publishedSpatialVersion,
                    RegistrySpatialVersion = registrySpatialVersion,
                    Delta = delta,
                    Flags = flags,
                    Label = metadata.Label
                });
            }

            continuityState.WarningCount = warningCount;
            continuityState.FailureCount = failureCount;
            continuityState.LastCheckTick = currentTick;

            if (previousAlertCount != alertsBuffer.Length ||
                previousWarnings != warningCount ||
                previousFailures != failureCount)
            {
                continuityState.Version++;
            }
        }
    }
}
