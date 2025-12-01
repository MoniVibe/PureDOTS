using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Presentation;

namespace Godgame.Presentation
{
    /// <summary>
    /// Seeds a minimal presentation binding so placeholder effects can play even without authored bindings.
    /// DISABLED: Using Unity default objects for now. Re-enable when custom visuals are ready.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PureDOTS.Systems.PresentationBootstrapSystem))]
    public partial struct GodgamePresentationBindingBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PresentationCommandQueue>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            Entity bindingEntity;

            if (!SystemAPI.TryGetSingletonEntity<PresentationBindingReference>(out bindingEntity))
            {
                bindingEntity = entityManager.CreateEntity(typeof(PresentationBindingReference));
            }

            var bindingRef = entityManager.GetComponentData<PresentationBindingReference>(bindingEntity);
            if (bindingRef.Binding.IsCreated && HasEffect(bindingRef.Binding, GodgamePresentationIds.MiraclePingEffectId))
            {
                state.Enabled = false;
                return;
            }

            var blob = BuildBindingBlob();

            if (bindingRef.Binding.IsCreated)
            {
                bindingRef.Binding.Dispose();
            }

            bindingRef.Binding = blob;
            entityManager.SetComponentData(bindingEntity, bindingRef);
            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton(out PresentationBindingReference binding) && binding.Binding.IsCreated)
            {
                binding.Binding.Dispose();
            }
        }

        private static BlobAssetReference<PresentationBindingBlob> BuildBindingBlob()
        {
            var builder = new BlobBuilder(Allocator.Persistent);
            ref var root = ref builder.ConstructRoot<PresentationBindingBlob>();

            var effects = builder.Allocate(ref root.Effects, 1);
            var miracleStyle = new PresentationStyleBlock
            {
                Style = GodgamePresentationIds.MiraclePingStyle,
                PaletteIndex = 2,
                Size = 1f,
                Speed = 1f
            };
            effects[0] = new PresentationEffectBinding
            {
                EffectId = GodgamePresentationIds.MiraclePingEffectId,
                Kind = PresentationKind.Particle,
                Style = miracleStyle,
                Lifetime = PresentationLifetimePolicy.Timed,
                DurationSeconds = 1.5f,
                AttachRule = PresentationAttachRule.World
            };

            builder.Allocate(ref root.Companions, 0);
            return builder.CreateBlobAssetReference<PresentationBindingBlob>(Allocator.Persistent);
        }

        private static bool HasEffect(BlobAssetReference<PresentationBindingBlob> binding, int effectId)
        {
            ref var blob = ref binding.Value;
            ref var effects = ref blob.Effects;
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].EffectId == effectId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
