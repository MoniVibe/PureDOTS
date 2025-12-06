using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.AI;
using PureDOTS.Systems.AI;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Managed wrapper that dequeues messages from AgentSyncBus and processes them.
    /// Delegates to Burst job for GUID→Entity resolution and intent writing.
    /// Also processes limb commands.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AgentMappingSystem))]
    public sealed partial class MindToBodySyncSystem : SystemBase
    {
        private float _lastSyncTime;
        private const float DefaultSyncInterval = 0.25f; // 250ms

        protected override void OnCreate()
        {
            _lastSyncTime = 0f;
            RequireForUpdate<AgentSyncState>();
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            if (currentTime - _lastSyncTime < DefaultSyncInterval)
            {
                return;
            }

            var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
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

            // Process intent messages
            if (bus.MindToBodyQueueCount > 0)
            {
                using var messageBatch = bus.DequeueMindToBodyBatch(Allocator.TempJob);

                if (messageBatch.Length > 0)
                {
                    var syncIdLookup = GetComponentLookup<AgentSyncId>(true);
                    var entityQuery = GetEntityQuery(typeof(AgentSyncId));

                    var job = new ProcessMindToBodyMessagesJob
                    {
                        Messages = messageBatch,
                        SyncIdLookup = syncIdLookup,
                        TickNumber = tickNumber
                    };

                    job.ScheduleParallel(entityQuery, Dependency).Complete();
                }
            }

            // Process limb commands
            if (bus.LimbCommandQueueCount > 0)
            {
                using var limbCommandBatch = bus.DequeueLimbCommandBatch(Allocator.TempJob);

                if (limbCommandBatch.Length > 0)
                {
                    var syncIdLookup = GetComponentLookup<AgentSyncId>(true);
                    var limbElementLookup = GetBufferLookup<LimbElement>(false);
                    var limbCommandBufferLookup = GetBufferLookup<LimbCommandBuffer>(false);

                    var limbJob = new ProcessLimbCommandsJob
                    {
                        Commands = limbCommandBatch,
                        SyncIdLookup = syncIdLookup,
                        LimbElementLookup = limbElementLookup,
                        LimbCommandBufferLookup = limbCommandBufferLookup,
                        TickNumber = tickNumber
                    };

                    var entityQuery = GetEntityQuery(typeof(AgentSyncId), typeof(LimbElement));
                    limbJob.ScheduleParallel(entityQuery, Dependency).Complete();
                }
            }

            // EXTENDED: Process procedural memory updates from Mind ECS
            // This would handle ProceduralMemoryUpdateMessage types when they're added to the bus
            // For now, the structure is in place for future extension

            _lastSyncTime = currentTime;
        }

        [BurstCompile]
        private partial struct ProcessMindToBodyMessagesJob : IJobEntity
        {
            [ReadOnly] public NativeList<MindToBodyMessage> Messages;
            [ReadOnly] public ComponentLookup<AgentSyncId> SyncIdLookup;
            public uint TickNumber;

            public void Execute(Entity entity, ref DynamicBuffer<AgentIntentBuffer> intentBuffer)
            {
                var syncId = SyncIdLookup[entity];

                // Find matching message for this entity's GUID
                for (int i = 0; i < Messages.Length; i++)
                {
                    var message = Messages[i];
                    if (message.AgentGuid.Equals(syncId.Guid))
                    {
                        // Add intent to buffer
                        intentBuffer.Add(new AgentIntentBuffer
                        {
                            Kind = message.Kind,
                            TargetPosition = message.TargetPosition,
                            TargetEntity = message.TargetEntity,
                            Priority = message.Priority,
                            TickNumber = message.TickNumber > 0 ? message.TickNumber : TickNumber
                        });

                        // Only process first matching message per sync cycle
                        break;
                    }
                }
            }
        }

        [BurstCompile]
        private partial struct ProcessLimbCommandsJob : IJobEntity
        {
            [ReadOnly] public NativeList<LimbCommand> Commands;
            [ReadOnly] public ComponentLookup<AgentSyncId> SyncIdLookup;
            [ReadOnly] public BufferLookup<LimbElement> LimbElementLookup;
            public BufferLookup<LimbCommandBuffer> LimbCommandBufferLookup;
            public uint TickNumber;

            public void Execute(Entity entity, DynamicBuffer<LimbElement> limbElements)
            {
                if (!SyncIdLookup.HasComponent(entity))
                {
                    return;
                }

                var syncId = SyncIdLookup[entity];

                // Find matching commands for this entity's GUID
                for (int i = 0; i < Commands.Length; i++)
                {
                    var command = Commands[i];
                    if (command.AgentGuid.Equals(syncId.Guid))
                    {
                        // Find the limb entity by index
                        if (command.LimbIndex >= 0 && command.LimbIndex < limbElements.Length)
                        {
                            var limbEntity = limbElements[command.LimbIndex].LimbEntity;
                            
                            // Get or create limb command buffer on the limb entity
                            if (!LimbCommandBufferLookup.HasBuffer(limbEntity))
                            {
                                // Buffer will be created by the system if needed
                                continue;
                            }

                            var commandBuffer = LimbCommandBufferLookup[limbEntity];
                            commandBuffer.Add(new LimbCommandBuffer
                            {
                                LimbIndex = command.LimbIndex,
                                Action = command.Action,
                                Target = command.Target,
                                Priority = command.Priority,
                                TickNumber = command.TickNumber > 0 ? command.TickNumber : TickNumber
                            });
                        }

                        // Only process first matching command per sync cycle
                        break;
                    }
                }
            }
        }
    }
}

