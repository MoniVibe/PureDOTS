using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Space;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems;
using PureDOTS.Systems.Space;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.EditMode
{
    /// <summary>
    /// Unit tests for planet systems.
    /// Tests appeal calculation, preference matching, and nested orbit validation.
    /// </summary>
    public class PlanetSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private Entity _planetEntity;
        private Entity _speciesEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PlanetSystemTests");
            _entityManager = _world.EntityManager;

            // Create required singletons
            EnsureTimeState();
            EnsureRewindState();

            // Create a test planet
            _planetEntity = _entityManager.CreateEntity();

            // Add planet components
            _entityManager.AddComponentData(_planetEntity, new PlanetFlavorComponent
            {
                Flavor = PlanetFlavor.Continental
            });

            _entityManager.AddComponentData(_planetEntity, new PlanetPhysicalProperties
            {
                Mass = 5.97e24f, // Earth mass
                Density = 5514f,
                Radius = 6.371e6f, // Earth radius
                SurfaceGravity = 9.8f // Earth gravity
            });

            _entityManager.AddComponentData(_planetEntity, new PlanetGravityField
            {
                SurfaceGravity = 9.8f,
                FalloffExponent = 2.0f,
                MaxDistance = 0f
            });

            var biomesBuffer = _entityManager.AddBuffer<PlanetBiome>(_planetEntity);
            biomesBuffer.Add(new PlanetBiome { BiomeType = 1, Coverage = 0.4f }); // Forest
            biomesBuffer.Add(new PlanetBiome { BiomeType = 2, Coverage = 0.3f }); // Plains
            biomesBuffer.Add(new PlanetBiome { BiomeType = 3, Coverage = 0.3f }); // Mountains

            var resourcesBuffer = _entityManager.AddBuffer<PlanetResource>(_planetEntity);
            resourcesBuffer.Add(new PlanetResource { ResourceType = Entity.Null, Amount = 100f, MaxAmount = 100f, RegenerationRate = 0f });

            _entityManager.AddComponentData(_planetEntity, new PlanetAppeal
            {
                AppealScore = 0f,
                BaseAppeal = 0f,
                BiomeDiversityBonus = 0f,
                ResourceRichnessBonus = 0f,
                HabitabilityPenalty = 0f,
                LastCalculationTick = 0
            });

            _entityManager.AddComponentData(_planetEntity, new PlanetParent
            {
                ParentPlanet = Entity.Null
            });

            _entityManager.AddBuffer<PlanetSatellite>(_planetEntity);
            _entityManager.AddBuffer<PlanetCompatibility>(_planetEntity);

            // Create a test species
            _speciesEntity = _entityManager.CreateEntity();

            var preferredFlavors = _entityManager.AddBuffer<PreferredPlanetFlavor>(_speciesEntity);
            preferredFlavors.Add(new PreferredPlanetFlavor { Flavor = PlanetFlavor.Continental, Weight = 1.0f });

            var preferredBiomes = _entityManager.AddBuffer<PreferredBiome>(_speciesEntity);
            preferredBiomes.Add(new PreferredBiome { BiomeType = 1, Weight = 0.5f }); // Forest
            preferredBiomes.Add(new PreferredBiome { BiomeType = 2, Weight = 0.5f }); // Plains

            _entityManager.AddComponentData(_speciesEntity, new SpeciesPlanetPreference
            {
                MinAppealThreshold = 0.5f,
                PreferredGravity = 9.8f,
                GravityTolerancePercent = 20f,
                ToleratesExtremeEnvironments = false
            });

            _entityManager.AddComponentData(_speciesEntity, SpeciesPreferenceWeights.Default);
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
        public void PlanetAppealSystem_CalculatesAppealCorrectly()
        {
            var appealSystem = _world.GetOrCreateSystem<PlanetAppealSystem>();

            // Update appeal system
            var timeState = GetTimeState();
            timeState.DeltaTime = 0f;
            timeState.Tick = 1;
            SetTimeState(timeState);

            appealSystem.Update(_world.Unmanaged);

            // Check that appeal was calculated
            var appeal = _entityManager.GetComponentData<PlanetAppeal>(_planetEntity);
            Assert.That(appeal.AppealScore, Is.GreaterThan(0f));
            Assert.That(appeal.BaseAppeal, Is.GreaterThan(0f)); // Continental should have base appeal
            Assert.That(appeal.BiomeDiversityBonus, Is.GreaterThan(0f)); // 3 biomes = diversity bonus
            Assert.That(appeal.ResourceRichnessBonus, Is.GreaterThan(0f)); // 1 resource = richness bonus
        }

        [Test]
        public void SpeciesPreferenceMatchingSystem_CalculatesCompatibilityCorrectly()
        {
            var appealSystem = _world.GetOrCreateSystem<PlanetAppealSystem>();
            var matchingSystem = _world.GetOrCreateSystem<SpeciesPreferenceMatchingSystem>();

            // First calculate appeal
            var timeState = GetTimeState();
            timeState.DeltaTime = 0f;
            timeState.Tick = 1;
            SetTimeState(timeState);

            appealSystem.Update(_world.Unmanaged);
            matchingSystem.Update(_world.Unmanaged);

            // Check that compatibility was calculated
            var compatibilityBuffer = _entityManager.GetBuffer<PlanetCompatibility>(_planetEntity);
            Assert.That(compatibilityBuffer.Length, Is.GreaterThan(0));

            // Find compatibility entry for our species
            bool found = false;
            for (int i = 0; i < compatibilityBuffer.Length; i++)
            {
                if (compatibilityBuffer[i].SpeciesEntity == _speciesEntity)
                {
                    var compat = compatibilityBuffer[i];
                    Assert.That(compat.CompatibilityScore, Is.GreaterThan(0f));
                    Assert.That(compat.FlavorMatchScore, Is.EqualTo(1f)); // Perfect flavor match
                    Assert.That(compat.IsHabitable, Is.True); // Should be habitable
                    found = true;
                    break;
                }
            }

            Assert.That(found, Is.True, "Compatibility entry for species should exist");
        }

        [Test]
        public void PlanetOrbitHierarchySystem_MaintainsSatelliteBuffers()
        {
            var hierarchySystem = _world.GetOrCreateSystem<PlanetOrbitHierarchySystem>();

            // Create a moon entity
            var moonEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(moonEntity, new PlanetParent
            {
                ParentPlanet = _planetEntity
            });

            _entityManager.AddComponentData(moonEntity, new OrbitParameters
            {
                OrbitalPeriodSeconds = 86400f,
                InitialPhase = 0f,
                OrbitNormal = new float3(0f, 1f, 0f),
                TimeOfDayOffset = 0f,
                ParentPlanet = _planetEntity
            });

            // Update hierarchy system
            var timeState = GetTimeState();
            timeState.DeltaTime = 0f;
            timeState.Tick = 1;
            SetTimeState(timeState);

            hierarchySystem.Update(_world.Unmanaged);

            // Check that moon was added to planet's satellite buffer
            var satellitesBuffer = _entityManager.GetBuffer<PlanetSatellite>(_planetEntity);
            Assert.That(satellitesBuffer.Length, Is.GreaterThan(0));

            bool moonFound = false;
            for (int i = 0; i < satellitesBuffer.Length; i++)
            {
                if (satellitesBuffer[i].SatelliteEntity == moonEntity)
                {
                    moonFound = true;
                    break;
                }
            }

            Assert.That(moonFound, Is.True, "Moon should be in planet's satellite buffer");
        }

        private void EnsureTimeState()
        {
            var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            if (query.IsEmpty)
            {
                var timeEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    DeltaTime = 0f,
                    ElapsedTime = 0f,
                    IsPaused = false,
                    FixedDeltaTime = 1f / 60f,
                    CurrentSpeedMultiplier = 1f
                });
            }
        }

        private void EnsureRewindState()
        {
            var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<RewindState>());
            if (query.IsEmpty)
            {
                var rewindEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(rewindEntity, new RewindState
                {
                    Mode = RewindMode.Record,
                    TargetTick = 0,
                    TickDuration = 1f / 60f,
                    MaxHistoryTicks = 3600,
                    PendingStepTicks = 0
                });
                _entityManager.AddComponentData(rewindEntity, new RewindLegacyState
                {
                    PlaybackSpeed = 1f,
                    CurrentTick = 0,
                    StartTick = 0,
                    PlaybackTick = 0,
                    PlaybackTicksPerSecond = 60f,
                    ScrubDirection = ScrubDirection.None,
                    ScrubSpeedMultiplier = 1f,
                    RewindWindowTicks = 3600,
                    ActiveTrack = default
                });
            }
        }

        private TimeState GetTimeState()
        {
            var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            return _entityManager.GetComponentData<TimeState>(query.GetSingletonEntity());
        }

        private void SetTimeState(TimeState state)
        {
            var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            _entityManager.SetComponentData(query.GetSingletonEntity(), state);
        }
    }
}
