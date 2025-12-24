#if INCLUDE_GODGAME_IN_PUREDOTS
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
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
        private uint _gridVersion;

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

            ConfigureSpatialGrid();
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
                target: new float3(2048f, 0f, 2048f),
                radius: 12f,
                intensity: 0.8f,
                cooldown: 4f,
                energyCost: 15f,
                chargePercent: 75f,
                addResidency: true,
                residencyCellId: 8);

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

            Assert.AreEqual(8, entries[0].TargetCellId, "Miracles with SpatialGridResidency should publish their resolved cell.");
            Assert.AreEqual(_gridVersion, entries[0].SpatialVersion);
            Assert.AreEqual(1, registry.SpatialResolvedCount);
            Assert.AreEqual(registry.SpatialResolvedCount, metadata.Continuity.SpatialResolvedCount);
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
            float chargePercent,
            bool addResidency = false,
            int residencyCellId = 0)
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

            if (addResidency)
            {
                _entityManager.AddComponentData(entity, new SpatialGridResidency
                {
                    CellId = residencyCellId,
                    LastPosition = target,
                    Version = _gridVersion
                });
            }

            return entity;
        }

        private void ConfigureSpatialGrid()
        {
            var gridEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>()).GetSingletonEntity();
            var config = _entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            config.WorldMin = new float3(-32f, 0f, -32f);
            config.WorldMax = new float3(32f, 0f, 32f);
            config.CellSize = 2f;
            config.CellCounts = (int3)math.max(new int3(1, 1, 1), math.ceil(math.abs(config.WorldMax - config.WorldMin) / config.CellSize));
            _entityManager.SetComponentData(gridEntity, config);

            var state = _entityManager.GetComponentData<SpatialGridState>(gridEntity);
            state.Version = math.max(1u, state.Version + 1u);
            _gridVersion = state.Version;
            _entityManager.SetComponentData(gridEntity, state);
        }
    }
}
#endif
