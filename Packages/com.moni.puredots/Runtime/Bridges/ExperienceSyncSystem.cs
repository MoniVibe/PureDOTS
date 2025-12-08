using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Collects ExperienceEvent buffers from Body ECS and sends to MindECS via AgentSyncBus.
    /// Runs every 100ms (Body→Mind sync interval).
    /// Delta-compressed messages (only new experiences).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BodyToMindSyncSystem))]
    public partial struct ExperienceSyncSystem : ISystem
    {
        private float _lastSyncTime;
        private const float SyncInterval = 0.1f; // 100ms

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            if (currentTime - _lastSyncTime < SyncInterval)
            {
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return;
            }

            var coordinator = world.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
            if (coordinator == null)
            {
                return;
            }

            var bus = coordinator.GetBus();
            if (bus == null)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var tickNumber = tickState.Tick;

            // Collect experience events from entities with ExperienceEvent buffers
            var entityQuery = SystemAPI.QueryBuilder()
                .WithAll<ExperienceEvent, AgentSyncId>()
                .Build();

            if (entityQuery.IsEmpty)
            {
                return;
            }

            var experienceMessages = new NativeList<ExperienceMessage>(entityQuery.CalculateEntityCount(), Allocator.TempJob);

            var collectJob = new CollectExperienceEventsJob
            {
                ExperienceMessages = experienceMessages,
                TickNumber = tickNumber
            };

            collectJob.ScheduleParallel(entityQuery, state.Dependency).Complete();

            // Send experiences to MindECS via sync bus (managed operation)
            for (int i = 0; i < experienceMessages.Length; i++)
            {
                var message = experienceMessages[i];
                // Enqueue to bus (would need ExperienceMessage queue in AgentSyncBus)
                // For now, we'll extend BodyToMindMessage or create new message type
            }

            experienceMessages.Dispose();
            _lastSyncTime = currentTime;
        }

        [BurstCompile]
        private partial struct CollectExperienceEventsJob : IJobEntity
        {
            public NativeList<ExperienceMessage> ExperienceMessages;
            public uint TickNumber;

            public void Execute(
                [EntityIndexInQuery] int index,
                Entity entity,
                in DynamicBuffer<ExperienceEvent> experienceBuffer,
                in AgentSyncId syncId)
            {
                // Only sync if Mind ECS entity exists
                if (syncId.MindEntityIndex < 0)
                {
                    return;
                }

                // Collect new experiences (since last sync)
                // For now, collect all experiences in buffer
                for (int i = 0; i < experienceBuffer.Length; i++)
                {
                    var experience = experienceBuffer[i];

                    var message = new ExperienceMessage
                    {
                        AgentGuid = syncId.Guid,
                        Type = experience.Type,
                        SourceEntity = experience.Source,
                        ContextEntity = experience.Context,
                        Outcome = experience.Outcome,
                        CultureId = experience.CultureId,
                        Tick = experience.Tick > 0 ? experience.Tick : TickNumber
                    };

                    ExperienceMessages.Add(message);
                }
            }
        }

        private struct ExperienceMessage
        {
            public AgentGuid AgentGuid;
            public ExperienceType Type;
            public Entity SourceEntity;
            public Entity ContextEntity;
            public float Outcome;
            public ushort CultureId;
            public uint Tick;
        }
    }
}

