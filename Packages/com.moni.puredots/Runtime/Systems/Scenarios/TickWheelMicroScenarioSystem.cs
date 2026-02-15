using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Scenarios
{
    public struct TickWheelMicroSeededTag : IComponentData
    {
    }

    /// <summary>
    /// Seeds deterministic scheduler events for scenario.puredots.tickwheel.micro.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ScenarioEntityBootstrapSystem))]
    public partial struct TickWheelMicroScenarioSystem : ISystem
    {
        private FixedString64Bytes _targetScenarioId;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            state.RequireForUpdate<TickWheelSingletonTag>();
            _targetScenarioId = new FixedString64Bytes("scenario.puredots.tickwheel.micro");
        }

        public void OnUpdate(ref SystemState state)
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            if (!scenarioInfo.ScenarioId.Equals(_targetScenarioId))
            {
                state.Enabled = false;
                return;
            }

            if (SystemAPI.HasSingleton<TickWheelMicroSeededTag>())
            {
                state.Enabled = false;
                return;
            }

            var startTick = ResolveCurrentTick() + 1u;
            var wheelEntity = SystemAPI.GetSingletonEntity<TickWheelSingletonTag>();
            var settings = state.EntityManager.GetComponentData<TickWheelSettings>(wheelEntity);
            settings.WheelSize = 512u;
            settings.BucketStride = 1u;
            state.EntityManager.SetComponentData(wheelEntity, settings);

            var targetCount = 8;
            var eventCount = 4096;
            var dueRange = 180;

            var targets = new NativeArray<Entity>(targetCount, Allocator.Temp);
            for (var i = 0; i < targetCount; i++)
            {
                var target = state.EntityManager.CreateEntity();
                state.EntityManager.AddBuffer<TickWheelReceipt>(target);
                targets[i] = target;
            }

            var requests = state.EntityManager.GetBuffer<TickWheelScheduleRequest>(wheelEntity);
            requests.Clear();

            for (var i = 0; i < eventCount; i++)
            {
                var dueOffset = (uint)(1 + ((i * 17) % dueRange));
                var targetIndex = i % targetCount;
                requests.Add(new TickWheelScheduleRequest
                {
                    DueTick = startTick + dueOffset,
                    PayloadId = i % 31,
                    Target = targets[targetIndex],
                    TieBreakA = (uint)targetIndex,
                    TieBreakB = (uint)i
                });
            }

            targets.Dispose();

            state.EntityManager.CreateEntity(typeof(TickWheelMicroSeededTag));
            state.Enabled = false;
        }

        private static uint ResolveCurrentTick()
        {
            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickState))
            {
                return tickState.Tick;
            }

            return SystemAPI.TryGetSingleton<TimeState>(out var timeState) ? timeState.Tick : 0u;
        }
    }
}
