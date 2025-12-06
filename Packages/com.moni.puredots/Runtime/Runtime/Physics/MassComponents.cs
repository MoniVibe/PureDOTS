using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Hierarchical mass component representing an object's mass properties.
    /// Stores mass, center of mass, and diagonalized inertia tensor.
    /// </summary>
    public struct MassComponent : IComponentData
    {
        /// <summary>
        /// Total mass in kg.
        /// </summary>
        public float Mass;

        /// <summary>
        /// Center of mass in local space.
        /// </summary>
        public float3 CenterOfMass;

        /// <summary>
        /// Diagonalized inertia tensor (Ixx, Iyy, Izz).
        /// </summary>
        public float3 InertiaTensor;
    }

    /// <summary>
    /// Tag component indicating mass needs recalculation.
    /// Added when cargo changes or mass-affecting components are modified.
    /// </summary>
    public struct MassDirtyTag : IComponentData
    {
    }

    /// <summary>
    /// Tag component indicating cargo has changed.
    /// Triggers mass recalculation for cargo-dependent entities.
    /// </summary>
    public struct CargoChangedTag : IComponentData
    {
    }

    /// <summary>
    /// Accumulator struct for parallel mass reduction.
    /// Used in hierarchical mass aggregation jobs.
    /// </summary>
    public struct MassAccumulator
    {
        public float TotalMass;
        public float3 WeightedPositionSum; // Mass * Position for COM calculation
        public float3 InertiaSum; // Accumulated inertia tensor components

        public void Add(in MassComponent mass, in float3 position)
        {
            TotalMass += mass.Mass;
            WeightedPositionSum += mass.Mass * position;
            
            // Accumulate inertia using parallel axis theorem
            // I = I_local + m * (r² * Identity - outer(r, r))
            float3 r = position - mass.CenterOfMass;
            float rSq = math.lengthsq(r);
            float3 inertiaOffset = mass.Mass * (rSq * new float3(1, 1, 1) - r * r);
            InertiaSum += mass.InertiaTensor + inertiaOffset;
        }

        public void Combine(in MassAccumulator other)
        {
            TotalMass += other.TotalMass;
            WeightedPositionSum += other.WeightedPositionSum;
            InertiaSum += other.InertiaSum;
        }

        public MassComponent ToMassComponent()
        {
            var com = TotalMass > 0 ? WeightedPositionSum / TotalMass : float3.zero;
            return new MassComponent
            {
                Mass = TotalMass,
                CenterOfMass = com,
                InertiaTensor = InertiaSum
            };
        }
    }
}

