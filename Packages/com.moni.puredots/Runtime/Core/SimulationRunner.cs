using Unity.Entities;
using UnityEngine;
using PureDOTS.AI.MindECS;
using PureDOTS.AI.AggregateECS;
using PureDOTS.Runtime.Bridges;

namespace PureDOTS.Core
{
    /// <summary>
    /// Coordinates simulation execution across all ECS worlds.
    /// </summary>
    public static class SimulationRunner
    {
        private static World _bodyWorld;
        private static MindECSWorld _mindWorld;
        private static AggregateECSWorld _aggregateWorld;
        private static float _mindTickRate;
        private static float _aggregateTickRate;
        private static float _mindAccumulator;
        private static float _aggregateAccumulator;

        /// <summary>
        /// Starts simulation for all worlds with configurable tick rates.
        /// </summary>
        public static void StartAllWorlds(World bodyWorld, MindECSWorld mindWorld, AggregateECSWorld aggregateWorld, float mindTickRate, float aggregateTickRate)
        {
            _bodyWorld = bodyWorld;
            _mindWorld = mindWorld;
            _aggregateWorld = aggregateWorld;
            _mindTickRate = mindTickRate;
            _aggregateTickRate = aggregateTickRate;
            _mindAccumulator = 0f;
            _aggregateAccumulator = 0f;

            Debug.Log($"[SimulationRunner] Started all worlds - Mind: {mindTickRate}Hz, Aggregate: {aggregateTickRate}Hz");
        }

        /// <summary>
        /// Updates all worlds based on tick rates. Call from Unity's Update loop.
        /// </summary>
        public static void UpdateAllWorlds(float deltaTime)
        {
            if (_bodyWorld == null || !_bodyWorld.IsCreated)
                return;

            // Body world updates every frame (handled by Unity's PlayerLoop)
            // Mind and Aggregate worlds update at their configured rates

            _mindAccumulator += deltaTime;
            if (_mindAccumulator >= 1f / _mindTickRate)
            {
                _mindWorld?.Update(_mindAccumulator);
                _mindAccumulator = 0f;
            }

            _aggregateAccumulator += deltaTime;
            if (_aggregateAccumulator >= 1f / _aggregateTickRate)
            {
                _aggregateWorld?.Update(_aggregateAccumulator);
                _aggregateAccumulator = 0f;
            }
        }
    }
}

