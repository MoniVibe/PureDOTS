using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Minimal projectile component for ECS-optimized projectile system.
    /// All constants live in ProjectileSpec blob referenced by ArchetypeId.
    /// </summary>
    public struct Projectile : IComponentData
    {
        public float3 Position;
        public float3 Velocity;
        public quaternion Rotation;
        public float Lifetime;
        public ushort ArchetypeId; // Maps to ProjectileSpec in blob catalog
    }

    /// <summary>
    /// World RNG state singleton for deterministic projectile spread and damage variance.
    /// Updated each tick: Seed = hash(worldTick, previousSeed)
    /// </summary>
    public struct WorldRng : IComponentData
    {
        public uint Seed;
    }

    /// <summary>
    /// Projectile pool component - manages per-archetype entity pools.
    /// Stored on singleton entity managing all projectile pools.
    /// </summary>
    public struct ProjectilePool : IComponentData
    {
        public BlobAssetReference<ProjectileArchetypeCatalogBlob> ArchetypeCatalog;
    }

    /// <summary>
    /// Tag component for projectiles that are pooled (can be enabled/disabled).
    /// </summary>
    public struct PooledProjectileTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Component storing projectile metadata needed for hit processing and effects.
    /// Separated from minimal Projectile component for cache efficiency.
    /// </summary>
    public struct ProjectileMetadata : IComponentData
    {
        public Entity SourceEntity; // Entity that fired this projectile
        public Entity TargetEntity; // Target entity (for homing, Entity.Null for ballistic)
        public float3 PrevPos; // Previous position for continuous collision detection
        public float DistanceTraveled; // Total distance traveled
        public float HitsLeft; // Remaining pierce count
        public float Age; // Seconds since spawn
        public uint Seed; // Deterministic shot seed for damage/crit rolls
        public int ShotSequence; // Sequence number of the shot that spawned this projectile
        public int PelletIndex; // Index within spread pattern (0 for single shots)
    }
}

