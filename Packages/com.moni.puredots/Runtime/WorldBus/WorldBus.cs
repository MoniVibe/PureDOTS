using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.WorldBus
{
    /// <summary>
    /// Manager for routing events between ECS worlds.
    /// Uses fixed-capacity ring buffer and deterministic tick scheduling.
    /// </summary>
    [BurstCompile]
    public struct WorldBus
    {
        private NativeQueue<WorldMessage> _messageQueue;
        private NativeList<WorldMessage> _ringBuffer;
        private int _capacity;
        private int _writeIndex;
        private Allocator _allocator;

        public WorldBus(int capacity, Allocator allocator)
        {
            _messageQueue = new NativeQueue<WorldMessage>(allocator);
            _ringBuffer = new NativeList<WorldMessage>(capacity, allocator);
            _capacity = capacity;
            _writeIndex = 0;
            _allocator = allocator;
        }

        [BurstCompile]
        public void EnqueueMessage(in WorldMessage message)
        {
            if (_ringBuffer.Length < _capacity)
            {
                _ringBuffer.Add(message);
            }
            else
            {
                // Ring buffer: overwrite oldest
                _ringBuffer[_writeIndex % _capacity] = message;
                _writeIndex++;
            }
            _messageQueue.Enqueue(message);
        }

        [BurstCompile]
        public bool TryDequeueMessage(out WorldMessage message)
        {
            return _messageQueue.TryDequeue(out message);
        }

        [BurstCompile]
        public int GetMessageCount()
        {
            return _messageQueue.Count;
        }

        public void Dispose()
        {
            if (_messageQueue.IsCreated)
            {
                _messageQueue.Dispose();
            }
            if (_ringBuffer.IsCreated)
            {
                _ringBuffer.Dispose();
            }
        }
    }

    /// <summary>
    /// Singleton component managing the world bus.
    /// </summary>
    public struct WorldBusState : IComponentData
    {
        public byte WorldId;
        public int MessageCount;
    }
}

