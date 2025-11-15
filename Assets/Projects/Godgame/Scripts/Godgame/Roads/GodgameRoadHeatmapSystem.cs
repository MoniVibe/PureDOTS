using Godgame.Roads;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(VillagerMovementSystem))]
    public partial struct GodgameRoadHeatmapSystem : ISystem
    {
        private struct HeatAccumulator
        {
            public int3 Cell;
            public float Heat;
            public float2 Direction;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GodgameRoadConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GodgameRoadConfig>();
            var time = SystemAPI.GetSingleton<TimeState>();
            if (time.IsPaused)
            {
                return;
            }

            var heatEntity = EnsureHeatEntity(ref state);
            var buffer = state.EntityManager.GetBuffer<GodgameRoadHeatCell>(heatEntity);
            var decay = config.HeatDecayPerSecond * time.FixedDeltaTime;

            // Decay existing heat
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                var entry = buffer[i];
                entry.Heat = math.max(0f, entry.Heat - decay);
                entry.DirectionSum *= math.max(0f, 1f - decay);
                if (entry.Heat <= 0.01f)
                {
                    buffer.RemoveAt(i);
                }
                else
                {
                    buffer[i] = entry;
                }
            }

            var accumulators = new NativeParallelHashMap<int, HeatAccumulator>(buffer.Length + 64, Allocator.Temp);

            foreach (var (movement, transform) in SystemAPI.Query<RefRO<VillagerMovement>, RefRO<LocalTransform>>())
            {
                if (movement.ValueRO.IsMoving == 0)
                {
                    continue;
                }

                var position = transform.ValueRO.Position;
                var cell = Quantize(position, config.HeatCellSize);
                var key = math.hash(cell);
                var dir2 = new float2(movement.ValueRO.Velocity.x, movement.ValueRO.Velocity.z);

                accumulators.TryGetValue(key, out var acc);
                acc.Cell = cell;
                acc.Heat += time.FixedDeltaTime;
                acc.Direction += math.normalizesafe(dir2);
                accumulators[key] = acc;
            }

            var keyValues = accumulators.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < keyValues.Keys.Length; i++)
            {
                var acc = keyValues.Values[i];
                bool found = false;
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (math.all(buffer[i].Cell == acc.Cell))
                    {
                        var entry = buffer[i];
                        entry.Heat += acc.Heat;
                        entry.DirectionSum += acc.Direction;
                        buffer[i] = entry;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    buffer.Add(new GodgameRoadHeatCell
                    {
                        Cell = acc.Cell,
                        Heat = acc.Heat,
                        DirectionSum = acc.Direction
                    });
                }
            }

            keyValues.Dispose();
            accumulators.Dispose();
        }

        private static Entity EnsureHeatEntity(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonEntity<GodgameRoadHeatMap>(out var entity))
            {
                return entity;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var newEntity = ecb.CreateEntity();
            ecb.AddComponent<GodgameRoadHeatMap>(newEntity);
            ecb.AddBuffer<GodgameRoadHeatCell>(newEntity);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return newEntity;
        }

        private static int3 Quantize(float3 position, float cellSize)
        {
            var size = math.max(0.5f, cellSize);
            return (int3)math.floor(position / size);
        }
    }
}
