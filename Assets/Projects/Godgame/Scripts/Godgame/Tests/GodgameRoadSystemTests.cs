using Godgame.Roads;
using Godgame.Systems;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using PureDOTS.Tests;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Tests
{
    public class GodgameRoadSystemTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("GodgameRoadTests");
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void BootstrapSpawnsRoadSegmentsWithHandles()
        {
            CreateRoadConfig();
            var center = _entityManager.CreateEntity(typeof(GodgameVillageCenter), typeof(LocalTransform));
            _entityManager.SetComponentData(center, new GodgameVillageCenter
            {
                RoadRingRadius = 4f,
                BaseHeight = 0f
            });
            _entityManager.SetComponentData(center, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            _world.UpdateSystem<GodgameVillageRoadBootstrapSystem>();

            var roadQuery = _entityManager.CreateEntityQuery(typeof(GodgameRoadSegment));
            Assert.AreEqual(4, roadQuery.CalculateEntityCount());

            var handleQuery = _entityManager.CreateEntityQuery(typeof(GodgameRoadHandle));
            Assert.AreEqual(8, handleQuery.CalculateEntityCount());
        }

        [Test]
        public void StretchSystemUpdatesRoadFromHandle()
        {
            CreateRoadConfig();
            var center = _entityManager.CreateEntity(typeof(GodgameVillageCenter), typeof(LocalTransform));
            _entityManager.SetComponentData(center, new GodgameVillageCenter
            {
                RoadRingRadius = 4f,
                BaseHeight = 0f
            });
            _entityManager.SetComponentData(center, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            _world.UpdateSystem<GodgameVillageRoadBootstrapSystem>();

            var handleQuery = _entityManager.CreateEntityQuery(typeof(GodgameRoadHandle), typeof(LocalTransform));
            var handles = handleQuery.ToEntityArray(Allocator.Temp);
            var handle = handles[0];
            handles.Dispose();

            var road = _entityManager.GetComponentData<GodgameRoadHandle>(handle).Road;
            var segment = _entityManager.GetComponentData<GodgameRoadSegment>(road);
            float originalLength = math.length(segment.End - segment.Start);

            var handleTransform = _entityManager.GetComponentData<LocalTransform>(handle);
            handleTransform.Position += new float3(5f, 0f, 0f);
            _entityManager.SetComponentData(handle, handleTransform);
            _entityManager.AddComponentData(handle, new HandHeldTag { Holder = Entity.Null });

            _world.UpdateSystem<GodgameRoadStretchSystem>();

            segment = _entityManager.GetComponentData<GodgameRoadSegment>(road);
            float newLength = math.length(segment.End - segment.Start);
            Assert.Greater(newLength, originalLength);
        }

        [Test]
        public void HeatmapTriggersAutoBuild()
        {
            CreateRoadConfig(threshold: 0.5f);
            var heatEntity = _entityManager.CreateEntity(typeof(GodgameRoadHeatMap));
            _entityManager.AddBuffer<GodgameRoadHeatCell>(heatEntity);

            var villager = _entityManager.CreateEntity(typeof(VillagerMovement), typeof(LocalTransform));
                _entityManager.SetComponentData(villager, new VillagerMovement
            {
                Velocity = new float3(1f, 0f, 0f),
                BaseSpeed = 2f,
                CurrentSpeed = 2f,
                IsMoving = 1
            });
            _entityManager.SetComponentData(villager, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            var center = _entityManager.CreateEntity(typeof(GodgameVillageCenter), typeof(LocalTransform));
            _entityManager.SetComponentData(center, new GodgameVillageCenter
            {
                RoadRingRadius = 4f,
                BaseHeight = 0f
            });
            _entityManager.SetComponentData(center, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            _world.UpdateSystem<GodgameRoadHeatmapSystem>();
            _world.UpdateSystem<GodgameRoadHeatmapSystem>(); // accumulate twice
            _world.UpdateSystem<GodgameRoadAutoBuildSystem>();

            var roadQuery = _entityManager.CreateEntityQuery(typeof(GodgameRoadSegment));
            Assert.GreaterOrEqual(roadQuery.CalculateEntityCount(), 1);
        }

        private void CreateRoadConfig(float threshold = 6f)
        {
            var config = _entityManager.CreateEntity(typeof(GodgameRoadConfig));
            _entityManager.SetComponentData(config, new GodgameRoadConfig
            {
                DefaultRoadWidth = 2.5f,
                InitialStretchLength = 5f,
                RoadMeshBaseLength = 4f,
                HandleMass = 0.1f,
                HandleFollowLerp = 0.5f,
                HeatCellSize = 2f,
                HeatDecayPerSecond = 0.01f,
                HeatBuildThreshold = threshold,
                AutoBuildLength = 6f,
                RoadDescriptor = default,
                HandleDescriptor = default
            });
        }
    }
}
