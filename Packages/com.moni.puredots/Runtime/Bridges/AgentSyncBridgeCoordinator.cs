using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Managed system that coordinates AgentSyncBus between Burst systems and DefaultEcs world.
    /// Provides singleton access to the bus for both Unity Entities and DefaultEcs systems.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public sealed class AgentSyncBridgeCoordinator : ComponentSystemBase
    {
        private static readonly ProfilerMarker InitializeMarker = new("AgentSyncBridgeCoordinator.Initialize");
        private static readonly ProfilerMarker UpdateMarker = new("AgentSyncBridgeCoordinator.Update");

        private AgentSyncBus _syncBus;
        private bool _isInitialized;

        protected override void OnCreate()
        {
            using (InitializeMarker.Auto())
            {
                _syncBus = new AgentSyncBus();
                _isInitialized = true;

                // Create singleton entity for AgentSyncState marker
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponent<AgentSyncState>(entity);

                UnityEngine.Debug.Log("[AgentSyncBridgeCoordinator] Initialized AgentSyncBus.");
            }
        }

        protected override void OnUpdate()
        {
            using (UpdateMarker.Auto())
            {
                // Coordinator doesn't need per-frame updates, but we keep it alive
                // The bus is accessed via GetBus() method
            }
        }

        protected override void OnDestroy()
        {
            if (_syncBus != null)
            {
                _syncBus.Clear();
                _syncBus = null;
                _isInitialized = false;
                UnityEngine.Debug.Log("[AgentSyncBridgeCoordinator] Disposed AgentSyncBus.");
            }
        }

        /// <summary>
        /// Get the AgentSyncBus instance. Returns null if not initialized.
        /// </summary>
        public AgentSyncBus GetBus()
        {
            return _syncBus;
        }

        /// <summary>
        /// Get the AgentSyncBus instance from the active world.
        /// Static accessor for managed systems that can't access ComponentSystemBase.
        /// </summary>
        public static AgentSyncBus GetBusFromWorld(World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            var coordinator = world.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
            return coordinator?.GetBus();
        }

        /// <summary>
        /// Get the AgentSyncBus instance from the default game world.
        /// </summary>
        public static AgentSyncBus GetBusFromDefaultWorld()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            return GetBusFromWorld(world);
        }
    }
}

