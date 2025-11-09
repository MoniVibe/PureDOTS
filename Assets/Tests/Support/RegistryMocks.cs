using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Resource;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Support
{
    /// <summary>
    /// Mock registry utilities for testing without full system initialization.
    /// </summary>
    public static class RegistryMocks
    {
        /// <summary>
        /// Creates a mock resource registry entry.
        /// </summary>
        public static ResourceRegistryEntry CreateMockResourceEntry(
            Entity sourceEntity,
            float3 position,
            ushort resourceTypeIndex = 0,
            float unitsRemaining = 100f)
        {
            return new ResourceRegistryEntry
            {
                ResourceTypeIndex = resourceTypeIndex,
                SourceEntity = sourceEntity,
                Position = position,
                UnitsRemaining = unitsRemaining,
                ActiveTickets = 0,
                ClaimFlags = 0,
                LastMutationTick = 0,
                CellId = 0,
                SpatialVersion = 0,
                FamilyIndex = 0,
                Tier = ResourceTier.Raw
            };
        }

        /// <summary>
        /// Creates a mock storehouse registry entry.
        /// </summary>
        public static StorehouseRegistryEntry CreateMockStorehouseEntry(
            Entity sourceEntity,
            float3 position,
            float capacity = 1000f)
        {
            var entry = new StorehouseRegistryEntry
            {
                StorehouseEntity = sourceEntity,
                Position = position,
                TotalCapacity = capacity,
                TotalStored = 0f,
                TypeSummaries = default,
                LastMutationTick = 0,
                CellId = 0,
                SpatialVersion = 0
            };
            entry.TypeSummaries = new FixedList32Bytes<StorehouseRegistryCapacitySummary>();
            return entry;
        }

        /// <summary>
        /// Creates a mock villager registry entry.
        /// </summary>
        public static VillagerRegistryEntry CreateMockVillagerEntry(
            Entity villagerEntity,
            float3 position,
            int villagerId = 0,
            VillagerJob.JobType jobType = VillagerJob.JobType.None)
        {
            return new VillagerRegistryEntry
            {
                VillagerEntity = villagerEntity,
                VillagerId = villagerId,
                FactionId = 0,
                Position = position,
                CellId = 0,
                SpatialVersion = 0,
                JobType = jobType,
                JobPhase = VillagerJob.JobPhase.Idle,
                ActiveTicketId = 0,
                CurrentResourceTypeIndex = 0,
                AvailabilityFlags = 0,
                Discipline = 50,
                HealthPercent = 100,
                MoralePercent = 50,
                EnergyPercent = 100,
                AIState = 0,
                AIGoal = 0,
                CurrentTarget = Entity.Null,
                Productivity = 1f
            };
        }

        /// <summary>
        /// Creates a mock resource entity with the minimum components required by tests.
        /// </summary>
        public static Entity CreateMockResource(
            EntityManager entityManager,
            float3 position,
            FixedString64Bytes resourceTypeId,
            float unitsRemaining = 100f,
            float gatherRatePerWorker = 1f)
        {
            var entity = entityManager.CreateEntity(
                typeof(LocalTransform),
                typeof(ResourceTypeId),
                typeof(ResourceSourceConfig),
                typeof(ResourceSourceState));

            entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
            entityManager.SetComponentData(entity, new ResourceTypeId { Value = resourceTypeId });
            entityManager.SetComponentData(entity, new ResourceSourceConfig
            {
                GatherRatePerWorker = gatherRatePerWorker,
                MaxSimultaneousWorkers = 4,
                RespawnSeconds = 0f,
                Flags = ResourceSourceConfig.FlagInfinite
            });
            entityManager.SetComponentData(entity, new ResourceSourceState
            {
                UnitsRemaining = unitsRemaining
            });

            return entity;
        }

        /// <summary>
        /// Creates a mock storehouse entity with inventory/config components.
        /// </summary>
        public static Entity CreateMockStorehouse(
            EntityManager entityManager,
            float3 position,
            float capacity = 1000f,
            FixedString64Bytes label = default)
        {
            var entity = entityManager.CreateEntity(
                typeof(LocalTransform),
                typeof(StorehouseConfig),
                typeof(StorehouseInventory));

            entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
            var resolvedLabel = label;
            if (resolvedLabel.Length == 0)
            {
                resolvedLabel = new FixedString64Bytes("Storehouse");
            }

            entityManager.SetComponentData(entity, new StorehouseConfig
            {
                ShredRate = 0f,
                MaxShredQueueSize = 4,
                InputRate = 10f,
                OutputRate = 10f,
                Label = resolvedLabel
            });
            entityManager.SetComponentData(entity, new StorehouseInventory
            {
                TotalCapacity = capacity,
                TotalStored = 0f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0u
            });

            return entity;
        }

        /// <summary>
        /// Creates a mock villager entity with default AI/job state for deterministic tests.
        /// </summary>
        public static Entity CreateMockVillager(
            EntityManager entityManager,
            float3 position,
            int villagerId = 0,
            VillagerJob.JobType jobType = VillagerJob.JobType.None)
        {
            var entity = entityManager.CreateEntity(
                typeof(LocalTransform),
                typeof(VillagerId),
                typeof(VillagerAIState),
                typeof(VillagerJob),
                typeof(VillagerNeeds),
                typeof(VillagerAvailability));

            entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
            entityManager.SetComponentData(entity, new VillagerId
            {
                Value = villagerId,
                FactionId = 0
            });
            entityManager.SetComponentData(entity, new VillagerAIState
            {
                CurrentState = VillagerAIState.State.Idle,
                CurrentGoal = VillagerAIState.Goal.None,
                TargetPosition = position,
                StateStartTick = 0u,
                StateTimer = 0f
            });
            entityManager.SetComponentData(entity, new VillagerJob
            {
                Type = jobType,
                Phase = VillagerJob.JobPhase.Idle,
                ActiveTicketId = 0u,
                Productivity = 1f,
                LastStateChangeTick = 0u
            });
            entityManager.SetComponentData(entity, new VillagerNeeds
            {
                Health = 100f,
                MaxHealth = 100f,
                Hunger = 0,
                Energy = 1000,
                Morale = 500,
                Temperature = 0
            });
            entityManager.SetComponentData(entity, new VillagerAvailability
            {
                IsAvailable = 1,
                IsReserved = 0,
                LastChangeTick = 0u,
                BusyTime = 0f
            });

            return entity;
        }

        /// <summary>
        /// Populates a registry buffer with mock entries in a grid pattern.
        /// </summary>
        public static void PopulateResourceRegistryGrid(
            ref DynamicBuffer<ResourceRegistryEntry> buffer,
            int gridWidth,
            int gridHeight,
            float spacing,
            float3 origin,
            Allocator allocator = Allocator.Temp)
        {
            buffer.Clear();
            int count = gridWidth * gridHeight;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var position = origin + new float3(x * spacing, 0f, y * spacing);
                    var entity = Entity.Null; // Mock entity - in real tests, use EntityManager.CreateEntity()
                    var entry = CreateMockResourceEntry(entity, position);
                    buffer.Add(entry);
                }
            }
        }

        /// <summary>
        /// Populates a registry buffer with mock entries in a circle pattern.
        /// </summary>
        public static void PopulateRegistryCircle(
            ref DynamicBuffer<ResourceRegistryEntry> buffer,
            float3 center,
            float radius,
            int count,
            Allocator allocator = Allocator.Temp)
        {
            buffer.Clear();
            float angleStep = 2f * math.PI / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;
                var position = center + new float3(
                    math.cos(angle) * radius,
                    0f,
                    math.sin(angle) * radius);
                var entity = Entity.Null; // Mock entity
                var entry = CreateMockResourceEntry(entity, position);
                buffer.Add(entry);
            }
        }

        /// <summary>
        /// Creates a mock registry singleton entity with buffers for testing.
        /// </summary>
        public static Entity CreateMockRegistryEntity(
            EntityManager entityManager,
            RegistryKind kind)
        {
            var entity = entityManager.CreateEntity();

            switch (kind)
            {
                case RegistryKind.Resource:
                    entityManager.AddComponent<ResourceRegistry>(entity);
                    entityManager.AddBuffer<ResourceRegistryEntry>(entity);
                    break;
                case RegistryKind.Storehouse:
                    entityManager.AddComponent<StorehouseRegistry>(entity);
                    entityManager.AddBuffer<StorehouseRegistryEntry>(entity);
                    break;
                case RegistryKind.Villager:
                    entityManager.AddComponent<VillagerRegistry>(entity);
                    entityManager.AddBuffer<VillagerRegistryEntry>(entity);
                    break;
                default:
                    break;
            }

            return entity;
        }
    }
}


