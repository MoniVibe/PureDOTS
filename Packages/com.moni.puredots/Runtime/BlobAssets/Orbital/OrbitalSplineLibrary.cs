using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.BlobAssets.Orbital
{
    /// <summary>
    /// Pre-computed orbital trajectory stored as parametric spline.
    /// Stable orbits are pre-computed and stored, then evaluated at runtime.
    /// Each system loads its relevant spline set, adjusting orientation with 6-DoF frame data.
    /// </summary>
    public struct OrbitalSplineBlob
    {
        /// <summary>Control points for the spline (position).</summary>
        public BlobArray<float3> ControlPoints;

        /// <summary>Knot vector for the spline.</summary>
        public BlobArray<float> Knots;

        /// <summary>Period of the orbit in seconds.</summary>
        public float Period;

        /// <summary>Starting time offset.</summary>
        public float TimeOffset;

        /// <summary>
        /// Evaluates the orbit position at time t.
        /// </summary>
        public float3 EvaluateOrbit(float t)
        {
            if (ControlPoints.Length == 0)
            {
                return float3.zero;
            }

            // Normalize time to [0, Period]
            float normalizedTime = (t + TimeOffset) % Period;
            if (normalizedTime < 0f)
            {
                normalizedTime += Period;
            }

            // Simple linear interpolation between control points for now
            // Full implementation would use cubic spline interpolation
            float splineT = normalizedTime / Period;
            float pointIndex = splineT * (ControlPoints.Length - 1);
            int index0 = (int)math.floor(pointIndex);
            int index1 = math.min(index0 + 1, ControlPoints.Length - 1);
            float fraction = pointIndex - index0;

            return math.lerp(ControlPoints[index0], ControlPoints[index1], fraction);
        }
    }

    /// <summary>
    /// Component providing access to orbital spline library blob.
    /// </summary>
    public struct OrbitalSplineLibrary : IComponentData
    {
        public BlobAssetReference<OrbitalSplineBlob> Spline;

        public readonly bool IsCreated => Spline.IsCreated;
    }
}

