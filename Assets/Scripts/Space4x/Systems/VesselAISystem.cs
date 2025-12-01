using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Space4X.Runtime;
using Space4X.Runtime.Transport;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Space4X.Systems
{
    /// <summary>
    /// AI system for vessels - handles state transitions and capacity-based goal overrides.
    /// Target selection is now handled by the shared AI pipeline (AISensorUpdateSystem -> AIUtilityScoringSystem -> AITaskResolutionSystem).
    /// This system ensures vessels switch between Mining and Returning based on capacity.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PureDOTS.Systems.ResourceSystemGroup))]
    [UpdateAfter(typeof(Space4XVesselAICommandBridgeSystem))]
    [UpdateBefore(typeof(VesselTargetingSystem))]
    public partial struct VesselAISystem : ISystem
    {
        private EntityQuery _vesselQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _vesselQuery = SystemAPI.QueryBuilder()
                .WithAll<VesselAIState, MinerVessel, LocalTransform>()
                .Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (_vesselQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            // Simplified: just handle capacity-based goal overrides and state transitions
            // Target selection is handled by the shared AI pipeline
            var job = new UpdateVesselAIJob
            {
                DeltaTime = timeState.FixedDeltaTime,
                CurrentTick = timeState.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct UpdateVesselAIJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;

            public void Execute(ref VesselAIState aiState, in MinerVessel vessel, in LocalTransform transform)
            {
                aiState.StateTimer += DeltaTime;

                // Capacity-based goal override: if vessel becomes full, force Returning goal
                if (vessel.Load >= vessel.Capacity * 0.95f && aiState.CurrentGoal != VesselAIState.Goal.Returning)
                {
                    aiState.CurrentGoal = VesselAIState.Goal.Returning;
                    aiState.CurrentState = VesselAIState.State.Returning;
                    aiState.StateTimer = 0f;
                    aiState.StateStartTick = CurrentTick;
                }
                // If vessel is not full and goal is Returning, switch to Mining
                else if (vessel.Load < vessel.Capacity * 0.95f && aiState.CurrentGoal == VesselAIState.Goal.Returning && aiState.CurrentState != VesselAIState.State.Mining)
                {
                    aiState.CurrentGoal = VesselAIState.Goal.Mining;
                    aiState.CurrentState = VesselAIState.State.MovingToTarget;
                    aiState.StateTimer = 0f;
                    aiState.StateStartTick = CurrentTick;
                }
                // State transition: when moving to target and close enough, transition to mining
                // (Actual distance check happens in VesselGatheringSystem, but we can set state here)
                else if (aiState.CurrentState == VesselAIState.State.MovingToTarget && 
                         aiState.TargetEntity != Entity.Null &&
                         vessel.Load < vessel.Capacity * 0.95f &&
                         aiState.CurrentGoal == VesselAIState.Goal.Mining)
                {
                    // VesselGatheringSystem will handle the actual transition when close enough
                    // This is just a placeholder - the real transition happens in VesselGatheringSystem
                }
            }
        }
    }
}

