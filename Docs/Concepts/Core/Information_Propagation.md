# Information Propagation + Relationships — Plan (v1)

> Scope: shared simulation system for **perceived reality** (true/false/unknown), **fog-of-war persistence**, **rumors/hearsay**, **multi-network comms**, **deception/verification**, and **directed relationships** at **millions of entities** scale.  
> Goal: enable behavior like "an agent knows *that* well (seen) but not the closer one (never seen/heard/asked)."

---

## 0) Design principles

- **Perceived reality ≠ world truth**: entities act on beliefs.
- **Bounded per-entity memory**: most knowledge is referenced from shared pools.
- **Attention tiers**: deep individuality near player / narrative, abstraction elsewhere.
- **Network-first propagation**: everything travels via a configurable comms network.
- **Policies everywhere**: behavior is modifiable through data-driven rules, budgets, and caps.

---

## 1) Core concepts

### 1.1 World Truth (objective)
- Authoritative world state (wells, fleets, resources, miracles, etc.).
- Not stored per entity.

### 1.2 Claim (shareable structured proposition)
A claim is a *statement* about the world or a concept, using a **multipart template**.

**ClaimTemplate**
- `PredicateId`
- Required slots: `Subject`, `Object`, optional `Qualifiers`
- Drift rules (how it mutates over hops)
- Verification rules (what evidence can validate it)
- Relevance hints (who cares by default)

**ClaimInstance**
- `Subject` (entity / group / location anchor)
- `PredicateId`
- `Object` (entity / value / location / concept)
- `Qualifiers` (distance, quantity estimate, tags, time window)
- `Context` (where/when generated, channel, language)
- `Provenance` (source id, hop count, network id)
- `EvidenceRefs[]` (observation/doc/witness ids)

### 1.3 Evidence (optional)
- **Observation evidence**: produced by sensory limbs / LOS.
- **Documents**: notes, logs, archaeology.
- **Witness records**: "I saw X at Y".

Evidence exists as separate objects. Claims may reference evidence.

### 1.4 Belief (holder-specific stance on a claim)
Belief is an overlay stored per holder for a referenced claim.

**BeliefOverlay**
- `BeliefKind`: `Affirm | Deny | Unsure`
- `Confidence`: 0..1
- `Relevance`: 0..1
- `Staleness`: 0..1
- `SourceSummary`: compressed (last source + trust bucket)
- `ManipulationFlags`: suspected lie/forgery/propaganda

---

## 2) Storage topology (scales to millions)

### 2.1 Personal memory (per entity; bounded)
- Ring buffer of claim references + belief overlays.
- Default cap: **64** (policy-controlled).
- Promotion rules: store only if directly observed OR goal-relevant OR repeated by trusted sources OR high-stakes.

### 2.2 Group knowledge (shared caches; bounded)
Shared caches for:
- squads, settlements, factions, cults/guilds (optional, data-defined)

Default cap: **2048** claim refs per group cache (policy-controlled).

### 2.3 Spatial rumor pools (region bulletin boards)
- Each cell/sector maintains a capped pool of circulating claim refs.
- Default cap: **256** per cell (policy-controlled).
- Agents "sample" from the pool based on topics + interests + current needs.

### 2.4 Institutional archives & documents (persistent truth-emergence)
- Documents/evidence are **discoverable sources**, not broadcast to everyone automatically.
- Claims can reference documents; agents can spread "I read X" as a claim.

---

## 3) Propagation networks (dynamic + spatial)

Everything that spreads uses a **Network** definition.

**NetworkConfig**
- `Bandwidth`: messages per update per node + global
- `LatencyModel`: hop queues / distance curve / priority lanes
- `IntegrityModel`: interceptable / forgeable / signed / encrypted
- `Compatibility`: language requirements, decode error model
- `TopicBias`: what this network tends to carry (orders vs gossip)

Default networks (configurable):
- Proximity gossip (spatial)
- Squad comms (dynamic membership)
- Institutional broadcast (priests, scouts, bureaus)
- Tech comms (radio/FTL)
- Exotic (telepathy/mind-reading) as optional layer

---

## 4) Deception, verification, drift, and reach

### 4.1 Deception model
Outgoing claims can be *truthful* or *crafted lies*.

**Lie metadata**
- `LieQuality` (tongue-smith + charisma + language mastery + plausibility)
- `Targeting` (optional: tailored to receiver biases)

**Receiver credibility update uses**
- source trust (relationships / group / institution)
- channel integrity (intercepted/forged?)
- plausibility (bias + prior beliefs)
- verification opportunities (LOS, corroboration, mind read)

### 4.2 Verification model (triggered, not constant)
Verification happens when:
- claim would change a decision,
- stakes are high,
- agent has explicit verify behavior.

Verification sources:
- direct observation
- corroboration (independent sources)
- institutional sources (weighted, not absolute)
- mind-reading (optional; can be resisted/spoofed per setting)

### 4.3 Drift model (telephone-game)
Template-driven transforms per hop / per decode quality:
- detail blur (quantization)
- name distortion (language mismatch)
- location drift (coarse cell rounding)
- attribute exaggeration (myth-making)
Drift strength is policy-controlled by network + language skill + hop count.

### 4.4 Reach and decay
- **Relevance decays** toward zero unless refreshed by use/mention/goals.
- **Confidence** can be manipulated by propaganda/deception/corroboration.
- **Reach decays** via hop-limits + TTL + forwarding propensity.

---

## 5) Decision integration contract (minimal API)

Downstream AI should never directly traverse rumor internals.

### 5.1 Query
- `GetBeliefsAbout(subject/predicate/object filters, maxResults)`
- returns best matching claims + belief overlays.

### 5.2 Event hook
- `BeliefChanged(holder, claimRef, delta)`
- consumed by: decision-making, emotions, reputation, relations.

### 5.3 Actions
- `BroadcastClaim(network, claimRef, audienceHints)`
- `AskForInfo(topic/filters, target/network)`
- `VerifyClaim(claimRef, method)`

---

## 6) Relationships system (directed, tiered, pooled)

### 6.1 Directed edges (default)
- A→B can differ from B→A.
- Reciprocity computed on-demand if needed (combine both edges when present).

### 6.2 Attention tiers + caps (policy ceiling)
- Tier0 (focus/narrative): cap **150**
- Tier1 (nearby): cap **48**
- Tier2 (distant): cap **8**
- Tier3 (background): cap **0**
Caps are policy-controlled; 150 is the **ceiling**, not mandatory allocation.

### 6.3 Paged pooled storage (avoid per-entity DynamicBuffer bloat)
Entity stores:
- `RelationshipPageHandle` + `EdgeCount` + `Tier`

Central pools store fixed-size pages:
- page classes: 0/8/16/32/64/128/256 edges
Promotion/demotion moves between page classes (copy/rehash) rather than resizing buffers.

### 6.4 Edge layout (hot vs cold)
**Core edge (hot path)**
- TargetId
- Trust (fixed-point)
- Affinity (fixed-point)
- Familiarity
- LastInteractionTick
- Flags (bitfield)

**Optional extensions (cold path; only when needed)**
- Lie history counters
- Topic trust buckets
- Interaction context tags

### 6.5 TrustPolicy lookup order (fast fallback chain)
When evaluating a source:
1) direct edge (if present)
2) group-level trust
3) institutional trust
4) baseline stranger model (culture + reputation + bias)

---

## 7) Configuration & modding surface

Everything important is a policy module with data-driven knobs:

- `MemoryPolicy` (promotion/eviction; ring sizes)
- `GroupPolicy` (which groups exist; cache caps)
- `RumorPoolPolicy` (cell size; cap; sampling)
- `PropagationPolicy` (networks, routing, budgets, latency)
- `IntegrityPolicy` (intercept/forge/sign/encrypt)
- `DriftPolicy` (transforms per predicate/network)
- `TrustPolicy` (institutional curves; relationship weights)
- `DeceptionPolicy` (lie quality; detection contests)
- `VerificationPolicy` (triggers; methods; costs)
- `DecisionContract` (how belief deltas map into AI hooks)

---

## 8) Game presets (same engine, different tuning)

### 8.1 Godgame preset
- Higher gossip bandwidth near settlements
- Strong institutional sources (priests/elders)
- Higher drift (myth-making), stronger document-based truth restoration
- More "idea/ideal" predicates (faith, taboo, morality)

### 8.2 Space4x preset
- Structured comms dominates; gossip reduced
- Strong integrity mechanics (encryption/forgery)
- Numeric/positional predicates (sightings, estimates, strength)
- Drift low on signed comms; higher on third-hand rumors

---

## 9) Implementation order (execution-focused)

1) **Define schemas**
   - ClaimTemplate, ClaimInstance, BeliefOverlay, Evidence, NetworkConfig
2) **Build Claim registry**
   - stable ids for predicates/templates; shared storage for instances
3) **Implement memory layers**
   - personal ring buffer (cap + eviction)
   - group caches (cap + shared refs)
   - rumor pools (cell mapping + sampling)
4) **Implement network propagation**
   - enqueue, latency queues, bandwidth budgets
   - decode + language noise hooks
5) **Belief update pipeline**
   - merge rules, staleness/relevance handling, BeliefChanged events
6) **Trust & relationship core**
   - directed edges + pooled pages
   - trust lookup chain (edge → group → institution → baseline)
7) **Deception + integrity**
   - forge/intercept hooks
   - lie quality metadata + detection contests
8) **Verification mechanisms**
   - corroboration counters
   - observation/document checks
9) **Decision API**
   - query + events + actions (ask/broadcast/verify)
10) **Instrumentation & guardrails**
   - global budgets (edges, claims, messages)
   - per-tier counts and spillover
   - worst-case monitoring (queue sizes, churn)

---

## 10) Guardrails (avoid silent blow-ups)

- **Global edge budget** + backpressure:
  - demote Tier1 caps, increase eviction, stop creating edges unless high-stakes
- **Message budget** per network + per node:
  - drop/deprioritize low-relevance topics under load
- **Verification budget**:
  - verification is event-triggered and rate-limited
- **Hot-loop bans**:
  - no "scan all claims" or "compare all neighbors" loops; always query via spatial/group indices

---

## Appendix: default numbers (all policy-controlled)

- Personal memory cap: **64**
- Group cache cap: **2048**
- Rumor pool cap: **256**
- Relationship caps (Tier0/1/2/3): **150 / 48 / 8 / 0**
- Default meaningful groups: squad, settlement, faction (cults/guilds optional)
