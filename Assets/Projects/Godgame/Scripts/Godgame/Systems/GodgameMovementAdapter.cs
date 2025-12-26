using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using PureDOTS.Systems;
using PureDOTS.Systems.Movement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Bridges MovementState.Vel to Godgame terrain movement.
    /// Implements ground movement policy: constrains to terrain surface,
    /// zeroes vertical velocity, keeps units upright (yaw-only rotation).
    /// 
    /// For flying units (with FlyingMovementTag), uses 2.5D movement with altitude control.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementIntegrateSystem))]
    public partial struct GodgameMovementAdapter : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var deltaTime = timeState.DeltaTime;

            var moistureGrid = default(MoistureGrid);
            SystemAPI.TryGetSingleton(out moistureGrid);
            var terrainPlane = default(TerrainHeightPlane);
            SystemAPI.TryGetSingleton(out terrainPlane);
            var flatSurface = default(TerrainFlatSurface);
            SystemAPI.TryGetSingleton(out flatSurface);
            var solidSphere = default(TerrainSolidSphere);
            SystemAPI.TryGetSingleton(out solidSphere);
            var terrainConfig = TerrainWorldConfig.Default;
            SystemAPI.TryGetSingleton(out terrainConfig);
            var globalTerrainVersion = 0u;
            if (SystemAPI.TryGetSingleton<TerrainVersion>(out var terrainVersion))
            {
                globalTerrainVersion = terrainVersion.Value;
            }

            var terrainContext = new TerrainQueryContext
            {
                MoistureGrid = moistureGrid,
                HeightPlane = terrainPlane,
                FlatSurface = flatSurface,
                SolidSphere = solidSphere,
                WorldConfig = terrainConfig,
                GlobalTerrainVersion = globalTerrainVersion
            };

            var groundConfigs = SystemAPI.GetComponentLookup<GroundMovementConfig>(true);
            groundConfigs.Update(ref state);

            // Process ground units (default behavior for Godgame)
            var groundJob = new GodgameGroundMovementJob
            {
                DeltaTime = deltaTime,
                TerrainContext = terrainContext,
                GroundConfigs = groundConfigs
            };
            state.Dependency = groundJob.ScheduleParallel(state.Dependency);

            // Process flying units (with FlyingMovementTag)
            var flyingJob = new GodgameFlyingMovementJob
            {
                DeltaTime = deltaTime,
                TerrainContext = terrainContext
            };
            state.Dependency = flyingJob.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Ground movement job: constrains units to terrain surface with yaw-only rotation.
        /// Uses GroundMovementTag or defaults to ground behavior if no movement tag present.
        /// </summary>
        [BurstCompile]
        [WithNone(typeof(FlyingMovementTag), typeof(SpaceMovementTag))]
        public partial struct GodgameGroundMovementJob : IJobEntity
        {
            public float DeltaTime;
            public TerrainQueryContext TerrainContext;
            [ReadOnly] public ComponentLookup<GroundMovementConfig> GroundConfigs;

            void Execute(
                Entity entity,
                ref MovementState movementState,
                ref LocalTransform transform,
                in MovementModelRef modelRef)
            {
                if (!modelRef.Blob.IsCreated)
                {
                    return;
                }

                ref var spec = ref modelRef.Blob.Value;

                // Ground movement: zero vertical velocity component
                float3 vel = movementState.Vel;
                vel.y = 0f;
                movementState.Vel = vel;

                // Apply ground friction
                if (spec.GroundFriction > 0f)
                {
                    float frictionFactor = 1f - (spec.GroundFriction * DeltaTime);
                    frictionFactor = math.max(0f, frictionFactor);
                    vel *= frictionFactor;
                    movementState.Vel = vel;
                }

                float heightOffset = 0f;
                if (GroundConfigs.HasComponent(entity))
                {
                    heightOffset = GroundConfigs[entity].HeightOffset;
                }

                var position = transform.Position;
                if (TerrainQueryFacade.TrySampleHeight(TerrainContext, position, out var groundHeight))
                {
                    position.y = groundHeight + heightOffset;
                    transform.Position = position;
                }

                // Update rotation to face velocity direction (yaw-only for ground units)
                float velLength = math.length(vel);
                if (velLength > 1e-6f)
                {
                    float3 forward = math.normalize(vel);
                    // Project forward onto XZ plane for yaw-only rotation
                    forward.y = 0f;
                    if (math.lengthsq(forward) > 1e-6f)
                    {
                        forward = math.normalize(forward);
                        // Use yaw-only rotation to keep ground units upright
                        // This extracts yaw and creates a rotation around world Y
                        OrientationHelpers.LookRotationSafe3D(forward, OrientationHelpers.WorldUp, out quaternion lookRot);
                        OrientationHelpers.ConstrainToYawOnly(lookRot, out quaternion yawOnlyRot);
                        transform.Rotation = yawOnlyRot;
                    }
                }
            }
        }

        /// <summary>
        /// Flying movement job: 2.5D movement with altitude control.
        /// Maintains heading but allows pitch for visuals.
        /// </summary>
        [BurstCompile]
        [WithAll(typeof(FlyingMovementTag))]
        public partial struct GodgameFlyingMovementJob : IJobEntity
        {
            public float DeltaTime;
            public TerrainQueryContext TerrainContext;

            void Execute(
                Entity entity,
                ref MovementState movementState,
                ref LocalTransform transform,
                in MovementModelRef modelRef,
                in FlyingMovementConfig flyingConfig)
            {
                if (!modelRef.Blob.IsCreated)
                {
                    return;
                }

                ref var spec = ref modelRef.Blob.Value;

                // Flying movement: allow vertical velocity but constrain altitude
                float3 vel = movementState.Vel;
                
                float terrainHeight = TerrainQueryFacade.SampleHeight(TerrainContext, transform.Position);
                
                float currentAltitude = transform.Position.y - terrainHeight;
                
                // Clamp altitude to configured range
                if (currentAltitude < flyingConfig.MinAltitude)
                {
                    vel.y = math.max(vel.y, flyingConfig.AltitudeChangeRate);
                }
                else if (currentAltitude > flyingConfig.MaxAltitude)
                {
                    vel.y = math.min(vel.y, -flyingConfig.AltitudeChangeRate);
                }
                
                movementState.Vel = vel;

                // Apply air friction (less than ground friction)
                float airFriction = spec.GroundFriction * 0.3f; // Flying units have less friction
                if (airFriction > 0f)
                {
                    float frictionFactor = 1f - (airFriction * DeltaTime);
                    frictionFactor = math.max(0f, frictionFactor);
                    vel *= frictionFactor;
                    movementState.Vel = vel;
                }

                // Update rotation to face velocity direction
                // For flying units, we allow pitch but constrain roll
                float velLength = math.length(vel);
                if (velLength > 1e-6f)
                {
                    float3 forward = math.normalize(vel);
                    // Use current up vector to preserve some orientation continuity
                    OrientationHelpers.DeriveUpFromRotation(transform.Rotation, OrientationHelpers.WorldUp, out float3 currentUp);
                    OrientationHelpers.LookRotationSafe3D(forward, currentUp, out quaternion newRotation);
                    transform.Rotation = newRotation;
                }
            }
        }
    }
}
