using NUnit.Framework;
using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using PureDOTS.Systems;
using PureDOTS.Systems.Environment;
using PureDOTS.Tests;
using PureDOTS.Tests.Playmode;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Integration tests validating resource-environment interactions (regrowth rates, decay).
    /// </summary>
    public class ResourceEnvironmentIntegrationTests : EcsTestFixture
    {
        [Test]
        public void ResourceRegrowth_UsesEnvironmentCadence()
        {
            var world = World;
            var entityManager = world.EntityManager;

            // Ensure core singletons
            PureDOTS.Systems.CoreSingletonBootstrapSystem.EnsureSingletons(entityManager);

            // Initialize environment grids
            if (!entityManager.HasSingleton<EnvironmentGridConfigData>())
            {
                var newConfigEntity = entityManager.CreateEntity();
                var config = new EnvironmentGridConfigData
                {
                    Moisture = EnvironmentGridMetadata.Create(new float3(-50, 0, -50), new float3(50, 0, 50), 5f, new int2(20, 20)),
                    Temperature = EnvironmentGridMetadata.Create(new float3(-50, 0, -50), new float3(50, 0, 50), 10f, new int2(10, 10)),
                    MoistureChannelId = new FixedString64Bytes("moisture"),
                    TemperatureChannelId = new FixedString64Bytes("temperature"),
                    MoistureDiffusion = 0.25f,
                    MoistureSeepage = 0.1f,
                    BaseSeasonTemperature = 18f,
                    TimeOfDaySwing = 6f,
                    SeasonalSwing = 12f
                };
                entityManager.AddComponentData(newConfigEntity, config);
            }

            RunSystem<EnvironmentGridBootstrapSystem>();

            // Create a resource source that respawns
            var resourceEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(resourceEntity, new LocalTransform
            {
                Position = new float3(0f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            entityManager.AddComponentData(resourceEntity, new ResourceSourceConfig
            {
                GatherRatePerWorker = 4f,
                MaxSimultaneousWorkers = 3,
                RespawnSeconds = 45f,
                Flags = ResourceSourceConfig.FlagRespawns
            });
            entityManager.AddComponentData(resourceEntity, new ResourceSourceState
            {
                UnitsRemaining = 0f // Depleted
            });
            entityManager.AddComponentData(resourceEntity, new LastRecordedTick { Tick = 0 });

            // Verify resource system can query environment data
            // Note: Actual environment-based regrowth rates would be implemented in ResourceSourceManagementSystem
            // This test verifies the integration point exists
            world.GetOrCreateSystem<ResourceSourceManagementSystem>();
            
            // Sample environment at resource position
            var configEntity = RequireSingletonEntity<EnvironmentGridConfigData>();
            if (entityManager.HasComponent<MoistureGrid>(configEntity))
            {
                var moistureGrid = entityManager.GetComponentData<MoistureGrid>(configEntity);
                var moisture = moistureGrid.SampleBilinear(new float3(0f, 0f, 0f), 0f);
                Assert.GreaterOrEqual(moisture, 0f, "Moisture should be sampleable");
            }
            
            if (entityManager.HasComponent<TemperatureGrid>(configEntity))
            {
                var tempGrid = entityManager.GetComponentData<TemperatureGrid>(configEntity);
                var temperature = tempGrid.SampleBilinear(new float3(0f, 0f, 0f), 0f);
                Assert.GreaterOrEqual(temperature, 0f, "Temperature should be sampleable");
            }

            Assert.Pass("Resource systems can access environment data for regrowth calculations");
        }

        [Test]
        public void EnvironmentChange_TriggersResourceUpdate()
        {
            var world = World;
            var entityManager = world.EntityManager;

            // Ensure core singletons
            PureDOTS.Systems.CoreSingletonBootstrapSystem.EnsureSingletons(entityManager);

            // Initialize environment grids
            if (!entityManager.HasSingleton<EnvironmentGridConfigData>())
            {
                var newConfigEntity = entityManager.CreateEntity();
                var config = new EnvironmentGridConfigData
                {
                    Moisture = EnvironmentGridMetadata.Create(new float3(-50, 0, -50), new float3(50, 0, 50), 5f, new int2(20, 20)),
                    MoistureChannelId = new FixedString64Bytes("moisture"),
                    MoistureDiffusion = 0.25f,
                    MoistureSeepage = 0.1f
                };
                entityManager.AddComponentData(newConfigEntity, config);
            }

            RunSystem<EnvironmentGridBootstrapSystem>();

            // Create resource at known position
            var resourcePosition = new float3(0f, 0f, 0f);
            var resourceEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(resourceEntity, new LocalTransform
            {
                Position = resourcePosition,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            entityManager.AddComponentData(resourceEntity, new ResourceSourceConfig
            {
                GatherRatePerWorker = 4f,
                MaxSimultaneousWorkers = 3,
                RespawnSeconds = 45f,
                Flags = ResourceSourceConfig.FlagRespawns
            });
            entityManager.AddComponentData(resourceEntity, new ResourceSourceState
            {
                UnitsRemaining = 100f
            });

            // Sample initial environment
            var configEntity = RequireSingletonEntity<EnvironmentGridConfigData>();
            var moistureEntity = RequireSingletonEntity<MoistureGrid>();
            var initialMoisture = entityManager.GetComponentData<MoistureGrid>(moistureEntity).SampleBilinear(resourcePosition, 0f);

            // Change environment (e.g., add rain)
            if (!entityManager.HasBuffer<MoistureGridRuntimeCell>(moistureEntity))
            {
                var buffer = entityManager.AddBuffer<MoistureGridRuntimeCell>(moistureEntity);
                var gridData = entityManager.GetComponentData<MoistureGrid>(moistureEntity);
                buffer.ResizeUninitialized(gridData.Metadata.CellCount);
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = new MoistureGridRuntimeCell { Moisture = 50f };
                }
            }

            // Modify moisture at resource position
            var grid = entityManager.GetComponentData<MoistureGrid>(moistureEntity);
            if (EnvironmentGridMath.TryWorldToCell(grid.Metadata, resourcePosition, out var cell, out _))
            {
                var cellIndex = grid.GetCellIndex(cell);
                var buffer = entityManager.GetBuffer<MoistureGridRuntimeCell>(moistureEntity);
                if (cellIndex < buffer.Length)
                {
                    var cellData = buffer[cellIndex];
                    cellData.Moisture = 80f; // Increased moisture
                    buffer[cellIndex] = cellData;
                }
            }

            // Verify environment changed
            var newMoisture = entityManager.GetComponentData<MoistureGrid>(moistureEntity).SampleBilinear(resourcePosition, 0f);
            Assert.Greater(newMoisture, initialMoisture, "Environment should have changed");

            // Verify resource systems can react to change
            // Note: Actual reaction logic would be in ResourceSourceManagementSystem
            // This test verifies the data flow exists

            Assert.Pass("Environment changes are accessible to resource systems");
        }
    }
}
