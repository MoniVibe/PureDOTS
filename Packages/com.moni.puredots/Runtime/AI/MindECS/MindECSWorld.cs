using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.MindECS.Systems;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;

namespace PureDOTS.AI.MindECS
{
    /// <summary>
    /// Manages the DefaultEcs world for cognitive AI layer.
    /// Singleton instance accessible from Unity Entities systems.
    /// </summary>
    public class MindECSWorld
    {
        private static MindECSWorld _instance;
        private World _world;
        private SequentialSystem<ISystem<World>> _systems;
        private AgentSyncBus _syncBus;

        public World World => _world;
        public SequentialSystem<ISystem<World>> Systems => _systems;

        private MindECSWorld(AgentSyncBus syncBus = null)
        {
            _world = new World();
            _systems = new SequentialSystem<ISystem<World>>();
            _syncBus = syncBus;

            // Initialize cognitive system with sync bus
            if (_syncBus != null)
            {
                var cognitiveSystem = new CognitiveSystem(_world, _syncBus);
                _systems.Add(cognitiveSystem);
            }
        }

        public static MindECSWorld Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to get sync bus from Unity world if available
                    var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
                    _instance = new MindECSWorld(bus);
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
            _instance = new MindECSWorld(syncBus);
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

