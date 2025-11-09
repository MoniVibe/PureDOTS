using PureDOTS.Runtime.Camera;
using PureDOTS.Runtime.Hand;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Camera
{
    /// <summary>
    /// Single-writer system for CameraState.
    /// Consumes GodIntent and computes deterministic camera transforms.
    /// Note: Runs in CameraInputSystemGroup, which executes after SimulationSystemGroup where IntentMappingSystem runs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CameraInputSystemGroup))]
    public partial struct CameraSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (cameraState, cameraConfig, intentRO, cameraEntity) in SystemAPI
                .Query<RefRW<CameraState>, RefRO<CameraConfig>, RefRO<GodIntent>>()
                .WithEntityAccess())
            {
                var stateData = cameraState.ValueRW;
                var config = cameraConfig.ValueRO;
                var intent = intentRO.ValueRO;

                stateData.LastUpdateTick = currentTick;
                stateData.PlayerId = intent.PlayerId;

                // Handle orbit (MMB)
                if (intent.StartOrbit != 0)
                {
                    stateData.IsOrbiting = 1;
                    // Establish pivot from current cursor position (simplified - should raycast)
                    stateData.PivotPosition = stateData.TargetPosition; // Use current position as pivot
                }
                else if (intent.StopOrbit != 0)
                {
                    stateData.IsOrbiting = 0;
                }

                if (stateData.IsOrbiting != 0)
                {
                    // Apply orbit delta
                    float sensitivity = ComputeOrbitSensitivity(config, stateData.Distance);
                    stateData.Yaw += intent.OrbitIntent.x * sensitivity * config.OrbitYawSensitivity;
                    stateData.Pitch = math.clamp(
                        stateData.Pitch + intent.OrbitIntent.y * sensitivity * config.OrbitPitchSensitivity,
                        config.PitchClamp.x,
                        config.PitchClamp.y);

                    // Compute camera position relative to pivot
                    float3 direction = ComputeDirectionFromEuler(stateData.Pitch, stateData.Yaw);
                    stateData.TargetPosition = stateData.PivotPosition - direction * stateData.Distance;
                    stateData.TargetForward = direction;
                    stateData.TargetUp = ComputeUpFromEuler(stateData.Pitch, stateData.Yaw);
                }

                // Handle pan (LMB)
                if (intent.StartPan != 0)
                {
                    stateData.IsPanning = 1;
                    stateData.PanPlaneHeight = stateData.TargetPosition.y;
                }
                else if (intent.StopPan != 0)
                {
                    stateData.IsPanning = 0;
                }

                if (stateData.IsPanning != 0)
                {
                    // Pan camera based on pan delta (world-space)
                    float3 panVector = new float3(intent.PanIntent.x, 0f, intent.PanIntent.y) * config.PanScale;
                    stateData.TargetPosition += panVector;
                    stateData.PivotPosition += panVector; // Also move pivot if orbiting
                }

                // Free-move input (WASD + Z/X)
                float2 freeMove = intent.FreeMoveIntent;
                if (math.lengthsq(freeMove) > 0.0001f)
                {
                    float3 forward = math.normalizesafe(stateData.TargetForward, new float3(0f, 0f, 1f));
                    float3 up = math.normalizesafe(stateData.TargetUp, new float3(0f, 1f, 0f));
                    float3 right = math.normalizesafe(math.cross(up, forward), new float3(1f, 0f, 0f));

                    float3 moveVector;
                    if (intent.CameraYAxisUnlocked != 0)
                    {
                        moveVector = (forward * freeMove.y + right * freeMove.x) * config.PanScale;
                    }
                    else
                    {
                        float3 forwardFlat = math.normalizesafe(new float3(forward.x, 0f, forward.z), new float3(0f, 0f, 1f));
                        float3 rightFlat = math.normalizesafe(new float3(forwardFlat.z, 0f, -forwardFlat.x), new float3(1f, 0f, 0f));
                        moveVector = (forwardFlat * freeMove.y + rightFlat * freeMove.x) * config.PanScale;
                    }

                    stateData.TargetPosition += moveVector;
                    stateData.PivotPosition += moveVector;
                }

                if (math.abs(intent.VerticalMoveIntent) > 0.001f)
                {
                    float verticalDelta = intent.VerticalMoveIntent * config.PanScale;
                    float3 verticalDir = intent.CameraYAxisUnlocked != 0
                        ? math.normalizesafe(stateData.TargetUp, new float3(0f, 1f, 0f))
                        : new float3(0f, 1f, 0f);
                    stateData.TargetPosition += verticalDir * verticalDelta;
                    stateData.PivotPosition += verticalDir * verticalDelta;
                }

                // Handle zoom (scroll)
                if (math.abs(intent.ZoomIntent) > 0.001f)
                {
                    float zoomDelta = intent.ZoomIntent * config.ZoomSpeed;
                    stateData.Distance = math.clamp(
                        stateData.Distance - zoomDelta, // Negative zoom = closer
                        config.MinDistance,
                        config.MaxDistance);

                    // Update position if orbiting
                    if (stateData.IsOrbiting != 0)
                    {
                        float3 direction = ComputeDirectionFromEuler(stateData.Pitch, stateData.Yaw);
                        stateData.TargetPosition = stateData.PivotPosition - direction * stateData.Distance;
                    }
                }

                // Apply optional smoothing (gameplay-consistent, not visual-only)
                if (config.SmoothingDamping > 0f)
                {
                    // This is deterministic smoothing for gameplay consistency
                    // Visual smoothing happens in presentation bridge
                    float smoothingFactor = 1f - math.clamp(deltaTime / config.SmoothingDamping, 0f, 1f);
                    // Apply smoothing to position if needed (simplified)
                }

                // Ensure terrain clearance
                if (config.TerrainClearance > 0f)
                {
                    stateData.TargetPosition.y = math.max(
                        stateData.TargetPosition.y,
                        stateData.PivotPosition.y + config.TerrainClearance);
                }

                cameraState.ValueRW = stateData;
            }
        }

        private static float ComputeOrbitSensitivity(CameraConfig config, float distance)
        {
            if (distance < 20f)
            {
                return config.CloseOrbitSensitivity;
            }
            else if (distance > 100f)
            {
                return config.FarOrbitSensitivity;
            }
            else
            {
                // Interpolate between close and far
                float t = (distance - 20f) / 80f;
                return math.lerp(config.CloseOrbitSensitivity, config.FarOrbitSensitivity, t);
            }
        }

        private static float3 ComputeDirectionFromEuler(float pitchDegrees, float yawDegrees)
        {
            float pitchRad = math.radians(pitchDegrees);
            float yawRad = math.radians(yawDegrees);
            float cosPitch = math.cos(pitchRad);
            return new float3(
                math.sin(yawRad) * cosPitch,
                math.sin(pitchRad),
                math.cos(yawRad) * cosPitch);
        }

        private static float3 ComputeUpFromEuler(float pitchDegrees, float yawDegrees)
        {
            float pitchRad = math.radians(pitchDegrees);
            float yawRad = math.radians(yawDegrees);
            return new float3(
                -math.sin(yawRad) * math.sin(pitchRad),
                math.cos(pitchRad),
                -math.cos(yawRad) * math.sin(pitchRad));
        }
    }
}
