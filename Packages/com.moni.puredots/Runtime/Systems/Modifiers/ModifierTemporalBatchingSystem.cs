using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Applies modifiers every ModifierTickInterval (configurable, default 0.25s = 4Hz).
    /// Processes multiple ticks for time-based decay.
    /// Formula: duration -= ticksElapsed; if (duration <= 0) expire()
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ModifierHotPathSystem))]
    public partial struct ModifierTemporalBatchingSystem : ISystem
    {
        private uint _lastBatchTick;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _lastBatchTick = 0;
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

            // Get config (or use defaults)
            var config = SystemAPI.HasSingleton<ModifierConfig>()
                ? SystemAPI.GetSingleton<ModifierConfig>()
                : ModifierConfig.Default;

            // Calculate ticks per batch (convert interval to ticks)
            uint ticksPerBatch = (uint)(config.ModifierTickInterval / timeState.FixedDeltaTime);
            if (ticksPerBatch == 0) ticksPerBatch = 1;

            // Check if it's time for a batch update
            if (currentTick - _lastBatchTick < ticksPerBatch)
            {
                return;
            }

            uint ticksElapsed = currentTick - _lastBatchTick;
            _lastBatchTick = currentTick;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Process batched modifier updates
            new BatchModifiersJob
            {
                TicksElapsed = ticksElapsed,
                Ecb = ecb
            }.ScheduleParallel();

            state.Dependency.Complete();
        }

        [BurstCompile]
        public partial struct BatchModifiersJob : IJobEntity
        {
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

                    // Skip permanent modifiers
                    if (modifier.Duration < 0)
                    {
                        continue;
                    }

                    // Decrement duration by elapsed ticks
                    short newDuration = (short)(modifier.Duration - (short)TicksElapsed);

                    if (newDuration <= 0)
                    {
                        // Modifier expired
                        modifiers.RemoveAtSwapBack(i);
                        Ecb.AddComponent<ModifierDirtyTag>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        modifier.Duration = newDuration;
                        modifiers[i] = modifier;
                    }
                }
            }
        }
    }
}

