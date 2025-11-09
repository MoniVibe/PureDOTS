using System;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Tests
{
    public class VillagerJobLoopTests
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
        private SystemHandle _jobInterruptHandle;
        private SystemHandle _depositHandle;
        private SystemHandle _storeInventoryHandle;
        private SystemHandle _jobEventFlushHandle;
        private SystemHandle _jobHistoryHandle;
        private SystemHandle _jobPlaybackHandle;
        private SystemHandle _jobTimeAdapterHandle;
        private SystemHandle _aiSystemHandle;
        private SystemHandle _targetingSystemHandle;

        private BlobAssetReference<ResourceTypeIndexBlob> _resourceCatalog;
        private Entity _catalogEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("VillagerJobLoopTestsWorld");
            _entityManager = _world.EntityManager;

            EnsureCoreSingletons();

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
            _jobInterruptHandle = _world.GetOrCreateSystem<VillagerJobInterruptSystem>();
            _depositHandle = _world.GetOrCreateSystem<ResourceDepositSystem>();
            _storeInventoryHandle = _world.GetOrCreateSystem<StorehouseInventorySystem>();
            _jobEventFlushHandle = _world.GetOrCreateSystem<VillagerJobEventFlushSystem>();
            _jobHistoryHandle = _world.GetOrCreateSystem<VillagerJobHistorySystem>();
            _jobPlaybackHandle = _world.GetOrCreateSystem<VillagerJobPlaybackSystem>();
            _jobTimeAdapterHandle = _world.GetOrCreateSystem<VillagerJobTimeAdapterSystem>();
            _aiSystemHandle = _world.GetOrCreateSystem<VillagerAISystem>();
            _targetingSystemHandle = _world.GetOrCreateSystem<VillagerTargetingSystem>();

            // Villager job bootstrap ensures singletons.
            _world.GetOrCreateSystem<VillagerJobBootstrapSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_resourceCatalog.IsCreated)
            {
                _resourceCatalog.Dispose();
            }

            if (_catalogEntity != Entity.Null && _entityManager.Exists(_catalogEntity))
            {
                _entityManager.DestroyEntity(_catalogEntity);
            }

            _world.Dispose();
        }

        [Test]
        public void GatherDeliverLoop_CompletesAndUpdatesStorehouse()
        {
            CreateResourceTypeCatalog("Wood");

            var resource = CreateResource(new float3(0f, 0f, 0f), "Wood", 200f, 30f);
            var storehouse = CreateStorehouse(new float3(2f, 0f, 0f), "Wood", 500f);
            var villager = CreateVillager(new float3(0f, 0f, 0f));

            UpdateSystem(_reservationBootstrapHandle);
            UpdateSystem(_resourceRegistryHandle);
            UpdateSystem(_storehouseRegistryHandle);

            UpdateSystem(_jobInitHandle);
            UpdateSystem(_jobRequestHandle);
            UpdateSystem(_jobAssignHandle);

            var ticket = _entityManager.GetComponentData<VillagerJobTicket>(villager);
            Assert.AreNotEqual(0u, ticket.TicketId);
            Assert.AreEqual(resource, ticket.ResourceEntity);

            // Simulate gather ticks
            for (int i = 0; i < 5; i++)
            {
                IncrementTick();
                UpdateSystem(_jobExecuteHandle);
            }

            var job = _entityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Gathering, job.Phase);

            // Move villager near storehouse and deliver
            _entityManager.SetComponentData(villager, LocalTransform.FromPositionRotationScale(new float3(2f, 0f, 0f), quaternion.identity, 1f));

            UpdateSystem(_jobDeliverHandle);
            UpdateSystem(_jobEventFlushHandle);

            // Deposit resources
            IncrementTick();
            UpdateSystem(_depositHandle);
            UpdateSystem(_storeInventoryHandle);

            job = _entityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Completed, job.Phase);

            var inventory = _entityManager.GetComponentData<StorehouseInventory>(storehouse);
            Assert.Greater(inventory.TotalStored, 0f);

            // Job should return to idle on next request pass
            UpdateSystem(_jobRequestHandle);
            job = _entityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Idle, job.Phase);
        }

        [Test]
        public void Interrupt_ReleasesReservations()
        {
            CreateResourceTypeCatalog("Wood");

            var resource = CreateResource(new float3(0f, 0f, 0f), "Wood", 100f, 20f);
            CreateStorehouse(new float3(5f, 0f, 0f), "Wood", 500f);
            var villager = CreateVillager(new float3(0f, 0f, 0f));

            UpdateSystem(_reservationBootstrapHandle);
            UpdateSystem(_resourceRegistryHandle);
            UpdateSystem(_storehouseRegistryHandle);
            UpdateSystem(_jobInitHandle);
            UpdateSystem(_jobRequestHandle);
            UpdateSystem(_jobAssignHandle);

            var reservation = _entityManager.GetComponentData<ResourceJobReservation>(resource);
            Assert.AreEqual(1, reservation.ActiveTickets);

            reservation.ClaimFlags |= ResourceRegistryClaimFlags.PlayerClaim;
            _entityManager.SetComponentData(resource, reservation);

            UpdateSystem(_jobInterruptHandle);

            reservation = _entityManager.GetComponentData<ResourceJobReservation>(resource);
            Assert.AreEqual(0, reservation.ActiveTickets);
            Assert.AreEqual(0f, reservation.ReservedUnits);

            var job = _entityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Interrupted, job.Phase);
        }

        [Test]
        public void Rewind_RestoresJobState()
        {
            CreateResourceTypeCatalog("Wood");

            var resource = CreateResource(new float3(0f, 0f, 0f), "Wood", 100f, 25f);
            CreateStorehouse(new float3(1f, 0f, 0f), "Wood", 200f);
            var villager = CreateVillager(new float3(0f, 0f, 0f));

            UpdateSystem(_reservationBootstrapHandle);
            UpdateSystem(_resourceRegistryHandle);
            UpdateSystem(_storehouseRegistryHandle);
            UpdateSystem(_jobInitHandle);
            UpdateSystem(_jobRequestHandle);
            UpdateSystem(_jobAssignHandle);

            IncrementTick();
            UpdateSystem(_jobExecuteHandle);
            UpdateSystem(_jobTimeAdapterHandle);
            UpdateSystem(_jobHistoryHandle);

            var before = _entityManager.GetComponentData<VillagerJobProgress>(villager);
            Assert.Greater(before.Gathered, 0f);

            // Mutate state to verify rewind restores
            var job = _entityManager.GetComponentData<VillagerJob>(villager);
            job.Phase = VillagerJob.JobPhase.Delivering;
            _entityManager.SetComponentData(villager, job);

            var ticket = _entityManager.GetComponentData<VillagerJobTicket>(villager);
            ticket.ResourceEntity = Entity.Null;
            _entityManager.SetComponentData(villager, ticket);

            var rewind = _entityManager.GetSingleton<RewindState>();
            rewind.Mode = RewindMode.Playback;
            rewind.PlaybackTick = _entityManager.GetSingleton<TimeState>().Tick;
            _entityManager.SetSingleton(rewind);

            UpdateSystem(_jobTimeAdapterHandle);
            UpdateSystem(_jobPlaybackHandle);

            var restored = _entityManager.GetComponentData<VillagerJobProgress>(villager);
            Assert.AreEqual(before.Gathered, restored.Gathered);

            job = _entityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Gathering, job.Phase);

            rewind.Mode = RewindMode.Record;
            _entityManager.SetSingleton(rewind);
            UpdateSystem(_jobTimeAdapterHandle);
        }

        [Test]
        public void Targeting_UsesRegistryPositionWhenTransformMissing()
        {
            CreateResourceTypeCatalog("Wood");

            var resource = CreateResource(new float3(0f, 0f, 0f), "Wood", 150f, 25f);
            CreateStorehouse(new float3(5f, 0f, 0f), "Wood", 500f);
            var villager = CreateVillager(new float3(0f, 0f, 0f));

            UpdateSystem(_reservationBootstrapHandle);
            UpdateSystem(_resourceRegistryHandle);
            UpdateSystem(_storehouseRegistryHandle);
            UpdateSystem(_jobInitHandle);
            UpdateSystem(_jobRequestHandle);
            UpdateSystem(_jobAssignHandle);
            UpdateSystem(_aiSystemHandle);
            UpdateSystem(_targetingSystemHandle);

            var aiState = _entityManager.GetComponentData<VillagerAIState>(villager);
            Assert.AreEqual(resource, aiState.TargetEntity);
            Assert.AreNotEqual(float3.zero, aiState.TargetPosition);

            _entityManager.RemoveComponent<LocalTransform>(resource);
            aiState.TargetPosition = float3.zero;
            _entityManager.SetComponentData(villager, aiState);

            UpdateSystem(_targetingSystemHandle);

            aiState = _entityManager.GetComponentData<VillagerAIState>(villager);
            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var entries = _entityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            Assert.IsTrue(entries.TryFindEntryIndex(resource, out var entryIndex));
            Assert.AreEqual(entries[entryIndex].Position, aiState.TargetPosition);
        }

        private void EnsureCoreSingletons()
        {
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            if (!_entityManager.HasSingleton<HistorySettings>())
            {
                _entityManager.CreateEntity(typeof(HistorySettings));
                _entityManager.SetSingleton(HistorySettingsDefaults.CreateDefault());
            }
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
