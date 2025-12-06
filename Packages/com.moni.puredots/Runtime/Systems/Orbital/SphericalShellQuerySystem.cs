using PureDOTS.Runtime.Components.Orbital;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Orbital
{
    /// <summary>
    /// Provides radius-based shell queries for spatial lookups.
    /// Replaces disc-grid spatial queries with isotropic radius checks.
    /// Jump routes become radius checks: if (distance < shellRadius)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SphericalShellUpdateSystem))]
    public partial struct SphericalShellQuerySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShellMembership>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // This system provides query utilities - actual queries are performed
            // by other systems that need spatial lookups
            // No per-frame work needed here
        }

        /// <summary>
        /// Finds all entities within a given radius of a position.
        /// Used by jump route calculations and spatial queries.
        /// </summary>
        public static void QueryEntitiesInRadius(
            ref SystemState state,
            float3 center,
            double radius,
            NativeList<Entity> results)
        {
            results.Clear();

            foreach (var (shell, sixDoF, entity) in SystemAPI.Query<RefRO<ShellMembership>, RefRO<SixDoFState>>()
                .WithEntityAccess())
            {
                double distance = math.length((double3)(sixDoF.ValueRO.Position - center));
                if (distance <= radius)
                {
                    results.Add(entity);
                }
            }
        }

        /// <summary>
        /// Checks if an entity is within a given shell's radius bounds.
        /// </summary>
        public static bool IsEntityInShellRadius(
            ref SystemState state,
            Entity entity,
            ShellType shellType)
        {
            if (!SystemAPI.HasComponent<ShellMembership>(entity))
            {
                return false;
            }

            var shell = SystemAPI.GetComponent<ShellMembership>(entity);
            return shell.ShellIndex == (int)shellType;
        }
    }
}

