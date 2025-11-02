using PureDOTS.Runtime.Camera;
using Space4X.CameraComponents;
using Space4X.CameraControls;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space4X.Systems
{
    /// <summary>
    /// DOTS system that processes Space4X camera input and updates camera state.
    /// Runs in CameraInputSystemGroup (OrderFirst in SimulationSystemGroup) for highest priority processing.
    /// This ensures camera updates happen immediately after input, with absolute priority over other systems.
    /// </summary>
    [UpdateInGroup(typeof(PureDOTS.Systems.CameraInputSystemGroup))]
    [UpdateAfter(typeof(Space4XCameraInputSystem))]
    public partial struct Space4XCameraSystem : ISystem
    {
        private const float kMaxCameraStepDelta = 1f / 30f; // Clamp movement step to ~33ms to smooth over large frame hitches
        private const float kFallbackDelta = 1f / 60f;

        public void OnCreate(ref SystemState state)
        {
            // Note: Debug.Log in Burst should work, but might be suppressed
            UnityEngine.Debug.Log("[Space4XCameraSystem] System created!");
            // Don't require these singletons - check manually to allow initialization to happen first
            // state.RequireForUpdate<Space4XCameraInput>();
            // state.RequireForUpdate<Space4XCameraState>();
            // RewindState is optional - don't require it
        }

        public void OnUpdate(ref SystemState state)
        {
            if (CameraRigService.IsEcsCameraEnabled)
            {
                return;
            }

            // Reduced logging - only log on initialization (excessive logging causes performance hiccups)
            
            // Check rewind state - skip updates during rewind (if rewind system is active)
            if (SystemAPI.HasSingleton<RewindState>())
            {
                var rewindState = SystemAPI.GetSingleton<RewindState>();
                if (rewindState.Mode != RewindMode.Record)
                {
                    return;
                }
            }

            // Get input singleton
            if (!SystemAPI.TryGetSingleton<Space4XCameraInput>(out var input))
            {
                // Only log warning on first few frames or if persistent issue (reduced verbosity)
                if (UnityEngine.Time.frameCount <= 3)
                {
                    UnityEngine.Debug.LogWarning("[Space4XCameraSystem] Camera input singleton not found! Waiting for initialization...");
                }
                return;
            }
            
            // Reduced logging - only log on initialization or errors (excessive logging causes performance hiccups)
            // Use UnityEngine.Debug explicitly for Burst compatibility

            // Get camera state singleton (must be created by initialization system)
            if (!SystemAPI.TryGetSingletonEntity<Space4XCameraState>(out var cameraStateEntity))
            {
                if (UnityEngine.Time.frameCount % 60 == 0)
                {
                    UnityEngine.Debug.LogWarning("[Space4XCameraSystem] Camera state singleton not found! Waiting for initialization...");
                }
                return; // Camera state not initialized yet
            }
            
            if (Space4XCameraMouseController.TryGetLatestState(out var latestState))
            {
                if (SystemAPI.TryGetSingletonRW<Space4XCameraDiagnostics>(out var monoDiagnostics))
                {
                    ref var diag = ref monoDiagnostics.ValueRW;
                    var currentFrame = (uint)UnityEngine.Time.frameCount;
                    if (diag.FrameId != currentFrame)
                    {
                        diag.FrameId = currentFrame;
                        diag.TicksThisFrame = 0;
                        diag.CatchUpTicks = 0;
                        diag.InputStaleTicks = 0;
                    }

                    diag.TicksThisFrame++;
                    diag.MonoControllerActive = true;
                    diag.MovementHandledExternally = true;
                    diag.RotationHandledExternally = true;
                    diag.PendingRotateBudget = float2.zero;
                    diag.PendingZoomBudget = 0f;
                    diag.BudgetTicksRemaining = 0;
                }

                SystemAPI.SetComponent(cameraStateEntity, latestState);

                if (SystemAPI.TryGetSingletonEntity<Space4XCameraInputFlags>(out var flagsEntity))
                {
                    var flags = new Space4XCameraInputFlags
                    {
                        MovementHandled = true,
                        RotationHandled = true
                    };
                    SystemAPI.SetComponent(flagsEntity, flags);
                }

                return;
            }

            var cameraState = SystemAPI.GetComponent<Space4XCameraState>(cameraStateEntity);
            
            var oldPosition = cameraState.Position;

            // Get config or use defaults
            Space4XCameraConfig config = SystemAPI.TryGetSingleton<Space4XCameraConfig>(out var existingConfig)
                ? existingConfig
                : GetDefaultConfig();

            var deltaTime = SystemAPI.Time.DeltaTime;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
            {
                deltaTime = kFallbackDelta;
            }

            var bridgeDelta = Space4XCameraInputBridge.LastSampleDeltaTime;
            if (!float.IsNaN(bridgeDelta) && !float.IsInfinity(bridgeDelta) && bridgeDelta > 0f)
            {
                deltaTime = math.min(deltaTime, bridgeDelta);
            }

            deltaTime = math.min(deltaTime, kMaxCameraStepDelta);
            if (deltaTime <= 0f)
            {
                deltaTime = kFallbackDelta;
            }

            var movementHandledExternally = false;
            var rotationHandledExternally = false;
            if (SystemAPI.TryGetSingleton<Space4XCameraInputFlags>(out var inputFlags))
            {
                movementHandledExternally = inputFlags.MovementHandled;
                rotationHandledExternally = inputFlags.RotationHandled;
            }

            RefRW<Space4XCameraDiagnostics> diagnosticsHandle;
            var hasDiagnostics = SystemAPI.TryGetSingletonRW<Space4XCameraDiagnostics>(out diagnosticsHandle);
            if (hasDiagnostics)
            {
                ref var diagnostics = ref diagnosticsHandle.ValueRW;
                var currentFrame = (uint)UnityEngine.Time.frameCount;
                if (diagnostics.FrameId != currentFrame)
                {
                    diagnostics.FrameId = currentFrame;
                    diagnostics.TicksThisFrame = 0;
                    diagnostics.CatchUpTicks = 0;
                    diagnostics.InputStaleTicks = 0;
                }

                diagnostics.TicksThisFrame++;
                if (diagnostics.TicksThisFrame > 1)
                {
                    diagnostics.CatchUpTicks++;
                }

                diagnostics.MonoControllerActive = false;
            }

            float2 rotateBudgetSlice = float2.zero;
            float zoomBudgetSlice = 0f;
            RefRW<CameraInputBudget> budgetHandle;
            var hasBudget = SystemAPI.TryGetSingletonRW<CameraInputBudget>(out budgetHandle);
            if (hasBudget)
            {
                ref var budget = ref budgetHandle.ValueRW;
                var ticksToSpend = math.max(1, budget.TicksToSpend);
                rotateBudgetSlice = budget.RotateBudget / ticksToSpend;
                zoomBudgetSlice = budget.ZoomBudget / ticksToSpend;

                budget.RotateBudget -= rotateBudgetSlice;
                budget.ZoomBudget -= zoomBudgetSlice;
                budget.TicksToSpend = math.max(0, budget.TicksToSpend - 1);

                if (math.lengthsq(budget.RotateBudget) < 1e-6f)
                {
                    budget.RotateBudget = float2.zero;
                }

                if (math.abs(budget.ZoomBudget) < 1e-6f)
                {
                    budget.ZoomBudget = 0f;
                }
            }

            // Handle perspective mode toggle
            if (input.TogglePerspectiveMode)
            {
                cameraState.PerspectiveMode = !cameraState.PerspectiveMode;
                // Note: Perspective mode toggle - logging removed to prevent performance impact
            }

            // Apply pan (either RTS-style or Perspective-style based on mode)
            // Use very small threshold - input system already filters noise, so trust small values for smooth movement
            if (!movementHandledExternally && config.EnablePan && math.lengthsq(input.PanInput) > 0.00000001f) // 0.0001^2 = 0.00000001 - allows smooth, real-time input
            {
                float3 moveDir;
                
                if (cameraState.PerspectiveMode)
                {
                    // Perspective mode: WASD moves relative to camera orientation (like FPS camera)
                    // Use full camera rotation (pitch + yaw) to get forward, right, and up vectors
                    var fullRotation = quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0f);
                    var forward = math.mul(fullRotation, new float3(0f, 0f, 1f));
                    var right = math.mul(fullRotation, new float3(1f, 0f, 0f));
                    var up = math.mul(fullRotation, new float3(0f, 1f, 0f));
                    
                    // WASD: W=forward, S=backward, A=left, D=right (relative to camera)
                    // Vertical pan (Q/E) also moves relative to camera's up when in perspective mode
                    moveDir = (right * input.PanInput.x + forward * input.PanInput.y) * config.PanSpeed * deltaTime;
                }
                else
                {
                    // RTS mode: horizontal movement relative to camera rotation (original behavior)
                    // Calculate forward and right vectors on horizontal plane (ignoring pitch)
                    var yawRotation = quaternion.Euler(0f, cameraState.Yaw, 0f);
                    var forward = math.mul(yawRotation, new float3(0f, 0f, 1f));
                    var right = math.mul(yawRotation, new float3(1f, 0f, 0f));
                    
                    // Pan is relative to camera's yaw, but always horizontal
                    moveDir = (right * input.PanInput.x + forward * input.PanInput.y) * config.PanSpeed * deltaTime;
                }
                
                cameraState.Position += moveDir;

                // Enforce pan bounds if enabled
                if (config.UsePanBounds)
                {
                    cameraState.Position = math.clamp(cameraState.Position, config.PanBoundsMin, config.PanBoundsMax);
                }
            }

            // Apply vertical pan (Q/E up/down movement)
            // Use very small threshold - input system already filters noise, so trust small values for smooth movement
            if (!movementHandledExternally && math.abs(input.VerticalPanInput) > 0.0001f) // Minimal threshold for smooth, real-time input
            {
                if (!config.EnablePan)
                {
                    // Note: VerticalPan input received but EnablePan is false - logging removed to prevent performance impact
                }
                else
                {
                    var verticalSpeed = config.VerticalPanSpeed > 0f ? config.VerticalPanSpeed : config.PanSpeed;
                    float3 verticalMove;
                    
                    if (cameraState.PerspectiveMode)
                    {
                        // In perspective mode, Q/E moves relative to camera's up vector
                        var fullRotation = quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0f);
                        var up = math.mul(fullRotation, new float3(0f, 1f, 0f));
                        verticalMove = up * input.VerticalPanInput * verticalSpeed * deltaTime;
                    }
                    else
                    {
                        // In RTS mode, Q/E moves in world space (Y axis)
                        verticalMove = new float3(0f, input.VerticalPanInput * verticalSpeed * deltaTime, 0f);
                    }
                    
                    cameraState.Position += verticalMove;

                    // Enforce pan bounds if enabled
                    if (config.UsePanBounds)
                    {
                        cameraState.Position = math.clamp(cameraState.Position, config.PanBoundsMin, config.PanBoundsMax);
                    }
                }
            }

            // Apply zoom
            // Use very small threshold - input system already filters noise, so trust small values for smooth zoom
            var zoomSignal = input.ZoomInput + zoomBudgetSlice;
            if (movementHandledExternally && hasBudget)
            {
                ref var budget = ref budgetHandle.ValueRW;
                budget.ZoomBudget = 0f;
                budget.TicksToSpend = 0;
                zoomSignal = 0f;
            }

            if (!movementHandledExternally && config.EnableZoom && math.abs(zoomSignal) > 0.0001f) // Minimal threshold for smooth, real-time zoom
            {
                // For RTS camera, zoom moves forward/back along view direction
                var forward = math.forward(quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0f));
                var zoomDelta = forward * zoomSignal * config.ZoomSpeed * deltaTime;
                var newPosition = cameraState.Position + zoomDelta;
                
                // Enforce zoom distance limits (simplified - distance from origin)
                var distance = math.length(newPosition);
                if (distance >= config.ZoomMinDistance && distance <= config.ZoomMaxDistance)
                {
                    cameraState.Position = newPosition;
                }
            }

            // Apply rotation if input is present (MonoBehaviour controller zeros RotateInput after consuming)
            // Use very small threshold to allow smooth rotation even with tiny movements
            // The input system already filters noise, so we can trust small values here
            var rotateSignal = input.RotateInput + rotateBudgetSlice;
            if (rotationHandledExternally && hasBudget)
            {
                ref var budget = ref budgetHandle.ValueRW;
                budget.RotateBudget = float2.zero;
                budget.TicksToSpend = 0;
                rotateSignal = float2.zero;
            }

            if (!rotationHandledExternally && config.EnableRotation && math.lengthsq(rotateSignal) > 0.00000001f) // 0.0001^2 = 0.00000001 - very small to allow smooth rotation
            {
                // Rotation speed is expressed as degrees per pixel of mouse movement.
                // Apply rotation directly without additional filtering for smooth, real-time response
                var rotationDelta = rotateSignal * config.RotationSpeed;
                cameraState.Yaw += math.radians(rotationDelta.x);
                cameraState.Pitch -= math.radians(rotationDelta.y); // Invert Y for intuitive mouse control
                
                // Clamp pitch
                cameraState.Pitch = math.clamp(cameraState.Pitch, config.PitchMin, config.PitchMax);
            }

            if (hasDiagnostics)
            {
                ref var diagnostics = ref diagnosticsHandle.ValueRW;
                if (hasBudget)
                {
                    var currentBudgetFrame = budgetHandle.ValueRW.BridgeFrameId;
                    if (currentBudgetFrame != 0u)
                    {
                        if (diagnostics.LastBudgetFrameId == currentBudgetFrame)
                        {
                            diagnostics.InputStaleTicks++;
                        }
                        else
                        {
                            diagnostics.LastBudgetFrameId = currentBudgetFrame;
                        }
                    }

                    diagnostics.PendingRotateBudget = budgetHandle.ValueRW.RotateBudget;
                    diagnostics.PendingZoomBudget = budgetHandle.ValueRW.ZoomBudget;
                    diagnostics.BudgetTicksRemaining = budgetHandle.ValueRW.TicksToSpend;
                }
                else
                {
                    diagnostics.PendingRotateBudget = float2.zero;
                    diagnostics.PendingZoomBudget = 0f;
                    diagnostics.BudgetTicksRemaining = 0;
                }

                diagnostics.MovementHandledExternally = movementHandledExternally;
                diagnostics.RotationHandledExternally = rotationHandledExternally;
            }

            // Handle reset
            if (input.ResetRequested)
            {
                cameraState.Position = new float3(0f, 25f, -30f);
                cameraState.Pitch = math.radians(40f);
                cameraState.Yaw = 0f;
            }

            // Update rotation quaternion
            cameraState.Rotation = quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0f);

            // Write updated state back (ALWAYS write, even if nothing changed, to ensure it's synced)
            SystemAPI.SetComponent(cameraStateEntity, cameraState);

            // Reduced logging - only log on initialization (excessive logging causes performance hiccups)
            if (UnityEngine.Time.frameCount <= 3)
            {
                UnityEngine.Debug.Log($"[Space4XCameraSystem] Initialized - Initial state: Pos={cameraState.Position}, Pitch={math.degrees(cameraState.Pitch)}, Yaw={math.degrees(cameraState.Yaw)}");
            }
        }

        private static Space4XCameraConfig GetDefaultConfig()
        {
            return new Space4XCameraConfig
            {
                PanSpeed = 10f,
                VerticalPanSpeed = 10f, // Same speed as horizontal pan by default
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
                EnableRotation = true // Enable rotation for RMB drag pitch/yaw
            };
        }
    }
}

