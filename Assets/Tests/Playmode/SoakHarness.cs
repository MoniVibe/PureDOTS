using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Nightly soak harness validating registry rebuilds, debug HUD metrics, and telemetry output.
    /// </summary>
    public class SoakHarness
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("SoakHarnessWorld", WorldFlags.Editor | WorldFlags.Staging);
            _entityManager = _world.EntityManager;
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
        [Category("Soak")]
        public void MetaRegistrySoakHarness_RunsForMultipleTicks()
        {
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            const int factionCount = 48;
            const int hazardCount = 32;
            const int areaEffectCount = 40;
            const int cultureCount = 24;
            const int tickCount = 128;

            float totalFactionResources = 0f;
            int totalFactionTerritory = 0;
            float expectedHazardIntensity = 0f;
            float totalAreaStrength = 0f;
            float totalAlignment = 0f;

            for (int i = 0; i < factionCount; i++)
            {
                var entity = _entityManager.CreateEntity();
                var territory = 4 + (i % 5);
                var resources = 50f + i * 3.25f;

                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(i % 16, 0f, i / 16)));
                _entityManager.AddComponentData(entity, new FactionId
                {
                    Value = (ushort)(i + 1),
                    Name = new FixedString64Bytes($"Faction-{i}"),
                    Type = (FactionType)((i % 3) + 1)
                });
                _entityManager.AddComponentData(entity, new FactionState
                {
                    ResourceStockpile = resources,
                    PopulationCount = 100 + i,
                    TerritoryCellCount = territory,
                    DiplomaticStatus = DiplomaticStatusFlags.Allied,
                    TerritoryCenter = new float3(i % 16, 0f, i / 16)
                });

                totalFactionResources += resources;
                totalFactionTerritory += territory;
            }

            for (int i = 0; i < hazardCount; i++)
            {
                var entity = _entityManager.CreateEntity();
                var intensity = 0.02f + (i % 4) * 0.004f;
                expectedHazardIntensity += intensity;

                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(i % 8, 0f, i / 8)));
                _entityManager.AddComponentData(entity, new ClimateHazardState
                {
                    HazardType = ClimateHazardType.Storm,
                    CurrentIntensity = intensity,
                    Radius = 20f + i,
                    MaxIntensity = intensity + 0.1f,
                    StartTick = 0u,
                    DurationTicks = 5000u,
                    HazardName = new FixedString64Bytes($"Storm-{i}"),
                    AffectedEnvironmentChannels = EnvironmentChannelMask.Moisture | EnvironmentChannelMask.Wind
                });
            }
            expectedHazardIntensity = math.clamp(expectedHazardIntensity, 0f, 1f);

            for (int i = 0; i < areaEffectCount; i++)
            {
                var entity = _entityManager.CreateEntity();
                var strength = 0.5f + (i % 5) * 0.15f;
                totalAreaStrength += strength;

                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(i % 10, 0f, i / 10)));
                _entityManager.AddComponentData(entity, new AreaEffectState
                {
                    EffectType = AreaEffectType.Buff,
                    CurrentStrength = strength,
                    Radius = 12f + (i % 3),
                    MaxStrength = strength + 0.25f,
                    OwnerEntity = Entity.Null,
                    EffectId = (ushort)(100 + i),
                    AffectedArchetypes = AreaEffectTargetMask.Villagers,
                    EffectName = new FixedString64Bytes($"Aura-{i}"),
                    ExpirationTick = 10000u
                });
            }

            for (int i = 0; i < cultureCount; i++)
            {
                var entity = _entityManager.CreateEntity();
                var alignment = -0.4f + i * 0.03f;
                totalAlignment += alignment;

                _entityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(i % 6, 0f, i / 6)));
                _entityManager.AddComponentData(entity, new CultureState
                {
                    CultureId = (ushort)(200 + i),
                    CultureName = new FixedString64Bytes($"Culture-{i}"),
                    CultureType = CultureType.Technological,
                    MemberCount = 250 + i * 3,
                    CurrentAlignment = alignment,
                    AlignmentVelocity = 0.01f * (i % 4),
                    BaseAlignment = alignment * 0.8f,
                    AlignmentFlags = CultureAlignmentFlags.Stable,
                    Description = new FixedString128Bytes("Soak harness culture")
                });
            }

            var factionHandle = CreateSystem<FactionRegistrySystem>();
            var hazardHandle = CreateSystem<ClimateHazardRegistrySystem>();
            var areaHandle = CreateSystem<AreaEffectRegistrySystem>();
            var cultureHandle = CreateSystem<CultureAlignmentRegistrySystem>();
            var debugHandle = CreateSystem<DebugDisplaySystem>();

            for (int tick = 0; tick < tickCount; tick++)
            {
                AdvanceTick(_entityManager);
                RunSystem<FactionRegistrySystem>(factionHandle);
                RunSystem<ClimateHazardRegistrySystem>(hazardHandle);
                RunSystem<AreaEffectRegistrySystem>(areaHandle);
                RunSystem<CultureAlignmentRegistrySystem>(cultureHandle);
                RunSystem<DebugDisplaySystem>(debugHandle);
            }

            var debugQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>());
            var debugData = debugQuery.GetSingleton<DebugDisplayData>();

            Assert.GreaterOrEqual(debugData.FactionRegistryCount, 1, "Faction registry singleton should be populated.");
            Assert.AreEqual(factionCount, debugData.FactionEntryCount);
            Assert.AreEqual(totalFactionTerritory, debugData.TotalFactionTerritoryCells);
            Assert.That(debugData.TotalFactionResources, Is.EqualTo(totalFactionResources).Within(0.01f));

            Assert.AreEqual(hazardCount, debugData.ClimateHazardActiveCount);
            Assert.That(debugData.ClimateHazardGlobalIntensity, Is.EqualTo(expectedHazardIntensity).Within(0.001f));

            Assert.AreEqual(areaEffectCount, debugData.AreaEffectActiveCount);
            Assert.That(debugData.AreaEffectAverageStrength, Is.EqualTo(totalAreaStrength / areaEffectCount).Within(0.001f));

            Assert.GreaterOrEqual(debugData.CultureRegistryCount, 1);
            Assert.AreEqual(cultureCount, debugData.CultureEntryCount);
            Assert.That(debugData.CultureGlobalAlignmentScore, Is.EqualTo(totalAlignment / cultureCount).Within(0.001f));

            var telemetryQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TelemetryStream>());
            var telemetryEntity = telemetryQuery.GetSingletonEntity();
            var telemetryBuffer = _entityManager.GetBuffer<TelemetryMetric>(telemetryEntity);

            AssertMetricEquals(telemetryBuffer, new FixedString64Bytes("registry.faction.count"), debugData.FactionRegistryCount);
            AssertMetricEquals(telemetryBuffer, new FixedString64Bytes("registry.hazard.count"), debugData.ClimateHazardActiveCount);
            AssertMetricEquals(telemetryBuffer, new FixedString64Bytes("registry.area.count"), debugData.AreaEffectActiveCount);
            AssertMetricEquals(telemetryBuffer, new FixedString64Bytes("registry.culture.count"), debugData.CultureRegistryCount);
        }

        private static void AdvanceTick(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>());
            var timeEntity = query.GetSingletonEntity();
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick++;
            entityManager.SetComponentData(timeEntity, timeState);
        }

        private SystemHandle CreateSystem<T>() where T : unmanaged, ISystem
        {
            var handle = _world.CreateSystem<T>();
            ref var state = ref _world.Unmanaged.ResolveSystemStateRef(handle);
            ref var system = ref _world.Unmanaged.GetUnsafeSystemRef<T>(handle);
            system.OnCreate(ref state);
            return handle;
        }

        private void RunSystem<T>(SystemHandle handle) where T : unmanaged, ISystem
        {
            ref var system = ref _world.Unmanaged.GetUnsafeSystemRef<T>(handle);
            ref var state = ref _world.Unmanaged.ResolveSystemStateRef(handle);
            system.OnUpdate(ref state);
        }

        private static void AssertMetricEquals(DynamicBuffer<TelemetryMetric> buffer, in FixedString64Bytes key, float expected)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Key.Equals(key))
                {
                    Assert.That(buffer[i].Value, Is.EqualTo(expected).Within(0.001f), $"Telemetry metric {key} should match expected value.");
                    return;
                }
            }

            Assert.Fail($"Telemetry metric {key} not found in stream.");
        }
    }
}
