using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Loads and validates behavior catalog blob assets.
    /// Runs once during initialization.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup), OrderFirst = true)]
    public partial struct BehaviorCatalogSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Catalog loading handled by authoring/baking systems
            // This system exists for future validation/health checks
        }
    }
}

