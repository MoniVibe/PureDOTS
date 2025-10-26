using Unity.Collections;
using Unity.Entities;

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

        // Pooling diagnostics snapshot sourced from Nx pooling runtime
        public bool PoolingActive;
        public PoolingDiagnostics PoolingSnapshot;
        public FixedString128Bytes PoolingStateText;

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



