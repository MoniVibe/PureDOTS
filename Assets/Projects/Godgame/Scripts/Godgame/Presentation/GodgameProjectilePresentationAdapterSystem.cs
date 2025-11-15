using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Presentation
{
    /// <summary>
    /// Applies presentation bindings to miracle projectiles/tokens so VFX + meshes can be swapped freely.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PureDOTS.Systems.HandMiracleSystem))]
    public partial struct GodgameProjectilePresentationAdapterSystem : ISystem
    {
        private ComponentLookup<GodgamePresentationBinding> _bindingLookup;
        private EntityQuery _projectileQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bindingLookup = state.GetComponentLookup<GodgamePresentationBinding>();
            _projectileQuery = SystemAPI.QueryBuilder()
                .WithAll<MiracleToken, LocalTransform>()
                .Build();

            state.RequireForUpdate(_projectileQuery);
            state.RequireForUpdate<PresentationCommandQueue>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _bindingLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (token, entity) in SystemAPI
                         .Query<RefRO<MiracleToken>>()
                         .WithEntityAccess())
            {
                var descriptor = GodgameMiraclePresentationDescriptors.ResolveProjectile(token.ValueRO.Type);
                if (!descriptor.IsValid)
                {
                    if (_bindingLookup.HasComponent(entity))
                    {
                        ecb.RemoveComponent<GodgamePresentationBinding>(entity);
                        ecb.AddComponent<GodgamePresentationDirtyTag>(entity);
                    }

                    continue;
                }

                bool tintOverride = GodgameMiraclePresentationDescriptors.TryGetTint(token.ValueRO.Type, out var tint);

                var binding = new GodgamePresentationBinding
                {
                    Descriptor = descriptor,
                    PositionOffset = float3.zero,
                    RotationOffset = quaternion.identity,
                    ScaleMultiplier = 1f,
                    Tint = tintOverride ? tint : float4.zero,
                    VariantSeed = math.hash(new uint2((uint)entity.Index, (uint)token.ValueRO.Type)),
                    Flags = GodgamePresentationFlagUtility.WithOverrides(tintOverride, false, false)
                };

                GodgamePresentationBindingUtility.ApplyBinding(entity, binding, ref _bindingLookup, ecb);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
