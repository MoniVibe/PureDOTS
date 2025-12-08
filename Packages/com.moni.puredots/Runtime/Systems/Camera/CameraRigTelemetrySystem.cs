#if PUREDOTS_LEGACY_CAMERA
using PureDOTS.Runtime.Camera;
using PureDOTS.Runtime.Telemetry;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Camera
{
    internal static class CameraRigTelemetryKeys
    {
        public static readonly FixedString64Bytes Distance = "camera.distance";
        public static readonly FixedString64Bytes Pitch = "camera.pitch";
        public static readonly FixedString64Bytes Yaw = "camera.yaw";
        public static readonly FixedString64Bytes PlayerId = "camera.player_id";
    }

    /// <summary>
    /// Copies the latest CameraState into a telemetry singleton for HUD/analytics consumption.
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    [UpdateBefore(typeof(PureDOTS.Systems.DebugDisplaySystem))]
    public partial struct CameraRigTelemetrySystem : ISystem
    {
        private EntityQuery _cameraQuery;
        private EntityQuery _telemetryQuery;

        public void OnCreate(ref SystemState state)
        {
            _cameraQuery = state.GetEntityQuery(ComponentType.ReadOnly<CameraState>());
            _telemetryQuery = state.GetEntityQuery(ComponentType.ReadOnly<CameraRigTelemetry>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_cameraQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var telemetryEntity = EnsureTelemetryEntity(ref state);

            foreach (var camera in SystemAPI.Query<RefRO<CameraState>>())
            {
                var cam = camera.ValueRO;
                state.EntityManager.SetComponentData(telemetryEntity, new CameraRigTelemetry
                {
                    PlayerId = cam.PlayerId,
                    LastTick = cam.LastUpdateTick,
                    Position = cam.TargetPosition,
                    Forward = cam.TargetForward,
                    Up = cam.TargetUp,
                    Distance = cam.Distance,
                    Pitch = cam.Pitch,
                    Yaw = cam.Yaw,
                    Shake = 0f
                });

                // Emit telemetry metrics
                TelemetryHub.Enqueue(new TelemetryMetric { Key = CameraRigTelemetryKeys.Distance, Value = cam.Distance, Unit = TelemetryMetricUnit.Count });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = CameraRigTelemetryKeys.Pitch, Value = cam.Pitch, Unit = TelemetryMetricUnit.None });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = CameraRigTelemetryKeys.Yaw, Value = cam.Yaw, Unit = TelemetryMetricUnit.None });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = CameraRigTelemetryKeys.PlayerId, Value = cam.PlayerId, Unit = TelemetryMetricUnit.Count });
                break;
            }
        }

        private Entity EnsureTelemetryEntity(ref SystemState state)
        {
            if (!_telemetryQuery.IsEmptyIgnoreFilter)
            {
                return _telemetryQuery.GetSingletonEntity();
            }

            return state.EntityManager.CreateEntity(typeof(CameraRigTelemetry));
        }
    }
}
#endif
