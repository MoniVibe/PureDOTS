# Exploration & Discovery — Plan (Concept)

**Purpose:** A shared, game-agnostic Exploration/Discovery + Knowledge/Intel backbone for both **Godgame** (B&W-like) and **Space4x**, supporting:
- massive simulation scale via **aggregation + compression**
- rich emergent behavior via **uncertainty-aware decisions**
- secrets, rumors, markets, censorship, and noisy sensing via **policy**, not hardcoded rules

---

## Design Locks (current decisions)

### Knowledge ownership & diffusion
- Knowledge is exchanged based on **language/sign compatibility**, **relations/trust**, and **interaction**.
- Knowledge **merges** during exchanges; diffusion is **organic** and **slow**.
- Multiple overlapping aggregates exist: **guild, village, empire, faction, family, dynasty, business, fleet, cult, etc.**
- Diffusion is **local-first**: local aggregates update/spread fastest; larger entities lag unless infrastructure exists.
- Sharing defaults to **downward dissemination** (approved/filtered knowledge), but **both directions** occur depending on interaction and intent.

### What can be known
- Discovery types include: recipes, material properties, locations, resources, paths/lanes, threats, quests/events, lore/secrets, relations, anomalies, and broad world news (wars/laws/slavery/tyranny/etc.).
- Secrets include: hidden places, intentions, identities, relations, possessions, beliefs, skills/traits, tech/blueprints, knowledge itself.

### Fog-of-war
- Fog-of-war = **not currently sensed** (outside senses).
- Entities can share sensed knowledge via nearby communication (speech/signs), and via larger comms networks depending on tech.

### Mastery + decay
- Knowledge has **mastery** that can exceed 100% up to **400%** for relevant categories.
- Knowledge can **decay**, with category-specific behavior; low knowledge can be forgotten fully, while high mastery decays slower and may retain special unlocks.

### Exploration (layered + quality-based)
- Multi-layer exploration:
  1) broad scan (orbital/area)
  2) surface scan
  3) expeditions (risky, deeper yields)
  4) archaeological digs (deepest yields, ancient knowledge/exotics)
- Exploration has **quality** and can be **rerolled** via repeated attempts / better explorers.
- Sites can be "**depleted**" of knowledge **for an entity/aggregate** at 100% completeness.
- Resources do not truly deplete; **richness** can decline over time.

### Economy + secrecy
- Explorers may withhold valuable knowledge and **barter** it; markets can be informal (town square bulletins).
- Knowledge propagation requires **intent**: it doesn't spread unless the holder chooses (or it is stolen/coerced/mind-read/interrogated).

### Verification & uncertainty
- Claims become reliable through:
  - **direct observation**
  - **trusted authority**
  - **ritual/tech scans**
- **Scans can be wrong.**
- AI acts on **uncertainty** (expected utility), and will seek information when it has value.

### Player intel
- Player intel can follow the same system as entities (omniscient mode becomes a policy switch).

---

## Core Abstractions (engine invariants)

### 1) KnowledgeId
A stable identifier for anything knowable (facts, skills, secrets, events, models).

### 2) KnowledgeClaim (belief about a KnowledgeId)
A claim is not "truth"; it's what a store believes. Claims support:
- **confidence** (and optionally a probability distribution over variants)
- **staleness** (how out-of-date the claim is)
- **detail level** (coarse → exact → access method → parameters)
- **mastery** (0–400% where applicable)
- **shareability** (share / barter / restricted / secret)
- **value** (economic leverage)
- **mutation rules** (rumor drift vs locked)

### 3) EvidenceEvent
All belief updates come from evidence events (policy-driven strength):
- ObservedSelf
- AuthorityStatement (trust-weighted)
- ScanResult (error model: false pos/neg, bias, grade, anomaly sensitivity)
- Corroboration / Contradiction
- Theft / Interrogation / MindRead (category policy)

### 4) KnowledgeStore
Where claims live:
- **Personal overlay** (small; secrets, mastery deltas, personally verified detail)
- **Aggregate stores** (guild/village/empire/etc.; main diffusion surface)
- **Player store** (same mechanism; omniscience is a policy)

### 5) SituationModel
Rolling summaries derived from event streams:
- war intensity, tyranny index, famine risk, piracy level, plague spread, etc.

### 6) KnowledgePacket (exploration output)
The exploration system emits packets that set/adjust claims:
- KnowledgeId + detail level + confidence delta + evidence tags + quality + risk tags

---

## Layered Ownership Model (scale without "hard caps")

### Default ownership
- **Aggregate-only (default):** world news, laws, broad foreign intel, coarse locations/routes, generic threat levels, common recipes.
- **Personal overlay:** secrets/restricted claims, personally verified exact details, mastery deltas, private relations/intentions, expedition-specific notes until promoted.
- **Hybrid:** locations/resources can be aggregate at coarse level + personal at exact level.

### "Resolved Knowledge View"
An entity's knowledge query resolves:
1) personal overlay overrides
2) local aggregates (fastest, most trusted by default)
3) wider aggregates (empire/faction)
4) public/world feeds if enabled

---

## Compression & Forgetting (uncapped design, bounded compute)

### Auto-promotion (shareable)
- Shareable personal claims are promoted into the most relevant local aggregate store.
- Optional: keep a tiny "I discovered this" credit marker.

### Compression
When stores grow:
- events → situation model deltas (reduce long-tail spam)
- many minor facts → region/topic summaries
- rumors → top-K variants + "Other"
- personal histories → episodic journal highlights

### Forgetting (feature)
Evict low-value items first:
- low confidence
- high staleness
- low relevance to goals
- low recency
Keep minimal "scar memories" for impactful events if desired.

---

## Propagation Model (organic diffusion)

### Transfer is filtered by policy
A knowledge transfer occurs only if filters pass:
- language/sign compatibility
- relation/trust
- intent (share/barter/withhold)
- channel availability (gossip, bulletins, messenger, pigeons, embassy, broadcast)
- censorship/propaganda rules
- category rules (secrets may never auto-propagate)

### Diffusion surfaces (LOD-aware)
- **High LOD:** optional "conversation events" for immersion/debug; transfers occur explicitly.
- **Low LOD:** simulate diffusion as bounded transfers between stores using "interaction intensity" (co-location, trade flow, border contact).

### Local-first default
- local ↔ local transfers are strongest
- local ↔ empire transfers require infrastructure/reps; otherwise slow

---

## Rumors, Lies, Myths

### Rumor mutation
- Unverified claims can drift and split into variants.
- Verified claims can lock variant mutation, but dynamic facts can still go stale.

### Lies and propaganda
- Stores may hold contradictory claims simultaneously (politics needs this).
- Query-time selection chooses which claim to act on vs which claim to share (intent).

### Myths/legends
- Old rumor clusters can be compressed into narrative variants over long horizons.

---

## Exploration System

### Exploration actions (depth ladder)
1) Broad scan (orbital/area): discovers coarse POIs/anomalies/routes
2) Surface scan: refines detail + confidence; may reveal access methods
3) Expedition: risky, deeper yields; can uncover hidden resources/relations/events
4) Archaeological dig: rare knowledge/exotics/ancient lore; highest risk + payoff

### Quality
- Quality influences: confidence gain, detail unlocks, scan grade, loot/rare tables, anomaly exposure.
- Rerolls happen by repeating actions and/or using better explorers and equipment.

### Depletion semantics
- "Depleted" means **knowledge completeness** reached 100% for that store (entity/aggregate).
- Others may still profit due to different tools/skills/depth tables or newly spawned world state.

### Catastrophic outcomes
- Exploration may irreversibly change an individual and/or trigger crises (policy-driven by tags and risk tables).

---

## Uncertainty-Aware AI (Expected Utility + Value of Information)

### Action selection
AI chooses actions based on expected utility under uncertainty:
- supports categorical rumors, numeric uncertainty, spatial uncertainty
- risk attitude traits bias decisions (cautious vs reckless vs desperate)

### Value of Information (VOI)
AI may choose "learn first" actions (scan/scout/buy intel/interrogate) when:
- expected improvement > information cost + risk

This makes scouts/explorers/spies economically and strategically relevant without scripting.

---

## Flexibility & Runtime Modding Knobs

Everything below should be data/policy-driven:
- knowledge categories/tags and belief representation type (categorical/numeric/spatial)
- mastery caps (0–400%) per category + perk unlock mapping
- decay/staleness curves per category
- evidence weighting per category (observation/authority/scan/etc.)
- scan error models by tech/ritual grade + anomaly modifiers
- trust computation (relations + role + reputation + context)
- propagation channel definitions (bandwidth/latency/range)
- censorship/propaganda rules per aggregate
- compression/forgetting heuristics per store type
- rumor mutation behavior and top-K limits
- exploration packet tables per depth action + quality reroll rules
- crisis triggers from exploration outcomes

---

## Integration Points (shared)
- **Procedural World Gen:** produces discoverables (KnowledgeIds) + tags
- **Communication:** channels for transfers (gossip → broadcast)
- **Relations:** trust affects authority and sharing
- **Resource/Economy:** knowledge markets + discovery unlocks
- **Quests/Tasks:** exploration targets and payoffs
- **Memory/Story:** episodic journals, event feeds, legends

---

## Orders of Execution (implementation plan)

### Phase 0 — Data foundation
- Define KnowledgeId registry + tag system.
- Define Claim schema + evidence schema + store schema.
- Define policy container format (runtime-editable).

### Phase 1 — Minimal knowledge loop
- Personal overlay + one local aggregate store.
- Transfers: language/trust/intent filters.
- Basic decay + staleness.
- Basic compression (event→model rollup, summary facts).

### Phase 2 — Exploration packets
- Implement exploration depth ladder producing KnowledgePackets.
- Apply packets to stores via evidence updates.
- Add scan error model and rumor mutation.

### Phase 3 — Overlapping aggregates + diffusion
- Multiple aggregate memberships per entity.
- Local-first diffusion between aggregates via interaction intensity.
- Censorship/propaganda policy hooks.

### Phase 4 — Uncertainty-aware AI
- Belief distributions for selected tags.
- Expected utility action selection.
- VOI-driven "learn first" behaviors (scout/buy intel/etc.).

### Phase 5 — Economy + secrecy
- Knowledge value models + markets/bulletins.
- Barter/contract mechanics for intel sale.
- Theft/interrogation/mind-read channels (policy gated).

### Phase 6 — Debuggability & authoring UX (recommended)
- Knowledge inspector: why do I believe this?
- Evidence trace summaries (not full chat logs).
- Diffusion visualizer: where did this spread?
- Rumor cluster viewer: variants + confidence.

---
