using Godgame.Resources;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Ensures aggregate pile command buffers exist for gameplay systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AggregatePileCommandBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AggregatePileConfig>();

            if (!SystemAPI.TryGetSingletonEntity<AggregatePileCommandState>(out var commandEntity))
            {
                commandEntity = state.EntityManager.CreateEntity(typeof(AggregatePileCommandState));
                state.EntityManager.AddBuffer<AggregatePileAddCommand>(commandEntity);
                state.EntityManager.AddBuffer<AggregatePileTakeCommand>(commandEntity);
                state.EntityManager.AddBuffer<AggregatePileCommandResult>(commandEntity);
            }
            else
            {
                EnsureBuffer<AggregatePileAddCommand>(ref state, commandEntity);
                EnsureBuffer<AggregatePileTakeCommand>(ref state, commandEntity);
                EnsureBuffer<AggregatePileCommandResult>(ref state, commandEntity);
            }
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        private static void EnsureBuffer<T>(ref SystemState state, Entity entity) where T : unmanaged, IBufferElementData
        {
            if (!state.EntityManager.HasBuffer<T>(entity))
            {
                state.EntityManager.AddBuffer<T>(entity);
            }
        }
    }

    /// <summary>
    /// Processes aggregate pile commands, handles merge/split logic, and updates visuals.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AggregatePileSystem : ISystem
    {
        private EntityQuery _pileQuery;
        private ComponentLookup<AggregatePile> _pileLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<AggregatePileVisual> _visualLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AggregatePileConfig>();
            state.RequireForUpdate<AggregatePileCommandState>();
            state.RequireForUpdate<TimeState>();

            _pileQuery = SystemAPI.QueryBuilder()
                .WithAll<AggregatePile, LocalTransform>()
                .Build();

            _pileLookup = state.GetComponentLookup<AggregatePile>(isReadOnly: false);
            _transformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: false);
            _visualLookup = state.GetComponentLookup<AggregatePileVisual>(isReadOnly: false);
        }

        public void OnUpdate(ref SystemState state)
        {
            _pileLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _visualLookup.Update(ref state);

            var config = SystemAPI.GetSingleton<AggregatePileConfig>();
            var time = SystemAPI.GetSingleton<TimeState>();
            var deltaTime = time.FixedDeltaTime * math.max(0.0001f, time.CurrentSpeedMultiplier <= 0f ? 1f : time.CurrentSpeedMultiplier);
            var elapsedTime = time.Tick * deltaTime;

            var runtime = SystemAPI.GetSingletonRW<AggregatePileRuntimeState>();

            var commandEntity = SystemAPI.GetSingletonEntity<AggregatePileCommandState>();
            var addBuffer = state.EntityManager.GetBuffer<AggregatePileAddCommand>(commandEntity);
            var takeBuffer = state.EntityManager.GetBuffer<AggregatePileTakeCommand>(commandEntity);
            var resultBuffer = state.EntityManager.GetBuffer<AggregatePileCommandResult>(commandEntity);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            ProcessTakeCommands(ref state, takeBuffer, resultBuffer, ref config, elapsedTime, ref ecb, runtime);
            ProcessAddCommands(ref state, addBuffer, resultBuffer, ref config, elapsedTime, ref ecb, runtime);

            addBuffer.Clear();
            takeBuffer.Clear();

            if (elapsedTime >= runtime.ValueRO.NextMergeTime)
            {
                RunMergePass(ref state, ref config, ref ecb, elapsedTime, runtime);
                runtime.ValueRW.NextMergeTime = elapsedTime + math.max(0.05f, config.MergeCheckSeconds);
            }

            RunSplitPass(ref state, ref config, ref ecb, elapsedTime, runtime);
            UpdateVisualTargets(ref state, deltaTime);
        }

        private void ProcessTakeCommands(ref SystemState state,
            DynamicBuffer<AggregatePileTakeCommand> takeBuffer,
            DynamicBuffer<AggregatePileCommandResult> resultBuffer,
            ref AggregatePileConfig config,
            float elapsedTime,
            ref EntityCommandBuffer ecb,
            RefRW<AggregatePileRuntimeState> runtime)
        {
            for (var i = 0; i < takeBuffer.Length; i++)
            {
                var command = takeBuffer[i];

                if (!_pileLookup.HasComponent(command.Pile))
                {
                    resultBuffer.Add(new AggregatePileCommandResult
                    {
                        Requester = command.Requester,
                        Pile = command.Pile,
                        ResourceTypeIndex = 0,
                        Amount = 0f,
                        Type = AggregatePileCommandResultType.TakePartial
                    });
                    continue;
                }

                var pile = _pileLookup[command.Pile];
                var previousAmount = pile.Amount;
                var requested = math.max(0f, command.Amount);
                var removed = math.min(requested, pile.Amount);

                pile.Amount -= removed;
                pile.State = AggregatePileState.Growing;
                pile.LastMutationTime = elapsedTime;
                _pileLookup[command.Pile] = pile;

                if (_transformLookup.HasComponent(command.Pile))
                {
                    var transform = _transformLookup[command.Pile];
                    transform.Scale = AggregatePileVisualUtility.CalculateScale(pile.Amount);
                    _transformLookup[command.Pile] = transform;
                }

                if (pile.Amount <= config.ConservationEpsilon)
                {
                    ecb.DestroyEntity(command.Pile);
                    runtime.ValueRW.ActivePiles = math.max(0, runtime.ValueRO.ActivePiles - 1);
                }

                resultBuffer.Add(new AggregatePileCommandResult
                {
                    Requester = command.Requester,
                    Pile = command.Pile,
                    ResourceTypeIndex = pile.ResourceTypeIndex,
                    Amount = removed,
                    Type = removed >= requested - config.ConservationEpsilon
                        ? AggregatePileCommandResultType.TakeCompleted
                        : AggregatePileCommandResultType.TakePartial
                });
            }
        }

        private void ProcessAddCommands(ref SystemState state,
            DynamicBuffer<AggregatePileAddCommand> addBuffer,
            DynamicBuffer<AggregatePileCommandResult> resultBuffer,
            ref AggregatePileConfig config,
            float elapsedTime,
            ref EntityCommandBuffer ecb,
            RefRW<AggregatePileRuntimeState> runtime)
        {
            for (var i = 0; i < addBuffer.Length; i++)
            {
                var command = addBuffer[i];
                var remaining = math.max(0f, command.Amount);
                if (remaining < config.MinSpawnAmount)
                {
                    continue;
                }

                var accepted = 0f;

                if ((command.Flags & AggregatePileAddFlags.ForceNewPile) == 0)
                {
                    var targetEntity = FindBestPile(ref state, command, ref config);
                    if (targetEntity != Entity.Null)
                    {
                        var added = AddToPile(targetEntity, command.ResourceTypeIndex, remaining, elapsedTime, ref config);
                        accepted += added;
                        remaining -= added;
                    }
                }

                while (remaining > config.MinSpawnAmount && CanSpawnPile(ref state, ref config, runtime))
                {
                    var spawnAmount = math.min(remaining, config.DefaultMaxCapacity);
                    var newEntity = SpawnPile(ref state, command.ResourceTypeIndex, spawnAmount, command.Position, ref config, ref ecb, elapsedTime);
                    accepted += spawnAmount;
                    remaining -= spawnAmount;
                }

                var resultType = remaining <= config.MinSpawnAmount
                    ? AggregatePileCommandResultType.AddAccepted
                    : AggregatePileCommandResultType.AddRejected;

                resultBuffer.Add(new AggregatePileCommandResult
                {
                    Requester = command.Requester,
                    Pile = Entity.Null,
                    ResourceTypeIndex = command.ResourceTypeIndex,
                    Amount = accepted,
                    Type = resultType
                });
            }
        }

        private Entity FindBestPile(ref SystemState state, AggregatePileAddCommand command, ref AggregatePileConfig config)
        {
            if (command.PreferredPile != Entity.Null && _pileLookup.HasComponent(command.PreferredPile))
            {
                var preferred = _pileLookup[command.PreferredPile];
                if (preferred.ResourceTypeIndex == command.ResourceTypeIndex)
                {
                    return command.PreferredPile;
                }
            }

            Entity bestEntity = Entity.Null;
            var bestDistance = float.MaxValue;
            var requiredRadius = command.MergeRadiusOverride > 0f
                ? command.MergeRadiusOverride
                : config.MergeRadius;
            var radiusSq = requiredRadius * requiredRadius;

            foreach (var (pileRef, transformRef, entity) in SystemAPI
                         .Query<RefRO<AggregatePile>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var pile = pileRef.ValueRO;
                if (pile.ResourceTypeIndex != command.ResourceTypeIndex)
                {
                    continue;
                }

                var distanceSq = math.distancesq(transformRef.ValueRO.Position, command.Position);
                if (distanceSq > radiusSq)
                {
                    continue;
                }

                var availableCapacity = math.max(0f, pile.MaxCapacity - pile.Amount);
                if (availableCapacity <= 0f)
                {
                    continue;
                }

                if (distanceSq < bestDistance)
                {
                    bestDistance = distanceSq;
                    bestEntity = entity;
                }
            }

            return bestEntity;
        }

        private float AddToPile(Entity entity, ushort resourceTypeIndex, float amount, float elapsedTime, ref AggregatePileConfig config)
        {
            if (!_pileLookup.HasComponent(entity))
            {
                return 0f;
            }

            var pile = _pileLookup[entity];
            if (pile.ResourceTypeIndex != resourceTypeIndex)
            {
                return 0f;
            }

            var capacityRemaining = math.max(0f, pile.MaxCapacity - pile.Amount);
            var accepted = math.min(capacityRemaining, amount);
            if (accepted <= config.ConservationEpsilon)
            {
                return 0f;
            }

            pile.Amount += accepted;
            pile.State = AggregatePileState.Growing;
            pile.LastMutationTime = elapsedTime;
            _pileLookup[entity] = pile;

            if (_transformLookup.HasComponent(entity))
            {
                var transform = _transformLookup[entity];
                transform.Scale = AggregatePileVisualUtility.CalculateScale(pile.Amount);
                _transformLookup[entity] = transform;
            }

            return accepted;
        }

        private bool CanSpawnPile(ref SystemState state, ref AggregatePileConfig config, RefRW<AggregatePileRuntimeState> runtime)
        {
            if (runtime.ValueRO.ActivePiles >= config.MaxActivePiles)
            {
                return false;
            }

            runtime.ValueRW.ActivePiles++;
            return true;
        }

        private Entity SpawnPile(ref SystemState state, ushort resourceTypeIndex, float amount, float3 position, ref AggregatePileConfig config, ref EntityCommandBuffer ecb, float elapsedTime)
        {
            var entity = ecb.CreateEntity();
            var pile = new AggregatePile
            {
                ResourceTypeIndex = resourceTypeIndex,
                Amount = amount,
                MaxCapacity = config.DefaultMaxCapacity,
                MergeRadius = config.MergeRadius,
                LastMutationTime = elapsedTime,
                State = AggregatePileState.Growing
            };

            ecb.AddComponent(entity, pile);
            ecb.AddComponent(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, AggregatePileVisualUtility.CalculateScale(amount)));
            ecb.AddComponent(entity, new AggregatePileVisual
            {
                CurrentScale = AggregatePileVisualUtility.CalculateScale(amount),
                TargetScale = AggregatePileVisualUtility.CalculateScale(amount)
            });

            return entity;
        }

        private void RunMergePass(ref SystemState state, ref AggregatePileConfig config, ref EntityCommandBuffer ecb, float elapsedTime, RefRW<AggregatePileRuntimeState> runtime)
        {
            var entities = _pileQuery.ToEntityArray(Allocator.Temp);
            var transforms = _pileQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            var markedForRemoval = new NativeHashSet<Entity>(entities.Length, Allocator.Temp);

            for (var i = 0; i < entities.Length; i++)
            {
                var entityA = entities[i];
                if (!_pileLookup.HasComponent(entityA))
                {
                    continue;
                }

                var pileA = _pileLookup[entityA];
                var posA = transforms[i].Position;

                for (var j = i + 1; j < entities.Length; j++)
                {
                    var entityB = entities[j];
                    if (entityA == entityB || markedForRemoval.Contains(entityB))
                    {
                        continue;
                    }

                    if (!_pileLookup.HasComponent(entityB))
                    {
                        continue;
                    }

                    var pileB = _pileLookup[entityB];
                    if (pileA.ResourceTypeIndex != pileB.ResourceTypeIndex)
                    {
                        continue;
                    }

                    var posB = transforms[j].Position;
                    var distanceSq = math.distancesq(posA, posB);
                    var radius = math.min(pileA.MergeRadius, pileB.MergeRadius);
                    if (distanceSq > radius * radius)
                    {
                        continue;
                    }

                    var available = math.max(0f, pileA.MaxCapacity - pileA.Amount);
                    var transfer = math.min(available, pileB.Amount);
                    if (transfer <= config.ConservationEpsilon)
                    {
                        continue;
                    }

                    pileA.Amount += transfer;
                    pileA.State = AggregatePileState.Merging;
                    pileA.LastMutationTime = elapsedTime;
                    _pileLookup[entityA] = pileA;

                    pileB.Amount -= transfer;
                    _pileLookup[entityB] = pileB;

                    if (pileB.Amount <= config.ConservationEpsilon)
                    {
                        markedForRemoval.Add(entityB);
                    }

                    UpdateTransformScale(entityA, pileA.Amount);
                    UpdateTransformScale(entityB, pileB.Amount);
                }
            }

            foreach (var entity in markedForRemoval)
            {
                ecb.DestroyEntity(entity);
                runtime.ValueRW.ActivePiles = math.max(0, runtime.ValueRO.ActivePiles - 1);
            }
        }

        private void RunSplitPass(ref SystemState state, ref AggregatePileConfig config, ref EntityCommandBuffer ecb, float elapsedTime, RefRW<AggregatePileRuntimeState> runtime)
        {
            foreach (var (pileRef, transformRef, entity) in SystemAPI
                         .Query<RefRW<AggregatePile>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var pile = pileRef.ValueRO;
                if (pile.Amount <= config.SplitThreshold)
                {
                    continue;
                }

                var overflow = pile.Amount - config.SplitThreshold;
                pile.Amount = config.SplitThreshold;
                pile.State = AggregatePileState.Splitting;
                pile.LastMutationTime = elapsedTime;
                pileRef.ValueRW = pile;

                UpdateTransformScale(entity, pile.Amount);

                if (!CanSpawnPile(ref state, ref config, runtime))
                {
                    continue;
                }

                var outstanding = overflow;
                var spawnOffset = new float3(0.5f, 0f, 0.5f);
                while (outstanding > config.MinSpawnAmount && CanSpawnPile(ref state, ref config, runtime))
                {
                    var spawnAmount = math.min(outstanding, config.DefaultMaxCapacity);
                    var spawnPos = transformRef.ValueRO.Position + spawnOffset;
                    SpawnPile(ref state, pile.ResourceTypeIndex, spawnAmount, spawnPos, ref config, ref ecb, elapsedTime);
                    outstanding -= spawnAmount;
                    spawnOffset.xz += new float2(0.5f, 0.5f);
                }
            }
        }

        private void UpdateTransformScale(Entity entity, float amount)
        {
            if (!_transformLookup.HasComponent(entity))
            {
                return;
            }

            var transform = _transformLookup[entity];
            transform.Scale = AggregatePileVisualUtility.CalculateScale(amount);
            _transformLookup[entity] = transform;

            if (_visualLookup.HasComponent(entity))
            {
                var visual = _visualLookup[entity];
                visual.TargetScale = transform.Scale;
                _visualLookup[entity] = visual;
            }
        }

        private void UpdateVisualTargets(ref SystemState state, float deltaTime)
        {
            foreach (var (visualRef, transformRef, entity) in SystemAPI
                         .Query<RefRW<AggregatePileVisual>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                var visual = visualRef.ValueRO;
                var current = transformRef.ValueRO.Scale;
                var target = visual.TargetScale;
                var newScale = math.lerp(current, target, math.saturate(deltaTime * 5f));
                transformRef.ValueRW.Scale = newScale;
                visual.CurrentScale = newScale;
                visualRef.ValueRW = visual;
            }
        }
    }

    internal static class AggregatePileVisualUtility
    {
        private static readonly float2[] Curve =
        {
            new float2(0f, 0.25f),
            new float2(100f, 1f),
            new float2(500f, 3f),
            new float2(1000f, 10f),
            new float2(2500f, 30f),
            new float2(5000f, 50f)
        };

        public static float CalculateScale(float amount)
        {
            var clamped = math.max(0f, amount);
            for (var i = 1; i < Curve.Length; i++)
            {
                if (clamped <= Curve[i].x)
                {
                    var prev = Curve[i - 1];
                    var next = Curve[i];
                    var t = math.saturate((clamped - prev.x) / math.max(0.0001f, next.x - prev.x));
                    return math.lerp(prev.y, next.y, t);
                }
            }

            return Curve[Curve.Length - 1].y;
        }
    }
}
