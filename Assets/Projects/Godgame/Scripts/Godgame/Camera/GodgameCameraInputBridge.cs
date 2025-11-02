using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Godgame.Camera
{
    /// <summary>
    /// High-priority MonoBehaviour bridge that captures raw camera input outside the DOTS world.
    /// Ensures Godgame camera controls respond immediately and at high refresh rates.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class GodgameCameraInputBridge : MonoBehaviour
    {
        public struct Snapshot
        {
            public Vector2 Move;
            public float Vertical;
            public Vector2 Look;
            public Vector2 PointerPosition;
            public Vector2 PointerDelta;
            public float Scroll;
            public bool PrimaryHeld;
            public bool SecondaryHeld;
            public bool MiddleHeld;
            public bool ThrowModifier;
            public bool ToggleMode;
            public int Frame;
        }

        private static GodgameCameraInputBridge _instance;
        private static Snapshot _snapshot;

        private const float k_MinSampleRateHz = 30f;
        private const float k_DefaultMaxSampleRateHz = 240f;

        [SerializeField]
        private float _maxSampleRateHz = k_DefaultMaxSampleRateHz;

        private static float s_lastSampleTime;
        private static float s_lastSampleDelta;
        private bool _capturePending;
        private bool _actionsInitialized;

        private InputActionAsset _inputActions;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _cameraVerticalAction;
        private InputAction _cameraToggleModeAction;

        public static bool TryGetSnapshot(out Snapshot snapshot)
        {
            snapshot = _snapshot;
            return _instance != null;
        }

        public static float LastSampleDeltaTime => s_lastSampleDelta;

        public static float SampleRateHz => s_lastSampleDelta > Mathf.Epsilon ? 1f / s_lastSampleDelta : 0f;

        public static float MaxSampleRateHz
        {
            get => _instance != null ? _instance._maxSampleRateHz : k_DefaultMaxSampleRateHz;
            set
            {
                if (_instance == null)
                {
                    return;
                }

                _instance._maxSampleRateHz = Mathf.Clamp(value, k_MinSampleRateHz, 1000f);
            }
        }

        public static void ConsumeToggle()
        {
            if (_instance == null)
            {
                return;
            }

            var snapshot = _snapshot;
            snapshot.ToggleMode = false;
            _snapshot = snapshot;
        }

        public static void ConsumeMovement()
        {
            if (_instance == null)
            {
                return;
            }

            var snapshot = _snapshot;
            snapshot.Move = Vector2.zero;
            snapshot.Vertical = 0f;
            _snapshot = snapshot;
        }

        public static void ConsumeLook()
        {
            if (_instance == null)
            {
                return;
            }

            var snapshot = _snapshot;
            snapshot.Look = Vector2.zero;
            snapshot.PointerDelta = Vector2.zero;
            _snapshot = snapshot;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("Godgame Camera Input Bridge")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _instance = go.AddComponent<GodgameCameraInputBridge>();
            if (go.GetComponent<GodgameCameraController>() == null)
            {
                go.AddComponent<GodgameCameraController>();
            }
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            InputSystem.onBeforeUpdate += HandleBeforeUpdate;
            InputSystem.onAfterUpdate += HandleAfterUpdate;
            s_lastSampleTime = Time.realtimeSinceStartup;
            s_lastSampleDelta = 0f;
            _capturePending = true;
            CaptureInput();
        }

        private void OnDisable()
        {
            InputSystem.onBeforeUpdate -= HandleBeforeUpdate;
            InputSystem.onAfterUpdate -= HandleAfterUpdate;
            _capturePending = false;
        }

        private void HandleBeforeUpdate()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic)
            {
                return;
            }

            _capturePending = true;
        }

        private void HandleAfterUpdate()
        {
            if (!_capturePending || InputState.currentUpdateType != InputUpdateType.Dynamic)
            {
                return;
            }

            var now = Time.realtimeSinceStartup;
            var minInterval = 1f / Mathf.Clamp(_maxSampleRateHz, k_MinSampleRateHz, 1000f);
            if (now - s_lastSampleTime < minInterval)
            {
                return;
            }

            _capturePending = false;
            s_lastSampleDelta = now - s_lastSampleTime;
            s_lastSampleTime = now;

            CaptureInput();
        }

        private void CaptureInput()
        {
            EnsureInputActions();

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            var snapshot = new Snapshot
            {
                Move = Vector2.zero,
                Vertical = 0f,
                Look = Vector2.zero,
                PointerPosition = Vector2.zero,
                PointerDelta = Vector2.zero,
                Scroll = 0f,
                PrimaryHeld = false,
                SecondaryHeld = false,
                MiddleHeld = false,
                ThrowModifier = false,
                ToggleMode = false,
                Frame = Time.frameCount
            };

            if (_moveAction != null)
            {
                var moveValue = _moveAction.ReadValue<Vector2>();
                if (moveValue.sqrMagnitude > 1f)
                {
                    moveValue = moveValue.normalized;
                }

                snapshot.Move = moveValue;
            }
            else if (keyboard != null)
            {
                float x = 0f;
                float y = 0f;

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    y += 1f;
                }
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    y -= 1f;
                }
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    x -= 1f;
                }
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    x += 1f;
                }

                var move = new Vector2(x, y);
                if (move.sqrMagnitude > 1f)
                {
                    move.Normalize();
                }

                snapshot.Move = move;
            }

            if (_cameraVerticalAction != null)
            {
                snapshot.Vertical = _cameraVerticalAction.ReadValue<float>();
            }
            else if (keyboard != null)
            {
                if (keyboard.eKey.isPressed)
                {
                    snapshot.Vertical = 1f;
                }
                else if (keyboard.qKey.isPressed)
                {
                    snapshot.Vertical = -1f;
                }
            }

            if (_cameraToggleModeAction != null)
            {
                snapshot.ToggleMode = _cameraToggleModeAction.WasPressedThisFrame();
            }
            else if (keyboard != null)
            {
                snapshot.ToggleMode = keyboard.tabKey.wasPressedThisFrame;
            }

            if (mouse != null)
            {
                snapshot.PointerPosition = mouse.position.ReadValue();

                if (_lookAction != null)
                {
                    var look = _lookAction.ReadValue<Vector2>();
                    snapshot.Look = look;
                    snapshot.PointerDelta = look;
                }
                else
                {
                    var delta = mouse.delta.ReadValue();
                    snapshot.Look = delta;
                    snapshot.PointerDelta = delta;
                }

                var scroll = mouse.scroll.ReadValue();
                snapshot.Scroll = scroll.y;

                snapshot.PrimaryHeld = mouse.leftButton.isPressed;
                snapshot.SecondaryHeld = mouse.rightButton.isPressed;
                snapshot.MiddleHeld = mouse.middleButton.isPressed;
            }

            if (keyboard != null)
            {
                snapshot.ThrowModifier = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }

            _snapshot = snapshot;
        }

        private void EnsureInputActions()
        {
            if (_actionsInitialized && _inputActions != null)
            {
                return;
            }

            if (_inputActions == null)
            {
                var asset = Resources.Load<InputActionAsset>("InputSystem_Actions");
#if UNITY_EDITOR
                if (asset == null)
                {
                    asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                }
#endif

                if (asset != null)
                {
                    _inputActions = ScriptableObject.Instantiate(asset);
                }
            }

            if (_inputActions == null)
            {
                return;
            }

            var playerMap = _inputActions.FindActionMap("Player", throwIfNotFound: false);
            if (playerMap != null)
            {
                if (!playerMap.enabled)
                {
                    playerMap.Enable();
                }

                _moveAction = playerMap.FindAction("Move", throwIfNotFound: false);
                _lookAction = playerMap.FindAction("Look", throwIfNotFound: false);
            }

            var cameraMap = _inputActions.FindActionMap("Camera", throwIfNotFound: false);
            if (cameraMap != null)
            {
                if (!cameraMap.enabled)
                {
                    cameraMap.Enable();
                }

                _cameraVerticalAction = cameraMap.FindAction("CameraVertical", throwIfNotFound: false);
                _cameraToggleModeAction = cameraMap.FindAction("CameraToggleMode", throwIfNotFound: false);
            }

            _actionsInitialized = true;
        }
    }
}


