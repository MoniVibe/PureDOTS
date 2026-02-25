using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.UI
{
    /// <summary>
    /// Canonical panel ids shared by TRI games.
    /// Game-specific adapters can map these to concrete screens/windows.
    /// </summary>
    public enum UiPanelKind : byte
    {
        None = 0,
        Inventory = 1,
        Character = 2,
        Fleet = 3,
        Colony = 4,
        Build = 5,
        Diplomacy = 6,
        Research = 7,
        Map = 8,
        Debug = 9
    }

    public enum UiIntentKind : byte
    {
        None = 0,
        TogglePanel = 1,
        OpenPanel = 2,
        ClosePanel = 3,
        CloseTopLayer = 4,
        SetInventoryTab = 5,
        PushTooltip = 6,
        PinTopTooltip = 7,
        PopTooltip = 8,
        PopAllTooltips = 9
    }

    public enum UiTooltipAnchor : byte
    {
        Cursor = 0,
        Element = 1,
        World = 2
    }

    public enum UiTooltipMode : byte
    {
        Hover = 0,
        Focus = 1,
        Pinned = 2
    }

    public static class UiIntentFlags
    {
        public const byte Modal = 1 << 0;
        public const byte KeepTooltips = 1 << 1;
    }

    public static class UiKernelConstants
    {
        public const byte MaxTooltipDepth = 3;
        public const byte InventoryTabCount = 4;
    }

    /// <summary>
    /// Root singleton tag for the shared UI kernel.
    /// </summary>
    public struct UiKernelRootTag : IComponentData
    {
    }

    /// <summary>
    /// Compact snapshot consumed by UI bridges (UI Toolkit/Mono/debug overlays).
    /// </summary>
    public struct UiKernelState : IComponentData
    {
        public UiPanelKind ActivePrimaryPanel;
        public byte InventoryOpen;
        public byte InventoryTab;
        public byte TooltipDepth;
        public byte TooltipPinnedCount;
        public uint Revision;
    }

    [InternalBufferCapacity(16)]
    public struct UiIntent : IBufferElementData
    {
        public UiIntentKind Kind;
        public UiPanelKind Panel;
        public byte Param0;
        public byte Flags;
        public uint Data0;
        public Entity Target;
        public float2 ScreenPosition;
        public float3 WorldPosition;
        public FixedString64Bytes PrimaryKey;
        public FixedString64Bytes SecondaryKey;
    }

    [InternalBufferCapacity(8)]
    public struct UiOpenPanel : IBufferElementData
    {
        public UiPanelKind Panel;
        public byte Layer;
        public byte IsModal;
        public byte Flags;
    }

    [InternalBufferCapacity(16)]
    public struct UiTooltipEntry : IBufferElementData
    {
        public UiTooltipAnchor Anchor;
        public UiTooltipMode Mode;
        public byte Depth;
        public sbyte ParentIndex;
        public uint Token;
        public Entity Subject;
        public FixedString64Bytes PrimaryKey;
        public FixedString64Bytes SecondaryKey;
        public float2 ScreenPosition;
        public float3 WorldPosition;
        public ushort Flags;
    }
}
