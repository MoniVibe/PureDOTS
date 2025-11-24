using Godgame.Authoring;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Villagers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Spawns villagers at runtime based on VillageSpawnerConfig.
    /// Pure DOTS system - no MonoBehaviour dependencies.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(VillagerMovementSystem))]
    public partial struct VillageSpawnerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VillageSpawnerConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (spawner, transform, entity) in SystemAPI
                         .Query<RefRW<VillageSpawnerConfig>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var config = spawner.ValueRO;
                
                // Already spawned all villagers
                if (config.SpawnedCount >= config.VillagerCount)
                {
                    continue;
                }

                // Check if prefab is valid
                if (config.VillagerPrefab == Entity.Null)
                {
                    continue;
                }

                // Spawn remaining villagers
                var spawnCount = config.VillagerCount - config.SpawnedCount;
                var spawnPosition = transform.ValueRO.Position;
                var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + (uint)entity.Index);

                for (var i = 0; i < spawnCount; i++)
                {
                    // Random position within spawn radius
                    var angle = random.NextFloat(0f, math.PI * 2f);
                    var distance = random.NextFloat(0f, config.SpawnRadius);
                    var offset = new float3(
                        math.cos(angle) * distance,
                        0f,
                        math.sin(angle) * distance
                    );
                    var villagerPosition = spawnPosition + offset;

                    // Instantiate villager prefab
                    var villagerEntity = ecb.Instantiate(config.VillagerPrefab);
                    
                    // Set position
                    ecb.SetComponent(villagerEntity, LocalTransform.FromPositionRotationScale(
                        villagerPosition,
                        quaternion.identity,
                        1f
                    ));

                    // Ensure PureDOTS components exist (prefab should have them, but ensure they're set)
                    if (!state.EntityManager.HasComponent<VillagerId>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new VillagerId
                        {
                            Value = config.SpawnedCount + i + 1,
                            FactionId = 0
                        });
                    }

                    if (!state.EntityManager.HasComponent<VillagerJob>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new VillagerJob
                        {
                            Type = config.DefaultJobType,
                            Phase = VillagerJob.JobPhase.Idle,
                            ActiveTicketId = 0,
                            Productivity = 1f
                        });
                    }

                    if (!state.EntityManager.HasComponent<VillagerAIState>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new VillagerAIState
                        {
                            CurrentState = VillagerAIState.State.Idle,
                            CurrentGoal = config.DefaultAIGoal,
                            TargetEntity = Entity.Null
                        });
                    }

                    // Add new WorkOffer/WorkClaim system components
                    if (!state.EntityManager.HasComponent<WorkClaim>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new WorkClaim());
                    }
                    if (!state.EntityManager.HasComponent<VillagerSeed>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new VillagerSeed { Value = (uint)(villagerEntity.Index ^ 0x12345678) });
                    }
                    if (!state.EntityManager.HasComponent<VillagerNeedsHot>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new VillagerNeedsHot());
                    }
                    if (!state.EntityManager.HasComponent<VillagerShiftState>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new VillagerShiftState 
                        { 
                            DayShiftEnabled = 1, 
                            NightShiftEnabled = 0,
                            IsDaytime = 1,
                            ShouldWork = 1,
                            LastUpdateTick = 0
                        });
                    }
                    if (!state.EntityManager.HasComponent<VillagerJobPriorityState>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new VillagerJobPriorityState());
                    }
                    if (!state.EntityManager.HasComponent<SpatialLayerTag>(villagerEntity))
                    {
                        ecb.AddComponent(villagerEntity, new SpatialLayerTag { LayerId = 0 });
                    }
                }

                // Update spawner config
                spawner.ValueRW.SpawnedCount = config.VillagerCount;
            }
        }
    }
}





