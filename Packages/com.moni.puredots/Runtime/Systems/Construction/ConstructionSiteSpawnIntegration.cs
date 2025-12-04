using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Construction;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Construction
{
    /// <summary>
    /// Bridges approved ConstructionIntents to existing ConstructionSiteSpawnSystem.
    /// Converts intents to construction site spawn commands.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ConstructionDecisionSystem))]
    public partial struct ConstructionSiteSpawnIntegration : ISystem
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

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // Process groups with approved ConstructionIntents
            foreach (var (coordinator, intents, entity) in SystemAPI.Query<
                RefRW<BuildCoordinator>,
                DynamicBuffer<ConstructionIntent>>().WithEntityAccess())
            {
                var coordinatorValue = coordinator.ValueRO;

                for (int i = 0; i < intents.Length; i++)
                {
                    var intent = intents[i];
                    if (intent.Status != 1) // Not Approved
                        continue;

                    // TODO: Convert intent to construction site spawn command
                    // This would:
                    // 1. Read BuildingPatternSpec from catalog using PatternId
                    // 2. Create ConstructionSite entity with appropriate components
                    // 3. Set position based on SuggestedCenter (with pathfinding/terrain checks)
                    // 4. Mark intent as Status=Realized (or decrement DesiredCount if more needed)

                    // For now, stub - would integrate with existing ConstructionSiteSpawnSystem
                    // Mark intent as realized for now
                    intent.Status = 2; // Realized
                    intents[i] = intent;

                    // Increment active site count
                    var newCoordinator = coordinator.ValueRO;
                    newCoordinator.ActiveSiteCount = (byte)math.min(255, newCoordinator.ActiveSiteCount + 1);
                    coordinator.ValueRW = newCoordinator;
                }
            }
        }
    }
}

