using Godgame.Runtime;
using PureDOTS.Runtime.Hand;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RainMiracleCommandBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RainMiracleCommandQueue>());
            Entity queueEntity;
            if (query.IsEmptyIgnoreFilter)
            {
                queueEntity = entityManager.CreateEntity(typeof(RainMiracleCommandQueue));
            }
            else
            {
                queueEntity = query.GetSingletonEntity();
            }

            if (!entityManager.HasBuffer<RainMiracleCommand>(queueEntity))
            {
                entityManager.AddBuffer<RainMiracleCommand>(queueEntity);
            }

            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state) { }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(HandSystemGroup))]
    public partial struct RainMiracleSystem : ISystem
    {
        private EntityQuery _commandQuery;
        private TimeAwareController _controller;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _commandQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RainMiracleCommandQueue>()
                .WithAllRW<RainMiracleCommand>());

            state.RequireForUpdate(_commandQuery);
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _controller = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_controller.TryBegin(timeState, rewindState, out var context))
            {
                return;
            }

            if (!(context.IsRecordPhase || context.IsCatchUpPhase))
            {
                return;
            }

            if (_commandQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entityManager = state.EntityManager;
            var queueEntity = _commandQuery.GetSingletonEntity();
            var commands = entityManager.GetBuffer<RainMiracleCommand>(queueEntity);
            if (commands.Length == 0)
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                if (command.RainCloudPrefab == Entity.Null)
                {
                    continue;
                }

                double elapsed = SystemAPI.Time.ElapsedTime;
                uint seed = command.Seed != 0 ? command.Seed : (uint)math.max(1, (int)(elapsed * 1000.0) + i + 1);
                var random = Unity.Mathematics.Random.CreateFromIndex(seed);
                float radius = math.max(0f, command.Radius);

                for (int c = 0; c < command.CloudCount; c++)
                {
                    float angle = random.NextFloat(0f, math.PI * 2f);
                    float distance = radius > 0f ? random.NextFloat(0f, radius) : 0f;
                    float3 offset = new float3(math.cos(angle) * distance, command.HeightOffset, math.sin(angle) * distance);
                    float3 position = command.Center + offset;

                    var cloud = ecb.Instantiate(command.RainCloudPrefab);
                    ecb.SetComponent(cloud, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
                }
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
            commands.Clear();
        }
    }
}
