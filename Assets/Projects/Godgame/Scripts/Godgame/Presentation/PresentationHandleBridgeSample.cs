using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;

namespace Godgame.Presentation
{
    /// <summary>
    /// Sample Mono/Entities bridge that reacts to PresentationHandle data on the owning entity.
    /// Demonstrates swapping materials, nudging animator states, and updating VFX seed bindings
    /// without writing bespoke DOTS systems per prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PresentationHandleBridgeSample : MonoBehaviour
    {
        [Header("Entity Link")]
        [Tooltip("Entity to read PresentationHandle data from. Leave null to disable.")]
        public Entity linkedEntity;

        [Header("Renderer Overrides")]
        [Tooltip("Renderer that can swap shared materials based on VariantSeed.")]
        public Renderer targetRenderer;

        [Tooltip("Optional materials to cycle when the descriptor VariantSeed changes.")]
        public Material[] materialVariants;

        [Header("Animator Overrides")]
        [Tooltip("Animator that plays simple keyed states when the descriptor changes.")]
        public Animator targetAnimator;

        [Tooltip("Animator state names to cycle through when visuals respawn.")]
        public string[] animatorStateNames;

        [Header("VFX Overrides")]
        [Tooltip("Optional VisualEffect graph that will be reseeded per handle.")]
        public VisualEffect visualEffect;

        [Tooltip("VisualEffect property name that stores the seed.")]
        public string visualEffectSeedProperty = "Seed";

        private EntityManager _entityManager;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                enabled = false;
                return;
            }

            _entityManager = world.EntityManager;

            if (linkedEntity == Entity.Null)
            {
                // No link available; disable until a runtime script assigns one.
                enabled = false;
                return;
            }

        }

        private void LateUpdate()
        {
            if (_entityManager == null || !_entityManager.Exists(linkedEntity))
            {
                return;
            }

            if (!_entityManager.HasComponent<PresentationHandle>(linkedEntity))
            {
                return;
            }

            var handle = _entityManager.GetComponentData<PresentationHandle>(linkedEntity);
            ApplyMaterialOverride(handle);
            ApplyAnimatorOverride(handle);
            ApplyVfxOverride(handle);
        }

        private void ApplyMaterialOverride(in PresentationHandle handle)
        {
            if (targetRenderer == null || materialVariants == null || materialVariants.Length == 0)
            {
                return;
            }

            var index = (int)(handle.VariantSeed % (uint)materialVariants.Length);
            targetRenderer.sharedMaterial = materialVariants[index];
        }

        private void ApplyAnimatorOverride(in PresentationHandle handle)
        {
            if (targetAnimator == null || animatorStateNames == null || animatorStateNames.Length == 0)
            {
                return;
            }

            var index = (int)(handle.VariantSeed % (uint)animatorStateNames.Length);
            var stateName = animatorStateNames[index];
            if (!string.IsNullOrEmpty(stateName))
            {
                targetAnimator.Play(stateName, 0, 0f);
            }
        }

        private void ApplyVfxOverride(in PresentationHandle handle)
        {
            if (visualEffect == null || string.IsNullOrEmpty(visualEffectSeedProperty))
            {
                return;
            }

            if (visualEffect.HasUInt(visualEffectSeedProperty))
            {
                visualEffect.SetUInt(visualEffectSeedProperty, handle.VariantSeed);
            }
            else if (visualEffect.HasFloat(visualEffectSeedProperty))
            {
                visualEffect.SetFloat(visualEffectSeedProperty, handle.VariantSeed);
            }
        }
    }
}
