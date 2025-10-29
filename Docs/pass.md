Keep the architecture, but tighten it around a desired-set → diff → bounded loader pattern with hysteresis, priorities, and observability. Add lightweight editor validators and a few runtime guards so designers can’t “shoot the loader in the foot”.

What’s strong already

Data separation: authoring → baked blobs for descriptors & focus points ✔︎

Deterministic backbone: scan → emit commands only in “record mode” ✔︎

SceneSystem integration: systems/asmdef set up to call Load/Unload ✔︎

State mirror: a pass that reflects actual Scene status back into ECS ✔︎

Gaps & risks to close

Thrash risk: without hysteresis or cooldowns, fast focus motion can spam load/unload.

Non-bounded concurrency: SceneSystem can pile up async ops and hitch.

No prioritization: near/far sections treated equally → poor camera feels.

No protection from content mistakes: missing Scene GUIDs or overlapping sectors.

Multi-focus edge cases: two players/camera cuts; unioning desired sections.

No perf/telemetry: hard to answer “why didn’t it load?” or “what’s the queue?”

Determinism checkpoints: record/playback isn’t verified yet.

World isolation: ensure each World’s streaming only touches its own SceneSystem.

- Immediate next steps (1–2 days)
  - [x] Make the scanner diff-based (idempotent) with Desired∖Loaded diffs.
  - [x] Add hysteresis to section radii (Enter vs Exit).
  - [x] Bound the loader (concurrency and per-tick caps).
  - [x] Prioritize work by distance/heading.
  - [x] Support multi-focus union.
  - [x] Add an editor validator for common content mistakes.
  - [x] Surface instrumentation & a lightweight debug overlay.
  - [x] Harden safety/invariants (cooldowns, single pending op).

Immediate priority

- [ ] Telemetry: record the first load/unload ticks and queue depth in the overlay so changes in demand are obvious on first play.
- [ ] Guardrails: enforce the single-op-per-section invariant, show active cooldowns (with a debug “clear cooldowns” button), and assert each streaming system only touches its own world’s SceneSystem/coordinator.
- [ ] Tests: add PlayMode smoke/zig-zag/multi-focus coverage plus an EditMode validator test to lock the pipeline down.

Short backlog

Designer preview UI: in-scene gizmos for section bounds, live labels for state (Desired/Loading/Loaded/Pinned/Cooldown).

Pins & sticky loads: a Pinned enableable component so scripts (cutscenes/boss rooms) can hold a section loaded regardless of focus.

Budget-aware eviction: optional memory budget (rough estimate per section) → unload farthest first when over budget.

Look-ahead prefetch: ahead along focus velocity & path nodes (if you have a grid), limited by PrefetchBudget.

Record/Replay tests: record the diff stream over a canned focus path, then replay in a clean World and assert exact state transitions and timings.

World topology guards: assert that each World has its own SceneSystem and streaming state singleton; forbid cross-world commands.

Build-pipeline hook: a prebuild check that ensures the SubScenes referenced by descriptors are included in the Entities/Scenes build artifacts.

Error report surface: aggregate errors into a single “Streaming Health” report asset in the Project window.

Concrete API/data tweaks

Descriptor blob: add EnterRadius, ExitRadius, Priority (small int), and an optional EstimatedCost (bytes/time hint).

Runtime state per section: State (Idle/QueuedLoad/Loading/Loaded/QueuedUnload/Unloading/Error), LastSeenTick, CooldownUntilTick, PinCount.

Focus data: world-space Position, Velocity, RadiusScale (for zoomed-out cameras), optional FollowEntity resolved by your focus-updater.

Queues: StreamingQueue entity with two DynamicBuffers (Loads, Unloads), each entry holds SectionId + Reason + Score.

Events: single-frame buffers for OnSectionLoaded, OnSectionUnloaded so other systems (AI, audio) can react without polling.

System order (suggested)

FocusUpdateSystem (Presentation or Simulation, before scanning): resolves FollowEntity → writes Position/Velocity.

DesiredSetBuildSystem: computes desired set & scores (read-only baked descriptors).

StreamingDiffSystem: diffs Desired vs Loaded → writes to queues (bounded by per-tick limits).

StreamingLoaderSystem: pops queues (respecting MaxConcurrentLoads) → calls SceneSystem.LoadSceneAsync/UnloadScene.

StreamingStateSyncSystem: mirrors SceneSystem status → updates section states, fires events.

StreamingCooldownSystem: handles error cooldowns, clears stale queued ops.

All of the above live in your PureDOTS project as an opt-in “Streaming” feature module. Games only place authoring components and, optionally, pins or custom prioritizers.

Editor tooling checklist (tiny but high impact)

Menu: PureDOTS/Streaming/Validate Sections → opens a window with a sortable list (Missing GUID, Overlap, Zero Radius, Duplicate IDs).

Scene gizmos: draw Enter/Exit radii and label current state.

Playmode toolbar badge: shows counts (Desired/Loading/Loaded) and a warning icon if any Error state exists.
What to do next (in order)
1) Wire it into a real game world (1–2 hours)

Enable the feature in the game composition root (game-side):

Add your Streaming feature to the world spec where gameplay runs (usually the Client/SinglePlayer world, FixedStep group).

Set conservative defaults in the Coordinator (MaxConcurrentLoads=1–2, MaxLoadsPerTick=1, MaxUnloadsPerTick=1) for first pass.

Place authoring:

Tag 3–6 SubScene anchors with StreamingSectionAuthoring.

Add a StreamingFocusAuthoring to camera or player (velocity capture on).

Drop the debug overlay in a test scene so designers can see queue/active/loaded/error at runtime.

Goal: get first loads/unloads happening with visible telemetry.

2) Add “don’t-shoot-yourself” guardrails (same day)

One-op-per-section invariant (if not already): a section can be Idle/Queued/Loading/Loaded/QueuedUnload/Unloading/Error—never two at once.

Cooldown auditing: surface the current cooldown list in the overlay and expose a “clear cooldowns” button (debug only).

World isolation: assert that scanner/loader/state systems only touch the local world’s SceneSystem and streaming singletons.

3) Bake in test coverage (1 day)

Create three PlayMode tests and one EditMode test (engine-side):

Smoke path

Straight line through 3–5 sections.

Asserts: no thrash (≤1 load per boundary crossing), final Desired==Loaded, queue drains to zero.

Zig-zag hysteresis

Oscillate across a boundary at high frequency.

Asserts: commands don’t flip-flop; Enter/Exit radii respected.

Multi-focus union

Two focuses diverge then converge.

Asserts: section only unloads when no focus retains it; union semantics verified.

Validator test (EditMode)

Feed fake descriptors: missing GUID, overlap, zero radius.

Asserts: each error reported exactly once.

Bonus: Replay determinism

Record the diff stream for a canned path → replay in a fresh world. Assert identical state timeline and counts.

4) Lock tuning knobs & profiles

Ship config assets (engine-side) that games can select:

Profiles: Conservative, Default, Aggressive.

Conservative: MaxConcurrent=1, Loads/Tick=1, Unloads/Tick=1, Enter=1.2×Exit, PrefetchBudget=0.

Default: MaxConcurrent=2, Loads/Tick=2, Unloads/Tick=2, Enter=1.15×Exit, PrefetchBudget=1–2.

Aggressive: MaxConcurrent=4, Loads/Tick=4, Unloads/Tick=2, Enter=1.1×Exit, PrefetchBudget=3–4.

Priority & cost policy (documented): score = distance bias – priority weight – heading bonus + cost penalty.

5) Designer UX polish (quick wins)

Scene gizmos: Draw Enter/Exit rings and label state (Desired/Loading/Loaded/Cooldown).

Validator window: Add a “Select offenders” button + “Fix common issues” (e.g., set default radii).

Overlay toggles: show/hide queues, color by state, highlight cooldown/error.

6) Memory/IO safety net (next 1–2 days)

Soft memory budget per world (config): when EstimatedCost sum of Loaded exceeds budget, auto-queue farthest-for-unload (respect Pins).

Pins: expose Pinned enableable + simple API Pin(sectionId) / Unpin(sectionId); overlay shows PinCount.

Failure telemetry: keep rolling counts & last error string per section; add a one-click copy of a “Streaming Health Report” to clipboard.

7) Build pipeline & CI

Prebuild check (Editor script): ensure every referenced Scene GUID is in build artifacts; error out on missing.

Headless test: run the smoke/zig-zag tests in CI (PlayMode in batchmode).

Perf sampling: log average load time, max active loads, and total section count per test run (CSV artifact).

8) Integration contract (docs you should freeze now)

Authoring rules: how to sized radii, naming sections, overlap policy, priority ranges, cost estimation guidance.

Runtime events: expose single-frame buffers OnSectionLoaded / OnSectionUnloaded for other systems to react (AI, audio, nav).

Composition knobs: what’s per-world vs global, how to override the default profiles in a game project.

9) Optional—but high ROI soon

Look-ahead prefetch along velocity/path nodes (capped by PrefetchBudget).

Designer preview mode: in-editor simulate a focus path (without entering Play Mode) to preview load order.

Pluggable prioritizer: interface so a game can swap the score function (e.g., favor quest-critical sections).

“Go/No-go” checklist for calling the feature “engine-ready”

✅ First-play demo scene loads/unloads deterministically with overlay visible

✅ Four tests pass (smoke, zig-zag, union, validator) + optional replay determinism

✅ Validator reports zero errors in your sample content

✅ Profiles exist and the game can switch them

✅ Prebuild check prevents missing GUIDs

✅ One-op-per-section invariant enforced (and asserted)

✅ Cooldowns surfaced & clearable in debug

Suggested defaults to start with

EnterRadius = ExitRadius * 1.15

MaxConcurrentLoads = 2

MaxLoadsPerTick = 2, MaxUnloadsPerTick = 2

Heading bonus = clamp(dot(velocityDir, toSectionDir), 0..1) * small_bias

Score sort = lowest score first (distance-biased); stable-sort by GUID for determinism ties

Cooldown after failure = 3–5s (scaled by EstimatedCost)
