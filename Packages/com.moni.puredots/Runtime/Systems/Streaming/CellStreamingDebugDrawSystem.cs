using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// Optional gizmo/debug-draw for streaming window and cell centers (Editor/dev builds only).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CellStreamingDebugDrawSystem : ISystem
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellStreamingWindow>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<CellStreamingWindow>(out var window))
            {
                return;
            }

            // Draw window (XZ rectangle) at a small Y offset for visibility.
            var center = window.Center;
            var half = window.HalfExtents;
            var p1 = new Vector3(center.x - half.x, center.y + 0.5f, center.z - half.y);
            var p2 = new Vector3(center.x + half.x, center.y + 0.5f, center.z - half.y);
            var p3 = new Vector3(center.x + half.x, center.y + 0.5f, center.z + half.y);
            var p4 = new Vector3(center.x - half.x, center.y + 0.5f, center.z + half.y);
            Debug.DrawLine(p1, p2, Color.green);
            Debug.DrawLine(p2, p3, Color.green);
            Debug.DrawLine(p3, p4, Color.green);
            Debug.DrawLine(p4, p1, Color.green);

            // Draw cell centers (optional, light color).
            foreach (var cell in SystemAPI.Query<RefRO<SimulationCell>>())
            {
                var coord = cell.ValueRO.CellCoordinates;
                // Assume CellSize in config; if missing, default 1.
                var cellSize = SystemAPI.TryGetSingleton<CellStreamingConfig>(out var cfg)
                    ? cfg.CellSize
                    : new Unity.Mathematics.float2(1f, 1f);
                var cellCenter = new Vector3(
                    coord.x * cellSize.x + cellSize.x * 0.5f,
                    center.y + 0.25f,
                    coord.y * cellSize.y + cellSize.y * 0.5f);
                Debug.DrawLine(cellCenter, cellCenter + Vector3.up * 0.5f, Color.cyan);
            }
        }
#else
        public void OnCreate(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
#endif
    }
}
