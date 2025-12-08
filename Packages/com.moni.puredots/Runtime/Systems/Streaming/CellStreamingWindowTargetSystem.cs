using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// Updates CellStreamingWindowTarget from the main camera (fallback) if none is provided by gameplay.
    /// Editor/dev convenience to keep streaming responsive without manual wiring.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(CellStreamingWindowUpdateSystem))]
    public partial struct CellStreamingWindowTargetSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            // If a target already exists, do nothing (gameplay should own it).
            if (SystemAPI.HasSingleton<CellStreamingWindowTarget>())
            {
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            // Default half-extents based on orthographic size or a fixed radius.
            float2 halfExtents;
            if (cam.orthographic)
            {
                halfExtents = new float2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
            }
            else
            {
                // Fallback: fixed 50-unit half-extents; tweak as needed.
                halfExtents = new float2(50f, 50f);
            }

            var pos = cam.transform.position;
            var target = new CellStreamingWindowTarget
            {
                Position = new float3(pos.x, pos.y, pos.z),
                HalfExtents = halfExtents
            };

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, target);
        }
    }
}
