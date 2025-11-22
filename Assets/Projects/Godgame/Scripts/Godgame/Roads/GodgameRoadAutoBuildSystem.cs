using Godgame.Presentation;
using Godgame.Roads;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Monitors heatmap cells and spawns auto-built road segments along popular villager paths.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GodgameRoadHeatmapSystem))]
    public partial struct GodgameRoadAutoBuildSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GodgameRoadConfig>();
            state.RequireForUpdate<GodgameRoadHeatMap>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GodgameRoadConfig>();
            var heatEntity = SystemAPI.GetSingletonEntity<GodgameRoadHeatMap>();
            var buffer = state.EntityManager.GetBuffer<GodgameRoadHeatCell>(heatEntity);

            var bestIndex = -1;
            float bestHeat = config.HeatBuildThreshold;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Heat > bestHeat)
                {
                    bestHeat = buffer[i].Heat;
                    bestIndex = i;
                }
            }

            if (bestIndex == -1)
            {
                return;
            }

            var cell = buffer[bestIndex];
            var updatedCell = cell;
            updatedCell.Heat = 0f;
            updatedCell.DirectionSum = float2.zero;
            buffer[bestIndex] = updatedCell;

            var direction = math.normalizesafe(new float3(cell.DirectionSum.x, 0f, cell.DirectionSum.y));
            if (math.lengthsq(direction) < 0.1f)
            {
                direction = new float3(1f, 0f, 0f);
            }

            var center = CellCenter(cell.Cell, config.HeatCellSize);
            center.y = 0f;

            var start = center - direction * (config.AutoBuildLength * 0.5f);
            var end = center + direction * (config.AutoBuildLength * 0.5f);

            // Avoid duplicates
            if (RoadExistsNear(ref state, center, config.AutoBuildLength * 0.75f))
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var nearestVillage = FindNearestVillage(ref state, center, 60f);
            if (nearestVillage != Entity.Null && state.EntityManager.HasComponent<GodgameVillageCenter>(nearestVillage))
            {
                var village = state.EntityManager.GetComponentData<GodgameVillageCenter>(nearestVillage);
                start.y = village.BaseHeight;
                end.y = village.BaseHeight;
            }
            GodgameVillageRoadBootstrapSystem.SpawnRoadSegment(ref ecb, nearestVillage, start, end, config, true);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private Entity FindNearestVillage(ref SystemState state, float3 position, float maxDistance)
        {
            Entity best = Entity.Null;
            float bestDistSq = maxDistance * maxDistance;

            foreach (var (center, transform, entity) in SystemAPI.Query<RefRO<GodgameVillageCenter>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                float distSq = math.lengthsq(transform.ValueRO.Position - position);
                if (distSq < bestDistSq)
                {
                    best = entity;
                    bestDistSq = distSq;
                }
            }

            return best;
        }

        private bool RoadExistsNear(ref SystemState state, float3 position, float tolerance)
        {
            float tolSq = tolerance * tolerance;
            foreach (var (segment, transform) in SystemAPI.Query<RefRO<GodgameRoadSegment>, RefRO<LocalTransform>>())
            {
                float distSq = math.lengthsq(transform.ValueRO.Position - position);
                if (distSq <= tolSq)
                {
                    return true;
                }
            }

            return false;
        }

        private static float3 CellCenter(int3 cell, float cellSize)
        {
            var size = math.max(0.5f, cellSize);
            return (cell + new float3(0.5f, 0.5f, 0.5f)) * size;
        }
    }
}
