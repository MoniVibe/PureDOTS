using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Blob asset containing behavior catalog with all behavior nodes.
    /// Read-only, shared by millions of entities.
    /// </summary>
    public struct BehaviorCatalogBlob
    {
        public BlobArray<BehaviorNode> Nodes;
    }

    /// <summary>
    /// Tactic success rate blob for fleet command learning.
    /// Maps culture IDs to tactic effectiveness weights.
    /// </summary>
    public struct TacticSuccessRateBlob { }
}
