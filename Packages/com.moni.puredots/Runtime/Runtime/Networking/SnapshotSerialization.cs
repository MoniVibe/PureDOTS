using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Interface for components that can serialize their state to snapshots.
    /// Systems using RewindState can call these to record component diffs each tick.
    /// Later, these diffs will be shipped over the wire to resync late clients or spectators.
    /// </summary>
    public interface ISnapshotSerializable
    {
        void WriteSnapshot(ref SnapshotWriter writer);
        void ReadSnapshot(ref SnapshotReader reader);
    }

    /// <summary>
    /// Burst-safe snapshot writer for recording component state.
    /// No network code yet - just writes to memory buffers.
    /// </summary>
    [BurstCompile]
    public unsafe struct SnapshotWriter
    {
        private NativeList<byte> _buffer;
        private bool _isCreated;

        public SnapshotWriter(int initialCapacity, Allocator allocator)
        {
            _buffer = new NativeList<byte>(initialCapacity, allocator);
            _isCreated = true;
        }

        public void WriteInt(int value)
        {
            if (!_isCreated) return;
            _buffer.AddRange((byte*)&value, sizeof(int));
        }

        public void WriteUInt(uint value)
        {
            if (!_isCreated) return;
            _buffer.AddRange((byte*)&value, sizeof(uint));
        }

        public void WriteFloat(float value)
        {
            if (!_isCreated) return;
            _buffer.AddRange((byte*)&value, sizeof(float));
        }

        public void WriteULong(ulong value)
        {
            if (!_isCreated) return;
            _buffer.AddRange((byte*)&value, sizeof(ulong));
        }

        public void WriteBytes(byte* data, int length)
        {
            if (!_isCreated) return;
            _buffer.AddRange(data, length);
        }

        public NativeArray<byte> ToArray(Allocator allocator)
        {
            if (!_isCreated)
            {
                return default;
            }
            return _buffer.ToArray(allocator);
        }

        public void Clear()
        {
            if (_isCreated)
            {
                _buffer.Clear();
            }
        }

        public void Dispose()
        {
            if (_isCreated)
            {
                _buffer.Dispose();
                _isCreated = false;
            }
        }

        public int Length => _isCreated ? _buffer.Length : 0;
    }

    /// <summary>
    /// Burst-safe snapshot reader for restoring component state.
    /// No network code yet - just reads from memory buffers.
    /// </summary>
    [BurstCompile]
    public unsafe struct SnapshotReader
    {
        private NativeArray<byte> _buffer;
        private int _position;
        private bool _isCreated;

        public SnapshotReader(NativeArray<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
            _isCreated = buffer.IsCreated;
        }

        public int ReadInt()
        {
            if (!_isCreated || _position + sizeof(int) > _buffer.Length)
            {
                return 0;
            }
            int value = *(int*)((byte*)_buffer.GetUnsafeReadOnlyPtr() + _position);
            _position += sizeof(int);
            return value;
        }

        public uint ReadUInt()
        {
            if (!_isCreated || _position + sizeof(uint) > _buffer.Length)
            {
                return 0;
            }
            uint value = *(uint*)((byte*)_buffer.GetUnsafeReadOnlyPtr() + _position);
            _position += sizeof(uint);
            return value;
        }

        public float ReadFloat()
        {
            if (!_isCreated || _position + sizeof(float) > _buffer.Length)
            {
                return 0f;
            }
            float value = *(float*)((byte*)_buffer.GetUnsafeReadOnlyPtr() + _position);
            _position += sizeof(float);
            return value;
        }

        public ulong ReadULong()
        {
            if (!_isCreated || _position + sizeof(ulong) > _buffer.Length)
            {
                return 0;
            }
            ulong value = *(ulong*)((byte*)_buffer.GetUnsafeReadOnlyPtr() + _position);
            _position += sizeof(ulong);
            return value;
        }

        public void ReadBytes(byte* data, int length)
        {
            if (!_isCreated || _position + length > _buffer.Length)
            {
                return;
            }
            UnsafeUtility.MemCpy(data, (byte*)_buffer.GetUnsafeReadOnlyPtr() + _position, length);
            _position += length;
        }

        public int Position => _position;
        public int Length => _isCreated ? _buffer.Length : 0;
        public bool IsValid => _isCreated && _position < _buffer.Length;
    }
}

