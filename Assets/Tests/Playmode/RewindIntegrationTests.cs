using NUnit.Framework;
using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems;
using PureDOTS.Systems.Environment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Tests
{
    public class RewindIntegrationTests
    {
        private World _world;
        private EntityManager _entityManager;

        private SystemHandle _reservationBootstrapHandle;
        private SystemHandle _resourceRegistryHandle;
        private SystemHandle _storehouseRegistryHandle;
        private SystemHandle _jobInitHandle;
        private SystemHandle _jobRequestHandle;
        private SystemHandle _jobAssignHandle;
        private SystemHandle _jobExecuteHandle;
        private SystemHandle _jobDeliverHandle;
        private SystemHandle _jobEventFlushHandle;
        private SystemHandle _depositHandle;
        private SystemHandle _storeInventoryHandle;
        private SystemHandle _jobPlaybackHandle;
        private SystemHandle _jobTimeAdapterHandle;
        private SystemHandle _moistureAdapterHandle;
        private SystemHandle _storehouseAdapterHandle;

        private BlobAssetReference<ResourceTypeIndexBlob> _resourceCatalog;
        private BlobAssetReference<MoistureGridBlob> _moistureBlob;
        private Entity _catalogEntity;
        private Entity _moistureEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("RewindIntegration");
            _entityManager = _world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            if (!_entityManager.HasSingleton<HistorySettings>())
            {
                var history = _entityManager.CreateEntity(typeof(HistorySettings));
                _entityManager.SetComponentData(history, HistorySettingsDefaults.CreateDefault());
            }

            if (!_entityManager.HasSingleton<ClimateState>())
            {
                var climateEntity = _entityManager.CreateEntity(typeof(ClimateState));
                _entityManager.SetComponentData(climateEntity, new ClimateState
                {
                    CurrentSeason = Season.Spring,
                    SeasonProgress = 0f,
                    TimeOfDayHours = 12f,
                    DayNightProgress = 0.5f,
                    GlobalTemperature = 20f,
                    GlobalWindDirection = new float2(1f, 0f),
                    GlobalWindStrength = 3f,
                    AtmosphericMoisture = 50f,
                    CloudCover = 30f,
                    LastUpdateTick = 0
                });
            }

            if (!_entityManager.HasSingleton<EnvironmentGridConfigData>())
            {
                var gridConfigEntity = _entityManager.CreateEntity(typeof(EnvironmentGridConfigData));
                var metadata = EnvironmentGridMetadata.Create(new float3(-10f, 0f, -10f), new float3(10f, 0f, 10f), 1f, new int2(1, 1));
                _entityManager.SetComponentData(gridConfigEntity, new EnvironmentGridConfigData
                {
                    Moisture = metadata,
                    Temperature = metadata,
                    Sunlight = metadata,
                    Wind = metadata,
                    Biome = metadata,
                    MoistureDiffusion = 0f,
                    MoistureSeepage = 0f,
                    BaseSeasonTemperature = 20f,
                    TimeOfDaySwing = 5f,
                    SeasonalSwing = 10f,
                    DefaultSunDirection = new float3(0f, 1f, 0f),
                    DefaultSunIntensity = 1f,
                    DefaultWindDirection = new float2(1f, 0f),
                    DefaultWindStrength = 3f
                });
            }

            _reservationBootstrapHandle = _world.GetOrCreateSystem<ResourceReservationBootstrapSystem>();
            _resourceRegistryHandle = _world.GetOrCreateSystem<ResourceRegistrySystem>();
            var systemsAssembly = typeof(ResourceRegistrySystem).Assembly;
            var storehouseRegistryType = systemsAssembly.GetType("PureDOTS.Systems.StorehouseRegistrySystem");
            Assert.IsNotNull(storehouseRegistryType, "StorehouseRegistrySystem type not found. Ensure PureDOTS.Systems assembly is referenced.");
            _storehouseRegistryHandle = _world.GetOrCreateSystem(storehouseRegistryType);
            _jobInitHandle = _world.GetOrCreateSystem<VillagerJobInitializationSystem>();
            _jobRequestHandle = _world.GetOrCreateSystem<VillagerJobRequestSystem>();
            _jobAssignHandle = _world.GetOrCreateSystem<VillagerJobAssignmentSystem>();
            _jobExecuteHandle = _world.GetOrCreateSystem<VillagerJobExecutionSystem>();
            _jobDeliverHandle = _world.GetOrCreateSystem<VillagerJobDeliverySystem>();
            _jobEventFlushHandle = _world.GetOrCreateSystem<VillagerJobEventFlushSystem>();
            _depositHandle = _world.GetOrCreateSystem<ResourceDepositSystem>();
            _storeInventoryHandle = _world.GetOrCreateSystem<StorehouseInventorySystem>();
            _jobPlaybackHandle = _world.GetOrCreateSystem<VillagerJobPlaybackSystem>();
            _jobTimeAdapterHandle = _world.GetOrCreateSystem<VillagerJobTimeAdapterSystem>();
            _moistureAdapterHandle = _world.GetOrCreateSystem<MoistureGridTimeAdapterSystem>();
            _storehouseAdapterHandle = _world.GetOrCreateSystem<StorehouseInventoryTimeAdapterSystem>();

            _world.GetOrCreateSystem<VillagerJobBootstrapSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_resourceCatalog.IsCreated)
            {
                _resourceCatalog.Dispose();
            }

            if (_moistureBlob.IsCreated)
            {
                _moistureBlob.Dispose();
            }

            if (_catalogEntity != Entity.Null && _entityManager.Exists(_catalogEntity))
            {
                _entityManager.DestroyEntity(_catalogEntity);
            }

            if (_moistureEntity != Entity.Null && _entityManager.Exists(_moistureEntity))
            {
                _entityManager.DestroyEntity(_moistureEntity);
            }

            _world.Dispose();
        }

        [Test]
        public void Rewind_Restores_MiracleMoisture_And_VillagerFlows()
        {
            CreateResourceTypeCatalog("Wood");
            CreateMoistureGrid(initialMoisture: 10f);

            var resource = CreateResource(new float3(0f, 0f, 0f), "Wood", 120f, 30f);
            var storehouse = CreateStorehouse(new float3(2f, 0f, 0f), "Wood", 500f);
            var villager = CreateVillager(new float3(0f, 0f, 0f));

            float baselineMoisture = GetSingleCellMoisture();
            var storehouseInventory = _entityManager.GetComponentData<StorehouseInventory>(storehouse);
            Assert.AreEqual(0f, storehouseInventory.TotalStored, 0.001f);

            UpdateSystem(_moistureAdapterHandle);
            UpdateSystem(_storehouseAdapterHandle);
            UpdateSystem(_jobTimeAdapterHandle);

            // Simulate miracle rain and villager gather/dump flow.
            ApplyRainDelta(25f);

            UpdateSystem(_reservationBootstrapHandle);
            UpdateSystem(_resourceRegistryHandle);
            UpdateSystem(_storehouseRegistryHandle);

            UpdateSystem(_jobInitHandle);
            UpdateSystem(_jobRequestHandle);
            UpdateSystem(_jobAssignHandle);

            for (int i = 0; i < 4; i++)
            {
                IncrementTick();
                UpdateSystem(_jobExecuteHandle);
            }

            _entityManager.SetComponentData(villager,
                LocalTransform.FromPositionRotationScale(new float3(2f, 0f, 0f), quaternion.identity, 1f));

            UpdateSystem(_jobDeliverHandle);
            UpdateSystem(_jobEventFlushHandle);

            IncrementTick();
            UpdateSystem(_depositHandle);
            UpdateSystem(_storeInventoryHandle);

            float postRainMoisture = GetSingleCellMoisture();
            Assert.Greater(postRainMoisture, baselineMoisture);

            storehouseInventory = _entityManager.GetComponentData<StorehouseInventory>(storehouse);
            Assert.Greater(storehouseInventory.TotalStored, 0f);

            var job = _entityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreNotEqual(VillagerJob.JobPhase.Idle, job.Phase);

            var rewind = _entityManager.GetSingleton<RewindState>();
            rewind.Mode = RewindMode.Playback;
            rewind.TargetTick = 0;
            _entityManager.SetSingleton(rewind);
            var legacy = _entityManager.GetSingleton<RewindLegacyState>();
            legacy.PlaybackTick = 0;
            legacy.StartTick = _entityManager.GetSingleton<TimeState>().Tick;
            _entityManager.SetSingleton(legacy);
            var timeState = _entityManager.GetSingleton<TimeState>();
            timeState.Tick = 0;
            _entityManager.SetSingleton(timeState);

            UpdateSystem(_moistureAdapterHandle);
            UpdateSystem(_storehouseAdapterHandle);
            UpdateSystem(_jobTimeAdapterHandle);
            UpdateSystem(_jobPlaybackHandle);

            float restoredMoisture = GetSingleCellMoisture();
            Assert.AreEqual(baselineMoisture, restoredMoisture, 0.001f);

            storehouseInventory = _entityManager.GetComponentData<StorehouseInventory>(storehouse);
            Assert.AreEqual(0f, storehouseInventory.TotalStored, 0.001f);

            job = _entityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Idle, job.Phase);

            rewind.Mode = RewindMode.Record;
            _entityManager.SetSingleton(rewind);
            UpdateSystem(_jobTimeAdapterHandle);
        }

        private void CreateMoistureGrid(float initialMoisture)
        {
            var metadata = EnvironmentGridMetadata.Create(new float3(-4f, 0f, -4f), new float3(4f, 0f, 4f), 1f, new int2(1, 1));

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<MoistureGridBlob>();
            var moisture = builder.Allocate(ref root.Moisture, 1);
            moisture[0] = initialMoisture;
            builder.Allocate(ref root.DrainageRate, 1)[0] = 0f;
            builder.Allocate(ref root.TerrainHeight, 1)[0] = 0f;
            builder.Allocate(ref root.LastRainTick, 1)[0] = 0u;
            builder.Allocate(ref root.EvaporationRate, 1)[0] = 0f;

            _moistureBlob = builder.CreateBlobAssetReference<MoistureGridBlob>(Allocator.Persistent);
            builder.Dispose();

            _moistureEntity = _entityManager.CreateEntity(typeof(MoistureGrid), typeof(MoistureGridSimulationState));
            _entityManager.SetComponentData(_moistureEntity, new MoistureGrid
            {
                Metadata = metadata,
                Blob = _moistureBlob,
                ChannelId = new FixedString64Bytes("moisture"),
                DiffusionCoefficient = 0f,
                SeepageCoefficient = 0f,
                LastUpdateTick = 0,
                LastTerrainVersion = 0
            });
            _entityManager.SetComponentData(_moistureEntity, new MoistureGridSimulationState
            {
                LastEvaporationTick = uint.MaxValue,
                LastSeepageTick = uint.MaxValue
            });

            var buffer = _entityManager.AddBuffer<MoistureGridRuntimeCell>(_moistureEntity);
            buffer.Add(new MoistureGridRuntimeCell
            {
                Moisture = initialMoisture,
                EvaporationRate = 0f,
                LastRainTick = 0u
            });
        }

        private float GetSingleCellMoisture()
        {
            var buffer = _entityManager.GetBuffer<MoistureGridRuntimeCell>(_moistureEntity);
            return buffer[0].Moisture;
        }

        private void ApplyRainDelta(float delta)
        {
            var buffer = _entityManager.GetBuffer<MoistureGridRuntimeCell>(_moistureEntity);
            var cell = buffer[0];
            cell.Moisture += delta;
            cell.LastRainTick = _entityManager.GetSingleton<TimeState>().Tick;
            buffer[0] = cell;
        }

        private void CreateResourceTypeCatalog(params string[] ids)
        {
            if (_catalogEntity != Entity.Null && _entityManager.Exists(_catalogEntity))
            {
                _entityManager.DestroyEntity(_catalogEntity);
                _catalogEntity = Entity.Null;
            }

            if (_resourceCatalog.IsCreated)
            {
                _resourceCatalog.Dispose();
                _resourceCatalog = default;
            }

            _catalogEntity = _entityManager.CreateEntity();
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceTypeIndexBlob>();

            var length = ids?.Length ?? 0;
            var idsBuilder = builder.Allocate(ref root.Ids, length);
            var namesBuilder = builder.Allocate(ref root.DisplayNames, length);
            var colorsBuilder = builder.Allocate(ref root.Colors, length);

            for (int i = 0; i < length; i++)
            {
                idsBuilder[i] = new FixedString64Bytes(ids[i]);
                builder.AllocateString(ref namesBuilder[i], ids[i]);
                colorsBuilder[i] = new Color32(255, 255, 255, 255);
            }

            _resourceCatalog = builder.CreateBlobAssetReference<ResourceTypeIndexBlob>(Allocator.Persistent);
            builder.Dispose();

            _entityManager.AddComponentData(_catalogEntity, new ResourceTypeIndex
            {
                Catalog = _resourceCatalog
            });
        }

        private Entity CreateResource(float3 position, FixedString64Bytes resourceId, float unitsRemaining, float gatherRate)
        {
            var entity = _entityManager.CreateEntity(typeof(ResourceSourceConfig), typeof(ResourceSourceState), typeof(ResourceTypeId), typeof(LocalTransform));
            _entityManager.SetComponentData(entity, new ResourceSourceConfig
            {
                GatherRatePerWorker = gatherRate,
                MaxSimultaneousWorkers = 4,
                RespawnSeconds = 10f,
                Flags = 0
            });
            _entityManager.SetComponentData(entity, new ResourceSourceState { UnitsRemaining = unitsRemaining });
            _entityManager.SetComponentData(entity, new ResourceTypeId { Value = resourceId });
            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
            return entity;
        }

        private Entity CreateStorehouse(float3 position, FixedString64Bytes resourceId, float capacity)
        {
            var entity = _entityManager.CreateEntity(typeof(StorehouseConfig), typeof(StorehouseInventory), typeof(LocalTransform));
            _entityManager.SetComponentData(entity, new StorehouseConfig
            {
                InputRate = 50f,
                OutputRate = 15f,
                ShredRate = 0f,
                MaxShredQueueSize = 4
            });
            _entityManager.SetComponentData(entity, new StorehouseInventory
            {
                TotalStored = 0f,
                TotalCapacity = capacity,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });
            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));

            var capacities = _entityManager.AddBuffer<StorehouseCapacityElement>(entity);
            capacities.Add(new StorehouseCapacityElement
            {
                ResourceTypeId = resourceId,
                MaxCapacity = capacity
            });

            _entityManager.AddBuffer<StorehouseInventoryItem>(entity);
            return entity;
        }

        private Entity CreateVillager(float3 position)
        {
            var entity = _entityManager.CreateEntity(
                typeof(VillagerJob),
                typeof(VillagerJobTicket),
                typeof(VillagerJobProgress),
                typeof(VillagerAvailability),
                typeof(VillagerNeeds),
                typeof(VillagerAIState),
                typeof(LocalTransform));

            _entityManager.SetComponentData(entity, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Idle,
                ActiveTicketId = 0,
                Productivity = 1f,
                LastStateChangeTick = 0
            });

            _entityManager.SetComponentData(entity, new VillagerJobTicket
            {
                TicketId = 0,
                JobType = VillagerJob.JobType.Gatherer,
                ResourceTypeIndex = ushort.MaxValue,
                ResourceEntity = Entity.Null,
                StorehouseEntity = Entity.Null,
                Priority = 0,
                Phase = (byte)VillagerJob.JobPhase.Idle,
                ReservedUnits = 0f,
                AssignedTick = 0,
                LastProgressTick = 0
            });

            _entityManager.SetComponentData(entity, new VillagerJobProgress
            {
                Gathered = 0f,
                Delivered = 0f,
                TimeInPhase = 0f,
                LastUpdateTick = 0
            });

            _entityManager.SetComponentData(entity, new VillagerAvailability
            {
                IsAvailable = 1,
                IsReserved = 0,
                LastChangeTick = 0,
                BusyTime = 0f
            });

            var needs = new VillagerNeeds
            {
                Health = 100f,
                MaxHealth = 100f
            };
            needs.SetHunger(10f);
            needs.SetEnergy(90f);
            needs.SetMorale(80f);
            needs.SetTemperature(20f);
            _entityManager.SetComponentData(entity, needs);

            _entityManager.SetComponentData(entity, new VillagerAIState
            {
                CurrentState = VillagerAIState.State.Working,
                CurrentGoal = VillagerAIState.Goal.Work,
                TargetEntity = Entity.Null,
                TargetPosition = position,
                StateTimer = 0f,
                StateStartTick = 0
            });

            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));

            _entityManager.AddBuffer<VillagerJobCarryItem>(entity);
            _entityManager.AddBuffer<VillagerInventoryItem>(entity);
            _entityManager.AddBuffer<VillagerJobHistorySample>(entity);
            return entity;
        }

        private void RunSystem(SystemHandle handle)
        {
            handle.Update(_world.Unmanaged);
        }

        private void UpdateSystem(SystemHandle handle)
        {
            handle.Update(_world.Unmanaged);
        }

        private void IncrementTick()
        {
            var time = _entityManager.GetSingleton<TimeState>();
            time.Tick += 1;
            _entityManager.SetSingleton(time);
        }
    }
}
