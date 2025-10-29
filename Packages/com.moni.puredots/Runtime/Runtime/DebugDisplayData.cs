using PureDOTS.Runtime.Spatial;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    public struct DebugDisplayData : IComponentData
    {
        public uint CurrentTick;
        public bool IsPaused;
        public FixedString128Bytes TimeStateText;
        public FixedString128Bytes RewindStateText;
        public int VillagerCount;
        public float TotalResourcesStored;

        // Registry diagnostics surfaced for tooling overlays
        public int RegisteredRegistryCount;
        public int RegisteredEntryCount;
        public uint RegistryDirectoryVersion;
        public uint RegistryDirectoryLastUpdateTick;
        public uint RegistryDirectoryAggregateHash;
        public FixedString128Bytes RegistryStateText;
        public FixedString512Bytes RegistryHealthHeadline;
        public FixedString512Bytes RegistryHealthAlerts;
        public byte RegistryWorstHealthLevel;
        public int RegistryHealthyCount;
        public int RegistryWarningCount;
        public int RegistryCriticalCount;
        public int RegistryFailureCount;
        public uint RegistryInstrumentationVersion;
        public uint RegistryContinuityVersion;
        public int RegistryContinuityWarningCount;
        public int RegistryContinuityFailureCount;
        public FixedString512Bytes RegistryContinuityAlerts;
        public bool RegistryHasAlerts;
        public int ResourceSpatialResolved;
        public int ResourceSpatialFallback;
        public int ResourceSpatialUnmapped;
        public int StorehouseSpatialResolved;
        public int StorehouseSpatialFallback;
        public int StorehouseSpatialUnmapped;
        public int BandRegistryCount;
        public int BandEntryCount;
        public int BandTotalMembers;
        public int BandSpatialResolved;
        public int BandSpatialFallback;
        public int BandSpatialUnmapped;

        // Pooling diagnostics snapshot sourced from Nx pooling runtime
        public bool PoolingActive;
        public PoolingDiagnostics PoolingSnapshot;
        public FixedString128Bytes PoolingStateText;

        // Spatial diagnostics
        public int SpatialCellCount;
        public int SpatialIndexedEntityCount;
        public uint SpatialVersion;
        public uint SpatialLastUpdateTick;
        public FixedString128Bytes SpatialStateText;
        public int SpatialDirtyAddCount;
        public int SpatialDirtyUpdateCount;
        public int SpatialDirtyRemoveCount;
        public float SpatialLastRebuildMilliseconds;
        public SpatialGridRebuildStrategy SpatialLastStrategy;

        // Streaming diagnostics
        public int StreamingDesiredCount;
        public int StreamingLoadedCount;
        public int StreamingLoadingCount;
        public int StreamingQueuedLoads;
        public int StreamingQueuedUnloads;
        public int StreamingPendingCommands;
        public int StreamingActiveCooldowns;
        public uint StreamingFirstLoadTick;
        public uint StreamingFirstUnloadTick;
        public FixedString128Bytes StreamingStateText;

        // Environment diagnostics
        public FixedString128Bytes SunlightStateText;
        public float SunlightDirectAverage;
        public float SunlightAmbientAverage;
        public float SunlightIntensity;
        public float3 SunlightDirection;
        public ushort SunlightMaxOccluders;
        public uint SunlightLastUpdateTick;

        // Frame timing diagnostics
        public FixedString128Bytes FrameTimingText;
        public FixedString32Bytes FrameTimingWorstGroup;
        public float FrameTimingWorstDurationMs;
        public int FrameTimingSampleCount;
        public bool FrameTimingBudgetExceeded;

        // Memory allocation diagnostics
        public FixedString128Bytes AllocationStateText;
        public int GcCollectionsGeneration0;
        public int GcCollectionsGeneration1;
        public int GcCollectionsGeneration2;

        // Replay capture diagnostics
        public int ReplayEventCount;
        public FixedString128Bytes ReplayStateText;
    }
}

