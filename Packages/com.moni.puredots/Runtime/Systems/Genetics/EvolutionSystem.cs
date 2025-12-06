using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Systems.Genetics
{
    /// <summary>
    /// Evolution system for selection and population shifts.
    /// Population shifts toward efficient behaviors automatically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(InheritanceSystem))]
    public partial struct EvolutionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Evaluate fitness and selection
            // In full implementation, would:
            // 1. Calculate fitness scores based on performance
            // 2. Select high-fitness entities for reproduction
            // 3. Apply selection pressure
            // 4. Track population evolution

            var geneticQuery = state.GetEntityQuery(
                typeof(GeneticState),
                typeof(GeneSpec));

            if (geneticQuery.IsEmpty)
            {
                return;
            }

            var job = new EvaluateFitnessJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(geneticQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct EvaluateFitnessJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                ref GeneticState genetic,
                in DynamicBuffer<GeneSpec> genes)
            {
                // Calculate fitness based on traits
                // In full implementation, would:
                // 1. Evaluate performance metrics
                // 2. Calculate fitness score
                // 3. Update genetic state
                // 4. Apply selection pressure

                // Example: Simple fitness calculation
                var fitness = 0f;
                for (int i = 0; i < genes.Length; i++)
                {
                    fitness += genes[i].TraitValue; // Sum of trait values
                }
                fitness /= math.max(1, genes.Length); // Average

                genetic.Fitness = fitness;
                genetic.LastEvolutionTick = CurrentTick;
            }
        }
    }
}

