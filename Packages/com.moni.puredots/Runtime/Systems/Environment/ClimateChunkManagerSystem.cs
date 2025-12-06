using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Manages active chunk set, loads/unloads chunks based on spatial queries, serializes inactive chunks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateBefore(typeof(MoistureEvaporationSystem))]
    public partial struct ClimateChunkManagerSystem : ISystem
    {
        private const int DefaultChunkSize = 64;
        private const int DefaultMaxActiveChunks = 100;
        private const uint SerializationInterval = 60; // Serialize every 60 ticks

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<EnvironmentGridConfigData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (timeState.IsPaused)
            {
                return;
            }

            var currentTick = timeState.Tick;

            // Get or create chunk manager
            ClimateChunkManager manager;
            if (!SystemAPI.TryGetSingleton(out manager))
            {
                manager = new ClimateChunkManager
                {
                    ChunkSize = DefaultChunkSize,
                    MaxActiveChunks = DefaultMaxActiveChunks,
                    SerializationTick = 0
                };
                SystemAPI.SetSingleton(manager);
            }

            // Process active chunk requests
            var managerEntity = SystemAPI.GetSingletonEntity<ClimateChunkManager>();
            if (SystemAPI.HasBuffer<ActiveChunkRequest>(managerEntity))
            {
                var requests = SystemAPI.GetBuffer<ActiveChunkRequest>(managerEntity);
                ProcessChunkRequests(ref state, requests, manager, currentTick);
            }

            // Serialize inactive chunks periodically
            if (currentTick - manager.SerializationTick >= SerializationInterval)
            {
                SerializeInactiveChunks(ref state, manager, currentTick);
                manager.SerializationTick = currentTick;
                SystemAPI.SetSingleton(manager);
            }
        }

        private void ProcessChunkRequests(ref SystemState state, DynamicBuffer<ActiveChunkRequest> requests, ClimateChunkManager manager, uint currentTick)
        {
            // Mark requested chunks as active
            var chunkQuery = SystemAPI.QueryBuilder()
                .WithAll<ClimateChunk>()
                .Build();

            var chunkLookup = SystemAPI.GetComponentLookup<ClimateChunk>(false);

            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                var chunkCoord = request.ChunkCoord;

                // Find or create chunk entity
                Entity chunkEntity = Entity.Null;
                foreach (var (chunk, entity) in SystemAPI.Query<RefRW<ClimateChunk>>().WithEntityAccess())
                {
                    if (chunk.ValueRO.ChunkCoord.Equals(chunkCoord))
                    {
                        chunkEntity = entity;
                        break;
                    }
                }

                if (chunkEntity == Entity.Null)
                {
                    // Create new chunk entity
                    chunkEntity = state.EntityManager.CreateEntity();
                    var chunk = new ClimateChunk
                    {
                        ChunkCoord = chunkCoord,
                        IsActive = 1,
                        LastAccessTick = currentTick
                    };
                    state.EntityManager.AddComponentData(chunkEntity, chunk);
                    // Blob will be created when chunk is first accessed
                }
                else
                {
                    // Activate existing chunk
                    var chunk = chunkLookup[chunkEntity];
                    chunk.IsActive = 1;
                    chunk.LastAccessTick = currentTick;
                    chunkLookup[chunkEntity] = chunk;
                }
            }

            // Clear processed requests
            requests.Clear();
        }

        private void SerializeInactiveChunks(ref SystemState state, ClimateChunkManager manager, uint currentTick)
        {
            var chunkQuery = SystemAPI.QueryBuilder()
                .WithAll<ClimateChunk>()
                .Build();

            if (chunkQuery.IsEmpty)
            {
                return;
            }

            var chunkLookup = SystemAPI.GetComponentLookup<ClimateChunk>(false);
            var activeChunks = new NativeList<int2>(Allocator.Temp);

            // Count active chunks
            foreach (var (chunk, _) in SystemAPI.Query<RefRO<ClimateChunk>>())
            {
                if (chunk.ValueRO.IsActive == 1)
                {
                    activeChunks.Add(chunk.ValueRO.ChunkCoord);
                }
            }

            // If we exceed max active chunks, serialize oldest inactive chunks
            if (activeChunks.Length > manager.MaxActiveChunks)
            {
                var chunksToSerialize = activeChunks.Length - manager.MaxActiveChunks;
                var chunksByAge = new NativeList<(int2 coord, uint age)>(Allocator.Temp);

                foreach (var (chunk, _) in SystemAPI.Query<RefRO<ClimateChunk>>())
                {
                    if (chunk.ValueRO.IsActive == 1)
                    {
                        var age = currentTick - chunk.ValueRO.LastAccessTick;
                        chunksByAge.Add((chunk.ValueRO.ChunkCoord, age));
                    }
                }

                // Sort by age (oldest first)
                chunksByAge.Sort(new ChunkAgeComparer());

                // Serialize oldest chunks
                for (int i = 0; i < chunksToSerialize && i < chunksByAge.Length; i++)
                {
                    var coord = chunksByAge[i].coord;
                    foreach (var (chunk, entity) in SystemAPI.Query<RefRW<ClimateChunk>>().WithEntityAccess())
                    {
                        if (chunk.ValueRO.ChunkCoord.Equals(coord))
                        {
                            // Mark as inactive (serialization would happen here in full implementation)
                            chunk.ValueRW.IsActive = 0;
                            break;
                        }
                    }
                }
            }
        }

        private struct ChunkAgeComparer : System.Collections.Generic.IComparer<(int2 coord, uint age)>
        {
            public int Compare((int2 coord, uint age) x, (int2 coord, uint age) y)
            {
                return y.age.CompareTo(x.age); // Descending order (oldest first)
            }
        }
    }
}

