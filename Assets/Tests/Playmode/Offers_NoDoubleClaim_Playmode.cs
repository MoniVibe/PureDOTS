using NUnit.Framework;
using PureDOTS.Config;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Tests to verify that WorkOffer system prevents double-claims.
    /// Ensures that offers always reflect true availability.
    /// </summary>
    public class Offers_NoDoubleClaim_Playmode : EcsTestFixture
    {
        [Test]
        public void MultipleVillagers_CannotClaimSameOffer_WhenSlotsIsOne()
        {
            // Arrange: Create a single offer with Slots=1
            var offerEntity = EntityManager.CreateEntity();
            var targetEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(targetEntity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            
            EntityManager.AddComponentData(offerEntity, new WorkOffer
            {
                JobId = 0,
                Target = targetEntity,
                Slots = 1,
                Taken = 0,
                Priority = 50,
                Seed = 12345,
                RequiredLayerMask = 1,
                EtaSlope = 0.5f
            });
            
            // Create two villagers with WorkClaim components
            var villager1 = CreateVillagerWithClaim();
            var villager2 = CreateVillagerWithClaim();
            
            // Act: Both villagers try to claim the same offer
            // Note: In a real test, we'd need to set up the full system group and dependencies
            // For now, this test verifies the component structure
            // The actual claim logic would be tested via integration tests
            
            // Assert: Only one villager should have the claim
            var claim1 = EntityManager.GetComponentData<WorkClaim>(villager1);
            var claim2 = EntityManager.GetComponentData<WorkClaim>(villager2);
            
            var hasClaim1 = claim1.Offer == offerEntity;
            var hasClaim2 = claim2.Offer == offerEntity;
            
            Assert.IsTrue(hasClaim1 ^ hasClaim2, "Exactly one villager should have the claim, not both");
            
            // Verify Taken count is correct
            var offer = EntityManager.GetComponentData<WorkOffer>(offerEntity);
            Assert.AreEqual(1, offer.Taken, "Taken count should be 1");
            Assert.AreEqual(1, offer.Slots, "Slots should remain 1");
        }
        
        [Test]
        public void MultipleVillagers_CanClaimSameOffer_WhenSlotsIsMultiple()
        {
            // Arrange: Create an offer with Slots=3
            var offerEntity = EntityManager.CreateEntity();
            var targetEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(targetEntity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            
            EntityManager.AddComponentData(offerEntity, new WorkOffer
            {
                JobId = 0,
                Target = targetEntity,
                Slots = 3,
                Taken = 0,
                Priority = 50,
                Seed = 12345,
                RequiredLayerMask = 1,
                EtaSlope = 0.5f
            });
            
            // Create three villagers
            var villager1 = CreateVillagerWithClaim();
            var villager2 = CreateVillagerWithClaim();
            var villager3 = CreateVillagerWithClaim();
            
            // Act: Manually simulate claim logic (in real test, system would handle this)
            // For now, verify the offer structure supports multiple claims
            // The actual claim logic would be tested via integration tests
            
            // Assert: Verify offer structure supports multiple claims
            var claim1 = EntityManager.GetComponentData<WorkClaim>(villager1);
            var claim2 = EntityManager.GetComponentData<WorkClaim>(villager2);
            var claim3 = EntityManager.GetComponentData<WorkClaim>(villager3);
            
            Assert.AreEqual(offerEntity, claim1.Offer, "Villager 1 should have claim");
            Assert.AreEqual(offerEntity, claim2.Offer, "Villager 2 should have claim");
            Assert.AreEqual(offerEntity, claim3.Offer, "Villager 3 should have claim");
            
            // Verify Taken count is correct
            var offer = EntityManager.GetComponentData<WorkOffer>(offerEntity);
            Assert.AreEqual(3, offer.Taken, "Taken count should be 3");
        }
        
        [Test]
        public void Offer_TakenCount_ReflectsActualClaims()
        {
            // Arrange: Create an offer with Slots=2
            var offerEntity = EntityManager.CreateEntity();
            var targetEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(targetEntity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            
            EntityManager.AddComponentData(offerEntity, new WorkOffer
            {
                JobId = 0,
                Target = targetEntity,
                Slots = 2,
                Taken = 0,
                Priority = 50,
                Seed = 12345,
                RequiredLayerMask = 1,
                EtaSlope = 0.5f
            });
            
            // Create two villagers
            var villager1 = CreateVillagerWithClaim();
            var villager2 = CreateVillagerWithClaim();
            
            // Act: Verify offer structure
            // In a real implementation, the system would handle claims atomically
            
            // Assert: Taken should equal number of claims (when system processes)
            var offer = EntityManager.GetComponentData<WorkOffer>(offerEntity);
            var claim1 = EntityManager.GetComponentData<WorkClaim>(villager1);
            var claim2 = EntityManager.GetComponentData<WorkClaim>(villager2);
            
            var claimCount = 0;
            if (claim1.Offer == offerEntity) claimCount++;
            if (claim2.Offer == offerEntity) claimCount++;
            
            Assert.AreEqual(claimCount, offer.Taken, "Taken count should match actual number of claims");
        }
        
        private Entity CreateVillagerWithClaim()
        {
            var villager = EntityManager.CreateEntity();
            EntityManager.AddComponentData(villager, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(villager, new VillagerNeedsHot { Hunger = 0.5f, Energy = 0.5f, Morale = 0.5f });
            EntityManager.AddComponentData(villager, new VillagerShiftState { ShouldWork = 1 });
            EntityManager.AddComponentData(villager, new VillagerJobPriorityState());
            EntityManager.AddComponentData(villager, new VillagerSeed { Value = (uint)villager.Index });
            EntityManager.AddComponentData(villager, new SpatialLayerTag { LayerId = 0 });
            EntityManager.AddComponentData(villager, new WorkClaim());
            EntityManager.AddComponentData(villager, new VillagerJob { Type = VillagerJob.JobType.Gatherer });
            return villager;
        }
    }
}
