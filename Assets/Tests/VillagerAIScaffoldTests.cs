using NUnit.Framework;
using PureDOTS.Config;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villagers;
using Unity.Entities;
using Unity.Entities.Tests;

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
    }
}

