# Debugging & Visualization + Social/Knowledge Substrate — V1 Plan (Flexible)

This plan captures the **conceptual contracts + policy tables** for:
- **Directed Knowledge** (Seen/Met/Faction/Rumor)
- **Directed Relations** (multi-type, base + per-type modifiers, decay toward neutral with passive floors)
- **Bundles / Group Nodes** (village/army/guild/business/etc. as first-class nodes)
- **Debug UI** (hover tooltip, select inspector, relation navigation)
- **World-space Overlays** (color-coded relation lines, sensors/weapon arcs, comms pulses)
- **Rewind** (10-minute adjustable window, pause-until-caught-up, global correctness)

Everything is designed so that behavior is **policy/config-driven** rather than hard-coded.

---

## 0) Invariants (lock these now)

1) **Knowledge is directed**: A knows B ≠ B knows A  
2) **Knowledge is source-tagged**: Seen / Met / Faction / Rumor  
3) **Relations are directed** and **multi-type** per target  
4) Relation strength is computed as:  
   `Strength(type) = BaseStrength + TypeModifier[type]`  
5) **Memory is bounded**: caps + eviction (no unbounded graphs/history)  
6) Visualizations generate draw data only for **Hovered / Selected / Pinned** sets (never full-world)  
7) **Rewind correctness**: restore checkpoint → re-sim event log → resume (paused until caught up)

---

## 1) UX Contract

### 1.1 Hover / Select
- **Hover entity** → show tooltip with core state + **important relations only** (top-K by importance score)
- **Click entity** → select and expand tooltip into an **Inspector panel**
  - Shows useful info + **all immediate relations** (with optional collapse of low-importance items)
  - Clicking a relation navigates to that entity's list/inspector

### 1.2 Relation Lines (World-space)
- Default: **semantic colors** by relation type  
- Toggle: **state colors** (strength/valence/etc.)
- Hover: draw top-K relation lines for hovered entity  
- Selected: draw all immediate relations (optionally bundled/collapsed + LOD)

### 1.3 Sensors & Arcs Overlay
- Visualize sensor modalities (LOS/sound/smell; ship sensors; weapon arcs/ranges)
- Render volumes as primitives: cone / sphere / capsule / arc sector / rays

### 1.4 Inspector "Thinking" Tree
Tree-based by default; timeline can be added later.
- Perception (from sensor limbs)
- Affordances (from actionable limbs + locomotion constraints)
- Desires/weights (wants, wishes, ambitions, etc.)
- Decision (goal/task stack + reason)
- Intent (true) + Disguise (presented) + Communications + "disguised intent payload"

Reveal Modes:
- **Dev mode**: show true intent + disguise
- **Player-like mode**: hide true intent (optional for later)

---

## 2) Conceptual Data Contracts (minimal shapes)

### 2.1 Knowledge Graph

**KnownEntry** (per observer → target, directed)
- `TargetId`
- `SourceMask`: Seen | Met | Faction | Rumor
- `Confidence` (0..1): certainty of identity / correctness
- `MemoryStrength` (0..1): stickiness / resistance to forgetting
- `SalienceMask`: suspicious/crime/ritual/combat/etc.
- `LastSeenTick`, `LastMetTick`, `LastUpdatedTick`
- `Flags`: SuspiciousSticky, FocusPinned, etc.

**SalientObservation** (per observer, bounded ring buffer)
- `TargetId` (or BundleId)
- `Tag` (enum)
- `Tick`, `PartitionId`
- `Confidence`, `EvidenceStrength`

> **Hindsight** is implemented by event-driven scans over this ring buffer, not per-target deep history.

### 2.2 Relation Graph

**RelationEdge** (per source → target, directed)
- `TargetId`
- `BaseStrength` (0..100; neutral default 50)
- `TypeMask` (relation types present)
- `TypeModifiers`: small list `{type, mod}` (avoid huge per-type arrays)
- `LastInteractionTick`
- Optional: `DecayLockMask` (conditions prevent passive decay)

### 2.3 Bundles / Group Nodes

Bundles are first-class nodes (entities) that represent:
- Village, Army, Guild, Band, Business, Faction, Fleet, etc.

They reduce graph density by allowing most edges to target the group rather than thousands of individuals.

---

## 3) Policy Tables (the flexibility core)

All policies below are **data**; swap per game, per faction, per difficulty, per debug mode.

### 3.1 Defaults (tune freely)

```yaml
Defaults:
  relation_neutral: 50

  # IMPORTANT: caps are hard scalability levers. Start conservative; raise only if needed.
  known_caps:
    Seen:   256
    Met:     64
    Faction:128
    Rumor:  128

  salient_observation_ring_size: 128

  hover_top_k_relations: 8
  max_lines_drawn_per_selection: 128

  rewind_window_seconds_default: 600   # 10 minutes (adjustable)
```

### 3.2 KnowledgeSourcePolicy

```yaml
KnowledgeSourcePolicy:
  Seen:
    cap: Defaults.known_caps.Seen
    confidence_gain:
      time_in_view: 0.002        # per observe step
      observe_action_mult: 4.0   # deliberate "observe"
      sensor_quality_mult: true
      skill_mult: [WIS, INT, Perception]
    memory_gain:
      time_in_view: 0.001
      observe_action_mult: 3.0
      salience_bonus: true
    forget_bias: high
    sticky_if_salient: true

  Met:
    cap: Defaults.known_caps.Met
    confidence_min: 0.6
    memory_strength_min: 0.6
    forget_bias: low
    sticky_by_context: true       # crimes, contracts, oaths, high-salience meetings

  Faction:
    cap: Defaults.known_caps.Faction
    confidence_min: 0.4
    forget_bias: medium

  Rumor:
    cap: Defaults.known_caps.Rumor
    confidence_min: 0.2
    decay_confidence_fast: true
```

### 3.3 KnowledgeEvictionPolicy

```yaml
KnowledgeEvictionPolicy:
  # Lower score → evict first when source cap exceeded.
  keep_score_weights:
    memory_strength: 0.45
    relation_importance: 0.25
    salience: 0.20
    recency: 0.10
  evict_lowest_first: true
```

### 3.4 Salience Tags (initial vocabulary)

Keep this list **small** initially; expand later.

```yaml
SalienceTagsV1:
  - SuspiciousActivity
  - ViolenceOrAttack
  - TheftOrTrespass
  - MiracleOrAnomaly
  - RitualOrWorship
  - TradeOrContract
  - LeadershipEvent
  - BetrayalOrDeception
  - RescueOrAid
  - DeathOrDisappearance
```

### 3.5 HindsightPolicy (retrospective reinterpretation)

```yaml
HindsightPolicy:
  triggers:
    - CrimeDiscovered
    - AttackOccurred
    - MiracleCast
    - BetrayalRevealed
    - DeathInvestigated

  scan_scope:
    by_partition_radius: true
    include_faction_knowledge: optional

  search_window_ticks: configurable

  upgrades_on_match:
    increase_salience: true
    increase_confidence: true
    relation_modifiers_add:
      - Fear
      - FriendFoe
```

> **Budget guardrail**: only scan each observer's ring buffer (bounded), and only for observers in scope.

### 3.6 RelationTypePolicy

Relation types (directed):
- friend/foe, owner, faction, guild, band, army, village, business, companions, family, lovers, loyalty, debt, fear, master, apprentice

```yaml
RelationTypePolicy:
  neutral: Defaults.relation_neutral

  # Each type declares:
  # - semantic_color (for default line color)
  # - importance_weight (for top-K selection)
  # - retention_class (maps to passive decay floors)
  # - bundle_preferred (edges should prefer group nodes when possible)
  Family:      { semantic_color: family,      importance_weight: 3.0, retention_class: High,   bundle_preferred: false }
  Lovers:      { semantic_color: lovers,      importance_weight: 3.0, retention_class: High,   bundle_preferred: false }
  Loyalty:     { semantic_color: loyalty,     importance_weight: 2.5, retention_class: High,   bundle_preferred: true  }
  Master:      { semantic_color: master,      importance_weight: 2.5, retention_class: High,   bundle_preferred: false }
  Apprentice:  { semantic_color: apprentice,  importance_weight: 2.5, retention_class: High,   bundle_preferred: false }

  Companions:  { semantic_color: companions,  importance_weight: 2.0, retention_class: Medium, bundle_preferred: false }
  FriendFoe:   { semantic_color: friendfoe,   importance_weight: 2.0, retention_class: Medium, bundle_preferred: false }
  Debt:        { semantic_color: debt,        importance_weight: 1.8, retention_class: Medium, bundle_preferred: false }

  Fear:        { semantic_color: fear,        importance_weight: 1.8, retention_class: Low,    bundle_preferred: false }

  Faction:     { semantic_color: faction,     importance_weight: 1.5, retention_class: Medium, bundle_preferred: true  }
  Guild:       { semantic_color: guild,       importance_weight: 1.5, retention_class: Medium, bundle_preferred: true  }
  Village:     { semantic_color: village,     importance_weight: 1.5, retention_class: Medium, bundle_preferred: true  }
  Army:        { semantic_color: army,        importance_weight: 1.5, retention_class: Medium, bundle_preferred: true  }
  Band:        { semantic_color: band,        importance_weight: 1.4, retention_class: Medium, bundle_preferred: true  }
  Business:    { semantic_color: business,    importance_weight: 1.2, retention_class: Medium, bundle_preferred: true  }
  Owner:       { semantic_color: owner,       importance_weight: 2.2, retention_class: High,   bundle_preferred: false }
```

### 3.7 RetentionClasses (passive decay floors + decay rates)

Decay is toward neutral, but passive decay cannot cross a type-dependent floor.

```yaml
RetentionClasses:
  High:
    passive_retention: 0.75    # strong ties remain strong passively
    decay_rate_to_neutral: slow

  Medium:
    passive_retention: 0.50
    decay_rate_to_neutral: medium

  Low:
    passive_retention: 0.25
    decay_rate_to_neutral: fast
```

**Conceptual passive floor math** (applies only to passive decay):
- Let `Neutral = RelationTypePolicy.neutral`
- Let `d = BaseStrength - Neutral`
- `PassiveMin = Neutral + d * passive_retention`
- Passive decay cannot reduce `BaseStrength` below `PassiveMin`
- Active events can override (betrayal, coercion, proof, etc.)

### 3.8 ImportancePolicy (hover top-K vs selected all)

```yaml
ImportancePolicy:
  hover_top_k: Defaults.hover_top_k_relations
  select_show_all_immediate: true
  collapse_low_importance_into_other: true
  max_lines_drawn_per_selection: Defaults.max_lines_drawn_per_selection

  # Edge importance score (conceptual):
  # importance = type_importance * (1 + strength_factor) + recency_factor + salience_factor + player_focus_bonus
  weights:
    strength: 1.0
    recency: 0.6
    salience: 0.8
    player_focus: 2.0
```

### 3.9 VisualizationPolicy (semantic/state colors, bundling, LOD)

```yaml
VisualizationPolicy:
  default_color_mode: Semantic
  allow_state_color_toggle: true

  bundling:
    enabled: true
    show_bundle_nodes: true
    expand_bundle_on_click: true

  world_lines:
    draw_only_for: [Hovered, Selected, Pinned]
    distance_lod: true
    thickness_by_importance: optional
    max_lines_drawn: Defaults.max_lines_drawn_per_selection
```

### 3.10 InspectorPolicy (tree + reveal modes)

```yaml
InspectorPolicy:
  sections:
    - Overview
    - Relations
    - GoalsAndTasks
    - Perception
    - Affordances
    - Desires
    - Decision
    - IntentAndDisguise
    - Communications
    - SensorsAndArcs

  reveal_modes:
    Dev:
      show_true_intent: true
      show_disguise: true
    PlayerLike:
      show_true_intent: false
      show_disguise: true
```

---

## 4) Rewind Plan (pause-until-caught-up)

Rewind is the primary debugging lever:
- Default window: **10 minutes real-life**, adjustable
- Scope modes: **global**, **per-entity**, **local radius around entity/miracle**, adjustable
- UX rule: rewinding **pauses** until the world catches up, then resumes

### 4.1 RewindPolicy

```yaml
RewindPolicy:
  window_seconds_default: Defaults.rewind_window_seconds_default
  checkpoint_spacing_ticks: configurable
  pause_until_caught_up: true

  # Log what is required for sim correctness:
  log_channels_v1:
    - PlayerInputs
    - RandomSeedsOrRngStream
    - StructuralChanges
    - Communications
    - MajorEvents

  # Optional (high value for debugging; enable when affordable):
  log_channels_optional:
    - AIKeyDecisions
    - SuspicionAndHindsightEvents

  # Derived during catch-up (no need to store):
  derived_on_catchup:
    - animations
    - cosmetic_fx
    - low_priority_ui_state
```

### 4.2 Fidelity Levels (opt-in per subsystem)

- **Level 0 (Derived)**: recompute on catch-up (cheap storage, more CPU during rewind)
- **Level 1 (Event Logged)**: store inputs/events (good default)
- **Level 2 (Delta Logged)**: store state deltas for hard-to-recompute state (use sparingly)

---

## 5) Execution Order (build plan)

1) **Define enums + tables**
   - Knowledge sources, relation types, retention classes, salience tags, reveal modes, overlay modes

2) **Implement KnowledgeGraph + Eviction**
   - KnownEntry storage (per observer), ring buffer of SalientObservations, eviction scoring

3) **Implement RelationGraph + Bundles**
   - RelationEdge storage (directed), multi-type modifiers list, group node entities and membership

4) **Importance scoring + Selection sets**
   - Hovered / Selected / Pinned sets
   - Extract top-K edges for hover; full immediate edges for selected (with collapse option)

5) **Overlays V1**
   - Relation lines (semantic/state toggle)
   - Bundle nodes + expansion
   - Comms pulses (sender → receiver, optional by channel)

6) **Sensor/Weapon overlays V1**
   - Volumes for modalities + arcs/ranges; show hits/contained targets optionally

7) **Inspector V1**
   - Goal/task + reason + intent/disguise + comms + sensors/affordances
   - Reveal mode toggle

8) **Rewind backbone V1**
   - Checkpoint restore + event log catch-up sim
   - Pause UI during catch-up, resume when coherent

9) **Hindsight hook (optional V1.1 but design-ready)**
   - Emit SalienceObservations
   - Trigger retrospective scans on major events

---

## 6) Extension Points (future-proof without redesign)

- **Belief / inference layer**: observer's inferred intent (separate from true intent)  
- **Rumor propagation**: comms graph injects Rumor knowledge with confidence decay  
- **Timeline inspector**: show decision/comms/event timeline per entity  
- **System interaction viz**: event flow + per-system counters correlated with overlays  
- **Additional overlays**: influence fields, path previews, contention hotspots, etc.

---

## 7) Notes (performance guardrails)

- Never draw or traverse the full world relation graph. Always scope by selection + caps + bundling.
- Never store per-target deep history. Use bounded ring buffers of salient observations.
- Keep relation modifier storage sparse (list of present types), not full arrays per edge.
- Rewind storage must not be full snapshots every tick. Use checkpoints + event log.

---

## 8) TODO knobs (fill/tune anytime)
- Caps: Known entries per source (Seen/Met/Faction/Rumor)
- Ring buffer size for salient observations
- Salience tags vocabulary
- Retention class offsets + decay rates per type
- Hover top-K and line rendering LOD thresholds
- Rewind checkpoint spacing + logged channels
