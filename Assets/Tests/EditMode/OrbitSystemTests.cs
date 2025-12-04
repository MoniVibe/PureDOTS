using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems;
using PureDOTS.Systems.Time;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.EditMode
{
    /// <summary>
    /// Unit tests for orbit and time-of-day systems.
    /// Tests orbital phase advancement, time-of-day calculation, and sunlight computation.
    /// </summary>
    public class OrbitSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private Entity _planetEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("OrbitSystemTests");
            _entityManager = _world.EntityManager;

            // Create required singletons
            EnsureTimeState();
            EnsureRewindState();

            // Create a test planet entity
            _planetEntity = _entityManager.CreateEntity();

            // Add orbit components
            _entityManager.AddComponentData(_planetEntity, new OrbitParameters
            {
                OrbitalPeriodSeconds = 86400f, // 24 hours
                InitialPhase = 0f,
                OrbitNormal = new float3(0f, 1f, 0f),
                TimeOfDayOffset = 0f
            });

            _entityManager.AddComponentData(_planetEntity, new OrbitState
            {
                OrbitalPhase = 0f,
                LastUpdateTick = 0
            });

            _entityManager.AddComponentData(_planetEntity, new TimeOfDayState
            {
                TimeOfDayNorm = 0f,
                Phase = TimeOfDayPhase.Night,
                PreviousPhase = TimeOfDayPhase.Night
            });

            _entityManager.AddComponentData(_planetEntity, new SunlightFactor
            {
                Sunlight = 0f
            });

            _entityManager.AddComponentData(_planetEntity, TimeOfDayConfig.Default);
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
        public void OrbitAdvanceSystem_AdvancesPhaseCorrectly()
        {
            var orbitSystem = _world.GetOrCreateSystemManaged<OrbitAdvanceSystem>();

            // Advance time by 1 hour (3600 seconds)
            var timeQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            var timeEntity = timeQuery.GetSingletonEntity();
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.DeltaTime = 3600f; // 1 hour
            timeState.Tick = 1;
            _entityManager.SetComponentData(timeEntity, timeState);

            // Update orbit system
            orbitSystem.Update(_world.Unmanaged);

            // Check that phase advanced correctly
            // 1 hour / 24 hours = 0.04166...
            var orbitState = _entityManager.GetComponentData<OrbitState>(_planetEntity);
            Assert.That(orbitState.OrbitalPhase, Is.GreaterThan(0f));
            Assert.That(orbitState.OrbitalPhase, Is.LessThan(0.05f));
            Assert.That(orbitState.LastUpdateTick, Is.EqualTo(1u));
        }

        [Test]
        public void OrbitAdvanceSystem_WrapsPhaseCorrectly()
        {
            var orbitSystem = _world.GetOrCreateSystemManaged<OrbitAdvanceSystem>();

            // Set phase near 1.0
            var orbitState = _entityManager.GetComponentData<OrbitState>(_planetEntity);
            orbitState.OrbitalPhase = 0.99f;
            _entityManager.SetComponentData(_planetEntity, orbitState);

            // Advance time by 1 hour
            var timeQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            var timeEntity = timeQuery.GetSingletonEntity();
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.DeltaTime = 3600f;
            timeState.Tick = 1;
            _entityManager.SetComponentData(timeEntity, timeState);

            // Update orbit system
            orbitSystem.Update(_world.Unmanaged);

            // Check that phase wrapped correctly
            orbitState = _entityManager.GetComponentData<OrbitState>(_planetEntity);
            Assert.That(orbitState.OrbitalPhase, Is.GreaterThanOrEqualTo(0f));
            Assert.That(orbitState.OrbitalPhase, Is.LessThan(1f));
        }

        [Test]
        public void TimeOfDaySystem_CalculatesPhaseCorrectly()
        {
            var orbitSystem = _world.GetOrCreateSystemManaged<OrbitAdvanceSystem>();
            var timeOfDaySystem = _world.GetOrCreateSystemManaged<TimeOfDaySystem>();

            // Set orbital phase to 0.5 (noon)
            var orbitState = _entityManager.GetComponentData<OrbitState>(_planetEntity);
            orbitState.OrbitalPhase = 0.5f;
            _entityManager.SetComponentData(_planetEntity, orbitState);

            // Update time-of-day system
            var timeQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            var timeEntity = timeQuery.GetSingletonEntity();
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.DeltaTime = 0f; // No time advance needed
            timeState.Tick = 1;
            _entityManager.SetComponentData(timeEntity, timeState);

            timeOfDaySystem.Update(_world.Unmanaged);

            // Check that phase is Day (0.5 is between DayThreshold 0.25 and DuskThreshold 0.75)
            var timeOfDayState = _entityManager.GetComponentData<TimeOfDayState>(_planetEntity);
            Assert.That(timeOfDayState.TimeOfDayNorm, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(timeOfDayState.Phase, Is.EqualTo(TimeOfDayPhase.Day));
        }

        [Test]
        public void TimeOfDaySystem_CalculatesSunlightCorrectly()
        {
            var timeOfDaySystem = _world.GetOrCreateSystemManaged<TimeOfDaySystem>();

            // Set orbital phase to 0.5 (noon) - should give maximum sunlight
            var orbitState = _entityManager.GetComponentData<OrbitState>(_planetEntity);
            orbitState.OrbitalPhase = 0.5f;
            _entityManager.SetComponentData(_planetEntity, orbitState);

            // Update time-of-day system
            var timeState = _entityManager.GetComponentData<TimeState>(_entityManager.GetSingletonEntity<TimeState>());
            timeState.DeltaTime = 0f;
            timeState.Tick = 1;
            _entityManager.SetComponentData(_entityManager.GetSingletonEntity<TimeState>(), timeState);

            timeOfDaySystem.Update(_world.Unmanaged);

            // Check that sunlight is near maximum (cosine curve peaks at 0.5)
            var sunlightFactor = _entityManager.GetComponentData<SunlightFactor>(_planetEntity);
            Assert.That(sunlightFactor.Sunlight, Is.GreaterThan(0.9f)); // Should be close to 1.0 at noon
        }

        [Test]
        public void TimeOfDaySystem_CalculatesSunlightAtMidnight()
        {
            var timeOfDaySystem = _world.GetOrCreateSystemManaged<TimeOfDaySystem>();

            // Set orbital phase to 0.0 (midnight) - should give minimum sunlight
            var orbitState = _entityManager.GetComponentData<OrbitState>(_planetEntity);
            orbitState.OrbitalPhase = 0.0f;
            _entityManager.SetComponentData(_planetEntity, orbitState);

            // Update time-of-day system
            var timeState = _entityManager.GetComponentData<TimeState>(_entityManager.GetSingletonEntity<TimeState>());
            timeState.DeltaTime = 0f;
            timeState.Tick = 1;
            _entityManager.SetComponentData(_entityManager.GetSingletonEntity<TimeState>(), timeState);

            timeOfDaySystem.Update(_world.Unmanaged);

            // Check that sunlight is near minimum
            var sunlightFactor = _entityManager.GetComponentData<SunlightFactor>(_planetEntity);
            Assert.That(sunlightFactor.Sunlight, Is.LessThan(0.1f)); // Should be close to 0.0 at midnight
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
                    CurrentTick = 0,
                    TargetTick = 0,
                    PlaybackSpeed = 1f,
                    StartTick = 0,
                    PlaybackTick = 0,
                    PlaybackTicksPerSecond = 60f,
                    ScrubDirection = ScrubDirection.None,
                    ScrubSpeedMultiplier = 1f,
                    RewindWindowTicks = 3600u
                });
            }
        }
    }
}

