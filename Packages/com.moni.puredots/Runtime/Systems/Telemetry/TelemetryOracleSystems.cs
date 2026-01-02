using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Interrupts;
using PureDOTS.Runtime.Intent;
using PureDOTS.Runtime.Movement;
using PureDOTS.Runtime.Physics;
using PureDOTS.Runtime.Power;
using PureDOTS.Runtime.Space;
using PureDOTS.Runtime.Telemetry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Telemetry
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TelemetryOracleBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TelemetryStream>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            var telemetryEntity = SystemAPI.GetSingletonEntity<TelemetryStream>();

            if (!entityManager.HasComponent<TelemetryOracleAccumulator>(telemetryEntity))
            {
                entityManager.AddComponentData(telemetryEntity, TelemetryOracleAccumulator.CreateDefault());
            }

            if (!entityManager.HasBuffer<TelemetryOracleLatencySample>(telemetryEntity))
            {
                entityManager.AddBuffer<TelemetryOracleLatencySample>(telemetryEntity);
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<MovementState>>()
                         .WithNone<TelemetryOracleMovementModeState>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new TelemetryOracleMovementModeState());
            }

            foreach (var (module, entity) in SystemAPI.Query<RefRO<ShipModule>>()
                         .WithNone<TelemetryOracleModuleState>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new TelemetryOracleModuleState
                {
                    Initialized = 1,
                    LastState = module.ValueRO.State
                });
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PureDOTS.Runtime.Telemetry.BehaviorTelemetryAggregateSystem))]
    public partial struct TelemetryOracleAccumulatorSystem : ISystem
    {
        private BufferLookup<Interrupt> _interruptLookup;
        private BufferLookup<QueuedIntent> _queuedIntentLookup;
        private ComponentLookup<TelemetryOracleMovementModeState> _movementModeLookup;
        private ComponentLookup<TelemetryOracleModuleState> _moduleStateLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TelemetryStream>();
            state.RequireForUpdate<TelemetryExportConfig>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<TelemetryOracleAccumulator>();

            _interruptLookup = state.GetBufferLookup<Interrupt>(true);
            _queuedIntentLookup = state.GetBufferLookup<QueuedIntent>(true);
            _movementModeLookup = state.GetComponentLookup<TelemetryOracleMovementModeState>(false);
            _moduleStateLookup = state.GetComponentLookup<TelemetryOracleModuleState>(false);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TelemetryExportConfig>(out var exportConfig) ||
                exportConfig.Enabled == 0 ||
                (exportConfig.Flags & TelemetryExportFlags.IncludeTelemetryMetrics) == 0)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var tick = timeState.Tick;
            var cadence = exportConfig.CadenceTicks > 0 ? exportConfig.CadenceTicks : 30u;
            var shouldExport = cadence <= 1u || tick % cadence == 0u;

            _interruptLookup.Update(ref state);
            _queuedIntentLookup.Update(ref state);
            _movementModeLookup.Update(ref state);
            _moduleStateLookup.Update(ref state);

            var accumulator = SystemAPI.GetSingletonRW<TelemetryOracleAccumulator>();
            var acc = accumulator.ValueRO;
            var latencyBuffer = SystemAPI.GetSingletonBuffer<TelemetryOracleLatencySample>();

            var deltaSeconds = timeState.IsPaused ? 0f : math.max(0f, timeState.DeltaSeconds);
            acc.SampleTicks += 1;
            acc.SampleSeconds += deltaSeconds;

            AccumulateAiMetrics(ref state, tick, ref acc, ref latencyBuffer);
            AccumulateMovementMetrics(ref state, ref acc);
            AccumulateModuleMetrics(ref state, deltaSeconds, ref acc);
            AccumulatePowerMetrics(ref state, deltaSeconds, ref acc);
            AccumulateCollisionMetrics(ref state, ref acc);

            if (shouldExport)
            {
                EmitOracleMetrics(ref state, ref acc, ref latencyBuffer);
                acc = TelemetryOracleAccumulator.CreateDefault();
            }

            accumulator.ValueRW = acc;
        }

        private void AccumulateAiMetrics(ref SystemState state, uint tick, ref TelemetryOracleAccumulator acc, ref DynamicBuffer<TelemetryOracleLatencySample> latencyBuffer)
        {
            uint workAvailable = 0;
            uint idleWithWork = 0;
            uint intentUpdates = 0;
            uint intentSamples = 0;
            ulong intentAgeSum = 0;
            uint intentAgeMax = 0;

            foreach (var (intent, entity) in SystemAPI.Query<RefRO<EntityIntent>>().WithEntityAccess())
            {
                var hasWork = false;
                if (_interruptLookup.HasBuffer(entity))
                {
                    var buffer = _interruptLookup[entity];
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].IsProcessed == 0)
                        {
                            hasWork = true;
                            break;
                        }
                    }
                }

                if (!hasWork && _queuedIntentLookup.HasBuffer(entity))
                {
                    var queue = _queuedIntentLookup[entity];
                    hasWork = queue.Length > 0;
                }

                if (hasWork)
                {
                    workAvailable += 1;
                    if (intent.ValueRO.Mode == IntentMode.Idle || intent.ValueRO.IsValid == 0)
                    {
                        idleWithWork += 1;
                    }
                }

                if (intent.ValueRO.IntentSetTick == tick && intent.ValueRO.IsValid != 0)
                {
                    intentUpdates += 1;
                }

                if (intent.ValueRO.IsValid != 0 && intent.ValueRO.Mode != IntentMode.Idle)
                {
                    var age = tick >= intent.ValueRO.IntentSetTick ? tick - intent.ValueRO.IntentSetTick : 0u;
                    intentSamples += 1;
                    intentAgeSum += age;
                    intentAgeMax = math.max(intentAgeMax, age);

                    if (latencyBuffer.Length < MaxLatencySamples)
                    {
                        latencyBuffer.Add(new TelemetryOracleLatencySample { Value = age });
                    }
                }
            }

            acc.WorkAvailableCount += workAvailable;
            acc.IdleWithWorkCount += idleWithWork;
            acc.IntentUpdates += intentUpdates;
            acc.IntentSamples += intentSamples;
            acc.IntentAgeSumTicks += intentAgeSum;
            acc.IntentAgeMaxTicks = math.max(acc.IntentAgeMaxTicks, intentAgeMax);
        }

        private void AccumulateMovementMetrics(ref SystemState state, ref TelemetryOracleAccumulator acc)
        {
            foreach (var (movement, entity) in SystemAPI.Query<RefRO<MovementState>>().WithEntityAccess())
            {
                if (math.lengthsq(movement.ValueRO.Desired) > StuckDesiredThresholdSq &&
                    math.lengthsq(movement.ValueRO.Vel) < StuckVelocityThresholdSq)
                {
                    acc.MoveStuckTicks += 1;
                }

                if (_movementModeLookup.HasComponent(entity))
                {
                    var modeState = _movementModeLookup.GetRefRW(entity);
                    if (modeState.ValueRO.Initialized == 0)
                    {
                        modeState.ValueRW.Initialized = 1;
                        modeState.ValueRW.LastMode = movement.ValueRO.Mode;
                    }
                    else if (modeState.ValueRO.LastMode != movement.ValueRO.Mode)
                    {
                        acc.MoveModeFlipCount += 1;
                        modeState.ValueRW.LastMode = movement.ValueRO.Mode;
                    }
                }
            }
        }

        private void AccumulateModuleMetrics(ref SystemState state, float deltaSeconds, ref TelemetryOracleAccumulator acc)
        {
            var notReadyCount = 0;
            foreach (var (module, entity) in SystemAPI.Query<RefRO<ShipModule>>().WithEntityAccess())
            {
                if (module.ValueRO.State != ModuleState.Active)
                {
                    notReadyCount += 1;
                }

                if (_moduleStateLookup.HasComponent(entity))
                {
                    var moduleState = _moduleStateLookup.GetRefRW(entity);
                    if (moduleState.ValueRO.Initialized == 0)
                    {
                        moduleState.ValueRW.Initialized = 1;
                        moduleState.ValueRW.LastState = module.ValueRO.State;
                    }
                    else if (moduleState.ValueRO.LastState != module.ValueRO.State)
                    {
                        if (moduleState.ValueRO.LastState == ModuleState.Active ||
                            module.ValueRO.State == ModuleState.Active)
                        {
                            acc.ModuleSpoolTransitions += 1;
                        }

                        moduleState.ValueRW.LastState = module.ValueRO.State;
                    }
                }
            }

            if (notReadyCount > 0 && deltaSeconds > 0f)
            {
                acc.ModuleNotReadySeconds += notReadyCount * deltaSeconds;
            }
        }

        private static void AccumulatePowerMetrics(ref SystemState state, float deltaSeconds, ref TelemetryOracleAccumulator acc)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            var deficit = false;
            foreach (var power in SystemAPI.Query<RefRO<CarrierPowerBudget>>())
            {
                var budget = power.ValueRO;
                if (budget.OverBudget || budget.CurrentGeneration + 0.001f < budget.CurrentDraw)
                {
                    deficit = true;
                    break;
                }
            }

            if (!deficit)
            {
                foreach (var aggregate in SystemAPI.Query<RefRO<AggregatePowerState>>())
                {
                    if (aggregate.ValueRO.BlackoutLevel > 0.001f || aggregate.ValueRO.Coverage < 0.99f)
                    {
                        deficit = true;
                        break;
                    }
                }
            }

            if (deficit)
            {
                acc.PowerDeficitSeconds += deltaSeconds;
            }

            var minSoc = acc.PowerSocInitialized != 0 ? acc.PowerMinSoc : 1f;
            var socFound = false;
            foreach (var snapshot in SystemAPI.Query<RefRO<PowerStateSnapshot>>())
            {
                minSoc = math.min(minSoc, math.saturate(snapshot.ValueRO.PowerFactor));
                socFound = true;
            }

            if (!socFound)
            {
                foreach (var aggregate in SystemAPI.Query<RefRO<AggregatePowerState>>())
                {
                    minSoc = math.min(minSoc, math.saturate(aggregate.ValueRO.Coverage));
                    socFound = true;
                }
            }

            if (socFound)
            {
                acc.PowerMinSoc = minSoc;
                acc.PowerSocInitialized = 1;
            }
        }

        private static void AccumulateCollisionMetrics(ref SystemState state, ref TelemetryOracleAccumulator acc)
        {
            uint collisionEvents = 0;
            foreach (var buffer in SystemAPI.Query<DynamicBuffer<PhysicsCollisionEventElement>>())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].EventType == PhysicsCollisionEventType.Collision)
                    {
                        collisionEvents += 1;
                    }
                }
            }

            if (collisionEvents > 0)
            {
                acc.CollisionDamageEvents += collisionEvents / 2;
            }
        }

        private static void EmitOracleMetrics(ref SystemState state, ref TelemetryOracleAccumulator acc, ref DynamicBuffer<TelemetryOracleLatencySample> latencyBuffer)
        {
            if (!SystemAPI.TryGetSingletonBuffer<TelemetryMetric>(out var metrics))
            {
                return;
            }

            var exportState = SystemAPI.GetSingleton<TelemetryExportState>();
            var nearMissCount = 0u;
            foreach (var hazard in SystemAPI.Query<RefRO<HazardDodgeTelemetry>>())
            {
                nearMissCount += hazard.ValueRO.AvoidanceTransitionsInterval;
            }

            var idleRatio = acc.WorkAvailableCount > 0
                ? (float)acc.IdleWithWorkCount / acc.WorkAvailableCount
                : 0f;
            var seconds = math.max(0.0001f, acc.SampleSeconds);
            var churnPerMin = acc.IntentUpdates > 0 ? (acc.IntentUpdates / seconds) * 60f : 0f;
            var latencyMean = acc.IntentSamples > 0 ? (float)acc.IntentAgeSumTicks / acc.IntentSamples : 0f;
            var latencyP95 = ResolveP95(latencyBuffer, acc.IntentAgeMaxTicks);

            metrics.AddMetric("ai.idle_with_work_ratio", idleRatio, TelemetryMetricUnit.Ratio);
            metrics.AddMetric("ai.task_latency_ticks.mean", latencyMean, TelemetryMetricUnit.Count);
            metrics.AddMetric("ai.task_latency_ticks.p95", latencyP95, TelemetryMetricUnit.Count);
            metrics.AddMetric("ai.decision_churn_per_min", churnPerMin, TelemetryMetricUnit.Custom);
            metrics.AddMetric("ai.intent_flip_count", acc.IntentUpdates, TelemetryMetricUnit.Count);

            metrics.AddMetric("move.stuck_ticks", acc.MoveStuckTicks, TelemetryMetricUnit.Count);
            metrics.AddMetric("move.mode_flip_count", acc.MoveModeFlipCount, TelemetryMetricUnit.Count);
            metrics.AddMetric("move.collision_near_miss_count", nearMissCount, TelemetryMetricUnit.Count);
            metrics.AddMetric("move.collision_damage_events", acc.CollisionDamageEvents, TelemetryMetricUnit.Count);

            metrics.AddMetric("power.deficit_time_s", acc.PowerDeficitSeconds, TelemetryMetricUnit.Custom);
            metrics.AddMetric("power.battery_min_soc", acc.PowerSocInitialized != 0 ? acc.PowerMinSoc : 1f, TelemetryMetricUnit.Ratio);

            metrics.AddMetric("module.spool_transitions", acc.ModuleSpoolTransitions, TelemetryMetricUnit.Count);
            metrics.AddMetric("module.not_ready_time_s", acc.ModuleNotReadySeconds, TelemetryMetricUnit.Custom);

            metrics.AddMetric("telemetry.bytes_written", (float)exportState.BytesWritten, TelemetryMetricUnit.Bytes);
            metrics.AddMetric("telemetry.truncated", exportState.CapReached != 0 ? 1f : 0f, TelemetryMetricUnit.Count);
            metrics.AddMetric("telemetry.drop_count", 0f, TelemetryMetricUnit.Count);

            latencyBuffer.Clear();
        }

        private static float ResolveP95(in DynamicBuffer<TelemetryOracleLatencySample> samples, uint fallback)
        {
            if (!samples.IsCreated || samples.Length == 0)
            {
                return fallback;
            }

            var values = new NativeList<uint>(samples.Length, Allocator.Temp);
            for (int i = 0; i < samples.Length; i++)
            {
                values.Add(samples[i].Value);
            }

            values.Sort();
            var index = (int)math.floor(0.95f * (values.Length - 1));
            index = math.clamp(index, 0, values.Length - 1);
            var value = values[index];
            values.Dispose();
            return value;
        }

        private const float StuckDesiredThresholdSq = 0.25f;
        private const float StuckVelocityThresholdSq = 0.0004f;
        private const int MaxLatencySamples = 256;
    }
}
