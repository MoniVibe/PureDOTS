using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Double buffer wrapper for avoiding write contention.
    /// Each thread writes exclusively to its own buffer; all reads come from previous frame.
    /// </summary>
    [BurstCompile]
    public struct DoubleBuffer<T> where T : unmanaged
    {
        private NativeArray<T> _readBuffer;
        private NativeArray<T> _writeBuffer;
        private bool _swapped;

        public DoubleBuffer(int length, Allocator allocator)
        {
            _readBuffer = new NativeArray<T>(length, allocator, NativeArrayOptions.ClearMemory);
            _writeBuffer = new NativeArray<T>(length, allocator, NativeArrayOptions.ClearMemory);
            _swapped = false;
        }

        public void Dispose()
        {
            if (_readBuffer.IsCreated)
            {
                _readBuffer.Dispose();
            }

            if (_writeBuffer.IsCreated)
            {
                _writeBuffer.Dispose();
            }
        }

        public bool IsCreated => _readBuffer.IsCreated && _writeBuffer.IsCreated;

        /// <summary>
        /// Gets the read buffer (previous frame's data).
        /// </summary>
        public NativeArray<T> ReadBuffer => _readBuffer;

        /// <summary>
        /// Gets the write buffer (current frame's data).
        /// </summary>
        public NativeArray<T> WriteBuffer => _writeBuffer;

        /// <summary>
        /// Swaps read and write buffers at frame end.
        /// </summary>
        [BurstCompile]
        public void Swap()
        {
            var temp = _readBuffer;
            _readBuffer = _writeBuffer;
            _writeBuffer = temp;
            _swapped = !_swapped;
        }

        /// <summary>
        /// Gets an element from the read buffer.
        /// </summary>
        [BurstCompile]
        public T Read(int index)
        {
            return _readBuffer[index];
        }

        /// <summary>
        /// Writes an element to the write buffer.
        /// </summary>
        [BurstCompile]
        public void Write(int index, T value)
        {
            _writeBuffer[index] = value;
        }

        public int Length => _readBuffer.Length;
    }

    /// <summary>
    /// Manager for double-buffered component arrays.
    /// </summary>
    public static class DoubleBufferManager
    {
        /// <summary>
        /// Swaps all double buffers at frame end.
        /// Called from LateSimulationSystemGroup.
        /// </summary>
        [BurstCompile]
        public static void SwapAllBuffers(ref SystemState state)
        {
            // This would be called from a system that manages double buffers
            // Implementation depends on how buffers are stored (singleton, component, etc.)
        }
    }
}

