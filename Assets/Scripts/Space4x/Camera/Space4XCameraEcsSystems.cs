using PureDOTS.Runtime.Camera;
using PureDOTS.Runtime.Config;
using PureDOTS.Systems;
using Space4X.CameraComponents;
using Space4X.CameraControls;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space4X.CameraSystems
{
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [CreateAfter(typeof(RuntimeConfigBootstrapSystem))]
    public sealed partial class Space4XCameraBootstrapSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            var entityManager = EntityManager;

            if (!SystemAPI.TryGetSingletonEntity<Space4XCameraState>(out _))
            {
                var stateEntity = entityManager.CreateEntity(typeof(Space4XCameraState));
                entityManager.SetComponentData(stateEntity, CreateDefaultState());
            }

            if (!SystemAPI.TryGetSingletonEntity<Space4XCameraConfig>(out _))
            {
                var configEntity = entityManager.CreateEntity(typeof(Space4XCameraConfig));
                entityManager.SetComponentData(configEntity, CreateDefaultConfig());
            }

            if (!SystemAPI.TryGetSingletonEntity<Space4XCameraInputFlags>(out _))
            {
                var flagsEntity = entityManager.CreateEntity(typeof(Space4XCameraInputFlags));
                entityManager.SetComponentData(flagsEntity, new Space4XCameraInputFlags());
            }

            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        private static Space4XCameraState CreateDefaultState()
        {
            return new Space4XCameraState
            {
                Position = new float3(0f, 25f, -30f),
                Rotation = quaternion.Euler(math.radians(40f), 0f, 0f),
                Pitch = math.radians(40f),
                Yaw = 0f,
                PerspectiveMode = false
            };
        }

        private static Space4XCameraConfig CreateDefaultConfig()
        {
            return new Space4XCameraConfig
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
        }
    }

    [UpdateInGroup(typeof(CameraPhaseGroup))]
    public sealed partial class Space4XCameraEcsSystem : SystemBase
    {
        private RuntimeConfigVar _ecsModeVar;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<Space4XCameraState>();
            RequireForUpdate<Space4XCameraConfig>();
            RuntimeConfigRegistry.Initialize();
            _ecsModeVar = Space4XCameraConfigVars.EcsModeEnabled;
        }

        protected override void OnUpdate()
        {
            if (_ecsModeVar == null || !_ecsModeVar.BoolValue)
            {
                return;
            }

            if (!Space4XCameraInputBridge.TryGetSnapshot(out var snapshot))
            {
                return;
            }

            var stateEntity = SystemAPI.GetSingletonEntity<Space4XCameraState>();
            var state = EntityManager.GetComponentData<Space4XCameraState>(stateEntity);
            var config = SystemAPI.GetSingleton<Space4XCameraConfig>();

            var deltaTime = ResolveDeltaTime();

            var pan = new float2(snapshot.Pan.x, snapshot.Pan.y);
            var verticalPan = snapshot.VerticalPan;
            var zoom = snapshot.Zoom;
            var rotate = new float2(snapshot.Rotate.x, snapshot.Rotate.y);

            var hasPanInput = math.lengthsq(pan) > 0f;
            var hasVerticalInput = math.abs(verticalPan) > 0f;
            var hasZoomInput = math.abs(zoom) > 0f;
            var hasRotationInput = math.lengthsq(rotate) > float.Epsilon;

            var movementApplied = false;
            var rotationApplied = false;

            if (snapshot.TogglePerspectiveMode)
            {
                state.PerspectiveMode = !state.PerspectiveMode;
            }

            if (snapshot.ResetRequested)
            {
                state.Position = new float3(0f, 25f, -30f);
                state.Pitch = math.radians(40f);
                state.Yaw = 0f;
                state.PerspectiveMode = false;
                movementApplied = true;
                rotationApplied = true;
            }

            if (config.EnableRotation && hasRotationInput)
            {
                var rotationDelta = rotate * config.RotationSpeed;
                state.Yaw += math.radians(rotationDelta.x);
                state.Pitch -= math.radians(rotationDelta.y);
                state.Pitch = math.clamp(state.Pitch, config.PitchMin, config.PitchMax);
                rotationApplied = true;
            }

            if (config.EnablePan && hasPanInput)
            {
                var moveInput = pan;
                if (math.lengthsq(moveInput) > 1f)
                {
                    moveInput = math.normalize(moveInput);
                }

                float3 moveDir;
                if (state.PerspectiveMode)
                {
                    var fullRotation = quaternion.Euler(state.Pitch, state.Yaw, 0f);
                    var forward = math.mul(fullRotation, new float3(0f, 0f, 1f));
                    var right = math.mul(fullRotation, new float3(1f, 0f, 0f));
                    moveDir = (right * moveInput.x + forward * moveInput.y) * config.PanSpeed * deltaTime;
                }
                else
                {
                    var yawRotation = quaternion.Euler(0f, state.Yaw, 0f);
                    var forward = math.mul(yawRotation, new float3(0f, 0f, 1f));
                    var right = math.mul(yawRotation, new float3(1f, 0f, 0f));
                    moveDir = (right * moveInput.x + forward * moveInput.y) * config.PanSpeed * deltaTime;
                }

                state.Position += moveDir;
                movementApplied = true;
            }

            if (config.EnablePan && hasVerticalInput)
            {
                var verticalSpeed = config.VerticalPanSpeed > 0f ? config.VerticalPanSpeed : config.PanSpeed;
                float3 verticalMove;

                if (state.PerspectiveMode)
                {
                    var fullRotation = quaternion.Euler(state.Pitch, state.Yaw, 0f);
                    var up = math.mul(fullRotation, new float3(0f, 1f, 0f));
                    verticalMove = up * verticalPan * verticalSpeed * deltaTime;
                }
                else
                {
                    verticalMove = new float3(0f, verticalPan * verticalSpeed * deltaTime, 0f);
                }

                state.Position += verticalMove;
                movementApplied = true;
            }

            if (config.EnableZoom && hasZoomInput)
            {
                var forward = math.forward(quaternion.Euler(state.Pitch, state.Yaw, 0f));
                var zoomDelta = forward * zoom * config.ZoomSpeed * deltaTime;
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

            state.Rotation = quaternion.Euler(state.Pitch, state.Yaw, 0f);

            if (movementApplied || rotationApplied || snapshot.ResetRequested || snapshot.TogglePerspectiveMode)
            {
                EntityManager.SetComponentData(stateEntity, state);
            }

            if (SystemAPI.TryGetSingletonRW<Space4XCameraInputFlags>(out var flags))
            {
                flags.ValueRW = new Space4XCameraInputFlags
                {
                    MovementHandled = true,
                    RotationHandled = true
                };
            }

            var camera = Camera.main;
            var rigState = new CameraRigState
            {
                Position = new Vector3(state.Position.x, state.Position.y, state.Position.z),
                Rotation = new Quaternion(state.Rotation.value.x, state.Rotation.value.y, state.Rotation.value.z, state.Rotation.value.w),
                Pitch = state.Pitch,
                Yaw = state.Yaw,
                Distance = math.length(state.Position),
                PerspectiveMode = state.PerspectiveMode,
                FieldOfView = camera != null ? camera.fieldOfView : 60f,
                RigType = CameraRigType.Space4X
            };

            CameraRigService.Publish(rigState);

            if (hasPanInput || hasVerticalInput || hasZoomInput || snapshot.ResetRequested || snapshot.TogglePerspectiveMode)
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
        }

        private static float ResolveDeltaTime()
        {
            var bridgeDelta = Space4XCameraInputBridge.LastSampleDeltaTime;
            const float maxStepDelta = 1f / 30f;
            const float fallbackDelta = 1f / 60f;

            if (!float.IsNaN(bridgeDelta) && !float.IsInfinity(bridgeDelta) && bridgeDelta > 0f)
            {
                return Mathf.Min(bridgeDelta, maxStepDelta);
            }

            var unscaled = UnityEngine.Time.unscaledDeltaTime;
            if (float.IsNaN(unscaled) || float.IsInfinity(unscaled) || unscaled <= 0f)
            {
                unscaled = UnityEngine.Time.deltaTime;
            }

            if (float.IsNaN(unscaled) || float.IsInfinity(unscaled) || unscaled <= 0f)
            {
                unscaled = fallbackDelta;
            }

            return Mathf.Min(unscaled, maxStepDelta);
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public sealed partial class Space4XCameraRigSyncSystem : SystemBase
    {
        private Camera _camera;

        protected override void OnCreate()
        {
            base.OnCreate();
            RuntimeConfigRegistry.Initialize();
        }

        protected override void OnUpdate()
        {
            if (!CameraRigService.IsEcsCameraEnabled)
            {
                return;
            }

            if (!CameraRigService.HasState)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            var state = CameraRigService.Current;
            _camera.transform.SetPositionAndRotation(state.Position, state.Rotation);
            if (_camera.transform.localScale != Vector3.one)
            {
                _camera.transform.localScale = Vector3.one;
            }
        }
    }
}


