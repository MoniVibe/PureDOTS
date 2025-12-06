using Unity.Entities;
using Unity.Profiling;
using PureDOTS.AI.MindECS;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Managed system that updates DefaultEcs world on schedule.
    /// Runs at configurable cadence (2-5 Hz default) after fixed-step simulation.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public sealed partial class MindECSUpdateSystem : SystemBase
    {
        private static readonly ProfilerMarker UpdateMarker = new("MindECSUpdateSystem.Update");

        private float _lastUpdateTime;
        private const float MinUpdateInterval = 0.2f; // 5 Hz max
        private const float MaxUpdateInterval = 1.0f; // 1 Hz min
        private float _currentUpdateInterval = 0.25f; // 4 Hz default

        protected override void OnCreate()
        {
            // Initialize MindECSWorld if not already initialized
            var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
            if (bus != null)
            {
                MindECSWorld.Initialize(bus);
            }
            else
            {
                // Fallback: create world without bus (will be updated when coordinator is ready)
                _ = MindECSWorld.Instance;
            }

            _lastUpdateTime = 0f;
        }

        protected override void OnUpdate()
        {
            using (UpdateMarker.Auto())
            {
                var timeState = SystemAPI.GetSingleton<TimeState>();
                if (timeState.IsPaused)
                {
                    return;
                }

                var rewindState = SystemAPI.GetSingleton<RewindState>();
                // Skip Mind ECS updates during rewind (non-deterministic layer)
                if (rewindState.Mode != RewindMode.Record)
                {
                    return;
                }

                var currentTime = (float)SystemAPI.Time.ElapsedTime;
                
                // Throttle updates based on configured interval
                if (currentTime - _lastUpdateTime < _currentUpdateInterval)
                {
                    return;
                }

                var deltaTime = currentTime - _lastUpdateTime;
                _lastUpdateTime = currentTime;

                // Update DefaultEcs world
                var mindWorld = MindECSWorld.Instance;
                if (mindWorld != null)
                {
                    mindWorld.Update(deltaTime);
                }
            }
        }

        protected override void OnDestroy()
        {
            // Note: We don't dispose MindECSWorld here as it's a singleton
            // It will be disposed when the Unity world is destroyed
        }
    }
}

