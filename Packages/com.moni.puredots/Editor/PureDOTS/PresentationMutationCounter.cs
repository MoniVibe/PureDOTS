#if UNITY_EDITOR
using Unity.Collections;

namespace PureDOTS.Editor
{
    /// <summary>
    /// Editor-only counter to track structural mutations in presentation bridge systems.
    /// Used by tests to verify that mutations only occur at ECB boundaries.
    /// </summary>
    public static class PresentationMutationCounter
    {
        public static int BeginCalls;
        public static int EndCalls;

        public static void Reset()
        {
            BeginCalls = 0;
            EndCalls = 0;
        }

        public static void OnBegin()
        {
            BeginCalls++;
        }

        public static void OnEnd()
        {
            EndCalls++;
        }
    }
}
#endif

