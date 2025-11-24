using NUnit.Framework;
using PureDOTS.Runtime.Villagers;
using Unity.Entities;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Tests to verify deterministic tie-breaking with same seed produces same results.
    /// </summary>
    public class Offers_FairTieBreak_Deterministic : EcsTestFixture
    {
        [Test]
        public void TieBreak_WithSameSeed_ProducesSameResult()
        {
            // Arrange: Same inputs
            uint baseSeed = 12345;
            uint villagerId = 100;
            uint tick = 50;
            
            // Act: Call TieBreak multiple times with same inputs
            var result1 = VillagerSeed.TieBreak(baseSeed, villagerId, tick);
            var result2 = VillagerSeed.TieBreak(baseSeed, villagerId, tick);
            var result3 = VillagerSeed.TieBreak(baseSeed, villagerId, tick);
            
            // Assert: All results should be identical (deterministic)
            Assert.AreEqual(result1, result2, "TieBreak should be deterministic - same inputs produce same output");
            Assert.AreEqual(result2, result3, "TieBreak should be deterministic - same inputs produce same output");
        }
        
        [Test]
        public void TieBreak_WithDifferentVillagerId_ProducesDifferentResult()
        {
            // Arrange: Same seed and tick, different villager IDs
            uint baseSeed = 12345;
            uint tick = 50;
            uint villagerId1 = 100;
            uint villagerId2 = 200;
            
            // Act: Call TieBreak with different villager IDs
            var result1 = VillagerSeed.TieBreak(baseSeed, villagerId1, tick);
            var result2 = VillagerSeed.TieBreak(baseSeed, villagerId2, tick);
            
            // Assert: Results should be different (fair distribution)
            Assert.AreNotEqual(result1, result2, "Different villager IDs should produce different tie-break values");
        }
        
        [Test]
        public void TieBreak_WithDifferentTick_ProducesDifferentResult()
        {
            // Arrange: Same seed and villager ID, different ticks
            uint baseSeed = 12345;
            uint villagerId = 100;
            uint tick1 = 50;
            uint tick2 = 51;
            
            // Act: Call TieBreak with different ticks
            var result1 = VillagerSeed.TieBreak(baseSeed, villagerId, tick1);
            var result2 = VillagerSeed.TieBreak(baseSeed, villagerId, tick2);
            
            // Assert: Results should be different
            Assert.AreNotEqual(result1, result2, "Different ticks should produce different tie-break values");
        }
        
        [Test]
        public void TieBreak_WithDifferentSeed_ProducesDifferentResult()
        {
            // Arrange: Same villager ID and tick, different seeds
            uint baseSeed1 = 12345;
            uint baseSeed2 = 54321;
            uint villagerId = 100;
            uint tick = 50;
            
            // Act: Call TieBreak with different seeds
            var result1 = VillagerSeed.TieBreak(baseSeed1, villagerId, tick);
            var result2 = VillagerSeed.TieBreak(baseSeed2, villagerId, tick);
            
            // Assert: Results should be different
            Assert.AreNotEqual(result1, result2, "Different seeds should produce different tie-break values");
        }
        
        [Test]
        public void TieBreak_DistributesFairly_AcrossMultipleVillagers()
        {
            // Arrange: Multiple villagers with same utility (same seed, same tick)
            uint baseSeed = 12345;
            uint tick = 50;
            var results = new System.Collections.Generic.HashSet<uint>();
            
            // Act: Generate tie-break values for 100 different villagers
            for (uint villagerId = 0; villagerId < 100; villagerId++)
            {
                var result = VillagerSeed.TieBreak(baseSeed, villagerId, tick);
                results.Add(result);
            }
            
            // Assert: Should have good distribution (most values should be unique)
            // With 100 villagers, we expect at least 90 unique values (90% uniqueness)
            Assert.GreaterOrEqual(results.Count, 90, "TieBreak should distribute fairly across multiple villagers");
        }
    }
}

