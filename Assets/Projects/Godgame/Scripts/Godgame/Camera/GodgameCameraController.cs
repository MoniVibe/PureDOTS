using Godgame.Interaction;
using PureDOTS.Runtime.Camera;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityCamera = UnityEngine.Camera;

namespace Godgame.Camera
{
    /// <summary>
    /// MonoBehaviour camera controller that applies Godgame camera input before DOTS systems run.
    /// Maintains authoritative camera pose while keeping DOTS singletons in sync.
    /// </summary>
    [DefaultExecutionOrder(-9980)]
    public sealed class GodgameCameraController : MonoBehaviour, ICameraStateProvider
    {
        [Header("RTS/Free-fly Settings")]
        [SerializeField] private float movementSpeed = 10f;
        [SerializeField] private float rotationSensitivity = 2f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 6f;
        [SerializeField] private float zoomMin = 6f;
        [SerializeField] private float zoomMax = 220f;

        [Header("Orbital Settings")]
        [SerializeField] private float orbitalRotationSpeed = 1f;
        [SerializeField] private float panSensitivity = 1f;

        [Header("Distance-Scaled Sensitivity")]
        [SerializeField] private float sensitivityClose = 1.5f;
        [SerializeField] private float sensitivityMid = 1.0f;
        [SerializeField] private float sensitivityFar = 0.6f;

        [Header("Pitch Limits (degrees)")]
        [SerializeField] private float pitchMin = -30f;
        [SerializeField] private float pitchMax = 85f;

        public CameraRigState CurrentCameraState { get; private set; } = default;

        private static GodgameCameraController s_instance;

        public static bool TryGetCurrentState(out CameraRigState state)
        {
            if (s_instance != null)
            {
                state = s_instance.CurrentCameraState;
                return true;
            }

            state = default;
            return false;
        }

        private UnityCamera _unityCamera;
        private Transform _cameraTransform;

        private CameraMode _mode = CameraMode.RTSFreeFly;
        private Vector3 _position;
        private Quaternion _rotation;
        private float _pitchDegrees;
        private float _yawDegrees;
        private float _distanceFromPivot = 20f;
        private Vector3 _orbitalFocus;

        private EntityManager _entityManager;
        private Entity _cameraTransformEntity = Entity.Null;
        private Entity _cameraModeEntity = Entity.Null;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(this);
                return;
            }

            s_instance = this;
        }

        private void OnEnable()
        {
            ResolveCameraTransform();
            InitializeStateFromTransform();
        }

        private void OnDisable()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void Update()
        {
            if (BW2StyleCameraController.HasActiveRig)
            {
                return;
            }

            if (!GodgameCameraInputBridge.TryGetSnapshot(out var snapshot))
            {
                return;
            }

            if (!ResolveCameraTransform())
            {
                return;
            }

            var deltaTime = Time.deltaTime;

            if (snapshot.ToggleMode)
            {
                ToggleMode();
                GodgameCameraInputBridge.ConsumeToggle();
            }

            if (_mode == CameraMode.RTSFreeFly)
            {
                UpdateRtsMode(snapshot, deltaTime);
            }
            else
            {
                UpdateOrbitalMode(snapshot, deltaTime);
            }

            ApplyCameraPose();
            UpdateCurrentState();
            PushStateToDots();

            if (snapshot.Move.sqrMagnitude > 0f || Mathf.Abs(snapshot.Vertical) > 0f || Mathf.Abs(snapshot.Scroll) > 0.0001f)
            {
                GodgameCameraInputBridge.ConsumeMovement();
            }

            if (snapshot.Look.sqrMagnitude > 0f)
            {
                GodgameCameraInputBridge.ConsumeLook();
            }
        }

        private void ToggleMode()
        {
            _mode = _mode == CameraMode.RTSFreeFly ? CameraMode.Orbital : CameraMode.RTSFreeFly;

            if (_mode == CameraMode.Orbital)
            {
                _distanceFromPivot = Mathf.Clamp((_position - _orbitalFocus).magnitude, zoomMin, zoomMax);
                _orbitalFocus = _position + _rotation * Vector3.forward * Mathf.Max(1f, _distanceFromPivot);
            }
        }

        private void UpdateRtsMode(GodgameCameraInputBridge.Snapshot snapshot, float deltaTime)
        {
            var forward = _rotation * Vector3.forward;
            var right = _rotation * Vector3.right;

            var move = (right * snapshot.Move.x + forward * snapshot.Move.y) * panSensitivity;
            _position += move * movementSpeed * deltaTime;
            _position += Vector3.up * snapshot.Vertical * movementSpeed * deltaTime;

            if (snapshot.Look.sqrMagnitude > 0.0001f)
            {
                _yawDegrees += snapshot.Look.x * rotationSensitivity * deltaTime;
                _pitchDegrees -= snapshot.Look.y * rotationSensitivity * deltaTime;
                _pitchDegrees = Mathf.Clamp(_pitchDegrees, pitchMin, pitchMax);
                _rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            }

            if (Mathf.Abs(snapshot.Scroll) > 0.001f)
            {
                var zoomDelta = snapshot.Scroll * zoomSpeed * deltaTime;
                _position += forward * zoomDelta;
            }

            _distanceFromPivot = Mathf.Clamp((_position - _orbitalFocus).magnitude, zoomMin, zoomMax);
            _orbitalFocus = _position + _rotation * Vector3.forward * Mathf.Max(1f, _distanceFromPivot);
        }

        private void UpdateOrbitalMode(GodgameCameraInputBridge.Snapshot snapshot, float deltaTime)
        {
            if (snapshot.MiddleHeld && snapshot.Look.sqrMagnitude > 0.0001f)
            {
                var sensitivity = GetDistanceScaledSensitivity(_distanceFromPivot);
                _yawDegrees += snapshot.Look.x * orbitalRotationSpeed * sensitivity * deltaTime;
                _pitchDegrees -= snapshot.Look.y * orbitalRotationSpeed * sensitivity * deltaTime;
                _pitchDegrees = Mathf.Clamp(_pitchDegrees, pitchMin, pitchMax);
            }

            if (Mathf.Abs(snapshot.Scroll) > 0.001f)
            {
                _distanceFromPivot = Mathf.Clamp(_distanceFromPivot - snapshot.Scroll * zoomSpeed * deltaTime, zoomMin, zoomMax);
            }

            var rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            var offset = rotation * (Vector3.back * _distanceFromPivot);
            _position = _orbitalFocus + offset;
            _rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
        }

        private void ApplyCameraPose()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            _cameraTransform.SetPositionAndRotation(_position, _rotation);

            if (_cameraTransform.localScale != Vector3.one)
            {
                _cameraTransform.localScale = Vector3.one;
            }
        }

        private void UpdateCurrentState()
        {
            CurrentCameraState = new CameraRigState
            {
                Position = _position,
                Rotation = _rotation,
                Pitch = math.radians(_pitchDegrees),
                Yaw = math.radians(_yawDegrees),
                Distance = _distanceFromPivot,
                PerspectiveMode = _mode == CameraMode.RTSFreeFly,
                FieldOfView = _unityCamera != null ? _unityCamera.fieldOfView : 60f,
                RigType = CameraRigType.Godgame
            };

            CameraRigService.Publish(CurrentCameraState);
        }

        private void InitializeStateFromTransform()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            _position = _cameraTransform.position;
            _rotation = _cameraTransform.rotation;

            var euler = _rotation.eulerAngles;
            _pitchDegrees = NormalizeDegrees(euler.x);
            _yawDegrees = NormalizeDegrees(euler.y);
            _distanceFromPivot = Mathf.Max(0.1f, _cameraTransform.position.magnitude);
            _orbitalFocus = _position + _rotation * Vector3.forward * _distanceFromPivot;
        }

        private bool ResolveCameraTransform()
        {
            if (_cameraTransform != null)
            {
                return true;
            }

            _unityCamera = UnityCamera.main;
            if (_unityCamera == null)
            {
                return false;
            }

            _cameraTransform = _unityCamera.transform;
            InitializeStateFromTransform();
            return true;
        }

        private void PushStateToDots()
        {
            if (BW2StyleCameraController.HasActiveRig)
            {
                return;
            }

            if (!TryEnsureEntityManager())
            {
                return;
            }

            if (_cameraTransformEntity != Entity.Null && _entityManager.Exists(_cameraTransformEntity))
            {
                var cameraTransform = new CameraTransform
                {
                    Position = new float3(_position.x, _position.y, _position.z),
                    Rotation = new quaternion(_rotation.x, _rotation.y, _rotation.z, _rotation.w),
                    DistanceFromPivot = _distanceFromPivot,
                    PitchAngle = _pitchDegrees
                };
                _entityManager.SetComponentData(_cameraTransformEntity, cameraTransform);
            }

            if (_cameraModeEntity != Entity.Null && _entityManager.Exists(_cameraModeEntity))
            {
                _entityManager.SetComponentData(_cameraModeEntity, new CameraModeState
                {
                    Mode = _mode,
                    JustToggled = false
                });
            }
        }

        private bool TryEnsureEntityManager()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            if (_entityManager != world.EntityManager)
            {
                _entityManager = world.EntityManager;
                _cameraTransformEntity = Entity.Null;
                _cameraModeEntity = Entity.Null;
            }

            if (_cameraTransformEntity == Entity.Null || !_entityManager.Exists(_cameraTransformEntity))
            {
                using var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<CameraTransform>());
                if (query.TryGetSingletonEntity<CameraTransform>(out var entity))
                {
                    _cameraTransformEntity = entity;
                }
            }

            if (_cameraModeEntity == Entity.Null || !_entityManager.Exists(_cameraModeEntity))
            {
                using var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<CameraModeState>());
                if (query.TryGetSingletonEntity<CameraModeState>(out var entity))
                {
                    _cameraModeEntity = entity;
                }
            }

            return (_cameraTransformEntity != Entity.Null && _entityManager.Exists(_cameraTransformEntity)) ||
                   (_cameraModeEntity != Entity.Null && _entityManager.Exists(_cameraModeEntity));
        }

        private float GetDistanceScaledSensitivity(float distance)
        {
            if (distance <= 20f)
            {
                return sensitivityClose;
            }

            if (distance <= 100f)
            {
                return sensitivityMid;
            }

            return sensitivityFar;
        }

        private static float NormalizeDegrees(float degrees)
        {
            while (degrees > 180f) degrees -= 360f;
            while (degrees < -180f) degrees += 360f;
            return degrees;
        }
    }
}


