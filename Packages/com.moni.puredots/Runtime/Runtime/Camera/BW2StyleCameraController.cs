using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
// Note: PureDOTS.Input dependency removed to break cycle - Runtime should not depend on Input
// Input functionality is available in the Camera assembly version of this controller
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
    /// CAMERA RIG CONTRACT - BW2StyleCameraController
    ///
    /// A reusable camera controller implementing Black & White 2 style camera behavior.
    /// Given input + deltaTime + current state, produces a new CameraRigState that behaves
    /// like a B&W2 orbit/RTS camera with terrain awareness.
    ///
    /// CONTRACT GUARANTEES:
    /// - LMB drag: Pans camera across terrain maintaining height
    /// - MMB drag: Orbits around pivot point with distance-scaled sensitivity
    /// - Scroll wheel: Zooms in/out with collision avoidance
    /// - Terrain clamping: Prevents camera from going through ground
    /// - Pivot locking: MMB press locks orbit pivot to terrain point
    /// - Publishes CameraRigState to CameraRigService automatically
    ///
    /// CONFIGURATION:
    /// - Ground layer mask for terrain collision
    /// - Pan/zoom/orbit sensitivities with distance scaling
    /// - Terrain clearance and collision buffer settings
    /// - UI interaction allowances
    ///
    /// USAGE PATTERN:
    /// 1. Attach to GameObject with Camera + CameraRigApplier + BW2CameraInputBridge
    /// 2. Configure terrain mask and sensitivities
    /// 3. Controller automatically handles input and publishes CameraRigState
    /// 4. CameraRigApplier applies the state to Camera.main
    ///
    /// Uses frame-time (Time.deltaTime).
    /// Not part of deterministic simulation / rewind.
    /// Safe utility for game projects to build cameras on.
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
        // Note: HandCameraInputRouter removed - Runtime assembly cannot reference Input assembly
        // Use the Camera assembly version of this controller if input routing is needed
        // [SerializeField] HandCameraInputRouter inputRouter;

        [Header("Ground")]
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float groundProbeDistance = 600f;

        [Header("Pan")]
        [SerializeField] float panScale = 1f;
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

            if (groundMask.value == 0)
            {
                Debug.LogWarning($"{nameof(BW2StyleCameraController)} on {name} has an empty groundMask; terrain snapping and collision guards will be inactive.", this);
                warnedMissingGroundMask = true;
            }

            cachedEventSystem = EventSystem.current;

            Vector3 pivot = pivotTransform != null ? pivotTransform.position : pivotPosition;
            if (pivotTransform == null && pivot == Vector3.zero) pivotPosition = transform.position;

            if (pivot == Vector3.zero)
            {
                pivot = DetermineInitialPivot();
                Pivot = pivot;
            }
            else
            {
                SnapPivotToTerrain();
            }

            InitialiseCameraState();
            AlignPivotToHandCursor();
            ApplyCameraPose();
        }

        void OnEnable()
        {
            s_activeRigCount++;
            EnsureInputRouter();
            HybridControlCoordinator.Mode = HybridControlCoordinator.InputMode.GodgameOnly;
        }

        void OnDisable()
        {
            s_activeRigCount = Mathf.Max(0, s_activeRigCount - 1);
            if (s_activeRigCount == 0 && HybridControlCoordinator.Mode == HybridControlCoordinator.InputMode.GodgameOnly)
            {
                HybridControlCoordinator.Mode = HybridControlCoordinator.InputMode.Dual;
            }
            DisposeHandQuery();
        }

        void OnDestroy()
        {
            if (runtimePivot != null)
            {
                Destroy(runtimePivot.gameObject);
                runtimePivot = null;
            }
            DisposeHandQuery();
        }

        void LateUpdate()
        {
            if (targetCamera == null)
            {
                Debug.LogWarning($"{nameof(BW2StyleCameraController)} requires a Camera reference.", this);
                enabled = false;
                return;
            }

            if (cachedEventSystem == null || !cachedEventSystem.isActiveAndEnabled)
                cachedEventSystem = EventSystem.current;

            EnsureInputRouter();
            UpdateInputSources();

            Vector2 pointer = GetPointerPosition();
            bool pointerOverUI = _hasRouterContext ? _routerContext.PointerOverUI : (cachedEventSystem != null && cachedEventSystem.IsPointerOverGameObject());

            HandlePan(pointer, pointerOverUI);

            bool orbitHeld = GetOrbitHeld();
            bool orbitPressed = GetOrbitPressedThisFrame();
            bool orbitReleased = GetOrbitReleasedThisFrame();
            HandleOrbit(pointer, pointerOverUI, orbitHeld, orbitPressed, orbitReleased);

            bool handAvailable = TryGetHandCursor(out var handCursor);
            HandleZoom(pointer, pointerOverUI, orbitHeld, handAvailable, handCursor);

            if (!orbitPivotLocked)
            {
                SnapPivotToTerrain();
            }
            else
            {
                MaintainLockedPivotHeight();
            }

            ApplyCameraPose();
        }

        void UpdateInputSources()
        {
            _hasSnapshot = BW2CameraInputBridge.TryGetSnapshot(out _inputSnapshot);
            // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
            // Code falls back to Unity InputSystem directly
            _hasRouterContext = false;
        }

        bool TryGetPointerWorld(out Vector3 world)
        {
            if (_hasRouterContext && (_routerContext.HasWorldHit || _routerContext.HitGround))
            {
                world = _routerContext.WorldPoint;
                return true;
            }

            if (targetCamera != null)
            {
                var pointer = GetPointerPosition();
                var ray = targetCamera.ScreenPointToRay(pointer);
                if (groundMask.value != 0 && UnityEngine.Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
                {
                    world = hit.point;
                    return true;
                }

                var plane = new Plane(Vector3.up, Pivot);
                if (plane.Raycast(ray, out float enter))
                {
                    world = ray.GetPoint(Mathf.Min(enter, groundProbeDistance));
                    return true;
                }
            }

            world = Vector3.zero;
            return false;
        }

        void HandlePan(Vector2 pointer, bool pointerOverUI)
        {
            bool panPressed = GetPanPressedThisFrame();
            bool panHeld = GetPanHeld();

            if (panPressed && (!pointerOverUI || allowPanOverUI))
            {
                BeginGrab(pointer);
            }

            if (!panHeld)
            {
                grabbing = false;
                lockedPivotGrabActive = false;
                return;
            }

            if (grabbing && (!pointerOverUI || allowPanOverUI))
            {
                UpdateGrab(pointer);
            }
        }

        void BeginGrab(Vector2 pointer)
        {
            if (!TryGetPointerWorld(out panWorldStart))
            {
                grabbing = false;
                return;
            }

            panPlane = new Plane(Vector3.up, Pivot);
            panPivotStart = Pivot;
            if (orbitPivotLocked)
            {
                lockedPivotStart = lockedPivot;
                lockedPivotGrabActive = true;
            }
            else
            {
                lockedPivotGrabActive = false;
            }

            if (!TrySampleTerrainHeight(new Vector3(_currentCameraPosition.x, _currentCameraPosition.y + clearanceProbeHeight, _currentCameraPosition.z), out float groundY))
            {
                groundY = Pivot.y;
            }
            grabHeightOffset = _currentCameraPosition.y - groundY;
            grabbing = true;
        }

        void UpdateGrab(Vector2 pointer)
        {
            if (!TryGetPointerWorld(out var currentWorld))
            {
                return;
            }
            Vector3 delta = currentWorld - panWorldStart;

            if (delta.sqrMagnitude < panDeadzoneMeters * panDeadzoneMeters)
            {
                return;
            }

            Vector3 newPivot = panPivotStart - delta * panScale;
            newPivot.y = panPivotStart.y;
            Pivot = newPivot;

            if (orbitPivotLocked && lockedPivotGrabActive)
            {
                Vector3 locked = lockedPivotStart - delta * panScale;
                locked.y = lockedPivotStart.y;
                lockedPivot = locked;
            }
        }

        void HandleOrbit(Vector2 pointer, bool pointerOverUI, bool orbitHeld, bool orbitPressed, bool orbitReleased)
        {
            if (orbitPressed && (!pointerOverUI || allowOrbitOverUI))
            {
                LockOrbitPivot(pointer);
            }

            if (orbitReleased && !orbitHeld && orbitPivotLocked)
            {
                orbitPivotLocked = false;
                Pivot = lockedPivot;
                distance = Mathf.Clamp(lockedDistance, minDistance, maxDistance);
            }

            if (orbitHeld && (!pointerOverUI || allowOrbitOverUI))
            {
                Vector2 lookDelta = GetLookDelta();
                if (lookDelta.sqrMagnitude > 0.0001f)
                {
                    float radius = orbitPivotLocked ? lockedDistance : distance;
                    float sensitivityScale = Mathf.Lerp(closeOrbitSensitivity, farOrbitSensitivity, Mathf.InverseLerp(minDistance, maxDistance, radius));
                    yaw = Mathf.Repeat(yaw + lookDelta.x * orbitYawSensitivity * sensitivityScale + 180f, 360f) - 180f;
                    pitch = Mathf.Clamp(pitch + lookDelta.y * orbitPitchSensitivity * sensitivityScale, pitchClamp.x, pitchClamp.y);
                }
            }
        }

        void LockOrbitPivot(Vector2 pointer)
        {
            if (!TryProjectToGround(pointer, out var world, out _))
            {
                var ray = targetCamera.ScreenPointToRay(pointer);
                var plane = new Plane(Vector3.up, Pivot);
                if (plane.Raycast(ray, out float enter))
                {
                    world = ray.GetPoint(Mathf.Min(enter, pivotLockProbeDistance));
                }
                else
                {
                    world = Pivot;
                }
            }

            lockedPivot = world;
            MaintainLockedPivotHeight();
            lockedDistance = distance;
            orbitPivotLocked = true;
        }

        void HandleZoom(Vector2 pointer, bool pointerOverUI, bool orbitHeld, bool handAvailable, Vector3 handCursor)
        {
            float scroll = GetScrollDelta();
            if (Mathf.Abs(scroll) < 0.001f)
            {
                return;
            }

            if (pointerOverUI && !allowZoomOverUI)
            {
                return;
            }

            float zoomSign = invertZoom ? -scroll : scroll;
            float newDistance = Mathf.Clamp(distance - zoomSign * zoomSpeed, minDistance, maxDistance);

            Vector3 zoomFocus = handAvailable ? handCursor : Pivot;
            if (_hasRouterContext && (_routerContext.HasWorldHit || _routerContext.HitGround))
            {
                zoomFocus = _routerContext.WorldPoint;
            }
            zoomFocus.y = Pivot.y;

            if (orbitHeld && orbitPivotLocked)
            {
                lockedDistance = newDistance;
                lockedPivot = Vector3.Lerp(lockedPivot, zoomFocus, 0.25f);
            }
            else
            {
                Vector3 pivot = Vector3.Lerp(Pivot, zoomFocus, 0.25f);
                Pivot = pivot;
            }

            distance = newDistance;
        }

        void InitialiseCameraState()
        {
            if (targetCamera == null) return;

            Vector3 pivot = Pivot;
            Vector3 camPos = _currentCameraPosition;
            Vector3 rel = camPos - pivot;
            if (rel.sqrMagnitude < 0.0001f)
            {
                var forward = _currentCameraRotation * Vector3.forward;
                rel = -forward * Mathf.Max(minDistance, 0.1f);
            }

            distance = Mathf.Clamp(rel.magnitude, minDistance, maxDistance);
            yaw = Mathf.Atan2(rel.x, rel.z) * Mathf.Rad2Deg;
            float denom = Mathf.Max(0.0001f, rel.magnitude);
            float ratio = Mathf.Clamp(rel.y / denom, -1f, 1f);
            pitch = Mathf.Clamp(Mathf.Asin(ratio) * Mathf.Rad2Deg, pitchClamp.x, pitchClamp.y);
            lockedDistance = distance;
            lockedPivot = pivot;
            orbitPivotLocked = false;
        }

        Vector3 DetermineInitialPivot()
        {
            if (targetCamera != null)
            {
                Vector3 camPos = _currentCameraPosition;
                if (TrySampleTerrainHeight(new Vector3(camPos.x, camPos.y + clearanceProbeHeight, camPos.z), out float groundY))
                    return new Vector3(camPos.x, groundY, camPos.z);
            }

            Vector3 fallback = transform.position;
            fallback.y = Pivot.y;
            return fallback;
        }

        void SnapPivotToTerrain()
        {
            if (groundMask.value == 0) return;

            Vector3 pivot = Pivot;
            float probeHeight = Mathf.Max(clearanceProbeHeight, 50f);
            Vector3 origin = pivot + Vector3.up * probeHeight;
            if (UnityEngine.Physics.Raycast(origin, Vector3.down, out var hit, probeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                pivot.y = hit.point.y;
                Pivot = pivot;
            }
        }

        void ApplyCameraPose()
        {
            Vector3 pivot = orbitPivotLocked ? lockedPivot : Pivot;
            float radius = Mathf.Clamp(orbitPivotLocked ? lockedDistance : distance, minDistance, maxDistance);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            Vector3 desired = pivot + direction * radius;

            desired = ResolveCollision(pivot, desired);
            desired = EnsureTerrainClearance(desired);

            if (grabbing)
            {
                Vector3 sampleOrigin = new Vector3(desired.x, desired.y + clearanceProbeHeight, desired.z);
                if (TrySampleTerrainHeight(sampleOrigin, out float groundY))
                {
                    float targetY = groundY + grabHeightOffset;
                    float minY = groundY + terrainClearance;
                    desired.y = Mathf.Max(minY, targetY);
                }
            }

            float resolvedDistance = Vector3.Distance(pivot, desired);
            if (!Mathf.Approximately(resolvedDistance, radius))
            {
                distance = Mathf.Clamp(resolvedDistance, minDistance, maxDistance);
                if (orbitPivotLocked)
                {
                    lockedDistance = distance;
                }
            }

            _currentCameraPosition = desired;
            _currentCameraRotation = rotation;

            var rigState = new CameraRigState
            {
                Position = desired,
                Rotation = rotation,
                Pitch = math.radians(pitch),
                Yaw = math.radians(yaw),
                Distance = distance,
                PerspectiveMode = true,
                FieldOfView = targetCamera != null ? targetCamera.fieldOfView : 60f,
                RigType = CameraRigType.BW2
            };

            CameraRigService.Publish(rigState);
        }

        Vector3 EnsureTerrainClearance(Vector3 desired)
        {
            if (groundMask.value == 0) return desired;

            float probeHeight = Mathf.Max(clearanceProbeHeight, 25f);
            Vector3 origin = desired + Vector3.up * probeHeight;
            if (UnityEngine.Physics.Raycast(origin, Vector3.down, out var hit, probeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                float minY = hit.point.y + terrainClearance;
                if (desired.y < minY) desired.y = minY;
            }

            return desired;
        }

        Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            if (groundMask.value == 0 || collisionProbeRadius <= 0f)
            {
                return desired;
            }

            Vector3 toCamera = desired - pivot;
            float maxDistance = toCamera.magnitude;
            if (maxDistance <= 0.0001f)
            {
                return desired;
            }

            Vector3 direction = toCamera / maxDistance;
            if (UnityEngine.Physics.SphereCast(pivot, collisionProbeRadius, direction, out var hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                float adjusted = Mathf.Max(collisionBuffer, hit.distance - collisionBuffer);
                return pivot + direction * adjusted;
            }

            return desired;
        }

        void MaintainLockedPivotHeight()
        {
            if (!orbitPivotLocked || groundMask.value == 0) return;

            float probeHeight = Mathf.Max(clearanceProbeHeight, 50f);
            Vector3 origin = lockedPivot + Vector3.up * probeHeight;
            if (UnityEngine.Physics.Raycast(origin, Vector3.down, out var hit, probeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                lockedPivot.y = hit.point.y;
            }
        }

        bool TryProjectToGround(Vector2 screenPosition, out Vector3 world, out Vector3 normal)
        {
            if (targetCamera == null)
            {
                world = Vector3.zero;
                normal = Vector3.up;
                return false;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            if (groundMask.value != 0 && UnityEngine.Physics.Raycast(ray, out var hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                world = hit.point;
                normal = hit.normal;
                return true;
            }

            Plane plane = new(Vector3.up, new Vector3(0f, Pivot.y, 0f));
            if (plane.Raycast(ray, out float t))
            {
                world = ray.GetPoint(t);
                normal = Vector3.up;
                return true;
            }

            if (!warnedMissingGroundMask)
            {
                Debug.LogWarning($"{nameof(BW2StyleCameraController)} could not project pointer to terrain. Check groundMask setup.", this);
                warnedMissingGroundMask = true;
            }

            world = Vector3.zero;
            normal = Vector3.up;
            return false;
        }

        bool TrySampleTerrainHeight(Vector3 origin, out float groundY)
        {
            groundY = origin.y;
            if (groundMask.value == 0) return false;

            float probeHeight = Mathf.Max(clearanceProbeHeight, 25f);
            if (UnityEngine.Physics.Raycast(origin, Vector3.down, out var hit, probeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y;
                return true;
            }

            return false;
        }

        Vector2 GetPointerPosition()
        {
            if (_hasRouterContext)
            {
                return _routerContext.PointerScreenPosition;
            }

            if (_hasSnapshot)
            {
                return _inputSnapshot.PointerPosition;
            }
#if ENABLE_INPUT_SYSTEM
            // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
            // if (inputRouter != null) { return inputRouter.PointerPosition; }
            var mouse = Mouse.current;
            if (mouse != null) return mouse.position.ReadValue();
            var pointer = Pointer.current;
            if (pointer != null) return pointer.position.ReadValue();
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        bool GetPanPressedThisFrame()
        {
            if (_hasSnapshot)
            {
                return _inputSnapshot.LeftPressed && !_inputSnapshot.RightHeld;
            }
            return
#if ENABLE_INPUT_SYSTEM
                // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
#else
                Input.GetMouseButtonDown(0);
#endif
        }

        bool GetPanHeld()
        {
            if (_hasSnapshot)
            {
                return _inputSnapshot.LeftHeld && !_inputSnapshot.RightHeld;
            }
            return
#if ENABLE_INPUT_SYSTEM
                // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
                (Mouse.current != null && Mouse.current.leftButton.isPressed);
#else
                Input.GetMouseButton(0);
#endif
        }

        bool GetOrbitHeld()
        {
            if (_hasSnapshot)
            {
                return _inputSnapshot.RightHeld || (_inputSnapshot.LeftHeld && _inputSnapshot.RightHeld);
            }
            return
#if ENABLE_INPUT_SYSTEM
                // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
                (Mouse.current != null && Mouse.current.middleButton.isPressed);
#else
                Input.GetMouseButton(2);
#endif
        }

        bool GetOrbitPressedThisFrame()
        {
            if (_hasSnapshot)
            {
                return _inputSnapshot.RightPressed || (_inputSnapshot.LeftPressed && _inputSnapshot.RightHeld);
            }
            return
#if ENABLE_INPUT_SYSTEM
                // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
                (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame);
#else
                Input.GetMouseButtonDown(2);
#endif
        }

        bool GetOrbitReleasedThisFrame()
        {
            if (_hasSnapshot)
            {
                return _inputSnapshot.RightReleased;
            }
            return
#if ENABLE_INPUT_SYSTEM
                // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
                (Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame);
#else
                Input.GetMouseButtonUp(2);
#endif
        }

        Vector2 GetLookDelta()
        {
            if (_hasSnapshot)
            {
                return _inputSnapshot.PointerDelta;
            }
            return
#if ENABLE_INPUT_SYSTEM
                // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
                (Mouse.current != null ? Mouse.current.delta.ReadValue() :
                        (Pointer.current != null ? Pointer.current.delta.ReadValue() : Vector2.zero));
#else
                new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
        }

        float GetScrollDelta()
        {
            if (_hasSnapshot)
            {
                return _inputSnapshot.Scroll;
            }
#if ENABLE_INPUT_SYSTEM
            // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
            // if (inputRouter != null) { return inputRouter.ScrollValue.y; }

            if (Mouse.current != null)
            {
                return Mouse.current.scroll.ReadValue().y;
            }

            var pointer = Pointer.current;
            if (pointer != null)
            {
                if (pointer is Mouse fallbackMouse)
                {
                    return fallbackMouse.scroll.ReadValue().y;
                }

                var scrollControl = pointer.TryGetChildControl<Vector2Control>("scroll");
                if (scrollControl != null)
                {
                    return scrollControl.ReadValue().y;
                }
            }

            return 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        void AlignPivotToHandCursor()
        {
            if (TryGetHandCursor(out var handCursor))
            {
                Vector3 pivot = Pivot;
                pivot.x = handCursor.x;
                pivot.z = handCursor.z;
                Pivot = pivot;
                SnapPivotToTerrain();
            }
        }

        bool TryGetHandCursor(out Vector3 cursor)
        {
            cursor = default;
#if GODGAME
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                DisposeHandQuery();
                return false;
            }

            if (handWorld != world || !handQueryValid)
            {
                DisposeHandQuery();
                handWorld = world;
                try
                {
                    handQuery = handWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<DivineHandTag>(),
                        ComponentType.ReadOnly<DivineHandState>());
                    handQueryValid = true;
                }
                catch
                {
                    handQueryValid = false;
                    return false;
                }
            }

            if (!handQueryValid || handQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            if (handQuery.TryGetSingleton(out DivineHandState state))
            {
                float3 c = state.CursorPosition;
                cursor = new Vector3(c.x, c.y, c.z);
                return true;
            }
#endif
            return false;
        }

        void DisposeHandQuery()
        {
            if (!handQueryValid)
            {
                return;
            }

            if (handWorld != null && handWorld.IsCreated)
            {
                try
                {
                    handQuery.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{nameof(BW2StyleCameraController)}] Failed to dispose hand query: {ex.Message}", this);
                }
            }

            handQueryValid = false;
            handWorld = null;
        }

        void EnsureInputRouter()
        {
            // Note: inputRouter removed - Runtime assembly cannot reference Input assembly
            // Use the Camera assembly version of this controller if input routing is needed
        }
    }
}
