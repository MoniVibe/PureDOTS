using PureDOTS.Runtime.Camera;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Camera
{
    /// <summary>
    /// Presentation bridge MonoBehaviour that reads CameraState from ECS
    /// and applies visual-only smoothing to the GameObject camera.
    /// This is a reference implementation - game-specific bridges can extend this pattern.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraPresentationBridge : MonoBehaviour
    {
        [Header("Smoothing")]
        [SerializeField] float positionSmoothing = 0.1f;
        [SerializeField] float rotationSmoothing = 0.1f;

        [Header("Configuration")]
        [SerializeField] byte playerId = 0; // Default player

        private World _world;
        private EntityQuery _cameraQuery;
        private UnityEngine.Camera _targetCamera;
        private bool _queryValid;

        private Vector3 _smoothedPosition;
        private Quaternion _smoothedRotation;

        void Awake()
        {
            _targetCamera = GetComponent<UnityEngine.Camera>();
            EnsureWorld();
        }

        void LateUpdate()
        {
            EnsureWorld();
            if (_world == null || !_world.IsCreated || !_queryValid || _cameraQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            Entity cameraEntity;
            try
            {
                // Find camera entity matching PlayerId
                using (var entities = _cameraQuery.ToEntityArray(Allocator.Temp))
                {
                    cameraEntity = Entity.Null;
                    var entityManager = _world.EntityManager;
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (entityManager.HasComponent<CameraState>(entities[i]))
                        {
                            var state = entityManager.GetComponentData<CameraState>(entities[i]);
                            if (state.PlayerId == playerId)
                            {
                                cameraEntity = entities[i];
                                break;
                            }
                        }
                    }
                }

                if (cameraEntity == Entity.Null)
                {
                    return;
                }

                var cameraState = _world.EntityManager.GetComponentData<CameraState>(cameraEntity);

                // Read authoritative state from ECS
                Vector3 targetPos = new Vector3(
                    cameraState.TargetPosition.x,
                    cameraState.TargetPosition.y,
                    cameraState.TargetPosition.z);
                Vector3 targetForward = new Vector3(
                    cameraState.TargetForward.x,
                    cameraState.TargetForward.y,
                    cameraState.TargetForward.z);
                Vector3 targetUp = new Vector3(
                    cameraState.TargetUp.x,
                    cameraState.TargetUp.y,
                    cameraState.TargetUp.z);

                // Apply visual-only smoothing (presentation layer, not gameplay)
                float deltaTime = UnityEngine.Time.deltaTime;
                _smoothedPosition = Vector3.Lerp(_smoothedPosition, targetPos, deltaTime / positionSmoothing);
                Quaternion targetRotation = Quaternion.LookRotation(targetForward, targetUp);
                _smoothedRotation = Quaternion.Slerp(_smoothedRotation, targetRotation, deltaTime / rotationSmoothing);

                // Update GameObject camera
                _targetCamera.transform.position = _smoothedPosition;
                _targetCamera.transform.rotation = _smoothedRotation;
                _targetCamera.fieldOfView = cameraState.FOV;
            }
            catch
            {
                // Query may be invalid during world teardown
                _queryValid = false;
            }
        }

        private void EnsureWorld()
        {
            if (_world != null && _world.IsCreated)
            {
                return;
            }

            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null || !_world.IsCreated)
            {
                _queryValid = false;
                return;
            }

            var entityManager = _world.EntityManager;
            _cameraQuery = entityManager.CreateEntityQuery(typeof(CameraState), typeof(CameraTag));
            _queryValid = true;
        }

        void OnDestroy()
        {
            if (_cameraQuery != default)
            {
                _cameraQuery.Dispose();
            }
        }
    }
}






















