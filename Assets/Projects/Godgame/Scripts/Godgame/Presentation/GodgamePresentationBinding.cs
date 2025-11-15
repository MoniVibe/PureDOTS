using System.Runtime.CompilerServices;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Presentation
{
    /// <summary>
    /// Descriptor binding placed on gameplay entities so their presentation visuals can be swapped per entity.
    /// </summary>
    public struct GodgamePresentationBinding : IComponentData
    {
        public Unity.Entities.Hash128 Descriptor;
        public float3 PositionOffset;
        public quaternion RotationOffset;
        public float ScaleMultiplier;
        public float4 Tint;
        public uint VariantSeed;
        public PresentationSpawnFlags Flags;

        public static GodgamePresentationBinding Create(Unity.Entities.Hash128 descriptor)
        {
            return new GodgamePresentationBinding
            {
                Descriptor = descriptor,
                PositionOffset = float3.zero,
                RotationOffset = quaternion.identity,
                ScaleMultiplier = 1f,
                Tint = float4.zero,
                VariantSeed = 0u,
                Flags = PresentationSpawnFlags.AllowPooling
            };
        }
    }

    /// <summary>
    /// Tag that forces the assignment system to recycle and respawn the bound presentation next frame.
    /// </summary>
    public struct GodgamePresentationDirtyTag : IComponentData { }

    public static class GodgamePresentationFlagUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PresentationSpawnFlags WithOverrides(
            bool overrideTint,
            bool overrideScale,
            bool overrideTransform)
        {
            var flags = PresentationSpawnFlags.AllowPooling;
            if (overrideTint)
            {
                flags |= PresentationSpawnFlags.OverrideTint;
            }

            if (overrideScale)
            {
                flags |= PresentationSpawnFlags.OverrideScale;
            }

            if (overrideTransform)
            {
                flags |= PresentationSpawnFlags.OverrideTransform;
            }

            return flags;
        }
    }

    public static class GodgamePresentationBindingUtility
    {
        private const float Epsilon = 1e-3f;

        public static bool ApplyBinding(
            Entity entity,
            in GodgamePresentationBinding binding,
            ref ComponentLookup<GodgamePresentationBinding> lookup,
            EntityCommandBuffer ecb)
        {
            if (!lookup.HasComponent(entity))
            {
                ecb.AddComponent(entity, binding);
                ecb.AddComponent<GodgamePresentationDirtyTag>(entity);
                return true;
            }

            var current = lookup[entity];
            if (BindingsDiffer(current, binding))
            {
                ecb.SetComponent(entity, binding);
                ecb.AddComponent<GodgamePresentationDirtyTag>(entity);
                return true;
            }

            return false;
        }

        public static bool BindingsDiffer(in GodgamePresentationBinding current, in GodgamePresentationBinding next)
        {
            if (current.Descriptor != next.Descriptor ||
                current.VariantSeed != next.VariantSeed ||
                current.Flags != next.Flags)
            {
                return true;
            }

            if (math.abs(current.ScaleMultiplier - next.ScaleMultiplier) > Epsilon)
            {
                return true;
            }

            if (!Float3NearEquals(current.PositionOffset, next.PositionOffset) ||
                !QuaternionNearEquals(current.RotationOffset, next.RotationOffset) ||
                !Float4NearEquals(current.Tint, next.Tint))
            {
                return true;
            }

            return false;
        }

        private static bool Float3NearEquals(float3 a, float3 b)
        {
            return math.all(math.abs(a - b) <= Epsilon);
        }

        private static bool Float4NearEquals(float4 a, float4 b)
        {
            return math.all(math.abs(a - b) <= Epsilon);
        }

        private static bool QuaternionNearEquals(quaternion a, quaternion b)
        {
            return math.all(math.abs(a.value - b.value) <= Epsilon);
        }
    }
}
