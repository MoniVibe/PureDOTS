using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Updates StaminaState each tick: regenerates Current based on RegenRate.
    /// Mirrors FocusUpdateSystem pattern.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial struct StaminaUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            if (timeState.IsPaused)
                return;

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            float deltaTime = timeState.FixedDeltaTime;

            var job = new UpdateStaminaJob
            {
                DeltaTime = deltaTime,
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct UpdateStaminaJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;

            void Execute(ref StaminaState stamina)
            {
                // Regenerate: Current += RegenRate * DeltaTime
                float netChange = stamina.RegenRate * DeltaTime;
                stamina.Current = math.clamp(stamina.Current + netChange, 0f, stamina.Max);

                // Soft threshold penalties applied by other systems reading StaminaState
                // Hard threshold exhaustion handled by combat systems
            }
        }
    }
}

