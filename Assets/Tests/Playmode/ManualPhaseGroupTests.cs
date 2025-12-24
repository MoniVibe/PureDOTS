#if PUREDOTS_LEGACY_CAMERA
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PureDOTS.Runtime.Config;
using PureDOTS.Systems;
using Unity.Entities;

namespace PureDOTS.Tests
{
    public class ManualPhaseGroupTests
    {
        private World _world;
        private EntityManager _entityManager;
        private string _configFilePath;

        [SetUp]
        public void SetUp()
        {
            RuntimeConfigRegistry.ResetForTests();
            _configFilePath = Path.Combine(Path.GetTempPath(), $"puredots_config_{Guid.NewGuid():N}.cfg");
            RuntimeConfigRegistry.StoragePath = _configFilePath;
            RuntimeConfigRegistry.Initialize();

            _world = new World("ManualPhaseGroupTests");
            _entityManager = _world.EntityManager;

            // Ensure baseline groups exist.
            _world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _world.GetOrCreateSystemManaged<Unity.Entities.LateSimulationSystemGroup>();
            _world.GetOrCreateSystemManaged<CameraInputSystemGroup>();
            _world.GetOrCreateSystemManaged<GameplaySystemGroup>();

            // Phase groups under test.
            _world.GetOrCreateSystemManaged<CameraPhaseGroup>();
            _world.GetOrCreateSystemManaged<TransportPhaseGroup>();
            _world.GetOrCreateSystemManaged<HistoryPhaseGroup>();
            _world.GetOrCreateSystemManaged<HistorySystemGroup>();

            // Bootstrap manual phase control singletons.
            _world.CreateSystemManaged<ManualPhaseBootstrapSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
            }
        }

        [Test]
        public void ManualPhaseController_AppliesControlToggles()
        {
            var controller = _world.CreateSystemManaged<ManualPhaseControllerSystem>();
            var transportGroup = _world.GetExistingSystemManaged<TransportPhaseGroup>();
            var cameraGroup = _world.GetExistingSystemManaged<CameraPhaseGroup>();

            RuntimeConfigRegistry.SetValue("camera.ecs.enabled", "1", out _);
            RuntimeConfigRegistry.SetValue("phase.transport.enabled", "0", out _);
            controller.Update();

            Assert.IsFalse(transportGroup.Enabled, "Transport phase group should be disabled by control toggle.");

            RuntimeConfigRegistry.SetValue("phase.transport.enabled", "1", out _);
            RuntimeConfigRegistry.SetValue("phase.camera.enabled", "0", out _);
            controller.Update();

            Assert.IsTrue(transportGroup.Enabled, "Transport phase group should be re-enabled.");
            Assert.IsFalse(cameraGroup.Enabled, "Camera phase group should reflect control toggle.");
        }

        [Test]
        public void TransportPhase_RunsBetweenSpatialAndGameplay()
        {
            var spatialGroup = _world.GetOrCreateSystemManaged<SpatialSystemGroup>();
            var gameplayGroup = _world.GetExistingSystemManaged<GameplaySystemGroup>();
            var transportGroup = _world.GetExistingSystemManaged<TransportPhaseGroup>();
            var simulationGroup = _world.GetExistingSystemManaged<SimulationSystemGroup>();

            var controller = _world.CreateSystemManaged<ManualPhaseControllerSystem>();
            controller.Update();

            ExecutionOrder.Clear();

            var tag = _entityManager.CreateEntity(typeof(OrderCaptureTag));

            var spatialMarker = _world.CreateSystemManaged<SpatialMarkerSystem>();
            var transportMarker = _world.CreateSystemManaged<TransportMarkerSystem>();
            var gameplayMarker = _world.CreateSystemManaged<GameplayMarkerSystem>();

            spatialGroup.AddSystemToUpdateList(spatialMarker);
            transportGroup.AddSystemToUpdateList(transportMarker);
            gameplayGroup.AddSystemToUpdateList(gameplayMarker);

            spatialGroup.SortSystems();
            transportGroup.SortSystems();
            gameplayGroup.SortSystems();
            simulationGroup.SortSystems();

            simulationGroup.Update();

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, ExecutionOrder, "Spatial, Transport, Gameplay order expected.");

            _entityManager.DestroyEntity(tag);
        }

        private static readonly List<int> ExecutionOrder = new List<int>(8);

        private struct OrderCaptureTag : IComponentData
        {
        }

        [UpdateInGroup(typeof(SpatialSystemGroup))]
        private sealed partial class SpatialMarkerSystem : SystemBase
        {
            protected override void OnCreate()
            {
                base.OnCreate();
                RequireForUpdate<OrderCaptureTag>();
            }

            protected override void OnUpdate()
            {
                ExecutionOrder.Add(0);
            }
        }

        [UpdateInGroup(typeof(TransportPhaseGroup))]
        private sealed partial class TransportMarkerSystem : SystemBase
        {
            protected override void OnCreate()
            {
                base.OnCreate();
                RequireForUpdate<OrderCaptureTag>();
            }

            protected override void OnUpdate()
            {
                ExecutionOrder.Add(1);
            }
        }

        [UpdateInGroup(typeof(GameplaySystemGroup))]
        private sealed partial class GameplayMarkerSystem : SystemBase
        {
            protected override void OnCreate()
            {
                base.OnCreate();
                RequireForUpdate<OrderCaptureTag>();
            }

            protected override void OnUpdate()
            {
                ExecutionOrder.Add(2);
            }
        }
    }
}
#endif

