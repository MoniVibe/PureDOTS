using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Miracles
{
    /// <summary>
    /// Updates miracle cooldown timers every frame.
    /// Decrements RemainingSeconds and handles charge recharge.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MiracleEffectSystemGroup))]
    [UpdateAfter(typeof(MiracleActivationSystem))]
    public partial struct MiracleCooldownSystem : ISystem
    {
        private TimeAwareController _controller;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _controller = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_controller.TryBegin(timeState, rewindState, out var context))
            {
                return;
            }

            // Only process in Record mode
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Update all cooldown buffers
            foreach (var cooldowns in SystemAPI.Query<RefRW<DynamicBuffer<MiracleCooldown>>>())
            {
                for (int i = 0; i < cooldowns.ValueRW.Length; i++)
                {
                    var cooldown = cooldowns.ValueRW[i];
                    cooldown.RemainingSeconds = math.max(0f, cooldown.RemainingSeconds - deltaTime);
                    
                    // Future: Handle charge recharge when cooldown completes
                    // For MVP, charges are only restored manually or on activation
                    
                    cooldowns.ValueRW[i] = cooldown;
                }
            }
        }
    }
}

