using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Collections;

namespace PureDOTS.Tests
{
    public class AbilityRegistrySystemTests
    {
        [Test]
        public void AbilityRegistrySystem_PopulatesEntries()
        {
            using var world = new World("AbilityRegistrySystemTests");
            var entityManager = world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(entityManager);

            var timeEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                Tick = 5,
                IsPaused = false,
                FixedDeltaTime = 0.016f,
                CurrentSpeedMultiplier = 1f
            });

            var rewindEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                PlaybackTick = 0
            });

            var abilityEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(abilityEntity, new AbilityId { Value = (FixedString64Bytes)"Meteor" });
            entityManager.AddComponentData(abilityEntity, new AbilityState
            {
                CooldownRemaining = 0f,
                Charges = 2,
                Flags = AbilityStatusFlags.Ready,
                Owner = Entity.Null
            });

            world.UpdateSystem<AbilityRegistrySystem>();
            world.UpdateSystem<RegistryDirectorySystem>();

            var registryEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<AbilityRegistry>()).GetSingletonEntity();
            var registry = entityManager.GetComponentData<AbilityRegistry>(registryEntity);
            Assert.AreEqual(1, registry.TotalAbilities);
            Assert.AreEqual(1, registry.ReadyAbilityCount);

            var entries = entityManager.GetBuffer<AbilityRegistryEntry>(registryEntity);
            Assert.AreEqual(1, entries.Length);
            var entry = entries[0];
            Assert.AreEqual(abilityEntity, entry.AbilityEntity);
            Assert.AreEqual((FixedString64Bytes)"Meteor", entry.AbilityId);
            Assert.AreEqual(0f, entry.CooldownRemaining, 0.0001f);
            Assert.AreEqual(2, entry.Charges);
            Assert.AreEqual(AbilityStatusFlags.Ready, entry.Flags);

            var metadata = entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(RegistryKind.Ability, metadata.Kind);
            Assert.AreEqual(1, metadata.EntryCount);
        }
    }
}
