using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Global presentation theme selection. Games can swap this at runtime to restyle all entities.
    /// </summary>
    public struct ActiveRenderTheme : IComponentData
    {
        public ushort ThemeId;
    }

    /// <summary>
    /// Game-agnostic semantic id used during variant resolution.
    /// </summary>
    public struct RenderSemanticKey : IComponentData
    {
        public ushort Value;
    }

    /// <summary>
    /// Optional per-entity theme override. Enableable so toggling does not cause structural changes.
    /// </summary>
    public struct RenderThemeOverride : IComponentData, IEnableableComponent
    {
        public ushort Value;
    }

    /// <summary>
    /// Stable, game-agnostic key identifying a resolved render variant.
    /// </summary>
    public struct RenderVariantKey : IComponentData, IEquatable<RenderVariantKey>
    {
        public int Value;

        public RenderVariantKey(int value) => Value = value;

        public bool Equals(RenderVariantKey other) => Value == other.Value;
        public override bool Equals(object obj) => obj is RenderVariantKey other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();

        public static implicit operator RenderVariantKey(int value) => new RenderVariantKey(value);
        public static implicit operator int(RenderVariantKey key) => key.Value;
    }

    /// <summary>
    /// Describes the high-level presenter category for a variant.
    /// </summary>
    public enum RenderPresenterKind : byte
    {
        None = 0,
        Sprite = 1,
        Mesh = 2,
        Debug = 3
    }

    /// <summary>
    /// Presenter mask encoded per variant entry.
    /// </summary>
    [Flags]
    public enum RenderPresenterMask : byte
    {
        None = 0,
        Mesh = 1 << 0,
        Sprite = 1 << 1,
        Debug = 1 << 2
    }

    /// <summary>
    /// Singleton storage for the active render presentation catalog.
    /// </summary>
    public struct RenderPresentationCatalog : IComponentData
    {
        public BlobAssetReference<RenderPresentationCatalogBlob> Blob;
        public Entity RenderMeshArrayEntity;
    }

    /// <summary>
    /// Legacy alias kept for existing authoring/runtime bootstrap code.
    /// </summary>
    [Obsolete("Use RenderPresentationCatalog instead.")]
    public struct RenderCatalogSingleton : IComponentData
    {
        public BlobAssetReference<RenderPresentationCatalogBlob> Blob;
        public Entity RenderMeshArrayEntity;
    }

    /// <summary>
    /// Blob root describing themed variant mappings and presenter data.
    /// </summary>
    public struct RenderPresentationCatalogBlob
    {
        public BlobArray<RenderVariantData> Variants;
        public BlobArray<RenderThemeRow> Themes;
        public BlobArray<int> ThemeIndexLookup;
        public int SemanticKeyCount;
        public int LodCount;
        public ushort DefaultThemeIndex;
    }

    public struct RenderVariantData
    {
        public ushort MaterialIndex;
        public ushort MeshIndex;
        public ushort SubMesh;
        public byte RenderLayer;
        public RenderPresenterMask PresenterMask;
        public float3 BoundsCenter;
        public float3 BoundsExtents;
    }

    public struct RenderThemeRow
    {
        public ushort ThemeId;
        public BlobArray<int> VariantIndices;
    }

    public struct RenderVariantRecord
    {
        public RenderVariantKey Key;
        public RenderPresenterKind Kind;
        public int DefIndex;
    }

    public struct SpritePresenterDef
    {
        public int SpriteId;
        public float2 Size;
        public float4 Tint;
    }

    public struct MeshPresenterDef
    {
        public int MeshId;
        public int MaterialId;
    }

    public struct DebugPresenterDef
    {
        public int DebugShapeId;
        public float4 Color;
    }

    public struct MeshPresenter : IComponentData, IEnableableComponent
    {
        public ushort DefIndex;
    }

    public struct SpritePresenter : IComponentData, IEnableableComponent
    {
        public ushort DefIndex;
    }

    public struct DebugPresenter : IComponentData, IEnableableComponent
    {
        public ushort DefIndex;
    }

    /// <summary>
    /// Per-instance tint that maps to a material property for instanced rendering.
    /// </summary>
    [MaterialProperty("_RenderTint")]
    public struct RenderTint : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// Per-instance texture slice/atlas index (Texture2DArray slice or atlas ID).
    /// </summary>
    [MaterialProperty("_RenderTexSlice")]
    public struct RenderTexSlice : IComponentData
    {
        public ushort Value;
    }

    /// <summary>
    /// Per-instance UV transform (xy = scale, zw = offset).
    /// </summary>
    [MaterialProperty("_RenderUvST")]
    public struct RenderUvTransform : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// Catalog versioning singleton for invalidating cached presenter data.
    /// </summary>
    public struct RenderCatalogVersion : IComponentData
    {
        public uint Value;
    }

    /// <summary>
    /// Optional cached state for systems that want to diff resolved variants manually.
    /// </summary>
    public struct RenderVariantResolved : IComponentData
    {
        public RenderVariantKey LastKey;
        public RenderPresenterKind LastKind;
        public int LastDefIndex;
    }

    public static class RenderPresentationConstants
    {
        /// <summary>
        /// Sentinel stored inside presenter components when no variant has been assigned yet.
        /// </summary>
        public const ushort UnassignedPresenterDefIndex = ushort.MaxValue;
    }
}
