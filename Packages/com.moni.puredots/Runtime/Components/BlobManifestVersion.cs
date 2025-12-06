using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Singleton tracking blob manifest version for change detection.
    /// Incremented when blobs are reloaded from JSON manifests.
    /// </summary>
    public struct BlobManifestVersion : IComponentData
    {
        public uint Version;
        public uint LastReloadTick;
    }
}

