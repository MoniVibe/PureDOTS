using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Utility methods for spatial hashing and coordinate transforms.
    /// </summary>
    [BurstCompile]
    public static class SpatialHash
    {
        [BurstCompile]
        public static int3 Quantize(float3 position, in SpatialGridConfig config)
        {
            var local = (position - config.WorldMin) / math.max(config.CellSize, 1e-3f);
            var maxCell = (float3)(config.CellCounts - 1);
            var wrapped = math.clamp(local, float3.zero, maxCell);
            return (int3)math.floor(wrapped + 1e-4f);
        }

        [BurstCompile]
        public static int Flatten(int3 cell, in SpatialGridConfig config)
        {
            return cell.x * config.CellCounts.y * config.CellCounts.z
                + cell.y * config.CellCounts.z
                + cell.z;
        }

        [BurstCompile]
        public static uint MortonKey(int3 cell, uint seed = 0u)
        {
            var x = (uint)cell.x;
            var y = (uint)cell.y;
            var z = (uint)cell.z;

            x = Part1By2(x);
            y = Part1By2(y);
            z = Part1By2(z);

            var morton = x | (y << 1) | (z << 2);
            return morton ^ seed;
        }

        private static uint Part1By2(uint x)
        {
            x = (x | (x << 16)) & 0x030000FF;
            x = (x | (x << 8)) & 0x0300F00F;
            x = (x | (x << 4)) & 0x030C30C3;
            x = (x | (x << 2)) & 0x09249249;
            return x;
        }
    }

    /// <summary>
    /// Deterministic key representing a grid cell.
    /// </summary>
    public struct GridCellKey : System.IEquatable<GridCellKey>
    {
        public int3 Coordinates;
        public uint Hash;

        public GridCellKey(int3 coords, uint hash)
        {
            Coordinates = coords;
            Hash = hash;
        }

        public bool Equals(GridCellKey other)
        {
            return math.all(Coordinates == other.Coordinates) && Hash == other.Hash;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCellKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)math.hash(new uint4((uint)Coordinates.x, (uint)Coordinates.y, (uint)Coordinates.z, Hash));
        }
    }

    /// <summary>
    /// Burst-friendly helpers for common spatial queries.
    /// </summary>
    public static class SpatialQueryHelper
    {
        public static bool TryGetCellSlice(in DynamicBuffer<SpatialGridCellRange> ranges, in DynamicBuffer<SpatialGridEntry> entries, int cellId, out NativeSlice<SpatialGridEntry> slice)
        {
            if ((uint)cellId >= ranges.Length)
            {
                slice = default;
                return false;
            }

            var range = ranges[cellId];
            if (range.Count <= 0)
            {
                slice = default;
                return false;
            }

            var entryArray = entries.AsNativeArray();
            slice = entryArray.Slice(range.StartIndex, range.Count);
            return true;
        }

        public static void CollectEntitiesInRadius(
            float3 position,
            float radius,
            in SpatialGridConfig config,
            in DynamicBuffer<SpatialGridCellRange> ranges,
            in DynamicBuffer<SpatialGridEntry> entries,
            ref NativeList<Entity> results)
        {
            var cellCoords = SpatialHash.Quantize(position, config);
            var maxOffset = (int)math.ceil(radius / math.max(config.CellSize, 1e-3f));
            var radiusSq = radius * radius;

            var entryArray = entries.AsNativeArray();

            for (var dx = -maxOffset; dx <= maxOffset; dx++)
            {
                for (var dy = -maxOffset; dy <= maxOffset; dy++)
                {
                    for (var dz = -maxOffset; dz <= maxOffset; dz++)
                    {
                        var neighbor = cellCoords + new int3(dx, dy, dz);
                        if (!IsWithinBounds(neighbor, config.CellCounts))
                        {
                            continue;
                        }

                        var cellId = SpatialHash.Flatten(neighbor, config);
                        if ((uint)cellId >= ranges.Length)
                        {
                            continue;
                        }

                        var range = ranges[cellId];
                        if (range.Count <= 0)
                        {
                            continue;
                        }

                        for (var i = 0; i < range.Count; i++)
                        {
                            var entry = entryArray[range.StartIndex + i];
                            var distSq = math.lengthsq(entry.Position - position);
                            if (distSq <= radiusSq)
                            {
                                results.Add(entry.Entity);
                            }
                        }
                    }
                }
            }

            results.Sort(new EntityDeterministicComparer());
        }

        public static void GetCellEntities(
            int3 cellCoords,
            in SpatialGridConfig config,
            in DynamicBuffer<SpatialGridCellRange> ranges,
            in DynamicBuffer<SpatialGridEntry> entries,
            ref NativeList<Entity> results)
        {
            if (!IsWithinBounds(cellCoords, config.CellCounts))
            {
                return;
            }

            var cellId = SpatialHash.Flatten(cellCoords, config);
            if ((uint)cellId >= ranges.Length)
            {
                return;
            }

            var range = ranges[cellId];
            if (range.Count <= 0)
            {
                return;
            }

            var entryArray = entries.AsNativeArray();
            for (var i = 0; i < range.Count; i++)
            {
                results.Add(entryArray[range.StartIndex + i].Entity);
            }
        }

        public static void OverlapAABB(
            float3 aabbMin,
            float3 aabbMax,
            in SpatialGridConfig config,
            in DynamicBuffer<SpatialGridCellRange> ranges,
            in DynamicBuffer<SpatialGridEntry> entries,
            ref NativeList<Entity> results)
        {
            var minCell = SpatialHash.Quantize(aabbMin, config);
            var maxCell = SpatialHash.Quantize(aabbMax, config);

            minCell = math.clamp(minCell, int3.zero, config.CellCounts - 1);
            maxCell = math.clamp(maxCell, int3.zero, config.CellCounts - 1);

            if (math.any(maxCell < minCell))
            {
                return;
            }

            var entryArray = entries.AsNativeArray();

            for (var x = minCell.x; x <= maxCell.x; x++)
            {
                for (var y = minCell.y; y <= maxCell.y; y++)
                {
                    for (var z = minCell.z; z <= maxCell.z; z++)
                    {
                        var coords = new int3(x, y, z);
                        var cellId = SpatialHash.Flatten(coords, config);
                        if ((uint)cellId >= ranges.Length)
                        {
                            continue;
                        }

                        var range = ranges[cellId];
                        if (range.Count <= 0)
                        {
                            continue;
                        }

                        for (var i = 0; i < range.Count; i++)
                        {
                            var entry = entryArray[range.StartIndex + i];
                            if (entry.Position.x < aabbMin.x || entry.Position.x > aabbMax.x ||
                                entry.Position.y < aabbMin.y || entry.Position.y > aabbMax.y ||
                                entry.Position.z < aabbMin.z || entry.Position.z > aabbMax.z)
                            {
                                continue;
                            }

                            results.Add(entry.Entity);
                        }
                    }
                }
            }

            results.Sort(new EntityDeterministicComparer());
        }

        public static bool TryFindClosest(
            float3 position,
            in SpatialGridConfig config,
            in DynamicBuffer<SpatialGridCellRange> ranges,
            in DynamicBuffer<SpatialGridEntry> entries,
            out Entity closestEntity,
            out float closestDistanceSq)
        {
            var maxOffset = 1;
            var cellCoords = SpatialHash.Quantize(position, config);
            var entryArray = entries.AsNativeArray();

            closestEntity = Entity.Null;
            closestDistanceSq = float.MaxValue;

            while (closestEntity == Entity.Null && maxOffset < math.max(config.CellCounts.x, math.max(config.CellCounts.y, config.CellCounts.z)))
            {
                var found = false;

                for (var dx = -maxOffset; dx <= maxOffset; dx++)
                {
                    for (var dy = -maxOffset; dy <= maxOffset; dy++)
                    {
                        for (var dz = -maxOffset; dz <= maxOffset; dz++)
                        {
                            var neighbor = cellCoords + new int3(dx, dy, dz);
                            if (!IsWithinBounds(neighbor, config.CellCounts))
                            {
                                continue;
                            }

                            var cellId = SpatialHash.Flatten(neighbor, config);
                            if ((uint)cellId >= ranges.Length)
                            {
                                continue;
                            }

                            var range = ranges[cellId];
                            for (var i = 0; i < range.Count; i++)
                            {
                                var entry = entryArray[range.StartIndex + i];
                                var distSq = math.lengthsq(entry.Position - position);
                                if (distSq < closestDistanceSq)
                                {
                                    closestDistanceSq = distSq;
                                    closestEntity = entry.Entity;
                                    found = true;
                                }
                            }
                        }
                    }
                }

                if (found)
                {
                    return true;
                }

                maxOffset++;
            }

            return closestEntity != Entity.Null;
        }

        private static bool IsWithinBounds(int3 coords, int3 maxCounts)
        {
            return coords.x >= 0 && coords.y >= 0 && coords.z >= 0
                && coords.x < maxCounts.x
                && coords.y < maxCounts.y
                && coords.z < maxCounts.z;
        }
    }

    /// <summary>
    /// Ensures deterministic ordering when collecting entities from the grid.
    /// </summary>
    public struct EntityDeterministicComparer : IComparer<Entity>
    {
        public int Compare(Entity x, Entity y)
        {
            return x.Index.CompareTo(y.Index);
        }
    }
}
