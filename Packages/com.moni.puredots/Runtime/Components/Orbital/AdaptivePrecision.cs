using Unity.Entities;

namespace PureDOTS.Runtime.Components.Orbital
{
    /// <summary>
    /// Precision level for adaptive precision per distance scale.
    /// </summary>
    public enum PrecisionLevel : byte
    {
        Double = 0,  // Galactic centers (hundreds of kpc)
        Float = 1,   // System-scale (AU)
        Half = 2     // Local object transforms
    }

    /// <summary>
    /// Marks an entity's precision level for mixed-precision calculations.
    /// Vectors normalized before casting to maintain Burst determinism.
    /// </summary>
    public struct AdaptivePrecision : IComponentData
    {
        /// <summary>Precision level for this entity.</summary>
        public PrecisionLevel Level;

        /// <summary>Distance threshold for precision scaling (meters).</summary>
        public double DistanceThreshold;
    }
}

