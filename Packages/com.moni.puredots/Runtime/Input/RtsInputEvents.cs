using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Input
{
    /// <summary>
    /// Marker component for the singleton RTS input entity that holds all RTS event buffers.
    /// </summary>
    public struct RtsInputSingletonTag : IComponentData { }

    /// <summary>
    /// Shared command plane state used by game-specific camera/order adapters for RTS right-click projection.
    /// </summary>
    public struct RtsCommandPlaneState : IComponentData
    {
        public float3 PlaneOrigin;
        public float3 PlaneNormal;
        public float PlaneHeight;
        public float3 LastCommandPoint;
        public byte HasLastCommandPoint;
    }

    /// <summary>
    /// Selection click event (single-click on world).
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct SelectionClickEvent : IBufferElementData
    {
        public float2 ScreenPos;
        public SelectionClickMode Mode;
        public byte PlayerId;
    }

    /// <summary>
    /// Selection box event (drag rectangle).
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct SelectionBoxEvent : IBufferElementData
    {
        public float2 ScreenMin;
        public float2 ScreenMax;
        public SelectionBoxMode Mode;
        public byte PlayerId;
    }

    /// <summary>
    /// Right-click contextual order event.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct RightClickEvent : IBufferElementData
    {
        public float2 ScreenPos;
        public byte Queue;  // 1 if Shift-held (queue order), 0 if immediate
        public byte Ctrl;   // 1 if Ctrl-held (attack-move modifier), 0 if none
        public byte PlayerId;
        // Optional world-resolution payload for adapters that pre-resolve the click.
        public byte HasWorldPoint; // 1 when WorldPoint is valid.
        public float3 WorldPoint;
        public byte HasHitEntity; // 1 when HitEntity is valid.
        public Entity HitEntity;
    }

    /// <summary>
    /// Control group input event (Ctrl+Number save, Number recall).
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct ControlGroupInputEvent : IBufferElementData
    {
        public byte Number;      // 0-9
        public byte Save;        // 1 if Ctrl pressed
        public byte Additive;    // 1 if Shift+Ctrl
        public byte Recall;      // 1 if plain number press
        public byte PlayerId;
    }

    /// <summary>
    /// Time control input event.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct TimeControlInputEvent : IBufferElementData
    {
        public TimeControlCommandKind Kind;
        public float FloatParam;  // For SetScale
        public int IntParam;      // For StepTicks
        public byte PlayerId;
    }

    /// <summary>
    /// God-hand command event.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct GodHandCommandEvent : IBufferElementData
    {
        public GodHandCommandKind Kind;
        public byte PlayerId;
    }

    /// <summary>
    /// Camera focus event (double-click LMB).
    /// </summary>
    [InternalBufferCapacity(2)]
    public struct CameraFocusEvent : IBufferElementData
    {
        public float3 WorldPosition;
        public Entity HitEntity;  // Entity.Null if none
        public byte PlayerId;
    }

    public enum CameraRequestKind : byte
    {
        None = 0,
        FocusWorld = 1,
        RecallBookmark = 2,
    }

    /// <summary>
    /// High-level camera requests emitted by ECS systems (selection/control-groups) and consumed by game camera rigs.
    /// Keeps the camera contract single-writer by avoiding direct Camera.main transform mutation in ECS systems.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct CameraRequestEvent : IBufferElementData
    {
        public CameraRequestKind Kind;
        public float3 WorldPosition; // FocusWorld
        public float3 BookmarkPosition; // RecallBookmark
        public quaternion BookmarkRotation; // RecallBookmark
        public byte PlayerId;
    }

    /// <summary>
    /// Rock break event (double-click RMB on rock).
    /// </summary>
    [InternalBufferCapacity(2)]
    public struct RockBreakEvent : IBufferElementData
    {
        public Entity RockEntity;  // Entity.Null if resolved via hit position
        public float3 HitPosition;
        public byte PlayerId;
    }

    /// <summary>
    /// Save/load command event.
    /// </summary>
    [InternalBufferCapacity(2)]
    public struct SaveLoadCommandEvent : IBufferElementData
    {
        public SaveLoadCommandKind Kind;
        public byte PlayerId;
    }
}


















