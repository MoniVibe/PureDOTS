using Godgame.Registry;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Handles villager movement toward AI target entities.
    /// Pure DOTS system - no MonoBehaviour dependencies.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct VillagerMovementSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        }

        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            
            // Check if we have any villagers to process
            var villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<VillagerId, VillagerAIState, LocalTransform>()
                .Build();
            
            if (villagerQuery.IsEmptyIgnoreFilter)
            {
                return; // No villagers found
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            const float movementSpeed = 3f; // units per second
            const float arrivalDistance = 0.5f; // stop when this close to target

            foreach (var (transform, aiState, entity) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<VillagerAIState>>()
                         .WithAll<VillagerId>()
                         .WithEntityAccess())
            {
                var currentPos = transform.ValueRO.Position;
                var targetEntity = aiState.ValueRO.TargetEntity;

                // Only move if we have a valid target and are in a working state
                if (targetEntity == Entity.Null || 
                    aiState.ValueRO.CurrentState != VillagerAIState.State.Working)
                {
                    continue;
                }

                // Check if target has a transform
                if (!_transformLookup.HasComponent(targetEntity))
                {
                    continue;
                }

                var targetTransform = _transformLookup[targetEntity];
                var targetPos = targetTransform.Position;
                var distance = math.distance(currentPos, targetPos);

                // If we're close enough, stop moving
                if (distance <= arrivalDistance)
                {
                    continue;
                }

                // Move toward target
                var direction = math.normalize(targetPos - currentPos);
                var movement = direction * movementSpeed * deltaTime;
                var newPos = currentPos + movement;

                // Update transform
                transform.ValueRW.Position = newPos;

                // Face movement direction
                if (math.lengthsq(direction) > 0.0001f)
                {
                    var rotation = quaternion.LookRotationSafe(direction, math.up());
                    transform.ValueRW.Rotation = rotation;
                }
            }
        }
    }

    /// <summary>
    /// Handles villager mining loop: assign mining targets, mine resources, deposit at storehouse.
    /// Pure DOTS system - no MonoBehaviour dependencies.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(VillagerMovementSystem))]
    public partial struct VillagerMiningSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<GodgameResourceNode> _resourceNodeLookup;
        private ComponentLookup<GodgameStorehouse> _storehouseLookup;
        private ComponentLookup<StorehouseInventory> _storehouseInventoryLookup;
        private ComponentLookup<VillagerDisciplineState> _disciplineLookup;
        private BufferLookup<StorehouseInventoryItem> _storehouseInventoryItemsLookup;
        private BufferLookup<StorehouseCapacityElement> _storehouseCapacityLookup;

        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            _resourceNodeLookup = state.GetComponentLookup<GodgameResourceNode>(isReadOnly: false);
            _storehouseLookup = state.GetComponentLookup<GodgameStorehouse>(isReadOnly: true);
            _storehouseInventoryLookup = state.GetComponentLookup<StorehouseInventory>(isReadOnly: false);
            _disciplineLookup = state.GetComponentLookup<VillagerDisciplineState>(isReadOnly: true);
            _storehouseInventoryItemsLookup = state.GetBufferLookup<StorehouseInventoryItem>(isReadOnly: false);
            _storehouseCapacityLookup = state.GetBufferLookup<StorehouseCapacityElement>(isReadOnly: true);
        }

        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _resourceNodeLookup.Update(ref state);
            _storehouseLookup.Update(ref state);
            _storehouseInventoryLookup.Update(ref state);
            _disciplineLookup.Update(ref state);
            _storehouseInventoryItemsLookup.Update(ref state);
            _storehouseCapacityLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            const float miningDistance = 1.5f; // how close to be to mine
            const float depositDistance = 2f; // how close to be to deposit
            const float harvestRate = 10f; // resource units per second
            const float maxCarryCapacity = 50f; // max resources a villager can carry

            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (villagerId, job, aiState, transform, entity) in SystemAPI
                         .Query<RefRO<VillagerId>, RefRW<VillagerJob>, RefRW<VillagerAIState>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var currentPos = transform.ValueRO.Position;
                var currentPhase = job.ValueRO.Phase;
                var currentGoal = aiState.ValueRO.CurrentGoal;
                var hasCarrying = state.EntityManager.HasComponent<VillagerCarrying>(entity);
                var carrying = hasCarrying ? state.EntityManager.GetComponentData<VillagerCarrying>(entity) : default;

                // Only process gatherers/miners
                if (job.ValueRO.Type != VillagerJob.JobType.Gatherer)
                {
                    continue;
                }

                // Determine preferred resource type based on discipline
                ushort preferredResourceType = 0; // 0 = any
                if (_disciplineLookup.HasComponent(entity))
                {
                    var discipline = _disciplineLookup[entity];
                    // Forester prefers Wood (1), Miner prefers Ore (2)
                    if (discipline.Value == VillagerDisciplineType.Forester)
                    {
                        preferredResourceType = 1; // Wood
                    }
                    else if (discipline.Value == VillagerDisciplineType.Miner)
                    {
                        preferredResourceType = 2; // Ore
                    }
                }

                // Assign resource node target if idle and no target
                if (currentPhase == VillagerJob.JobPhase.Idle && 
                    aiState.ValueRO.TargetEntity == Entity.Null &&
                    !hasCarrying)
                {
                    // Auto-assign Work goal for gatherers if not set
                    if (currentGoal == VillagerAIState.Goal.None)
                    {
                        aiState.ValueRW.CurrentGoal = VillagerAIState.Goal.Work;
                    }

                    // Find a resource node if goal is Work
                    if (aiState.ValueRO.CurrentGoal == VillagerAIState.Goal.Work)
                    {
                        Entity? resourceNode = FindResourceNode(ref state, currentPos, preferredResourceType);
                        if (resourceNode.HasValue)
                        {
                            aiState.ValueRW.TargetEntity = resourceNode.Value;
                            aiState.ValueRW.CurrentState = VillagerAIState.State.Working;
                            job.ValueRW.Phase = VillagerJob.JobPhase.Gathering;
                        }
                    }
                }

                // If we have a target, check if we've reached it
                var targetEntity = aiState.ValueRO.TargetEntity;
                if (targetEntity != Entity.Null && _transformLookup.HasComponent(targetEntity))
                {
                    var targetPos = _transformLookup[targetEntity].Position;
                    var distance = math.distance(currentPos, targetPos);

                    // Gathering phase: harvest from resource node
                    if (currentPhase == VillagerJob.JobPhase.Gathering && distance <= miningDistance)
                    {
                        // Check if target is a resource node
                        if (_resourceNodeLookup.HasComponent(targetEntity))
                        {
                            var resourceNode = _resourceNodeLookup[targetEntity];
                            
                            // Ensure villager has carrying component
                            if (!hasCarrying)
                            {
                                carrying = new VillagerCarrying
                                {
                                    ResourceTypeIndex = resourceNode.ResourceTypeIndex,
                                    Amount = 0f,
                                    MaxCarryCapacity = maxCarryCapacity
                                };
                                ecb.AddComponent(entity, carrying);
                                hasCarrying = true;
                            }

                            // Harvest resources
                            var harvestAmount = harvestRate * deltaTime;
                            var availableInNode = resourceNode.RemainingAmount;
                            var spaceInInventory = carrying.MaxCarryCapacity - carrying.Amount;
                            var actualHarvest = math.min(harvestAmount, math.min(availableInNode, spaceInInventory));

                            if (actualHarvest > 0f)
                            {
                                // Update resource node
                                resourceNode.RemainingAmount = math.max(0f, resourceNode.RemainingAmount - actualHarvest);
                                _resourceNodeLookup[targetEntity] = resourceNode;

                                // Update villager carrying
                                carrying.Amount += actualHarvest;
                                state.EntityManager.SetComponentData(entity, carrying);

                                // If inventory full or node depleted, go to storehouse
                                if (carrying.Amount >= carrying.MaxCarryCapacity || resourceNode.RemainingAmount <= 0f)
                                {
                                    Entity? storehouse = FindNearestStorehouse(ref state, currentPos, carrying.ResourceTypeIndex);
                                    if (storehouse.HasValue)
                                    {
                                        aiState.ValueRW.TargetEntity = storehouse.Value;
                                        job.ValueRW.Phase = VillagerJob.JobPhase.Delivering;
                                    }
                                    else
                                    {
                                        // No storehouse found, go idle
                                        aiState.ValueRW.TargetEntity = Entity.Null;
                                        aiState.ValueRW.CurrentState = VillagerAIState.State.Idle;
                                        job.ValueRW.Phase = VillagerJob.JobPhase.Idle;
                                    }
                                }
                            }
                            else if (resourceNode.RemainingAmount <= 0f)
                            {
                                // Node depleted, find new node
                                Entity? newNode = FindResourceNode(ref state, currentPos, preferredResourceType);
                                if (newNode.HasValue)
                                {
                                    aiState.ValueRW.TargetEntity = newNode.Value;
                                }
                                else
                                {
                                    // No nodes available, go idle
                                    aiState.ValueRW.TargetEntity = Entity.Null;
                                    aiState.ValueRW.CurrentState = VillagerAIState.State.Idle;
                                    job.ValueRW.Phase = VillagerJob.JobPhase.Idle;
                                }
                            }
                        }
                    }

                    // Returning phase: deposit at storehouse
                    if (currentPhase == VillagerJob.JobPhase.Delivering && distance <= depositDistance && hasCarrying)
                    {
                        // Check if target is a storehouse with PureDOTS components
                        if (_storehouseInventoryLookup.HasComponent(targetEntity) &&
                            _storehouseInventoryItemsLookup.HasBuffer(targetEntity))
                        {
                            var deposited = DepositToStorehouse(ref state, targetEntity, entity, carrying);
                            
                            if (deposited)
                            {
                                // Remove carrying component
                                ecb.RemoveComponent<VillagerCarrying>(entity);
                                
                                // Go back to gathering
                                job.ValueRW.Phase = VillagerJob.JobPhase.Idle;
                                aiState.ValueRW.TargetEntity = Entity.Null;
                                aiState.ValueRW.CurrentState = VillagerAIState.State.Idle;

                                // Immediately start new gathering cycle if goal is still Work
                                if (aiState.ValueRO.CurrentGoal == VillagerAIState.Goal.Work)
                                {
                                    Entity? newNode = FindResourceNode(ref state, currentPos, preferredResourceType);
                                    if (newNode.HasValue)
                                    {
                                        aiState.ValueRW.TargetEntity = newNode.Value;
                                        aiState.ValueRW.CurrentState = VillagerAIState.State.Working;
                                        job.ValueRW.Phase = VillagerJob.JobPhase.Gathering;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the nearest available resource node matching the preferred resource type.
        /// </summary>
        private Entity? FindResourceNode(ref SystemState state, float3 position, ushort preferredResourceType)
        {
            Entity? nearestNode = null;
            float nearestDistance = float.MaxValue;

            foreach (var (resourceNode, transform, entity) in SystemAPI
                         .Query<RefRO<GodgameResourceNode>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                // Check if node has resources
                if (resourceNode.ValueRO.RemainingAmount <= 0f)
                {
                    continue;
                }

                // Filter by preferred resource type if specified (0 = any)
                if (preferredResourceType != 0 && resourceNode.ValueRO.ResourceTypeIndex != preferredResourceType)
                {
                    continue;
                }

                var distance = math.distance(position, transform.ValueRO.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNode = entity;
                }
            }

            return nearestNode;
        }

        /// <summary>
        /// Finds the nearest storehouse that can accept the specified resource type.
        /// </summary>
        private Entity? FindNearestStorehouse(ref SystemState state, float3 position, ushort resourceTypeIndex)
        {
            Entity? nearest = null;
            float nearestDistance = float.MaxValue;

            // Try to find storehouse that has capacity for this resource type
            foreach (var (storehouse, transform, entity) in SystemAPI
                         .Query<RefRO<GodgameStorehouse>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                // Check if storehouse has capacity for this resource type
                if (!HasCapacityForResource(ref state, entity, resourceTypeIndex))
                {
                    continue;
                }

                var distance = math.distance(position, transform.ValueRO.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = entity;
                }
            }

            // Fallback: find any storehouse if none found with capacity
            if (!nearest.HasValue)
            {
                foreach (var (storehouse, transform, entity) in SystemAPI
                             .Query<RefRO<GodgameStorehouse>, RefRO<LocalTransform>>()
                             .WithEntityAccess())
                {
                    var distance = math.distance(position, transform.ValueRO.Position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = entity;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// Checks if a storehouse has capacity for the specified resource type.
        /// </summary>
        private bool HasCapacityForResource(ref SystemState state, Entity storehouseEntity, ushort resourceTypeIndex)
        {
            if (!_storehouseCapacityLookup.HasBuffer(storehouseEntity))
            {
                return false;
            }

            var capacities = _storehouseCapacityLookup[storehouseEntity];

            // Map resourceTypeIndex to ResourceTypeId string
            FixedString64Bytes resourceTypeId = default;
            if (resourceTypeIndex == 1)
            {
                resourceTypeId = new FixedString64Bytes("wood");
            }
            else if (resourceTypeIndex == 2)
            {
                resourceTypeId = new FixedString64Bytes("ore");
            }

            for (var i = 0; i < capacities.Length; i++)
            {
                if (capacities[i].ResourceTypeId.Equals(resourceTypeId))
                {
                    // Check if there's space (compare with current inventory)
                    if (_storehouseInventoryLookup.HasComponent(storehouseEntity))
                    {
                        var inventory = _storehouseInventoryLookup[storehouseEntity];
                        return inventory.TotalStored < inventory.TotalCapacity;
                    }
                    return true; // Has capacity defined
                }
            }

            return false;
        }

        /// <summary>
        /// Deposits resources from villager carrying component into storehouse inventory.
        /// Returns true if deposit was successful.
        /// </summary>
        private bool DepositToStorehouse(ref SystemState state, Entity storehouseEntity, Entity villagerEntity, VillagerCarrying carrying)
        {
            if (!_storehouseInventoryLookup.HasComponent(storehouseEntity) ||
                !_storehouseInventoryItemsLookup.HasBuffer(storehouseEntity))
            {
                return false;
            }

            var inventory = _storehouseInventoryLookup[storehouseEntity];
            var inventoryItems = _storehouseInventoryItemsLookup[storehouseEntity];

            // Check if storehouse has capacity
            if (inventory.TotalStored >= inventory.TotalCapacity)
            {
                return false;
            }

            // Map resourceTypeIndex to ResourceTypeId string
            FixedString64Bytes resourceTypeId = default;
            if (carrying.ResourceTypeIndex == 1)
            {
                resourceTypeId = new FixedString64Bytes("wood");
            }
            else if (carrying.ResourceTypeIndex == 2)
            {
                resourceTypeId = new FixedString64Bytes("ore");
            }
            else
            {
                return false; // Unknown resource type
            }

            // Find or create inventory item for this resource type
            int itemIndex = -1;
            for (var i = 0; i < inventoryItems.Length; i++)
            {
                if (inventoryItems[i].ResourceTypeId.Equals(resourceTypeId))
                {
                    itemIndex = i;
                    break;
                }
            }

            // Calculate how much we can deposit
            var availableCapacity = inventory.TotalCapacity - inventory.TotalStored;
            var depositAmount = math.min(carrying.Amount, availableCapacity);

            if (depositAmount <= 0f)
            {
                return false;
            }

            // Update inventory item
            if (itemIndex >= 0)
            {
                var item = inventoryItems[itemIndex];
                item.Amount += depositAmount;
                inventoryItems[itemIndex] = item;
            }
            else
            {
                // Create new inventory item
                inventoryItems.Add(new StorehouseInventoryItem
                {
                    ResourceTypeId = resourceTypeId,
                    Amount = depositAmount,
                    Reserved = 0f
                });
            }

            // Update inventory totals
            inventory.TotalStored += depositAmount;
            // Use current tick if TimeState exists, otherwise use 0
            var tick = SystemAPI.TryGetSingleton<TimeState>(out var timeState) ? timeState.Tick : 0u;
            inventory.LastUpdateTick = tick;
            _storehouseInventoryLookup[storehouseEntity] = inventory;

            // Update villager carrying (reduce by deposited amount)
            carrying.Amount -= depositAmount;
            if (carrying.Amount <= 0f)
            {
                // Will be removed by ECB in calling code
            }
            else
            {
                state.EntityManager.SetComponentData(villagerEntity, carrying);
            }

            return true;
        }
    }
}

