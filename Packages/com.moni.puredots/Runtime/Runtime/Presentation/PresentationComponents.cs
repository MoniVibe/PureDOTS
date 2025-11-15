using System;
using System.Security.Cryptography;
using System.Text;
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
        public Unity.Entities.Hash128 KeyHash;
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
        public Unity.Entities.Hash128 DescriptorHash;
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
        public Unity.Entities.Hash128 DescriptorHash;
        public uint VariantSeed;
    }

    public struct PresentationHandleSyncConfig : IComponentData
    {
        public float PositionLerp;
        public float RotationLerp;
        public float ScaleLerp;
        public float3 VisualOffset;

        public static PresentationHandleSyncConfig Default => new PresentationHandleSyncConfig
        {
            PositionLerp = 1f,
            RotationLerp = 1f,
            ScaleLerp = 1f,
            VisualOffset = float3.zero
        };
    }

    public struct PresentationPoolStats : IComponentData
    {
        public uint ActiveVisuals;
        public uint SpawnedThisFrame;
        public uint RecycledThisFrame;
        public ulong TotalSpawned;
        public ulong TotalRecycled;
    }

    public struct PresentationReloadCommand : IComponentData
    {
        public uint RequestId;
    }

    public static class PresentationKeyUtility
    {
        private const int MaxKeyLength = 48;

        public static bool TryParseKey(string key, out Unity.Entities.Hash128 hash, out string sanitizedKey)
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

            // Compute hash using MD5 (128-bit) and convert to Hash128
            var inputBytes = Encoding.UTF8.GetBytes(lower);
            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(inputBytes);
                // Convert 16-byte MD5 hash to hex string (32 hex chars = 128 bits)
                var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                hash = new Unity.Entities.Hash128(hashHex);
            }
            
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
        public static bool TryGetDescriptor(ref PresentationRegistryReference registryRef, Unity.Entities.Hash128 key, out PresentationDescriptor descriptor)
        {
            descriptor = default;

            if (!registryRef.Registry.IsCreated)
            {
                return false;
            }

            ref var blob = ref registryRef.Registry.Value;
            ref var descriptors = ref blob.Descriptors;
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
