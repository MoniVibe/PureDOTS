using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectileKind = PureDOTS.Runtime.Combat.ProjectileKind;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Builds HazardSlice entries from projectile states and specs.
    /// Predicts danger envelopes for the next LookaheadSecMax seconds.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileEffectExecutionSystem))]
    public partial struct BuildHazardSlicesSystem : ISystem
    {
        private EntityQuery _hazardSliceBufferQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ProjectileEntity>();

            _hazardSliceBufferQuery = SystemAPI.QueryBuilder()
                .WithAll<HazardSlice>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog))
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;
            var deltaTime = timeState.DeltaTime;

            // Find or create hazard slice buffer singleton
            Entity sliceBufferEntity;
            if (!SystemAPI.TryGetSingletonEntity<HazardSliceBuffer>(out sliceBufferEntity))
            {
                sliceBufferEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<HazardSliceBuffer>(sliceBufferEntity);
                state.EntityManager.AddBuffer<HazardSlice>(sliceBufferEntity);
            }

            var sliceBuffer = SystemAPI.GetBuffer<HazardSlice>(sliceBufferEntity);
            sliceBuffer.Clear();

            // Find maximum lookahead from all avoidance profiles
            float maxLookaheadSec = 5f; // Default
            foreach (var profile in SystemAPI.Query<RefRO<AvoidanceProfile>>())
            {
                maxLookaheadSec = math.max(maxLookaheadSec, profile.ValueRO.LookaheadSec);
            }

            uint lookaheadTicks = (uint)math.ceil(maxLookaheadSec / deltaTime);

            // Use a temporary native list per chunk to collect slices
            // We'll use a single-threaded approach for now to avoid parallel write conflicts
            var tempSlices = new NativeList<HazardSlice>(Allocator.TempJob);

            var job = new BuildHazardSlicesJob
            {
                ProjectileCatalog = projectileCatalog.Catalog,
                CurrentTick = currentTick,
                LookaheadTicks = lookaheadTicks,
                DeltaTime = deltaTime,
                TempSlices = tempSlices
            };

            // Run single-threaded to avoid parallel write conflicts
            state.Dependency = job.Schedule(state.Dependency);
            state.Dependency.Complete();

            // Copy temp slices to buffer
            sliceBuffer.Clear();
            for (int i = 0; i < tempSlices.Length; i++)
            {
                sliceBuffer.Add(tempSlices[i]);
            }

            tempSlices.Dispose();
        }

        [BurstCompile]
        public partial struct BuildHazardSlicesJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<ProjectileCatalogBlob> ProjectileCatalog;
            public uint CurrentTick;
            public uint LookaheadTicks;
            public float DeltaTime;
            [NativeDisableParallelForRestriction] public NativeList<HazardSlice> TempSlices;

            void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                in ProjectileEntity projectile,
                in LocalTransform transform,
                in VelocitySample velocity)
            {
                // Find projectile spec
                if (!TryFindProjectileSpec(ProjectileCatalog, projectile.ProjectileId, out var spec))
                {
                    return;
                }

                float3 pos = transform.Position;
                float3 vel = projectile.Velocity;
                float speed = math.length(vel);

                if (speed < 1e-6f)
                {
                    return; // Stationary projectiles don't create hazards
                }

                // Determine hazard kind from projectile spec
                HazardKind kind = 0;
                if (spec.AoERadius > 0f)
                {
                    kind |= HazardKind.AoE;
                }
                if (spec.ChainRange > 0f)
                {
                    kind |= HazardKind.Chain;
                }
                if ((ProjectileKind)spec.Kind == ProjectileKind.Homing)
                {
                    kind |= HazardKind.Homing;
                }

                // Predict trajectory for lookahead period
                float lifetime = spec.Lifetime;
                float remainingLifetime = lifetime - projectile.Age;
                float predictionTime = math.min(remainingLifetime, LookaheadTicks * DeltaTime);

                if (predictionTime <= 0f)
                {
                    return;
                }

                // Create segment slice (current position to predicted end)
                var segmentSlice = new HazardSlice
                {
                    Center = pos,
                    Vel = vel,
                    Radius0 = spec.AoERadius,
                    RadiusGrow = 0f, // No growth during flight
                    StartTick = CurrentTick,
                    EndTick = CurrentTick + (uint)math.ceil(predictionTime / DeltaTime),
                    Kind = kind,
                    ChainRadius = spec.ChainRange,
                    ContagionProb = 0f, // TODO: Extract from EffectOp if Plague
                    HomingConeCos = (ProjectileKind)spec.Kind == ProjectileKind.Homing ? math.cos(math.radians(45f)) : 0f, // 45 degree cone
                    SprayVariance = 0f, // TODO: Extract from weapon spread
                    TeamMask = 0xFFFFFFFF, // TODO: Extract from projectile source team
                    Seed = projectile.Seed
                };

                // Add segment slice
                TempSlices.Add(segmentSlice);

                // If AoE projectile, create impact slice
                if (spec.AoERadius > 0f && remainingLifetime > 0f)
                {
                    float3 impactPos = pos + vel * remainingLifetime;
                    float impactRadius = spec.AoERadius;
                    float blastRadius = impactRadius * 2f; // Blast wave expands to 2x radius

                    var impactSlice = new HazardSlice
                    {
                        Center = impactPos,
                        Vel = float3.zero, // Stationary blast
                        Radius0 = impactRadius,
                        RadiusGrow = (blastRadius - impactRadius) / 0.1f, // Expand over 0.1 seconds
                        StartTick = CurrentTick + (uint)math.ceil(remainingLifetime / DeltaTime),
                        EndTick = CurrentTick + (uint)math.ceil((remainingLifetime + 0.5f) / DeltaTime), // 0.5s blast duration
                        Kind = HazardKind.AoE,
                        ChainRadius = 0f,
                        ContagionProb = 0f,
                        HomingConeCos = 0f,
                        SprayVariance = 0f,
                        TeamMask = 0xFFFFFFFF,
                        Seed = projectile.Seed
                    };

                    // Add impact slice
                    TempSlices.Add(impactSlice);
                }
            }

            private static bool TryFindProjectileSpec(
                BlobAssetReference<ProjectileCatalogBlob> catalog,
                FixedString64Bytes projectileId,
                out ProjectileSpec spec)
            {
                spec = default;
                if (!catalog.IsCreated)
                {
                    return false;
                }

                ref var catalogRef = ref catalog.Value;
                for (int i = 0; i < catalogRef.Projectiles.Length; i++)
                {
                    if (catalogRef.Projectiles[i].Id.Equals(projectileId))
                    {
                        spec = catalogRef.Projectiles[i];
                        return true;
                    }
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Tag component marking the entity that holds the hazard slice buffer.
    /// </summary>
    public struct HazardSliceBuffer : IComponentData { }
}

