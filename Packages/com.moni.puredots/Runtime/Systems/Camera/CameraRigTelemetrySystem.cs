using PureDOTS.Runtime.Camera;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Camera
{
    /// <summary>
    /// Copies the latest CameraState into a telemetry singleton for HUD/analytics consumption.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
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
