using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Resource
{
    /// <summary>
    /// API helpers for storehouse deposit/withdraw operations.
    /// Used by job execution systems for conservation checks.
    /// </summary>
    public static class StorehouseApi
    {
        /// <summary>
        /// Attempts to deposit resources into a storehouse.
        /// Returns true if deposit was successful.
        /// </summary>
        public static bool TryDeposit(
            Entity storehouseEntity,
            ushort resourceTypeIndex,
            float amount,
            ref StorehouseInventory inventory,
            DynamicBuffer<StorehouseInventoryItem> items,
            out float depositedAmount)
        {
            depositedAmount = 0f;
            
            if (amount <= 0f)
            {
                return false;
            }
            
            // Check capacity
            var capacityRemaining = inventory.TotalCapacity - inventory.TotalStored;
            if (capacityRemaining <= 0f)
            {
                return false;
            }
            
            // Find or create inventory item for this resource type
            int itemIndex = -1;
            for (int i = 0; i < items.Length; i++)
            {
                // Note: StorehouseInventoryItem uses ResourceTypeId (string), not index
                // This is a simplified version - full implementation would map index to ID
                if (items[i].ResourceTypeId.Length == 0) // Empty slot
                {
                    itemIndex = i;
                    break;
                }
            }
            
            // Deposit amount (limited by capacity)
            depositedAmount = math.min(amount, capacityRemaining);
            inventory.TotalStored += depositedAmount;
            
            if (itemIndex >= 0 && itemIndex < items.Length)
            {
                var item = items[itemIndex];
                item.Amount += depositedAmount;
                items[itemIndex] = item;
            }
            else if (items.Length < items.Capacity)
            {
                // Add new item
                items.Add(new StorehouseInventoryItem
                {
                    ResourceTypeId = new Unity.Collections.FixedString64Bytes($"Resource_{resourceTypeIndex}"),
                    Amount = depositedAmount,
                    Reserved = 0f,
                    TierId = 0,
                    AverageQuality = 100
                });
            }
            
            return depositedAmount > 0f;
        }
        
        /// <summary>
        /// Attempts to withdraw resources from a storehouse.
        /// Returns true if withdrawal was successful.
        /// </summary>
        public static bool TryWithdraw(
            Entity storehouseEntity,
            ushort resourceTypeIndex,
            float amount,
            ref StorehouseInventory inventory,
            DynamicBuffer<StorehouseInventoryItem> items,
            out float withdrawnAmount)
        {
            withdrawnAmount = 0f;
            
            if (amount <= 0f)
            {
                return false;
            }
            
            // Find inventory item for this resource type
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                // Simplified: check if item matches (full implementation would map index to ID)
                if (item.Amount > 0f && item.Reserved < item.Amount)
                {
                    var available = item.Amount - item.Reserved;
                    withdrawnAmount = math.min(amount, available);
                    
                    if (withdrawnAmount > 0f)
                    {
                        item.Amount -= withdrawnAmount;
                        items[i] = item;
                        inventory.TotalStored -= withdrawnAmount;
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}

