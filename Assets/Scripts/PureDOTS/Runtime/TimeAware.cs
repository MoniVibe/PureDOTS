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
}
