using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Component attached to entities that have weapons installed.
    /// </summary>
    public struct WeaponMount : IComponentData
    {
        public FixedString64Bytes WeaponId; // Reference to WeaponSpec
        public FixedString32Bytes TurretId; // Reference to TurretSpec (empty = fixed mount)
        public Entity TargetEntity; // Current target (Entity.Null if none)
        public float3 TargetPosition; // World-space target position
        public float LastFireTime; // Time of last shot
        public float HeatLevel; // Current heat (0-1)
        public float EnergyReserve; // Current energy available
        public bool IsFiring; // Whether weapon is currently firing
    }

    /// <summary>
    /// Component attached to active projectile entities.
    /// </summary>
    public struct ProjectileEntity : IComponentData
    {
        public FixedString64Bytes ProjectileId; // Reference to ProjectileSpec
        public Entity SourceEntity; // Entity that fired this projectile
        public Entity TargetEntity; // Target entity (for homing, Entity.Null for ballistic)
        public float3 Velocity; // Current velocity vector
        public float SpawnTime; // Time when projectile was created
        public float DistanceTraveled; // Total distance traveled
        public byte PierceCount; // Remaining pierce count
    }

    /// <summary>
    /// Component for turret traversal state.
    /// </summary>
    public struct TurretState : IComponentData
    {
        public FixedString32Bytes TurretId; // Reference to TurretSpec
        public quaternion CurrentRotation; // Current turret rotation
        public quaternion TargetRotation; // Desired rotation
        public float3 MuzzlePosition; // World-space muzzle position (updated by system)
        public float3 MuzzleForward; // World-space forward direction (updated by system)
    }

    /// <summary>
    /// Component marking entities that can be damaged.
    /// </summary>
    public struct Damageable : IComponentData
    {
        public float ShieldPoints;
        public float MaxShieldPoints;
        public float ArmorPoints;
        public float MaxArmorPoints;
        public float HullPoints;
        public float MaxHullPoints;
    }

    /// <summary>
    /// Component for impact effects queued by projectile systems.
    /// </summary>
    public struct ImpactEffectRequest : IComponentData
    {
        public float3 ImpactPosition;
        public float3 ImpactNormal;
        public FixedString64Bytes EffectId; // Presentation binding ID
        public float Magnitude; // Effect magnitude (for scaling)
    }

    /// <summary>
    /// Buffer element for queued projectile spawns.
    /// </summary>
    public struct ProjectileSpawnRequest : IBufferElementData
    {
        public FixedString64Bytes ProjectileId;
        public float3 SpawnPosition;
        public float3 SpawnDirection;
        public Entity SourceEntity;
        public Entity TargetEntity;
    }
}

