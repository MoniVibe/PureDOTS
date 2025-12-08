using System.Runtime.InteropServices;
using PureDOTS.Runtime.Registry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Hierarchical grid level identifiers for multi-resolution spatial partitioning.
    /// </summary>
    public enum HierarchicalGridLevel : byte
    {
        L0_Galactic = 0,  // 1 ly - 100 AU, 0.001 Hz, analytic orbits only
        L1_System = 1,   // 10^6 km, 0.01 Hz, coarse collision zones
        L2_Planet = 2,   // 1-10 km, 1 Hz, full deterministic grid
        L3_Local = 3     // 1-100 m, 60 Hz, fine physics & AI
    }

    /// <summary>
    /// Configuration for a single hierarchical grid level.
    /// </summary>
    public struct HierarchicalLevelConfig
    {
        public HierarchicalGridLevel Level;
        public float CellSize;
        public float TickRate; // Updates per second
        public bool UseAnalyticOrbits; // If true, only store orbital parameters, not entity positions
        public float3 WorldMin;
        public float3 WorldMax;
        public int3 CellCounts;

        public readonly float3 WorldExtent => WorldMax - WorldMin;
        public readonly int CellCount => math.max(CellCounts.x * CellCounts.y * CellCounts.z, 0);
    }

    /// <summary>
    /// Configuration for the active spatial grid provider.
    /// Authored through data assets and baked into a singleton.
    /// Supports both single-level (legacy) and multi-level hierarchical grids.
    /// 
    /// <para>
    /// <b>Legacy Mode:</b> Set <see cref="IsHierarchical"/> = false, use <see cref="CellSize"/> and <see cref="CellCounts"/>.
    /// </para>
    /// 
    /// <para>
    /// <b>Hierarchical Mode:</b> Set <see cref="IsHierarchical"/> = true, configure <see cref="HierarchicalLevels"/> (L0-L3).
    /// Use <see cref="TryGetLevelConfig"/> to access level-specific settings.
    /// </para>
    /// 
    /// <para>
    /// <b>Usage Example:</b>
    /// <code>
    /// var config = SystemAPI.GetSingleton&lt;SpatialGridConfig&gt;();
    /// if (config.IsHierarchical &amp;&amp; config.TryGetLevelConfig(HierarchicalGridLevel.L3_Local, out var level))
    /// {
    ///     var cellSize = level.CellSize; // Level-specific cell size
    /// }
    /// else
    /// {
    ///     var cellSize = config.CellSize; // Legacy single-level
    /// }
    /// </code>
    /// </para>
    /// 
    /// See also: <see cref="HierarchicalSpatialGridGuide.md"/>, <see cref="SpatialGridMigration"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SpatialGridConfig : IComponentData
    {
        public float CellSize; // Legacy single-level cell size (used when HierarchicalLevels.Length == 0)
        public float3 WorldMin;
        public float3 WorldMax;
        public int3 CellCounts; // Legacy single-level cell counts
        public uint HashSeed;
        public byte ProviderId;

        // Hierarchical grid support
        public byte IsHierarchicalByte; // 0 = legacy, 1 = hierarchical
        public bool IsHierarchical => IsHierarchicalByte != 0;
        public FixedList512Bytes<HierarchicalLevelConfig> HierarchicalLevels; // Per-level configurations

        // Adaptive subdivision thresholds
        public float UpperDensityThreshold; // Subdivide if density > this (default: 100.0 entities/cell)
        public float LowerDensityThreshold; // Merge if density < this (default: 10.0 entities/cell)
        public int MaxSubdivisionDepth; // Maximum octree depth (default: 4 levels)

        public readonly float3 WorldExtent => WorldMax - WorldMin;

        public readonly int CellCount => math.max(CellCounts.x * CellCounts.y * CellCounts.z, 0);

        /// <summary>
        /// Gets the level configuration for a specific hierarchical level, or returns default if not found.
        /// </summary>
        public readonly bool TryGetLevelConfig(HierarchicalGridLevel level, out HierarchicalLevelConfig config)
        {
            if (!IsHierarchical || HierarchicalLevels.Length == 0)
            {
                config = default;
                return false;
            }

            for (int i = 0; i < HierarchicalLevels.Length; i++)
            {
                if (HierarchicalLevels[i].Level == level)
                {
                    config = HierarchicalLevels[i];
                    return true;
                }
            }

            config = default;
            return false;
        }

        /// <summary>
        /// Gets the active cell size for the current configuration (hierarchical or legacy).
        /// </summary>
        public readonly float GetActiveCellSize(HierarchicalGridLevel? level = null)
        {
            if (IsHierarchical && level.HasValue)
            {
                if (TryGetLevelConfig(level.Value, out var levelConfig))
                {
                    return levelConfig.CellSize;
                }
            }

            return CellSize; // Legacy single-level
        }
    }

    /// <summary>
    /// Runtime state for the spatial grid including double buffer tracking.
    /// </summary>
    public enum SpatialGridRebuildStrategy : byte
    {
        None = 0,
        Full = 1,
        Partial = 2
    }

    public struct SpatialGridState : IComponentData
    {
        public int ActiveBufferIndex;
        public int TotalEntries;
        public uint Version;
        public uint LastUpdateTick;
        public uint LastDirtyTick;
        public uint DirtyVersion;
        public int DirtyAddCount;
        public int DirtyUpdateCount;
        public int DirtyRemoveCount;
        public float LastRebuildMilliseconds;
        public SpatialGridRebuildStrategy LastStrategy;
    }

    /// <summary>
    /// Buffer element describing the compact entity slice that backs a cell.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridCellRange : IBufferElementData
    {
        public int StartIndex;
        public int Count;
    }

    /// <summary>
    /// Buffer element storing the flattened entity list for all cells.
    /// Supports both legacy CellId (int) and new CellKey (ulong SFC) for backward compatibility.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridEntry : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public int CellId; // Legacy: flattened cell index (backward compatibility)
        public ulong CellKey; // New: space-filling curve key (Morton/Hilbert)

        /// <summary>
        /// Gets the primary cell identifier. Uses CellKey if non-zero, otherwise falls back to CellId.
        /// </summary>
        public readonly ulong GetPrimaryKey()
        {
            return CellKey != 0 ? CellKey : (ulong)(uint)CellId;
        }

        /// <summary>
        /// Sets both CellId and CellKey from a Morton key.
        /// </summary>
        public void SetFromMortonKey(ulong mortonKey, int fallbackCellId = -1)
        {
            CellKey = mortonKey;
            if (fallbackCellId >= 0)
            {
                CellId = fallbackCellId;
            }
        }
    }

    /// <summary>
    /// Tag component applied to entities that should be indexed by the spatial grid.
    /// </summary>
    public struct SpatialIndexedTag : IComponentData
    {
    }

    /// <summary>
    /// Buffer used as a staging area while rebuilding the grid.
    /// Supports both legacy CellId and new CellKey for backward compatibility.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridStagingEntry : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public int CellId; // Legacy: flattened cell index
        public ulong CellKey; // New: space-filling curve key

        public readonly ulong GetPrimaryKey()
        {
            return CellKey != 0 ? CellKey : (ulong)(uint)CellId;
        }
    }

    /// <summary>
    /// Buffer used as a staging area for cell ranges while rebuilding.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridStagingCellRange : IBufferElementData
    {
        public int StartIndex;
        public int Count;
    }

    public enum SpatialGridDirtyOpType : byte
    {
        None = 0,
        Add = 1,
        Update = 2,
        Remove = 3
    }

    [InternalBufferCapacity(0)]
    public struct SpatialGridDirtyOp : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public int OldCellId; // Legacy: flattened cell index
        public int NewCellId; // Legacy: flattened cell index
        public ulong OldCellKey; // New: space-filling curve key
        public ulong NewCellKey; // New: space-filling curve key
        public SpatialGridDirtyOpType Operation;

        public readonly ulong GetOldPrimaryKey()
        {
            return OldCellKey != 0 ? OldCellKey : (ulong)(uint)OldCellId;
        }

        public readonly ulong GetNewPrimaryKey()
        {
            return NewCellKey != 0 ? NewCellKey : (ulong)(uint)NewCellId;
        }
    }

    [InternalBufferCapacity(0)]
    public struct SpatialGridEntryLookup : IBufferElementData
    {
        public Entity Entity;
        public int EntryIndex;
        public int CellId; // Legacy: flattened cell index
        public ulong CellKey; // New: space-filling curve key

        public readonly ulong GetPrimaryKey()
        {
            return CellKey != 0 ? CellKey : (ulong)(uint)CellId;
        }
    }

    public struct SpatialGridResidency : ICleanupComponentData
    {
        public int CellId; // Legacy: flattened cell index
        public ulong CellKey; // New: space-filling curve key
        public float3 LastPosition;
        public uint Version;

        public readonly ulong GetPrimaryKey()
        {
            return CellKey != 0 ? CellKey : (ulong)(uint)CellId;
        }
    }

    /// <summary>
    /// Compact descriptor describing a radius-based spatial search.
    /// Provides reusable configuration that can be shared between entity categories.
    /// </summary>
    public struct SpatialQueryDescriptor
    {
        public float3 Origin;
        public float Radius;
        public int MaxResults;
        public SpatialQueryOptions Options;
        public float Tolerance;
        public Entity ExcludedEntity;
    }

    /// <summary>
    /// Options that modify how spatial descriptors behave.
    /// </summary>
    [System.Flags]
    public enum SpatialQueryOptions : byte
    {
        None = 0,
        IgnoreSelf = 1 << 0,
        ProjectToXZ = 1 << 1,
        RequireDeterministicSorting = 1 << 2
    }

    /// <summary>
    /// Result range metadata written by batched spatial jobs.
    /// </summary>
    public struct SpatialQueryRange
    {
        public int Start;
        public int Capacity;
        public int Count;
    }

    /// <summary>
    /// References to domain registries that consume spatial data.
    /// Updated by the spatial rebuild systems each time the grid refreshes.
    /// </summary>
    public struct SpatialRegistryMetadata : IComponentData
    {
        public FixedList128Bytes<RegistryHandle> Handles;
        public uint Version;

        public void ResetHandles()
        {
            if (Handles.Length > 0)
            {
                Handles.Clear();
                Version++;
            }
        }

        public bool TryGetHandle(RegistryKind kind, out RegistryHandle handle)
        {
            for (var i = 0; i < Handles.Length; i++)
            {
                var candidate = Handles[i];
                if (candidate.Kind == kind)
                {
                    handle = candidate;
                    return true;
                }
            }

            handle = default;
            return false;
        }

        public void SetHandle(RegistryHandle handle)
        {
            for (var i = 0; i < Handles.Length; i++)
            {
                var existing = Handles[i];
                if (existing.RegistryEntity != handle.RegistryEntity)
                {
                    continue;
                }

                Handles[i] = handle;
                Version++;
                return;
            }

            if (Handles.Length < Handles.Capacity)
            {
                Handles.Add(handle);
            }
            else
            {
                Handles[Handles.Length - 1] = handle;
            }

            Version++;
        }
    }

    /// <summary>
    /// Optional instrumentation toggle that enables console logging for spatial grid rebuilds.
    /// Attach to the grid singleton and configure <see cref="MinTickDelta"/> to activate.
    /// </summary>
    public struct SpatialConsoleInstrumentation : IComponentData
    {
        public const byte FlagLogOnlyOnChange = 1 << 0;

        /// <summary>
        /// Minimum number of ticks between log emissions. Zero disables tick-based throttling.
        /// </summary>
        public uint MinTickDelta;

        /// <summary>
        /// Tick when the last log entry was emitted.
        /// </summary>
        public uint LastLoggedTick;

        /// <summary>
        /// Spatial grid version that was logged most recently.
        /// </summary>
        public uint LastLoggedVersion;

        /// <summary>
        /// Behaviour flags (see Flag constants above).
        /// </summary>
        public byte Flags;

        public readonly bool ShouldLogOnlyOnChange => (Flags & FlagLogOnlyOnChange) != 0;
    }

    /// <summary>
    /// Core spatial cell data structure for hierarchical grids.
    /// Stores hot data (entities, positions) and cold data (density, statistics).
    /// </summary>
    public struct SpatialCell
    {
        public int3 Index;
        public AABB Bounds;
        public NativeList<Entity> Entities; // Hot: queried per tick
        public float Density; // Cold: queried per second+
        public byte Level; // 0-3 (HierarchicalGridLevel)
        public ulong MortonKey; // Space-filling curve index for cache coherence

        public readonly bool IsEmpty => !Entities.IsCreated || Entities.Length == 0;
        private static bool IsBoundsValid(in AABB bounds)
        {
            return math.all(bounds.Max >= bounds.Min) && math.all(math.isfinite(bounds.Max)) && math.all(math.isfinite(bounds.Min));
        }
        public readonly float Volume
        {
            get
            {
                if (!IsBoundsValid(in Bounds))
                {
                    return 0f;
                }

                var size = Bounds.Max - Bounds.Min;
                return math.abs(size.x * size.y * size.z);
            }
        }
        public readonly float EntityDensity => Volume > 0f && Entities.IsCreated ? Entities.Length / Volume : 0f;
    }

    /// <summary>
    /// Runtime state for hierarchical spatial grids, tracking per-level grid state and aggregation metadata.
    /// </summary>
    public struct HierarchicalSpatialGridState : IComponentData
    {
        /// <summary>
        /// Per-level grid versions (tracks when each level was last updated).
        /// </summary>
        public FixedList64Bytes<uint> LevelVersions; // Max 4 levels (L0-L3)

        /// <summary>
        /// Per-level active cell counts.
        /// </summary>
        public FixedList64Bytes<int> LevelCellCounts;

        /// <summary>
        /// Per-level total entity counts.
        /// </summary>
        public FixedList64Bytes<int> LevelEntityCounts;

        /// <summary>
        /// Last tick when each level was aggregated (for lazy aggregation).
        /// </summary>
        public FixedList64Bytes<uint> LastAggregationTicks;

        /// <summary>
        /// Aggregation interval per level (every Nth tick).
        /// </summary>
        public FixedList64Bytes<uint> AggregationIntervals;

        /// <summary>
        /// Active level per region (for multi-region grids).
        /// </summary>
        public byte ActiveLevel;

        /// <summary>
        /// Global version counter for the hierarchical grid.
        /// </summary>
        public uint Version;

        /// <summary>
        /// Last update tick for the hierarchical grid.
        /// </summary>
        public uint LastUpdateTick;

        public void InitializeLevel(HierarchicalGridLevel level, uint aggregationInterval)
        {
            var index = (int)level;
            if (index >= LevelVersions.Length)
            {
                return;
            }

            if (index >= LevelVersions.Length)
            {
                return;
            }

            LevelVersions[index] = 0;
            LevelCellCounts[index] = 0;
            LevelEntityCounts[index] = 0;
            LastAggregationTicks[index] = 0;
            AggregationIntervals[index] = aggregationInterval;
        }

        public readonly bool ShouldAggregateLevel(HierarchicalGridLevel level, uint currentTick)
        {
            var index = (int)level;
            if (index >= LastAggregationTicks.Length || index >= AggregationIntervals.Length)
            {
                return false;
            }

            var lastTick = LastAggregationTicks[index];
            var interval = AggregationIntervals[index];
            return interval > 0 && (currentTick - lastTick) >= interval;
        }
    }

    /// <summary>
    /// World index shared component for multi-ECS support.
    /// Each ECS world has a separate grid singleton with a unique WorldIndex.
    /// </summary>
    public struct SpatialGridWorldIndex : ISharedComponentData
    {
        public int WorldIndex;

        public SpatialGridWorldIndex(int worldIndex)
        {
            WorldIndex = worldIndex;
        }
    }

    /// <summary>
    /// Observer component for region streaming and culling.
    /// Cameras and AI regions subscribe to active cell ranges.
    /// </summary>
    public struct SpatialObserver : IComponentData
    {
        public float3 Position;
        public float Radius;
        public uint LastUpdateTick;
        public bool IsActive;
    }

    /// <summary>
    /// Buffer storing active cell keys for a spatial observer.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialObserverActiveCells : IBufferElementData
    {
        public ulong CellKey; // SFC key of active cell
    }

    /// <summary>
    /// Compressed cell summary for inactive cells (streamed to disk or compressed in memory).
    /// </summary>
    public struct CompressedCellSummary
    {
        public float3 Centroid; // Average position of entities
        public float TotalMass; // Sum of entity masses
        public int EntityCount; // Number of entities in this cell
        public ulong CellKey; // SFC key for reactivation lookup
        public byte Level; // Hierarchical level (0-3)
    }

    /// <summary>
    /// Buffer storing compressed cell summaries for inactive cells.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct CompressedCellSummaryBuffer : IBufferElementData
    {
        public CompressedCellSummary Summary;
    }

    /// <summary>
    /// Streaming configuration for spatial grids.
    /// </summary>
    public struct SpatialGridStreamingConfig : IComponentData
    {
        public float StreamingRadius; // Deactivate cells beyond this radius (default: 1000.0)
        public bool EnableStreaming; // If false, all cells stay active (default: false)
        public uint StreamingUpdateInterval; // Update interval in ticks (default: 60)
    }
}
