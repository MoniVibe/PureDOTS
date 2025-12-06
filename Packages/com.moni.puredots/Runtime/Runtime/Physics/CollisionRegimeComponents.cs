using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Collision regime enum - determines which collision system handles the entity.
    /// </summary>
    public enum CollisionRegime : byte
    {
        /// <summary>
        /// Micro regime: objects < 100m radius.
        /// Uses Newtonian rigid-body impact physics.
        /// </summary>
        Micro = 0,

        /// <summary>
        /// Meso regime: objects 100m - 10km radius.
        /// Uses cratering / momentum transfer physics.
        /// </summary>
        Meso = 1,

        /// <summary>
        /// Macro regime: objects > 10km radius (moons, planets).
        /// Uses hydrodynamic approximation (SPH or energy map).
        /// </summary>
        Macro = 2
    }

    /// <summary>
    /// Collision properties for regime selection and physics calculations.
    /// Required component for all entities that participate in collision detection.
    /// 
    /// Regime is automatically computed by CollisionRegimeSelectorSystem based on Radius:
    /// - Radius < 100m → Micro regime
    /// - Radius 100m-10km → Meso regime  
    /// - Radius > 10km → Macro regime
    /// 
    /// Usage:
    /// - Add via CollisionPropertiesAuthoring component in Unity Editor
    /// - Or add at runtime: EntityManager.AddComponent(entity, new CollisionProperties { Radius = 1f, Mass = 1000f })
    /// </summary>
    public struct CollisionProperties : IComponentData
    {
        /// <summary>
        /// Collision radius in meters.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Mass in kilograms.
        /// </summary>
        public float Mass;

        /// <summary>
        /// Regime threshold radius (100m for Micro/Meso, 10km for Meso/Macro).
        /// </summary>
        public float RegimeThreshold;

        /// <summary>
        /// Current collision regime (computed by CollisionRegimeSelectorSystem).
        /// </summary>
        public CollisionRegime Regime;
    }

    /// <summary>
    /// Structural integrity for Micro regime entities.
    /// Tracks damage from collisions (0 = destroyed, 1 = pristine).
    /// </summary>
    public struct StructuralIntegrity : IComponentData
    {
        /// <summary>
        /// Integrity value from 0.0 (destroyed) to 1.0 (pristine).
        /// </summary>
        public float Value;

        /// <summary>
        /// Maximum integrity (for regeneration/repair systems).
        /// </summary>
        public float MaxValue;
    }

    /// <summary>
    /// Crater state for Meso regime impacts.
    /// Tracks crater formation and ejecta mass.
    /// </summary>
    public struct CraterState : IComponentData
    {
        /// <summary>
        /// Crater radius in meters.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Ejecta mass in kilograms.
        /// </summary>
        public float EjectaMass;

        /// <summary>
        /// Impact position in world space.
        /// </summary>
        public float3 ImpactPosition;

        /// <summary>
        /// Tick when crater was formed.
        /// </summary>
        public uint FormationTick;
    }

    /// <summary>
    /// Thermal state for Macro regime entities (planets, moons).
    /// Tracks temperature, melt percentage, and atmosphere loss.
    /// </summary>
    public struct ThermoState : IComponentData
    {
        /// <summary>
        /// Temperature in Kelvin.
        /// </summary>
        public float Temperature;

        /// <summary>
        /// Crust melt percentage (0.0 to 1.0).
        /// </summary>
        public float MeltPercentage;

        /// <summary>
        /// Atmosphere mass in kilograms.
        /// </summary>
        public float AtmosphereMass;

        /// <summary>
        /// Base temperature before impacts (for recovery calculations).
        /// </summary>
        public float BaseTemperature;
    }

    /// <summary>
    /// Standardized impact event emitted by all collision regimes.
    /// Routes to appropriate impact system based on regime.
    /// 
    /// Created by ImpactEventRouterSystem from Unity Physics CollisionEvent.
    /// Consumed by MicroCollisionSystem, MesoCollisionSystem, MacroCollisionSystem.
    /// 
    /// Usage:
    /// - Query buffer: SystemAPI.GetBuffer&lt;ImpactEvent&gt;(entity)
    /// - Process events each frame and clear buffer to avoid accumulation
    /// - Check Regime field to determine which system handled the collision
    /// </summary>
    public struct ImpactEvent : IBufferElementData
    {
        /// <summary>
        /// Entity A (projectile/impactor).
        /// </summary>
        public Entity A;

        /// <summary>
        /// Entity B (target).
        /// </summary>
        public Entity B;

        /// <summary>
        /// Q value (impact energy per unit target mass) in J/kg.
        /// </summary>
        public float Q;

        /// <summary>
        /// Impact position in world space.
        /// </summary>
        public float3 Pos;

        /// <summary>
        /// Collision regime for this impact.
        /// </summary>
        public CollisionRegime Regime;

        /// <summary>
        /// Tick when impact occurred.
        /// </summary>
        public uint Tick;
    }
}

