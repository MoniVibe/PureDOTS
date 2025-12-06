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
    /// Micro collision system for objects < 100m radius.
    /// Uses 6-DoF momentum conservation, velocity clamping, and structural integrity updates.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MicroCollisionSystemGroup))]
    public partial struct MicroCollisionSystem : ISystem
    {
        private ComponentLookup<PhysicsVelocity> _velocityLookup;
        private ComponentLookup<PhysicsMass> _massLookup;
        private ComponentLookup<CollisionProperties> _collisionPropsLookup;
        private ComponentLookup<StructuralIntegrity> _integrityLookup;
        private BufferLookup<ImpactEvent> _impactEventLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _velocityLookup = state.GetComponentLookup<PhysicsVelocity>(false);
            _massLookup = state.GetComponentLookup<PhysicsMass>(true);
            _collisionPropsLookup = state.GetComponentLookup<CollisionProperties>(true);
            _integrityLookup = state.GetComponentLookup<StructuralIntegrity>(false);
            _impactEventLookup = state.GetBufferLookup<ImpactEvent>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var timeState = SystemAPI.GetSingleton<TimeState>();

            _velocityLookup.Update(ref state);
            _massLookup.Update(ref state);
            _collisionPropsLookup.Update(ref state);
            _integrityLookup.Update(ref state);
            _impactEventLookup.Update(ref state);

            var job = new ProcessMicroCollisionsJob
            {
                VelocityLookup = _velocityLookup,
                MassLookup = _massLookup,
                CollisionPropsLookup = _collisionPropsLookup,
                IntegrityLookup = _integrityLookup,
                ImpactEventLookup = _impactEventLookup,
                CurrentTick = timeState.Tick,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(CollisionProperties))]
        [WithChangeFilter(typeof(CollisionProperties))]
        partial struct ProcessMicroCollisionsJob : IJobEntity
        {
            public ComponentLookup<PhysicsVelocity> VelocityLookup;
            [ReadOnly] public ComponentLookup<PhysicsMass> MassLookup;
            [ReadOnly] public ComponentLookup<CollisionProperties> CollisionPropsLookup;
            public ComponentLookup<StructuralIntegrity> IntegrityLookup;
            public BufferLookup<ImpactEvent> ImpactEventLookup;
            public uint CurrentTick;
            public float DeltaTime;

            public void Execute(Entity entity, ref CollisionProperties props)
            {
                // Only process micro regime entities
                if (props.Regime != CollisionRegime.Micro)
                    return;

                // Process impact events for this entity
                if (!ImpactEventLookup.HasBuffer(entity))
                    return;

                var impactEvents = ImpactEventLookup[entity];
                for (int i = 0; i < impactEvents.Length; i++)
                {
                    var impact = impactEvents[i];
                    if (impact.Regime != CollisionRegime.Micro)
                        continue;

                    ProcessImpact(entity, impact);
                }
            }

            [BurstCompile]
            private void ProcessImpact(Entity entity, ImpactEvent impact)
            {
                // Get collision properties for both entities
                if (!CollisionPropsLookup.HasComponent(impact.A) || !CollisionPropsLookup.HasComponent(impact.B))
                    return;

                var propsA = CollisionPropsLookup[impact.A];
                var propsB = CollisionPropsLookup[impact.B];

                // Determine which entity is this one
                bool isA = entity == impact.A;
                var otherEntity = isA ? impact.B : impact.A;
                var otherProps = isA ? propsB : propsA;

                // Get velocities
                if (!VelocityLookup.HasComponent(entity) || !VelocityLookup.HasComponent(otherEntity))
                    return;

                var velocityA = VelocityLookup[entity];
                var velocityB = VelocityLookup[otherEntity];

                // Get masses
                float massA = GetMass(entity);
                float massB = GetMass(otherEntity);

                if (massA <= 0f || massB <= 0f)
                    return;

                // Compute collision normal (from impact position to entity center)
                // For simplicity, use relative velocity direction as normal approximation
                var relativeVel = velocityA.Linear - velocityB.Linear;
                var normal = math.normalize(relativeVel);
                if (math.lengthsq(normal) < 0.001f)
                    normal = new float3(0f, 1f, 0f); // Default up vector

                // Apply momentum conservation
                CollisionMath.ComputeMomentumConservation(
                    velocityA.Linear, velocityB.Linear,
                    massA, massB,
                    normal,
                    out var vAOut, out var vBOut);

                // Clamp velocities to prevent numeric blow-ups
                var escapeVelocity = 10000f; // 10 km/s escape velocity
                var terminalVelocity = 5000f; // 5 km/s terminal velocity
                vAOut = CollisionMath.ClampVelocity(vAOut, escapeVelocity, terminalVelocity);
                vBOut = CollisionMath.ClampVelocity(vBOut, escapeVelocity, terminalVelocity);

                // Update velocities
                var velA = VelocityLookup[entity];
                velA.Linear = vAOut;
                VelocityLookup[entity] = velA;

                var velB = VelocityLookup[otherEntity];
                velB.Linear = vBOut;
                VelocityLookup[otherEntity] = velB;

                // Update structural integrity based on Q value
                if (IntegrityLookup.HasComponent(entity))
                {
                    var integrity = IntegrityLookup[entity];
                    var damage = ComputeDamage(impact.Q);
                    integrity.Value = math.max(0f, integrity.Value - damage);
                    IntegrityLookup[entity] = integrity;
                }

                // Emit elastic bounce event if Q < threshold
                if (impact.Q < CollisionMath.Q_THRESHOLD_ELASTIC)
                {
                    // Event already in buffer, no additional action needed
                }
            }

            [BurstCompile]
            private float GetMass(Entity entity)
            {
                if (!MassLookup.HasComponent(entity))
                {
                    // Try to get from CollisionProperties
                    if (CollisionPropsLookup.HasComponent(entity))
                        return CollisionPropsLookup[entity].Mass;
                    return 1f; // Default mass
                }

                var mass = MassLookup[entity];
                if (mass.InverseMass <= 0f)
                    return float.MaxValue; // Infinite mass

                return 1f / mass.InverseMass;
            }

            [BurstCompile]
            private float ComputeDamage(float q)
            {
                // Damage scales with Q value
                // Q < 10³: minimal damage (0.01)
                // Q = 10⁶: significant damage (0.1)
                // Q > 10⁶: catastrophic damage (0.5+)
                if (q < CollisionMath.Q_THRESHOLD_ELASTIC)
                    return 0.01f;

                if (q < CollisionMath.Q_THRESHOLD_CATASTROPHIC)
                    return 0.1f * (q / CollisionMath.Q_THRESHOLD_CATASTROPHIC);

                return 0.5f + 0.5f * math.min(1f, (q - CollisionMath.Q_THRESHOLD_CATASTROPHIC) / CollisionMath.Q_THRESHOLD_CATASTROPHIC);
            }
        }
    }
}

