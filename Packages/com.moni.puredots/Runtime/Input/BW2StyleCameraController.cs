using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
using PureDOTS.Input;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Hybrid;
using UnityEngineCamera = UnityEngine.Camera;
#if GODGAME
using Godgame.Runtime;
#endif

namespace PureDOTS.Runtime.Camera
{
    /// <summary>
    /// Black & White 2 inspired camera controller: LMB pans across terrain, MMB orbits the scene,
    /// scroll wheel adjusts zoom radius. Terrain clamps are the only height restriction applied.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngineCamera))]
    [RequireComponent(typeof(CameraRigApplier))]
    [RequireComponent(typeof(BW2CameraInputBridge))]
    public sealed class BW2StyleCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UnityEngine.Camera targetCamera;
        [SerializeField] Transform pivotTransform;

        [Header("Input")]
        [SerializeField] HandCameraInputRouter inputRouter;

        [Header("Ground")]
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float groundProbeDistance = 600f;

        public LayerMask GroundMask
        {
            get => groundMask;
            set => groundMask = value;
        }

        [Header("Pan")]
        [SerializeField] float panScale = 1f;

        public float PanScale
        {
            get => panScale;
            set => panScale = value;
        }
        [SerializeField] float panDeadzoneMeters = 0.01f;
        [SerializeField] bool allowPanOverUI;

        [Header("Orbit")]
        [SerializeField] float orbitYawSensitivity = 0.25f;
        [SerializeField] float orbitPitchSensitivity = 0.25f;
        [SerializeField] Vector2 pitchClamp = new(-30f, 85f);
        [SerializeField] bool allowOrbitOverUI = true;
        [SerializeField] float closeOrbitSensitivity = 1.5f;
        [SerializeField] float farOrbitSensitivity = 0.6f;
        [SerializeField] float pivotLockProbeDistance = 800f;

        [Header("Zoom")]
        [SerializeField] float zoomSpeed = 6f;

        public float ZoomSpeed
        {
            get => zoomSpeed;
            set => zoomSpeed = value;
        }
        [SerializeField] float minDistance = 6f;
        [SerializeField] float maxDistance = 220f;
        [SerializeField] bool invertZoom;
        [SerializeField] bool allowZoomOverUI = true;

        [Header("Terrain Clearance")]
        [SerializeField] float terrainClearance = 2f;
        [SerializeField] float clearanceProbeHeight = 300f;
        [SerializeField] float collisionProbeRadius = 0.6f;
        [SerializeField] float collisionBuffer = 0.4f;

        float yaw;
        float pitch;
        float distance;

        EventSystem cachedEventSystem;
        Transform runtimePivot;
        Vector3 pivotPosition;
        bool warnedMissingGroundMask;
        World handWorld;
        EntityQuery handQuery;
        bool handQueryValid;
        bool orbitPivotLocked;
        Vector3 lockedPivot;
        float lockedDistance;
        bool grabbing;
        Plane panPlane;
        Vector3 panWorldStart;
        Vector3 panPivotStart;
        Vector3 lockedPivotStart;
        bool lockedPivotGrabActive;
        float grabHeightOffset;

        static int s_activeRigCount;

        BW2CameraInputBridge.Snapshot _inputSnapshot;
        bool _hasSnapshot;
        RmbContext _routerContext;
        bool _hasRouterContext;
        Vector3 _currentCameraPosition;
        Quaternion _currentCameraRotation = Quaternion.identity;

        public static bool HasActiveRig => s_activeRigCount > 0;

        Vector3 Pivot
        {
            get => pivotTransform != null ? pivotTransform.position : pivotPosition;
            set
            {
                if (pivotTransform != null) pivotTransform.position = value;
                else pivotPosition = value;
            }
        }

        void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<UnityEngine.Camera>();
                if (targetCamera == null)
                {
                    targetCamera = GetComponentInChildren<UnityEngine.Camera>();
                    if (targetCamera == null) targetCamera = UnityEngine.Camera.main;
                }
            }

            _currentCameraPosition = targetCamera != null ? targetCamera.transform.position : Vector3.zero;
            _currentCameraRotation = targetCamera != null ? targetCamera.transform.rotation : Quaternion.identity;

            EnsureInputRouter();

            if (pivotTransform == null)
            {
                runtimePivot = new GameObject("[CameraPivot]").transform;
                pivotTransform = runtimePivot;
            }
        }

        void OnEnable()
        {
            s_activeRigCount++;
        }

        void OnDisable()
        {
            s_activeRigCount = math.max(0, s_activeRigCount - 1);
        }

        void EnsureInputRouter()
        {
            if (inputRouter == null)
                inputRouter = GetComponent<HandCameraInputRouter>();
            if (inputRouter == null)
                inputRouter = GetComponentInParent<HandCameraInputRouter>();
            if (inputRouter == null)
                inputRouter = FindFirstObjectByType<HandCameraInputRouter>();
            if (inputRouter == null && !warnedMissingGroundMask)
            {
                Debug.LogWarning($"{nameof(BW2StyleCameraController)} on {name} could not find {nameof(HandCameraInputRouter)}; input will be inactive.", this);
            }
        }

        void Update()
        {
            if (inputRouter == null)
            {
                EnsureInputRouter();
                return;
            }

            UpdateInputSnapshot();
            UpdateRouterContext();

            // Basic pan/orbit/zoom based on snapshot
            ApplyInput();

            // Publish rig state
            PublishRig();
        }

        void UpdateInputSnapshot()
        {
            _hasSnapshot = BW2CameraInputBridge.TryGetSnapshot(out _inputSnapshot);
        }

        void UpdateRouterContext()
        {
            _hasRouterContext = false;
            if (!_hasSnapshot) return;
            if (targetCamera == null) return;

            var ray = targetCamera.ScreenPointToRay(_inputSnapshot.PointerPosition);

            // simple ground hit
            bool hasHit = UnityEngine.Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask);
            _routerContext = new RmbContext(
                _inputSnapshot.PointerPosition,
                ray,
                pointerOverUI: false,
                hasWorldHit: hasHit,
                worldHit: hasHit ? hit : default,
                worldPoint: hasHit ? hit.point : float3.zero,
                worldLayer: hasHit && hit.collider != null ? hit.collider.gameObject.layer : -1,
                deltaTime: UnityEngine.Time.deltaTime,
                unscaledDeltaTime: UnityEngine.Time.unscaledDeltaTime,
                handHasCargo: false,
                hitStorehouse: false,
                hitPile: false,
                hitDraggable: false,
                hitGround: hasHit);
            _hasRouterContext = true;
        }

        void ApplyInput()
        {
            if (!_hasSnapshot) return;

            // Zoom
            float scroll = _inputSnapshot.Scroll;
            if (math.abs(scroll) > 0.01f)
            {
                float zoomDir = invertZoom ? -scroll : scroll;
                distance = math.clamp(distance - zoomDir * zoomSpeed * UnityEngine.Time.deltaTime, minDistance, maxDistance);
            }

            // Orbit
            if (_inputSnapshot.MiddleHeld || (_inputSnapshot.RightHeld && allowOrbitOverUI))
            {
                yaw += _inputSnapshot.PointerDelta.x * orbitYawSensitivity;
                pitch = math.clamp(pitch - _inputSnapshot.PointerDelta.y * orbitPitchSensitivity, pitchClamp.x, pitchClamp.y);
            }

            // Pan (edge scroll)
            Vector2 delta = Vector2.zero;
            if (_inputSnapshot.EdgeLeft) delta.x -= 1f;
            if (_inputSnapshot.EdgeRight) delta.x += 1f;
            if (_inputSnapshot.EdgeTop) delta.y += 1f;
            if (_inputSnapshot.EdgeBottom) delta.y -= 1f;

            if (delta.sqrMagnitude > 0.0001f)
            {
                float panSpeed = panScale * math.max(distance, 1f);
                var yawRot = Quaternion.Euler(0f, yaw, 0f);
                Vector3 right = yawRot * Vector3.right;
                Vector3 forward = yawRot * Vector3.forward;
                _currentCameraPosition += (-right * delta.x + -forward * delta.y) * panSpeed * UnityEngine.Time.deltaTime;
            }

            // Apply to camera and pivot
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 camPos = _currentCameraPosition - rot * Vector3.forward * distance;
            targetCamera.transform.SetPositionAndRotation(camPos, rot);
            Pivot = _currentCameraPosition;
            _currentCameraRotation = rot;
        }

        void PublishRig()
        {
            var state = new CameraRigState
            {
                Position = targetCamera.transform.position,
                Rotation = targetCamera.transform.rotation,
                Pitch = pitch,
                Yaw = yaw,
                Distance = distance,
                PerspectiveMode = true,
                FieldOfView = targetCamera.fieldOfView,
                RigType = CameraRigType.Space4X
            };
            CameraRigService.Publish(state);
        }
    }
}

