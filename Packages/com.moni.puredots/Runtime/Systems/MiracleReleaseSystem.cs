using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Consumes miracle release events emitted by DivineHandSystem and dispatches them to the
    /// appropriate effect queues (e.g., rain miracle commands).
    /// Runs after DivineHandSystem so it can react to events in the same frame.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(HandSystemGroup))]
    [UpdateAfter(typeof(DivineHandSystem))]
    public partial struct MiracleReleaseSystem : ISystem
    {
        private EntityQuery _releaseQuery;
        private EntityQuery _rainQueueQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _releaseQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MiracleReleaseEvent>());
            _rainQueueQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RainMiracleCommandQueue, RainMiracleCommand>());
            state.RequireForUpdate(_releaseQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_releaseQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entityManager = state.EntityManager;
            DynamicBuffer<RainMiracleCommand> rainCommands = default;
            bool hasRainQueue = !_rainQueueQuery.IsEmptyIgnoreFilter;
            if (hasRainQueue)
            {
                var queueEntity = _rainQueueQuery.GetSingletonEntity();
                rainCommands = entityManager.GetBuffer<RainMiracleCommand>(queueEntity);
            }

            foreach (var (releaseBuffer, entity) in SystemAPI
                         .Query<DynamicBuffer<MiracleReleaseEvent>>()
                         .WithEntityAccess())
            {
                if (releaseBuffer.Length == 0)
                {
                    continue;
                }

                for (int i = 0; i < releaseBuffer.Length; i++)
                {
                    var release = releaseBuffer[i];
                    switch (release.Type)
                    {
                        case MiracleType.Rain:
                            if (hasRainQueue)
                            {
                                QueueRainMiracle(ref state, release, rainCommands);
                            }
                            break;
                        default:
                            break;
                    }
                }

                releaseBuffer.Clear();
            }
        }

        private void QueueRainMiracle(ref SystemState state, in MiracleReleaseEvent release, DynamicBuffer<RainMiracleCommand> commands)
        {
            var entityManager = state.EntityManager;
            if (release.ConfigEntity == Entity.Null ||
                !entityManager.HasComponent<RainMiracleConfig>(release.ConfigEntity))
            {
                return;
            }

            var config = entityManager.GetComponentData<RainMiracleConfig>(release.ConfigEntity);
            if (config.RainCloudPrefab == Entity.Null)
            {
                return;
            }

            var command = new RainMiracleCommand
            {
                Center = release.Position,
                CloudCount = math.max(1, config.CloudCount),
                Radius = math.max(0f, config.SpawnRadius),
                HeightOffset = config.SpawnHeightOffset,
                RainCloudPrefab = config.RainCloudPrefab,
                Seed = config.Seed != 0 ? config.Seed : (uint)math.abs((int)math.round(release.Position.x * 73856093f + release.Position.z * 19349663f))
            };

            commands.Add(command);
        }
    }
}
