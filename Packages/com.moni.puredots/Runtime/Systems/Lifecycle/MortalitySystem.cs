using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Lifecycle;

namespace PureDOTS.Systems.Lifecycle
{
    /// <summary>
    /// Emits natural death events based on mortality configuration.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LifecycleProgressSystem))]
    public partial struct MortalitySystem : ISystem
    {
        private BufferLookup<DeathEvent> _deathEventLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _deathEventLookup = state.GetBufferLookup<DeathEvent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            _deathEventLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new MortalityJob
            {
                CurrentTick = timeState.Tick,
                DeathEventLookup = _deathEventLookup,
                Ecb = ecb
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct MortalityJob : IJobEntity
        {
            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public BufferLookup<DeathEvent> DeathEventLookup;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                in LifecycleState lifecycle,
                in MortalityConfig mortality,
                in Health health,
                in DeathState deathState)
            {
                if (deathState.IsDead)
                {
                    return;
                }

                if (mortality.CanDieOfAge == 0)
                {
                    return;
                }

                if (DeathEventLookup.HasBuffer(entity) && DeathEventLookup[entity].Length > 0)
                {
                    return;
                }

                var adjusted = mortality;
                if (mortality.LifespanVariance > 0f)
                {
                    float variance = (Deterministic01(entity.Index, 0xC011CAFEu) * 2f - 1f) * mortality.LifespanVariance;
                    adjusted.NaturalLifespan = math.max(1f, mortality.NaturalLifespan + variance);
                }

                float deathChance = LifecycleHelpers.CalculateDeathChance(lifecycle, adjusted);
                if (deathChance <= 0f)
                {
                    return;
                }

                float roll = Deterministic01(entity.Index, CurrentTick);
                if (roll > deathChance)
                {
                    return;
                }

                if (!DeathEventLookup.HasBuffer(entity))
                {
                    Ecb.AddBuffer<DeathEvent>(entityInQueryIndex, entity);
                }

                Ecb.AppendToBuffer(entityInQueryIndex, entity, new DeathEvent
                {
                    DeadEntity = entity,
                    KillerEntity = Entity.Null,
                    KillingBlowType = DamageType.True,
                    DeathTick = CurrentTick
                });
            }
        }

        [BurstCompile]
        private static float Deterministic01(int entityIndex, uint salt)
        {
            uint hash = math.hash(new uint2((uint)entityIndex, salt));
            return (hash & 0x00FFFFFF) / (float)0x01000000;
        }
    }
}
