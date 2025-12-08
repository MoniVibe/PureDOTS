using PureDOTS.Systems;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Builds target packets from spatial queries (max 8 targets).
    /// Uses AoSoA-friendly fixed-size packets.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ReboundSystem))]
    public partial struct TargetPacketSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            if (timeState.IsPaused)
                return;

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var job = new BuildTargetPacketsJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct BuildTargetPacketsJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                in LocalTransform transform,
                in MultiTargetBehaviorTag tag,
                DynamicBuffer<HitBuffer> targetPackets)
            {
                // Simplified: would use SpatialGridSystem for actual target queries
                // For now, this is a placeholder that would be extended with spatial queries
                
                // Clear old packets
                if (targetPackets.Length > 0)
                {
                    targetPackets.Clear();
                }

                // In full implementation:
                // 1. Query spatial grid for nearby entities
                // 2. Filter by combat-relevant tags/components
                // 3. Sort by distance/priority
                // 4. Build TargetPacket with up to 8 targets
                // 5. Store in HitBuffer or dedicated TargetPacket buffer
            }
        }
    }
}

