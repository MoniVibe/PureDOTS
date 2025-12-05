using PureDOTS.Runtime;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Village;
using PureDOTS.Runtime.Villager;
using PureDOTS.Runtime.Groups;
using PureDOTS.Runtime.Formation;
using PureDOTS.Runtime.IntergroupRelations;
using PureDOTS.Runtime.Platform;
#if SPACE4X_AVAILABLE
using Space4X.Mining;
#endif
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Bootstrap
{
    /// <summary>
    /// PureDOTS system that spawns demo scenario entities from DemoScenarioConfig.
    /// Spreads spawning across multiple frames using BootPhase state machine.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(DemoScenarioBootstrapSystem))]
    public partial struct DemoScenarioRunnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DemoScenarioConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<DemoScenarioConfig>(out var config))
            {
                return;
            }

            if (!SystemAPI.TryGetSingletonEntity<DemoScenarioState>(out var scenarioEntity))
            {
                return;
            }

            var scenarioState = SystemAPI.GetComponent<DemoScenarioState>(scenarioEntity);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            switch (scenarioState.BootPhase)
            {
                case DemoBootPhase.None:
                    // Initialize boot phase based on config
                    if (config.EnableGodgame)
                    {
                        scenarioState.BootPhase = DemoBootPhase.SpawnGodgame;
                    }
                    else if (config.EnableSpace4x)
                    {
                        scenarioState.BootPhase = DemoBootPhase.SpawnSpace4x;
                    }
                    else
                    {
                        // Nothing to spawn, mark as done
                        scenarioState.BootPhase = DemoBootPhase.Done;
                        scenarioState.IsInitialized = true;
                    }
                    ecb.SetComponent(scenarioEntity, scenarioState);
                    ecb.Playback(state.EntityManager);
                    break;

                case DemoBootPhase.SpawnGodgame:
                    {
                        var random = Unity.Mathematics.Random.CreateFromIndex(config.GodgameSeed);
                        SpawnGodgameSlice(ref state, ref ecb, config, ref random);
                        ecb.Playback(state.EntityManager);

                        // Move to next phase
                        scenarioState.BootPhase = config.EnableSpace4x ? DemoBootPhase.SpawnSpace4x : DemoBootPhase.Done;
                        ecb.SetComponent(scenarioEntity, scenarioState);
                        ecb.Playback(state.EntityManager);
                    }
                    break;

                case DemoBootPhase.SpawnSpace4x:
                    {
                        var spaceRandom = Unity.Mathematics.Random.CreateFromIndex(config.Space4xSeed);
                        SpawnSpace4xSlice(ref state, ref ecb, config, ref spaceRandom);
                        ecb.Playback(state.EntityManager);

                        // Mark as done
                        scenarioState.BootPhase = DemoBootPhase.Done;
                        scenarioState.IsInitialized = true;
                        scenarioState.EnableGodgame = config.EnableGodgame;
                        scenarioState.EnableSpace4x = config.EnableSpace4x;
                        ecb.SetComponent(scenarioEntity, scenarioState);

                        // Mark config as complete
                        if (SystemAPI.TryGetSingletonEntity<DemoScenarioConfig>(out var configEntity))
                        {
                            ecb.AddComponent<DemoScenarioCompleteTag>(configEntity);
                        }

                        ecb.Playback(state.EntityManager);
                        state.Enabled = false;
                    }
                    break;

                case DemoBootPhase.Done:
                    state.Enabled = false;
                    break;
            }
        }

        private static void SpawnGodgameSlice(ref SystemState state, ref EntityCommandBuffer ecb, in DemoScenarioConfig config, ref Unity.Mathematics.Random random)
        {
            // Spawn flat terrain tile (simple quad)
            var terrainEntity = ecb.CreateEntity();
            ecb.AddComponent(terrainEntity, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 200f // Large flat plane
            });
            // Note: Visual representation handled by presentation layer

            // Create orgs for villages first
            NativeList<Entity> villageOrgs = new NativeList<Entity>(config.VillageCount, Allocator.Temp);
            for (int v = 0; v < config.VillageCount; v++)
            {
                var orgEntity = ecb.CreateEntity();
                ecb.AddComponent<OrgTag>(orgEntity);
                ecb.AddComponent(orgEntity, new OrgId
                {
                    Value = v + 1,
                    Kind = OrgKind.Faction
                });
                ecb.AddComponent(orgEntity, new OrgAlignment
                {
                    Moral = random.NextFloat(-0.5f, 0.5f),
                    Order = random.NextFloat(-0.5f, 0.5f),
                    Purity = random.NextFloat(-0.5f, 0.5f)
                });
                ecb.AddComponent(orgEntity, new OrgOutlook
                {
                    Primary = (byte)(v % 4), // Cycle through outlooks
                    Secondary = (byte)((v + 1) % 4)
                });
                villageOrgs.Add(orgEntity);
            }

            // Spawn villages
            for (int v = 0; v < config.VillageCount; v++)
            {
                float angle = (float)v / config.VillageCount * math.PI * 2f;
                float radius = 20f + random.NextFloat(0f, 10f) * config.Density;
                float3 villagePos = new float3(
                    math.cos(angle) * radius,
                    0f,
                    math.sin(angle) * radius
                );

                var villageEntity = ecb.CreateEntity();
                ecb.AddComponent(villageEntity, new LocalTransform
                {
                    Position = villagePos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                // Basic village components
                ecb.AddComponent<VillageTag>(villageEntity);
                ecb.AddComponent(villageEntity, new PureDOTS.Runtime.Village.VillageId
                {
                    Value = v + 1
                });
                ecb.AddComponent(villageEntity, new VillageAlignment
                {
                    LawChaos = 0f,
                    GoodEvil = 0f,
                    OrderChaos = 0f
                });
                ecb.AddComponent(villageEntity, new VillageResources
                {
                    Food = 100f,
                    Wood = 50f,
                    Stone = 30f,
                    Ore = 20f,
                    Metal = 0f,
                    Fuel = 0f
                });
                
                // Assign owner org
                ecb.AddComponent(villageEntity, new OwnerOrg
                {
                    OrgEntity = villageOrgs[v]
                });

                // Spawn Storage building near village
                float3 storagePos = villagePos + new float3(2f, 0f, 2f);
                var storageEntity = ecb.CreateEntity();
                ecb.AddComponent(storageEntity, new LocalTransform
                {
                    Position = storagePos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                ecb.AddComponent<StorageTag>(storageEntity);
                ecb.AddComponent<RewindableTag>(storageEntity);
                ecb.AddBuffer<ResourceStack>(storageEntity); // Inventory for storage

                // Spawn Lumberyard facility
                float3 lumberyardPos = villagePos + new float3(-2f, 0f, 2f);
                var lumberyardEntity = ecb.CreateEntity();
                ecb.AddComponent(lumberyardEntity, new LocalTransform
                {
                    Position = lumberyardPos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                ecb.AddComponent<LumberyardTag>(lumberyardEntity);
                ecb.AddComponent(lumberyardEntity, new PureDOTS.Runtime.Facility.Facility
                {
                    ArchetypeId = PureDOTS.Runtime.Facility.FacilityArchetypeId.Lumberyard,
                    CurrentRecipeId = 0, // First recipe for Lumberyard
                    WorkProgress = 0f
                });
                ecb.AddBuffer<ResourceStack>(lumberyardEntity); // Inventory for facility
                ecb.AddComponent<RewindableTag>(lumberyardEntity);

                // Spawn Smelter facility
                float3 smelterPos = villagePos + new float3(2f, 0f, -2f);
                var smelterEntity = ecb.CreateEntity();
                ecb.AddComponent(smelterEntity, new LocalTransform
                {
                    Position = smelterPos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                ecb.AddComponent<SmelterTag>(smelterEntity);
                ecb.AddComponent(smelterEntity, new PureDOTS.Runtime.Facility.Facility
                {
                    ArchetypeId = PureDOTS.Runtime.Facility.FacilityArchetypeId.Smelter,
                    CurrentRecipeId = 1, // First recipe for Smelter
                    WorkProgress = 0f
                });
                ecb.AddBuffer<ResourceStack>(smelterEntity); // Inventory for facility
                ecb.AddComponent<RewindableTag>(smelterEntity);

                // Create village group/band
                var villageGroupEntity = ecb.CreateEntity();
                ecb.AddComponent(villageGroupEntity, new LocalTransform
                {
                    Position = villagePos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                ecb.AddComponent<GroupTag>(villageGroupEntity);
                ecb.AddComponent(villageGroupEntity, new GroupIdentity
                {
                    GroupId = v + 1,
                    ParentEntity = villageEntity,
                    LeaderEntity = Entity.Null, // Will be set to first villager
                    FormationTick = 0,
                    Status = GroupStatus.Active
                });
                ecb.AddComponent(villageGroupEntity, GroupConfig.Default);
                var groupMembers = ecb.AddBuffer<GroupMember>(villageGroupEntity);

                // Spawn villagers for this village
                Entity firstVillager = Entity.Null;
                for (int i = 0; i < config.VillagersPerVillage; i++)
                {
                    float villagerAngle = random.NextFloat(0f, math.PI * 2f);
                    float villagerRadius = random.NextFloat(2f, 8f);
                    float3 villagerPos = villagePos + new float3(
                        math.cos(villagerAngle) * villagerRadius,
                        0f,
                        math.sin(villagerAngle) * villagerRadius
                    );

                    var villagerEntity = CreateVillagerEntity(ref ecb, villagerPos, v + 1, i + 1);
                    
                    if (i == 0)
                    {
                        firstVillager = villagerEntity;
                    }

                    // Add villager to village group
                    groupMembers.Add(new GroupMember
                    {
                        MemberEntity = villagerEntity,
                        Weight = i == 0 ? 1f : 0.5f, // First villager is leader
                        Role = i == 0 ? GroupRole.Leader : GroupRole.Member,
                        JoinedTick = 0,
                        Flags = GroupMemberFlags.Active
                    });
                }

                // Set first villager as leader
                if (firstVillager != Entity.Null)
                {
                    var groupIdentity = state.EntityManager.GetComponentData<GroupIdentity>(villageGroupEntity);
                    groupIdentity.LeaderEntity = firstVillager;
                    ecb.SetComponent(villageGroupEntity, groupIdentity);
                }

                // Spawn resource nodes around village
                int nodeCount = math.max(3, (int)(5 * config.Density));
                for (int n = 0; n < nodeCount; n++)
                {
                    float nodeAngle = random.NextFloat(0f, math.PI * 2f);
                    float nodeRadius = random.NextFloat(10f, 20f);
                    float3 nodePos = villagePos + new float3(
                        math.cos(nodeAngle) * nodeRadius,
                        0f,
                        math.sin(nodeAngle) * nodeRadius
                    );

                    // Alternate between tree, stone, ore
                    var nodeType = (n % 3);
                    float nodeRichness = 100f * config.Density;
                    CreateResourceNode(ref ecb, nodePos, nodeType, nodeRichness);
                }
            }

            // Spawn bands
            for (int b = 0; b < config.StartingBandCount; b++)
            {
                float angle = random.NextFloat(0f, math.PI * 2f);
                float radius = 30f + random.NextFloat(0f, 15f);
                float3 bandPos = new float3(
                    math.cos(angle) * radius,
                    0f,
                    math.sin(angle) * radius
                );

                // Basic band entity
                var bandEntity = ecb.CreateEntity();
                ecb.AddComponent(bandEntity, new LocalTransform
                {
                    Position = bandPos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                ecb.AddComponent<GroupTag>(bandEntity);
                ecb.AddComponent(bandEntity, new GroupIdentity
                {
                    GroupId = b + 1,
                    ParentEntity = Entity.Null,
                    LeaderEntity = Entity.Null,
                    FormationTick = 0,
                    Status = GroupStatus.Active
                });
                ecb.AddComponent(bandEntity, GroupConfig.Default);
                ecb.AddComponent(bandEntity, new GroupStanceState
                {
                    Stance = GroupStance.Hold,
                    PrimaryTarget = Entity.Null,
                    Aggression = 0f,
                    Discipline = 0.5f
                });
                ecb.AddComponent(bandEntity, new FormationState
                {
                    Type = PureDOTS.Runtime.Formation.FormationType.Patrol,
                    AnchorPosition = bandPos,
                    AnchorRotation = quaternion.identity,
                    Spacing = 2f,
                    Scale = 1f,
                    MaxSlots = 8,
                    FilledSlots = 0,
                    IsMoving = false,
                    LastUpdateTick = 0
                });
                ecb.AddBuffer<GroupMember>(bandEntity);
            }
        }

        private static void SpawnSpace4xSlice(ref SystemState state, ref EntityCommandBuffer ecb, in DemoScenarioConfig config, ref Unity.Mathematics.Random random)
        {
            // Create orgs for carriers first
            NativeList<Entity> carrierOrgs = new NativeList<Entity>(config.CarrierCount, Allocator.Temp);
            for (int c = 0; c < config.CarrierCount; c++)
            {
                var orgEntity = ecb.CreateEntity();
                ecb.AddComponent<OrgTag>(orgEntity);
                ecb.AddComponent(orgEntity, new OrgId
                {
                    Value = c + 1,
                    Kind = OrgKind.Faction
                });
                ecb.AddComponent(orgEntity, new OrgAlignment
                {
                    Moral = random.NextFloat(-0.5f, 0.5f),
                    Order = random.NextFloat(-0.5f, 0.5f),
                    Purity = random.NextFloat(-0.5f, 0.5f)
                });
                ecb.AddComponent(orgEntity, new OrgOutlook
                {
                    Primary = (byte)(c % 4),
                    Secondary = (byte)((c + 1) % 4)
                });
                carrierOrgs.Add(orgEntity);
            }

            // Spawn carriers
            for (int c = 0; c < config.CarrierCount; c++)
            {
                float angle = (float)c / config.CarrierCount * math.PI * 2f;
                float radius = 50f + random.NextFloat(0f, 20f);
                float3 carrierPos = new float3(
                    math.cos(angle) * radius,
                    random.NextFloat(-5f, 5f),
                    math.sin(angle) * radius
                );

                var carrierEntity = ecb.CreateEntity();
                ecb.AddComponent(carrierEntity, new LocalTransform
                {
                    Position = carrierPos,
                    Rotation = quaternion.identity,
                    Scale = 10f // Carriers are large
                });

                // Platform components
                ecb.AddComponent<PlatformTag>(carrierEntity);
                ecb.AddComponent(carrierEntity, new PlatformKind
                {
                    Flags = PlatformFlags.Capital | PlatformFlags.IsCarrier | PlatformFlags.HasHangar
                });
                ecb.AddComponent(carrierEntity, new PlatformHullRef
                {
                    HullId = 1 // Stub hull ID
                });
                ecb.AddComponent(carrierEntity, new PlatformResources
                {
                    Ore = 0f,
                    RefinedOre = 0f,
                    Fuel = 100f,
                    Supplies = 50f,
                    RawMaterials = 0f,
                    ProcessedMaterials = 0f
                });

                // HangarBay buffer
                int minersPerCarrier = 3; // Default value
                int craftPerCarrier = 2; // Default value
                var hangarBays = ecb.AddBuffer<HangarBay>(carrierEntity);
                hangarBays.Add(new HangarBay
                {
                    HangarClassId = 1,
                    Capacity = minersPerCarrier + craftPerCarrier,
                    ReservedSlots = 0,
                    OccupiedSlots = 0,
                    LaunchRate = 1f,
                    RecoveryRate = 1f
                });

                var hangarAssignments = ecb.AddBuffer<HangarAssignment>(carrierEntity);

                // Add Facility component for refinery (ore -> refined ore)
                ecb.AddComponent(carrierEntity, new PureDOTS.Runtime.Facility.Facility
                {
                    ArchetypeId = PureDOTS.Runtime.Facility.FacilityArchetypeId.Refinery,
                    CurrentRecipeId = 2, // Refinery recipe
                    WorkProgress = 0f
                });
                ecb.AddBuffer<ResourceStack>(carrierEntity); // Inventory for facility

                // Spawn miners for this carrier
                for (int m = 0; m < minersPerCarrier; m++)
                {
                    float3 minerPos = carrierPos + random.NextFloat3Direction() * random.NextFloat(5f, 10f);
                    var minerEntity = CreateMiningVessel(ref ecb, minerPos, carrierEntity, m + 1);

                    // Add to hangar assignment
                    hangarAssignments.Add(new HangarAssignment
                    {
                        SubPlatform = minerEntity,
                        HangarIndex = 0
                    });
                }

                // Assign owner org
                ecb.AddComponent(carrierEntity, new OwnerOrg
                {
                    OrgEntity = carrierOrgs[c]
                });

                // Add rewind support
                ecb.AddComponent<RewindableTag>(carrierEntity);
            }

            carrierOrgs.Dispose();

            // Spawn asteroids
            for (int a = 0; a < config.AsteroidCount; a++)
            {
                float angle = random.NextFloat(0f, math.PI * 2f);
                float radius = 60f + random.NextFloat(0f, 30f);
                float3 asteroidPos = new float3(
                    math.cos(angle) * radius,
                    random.NextFloat(-10f, 10f),
                    math.sin(angle) * radius
                );

                var asteroidEntity = ecb.CreateEntity();
                float3 randomEuler = random.NextFloat3(new float3(0f, 0f, 0f), new float3(360f, 360f, 360f));
                ecb.AddComponent(asteroidEntity, new LocalTransform
                {
                    Position = asteroidPos,
                    Rotation = quaternion.Euler(math.radians(randomEuler)),
                    Scale = 1f + random.NextFloat(0.5f, 2f)
                });

                // Add ResourceNodeTag and ResourceDeposit
                ecb.AddComponent<ResourceNodeTag>(asteroidEntity);
                ecb.AddComponent(asteroidEntity, new ResourceDeposit
                {
                    ResourceTypeId = 2, // Ore (assuming 0=wood, 1=stone, 2=ore)
                    CurrentAmount = 200f * config.Density,
                    MaxAmount = 200f * config.Density,
                    RegenPerSecond = 0f
                });

                // Add rewind support
                ecb.AddComponent<RewindableTag>(asteroidEntity);
            }
        }

        private static Entity CreateMiningVessel(ref EntityCommandBuffer ecb, float3 position, Entity carrierEntity, int minerId)
        {
            var entity = ecb.CreateEntity();
            ecb.AddComponent(entity, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

#if SPACE4X_AVAILABLE
            // Mining vessel components
            ecb.AddComponent<MiningVesselTag>(entity);
            ecb.AddComponent(entity, new MiningVesselFrameDef
            {
                MaxCargo = 50f,
                MiningRate = 5f
            });
            ecb.AddComponent(entity, new CraftFrameRef
            {
                FrameId = 1 // Stub frame ID
            });
            ecb.AddComponent(entity, new MiningJob
            {
                Phase = MiningPhase.Idle,
                TargetAsteroid = Entity.Null,
                CarrierEntity = carrierEntity,
                CargoAmount = 0f,
                TargetPosition = float3.zero,
                LastStateChangeTick = 0
            });
#else
            // Space4X types not available - create minimal platform entity instead
            ecb.AddComponent<PlatformTag>(entity);
            ecb.AddComponent(entity, new PlatformKind
            {
                Flags = PlatformFlags.Craft
            });
#endif

            // Add rewind support
            ecb.AddComponent<RewindableTag>(entity);

            return entity;
        }

        private static Entity CreateVillagerEntity(ref EntityCommandBuffer ecb, float3 position, int villageId, int villagerId)
        {
            var entity = ecb.CreateEntity();
            ecb.AddComponent(entity, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            ecb.AddComponent(entity, new VillagerId
            {
                Value = villagerId,
                FactionId = villageId
            });

            ecb.AddComponent(entity, new VillagerNeeds
            {
                Food = 50,
                Rest = 80,
                Sleep = 70,
                GeneralHealth = 100,
                Health = 100f,
                MaxHealth = 100f,
                Hunger = 50f,
                Energy = 80f,
                Morale = 75f,
                Temperature = 20f
            });

            ecb.AddComponent(entity, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Idle,
                ActiveTicketId = 0,
                Productivity = 1f,
                LastStateChangeTick = 0
            });

            ecb.AddComponent(entity, new VillagerAIState
            {
                CurrentState = VillagerAIState.State.Idle,
                CurrentGoal = VillagerAIState.Goal.Work,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                StateTimer = 0f,
                StateStartTick = 0
            });

            // Add movement component
            ecb.AddComponent(entity, new VillagerMovement
            {
                Velocity = float3.zero,
                BaseSpeed = 3f,
                CurrentSpeed = 3f,
                DesiredRotation = quaternion.identity,
                IsMoving = 0,
                IsStuck = 0,
                LastMoveTick = 0
            });

            // Add rewind support
            ecb.AddComponent<RewindableTag>(entity);
            ecb.AddBuffer<PositionHistorySample>(entity);
            ecb.AddBuffer<HealthHistorySample>(entity);

            return entity;
        }

        private static void CreateResourceNode(ref EntityCommandBuffer ecb, float3 position, int nodeType, float richness = 100f)
        {
            var entity = ecb.CreateEntity();
            ecb.AddComponent(entity, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // Add ResourceNodeTag
            ecb.AddComponent<ResourceNodeTag>(entity);
            
            // Mark as pickable for hand interactions
            ecb.AddComponent<PickableTag>(entity);

            // Add type-specific tag
            // nodeType: 0=tree, 1=stone, 2=ore
            if (nodeType == 0)
            {
                ecb.AddComponent<TreeTag>(entity);
            }
            else if (nodeType == 1)
            {
                ecb.AddComponent<StoneNodeTag>(entity);
            }
            else if (nodeType == 2)
            {
                ecb.AddComponent<OreNodeTag>(entity);
            }

            // Add ResourceDeposit component
            // ResourceTypeId: 0=wood, 1=stone, 2=ore (assuming catalog order)
            // For demo, we'll use the nodeType directly as ResourceTypeId index
            ecb.AddComponent(entity, new ResourceDeposit
            {
                ResourceTypeId = nodeType, // 0=wood, 1=stone, 2=ore
                CurrentAmount = richness,
                MaxAmount = richness,
                RegenPerSecond = 0f // No regeneration for demo
            });

            // Add rewind support
            ecb.AddComponent<RewindableTag>(entity);
        }
    }
}

