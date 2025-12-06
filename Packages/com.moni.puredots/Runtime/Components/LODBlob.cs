using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// BlobAsset structure for LOD thresholds.
    /// Defines distance and importance thresholds for LOD assignment.
    /// </summary>
    public struct LODThresholdBlob
    {
        public BlobString ProfileId;        // Profile identifier
        public float HighDistanceThreshold; // Distance threshold for High detail
        public float MediumDistanceThreshold; // Distance threshold for Medium detail
        public float LowDistanceThreshold;   // Distance threshold for Low detail
        public float HighImportanceThreshold; // Importance threshold for High detail
        public float MediumImportanceThreshold; // Importance threshold for Medium detail
        public float CPULoadHighThreshold;   // CPU load threshold to force High→Medium
        public float CPULoadMediumThreshold;  // CPU load threshold to force Medium→Low
    }

    /// <summary>
    /// Catalog of LOD threshold profiles.
    /// </summary>
    public struct LODThresholdCatalogBlob
    {
        public BlobArray<LODThresholdBlob> Profiles;
    }
}

