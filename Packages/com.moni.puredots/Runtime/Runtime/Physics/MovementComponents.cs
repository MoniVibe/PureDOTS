using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Engine specification blob defining thrust, torque, and fuel efficiency.
    /// </summary>
    public struct EngineSpec
    {
        /// <summary>
        /// Engine identifier.
        /// </summary>
        public FixedString64Bytes EngineId;

        /// <summary>
        /// Maximum thrust in N (Newtons).
        /// </summary>
        public float Thrust;

        /// <summary>
        /// Maximum torque in N·m (Newton-meters).
        /// </summary>
        public float Torque;

        /// <summary>
        /// Fuel efficiency (thrust per unit fuel consumed).
        /// Higher values = more efficient.
        /// </summary>
        public float FuelEfficiency;

        /// <summary>
        /// Thrust vector direction (normalized).
        /// </summary>
        public float3 ThrustDirection;
    }

    /// <summary>
    /// Engine catalog blob containing engine specifications.
    /// </summary>
    public struct EngineCatalogBlob
    {
        public BlobArray<EngineSpec> Engines;
    }

    /// <summary>
    /// Singleton component holding the engine catalog reference.
    /// </summary>
    public struct EngineCatalog : IComponentData
    {
        public BlobAssetReference<EngineCatalogBlob> Catalog;
    }

    /// <summary>
    /// Component storing engine reference for an entity.
    /// </summary>
    public struct EngineReference : IComponentData
    {
        /// <summary>
        /// Engine identifier for lookup in catalog.
        /// </summary>
        public FixedString64Bytes EngineId;
    }

    /// <summary>
    /// Component storing physics velocity for mass-aware movement.
    /// </summary>
    public struct PhysicsVelocity : IComponentData
    {
        /// <summary>
        /// Linear velocity in m/s.
        /// </summary>
        public float3 Linear;

        /// <summary>
        /// Angular velocity in rad/s.
        /// </summary>
        public float3 Angular;
    }

    /// <summary>
    /// Component storing applied forces and torques.
    /// </summary>
    public struct AppliedForces : IComponentData
    {
        /// <summary>
        /// Applied force in N.
        /// </summary>
        public float3 Force;

        /// <summary>
        /// Applied torque in N·m.
        /// </summary>
        public float3 Torque;
    }

    /// <summary>
    /// Component tracking fuel consumption.
    /// </summary>
    public struct FuelConsumption : IComponentData
    {
        /// <summary>
        /// Current fuel consumption rate in units/second.
        /// </summary>
        public float ConsumptionRate;

        /// <summary>
        /// Total fuel consumed this tick.
        /// </summary>
        public float FuelUsed;

        /// <summary>
        /// Last update tick.
        /// </summary>
        public uint LastUpdateTick;
    }
}

