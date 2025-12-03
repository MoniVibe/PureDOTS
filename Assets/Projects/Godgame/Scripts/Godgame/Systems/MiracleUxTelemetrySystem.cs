using Godgame.Runtime;
using Godgame.Systems;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Systems
{
    /// <summary>
    /// Collects miracle UX telemetry data for cast latency and cancellation tracking.
    /// Game projects can use this data for HUD displays and design tuning.
    /// </summary>
    /// <remarks>
    /// This system:
    /// - Tracks input-to-activation latency for completed casts
    /// - Records cancellation reasons
    /// - Maintains a rolling buffer of recent telemetry entries
    /// - Publishes aggregate metrics to the telemetry stream
    /// </remarks>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(MiracleRegistrySystem))]
    public partial struct MiracleUxTelemetrySystem : ISystem
    {
        // Instance fields for Burst-compatible FixedString patterns (initialized in OnCreate)
        private FixedString64Bytes _miracleNameRain;
        private FixedString64Bytes _miracleNameFireball;
        private FixedString64Bytes _miracleNameHeal;
        private FixedString64Bytes _miracleNameBlessing;
        private FixedString64Bytes _miracleNameFertility;
        private FixedString64Bytes _miracleNameSunlight;
        private FixedString64Bytes _miracleNameFire;
        private FixedString64Bytes _miracleNameLightning;
        private FixedString64Bytes _miracleNameShield;
        private FixedString64Bytes _miracleNameUnknown;

        private FixedString64Bytes _keyLatencyAvg;
        private FixedString64Bytes _keyTotalCancellations;
        private FixedString64Bytes _keyCancelledByUser;
        private FixedString64Bytes _keyCancelledByTarget;
        private FixedString64Bytes _keyCancelledByResources;
        private FixedString64Bytes _keyCompletedCasts;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MiracleRegistry>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _keyLatencyAvg = new FixedString64Bytes("registry.miracles.castLatency.avg_ms");
            _keyTotalCancellations = new FixedString64Bytes("registry.miracles.cancellations.total");
            _keyCancelledByUser = new FixedString64Bytes("registry.miracles.cancellations.byUser");
            _keyCancelledByTarget = new FixedString64Bytes("registry.miracles.cancellations.byTarget");
            _keyCancelledByResources = new FixedString64Bytes("registry.miracles.cancellations.byResources");
            _keyCompletedCasts = new FixedString64Bytes("registry.miracles.completedCasts");
            
            // Initialize FixedString patterns (OnCreate is not Burst-compiled)
            _miracleNameRain = new FixedString64Bytes("Rain");
            _miracleNameFireball = new FixedString64Bytes("Fireball");
            _miracleNameHeal = new FixedString64Bytes("Heal");
            _miracleNameBlessing = new FixedString64Bytes("Blessing");
            _miracleNameFertility = new FixedString64Bytes("Fertility");
            _miracleNameSunlight = new FixedString64Bytes("Sunlight");
            _miracleNameFire = new FixedString64Bytes("Fire");
            _miracleNameLightning = new FixedString64Bytes("Lightning");
            _miracleNameShield = new FixedString64Bytes("Shield");
            _miracleNameUnknown = new FixedString64Bytes("Unknown");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Ensure telemetry state singleton exists
            var telemetryStateEntity = Entity.Null;
            if (!SystemAPI.TryGetSingletonEntity<MiracleUxTelemetryState>(out telemetryStateEntity))
            {
                telemetryStateEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(telemetryStateEntity, new MiracleUxTelemetryState
                {
                    EntryCount = 0,
                    Version = 0,
                    TotalLatencyMs = 0f,
                    CompletedCastCount = 0,
                    TotalCancellations = 0,
                    CancellationsUserCancelled = 0,
                    CancellationsTargetInvalid = 0,
                    CancellationsInterrupted = 0,
                    CancellationsInsufficientResources = 0
                });
                state.EntityManager.AddBuffer<MiracleUxTelemetry>(telemetryStateEntity);
            }

            ref var telemetryState = ref SystemAPI.GetComponentRW<MiracleUxTelemetryState>(telemetryStateEntity).ValueRW;
            var telemetryBuffer = state.EntityManager.GetBuffer<MiracleUxTelemetry>(telemetryStateEntity);

            // Process miracle registry entries to collect telemetry
            var registryEntity = SystemAPI.GetSingletonEntity<MiracleRegistry>();
            var entries = state.EntityManager.GetBuffer<MiracleRegistryEntry>(registryEntity);

            // Track lifecycle transitions for telemetry
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                // Check for completed casts (transition to CoolingDown with valid timing data)
                if (entry.Lifecycle == MiracleLifecycleState.CoolingDown &&
                    entry.CastStartTick > 0 &&
                    entry.LastCastTick > entry.CastStartTick)
                {
                    var latencyTicks = entry.LastCastTick - entry.CastStartTick;
                    var latencySeconds = latencyTicks * timeState.FixedDeltaTime;
                    var latencyMs = latencySeconds * 1000f;

                    // Add to rolling buffer
                    AddTelemetryEntry(ref telemetryBuffer, ref telemetryState, new MiracleUxTelemetry
                    {
                        MiracleId = GetMiracleTypeName(entry.Type, _miracleNameRain, _miracleNameFireball, _miracleNameHeal,
                            _miracleNameBlessing, _miracleNameFertility, _miracleNameSunlight, _miracleNameFire,
                            _miracleNameLightning, _miracleNameShield, _miracleNameUnknown),
                        InputTick = entry.LastInputTick,
                        ActivationTick = entry.CastStartTick,
                        CancelTick = 0,
                        CancelReason = MiracleCancelReason.None,
                        LatencySeconds = latencySeconds,
                        CasterEntity = entry.CasterEntity,
                        IsCompleted = 1
                    });

                    telemetryState.RecordCompletion(latencyMs);
                }

                // Check for cancellations
                if (entry.CancelTick > 0 && entry.CancelReason != MiracleCancelReason.None)
                {
                    AddTelemetryEntry(ref telemetryBuffer, ref telemetryState, new MiracleUxTelemetry
                    {
                        MiracleId = GetMiracleTypeName(entry.Type, _miracleNameRain, _miracleNameFireball, _miracleNameHeal,
                            _miracleNameBlessing, _miracleNameFertility, _miracleNameSunlight, _miracleNameFire,
                            _miracleNameLightning, _miracleNameShield, _miracleNameUnknown),
                        InputTick = entry.LastInputTick,
                        ActivationTick = entry.CastStartTick,
                        CancelTick = entry.CancelTick,
                        CancelReason = entry.CancelReason,
                        LatencySeconds = 0f,
                        CasterEntity = entry.CasterEntity,
                        IsCompleted = 0
                    });

                    telemetryState.RecordCancellation(entry.CancelReason);
                }
            }

            // Publish to telemetry stream if available
            if (SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var streamEntity))
            {
                var metricsBuffer = state.EntityManager.GetBuffer<TelemetryMetric>(streamEntity);

                metricsBuffer.AddMetric(_keyLatencyAvg, telemetryState.AverageLatencyMs, TelemetryMetricUnit.DurationMilliseconds);
                metricsBuffer.AddMetric(_keyCompletedCasts, telemetryState.CompletedCastCount, TelemetryMetricUnit.Count);
                metricsBuffer.AddMetric(_keyTotalCancellations, telemetryState.TotalCancellations, TelemetryMetricUnit.Count);
                metricsBuffer.AddMetric(_keyCancelledByUser, telemetryState.CancellationsUserCancelled, TelemetryMetricUnit.Count);
                metricsBuffer.AddMetric(_keyCancelledByTarget, telemetryState.CancellationsTargetInvalid, TelemetryMetricUnit.Count);
                metricsBuffer.AddMetric(_keyCancelledByResources, telemetryState.CancellationsInsufficientResources, TelemetryMetricUnit.Count);
            }

            telemetryState.EntryCount = telemetryBuffer.Length;
        }

        private static void AddTelemetryEntry(
            ref DynamicBuffer<MiracleUxTelemetry> buffer,
            ref MiracleUxTelemetryState telemetryState,
            in MiracleUxTelemetry entry)
        {
            // Recycle oldest entry if at capacity
            if (buffer.Length >= MiracleUxTelemetryState.MaxCapacity)
            {
                buffer.RemoveAt(0);
            }

            buffer.Add(entry);
            telemetryState.Version++;
        }

        private static FixedString64Bytes GetMiracleTypeName(
            MiracleType type,
            in FixedString64Bytes nameRain,
            in FixedString64Bytes nameFireball,
            in FixedString64Bytes nameHeal,
            in FixedString64Bytes nameBlessing,
            in FixedString64Bytes nameFertility,
            in FixedString64Bytes nameSunlight,
            in FixedString64Bytes nameFire,
            in FixedString64Bytes nameLightning,
            in FixedString64Bytes nameShield,
            in FixedString64Bytes nameUnknown)
        {
            return type switch
            {
                MiracleType.Rain => nameRain,
                MiracleType.Fireball => nameFireball,
                MiracleType.Heal or MiracleType.Healing => nameHeal,
                MiracleType.Blessing => nameBlessing,
                MiracleType.Fertility => nameFertility,
                MiracleType.Sunlight => nameSunlight,
                MiracleType.Fire => nameFire,
                MiracleType.Lightning => nameLightning,
                MiracleType.Shield => nameShield,
                _ => nameUnknown
            };
        }
    }

    /// <summary>
    /// Template system for game projects to bridge miracle UX telemetry to game-specific displays.
    /// Games should inherit from or adapt this pattern for their HUD/dashboard needs.
    /// </summary>
    /// <remarks>
    /// Example usage:
    /// - Subscribe to MiracleUxTelemetryState.Version changes
    /// - Read MiracleUxTelemetry buffer for recent cast data
    /// - Display latency metrics on HUD
    /// - Show cancellation breakdown for design analytics
    /// </remarks>
    public static class MiracleUxTelemetryBridge
    {
        /// <summary>
        /// Gets the current telemetry state from the world.
        /// </summary>
        public static bool TryGetTelemetryState(World world, out MiracleUxTelemetryState state, out DynamicBuffer<MiracleUxTelemetry> buffer)
        {
            state = default;
            buffer = default;

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MiracleUxTelemetryState>());

            if (query.IsEmptyIgnoreFilter)
            {
                return false;
            }

            var entity = query.GetSingletonEntity();
            state = entityManager.GetComponentData<MiracleUxTelemetryState>(entity);

            if (entityManager.HasBuffer<MiracleUxTelemetry>(entity))
            {
                buffer = entityManager.GetBuffer<MiracleUxTelemetry>(entity);
            }

            return true;
        }

        /// <summary>
        /// Calculates P95 latency from the telemetry buffer.
        /// </summary>
        public static float CalculateP95LatencyMs(in DynamicBuffer<MiracleUxTelemetry> buffer, Allocator allocator = Allocator.Temp)
        {
            if (buffer.Length == 0)
            {
                return 0f;
            }

            var completedLatencies = new NativeList<float>(buffer.Length, allocator);
            for (var i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];
                if (entry.IsCompleted != 0 && entry.LatencySeconds > 0f)
                {
                    completedLatencies.Add(entry.LatencySeconds * 1000f);
                }
            }

            if (completedLatencies.Length == 0)
            {
                completedLatencies.Dispose();
                return 0f;
            }

            completedLatencies.Sort();
            var p95Index = (int)(completedLatencies.Length * 0.95f);
            p95Index = Unity.Mathematics.math.clamp(p95Index, 0, completedLatencies.Length - 1);
            var result = completedLatencies[p95Index];
            completedLatencies.Dispose();

            return result;
        }
    }
}

