using PureDOTS.Runtime.Registry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Configuration for the active spatial grid provider.
    /// Authored through data assets and baked into a singleton.
    /// </summary>
    public struct SpatialGridConfig : IComponentData
    {
        public float CellSize;
        public float3 WorldMin;
        public float3 WorldMax;
        public int3 CellCounts;
        public uint HashSeed;
        public byte ProviderId;

        public readonly float3 WorldExtent => WorldMax - WorldMin;

        public readonly int CellCount => math.max(CellCounts.x * CellCounts.y * CellCounts.z, 0);
    }

    /// <summary>
    /// Runtime state for the spatial grid including double buffer tracking.
    /// </summary>
    public struct SpatialGridState : IComponentData
    {
        public int ActiveBufferIndex;
        public int TotalEntries;
        public uint Version;
        public uint LastUpdateTick;
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
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridEntry : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public int CellId;
    }

    /// <summary>
    /// Tag component applied to entities that should be indexed by the spatial grid.
    /// </summary>
    public struct SpatialIndexedTag : IComponentData
    {
    }

    /// <summary>
    /// Buffer used as a staging area while rebuilding the grid.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridStagingEntry : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public int CellId;
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
}
