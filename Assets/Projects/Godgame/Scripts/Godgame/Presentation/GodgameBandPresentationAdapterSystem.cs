using Godgame.Registry;
using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Presentation
{
    /// <summary>
    /// Emits presentation commands for band aggregates so the flag/overlay visuals stay aligned with simulation data.
    /// DISABLED: Using Unity default objects for now. Re-enable when custom visuals are ready.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BandAggregationSystem))]
    public partial struct GodgameBandPresentationAdapterSystem : ISystem
    {
        private EntityQuery _bandQuery;
        private ComponentLookup<PresentationHandle> _handleLookup;

        public void OnCreate(ref SystemState state)
        {
            _handleLookup = state.GetComponentLookup<PresentationHandle>();
            _bandQuery = SystemAPI.QueryBuilder()
                .WithAll<GodgameBand, LocalTransform>()
                .Build();

            state.RequireForUpdate(_bandQuery);
            state.RequireForUpdate<PresentationCommandQueue>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<PresentationCommandQueue>(out var queueEntity))
            {
                return;
            }

            var spawnBuffer = state.EntityManager.GetBuffer<PresentationSpawnRequest>(queueEntity);
            var recycleBuffer = state.EntityManager.GetBuffer<PresentationRecycleRequest>(queueEntity);

            _handleLookup.Update(ref state);

            foreach (var (band, transform, entity) in SystemAPI
                         .Query<RefRO<GodgameBand>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var bandData = band.ValueRO;
                var hasHandle = _handleLookup.HasComponent(entity);

                if (bandData.MemberCount <= 0)
                {
                    if (hasHandle)
                    {
                        recycleBuffer.Add(new PresentationRecycleRequest { Target = entity });
                    }

                    continue;
                }

                var descriptorHash = GodgameBandPresentationDescriptors.Resolve(bandData.StatusFlags);
                if (!descriptorHash.IsValid)
                {
                    if (hasHandle)
                    {
                        recycleBuffer.Add(new PresentationRecycleRequest { Target = entity });
                    }

                    continue;
                }

                var tintColor = GodgameBandPresentationDescriptors.EvaluateMoraleTint(bandData.Morale, bandData.Cohesion);
                var tintBucket = GodgameBandPresentationDescriptors.ComputeTintBucket(bandData.Morale, bandData.Cohesion);
                var variantSeed = ((uint)(bandData.BandId & 0xFFFF) << 8) | tintBucket;

                if (hasHandle)
                {
                    var handle = _handleLookup[entity];
                    if (handle.DescriptorHash == descriptorHash && handle.VariantSeed == variantSeed)
                    {
                        continue;
                    }

                    recycleBuffer.Add(new PresentationRecycleRequest { Target = entity });
                }

                var anchor = bandData.Anchor;
                if (math.lengthsq(anchor) < 1e-3f)
                {
                    anchor = transform.ValueRO.Position;
                }

                var facing = math.lengthsq(bandData.Facing) > 1e-4f
                    ? math.normalizesafe(bandData.Facing, new float3(0f, 0f, 1f))
                    : math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));

                var rotation = quaternion.LookRotationSafe(facing, math.up());
                var position = anchor + math.mul(rotation, new float3(0f, GodgameBandPresentationDescriptors.FlagHeight, 0f));

                spawnBuffer.Add(new PresentationSpawnRequest
                {
                    Target = entity,
                    DescriptorHash = descriptorHash,
                    Position = position,
                    Rotation = rotation,
                    ScaleMultiplier = GodgameBandPresentationDescriptors.FlagScale,
                    Tint = tintColor,
                    VariantSeed = variantSeed,
                    Flags = PresentationSpawnFlags.AllowPooling
                            | PresentationSpawnFlags.OverrideTint
                            | PresentationSpawnFlags.OverrideScale
                });
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }

    internal static class GodgameBandPresentationDescriptors
    {
        public const float FlagHeight = 2.2f;
        public const float FlagScale = 1.1f;

        private static readonly Unity.Entities.Hash128 Idle = Compute("godgame.band.idle");
        private static readonly Unity.Entities.Hash128 Moving = Compute("godgame.band.moving");
        private static readonly Unity.Entities.Hash128 Engaged = Compute("godgame.band.engaged");
        private static readonly Unity.Entities.Hash128 Routing = Compute("godgame.band.routing");
        private static readonly Unity.Entities.Hash128 Resting = Compute("godgame.band.resting");
        private static readonly Unity.Entities.Hash128 NeedsSupply = Compute("godgame.band.supply");

        public static Unity.Entities.Hash128 Resolve(BandStatusFlags flags)
        {
            if ((flags & BandStatusFlags.Engaged) != 0)
            {
                return Engaged;
            }

            if ((flags & BandStatusFlags.Routing) != 0)
            {
                return Routing;
            }

            if ((flags & BandStatusFlags.NeedsSupply) != 0)
            {
                return NeedsSupply;
            }

            if ((flags & BandStatusFlags.Moving) != 0)
            {
                return Moving;
            }

            if ((flags & BandStatusFlags.Resting) != 0)
            {
                return Resting;
            }

            return Idle;
        }

        public static float4 EvaluateMoraleTint(float morale, float cohesion)
        {
            var moraleT = math.saturate(morale / 100f);
            var cohesionT = math.saturate(cohesion / 100f);

            var low = new float3(0.75f, 0.15f, 0.1f);
            var high = new float3(0.15f, 0.85f, 0.35f);
            var color = math.lerp(low, high, moraleT);
            color = math.lerp(color * 0.65f, color, cohesionT);

            return new float4(color, 1f);
        }

        public static byte ComputeTintBucket(float morale, float cohesion)
        {
            var moraleBucket = (byte)math.clamp((int)math.floor(morale / 25f), 0, 4);
            var cohesionBucket = (byte)math.clamp((int)math.floor(cohesion / 25f), 0, 4);
            return (byte)((moraleBucket << 3) | cohesionBucket);
        }

        private static Unity.Entities.Hash128 Compute(string key)
        {
            return PresentationKeyUtility.TryParseKey(key, out var hash, out _)
                ? hash
                : default;
        }
    }
}
