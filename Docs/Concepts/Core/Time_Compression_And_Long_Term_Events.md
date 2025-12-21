# Time Compression & Long-Term Events — LOCKED Concept Spec (PureDOTS / Space4x / Godgame)

**Status:** Locked for implementation (flexible & tunable via profiles/policies; deterministic core).  
**Primary goals:** seamless presentation, tick-truth simulation, global + local time manipulation (including rewind), long-term phased events, adaptive time compression, centuries of queryable history.

---

## 0) Design principles (invariants)

1. **Simulation truth is fixed ticks (deterministic).**  
   Presentation is continuous and must never dictate simulation outcomes.

2. **Everything tunable is data-driven via Profile/Policy IDs.**  
   Hot-path code stays branch-light, Burst-friendly, and deterministic.

3. **Local time manipulation is spatial.**  
   Time effects apply to a **volume/bubble** ("TimeDomain") and affect only entities inside.

4. **Rewind affects only entities inside the bubble.**  
   Nothing outside is "pulled back" or retroactively changed.

5. **Deterministic randomness.**  
   Any tie-break or stochastic resolution is deterministic from a global root RNG stream, optionally "seed-locked" per system per player settings.

---

## 1) Core concepts

### 1.1 Global time
- `GlobalTick`: authoritative baseline tick counter (monotonic forward).
- Global time exists even if some domains run slower/faster/rewind.

### 1.2 TimeDomain (local time bubble)
A **TimeDomain** is a spatial volume that applies time manipulation to entities inside it.

- `TimeDomainProfileId` (data): overlap/arbitration/boundary/settle rules, caps, slider mapping.
- `Scale`: time multiplier (float) applied to tick advancement:
  - `Scale > 1` : fast-forward
  - `0 < Scale < 1` : slow-motion
  - `Scale = 0` : pause
  - `Scale < 0` : rewind (negative direction)

**Membership:** entities are considered "inside" based on spatial containment (volume/bubble).

### 1.3 Effective time within a domain
Each domain tracks its own effective time cursor:
- `DomainTick` (int) + `DomainAccumulator` (float)  
  `DomainAccumulator += Scale` each frame (or control update), then converts into integer steps.

This allows smooth scaling without fractional simulation ticks.

---

## 2) Cross-domain interaction & arbitration

### 2.1 Arbitration rule ("latest arbitrates")
When two entities from different domains interact, the interaction is processed in the context of the entity/domain with the **latest effective time**.

Define:
- `EffectiveTick(entity) = GlobalTick + DomainLocalOffset + DomainTick` (conceptual)
- Winner = participant with max `EffectiveTick`
- Fallback: **global authoritative tick** when ambiguous

**Tie-break:** deterministic RNG derived from the root stream (see §7).

### 2.2 Overlap rule (default, tunable)
If multiple TimeDomains overlap a position:
1. **Priority wins** (profile-driven integer priority)
2. If equal priority: **latest effective time wins**
3. If still tied: deterministic RNG tie-break

This is tunable in `TimeDomainProfile`.

### 2.3 Boundary rule during rewind (locked)
While a TimeDomain is rewinding:
- Entities attempting to cross in/out of the rewinding bubble **freeze in stasis** at the boundary (no crossing).
- Cross-boundary interactions that would create new causal links are **blocked** until rewind ends.

### 2.4 Collision rule during rewind (locked)
- Rewinding entities colliding with boundary-stasis entities:
  - apply **simple push-out/nudge** resolution (no coupled physics, no complex impulses)
  - stasis entities remain inert

---

## 3) Time control UX (player-only)

- Player controls time (global and/or local bubbles).  
- Optional special modes are allowed (e.g., rival "god of time"), implemented as game-side logic that edits domain profiles/scales.
- Player can configure triggers for auto pause/slow (event-driven) and can disable auto-decompression.

**Presentation preference:** continuous animation whenever feasible; LOD/perf may choose simplified presentation far away.

---

## 4) Rewind as core gameplay (locked)

### 4.1 Constraints (locked defaults; tunable by profile)
- **Max rewind window:** **10 minutes equivalent ticks**.
- **Max entities inside a rewind bubble:** **hundreds** (enforced by domain caps).

### 4.2 Default base tick rate (project default, tunable)
To bound memory and keep large-scale sims stable:
- **Default `BaseSimTickRate = 20 Hz`** (configurable).
- Therefore **10 minutes window = 10 * 60 * 20 = 12,000 ticks** by default.

> This is a default for Space4x/Godgame scope; it remains configurable per project and per domain profile.

### 4.3 Rewindable state is profile-driven (flexible)
Rewind supports a configurable set of components (examples):
- Health
- Inventory
- Production progress
- Relations state (if desired)
- Fire state
- Perception caches (optional)
- Transform/Velocity (usually required for smooth rewind)

**Rule:** what rewinds is defined by `RewindProfileId`. Adding/removing rewindable components is supported without redesign.

### 4.4 AI behavior during rewind (locked)
- **No planning during rewind.**
- After rewind ends, a short **settle hiccup** runs (profile-driven), then planning resumes.

**Default settle recipe (tunable):**
1. collision stabilization (push-out)
2. path revalidation for Tier-0/Tier-1 entities
3. event resync (phases + queues) for affected actors
4. perception refresh (near player)

---

## 5) Rewind implementation model (deterministic + bounded)

### 5.1 Recording activation (recommended default)
- Rewind recording is **armed only** when a domain is rewind-enabled/eligible (profile-driven).
- Non-rewind domains incur no ring-buffer memory.

### 5.2 Ring buffer strategy (default; tunable)
To avoid full snapshot cost:
- **Keyframes** every `K` ticks (default `K = 10`, tunable).
- **Deltas** between keyframes:
  - On-change patches for discrete state changes (inventory edits, damage, phase transitions, production increments).
  - Optional transform deltas for presentation smoothness.

Replay during rewind:
- restore state from recorded buffers for `tick-1`
- run minimal collision push-out
- skip expensive simulation systems + planning

### 5.3 Structural changes inside rewind domains (recommended default)
For stability + low cost:
- Prefer **enable/disable pooling** over spawn/destroy inside rewind bubbles during the window.
- If true spawn/destroy is required, use stable IDs + indirection (slower; optional policy).

---

## 6) Long-term events (hybrid, phased, interruptible)

### 6.1 Event model (locked)
- Events are **hybrid** and consist of **phases**.
- Phases can be **inserted/removed dynamically**.
- Events can be **paused / delayed / interrupted / cancelled**.
- Events can produce **intermediate outputs of any kind** and spawn:
  - other events
  - situations
  - crises

### 6.2 Intermediate outputs enabling other systems (locked)
Example: "half-built wall enables defense event"
- Intermediate outputs publish **capabilities** (tags, typed resources, or generic effects) usable by other event rules.

### 6.3 Resource policy (flexible)
Per event type:
- **Reserve-upfront** or **pay-as-you-go** resource consumption.
- On cancel: **partial refund by default**, modulated by player settings and per-event overrides.

---

## 7) Deterministic scheduling & RNG

### 7.1 Scheduling order (stable)
Event and simulation execution order within a domain step:
1. Event queue step (phase changes, spawns, interruptions)
2. Simulation step (combat/production/relations/perception/fire/pathing per fidelity)
3. History logging step

Cross-domain interactions are resolved after domain steps using "latest arbitrates".

### 7.2 Deterministic RNG (locked)
- A **global root RNG stream** is the source of entropy.
- It derives stable substreams for:
  - system
  - domain
  - event type
  - event instance
  - phase
  - tie-breaks

### 7.3 Player "seed lock" (locked as feature; per-system toggles)
- Player settings may lock seeds **per system** (combat locked, vegetation unlocked, etc.).
- Rewind respects these toggles to reduce frustration:
  - repeating the same actions yields consistent outcomes in locked systems

---

## 8) Time compression, LOD, and auto-decompression

### 8.1 Adaptive compression (locked)
- Compression is adaptive and controlled by player slider + profiles.
- Domains compute how many ticks to process per frame via `Scale` + accumulator.

### 8.2 Fidelity policy (locked)
High fidelity (especially near player / Tier-0/1):
- combat
- resources & production
- relations (near)
- perception (near)
- fire spread/decline
- pathfinding (near)

Degradable safely (Tier-2/3):
- vegetation
- climate
- wind/water dynamics
- far-away pathfinding approximations

### 8.3 Auto-decompression on "important" events (locked)
- Auto-decompress triggers on "important events".
- Player can disable auto-decompression.

**Importance computation (default; tunable):**
- importance = weighted score from signals:
  - combat involvement
  - threatened player-owned assets
  - discoveries
  - narrative-tagged events
  - Tier-0 involvement
  - etc.

Weights are **authorable per event type** via `EventTypePolicyId`.

---

## 9) History & myths (centuries-scale, queryable)

### 9.1 Canonical ledger (preferred)
- Append-only **event ledger** is the canonical history.
- Target: supports **centuries** and **player-facing queries** (legends/myths).
- Expected peak volume: **thousands of events/day** (validate in profiling).

Ledger record fields (compact):
- time (global tick + domain context)
- location/domain id
- actors
- event type
- severity
- narrative tags / arc ids
- outcomes

### 9.2 Indexes for player queries (required)
Maintain tunable indexes:
- by actor
- by location
- by category/type
- by arc/legend tags

### 9.3 Compaction pipeline (recommended)
To keep performance stable over centuries:
- raw events → daily summaries → chapter summaries → myth/legend entries  
Compaction rules are policy-driven; myth generation is game-side presentation over ledger.

---

## 10) Policy matrix (the tunability backbone)

All tunability is expressed via IDs to Burst-friendly blob/tables.

### 10.1 TimeDomainProfile
- scale curve mapping (player slider → scale)
- overlap rule (priority/latest/forbid)
- arbitration rule ("latest" definition + fallback)
- boundary rule (freeze-on-cross for rewind)
- settle recipe selection
- caps (entity cap, rewind window cap)
- rewind enabled/armed flags

### 10.2 RewindProfile
- rewindable component list
- per-component recording strategy (EveryTick / KeyframeStride / OnChange)
- keyframe stride K
- delta encoding mode
- optional quantization
- structural policy (prefer enable/disable vs stable-id spawn/destroy)
- rewind collision policy parameters

### 10.3 CompressionProfile
- tier thresholds (distance/focus/importance)
- per-tier system decimation rates
- degrade modes by system
- auto-decompress threshold + hysteresis
- player slider mapping

### 10.4 EventTypePolicy
- importance weights
- allowed interruptions
- resource policy defaults + refund defaults
- phase mutation permissions
- output typing policy

### 10.5 RNGPolicy
- root seed derivation
- per-system seed-lock toggles
- substream derivation scheme

### 10.6 HistoryPolicy
- ledger detail level
- indexing budgets/strategies
- compaction rules and thresholds
- narrative tagging rules

---

## 11) Execution order (engine-facing)

### Group A — Input & time control
1. Apply player time controls → update domain scales/profiles.
2. Apply pause/slow triggers (if enabled).

### Group B — Domain scheduling
3. Update each domain accumulator from scale.
4. Convert accumulators → integer tick steps per domain.
5. Build a deterministic task list of domain steps (sorted).

### Group C — Domain step execution
For each domain step in order:
6. If forward:
   - process event queue (phases/spawns/interruptions)
   - process simulation tick (fidelity per tier)
   - log history
   - record rewind buffers (if armed)
7. If rewind:
   - restore recorded state (tick-1)
   - collision push-out + boundary stasis resolution
   - (skip AI planning)

### Group D — Cross-domain resolution
8. Resolve cross-domain interactions using "latest arbitrates".
9. Enforce boundary rules (no crossing during rewind).

### Group E — Resume settle
10. If rewind ended:
   - run settle recipe
   - re-enable planning

### Group F — Presentation
11. Interpolate/extrapolate for visuals (LOD-aware).

---

## 12) Project-scoped defaults (Space4x + Godgame friendly)

These are defaults chosen for your scope; everything remains tunable by profiles.

- `BaseSimTickRate`: **20 Hz**
- `MaxRewindWindowTicks`: **12,000** (10 minutes at 20 Hz)
- `DefaultKeyframeStrideK`: **10**
- `DefaultEntityCapPerRewindBubble`: **512** (safety buffer over "hundreds"; enforceable downwards)
- `DefaultArbitration`: priority → latest → deterministic RNG tie-break
- `DefaultRecording`: armed only for rewind-enabled bubbles; on-change deltas + periodic keyframes
- `DefaultAutoDecompress`: enabled; player can disable
- `DefaultImportance`: authorable weights per event type (combat/assets/discovery/narrative/Tier-0 signals)

---

## 13) Split of responsibilities (PureDOTS vs game-side)

### PureDOTS (shared, game-agnostic)
- TimeDomain runtime + arbitration + scheduling
- Deterministic event queue + phased events runtime
- Rewind recording/playback infrastructure + profiles
- Adaptive compression + tiering infrastructure
- RNG root/substream derivation + seed lock toggles
- History ledger storage + indexing primitives + compaction pipeline hooks

### Game-side (Space4x / Godgame)
- Authoring of profiles/policies (weights, curves, thresholds)
- Narrative tagging, myth generation, UI queries over ledger
- Presentation: animation, VFX, camera, time control UI, "important event" surfacing
- Optional special modes (time-rival, scripted time anomalies)

---

## 14) Notes on performance & stability (guidance)

- Avoid unbounded pairwise systems in high-fidelity tiers; rely on spatial bucketing and capped relationship slots.
- Prefer event-driven changes over per-tick polling where possible.
- In rewind bubbles, prefer pooled enable/disable to keep identity stable.
- Keep policy lookups O(1) via IDs and contiguous tables/blobs.

---

**End of locked spec.**
