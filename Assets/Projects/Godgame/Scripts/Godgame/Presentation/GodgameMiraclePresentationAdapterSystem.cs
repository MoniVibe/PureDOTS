using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Presentation
{
    /// <summary>
    /// Ensures miracle entities spawn the correct presentation prefab based on type + lifecycle.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PureDOTS.Systems.MiracleRegistrySystem))]
    public partial struct GodgameMiraclePresentationAdapterSystem : ISystem
    {
        private ComponentLookup<GodgamePresentationBinding> _bindingLookup;
        private EntityQuery _miracleQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bindingLookup = state.GetComponentLookup<GodgamePresentationBinding>();
            _miracleQuery = SystemAPI.QueryBuilder()
                .WithAll<MiracleDefinition, MiracleRuntimeState, LocalTransform>()
                .Build();

            state.RequireForUpdate(_miracleQuery);
            state.RequireForUpdate<PresentationCommandQueue>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _bindingLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (definition, runtime, entity) in SystemAPI
                         .Query<RefRO<MiracleDefinition>, RefRO<MiracleRuntimeState>>()
                         .WithEntityAccess())
            {
                var descriptor = GodgameMiraclePresentationDescriptors.ResolveMiracle(
                    definition.ValueRO.Type,
                    runtime.ValueRO.Lifecycle,
                    definition.ValueRO.CastingMode);

                if (!descriptor.IsValid)
                {
                    if (_bindingLookup.HasComponent(entity))
                    {
                        ecb.RemoveComponent<GodgamePresentationBinding>(entity);
                        ecb.AddComponent<GodgamePresentationDirtyTag>(entity);
                    }

                    continue;
                }

                float radius = runtime.ValueRO.CurrentRadius > 0f
                    ? runtime.ValueRO.CurrentRadius
                    : math.max(0.5f, definition.ValueRO.BaseRadius);
                bool overrideScale = math.abs(radius - 1f) > 1e-3f;
                bool tintOverride = GodgameMiraclePresentationDescriptors.TryGetTint(definition.ValueRO.Type, out var tint);

                var binding = new GodgamePresentationBinding
                {
                    Descriptor = descriptor,
                    PositionOffset = float3.zero,
                    RotationOffset = quaternion.identity,
                    ScaleMultiplier = radius,
                    Tint = tintOverride ? tint : float4.zero,
                    VariantSeed = math.hash(new uint2((uint)entity.Index, (uint)definition.ValueRO.Type)),
                    Flags = GodgamePresentationFlagUtility.WithOverrides(tintOverride, overrideScale, false)
                };

                GodgamePresentationBindingUtility.ApplyBinding(entity, binding, ref _bindingLookup, ecb);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
