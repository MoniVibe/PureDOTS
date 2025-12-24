using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Logistics;
using PureDOTS.Runtime.Logistics.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Logistics.Systems
{
    /// <summary>
    /// Manages resource reservations: inventory, capacity, and service reservations.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ResourceLogisticsPlanningSystem))]
    [UpdateBefore(typeof(ResourceLogisticsDispatchSystem))]
    public partial struct ResourceReservationSystem : ISystem
    {
        private ComponentLookup<LogisticsOrder> _orderLookup;
        private ComponentLookup<InventoryReservation> _inventoryReservationLookup;
        private ComponentLookup<CapacityReservation> _capacityReservationLookup;
        private ComponentLookup<ServiceReservation> _serviceReservationLookup;
        private ComponentLookup<ReservationPolicy> _policyLookup;

        private int _nextReservationId;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<LogisticsOrder>();
            _orderLookup = state.GetComponentLookup<LogisticsOrder>(false);
            _inventoryReservationLookup = state.GetComponentLookup<InventoryReservation>(false);
            _capacityReservationLookup = state.GetComponentLookup<CapacityReservation>(false);
            _serviceReservationLookup = state.GetComponentLookup<ServiceReservation>(false);
            _policyLookup = state.GetComponentLookup<ReservationPolicy>(false);
            _nextReservationId = 1;
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
            _inventoryReservationLookup.Update(ref state);
            _capacityReservationLookup.Update(ref state);
            _serviceReservationLookup.Update(ref state);
            _policyLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            uint defaultTTL = 1000; // Default TTL in ticks
            if (SystemAPI.TryGetSingleton<ReservationPolicy>(out var policy))
            {
                defaultTTL = (uint)(policy.DefaultTTLSeconds * 60); // Convert to ticks (assuming 60 ticks/sec)
            }

            // Create reservations for orders in Planning status
            foreach (var (order, orderEntity) in SystemAPI.Query<RefRW<LogisticsOrder>>()
                .WithEntityAccess())
            {
                if (order.ValueRO.Status != LogisticsOrderStatus.Planning)
                {
                    continue;
                }

                // Reserve inventory at source
                var invReservation = ResourceReservationService.ReserveInventory(
                    order.ValueRO.SourceNode,
                    Entity.Null, // Container would be determined from node
                    order.ValueRO.ResourceId,
                    order.ValueRO.RequestedAmount,
                    orderEntity,
                    tickTime.Tick,
                    defaultTTL,
                    _nextReservationId++);

                var invResEntity = ecb.CreateEntity();
                ecb.AddComponent(invResEntity, invReservation);

                order.ValueRW.Status = LogisticsOrderStatus.Reserved;
                order.ValueRW.ReservedAmount = order.ValueRO.RequestedAmount;
            }

            // Cancel expired reservations and remove released/expired ones
            foreach (var (reservation, entity) in SystemAPI.Query<RefRW<InventoryReservation>>()
                .WithEntityAccess())
            {
                ResourceReservationService.CancelExpiredReservation(ref reservation.ValueRW, tickTime.Tick);
                if (reservation.ValueRO.Status == ReservationStatus.Released ||
                    reservation.ValueRO.Status == ReservationStatus.Expired)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            foreach (var (reservation, entity) in SystemAPI.Query<RefRW<CapacityReservation>>()
                .WithEntityAccess())
            {
                ResourceReservationService.CancelExpiredReservation(ref reservation.ValueRW, tickTime.Tick);
                if (reservation.ValueRO.Status == ReservationStatus.Released ||
                    reservation.ValueRO.Status == ReservationStatus.Expired)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            foreach (var (reservation, entity) in SystemAPI.Query<RefRW<ServiceReservation>>()
                .WithEntityAccess())
            {
                ResourceReservationService.CancelExpiredReservation(ref reservation.ValueRW, tickTime.Tick);
                if (reservation.ValueRO.Status == ReservationStatus.Released ||
                    reservation.ValueRO.Status == ReservationStatus.Expired)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}

