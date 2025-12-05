using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace PureDOTS.Demo.Village
{
    /// <summary>
    /// Debug system that logs counts of village demo entities (villagers, homes, works).
    /// Runs once per world to provide visibility into demo entity spawning.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct VillageDebugSystem : ISystem
    {
        bool _loggedOnce;

        public void OnCreate(ref SystemState state)
        {
            UnityEngine.Debug.Log($"[VillageDebugSystem] OnCreate in world: {state.WorldUnmanaged.Name}");
            // Only run if the basic village components exist in this world.
            state.RequireForUpdate<VillageTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Guard: Skip if world is not ready (during domain reload)
            if (!state.WorldUnmanaged.IsCreated)
                return;

#if UNITY_EDITOR
            // Guard: Skip heavy work in editor when not playing
            if (!UnityEngine.Application.isPlaying)
                return;
#endif

            if (_loggedOnce)
                return;

            _loggedOnce = true;

            var em = state.EntityManager;

            int villagerCount = 0;
            int homeCount     = 0;
            int workCount     = 0;

            // Villagers
            using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<VillagerTag>()))
            {
                villagerCount = q.CalculateEntityCount();
            }

            // Homes
            using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<HomeLot>()))
            {
                homeCount = q.CalculateEntityCount();
            }

            // Works
            using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<WorkLot>()))
            {
                workCount = q.CalculateEntityCount();
            }

            int villageCount = homeCount + workCount;
            UnityEngine.Debug.Log(
                $"[VillageDebugSystem] World '{state.WorldUnmanaged.Name}': Villages: {villageCount}, Villagers: {villagerCount}, Homes: {homeCount}, Works: {workCount}");
        }
    }
}

