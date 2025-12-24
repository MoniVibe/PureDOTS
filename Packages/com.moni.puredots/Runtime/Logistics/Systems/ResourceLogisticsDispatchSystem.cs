using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Logistics;
using PureDOTS.Runtime.Logistics.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Logistics.Systems
{
    /// <summary>
    /// Assigns transports to orders and creates shipments.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ResourceReservationSystem))]
    [UpdateBefore(typeof(ResourceRoutingSystem))]
    public partial struct ResourceLogisticsDispatchSystem : ISystem
    {
        private ComponentLookup<LogisticsOrder> _orderLookup;
        private ComponentLookup<Shipment> _shipmentLookup;
        private ComponentLookup<LogisticsNode> _nodeLookup;
        private ComponentLookup<HaulerCapacity> _haulerCapacityLookup;
        private ComponentLookup<CapacityReservation> _capacityReservationLookup;

        private int _nextShipmentId;
        private int _nextCapacityReservationId;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<LogisticsOrder>();
            _orderLookup = state.GetComponentLookup<LogisticsOrder>(false);
            _shipmentLookup = state.GetComponentLookup<Shipment>(false);
            _nodeLookup = state.GetComponentLookup<LogisticsNode>(false);
            _haulerCapacityLookup = state.GetComponentLookup<HaulerCapacity>(true);
            _capacityReservationLookup = state.GetComponentLookup<CapacityReservation>(false);
            _nextShipmentId = 1;
            _nextCapacityReservationId = 1;
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
            _shipmentLookup.Update(ref state);
            _nodeLookup.Update(ref state);
            _haulerCapacityLookup.Update(ref state);
            _capacityReservationLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Build set of assigned transports (transports already assigned to shipments)
            var assignedTransports = new NativeHashSet<Entity>(16, Allocator.Temp);
            foreach (var (shipment, _) in SystemAPI.Query<RefRO<Shipment>>()
                .WithEntityAccess())
            {
                if (shipment.ValueRO.AssignedTransport != Entity.Null &&
                    shipment.ValueRO.Status != ShipmentStatus.Delivered &&
                    shipment.ValueRO.Status != ShipmentStatus.Failed)
                {
                    assignedTransports.Add(shipment.ValueRO.AssignedTransport);
                }
            }

            // Estimate resource mass/volume (simplified: 1 unit = 1 kg mass, 0.001 m³ volume)
            const float massPerUnit = 1f;
            const float volumePerUnit = 0.001f;

            // Assign transports to reserved orders
            foreach (var (order, orderEntity) in SystemAPI.Query<RefRW<LogisticsOrder>>()
                .WithEntityAccess())
            {
                if (order.ValueRO.Status != LogisticsOrderStatus.Reserved)
                {
                    continue;
                }

                if (order.ValueRO.AssignedTransport != Entity.Null)
                {
                    continue; // Already assigned
                }

                float requiredMass = order.ValueRO.RequestedAmount * massPerUnit;
                float requiredVolume = order.ValueRO.RequestedAmount * volumePerUnit;

                // Find suitable transport with sufficient capacity
                Entity bestTransport = Entity.Null;
                float bestCapacityUtilization = float.MaxValue;

                foreach (var (haulerTag, transportEntity) in SystemAPI.Query<RefRO<HaulerTag>>()
                    .WithEntityAccess())
                {
                    // Skip already assigned transports
                    if (assignedTransports.Contains(transportEntity))
                    {
                        continue;
                    }

                    // Check capacity
                    if (!_haulerCapacityLookup.HasComponent(transportEntity))
                    {
                        continue;
                    }

                    var capacity = _haulerCapacityLookup[transportEntity];

                    // Calculate already reserved capacity
                    float reservedMass = 0f;
                    float reservedVolume = 0f;
                    foreach (var (reservation, _) in SystemAPI.Query<RefRO<CapacityReservation>>()
                        .WithEntityAccess())
                    {
                        if (reservation.ValueRO.TransportEntity == transportEntity &&
                            reservation.ValueRO.Status == ReservationStatus.Active)
                        {
                            reservedMass += reservation.ValueRO.ReservedMass;
                            reservedVolume += reservation.ValueRO.ReservedVolume;
                        }
                    }

                    float availableMass = capacity.MaxMass - reservedMass;
                    float availableVolume = capacity.MaxVolume - reservedVolume;

                    // Check if transport can carry the order
                    if (availableMass >= requiredMass && availableVolume >= requiredVolume)
                    {
                        // Prefer transport with best capacity utilization (closest fit)
                        float massUtilization = requiredMass / capacity.MaxMass;
                        float volumeUtilization = requiredVolume / capacity.MaxVolume;
                        float utilization = math.max(massUtilization, volumeUtilization);

                        if (utilization < bestCapacityUtilization)
                        {
                            bestTransport = transportEntity;
                            bestCapacityUtilization = utilization;
                        }
                    }
                }

                if (bestTransport != Entity.Null)
                {
                    ResourceLogisticsService.AssignTransport(ref order.ValueRW, bestTransport);
                    order.ValueRW.Status = LogisticsOrderStatus.Dispatched;

                    // Add transport to assigned set to prevent double-assignment
                    assignedTransports.Add(bestTransport);

                    // Create capacity reservation
                    uint defaultTTL = 1000; // Default TTL in ticks
                    var capacityReservation = ResourceReservationService.ReserveCapacity(
                        bestTransport,
                        Entity.Null, // Container would be determined from transport
                        order.ValueRO.ResourceId,
                        order.ValueRO.RequestedAmount,
                        requiredMass,
                        requiredVolume,
                        orderEntity,
                        tickTime.Tick,
                        defaultTTL,
                        _nextCapacityReservationId++);

                    var capacityResEntity = ecb.CreateEntity();
                    ecb.AddComponent(capacityResEntity, capacityReservation);

                    // Create shipment
                    ResourceLogisticsService.CreateShipment(
                        order.ValueRO,
                        Entity.Null, // Route will be set by routing system
                        _nextShipmentId++,
                        tickTime.Tick,
                        100f,
                        out var shipment); // Placeholder transit time

                    shipment.AllocatedMass = requiredMass;
                    shipment.AllocatedVolume = requiredVolume;

                    var shipmentEntity = ecb.CreateEntity();
                    ecb.AddComponent(shipmentEntity, shipment);
                    
                    // Populate ShipmentOrderRef buffer
                    var orderRefBuffer = ecb.AddBuffer<ShipmentOrderRef>(shipmentEntity);
                    orderRefBuffer.Add(new ShipmentOrderRef
                    {
                        OrderEntity = orderEntity,
                        AllocatedAmount = order.ValueRO.RequestedAmount
                    });

                    // Populate ShipmentCargoAllocation buffer
                    var cargoAllocationBuffer = ecb.AddBuffer<ShipmentCargoAllocation>(shipmentEntity);
                    cargoAllocationBuffer.Add(new ShipmentCargoAllocation
                    {
                        ResourceId = order.ValueRO.ResourceId,
                        AllocatedAmount = order.ValueRO.RequestedAmount, // Actual allocated = requested for now
                        ContainerEntity = Entity.Null, // Could be determined from transport later
                        BatchEntity = Entity.Null
                    });

                    order.ValueRW.ShipmentEntity = shipmentEntity;
                }
            }

            assignedTransports.Dispose();
            ecb.Playback(state.EntityManager);
        }
    }
}

