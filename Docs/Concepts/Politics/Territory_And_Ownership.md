# Territory & Ownership — Architecture Plan (PureDOTS)

> Purpose: a **game-agnostic** territory/ownership module consumed by *Godgame* and *Space4x* with **policy-driven behavior**, **hard performance caps**, and **deterministic command/event integration**.

---

## 0. Goals

- **Flexible**: rules are policy/config driven; games can interpret outputs differently.
- **Bounded cost**: any "slider-heavy" configuration still respects **budgets/caps** (no unbounded O(N²)).
- **Deterministic by default**: stable results given same command stream (tie-break rules explicit).
- **Moddable**: extensible *types* (rights, treaty clauses, control sources, dispute kinds) via hashed IDs + registries.
- **Telemetry-first**: territory produces an event ledger + counters; consumers decide what to display.
- **PureDOTS**: Burst/job-friendly data layout; no managed allocations in hot path.

### Non-goals
- No UI/UX decisions.
- No gameplay lock-in: "mana multipliers", "supply", "intel" etc. are **consumer hooks** derived from territory metrics.

---

## 1. Core abstractions (game-agnostic)

### 1.1 Sites and topology
Territory operates on a **graph of Sites**:

- `SiteId`: contiguous 0..N-1 per topology layer.
- `TopologyLayer`: e.g., coarse global, refined bubbles, etc.
- `Adjacency`: per-site neighbor list.

**Games provide**:
- Topology blob(s) (nodes + adjacency + optional weights).
- World→Site mapping (grid, nav clusters, sector/system/lane mapping, etc.).

Territory never assumes "grid" vs "space lanes"; it only consumes `SiteId`.

### 1.2 Claimants and sovereignty
- `ClaimantId`: stable identity for *any owning actor* (individual, aggregate, force, etc.).
- **Sovereign**: the governing authority for a site (exclusive/shared/leased… defined by policy).
- **Legitimacy**: a scalar representing claim strength/acceptance (policy-defined semantics).

### 1.3 Control overlay and frontier
Each site tracks:
- **Top-K contenders** by control strength (K is capped; always).
- **Frontier state**: contested-band metrics (tension, softness, confidence).

This supports "soft contested regions" while allowing crisp absolutes.

### 1.4 Rights, treaties, disputes
- Rights are capabilities (enter, mine, build, tax, worship… consumer-defined).
- Treaties are first-class agreements (clauses are extensible types).
- Disputes are tracked state machines (escalation is policy-driven; resolution may be external).

---

## 2. Module boundary: Commands in / Events out (authoritative pipeline)

### 2.1 Why this boundary
Random systems directly writing territory state will break invariants and make mods/debugging impossible.
Instead, territory owns the state transitions and exposes:
- **Commands**: requests to mutate state (validated + applied deterministically).
- **Events**: append-only results and telemetry hooks for downstream systems.

### 2.2 Command intake (write model)
A singleton buffer (bounded) of `TerritoryCommand`.
Examples (not gameplay-specific; just state mutations):

- Sovereignty: `SetSovereign`, `TransferSovereignty`, `ShareSovereignty`, `ClearSovereign`
- Rights: `GrantRight`, `RevokeRight`
- Treaties: `AddTreaty`, `RemoveTreaty`, `AmendTreaty`
- Control: `UpsertControlSource`, `RemoveControlSource`, `PushEphemeralContribution`
- Disputes: `RaiseDispute`, `ResolveDispute`, `RecordBreach`, `RecordIncident`

**Note:** "ephemeral" vs "persistent" control sources are both supported (see §4).

### 2.3 Event output (read model)
A singleton buffer (bounded) of `TerritoryEvent`:

- `SovereigntyChanged`
- `ControlOverlayChanged` (Top-K changed)
- `FrontierShifted`
- `RightGranted/Revoked`
- `TreatyChanged`
- `IncidentRecorded` (trespass, theft, blockade, occupation… type-extensible)
- `LegitimacyChanged`
- `CasusBelliUpdated` (type-extensible)

Events are the primary telemetry stream.

### 2.4 Query API (read-only)
Consumers (game-side) query territory via Burst-friendly functions:

- `GetSovereign(site, layer) -> SovereignState`
- `GetControlTopK(site, layer) -> FixedList<ControlEntry>`
- `GetFrontier(site, layer) -> FrontierState`
- `CheckRight(site, layer, claimant, rightType) -> AccessResult`
- `GetMetric(site, layer, MetricType) -> float` (e.g., confidence, tension, legitimacy)

No consumer can mutate authoritative state except through commands.

---

## 3. Data layout (ECS + Burst-friendly)

### 3.1 Entity model
- **TerritoryWorld** (singleton entity)
  - config refs (policy blobs, budgets)
  - registries (type mapping tables)
  - global counters / debug toggles
- **TerritorySite** (1 entity per SiteId per layer)
  - sovereignty + legitimacy
  - frontier state
  - Top-K control overlay (fixed-cap)
  - rights overrides (fixed-cap)
  - dirty flags
- **Claimant** (1 entity per ClaimantId)
  - identity metadata (tags/classification; optional)
- **Treaty** (1 entity per treaty record; optional)
- **Dispute** (1 entity per dispute record; optional)
- **TerritoryControlSource** (persistent contributors like buildings/walls/control points; optional)

### 3.2 Fixed-cap lists (avoid DynamicBuffer on sites)
To guarantee bounded work and predictable memory:
- Top-K control overlay: `FixedList128Bytes<ControlEntry>` (or compile-time max)
- Rights overrides: `FixedList128Bytes<RightOverrideEntry>`
- Local treaty exceptions (optional): fixed-cap

DynamicBuffers are allowed for:
- Command buffer (world singleton)
- Event buffer (world singleton)
- Optional debug trace buffer (world singleton)
These are bounded by budgets per update pass.

### 3.3 Extensible type IDs
Use hashed IDs with registry-compaction:

- External ID: `Hash128` (or 64-bit hash) for mod extensibility
- Internal Index: `ushort`/`uint` dense index for fast tables/bitsets

Types:
- `RightType`
- `ControlType`
- `TreatyClauseType`
- `DisputeType`
- `IncidentType`

---

## 4. Control contributions (generic and incremental)

Territory accepts control signals from *any* system without knowing semantics.

### 4.1 Two contribution modes

**A) Persistent sources** (best for buildings/walls/control points)
- A `TerritoryControlSource` entity declares:
  - `SiteId`, `LayerId`, `ClaimantId`, `ControlTypeIndex`
  - strength/support/tags
- Territory caches membership and recomputes sites when sources change.

**B) Ephemeral contributions** (best for transient presence: patrols, crowds, forces)
- External systems push `PushEphemeralContribution` commands into the queue,
  but ONLY for dirty/active sites (the system creating the contributions decides where).
- Territory merges ephemeral contributions during aggregation pass and discards them after.

### 4.2 Aggregation
For each dirty site:
- merge contributions by `(ClaimantId, ControlTypeIndex)` (policy-defined)
- compute a scalar `controlStrength` per claimant
- keep only Top-K contenders (+ "Other" bucket for telemetry)

---

## 5. Frontier and borders (derived state)

### 5.1 Frontier state (contested band)
Computed from:
- delta between top contenders
- local tension
- legitimacy and support metrics
- optional neighbor smoothing (policy + budget limited)

Frontier supports:
- soft gradient band (contested)
- crisp absolutes when policy says sovereign is solid

### 5.2 Border derivation output
Territory outputs **per-site metrics**, not rendered geometry.
Games decide how to render border lines/heatmaps, including "visible to player" rules.

---

## 6. Sovereignty resolution (policy-driven)

Sovereignty changes are **not automatic** unless policy says so.

Mechanism:
- Evaluate thresholds/triggers on dirty sites:
  - contender dominance thresholds
  - legitimacy thresholds
  - treaty constraints
  - dispute states (occupation, truce, arbitration, etc.)
- Apply resolution rules deterministically:
  - stable tie-breakers (ClaimantId, SiteId)
  - explicit "shared/leased/sold" modes (data-driven)

---

## 7. Rights/treaties evaluation (fast path)

### 7.1 Compiled rights cache
For each sovereign claimant:
- compile treaties + default policy into dense tables/bitsets:
  - `RightTypeIndex × GranteeClassIndex -> Allow/Deny/Conditional`
- apply site-level overrides after the fast check (fixed-cap list)

Result: `CheckRight()` stays O(1) in common case.

### 7.2 Treaties
Treaties are entities (optional) with:
- participants
- clause list (type-extensible)
- duration/expiry/revocation policies
Compiled caches update only when treaties change.

---

## 8. Disputes, legitimacy, casus belli (state + events)

Territory tracks:
- dispute records per site/claimant pair (capped by policy) OR per-site "primary disputes" (Top-K)
- legitimacy deltas caused by incidents and treaty actions
- casus belli as a derived metric (type-extensible)

Territory does **not** decide diplomacy; it provides the facts + state machine hooks.

---

## 9. Execution order (Territory pipeline)

1. **Ingest commands**
   - validate, update sources/treaties/rights/sovereignty
   - mark dirty sites
2. **Collect dirty set**
   - include neighbor expansion as budget allows (frontier smoothing)
3. **Aggregate control**
   - merge persistent + ephemeral contributions
   - compute Top-K overlay
4. **Update frontier**
   - compute contested band metrics and "confidence"
5. **Resolve sovereignty**
   - apply policy triggers and deterministic tie-breaks
6. **Compile rights caches (if dirty)**
   - per-sovereign compilation only when treaties/rights change
7. **Emit events**
   - append-only: state changes + incidents + trace (optional)

---

## 10. Budgets and sliders (flexibility without unbounded work)

All "player sliders" feed into budgets. Budgets have hard ceilings.

### 10.1 Hard ceilings (compile-time)
- `MAX_CONTENDERS_PER_SITE` (e.g., 16)
- `MAX_RIGHT_OVERRIDES_PER_SITE` (e.g., 64)
- `MAX_SITE_NEIGHBORS_TOUCHED_PER_PASS` (e.g., 64)
- `MAX_COMMANDS_PER_PASS` (e.g., 1–10M depending on platform)
- `MAX_EVENTS_PER_PASS` (bounded + drop/summarize policy)

### 10.2 Runtime budgets (player/config sliders)
- `ContendersPerSite` (<= MAX)
- `RightsOverridesPerSite` (<= MAX)
- `RefinedSitesBudget`
- `FrontierSmoothingRadius`
- `TelemetryVerbosity` (event categories enabled, sampling)
- `DisputeTrackingDepth` (how many active disputes are retained)

### 10.3 Degradation behavior
When budgets are exceeded:
- keep correctness via Top-K + "Other"
- drop low-priority events or summarize counts
- clamp refined site creation

Never remove caps.

---

## 11. Telemetry and debugging

### 11.1 Event ledger
- structured events with type IDs + small payload
- supports filtering (by site, claimant, event type, severity)

### 11.2 Trace channel (optional)
A toggle-able, bounded "reasoning trace" for:
- why access denied
- why sovereignty changed
- why legitimacy changed

Trace is off by default, compiled away or gated by define if desired.

---

## 12. Persistence and versioning

### 12.1 Save format
Serialize authoritative state:
- per-site sovereignty/control/frontier/overrides
- treaties and disputes (if enabled)
- registries version + policy blob IDs

### 12.2 Migration
Provide versioned migration hooks:
- policy schema changes
- type registry changes
- topology changes (site remap)

---

## 13. Package layout (PureDOTS suggestion)

- `PureDOTS.Territory/Runtime/`
  - `Components/` (Site, Claimant, World, Treaties, Disputes, Sources)
  - `Commands/` (command structs + enqueue helpers)
  - `Events/` (event structs + sinks)
  - `Policies/` (blob schemas + strategy selectors)
  - `Topology/` (blob schemas + layer utilities)
  - `Systems/` (pipeline systems, ordered group)
  - `Util/` (hash registry, fixedlist helpers, determinism helpers)
- `PureDOTS.Territory/Tests/`
  - determinism tests, stress tests, fuzzers

---

## 14. Implementation plan (dependency order)

1. **Type registries**
   - hashed ID → compact index mapping (Right/Control/Treaty/Incident/Dispute)
2. **Topology blobs**
   - layer support, adjacency encoding, SiteId conventions
3. **World singleton + budgets**
   - runtime config + hard ceilings, debug toggles
4. **Site entity generation**
   - spawn one TerritorySite per SiteId per layer (authoritative state container)
5. **Commands + events**
   - bounded buffers, enqueue helpers, validation
6. **Dirty-set system**
   - mark sites, neighbor expansion with budgets
7. **Control aggregation**
   - persistent sources + ephemeral contributions, Top-K selection, "Other" bucket
8. **Frontier derivation**
   - confidence/tension/softness metrics, optional neighbor smoothing
9. **Sovereignty resolution**
   - policy-driven triggers and deterministic outcomes
10. **Rights/treaty compilation**
   - fast tables/bitsets + site overrides
11. **Dispute tracking**
   - minimal state machine + incident hooks + event emission
12. **Telemetry + trace**
   - filters, sampling, counters, reasoning trace (bounded)
13. **Persistence**
   - serialize state + registry versions + migration scaffolding
14. **Stress + determinism tests**
   - fuzz command sequences, validate invariants, measure allocations

---

## 15. Open choices (kept intentionally policy-driven)

- Which metrics drive sovereignty changes (weights and thresholds)
- Which incidents exist and how they affect legitimacy/casus belli
- Which rights exist and what they mean
- How much frontier smoothing to apply
- How games interpret "confidence/tension/legitimacy" into costs and behaviors

These remain consumer/policy decisions; the architecture supports all.

---

## Appendix: Suggested core structs (sketch)

> Not final code; only shape and constraints.

- `TerritorySiteState`
  - `SiteId`, `LayerId`
  - `SovereignClaimantId`, `SovereignMode`
  - `Legitimacy`, `Confidence`, `Tension`
  - `FixedList<ControlEntry> TopK`
  - `FixedList<RightOverrideEntry> Overrides`
  - `DirtyFlags`

- `ControlEntry`
  - `ClaimantId`
  - `ControlStrength`
  - `LocalSupport`
  - `Tags`

- `RightOverrideEntry`
  - `RightTypeIndex`
  - `GranteeClaimantId`
  - `Allow/Deny`
  - `Terms/ExpiryIndex` (optional)
