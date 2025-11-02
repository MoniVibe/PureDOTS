using Space4X.CameraComponents;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space4X.Authoring
{
    /// <summary>
    /// ScriptableObject profile for Space4X camera configuration.
    /// Create assets from this in Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "Space4XCameraProfile", menuName = "Space4X/Camera Profile", order = 1)]
    public class Space4XCameraProfile : ScriptableObject
    {
        [Header("Pan Settings")]
        [Tooltip("Speed of camera pan (WASD/Arrow keys)")]
        public float PanSpeed = 10f;
        
        [Tooltip("Speed of vertical camera pan (Q/E keys)")]
        public float VerticalPanSpeed = 10f;
        
        [Tooltip("Minimum bounds for camera pan (world space)")]
        public Vector3 PanBoundsMin = new Vector3(-100f, 0f, -100f);
        
        [Tooltip("Maximum bounds for camera pan (world space)")]
        public Vector3 PanBoundsMax = new Vector3(100f, 100f, 100f);
        
        [Tooltip("Enable pan bounds enforcement")]
        public bool UsePanBounds = false;

        [Header("Zoom Settings")]
        [Tooltip("Speed of camera zoom (scroll wheel)")]
        public float ZoomSpeed = 5f;
        
        [Tooltip("Minimum zoom distance")]
        public float ZoomMinDistance = 10f;
        
        [Tooltip("Maximum zoom distance")]
        public float ZoomMaxDistance = 500f;

        [Header("Rotation Settings")]
        [Tooltip("Rotation sensitivity in degrees per pixel of mouse movement")]
        public float RotationSpeed = 0.25f;
        
        [Tooltip("Minimum pitch angle (degrees)")]
        public float PitchMin = -30f;
        
        [Tooltip("Maximum pitch angle (degrees)")]
        public float PitchMax = 85f;

        [Header("General Settings")]
        [Tooltip("Smoothing factor for camera movement (0-1, lower = smoother)")]
        [Range(0f, 1f)]
        public float Smoothing = 0.1f;

        [Header("Feature Toggles")]
        public bool EnablePan = true;
        public bool EnableZoom = true;
        public bool EnableRotation = false;

        /// <summary>
        /// Converts profile to DOTS component data.
        /// </summary>
        public Space4XCameraConfig ToComponent()
        {
            return new Space4XCameraConfig
            {
                PanSpeed = PanSpeed,
                VerticalPanSpeed = VerticalPanSpeed,
                PanBoundsMin = PanBoundsMin,
                PanBoundsMax = PanBoundsMax,
                UsePanBounds = UsePanBounds,
                ZoomSpeed = ZoomSpeed,
                ZoomMinDistance = ZoomMinDistance,
                ZoomMaxDistance = ZoomMaxDistance,
                RotationSpeed = RotationSpeed,
                PitchMin = math.radians(PitchMin),
                PitchMax = math.radians(PitchMax),
                Smoothing = Smoothing,
                EnablePan = EnablePan,
                EnableZoom = EnableZoom,
                EnableRotation = EnableRotation
            };
        }
    }
}

