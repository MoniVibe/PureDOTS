using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Math;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Selects collision regime (Micro/Meso/Macro) based on object sizes.
    /// Computes radius ratios and tags entities with appropriate CollisionRegime component.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(MicroCollisionSystemGroup))]
    [UpdateBefore(typeof(MesoCollisionSystemGroup))]
    [UpdateBefore(typeof(MacroCollisionSystemGroup))]
    public partial struct CollisionRegimeSelectorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new SelectRegimeJob
            {
                CurrentTick = SystemAPI.GetSingleton<TimeState>().Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(CollisionProperties))]
        partial struct SelectRegimeJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(ref CollisionProperties props)
            {
                // Determine regime based on radius
                var regime = DetermineRegime(props.Radius);
                
                // Update regime if changed
                if (props.Regime != regime)
                {
                    props.Regime = regime;
                    props.RegimeThreshold = GetRegimeThreshold(regime);
                }
            }

            [BurstCompile]
            private CollisionRegime DetermineRegime(float radius)
            {
                if (radius < CollisionMath.REGIME_MICRO_MAX)
                    return CollisionRegime.Micro;
                
                if (radius < CollisionMath.REGIME_MACRO_MIN)
                    return CollisionRegime.Meso;
                
                return CollisionRegime.Macro;
            }

            [BurstCompile]
            private float GetRegimeThreshold(CollisionRegime regime)
            {
                return regime switch
                {
                    CollisionRegime.Micro => CollisionMath.REGIME_MICRO_MAX,
                    CollisionRegime.Meso => CollisionMath.REGIME_MESO_MAX,
                    CollisionRegime.Macro => CollisionMath.REGIME_MACRO_MIN,
                    _ => CollisionMath.REGIME_MICRO_MAX
                };
            }
        }
    }

    /// <summary>
    /// Helper system that selects collision regime for collision pairs.
    /// Computes radius ratio and determines which regime system should handle the collision.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(CollisionRegimeSelectorSystem))]
    public partial struct CollisionPairRegimeSelectorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // This system will be used by collision systems to determine
            // which regime to use for a collision pair.
            // Implementation deferred to collision systems that consume ImpactEvent.
        }

        /// <summary>
        /// Determines collision regime for a collision pair based on radius ratio.
        /// </summary>
        [BurstCompile]
        public static CollisionRegime SelectPairRegime(
            float radiusA, float radiusB,
            CollisionRegime regimeA, CollisionRegime regimeB)
        {
            // If both are same regime, use that
            if (regimeA == regimeB)
                return regimeA;

            // Compute radius ratio
            var radiusRatio = radiusA > radiusB 
                ? radiusA / radiusB 
                : radiusB / radiusA;

            // If ratio > 1e5, skip fine physics (use macro)
            if (radiusRatio > CollisionMath.RADIUS_RATIO_CULL_THRESHOLD)
                return CollisionRegime.Macro;

            // If either is macro, use macro
            if (regimeA == CollisionRegime.Macro || regimeB == CollisionRegime.Macro)
                return CollisionRegime.Macro;

            // If either is meso, use meso
            if (regimeA == CollisionRegime.Meso || regimeB == CollisionRegime.Meso)
                return CollisionRegime.Meso;

            // Both micro, use micro
            return CollisionRegime.Micro;
        }
    }
}

