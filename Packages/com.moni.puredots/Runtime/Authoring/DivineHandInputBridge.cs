using PureDOTS.Input;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring
{
    /// <summary>
    /// Bridges HandCameraInputRouter pointer data into DOTS divine hand input and participates in RMB routing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DivineHandInputBridge : MonoBehaviour, IRmbHandler
    {
        [Header("Camera & Raycast")]
        [SerializeField] Camera targetCamera;
        [Tooltip("Additional height fallback if the ground plane is not hit.")]
        [SerializeField] float fallbackHeight = 12f;
        [Tooltip("Max ray distance when projecting onto the ground plane.")]
        [SerializeField] float maxRayDistance = 500f;

        [Header("Input Routing")]
        [SerializeField] HandCameraInputRouter inputRouter;
        [SerializeField] int rmbPriority = 50;
        [Tooltip("When enabled, only update while the GameWindow is focused.")]
        [SerializeField] bool requireFocus = true;

        World _world;
        EntityQuery _handQuery;
        bool _queryValid;

        float _holdTimer;
        float _releasedCharge;
        bool _holdActive;
        bool _hasReleasedCharge;

        byte _pendingGrabPress;
        byte _pendingGrabRelease;

        bool _registeredWithRouter;
        HandState _currentState = HandState.Empty;
        bool _handHasCargo;
        float _cooldownTimer;

        public int Priority => rmbPriority;

        void Awake()
        {
            EnsureRouter();
            EnsureWorld();
        }

        void OnEnable()
        {
            EnsureRouter();
            EnsureWorld();
            RegisterWithRouter();
        }

        void OnDisable()
        {
            UnregisterFromRouter();
            DisposeQuery();
        }

        void OnDestroy()
        {
            UnregisterFromRouter();
            DisposeQuery();
        }

        void Update()
        {
            if (requireFocus && !Application.isFocused)
            {
                return;
            }

            EnsureRouter();
            RegisterWithRouter();
            EnsureWorld();
            if (_world == null || !_world.IsCreated || !_queryValid || _handQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entityManager = _world.EntityManager;
            Entity handEntity;
            try
            {
                handEntity = _handQuery.GetSingletonEntity();
            }
            catch
            {
                return;
            }

            var config = entityManager.GetComponentData<DivineHandConfig>(handEntity);
            var input = entityManager.GetComponentData<DivineHandInput>(handEntity);
            var state = entityManager.GetComponentData<DivineHandState>(handEntity);

            ComputeCursor(config, out Vector3 cursorWorld, out Vector3 aimDirection);

            // Update DivineHandInput with new structure
            input.SampleTick = 0; // Will be set by system
            input.PlayerId = 0; // Default player
            input.PointerPosition = float2.zero; // Would need to track if needed
            input.PointerDelta = float2.zero;
            input.CursorWorldPosition = new float3(cursorWorld.x, cursorWorld.y, cursorWorld.z);
            input.AimDirection = math.normalizesafe(new float3(aimDirection.x, aimDirection.y, aimDirection.z), new float3(0f, -1f, 0f));
            input.PrimaryHeld = 0; // Not used in this bridge (RMB handling)
            input.SecondaryHeld = _holdActive ? (byte)1 : (byte)0;
            input.ThrowCharge = _holdActive ? _holdTimer : (_hasReleasedCharge ? _releasedCharge : 0f);
            input.PointerOverUI = 0; // Would need to check EventSystem if needed
            input.AppHasFocus = Application.isFocused ? (byte)1 : (byte)0;

            entityManager.SetComponentData(handEntity, input);

            // Write edge events to buffer
            if (entityManager.HasBuffer<HandInputEdge>(handEntity))
            {
                var edges = entityManager.GetBuffer<HandInputEdge>(handEntity);
                if (_pendingGrabPress != 0)
                {
                    edges.Add(new HandInputEdge
                    {
                        Button = InputButton.Secondary, // RMB is secondary
                        Kind = InputEdgeKind.Down,
                        Tick = 0, // Will be set by system
                        PointerPosition = float2.zero // Would need to track this if needed
                    });
                }
                if (_pendingGrabRelease != 0)
                {
                    edges.Add(new HandInputEdge
                    {
                        Button = InputButton.Secondary,
                        Kind = InputEdgeKind.Up,
                        Tick = 0,
                        PointerPosition = float2.zero
                    });
                }
            }

            bool hasHeldEntity = state.HeldEntity != Entity.Null && entityManager.Exists(state.HeldEntity);
            inputRouter?.ReportHandCargo(hasHeldEntity);
            _handHasCargo = hasHeldEntity;
            _currentState = state.CurrentState;
            _cooldownTimer = state.CooldownTimer;

            _pendingGrabPress = 0;
            _pendingGrabRelease = 0;

            if (!_holdActive)
            {
                if (_hasReleasedCharge)
                {
                    _releasedCharge = 0f;
                    _hasReleasedCharge = false;
                }
                _holdTimer = 0f;
            }
        }

        public bool CanHandle(in RmbContext context)
        {
            if (context.PointerOverUI)
            {
                return false;
            }

            if (context.HandHasCargo)
            {
                return true;
            }

            return _cooldownTimer <= 0.0001f;
        }

        public void OnRmb(in RmbContext context, RmbPhase phase)
        {
            switch (phase)
            {
                case RmbPhase.Started:
                    _holdTimer = 0f;
                    _releasedCharge = 0f;
                    _holdActive = true;
                    _hasReleasedCharge = false;
                    _pendingGrabPress = 1;
                    _pendingGrabRelease = 0;
                    break;

                case RmbPhase.Performed:
                    if (_holdActive)
                    {
                        _holdTimer += context.DeltaTime;
                    }
                    break;

                case RmbPhase.Canceled:
                    _pendingGrabRelease = 1;
                    _holdActive = false;
                    _releasedCharge = _holdTimer;
                    _hasReleasedCharge = true;
                    break;
            }
        }

        void ComputeCursor(in DivineHandConfig config, out Vector3 cursorWorld, out Vector3 aimDirection)
        {
            Ray pointerRay;
            Vector3 worldPoint;
            bool rayValid = false;

            if (inputRouter != null)
            {
                var context = inputRouter.CurrentContext;
                pointerRay = context.PointerRay;
                worldPoint = context.WorldPoint;
                rayValid = pointerRay.direction.sqrMagnitude > 0.0001f;
            }
            else
            {
                pointerRay = default;
                worldPoint = Vector3.zero;
            }

            if (!rayValid)
            {
                Camera cam = targetCamera != null ? targetCamera : Camera.main;
                if (cam == null)
                {
                    cursorWorld = transform.position;
                    aimDirection = Vector3.down;
                    return;
                }

                Vector3 screenPoint = UnityEngine.Input.mousePosition;
                pointerRay = cam.ScreenPointToRay(screenPoint);
                worldPoint = pointerRay.origin + pointerRay.direction * maxRayDistance;
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(pointerRay, out float enter))
                {
                    worldPoint = pointerRay.GetPoint(Mathf.Min(enter, maxRayDistance));
                }
            }

            float height = Mathf.Max(worldPoint.y, config.HoldHeightOffset, fallbackHeight);
            cursorWorld = new Vector3(worldPoint.x, height, worldPoint.z);

            Vector3 aimVector = worldPoint - pointerRay.origin;
            aimDirection = aimVector.sqrMagnitude > 0.0001f ? aimVector.normalized : pointerRay.direction.normalized;
        }

        void EnsureRouter()
        {
            if (inputRouter == null)
            {
                inputRouter = GetComponent<HandCameraInputRouter>();
            }

            if (inputRouter == null)
            {
                inputRouter = FindFirstObjectByType<HandCameraInputRouter>();
            }
        }

        void RegisterWithRouter()
        {
            if (_registeredWithRouter || inputRouter == null) return;
            inputRouter.RegisterHandler(this);
            _registeredWithRouter = true;
        }

        void UnregisterFromRouter()
        {
            if (!_registeredWithRouter || inputRouter == null) return;
            inputRouter.UnregisterHandler(this);
            _registeredWithRouter = false;
        }

        void EnsureWorld()
        {
            var newWorld = World.DefaultGameObjectInjectionWorld;
            if (newWorld == null || !newWorld.IsCreated)
            {
                DisposeQuery();
                _world = null;
                return;
            }

            if (_world == newWorld && _queryValid)
            {
                return;
            }

            DisposeQuery();

            _world = newWorld;
            _handQuery = _world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DivineHandTag>(),
                ComponentType.ReadWrite<DivineHandInput>(),
                ComponentType.ReadOnly<DivineHandConfig>(),
                ComponentType.ReadOnly<DivineHandState>());
            _queryValid = true;
        }

        void DisposeQuery()
        {
            if (_queryValid)
            {
                _handQuery.Dispose();
                _queryValid = false;
            }
        }
    }
}
