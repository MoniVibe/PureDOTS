using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Space4X.CameraControls
{
    /// <summary>
    /// High-priority MonoBehaviour bridge that captures raw player input outside the DOTS world.
    /// Runs at the very start of the frame so keyboard/mouse state is fresh when DOTS systems execute.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class Space4XCameraInputBridge : MonoBehaviour
    {
        public struct Snapshot
        {
            public Vector2 Pan;
            public float VerticalPan;
            public float Zoom;
            public Vector2 Rotate;
            public bool ResetRequested;
            public bool TogglePerspectiveMode;
            public int Frame;
        }

        private static Space4XCameraInputBridge _instance;
        private static Snapshot _snapshot;

        private const float k_DefaultMinSampleRateHz = 60f;
        private const float k_DefaultMaxSampleRateHz = 240f;

        [SerializeField]
        private float _minSampleRateHz = k_DefaultMinSampleRateHz;

        [SerializeField]
        private float _maxSampleRateHz = k_DefaultMaxSampleRateHz;

        private static float s_lastSampleTime;
        private static float s_lastSampleDelta;
        private bool _capturePending;

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

                _instance.SetSampleRateRange(_instance._minSampleRateHz, value);
            }
        }

        public static float MinSampleRateHz
        {
            get => _instance != null ? _instance._minSampleRateHz : k_DefaultMinSampleRateHz;
            set
            {
                if (_instance == null)
                {
                    return;
                }

                _instance.SetSampleRateRange(value, _instance._maxSampleRateHz);
            }
        }

        public static void ConfigureSampleRate(float minHz, float maxHz)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.SetSampleRateRange(minHz, maxHz);
        }

        public static bool TryGetSnapshot(out Snapshot snapshot)
        {
            snapshot = _snapshot;
            return _instance != null;
        }

        public static void ConsumeFrameFlags()
        {
            if (_instance == null)
            {
                return;
            }

            var snapshot = _snapshot;
            snapshot.ResetRequested = false;
            snapshot.TogglePerspectiveMode = false;
            _snapshot = snapshot;
        }

        public static void ConsumeRotation()
        {
            if (_instance == null)
            {
                return;
            }

            var snapshot = _snapshot;
            snapshot.Rotate = Vector2.zero;
            _snapshot = snapshot;
        }

        public static void ConsumeMovement()
        {
            if (_instance == null)
            {
                return;
            }

            var snapshot = _snapshot;
            snapshot.Pan = Vector2.zero;
            snapshot.VerticalPan = 0f;
            snapshot.Zoom = 0f;
            _snapshot = snapshot;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("Space4X Camera Input Bridge")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _instance = go.AddComponent<Space4XCameraInputBridge>();
            if (go.GetComponent<Space4XCameraMouseController>() == null)
            {
                go.AddComponent<Space4XCameraMouseController>();
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
            SetSampleRateRange(_minSampleRateHz, _maxSampleRateHz);
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

        private void Update()
        {
            EnsureSampleRateValid();
            var now = Time.realtimeSinceStartup;
            var minInterval = 1f / _maxSampleRateHz;
            if (now - s_lastSampleTime < minInterval)
            {
                return;
            }

            s_lastSampleDelta = now - s_lastSampleTime;
            s_lastSampleTime = now;
            _capturePending = false;
            CaptureInput();
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

            EnsureSampleRateValid();
            var now = Time.realtimeSinceStartup;
            var minInterval = 1f / _maxSampleRateHz;
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
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var gamepad = Gamepad.current;

            var snapshot = new Snapshot
            {
                Pan = Vector2.zero,
                VerticalPan = 0f,
                Zoom = 0f,
                Rotate = Vector2.zero,
                ResetRequested = false,
                TogglePerspectiveMode = false,
                Frame = Time.frameCount
            };

            Vector2 pan = Vector2.zero;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    pan.y += 1f;
                }
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    pan.y -= 1f;
                }
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    pan.x -= 1f;
                }
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    pan.x += 1f;
                }

                if (keyboard.eKey.isPressed)
                {
                    snapshot.VerticalPan = 1f;
                }
                else if (keyboard.qKey.isPressed)
                {
                    snapshot.VerticalPan = -1f;
                }

                snapshot.ResetRequested = keyboard.rKey.wasPressedThisFrame;
                snapshot.TogglePerspectiveMode = keyboard.vKey.wasPressedThisFrame;
            }

            if (gamepad != null)
            {
                var stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.0001f)
                {
                    pan += stick;
                }

                var vertical = gamepad.leftTrigger.ReadValue() - gamepad.rightTrigger.ReadValue();
                if (Mathf.Abs(vertical) > Mathf.Epsilon)
                {
                    snapshot.VerticalPan = Mathf.Clamp(vertical, -1f, 1f);
                }

                if (gamepad.buttonNorth.wasPressedThisFrame)
                {
                    snapshot.ResetRequested = true;
                }

                if (gamepad.buttonEast.wasPressedThisFrame)
                {
                    snapshot.TogglePerspectiveMode = true;
                }

                var lookStick = gamepad.rightStick.ReadValue();
                if (Mathf.Abs(lookStick.y) > 0.0001f && Mathf.Abs(snapshot.Zoom) < 0.0001f)
                {
                    snapshot.Zoom = -lookStick.y;
                }
            }

            if (pan.sqrMagnitude > 1f)
            {
                pan.Normalize();
            }

            snapshot.Pan = pan;

            if (mouse != null)
            {
                var mouseDelta = mouse.delta.ReadValue();

                if (mouse.middleButton.isPressed)
                {
                    snapshot.Pan += new Vector2(mouseDelta.x, -mouseDelta.y) * 0.01f;
                }

                if (mouse.rightButton.isPressed)
                {
                    snapshot.Rotate = mouseDelta;
                }

                var scroll = mouse.scroll.ReadValue();
                if (!Mathf.Approximately(scroll.y, 0f))
                {
                    snapshot.Zoom = -scroll.y;
                }
            }

            _snapshot = snapshot;
        }

        private void LateUpdate()
        {
            ConsumeFrameFlags();
        }

        private void SetSampleRateRange(float minHz, float maxHz)
        {
            var clampedMin = Mathf.Clamp(minHz, k_DefaultMinSampleRateHz, k_DefaultMaxSampleRateHz);
            var clampedMax = Mathf.Clamp(maxHz, clampedMin, k_DefaultMaxSampleRateHz);

            _minSampleRateHz = clampedMin;
            _maxSampleRateHz = clampedMax;
        }

        private void EnsureSampleRateValid()
        {
            SetSampleRateRange(_minSampleRateHz, _maxSampleRateHz);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetSampleRateRange(_minSampleRateHz, _maxSampleRateHz);
        }
#endif
    }
}

