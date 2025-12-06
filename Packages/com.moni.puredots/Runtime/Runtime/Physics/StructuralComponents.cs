using Unity.Entities;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Structural state component tracking stress, strain, and integrity.
    /// Used for damage from inertia shifts and heavy cargo.
    /// </summary>
    public struct StructuralState : IComponentData
    {
        /// <summary>
        /// Current stress in Pa (Force / Area).
        /// </summary>
        public float Stress;

        /// <summary>
        /// Current strain (Stress / YoungsModulus).
        /// </summary>
        public float Strain;

        /// <summary>
        /// Yield threshold in Pa. Material yields when Stress > YieldThreshold.
        /// </summary>
        public float YieldThreshold;

        /// <summary>
        /// Structural integrity (0-1, 1 = perfect, 0 = destroyed).
        /// </summary>
        public float Integrity;

        /// <summary>
        /// Cross-sectional area in m² for stress calculations.
        /// </summary>
        public float CrossSectionalArea;

        /// <summary>
        /// Last update tick.
        /// </summary>
        public uint LastUpdateTick;
    }
}

