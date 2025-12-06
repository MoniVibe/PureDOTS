using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Shared;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Config;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Percept record sent from Body ECS to Mind ECS.
    /// </summary>
    public struct Percept
    {
        public AgentGuid AgentGuid;
        public SensorType Type;
        public float Confidence;
        public float3 Source;
        public Entity TargetEntity;
        public uint TickNumber;
    }

    /// <summary>
    /// Managed system that collects SensorReadingBuffer data from PureDOTS entities
    /// and serializes to Percept records for Mind ECS consumption.
    /// Runs every 250ms (configurable via CognitiveTickProfile).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Systems.AI.VisionSystem))]
    [UpdateAfter(typeof(Systems.AI.SmellSystem))]
    [UpdateAfter(typeof(Systems.AI.HearingSystem))]
    [UpdateAfter(typeof(Systems.AI.RadarSystem))]
    [UpdateBefore(typeof(BodyToMindSyncSystem))]
    public sealed partial class PerceptionBridgeSystem : SystemBase
    {
        private float _lastSyncTime;
        private const float DefaultSyncInterval = 0.25f; // 250ms

        protected override void OnCreate()
        {
            _lastSyncTime = 0f;
            RequireForUpdate<AgentSyncState>();
            RequireForUpdate<TickTimeState>();
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Use configurable interval if CognitiveTickProfile exists, otherwise use default
            float syncInterval = DefaultSyncInterval;
            // TODO: Read from CognitiveTickProfile if available
            
            if (currentTime - _lastSyncTime < syncInterval)
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

            // Collect sensor readings in Burst job
            var entityQuery = GetEntityQuery(
                ComponentType.ReadOnly<AgentSyncId>(),
                ComponentType.ReadOnly<SensorReadingBuffer>());

            var percepts = new NativeList<Percept>(entityQuery.CalculateEntityCount() * 4, Allocator.TempJob);
            var syncIdLookup = GetComponentLookup<AgentSyncId>(true);

            var collectJob = new CollectSensorReadingsJob
            {
                Percepts = percepts,
                TickNumber = tickNumber,
                SyncIdLookup = syncIdLookup
            };

            collectJob.ScheduleParallel(entityQuery, Dependency).Complete();
            syncIdLookup.Update(this);

            // Enqueue percepts to bus (managed operation)
            for (int i = 0; i < percepts.Length; i++)
            {
                bus.EnqueuePercept(percepts[i]);
            }

            // Collect influence field data for mean-field influence
            var influenceQuery = GetEntityQuery(typeof(InfluenceFieldData), typeof(AgentSyncId));
            if (!influenceQuery.IsEmpty)
            {
                var influenceData = influenceQuery.ToComponentDataArray<InfluenceFieldData>(Allocator.Temp);
                var influenceSyncIds = influenceQuery.ToComponentDataArray<AgentSyncId>(Allocator.Temp);
                
                // Sync influence field data to Mind ECS via telemetry
                // This enables mean-field coordination
                for (int i = 0; i < influenceData.Length; i++)
                {
                    var data = influenceData[i];
                    var syncId = influenceSyncIds[i];
                    
                    // Influence field data is aggregated telemetry
                    // Can be sent as part of BodyToMindMessage or separate influence message
                }
                
                influenceData.Dispose();
                influenceSyncIds.Dispose();
            }

            percepts.Dispose();
            _lastSyncTime = currentTime;
        }

        [BurstCompile]
        private struct CollectSensorReadingsJob : IJobEntity
        {
            public NativeList<Percept> Percepts;
            public uint TickNumber;
            [ReadOnly] public ComponentLookup<AgentSyncId> SyncIdLookup;

            public void Execute(
                [EntityIndexInQuery] int index,
                Entity entity,
                DynamicBuffer<SensorReadingBuffer> readings)
            {
                // Only process if entity has AgentSyncId (linked to Mind ECS)
                if (!SyncIdLookup.HasComponent(entity))
                {
                    return;
                }

                var syncId = SyncIdLookup[entity];
                if (syncId.MindEntityIndex < 0)
                {
                    return; // Not mapped to Mind ECS
                }

                // Convert sensor readings to percepts
                for (int i = 0; i < readings.Length; i++)
                {
                    var reading = readings[i];
                    
                    // Only include recent readings (within last 10 ticks)
                    if (TickNumber - reading.TickNumber > 10)
                    {
                        continue;
                    }

                    var percept = new Percept
                    {
                        AgentGuid = syncId.Guid,
                        Type = reading.SensorType,
                        Confidence = reading.Confidence,
                        Source = reading.Position,
                        TargetEntity = reading.Target,
                        TickNumber = reading.TickNumber
                    };

                    Percepts.Add(percept);
                }
            }
        }
    }
}

