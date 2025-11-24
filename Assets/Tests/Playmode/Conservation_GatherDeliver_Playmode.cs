using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Tests to verify resource conservation: sum resources in storehouses == gathered - consumed.
    /// </summary>
    public class Conservation_GatherDeliver_Playmode : EcsTestFixture
    {
        [Test]
        public void Conservation_GatheredEqualsDeposited_MinusConsumed()
        {
            // Arrange: Create storehouse
            var storehouse = EntityManager.CreateEntity();
            EntityManager.AddComponentData(storehouse, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(storehouse, new StorehouseConfig());
            EntityManager.AddComponentData(storehouse, new StorehouseInventory
            {
                TotalCapacity = 10000f,
                TotalStored = 0f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });
            EntityManager.AddBuffer<StorehouseInventoryItem>(storehouse);
            
            var inventory = EntityManager.GetComponentData<StorehouseInventory>(storehouse);
            var items = EntityManager.GetBuffer<StorehouseInventoryItem>(storehouse);
            
            // Simulate gathering and consumption over multiple cycles
            float totalGathered = 0f;
            float totalConsumed = 0f;
            float totalDeposited = 0f;
            
            // Cycle 1: Gather 500, consume 50, deposit 450
            totalGathered += 500f;
            totalConsumed += 50f;
            float deposit1 = totalGathered - totalConsumed - totalDeposited;
            StorehouseApi.TryDeposit(storehouse, 1, deposit1, ref inventory, items, out var deposited1);
            totalDeposited += deposited1;
            EntityManager.SetComponentData(storehouse, inventory);
            
            // Cycle 2: Gather 300, consume 30, deposit 270
            totalGathered += 300f;
            totalConsumed += 30f;
            float deposit2 = totalGathered - totalConsumed - totalDeposited;
            inventory = EntityManager.GetComponentData<StorehouseInventory>(storehouse);
            items = EntityManager.GetBuffer<StorehouseInventoryItem>(storehouse);
            StorehouseApi.TryDeposit(storehouse, 1, deposit2, ref inventory, items, out var deposited2);
            totalDeposited += deposited2;
            EntityManager.SetComponentData(storehouse, inventory);
            
            // Assert: Total stored should equal gathered - consumed
            inventory = EntityManager.GetComponentData<StorehouseInventory>(storehouse);
            float expectedStored = totalGathered - totalConsumed;
            Assert.AreEqual(expectedStored, inventory.TotalStored, 0.01f, 
                $"Total stored ({inventory.TotalStored}) should equal gathered ({totalGathered}) - consumed ({totalConsumed})");
        }
        
        [Test]
        public void Conservation_MultipleStorehouses_SumEqualsGatheredMinusConsumed()
        {
            // Arrange: Create multiple storehouses
            var storehouse1 = CreateStorehouse();
            var storehouse2 = CreateStorehouse();
            var storehouse3 = CreateStorehouse();
            
            float totalGathered = 1000f;
            float totalConsumed = 100f;
            float expectedStored = totalGathered - totalConsumed;
            
            // Distribute deposits across storehouses
            var inv1 = EntityManager.GetComponentData<StorehouseInventory>(storehouse1);
            var items1 = EntityManager.GetBuffer<StorehouseInventoryItem>(storehouse1);
            StorehouseApi.TryDeposit(storehouse1, 1, expectedStored * 0.4f, ref inv1, items1, out _);
            EntityManager.SetComponentData(storehouse1, inv1);
            
            var inv2 = EntityManager.GetComponentData<StorehouseInventory>(storehouse2);
            var items2 = EntityManager.GetBuffer<StorehouseInventoryItem>(storehouse2);
            StorehouseApi.TryDeposit(storehouse2, 1, expectedStored * 0.35f, ref inv2, items2, out _);
            EntityManager.SetComponentData(storehouse2, inv2);
            
            var inv3 = EntityManager.GetComponentData<StorehouseInventory>(storehouse3);
            var items3 = EntityManager.GetBuffer<StorehouseInventoryItem>(storehouse3);
            StorehouseApi.TryDeposit(storehouse3, 1, expectedStored * 0.25f, ref inv3, items3, out _);
            EntityManager.SetComponentData(storehouse3, inv3);
            
            // Assert: Sum of all storehouses equals expected
            inv1 = EntityManager.GetComponentData<StorehouseInventory>(storehouse1);
            inv2 = EntityManager.GetComponentData<StorehouseInventory>(storehouse2);
            inv3 = EntityManager.GetComponentData<StorehouseInventory>(storehouse3);
            
            float totalStored = inv1.TotalStored + inv2.TotalStored + inv3.TotalStored;
            Assert.AreEqual(expectedStored, totalStored, 0.01f, 
                $"Sum of storehouses ({totalStored}) should equal gathered ({totalGathered}) - consumed ({totalConsumed})");
        }
        
        private Entity CreateStorehouse()
        {
            var storehouse = EntityManager.CreateEntity();
            EntityManager.AddComponentData(storehouse, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(storehouse, new StorehouseConfig());
            EntityManager.AddComponentData(storehouse, new StorehouseInventory
            {
                TotalCapacity = 10000f,
                TotalStored = 0f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });
            EntityManager.AddBuffer<StorehouseInventoryItem>(storehouse);
            return storehouse;
        }
    }
}

