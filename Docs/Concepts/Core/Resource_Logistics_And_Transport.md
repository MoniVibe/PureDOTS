# Resource Logistics and Transport System (Locked Core Model)

**Status**: Locked (Core Model v1)  
**Category**: Core / Economy / Logistics  
**Related Systems**: Inventory, Trade/Contracts, Movement/Pathing, Forces/Combat, Economy/Pricing, Comms/Intel

---

## 1. Purpose

Provide a **game-agnostic logistics kernel** that supports:
- Physical, interactable transports (caravans / convoys / cargo ships / fleets / haulers).
- Heterogeneous endpoints (tile cells, districts, settlements, stations, warehouses, entity inventories).
- Resource lots with **quality, rarity, decay, legality, ownership**.
- Multi-hop supply chains, congestion/throughput, disruption, and rerouting based on knowledge/comms.
- Pluggable behavior: push/pull/market, planned vs emergent routing, risk/secrecy, consolidation, etc.

The kernel defines **data + execution contracts**; gameplay meaning is injected via policies and profiles.

---

## 2. Design Principles

1. **Stable IDs + Stores** for scale; ECS entities are *representations* when needed.
2. **Data-first extensibility**: new behaviors come from config/profiles; optional compiled hooks are registered (no hard coupling).
3. **Bounded complexity**: every potentially unbounded operation must have caps, batching, caching, or hierarchical fallback.
4. **Physical by default**: transports exist as entities and can be interacted with; abstraction is supported as an optional representation layer.
5. **Knowledge-driven decisions**: "cut supply line" and risk is mediated by comms/intel; ignorance is allowed and meaningful.

---

## 3. Core Primitives (First-Class Concepts)

The kernel standardizes these primitives as stable IDs and records:

- **NodeId**: any logistics endpoint (world position node, facility node, mobile node, inventory node).
- **ContainerId**: a storage module/compartment (ship hold module, warehouse bay, silo, passenger pod, fuel tank).
- **BatchId**: a resource lot (type + attribute bundle).
- **OrderId**: intent to move quantities under constraints.
- **ShipmentId**: execution instance fulfilling one or more orders.
- **RouteId**: planned itinerary expressed as edge sequence.

All are backed by stores (SoA where possible).

---

## 4. IDs, Stores, and Entities

### 4.1 Stable IDs
Use **64-bit stable IDs** with generation (or GUID-like) semantics:
- IDs remain stable across save/load.
- Stores maintain `Generation` to detect stale references.

### 4.2 Stores vs ECS Entities
- **Authoritative data** lives in stores: NodeStore, ContainerStore, BatchStore, OrderStore, ShipmentStore, RouteStore, ServiceStore, KnowledgeStore.
- ECS entities are used for:
  - **Physical transports** (caravan/ship/fleet entity).
  - **Interactables/presentation** (rendering, selection, combat, boarding, theft).
  - **High-frequency movement & physics** (when applicable).
- Entities reference store IDs (e.g., `TransportRef { ShipmentId, NodeId, BehaviorProfileId }`).

This keeps the kernel usable at extreme scale without forcing "everything is an entity".

---

## 5. Nodes (Endpoints) — NodeId + Resolver/Adapter

### 5.1 NodeStore Record
Each NodeId resolves to:
- `NodeKind` (TileCell, District, Settlement, Station, Warehouse, MobileTransport, EntityInventory, etc.)
- `Owner/FactionId`
- `SpatialKey` (grid cell / sector / system)
- `InventoryHandle` (0..N containers)
- `ServicesHandle` (docks, loading, customs, refuel, repair, gatejump, etc.)
- `GraphKey` (routing topology membership)

### 5.2 NodeResolver/Adapter
A resolver layer maps NodeId to actual backend:
- Tile/district adapters
- Facility adapters
- Entity inventory adapters
- Mobile transport adapters

This is the mechanism that allows "all of the above" endpoints without special-casing everywhere.

---

## 6. Resources — Hybrid Stacks with Batch Attributes

### 6.1 BatchStore
A BatchId represents a **lot** with shared attributes:
- `ResourceTypeId`
- `QualityGrade` (quantized)
- `RarityGrade` (quantized or static from type)
- `DecayQ` (quantized; plus optional `DecayProfileId`)
- `LegalityFlags` (bitmask)
- `OwnerId`
- `TagMask` (category/hazard/compatibility tags)

### 6.2 Inventory Entries
Inventories store `(BatchId, Quantity)` in containers.

### 6.3 Split/Merge Rules
- Split occurs when any attribute diverges (ownership, legality, quality/decay step, etc.).
- Merge occurs only when attributes are identical *and* container mixing rules allow it.
- Quantization is required to prevent "infinite fragmentation" of lots.

---

## 7. Containers & Inventory

### 7.1 ContainerStore
ContainerId record:
- `ParentNodeId`
- `ContainerTypeId`
- `CapacityMass`, `CapacityVolume`, `SlotCount` (optional)
- `AllowedTagMask`
- `MixingPolicyId` (e.g., bulk-mix allowed; discrete-only; hazardous isolation; personnel-only)
- `SpecialFacilityFlags` (exotics containment, luxury vault, cryo, etc.)
- `Load/UnloadRate` (local throughput contribution)

### 7.2 Inventory Representation
Default: **container-centric contents**:
- Each container has a content list of `(BatchId, qty)` entries.
- Container content is stored in a central structure (store-backed), not forced into ECS buffers.

Optional: mirror a subset to ECS buffers for presentation/interaction (never authoritative).

---

## 8. Orders, Shipments, and Reservations

### 8.1 OrderStore (Intent)
Order record:
- `SourceNodeId`, `DestNodeId` (may be a set/selector via policy)
- `RequestedBatches` (type + constraints + quantity)
- Constraints:
  - legality requirements
  - secrecy requirements
  - max risk
  - max route length/cost
  - delivery priority class
  - required services (customs, refrigeration facility, refuel points, etc.)
- `BehaviorProfileId` (planner behavior)
- State: `Created -> Planned -> Reserved -> Dispatched -> InTransit -> Delivered/Failed/Cancelled`

### 8.2 ShipmentStore (Execution)
Shipment record:
- `AssignedTransportRef` (entity id optional; can be abstract)
- `OrderRefs[]` (one-to-many, many-to-one supported)
- `AllocatedCargo[]` (containers + batch allocations)
- `RouteId`
- `RepresentationMode`:
  - `Physical` (default; entity exists)
  - `Abstract` (optional; may materialize on triggers)
- State machine mirrors the transport lifecycle.

### 8.3 Reservations (Kernel Feature)
Reservations are first-class to avoid double-spend and overcommit:
- **InventoryReservation**: reserve quantities at source.
- **CapacityReservation**: reserve container capacity on transport.
- **ServiceReservation**: reserve throughput/slot time at service nodes (docks, loaders, gates).

Reservations have TTL/cancellation policies implemented via policy hooks (not hardcoded gameplay).

---

## 9. Routing — Edge-Based Graph with Cost-Term Stack

### 9.1 Graph Model
- Routing is performed on an **Edge graph**:
  - EdgeId connects NodeId↔NodeId with traversal capabilities.
  - Edges carry static data (distance, mode, base cost) and dynamic overlays.

### 9.2 Dynamic Overlays (EdgeState)
Edge states can change:
- control/ownership restrictions
- hazard/risk levels
- interdiction likelihood
- congestion multipliers
- seasonal/timed modifiers

### 9.3 Cost-Term Stack (Extensible)
Total edge cost is the sum of cost terms:
- base traversal
- risk term (from hazard feed + profile)
- legality/contraband term
- border/politics term
- stealth term
- congestion/throughput term
- season/event term

Each term is:
- data-driven (weights in BehaviorProfile)
- optionally implemented by a registered compiled evaluator (extension point)

### 9.4 Route Cache
Route cache key includes:
- origin/destination graph keys
- behavior profile id
- legality mask / cargo tags
- knowledge version id (per faction)
- topology version id

Cache invalidates on:
- topology change
- significant overlay change
- knowledge version increment

---

## 10. Services & Congestion — Generic Service Nodes

Any Node may expose services. A service is defined by:
- `ServiceTypeId` (Dock, Load, Unload, Customs, Refuel, Repair, GateJump, etc.)
- `SlotCapacity` (parallelism)
- `ThroughputBudget` (units per execution step)
- `QueuePolicyId` (priority, bidding, faction rules, corruption/bribes if desired)

Congestion is modeled as **both**:
- queueing (slots)
- throughput limitation

---

## 11. Comms & Knowledge (Intel-Mediated Decisions)

### 11.1 Knowledge Scope
Default knowledge is **per-faction**:
- `FactionKnowledgeStore` holds beliefs about nodes/edges/regions.

Optional overlay:
- `EntityKnowledgeOverlay` for special actors (spies, smugglers, scouts).

### 11.2 Intel Events
Knowledge updates arrive as `IntelEvents` (source can be any comms backend):
- hazard detected / hazard cleared
- route interdicted
- node compromised
- border policy changed
- comm blackout / relay destroyed

### 11.3 Hazard Feed Interface
Kernel consumes normalized hazard info through a unified interface, regardless of whether the source is:
- edge-based
- region-based
- node-based

Policy decides response thresholds:
- reroute
- continue
- request escort
- abort/return

If intel is missing, shipments may proceed into danger (supported by design).

---

## 12. Behavior Profiles & Policy Modules

### 12.1 BehaviorProfile (Data-First)
BehaviorProfile is a data blob:
- weights: risk vs cost vs speed vs secrecy vs compliance
- toggles: allow consolidation, allow splitting, strict reservation, reroute aggressiveness
- caps: max hops, max detour, max queue wait, max stockout tolerance
- strategy ids: optional compiled "scorers" for special logic

### 12.2 Policy Modules (Extension Points)
Kernel calls policy interfaces; projects provide implementations:
- **Order generation policy** (push/pull/market/contract)
- **Planning policy** (consolidation/splitting, hub preference, secrecy)
- **Routing policy** (cost-term weights + constraints)
- **Loading policy** (container selection, mixing, facility requirements)
- **Dispatch policy** (transport selection, convoy formation)
- **Reroute policy** (intel response)
- **Failure policy** (loss, theft, abandonment, recovery, insurance)
- **Reservation policy** (when to reserve/release, partial reserve rules)

All policies operate on store data; they are swappable per faction/actor/profile.

---

## 13. Execution Order (Kernel Update Contract)

1. **Ingest Events**
   - inventory deltas, production/consumption ticks, contract changes
   - intel/comms events
   - topology/service changes

2. **Update Knowledge**
   - apply intel events → increment knowledge versions

3. **Recompute Demands**
   - node policies evaluate stockpiles and thresholds → create/update orders

4. **Plan Orders**
   - choose sources/destinations (if selectors)
   - consolidation/splitting decisions
   - compute/refresh routes (cache-aware)
   - schedule required services

5. **Reserve**
   - reserve inventory, capacity, services (partial reservations allowed)

6. **Dispatch**
   - assign or spawn transport representation
   - allocate containers and cargo manifests
   - finalize shipment state

7. **Transit & Services**
   - movement system advances physical entities along route edges
   - service nodes process queues: dock/load/unload/customs/refuel/etc.

8. **Delivery & Settlement**
   - commit inventory transfers
   - release reservations
   - update economic signals (optional integration)

9. **Failure & Reroute**
   - detect losses, interdiction, refusal, comm blackouts
   - apply reroute policy or fail shipment/order

10. **Telemetry**
   - emit explainable events and counters for debugging/UI

---

## 14. Save/Load and Determinism

### 14.1 Save/Load
- Stores serialize by stable IDs.
- Schema version stamps on store sections allow additive fields and modded extensions.

### 14.2 Determinism (Chosen Default)
- **Determinism Tier**: deterministic within a run (same build/settings) is the default.
- Optional strict mode can be layered later, but the core architecture does not require cross-platform lockstep.

This maximizes performance and flexibility while keeping reproducibility workable.

---

## 15. Performance Contracts (Hard Rules)

- No unbounded all-to-all scans; all queries are spatially bucketed, cached, or capped.
- No per-item entities for cargo; cargo is lot-based (BatchId) and stored in containers.
- Quantize churny attributes (quality/decay) to prevent batch fragmentation explosions.
- Avoid structural churn: store-backed lists rather than archetype-changing entity buffers.
- Route computations must be cache-aware; dynamic overlays update via versioning.
- Policies must expose caps (max hops, max reroutes, max consolidation size).

---

## 16. Introspection Contract (for tooltips and debugging)

The kernel must be able to answer, via cheap queries:
- "Why is this node short on X?" (stockout reasons)
- "What shipments are inbound/outbound?" (with ETA proxy from route state)
- "What constraint blocked this order?" (legality, facility missing, no capacity, no route, reservation contention)
- "Why did the planner choose this route?" (top cost terms + knowledge snapshot id)
- "What is congested?" (service queues and throughput saturation)

Expose this as structured records/events suitable for UI tooltips and debug overlays.

---

## 17. Resolved Design Decisions (from stub)

- [x] Transport representation: **physical entities by default**, optional abstract representation layer.
- [x] Route planning: **edge-based routing** with cost-term stack and caching.
- [x] Supply line tracking: **throughput over edges + active shipments** (derivable from ShipmentStore + Route edges).
- [x] Capacity model: **mass + volume + modular container slots**, with mixing constraints.
- [x] Speed model: **vessel/module-dependent**, load-adjusted via data curves.
- [x] Optimization strategy: **believable heuristics** via behavior profiles + knowledge/comms.

---

## 18. Notes for Integration

- Movement provides traversal along edges; logistics provides manifests and assignments.
- Combat/forces can publish hazard/interdiction intel into the hazard feed.
- Economy/pricing can consume fulfillment/shortage signals and publish demand signals into order generation.
- Trade/contracts can create orders and define legality/ownership rules.
