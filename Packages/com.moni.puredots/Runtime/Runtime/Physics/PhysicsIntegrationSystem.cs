using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Physics integration system computing mass-aware movement and fuel consumption.
    /// Calculates: acceleration = thrust / totalMass
    ///             turnRate = torque / (inertiaTensor.x + inertiaTensor.y + inertiaTensor.z)
    ///             fuelUse = thrust * Δv / engineEfficiency
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(StructuralIntegritySystem))]
    public partial struct PhysicsIntegrationSystem : ISystem
    {
        private EntityQuery _physicsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _physicsQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<PhysicsVelocity>(),
                ComponentType.ReadWrite<AppliedForces>(),
                ComponentType.ReadWrite<FuelConsumption>(),
                ComponentType.ReadOnly<MassComponent>(),
                ComponentType.ReadOnly<EngineReference>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<EngineCatalog>(out var engineCatalog))
            {
                return;
            }

            ref var catalogBlob = ref engineCatalog.Catalog.Value;
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var deltaTime = tickTimeState.FixedDeltaTime;

            var massLookup = state.GetComponentLookup<MassComponent>(true);

            var integrateJob = new IntegratePhysicsJob
            {
                EngineCatalog = catalogBlob,
                MassLookup = massLookup,
                DeltaTime = deltaTime,
                CurrentTick = tickTimeState.Tick,
                VelocityHandle = state.GetComponentTypeHandle<PhysicsVelocity>(false),
                ForcesHandle = state.GetComponentTypeHandle<AppliedForces>(false),
                FuelHandle = state.GetComponentTypeHandle<FuelConsumption>(false),
                EngineReferenceHandle = state.GetComponentTypeHandle<EngineReference>(true),
                EntityHandle = state.GetEntityTypeHandle()
            };

            state.Dependency = integrateJob.ScheduleParallel(_physicsQuery, state.Dependency);
        }

        [BurstCompile]
        private struct IntegratePhysicsJob : IJobChunk
        {
            [ReadOnly]
            public EngineCatalogBlob EngineCatalog;

            [ReadOnly]
            public ComponentLookup<MassComponent> MassLookup;

            public float DeltaTime;
            public uint CurrentTick;

            public ComponentTypeHandle<PhysicsVelocity> VelocityHandle;
            public ComponentTypeHandle<AppliedForces> ForcesHandle;
            public ComponentTypeHandle<FuelConsumption> FuelHandle;
            [ReadOnly]
            public ComponentTypeHandle<EngineReference> EngineReferenceHandle;
            [ReadOnly]
            public EntityTypeHandle EntityHandle;

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                ExecuteChunk(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            private void ExecuteChunk(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var velocities = chunk.GetNativeArray(VelocityHandle);
                var forces = chunk.GetNativeArray(ForcesHandle);
                var fuelConsumptions = chunk.GetNativeArray(FuelHandle);
                var engineRefs = chunk.GetNativeArray(EngineReferenceHandle);
                var entities = chunk.GetNativeArray(EntityHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var velocity = velocities[i];
                    var force = forces[i];
                    var fuelConsumption = fuelConsumptions[i];
                    var engineRef = engineRefs[i];

                    // Look up engine spec
                    EngineSpec engine = default;
                    bool foundEngine = false;
                    for (int j = 0; j < EngineCatalog.Engines.Length; j++)
                    {
                        if (EngineCatalog.Engines[j].EngineId.Equals(engineRef.EngineId))
                        {
                            engine = EngineCatalog.Engines[j];
                            foundEngine = true;
                            break;
                        }
                    }

                    if (!foundEngine || !MassLookup.HasComponent(entity))
                    {
                        velocities[i] = velocity;
                        forces[i] = force;
                        fuelConsumptions[i] = fuelConsumption;
                        continue;
                    }

                    var mass = MassLookup[entity];

                    // Calculate acceleration: acceleration = thrust / totalMass
                    var thrust = math.length(force.Force);
                    var acceleration = mass.Mass > 0f ? thrust / mass.Mass : 0f;
                    var accelerationVector = mass.Mass > 0f ? force.Force / mass.Mass : float3.zero;

                    // Update linear velocity
                    velocity.Linear += accelerationVector * DeltaTime;

                    // Calculate turn rate: turnRate = torque / (inertiaTensor.x + inertiaTensor.y + inertiaTensor.z)
                    var totalInertia = math.csum(mass.InertiaTensor);
                    var torqueMagnitude = math.length(force.Torque);
                    var turnRate = totalInertia > 0f ? torqueMagnitude / totalInertia : 0f;
                    var angularAcceleration = totalInertia > 0f ? force.Torque / totalInertia : float3.zero;

                    // Update angular velocity
                    velocity.Angular += angularAcceleration * DeltaTime;

                    // Calculate fuel use: fuelUse = thrust * Δv / engineEfficiency
                    var deltaV = math.length(accelerationVector) * DeltaTime;
                    var fuelUse = engine.FuelEfficiency > 0f ? (thrust * deltaV) / engine.FuelEfficiency : 0f;
                    fuelConsumption.ConsumptionRate = fuelUse / DeltaTime;
                    fuelConsumption.FuelUsed = fuelUse;
                    fuelConsumption.LastUpdateTick = CurrentTick;

                    // Reset forces for next frame
                    force.Force = float3.zero;
                    force.Torque = float3.zero;

                    velocities[i] = velocity;
                    forces[i] = force;
                    fuelConsumptions[i] = fuelConsumption;
                }
            }
        }
    }
}

