using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Immutable event log for deterministic replay.
    /// Append-only during forward simulation.
    /// During rewind: roll back state, trim log to ≤ target tick.
    /// Forward re-sim: replay same event stream → identical outcome guaranteed.
    /// </summary>
    public struct EventLog : System.IDisposable
    {
        private NativeList<EventRecord> _events;
        private NativeHashMap<ulong, int> _idToIndex;
        private ulong _nextEventId;
        private uint _oldestTick;

        public bool IsCreated => _events.IsCreated;
        public int EventCount => _events.Length;

        public EventLog(int initialCapacity, Allocator allocator)
        {
            _events = new NativeList<EventRecord>(initialCapacity, allocator);
            _idToIndex = new NativeHashMap<ulong, int>(initialCapacity, allocator);
            _nextEventId = 1ul;
            _oldestTick = 0u;
        }

        public void Dispose()
        {
            if (_events.IsCreated)
            {
                _events.Dispose();
            }
            if (_idToIndex.IsCreated)
            {
                _idToIndex.Dispose();
            }
        }

        /// <summary>
        /// Append event to log (append-only during forward simulation).
        /// </summary>
        public void Append(byte eventType, uint tick, FixedBytes64 payload, Entity entity = default)
        {
            var record = new EventRecord
            {
                Id = _nextEventId++,
                Type = eventType,
                Tick = tick,
                Payload = payload,
                Entity = entity
            };

            int index = _events.Length;
            _events.Add(record);
            _idToIndex[record.Id] = index;
        }

        /// <summary>
        /// Get all events at or before target tick.
        /// </summary>
        [BurstCompile]
        public NativeList<EventRecord> GetEventsUpToTick(uint targetTick, Allocator allocator)
        {
            var result = new NativeList<EventRecord>(_events.Length / 2, allocator);

            for (int i = 0; i < _events.Length; i++)
            {
                if (_events[i].Tick <= targetTick)
                {
                    result.Add(_events[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Get events in tick range [startTick, endTick].
        /// </summary>
        [BurstCompile]
        public NativeList<EventRecord> GetEventsInRange(uint startTick, uint endTick, Allocator allocator)
        {
            var result = new NativeList<EventRecord>(_events.Length / 4, allocator);

            for (int i = 0; i < _events.Length; i++)
            {
                var evt = _events[i];
                if (evt.Tick >= startTick && evt.Tick <= endTick)
                {
                    result.Add(evt);
                }
            }

            return result;
        }

        /// <summary>
        /// Trim log to remove events older than targetTick.
        /// Called during rewind to roll back event history.
        /// </summary>
        public void TrimToTick(uint targetTick)
        {
            if (_events.Length == 0)
            {
                _oldestTick = targetTick;
                return;
            }

            var newEvents = new NativeList<EventRecord>(_events.Length, Allocator.Temp);
            var newIdMap = new NativeHashMap<ulong, int>(_events.Length, Allocator.Temp);

            for (int i = 0; i < _events.Length; i++)
            {
                var evt = _events[i];
                if (evt.Tick <= targetTick)
                {
                    int newIndex = newEvents.Length;
                    newEvents.Add(evt);
                    newIdMap[evt.Id] = newIndex;
                }
            }

            // Replace old storage
            _events.Clear();
            _idToIndex.Clear();

            for (int i = 0; i < newEvents.Length; i++)
            {
                _events.Add(newEvents[i]);
            }

            foreach (var kvp in newIdMap)
            {
                _idToIndex[kvp.Key] = kvp.Value;
            }

            if (newEvents.Length > 0)
            {
                _oldestTick = targetTick;
            }

            newEvents.Dispose();
            newIdMap.Dispose();
        }

        /// <summary>
        /// Replay events from log up to targetTick.
        /// Used for forward re-simulation to guarantee identical outcome.
        /// </summary>
        [BurstCompile]
        public void ReplayEventsUpTo(uint targetTick, System.Action<EventRecord> onEvent)
        {
            for (int i = 0; i < _events.Length; i++)
            {
                var evt = _events[i];
                if (evt.Tick <= targetTick)
                {
                    onEvent(evt);
                }
            }
        }

        /// <summary>
        /// Get oldest tick in log.
        /// </summary>
        public uint GetOldestTick()
        {
            if (_events.Length == 0)
            {
                return _oldestTick;
            }

            uint oldest = uint.MaxValue;
            for (int i = 0; i < _events.Length; i++)
            {
                if (_events[i].Tick < oldest)
                {
                    oldest = _events[i].Tick;
                }
            }

            return oldest == uint.MaxValue ? _oldestTick : oldest;
        }

        /// <summary>
        /// Get newest tick in log.
        /// </summary>
        public uint GetNewestTick()
        {
            if (_events.Length == 0)
            {
                return 0u;
            }

            uint newest = 0u;
            for (int i = 0; i < _events.Length; i++)
            {
                if (_events[i].Tick > newest)
                {
                    newest = _events[i].Tick;
                }
            }

            return newest;
        }

        /// <summary>
        /// Prune events older than minTick (for memory management).
        /// </summary>
        public void PruneOlderThan(uint minTick)
        {
            TrimToTick(minTick);
        }
    }

    /// <summary>
    /// Helper for creating event records with typed payloads.
    /// </summary>
    [BurstCompile]
    public static class EventLogHelper
    {
        /// <summary>
        /// Create event record with unmanaged payload.
        /// </summary>
        [BurstCompile]
        public static EventRecord CreateEvent<T>(byte eventType, uint tick, T payload, Entity entity = default) where T : unmanaged
        {
            var record = new EventRecord
            {
                Type = eventType,
                Tick = tick,
                Entity = entity
            };

            // Copy payload into FixedBytes64
            unsafe
            {
                int payloadSize = UnsafeUtility.SizeOf<T>();
                int copySize = math.min(payloadSize, FixedBytes64.Size);

                void* payloadPtr = UnsafeUtility.AddressOf(ref record.Payload);
                UnsafeUtility.MemClear(payloadPtr, FixedBytes64.Size);
                void* sourcePtr = UnsafeUtility.AddressOf(ref payload);
                UnsafeUtility.MemCpy(payloadPtr, sourcePtr, copySize);
            }

            return record;
        }

        /// <summary>
        /// Read payload from event record.
        /// </summary>
        [BurstCompile]
        public static T ReadPayload<T>(in EventRecord record) where T : unmanaged
        {
            T payload = default;
            unsafe
            {
                int payloadSize = UnsafeUtility.SizeOf<T>();
                int copySize = math.min(payloadSize, FixedBytes64.Size);
                void* targetPtr = UnsafeUtility.AddressOf(ref payload);

                var payloadBytes = record.Payload;
                void* payloadPtr = UnsafeUtility.AddressOf(ref payloadBytes);
                UnsafeUtility.MemCpy(targetPtr, payloadPtr, copySize);
            }
            return payload;
        }
    }
}

