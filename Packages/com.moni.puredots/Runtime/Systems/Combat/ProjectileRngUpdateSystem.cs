using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Updates WorldRng singleton each tick for deterministic projectile spread and damage variance.
    /// Runs in TimeSystemGroup after TimeTickSystem to ensure tick is updated.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [UpdateAfter(typeof(TimeTickSystem))]
    public partial struct ProjectileRngUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            // Ensure WorldRng singleton exists
            if (!SystemAPI.HasSingleton<WorldRng>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<WorldRng>(entity);
                state.EntityManager.SetComponentData(entity, new WorldRng { Seed = 0x12345678u });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var worldRngEntity = SystemAPI.GetSingletonEntity<WorldRng>();
            var worldRng = SystemAPI.GetComponentRW<WorldRng>(worldRngEntity);

            // Deterministic seed update: Seed = hash(worldTick, previousSeed)
            uint previousSeed = worldRng.ValueRO.Seed;
            uint newSeed = math.asuint(math.hash(new uint2(tickState.Tick, previousSeed)));

            worldRng.ValueRW.Seed = newSeed;
        }
    }
}

