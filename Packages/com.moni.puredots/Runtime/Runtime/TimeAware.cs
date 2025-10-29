using System.Collections.Generic;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace PureDOTS.Runtime.Time
{
    public interface ITimeAware
    {
        void OnTick(uint tick);
        void Save(ref SystemState state, ref TimeStreamWriter writer);
        void Load(ref SystemState state, ref TimeStreamReader reader);
        void OnRewindStart();
        void OnRewindEnd();
    }

    public struct TimeStreamWriter
    {
        internal NativeList<byte> Buffer;

        public TimeStreamWriter(ref NativeList<byte> backingBuffer)
        {
            Buffer = backingBuffer;
            Buffer.Clear();
        }

        public void Write<T>(T value) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            var writeIndex = Buffer.Length;
            Buffer.ResizeUninitialized(writeIndex + size);

            var temp = new NativeArray<T>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            temp[0] = value;
            var bytes = temp.Reinterpret<byte>(size);
            for (int i = 0; i < size; i++)
            {
                Buffer[writeIndex + i] = bytes[i];
            }
            temp.Dispose();
        }
    }

    public struct TimeStreamReader
    {
        private NativeArray<byte> _buffer;
        private int _offset;

        public TimeStreamReader(NativeArray<byte> buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }

        public T Read<T>() where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            var bytes = _buffer.GetSubArray(_offset, size);
            var arr = bytes.Reinterpret<T>(1);
            var value = arr[0];
            _offset += size;
            return value;
        }
    }

    [System.Flags]
    public enum TimeAwareExecutionPhase : byte
    {
        None = 0,
        Record = 1 << 0,
        CatchUp = 1 << 1,
        Playback = 1 << 2
    }

    [System.Flags]
    public enum TimeAwareExecutionOptions : byte
    {
        None = 0,
        SkipWhenPaused = 1 << 0
    }

    public struct TimeAwareContext
    {
        public TimeState Time;
        public RewindState Rewind;
        public TimeAwareExecutionPhase Phase;
        public bool ModeChangedThisFrame;
        public RewindMode PreviousMode;

        public readonly bool IsRecordPhase => Phase == TimeAwareExecutionPhase.Record;
        public readonly bool IsCatchUpPhase => Phase == TimeAwareExecutionPhase.CatchUp;
        public readonly bool IsPlaybackPhase => Phase == TimeAwareExecutionPhase.Playback;
    }

    public struct TimeAwareController
    {
        private readonly TimeAwareExecutionPhase _phases;
        private readonly TimeAwareExecutionOptions _options;
        private RewindMode _lastMode;
        private bool _initialised;

        public TimeAwareController(TimeAwareExecutionPhase phases, TimeAwareExecutionOptions options = TimeAwareExecutionOptions.None)
        {
            _phases = phases;
            _options = options;
            _lastMode = RewindMode.Record;
            _initialised = false;
        }

        public bool TryBegin(in TimeState timeState, in RewindState rewindState, out TimeAwareContext context)
        {
            context = default;

            var phase = ConvertModeToPhase(rewindState.Mode);
            if ((_phases & phase) == 0)
            {
                UpdateModeCache(rewindState.Mode);
                return false;
            }

            if ((_options & TimeAwareExecutionOptions.SkipWhenPaused) != 0 &&
                phase == TimeAwareExecutionPhase.Record &&
                timeState.IsPaused)
            {
                UpdateModeCache(rewindState.Mode);
                return false;
            }

            var previous = _lastMode;
            bool modeChanged = !_initialised || rewindState.Mode != _lastMode;
            UpdateModeCache(rewindState.Mode);

            context = new TimeAwareContext
            {
                Time = timeState,
                Rewind = rewindState,
                Phase = phase,
                ModeChangedThisFrame = modeChanged,
                PreviousMode = previous
            };

            return true;
        }

        public void Reset()
        {
            _initialised = false;
            _lastMode = RewindMode.Record;
        }

        private void UpdateModeCache(RewindMode mode)
        {
            _lastMode = mode;
            _initialised = true;
        }

        private static TimeAwareExecutionPhase ConvertModeToPhase(RewindMode mode)
        {
            return mode switch
            {
                RewindMode.Record => TimeAwareExecutionPhase.Record,
                RewindMode.CatchUp => TimeAwareExecutionPhase.CatchUp,
                RewindMode.Playback => TimeAwareExecutionPhase.Playback,
                _ => TimeAwareExecutionPhase.None
            };
        }
    }

    public static class TimeAwareUtility
    {
        private struct EntityComparer : IComparer<Entity>
        {
            public int Compare(Entity x, Entity y)
            {
                var indexCompare = x.Index.CompareTo(y.Index);
                return indexCompare != 0 ? indexCompare : x.Version.CompareTo(y.Version);
            }
        }

        public static void SortEntities(NativeArray<Entity> entities)
        {
            NativeSortExtension.Sort(entities, new EntityComparer());
        }
    }
}
