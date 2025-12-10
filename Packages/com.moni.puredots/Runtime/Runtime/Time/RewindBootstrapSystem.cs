using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Time
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RewindBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            if (SystemAPI.HasSingleton<RewindState>())
            {
                state.Enabled = false;
                return;
            }

            var config = SystemAPI.GetSingleton<RewindConfig>();

            var e = em.CreateEntity();
            em.AddComponentData(e, new RewindState
            {
                Mode = config.InitialMode,
                CurrentTick = 0,
                TargetTick = 0,
                TickDuration = config.TickDuration,
                MaxHistoryTicks = config.MaxHistoryTicks,
                PendingStepTicks = 0
            });

            state.Enabled = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}


