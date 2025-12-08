using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// Adds a Clear() extension to NativeArray to zero the buffer.
    /// </summary>
    public static class NativeArrayClearExtensions
    {
        public static unsafe void Clear<T>(this NativeArray<T> array)
            where T : struct
        {
            if (!array.IsCreated || array.Length == 0)
                return;

            void* ptr = array.GetUnsafePtr();
            long bytes = (long)array.Length * UnsafeUtility.SizeOf<T>();

            UnsafeUtility.MemClear(ptr, bytes);
        }
    }
}

