using UnityEngine;
using UnityEngine.Rendering;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// Shared runtime helpers for checking execution environment (editor/headless/server).
    /// </summary>
    public static class RuntimeMode
    {
        /// <summary>
        /// True when running in headless or server contexts where graphics are unavailable.
        /// </summary>
        public static bool IsHeadless
        {
            get
            {
#if UNITY_SERVER
                return true;
#else
                return Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
#endif
            }
        }
    }
}
