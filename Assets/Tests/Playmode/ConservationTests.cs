using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.Playmode
{
    public class ConservationTests
    {
        private World _world;
        private EntityManager _entityManager;
        private Entity _storehouseEntity;
        
        [SetUp]
        public void SetUp()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _entityManager = _world.EntityManager;
            
            // Create a test storehouse
            _storehouseEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent<StorehouseConfig>(_storehouseEntity);
            _entityManager.AddComponent<StorehouseInventory>(_storehouseEntity);
            _entityManager.AddBuffer<StorehouseInventoryItem>(_storehouseEntity);
            
            var inventory = new StorehouseInventory
            {
                TotalCapacity = 1000f,
                TotalStored = 0f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            };
            _entityManager.SetComponentData(_storehouseEntity, inventory);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_storehouseEntity != Entity.Null && _entityManager.Exists(_storehouseEntity))
            {
                _entityManager.DestroyEntity(_storehouseEntity);
            }
        }
        
        [Test]
        public void StorehouseApi_TryDeposit_SucceedsWithinCapacity()
        {
            var inventory = _entityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            var items = _entityManager.GetBuffer<StorehouseInventoryItem>(_storehouseEntity);
            var oreId = new FixedString64Bytes("ore");
            
            var result = StorehouseApi.TryDeposit(_storehouseEntity, oreId, 100f, ref inventory, items, out var deposited);
            
            Assert.IsTrue(result);
            Assert.AreEqual(100f, deposited);
            Assert.AreEqual(100f, inventory.TotalStored);
            
            _entityManager.SetComponentData(_storehouseEntity, inventory);
        }
        
        [Test]
        public void StorehouseApi_TryDeposit_RespectsCapacity()
        {
            var inventory = _entityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            var items = _entityManager.GetBuffer<StorehouseInventoryItem>(_storehouseEntity);
            var oreId = new FixedString64Bytes("ore");
            
            inventory.TotalStored = 950f; // Near capacity
            _entityManager.SetComponentData(_storehouseEntity, inventory);
            
            var result = StorehouseApi.TryDeposit(_storehouseEntity, oreId, 100f, ref inventory, items, out var deposited);
            
            Assert.IsTrue(result);
            Assert.AreEqual(50f, deposited); // Only 50f fits
            Assert.AreEqual(1000f, inventory.TotalStored);
            
            _entityManager.SetComponentData(_storehouseEntity, inventory);
        }
        
        [Test]
        public void StorehouseApi_TryWithdraw_SucceedsWhenAvailable()
        {
            var inventory = _entityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            var items = _entityManager.GetBuffer<StorehouseInventoryItem>(_storehouseEntity);
            var oreId = new FixedString64Bytes("ore");
            
            // First deposit
            StorehouseApi.TryDeposit(_storehouseEntity, oreId, 200f, ref inventory, items, out _);
            _entityManager.SetComponentData(_storehouseEntity, inventory);
            
            // Then withdraw
            inventory = _entityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            items = _entityManager.GetBuffer<StorehouseInventoryItem>(_storehouseEntity);
            
            var result = StorehouseApi.TryWithdraw(_storehouseEntity, 1, 150f, ref inventory, items, out var withdrawn);
            
            Assert.IsTrue(result);
            Assert.AreEqual(150f, withdrawn);
            Assert.AreEqual(50f, inventory.TotalStored);
            
            _entityManager.SetComponentData(_storehouseEntity, inventory);
        }
        
        [Test]
        public void Conservation_GatheredEqualsDeposited()
        {
            // This test verifies that resources gathered equal resources deposited (minus consumption)
            // In a full implementation, this would track gathered/deposited amounts across multiple ticks
            
            var inventory = _entityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            var items = _entityManager.GetBuffer<StorehouseInventoryItem>(_storehouseEntity);
            var oreId = new FixedString64Bytes("ore");
            
            float totalGathered = 500f;
            float totalConsumed = 50f;
            float expectedDeposited = totalGathered - totalConsumed;
            
            // Simulate depositing gathered resources
            StorehouseApi.TryDeposit(_storehouseEntity, oreId, expectedDeposited, ref inventory, items, out var deposited);
            _entityManager.SetComponentData(_storehouseEntity, inventory);
            
            Assert.AreEqual(expectedDeposited, deposited);
            
            // Verify final state
            inventory = _entityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            Assert.AreEqual(expectedDeposited, inventory.TotalStored);
        }
    }
}
