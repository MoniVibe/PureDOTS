# Production/Consumption Accounting (Actual vs Reference, Event-Driven)

**Status**: Locked (architecture + UX contract)  
**Category**: Architecture / Economy / Telemetry  
**Applies To**: Godgame, Space4X, shared PureDOTS  

Goal:
> Get the “mathy factory-game feel” (DSP/Factorio/Satisfactory stats + bottlenecks) **without** paying per-entity scan costs.  
> Do this by treating flow as **events** and aggregating hierarchically, with two views everywhere: **Actual** and **Reference/Capacity**.

This is an instrumentation + accounting contract, not a mandate that “every machine must be simulated at full detail.”

Related:
- `Docs/Architecture/Scalability_Contract.md` (hot vs cold, resident vs virtual, anti-pattern bans)
- `Docs/Architecture/Performance_Optimization_Patterns.md` (strict phase order + budgets)
- `Docs/Concepts/Core/Resource_Logistics_And_Transport.md` (logistics kernel; shipments/routes)
- `Packages/com.moni.puredots/Runtime/Runtime/Resources/SupplyChainComponents.cs` (SupplyStatus, routes, rates)
- `Packages/com.moni.puredots/Runtime/Runtime/Resources/SupplyChainHelpers.cs` (status math helpers)
- `Packages/com.moni.puredots/Runtime/Economy/Wealth/WealthTransactionSystem.cs` (ledger pattern: “change via events”)
- `Packages/com.moni.puredots/Runtime/Runtime/Registry/Aggregates/AggregateComponents.cs` (compressed sim + pseudo-history)

---

## 1) Two views everywhere (the key UX lens)

Every scope exposes **two parallel lenses**:

### 1.1 Actual (what really flowed)
Derived from **events** (“something was produced/consumed”) and therefore:
- cheap,
- deterministic,
- works for resident entities and virtualized aggregates.

### 1.2 Reference / Capacity (what would flow at max)
Derived from **installed capacity** (recipe graph + machine counts + max craft speed + policy modifiers):
- “ideal production/consumption if everything ran perfectly”
- used to compute working ratios and diagnose bottlenecks.

Why both matter:
- High Reference + low Actual consumption ⇒ **starved** (missing inputs / logistics / power / staffing).
- High Reference + low Actual production ⇒ **blocked** (output full, jammed, no storage, halted).

**Never** merge Actual and Reference into one number. Players can’t diagnose.

---

## 2) Canonical IDs + deterministic units

### 2.1 Canonical index
For hot-path accounting, use a single canonical index:
- `ushort ResourceTypeIndex` (or `int` if catalog grows beyond `ushort`)

If the game still uses string IDs in inventories (`FixedString64Bytes ResourceTypeId`), conversion happens at the boundary:
- authoring/config uses strings,
- runtime accounting uses indices.

### 2.2 Units are integers in “smallest unit”
Accounting stores integer quantities in the smallest meaningful unit:
- items, grams, milliliters, joules, etc.

Rationale:
- deterministic,
- cheap,
- no float drift in long histories.

UI can display rates as floats derived from integer sums over windows.

**Recurring mistake to avoid:** storing flows/rates as floats everywhere and then fighting drift + nondeterminism later.

---

## 3) Flow is an event (never a scan)

Producers/consumers emit deltas only when something happens:
- `Produced(resourceId, amount, scopeEntity)`
- `Consumed(resourceId, amount, scopeEntity)`

Optional boundary events (useful for DSP-style import/export):
- `Imported(resourceId, amount, scopeEntity, fromScope)`
- `Exported(resourceId, amount, scopeEntity, toScope)`

Implementation contract:
- emit into per-thread local buffers or per-scope queues during the tick,
- merge at one deterministic reduction point (single owner per scope or deterministic fold).

**Recurring mistake to avoid:** iterating every machine every tick “for stats.”

---

## 4) Hierarchical aggregation (machine → district → planet → empire)

Scopes are entities (or store-backed nodes) that own counters.

### 4.1 Scope ownership
Each producing/consuming actor must map to exactly one “accounting scope” at emission time:
- building/facility emits into village/district scope,
- ship module emits into ship scope,
- colony facilities emit into colony/planet scope.

### 4.2 Upward-only aggregation
Counters fold strictly upward:
- local scope → parent scope → … → top scope

This keeps the graph acyclic and makes reduction deterministic and cheap.

### 4.3 Virtualized aggregates still participate
Compressed groups can still emit and aggregate:
- generate pseudo-events deterministically over a tick interval,
- write pseudo-history entries (see `AggregateComponents.PseudoHistoryEntry`).

---

## 5) Sparse touched IDs (avoid “clear 12k entries per scope per tick”)

Each scope maintains:
- `Produced[resourceId]`, `Consumed[resourceId]` counters for the current tick/window
- a `TouchedIds` list for which resource IDs were modified

Clear/reset only touched IDs, not the whole catalog.

Implementation notes (policy choice):
- Dense arrays are allowed only when the catalog is small and scope count is small.
- At scale, prefer pooled sparse pages keyed by resourceId (still driven by TouchedIds).

**Recurring mistake to avoid:** global dense clears every tick per scope.

---

## 6) History as ring pyramids (Factorio-style windows, bounded storage)

Store rates over selectable time windows without keeping per-tick history forever.

Recommended shape:
- `HistoryShort` (fine bins, small ring)
- `HistoryMid` (coarser bins)
- `HistoryLong` (coarsest bins)

Each bin stores:
- produced amount
- consumed amount
- (optional) imported/exported

UI computes:
- `rate = sum(windowBins) / windowDuration`

Storage rules:
- bounded rings only (no unbounded append),
- per scope: history is either:
  - “all tracked resources” (if small), or
  - top-K / touched-only entries (policy), or
  - local monitors (see §8) when you want deep per-building visibility.

**Recurring mistake to avoid:** raw per-tick history for everything forever.

---

## 7) Reference/Capacity rates (DSP-style “Reference Rate”)

Reference capacity is computed from installed graph, not from runtime events:
- for each facility: recipe IO rates at max speed (plus modifiers)
- fold into the same scopes used for Actual accounting

Expose:
- `ReferenceProducedRate[resourceId]`
- `ReferenceConsumedRate[resourceId]`

Then compute:
- `WorkingRatio = Actual / max(Reference, ε)`

Interpretation:
- low WorkingRatio on consumption: starved inputs
- low WorkingRatio on production: blocked output / downtime

This is the main “factory optimization lens” we want.

---

## 8) Satisfactory-style local monitors (cheap clarity)

Global panels aren’t enough; targeted monitors provide most of the value at low cost.

Optional diagnostic entities/modules:
- **Building productivity monitor**: 0–100% operating fraction vs max over recent window.
- **Belt/pipe throughput monitor**: rolling average throughput history (e.g., 60s).

These monitors:
- track only a tiny set of IDs,
- are opt-in (player placed / debug-enabled),
- and are perfect for “why is this village starving / why is this colony stalling?” without global heavy queries.

---

## 9) Integrate with existing PureDOTS primitives (don’t build parallel stacks)

### 9.1 Supply chain status is derived, not authoritative
Use `SupplyStatus` and `SupplyChainHelpers` as a derived operational view:
- derive income/consumption rates from Actual windows,
- use Reference to interpret deficits (starved vs blocked),
- keep the authoritative flow ledger event-driven.

Files:
- `Packages/com.moni.puredots/Runtime/Runtime/Resources/SupplyChainComponents.cs`
- `Packages/com.moni.puredots/Runtime/Runtime/Resources/SupplyChainHelpers.cs`

### 9.2 Ledger pattern already exists (copy it)
We already use “change via event ledger” in economy:
- `WealthTransactionSystem` records transactions into a ledger buffer and applies them deterministically.

Resource flow should follow the same pattern: emit events → deterministic reduction/apply.

### 9.3 Aggregates/compression already have pseudo-history hooks
Use the existing compressed sim hooks to keep accounting meaningful under virtualization:
- `AggregateComponents.CompressedEvent`
- `AggregateComponents.PseudoHistoryEntry`

---

## 10) Performance mistakes to actively block

- Scanning all machines/buildings each tick just to compute stats.
- Clearing dense arrays every tick for every scope instead of clearing touched IDs.
- Global atomics/locks on hot counters (prefer per-scope ownership + deterministic reduce).
- Mixing Actual and Reference into one value (kills diagnosis).
- Using floats for authoritative flow totals and long histories (drift + nondeterminism risk).

---

## 11) Project defaults (initial presets)

### Godgame
- Scopes: building → village → region (optional) → world.
- Players primarily inspect: village scope + selected building monitors.

### Space4X
- Scopes: module/facility → ship/station/colony → planet → faction/empire.
- Outside-space logistics: import/export events are first-class (trade lanes, convoys, docking).

