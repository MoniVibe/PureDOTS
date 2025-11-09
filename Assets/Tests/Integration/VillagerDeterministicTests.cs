using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using PureDOTS.Runtime.Villager;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Validates deterministic behavior of villager systems under rewind/replay scenarios.
    /// Ensures villager needs, jobs, and AI state remain consistent across rewinds.
    /// </summary>
    public class VillagerDeterministicTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("VillagerDeterministicTests");
            _entityManager = _world.EntityManager;

            // Ensure required singletons
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            // Create time state
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState
            {
                Tick = 0,
                FixedDeltaTime = 1f / 60f,
                IsPaused = false
            });

            // Create rewind state
            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                PlaybackTick = 0
            });

            // Create villager behavior config
            var configEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(configEntity, VillagerBehaviorConfig.CreateDefaults());
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }

        private Entity CreateVillager(float3 position, float initialHunger = 50f, float initialEnergy = 100f)
        {
            var entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new VillagerId { Value = entity.Index, FactionId = 0 });
            _entityManager.AddComponentData(entity, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            _entityManager.AddComponentData(entity, new VillagerNeeds
            {
                Health = 100f,
                MaxHealth = 100f
            });
            var needs = _entityManager.GetComponentData<VillagerNeeds>(entity);
            needs.SetHunger(initialHunger);
            needs.SetEnergy(initialEnergy);
            needs.SetMorale(75f);
            _entityManager.SetComponentData(entity, needs);

            _entityManager.AddComponentData(entity, new VillagerAIState
            {
                CurrentState = VillagerAIState.State.Idle,
                CurrentGoal = VillagerAIState.Goal.None,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                StateTimer = 0f,
                StateStartTick = 0
            });
            _entityManager.AddComponentData(entity, new VillagerJob
            {
                Type = VillagerJob.JobType.None,
                Phase = VillagerJob.JobPhase.Idle,
                ActiveTicketId = 0,
                Productivity = 1f,
                LastStateChangeTick = 0
            });
            _entityManager.AddComponentData(entity, new VillagerAvailability
            {
                IsAvailable = 1,
                IsReserved = 0,
                LastChangeTick = 0,
                BusyTime = 0f
            });
            _entityManager.AddComponentData(entity, new VillagerDisciplineState
            {
                Value = VillagerDisciplineType.Unassigned,
                Level = 0,
                Experience = 0f
            });
            _entityManager.AddComponentData(entity, new VillagerMood
            {
                Mood = 75f,
                TargetMood = 75f,
                MoodChangeRate = 1f,
                Wellbeing = 75f
            });
            _entityManager.AddComponentData(entity, new VillagerMovement
            {
                Velocity = float3.zero,
                BaseSpeed = 5f,
                CurrentSpeed = 5f,
                DesiredRotation = quaternion.identity,
                IsMoving = 0,
                IsStuck = 0,
                LastMoveTick = 0
            });
            _entityManager.AddComponentData(entity, new VillagerFlags());

            return entity;
        }

        [Test]
        public void VillagerNeedsSystem_DeterministicDecay()
        {
            // Arrange: Create villager with initial needs
            var villager = CreateVillager(float3.zero, initialHunger: 50f, initialEnergy: 100f);
            var needsBefore = _entityManager.GetComponentData<VillagerNeeds>(villager);

            // Act: Run needs system for 60 ticks (1 second at 60Hz)
            // Systems are automatically updated via World.Update()
            for (int i = 0; i < 60; i++)
            {
                var timeState = _entityManager.GetComponentData<TimeState>(
                    _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity());
                timeState.Tick++;
                _entityManager.SetComponentData(
                    _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity(),
                    timeState);
                _world.Update();
            }

            // Assert: Needs should have changed deterministically
            var needsAfter = _entityManager.GetComponentData<VillagerNeeds>(villager);
            Assert.Greater(needsAfter.HungerFloat, needsBefore.HungerFloat, "Hunger should increase over time");
            Assert.Less(needsAfter.EnergyFloat, needsBefore.EnergyFloat, "Energy should decrease (no work state, but natural decay may occur)");
        }

        [Test]
        public void VillagerNeedsSystem_RewindSafe()
        {
            // Arrange: Create villager and run for 100 ticks
            var villager = CreateVillager(float3.zero, initialHunger: 50f, initialEnergy: 100f);

            // Record initial state
            var initialState = _entityManager.GetComponentData<VillagerNeeds>(villager);

            // Run for 100 ticks
            for (int i = 0; i < 100; i++)
            {
                var timeState = _entityManager.GetComponentData<TimeState>(
                    _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity());
                timeState.Tick++;
                _entityManager.SetComponentData(
                    _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity(),
                    timeState);
                _world.Update();
            }

            var stateAfter100 = _entityManager.GetComponentData<VillagerNeeds>(villager);

            // Rewind to tick 50
            var rewindEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            _entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Playback,
                PlaybackTick = 50
            });

            // Simulate playback (systems should skip updates)
            _world.Update();

            // Assert: Needs should not change during playback
            var stateDuringPlayback = _entityManager.GetComponentData<VillagerNeeds>(villager);
            Assert.AreEqual(stateAfter100.HungerFloat, stateDuringPlayback.HungerFloat, 0.001f,
                "Needs should not change during playback mode");
        }

        [Test]
        public void VillagerSystems_DeterministicStateAcrossRuns()
        {
            // Arrange: Create identical villagers in two separate runs
            Entity villager1, villager2;
            VillagerNeeds state1, state2;

            // Run 1: Create villager, run for 100 ticks
            {
                villager1 = CreateVillager(float3.zero, initialHunger: 50f, initialEnergy: 100f);
                for (int i = 0; i < 100; i++)
                {
                    var timeState = _entityManager.GetComponentData<TimeState>(
                        _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity());
                    timeState.Tick++;
                    _entityManager.SetComponentData(
                        _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity(),
                        timeState);
                    _world.Update();
                }
                state1 = _entityManager.GetComponentData<VillagerNeeds>(villager1);
            }

            // Reset world for run 2
            _world.Dispose();
            SetUp();

            // Run 2: Create identical villager, run for 100 ticks
            {
                villager2 = CreateVillager(float3.zero, initialHunger: 50f, initialEnergy: 100f);
                for (int i = 0; i < 100; i++)
                {
                    var timeState = _entityManager.GetComponentData<TimeState>(
                        _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity());
                    timeState.Tick++;
                    _entityManager.SetComponentData(
                        _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity(),
                        timeState);
                    _world.Update();
                }
                state2 = _entityManager.GetComponentData<VillagerNeeds>(villager2);
            }

            // Assert: States should match exactly (deterministic)
            Assert.AreEqual(state1.HungerFloat, state2.HungerFloat, 0.001f,
                "Hunger should be deterministic across identical runs");
            Assert.AreEqual(state1.EnergyFloat, state2.EnergyFloat, 0.001f,
                "Energy should be deterministic across identical runs");
            Assert.AreEqual(state1.MoraleFloat, state2.MoraleFloat, 0.001f,
                "Morale should be deterministic across identical runs");
        }

        [Test]
        public void VillagerFlags_BackwardCompatible()
        {
            // Arrange: Create villager with flags
            var villager = CreateVillager(float3.zero);
            var flags = _entityManager.GetComponentData<VillagerFlags>(villager);

            // Act: Set flags
            flags.IsSelected = true;
            flags.IsWorking = true;
            _entityManager.SetComponentData(villager, flags);

            // Assert: Flags should be readable
            var flagsAfter = _entityManager.GetComponentData<VillagerFlags>(villager);
            Assert.IsTrue(flagsAfter.IsSelected, "IsSelected flag should be set");
            Assert.IsTrue(flagsAfter.IsWorking, "IsWorking flag should be set");
            Assert.IsFalse(flagsAfter.IsDead, "IsDead flag should not be set");
        }
    }
}

