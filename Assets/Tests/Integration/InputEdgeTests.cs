using NUnit.Framework;
using PureDOTS.Runtime.Camera;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Hand;
using PureDOTS.Input;
using PureDOTS.Systems;
using PureDOTS.Systems.Input;
using PureDOTS.Tests;
using PureDOTS.Tests.Playmode;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.Integration
{
    public class InputEdgeTests : EcsTestFixture
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
        }

        [Test]
        public void HandInputEdge_CapturesDownAndUpEvents()
        {
            // Create hand entity with input components
            var handEntity = EntityManager.CreateEntity(
                typeof(DivineHandTag),
                typeof(DivineHandConfig),
                typeof(DivineHandInput),
                typeof(HandInputEdge),
                typeof(GodIntent));

            var handInput = new DivineHandInput
            {
                SampleTick = 10,
                PointerPosition = new float2(0.5f, 0.5f),
                CursorWorldPosition = new float3(0, 0, 0)
            };
            EntityManager.SetComponentData(handEntity, handInput);

            // Add edge events
            var edges = EntityManager.GetBuffer<HandInputEdge>(handEntity);
            edges.Add(new HandInputEdge
            {
                Button = InputButton.Primary,
                Kind = InputEdgeKind.Down,
                Tick = 10,
                PointerPosition = new float2(0.5f, 0.5f)
            });
            edges.Add(new HandInputEdge
            {
                Button = InputButton.Primary,
                Kind = InputEdgeKind.Up,
                Tick = 15,
                PointerPosition = new float2(0.6f, 0.6f)
            });

            // Process intent mapping
            World.GetOrCreateSystem<IntentMappingSystem>();
            var timeState = EntityManager.GetSingleton<TimeState>();
            timeState.Tick = 10;
            EntityManager.SetSingleton(timeState);
            World.Update();

            // Verify intent was set
            var intent = EntityManager.GetComponentData<GodIntent>(handEntity);
            Assert.AreEqual(1, intent.StartSelect, "StartSelect should be set on Down event");

            // Process Up event
            timeState.Tick = 15;
            EntityManager.SetSingleton(timeState);
            World.Update();

            intent = EntityManager.GetComponentData<GodIntent>(handEntity);
            Assert.AreEqual(1, intent.CancelAction, "CancelAction should be set on Up event without charge");
        }

        [Test]
        public void IntentMapping_RespectsUIBlocking()
        {
            var handEntity = EntityManager.CreateEntity(
                typeof(DivineHandTag),
                typeof(DivineHandConfig),
                typeof(DivineHandInput),
                typeof(HandInputEdge),
                typeof(GodIntent));

            var handInput = new DivineHandInput
            {
                SampleTick = 10,
                PointerPosition = new float2(0.5f, 0.5f),
                CursorWorldPosition = new float3(0, 0, 0),
                PointerOverUI = 1 // Blocked by UI
            };
            EntityManager.SetComponentData(handEntity, handInput);

            var edges = EntityManager.GetBuffer<HandInputEdge>(handEntity);
            edges.Add(new HandInputEdge
            {
                Button = InputButton.Primary,
                Kind = InputEdgeKind.Down,
                Tick = 10,
                PointerPosition = new float2(0.5f, 0.5f)
            });

            World.GetOrCreateSystem<IntentMappingSystem>();
            var timeState = EntityManager.GetSingleton<TimeState>();
            timeState.Tick = 10;
            EntityManager.SetSingleton(timeState);
            World.Update();

            // Verify intent is cleared when over UI
            var intent = EntityManager.GetComponentData<GodIntent>(handEntity);
            Assert.AreEqual(0, intent.StartSelect, "StartSelect should be cleared when pointer is over UI");
        }

        [Test]
        public void CameraInputEdge_TriggersOrbitIntent()
        {
            var cameraEntity = EntityManager.CreateEntity(
                typeof(CameraTag),
                typeof(CameraConfig),
                typeof(CameraInputState),
                typeof(CameraInputEdge),
                typeof(GodIntent));

            var cameraInput = new CameraInputState
            {
                SampleTick = 10,
                PointerPosition = new float2(0.5f, 0.5f)
            };
            EntityManager.SetComponentData(cameraEntity, cameraInput);

            var edges = EntityManager.GetBuffer<CameraInputEdge>(cameraEntity);
            edges.Add(new CameraInputEdge
            {
                Button = InputButton.Middle,
                Kind = InputEdgeKind.Down,
                Tick = 10,
                PointerPosition = new float2(0.5f, 0.5f)
            });

            World.GetOrCreateSystem<IntentMappingSystem>();
            var timeState = EntityManager.GetSingleton<TimeState>();
            timeState.Tick = 10;
            EntityManager.SetSingleton(timeState);
            World.Update();

            var intent = EntityManager.GetComponentData<GodIntent>(cameraEntity);
            Assert.AreEqual(1, intent.StartOrbit, "StartOrbit should be set on Middle button Down");
        }
    }
}
