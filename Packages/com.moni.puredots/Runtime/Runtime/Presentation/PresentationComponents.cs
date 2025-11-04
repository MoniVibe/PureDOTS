using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    [Flags]
    public enum PresentationSpawnFlags : byte
    {
        None = 0,
        AllowPooling = 1 << 0,
        ForceAnimateOnSpawn = 1 << 1,
        OverrideTint = 1 << 2,
        OverrideScale = 1 << 3,
        OverrideTransform = 1 << 4
    }

    public struct PresentationDescriptor
    {
        public Hash128 KeyHash;
        public Entity Prefab;
        public float3 DefaultOffset;
        public float DefaultScale;
        public float4 DefaultTint;
        public PresentationSpawnFlags DefaultFlags;
    }

    public struct PresentationRegistryBlob
    {
        public BlobArray<PresentationDescriptor> Descriptors;
    }

    public struct PresentationRegistryReference : IComponentData
    {
        public BlobAssetReference<PresentationRegistryBlob> Registry;
    }

    public struct PresentationCommandQueue : IComponentData
    {
    }

    public struct PresentationSpawnRequest : IBufferElementData
    {
        public Entity Target;
        public Hash128 DescriptorHash;
        public float3 Position;
        public quaternion Rotation;
        public float ScaleMultiplier;
        public float4 Tint;
        public uint VariantSeed;
        public PresentationSpawnFlags Flags;
    }

    public struct PresentationRecycleRequest : IBufferElementData
    {
        public Entity Target;
    }

    public struct PresentationHandle : IComponentData
    {
        public Entity Visual;
        public Hash128 DescriptorHash;
        public uint VariantSeed;
    }

    public static class PresentationKeyUtility
    {
        private const int MaxKeyLength = 48;

        public static bool TryParseKey(string key, out Hash128 hash, out string sanitizedKey)
        {
            sanitizedKey = string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                hash = default;
                return false;
            }

            string lower = key.Trim().ToLowerInvariant();
            if (lower.Length > MaxKeyLength)
            {
                lower = lower.Substring(0, MaxKeyLength);
            }

            hash = Hash128.Compute(lower);
            if (!hash.IsValid)
            {
                return false;
            }

            sanitizedKey = lower;
            return true;
        }
    }

    public static class PresentationRegistryUtility
    {
        public static bool TryGetDescriptor(ref PresentationRegistryReference registryRef, Hash128 key, out PresentationDescriptor descriptor)
        {
            descriptor = default;

            if (!registryRef.Registry.IsCreated)
            {
                return false;
            }

            ref var blob = ref registryRef.Registry.Value;
            var descriptors = blob.Descriptors;
            for (int i = 0; i < descriptors.Length; i++)
            {
                if (descriptors[i].KeyHash == key)
                {
                    descriptor = descriptors[i];
                    return true;
                }
            }

            return false;
        }
    }
}

