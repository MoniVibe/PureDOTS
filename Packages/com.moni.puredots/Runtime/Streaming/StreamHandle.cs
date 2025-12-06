using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Streaming
{
    /// <summary>
    /// Reference to an async-loaded asset.
    /// Burst-safe handle wrapper for ECS systems.
    /// </summary>
    public struct StreamHandle
    {
        /// <summary>
        /// Unique identifier for the stream handle.
        /// </summary>
        public ulong HandleId;

        /// <summary>
        /// Asset path or identifier.
        /// </summary>
        public FixedString512Bytes AssetPath;

        /// <summary>
        /// Load status: 0=pending, 1=loading, 2=ready, 3=error.
        /// </summary>
        public byte Status;

        /// <summary>
        /// Asset type identifier.
        /// </summary>
        public byte AssetType;

        public StreamHandle(ulong handleId, FixedString512Bytes assetPath, byte assetType)
        {
            HandleId = handleId;
            AssetPath = assetPath;
            Status = 0; // Pending
            AssetType = assetType;
        }
    }

    /// <summary>
    /// Burst-safe reference wrapper for stream handles.
    /// </summary>
    public struct StreamHandleRef : IComponentData
    {
        public NativeReference<StreamHandle> Handle;
    }
}

