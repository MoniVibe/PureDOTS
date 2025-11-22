using PureDOTS.Runtime.Components;
using PureDOTS.Systems.Environment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Presentation
{
    /// <summary>
    /// Applies presentation bindings to rain cloud entities so placeholder VFX can be swapped with authored assets.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MoistureRainSystem))]
    public partial struct GodgameRainCloudPresentationAdapterSystem : ISystem
    {
        private ComponentLookup<GodgamePresentationBinding> _bindingLookup;
        private EntityQuery _rainCloudQuery;

        public void OnCreate(ref SystemState state)
        {
            _bindingLookup = state.GetComponentLookup<GodgamePresentationBinding>();
            _rainCloudQuery = SystemAPI.QueryBuilder()
                .WithAll<RainCloudTag, RainCloudState, RainCloudConfig, LocalTransform>()
                .Build();

            state.RequireForUpdate(_rainCloudQuery);
            state.RequireForUpdate<PresentationCommandQueue>();
        }

        public void OnUpdate(ref SystemState state)
        {
            _bindingLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (cloudState, cloudConfig, entity) in SystemAPI
                         .Query<RefRO<RainCloudState>, RefRO<RainCloudConfig>>()
                         .WithAll<RainCloudTag>()
                         .WithEntityAccess())
            {
                float capacity = math.max(0.01f, cloudConfig.ValueRO.MoistureCapacity);
                float ratio = math.saturate(cloudState.ValueRO.MoistureRemaining / capacity);
                var descriptor = GodgameRainCloudPresentationDescriptors.Resolve(ratio);

                if (!descriptor.IsValid)
                {
                    if (_bindingLookup.HasComponent(entity))
                    {
                        ecb.RemoveComponent<GodgamePresentationBinding>(entity);
                        ecb.AddComponent<GodgamePresentationDirtyTag>(entity);
                    }

                    continue;
                }

                float radius = cloudState.ValueRO.ActiveRadius > 0f
                    ? cloudState.ValueRO.ActiveRadius
                    : math.max(2f, cloudConfig.ValueRO.BaseRadius);
                bool overrideScale = math.abs(radius - 1f) > 1e-3f;
                var tint = GodgameRainCloudPresentationDescriptors.EvaluateTint(ratio);

                var binding = new GodgamePresentationBinding
                {
                    Descriptor = descriptor,
                    PositionOffset = float3.zero,
                    RotationOffset = quaternion.identity,
                    ScaleMultiplier = radius,
                    Tint = tint,
                    VariantSeed = math.hash(new uint2((uint)entity.Index, (uint)math.round(ratio * 1000f))),
                    Flags = GodgamePresentationFlagUtility.WithOverrides(true, overrideScale, false)
                };

                GodgamePresentationBindingUtility.ApplyBinding(entity, binding, ref _bindingLookup, ecb);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    internal static class GodgameRainCloudPresentationDescriptors
    {
        private static readonly Hash128 Active = Compute("godgame.fx.raincloud.active");
        private static readonly Hash128 Dry = Compute("godgame.fx.raincloud.dry");

        public static Hash128 Resolve(float moistureRatio)
        {
            return moistureRatio <= 0.05f
                ? (Dry.IsValid ? Dry : Active)
                : (Active.IsValid ? Active : Dry);
        }

        public static float4 EvaluateTint(float moistureRatio)
        {
            var saturated = math.saturate(moistureRatio);
            var wetColor = new float4(0.35f, 0.6f, 0.95f, 0.85f);
            var dryColor = new float4(0.8f, 0.8f, 0.82f, 0.6f);
            return math.lerp(dryColor, wetColor, saturated);
        }

        private static Hash128 Compute(string key)
        {
            return PresentationKeyUtility.TryParseKey(key, out var hash, out _)
                ? hash
                : default;
        }
    }
}
