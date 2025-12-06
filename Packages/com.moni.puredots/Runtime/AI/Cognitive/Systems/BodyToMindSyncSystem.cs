using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.AI.Cognitive.Systems
{
    /// <summary>
    /// System that syncs procedural memory and context data from Body ECS to Mind ECS.
    /// Sends context hashes and action outcomes to Mind ECS for cognitive processing.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct BodyToMindSyncSystem : ISystem
    {
        private const float SyncInterval = 0.1f; // 100ms sync interval
        private float _lastSyncTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<AgentSyncState>();
            _lastSyncTime = 0f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record && rewind.Mode != RewindMode.CatchUp)
            {
                return;
            }

            var tickTime = SystemAPI.GetSingleton<TickTimeState>();
            if (tickTime.IsPaused)
            {
                return;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastSyncTime < SyncInterval)
            {
                return;
            }

            _lastSyncTime = currentTime;

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

            var syncIdLookup = SystemAPI.GetComponentLookup<AgentSyncId>(true);
            syncIdLookup.Update(ref state);

            var job = new BodyToMindSyncJob
            {
                SyncIdLookup = syncIdLookup,
                CurrentTick = tickTime.Tick,
                Bus = bus
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct BodyToMindSyncJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<AgentSyncId> SyncIdLookup;
            public uint CurrentTick;
            public AgentSyncBus Bus; // Note: Managed type, will need special handling

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex,
                in ContextHash contextHash,
                in ProceduralMemory memory,
                in LocalTransform transform)
            {
                if (!SyncIdLookup.HasComponent(entity))
                {
                    return;
                }

                var syncId = SyncIdLookup[entity];

                // Send context perception message
                if (contextHash.Hash != 0 && contextHash.LastComputedTick == CurrentTick)
                {
                    var contextMsg = new ContextPerceptionMessage
                    {
                        AgentGuid = syncId.Guid,
                        ContextHash = contextHash.Hash,
                        TerrainType = (byte)contextHash.TerrainType,
                        ObstacleTag = (byte)contextHash.ObstacleTag,
                        GoalType = (byte)contextHash.GoalType,
                        TickNumber = CurrentTick
                    };

                    // Note: In full implementation, would enqueue to bus
                    // For now, this is a placeholder that shows the sync structure
                }

                // Send action outcomes when actions complete
                // This would be called by action execution systems when outcomes are known
            }
        }
    }
}

