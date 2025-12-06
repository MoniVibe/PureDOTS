using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Networking;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Networking
{
    /// <summary>
    /// Manages deterministic RNG partitioning by player/tick.
    /// Seeds RNG with (WorldSeed, PlayerId, Tick) and stores per-player streams.
    /// Ensures no shared random state between worlds = no cross-client divergence.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct NetworkRNGSystem : ISystem
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
            uint currentTick = tickState.Tick;

            // Get or create RNG config singleton
            if (!SystemAPI.TryGetSingleton<NetworkRNGConfig>(out var config))
            {
                // Create default config
                var configEntity = state.EntityManager.CreateEntity();
                config = new NetworkRNGConfig
                {
                    WorldSeed = 1,
                    DefaultPlayerId = 0
                };
                state.EntityManager.AddComponentData(configEntity, config);
            }

            // Update NetworkRNG components with current tick
            foreach (var (rngRef, entity) in SystemAPI.Query<RefRW<NetworkRNG>>().WithEntityAccess())
            {
                ref var rng = ref rngRef.ValueRW;
                rng.WorldSeed = config.WorldSeed;
                rng.Tick = currentTick;
                
                // If PlayerId is zero, use default
                if (rng.PlayerId == 0)
                {
                    rng.PlayerId = config.DefaultPlayerId;
                }
            }
        }
    }
}

