using Godgame.Camera;
using Godgame.Interaction;
using Godgame.Interaction.Input;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Camera;

namespace Godgame.Camera
{
    /// <summary>
    /// Main camera control system implementing RTS/Free-fly and BW2-style Orbital modes.
    /// Follows PureDOTS patterns with deterministic, Burst-compatible logic where possible.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputReaderSystem))]
    public partial struct CameraControlSystem : ISystem
    {
        private bool _singletonsInitialized;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputState>();
            
            // Create singleton entities in OnCreate (managed context, not Burst)
            InitializeSingletons(ref state);
        }

        private void InitializeSingletons(ref SystemState state)
        {
            if (_singletonsInitialized)
            {
                return;
            }

            // Ensure all singleton components exist - create if missing
            // CameraSettings singleton
            if (!SystemAPI.TryGetSingletonEntity<CameraSettings>(out _))
            {
                var settingsEntity = state.EntityManager.CreateEntity(typeof(CameraSettings));
                state.EntityManager.SetComponentData(settingsEntity, CameraSettings.Default);
            }

            // CameraModeState singleton
            if (!SystemAPI.TryGetSingletonEntity<CameraModeState>(out _))
            {
                var modeEntity = state.EntityManager.CreateEntity(typeof(CameraModeState));
                state.EntityManager.SetComponentData(modeEntity, new CameraModeState
                {
                    Mode = CameraMode.RTSFreeFly,
                    JustToggled = false
                });
            }

            // CameraTransform singleton
            if (!SystemAPI.TryGetSingletonEntity<CameraTransform>(out _))
            {
                var transformEntity = state.EntityManager.CreateEntity(typeof(CameraTransform));
                
                // Try to initialize from camera entity's LocalTransform if it exists
                var initialTransform = new CameraTransform
                {
                    Position = new float3(0f, 10f, -10f),
                    Rotation = quaternion.LookRotationSafe(new float3(0f, 0f, 1f), new float3(0f, 1f, 0f)),
                    DistanceFromPivot = 10f,
                    PitchAngle = 45f
                };
                
                using var cameraEntityQuery = state.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<CameraTag>(),
                    ComponentType.ReadOnly<LocalTransform>());
                
                if (!cameraEntityQuery.IsEmptyIgnoreFilter)
                {
                    var cameraEntity = cameraEntityQuery.GetSingletonEntity();
                    var localTransform = state.EntityManager.GetComponentData<LocalTransform>(cameraEntity);
                    initialTransform.Position = localTransform.Position;
                    initialTransform.Rotation = localTransform.Rotation;
                    initialTransform.DistanceFromPivot = math.length(localTransform.Position);
                    // Calculate pitch from rotation
                    var forward = math.mul(localTransform.Rotation, new float3(0f, 0f, 1f));
                    initialTransform.PitchAngle = math.degrees(math.asin(math.clamp(forward.y, -1f, 1f)));
                }
                
                state.EntityManager.SetComponentData(transformEntity, initialTransform);
            }

            // CameraTerrainState singleton
            if (!SystemAPI.TryGetSingletonEntity<CameraTerrainState>(out _))
            {
                var terrainEntity = state.EntityManager.CreateEntity(typeof(CameraTerrainState));
                state.EntityManager.SetComponentData(terrainEntity, default(CameraTerrainState));
            }

            _singletonsInitialized = true;
        }

        public void OnUpdate(ref SystemState state)
        {
            // Early-out during rewind (per PureDOTS requirements)
            // Note: RewindState check would go here if PureDOTS provides it

            // Ensure singletons are initialized (managed context)
            if (!_singletonsInitialized)
            {
                InitializeSingletons(ref state);
                if (!_singletonsInitialized)
                {
                    return; // Still failed to initialize
                }
            }

            if (GodgameCameraController.TryGetCurrentState(out var monoState))
            {
                SyncFromMonoState(monoState);
                return;
            }
 
            // Get input state
            if (!SystemAPI.TryGetSingleton<InputState>(out var inputState))
            {
                return;
            }

            // Get singleton components using SystemAPI (handles entity lookup automatically)
            if (!SystemAPI.TryGetSingletonRW<CameraModeState>(out var cameraModeRW) ||
                !SystemAPI.TryGetSingletonRW<CameraSettings>(out var cameraSettingsRW) ||
                !SystemAPI.TryGetSingletonRW<CameraTransform>(out var cameraTransformRW) ||
                !SystemAPI.TryGetSingletonRW<CameraTerrainState>(out var cameraTerrainRW))
            {
                // Singletons don't exist yet - reinitialize
                _singletonsInitialized = false;
                InitializeSingletons(ref state);
                if (!_singletonsInitialized)
                {
                    return;
                }
                // Try again after initialization
                if (!SystemAPI.TryGetSingletonRW<CameraModeState>(out cameraModeRW) ||
                    !SystemAPI.TryGetSingletonRW<CameraSettings>(out cameraSettingsRW) ||
                    !SystemAPI.TryGetSingletonRW<CameraTransform>(out cameraTransformRW) ||
                    !SystemAPI.TryGetSingletonRW<CameraTerrainState>(out cameraTerrainRW))
                {
                    return; // Still failed
                }
            }

            ref var cameraMode = ref cameraModeRW.ValueRW;
            ref var settings = ref cameraSettingsRW.ValueRW;
            ref var transform = ref cameraTransformRW.ValueRW;
            ref var terrainState = ref cameraTerrainRW.ValueRW;

            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            // Handle mode toggle (with debouncing)
            if (inputState.CameraToggleMode && !cameraMode.JustToggled)
            {
                cameraMode.Mode = cameraMode.Mode == CameraMode.RTSFreeFly 
                    ? CameraMode.Orbital 
                    : CameraMode.RTSFreeFly;
                cameraMode.JustToggled = true;
            }
            else
            {
                cameraMode.JustToggled = false;
            }

            // Update camera based on current mode
            if (cameraMode.Mode == CameraMode.RTSFreeFly)
            {
                UpdateRTSFreeFlyMode(ref transform, ref settings, inputState, deltaTime);
            }
            else // Orbital mode
            {
                UpdateOrbitalMode(ref transform, ref settings, ref terrainState, inputState, deltaTime);
            }
        }

        private void UpdateRTSFreeFlyMode(
            ref CameraTransform transform,
            ref CameraSettings settings,
            InputState input,
            float deltaTime)
        {
            // Get camera forward/right/up vectors from rotation
            var forward = math.mul(transform.Rotation, new float3(0f, 0f, 1f));
            var right = math.mul(transform.Rotation, new float3(1f, 0f, 0f));
            var up = new float3(0f, 1f, 0f);

            // WASD movement (relative to camera rotation)
            var moveInput = input.Move;
            var horizontalMove = right * moveInput.x + forward * moveInput.y;
            transform.Position += horizontalMove * settings.MovementSpeed * deltaTime;

            // Q/E vertical movement (world space)
            transform.Position += up * input.Vertical * settings.MovementSpeed * deltaTime;

            // Mouse look rotation
            var lookDelta = input.PointerDelta;
            if (math.lengthsq(lookDelta) > 0.001f)
            {
                var rotationDelta = new float3(
                    -lookDelta.y * settings.RotationSensitivity * deltaTime,
                    lookDelta.x * settings.RotationSensitivity * deltaTime,
                    0f
                );

                var currentEuler = math.EulerXYZ(transform.Rotation);
                var newEuler = currentEuler + rotationDelta;
                
                // Clamp pitch to prevent flipping
                newEuler.x = math.clamp(newEuler.x, math.radians(-89f), math.radians(89f));
                
                transform.Rotation = quaternion.EulerXYZ(newEuler);
            }

            // Scroll wheel zoom (move forward/back in view direction)
            if (math.abs(input.Scroll) > 0.001f)
            {
                var zoomAmount = input.Scroll * settings.ZoomSpeed * deltaTime;
                transform.Position += forward * zoomAmount;
            }
        }

        private void UpdateOrbitalMode(
            ref CameraTransform transform,
            ref CameraSettings settings,
            ref CameraTerrainState terrainState,
            InputState input,
            float deltaTime)
        {
            var distance = transform.DistanceFromPivot;

            if (math.abs(input.Scroll) > 0.001f)
            {
                distance = math.clamp(distance - input.Scroll * settings.ZoomSpeed * deltaTime,
                    settings.ZoomMin, settings.ZoomMax);
            }

            var sensitivity = GetDistanceScaledSensitivity(distance, settings);

            var rotationEuler = math.degrees(math.EulerXYZ(transform.Rotation));

            if (input.MiddleHeld && math.lengthsq(input.PointerDelta) > 0.0001f)
            {
                rotationEuler.y += input.PointerDelta.x * settings.OrbitalRotationSpeed * sensitivity * deltaTime;
                rotationEuler.x -= input.PointerDelta.y * settings.OrbitalRotationSpeed * sensitivity * deltaTime;
                rotationEuler.x = math.clamp(rotationEuler.x, settings.PitchMin, settings.PitchMax);
            }

            transform.PitchAngle = rotationEuler.x;
            transform.DistanceFromPivot = distance;

            var rotation = quaternion.EulerXYZ(math.radians(rotationEuler));
            var offset = math.mul(rotation, new float3(0f, 0f, -distance));
            var focus = settings.OrbitalFocusPoint;

            transform.Position = focus - offset;
            transform.Rotation = quaternion.LookRotationSafe(focus - transform.Position, math.up());

            terrainState.GrabPlaneNormal = math.up();
            terrainState.GrabPlanePosition = focus;
        }

        private void SyncFromMonoState(CameraRigState monoState)
        {
            if (SystemAPI.TryGetSingletonRW<CameraTransform>(out var transformRW))
            {
                ref var transform = ref transformRW.ValueRW;
                transform.Position = new float3(monoState.Position.x, monoState.Position.y, monoState.Position.z);
                transform.Rotation = new quaternion(monoState.Rotation.x, monoState.Rotation.y, monoState.Rotation.z, monoState.Rotation.w);
                transform.DistanceFromPivot = monoState.Distance;
                transform.PitchAngle = math.degrees(monoState.Pitch);
            }

            if (SystemAPI.TryGetSingletonRW<CameraModeState>(out var modeRW))
            {
                ref var mode = ref modeRW.ValueRW;
                mode.Mode = monoState.PerspectiveMode ? CameraMode.RTSFreeFly : CameraMode.Orbital;
                mode.JustToggled = false;
            }
        }
 
        private float GetDistanceScaledSensitivity(float distance, CameraSettings settings)
        {
            if (distance <= 20f)
            {
                return settings.SensitivityClose;
            }
            else if (distance <= 100f)
            {
                return settings.SensitivityMid;
            }
            else
            {
                return settings.SensitivityFar;
            }
        }

    }
}

