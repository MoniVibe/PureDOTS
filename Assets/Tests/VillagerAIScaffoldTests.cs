using NUnit.Framework;
using PureDOTS.Config;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Tests for PureDOTS villager AI scaffolding.
    /// Verifies that AI components and systems compile and can be instantiated.
    /// </summary>
    public class VillagerAIScaffoldTests : ECSTestsFixture
    {
        [Test]
        public void VillagerArchetypeCatalog_CanBeCreated()
        {
            // Verify archetype catalog blob structure can be created
            using var builder = new Unity.Collections.BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var catalog = ref builder.ConstructRoot<VillagerArchetypeCatalogBlob>();
            var archetypesArray = builder.Allocate(ref catalog.Archetypes, 1);
            
            archetypesArray[0] = new VillagerArchetypeData
            {
                ArchetypeName = new Unity.Collections.FixedString64Bytes("TestArchetype"),
                BasePhysique = 50,
                BaseFinesse = 50,
                BaseWillpower = 50,
                HungerDecayRate = 0.01f,
                EnergyDecayRate = 0.02f,
                MoraleDecayRate = 0.005f,
                GatherJobWeight = 50,
                BuildJobWeight = 50,
                CraftJobWeight = 50,
                CombatJobWeight = 30,
                TradeJobWeight = 40,
                MoralAxisLean = 0,
                OrderAxisLean = 0,
                PurityAxisLean = 0,
                BaseLoyalty = 50
            };
            
            var blobAsset = builder.CreateBlobAssetReference<VillagerArchetypeCatalogBlob>(Unity.Collections.Allocator.Persistent);
            builder.Dispose();
            
            Assert.IsTrue(blobAsset.IsCreated, "Archetype catalog blob should be created");
            Assert.AreEqual(1, blobAsset.Value.Archetypes.Length, "Should have one archetype");
            
            blobAsset.Dispose();
        }
        
        [Test]
        public void VillagerUtilityScheduler_MethodsExist()
        {
            // Verify utility scheduler methods can be called (even if stubs)
            float needUtility = VillagerUtilityScheduler.CalculateNeedUtility(0.5f, 0.01f, 0.3f);
            float jobUtility = VillagerUtilityScheduler.CalculateJobUtility(50, 10f, 0.8f);
            
            // Methods should exist and not crash (stub implementations return 0)
            Assert.IsNotNull(VillagerUtilityScheduler.CalculateNeedUtility);
            Assert.IsNotNull(VillagerUtilityScheduler.CalculateJobUtility);
        }
        
        [Test]
        public void VillagerJobExecutionInterface_BehaviorsExist()
        {
            // Verify job behavior interfaces can be instantiated
            var gatherBehavior = new GatherJobBehavior();
            var buildBehavior = new BuildJobBehavior();
            var craftBehavior = new CraftJobBehavior();
            var combatBehavior = new CombatJobBehavior();
            
            Assert.IsNotNull(gatherBehavior);
            Assert.IsNotNull(buildBehavior);
            Assert.IsNotNull(craftBehavior);
            Assert.IsNotNull(combatBehavior);
            
            // Behaviors should implement IVillagerJobBehavior
            Assert.IsTrue(gatherBehavior is IVillagerJobBehavior);
            Assert.IsTrue(buildBehavior is IVillagerJobBehavior);
            Assert.IsTrue(craftBehavior is IVillagerJobBehavior);
            Assert.IsTrue(combatBehavior is IVillagerJobBehavior);
        }

        [Test]
        public void VillagerAI_SelectsNeedGoalWhenHungry()
        {
            var config = VillagerBehaviorConfig.CreateDefaults();
            var job = CreateAIJob(config);

            var entity = EntityManager.CreateEntity(typeof(LocalTransform));
            var aiState = new VillagerAIState();
            var needs = new VillagerNeeds
            {
                Health = 100f,
                MaxHealth = 100f
            };
            needs.SetHunger(0.95f);
            needs.SetEnergy(0.8f);

            var villagerJob = new VillagerJob { Type = VillagerJob.JobType.None, Phase = VillagerJob.JobPhase.Idle };
            var ticket = default(VillagerJobTicket);
            var flags = default(VillagerFlags);
            var transform = LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f);
            var resolved = new VillagerArchetypeResolved
            {
                ArchetypeIndex = 0,
                Data = CreateFallbackArchetype(config)
            };

            job.Execute(entity, ref aiState, ref needs, villagerJob, ticket, flags, transform, resolved);

            Assert.AreEqual(VillagerAIState.Goal.SurviveHunger, aiState.CurrentGoal);
        }

        [Test]
        public void VillagerAI_SelectsWorkWhenJobUtilityHigher()
        {
            var config = VillagerBehaviorConfig.CreateDefaults();
            var job = CreateAIJob(config);

            var villager = EntityManager.CreateEntity(typeof(LocalTransform));
            var resource = EntityManager.CreateEntity(typeof(LocalTransform));
            EntityManager.SetComponentData(resource, LocalTransform.FromPositionRotationScale(new float3(10f, 0f, 0f), quaternion.identity, 1f));

            var aiState = new VillagerAIState();
            var needs = new VillagerNeeds
            {
                Health = 100f,
                MaxHealth = 100f
            };
            needs.SetHunger(0.1f);
            needs.SetEnergy(0.9f);

            var villagerJob = new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Gathering
            };
            var ticket = new VillagerJobTicket
            {
                ResourceEntity = resource,
                Phase = (byte)VillagerJob.JobPhase.Gathering
            };
            var flags = default(VillagerFlags);
            var transform = LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f);
            var resolved = new VillagerArchetypeResolved
            {
                ArchetypeIndex = 0,
                Data = CreateFallbackArchetype(config)
            };

            job.Execute(villager, ref aiState, ref needs, villagerJob, ticket, flags, transform, resolved);

            Assert.AreEqual(VillagerAIState.Goal.Work, aiState.CurrentGoal);
        }

        private static VillagerAISystem.EvaluateVillagerAIJob CreateAIJob(VillagerBehaviorConfig config)
        {
            return new VillagerAISystem.EvaluateVillagerAIJob
            {
                DeltaTime = 1f,
                CurrentTick = 1,
                BehaviorConfig = config,
                BehaviorLookup = default,
                TransformLookup = default
            };
        }

        private static VillagerArchetypeData CreateFallbackArchetype(VillagerBehaviorConfig config)
        {
            return new VillagerArchetypeData
            {
                ArchetypeName = new Unity.Collections.FixedString64Bytes("test"),
                BasePhysique = 50,
                BaseFinesse = 50,
                BaseWillpower = 50,
                HungerDecayRate = math.max(config.HungerIncreaseRate * 0.01f, 0.01f),
                EnergyDecayRate = math.max(config.EnergyDecreaseRate * 0.01f, 0.01f),
                MoraleDecayRate = 0.02f,
                GatherJobWeight = 60,
                BuildJobWeight = 50,
                CraftJobWeight = 40,
                CombatJobWeight = 35,
                TradeJobWeight = 30,
                MoralAxisLean = 0,
                OrderAxisLean = 0,
                PurityAxisLean = 0,
                BaseLoyalty = 50
            };
        }

        [Test]
        public void VillagerArchetypeResolution_AppliesModifiers()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var catalogBlob = ref builder.ConstructRoot<VillagerArchetypeCatalogBlob>();
            var archetypes = builder.Allocate(ref catalogBlob.Archetypes, 1);
            archetypes[0] = new VillagerArchetypeData
            {
                ArchetypeName = new Unity.Collections.FixedString64Bytes("miner"),
                GatherJobWeight = 40,
                BuildJobWeight = 20,
                CraftJobWeight = 10,
                CombatJobWeight = 5,
                TradeJobWeight = 15,
                HungerDecayRate = 0.05f,
                EnergyDecayRate = 0.03f,
                MoraleDecayRate = 0.02f,
                BasePhysique = 40,
                BaseFinesse = 30,
                BaseWillpower = 20,
                BaseLoyalty = 40
            };
            var blobRef = builder.CreateBlobAssetReference<VillagerArchetypeCatalogBlob>(Allocator.Persistent);
            builder.Dispose();

            var catalogEntity = EntityManager.CreateEntity(typeof(VillagerArchetypeCatalogComponent));
            EntityManager.SetComponentData(catalogEntity, new VillagerArchetypeCatalogComponent { Catalog = blobRef });

            var villager = EntityManager.CreateEntity(typeof(VillagerArchetypeResolved));
            EntityManager.AddComponentData(villager, new VillagerArchetypeAssignment
            {
                ArchetypeName = new Unity.Collections.FixedString64Bytes("miner"),
                CachedIndex = -1
            });
            var modifiers = EntityManager.AddBuffer<VillagerArchetypeModifier>(villager);
            modifiers.Add(new VillagerArchetypeModifier
            {
                Source = VillagerArchetypeModifierSource.Family,
                GatherJobDelta = 15,
                HungerDecayMultiplier = 0.9f
            });
            modifiers.Add(new VillagerArchetypeModifier
            {
                Source = VillagerArchetypeModifierSource.Ambient,
                GatherJobDelta = -5,
                BuildJobDelta = 5,
                EnergyDecayMultiplier = 1.2f
            });

            var systemHandle = World.GetOrCreateSystem<VillagerArchetypeResolutionSystem>();
            systemHandle.Update(World.Unmanaged);

            var resolved = EntityManager.GetComponentData<VillagerArchetypeResolved>(villager);
            Assert.AreEqual(0, resolved.ArchetypeIndex);
            Assert.AreEqual(50, resolved.Data.GatherJobWeight);
            Assert.AreEqual(25, resolved.Data.BuildJobWeight);
            Assert.AreEqual(0.045f, resolved.Data.HungerDecayRate, 1e-4f);
            Assert.AreEqual(0.036f, resolved.Data.EnergyDecayRate, 1e-4f);

            blobRef.Dispose();
        }

        [Test]
        public void VillagerBelongingModifiers_ApplyTopRankedBelongings()
        {
            var familyEntity = EntityManager.CreateEntity();
            var familyProfiles = EntityManager.AddBuffer<VillagerAggregateModifierProfile>(familyEntity);
            familyProfiles.Add(new VillagerAggregateModifierProfile
            {
                LoyaltyThreshold = 150,
                Modifier = new VillagerArchetypeModifier
                {
                    Source = VillagerArchetypeModifierSource.Family,
                    GatherJobDelta = 20,
                    HungerDecayMultiplier = 0.95f
                }
            });

            var villageEntity = EntityManager.CreateEntity();
            var villageProfiles = EntityManager.AddBuffer<VillagerAggregateModifierProfile>(villageEntity);
            villageProfiles.Add(new VillagerAggregateModifierProfile
            {
                LoyaltyThreshold = 100,
                Modifier = new VillagerArchetypeModifier
                {
                    Source = VillagerArchetypeModifierSource.Village,
                    BuildJobDelta = 10,
                    EnergyDecayMultiplier = 1.1f
                }
            });

            var villager = EntityManager.CreateEntity(typeof(VillagerBelonging), typeof(VillagerArchetypeModifier));
            var belongings = EntityManager.GetBuffer<VillagerBelonging>(villager);
            belongings.Add(new VillagerBelonging
            {
                AggregateEntity = familyEntity,
                Kind = VillagerAggregateKind.Family,
                Loyalty = 180
            });
            belongings.Add(new VillagerBelonging
            {
                AggregateEntity = villageEntity,
                Kind = VillagerAggregateKind.Village,
                Loyalty = 120
            });

            var system = World.GetOrCreateSystem<VillagerBelongingModifierSystem>();
            system.Update(World.Unmanaged);

            var modifiers = EntityManager.GetBuffer<VillagerArchetypeModifier>(villager);
            Assert.AreEqual(2, modifiers.Length);
            Assert.Greater(modifiers[0].GatherJobDelta, modifiers[1].GatherJobDelta);
        }
    }
}
