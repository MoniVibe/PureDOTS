1) Ship three reusable spines (once) in PureDOTS, consume them in both games

Why: one investment, two demos; zero per-game hacks. 

TRI_PROJECT_BRIEFING

A. Frame/Time spine (deterministic core, rewind-safe)

Singletons: TickTimeState (tick, timescale, paused), RewindState (mode, targetTick).

Groups: Initialization → Simulation → FixedStepSimulation → Presentation. All gameplay resides in Simulation/FixedStep; Presentation is read-only and spawn/cleanup happens via ECBs on group boundaries.

Logs/budgets: ring buffers for InputCommandLog and SnapshotLog with fixed capacities (e.g., N = seconds * 60). Enforce budgets with asserts; expose via debug UI.

Contracts: systems that mutate state in Presentation throw (editor) or defer via ECB; FixedStep ticks advance only when TickTimeState.playing && !RewindState.active.

Definition of Done (spine A): pause/play/step works at variable frame rates; can rewind 2–5 seconds and resimulate deterministically; 6–10 PlayMode tests cover pause/rewind/step & fixed-tick determinism.

B. Presentation spine (binding without assets)

Canonical data (cold, swappable):

Presentable tag + PresentationBinding (BlobAsset): maps logical IDs (e.g., “FX.Miracle.Heal”, “Mesh.Unit.Basic”) to presentation kinds (Mesh, VFX, SFX, UI) and style tokens (palette indices, size).

Request buffers (hot, ephemeral):

PlayEffectRequest { EffectId, AttachTo, Lifetime }

SpawnCompanionRequest { CompanionKind, AttachTo }

DespawnCompanionRequest { Entity }

PresentationCleanupTag (auto-despawn after lifetime)

Ordering & safety:

Gameplay writes requests in (FixedStep) Simulation.

BeginPresentationECBSystem plays back creates/attaches.

Presentation systems render or drive companions read-only; EndPresentationECBSystem handles despawn/cleanup.

Editor-only guard system asserts on structural changes inside Presentation (other than ECB playback).

Minimal MB glue (hybrid-safe):

PresentationBridge (single MB per scene) hosts pools for placeholder meshes/particles and VFX Graph stubs; it only reacts to ECS requests by ID, never by GameObject reference.

Scene checklist: one PresentationBridge, one CameraRig, optional HUDDebug. Zero direct references from ECS to MB.

Testing: spawn N effect requests; assert pool reuse, no leaks, zero structural changes outside ECB windows.

Definition of Done (spine B): same ECS build runs with “graybox” placeholders today and prettier assets later by swapping the PresentationBinding Blob or Bridge table—no code changes.

C. Registry/Continuity spine (content without commitment)

Schema: RegistryId, DisplayName, ArchetypeRef, SpawnPrefabRef? (optional!), ContinuityMeta { Version, Residency, Category }, TelemetryKey.

Authoring: ScriptableObject registries bake to BlobAssets; per-game adapters live under Assets/Scripts/*/Registry/… but call the same PureDOTS bakers.

Continuity checks: an EditMode test validates cross-registry ID uniqueness, version sync, and residency consistency before entering PlayMode.

Legacy shims: optional HybridPrefabGuid field used only by the PresentationBridge when a pure ECS mesh isn’t present yet.

Definition of Done (spine C): both games can register “units/effects/resources” using the same schema; continuity tests pass; presentation works with or without actual meshes.

2) Vertical demo slices (per game) built only on the spines
Godgame demo (single scene)

Goal: prove input→gameplay→presentation→rewind loop with zero asset debt.

Slice 1 – Move & Act: WASD pans camera; click spawns a “Band” entity from the Registry; pressing Q enqueues PlayEffectRequest("FX.Miracle.Ping") on the selected target.

Slice 2 – Construction stub: hotkey places a “Jobsite” ghost (pure ECS primitive); finishing triggers another effect request + a HUD metric bump (telemetry counter).

Slice 3 – Time demo: hold R to rewind 3 seconds; verify entities and effects resimulated deterministically (tests assert snapshot bytewise equality for a tiny world).

Acceptance: no GameObject references in gameplay; removing the PresentationBridge still yields a running headless sim; all three slices covered by PlayMode tests.

Space4x demo (single scene)

Goal: minimal mine→haul loop using the same spines.

Slice 1 – Mining: a Miner ship reads input (or scripted order), ticks a “Mining” state machine in FixedStep, emits PlayEffectRequest("FX.Mining.Sparks").

Slice 2 – Carrier: on threshold, emit SpawnResource entities; Carrier auto-picks via FindNearest system; HUD increments “ore in hold”.

Slice 3 – Time demo: rewind during transfer; the same count/state replays.

Acceptance: all visuals are placeholders driven by PresentationBinding; removing bindings leaves the sim intact.

3) Concrete checklists to close your reported gaps
Presentation gaps → fixes

Companion/presentation layer: implement the Request/Binding pattern above; define hot/cold archetypes; add ordering attributes:

PresentationBridgePlaybackSystem : [UpdateInGroup(PresentationSystemGroup), UpdateBefore(EntitiesGraphicsSystem)]

PresentationCleanupSystem : [UpdateInGroup(PresentationSystemGroup), UpdateAfter(EntitiesGraphicsSystem)]

MonoBehaviour glue: one PresentationBridge with pools for: mesh primitive, particle stub, VFX Graph stub, audio stub. Public “Style” scriptable lets designers map IDs→placeholders without code.

Docs & tests: fill Docs/TODO/PresentationBridge_TODO.md with the ID taxonomy, request lifetimes, and scene checklist; add PlayMode tests for spawn/cleanup and ID lookup failure paths.

Time/rewind determinism gaps → fixes

Snapshot/command log: implement fixed-size ring buffers; document memory budgets (e.g., 120 ticks of snapshots max); enforce via asserts.

Group contracts: tie FixedStepSimulationSystemGroup to TickTimeState (tick only when playing) and gate resim on rewind; author Docs/DesignNotes/SystemExecutionOrder.md & RewindPatterns.md with the contract and code examples.

Tests: EditMode (serialization of snapshots, command packing), PlayMode (pause, variable FPS determinism, rewind/step-back).

Registry slices gaps → fixes

Coverage: stub schema entries for miracles/bands/creatures/threats/construction/jobs/logistics; each gets TelemetryKey and continuity meta.

Continuity snapshots: pre-PlayMode validation that every runtime entity spawned from a registry carries a ContinuityMeta.Version == Registry.Version.

Telemetry/metrics: minimal HUD/debug buffers fed by systems (no UI dependency). Expose counters via an in-game console or simple overlay.

Tests/docs gaps → fixes

Add a single TestAssembly per project with categories: Spine.Time, Spine.Presentation, Registry.*, Demo.*.

Update TruthSources + TODO cross-links to point at the new documents and fixtures; block PRs on those tests.

4) Tiny, asset-agnostic code seeds (drop into PureDOTS)
public struct TickTimeState : IComponentData { public int Tick; public float TimeScale; public bool Playing; }
public struct RewindState  : IComponentData { public bool Active; public int TargetTick; }

public struct PlayEffectRequest  : IBufferElementData { public FixedString64Bytes EffectId; public Entity AttachTo; public float Lifetime; }
public struct SpawnCompanionRequest : IBufferElementData { public FixedString64Bytes CompanionKind; public Entity AttachTo; }
public struct DespawnCompanionRequest : IBufferElementData { public Entity Companion; }

public struct PresentationBinding : IComponentData { public BlobAssetReference<BindingBlob> Blob; }
// Blob maps logical EffectIds / MeshIds to placeholder assets or style tokens. (No hard asset refs in ECS.)


Gameplay systems: write to PlayEffectRequest in FixedStep.

Presentation systems: read requests, call into the pooled bridge by ID; never touch GameObjects from ECS.

Cleanup: PresentationCleanupSystem converts expired lifetimes to DespawnCompanionRequest.

5) Guardrails that keep spaghetti out

Asmdef boundaries: PureDOTS.Runtime (ECS), PureDOTS.Presentation (bridge only), Game.* (adapters). No cross-refs from Runtime → Presentation.

Zero asset coupling in ECS: only IDs + style tokens in data; bindings live in blobs or SOs.

ECB discipline: all structural changes via Begin/End group ECBs.

Hybrid safety: an editor-only PresentationStructuralChangeGuardSystem that throws on illegal changes.

Feature flags: EnableRewind, EnableTelemetry, EnableHybridFallback—configurable per-scene Scriptable.

6) What each demo proves (and why you won’t regret it)

You validate input→sim→present with deterministic time.

You can swap visuals/VFX by editing bindings, not gameplay.

Registries, continuity, and telemetry are shared across both games.

Every slice is test-backed; no hidden GameObject references or hardcoded assets slip in.
