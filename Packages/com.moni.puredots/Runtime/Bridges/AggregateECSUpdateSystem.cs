using Unity.Entities;
using Unity.Profiling;
using PureDOTS.AI.AggregateECS;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Managed system that updates AggregateECSWorld on schedule.
    /// Runs at 1 Hz (configurable, slower than Mind ECS at 2-5 Hz).
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(AggregateBridgeSystem))]
    public sealed partial class AggregateECSUpdateSystem : SystemBase
    {
        private static readonly ProfilerMarker UpdateMarker = new("AggregateECSUpdateSystem.Update");

        private float _lastUpdateTime;
        private const float UpdateInterval = 1.0f; // 1 Hz

        protected override void OnCreate()
        {
            _lastUpdateTime = 0f;
            RequireForUpdate<TickTimeState>();
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
                // Skip Aggregate ECS updates during rewind (non-deterministic layer)
                if (rewindState.Mode != RewindMode.Record)
                {
                    return;
                }

                var currentTime = (float)SystemAPI.Time.ElapsedTime;
                
                // Throttle updates (1 Hz)
                if (currentTime - _lastUpdateTime < UpdateInterval)
                {
                    return;
                }

                var deltaTime = currentTime - _lastUpdateTime;
                _lastUpdateTime = currentTime;

                // Update AggregateECSWorld
                var aggregateWorld = AggregateECSWorld.Instance;
                if (aggregateWorld != null)
                {
                    aggregateWorld.Update(deltaTime);
                }
            }
        }

        protected override void OnDestroy()
        {
            // Note: We don't dispose AggregateECSWorld here as it's a singleton
            // It will be disposed when the Unity world is destroyed
        }
    }
}

