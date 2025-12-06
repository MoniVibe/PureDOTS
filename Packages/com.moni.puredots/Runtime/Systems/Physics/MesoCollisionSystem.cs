using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Math;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Meso collision system for objects 100m - 10km radius.
    /// Uses cratering / momentum transfer physics with crater generation and debris spawning.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MesoCollisionSystemGroup))]
    public partial struct MesoCollisionSystem : ISystem
    {
        private ComponentLookup<CollisionProperties> _collisionPropsLookup;
        private ComponentLookup<CraterState> _craterStateLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<ImpactEvent> _impactEventLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _collisionPropsLookup = state.GetComponentLookup<CollisionProperties>(true);
            _craterStateLookup = state.GetComponentLookup<CraterState>(false);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _impactEventLookup = state.GetBufferLookup<ImpactEvent>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var timeState = SystemAPI.GetSingleton<TimeState>();

            _collisionPropsLookup.Update(ref state);
            _craterStateLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _impactEventLookup.Update(ref state);

            var job = new ProcessMesoCollisionsJob
            {
                CollisionPropsLookup = _collisionPropsLookup,
                CraterStateLookup = _craterStateLookup,
                TransformLookup = _transformLookup,
                ImpactEventLookup = _impactEventLookup,
                CurrentTick = timeState.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(CollisionProperties))]
        partial struct ProcessMesoCollisionsJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<CollisionProperties> CollisionPropsLookup;
            public ComponentLookup<CraterState> CraterStateLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            public BufferLookup<ImpactEvent> ImpactEventLookup;

            public uint CurrentTick;

            public void Execute(Entity entity, ref CollisionProperties props)
            {
                // Only process meso regime entities
                if (props.Regime != CollisionRegime.Meso)
                    return;

                // Process impact events for this entity
                if (!ImpactEventLookup.HasBuffer(entity))
                    return;

                var impactEvents = ImpactEventLookup[entity];
                for (int i = 0; i < impactEvents.Length; i++)
                {
                    var impact = impactEvents[i];
                    if (impact.Regime != CollisionRegime.Meso)
                        continue;

                    ProcessImpact(entity, impact);
                }
            }

            [BurstCompile]
            private void ProcessImpact(Entity entity, ImpactEvent impact)
            {
                // Compute crater parameters from Q value
                var craterRadius = CollisionMath.ComputeCraterRadius(impact.Q);
                var ejectaMass = CollisionMath.ComputeEjectaMass(impact.Q);

                // Update or create crater state
                if (!CraterStateLookup.HasComponent(entity))
                {
                    // Add CraterState component
                    // Note: In real implementation, this would use EntityCommandBuffer
                    // For now, we'll assume it's added via authoring or another system
                    return;
                }

                var craterState = CraterStateLookup[entity];
                
                // Update crater if this impact creates a larger crater
                if (craterRadius > craterState.Radius)
                {
                    craterState.Radius = craterRadius;
                    craterState.EjectaMass += ejectaMass;
                    craterState.ImpactPosition = impact.Pos;
                    craterState.FormationTick = CurrentTick;
                    CraterStateLookup[entity] = craterState;
                }
                else
                {
                    // Add to existing ejecta mass
                    craterState.EjectaMass += ejectaMass;
                    CraterStateLookup[entity] = craterState;
                }

                // Spawn debris (handled by presentation/debris system)
                // DebrisSpec would be used here to spawn particles
            }
        }
    }
}

