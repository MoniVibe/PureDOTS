using UnityEngine;

namespace PureDOTS.Runtime.Camera
{
    /// <summary>
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
    /// Represents the authoritative state of a camera rig (position, orientation, and metadata).
    /// </summary>
    public struct CameraRigState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Pitch;
        public float Yaw;
        public float Distance;
        public bool PerspectiveMode;
        public float FieldOfView;
        public CameraRigType RigType;
    }

    public interface ICameraStateProvider
    {
        CameraRigState CurrentCameraState { get; }
    }
}


