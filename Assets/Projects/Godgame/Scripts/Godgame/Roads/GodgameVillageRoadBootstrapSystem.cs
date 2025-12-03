using Godgame.Presentation;
using Godgame.Roads;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Spawns the initial road loop + stretch handles around each village center.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct GodgameVillageRoadBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GodgameRoadConfig>();
            state.RequireForUpdate<GodgameVillageCenter>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GodgameRoadConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (center, transform, entity) in SystemAPI
                         .Query<RefRW<GodgameVillageCenter>, RefRO<LocalTransform>>()
                         .WithNone<GodgameVillageRoadInitializedTag>()
                         .WithEntityAccess())
            {
                var centerPos = transform.ValueRO.Position;
                center.ValueRW.BaseHeight = centerPos.y;
                float radius = math.max(1f, center.ValueRO.RoadRingRadius);
                SpawnDirections(ref ecb, entity, centerPos, radius, config);

                ecb.AddComponent<GodgameVillageRoadInitializedTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        internal static void SpawnRoadSegment(ref EntityCommandBuffer ecb,
            Entity villageCenter,
            float3 start,
            float3 end,
            in GodgameRoadConfig config,
            bool autoBuilt)
        {
            var road = ecb.CreateEntity();
            var segment = new GodgameRoadSegment
            {
                VillageCenter = villageCenter,
                Start = start,
                End = end,
                Width = config.DefaultRoadWidth,
                Flags = autoBuilt ? GodgameRoadFlags.AutoBuilt : (byte)0
            };

            ecb.AddComponent(road, segment);
            ecb.AddComponent(road, LocalTransformIdentityFromSegment(segment));

            if (config.RoadDescriptor.IsValid)
            {
                var binding = GodgamePresentationBinding.Create(config.RoadDescriptor);
                binding.ScaleMultiplier = ComputeScaleMultiplier(segment, config);
                binding.Flags = GodgamePresentationFlagUtility.WithOverrides(false, true, false);
                ecb.AddComponent(road, binding);
            }

            // Handles for both endpoints
            SpawnHandle(ref ecb, road, villageCenter, segment.Start, 0, config);
            SpawnHandle(ref ecb, road, villageCenter, segment.End, 1, config);
        }

        private static void SpawnHandle(ref EntityCommandBuffer ecb,
            Entity road,
            Entity villageCenter,
            float3 position,
            byte endpoint,
            in GodgameRoadConfig config)
        {
            var handle = ecb.CreateEntity();
            ecb.AddComponent(handle, new GodgameRoadHandle
            {
                Road = road,
                Endpoint = endpoint
            });
            ecb.AddComponent(handle, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
            ecb.AddComponent(handle, new HandPickable
            {
                Mass = config.HandleMass,
                MaxHoldDistance = 10f,
                ThrowImpulseMultiplier = 0f,
                FollowLerp = config.HandleFollowLerp
            });

            if (config.HandleDescriptor.IsValid)
            {
                ecb.AddComponent(handle, GodgamePresentationBinding.Create(config.HandleDescriptor));
            }
        }

        internal static LocalTransform LocalTransformIdentityFromSegment(in GodgameRoadSegment segment)
        {
            float3 delta = segment.End - segment.Start;
            float length = math.max(0.1f, math.length(delta));
            float3 direction = math.normalizesafe(delta, new float3(0, 0, 1));
            float3 midpoint = (segment.Start + segment.End) * 0.5f;
            // Use 3D-aware rotation for road segments
            // TODO: Use terrain surface normal for proper alignment on slopes
            OrientationHelpers.LookRotationSafe3D(direction, OrientationHelpers.WorldUp, out quaternion rotation);

            return LocalTransform.FromPositionRotationScale(midpoint, rotation, length);
        }

        internal static float ComputeScaleMultiplier(in GodgameRoadSegment segment, in GodgameRoadConfig config)
        {
            float length = math.length(segment.End - segment.Start);
            return length / math.max(0.1f, config.RoadMeshBaseLength);
        }

        private static void SpawnDirections(ref EntityCommandBuffer ecb,
            Entity villageCenter,
            float3 centerPos,
            float radius,
            in GodgameRoadConfig config)
        {
            SpawnRoadAlong(ref ecb, villageCenter, centerPos, radius, new float3(1f, 0f, 0f), config);
            SpawnRoadAlong(ref ecb, villageCenter, centerPos, radius, new float3(-1f, 0f, 0f), config);
            SpawnRoadAlong(ref ecb, villageCenter, centerPos, radius, new float3(0f, 0f, 1f), config);
            SpawnRoadAlong(ref ecb, villageCenter, centerPos, radius, new float3(0f, 0f, -1f), config);
        }

        private static void SpawnRoadAlong(ref EntityCommandBuffer ecb,
            Entity villageCenter,
            float3 centerPos,
            float radius,
            float3 direction,
            in GodgameRoadConfig config)
        {
            float3 start = centerPos + direction * radius;
            float3 end = start + direction * config.InitialStretchLength;
            SpawnRoadSegment(ref ecb, villageCenter, start, end, config, false);
        }
    }
}
