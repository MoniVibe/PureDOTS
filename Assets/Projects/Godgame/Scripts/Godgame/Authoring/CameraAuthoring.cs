using Godgame.Camera;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Godgame.Authoring
{
    /// <summary>
    /// Pure DOTS authoring component for camera setup.
    /// Bakes a camera entity with CameraTag, LocalTransform, CameraSettings, and initial CameraTransform.
    /// The CameraSyncSystem will sync CameraTransform to LocalTransform for rendering.
    /// </summary>
    public class CameraAuthoring : MonoBehaviour
    {
        [Header("Camera Settings")]
        [Tooltip("Movement speed for WASD controls")]
        public float MovementSpeed = 10f;
        
        [Tooltip("Mouse look rotation sensitivity")]
        public float RotationSensitivity = 2f;
        
        [Tooltip("Zoom speed per scroll tick")]
        public float ZoomSpeed = 6f;
        
        [Tooltip("Minimum zoom distance (meters)")]
        public float ZoomMin = 6f;
        
        [Tooltip("Maximum zoom distance (meters)")]
        public float ZoomMax = 220f;
        
        [Tooltip("Pan sensitivity for LMB drag")]
        public float PanSensitivity = 1f;
        
        [Header("Orbital Mode")]
        [Tooltip("Sensitivity multiplier for close range (6-20m)")]
        public float SensitivityClose = 1.5f;
        
        [Tooltip("Sensitivity multiplier for mid range (20-100m)")]
        public float SensitivityMid = 1.0f;
        
        [Tooltip("Sensitivity multiplier for far range (100-220m)")]
        public float SensitivityFar = 0.6f;
        
        [Tooltip("Minimum pitch angle (degrees)")]
        public float PitchMin = -30f;
        
        [Tooltip("Maximum pitch angle (degrees)")]
        public float PitchMax = 85f;
        
        [Header("Terrain")]
        [Tooltip("Minimum clearance above terrain (meters)")]
        public float TerrainClearance = 2f;
        
        [Tooltip("Collision buffer safety margin (meters)")]
        public float CollisionBuffer = 0.4f;
        
        [Header("Initial Transform")]
        [Tooltip("Initial camera position")]
        public Vector3 InitialPosition = new Vector3(0f, 10f, -10f);
        
        [Tooltip("Initial camera rotation (euler angles)")]
        public Vector3 InitialRotation = new Vector3(45f, 0f, 0f);
    }

    public class CameraBaker : Baker<CameraAuthoring>
    {
        public override void Bake(CameraAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Add camera tag
            AddComponent<CameraTag>(entity);
            
            // Add LocalTransform (DOTS transform for rendering)
            var initialPosition = authoring.InitialPosition;
            var initialRotation = quaternion.Euler(math.radians(authoring.InitialRotation));
            AddComponent(entity, LocalTransform.FromPositionRotationScale(
                initialPosition,
                initialRotation,
                1f));
            
            // Add CameraSettings component
            AddComponent(entity, new CameraSettings
            {
                MovementSpeed = authoring.MovementSpeed,
                RotationSensitivity = authoring.RotationSensitivity,
                ZoomSpeed = authoring.ZoomSpeed,
                ZoomMin = authoring.ZoomMin,
                ZoomMax = authoring.ZoomMax,
                OrbitalFocusPoint = float3.zero,
                OrbitalRotationSpeed = 1f,
                PanSensitivity = authoring.PanSensitivity,
                SensitivityClose = authoring.SensitivityClose,
                SensitivityMid = authoring.SensitivityMid,
                SensitivityFar = authoring.SensitivityFar,
                PitchMin = authoring.PitchMin,
                PitchMax = authoring.PitchMax,
                TerrainClearance = authoring.TerrainClearance,
                CollisionBuffer = authoring.CollisionBuffer
            });
            
            // Note: CameraTransform is a singleton managed by CameraControlSystem.
            // CameraSyncSystem will sync the singleton to this entity's LocalTransform.
        }
    }
}

