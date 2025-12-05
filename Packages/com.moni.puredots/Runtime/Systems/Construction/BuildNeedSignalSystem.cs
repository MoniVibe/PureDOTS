using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Construction;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Construction
{
    /// <summary>
    /// Generates BuildNeedSignal buffers on group entities based on individual needs.
    /// Runs per individual (villager/colonist/crew) and emits signals to their group.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BuildNeedSignalSystem : ISystem
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
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var currentTick = timeState.Tick;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // Process individuals with GroupMembership
            foreach (var (membership, entity) in SystemAPI.Query<RefRO<GroupMembership>>().WithEntityAccess())
            {
                var groupEntity = membership.ValueRO.Group;
                if (groupEntity == Entity.Null || !SystemAPI.Exists(groupEntity))
                    continue;

                // Get or create BuildNeedSignal buffer on group entity
                if (!SystemAPI.HasBuffer<BuildNeedSignal>(groupEntity))
                {
                    ecb.AddBuffer<BuildNeedSignal>(groupEntity);
                }

                // Note: Actual signal generation logic is game-specific
                // This system provides the framework; game-specific systems extend it
                // For now, this is a stub that can be extended by GodgameBuildNeedSignalSystem
            }
        }
    }
}





