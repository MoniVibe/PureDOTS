using UnityEngine;

namespace PureDOTS.Runtime.Camera
{
    /// <summary>
    /// CAMERA RIG CONTRACT - PRESENTATION CODE
    ///
    /// Uses frame-time (Time.deltaTime).
    /// Not part of deterministic simulation / rewind.
    /// Safe utility for game projects to build cameras on.
    ///
    /// Identifies which gameplay rig currently owns the camera transform.
    /// </summary>
    public enum CameraRigType : byte
    {
        None = 0,
        Godgame = 1,
        BW2 = 2,
        Space4X = 3
    }

    /// <summary>
    /// CAMERA RIG CONTRACT - CameraRigState
    ///
    /// Represents the complete state of a camera rig that can be applied to any Unity Camera.
    /// This is the contract between game-specific camera controllers and the presentation layer.
    ///
    /// CONTRACT GUARANTEES:
    /// - Position: World position of the camera
    /// - Rotation: World rotation of the camera (applied via SetPositionAndRotation)
    /// - Pitch/Yaw: Euler angles in degrees (for orbit/rotation cameras)
    /// - Distance: Distance from pivot/target (for zoom cameras)
    /// - PerspectiveMode: true=perspective, false=orthographic (game-specific interpretation)
    /// - FieldOfView: Camera field of view in degrees (0 = use camera default)
    /// - RigType: Which game rig owns this state (for debugging/conflict resolution)
    ///
    /// Uses frame-time (Time.deltaTime).
    /// Not part of deterministic simulation / rewind.
    /// Safe utility for game projects to build cameras on.
    /// </summary>
    public struct CameraRigState
    {
        /// <summary>World position of the camera.</summary>
        public Vector3 Position;

        /// <summary>World rotation of the camera (applied via SetPositionAndRotation).</summary>
        public Quaternion Rotation;

        /// <summary>Pitch angle in degrees (for orbit cameras).</summary>
        public float Pitch;

        /// <summary>Yaw angle in degrees (for orbit cameras).</summary>
        public float Yaw;

        /// <summary>Distance from pivot/target (for zoom cameras).</summary>
        public float Distance;

        /// <summary>true=perspective mode, false=orthographic (game-specific interpretation).</summary>
        public bool PerspectiveMode;

        /// <summary>Camera field of view in degrees (0 = use camera default).</summary>
        public float FieldOfView;

        /// <summary>Which game rig owns this state (for debugging/conflict resolution).</summary>
        public CameraRigType RigType;
    }

    public interface ICameraStateProvider
    {
        CameraRigState CurrentCameraState { get; }
    }
}


