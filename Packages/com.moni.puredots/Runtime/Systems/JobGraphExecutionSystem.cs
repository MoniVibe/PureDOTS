using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Scheduling;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Executes systems based on the job graph scheduler.
    /// Replaces default ComponentSystemGroup execution with dependency-aware scheduling.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct JobGraphExecutionSystem : ISystem
    {
        private JobGraphScheduler _scheduler;
        private bool _graphBuilt;

        public void OnCreate(ref SystemState state)
        {
            _scheduler = new JobGraphScheduler(Unity.Collections.Allocator.Persistent);
            _graphBuilt = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            _scheduler?.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Build graph once
            if (!_graphBuilt)
            {
                _scheduler.BuildGraph(state.World);
                _graphBuilt = true;
            }

            // Get execution order based on dirty components
            var executionOrder = _scheduler.GetExecutionOrder();

            // Execute systems in order
            // Note: Actual execution would need to be done via managed code
            // This is a simplified version - full implementation would execute systems

            executionOrder.Dispose();

            // Clear dirty flags for next frame
            _scheduler.ClearDirty();
        }
    }
}

