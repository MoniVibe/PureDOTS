using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Streaming
{
    /// <summary>
    /// Request for async asset streaming.
    /// </summary>
    public struct StreamingAssetRequest : IBufferElementData
    {
        /// <summary>
        /// Asset path or identifier.
        /// </summary>
        public FixedString512Bytes AssetPath;

        /// <summary>
        /// Asset type (0=terrain, 1=texture, 2=audio, etc.).
        /// </summary>
        public byte AssetType;

        /// <summary>
        /// Priority (0-255, higher = more important).
        /// </summary>
        public byte Priority;

        /// <summary>
        /// Requesting entity (for callbacks).
        /// </summary>
        public Entity Requester;

        public StreamingAssetRequest(FixedString512Bytes assetPath, byte assetType, byte priority, Entity requester)
        {
            AssetPath = assetPath;
            AssetType = assetType;
            Priority = priority;
            Requester = requester;
        }
    }
}

