using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Cold path system: decrements durations and removes expired modifiers.
    /// Runs at 0.2-1Hz (configurable, throttled) in ColdPathSystemGroup.
    /// Uses deferred write via command buffer for thread safety.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ModifierColdPathGroup))]
    public partial struct ModifierExpirySystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateIntervalTicks = 15; // ~0.25s at 60Hz = 4Hz update rate

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

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;

            // Throttle updates (cold path)
            if (_lastUpdateTick != 0 && currentTick - _lastUpdateTick < UpdateIntervalTicks)
            {
                return;
            }

            uint ticksElapsed = _lastUpdateTick != 0 ? currentTick - _lastUpdateTick : 1;
            _lastUpdateTick = currentTick;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Process all entities with modifier buffers
            new ExpireModifiersJob
            {
                CurrentTick = currentTick,
                TicksElapsed = ticksElapsed,
                Ecb = ecb
            }.ScheduleParallel();

            state.Dependency.Complete();
        }

        [BurstCompile]
        public partial struct ExpireModifiersJob : IJobEntity
        {
            public uint CurrentTick;
            public uint TicksElapsed;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref DynamicBuffer<ModifierInstance> modifiers)
            {
                // Process modifiers in reverse order for safe removal
                for (int i = modifiers.Length - 1; i >= 0; i--)
                {
                    var modifier = modifiers[i];

                    // Skip permanent modifiers (Duration == -1)
                    if (modifier.Duration < 0)
                    {
                        continue;
                    }

                    // Decrement duration by elapsed ticks
                    short newDuration = (short)(modifier.Duration - (short)TicksElapsed);

                    if (newDuration <= 0)
                    {
                        // Modifier expired - remove it
                        modifiers.RemoveAtSwapBack(i);

                        // Mark entity as dirty for hot path recomputation
                        Ecb.AddComponent<ModifierDirtyTag>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        // Update duration
                        modifier.Duration = newDuration;
                        modifiers[i] = modifier;
                    }
                }
            }
        }
    }
}

