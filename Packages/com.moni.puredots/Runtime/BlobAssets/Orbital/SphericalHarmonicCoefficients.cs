using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.BlobAssets.Orbital
{
    /// <summary>
    /// Spherical harmonic coefficients for gravitational field approximation.
    /// Pre-baked l=2 (quadrupole) coefficients for constant-time lookup.
    /// Supports 6-DoF roll-and-pitch motion without full N-body cost.
    /// </summary>
    public struct SphericalHarmonicCoefficientsBlob
    {
        /// <summary>Mass of the body (kg).</summary>
        public double Mass;

        /// <summary>Gravitational constant * Mass (m³/s²).</summary>
        public double GM;

        /// <summary>Quadrupole coefficients (l=2, m=-2 to +2).</summary>
        public double C20; // zonal (m=0)
        public double C21, S21; // sectoral (m=1)
        public double C22, S22; // tesseral (m=2)

        /// <summary>
        /// Computes gravitational acceleration using spherical harmonics.
        /// a = -∇Φ(r) where Φ includes quadrupole terms.
        /// </summary>
        public double3 ComputeAcceleration(double3 position)
        {
            double r = math.length(position);
            if (r < 1e-6)
            {
                return double3.zero;
            }

            double3 rHat = position / r;
            double rSquared = r * r;
            double rCubed = rSquared * r;

            // Monopole term: -GM/r² * rHat
            double3 acceleration = -(GM / rSquared) * rHat;

            // Quadrupole terms (simplified - full expansion would include Legendre P₂)
            // For now, use simplified quadrupole contribution
            double cosTheta = rHat.z; // z-component = cos(θ)
            double sinTheta = math.sqrt(1.0 - cosTheta * cosTheta);
            double cosPhi = rHat.x / sinTheta;
            double sinPhi = rHat.y / sinTheta;

            // P₂(cos θ) = (3 cos²θ - 1) / 2
            double P2 = (3.0 * cosTheta * cosTheta - 1.0) / 2.0;

            // Quadrupole contribution (simplified)
            double quadrupoleScale = GM * C20 / (rCubed * rSquared);
            acceleration += quadrupoleScale * P2 * rHat;

            return acceleration;
        }
    }

    /// <summary>
    /// Component providing access to spherical harmonic coefficients blob.
    /// </summary>
    public struct SphericalHarmonicGravity : IComponentData
    {
        public BlobAssetReference<SphericalHarmonicCoefficientsBlob> Coefficients;

        public readonly bool IsCreated => Coefficients.IsCreated;
    }
}

