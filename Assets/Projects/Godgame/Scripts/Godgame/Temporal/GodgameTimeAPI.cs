using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace Godgame.Temporal
{
    /// <summary>
    /// Godgame-specific wrappers for the core time control API.
    /// </summary>
    public static class GodgameTimeAPI
    {
        public static bool SetGlobalTimeSpeed(World world, float speed)
        {
            return SetGlobalTimeSpeed(world, speed, TimePlayerIds.SinglePlayer);
        }

        public static bool SetGlobalTimeSpeed(World world, float speed, byte playerId)
        {
            if (!TryGetCommandBuffer(world, out var commandBuffer))
            {
                return false;
            }

            commandBuffer.Add(new TimeControlCommand
            {
                Type = TimeControlCommandType.SetSpeed,
                FloatParam = math.clamp(speed, TimeControlLimits.DefaultMinSpeed, TimeControlLimits.DefaultMaxSpeed),
                Scope = TimeControlScope.Global,
                Source = TimeControlSource.Player,
                PlayerId = playerId,
                Priority = 100
            });

            return true;
        }

        public static bool RequestGlobalRewind(World world, uint targetTick, uint sourceId)
        {
            return RequestGlobalRewind(world, targetTick, sourceId, TimePlayerIds.SinglePlayer);
        }

        public static bool RequestGlobalRewind(World world, uint targetTick, uint sourceId, byte playerId)
        {
            if (!TryGetCommandBuffer(world, out var commandBuffer))
            {
                return false;
            }

            commandBuffer.Add(new TimeControlCommand
            {
                Type = TimeControlCommandType.StartRewind,
                UintParam = targetTick,
                Scope = TimeControlScope.Global,
                Source = TimeControlSource.Player,
                PlayerId = playerId,
                SourceId = sourceId,
                Priority = 100
            });

            return true;
        }

        public static Entity SpawnTimeBubble(
            World world,
            float3 center,
            float radius,
            TimeBubbleMode mode,
            float scale,
            uint durationTicks = 0,
            byte priority = 100,
            Entity sourceEntity = default,
            byte ownerPlayerId = TimePlayerIds.SinglePlayer,
            bool affectsOwnedEntitiesOnly = false)
        {
            if (world == null || !world.IsCreated)
            {
                return Entity.Null;
            }

            var entityManager = world.EntityManager;
            var bubbleEntity = entityManager.CreateEntity();

            uint currentTick = 0;
            var timeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>());
            if (!timeQuery.IsEmptyIgnoreFilter)
            {
                currentTick = timeQuery.GetSingleton<TimeState>().Tick;
            }

            uint bubbleId = (uint)(center.GetHashCode() ^ (int)currentTick);
            if (bubbleId == 0)
            {
                bubbleId = 1;
            }

            entityManager.AddComponentData(bubbleEntity, TimeBubbleId.Create(bubbleId));

            var bubbleParams = BuildParams(bubbleId, mode, scale, 0, priority);
            bubbleParams.DurationTicks = durationTicks;
            bubbleParams.CreatedAtTick = currentTick;
            bubbleParams.SourceEntity = sourceEntity;
            bubbleParams.OwnerPlayerId = ownerPlayerId;
            bubbleParams.AffectsOwnedEntitiesOnly = affectsOwnedEntitiesOnly;
            bubbleParams.AuthorityPolicy = ownerPlayerId == TimePlayerIds.SinglePlayer
                ? TimeBubbleAuthorityPolicy.SinglePlayerOnly
                : TimeBubbleAuthorityPolicy.LocalPlayerOnly;

            entityManager.AddComponentData(bubbleEntity, bubbleParams);
            entityManager.AddComponentData(bubbleEntity, TimeBubbleVolume.CreateSphere(center, radius));

            return bubbleEntity;
        }

        private static bool TryGetCommandBuffer(World world, out DynamicBuffer<TimeControlCommand> commandBuffer)
        {
            commandBuffer = default;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            var entityManager = world.EntityManager;
            var rewindQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>());
            if (rewindQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            var rewindEntity = rewindQuery.GetSingletonEntity();
            if (!entityManager.HasBuffer<TimeControlCommand>(rewindEntity))
            {
                entityManager.AddBuffer<TimeControlCommand>(rewindEntity);
            }

            commandBuffer = entityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            return true;
        }

        private static TimeBubbleParams BuildParams(uint bubbleId, TimeBubbleMode mode, float scale, int rewindOffsetTicks, byte priority)
        {
            return mode switch
            {
                TimeBubbleMode.Pause => TimeBubbleParams.CreatePause(bubbleId, priority),
                TimeBubbleMode.Stasis => TimeBubbleParams.CreateStasis(bubbleId, priority),
                TimeBubbleMode.Rewind => TimeBubbleParams.CreateRewind(bubbleId, rewindOffsetTicks, priority),
                _ => TimeBubbleParams.CreateScale(bubbleId, scale, priority)
            };
        }
    }
}
