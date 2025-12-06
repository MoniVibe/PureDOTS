using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Frame phase for triple-buffered scheduling.
    /// </summary>
    public enum FramePhase : byte
    {
        Render = 0,    // Frame N-1: Render
        Simulate = 1,  // Frame N: Simulate
        Load = 2       // Frame N+1: Load/Stream
    }

    /// <summary>
    /// Triple-buffered frame pipeline manager.
    /// While frame N simulates, frame N-1 renders, and frame N+1 preloads.
    /// </summary>
    [BurstCompile]
    public struct FramePipelineManager
    {
        private NativeStream _renderBuffer;
        private NativeStream _simulateBuffer;
        private NativeStream _loadBuffer;
        private FramePhase _currentPhase;
        private int _frameIndex;

        public FramePipelineManager(Allocator allocator)
        {
            _renderBuffer = new NativeStream(1, allocator);
            _simulateBuffer = new NativeStream(1, allocator);
            _loadBuffer = new NativeStream(1, allocator);
            _currentPhase = FramePhase.Simulate;
            _frameIndex = 0;
        }

        public void Dispose()
        {
            if (_renderBuffer.IsCreated)
            {
                _renderBuffer.Dispose();
            }

            if (_simulateBuffer.IsCreated)
            {
                _simulateBuffer.Dispose();
            }

            if (_loadBuffer.IsCreated)
            {
                _loadBuffer.Dispose();
            }
        }

        public bool IsCreated => _renderBuffer.IsCreated && _simulateBuffer.IsCreated && _loadBuffer.IsCreated;

        /// <summary>
        /// Gets the buffer for the current phase.
        /// </summary>
        [BurstCompile]
        public NativeStream GetCurrentBuffer()
        {
            return _currentPhase switch
            {
                FramePhase.Render => _renderBuffer,
                FramePhase.Simulate => _simulateBuffer,
                FramePhase.Load => _loadBuffer,
                _ => _simulateBuffer
            };
        }

        /// <summary>
        /// Gets the buffer for a specific phase.
        /// </summary>
        [BurstCompile]
        public NativeStream GetBuffer(FramePhase phase)
        {
            return phase switch
            {
                FramePhase.Render => _renderBuffer,
                FramePhase.Simulate => _simulateBuffer,
                FramePhase.Load => _loadBuffer,
                _ => _simulateBuffer
            };
        }

        /// <summary>
        /// Advances to the next frame, rotating buffers.
        /// </summary>
        [BurstCompile]
        public void AdvanceFrame()
        {
            _frameIndex++;
            
            // Rotate phases: Render -> Simulate -> Load -> Render
            _currentPhase = (FramePhase)(((int)_currentPhase + 1) % 3);
        }

        /// <summary>
        /// Gets the current frame phase.
        /// </summary>
        [BurstCompile]
        public FramePhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Gets the current frame index.
        /// </summary>
        [BurstCompile]
        public int FrameIndex => _frameIndex;
    }
}

