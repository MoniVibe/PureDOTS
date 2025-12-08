using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Scheduling
{
    /// <summary>
    /// Tracks which component types have been modified this frame.
    /// Used by the job graph scheduler to determine which systems need to run.
    /// </summary>
    [BurstCompile]
    public struct DirtyComponentTracker
    {
        private NativeHashSet<ComponentType> _dirtyComponents;
        private Allocator _allocator;

        public DirtyComponentTracker(Allocator allocator)
        {
            _dirtyComponents = new NativeHashSet<ComponentType>(16, allocator);
            _allocator = allocator;
        }

        [BurstCompile]
        public void MarkDirty(ComponentType componentType)
        {
            _dirtyComponents.Add(componentType);
        }

        [BurstCompile]
        public bool IsDirty(ComponentType componentType)
        {
            return _dirtyComponents.Contains(componentType);
        }

        [BurstCompile]
        public void Clear()
        {
            _dirtyComponents.Clear();
        }

        public void Dispose()
        {
            if (_dirtyComponents.IsCreated)
            {
                _dirtyComponents.Dispose();
            }
        }
    }
}

