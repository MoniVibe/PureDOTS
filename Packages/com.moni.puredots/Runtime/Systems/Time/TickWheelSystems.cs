using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Time
{
    /// <summary>
    /// One-shot bootstrap that creates the scheduler singleton and required buffers.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct TickWheelBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<TickWheelSingletonTag>())
            {
                state.Enabled = false;
                return;
            }

            var entity = state.EntityManager.CreateEntity(
                typeof(TickWheelSingletonTag),
                typeof(TickWheelSettings),
                typeof(TickWheelRuntimeState));

            state.EntityManager.SetComponentData(entity, TickWheelSettings.CreateDefault());
            state.EntityManager.SetComponentData(entity, default(TickWheelRuntimeState));

            var buckets = state.EntityManager.AddBuffer<TickWheelBucket>(entity);
            var events = state.EntityManager.AddBuffer<TickWheelEvent>(entity);
            var requests = state.EntityManager.AddBuffer<TickWheelScheduleRequest>(entity);

            events.Clear();
            requests.Clear();
            ResizeBuckets(ref buckets, 2048u);

            state.Enabled = false;
        }

        private static void ResizeBuckets(ref DynamicBuffer<TickWheelBucket> buckets, uint wheelSize)
        {
            var resolved = math.max(1, (int)wheelSize);
            buckets.ResizeUninitialized(resolved);
            for (var i = 0; i < resolved; i++)
            {
                buckets[i] = new TickWheelBucket
                {
                    HeadEventIndex = -1
                };
            }
        }
    }

    /// <summary>
    /// API helper for queuing scheduler requests into TickWheelScheduleRequest buffer.
    /// </summary>
    public static class TickWheelScheduleApi
    {
        public static bool TryEnqueue(EntityManager entityManager, in TickWheelScheduleRequest request)
        {
            if (!TryGetWheelEntity(entityManager, out var wheelEntity))
            {
                return false;
            }

            var requests = entityManager.GetBuffer<TickWheelScheduleRequest>(wheelEntity);
            requests.Add(request);
            return true;
        }

        private static bool TryGetWheelEntity(EntityManager entityManager, out Entity wheelEntity)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TickWheelSingletonTag>());
            if (query.IsEmptyIgnoreFilter)
            {
                wheelEntity = Entity.Null;
                return false;
            }

            wheelEntity = query.GetSingletonEntity();
            return true;
        }
    }

    /// <summary>
    /// Converts queued schedule requests into bucketed events (O(1) amortized insertion).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct TickWheelScheduleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickWheelSingletonTag>();
            state.RequireForUpdate<TickWheelSettings>();
            state.RequireForUpdate<TickWheelRuntimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var currentTick = ResolveCurrentTick();

            foreach (var (settingsRef, runtimeRef, buckets, events, requests) in
                     SystemAPI.Query<RefRO<TickWheelSettings>, RefRW<TickWheelRuntimeState>, DynamicBuffer<TickWheelBucket>, DynamicBuffer<TickWheelEvent>, DynamicBuffer<TickWheelScheduleRequest>>())
            {
                var settings = NormalizeSettings(settingsRef.ValueRO);
                EnsureBucketShape(ref buckets, settings.WheelSize);

                if (requests.Length == 0)
                {
                    break;
                }

                var runtime = runtimeRef.ValueRW;
                for (var i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];
                    var dueTick = math.max(request.DueTick, currentTick);
                    var bucketIndex = ComputeBucketIndex(dueTick, settings);
                    var eventIndex = events.Length;

                    var bucket = buckets[bucketIndex];
                    events.Add(new TickWheelEvent
                    {
                        DueTick = dueTick,
                        PayloadId = request.PayloadId,
                        Target = request.Target,
                        TieBreakA = request.TieBreakA,
                        TieBreakB = request.TieBreakB,
                        Sequence = runtime.NextSequence,
                        NextEventIndex = bucket.HeadEventIndex,
                        Active = 1
                    });

                    bucket.HeadEventIndex = eventIndex;
                    buckets[bucketIndex] = bucket;

                    runtime.NextSequence++;
                    runtime.ScheduledCount++;
                }

                requests.Clear();
                runtimeRef.ValueRW = runtime;
                break;
            }
        }

        private static TickWheelSettings NormalizeSettings(in TickWheelSettings settings)
        {
            return new TickWheelSettings
            {
                WheelSize = math.max(1u, settings.WheelSize),
                BucketStride = math.max(1u, settings.BucketStride)
            };
        }

        private static uint ResolveCurrentTick()
        {
            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickState))
            {
                return tickState.Tick;
            }

            return SystemAPI.TryGetSingleton<TimeState>(out var timeState) ? timeState.Tick : 0u;
        }

        private static int ComputeBucketIndex(uint dueTick, in TickWheelSettings settings)
        {
            var slotTick = dueTick / settings.BucketStride;
            return (int)(slotTick % settings.WheelSize);
        }

        private static void EnsureBucketShape(ref DynamicBuffer<TickWheelBucket> buckets, uint wheelSize)
        {
            if (buckets.Length == (int)wheelSize)
            {
                return;
            }

            buckets.ResizeUninitialized((int)wheelSize);
            for (var i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new TickWheelBucket
                {
                    HeadEventIndex = -1
                };
            }
        }
    }

    /// <summary>
    /// Dispatches due events each tick from the current wheel bucket.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TickWheelScheduleSystem))]
    public partial struct TickWheelDispatchSystem : ISystem
    {
        private BufferLookup<TickWheelReceipt> _receiptLookup;
        private EntityStorageInfoLookup _entityLookup;

        private FixedString64Bytes _scheduledCountKey;
        private FixedString64Bytes _firedCountKey;
        private FixedString64Bytes _maxLatenessTicksKey;
        private FixedString64Bytes _digestKey;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickWheelSingletonTag>();
            state.RequireForUpdate<TickWheelSettings>();
            state.RequireForUpdate<TickWheelRuntimeState>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _receiptLookup = state.GetBufferLookup<TickWheelReceipt>(true);
            _entityLookup = state.GetEntityStorageInfoLookup();

            _scheduledCountKey = new FixedString64Bytes("tickwheel.scheduled_count");
            _firedCountKey = new FixedString64Bytes("tickwheel.fired_count");
            _maxLatenessTicksKey = new FixedString64Bytes("tickwheel.max_lateness_ticks");
            _digestKey = new FixedString64Bytes("tickwheel.digest");
        }

        public void OnUpdate(ref SystemState state)
        {
            var currentTick = ResolveCurrentTick();
            _receiptLookup.Update(ref state);
            _entityLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (settingsRef, runtimeRef, buckets, events) in
                     SystemAPI.Query<RefRO<TickWheelSettings>, RefRW<TickWheelRuntimeState>, DynamicBuffer<TickWheelBucket>, DynamicBuffer<TickWheelEvent>>())
            {
                var settings = NormalizeSettings(settingsRef.ValueRO);
                EnsureBucketShape(ref buckets, settings.WheelSize);

                var runtime = runtimeRef.ValueRW;
                var dispatchIndex = ComputeBucketIndex(currentTick, settings);
                var dispatchBucket = buckets[dispatchIndex];
                var dueEventIndices = new NativeList<int>(Allocator.Temp);

                var keepHead = -1;
                var cursor = dispatchBucket.HeadEventIndex;
                while (cursor >= 0 && cursor < events.Length)
                {
                    var currentEvent = events[cursor];
                    var next = currentEvent.NextEventIndex;

                    if (currentEvent.Active == 0)
                    {
                        cursor = next;
                        continue;
                    }

                    if (currentEvent.DueTick <= currentTick)
                    {
                        dueEventIndices.Add(cursor);
                    }
                    else
                    {
                        currentEvent.NextEventIndex = keepHead;
                        events[cursor] = currentEvent;
                        keepHead = cursor;
                    }

                    cursor = next;
                }

                dispatchBucket.HeadEventIndex = keepHead;
                buckets[dispatchIndex] = dispatchBucket;

                SortDueEvents(ref dueEventIndices, ref events);

                for (var i = 0; i < dueEventIndices.Length; i++)
                {
                    var eventIndex = dueEventIndices[i];
                    if (eventIndex < 0 || eventIndex >= events.Length)
                    {
                        continue;
                    }

                    var entry = events[eventIndex];
                    if (entry.Active == 0)
                    {
                        continue;
                    }

                    var lateness = currentTick > entry.DueTick ? currentTick - entry.DueTick : 0u;
                    runtime.MaxLatenessTicks = math.max(runtime.MaxLatenessTicks, lateness);
                    runtime.FiredCount++;
                    runtime.Digest = MixDigest(runtime.Digest, in entry);
                    runtime.LastDispatchTick = currentTick;

                    if (entry.Target != Entity.Null &&
                        _entityLookup.Exists(entry.Target) &&
                        _receiptLookup.HasBuffer(entry.Target))
                    {
                        ecb.AppendToBuffer(entry.Target, new TickWheelReceipt
                        {
                            FiredTick = currentTick,
                            DueTick = entry.DueTick,
                            PayloadId = entry.PayloadId
                        });
                    }

                    entry.Active = 0;
                    entry.NextEventIndex = -1;
                    events[eventIndex] = entry;
                }

                dueEventIndices.Dispose();

                runtimeRef.ValueRW = runtime;
                ScenarioMetricsUtility.SetMetric(state.EntityManager, _scheduledCountKey, runtime.ScheduledCount);
                ScenarioMetricsUtility.SetMetric(state.EntityManager, _firedCountKey, runtime.FiredCount);
                ScenarioMetricsUtility.SetMetric(state.EntityManager, _maxLatenessTicksKey, runtime.MaxLatenessTicks);
                ScenarioMetricsUtility.SetMetric(state.EntityManager, _digestKey, runtime.Digest);
                break;
            }
        }

        private static TickWheelSettings NormalizeSettings(in TickWheelSettings settings)
        {
            return new TickWheelSettings
            {
                WheelSize = math.max(1u, settings.WheelSize),
                BucketStride = math.max(1u, settings.BucketStride)
            };
        }

        private static uint ResolveCurrentTick()
        {
            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickState))
            {
                return tickState.Tick;
            }

            return SystemAPI.TryGetSingleton<TimeState>(out var timeState) ? timeState.Tick : 0u;
        }

        private static int ComputeBucketIndex(uint dueTick, in TickWheelSettings settings)
        {
            var slotTick = dueTick / settings.BucketStride;
            return (int)(slotTick % settings.WheelSize);
        }

        private static void EnsureBucketShape(ref DynamicBuffer<TickWheelBucket> buckets, uint wheelSize)
        {
            if (buckets.Length == (int)wheelSize)
            {
                return;
            }

            buckets.ResizeUninitialized((int)wheelSize);
            for (var i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new TickWheelBucket
                {
                    HeadEventIndex = -1
                };
            }
        }

        private static void SortDueEvents(ref NativeList<int> eventIndices, ref DynamicBuffer<TickWheelEvent> events)
        {
            for (var i = 1; i < eventIndices.Length; i++)
            {
                var keyIndex = eventIndices[i];
                var j = i - 1;
                while (j >= 0 && CompareEvents(in events[keyIndex], in events[eventIndices[j]]) < 0)
                {
                    eventIndices[j + 1] = eventIndices[j];
                    j--;
                }

                eventIndices[j + 1] = keyIndex;
            }
        }

        private static int CompareEvents(in TickWheelEvent lhs, in TickWheelEvent rhs)
        {
            if (lhs.DueTick != rhs.DueTick)
            {
                return lhs.DueTick < rhs.DueTick ? -1 : 1;
            }

            if (lhs.TieBreakA != rhs.TieBreakA)
            {
                return lhs.TieBreakA < rhs.TieBreakA ? -1 : 1;
            }

            if (lhs.TieBreakB != rhs.TieBreakB)
            {
                return lhs.TieBreakB < rhs.TieBreakB ? -1 : 1;
            }

            if (lhs.PayloadId != rhs.PayloadId)
            {
                return lhs.PayloadId < rhs.PayloadId ? -1 : 1;
            }

            if (lhs.Target.Index != rhs.Target.Index)
            {
                return lhs.Target.Index < rhs.Target.Index ? -1 : 1;
            }

            if (lhs.Target.Version != rhs.Target.Version)
            {
                return lhs.Target.Version < rhs.Target.Version ? -1 : 1;
            }

            if (lhs.Sequence != rhs.Sequence)
            {
                return lhs.Sequence < rhs.Sequence ? -1 : 1;
            }

            return 0;
        }

        private static uint MixDigest(uint digest, in TickWheelEvent entry)
        {
            var value = digest == 0u ? 2166136261u : digest;
            value = HashStep(value, entry.DueTick);
            value = HashStep(value, (uint)entry.PayloadId);
            value = HashStep(value, (uint)entry.Target.Index);
            value = HashStep(value, (uint)entry.Target.Version);
            value = HashStep(value, entry.TieBreakA);
            value = HashStep(value, entry.TieBreakB);
            value = HashStep(value, entry.Sequence);
            return value;
        }

        private static uint HashStep(uint hash, uint data)
        {
            unchecked
            {
                var mixed = hash ^ data;
                return mixed * 16777619u;
            }
        }
    }
}
