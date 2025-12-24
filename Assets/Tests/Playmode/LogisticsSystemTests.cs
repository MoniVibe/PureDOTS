#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Logistics;
using PureDOTS.Runtime.Logistics.Components;
using PureDOTS.Runtime.Space;
using PureDOTS.Systems;
using PureDOTS.Systems.Logistics;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Tests.Playmode
{
    public class LogisticsSystemTests
    {
        private World _world;
        private EntityManager EntityManager => _world.EntityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("LogisticsSystemTests", WorldFlags.Game);
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            EnsureTimeState();
            EnsureRewindState();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void ResourceLogisticsSystem_SetsOrderInTransit()
        {
            var shipmentEntity = CreateShipment(ShipmentStatus.InTransit);
            var orderEntity = CreateOrder(shipmentEntity, LogisticsOrderStatus.Reserved);

            var logisticsSystem = _world.GetOrCreateSystem<ResourceLogisticsSystem>();
            logisticsSystem.Update(_world.Unmanaged);

            var updatedOrder = EntityManager.GetComponentData<LogisticsOrder>(orderEntity);
            Assert.AreEqual(LogisticsOrderStatus.InTransit, updatedOrder.Status);
        }

        [Test]
        public void ResourceLogisticsSystem_SetsOrderDelivered()
        {
            var shipmentEntity = CreateShipment(ShipmentStatus.Delivered);
            var orderEntity = CreateOrder(shipmentEntity, LogisticsOrderStatus.InTransit);

            var logisticsSystem = _world.GetOrCreateSystem<ResourceLogisticsSystem>();
            logisticsSystem.Update(_world.Unmanaged);

            var updatedOrder = EntityManager.GetComponentData<LogisticsOrder>(orderEntity);
            Assert.AreEqual(LogisticsOrderStatus.Delivered, updatedOrder.Status);
        }

        private Entity CreateShipment(ShipmentStatus status)
        {
            var shipmentEntity = EntityManager.CreateEntity(typeof(Shipment));
            EntityManager.SetComponentData(shipmentEntity, new Shipment
            {
                ShipmentId = 1,
                AssignedTransport = Entity.Null,
                RouteEntity = Entity.Null,
                Status = status,
                RepresentationMode = ShipmentRepresentationMode.Abstract,
                AllocatedMass = 0f,
                AllocatedVolume = 0f,
                DepartureTick = 0,
                EstimatedArrivalTick = 10
            });
            return shipmentEntity;
        }

        private Entity CreateOrder(Entity shipmentEntity, LogisticsOrderStatus initialStatus)
        {
            var orderEntity = EntityManager.CreateEntity(typeof(LogisticsOrder));
            EntityManager.SetComponentData(orderEntity, new LogisticsOrder
            {
                OrderId = 99,
                Kind = LogisticsJobKind.Supply,
                SourceNode = Entity.Null,
                DestinationNode = Entity.Null,
                ResourceId = new FixedString64Bytes("wood"),
                RequestedAmount = 10f,
                ReservedAmount = 10f,
                Status = initialStatus,
                AssignedTransport = Entity.Null,
                ShipmentEntity = shipmentEntity,
                CreatedTick = 0,
                EarliestDepartTick = 0,
                LatestArrivalTick = 0,
                Priority = 0,
                Constraints = default
            });
            return orderEntity;
        }

        private void EnsureTimeState()
        {
            if (!EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).IsEmpty)
            {
                return;
            }

            var timeEntity = EntityManager.CreateEntity(typeof(TimeState), typeof(TickTimeState));
            EntityManager.SetComponentData(timeEntity, new TimeState
            {
                Tick = 1,
                FixedDeltaTime = 1f / 60f,
                DeltaTime = 1f / 60f,
                DeltaSeconds = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                ElapsedTime = 1f / 60f,
                WorldSeconds = 1f / 60f,
                IsPaused = false
            });
            EntityManager.SetComponentData(timeEntity, new TickTimeState
            {
                Tick = 1,
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                TargetTick = 1,
                IsPaused = false,
                IsPlaying = true,
                WorldSeconds = 1f / 60f
            });
        }

        private void EnsureRewindState()
        {
            if (!EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).IsEmpty)
            {
                return;
            }

            var rewindEntity = EntityManager.CreateEntity(typeof(RewindState), typeof(RewindLegacyState));
            EntityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record
            });
            EntityManager.SetComponentData(rewindEntity, new RewindLegacyState
            {
                CurrentTick = 1,
                StartTick = 1,
                PlaybackTick = 1,
                PlaybackSpeed = 1f,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = ScrubDirection.None,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });
        }
    }
}
#endif

