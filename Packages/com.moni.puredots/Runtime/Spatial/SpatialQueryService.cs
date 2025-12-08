using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Global spatial service backed by NativeMultiHashMap for constant-time lookups.
    /// Provides unified spatial API for climate, comms, navmesh, and other domains.
    /// </summary>
    [BurstCompile]
    public struct SpatialQueryService
    {
        private NativeParallelMultiHashMap<int3, Entity> _spatialMap;
        private Allocator _allocator;

        public SpatialQueryService(Allocator allocator)
        {
            _spatialMap = new NativeParallelMultiHashMap<int3, Entity>(1024, allocator);
            _allocator = allocator;
        }

        [BurstCompile]
        public void RegisterEntity(int3 cellCoords, Entity entity)
        {
            _spatialMap.Add(cellCoords, entity);
        }

        [BurstCompile]
        public void UnregisterEntity(int3 cellCoords, Entity entity)
        {
            // Remove entity from cell
            if (_spatialMap.TryGetFirstValue(cellCoords, out var value, out var iterator))
            {
                do
                {
                    if (value == entity)
                    {
                        _spatialMap.Remove(iterator);
                        break;
                    }
                } while (_spatialMap.TryGetNextValue(out value, ref iterator));
            }
        }

        [BurstCompile]
        public void QueryCell(int3 cellCoords, ref NativeList<Entity> results)
        {
            if (_spatialMap.TryGetFirstValue(cellCoords, out var value, out var iterator))
            {
                do
                {
                    results.Add(value);
                } while (_spatialMap.TryGetNextValue(out value, ref iterator));
            }
        }

        [BurstCompile]
        public void QueryAABB(in AABB bounds, ref NativeList<Entity> results)
        {
            // Convert AABB to cell coordinates
            int3 minCell = CellCoordsFromPosition(bounds.Min);
            int3 maxCell = CellCoordsFromPosition(bounds.Max);

            // Query all cells in range
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        int3 cell = new int3(x, y, z);
                        QueryCell(cell, ref results);
                    }
                }
            }
        }

        [BurstCompile]
        private int3 CellCoordsFromPosition(float3 position, float cellSize = 10f)
        {
            return new int3(
                (int)math.floor(position.x / cellSize),
                (int)math.floor(position.y / cellSize),
                (int)math.floor(position.z / cellSize)
            );
        }

        public void Dispose()
        {
            if (_spatialMap.IsCreated)
            {
                _spatialMap.Dispose();
            }
        }
    }
}

