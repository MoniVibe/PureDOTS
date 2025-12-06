using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Bootstrap system that ensures ModifierEventCoordinator entity exists.
    /// Runs in InitializationSystemGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ModifierBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ensure ModifierEventCoordinator entity exists
            var coordinatorQuery = SystemAPI.QueryBuilder()
                .WithAll<ModifierEventCoordinator>()
                .Build();

            if (coordinatorQuery.IsEmpty)
            {
                var coordinatorEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<ModifierEventCoordinator>(coordinatorEntity);
                state.EntityManager.AddBuffer<ApplyModifierEvent>(coordinatorEntity);
            }

            // Ensure ModifierConfig singleton exists
            if (!SystemAPI.HasSingleton<ModifierConfig>())
            {
                var configEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<ModifierConfig>(configEntity);
                state.EntityManager.SetComponentData(configEntity, ModifierConfig.Default);
            }

            // Disable this system after first run
            state.Enabled = false;
        }
    }
}

