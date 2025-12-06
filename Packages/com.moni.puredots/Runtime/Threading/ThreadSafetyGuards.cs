using System;
using System.Diagnostics;
using Unity.Burst;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Thread safety enforcement macros for deterministic order verification.
    /// Compiles out in release builds for zero overhead.
    /// </summary>
    public static class ThreadSafetyGuards
    {
        /// <summary>
        /// Asserts thread safety condition in debug builds only.
        /// </summary>
        [Conditional("THREAD_SAFETY_CHECK")]
        [Conditional("UNITY_EDITOR")]
        public static void AssertThreadSafe(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Thread safety violation: {message}");
            }
        }

        /// <summary>
        /// Asserts that no double writes occur.
        /// </summary>
        [Conditional("THREAD_SAFETY_CHECK")]
        [Conditional("UNITY_EDITOR")]
        public static void AssertNoDoubleWrite(bool isWrite, ref int writeCount, string location)
        {
            if (isWrite)
            {
                writeCount++;
                if (writeCount > 1)
                {
                    throw new InvalidOperationException($"Double write detected at {location}");
                }
            }
        }

        /// <summary>
        /// Asserts deterministic order.
        /// </summary>
        [Conditional("THREAD_SAFETY_CHECK")]
        [Conditional("UNITY_EDITOR")]
        public static void AssertDeterministicOrder(int expectedOrder, int actualOrder, string systemName)
        {
            if (actualOrder != expectedOrder)
            {
                throw new InvalidOperationException(
                    $"Deterministic order violation in {systemName}: expected {expectedOrder}, got {actualOrder}");
            }
        }

        /// <summary>
        /// Resets write count for next frame.
        /// </summary>
        [Conditional("THREAD_SAFETY_CHECK")]
        [Conditional("UNITY_EDITOR")]
        public static void ResetWriteCount(ref int writeCount)
        {
            writeCount = 0;
        }
    }
}

