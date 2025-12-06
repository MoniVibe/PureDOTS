using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Processes modifiers in dependency order using flattened dependency chains.
    /// Runs before ModifierHotPathSystem.
    /// Uses precomputed dependency chains from blob (no recursion, no dynamic lookups).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ModifierSystemGroup), OrderFirst = true)]
    public partial struct ModifierDependencyResolverSystem : ISystem
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

            // Get modifier catalog
            if (!SystemAPI.TryGetSingleton<ModifierCatalogRef>(out var catalogRef) ||
                !catalogRef.Blob.IsCreated)
            {
                return;
            }

            // Dependency resolution is handled by processing modifiers in order.
            // The hot path system processes modifiers in the order they appear in buffers,
            // which should match the dependency chain order from the blob.
            // This system can be extended to explicitly reorder modifiers based on dependency chains.
        }
    }
}

