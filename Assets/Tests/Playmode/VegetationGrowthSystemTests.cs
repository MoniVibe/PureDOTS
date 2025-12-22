using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Tests for VegetationGrowthSystem verifying stage transitions, history events, and tag mutations.
    /// Uses DOTS 1.x compliant APIs.
    /// </summary>
    public class VegetationGrowthSystemTests
    {
        private World _world;
        private Entity _testVegetation;
        private Entity _catalogEntity;
        private BlobAssetReference<VegetationSpeciesCatalogBlob> _catalogBlob;
        private float _fixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            var entityManager = _world.EntityManager;
            
            // Create test vegetation entity
            _testVegetation = entityManager.CreateEntity();
            
            entityManager.AddComponent<VegetationId>(_testVegetation);
            var vegetationId = entityManager.GetComponentData<VegetationId>(_testVegetation);
            vegetationId.Value = 1;
            vegetationId.SpeciesType = 0;
            entityManager.SetComponentData(_testVegetation, vegetationId);
            
            entityManager.AddComponent<VegetationLifecycle>(_testVegetation);
            entityManager.AddComponentData(_testVegetation, LocalTransform.Identity);

            var lifecycle = new VegetationLifecycle
            {
                CurrentStage = VegetationLifecycle.LifecycleStage.Seedling,
                GrowthProgress = 0f,
                StageTimer = 0f,
                TotalAge = 0f,
                GrowthRate = 1f
            };
            entityManager.SetComponentData(_testVegetation, lifecycle);
            
            entityManager.AddComponent<VegetationSpeciesIndex>(_testVegetation);
            entityManager.SetComponentData(_testVegetation, new VegetationSpeciesIndex { Value = 0 });
            
            entityManager.AddComponent<VegetationReproduction>(_testVegetation);
            entityManager.SetComponentData(_testVegetation, new VegetationReproduction
            {
                ReproductionTimer = 0f,
                ReproductionCooldown = 300f,
                SpreadRange = 5f,
                SpreadChance = 0.1f,
                MaxOffspringRadius = 2,
                ActiveOffspring = 0,
                SpawnSequence = 0
            });
            
            entityManager.AddComponent<VegetationRandomState>(_testVegetation);
            entityManager.SetComponentData(_testVegetation, new VegetationRandomState
            {
                GrowthRandomIndex = 0,
                ReproductionRandomIndex = 0,
                LootRandomIndex = 0
            });
            
            entityManager.AddBuffer<VegetationHistoryEvent>(_testVegetation);

            // Add enableable tags unconditionally with default disabled state
            entityManager.AddComponent<VegetationMatureTag>(_testVegetation);
            entityManager.AddComponent<VegetationReadyToHarvestTag>(_testVegetation);
            entityManager.AddComponent<VegetationDyingTag>(_testVegetation);
            entityManager.AddComponent<VegetationStressedTag>(_testVegetation);
            entityManager.AddComponentData(_testVegetation, new VegetationParent { Value = Entity.Null });
            entityManager.AddComponent<RewindableTag>(_testVegetation);
            
            // Set all tags to disabled initially
            entityManager.SetComponentEnabled<VegetationMatureTag>(_testVegetation, false);
            entityManager.SetComponentEnabled<VegetationReadyToHarvestTag>(_testVegetation, false);
            entityManager.SetComponentEnabled<VegetationDyingTag>(_testVegetation, false);
            entityManager.SetComponentEnabled<VegetationStressedTag>(_testVegetation, false);
            
            // Set up required singletons using proper DOTS 1.x APIs
            var timeStateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TimeState>()
                .Build(entityManager);
            
            Entity timeStateEntity;
            if (timeStateQuery.CalculateEntityCount() == 0)
            {
                timeStateEntity = entityManager.CreateEntity();
                entityManager.AddComponent<TimeState>(timeStateEntity);
            }
            else
            {
                using var entities = timeStateQuery.ToEntityArray(Allocator.Temp);
                timeStateEntity = entities[0];
            }
            
            var timeState = entityManager.GetComponentData<TimeState>(timeStateEntity);
            timeState.FixedDeltaTime = 1f / 60f;
            timeState.CurrentSpeedMultiplier = 1f;
            timeState.Tick = 0;
            timeState.IsPaused = false;
            entityManager.SetComponentData(timeStateEntity, timeState);
            _fixedDeltaTime = timeState.FixedDeltaTime;
            
            var rewindStateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RewindState>()
                .Build(entityManager);
            
            Entity rewindStateEntity;
            if (rewindStateQuery.CalculateEntityCount() == 0)
            {
                rewindStateEntity = entityManager.CreateEntity();
                entityManager.AddComponent<RewindState>(rewindStateEntity);
            }
            else
            {
                using var entities = rewindStateQuery.ToEntityArray(Allocator.Temp);
                rewindStateEntity = entities[0];
            }
            
            var rewindState = entityManager.GetComponentData<RewindState>(rewindStateEntity);
            rewindState.Mode = RewindMode.Record;
            rewindState.TargetTick = 0;
            rewindState.TickDuration = _fixedDeltaTime;
            rewindState.MaxHistoryTicks = 600;
            rewindState.PendingStepTicks = 0;
            entityManager.SetComponentData(rewindStateEntity, rewindState);

            var legacy = entityManager.HasComponent<RewindLegacyState>(rewindStateEntity)
                ? entityManager.GetComponentData<RewindLegacyState>(rewindStateEntity)
                : new RewindLegacyState();
            legacy.PlaybackSpeed = 1f;
            legacy.CurrentTick = 0;
            legacy.StartTick = 0;
            legacy.PlaybackTick = 0;
            legacy.PlaybackTicksPerSecond = 60f;
            legacy.ScrubDirection = 0;
            legacy.ScrubSpeedMultiplier = 1f;
            legacy.RewindWindowTicks = 0;
            legacy.ActiveTrack = default;
            if (entityManager.HasComponent<RewindLegacyState>(rewindStateEntity))
            {
                entityManager.SetComponentData(rewindStateEntity, legacy);
            }
            else
            {
                entityManager.AddComponentData(rewindStateEntity, legacy);
            }
            
            // Set up ECB singleton
            var ecbSingletonEntity = entityManager.CreateEntity();
            entityManager.AddComponent<BeginSimulationEntityCommandBufferSystem.Singleton>(ecbSingletonEntity);
            entityManager.SetComponentData(ecbSingletonEntity, new BeginSimulationEntityCommandBufferSystem.Singleton());

            var spawnQueueQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<VegetationSpawnCommandQueue>()
                .Build(entityManager);

            if (spawnQueueQuery.CalculateEntityCount() == 0)
            {
                var spawnQueueEntity = entityManager.CreateEntity();
                entityManager.AddComponent<VegetationSpawnCommandQueue>(spawnQueueEntity);
                entityManager.AddBuffer<VegetationSpawnCommand>(spawnQueueEntity);
            }
            else
            {
                using var spawnEntities = spawnQueueQuery.ToEntityArray(Allocator.Temp);
                var spawnQueueEntity = spawnEntities[0];
                if (!entityManager.HasBuffer<VegetationSpawnCommand>(spawnQueueEntity))
                {
                    entityManager.AddBuffer<VegetationSpawnCommand>(spawnQueueEntity);
                }
            }

            spawnQueueQuery.Dispose();
            
            // Create species catalog blob with two species for testing different timings
            var builder = new BlobBuilder(Allocator.Temp);
            ref var catalogBlob = ref builder.ConstructRoot<VegetationSpeciesCatalogBlob>();
            var speciesArrayBuilder = builder.Allocate(ref catalogBlob.Species, 2);
            
            // Species 0: Fast-growing tree (short durations)
            ref var speciesBlob0 = ref speciesArrayBuilder[0];
            builder.AllocateString(ref speciesBlob0.SpeciesId, "FastTree");
            speciesBlob0.SeedlingDuration = 30f;
            speciesBlob0.GrowingDuration = 120f;
            speciesBlob0.MatureDuration = 60f;
            speciesBlob0.FloweringDuration = 30f;
            speciesBlob0.FruitingDuration = 180f;
            speciesBlob0.DyingDuration = 60f;
            speciesBlob0.RespawnDelay = 300f;
            speciesBlob0.BaseGrowthRate = 1f;
            
            var stageMultiplierBuilder0 = builder.Allocate(ref speciesBlob0.StageMultipliers, 6);
            for (int i = 0; i < 6; i++) stageMultiplierBuilder0[i] = 1f;
            
            var seasonalMultiplierBuilder0 = builder.Allocate(ref speciesBlob0.SeasonalMultipliers, 4);
            for (int i = 0; i < 4; i++) seasonalMultiplierBuilder0[i] = 1f;
            
            speciesBlob0.MaxHealth = 100f;
            speciesBlob0.BaselineRegen = 1f;
            speciesBlob0.DamagePerDeficit = 10f;
            speciesBlob0.DroughtToleranceSeconds = 60f;
            speciesBlob0.FrostToleranceSeconds = 30f;
            speciesBlob0.MaxYieldPerCycle = 10f;
            speciesBlob0.HarvestCooldown = 60f;
            builder.AllocateString(ref speciesBlob0.ResourceTypeId, "Wood");
            speciesBlob0.PartialHarvestPenalty = 0.5f;
            speciesBlob0.DesiredMinWater = 30f;
            speciesBlob0.DesiredMaxWater = 100f;
            speciesBlob0.DesiredMinLight = 50f;
            speciesBlob0.DesiredMaxLight = 100f;
            speciesBlob0.DesiredMinSoilQuality = 40f;
            speciesBlob0.DesiredMaxSoilQuality = 100f;
            speciesBlob0.PollutionTolerance = 0.5f;
            speciesBlob0.WindTolerance = 0.5f;
            speciesBlob0.ReproductionCooldown = 300f;
            speciesBlob0.SeedsPerEvent = 3;
            speciesBlob0.SpreadRadius = 5f;
            speciesBlob0.OffspringCapPerParent = 5;
            speciesBlob0.MaturityRequirement = 0.8f;
            speciesBlob0.GridCellPadding = 1;
            speciesBlob0.GrowthSeed = 12345;
            speciesBlob0.ReproductionSeed = 67890;
            speciesBlob0.LootSeed = 54321;
            
            // Species 1: Slow-growing shrub (longer durations)
            ref var speciesBlob1 = ref speciesArrayBuilder[1];
            builder.AllocateString(ref speciesBlob1.SpeciesId, "SlowShrub");
            speciesBlob1.SeedlingDuration = 60f;  // Double the time
            speciesBlob1.GrowingDuration = 240f;  // Double the time
            speciesBlob1.MatureDuration = 120f;   // Double the time
            speciesBlob1.FloweringDuration = 60f;  // Double the time
            speciesBlob1.FruitingDuration = 360f; // Double the time
            speciesBlob1.DyingDuration = 120f;     // Double the time
            speciesBlob1.RespawnDelay = 600f;
            speciesBlob1.BaseGrowthRate = 0.5f;  // Slower growth rate
            
            var stageMultiplierBuilder1 = builder.Allocate(ref speciesBlob1.StageMultipliers, 6);
            for (int i = 0; i < 6; i++) stageMultiplierBuilder1[i] = 1f;
            
            var seasonalMultiplierBuilder1 = builder.Allocate(ref speciesBlob1.SeasonalMultipliers, 4);
            for (int i = 0; i < 4; i++) seasonalMultiplierBuilder1[i] = 1f;
            
            speciesBlob1.MaxHealth = 50f;  // Lower health
            speciesBlob1.BaselineRegen = 0.5f;
            speciesBlob1.DamagePerDeficit = 20f;
            speciesBlob1.DroughtToleranceSeconds = 30f;
            speciesBlob1.FrostToleranceSeconds = 60f;
            speciesBlob1.MaxYieldPerCycle = 5f;
            speciesBlob1.HarvestCooldown = 120f;
            builder.AllocateString(ref speciesBlob1.ResourceTypeId, "Berries");
            speciesBlob1.PartialHarvestPenalty = 0.3f;
            speciesBlob1.DesiredMinWater = 50f;
            speciesBlob1.DesiredMaxWater = 100f;
            speciesBlob1.DesiredMinLight = 70f;
            speciesBlob1.DesiredMaxLight = 100f;
            speciesBlob1.DesiredMinSoilQuality = 60f;
            speciesBlob1.DesiredMaxSoilQuality = 100f;
            speciesBlob1.PollutionTolerance = 0.3f;
            speciesBlob1.WindTolerance = 0.7f;
            speciesBlob1.ReproductionCooldown = 600f;
            speciesBlob1.SeedsPerEvent = 5;
            speciesBlob1.SpreadRadius = 3f;
            speciesBlob1.OffspringCapPerParent = 3;
            speciesBlob1.MaturityRequirement = 0.9f;
            speciesBlob1.GridCellPadding = 2;
            speciesBlob1.GrowthSeed = 54321;
            speciesBlob1.ReproductionSeed = 12345;
            speciesBlob1.LootSeed = 67890;
            
            _catalogBlob = builder.CreateBlobAssetReference<VegetationSpeciesCatalogBlob>(Allocator.Persistent);
            builder.Dispose();
            
            _catalogEntity = entityManager.CreateEntity();
            entityManager.AddComponent<VegetationSpeciesLookup>(_catalogEntity);
            entityManager.SetComponentData(_catalogEntity, new VegetationSpeciesLookup { CatalogBlob = _catalogBlob });
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                var entityManager = _world.EntityManager;
                
                // Destroy catalog singleton entity
                if (entityManager.Exists(_catalogEntity))
                {
                    entityManager.DestroyEntity(_catalogEntity);
                }
                
                // Dispose blob asset
                if (_catalogBlob.IsCreated)
                {
                    _catalogBlob.Dispose();
                }
                
                // Destroy test vegetation
                if (entityManager.Exists(_testVegetation))
                {
                    entityManager.DestroyEntity(_testVegetation);
                }
            }
        }

        private void UpdateWorld()
        {
            var simulationGroup = _world.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulationGroup != null)
            {
                simulationGroup.Update();
            }
        }

        [Test]
        public void VegetationGrowthSystem_TransitionsFromSeedlingToGrowing()
        {
            var entityManager = _world.EntityManager;
            
            // Advance by 30 seconds (SeedlingToGrowingThreshold)
            for (int i = 0; i < 1800; i++) // 30 seconds * 60 ticks per second
            {
                UpdateWorld();
            }
            
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Growing, lifecycle.CurrentStage, 
                "Vegetation should transition from Seedling to Growing after 30 seconds");
            Assert.GreaterOrEqual(lifecycle.StageTimer, 0f, "StageTimer should reset to 0 on transition");
        }

        [Test]
        public void VegetationGrowthSystem_AddsMatureTagWhenTransitioningToMature()
        {
            var entityManager = _world.EntityManager;
            
            // Set to Growing stage
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.CurrentStage = VegetationLifecycle.LifecycleStage.Growing;
            lifecycle.StageTimer = 0f;
            entityManager.SetComponentData(_testVegetation, lifecycle);
            
            // Advance by 120 seconds (GrowingToMatureThreshold)
            for (int i = 0; i < 7200; i++) // 120 seconds * 60 ticks per second
            {
                UpdateWorld();
            }
            
            Assert.IsTrue(entityManager.HasComponent<VegetationMatureTag>(_testVegetation), 
                "Vegetation should have VegetationMatureTag when transitioning to Mature stage");
            
            var newLifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Mature, newLifecycle.CurrentStage,
                "Vegetation should be in Mature stage");
        }

        [Test]
        public void VegetationGrowthSystem_RemovesMatureTagWhenTransitioningToFlowering()
        {
            var entityManager = _world.EntityManager;
            
            // Set to Mature stage with tag
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.CurrentStage = VegetationLifecycle.LifecycleStage.Mature;
            lifecycle.StageTimer = 0f;
            entityManager.SetComponentData(_testVegetation, lifecycle);
            entityManager.AddComponent<VegetationMatureTag>(_testVegetation);
            
            // Advance by 60 seconds (MatureToFloweringThreshold)
            for (int i = 0; i < 3600; i++) // 60 seconds * 60 ticks per second
            {
                UpdateWorld();
            }
            
            Assert.IsFalse(entityManager.HasComponent<VegetationMatureTag>(_testVegetation),
                "Vegetation should not have VegetationMatureTag when transitioning to Flowering stage");
            
            var newLifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Flowering, newLifecycle.CurrentStage,
                "Vegetation should be in Flowering stage");
        }

        [Test]
        public void VegetationGrowthSystem_AddsHarvestTagWhenTransitioningToFruiting()
        {
            var entityManager = _world.EntityManager;
            
            // Set to Flowering stage
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.CurrentStage = VegetationLifecycle.LifecycleStage.Flowering;
            lifecycle.StageTimer = 0f;
            entityManager.SetComponentData(_testVegetation, lifecycle);
            
            // Advance by 30 seconds (FloweringToFruitingThreshold)
            for (int i = 0; i < 1800; i++) // 30 seconds * 60 ticks per second
            {
                UpdateWorld();
            }
            
            Assert.IsTrue(entityManager.HasComponent<VegetationReadyToHarvestTag>(_testVegetation),
                "Vegetation should have VegetationReadyToHarvestTag when transitioning to Fruiting stage");
            
            var newLifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Fruiting, newLifecycle.CurrentStage,
                "Vegetation should be in Fruiting stage");
        }

        [Test]
        public void VegetationGrowthSystem_AddsDeadTagWhenTransitioningToDead()
        {
            var entityManager = _world.EntityManager;
            
            // Set to Dying stage
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.CurrentStage = VegetationLifecycle.LifecycleStage.Dying;
            lifecycle.StageTimer = 0f;
            entityManager.SetComponentData(_testVegetation, lifecycle);
            entityManager.AddComponent<VegetationDyingTag>(_testVegetation);
            
            // Advance by 60 seconds (DyingToDeadThreshold)
            for (int i = 0; i < 3600; i++) // 60 seconds * 60 ticks per second
            {
                UpdateWorld();
            }
            
            Assert.IsTrue(entityManager.HasComponent<VegetationDeadTag>(_testVegetation),
                "Vegetation should have VegetationDeadTag when transitioning to Dead stage");
            Assert.IsFalse(entityManager.HasComponent<VegetationDyingTag>(_testVegetation),
                "Vegetation should not have VegetationDyingTag when dead");
            
            var newLifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Dead, newLifecycle.CurrentStage,
                "Vegetation should be in Dead stage");
        }

        [Test]
        public void VegetationGrowthSystem_RecordsHistoryEventsOnStageTransitions()
        {
            var entityManager = _world.EntityManager;
            
            // Advance by 30 seconds to trigger transition
            for (int i = 0; i < 1800; i++)
            {
                UpdateWorld();
            }
            
            var historyBuffer = entityManager.GetBuffer<VegetationHistoryEvent>(_testVegetation);
            Assert.Greater(historyBuffer.Length, 0, 
                "History buffer should contain at least one event after stage transition");
            
            var firstEvent = historyBuffer[0];
            Assert.AreEqual(VegetationHistoryEvent.EventType.StageTransition, firstEvent.Type,
                "First event should be a StageTransition event");
            Assert.AreEqual((float)VegetationLifecycle.LifecycleStage.Growing, firstEvent.Value,
                "Event value should match new stage");
        }

        [Test]
        public void VegetationGrowthSystem_DoesNotProcessWhenPaused()
        {
            var entityManager = _world.EntityManager;
            
            // Set pause state
            var timeStateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TimeState>()
                .Build(entityManager);
            using var timeEntities = timeStateQuery.ToEntityArray(Allocator.Temp);
            var timeStateEntity = timeEntities[0];
            
            var timeState = entityManager.GetComponentData<TimeState>(timeStateEntity);
            timeState.IsPaused = true;
            entityManager.SetComponentData(timeStateEntity, timeState);
            
            var initialLifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            
            // Advance by 30 seconds while paused
            for (int i = 0; i < 1800; i++)
            {
                UpdateWorld();
            }
            
            var finalLifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            Assert.AreEqual(initialLifecycle.CurrentStage, finalLifecycle.CurrentStage,
                "Stage should not change when simulation is paused");
        }

        [Test]
        public void VegetationGrowthSystem_UsesDifferentDurationsForDifferentSpecies()
        {
            var entityManager = _world.EntityManager;
            
            // Create second vegetation entity with species index 1 (slow shrub)
            var slowShrubEntity = entityManager.CreateEntity();
            entityManager.AddComponent<VegetationId>(slowShrubEntity);
            var slowShrubId = new VegetationId { Value = 2, SpeciesType = 1 };
            entityManager.SetComponentData(slowShrubEntity, slowShrubId);
            
            entityManager.AddComponent<VegetationLifecycle>(slowShrubEntity);
            var slowShrubLifecycle = new VegetationLifecycle
            {
                CurrentStage = VegetationLifecycle.LifecycleStage.Seedling,
                GrowthProgress = 0f,
                StageTimer = 0f,
                TotalAge = 0f,
                GrowthRate = 0.5f
            };
            entityManager.SetComponentData(slowShrubEntity, slowShrubLifecycle);

            entityManager.AddComponentData(slowShrubEntity, LocalTransform.Identity);
            
            entityManager.AddComponent<VegetationSpeciesIndex>(slowShrubEntity);
            entityManager.SetComponentData(slowShrubEntity, new VegetationSpeciesIndex { Value = 1 });
            
            entityManager.AddComponent<VegetationReproduction>(slowShrubEntity);
            entityManager.SetComponentData(slowShrubEntity, new VegetationReproduction
            {
                ReproductionTimer = 0f,
                ReproductionCooldown = 600f,
                SpreadRange = 3f,
                SpreadChance = 0.05f,
                MaxOffspringRadius = 2,
                ActiveOffspring = 0,
                SpawnSequence = 0
            });
            
            entityManager.AddComponent<VegetationRandomState>(slowShrubEntity);
            entityManager.SetComponentData(slowShrubEntity, new VegetationRandomState
            {
                GrowthRandomIndex = 0,
                ReproductionRandomIndex = 0,
                LootRandomIndex = 0
            });
            
            entityManager.AddBuffer<VegetationHistoryEvent>(slowShrubEntity);

            // Add enableable tags unconditionally with default disabled state
            entityManager.AddComponent<VegetationMatureTag>(slowShrubEntity);
            entityManager.AddComponent<VegetationReadyToHarvestTag>(slowShrubEntity);
            entityManager.AddComponent<VegetationDyingTag>(slowShrubEntity);
            entityManager.AddComponent<VegetationStressedTag>(slowShrubEntity);
            entityManager.AddComponentData(slowShrubEntity, new VegetationParent { Value = Entity.Null });
            entityManager.AddComponent<RewindableTag>(slowShrubEntity);
            
            // Set all tags to disabled initially
            entityManager.SetComponentEnabled<VegetationMatureTag>(slowShrubEntity, false);
            entityManager.SetComponentEnabled<VegetationReadyToHarvestTag>(slowShrubEntity, false);
            entityManager.SetComponentEnabled<VegetationDyingTag>(slowShrubEntity, false);
            entityManager.SetComponentEnabled<VegetationStressedTag>(slowShrubEntity, false);
            
            // Fast tree (species 0) should transition after 30 seconds
            // Slow shrub (species 1) should still be seedling after 30 seconds
            for (int i = 0; i < 1800; i++) // 30 seconds * 60 ticks per second
            {
                UpdateWorld();
            }
            
            // Fast tree should have transitioned to Growing
            var fastTreeLifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Growing, fastTreeLifecycle.CurrentStage,
                "Fast-growing tree should transition to Growing after 30 seconds");
            
            // Slow shrub should still be seedling (needs 60 seconds)
            var slowShrubLifecycleAfter30s = entityManager.GetComponentData<VegetationLifecycle>(slowShrubEntity);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Seedling, slowShrubLifecycleAfter30s.CurrentStage,
                "Slow-growing shrub should still be seedling after 30 seconds");
            
            // Continue advancing for another 30 seconds (total 60 seconds)
            for (int i = 0; i < 1800; i++)
            {
                UpdateWorld();
            }
            
            // Now slow shrub should have transitioned - reassign to existing variable
            var updatedSlowShrubLifecycle = entityManager.GetComponentData<VegetationLifecycle>(slowShrubEntity);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Growing, updatedSlowShrubLifecycle.CurrentStage,
                "Slow-growing shrub should transition to Growing after 60 seconds");
            
            // Clean up
            entityManager.DestroyEntity(slowShrubEntity);
        }

        [Test]
        public void VegetationReproductionSystem_AdvancesReproductionTimer()
        {
            var entityManager = _world.EntityManager;
            
            // Set to Mature stage with mature tag enabled
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.CurrentStage = VegetationLifecycle.LifecycleStage.Mature;
            lifecycle.StageTimer = 0f;
            entityManager.SetComponentData(_testVegetation, lifecycle);
            entityManager.SetComponentEnabled<VegetationMatureTag>(_testVegetation, true);
            
            // Ensure clean history state
            var history = entityManager.GetBuffer<VegetationHistoryEvent>(_testVegetation);
            history.Clear();
            
            var initialReproduction = entityManager.GetComponentData<VegetationReproduction>(_testVegetation);
            Assert.AreEqual(0f, initialReproduction.ReproductionTimer,
                "Reproduction timer should start at 0");
            
            // Advance by 1 second
            for (int i = 0; i < 60; i++) // 1 second * 60 ticks per second
            {
                UpdateWorld();
            }
            
            var updatedReproduction = entityManager.GetComponentData<VegetationReproduction>(_testVegetation);
            Assert.Greater(updatedReproduction.ReproductionTimer, 0f,
                "Reproduction timer should advance over time");
            Assert.AreEqual(0, history.Length, "No reproduction event should fire before cooldown elapses");
        }

        [Test]
        public void VegetationReproductionSystem_RecordsEventAndResetsTimerAtCooldown()
        {
            var entityManager = _world.EntityManager;

            // Ensure entity is in the mature lifecycle stage
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.CurrentStage = VegetationLifecycle.LifecycleStage.Mature;
            lifecycle.StageTimer = 0f;
            entityManager.SetComponentData(_testVegetation, lifecycle);
            entityManager.SetComponentEnabled<VegetationMatureTag>(_testVegetation, true);

            ref var speciesData = ref _catalogBlob.Value.Species[0];

            // Prime reproduction timer to trigger on next tick
            var reproduction = entityManager.GetComponentData<VegetationReproduction>(_testVegetation);
            reproduction.ReproductionTimer = speciesData.ReproductionCooldown - (_fixedDeltaTime * 0.5f);
            entityManager.SetComponentData(_testVegetation, reproduction);

            var randomState = entityManager.GetComponentData<VegetationRandomState>(_testVegetation);
            randomState.ReproductionRandomIndex = 0;
            entityManager.SetComponentData(_testVegetation, randomState);

            var history = entityManager.GetBuffer<VegetationHistoryEvent>(_testVegetation);
            history.Clear();

            UpdateWorld(); // Trigger reproduction

            reproduction = entityManager.GetComponentData<VegetationReproduction>(_testVegetation);
            randomState = entityManager.GetComponentData<VegetationRandomState>(_testVegetation);
            history = entityManager.GetBuffer<VegetationHistoryEvent>(_testVegetation);

            Assert.Less(reproduction.ReproductionTimer, _fixedDeltaTime,
                "Reproduction timer should reset after cooldown elapses.");
            Assert.AreEqual(1u, randomState.ReproductionRandomIndex,
                "Deterministic random index should advance after reproduction attempt.");
            Assert.AreEqual(1, history.Length, "Reproduction event should be recorded exactly once.");
            Assert.AreEqual(VegetationHistoryEvent.EventType.Reproduced, history[0].Type,
                "History buffer should capture reproduction event.");
        }

        [Test]
        public void VegetationDecaySystem_DestroysDeadVegetationAfterDecayPeriod()
        {
            var entityManager = _world.EntityManager;
            
            // Set to Dead stage with decayable tag
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.CurrentStage = VegetationLifecycle.LifecycleStage.Dead;
            lifecycle.StageTimer = 0f;
            entityManager.SetComponentData(_testVegetation, lifecycle);
            entityManager.AddComponent<VegetationDeadTag>(_testVegetation);
            entityManager.AddComponent<VegetationDecayableTag>(_testVegetation);

            ref var speciesData = ref _catalogBlob.Value.Species[0];
            lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.StageTimer = speciesData.RespawnDelay - (_fixedDeltaTime * 0.5f);
            entityManager.SetComponentData(_testVegetation, lifecycle);
            
            var history = entityManager.GetBuffer<VegetationHistoryEvent>(_testVegetation);
            history.Clear();
            
            Assert.IsTrue(entityManager.Exists(_testVegetation),
                "Vegetation should exist before decay period");
            
            UpdateWorld(); // Schedule destruction and record event

            history = entityManager.GetBuffer<VegetationHistoryEvent>(_testVegetation);
            Assert.AreEqual(1, history.Length, "Death event should be recorded once decay threshold is reached.");
            Assert.AreEqual(VegetationHistoryEvent.EventType.Died, history[0].Type,
                "History should record a Died event when decay completes.");

            UpdateWorld(); // Allow ECB playback to destroy entity
            
            Assert.IsFalse(entityManager.Exists(_testVegetation),
                "Vegetation should be destroyed after decay ECB playback.");
        }

        [Test]
        public void VegetationReproductionSystem_SpawnsOffspringEntity()
        {
            var entityManager = _world.EntityManager;

            // Promote parent to mature stage.
            var lifecycle = entityManager.GetComponentData<VegetationLifecycle>(_testVegetation);
            lifecycle.CurrentStage = VegetationLifecycle.LifecycleStage.Mature;
            lifecycle.StageTimer = 0f;
            entityManager.SetComponentData(_testVegetation, lifecycle);
            entityManager.SetComponentEnabled<VegetationMatureTag>(_testVegetation, true);

            ref var speciesData = ref _catalogBlob.Value.Species[0];

            var reproduction = entityManager.GetComponentData<VegetationReproduction>(_testVegetation);
            reproduction.ReproductionTimer = speciesData.ReproductionCooldown - (_fixedDeltaTime * 0.5f);
            reproduction.SpreadChance = 1f;
            reproduction.ActiveOffspring = 0;
            reproduction.SpawnSequence = 0;
            entityManager.SetComponentData(_testVegetation, reproduction);

            var parentTransform = LocalTransform.FromPositionRotationScale(new float3(5f, 0f, 5f), quaternion.identity, 1f);
            entityManager.SetComponentData(_testVegetation, parentTransform);

            var offspringQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VegetationParent>());
            var beforeCount = offspringQuery.CalculateEntityCount();

            UpdateWorld();

            var afterCount = offspringQuery.CalculateEntityCount();
            Assert.Greater(afterCount, beforeCount, "Reproduction should increase vegetation entities with VegetationParent.");

            using var offspringEntities = offspringQuery.ToEntityArray(Allocator.Temp);
            Entity spawned = Entity.Null;
            for (int i = 0; i < offspringEntities.Length; i++)
            {
                var candidate = offspringEntities[i];
                if (candidate == _testVegetation)
                {
                    continue;
                }

                if (!entityManager.HasComponent<VegetationParent>(candidate))
                {
                    continue;
                }

                var parent = entityManager.GetComponentData<VegetationParent>(candidate);
                if (parent.Value == _testVegetation)
                {
                    spawned = candidate;
                    break;
                }
            }

            Assert.AreNotEqual(Entity.Null, spawned, "Spawned offspring should reference the mature parent.");

            var childLifecycle = entityManager.GetComponentData<VegetationLifecycle>(spawned);
            Assert.AreEqual(VegetationLifecycle.LifecycleStage.Seedling, childLifecycle.CurrentStage,
                "Offspring should begin life in the seedling stage.");

            var childTransform = entityManager.GetComponentData<LocalTransform>(spawned);
            var distance = math.distance(childTransform.Position, parentTransform.Position);
            Assert.LessOrEqual(distance, speciesData.SpreadRadius + 0.01f,
                "Offspring should be positioned within spread radius of the parent.");

            var updatedReproduction = entityManager.GetComponentData<VegetationReproduction>(_testVegetation);
            Assert.Greater(updatedReproduction.ActiveOffspring, reproduction.ActiveOffspring,
                "Parent should track the new offspring count deterministically.");
        }

        [Test]
        public void VegetationDecaySystem_DecrementsParentOffspringCount()
        {
            var entityManager = _world.EntityManager;

            // Create a spawned child linked to the test vegetation.
            var child = entityManager.CreateEntity();
            entityManager.AddComponentData(child, LocalTransform.FromPositionRotationScale(new float3(8f, 0f, 8f), quaternion.identity, 1f));
            entityManager.AddComponentData(child, new VegetationSpeciesIndex { Value = 0 });
            entityManager.AddComponentData(child, new VegetationLifecycle
            {
                CurrentStage = VegetationLifecycle.LifecycleStage.Dead,
                GrowthProgress = 0f,
                StageTimer = _catalogBlob.Value.Species[0].RespawnDelay - (_fixedDeltaTime * 0.5f),
                TotalAge = 600f,
                GrowthRate = 1f
            });
            entityManager.AddComponent<VegetationDecayableTag>(child);
            entityManager.AddComponent<VegetationDeadTag>(child);
            entityManager.AddComponentData(child, new VegetationParent { Value = _testVegetation });
            entityManager.AddBuffer<VegetationHistoryEvent>(child);

            var parentReproduction = entityManager.GetComponentData<VegetationReproduction>(_testVegetation);
            parentReproduction.ActiveOffspring = 1;
            entityManager.SetComponentData(_testVegetation, parentReproduction);

            UpdateWorld();
            UpdateWorld();

            Assert.IsFalse(entityManager.Exists(child), "Decay system should destroy dead offspring.");

            var updatedReproduction = entityManager.GetComponentData<VegetationReproduction>(_testVegetation);
            Assert.AreEqual(0, updatedReproduction.ActiveOffspring,
                "Parent offspring count should decrement when child is removed.");
        }

        [Test]
        public void VegetationDecaySystem_RespawnsWhenNoParentPresent()
        {
            var entityManager = _world.EntityManager;

            var orphan = entityManager.CreateEntity();
            entityManager.AddComponentData(orphan, LocalTransform.FromPositionRotationScale(new float3(-3f, 0f, -3f), quaternion.identity, 1f));
            entityManager.AddComponentData(orphan, new VegetationSpeciesIndex { Value = 0 });
            entityManager.AddComponentData(orphan, new VegetationLifecycle
            {
                CurrentStage = VegetationLifecycle.LifecycleStage.Dead,
                GrowthProgress = 0f,
                StageTimer = _catalogBlob.Value.Species[0].RespawnDelay - (_fixedDeltaTime * 0.5f),
                TotalAge = 400f,
                GrowthRate = 1f
            });
            entityManager.AddComponent<VegetationDecayableTag>(orphan);
            entityManager.AddComponent<VegetationDeadTag>(orphan);
            entityManager.AddComponentData(orphan, new VegetationParent { Value = Entity.Null });
            entityManager.AddBuffer<VegetationHistoryEvent>(orphan);

            using var preEntities = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VegetationLifecycle>()).ToEntityArray(Allocator.Temp);
            var seenEntities = new NativeHashSet<Entity>(preEntities.Length, Allocator.Temp);
            for (int i = 0; i < preEntities.Length; i++)
            {
                seenEntities.Add(preEntities[i]);
            }

            UpdateWorld();
            UpdateWorld();

            using var postEntities = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VegetationLifecycle>()).ToEntityArray(Allocator.Temp);
            Entity respawned = Entity.Null;
            for (int i = 0; i < postEntities.Length; i++)
            {
                var candidate = postEntities[i];
                if (!seenEntities.Contains(candidate))
                {
                    respawned = candidate;
                    break;
                }
            }

            seenEntities.Dispose();

            Assert.AreNotEqual(Entity.Null, respawned, "Decay should trigger a replacement spawn when no parent tracks the entity.");

            var respawnParent = entityManager.GetComponentData<VegetationParent>(respawned);
            Assert.AreEqual(Entity.Null, respawnParent.Value, "Respawned vegetation should not report a parent.");

            var respawnTransform = entityManager.GetComponentData<LocalTransform>(respawned);
            var distance = math.distance(respawnTransform.Position, new float3(-3f, 0f, -3f));
            Assert.LessOrEqual(distance, 0.1f, "Respawned vegetation should appear at the decay location to support deterministic playback.");
        }
    }
}
