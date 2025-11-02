using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.CameraComponents
{
    /// <summary>
    /// DOTS components for Space4X RTS-style camera control.
    /// </summary>
    
    /// <summary>
    /// Camera control input singleton - populated by Space4XCameraInputSystem (pure DOTS).
    /// </summary>
    public struct Space4XCameraInput : IComponentData
    {
        public float2 PanInput;      // WASD/Arrow keys input (horizontal pan)
        public float VerticalPanInput; // Q/E keys input (vertical movement)
        public float ZoomInput;       // Scroll wheel input
        public float2 RotateInput;    // Right mouse drag input
        public bool ResetRequested;   // R key input
        public bool TogglePerspectiveMode; // V key input - toggles camera perspective mode
    }

    /// <summary>
    /// Flags used to coordinate between MonoBehaviour camera controllers and DOTS camera system.
    /// Allows high-priority MonoBehaviour logic to signal that specific inputs have already been applied.
    /// </summary>
    public struct Space4XCameraInputFlags : IComponentData
    {
        public bool MovementHandled;
        public bool RotationHandled;
    }

    /// <summary>
    /// Runtime diagnostics for camera input ownership and catch-up behavior.
    /// </summary>
    public struct Space4XCameraDiagnostics : IComponentData
    {
        public uint FrameId;
        public int TicksThisFrame;
        public int CatchUpTicks;
        public int InputStaleTicks;
        public uint LastBudgetFrameId;
        public bool MovementHandledExternally;
        public bool RotationHandledExternally;
        public bool MonoControllerActive;
        public float2 PendingRotateBudget;
        public float PendingZoomBudget;
        public int BudgetTicksRemaining;
    }

    /// <summary>
    /// Camera state singleton - updated by Space4XCameraSystem.
    /// </summary>
    public struct Space4XCameraState : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float Pitch;           // Vertical rotation angle
        public float Yaw;             // Horizontal rotation angle
        public bool PerspectiveMode; // When true, WASD moves relative to camera orientation (like FPS camera)
    }

    /// <summary>
    /// Camera configuration singleton - baked from Space4XCameraProfile.
    /// </summary>
    public struct Space4XCameraConfig : IComponentData
    {
        public float PanSpeed;
        public float VerticalPanSpeed; // Speed for Q/E vertical movement
        public float3 PanBoundsMin;
        public float3 PanBoundsMax;
        public bool UsePanBounds;
        public float ZoomSpeed;
        public float ZoomMinDistance;
        public float ZoomMaxDistance;
        public float RotationSpeed; // Degrees of rotation per pixel of mouse movement
        public float PitchMin;
        public float PitchMax;
        public float Smoothing;
        public bool EnablePan;
        public bool EnableZoom;
        public bool EnableRotation;
    }

    /// <summary>
    /// Camera input configuration - optional component for input feature flags.
    /// Can be set at runtime or baked from authoring components.
    /// If not present, default values are used (all features enabled).
    /// </summary>
    public struct Space4XCameraInputConfig : IComponentData
    {
        public bool EnablePan;
        public bool EnableZoom;
        public bool EnableRotation;
    }
}

