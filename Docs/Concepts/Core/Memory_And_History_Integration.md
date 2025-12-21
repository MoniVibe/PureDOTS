# Memory and History Integration System (Locked Spec)

**Status:** Locked (conceptual architecture baseline)  
**Target:** PureDOTS (game-agnostic). Designed to support both *Godgame* and *Space4x* by scaling from intimate, individual-level simulation to large aggregate worlds.

---

## 1. Design goals

### 1.1 Core goals
- **Single canonical history per major event**, interpreted differently by different holders (individuals and aggregates).
- **Meaningful individuality** where it matters, while keeping the architecture **scalable** (hot/cold split, handles, lazy derivations).
- **Flexible + moddable**: memory types/topics, policies, interpretation, propagation, deception, decay, merge, and conflict resolution are all **pluggable rules**.
- **Deterministic simulation core** for debugging, reproducibility, and future multiplayer/replay viability; presentation can be non-deterministic.

### 1.2 Non-goals (by default)
- Per-entity, always-on deep reasoning over large histories.
- Global "everyone knows everything" propagation without backpressure.

---

## 2. Key definitions

### 2.1 Memory (per holder)
A **Memory** is a *small thing* a holder remembers about:
- the world, other entities, lore, magic, tools/materials, events, and doctrines.

A memory can be **small or rich** depending on the holder's **MemoryProfile** (profile-driven richness, retention, and access).

### 2.2 Major Event
Anything involving **many entities cooperating or under threat** (battles, disasters, miracles, mass operations, etc.).  
Major Events are the only events that must produce a **canonical record** by default (policy-controlled).

### 2.3 Holder
Any entity that can "remember":
- **Individual** entities (agents, units, characters)
- **Aggregate** entities (village, guild, company, faction, empire, race, culture, dynasty, family, elite group, companion-duo, etc.)

### 2.4 Canonical Record
A single **shared truth record** describing a major event.  
Holders do not copy the record; they store **handles** to it, plus small per-holder deltas.

### 2.5 Interpretation / Impression
A holder-specific (or group/profile-specific) derived view of a record:
- trait deltas, relation deltas, reputation deltas, doctrine/tactical preferences, emotional tags, confidence/bias, etc.

### 2.6 Knowledge → Rumor
- **Participants** receive direct memories.
- Others may learn the event as **knowledge** which can become **rumor**, spreading faster through related aggregates.
- Rumors can drift or be manipulated (deception is first-class).

### 2.7 Claims (Narratives) and Conflicts
A **Claim** is an asserted narrative about a record (or fabricated event):
- Multiple claims can coexist and compete.
- Aggregates may accept one or more claims (schisms supported).

### 2.8 Evidence
Evidence strengthens or weakens claims:
- witnesses, artifacts, documents, magical verification, scars, records, trophies, etc.
- Evidence can be forged, planted, stolen, or decay (policy-controlled).

---

## 3. Architectural principles

### 3.1 Hot/Cold split (mandatory)
- **Hot data:** tiny, cache-friendly, directly consumed by AI/relations/reputation/tactics.
- **Cold data:** rich event details, large participant lists, and deep "story" fields.
Cold data is accessed only for:
- promoted/focused entities,
- player inspection ("why?"),
- story/UI extraction,
- offline/maintenance processing.

### 3.2 Central canonical store + per-holder handles (default)
To prevent duplicated state, canonical event data lives in a global store; holders keep compact handles.

### 3.3 Lazy + cached derivations (default)
Impressions are computed **on demand** and cached, keyed by an **InterpreterContext**:
- holder profile,
- holder aggregate memberships,
- role/culture/faction/rank,
- and any custom tags.

### 3.4 Aggregates are first-class entities (default)
Aggregates use the same memory pipeline as individuals:
- they can hold memories, accept narratives, and broadcast rumors.

### 3.5 Determinism boundary (default)
- Canonicalization, claims, acceptance scoring, decay/merge, and derived hot outputs are deterministic.
- Presentation-only effects (barks, animation variance) may be non-deterministic.

---

## 4. Data model (conceptual)

### 4.1 Stable identities (locked)
**Stable IDs exist** for remembered things, to support persistence, cross-save referential integrity, and lore continuity.

- **StableId**: 128-bit GUID-like value (or hashed composite) for entities that can be remembered.
- Entities may also have runtime Entity references; stable IDs are the durable link.
- Destroyed/merged/split entities use **Redirects** (tombstones) in an IdentityMap:
  - `StableId -> {AliveRef | RedirectToStableId | Tombstone}`

This keeps the system robust when entities despawn or aggregates change.

### 4.2 Core stores

#### CanonicalRecordStore (global, append-only)
Stores one record per major event:
- `RecordId`
- `Timestamp/Sequence` (deterministic ordering)
- `EventTypeId`
- `LocationRef` (stable)
- `PrimarySubjects` (stable IDs, possibly aggregates)
- `OutcomeSummary` (hot-friendly)
- `ColdPayloadRef` (optional pointer to rich details)
- `EvidenceRefs` (optional)

#### ClaimStore (global)
Claims reference canonical records or fabricate events:
- `ClaimId`
- `TargetRecordId` (or FabricatedRecord seed)
- `ClaimTypeId` (blame, exaggeration, denial, propaganda, etc.)
- `ProposerStableId/AggregateId`
- `DistortionParams` (confidence, bias, mutation vectors)
- `EvidenceRefs`
- `DeterministicSignature` (for stable merge/dedup)

#### EvidenceStore (global)
Evidence objects (artifacts/witness statements/documents/etc.):
- `EvidenceId`
- `EvidenceTypeId`
- `OwnerStableId/AggregateId`
- `Strength`, `Verifiability`, `ForgeryRisk`
- `LinkedRecordIds/ClaimIds`

#### ImpressionCache (global, rebuildable)
Cached derivations keyed by:
- `RecordId`
- `InterpreterContextId`
Outputs small "hot deltas":
- trait deltas, relation deltas, reputation deltas, doctrine tags, etc.

### 4.3 Per-holder memory handles

#### MemoryHandle (hot, compact)
Stored on individuals and aggregates:
- `RecordId`
- `SubjectStableId` (who/what this is "about")
- `TopicId` / `MemoryTypeId` (data-registered IDs)
- `Salience` (importance to holder)
- `Confidence` (accuracy belief)
- `BiasMask` (interpretive skew flags)
- `ClaimAffinity` (optional pointer/weight toward a claim)
- `LastTouchedSequence`

> **Rich detail is never required here**. Rich detail belongs to canonical record cold payloads or explain layers.

### 4.4 Profiles (controls richness and cost)

#### MemoryProfile
Holder-specific knobs (defaults can be set per archetype and overridden):
- `MaxActiveHandles` (soft cap by policy; not a hard gameplay limit)
- `RetentionCurve` (stickiness vs decay)
- `DetailAccessTier` (how often cold payload is allowed to be consulted)
- `InterpretationDepth` (how many impressions can be cached/consulted)
- `RumorSusceptibility` (mutation/acceptance tendencies)
- `DeceptionSkillAffinity` (for roles that can manipulate narratives)

---

## 5. Execution pipeline (order of execution)

### Stage 1 — Event ingest
All systems emit generic, typed events into an EventBus:
- must be deterministic ordering by sequence + stable IDs.

### Stage 2 — Canonicalization (pluggable)
`HistoryCanonicalizer` applies `ICanonicalizationPolicy`:
- decide if event becomes a canonical record
- normalize into canonical schema
- append to CanonicalRecordStore

### Stage 3 — Attachment (participants + aggregates)
`IAttachmentPolicy` decides recipients:
- participants always receive direct handles
- aggregates receive handles based on relevance rules
- optional inheritance sources may attach template handles (see Stage 6)

### Stage 4 — Propagation (knowledge → rumor)
`IPropagationPolicy` moves "knowledge tokens" through:
- individual ↔ aggregate edges
- aggregate ↔ aggregate edges (preferred for scale)
Propagation runs with strict **backpressure**:
- throttle fanout,
- degrade payload into lighter tokens,
- delay cold detail.

### Stage 5 — Deception and mutation transforms
`IDeceptionPolicy` can:
- fabricate claims,
- mutate rumor tokens,
- forge/suppress evidence,
- alter confidence/bias.

### Stage 6 — Conflict resolution into accepted narratives
`IConflictResolutionPolicy` scores claims for an aggregate:
- authority-weight,
- majority/pressure-weight,
- evidence-weight,
- counter-evidence and forgery risk.
Outputs:
- `AggregateAcceptedNarratives` (may hold multiple claims to represent schisms)

### Stage 7 — Derived impressions (lazy + cached)
Consumers request "hot impacts" (traits/relations/reputation/doctrine):
- compute via `IInterpretationPolicy` on demand
- cache by `(RecordId, InterpreterContextId)`
- optionally prewarm for focus-tier entities

### Stage 8 — Maintenance (decay / merge / retention)
Separate maintenance pass with policies:
- `IDecayPolicy`: degrade confidence/detail/salience
- `IMergePolicy`: merge similar handles into summaries
- `IRetentionPolicy`: pin "forever" memories (e.g., foundational myths, defining traumas) via tags and profile
- `ICompressionPolicy`: store/stream cold payload

Maintenance is where "budgeting" is enforced (soft by default), without hard-coding gameplay.

---

## 6. Collective memory (organic by architecture)

**Default model:** hybrid that remains flexible.
- **Bottom-up:** aggregates summarize member outputs via sampling/weighted contributions.
- **Top-down:** aggregates can store institutional records as their own handles.
- **Memetic dominance:** repeated retellings increase salience and acceptance probability.

All three behaviors are expressed through pluggable policies (no bespoke special cases).

---

## 7. Inheritance (locked default: reference-first)
Some memories can be inherited (family/culture/guild doctrine, etc.).  
**Default:** inheritance gives holders **references to collective handles**, not deep copies.
- When an entity becomes important or is promoted, it may materialize selected inherited references as personal handles (policy-controlled).

This keeps inheritance powerful without exploding per-entity state.

---

## 8. Tactics / doctrine learning (architecture-level)
Historical achievements influence tactics over time by producing **Doctrine outputs**:
- stored as impression deltas:
  - tags (e.g., "prefer chokepoints"), heuristics, or formation templates.
- ownership can be individual, squad, or aggregate (policy-controlled).
- doctrine selection is a consumer concern; memory system provides the learned preferences.

---

## 9. Query API (fast + inspectable)

### 9.1 Hot queries (fast path)
- "Does holder remember subject/topic?"
- "Top N salient memories about TopicId"
- "Current relationship/reputation modifiers driven by memory"
- "Doctrine tags relevant to context"

### 9.2 Cold queries (inspect/explain)
**Locked default:** Explain is first-class but optional in cost.
- Store minimal causal links in hot outputs:
  - `DerivedOutput -> {RecordIds, ClaimIds, EvidenceIds}`
- Full graph expansion can be enabled for debug or high-importance entities.

This supports "why did you think that?" without forcing heavy data for everyone.

---

## 10. Persistence & versioning

### 10.1 What persists (locked)
Persist:
- CanonicalRecordStore (records + cold payload refs)
- ClaimStore
- EvidenceStore
- Holder MemoryHandle buffers (individual + aggregate)
- AggregateAcceptedNarratives
- IdentityMap redirects/tombstones

Rebuildable (not required to persist):
- ImpressionCache (derive on load)

### 10.2 Schema evolution
- Canonical records and claims are versioned.
- Readers support backward compatibility; migration policies may upgrade old records on load.

---

## 11. Budgeting and backpressure (locked defaults)
You asked to "worry later," but the architecture must be safe now.

**Locked default:** soft budgets + graceful degradation.
- Propagation throttles before memory eviction.
- Merge summaries before dropping.
- Drop cold detail before dropping hot salience.
- Evict least-salient / least-referenced handles first (policy-controlled).

No gameplay assumptions are hard-coded; all eviction priorities are rule-driven.

---

## 12. Extension points (mod-friendly surface)
Everything below is a replaceable module:
- `ICanonicalizationPolicy`
- `IAttachmentPolicy`
- `IPropagationPolicy`
- `IDeceptionPolicy`
- `IConflictResolutionPolicy`
- `IInterpretationPolicy`
- `IDecayPolicy`
- `IMergePolicy`
- `IRetentionPolicy`
- `ICompressionPolicy`
- `IIdentityPolicy` (redirects/merges/splits)

### Type registration (locked default)
Memory topics and types are **data-registered IDs** (hash IDs):
- built-in set + modded additions
- stable across save/load

---

## 13. Scalability guardrails (non-negotiable architectural safety)
- Never require "everyone knows everyone."
- Never require aggregate → full member enumeration for propagation.
- Keep per-holder hot state compact (handles + scalars).
- Keep deep detail in cold payload and access it only by policy.
- Deterministic ordering for merges and acceptance scoring.

---

## 14. Minimal implementation checklist (to bootstrap later)
1) Define ID system (StableId + IdentityMap redirects).
2) Implement CanonicalRecordStore + ClaimStore + EvidenceStore (append-only + persistence).
3) Implement MemoryHandle buffer on holders (individual + aggregate).
4) Implement pipeline stages 1–4 (ingest → canonicalize → attach → propagate).
5) Add policy registry + data-ID registration (topics/types).
6) Add deterministic sorting keys + RNG streams for core.
7) Add maintenance stage (decay/merge/retention) as separate pass.
8) Add optional Explain links from derived outputs.

---

## 15. Locked decisions summary (for quick reference)
- **Hybrid storage:** centralized canonical store + per-holder memory handles
- **Hot/cold split:** mandatory
- **Interpretation:** lazy + cached impressions (rebuildable)
- **Aggregates:** first-class entities, same pipeline
- **Propagation:** aggregate-centric message passing + throttles
- **Deception:** first-class, transform stage
- **Conflict:** claim-based narratives; schisms supported
- **Evidence:** verifiable/forgeable objects influencing acceptance
- **Determinism:** deterministic core; presentation can vary
- **Stable identity:** cross-save stable IDs with redirects/tombstones
- **Explainability:** first-class causal links, expandable optionally
- **Budgets:** soft caps + graceful degradation via policies




