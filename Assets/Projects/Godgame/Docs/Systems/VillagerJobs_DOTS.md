# Villager Job Loop – Pure DOTS Design

**Owner:** Agent Beta  
**TruthSources:** `godgame/truthsources/Villagers_Jobs.md`, `VillagerTruth.md`, `TimeTruth.md`, `TimeEngine_Contract.md`, `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`, `Docs/DesignNotes/RewindPatterns.md`  
**Related Plans:** `ResourceRegistryPlan.md`, `Docs/TODO/SystemIntegration_TODO.md`, `Docs/DesignNotes/SystemExecutionOrder.md`

---

## Component Model

### Core Villager State
- `VillagerJob` (updated): encapsulates high-level job metadata (job type, current phase, productivity scalar, active ticket id, last state change tick). Phases map 1:1 to TruthSource graph: `Idle`, `Assigned`, `Gathering`, `Delivering`, `Completed`, `Interrupted`.
- `VillagerJobTicket` *(new `IComponentData`)*: deterministic ticket issued by the assignment pass.
  - Fields: `uint TicketId`, `VillagerJob.JobType JobType`, `ushort ResourceTypeIndex`, `Entity ResourceEntity`, `Entity StorehouseEntity`, `byte Priority`, `byte Phase`, `float ReservedUnits`, `uint AssignedTick`, `uint LastProgressTick`.
  - `Priority` mirrors Truth priority rules; `Phase` is authoritative for job loop state machine.
- `VillagerJobProgress` *(new `IComponentData`)*: accumulates gather/deliver progress per tick (`float Gathered`, `float Delivered`, `float TimeInPhase`, `uint LastUpdateTick`).
- `VillagerJobCarryItem` *(new `IBufferElementData`)*: virtual carry inventory driven by `ResourceTypeIndex`/`Amount`. Acts as the “virtual ownership” store required by the TruthSource. This buffer replaces ad-hoc use of `VillagerInventoryItem` inside the job loop, but the latter stays for generic inventory consumers.
- `VillagerJobHistorySample` *(new `IBufferElementData`)*: time-aware buffer storing `(Tick, TicketId, Phase, Gathered, Delivered, float3 TargetPosition)` for rewind playback.

### Resource & Storehouse Reservations
- `ResourceJobReservation` *(new `IComponentData` on resource sources)*: `{ byte ActiveTickets; byte PendingTickets; float ReservedUnits; uint LastMutationTick; }`. Keeps registry fields authoritative and constrains concurrent workers.
- `ResourceActiveTicket` *(new `IBufferElementData`)*: `{ Entity Villager; uint TicketId; float ReservedUnits; }` enabling deterministic release during interrupts or rewinds.
- `StorehouseJobReservation` *(new `IComponentData` on storehouses)*: `{ float ReservedCapacity; uint LastMutationTick; }` aggregates total reserved capacity from villager deliveries.
- `StorehouseReservationItem` *(new `IBufferElementData`)*: `{ ushort ResourceTypeIndex; float Reserved; }` matches `StorehouseRegistryCapacitySummary` for per-type reconciliation.

### Event Surface
- `VillagerJobEvent` *(new `IBufferElementData` on singleton `VillagerJobEventStream`)* exposes TruthSource events (`JobAssigned`, `JobProgress`, `JobCompleted`, `JobInterrupted`) with payload `{ uint Tick; Entity Villager; VillagerJobEventType Type; ushort ResourceTypeIndex; float Amount; uint TicketId; }`.
- `VillagerJobEventType` *(new enum)*: strongly typed event ids for downstream UI/logging.

### Time Integration
- `ITimeAware` *(new interface per TimeEngine contract)*: `OnTick(uint)`, `Save(ref TimeStreamWriter)`, `Load(ref TimeStreamReader)`, `OnRewindStart()`, `OnRewindEnd()`.
- `VillagerJobTimeAdapter` *(new `ISystem` tagged with `ITimeAware` implementation)*: serializes job ticket, progress, and reservation state into deterministic streams owned by the `TimeEngine`.
- `VillagerJobPlaybackGuardTag` *(alias of existing `PlaybackGuardTag` path)*: reused to gate systems while rewinding.

---

## System Graph & Ordering

### FixedStepSimulationSystemGroup
1. **VillagerJobRequestSystem** *(new)*  
   - Group: `FixedStepSimulationSystemGroup` → `VillagerJobFixedStepGroup` (new `ComponentSystemGroup`).  
   - Collects villagers needing work into a deterministic queue (filters `VillagerJob.Phase == Idle/Interrupted` and honours `VillagerAvailability`).  
   - Populates `VillagerJobRequestQueue` buffer (singleton) and clears stale tickets via command buffer.

2. **VillagerJobAssignmentSystem** *(refactored)*  
   - Consumes request queue, scans `ResourceRegistry` buffer ordered by (priority → available units → distance).  
   - Claims resources by incrementing `ResourceJobReservation` and writing `VillagerJobTicket`.  
   - Emits `JobAssigned` events via `BeginFixedStepSimulationEntityCommandBufferSystem`.

3. **VillagerJobExecutionSystem** *(new)*  
   - Processes active tickets in `Gathering` phase.  
   - Consumes deterministic time step (`TimeState.FixedDeltaTime`), updates `VillagerJobProgress`, `VillagerJobCarryItem`, and decrements `ResourceSourceState`.  
   - Uses `DeterministicCommandBuffer` wrapper to enqueue `JobProgress` events.

4. **VillagerJobDeliverySystem** *(new)*  
   - Runs after execution, switches tickets to `Delivering` when carry buffer exceeds threshold or resource depleted.  
   - Uses `StorehouseRegistry` to choose drop-off target, reserves capacity through `StorehouseJobReservation`/`StorehouseReservationItem`.  
   - Applies deposits using existing `ResourceDepositSystem` via new adapter buffer (`VillagerJobDeliveryCommand`) consumed by deposit system after registries refresh.

5. **VillagerJobInterruptSystem** *(new)*  
   - Runs last in fixed step group; listens for priority overrides (player claims flagged via `ResourceRegistryEntry.ClaimFlags`).  
   - Cancels/pauses tickets, releases reservations, emits `JobInterrupted`.

### SimulationSystemGroup (RecordSimulationSystemGroup)
6. **VillagerJobEventFlushSystem** *(new)*  
   - Converts deterministic event buffer into presentation-friendly queues (UI, analytics).  
   - Lives in `RecordSimulationSystemGroup` after `FixedStepSimulationSystemGroup` so events are finalized before other Simulation consumers.

7. **VillagerAISystem** *(existing)*  
   - Consumes `VillagerJob` phases (Working when `Phase == Gathering/Delivering`). Ordering already `[UpdateAfter(typeof(VillagerJobAssignmentSystem))]`; will be updated to target new job group.

### Rewind Hooks
- **VillagerJobHistorySystem** *(new, `HistorySystemGroup`)*: records `VillagerJobHistorySample` at cadence derived from `HistorySettings`.
- **VillagerJobPlaybackSystem** *(new, `PlaybackSimulationSystemGroup`)*: during rewind playback, restores `VillagerJob`, `VillagerJobTicket`, reservations, and carry buffers from history samples. Adds/removes `PlaybackGuardTag` compliance.
- `VillagerJobTimeAdapter` ties into `TimeEngine` Save/Load for branch replay; `Save` writes compact ticket/progress/reservation data, `Load` restores before playback resumes.

Ordering Summary:
```
[FixedStepSimulation]
  VillagerJobFixedStepGroup
    1. VillagerJobRequestSystem
    2. VillagerJobAssignmentSystem
    3. VillagerJobExecutionSystem
    4. VillagerJobDeliverySystem
    5. VillagerJobInterruptSystem

[Simulation]
  RecordSimulationSystemGroup
    - VillagerJobEventFlushSystem
    - VillagerSystemGroup (VillagerAI, Targeting, Movement, etc.)

[History]
  - VillagerJobHistorySystem

[Playback]
  - VillagerJobPlaybackSystem
```

---

## Registry Integration & Storehouse Updates

- **Consumption**
  - `VillagerJobAssignmentSystem` reads `ResourceRegistry` buffer (including new `ActiveTickets`, `ClaimFlags`, `LastMutationTick`).  
  - `VillagerJobDeliverySystem` reads `StorehouseRegistry` buffer (`TypeSummaries`, `LastMutationTick`) to choose capacity-aware drop-off targets without querying `EntityManager` directly.
  - `VillagerAISystem`/future gameplay systems can query the new `VillagerRegistry` buffer for availability, job phase, and ticket data without touching `EntityManager`.
  - Shared AI pipeline (`AISystemGroup`) exposes sensor/utility/steering modules; villagers opt-in via `AISensorConfig` + `VillagerAIUtilityBinding` to translate generic actions into villager-specific goals.

- **Reservation Updates**
  - Resource reservations mutate `ResourceJobReservation` & `ResourceActiveTicket` components; `ResourceRegistrySystem` mirrors them into buffer fields (`ActiveTickets`, `ClaimFlags`, `LastMutationTick`) during its scan.
  - Storehouse reservations update `StorehouseReservationItem` buffers; `StorehouseRegistrySystem` aggregates into `TypeSummaries` and updates `LastMutationTick`.
  - Villager availability updates automatically populate `VillagerRegistryEntry.AvailabilityFlags` (bitmask defined in `VillagerAvailabilityFlags`).

- **TruthSource Invariants**
  - Single write path: all storehouse totals modified through existing `StorehouseInventoryItem` buffers. Deliveries are emitted as `VillagerJobDeliveryCommand` dispatches consumed by `ResourceDepositSystem`, ensuring no direct mutation from job systems.
  - Player priority: `ClaimFlags` ensure villagers yield immediately when a storehouse/resource is claimed by player interactions (Interrupt system releases ticket and emits event).
  - Virtual carry: only `VillagerJobCarryItem` changes until deposit confirms via `ResourceDepositSystem`; no piles or resource totals mutated directly.
  - Legacy `StorehouseAPI` helpers are removed; consumers must use the registries + reservation components for Burst-safe interactions.

### Query & Reservation APIs

- **ResourceRegistry** (`ResourceRegistryEntry`)
  - `ActiveTickets` / `ClaimFlags` surface reservations. `ResourceJobReservation` is the authoritative component; mutate it and the registry buffer updates next frame.
  - `LastMutationTick` identifies latest structural change for deterministic tie-breaking.
- **StorehouseRegistry** (`StorehouseRegistryEntry`)
  - `TypeSummaries` (per-storehouse `FixedList32Bytes`) summarize capacity / stored / reserved units per resource type.
  - `StorehouseJobReservation` + `StorehouseReservationItem` hold the authoritative reservation totals. Update these components to reserve or release capacity.
- **VillagerRegistry** (`VillagerRegistryEntry`)
  - Provides `VillagerId`, `FactionId`, world position, job type/phase, ticket id, current target resource type (`CurrentResourceTypeIndex`), and availability flags.
  - Use `VillagerAvailabilityFlags.Available/Reserved` to filter workers without rereading component data.

#### Reserving Capacity Example

```csharp
// Reserve 10 wood units at a storehouse
var reservation = SystemAPI.GetComponentRW<StorehouseJobReservation>(storehouseEntity);
reservation.ValueRW.ReservedCapacity += 10f;
reservation.ValueRW.LastMutationTick = timeState.Tick;

var items = SystemAPI.GetBuffer<StorehouseReservationItem>(storehouseEntity);
var found = false;
for (int i = 0; i < items.Length; i++)
{
    if (items[i].ResourceTypeIndex == woodIndex)
    {
        var item = items[i];
        item.Reserved += 10f;
        items[i] = item;
        found = true;
        break;
    }
}
if (!found)
{
    items.Add(new StorehouseReservationItem { ResourceTypeIndex = woodIndex, Reserved = 10f });
}
```

Registry systems fold these component changes into their buffers automatically on the next update, so consumer systems only need to read from the buffers for fast Burst-compatible queries.

---

## Testing Strategy

### Unit & Integration Tests
1. **VillagerJobAssignmentTests.cs**  
   - Verifies request queue creation, resource selection (nearest, priority aware), ticket issuance, reservation counts, and event emission.
2. **VillagerJobExecutionTests.cs**  
   - Simulates gather ticks; ensures deterministic progress, resource depletion handling, and carry buffer fills while respecting productivity & needs modifiers.
3. **VillagerJobDeliveryTests.cs**  
   - Validates storehouse selection, capacity clamping, reservation bookkeeping, and delivery event generation.
4. **VillagerJobInterruptTests.cs**  
   - Covers player claim interrupts, resource depletion interrupts, and priority hand-offs between villagers.

### Rewind & Branching
- `VillagerJobRewindTests.cs`
  - Record a gather → deliver → idle loop; step back mid-gather and confirm state restoration.  
  - Hold rewind, branch replay, then resume record; verify ticket ids and reservations remain deterministic across branch divergence.  
  - Validate `ITimeAware.Save/Load` path serializes/deserializes reservations and carry buffers.

### Cross-System Scenarios
- Tests orchestrate gather→deliver→idle loops using TimeEngine APIs (`TimeEngine.GoTo`, `StepBack`, `Pause`) instead of `Time.timeScale`.  
- Priority conflict tests inject mock player claims via `ClaimFlags` and assert villagers yield within same fixed step.
- Ensure adapters keep `ResourceDepositSystem`/`StorehouseInventorySystem` working unchanged (looped tests assert totals via registries rather than internal buffers).

### Test Utilities
- Deterministic command buffer harness to inspect `VillagerJobEvent` streams per tick.  
- Helper for seeding resource/storehouse registries with blob-backed `ResourceTypeIndex` to avoid string comparisons in tests.

---

## Extension Points
- `VillagerJob.JobType` remains extensible; systems branch on `JobType` with `switch` statements and provide virtual methods (`ResolveAssignmentStrategy`, `ResolveDeliveryStrategy`) for future job families.
- `VillagerJobExecutionSystem` exposes partial method hooks (`ApplyJobSpecificProgress`) so specialized jobs (e.g., Builder) can inject logic without duplicating base gather loop.
- `VillagerJobEventFlushSystem` publishes events through `NativeStream`, enabling other domains (UI, analytics) to opt-in without coupling to job systems.

---

## Open Risks / Follow-ups
- Need coordination with presentation/UI teams for event consumption; placeholder `VillagerJobEventView` interface will be documented in hand-off.  
- Requires authoring step to tag villager prefabs with `RewindableTag` + allocate job buffers; baker update tracked separately.  
- Additional profiling once crowd sizes increase to confirm fixed-list summaries in `StorehouseRegistryEntry` stay within perf budgets (<32 resource types per storehouse assumed).
