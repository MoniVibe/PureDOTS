using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Systems;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;

namespace PureDOTS.AI.AggregateECS
{
    /// <summary>
    /// Manages the DefaultEcs world for aggregate AI layer (villages, fleets, bands).
    /// Singleton instance accessible from Unity Entities systems.
    /// Runs at 1 Hz (slower than individual agent cognition at 2-5 Hz).
    /// </summary>
    public class AggregateECSWorld
    {
        private static AggregateECSWorld _instance;
        private World _world;
        private SequentialSystem<ISystem<World>> _systems;
        private AgentSyncBus _syncBus;

        public World World => _world;
        public SequentialSystem<ISystem<World>> Systems => _systems;

        private AggregateECSWorld(AgentSyncBus syncBus = null)
        {
            _world = new World();
            _systems = new SequentialSystem<ISystem<World>>();
            _syncBus = syncBus;

            // Initialize aggregate intent system with sync bus
            if (_syncBus != null)
            {
                var aggregateIntentSystem = new AggregateIntentSystem(_world, _syncBus);
                _systems.Add(aggregateIntentSystem);
            }
        }

        public static AggregateECSWorld Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to get sync bus from Unity world if available
                    var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
                    _instance = new AggregateECSWorld(bus);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initialize with explicit sync bus (for testing or custom initialization).
        /// </summary>
        public static void Initialize(AgentSyncBus syncBus)
        {
            if (_instance != null)
            {
                _instance.Dispose();
            }
            _instance = new AggregateECSWorld(syncBus);
        }

        public void Update(float deltaTime)
        {
            _systems?.Update(deltaTime);
        }

        public void Dispose()
        {
            _systems?.Dispose();
            _world?.Dispose();
            _syncBus = null;
            _instance = null;
        }
    }
}

