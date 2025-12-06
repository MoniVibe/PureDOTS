using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Feeds BehaviorEvent buffer into PresentationCommandQueue for presentation layer consumption.
    /// Deterministic behavior event sync to presentation layer.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct CombatPresentationBridge : ISystem
    {
        private EntityQuery _presentationQueueQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _presentationQueueQuery = SystemAPI.QueryBuilder()
                .WithAll<PresentationCommandQueueTag, BehaviorEvent>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            // Presentation systems run at frame time, not tick time
            // This bridge reads deterministic behavior events and feeds to presentation

            var job = new BridgeBehaviorEventsJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct BridgeBehaviorEventsJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                in LocalTransform transform,
                DynamicBuffer<BehaviorEvent> behaviorEvents,
                DynamicBuffer<BehaviorEvent> presentationQueue)
            {
                // Copy behavior events to presentation queue
                // Presentation layer consumes these for animation/VFX
                for (int i = 0; i < behaviorEvents.Length; i++)
                {
                    var evt = behaviorEvents[i];
                    
                    // Only bridge recent events (within last few ticks)
                    if (CurrentTick - evt.StartTick < 10)
                    {
                        presentationQueue.Add(evt);
                    }
                }

                // Clear processed events
                behaviorEvents.Clear();
            }
        }
    }
}

