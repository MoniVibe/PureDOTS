using NUnit.Framework;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Transport;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public class TransportRegistryTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TransportRegistryTests");
            _entityManager = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void TransportRegistryBuilders_ProduceDeterministicOrderAndSummaries()
        {
            var minerRegistryEntity = _entityManager.CreateEntity(typeof(MinerVesselRegistry));
            var minerBuffer = _entityManager.AddBuffer<MinerVesselRegistryEntry>(minerRegistryEntity);

            using (var builder = new DeterministicRegistryBuilder<MinerVesselRegistryEntry>(4, Allocator.Temp))
            {
                var vesselA = _entityManager.CreateEntity();
                var vesselB = _entityManager.CreateEntity();
                var vesselC = _entityManager.CreateEntity();

                builder.Add(new MinerVesselRegistryEntry
                {
                    VesselEntity = vesselC,
                    Position = new float3(5f, 0f, 0f),
                    CellId = 9,
                    SpatialVersion = 3,
                    Capacity = 120f,
                    Load = 60f,
                    ResourceTypeIndex = 1,
                    Flags = TransportUnitFlags.Assigned | TransportUnitFlags.Carrying,
                    LastCommandTick = 11u
                });
                builder.Add(new MinerVesselRegistryEntry
                {
                    VesselEntity = vesselA,
                    Position = float3.zero,
                    CellId = -1,
                    SpatialVersion = 0,
                    Capacity = 80f,
                    Load = 0f,
                    ResourceTypeIndex = 2,
                    Flags = TransportUnitFlags.Idle,
                    LastCommandTick = 5u
                });
                builder.Add(new MinerVesselRegistryEntry
                {
                    VesselEntity = vesselB,
                    Position = new float3(2f, 0f, 1f),
                    CellId = 5,
                    SpatialVersion = 1,
                    Capacity = 100f,
                    Load = 20f,
                    ResourceTypeIndex = 3,
                    Flags = TransportUnitFlags.Assigned,
                    LastCommandTick = 9u
                });

                var minerAccumulator = new MinerVesselAccumulator();
                builder.ApplyTo(ref minerBuffer, ref minerAccumulator);

                Assert.AreEqual(3, minerBuffer.Length);
                Assert.That(minerBuffer[0].VesselEntity, Is.EqualTo(vesselA));
                Assert.That(minerBuffer[1].VesselEntity, Is.EqualTo(vesselB));
                Assert.That(minerBuffer[2].VesselEntity, Is.EqualTo(vesselC));
                Assert.AreEqual(3, minerAccumulator.Total);
                Assert.AreEqual(1, minerAccumulator.Available);
                Assert.AreEqual(300f, minerAccumulator.TotalCapacity, 1e-3f);
            }

            var haulerEntity = _entityManager.CreateEntity(typeof(HaulerRegistry));
            var haulerBuffer = _entityManager.AddBuffer<HaulerRegistryEntry>(haulerEntity);

            using (var builder = new DeterministicRegistryBuilder<HaulerRegistryEntry>(3, Allocator.Temp))
            {
                var hauler0 = _entityManager.CreateEntity();
                var hauler1 = _entityManager.CreateEntity();

                builder.Add(new HaulerRegistryEntry
                {
                    HaulerEntity = hauler1,
                    Position = new float3(1f, 0f, 3f),
                    CellId = 7,
                    SpatialVersion = 2,
                    CargoTypeIndex = 4,
                    ReservedCapacity = 40f,
                    EstimatedTravelTime = 12f,
                    RouteId = 7,
                    Flags = TransportUnitFlags.Assigned
                });
                builder.Add(new HaulerRegistryEntry
                {
                    HaulerEntity = hauler0,
                    Position = new float3(-2f, 0f, -2f),
                    CellId = -1,
                    SpatialVersion = 0,
                    CargoTypeIndex = 6,
                    ReservedCapacity = 0f,
                    EstimatedTravelTime = 5f,
                    RouteId = 3,
                    Flags = TransportUnitFlags.Idle
                });

                var accumulator = new HaulerAccumulator();
                builder.ApplyTo(ref haulerBuffer, ref accumulator);

                Assert.AreEqual(2, haulerBuffer.Length);
            }

            var freighterEntity = _entityManager.CreateEntity(typeof(FreighterRegistry));
            var freighterBuffer = _entityManager.AddBuffer<FreighterRegistryEntry>(freighterEntity);

            using (var builder = new DeterministicRegistryBuilder<FreighterRegistryEntry>(2, Allocator.Temp))
            {
                var freighterA = _entityManager.CreateEntity();
                var freighterB = _entityManager.CreateEntity();

                builder.Add(new FreighterRegistryEntry
                {
                    FreighterEntity = freighterB,
                    Position = new float3(10f, 0f, 10f),
                    CellId = 12,
                    SpatialVersion = 2,
                    Destination = new float3(50f, 0f, -20f),
                    ManifestId = "Manifest-002",
                    PayloadCapacity = 400f,
                    PayloadLoaded = 200f,
                    Flags = TransportUnitFlags.Carrying
                });

                builder.Add(new FreighterRegistryEntry
                {
                    FreighterEntity = freighterA,
                    Position = float3.zero,
                    CellId = -1,
                    SpatialVersion = 0,
                    Destination = new float3(100f, 0f, 25f),
                    ManifestId = "Manifest-001",
                    PayloadCapacity = 500f,
                    PayloadLoaded = 0f,
                    Flags = TransportUnitFlags.Idle
                });

                var accumulator = new FreighterAccumulator();
                builder.ApplyTo(ref freighterBuffer, ref accumulator);

                Assert.AreEqual(2, freighterBuffer.Length);
                Assert.AreEqual("Manifest-001", freighterBuffer[0].ManifestId.ToString());
                Assert.AreEqual(900f, accumulator.TotalCapacity, 1e-3f);
            }

            var wagonEntity = _entityManager.CreateEntity(typeof(WagonRegistry));
            var wagonBuffer = _entityManager.AddBuffer<WagonRegistryEntry>(wagonEntity);

            using (var builder = new DeterministicRegistryBuilder<WagonRegistryEntry>(2, Allocator.Temp))
            {
                var wagonA = _entityManager.CreateEntity();
                var wagonB = _entityManager.CreateEntity();
                var villager = _entityManager.CreateEntity();

                builder.Add(new WagonRegistryEntry
                {
                    WagonEntity = wagonB,
                    AssignedVillager = villager,
                    Position = new float3(8f, 0f, 4f),
                    CellId = 10,
                    SpatialVersion = 3,
                    CargoCapacity = 60f,
                    CargoReserved = 20f,
                    Flags = TransportUnitFlags.Assigned
                });

                builder.Add(new WagonRegistryEntry
                {
                    WagonEntity = wagonA,
                    AssignedVillager = Entity.Null,
                    Position = new float3(-3f, 0f, 7f),
                    CellId = -1,
                    SpatialVersion = 0,
                    CargoCapacity = 40f,
                    CargoReserved = 0f,
                    Flags = TransportUnitFlags.Idle
                });

                var accumulator = new WagonAccumulator();
                builder.ApplyTo(ref wagonBuffer, ref accumulator);

                Assert.AreEqual(2, wagonBuffer.Length);
                Assert.AreEqual(100f, accumulator.TotalCapacity, 1e-3f);
                Assert.AreEqual(1, accumulator.Available);
            }
        }

        private struct MinerVesselAccumulator : IRegistryAccumulator<MinerVesselRegistryEntry>
        {
            public int Total;
            public int Available;
            public float TotalCapacity;

            public void Accumulate(in MinerVesselRegistryEntry entry)
            {
                Total++;
                TotalCapacity += entry.Capacity;
                if ((entry.Flags & TransportUnitFlags.Assigned) == 0)
                {
                    Available++;
                }
            }
        }

        private struct HaulerAccumulator : IRegistryAccumulator<HaulerRegistryEntry>
        {
            public int Total;
            public void Accumulate(in HaulerRegistryEntry entry)
            {
                Total++;
            }
        }

        private struct FreighterAccumulator : IRegistryAccumulator<FreighterRegistryEntry>
        {
            public float TotalCapacity;

            public void Accumulate(in FreighterRegistryEntry entry)
            {
                TotalCapacity += entry.PayloadCapacity;
            }
        }

        private struct WagonAccumulator : IRegistryAccumulator<WagonRegistryEntry>
        {
            public float TotalCapacity;
            public int Available;

            public void Accumulate(in WagonRegistryEntry entry)
            {
                TotalCapacity += entry.CargoCapacity;
                if ((entry.Flags & TransportUnitFlags.Assigned) == 0)
                {
                    Available++;
                }
            }
        }
    }
}
