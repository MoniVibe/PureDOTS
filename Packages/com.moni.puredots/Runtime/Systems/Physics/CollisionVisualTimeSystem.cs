using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Visual time-scaling system for collision presentation.
    /// Computes visual speed factor: visualSpeed = simSpeed / (1 + massScale * 0.001f)
    /// Large-mass collisions appear slow due to size/velocity perception.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PureDotsPresentationSystemGroup))]
    public partial struct CollisionVisualTimeSystem : ISystem
    {
        private ComponentLookup<CollisionProperties> _collisionPropsLookup;
        private ComponentLookup<VisualTimeScale> _visualTimeScaleLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _collisionPropsLookup = state.GetComponentLookup<CollisionProperties>(true);
            _visualTimeScaleLookup = state.GetComponentLookup<VisualTimeScale>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _collisionPropsLookup.Update(ref state);
            _visualTimeScaleLookup.Update(ref state);

            var simSpeed = 1f; // Simulation speed (1.0 = real-time)
            var job = new ComputeVisualTimeScaleJob
            {
                CollisionPropsLookup = _collisionPropsLookup,
                VisualTimeScaleLookup = _visualTimeScaleLookup,
                SimSpeed = simSpeed
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(CollisionProperties))]
        partial struct ComputeVisualTimeScaleJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<CollisionProperties> CollisionPropsLookup;
            public ComponentLookup<VisualTimeScale> VisualTimeScaleLookup;
            public float SimSpeed;

            public void Execute(Entity entity, ref CollisionProperties props)
            {
                // Compute mass scale (normalized by some reference mass, e.g., 1000 kg)
                var massScale = props.Mass / 1000f;

                // Compute visual speed: visualSpeed = simSpeed / (1 + massScale * 0.001f)
                var visualSpeed = SimSpeed / (1f + massScale * 0.001f);

                // Update or create VisualTimeScale component
                if (!VisualTimeScaleLookup.HasComponent(entity))
                {
                    // Component will be added by presentation bridge if needed
                    return;
                }

                var visualTimeScale = VisualTimeScaleLookup[entity];
                visualTimeScale.Scale = visualSpeed;
                VisualTimeScaleLookup[entity] = visualTimeScale;
            }
        }
    }

    /// <summary>
    /// Visual time scale component for presentation systems.
    /// Used by camera and particle systems to apply time scaling.
    /// </summary>
    public struct VisualTimeScale : IComponentData
    {
        /// <summary>
        /// Visual time scale factor.
        /// Applied to camera and particle animations.
        /// </summary>
        public float Scale;
    }
}

