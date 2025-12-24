using System;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Logistics;
using PureDOTS.Runtime.Logistics.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Logistics.Systems
{
    /// <summary>
    /// Plans routes for orders and consolidates orders from same source/destination.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ResourceLogisticsOrderGenerationSystem))]
    [UpdateBefore(typeof(ResourceReservationSystem))]
    public partial struct ResourceLogisticsPlanningSystem : ISystem
    {
        private ComponentLookup<LogisticsOrder> _orderLookup;
        private ComponentLookup<LogisticsNode> _nodeLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<LogisticsOrder>();
            _orderLookup = state.GetComponentLookup<LogisticsOrder>(false);
            _nodeLookup = state.GetComponentLookup<LogisticsNode>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioState>(out var scenario) ||
                !scenario.IsInitialized ||
                !scenario.EnableEconomy)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) ||
                rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<TickTimeState>(out var tickTime))
            {
                return;
            }

            _orderLookup.Update(ref state);
            _nodeLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Consolidate orders by source/destination pair
            var orderGroups = new NativeHashMap<OrderGroupKey, NativeList<Entity>>(16, Allocator.Temp);

            foreach (var (order, entity) in SystemAPI.Query<RefRO<LogisticsOrder>>()
                .WithEntityAccess())
            {
                if (order.ValueRO.Status != LogisticsOrderStatus.Created)
                {
                    continue;
                }

                var key = new OrderGroupKey
                {
                    Source = order.ValueRO.SourceNode,
                    Destination = order.ValueRO.DestinationNode,
                    ResourceId = order.ValueRO.ResourceId
                };

                if (!orderGroups.TryGetValue(key, out var group))
                {
                    group = new NativeList<Entity>(4, Allocator.Temp);
                    orderGroups.Add(key, group);
                }

                group.Add(entity);
            }

            // Consolidate groups with multiple orders
            foreach (var kvp in orderGroups)
            {
                if (kvp.Value.Length > 1)
                {
                    // Consolidate orders
                    var orders = new NativeList<LogisticsOrder>(kvp.Value.Length, Allocator.Temp);
                    foreach (var orderEntity in kvp.Value)
                    {
                        orders.Add(_orderLookup[orderEntity]);
                    }

                    ResourceLogisticsService.ConsolidateOrders(orders, tickTime.Tick, out var consolidated);
                    // Use first order's ID (preserve original ID)
                    consolidated.OrderId = orders[0].OrderId;

                    // Update first order, remove others
                    _orderLookup[kvp.Value[0]] = consolidated;
                    consolidated.Status = LogisticsOrderStatus.Planning;
                    _orderLookup[kvp.Value[0]] = consolidated;

                    for (int i = 1; i < kvp.Value.Length; i++)
                    {
                        ecb.RemoveComponent<LogisticsOrder>(kvp.Value[i]);
                    }

                    orders.Dispose();
                }
                else if (kvp.Value.Length == 1)
                {
                    // Single order - mark as planning
                    var order = _orderLookup[kvp.Value[0]];
                    order.Status = LogisticsOrderStatus.Planning;
                    _orderLookup[kvp.Value[0]] = order;
                }
            }

            // Cleanup: dispose all NativeList instances before disposing map
            foreach (var kvp in orderGroups)
            {
                kvp.Value.Dispose();
            }
            orderGroups.Dispose();

            ecb.Playback(state.EntityManager);
        }

        private struct OrderGroupKey : IEquatable<OrderGroupKey>
        {
            public Entity Source;
            public Entity Destination;
            public FixedString64Bytes ResourceId;

            public bool Equals(OrderGroupKey other)
            {
                return Source.Equals(other.Source) &&
                       Destination.Equals(other.Destination) &&
                       ResourceId.Equals(other.ResourceId);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Source, Destination, ResourceId);
            }
        }
    }
}

