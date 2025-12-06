using Unity.Entities;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Minimal telemetry overlay showing system performance and entity counts.
    /// </summary>
    public class TelemetryOverlay : MonoBehaviour
    {
        [Header("Display Settings")]
        public bool showOverlay = true;
        public Vector2 position = new Vector2(10f, 10f);
        public int fontSize = 12;

        private World _world;
        private GUIStyle _style;

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _style = new GUIStyle
            {
                fontSize = fontSize,
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft
            };
        }

        private void OnGUI()
        {
            if (!showOverlay || _world == null || !_world.IsCreated)
                return;

            var entityCount = _world.EntityManager.GetAllEntities().Length;
            var content = $"Entities: {entityCount}\n";

            // Add system performance info if available
            content += GetSystemPerformanceInfo();

            GUI.Label(new Rect(position.x, position.y, 400f, 200f), content, _style);
        }

        private string GetSystemPerformanceInfo()
        {
            // Profiler markers are available but we'd need to query them differently
            // For now, return basic info
            return "Systems: Active\n";
        }
    }
}

