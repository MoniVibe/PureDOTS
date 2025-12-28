using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Logistics;
using PureDOTS.Runtime.Logistics.Components;
using PureDOTS.Runtime.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
        private const float DefaultReservationTTLSeconds = 16.6667f;
        private ComponentLookup<LogisticsOrder> _orderLookup;
        private ComponentLookup<InventoryReservation> _inventoryReservationLookup;
        private ComponentLookup<CapacityReservation> _capacityReservationLookup;
        private ComponentLookup<ServiceReservation> _serviceReservationLookup;
        private ComponentLookup<ReservationPolicy> _policyLookup;
        private BufferLookup<StorehouseInventoryItem> _storehouseInventoryItems;
        private ComponentLookup<StorehouseInventory> _storehouseInventoryLookup;

        private int _nextReservationId;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<LogisticsOrder>();
            state.RequireForUpdate<ResourceTypeIndex>();
            _orderLookup = state.GetComponentLookup<LogisticsOrder>(false);
            _inventoryReservationLookup = state.GetComponentLookup<InventoryReservation>(false);
            _capacityReservationLookup = state.GetComponentLookup<CapacityReservation>(false);
            _serviceReservationLookup = state.GetComponentLookup<ServiceReservation>(false);
            _policyLookup = state.GetComponentLookup<ReservationPolicy>(false);
            _storehouseInventoryItems = state.GetBufferLookup<StorehouseInventoryItem>(false);
            _storehouseInventoryLookup = state.GetComponentLookup<StorehouseInventory>(true);
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

            if (!SystemAPI.TryGetSingleton<ResourceTypeIndex>(out var resourceTypeIndex) ||
                !resourceTypeIndex.Catalog.IsCreated)
            {
                return;
            }

            _orderLookup.Update(ref state);
            _inventoryReservationLookup.Update(ref state);
            _capacityReservationLookup.Update(ref state);
            _serviceReservationLookup.Update(ref state);
            _policyLookup.Update(ref state);
            _storehouseInventoryItems.Update(ref state);
            _storehouseInventoryLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var fixedDeltaTime = math.max(1e-6f, tickTime.FixedDeltaTime);
            var defaultTTLSeconds = DefaultReservationTTLSeconds;
            var allowPartialReservations = false;
            if (SystemAPI.TryGetSingleton<ReservationPolicy>(out var policy))
            {
                defaultTTLSeconds = policy.DefaultTTLSeconds;
                allowPartialReservations = policy.AllowPartialReservations != 0;
            }
            var defaultTTL = (uint)math.max(1f, math.ceil(defaultTTLSeconds / fixedDeltaTime));

            // Create reservations for orders in Planning status
            foreach (var (order, orderEntity) in SystemAPI.Query<RefRW<LogisticsOrder>>()
                .WithEntityAccess())
            {
                if (order.ValueRO.Status != LogisticsOrderStatus.Planning)
                {
                    continue;
                }

                if (!TryReserveInventory(order.ValueRO.SourceNode,
                        order.ValueRO.ResourceId,
                        order.ValueRO.RequestedAmount,
                        allowPartialReservations,
                        resourceTypeIndex.Catalog,
                        out var reservedAmount,
                        out var failureReason))
                {
                    order.ValueRW.Status = LogisticsOrderStatus.Failed;
                    order.ValueRW.FailureReason = failureReason;
                    order.ValueRW.ReservedAmount = 0f;
                    continue;
                }

                // Reserve inventory at source
                var invReservation = ResourceReservationService.ReserveInventory(
                    order.ValueRO.SourceNode,
                    Entity.Null, // Container would be determined from node
                    order.ValueRO.ResourceId,
                    reservedAmount,
                    orderEntity,
                    tickTime.Tick,
                    defaultTTL,
                    _nextReservationId++);
                invReservation.ReservationFlags |= InventoryReservationFlags.ReservedApplied;

                var invResEntity = ecb.CreateEntity();
                ecb.AddComponent(invResEntity, invReservation);

                order.ValueRW.Status = LogisticsOrderStatus.Reserved;
                order.ValueRW.ReservedAmount = reservedAmount;
                order.ValueRW.FailureReason = ShipmentFailureReason.None;
            }

            // Cancel expired reservations and remove released/expired ones
            foreach (var (reservation, entity) in SystemAPI.Query<RefRW<InventoryReservation>>()
                .WithEntityAccess())
            {
                ResourceReservationService.CancelExpiredReservation(ref reservation.ValueRW, tickTime.Tick);
                if (reservation.ValueRO.Status == ReservationStatus.Released ||
                    reservation.ValueRO.Status == ReservationStatus.Expired ||
                    reservation.ValueRO.Status == ReservationStatus.Cancelled)
                {
                    ReleaseInventoryHold(reservation.ValueRO, resourceTypeIndex.Catalog);
                    ecb.DestroyEntity(entity);
                }
            }

            foreach (var (reservation, entity) in SystemAPI.Query<RefRW<CapacityReservation>>()
                .WithEntityAccess())
            {
                ResourceReservationService.CancelExpiredReservation(ref reservation.ValueRW, tickTime.Tick);
                if (reservation.ValueRO.Status == ReservationStatus.Released ||
                    reservation.ValueRO.Status == ReservationStatus.Expired ||
                    reservation.ValueRO.Status == ReservationStatus.Cancelled)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            foreach (var (reservation, entity) in SystemAPI.Query<RefRW<ServiceReservation>>()
                .WithEntityAccess())
            {
                ResourceReservationService.CancelExpiredReservation(ref reservation.ValueRW, tickTime.Tick);
                if (reservation.ValueRO.Status == ReservationStatus.Released ||
                    reservation.ValueRO.Status == ReservationStatus.Expired ||
                    reservation.ValueRO.Status == ReservationStatus.Cancelled)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
        }

        private bool TryReserveInventory(
            Entity sourceNode,
            FixedString64Bytes resourceId,
            float requestedAmount,
            bool allowPartial,
            BlobAssetReference<ResourceTypeIndexBlob> catalog,
            out float reservedAmount,
            out ShipmentFailureReason failureReason)
        {
            reservedAmount = 0f;
            failureReason = ShipmentFailureReason.None;

            if (sourceNode == Entity.Null ||
                !_storehouseInventoryLookup.HasComponent(sourceNode) ||
                !_storehouseInventoryItems.HasBuffer(sourceNode))
            {
                failureReason = ShipmentFailureReason.InvalidSource;
                return false;
            }

            if (requestedAmount <= 0f)
            {
                failureReason = ShipmentFailureReason.NoInventory;
                return false;
            }

            var resourceIndex = catalog.Value.LookupIndex(resourceId);
            if (resourceIndex < 0)
            {
                failureReason = ShipmentFailureReason.InvalidSource;
                return false;
            }

            var items = _storehouseInventoryItems[sourceNode];
            if (StorehouseMutationService.TryReserveOut(
                    (ushort)resourceIndex,
                    requestedAmount,
                    allowPartial,
                    catalog,
                    items,
                    out reservedAmount))
            {
                return true;
            }

            failureReason = ShipmentFailureReason.NoInventory;
            return false;
        }

        private void ReleaseInventoryHold(
            InventoryReservation reservation,
            BlobAssetReference<ResourceTypeIndexBlob> catalog)
        {
            if ((reservation.ReservationFlags & InventoryReservationFlags.ReservedApplied) == 0)
            {
                return;
            }

            if ((reservation.ReservationFlags & InventoryReservationFlags.Withdrawn) != 0)
            {
                return;
            }

            if (reservation.SourceNode == Entity.Null ||
                !_storehouseInventoryItems.HasBuffer(reservation.SourceNode))
            {
                return;
            }

            var items = _storehouseInventoryItems[reservation.SourceNode];
            var resourceIndex = catalog.Value.LookupIndex(reservation.ResourceId);
            if (resourceIndex < 0)
            {
                return;
            }

            StorehouseMutationService.CancelReserveOut(
                (ushort)resourceIndex,
                reservation.ReservedAmount,
                catalog,
                items);
        }
    }
}

