using UnityEngine;

namespace PureDOTS.Runtime.Camera
{
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
    }

    public interface ICameraStateProvider
    {
        CameraRigState CurrentCameraState { get; }
    }
}


