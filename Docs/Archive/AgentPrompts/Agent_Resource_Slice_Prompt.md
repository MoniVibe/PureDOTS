# Agent: Resource Slice - Logistics, Routing & Reservation

## Status: 🟡 PARTIAL IMPLEMENTATION - Ready for Extension

## Scope
Implement the resource logistics, routing, and reservation systems that manage resource flow from sources to destinations, handle transport planning, and prevent double-spending through reservations.

## Core Concept

**Resource Flow Pipeline**:
```
Resource Source (Mine, Forest, Farm)
    ↓ Extraction/Gathering
Resource Chunks/Piles (Physical entities)
    ↓ Hauling/Transport
Storehouse/Inventory (Aggregate storage)
    ↓ Logistics Orders
Transport Assignment (Caravan, Carrier, Ship)
    ↓ Routing & Transit
Destination Delivery (Construction, Consumption, Trade)
```

**Key Systems**:
1. **Resource Logistics**: Order generation, transport assignment, shipment tracking
2. **Resource Routing**: Pathfinding, route planning, cost calculation, rerouting
3. **Resource Reservation**: Inventory reservations, capacity reservations, service reservations

---

## Existing Implementation Status

### ✅ Implemented
- **Resource Components**: `ResourceComponents.cs` - ResourceTypeId, ResourceSource, ResourceChunk, Storehouse
- **Resource Registry**: `ResourceRegistrySystem.cs` - Spatial indexing of resources
- **Storehouse Systems**: `StorehouseSystems.cs` - Storage and inventory management
- **Logistics Jobs**: `LogisticsJobComponents.cs` - Basic job structure
- **Resource Piles**: `ResourcePileComponents.cs` - Physical resource entities
- **Resource Reservations**: `ResourceJobReservation`, `StorehouseJobReservation` (basic structure exists)

### 🟡 Partial / Missing
- **Resource Logistics Service**: Order planning, consolidation, transport assignment
- **Resource Routing System**: Route calculation, cost terms, caching, rerouting
- **Resource Reservation System**: Reservation lifecycle, TTL, cancellation policies
- **Service Nodes**: Dock/load/unload queue management, congestion tracking

---

## Stub Files to Create & Implement

### Resource Logistics Service (3 files)
- Create: `Runtime/Stubs/ResourceLogisticsStub.cs` → `Runtime/Logistics/ResourceLogisticsService.cs`
- Create: `Runtime/Stubs/ResourceLogisticsStubComponents.cs` → `Runtime/Logistics/ResourceLogisticsComponents.cs`
- Create: `Runtime/Stubs/ResourceLogisticsStubSystems.cs` → `Systems/Logistics/ResourceLogisticsSystem.cs`

**Requirements:**
- `CreateOrder(in Entity source, in Entity destination, FixedString64Bytes resourceId, float amount, LogisticsJobKind kind)` - Create logistics order
- `PlanOrder(in Entity orderEntity)` - Plan route and assign transport
- `ConsolidateOrders(in Entity source, in Entity destination)` - Consolidate multiple orders
- `AssignTransport(in Entity orderEntity, in Entity transportEntity)` - Assign transport to order
- `CreateShipment(in Entity orderEntity)` - Create shipment from order
- `UpdateShipmentState(in Entity shipmentEntity, LogisticsJobStatus status)` - Update shipment status

### Resource Routing Service (3 files)
- Create: `Runtime/Stubs/ResourceRoutingStub.cs` → `Runtime/Logistics/ResourceRoutingService.cs`
- Create: `Runtime/Stubs/ResourceRoutingStubComponents.cs` → `Runtime/Logistics/ResourceRoutingComponents.cs`
- Create: `Runtime/Stubs/ResourceRoutingStubSystems.cs` → `Systems/Logistics/ResourceRoutingSystem.cs`

**Requirements:**
- `CalculateRoute(in Entity source, in Entity destination, RouteProfile profile)` - Calculate route between nodes
- `GetRouteCost(in Entity routeEntity)` - Calculate total route cost
- `RerouteShipment(in Entity shipmentEntity, RouteRerouteReason reason)` - Reroute active shipment
- `UpdateRouteCache()` - Invalidate/update route cache
- `GetRouteETA(in Entity routeEntity)` - Estimate time to arrival
- `FindAlternateRoute(in Entity source, in Entity destination, RouteConstraints constraints)` - Find alternate routes

### Resource Reservation Service (3 files)
- Create: `Runtime/Stubs/ResourceReservationStub.cs` → `Runtime/Logistics/ResourceReservationService.cs`
- Create: `Runtime/Stubs/ResourceReservationStubComponents.cs` → `Runtime/Logistics/ResourceReservationComponents.cs`
- Create: `Runtime/Stubs/ResourceReservationStubSystems.cs` → `Systems/Logistics/ResourceReservationSystem.cs`

**Requirements:**
- `ReserveInventory(in Entity source, FixedString64Bytes resourceId, float amount, in Entity orderEntity)` - Reserve inventory at source
- `ReserveCapacity(in Entity transport, FixedString64Bytes resourceId, float amount, in Entity orderEntity)` - Reserve transport capacity
- `ReserveService(in Entity serviceNode, ServiceType serviceType, in Entity orderEntity)` - Reserve service slot/time
- `ReleaseReservation(in Entity reservationEntity)` - Release reservation
- `CheckReservationValidity(in Entity reservationEntity)` - Validate reservation still valid
- `CancelExpiredReservations()` - Cancel reservations past TTL

---

## Component Requirements

### 1. Logistics Order Components

```csharp
// Extend existing LogisticsJob with additional fields
public struct LogisticsOrder : IComponentData
{
    public int OrderId;
    public LogisticsJobKind Kind;
    public Entity SourceNode;
    public Entity DestinationNode;
    public FixedString64Bytes ResourceId;
    public float RequestedAmount;
    public float ReservedAmount;  // Amount currently reserved
    public LogisticsOrderStatus Status;
    public Entity AssignedTransport;
    public Entity ShipmentEntity;
    public uint CreatedTick;
    public uint EarliestDepartTick;
    public uint LatestArrivalTick;
    public byte Priority;
    public RouteConstraints Constraints;
}

public enum LogisticsOrderStatus : byte
{
    Created = 0,
    Planning = 1,
    Reserved = 2,
    Dispatched = 3,
    InTransit = 4,
    Delivered = 5,
    Failed = 6,
    Cancelled = 7
}

public struct RouteConstraints : IComponentData
{
    public float MaxRisk;
    public float MaxRouteLength;
    public float MaxCost;
    public byte LegalityFlags;
    public byte SecrecyFlags;
    public FixedList64Bytes<ServiceType> RequiredServices;
}
```

### 2. Shipment Components

```csharp
public struct Shipment : IComponentData
{
    public int ShipmentId;
    public Entity AssignedTransport;
    public Entity RouteEntity;
    public ShipmentStatus Status;
    public ShipmentRepresentationMode RepresentationMode;
    public float AllocatedMass;
    public float AllocatedVolume;
    public uint DepartureTick;
    public uint EstimatedArrivalTick;
    public uint ActualArrivalTick;
}

public enum ShipmentStatus : byte
{
    Created = 0,
    Loading = 1,
    InTransit = 2,
    Unloading = 3,
    Delivered = 4,
    Failed = 5,
    Rerouting = 6
}

public enum ShipmentRepresentationMode : byte
{
    Physical = 0,  // Entity exists
    Abstract = 1   // May materialize on triggers
}

[InternalBufferCapacity(8)]
public struct ShipmentOrderRef : IBufferElementData
{
    public Entity OrderEntity;
    public float AllocatedAmount;
}

[InternalBufferCapacity(16)]
public struct ShipmentCargoAllocation : IBufferElementData
{
    public FixedString64Bytes ResourceId;
    public Entity ContainerEntity;
    public float AllocatedAmount;
    public Entity BatchEntity;  // BatchId reference
}
```

### 3. Route Components

```csharp
public struct Route : IComponentData
{
    public int RouteId;
    public Entity SourceNode;
    public Entity DestinationNode;
    public RouteStatus Status;
    public float TotalDistance;
    public float TotalCost;
    public float EstimatedTransitTime;
    public uint CalculatedTick;
    public uint CacheVersion;
    public RouteCacheKey CacheKey;
}

public enum RouteStatus : byte
{
    Calculating = 0,
    Valid = 1,
    Invalid = 2,
    Expired = 3
}

[InternalBufferCapacity(32)]
public struct RouteEdge : IBufferElementData
{
    public Entity SourceNode;
    public Entity DestinationNode;
    public float Distance;
    public float BaseCost;
    public float RiskCost;
    public float CongestionCost;
    public float TotalCost;
    public RouteEdgeState State;
}

public struct RouteEdgeState : IComponentData
{
    public byte ControlFlags;  // Ownership, access restrictions
    public float HazardLevel;
    public float InterdictionLikelihood;
    public float CongestionMultiplier;
    public float SeasonalModifier;
    public uint LastUpdatedTick;
}

public struct RouteCacheKey : IComponentData
{
    public Entity SourceNode;
    public Entity DestinationNode;
    public int BehaviorProfileId;
    public byte LegalityMask;
    public uint KnowledgeVersionId;
    public uint TopologyVersionId;
}
```

### 4. Reservation Components

```csharp
public struct InventoryReservation : IComponentData
{
    public int ReservationId;
    public Entity SourceNode;
    public Entity ContainerEntity;
    public FixedString64Bytes ResourceId;
    public float ReservedAmount;
    public Entity OrderEntity;
    public ReservationStatus Status;
    public uint CreatedTick;
    public uint ExpiryTick;
    public byte ReservationFlags;
}

public struct CapacityReservation : IComponentData
{
    public int ReservationId;
    public Entity TransportEntity;
    public Entity ContainerEntity;
    public FixedString64Bytes ResourceId;
    public float ReservedCapacity;
    public float ReservedMass;
    public float ReservedVolume;
    public Entity OrderEntity;
    public ReservationStatus Status;
    public uint CreatedTick;
    public uint ExpiryTick;
}

public struct ServiceReservation : IComponentData
{
    public int ReservationId;
    public Entity ServiceNode;
    public ServiceType ServiceType;
    public Entity OrderEntity;
    public ReservationStatus Status;
    public uint ReservedSlotTime;
    public uint CreatedTick;
    public uint ExpiryTick;
}

public enum ReservationStatus : byte
{
    Active = 0,
    Committed = 1,
    Released = 2,
    Expired = 3,
    Cancelled = 4
}

public enum ServiceType : byte
{
    Dock = 0,
    Load = 1,
    Unload = 2,
    Customs = 3,
    Refuel = 4,
    Repair = 5,
    GateJump = 6
}
```

### 5. Node & Container Components

```csharp
public struct LogisticsNode : IComponentData
{
    public int NodeId;
    public NodeKind Kind;
    public Entity OwnerFaction;
    public float3 Position;
    public int SpatialCellId;
    public NodeServices Services;
}

public enum NodeKind : byte
{
    TileCell = 0,
    District = 1,
    Settlement = 2,
    Station = 3,
    Warehouse = 4,
    MobileTransport = 5,
    EntityInventory = 6
}

public struct NodeServices : IComponentData
{
    public byte ServiceFlags;  // Bitmask of available services
    public byte SlotCapacity;
    public float ThroughputBudget;
    public int QueuePolicyId;
}

[InternalBufferCapacity(8)]
public struct NodeContainerRef : IBufferElementData
{
    public Entity ContainerEntity;
    public float CapacityMass;
    public float CapacityVolume;
}

public struct LogisticsContainer : IComponentData
{
    public int ContainerId;
    public Entity ParentNode;
    public ContainerType ContainerType;
    public float CapacityMass;
    public float CapacityVolume;
    public int SlotCount;
    public byte AllowedTagMask;
    public int MixingPolicyId;
    public byte SpecialFacilityFlags;
    public float LoadRate;
    public float UnloadRate;
}
```

---

## Implementation Requirements

### Phase 1: Resource Logistics Service

**System**: `ResourceLogisticsSystem`
- Query entities with demand (construction sites, consumption points, trade orders)
- Generate `LogisticsOrder` components
- Consolidate orders from same source/destination
- Assign transports to orders
- Create `Shipment` entities from orders

**Order Generation Logic**:
- Construction sites create orders for missing resources
- Storehouses create orders when stock falls below threshold
- Trade contracts create orders for delivery
- Consumption points create orders for supplies

**Consolidation Logic**:
- Group orders by source/destination pair
- Combine orders for same resource type
- Respect transport capacity limits
- Maintain priority ordering

**Dependencies**:
- `LogisticsJobComponents.cs` (existing)
- `ResourceComponents.cs` (existing)
- `StorehouseSystems.cs` (existing)

### Phase 2: Resource Routing Service

**System**: `ResourceRoutingSystem`
- Build routing graph from nodes (spatial grid or explicit edges)
- Calculate routes using A* or Dijkstra
- Apply cost terms (distance, risk, congestion, legality)
- Cache routes with versioning
- Handle rerouting on intel/hazard updates

**Route Calculation**:
- Start from `SourceNode` → find path to `DestinationNode`
- Apply cost terms from `RouteConstraints` and `BehaviorProfile`
- Consider knowledge/intel (unknown hazards = higher risk)
- Cache results with `RouteCacheKey`

**Cost Terms**:
- Base traversal cost (distance × speed)
- Risk term (hazard level × risk tolerance)
- Legality term (contraband penalties)
- Border/politics term (faction relations)
- Stealth term (secrecy requirements)
- Congestion term (queue wait time)
- Seasonal/event term (temporary modifiers)

**Rerouting Logic**:
- Detect route invalidation (hazard detected, node compromised, border closed)
- Find alternate route
- Update shipment route
- Notify transport entity

**Dependencies**:
- Spatial grid system (for node discovery)
- Knowledge/intel system (for hazard awareness)
- Faction relations (for border access)

### Phase 3: Resource Reservation Service

**System**: `ResourceReservationSystem`
- Reserve inventory at source nodes when order created
- Reserve capacity on transports when shipment assigned
- Reserve service slots at nodes (docks, loaders)
- Track reservation TTL and expiry
- Release reservations on delivery/failure/cancellation

**Reservation Lifecycle**:
1. **Create**: Reserve when order planned
2. **Commit**: Commit when shipment dispatched
3. **Release**: Release on delivery or cancellation
4. **Expire**: Auto-release if TTL exceeded

**Reservation Policies**:
- **Partial Reservations**: Allow partial reserves if full amount unavailable
- **Reservation TTL**: Reservations expire after timeout (configurable)
- **Reservation Cancellation**: Cancel on order cancellation or transport failure
- **Reservation Contention**: Handle multiple orders competing for same resource

**Reservation Validation**:
- Check source still has reserved amount
- Check transport still has reserved capacity
- Check service node still has available slots
- Validate reservation hasn't expired

**Dependencies**:
- `ResourceJobReservation` (existing basic structure)
- `StorehouseJobReservation` (existing basic structure)
- Inventory systems (for checking availability)

### Phase 4: Service Node Management

**System**: `ServiceNodeSystem`
- Track service queues at nodes (docks, loaders, customs)
- Process service requests in priority order
- Track congestion (queue length, throughput saturation)
- Reserve service slots for orders
- Update throughput budgets

**Service Queue Management**:
- Queue policy: Priority, FIFO, Faction-based, Bidding
- Slot capacity: Parallel service slots
- Throughput budget: Units processed per tick
- Congestion tracking: Queue length, wait time, saturation

**Dependencies**:
- `NodeServices` component
- Queue policy system

---

## Integration Points

### With Existing Systems

1. **Resource Registry** (`ResourceRegistrySystem`)
   - Logistics system queries registry for available resources
   - Updates registry when resources reserved/consumed

2. **Storehouse Systems** (`StorehouseSystems`)
   - Logistics system reads storehouse inventory
   - Creates orders when stock low
   - Delivers resources to storehouses

3. **Construction Systems**
   - Construction sites create logistics orders for materials
   - Logistics system delivers materials to construction sites

4. **Transport Systems** (Movement/Pathfinding)
   - Routing system provides routes to transport entities
   - Transport entities follow route edges
   - Transport system reports position updates to logistics

5. **Knowledge/Intel Systems**
   - Routing system queries knowledge for hazard awareness
   - Intel events trigger route recalculation
   - Unknown hazards increase route risk

6. **Faction Relations**
   - Routing system checks faction relations for border access
   - Legality constraints based on faction ownership

### With Stub Systems

1. **Resource Logistics** (this agent)
   - Integrates with all resource systems
   - Provides order/shipment tracking

2. **Resource Routing** (this agent)
   - Integrates with spatial grid
   - Integrates with knowledge/intel
   - Integrates with movement systems

3. **Resource Reservation** (this agent)
   - Integrates with inventory systems
   - Integrates with transport capacity
   - Integrates with service queues

---

## Game-Specific Differences

### Godgame Resource Logistics

**Transport Types**:
- Villager haulers (individuals carrying chunks)
- Wagons/caravans (group transport)
- Boats (water transport)

**Route Types**:
- Overland paths (terrain-based)
- Water routes (rivers, coasts)
- Road networks (constructed)

**Service Nodes**:
- Docks (water transport)
- Markets (trade hubs)
- Storehouses (storage)

### Space4X Resource Logistics

**Transport Types**:
- Carriers (ship-based transport)
- Convoys (fleet transport)
- Freighters (dedicated cargo ships)

**Route Types**:
- Hyperlanes (jump routes)
- Gate networks (wormhole gates)
- Direct routes (sub-light)

**Service Nodes**:
- Stations (docking facilities)
- Refineries (processing)
- Shipyards (construction)

---

## System Ordering

**UpdateInGroup**: `SimulationSystemGroup`

**Order**:
1. `ResourceLogisticsOrderGenerationSystem` - Generate orders from demand
2. `ResourceLogisticsPlanningSystem` - Plan routes and consolidate orders
3. `ResourceReservationSystem` - Reserve inventory/capacity/services
4. `ResourceLogisticsDispatchSystem` - Assign transports and create shipments
5. `ResourceRoutingSystem` - Calculate/update routes
6. `ResourceRoutingRerouteSystem` - Handle rerouting on intel/hazards
7. `ServiceNodeSystem` - Process service queues
8. `ResourceLogisticsDeliverySystem` - Complete deliveries and release reservations

---

## Performance Requirements

### Scale Targets
- **Max Orders**: 1,000 active orders
- **Max Shipments**: 500 active shipments
- **Max Routes**: 10,000 cached routes
- **Max Reservations**: 2,000 active reservations
- **Max Service Queues**: 100 service nodes

### Performance Contracts
- No unbounded scans (use spatial indexing)
- Route calculations cached (invalidate on topology change)
- Reservation checks batched (not per-entity)
- Service queue processing throttled (not every tick)

### Burst Compatibility
- All systems must be `[BurstCompile]`
- Use `NativeHashMap` for lookups
- Use `NativeList` for temporary collections
- Avoid managed types in hot paths

---

## Testing Requirements

### Unit Tests

1. **Order Generation**
   - Test order creation from construction sites
   - Test order creation from storehouse thresholds
   - Test order consolidation logic

2. **Route Calculation**
   - Test route finding between nodes
   - Test cost term application
   - Test route caching
   - Test rerouting logic

3. **Reservation Management**
   - Test inventory reservation
   - Test capacity reservation
   - Test service reservation
   - Test reservation expiry
   - Test reservation release

### Integration Tests

1. **End-to-End Logistics**
   - Create order → plan route → reserve → dispatch → deliver
   - Test order cancellation → reservation release
   - Test route disruption → rerouting

2. **System Integration**
   - Test integration with Resource Registry
   - Test integration with Storehouse Systems
   - Test integration with Construction Systems
   - Test integration with Transport Systems

---

## Reference Documentation

- `Docs/Concepts/Core/Resource_Logistics_And_Transport.md` - Core logistics system specification
- `Runtime/Logistics/Components/LogisticsJobComponents.cs` - Existing logistics job components
- `Runtime/Runtime/ResourceComponents.cs` - Existing resource components
- `Runtime/Systems/StorehouseSystems.cs` - Existing storehouse systems
- `Docs/Mechanics/HaulLoop.md` - Haul loop mechanics (Space4X)
- `godgame/Docs/Concepts/Economy/Trade_And_Logistics_Chunk4.md` - Trade & logistics (Godgame)
- `Docs/Audit/Outstanding_Stubs_Audit.md` - Section 5 (Power, Resources, Production stubs)

---

## Implementation Notes

1. **Store-Backed Design**: Use stores (NodeStore, ContainerStore, BatchStore, OrderStore, ShipmentStore, RouteStore) for authoritative data, ECS entities for physical representation
2. **Stable IDs**: Use 64-bit stable IDs for all logistics primitives (NodeId, ContainerId, BatchId, OrderId, ShipmentId, RouteId)
3. **Knowledge-Driven**: Routes consider intel/knowledge; missing intel = higher risk
4. **Policy-Driven**: Behavior profiles control order generation, planning, routing, loading, dispatch, reroute, failure, reservation policies
5. **Deterministic**: Systems must be deterministic (same input → same output)
6. **Performance-First**: All queries spatially bucketed, cached, or capped

---

## Dependencies

### Existing Components
- `LogisticsJob` (`LogisticsJobComponents.cs`)
- `ResourceTypeId`, `ResourceSource`, `ResourceChunk` (`ResourceComponents.cs`)
- `StorehouseConfig`, `StorehouseInventory` (`ResourceComponents.cs`)
- `ResourceJobReservation`, `StorehouseJobReservation` (`ResourceComponents.cs`)

### Required Systems
- Spatial grid system (for node discovery)
- Knowledge/intel system (for hazard awareness)
- Faction relations (for border access)
- Transport/movement system (for shipment execution)

---

## Success Criteria

✅ Resource logistics system generates orders from demand  
✅ Orders are planned with routes and consolidated  
✅ Resources are reserved to prevent double-spending  
✅ Shipments are created and tracked  
✅ Routes are calculated with cost terms  
✅ Routes are cached and invalidated appropriately  
✅ Rerouting works on intel/hazard updates  
✅ Service nodes manage queues and congestion  
✅ Reservations expire and release correctly  
✅ System integrates with existing resource/storehouse systems  
✅ System supports both Godgame and Space4X  
✅ System is performant and Burst-compatible  
✅ System is deterministic

