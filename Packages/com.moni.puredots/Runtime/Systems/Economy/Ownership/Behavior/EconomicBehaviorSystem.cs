using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Economy.Ownership.Behavior
{
    [BurstCompile]
    [UpdateInGroup(typeof(MindEconomySystemGroup))]
    public partial struct EconomicBehaviorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }
        }
    }
}
