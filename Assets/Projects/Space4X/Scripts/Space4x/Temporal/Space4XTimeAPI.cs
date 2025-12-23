using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace Space4X.Temporal
{
    /// <summary>
    /// Space4X-specific wrappers for the core time control API.
    /// </summary>
    public static class Space4XTimeAPI
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

        public static Entity SpawnLocalTimeField(
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
            var fieldEntity = entityManager.CreateEntity();

            uint currentTick = 0;
            var timeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>());
            if (!timeQuery.IsEmptyIgnoreFilter)
            {
                currentTick = timeQuery.GetSingleton<TimeState>().Tick;
            }

            uint fieldId = (uint)(center.GetHashCode() ^ (int)currentTick);
            if (fieldId == 0)
            {
                fieldId = 1;
            }

            entityManager.AddComponentData(fieldEntity, TimeBubbleId.Create(fieldId));

            var bubbleParams = BuildParams(fieldId, mode, scale, 0, priority);
            bubbleParams.DurationTicks = durationTicks;
            bubbleParams.CreatedAtTick = currentTick;
            bubbleParams.SourceEntity = sourceEntity;
            bubbleParams.OwnerPlayerId = ownerPlayerId;
            bubbleParams.AffectsOwnedEntitiesOnly = affectsOwnedEntitiesOnly;
            bubbleParams.AuthorityPolicy = ownerPlayerId == TimePlayerIds.SinglePlayer
                ? TimeBubbleAuthorityPolicy.SinglePlayerOnly
                : TimeBubbleAuthorityPolicy.LocalPlayerOnly;

            entityManager.AddComponentData(fieldEntity, bubbleParams);
            entityManager.AddComponentData(fieldEntity, TimeBubbleVolume.CreateSphere(center, radius));

            return fieldEntity;
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
