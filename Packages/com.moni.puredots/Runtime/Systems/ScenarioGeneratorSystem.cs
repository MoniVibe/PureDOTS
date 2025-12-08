using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Scenario;
using PureDOTS.Runtime.Math;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Headless system that populates entities procedurally for stress testing.
    /// Generates terrain, fleets, villagers, miracles deterministically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ScenarioGeneratorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioParameters>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var parameters = SystemAPI.GetSingleton<ScenarioParameters>();
            uint seed = parameters.Seed;
            if (seed == 0)
            {
                seed = (uint)SystemAPI.Time.ElapsedTime.GetHashCode();
            }

            var entities = new NativeList<Entity>(parameters.EntityCount, Allocator.Temp);

            // Generate terrain
            GenerateTerrain(ref state, parameters.TerrainChunks, seed, ref entities);

            // Generate fleets
            GenerateFleets(ref state, parameters.FleetCount, seed + 1, ref entities);

            // Generate villagers
            GenerateVillagers(ref state, parameters.VillagerCount, seed + 2, ref entities);

            // Generate miracles
            GenerateMiracles(ref state, parameters.MiracleCount, seed + 3, ref entities);

            // Output scenario JSON
            FixedString512Bytes outputPath = "Scenarios/GeneratedScenario.json";
            var entityArray = entities.AsArray();
            ScenarioOutput.WriteToJson(ref parameters, in entityArray, in outputPath);

            entities.Dispose();
            state.Enabled = false; // Run once
        }

        [BurstCompile]
        private void GenerateTerrain(ref SystemState state, int count, uint seed, ref NativeList<Entity> entities)
        {
            uint rngState = seed;
            for (int i = 0; i < count; i++)
            {
                var entity = state.EntityManager.CreateEntity();
                // Add terrain components
                entities.Add(entity);
                rngState = MathKernel.NextRandom(ref rngState);
            }
        }

        [BurstCompile]
        private void GenerateFleets(ref SystemState state, int count, uint seed, ref NativeList<Entity> entities)
        {
            uint rngState = seed;
            for (int i = 0; i < count; i++)
            {
                var entity = state.EntityManager.CreateEntity();
                // Add fleet components
                entities.Add(entity);
                rngState = MathKernel.NextRandom(ref rngState);
            }
        }

        [BurstCompile]
        private void GenerateVillagers(ref SystemState state, int count, uint seed, ref NativeList<Entity> entities)
        {
            uint rngState = seed;
            for (int i = 0; i < count; i++)
            {
                var entity = state.EntityManager.CreateEntity();
                // Add villager components
                entities.Add(entity);
                rngState = MathKernel.NextRandom(ref rngState);
            }
        }

        [BurstCompile]
        private void GenerateMiracles(ref SystemState state, int count, uint seed, ref NativeList<Entity> entities)
        {
            uint rngState = seed;
            for (int i = 0; i < count; i++)
            {
                var entity = state.EntityManager.CreateEntity();
                // Add miracle components
                entities.Add(entity);
                rngState = MathKernel.NextRandom(ref rngState);
            }
        }
    }
}

