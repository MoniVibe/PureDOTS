using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Shadow buffer for terraforming preview calculations.
    /// Runs calculations on a copy of climate fields; commit = swap buffers.
    /// </summary>
    public struct TerraformingPreviewBuffer : IComponentData
    {
        public BlobAssetReference<TerraformingPreviewBlob> ShadowBlob;
        public uint PreviewVersion;
        public uint LastPreviewTick;
    }

    /// <summary>
    /// Blob payload storing shadow copies of environment fields for preview.
    /// </summary>
    public struct TerraformingPreviewBlob
    {
        public BlobArray<float> ShadowTemperature;
        public BlobArray<float> ShadowMoisture;
        public BlobArray<float> ShadowLight;
        public BlobArray<float> ShadowChemical;
    }

    /// <summary>
    /// Flag indicating preview mode is active.
    /// </summary>
    public struct TerraformingPreviewActiveTag : IComponentData
    {
    }

    /// <summary>
    /// Command to commit preview changes (swap shadow buffer to live).
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct TerraformingPreviewCommitCommand : IBufferElementData
    {
        public byte Commit; // 1 = commit, 0 = cancel
    }
}

