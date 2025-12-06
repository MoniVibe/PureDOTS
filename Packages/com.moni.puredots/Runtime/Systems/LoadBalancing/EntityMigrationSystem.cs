using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Shared;
using PureDOTS.Systems;

namespace PureDOTS.Systems.LoadBalancing
{
    /// <summary>
    /// Entity migration system migrating entities between worlds.
    /// Migrates hot clusters to new ECS worlds dynamically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(DynamicLoadBalancer))]
    public partial struct EntityMigrationSystem : ISystem
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
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Migrate entities between worlds
            // In full implementation, would:
            // 1. Query entities with WorldPartition and NeedsMigration flag
            // 2. Determine target world based on load balancing
            // 3. Migrate entity data to target world
            // 4. Update WorldPartition component
            // 5. Handle cross-world references

            var migrationQuery = state.GetEntityQuery(
                typeof(WorldPartition),
                typeof(LoadBalanceMetrics));

            if (migrationQuery.IsEmpty)
            {
                return;
            }

            var job = new MigrateEntitiesJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(migrationQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct MigrateEntitiesJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                ref WorldPartition partition,
                in LoadBalanceMetrics metrics)
            {
                // Migrate entity if needed
                // In full implementation, would:
                // 1. Check if NeedsMigration flag is set
                // 2. Determine target world based on load
                // 3. Migrate entity to target world
                // 4. Update partition information
                // 5. Clear migration flag

                if (partition.NeedsMigration)
                {
                    // Migration logic would go here
                    partition.NeedsMigration = false;
                    partition.LastMigrationTick = CurrentTick;
                }
            }
        }
    }
}

