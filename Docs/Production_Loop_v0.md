# Production Loop v0 (Shared PureDOTS)

**Purpose**: A minimal, shared extraction -> production loop usable by Space4x and Godgame, headless-first, deterministic, and telemetry-friendly.

---

## Goals (v0)

- Resource extraction from nodes -> stockpiles
- Crafting recipes (inputs -> outputs) with simple queues
- Facilities that host production slots
- Clear telemetry for throughput, idle time, and failure reasons
- Scenario-friendly: all steps drivable by JSON scenarios

Non-goals (v0): trade, pricing, logistics networks, or advanced workforce AI.

---

## Shared Data Model (PureDOTS)

### Core types
- **ResourceTypeId**: already in PureDOTS resource system
- **RecipeId**: simple integer or hash
- **FacilityTypeId**: defines allowed recipes + slots
- **StockpileId**: optional link to a stockpile entity (or use existing ResourceStore)

### Components (suggested)
- `ProductionFacility` (FacilityTypeId, SlotCount, PowerCost)
- `ProductionSlot` (FacilityEntity, SlotIndex, RecipeId, RemainingTicks, State)
- `ProductionQueueItem` (RecipeId, Quantity, Priority)
- `ProductionQueueBuffer` (buffer of ProductionQueueItem)
- `ProductionInput` (RecipeId -> required ResourceTypeId + amount)
- `ProductionOutput` (RecipeId -> output ResourceTypeId + amount)
- `ResourceStockpileRef` (links to stockpile entity or registry entry)
- `ExtractionNode` (ResourceTypeId, YieldPerTick, DepleteRate?)
- `Extractor` (Rate, TargetNode, OutputStockpile)

### States
- `ProductionSlotState`: Idle, WaitingForInputs, Running, OutputBlocked
- `ProductionFailureReason`: None, MissingInputs, StockpileFull, PowerOff

---

## Systems (v0)

1) **ExtractionSystem**
   - Reads `Extractor + ExtractionNode` and deposits into Stockpile
   - Emits telemetry: extracted_per_tick, stockpile_level

2) **ProductionQueueSystem**
   - Pops queue items into available `ProductionSlot`
   - Marks slot `WaitingForInputs` until inputs are reserved

3) **ProductionInputReserveSystem**
   - Checks stockpile for input resources
   - Consumes inputs and starts `RemainingTicks`

4) **ProductionTickSystem**
   - Decrements `RemainingTicks`
   - On completion, spawns output into stockpile

5) **ProductionTelemetrySystem**
   - Emits: slot_utilization, queue_depth, throughput, failure_reason

---

## Telemetry (v0)

- `prod.throughput.<resource>`
- `prod.queue_depth.<facility>`
- `prod.slot_utilization.<facility>`
- `prod.failure_reason.<facility>` (enum)
- `extract.rate.<resource>`
- `stockpile.level.<resource>`

---

## Scenario Hooks (v0)

Scenario actions to drive headless tests:
- spawn extraction nodes
- spawn facility with recipe list
- enqueue production items
- set stockpile levels
- run for N ticks

Validation expectations:
- output resource count >= expected
- slot utilization >= threshold
- queue drains within time

---

## Space4x Adapter (v0)

- Map **modules / ship parts** to recipes
- Facilities: carrier bay, station workshop
- Stockpile: carrier cargo or station storage
- Optional: gate production by crew skill or facility tier

---

## Godgame Adapter (v0)

- Minimal workshop: 1-2 recipes (e.g., tools, building materials)
- Stockpile: village storehouse
- Input: gathered resources (wood/stone/food)
- Output used to unlock simple construction

---

## Implementation Phases

**Phase A (PureDOTS shared)**
- Add core components + systems + telemetry
- Scenario actions for enqueue/stockpile

**Phase B (Space4x)**
- Add recipes for modules/ship parts
- Hook to existing refit/repair pipeline

**Phase C (Godgame)**
- Add workshop entity and 1-2 recipes
- Hook to construction cost consumption

---

## Open Questions

- Should production consume time in `TimeState` or fixed ticks?
- Do we reuse existing resource storage components or define a unified stockpile?
- Should queues be per-facility or per-region? (v0: per-facility)

---

## Acceptance (v0)

- Scenario: enqueue recipe -> output appears in stockpile within expected ticks
- Telemetry shows throughput + queue depth + utilization
- Deterministic results across runs
