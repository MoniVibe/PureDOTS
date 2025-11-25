using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Presentation
{
    /// <summary>
    /// Applies presentation bindings to miracle projectiles/tokens so VFX + meshes can be swapped freely.
    /// Also handles standard ProjectileEntity components for miracle projectiles that use the combat system.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MiracleReleaseSystem))]
    public partial struct GodgameProjectilePresentationAdapterSystem : ISystem
    {
        private ComponentLookup<GodgamePresentationBinding> _bindingLookup;
        private EntityQuery _miracleTokenQuery;
        private EntityQuery _projectileEntityQuery;

        public void OnCreate(ref SystemState state)
        {
            _bindingLookup = state.GetComponentLookup<GodgamePresentationBinding>();
            _miracleTokenQuery = SystemAPI.QueryBuilder()
                .WithAll<MiracleToken, LocalTransform>()
                .Build();
            _projectileEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<ProjectileEntity, LocalTransform>()
                .Build();

            state.RequireForUpdate<PresentationCommandQueue>();
        }

        public void OnUpdate(ref SystemState state)
        {
            _bindingLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Handle MiracleToken projectiles (existing behavior)
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

            // Handle ProjectileEntity components for miracle projectiles
            // Map ProjectileId to miracle type and apply appropriate presentation
            foreach (var (projectile, entity) in SystemAPI
                         .Query<RefRO<ProjectileEntity>>()
                         .WithEntityAccess())
            {
                // Check if this is a miracle projectile (projectile ID starts with "miracle." or similar pattern)
                // In practice, this would be determined by catalog or component tags
                // For now, we'll check if entity also has MiracleToken (hybrid case)
                bool isMiracleProjectile = state.EntityManager.HasComponent<MiracleToken>(entity);

                if (isMiracleProjectile)
                {
                    // Get miracle token to determine type
                    var token = state.EntityManager.GetComponentData<MiracleToken>(entity);
                    var descriptor = GodgameMiraclePresentationDescriptors.ResolveProjectile(token.Type);
                    
                    if (descriptor.IsValid)
                    {
                        bool tintOverride = GodgameMiraclePresentationDescriptors.TryGetTint(token.Type, out var tint);

                        var binding = new GodgamePresentationBinding
                        {
                            Descriptor = descriptor,
                            PositionOffset = float3.zero,
                            RotationOffset = quaternion.identity,
                            ScaleMultiplier = 1f,
                            Tint = tintOverride ? tint : float4.zero,
                            VariantSeed = math.hash(new uint2((uint)entity.Index, (uint)token.Type)),
                            Flags = GodgamePresentationFlagUtility.WithOverrides(tintOverride, false, false)
                        };

                        GodgamePresentationBindingUtility.ApplyBinding(entity, binding, ref _bindingLookup, ecb);
                    }
                }
                else
                {
                    // Standard projectile - could map ProjectileId to presentation descriptor
                    // For now, skip (would be handled by Space4X presentation system)
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
