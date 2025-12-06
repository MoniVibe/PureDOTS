using Unity.Entities;

namespace PureDOTS.Runtime.Components.Orbital
{
    /// <summary>
    /// Spherical shell membership for spatial partitioning.
    /// Replaces disc-grid spatial queries with radius-based shell lookups.
    /// </summary>
    public struct ShellMembership : IComponentData
    {
        /// <summary>Shell index (0=core, 1=inner, 2=outer).</summary>
        public int ShellIndex;

        /// <summary>Inner radius of shell in meters.</summary>
        public double InnerRadius;

        /// <summary>Outer radius of shell in meters.</summary>
        public double OuterRadius;

        /// <summary>Update frequency in Hz (core=1Hz, inner=0.1Hz, outer=0.01Hz).</summary>
        public float UpdateFrequency;

        /// <summary>Last update tick.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Shell types for different resolution levels.
    /// </summary>
    public enum ShellType : byte
    {
        Core = 0,    // Black-hole zone, high resolution, 1 Hz
        Inner = 1,  // Dense systems, medium resolution, 0.1 Hz
        Outer = 2   // Rogue stars, low resolution, 0.01 Hz
    }
}

