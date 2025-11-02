using PureDOTS.Runtime.Camera;
using Space4X.CameraComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space4X.CameraControls
{
    /// <summary>
    /// MonoBehaviour that applies camera controls (mouse rotation + keyboard movement) before the DOTS simulation.
    /// Captures the authoritative input snapshot and updates the camera state directly, ensuring zero-lag response.
    /// </summary>
    [DefaultExecutionOrder(-9990)]
    public sealed class Space4XCameraMouseController : MonoBehaviour, ICameraStateProvider
    {
        private const float kMaxCameraStepDelta = 1f / 30f;
        private const float kFallbackDelta = 1f / 60f;

        [Header("Input Bridge Tuning")]
        [SerializeField, Range(60f, 240f)]
        private float _bridgeMinSampleRateHz = 60f;

        [SerializeField, Range(60f, 240f)]
        private float _bridgeMaxSampleRateHz = 240f;

        [SerializeField]
        private bool _applyBridgeSampleRatesOnEnable = true;

        private EntityManager _entityManager;
        private World _world;

        private Entity _cameraStateEntity;
        private Entity _cameraConfigEntity;
        private Entity _cameraInputFlagsEntity;

        private Camera _unityCamera;
        private Transform _cameraTransform;

        private bool _stateResolved;
        private bool _configResolved;
        private bool _flagsResolved;

        public static Space4XCameraState LatestState
        {
            get
            {
                if (!CameraRigService.HasState)
                {
                    return default;
                }

                var rig = CameraRigService.Current;
                return new Space4XCameraState
                {
                    Position = new float3(rig.Position.x, rig.Position.y, rig.Position.z),
                    Rotation = new quaternion(rig.Rotation.x, rig.Rotation.y, rig.Rotation.z, rig.Rotation.w),
                    Pitch = rig.Pitch,
                    Yaw = rig.Yaw,
                    PerspectiveMode = rig.PerspectiveMode
                };
            }
        }

        public static bool TryGetLatestState(out Space4XCameraState state)
        {
            if (CameraRigService.HasState)
            {
                state = LatestState;
                return true;
            }

            state = default;
            return false;
        }

        public CameraRigState CurrentCameraState { get; private set; } = default;

        private EntityQuery _stateQuery;
        private EntityQuery _configQuery;
        private EntityQuery _flagsQuery;

        private static readonly Space4XCameraConfig k_DefaultConfig = new Space4XCameraConfig
        {
            PanSpeed = 10f,
            VerticalPanSpeed = 10f,
            PanBoundsMin = new float3(-100f, 0f, -100f),
            PanBoundsMax = new float3(100f, 100f, 100f),
            UsePanBounds = false,
            ZoomSpeed = 5f,
            ZoomMinDistance = 10f,
            ZoomMaxDistance = 500f,
            RotationSpeed = 0.25f,
            PitchMin = math.radians(-30f),
            PitchMax = math.radians(85f),
            Smoothing = 0.1f,
            EnablePan = true,
            EnableZoom = true,
            EnableRotation = true
        };

        private void OnDisable()
        {
            DisposeQueries();
        }

        private void OnDestroy()
        {
            DisposeQueries();
        }

        private void OnEnable()
        {
            if (_applyBridgeSampleRatesOnEnable)
            {
                ApplyBridgeSampleRates(_bridgeMinSampleRateHz, _bridgeMaxSampleRateHz);
            }
        }

        private void Update()
        {
            if (CameraRigService.IsEcsCameraEnabled)
            {
                return;
            }

            if (!Space4XCameraInputBridge.TryGetSnapshot(out var snapshot))
            {
                return;
            }

            if (!EnsureEntityManager())
            {
                return;
            }

            if (!EnsureCameraStateEntity())
            {
                return;
            }

            var config = EnsureCameraConfig();
            var deltaTime = ResolveDeltaTime();

            var flagsReady = EnsureCameraInputFlagsEntity();

            var state = _entityManager.GetComponentData<Space4XCameraState>(_cameraStateEntity);
            bool rotationApplied = false;
            bool movementApplied = false;

            var hasPanInput = snapshot.Pan.sqrMagnitude > 0f;
            var hasVerticalInput = math.abs(snapshot.VerticalPan) > 0f;
            var hasZoomInput = math.abs(snapshot.Zoom) > 0f;
            var hasMovementInput = hasPanInput || hasVerticalInput || hasZoomInput;
            var hasRotationInput = snapshot.Rotate.sqrMagnitude > float.Epsilon;

            // Perspective toggle
            if (snapshot.TogglePerspectiveMode)
            {
                state.PerspectiveMode = !state.PerspectiveMode;
            }

            // Reset
            if (snapshot.ResetRequested)
            {
                state.Position = new float3(0f, 25f, -30f);
                state.Pitch = math.radians(40f);
                state.Yaw = 0f;
                state.PerspectiveMode = false;
                movementApplied = true;
                rotationApplied = true;
            }

            // Rotation (mouse)
            if (config.EnableRotation && hasRotationInput)
            {
                var rotationDelta = snapshot.Rotate * config.RotationSpeed;
                state.Yaw += math.radians(rotationDelta.x);
                state.Pitch -= math.radians(rotationDelta.y);
                state.Pitch = math.clamp(state.Pitch, config.PitchMin, config.PitchMax);
                rotationApplied = true;
            }

            // Pan (WASD/MMB drag)
            if (config.EnablePan && snapshot.Pan.sqrMagnitude > 0f)
            {
                var pan = snapshot.Pan;
                if (pan.sqrMagnitude > 1f)
                {
                    pan = math.normalize(pan);
                }

                float3 moveDir;
                if (state.PerspectiveMode)
                {
                    var fullRotation = quaternion.Euler(state.Pitch, state.Yaw, 0f);
                    var forward = math.mul(fullRotation, new float3(0f, 0f, 1f));
                    var right = math.mul(fullRotation, new float3(1f, 0f, 0f));
                    moveDir = (right * pan.x + forward * pan.y) * config.PanSpeed * deltaTime;
                }
                else
                {
                    var yawRotation = quaternion.Euler(0f, state.Yaw, 0f);
                    var forward = math.mul(yawRotation, new float3(0f, 0f, 1f));
                    var right = math.mul(yawRotation, new float3(1f, 0f, 0f));
                    moveDir = (right * pan.x + forward * pan.y) * config.PanSpeed * deltaTime;
                }

                state.Position += moveDir;
                movementApplied = true;
            }

            // Vertical pan (Q/E)
            if (config.EnablePan && math.abs(snapshot.VerticalPan) > 0f)
            {
                var verticalSpeed = config.VerticalPanSpeed > 0f ? config.VerticalPanSpeed : config.PanSpeed;
                float3 verticalMove;

                if (state.PerspectiveMode)
                {
                    var fullRotation = quaternion.Euler(state.Pitch, state.Yaw, 0f);
                    var up = math.mul(fullRotation, new float3(0f, 1f, 0f));
                    verticalMove = up * snapshot.VerticalPan * verticalSpeed * deltaTime;
                }
                else
                {
                    verticalMove = new float3(0f, snapshot.VerticalPan * verticalSpeed * deltaTime, 0f);
                }

                state.Position += verticalMove;
                movementApplied = true;
            }

            // Zoom (scroll wheel)
            if (config.EnableZoom && math.abs(snapshot.Zoom) > 0f)
            {
                var forward = math.forward(quaternion.Euler(state.Pitch, state.Yaw, 0f));
                var zoomDelta = forward * snapshot.Zoom * config.ZoomSpeed * deltaTime;
                var newPosition = state.Position + zoomDelta;
                var distance = math.length(newPosition);
                if (distance >= config.ZoomMinDistance && distance <= config.ZoomMaxDistance)
                {
                    state.Position = newPosition;
                }
                movementApplied = true;
            }

            if (movementApplied && config.UsePanBounds)
            {
                state.Position = math.clamp(state.Position, config.PanBoundsMin, config.PanBoundsMax);
            }

            // Update rotation quaternion
            state.Rotation = quaternion.Euler(state.Pitch, state.Yaw, 0f);

            var stateChanged = movementApplied || rotationApplied || snapshot.ResetRequested || snapshot.TogglePerspectiveMode;
            if (stateChanged)
            {
                _entityManager.SetComponentData(_cameraStateEntity, state);
            }

            CurrentCameraState = new CameraRigState
            {
                Position = ToVector3(state.Position),
                Rotation = ToQuaternion(state.Rotation),
                Pitch = state.Pitch,
                Yaw = state.Yaw,
                Distance = math.length(state.Position),
                PerspectiveMode = state.PerspectiveMode
            };

            ApplyCameraPose(state);
            CameraRigService.Publish(CurrentCameraState);

            if (hasMovementInput || snapshot.ResetRequested || snapshot.TogglePerspectiveMode)
            {
                Space4XCameraInputBridge.ConsumeMovement();
            }

            if (hasRotationInput || snapshot.ResetRequested)
            {
                Space4XCameraInputBridge.ConsumeRotation();
            }

            if (snapshot.ResetRequested || snapshot.TogglePerspectiveMode)
            {
                Space4XCameraInputBridge.ConsumeFrameFlags();
            }

            Space4XCameraInputBridge.ConsumeMovement();
            Space4XCameraInputBridge.ConsumeRotation();

            if (flagsReady)
            {
                _entityManager.SetComponentData(_cameraInputFlagsEntity, new Space4XCameraInputFlags
                {
                    MovementHandled = true,
                    RotationHandled = true
                });
            }
        }

        [ContextMenu("Apply Bridge Sample Rates")]
        private void ApplyConfiguredBridgeSampleRates()
        {
            ApplyBridgeSampleRates(_bridgeMinSampleRateHz, _bridgeMaxSampleRateHz);
        }

        private void ApplyBridgeSampleRates(float minHz, float maxHz)
        {
            var clampedMin = Mathf.Clamp(minHz, 60f, 240f);
            var clampedMax = Mathf.Clamp(maxHz, clampedMin, 240f);
            _bridgeMinSampleRateHz = clampedMin;
            _bridgeMaxSampleRateHz = clampedMax;
            Space4XCameraInputBridge.ConfigureSampleRate(clampedMin, clampedMax);
        }

        private static float ResolveDeltaTime()
        {
            var bridgeDelta = Space4XCameraInputBridge.LastSampleDeltaTime;
            if (!float.IsNaN(bridgeDelta) && !float.IsInfinity(bridgeDelta) && bridgeDelta > 0f)
            {
                return Mathf.Min(bridgeDelta, kMaxCameraStepDelta);
            }

            var unscaledDelta = Time.unscaledDeltaTime;
            if (float.IsNaN(unscaledDelta) || float.IsInfinity(unscaledDelta) || unscaledDelta <= 0f)
            {
                unscaledDelta = Time.deltaTime;
            }

            if (float.IsNaN(unscaledDelta) || float.IsInfinity(unscaledDelta) || unscaledDelta <= 0f)
            {
                unscaledDelta = kFallbackDelta;
            }

            return Mathf.Min(unscaledDelta, kMaxCameraStepDelta);
        }

        private void ApplyCameraPose(in Space4XCameraState state)
        {
            if (!TryResolveUnityCamera())
            {
                return;
            }

            var position = ToVector3(state.Position);
            var rotation = ToQuaternion(state.Rotation);

            _cameraTransform.SetPositionAndRotation(position, rotation);

            if (_cameraTransform.localScale != Vector3.one)
            {
                _cameraTransform.localScale = Vector3.one;
            }
        }

        private bool TryResolveUnityCamera()
        {
            if (_cameraTransform != null)
            {
                return true;
            }

            _unityCamera = Camera.main;
            if (_unityCamera == null)
            {
                return false;
            }

            _cameraTransform = _unityCamera.transform;
            return _cameraTransform != null;
        }

        private static Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        private static Quaternion ToQuaternion(quaternion value)
        {
            return new Quaternion(value.value.x, value.value.y, value.value.z, value.value.w);
        }

        private bool EnsureEntityManager()
        {
            if (_world != null && _world.IsCreated)
            {
                return true;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            _world = world;
            _entityManager = world.EntityManager;

            try
            {
                DisposeQueries();
                _stateQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<Space4XCameraState>());
                _configQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<Space4XCameraConfig>());
                _flagsQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<Space4XCameraInputFlags>());
            }
            catch
            {
                DisposeQueries();
                return false;
            }

            _stateResolved = false;
            _configResolved = false;
            _flagsResolved = false;
            return true;
        }

        private bool EnsureCameraStateEntity()
        {
            if (_stateResolved && _entityManager.Exists(_cameraStateEntity))
            {
                return true;
            }

            if (_stateQuery == default)
            {
                return false;
            }

            if (_stateQuery.TryGetSingletonEntity<Space4XCameraState>(out _cameraStateEntity))
            {
                _stateResolved = true;
                return true;
            }

            return false;
        }

        private Space4XCameraConfig EnsureCameraConfig()
        {
            if (!_configResolved || !_entityManager.Exists(_cameraConfigEntity))
            {
                if (_configQuery == default)
                {
                    _cameraConfigEntity = default;
                    _configResolved = false;
                    return k_DefaultConfig;
                }

                if (!_configQuery.TryGetSingletonEntity<Space4XCameraConfig>(out _cameraConfigEntity))
                {
                    _cameraConfigEntity = _entityManager.CreateEntity(typeof(Space4XCameraConfig));
                    _entityManager.SetComponentData(_cameraConfigEntity, k_DefaultConfig);
                }

                _configResolved = true;
            }

            return _entityManager.GetComponentData<Space4XCameraConfig>(_cameraConfigEntity);
        }

        private bool EnsureCameraInputFlagsEntity()
        {
            if (_flagsResolved && _entityManager.Exists(_cameraInputFlagsEntity))
            {
                return true;
            }

            if (_flagsQuery == default)
            {
                return false;
            }

            if (!_flagsQuery.TryGetSingletonEntity<Space4XCameraInputFlags>(out _cameraInputFlagsEntity))
            {
                _cameraInputFlagsEntity = _entityManager.CreateEntity(typeof(Space4XCameraInputFlags));
                _entityManager.SetComponentData(_cameraInputFlagsEntity, new Space4XCameraInputFlags());
            }

            _flagsResolved = true;
            return true;
        }

        private void DisposeQueries()
        {
            if (_stateQuery != default)
            {
                _stateQuery.Dispose();
            }
            if (_configQuery != default)
            {
                _configQuery.Dispose();
            }
            if (_flagsQuery != default)
            {
                _flagsQuery.Dispose();
            }

            _stateQuery = default;
            _configQuery = default;
            _flagsQuery = default;

            _entityManager = default;
            _world = null;
            _stateResolved = false;
            _configResolved = false;
            _flagsResolved = false;
        }
    }
}

