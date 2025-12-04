using PureDOTS.Runtime.Platform;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Platform
{
    /// <summary>
    /// Manages launch/recall operations for craft/drones/swarms from carriers.
    /// Rate-limited by LaunchRate/RecoveryRate. Handles orphaned craft.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct HangarOpsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (hangarBays, assignments, kind, entity) in SystemAPI.Query<DynamicBuffer<HangarBay>, DynamicBuffer<HangarAssignment>, RefRO<PlatformKind>>().WithEntityAccess())
            {
                if ((kind.ValueRO.Flags & PlatformFlags.IsCarrier) == 0)
                {
                    continue;
                }

                ProcessHangarOperations(
                    ref state,
                    ref ecb,
                    entity,
                    hangarBays,
                    assignments,
                    timeState.Tick);
            }

            CheckOrphanedCraft(ref state, ref ecb);
        }

        [BurstCompile]
        private static void ProcessHangarOperations(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity carrierEntity,
            DynamicBuffer<HangarBay> hangarBays,
            DynamicBuffer<HangarAssignment> assignments,
            uint currentTick)
        {
            for (int bayIndex = 0; bayIndex < hangarBays.Length; bayIndex++)
            {
                var bay = hangarBays[bayIndex];
                
                if (bay.OccupiedSlots >= bay.Capacity)
                {
                    continue;
                }

                var availableSlots = bay.Capacity - bay.OccupiedSlots - bay.ReservedSlots;
                if (availableSlots <= 0)
                {
                    continue;
                }

                var launchCount = 0;
                var maxLaunches = (int)math.floor(bay.LaunchRate);

                var assignmentsToRemove = new NativeList<int>(8, Allocator.Temp);
                for (int i = 0; i < assignments.Length && launchCount < maxLaunches && launchCount < availableSlots; i++)
                {
                    var assignment = assignments[i];
                    
                    if (assignment.HangarIndex != bayIndex)
                    {
                        continue;
                    }

                    if (!SystemAPI.Exists(assignment.SubPlatform))
                    {
                        assignmentsToRemove.Add(i);
                        continue;
                    }

                    if (SystemAPI.HasComponent<PlatformKind>(assignment.SubPlatform))
                    {
                        var subKind = SystemAPI.GetComponent<PlatformKind>(assignment.SubPlatform);
                        if ((subKind.Flags & PlatformFlags.Craft) != 0 || (subKind.Flags & PlatformFlags.Drone) != 0)
                        {
                            LaunchSubPlatform(ref state, ref ecb, assignment.SubPlatform, carrierEntity);
                            assignmentsToRemove.Add(i);
                            launchCount++;
                            bay.OccupiedSlots--;
                        }
                    }
                }

                for (int i = assignmentsToRemove.Length - 1; i >= 0; i--)
                {
                    assignments.RemoveAt(assignmentsToRemove[i]);
                }
                assignmentsToRemove.Dispose();

                if (launchCount > 0)
                {
                    hangarBays[bayIndex] = bay;
                }
            }
        }

        [BurstCompile]
        private static void LaunchSubPlatform(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity subPlatform,
            Entity carrier)
        {
            if (SystemAPI.HasComponent<HangarAssignment>(subPlatform))
            {
                ecb.RemoveComponent<HangarAssignment>(subPlatform);
            }
        }

        [BurstCompile]
        private static void CheckOrphanedCraft(ref SystemState state, ref EntityCommandBuffer ecb)
        {
            foreach (var (kind, entity) in SystemAPI.Query<RefRO<PlatformKind>>().WithEntityAccess())
            {
                if ((kind.ValueRO.Flags & (PlatformFlags.Craft | PlatformFlags.Drone)) == 0)
                {
                    continue;
                }

                if (!SystemAPI.HasComponent<HangarAssignment>(entity))
                {
                    continue;
                }

                var assignment = SystemAPI.GetComponent<HangarAssignment>(entity);
                
                if (!SystemAPI.Exists(assignment.SubPlatform))
                {
                    if (SystemAPI.HasComponent<PlatformKind>(entity))
                    {
                        var platformKind = SystemAPI.GetComponent<PlatformKind>(entity);
                        platformKind.Flags |= PlatformFlags.IsDisposable;
                        ecb.SetComponent(entity, platformKind);
                    }
                }
            }
        }
    }
}

