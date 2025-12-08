using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// Simple in-memory snapshot store keyed by SimulationCell entity.
    /// Used as a stepping stone before EntityScene serialization.
    /// </summary>
    public struct CellSnapshotStore
    {
        public NativeParallelHashMap<Entity, CellSnapshot> Snapshots;

        public CellSnapshotStore(int capacity, Allocator allocator)
        {
            Snapshots = new NativeParallelHashMap<Entity, CellSnapshot>(capacity, allocator);
        }

        public void Dispose()
        {
            if (Snapshots.IsCreated)
            {
                Snapshots.Dispose();
            }
        }

        public bool TryGet(Entity cell, out CellSnapshot snapshot)
        {
            return Snapshots.TryGetValue(cell, out snapshot);
        }

        public void Set(Entity cell, CellSnapshot snapshot)
        {
            Snapshots[cell] = snapshot;
        }
    }

    /// <summary>
    /// Minimal snapshot data; extend as needed.
    /// </summary>
    public struct CellSnapshot
    {
        public int AgentCount;
    }
}
