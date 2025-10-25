using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Registry
{
    /// <summary>
    /// Helper for building deterministic registry buffers without allocations.
    /// </summary>
    /// <typeparam name="TEntry">Entry type written into the registry buffer.</typeparam>
    public struct DeterministicRegistryBuilder<TEntry> : IDisposable
        where TEntry : unmanaged, IBufferElementData, IComparable<TEntry>
    {
        private NativeList<TEntry> _entries;

        public DeterministicRegistryBuilder(int capacity, Allocator allocator)
        {
            _entries = new NativeList<TEntry>(allocator);
            _entries.Capacity = math.max(0, capacity);
        }

        public void Add(in TEntry entry)
        {
            _entries.Add(entry);
        }

        public void ApplyTo(ref DynamicBuffer<TEntry> buffer)
        {
            if (_entries.Length > 1)
            {
                var array = _entries.AsArray();
                NativeSortExtension.Sort(array);
            }

            buffer.Clear();
            buffer.ResizeUninitialized(_entries.Length);

            if (_entries.Length > 0)
            {
                buffer.AsNativeArray().CopyFrom(_entries.AsArray());
            }
        }

        public int Length => _entries.Length;

        public void Dispose()
        {
            if (_entries.IsCreated)
            {
                _entries.Dispose();
            }
        }
    }
}
