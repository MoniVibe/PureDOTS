using System.Threading;
using UnityEngine;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// Tracks headless exit intent to avoid bootstrapping new worlds during shutdown.
    /// </summary>
    public static class HeadlessExitState
    {
        private static int s_exitRequested;
        private static string s_reason;

        public static bool ExitRequested => s_exitRequested != 0;

        public static string ExitReason => s_reason ?? string.Empty;

        public static void SignalExit(string reason)
        {
            if (Interlocked.Exchange(ref s_exitRequested, 1) != 0)
            {
                return;
            }

            s_reason = reason ?? string.Empty;
            if (RuntimeMode.IsHeadless && Application.isBatchMode)
            {
                Debug.Log($"[HeadlessExitState] Exit requested. reason={s_reason}");
            }
        }
    }
}
