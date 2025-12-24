#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Hand;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Godgame.Tests.Hand
{
    /// <summary>
    /// Regression tests covering Phase 8 telemetry and Unity bridge behavior.
    /// </summary>
    public class DivineHandPhase8RegressionTests
    {
        [Test]
        public unsafe void TelemetryReportsHeldAmount()
        {
            using var world = new World("TelemetryWorld");
            var entityManager = world.EntityManager;

            // Telemetry singleton
            var telemetryEntity = entityManager.CreateEntity(typeof(TelemetryStream));
            entityManager.AddBuffer<TelemetryMetric>(telemetryEntity);

            // Hand entity with authoritative state + HandInteractionState mirror
            var handEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(handEntity, new PureDOTS.Runtime.Hand.HandState
            {
                CurrentState = HandStateType.Holding,
                PreviousState = HandStateType.Idle,
                ChargeTimer = 0.5f
            });
            entityManager.AddComponentData(handEntity, new HandInteractionState
            {
                HandEntity = handEntity,
                CurrentState = PureDOTS.Runtime.Components.HandState.Holding,
                PreviousState = PureDOTS.Runtime.Components.HandState.Idle,
                ActiveResourceType = 1,
                HeldAmount = 25,
                HeldCapacity = 100,
                LastUpdateTick = 1
            });

            // Run telemetry system once
            var systemHandle = world.CreateSystem<DivineHandTelemetrySystem>();
            ref var systemState = ref world.Unmanaged.ResolveSystemStateRef(systemHandle);
            world.Unmanaged.GetUnsafeSystemRef<DivineHandTelemetrySystem>(systemHandle).OnCreate(ref systemState);
            world.Unmanaged.GetUnsafeSystemRef<DivineHandTelemetrySystem>(systemHandle).OnUpdate(ref systemState);

            var metrics = entityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
            var targetKey = (FixedString64Bytes)"puredots.hand.avgHeldAmount";
            bool metricFound = false;
            for (int i = 0; i < metrics.Length; i++)
            {
                var metric = metrics[i];
                if (metric.Key.Equals(targetKey))
                {
                    metricFound = true;
                    Assert.Greater(metric.Value, 0f, "Telemetry metric should report held payload.");
                }
            }

            Assert.IsTrue(metricFound, "Telemetry metric 'puredots.hand.avgHeldAmount' was not emitted.");

            world.Unmanaged.GetUnsafeSystemRef<DivineHandTelemetrySystem>(systemHandle).OnDestroy(ref systemState);
        }

        [Test]
        public void EventBridgeDispatchesTypeAndAmountEvents()
        {
            using var world = new World("BridgeWorld");
            World.DefaultGameObjectInjectionWorld = world;
            var entityManager = world.EntityManager;

            var handEntity = entityManager.CreateEntity();
            var events = entityManager.AddBuffer<DivineHandEvent>(handEntity);
            events.Add(DivineHandEvent.StateChange(HandInteractionState.Idle, HandInteractionState.Holding));
            events.Add(DivineHandEvent.TypeChange(7));
            events.Add(DivineHandEvent.AmountChange(50, 200));

            var go = new GameObject("DivineHandEventBridge");
            var bridge = go.AddComponent<DivineHandEventBridge>();

            HandInteractionState fromState = HandInteractionState.Idle;
            HandInteractionState toState = HandInteractionState.Idle;
            ushort? observedType = null;
            int observedAmount = -1;
            int observedCapacity = -1;

            bridge.HandStateChanged += (from, to) =>
            {
                fromState = from;
                toState = to;
            };
            bridge.HandTypeChanged += type => observedType = type;
            bridge.HandAmountChanged += (amount, capacity) =>
            {
                observedAmount = amount;
                observedCapacity = capacity;
            };

            var dispatch = typeof(DivineHandEventBridge).GetMethod("DispatchEvents", BindingFlags.Instance | BindingFlags.NonPublic);
            dispatch!.Invoke(bridge, null);

            Assert.AreEqual(HandInteractionState.Idle, fromState);
            Assert.AreEqual(HandInteractionState.Holding, toState);
            Assert.AreEqual(7, observedType, "TypeChanged event not observed.");
            Assert.AreEqual(50, observedAmount, "AmountChanged event should report amount > 0.");
            Assert.AreEqual(200, observedCapacity, "AmountChanged event should report capacity.");

            GameObject.DestroyImmediate(go);
            World.DefaultGameObjectInjectionWorld = null;
        }
    }
}
#endif



