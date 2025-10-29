using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using PureDOTS.Tests;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    public class MiracleRegistryTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("MiracleRegistryTests");
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            var timeEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 240;
            _entityManager.SetComponentData(timeEntity, timeState);
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
        public void MiracleRegistrySystem_PopulatesEntries()
        {
            var miracleA = CreateMiracle(
                type: MiracleType.Rain,
                castingMode: MiracleCastingMode.Sustained,
                lifecycle: MiracleLifecycleState.Active,
                target: new float3(5f, 0f, 0f),
                radius: 12f,
                intensity: 0.8f,
                cooldown: 4f,
                energyCost: 15f,
                chargePercent: 75f);

            CreateMiracle(
                type: MiracleType.Fireball,
                castingMode: MiracleCastingMode.Token,
                lifecycle: MiracleLifecycleState.CoolingDown,
                target: new float3(-3f, 0f, 7f),
                radius: 6f,
                intensity: 1.5f,
                cooldown: 10f,
                energyCost: 25f,
                chargePercent: 20f);

            _world.UpdateSystem<MiracleRegistrySystem>();

            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<MiracleRegistry>()).GetSingletonEntity();
            var registry = _entityManager.GetComponentData<MiracleRegistry>(registryEntity);
            var entries = _entityManager.GetBuffer<MiracleRegistryEntry>(registryEntity);
            var metadata = _entityManager.GetComponentData<RegistryMetadata>(registryEntity);

            Assert.AreEqual(2, registry.TotalMiracles);
            Assert.AreEqual(1, registry.ActiveMiracles);
            Assert.AreEqual(1, registry.SustainedMiracles);
            Assert.AreEqual(1, registry.CoolingMiracles);
            Assert.Greater(registry.TotalEnergyCost, 0f);
            Assert.Greater(registry.TotalCooldownSeconds, 0f);

            Assert.AreEqual(entries.Length, metadata.EntryCount);
            Assert.Greater(metadata.Version, 0u);
            Assert.IsTrue(metadata.Continuity.HasSpatialData);

            Assert.AreEqual(MiracleType.Rain, entries[0].Type);
            Assert.AreEqual(MiracleLifecycleState.Active, entries[0].Lifecycle);
            Assert.AreEqual(MiracleRegistryFlags.Active | MiracleRegistryFlags.Sustained, entries[0].Flags);
            Assert.AreEqual(75f, entries[0].ChargePercent, 0.01f);
            Assert.AreEqual(12f, entries[0].CurrentRadius, 0.01f);

            Assert.AreEqual(MiracleLifecycleState.CoolingDown, entries[1].Lifecycle);
            Assert.AreEqual(MiracleRegistryFlags.CoolingDown, entries[1].Flags);
        }

        private Entity CreateMiracle(
            MiracleType type,
            MiracleCastingMode castingMode,
            MiracleLifecycleState lifecycle,
            float3 target,
            float radius,
            float intensity,
            float cooldown,
            float energyCost,
            float chargePercent)
        {
            var entity = _entityManager.CreateEntity(
                typeof(MiracleDefinition),
                typeof(MiracleRuntimeState),
                typeof(MiracleTarget),
                typeof(MiracleCaster),
                typeof(LocalTransform));

            _entityManager.SetComponentData(entity, new MiracleDefinition
            {
                Type = type,
                CastingMode = castingMode,
                BaseRadius = radius,
                BaseIntensity = intensity,
                BaseCost = energyCost,
                SustainedCostPerSecond = castingMode == MiracleCastingMode.Sustained ? energyCost * 0.25f : 0f
            });

            _entityManager.SetComponentData(entity, new MiracleRuntimeState
            {
                Lifecycle = lifecycle,
                ChargePercent = chargePercent,
                CurrentRadius = radius,
                CurrentIntensity = intensity,
                CooldownSecondsRemaining = cooldown,
                LastCastTick = 200u,
                AlignmentDelta = 0
            });

            _entityManager.SetComponentData(entity, new MiracleTarget
            {
                TargetPosition = target,
                TargetEntity = Entity.Null
            });

            _entityManager.SetComponentData(entity, new MiracleCaster
            {
                CasterEntity = Entity.Null
            });

            _entityManager.SetComponentData(entity, LocalTransform.FromPosition(target));

            return entity;
        }
    }
}
