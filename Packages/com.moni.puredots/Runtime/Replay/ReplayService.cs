using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Replay
{
    /// <summary>
    /// Service for writing command logs and tick hashes for replay functionality.
    /// </summary>
    [BurstCompile]
    public struct ReplayService
    {
        private NativeList<byte> _commandLog;
        private NativeHashMap<uint, ulong> _tickHashes; // Tick -> Hash
        private Allocator _allocator;

        public ReplayService(Allocator allocator)
        {
            _commandLog = new NativeList<byte>(1024, allocator);
            _tickHashes = new NativeHashMap<uint, ulong>(256, allocator);
            _allocator = allocator;
        }

        [BurstCompile]
        public void WriteCommand(uint tick, byte commandType, NativeArray<byte> commandData)
        {
            // Write tick
            WriteUInt32(tick);
            // Write command type
            _commandLog.Add(commandType);
            // Write command data length
            WriteUInt32((uint)commandData.Length);
            // Write command data
            _commandLog.AddRange(commandData);
        }

        [BurstCompile]
        public void WriteTickHash(uint tick, ulong hash)
        {
            _tickHashes.TryAdd(tick, hash);
        }

        [BurstCompile]
        public bool TryGetTickHash(uint tick, out ulong hash)
        {
            return _tickHashes.TryGetValue(tick, out hash);
        }

        [BurstCompile]
        private void WriteUInt32(uint value)
        {
            var bytes = new NativeArray<byte>(4, Allocator.Temp);
            unsafe
            {
                uint* ptr = (uint*)bytes.GetUnsafePtr();
                *ptr = value;
            }
            _commandLog.AddRange(bytes);
            bytes.Dispose();
        }

        public NativeArray<byte> GetCommandLog()
        {
            return _commandLog.AsArray();
        }

        public void Dispose()
        {
            if (_commandLog.IsCreated)
                _commandLog.Dispose();
            if (_tickHashes.IsCreated)
                _tickHashes.Dispose();
        }
    }
}

