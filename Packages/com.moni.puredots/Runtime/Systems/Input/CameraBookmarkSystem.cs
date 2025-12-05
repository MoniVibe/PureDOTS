using PureDOTS.Input;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Processes camera focus events (double-click LMB) and moves camera to target position.
    /// Uses frame-time for smooth camera movement (presentation code).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SelectionSystem))]
    public partial struct CameraBookmarkSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RtsInputSingletonTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity rtsInputEntity = SystemAPI.GetSingletonEntity<RtsInputSingletonTag>();

            if (!state.EntityManager.HasBuffer<CameraFocusEvent>(rtsInputEntity))
            {
                return;
            }

            var focusBuffer = state.EntityManager.GetBuffer<CameraFocusEvent>(rtsInputEntity);

            Camera camera = Camera.main;
            if (camera == null)
            {
                focusBuffer.Clear();
                return;
            }

            // Process focus events (typically only one per frame)
            for (int i = 0; i < focusBuffer.Length; i++)
            {
                var focusEvent = focusBuffer[i];
                Vector3 targetPos = new Vector3(focusEvent.WorldPosition.x, focusEvent.WorldPosition.y, focusEvent.WorldPosition.z);

                // Move camera to target (frame-time lerp for smooth movement)
                // Note: This is presentation code, so frame-time is appropriate
                camera.transform.position = Vector3.Lerp(camera.transform.position, targetPos, UnityEngine.Time.deltaTime * 5f);
            }

            // Clear buffer after processing
            focusBuffer.Clear();
        }
    }
}

