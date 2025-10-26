using System;
using NUnit.Framework;
using PureDOTS.Runtime.Registry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public class RegistryUtilitiesTests
    {
        struct TestRegistryEntry : IBufferElementData, IComparable<TestRegistryEntry>, IRegistryEntry, IRegistryFlaggedEntry
        {
            public Entity RegistryEntity { get; set; }
            public int Value;
            public byte RegistryFlags { get; set; }

            public int CompareTo(TestRegistryEntry other)
            {
                return Value.CompareTo(other.Value);
            }
        }

        [Test]
        public void DeterministicRegistryBuilder_SortsEntriesAndMarksMetadata()
        {
            using var world = new World("RegistryBuilderTest");
            var entityManager = world.EntityManager;
            var registryEntity = entityManager.CreateEntity(typeof(TestRegistryEntry), typeof(RegistryMetadata));
            var buffer = entityManager.AddBuffer<TestRegistryEntry>(registryEntity);
            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            metadata.Initialise(RegistryKind.Resource, 0, RegistryHandleFlags.SupportsSpatialQueries, "test");
            entityManager.SetComponentData(registryEntity, metadata);

            using var builder = new DeterministicRegistryBuilder<TestRegistryEntry>(3, Allocator.Temp);
            builder.Add(new TestRegistryEntry { RegistryEntity = Entity.Null, Value = 7 });
            builder.Add(new TestRegistryEntry { RegistryEntity = Entity.Null, Value = 3 });
            builder.Add(new TestRegistryEntry { RegistryEntity = Entity.Null, Value = 5 });

            var newMetadata = metadata;
            builder.ApplyTo(ref buffer, ref newMetadata, 10u);

            Assert.AreEqual(3, buffer.Length);
            Assert.That(buffer[0].Value, Is.EqualTo(3));
            Assert.That(buffer[1].Value, Is.EqualTo(5));
            Assert.That(buffer[2].Value, Is.EqualTo(7));
            Assert.AreEqual(metadata.Version + 1, newMetadata.Version);
            Assert.AreEqual(10u, newMetadata.LastUpdateTick);
        }

        [Test]
        public void RegistryFlagAccumulator_CountsMatchingEntries()
        {
            var accumulator = new RegistryFlagAccumulator<TestRegistryEntry> { Mask = 0b_0000_0010 };
            using var builder = new DeterministicRegistryBuilder<TestRegistryEntry>(3, Allocator.Temp);
            builder.Add(new TestRegistryEntry { RegistryEntity = Entity.Null, RegistryFlags = 0b0000_0010, Value = 1 });
            builder.Add(new TestRegistryEntry { RegistryEntity = Entity.Null, RegistryFlags = 0b0000_0001, Value = 2 });
            builder.Add(new TestRegistryEntry { RegistryEntity = Entity.Null, RegistryFlags = 0b0000_0010, Value = 3 });

            using var tempWorld = new World("AccumulatorWorld");
            var entityManager = tempWorld.EntityManager;
            var entity = entityManager.CreateEntity();
            var runtimeBuffer = entityManager.AddBuffer<TestRegistryEntry>(entity);

            builder.ApplyTo(ref runtimeBuffer, ref accumulator);

            Assert.AreEqual(2, accumulator.MatchingCount);
        }
    }
}
