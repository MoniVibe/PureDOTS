using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Math;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Routes physics collision events to ImpactEvent buffers and determines collision regime.
    /// Converts Unity Physics CollisionEvent to standardized ImpactEvent.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsPostEventSystemGroup))]
    [UpdateAfter(typeof(PhysicsEventSystem))]
    public partial struct ImpactEventRouterSystem : ISystem
    {
        private ComponentLookup<CollisionProperties> _collisionPropsLookup;
        private ComponentLookup<PhysicsMass> _massLookup;
        private ComponentLookup<PhysicsVelocity> _velocityLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<ImpactEvent> _impactEventLookup;
        private BufferLookup<PhysicsCollisionEventElement> _physicsEventLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _collisionPropsLookup = state.GetComponentLookup<CollisionProperties>(true);
            _massLookup = state.GetComponentLookup<PhysicsMass>(true);
            _velocityLookup = state.GetComponentLookup<PhysicsVelocity>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _impactEventLookup = state.GetBufferLookup<ImpactEvent>(false);
            _physicsEventLookup = state.GetBufferLookup<PhysicsCollisionEventElement>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var timeState = SystemAPI.GetSingleton<TimeState>();

            _collisionPropsLookup.Update(ref state);
            _massLookup.Update(ref state);
            _velocityLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _impactEventLookup.Update(ref state);
            _physicsEventLookup.Update(ref state);

            var job = new RouteImpactEventsJob
            {
                CollisionPropsLookup = _collisionPropsLookup,
                MassLookup = _massLookup,
                VelocityLookup = _velocityLookup,
                TransformLookup = _transformLookup,
                ImpactEventLookup = _impactEventLookup,
                PhysicsEventLookup = _physicsEventLookup,
                CurrentTick = timeState.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct RouteImpactEventsJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<CollisionProperties> CollisionPropsLookup;
            [ReadOnly] public ComponentLookup<PhysicsMass> MassLookup;
            [ReadOnly] public ComponentLookup<PhysicsVelocity> VelocityLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            public BufferLookup<ImpactEvent> ImpactEventLookup;
            [ReadOnly] public BufferLookup<PhysicsCollisionEventElement> PhysicsEventLookup;

            public uint CurrentTick;

            public void Execute(Entity entity, DynamicBuffer<PhysicsCollisionEventElement> physicsEvents)
            {
                // Only process collision events (not triggers)
                for (int i = 0; i < physicsEvents.Length; i++)
                {
                    var physicsEvent = physicsEvents[i];
                    if (physicsEvent.EventType != PhysicsCollisionEventType.Collision)
                        continue;

                    var otherEntity = physicsEvent.OtherEntity;

                    // Get collision properties for both entities
                    if (!CollisionPropsLookup.HasComponent(entity) || !CollisionPropsLookup.HasComponent(otherEntity))
                        continue;

                    var propsA = CollisionPropsLookup[entity];
                    var propsB = CollisionPropsLookup[otherEntity];

                    // Determine collision regime for this pair
                    var regime = CollisionPairRegimeSelectorSystem.SelectPairRegime(
                        propsA.Radius, propsB.Radius,
                        propsA.Regime, propsB.Regime);

                    // Compute Q value
                    var q = ComputeQValue(entity, otherEntity, propsA, propsB);

                    // Get impact position
                    var impactPos = physicsEvent.ContactPoint;
                    if (math.lengthsq(impactPos) < 0.001f)
                    {
                        // Fallback to midpoint between entities
                        if (TransformLookup.HasComponent(entity) && TransformLookup.HasComponent(otherEntity))
                        {
                            var posA = TransformLookup[entity].Position;
                            var posB = TransformLookup[otherEntity].Position;
                            impactPos = (posA + posB) * 0.5f;
                        }
                    }

                    // Add ImpactEvent to both entities' buffers
                    AddImpactEvent(entity, otherEntity, q, impactPos, regime);
                    AddImpactEvent(otherEntity, entity, q, impactPos, regime);
                }
            }

            [BurstCompile]
            private float ComputeQValue(Entity entityA, Entity entityB, CollisionProperties propsA, CollisionProperties propsB)
            {
                // Determine which is projectile (smaller mass) and which is target
                var massA = GetMass(entityA, propsA.Mass);
                var massB = GetMass(entityB, propsB.Mass);

                var massProjectile = massA < massB ? massA : massB;
                var massTarget = massA < massB ? massB : massA;

                // Get relative velocity
                var velocityA = VelocityLookup.HasComponent(entityA) ? VelocityLookup[entityA].Linear : float3.zero;
                var velocityB = VelocityLookup.HasComponent(entityB) ? VelocityLookup[entityB].Linear : float3.zero;
                var relativeVelocity = velocityA - velocityB;

                // Compute Q
                return CollisionMath.ComputeQ(massProjectile, massTarget, relativeVelocity);
            }

            [BurstCompile]
            private float GetMass(Entity entity, float fallbackMass)
            {
                if (MassLookup.HasComponent(entity))
                {
                    var mass = MassLookup[entity];
                    if (mass.InverseMass > 0f)
                        return 1f / mass.InverseMass;
                }
                return fallbackMass;
            }

            [BurstCompile]
            private void AddImpactEvent(Entity entity, Entity otherEntity, float q, float3 pos, CollisionRegime regime)
            {
                if (!ImpactEventLookup.HasBuffer(entity))
                    return;

                var buffer = ImpactEventLookup[entity];
                buffer.Add(new ImpactEvent
                {
                    A = entity,
                    B = otherEntity,
                    Q = q,
                    Pos = pos,
                    Regime = regime,
                    Tick = CurrentTick
                });
            }
        }
    }
}

