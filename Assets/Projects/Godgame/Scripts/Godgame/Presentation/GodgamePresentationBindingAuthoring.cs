using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Godgame.Presentation
{
    /// <summary>
    /// Authoring component to pick a descriptor key and optional overrides for Godgame entities.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GodgamePresentationBindingAuthoring : MonoBehaviour
    {
        [Header("Descriptor")]
        [Tooltip("Descriptor key defined in the PresentationRegistry asset (e.g. 'godgame.villager.default').")]
        public string descriptorKey = "godgame.villager.default";

        [Header("Overrides")]
        public Vector3 positionOffset;
        public Vector3 rotationOffsetEuler;
        [Min(0.01f)]
        public float scaleMultiplier = 1f;
        public bool overrideTransform;
        public bool overrideScale;
        public bool overrideTint;
        public Color tint = Color.white;
        public uint variantSeed;

        private void OnValidate()
        {
            scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            descriptorKey = string.IsNullOrWhiteSpace(descriptorKey)
                ? "godgame.villager.default"
                : descriptorKey.Trim();
        }

        public sealed class Baker : Unity.Entities.Baker<GodgamePresentationBindingAuthoring>
        {
            public override void Bake(GodgamePresentationBindingAuthoring authoring)
            {
                if (!PresentationKeyUtility.TryParseKey(authoring.descriptorKey, out var descriptor, out _))
                {
                    Debug.LogWarning($"GodgamePresentationBindingAuthoring '{authoring.name}' has an invalid descriptor key '{authoring.descriptorKey}'.");
                    return;
                }

                bool tintOverride = authoring.overrideTint;
                bool scaleOverride = authoring.overrideScale || math.abs(authoring.scaleMultiplier - 1f) > math.EPSILON;
                bool transformOverride = authoring.overrideTransform
                                         || authoring.positionOffset != Vector3.zero
                                         || authoring.rotationOffsetEuler != Vector3.zero;

                var binding = new GodgamePresentationBinding
                {
                    Descriptor = descriptor,
                    PositionOffset = new float3(authoring.positionOffset.x, authoring.positionOffset.y, authoring.positionOffset.z),
                    RotationOffset = quaternion.EulerXYZ(math.radians(authoring.rotationOffsetEuler)),
                    ScaleMultiplier = math.max(0.01f, authoring.scaleMultiplier),
                    Tint = tintOverride
                        ? new float4(authoring.tint.r, authoring.tint.g, authoring.tint.b, authoring.tint.a)
                        : float4.zero,
                    VariantSeed = authoring.variantSeed,
                    Flags = GodgamePresentationFlagUtility.WithOverrides(tintOverride, scaleOverride, transformOverride)
                };

                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);
                AddComponent(entity, binding);
            }
        }
    }
}
