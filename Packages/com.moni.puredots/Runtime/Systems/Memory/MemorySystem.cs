using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Memory;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Memory
{
    /// <summary>
    /// Applies memory add requests and decays generic memories over time.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct MemorySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;

            var config = SystemAPI.TryGetSingleton<MemoryConfig>(out var configSingleton)
                ? configSingleton
                : MemoryConfig.Default;

            foreach (var (requests, entity) in SystemAPI.Query<DynamicBuffer<MemoryAddRequest>>().WithEntityAccess())
            {
                if (requests.Length == 0)
                {
                    continue;
                }

                DynamicBuffer<MemoryEntry> memories;
                if (!state.EntityManager.HasBuffer<MemoryEntry>(entity))
                {
                    memories = state.EntityManager.AddBuffer<MemoryEntry>(entity);
                }
                else
                {
                    memories = state.EntityManager.GetBuffer<MemoryEntry>(entity);
                }

                for (int i = 0; i < requests.Length; i++)
                {
                    ApplyMemoryRequest(ref memories, requests[i], currentTick);
                }

                PruneMemories(ref memories, config.MaxMemories);
                requests.Clear();
            }

            foreach (var memories in SystemAPI.Query<DynamicBuffer<MemoryEntry>>())
            {
                DecayMemories(ref memories, currentTick, config.MinMagnitude);
                PruneMemories(ref memories, config.MaxMemories);
            }
        }

        private static void ApplyMemoryRequest(ref DynamicBuffer<MemoryEntry> memories, in MemoryAddRequest request, uint currentTick)
        {
            if (request.MemoryId.Length == 0)
            {
                return;
            }

            for (int i = 0; i < memories.Length; i++)
            {
                var entry = memories[i];
                if (!entry.MemoryId.Equals(request.MemoryId) || entry.RelatedEntity != request.RelatedEntity)
                {
                    continue;
                }

                entry.InitialMagnitude += request.Magnitude;
                entry.CurrentMagnitude += request.Magnitude;
                entry.FormedTick = currentTick;
                entry.DecayHalfLife = request.DecayHalfLife > 0 ? request.DecayHalfLife : entry.DecayHalfLife;
                entry.Flags |= request.Flags;
                memories[i] = entry;
                return;
            }

            memories.Add(new MemoryEntry
            {
                MemoryId = request.MemoryId,
                InitialMagnitude = request.Magnitude,
                CurrentMagnitude = request.Magnitude,
                FormedTick = currentTick,
                DecayHalfLife = request.DecayHalfLife,
                RelatedEntity = request.RelatedEntity,
                Flags = request.Flags
            });
        }

        private static void DecayMemories(ref DynamicBuffer<MemoryEntry> memories, uint currentTick, float minMagnitude)
        {
            for (int i = memories.Length - 1; i >= 0; i--)
            {
                var entry = memories[i];
                if (entry.DecayHalfLife > 0)
                {
                    var ticksSince = currentTick - entry.FormedTick;
                    var decayFactor = math.pow(0.5f, (float)ticksSince / entry.DecayHalfLife);
                    entry.CurrentMagnitude = entry.InitialMagnitude * decayFactor;
                }

                if (math.abs(entry.CurrentMagnitude) < minMagnitude)
                {
                    memories.RemoveAt(i);
                    continue;
                }

                memories[i] = entry;
            }
        }

        private static void PruneMemories(ref DynamicBuffer<MemoryEntry> memories, int maxMemories)
        {
            if (maxMemories <= 0 || memories.Length <= maxMemories)
            {
                return;
            }

            while (memories.Length > maxMemories)
            {
                int weakestIndex = 0;
                float weakestMagnitude = math.abs(memories[0].CurrentMagnitude);
                for (int i = 1; i < memories.Length; i++)
                {
                    var magnitude = math.abs(memories[i].CurrentMagnitude);
                    if (magnitude < weakestMagnitude)
                    {
                        weakestMagnitude = magnitude;
                        weakestIndex = i;
                    }
                }

                memories.RemoveAt(weakestIndex);
            }
        }
    }
}
