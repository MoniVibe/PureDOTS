using NUnit.Framework;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public class VegetationHarvestSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private Entity _queueEntity;
        private BlobAssetReference<VegetationSpeciesCatalogBlob> _catalogBlob;

        [SetUp]
        public void SetUp()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _entityManager = _world.EntityManager;

            EnsureTimeAndRewindSingletons();
            EnsureCommandQueue();
            EnsureSpeciesLookup();
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalogBlob.IsCreated)
            {
                _catalogBlob.Dispose();
                _catalogBlob = default;
            }

            var lookupQuery = _entityManager.CreateEntityQuery(typeof(VegetationSpeciesLookup));
            if (!lookupQuery.IsEmptyIgnoreFilter)
            {
                var entity = lookupQuery.GetSingletonEntity();
                var lookup = _entityManager.GetComponentData<VegetationSpeciesLookup>(entity);
                lookup.CatalogBlob = default;
                _entityManager.SetComponentData(entity, lookup);
            }
            lookupQuery.Dispose();
        }

        [Test]
        public void HarvestSystem_AddsYieldToVillagerInventory()
        {
            var timeEntity = GetSingletonEntity<TimeState>();
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 1200;
            _entityManager.SetComponentData(timeEntity, timeState);

            var vegetation = CreateFruitingVegetation(currentProduction: 15f);
            var villager = CreateVillager();

            var commands = _entityManager.GetBuffer<VegetationHarvestCommand>(_queueEntity);
            commands.Add(new VegetationHarvestCommand
            {
                Villager = villager,
                Vegetation = vegetation,
                SpeciesIndex = 0,
                RequestedAmount = 6f,
                IssuedTick = timeState.Tick,
                CommandId = 0x1u
            });

            UpdateSimulation();

            var inventory = _entityManager.GetBuffer<VillagerInventoryItem>(villager);
            Assert.AreEqual(1, inventory.Length);
            Assert.AreEqual(6f, inventory[0].Amount, 1e-3f);

            var production = _entityManager.GetComponentData<VegetationProduction>(vegetation);
            Assert.Less(production.CurrentProduction, 15f);
            Assert.AreEqual(timeState.Tick * timeState.FixedDeltaTime, production.LastHarvestTime, 1e-4f);
            Assert.IsTrue(_entityManager.IsComponentEnabled<VegetationReadyToHarvestTag>(vegetation));

            var history = _entityManager.GetBuffer<VegetationHistoryEvent>(vegetation);
            Assert.AreEqual(1, history.Length);
            Assert.AreEqual(VegetationHistoryEvent.EventType.Harvested, history[0].Type);
            Assert.AreEqual(6f, history[0].Value, 1e-3f);

            var receipts = _entityManager.GetBuffer<VegetationHarvestReceipt>(_queueEntity);
            Assert.AreEqual(1, receipts.Length);
            Assert.AreEqual(VegetationHarvestResult.Success, receipts[0].Result);
            Assert.AreEqual(6f, receipts[0].HarvestedAmount, 1e-3f);
        }

        [Test]
        public void HarvestSystem_RespectsCooldownAndNotReadyStates()
        {
            var timeEntity = GetSingletonEntity<TimeState>();
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 400;
            _entityManager.SetComponentData(timeEntity, timeState);

            var vegetation = CreateFruitingVegetation(currentProduction: 4f);
            var villager = CreateVillager();

            var commands = _entityManager.GetBuffer<VegetationHarvestCommand>(_queueEntity);
            commands.Add(new VegetationHarvestCommand
            {
                Villager = villager,
                Vegetation = vegetation,
                SpeciesIndex = 0,
                RequestedAmount = 4f,
                IssuedTick = timeState.Tick,
                CommandId = 0x2u
            });

            UpdateSimulation();

            // Attempt immediately again without advancing time (should hit cooldown)
            commands = _entityManager.GetBuffer<VegetationHarvestCommand>(_queueEntity);
            commands.Add(new VegetationHarvestCommand
            {
                Villager = villager,
                Vegetation = vegetation,
                SpeciesIndex = 0,
                RequestedAmount = 4f,
                IssuedTick = timeState.Tick + 1,
                CommandId = 0x3u
            });

            UpdateSimulation();

            var receipts = _entityManager.GetBuffer<VegetationHarvestReceipt>(_queueEntity);
            Assert.AreEqual(1, receipts.Length);
            Assert.AreEqual(VegetationHarvestResult.Cooldown, receipts[0].Result);

            // Clear command queue for next scenario
            commands.Clear();
            receipts.Clear();

            // Disable ready tag manually and try again (should report NotReady)
            _entityManager.SetComponentEnabled<VegetationReadyToHarvestTag>(vegetation, false);

            commands.Add(new VegetationHarvestCommand
            {
                Villager = villager,
                Vegetation = vegetation,
                SpeciesIndex = 0,
                RequestedAmount = 4f,
                IssuedTick = timeState.Tick + 2,
                CommandId = 0x4u
            });

            UpdateSimulation();

            receipts = _entityManager.GetBuffer<VegetationHarvestReceipt>(_queueEntity);
            Assert.AreEqual(1, receipts.Length);
            Assert.AreEqual(VegetationHarvestResult.NotReady, receipts[0].Result);
        }

        private Entity CreateFruitingVegetation(float currentProduction)
        {
            var vegetation = _entityManager.CreateEntity();
            _entityManager.AddComponentData(vegetation, new VegetationId { Value = 1, SpeciesType = 0 });
            _entityManager.AddComponentData(vegetation, new VegetationSpeciesIndex { Value = 0 });

            var production = new VegetationProduction
            {
                ResourceTypeId = "Wood",
                ProductionRate = 0f,
                MaxProductionCapacity = 25f,
                CurrentProduction = currentProduction,
                HarvestCooldown = 30f,
                LastHarvestTime = -999f
            };
            _entityManager.AddComponentData(vegetation, production);

            _entityManager.AddComponent<VegetationReadyToHarvestTag>(vegetation);
            _entityManager.SetComponentEnabled<VegetationReadyToHarvestTag>(vegetation, true);

            _entityManager.AddBuffer<VegetationHistoryEvent>(vegetation);

            return vegetation;
        }

        private Entity CreateVillager()
        {
            var villager = _entityManager.CreateEntity();
            _entityManager.AddBuffer<VillagerInventoryItem>(villager);
            return villager;
        }

        private void EnsureTimeAndRewindSingletons()
        {
            var timeEntity = GetSingletonEntity<TimeState>();
            var timeState = new TimeState
            {
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                Tick = 0,
                IsPaused = false
            };
            _entityManager.SetComponentData(timeEntity, timeState);

            var rewindEntity = GetSingletonEntity<RewindState>();
            var rewindState = new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = timeState.FixedDeltaTime,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            };
            _entityManager.SetComponentData(rewindEntity, rewindState);
            var legacy = new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = 0,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            };
            if (_entityManager.HasComponent<RewindLegacyState>(rewindEntity))
            {
                _entityManager.SetComponentData(rewindEntity, legacy);
            }
            else
            {
                _entityManager.AddComponentData(rewindEntity, legacy);
            }
        }

        private void EnsureCommandQueue()
        {
            var query = _entityManager.CreateEntityQuery(typeof(VegetationHarvestCommandQueue));
            if (query.IsEmptyIgnoreFilter)
            {
                _queueEntity = _entityManager.CreateEntity(typeof(VegetationHarvestCommandQueue));
                _entityManager.AddBuffer<VegetationHarvestCommand>(_queueEntity);
                _entityManager.AddBuffer<VegetationHarvestReceipt>(_queueEntity);
            }
            else
            {
                _queueEntity = query.GetSingletonEntity();
            }

            var commands = _entityManager.GetBuffer<VegetationHarvestCommand>(_queueEntity);
            commands.Clear();
            var receipts = _entityManager.GetBuffer<VegetationHarvestReceipt>(_queueEntity);
            receipts.Clear();
            query.Dispose();
        }

        private void EnsureSpeciesLookup()
        {
            var lookupQuery = _entityManager.CreateEntityQuery(typeof(VegetationSpeciesLookup));
            Entity lookupEntity;
            if (!lookupQuery.IsEmptyIgnoreFilter)
            {
                lookupEntity = lookupQuery.GetSingletonEntity();
                var lookup = _entityManager.GetComponentData<VegetationSpeciesLookup>(lookupEntity);
                if (lookup.CatalogBlob.IsCreated)
                {
                    lookup.CatalogBlob.Dispose();
                }
            }
            else
            {
                lookupEntity = _entityManager.CreateEntity(typeof(VegetationSpeciesLookup));
            }
            lookupQuery.Dispose();

            var builder = new BlobBuilder(Allocator.Temp);
            ref var catalogRoot = ref builder.ConstructRoot<VegetationSpeciesCatalogBlob>();
            var speciesArray = builder.Allocate(ref catalogRoot.Species, 1);

            ref var species = ref speciesArray[0];
            builder.AllocateString(ref species.SpeciesId, "HarvestTree");
            species.SeedlingDuration = 30f;
            species.GrowingDuration = 90f;
            species.MatureDuration = 60f;
            species.FloweringDuration = 15f;
            species.FruitingDuration = 60f;
            species.DyingDuration = 30f;
            species.RespawnDelay = 120f;
            species.BaseGrowthRate = 1f;

            var stageMultipliers = builder.Allocate(ref species.StageMultipliers, 6);
            for (int i = 0; i < stageMultipliers.Length; i++) stageMultipliers[i] = 1f;

            var seasonalMultipliers = builder.Allocate(ref species.SeasonalMultipliers, 4);
            for (int i = 0; i < seasonalMultipliers.Length; i++) seasonalMultipliers[i] = 1f;

            species.MaxHealth = 100f;
            species.BaselineRegen = 1f;
            species.DamagePerDeficit = 5f;
            species.DroughtToleranceSeconds = 60f;
            species.FrostToleranceSeconds = 30f;
            species.MaxYieldPerCycle = 8f;
            species.HarvestCooldown = 45f;
            builder.AllocateString(ref species.ResourceTypeId, "Wood");
            species.PartialHarvestPenalty = 0.25f;
            species.DesiredMinWater = 40f;
            species.DesiredMaxWater = 90f;
            species.DesiredMinLight = 50f;
            species.DesiredMaxLight = 100f;
            species.DesiredMinSoilQuality = 40f;
            species.DesiredMaxSoilQuality = 100f;
            species.PollutionTolerance = 0.25f;
            species.WindTolerance = 0.5f;
            species.ReproductionCooldown = 300f;
            species.SeedsPerEvent = 2;
            species.SpreadRadius = 4f;
            species.OffspringCapPerParent = 4;
            species.MaturityRequirement = 0.8f;
            species.GridCellPadding = 1;
            species.GrowthSeed = 0xA12F33u;
            species.ReproductionSeed = 0xB88954u;
            species.LootSeed = 0x199337u;

            _catalogBlob = builder.CreateBlobAssetReference<VegetationSpeciesCatalogBlob>(Allocator.Persistent);
            builder.Dispose();

            _entityManager.SetComponentData(lookupEntity, new VegetationSpeciesLookup
            {
                CatalogBlob = _catalogBlob
            });
        }

        private Entity GetSingletonEntity<T>() where T : unmanaged, IComponentData
        {
            var query = _entityManager.CreateEntityQuery(typeof(T));
            Entity entity;
            if (query.IsEmptyIgnoreFilter)
            {
                entity = _entityManager.CreateEntity(typeof(T));
            }
            else
            {
                entity = query.GetSingletonEntity();
            }
            query.Dispose();
            return entity;
        }

        private void UpdateSimulation()
        {
            var simulationGroup = _world.GetExistingSystemManaged<SimulationSystemGroup>() ??
                                  _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simulationGroup.Update();
        }
    }
}
