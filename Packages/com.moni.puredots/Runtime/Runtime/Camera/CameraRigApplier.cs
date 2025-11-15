using UnityEngine;
using UnityEngineCamera = UnityEngine.Camera;

namespace PureDOTS.Runtime.Camera
{
    /// <summary>
    /// Applies the authoritative <see cref="CameraRigState"/> to the attached <see cref="Camera"/>.
    /// Ensures only one place mutates <c>Camera.main</c> regardless of which gameplay rig is active.
    /// </summary>
    [RequireComponent(typeof(UnityEngineCamera))]
    [DefaultExecutionOrder(10000)]
    public sealed class CameraRigApplier : MonoBehaviour
    {
        private UnityEngineCamera _camera;

        private void Awake()
        {
            _camera = GetComponent<UnityEngineCamera>();
        }

        private void LateUpdate()
        {
            if (!CameraRigService.HasState)
            {
                return;
            }

            var state = CameraRigService.Current;
            if (state.RigType == CameraRigType.None)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = UnityEngineCamera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            _camera.transform.SetPositionAndRotation(state.Position, state.Rotation);
            if (state.FieldOfView > 0.01f)
            {
                _camera.fieldOfView = state.FieldOfView;
            }
        }
    }
}
