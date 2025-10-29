# Divine Hand & Camera Parity TODO

> **Generalisation Guideline**: Treat hand and camera control as reusable interaction modules. Core logic should be data-driven, with configuration assets defining behaviour so different games can adopt the same systems.

## Goal
- Maintain deterministic DOTS hand/camera logic (input routing, state machines, fixed-step transforms) inside the PureDOTS foundation; presentation parity with Black & White 2 remains deferred to downstream game builds.
- Document future parity ambitions so game teams can extend the logical core once they add presentation and fiction-specific feedback.
- Ensure every input flows through deterministic DOTS systems (New Input System → router → DOTS components) with no Mono-only side effects.
- Provide designers with configurable knobs (ScriptableObjects + blobs) for sensitivity, launch behaviour, highlights, and HUD when game layers are ready to consume them.
- Keep contracts in sync with `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and shared integration tasks in `Docs/TODO/SystemIntegration_TODO.md`.

> **Baseline scope note (2025-10-28):** PureDOTS ships only the logical DOTS data flow for hand/camera features. Visuals, feel matching, and fiction-specific behaviours are flagged as deferred below for the first game to implement.

## Plain-Language Primer
- The **divine hand** is the player cursor: it tracks the mouse, shows when objects can be picked up, and manipulates the world (grab, throw, miracles).
- The **RMB router** decides which action wins (UI, storage dump, siphon, etc.) so clicks do the expected thing.
- The **camera** should orbit around the terrain point under the cursor (MMB), pan by “grabbing land” (LMB), and zoom toward the cursor. It also snaps to anchors (temple/creature).
- Everything must be deterministic, Burst-friendly, and rewind-safe.

## Truth-Source References
- `godgame/truthsources/RMBtruthsource.md` — priority table, cooldown/hysteresis, raycast masks, launch behaviour.
- `godgame/truthsources/Hand_StateMachine.md` — state machine (Empty, Holding, Dragging, SlingshotAim, Dumping), events, guards.
- `godgame/truthsources/Cameraimplement.md` — control spec (pivot orbit, grab-land pan, zoom, anchor snaps, sensitivities).
- `godgame/truthsources/Input_Actions.md`, `Input_TimeControls.md` — New Input System action maps and routing expectations.
- `godgame/truthsources/Layers_Tags_Physics.md` — layer assignments for ground, pickables, UI.

## Dependencies & Shared Infrastructure
- Respect environment grid cadence (e.g., moisture/wind/sunlight sampling) when hand systems or camera effects react to climate-driven data.
- Adopt the central `HandInputRouterSystem`, `HandInteractionState`, and shared RMB priority table once landed; avoid bespoke routers.
- Use shared registry utilities/spatial queries for resource/miracle interactions triggered by the hand (piles, storehouses, neutral miracle tokens).

## BW2 Reference Values (Target Parity)
### Camera Settings
- **Orbit Sensitivity**: Close (6-20m): 1.5x | Mid (20-100m): 1.0x | Far (100-220m): 0.6x
- **Pitch Limits**: -30° to +85° (prevents camera flipping)
- **Zoom Range**: 6m minimum to 220m maximum
- **Zoom Speed**: 6 units per scroll tick
- **Pan Scale**: 1.0 (direct 1:1 feel)
- **Terrain Clearance**: 2m minimum above ground
- **Collision Buffer**: 0.4m safety margin

### Hand Settings
- **Pickup Radius**: 3m
- **Hold Lerp Speed**: 8.0 (smooth follow)
- **Hold Height Offset**: 2m above cursor
- **Throw Base Impulse**: 15 m/s
- **Throw Charge Multiplier**: 2.5x at max charge
- **Min Charge Time**: 0.3s (before slingshot activates)
- **Max Charge Time**: 2.0s (charge cap)
- **Post-Throw Cooldown**: 0.1s (spam prevention)
- **Siphon Rate**: 50 units/second from piles
- **Max Hand Capacity**: 500 units

### Input Hysteresis
- **Priority Cooldown**: 0.1s (prevents rapid mode switching)
- **State Change Frames**: 3 frames (smooth transitions)
- **Hover Debounce**: 0.05s (stable highlighting)

## Workstreams & Tasks

### 0. Requirements Reconnaissance
- [x] Audit `DivineHandSystem`, `DivineHandInputBridge`, `RainMiracleSystems`, `BW2StyleCameraController` for current behaviour, non-DOTS patterns, GC allocations.
- [x] Compile gap list vs. truth sources (router ordering, state events, launch modes, camera features, highlight cues).
- [x] Capture BW2 reference values (camera distance, sensitivity curves, cursor feedback) to guide tuning.
- [ ] Verify rewind touchpoints: what data must be rebuilt (hand state, highlight target, camera pivot) during playback/catch-up.

### 1. Input Routing & Configuration
- [ ] **Implement BW2 RMB Priority Router** (deterministic conflict resolution): **In Progress** — base router with cooldown/hysteresis landed; context probe now exposes storehouse/pile/draggable flags; priority table + handler forwarding still pending.
  1. **UI elements** - Always win (buttons, menus, modals) - blocks all game actions
  2. **Modal tools** - Active miracle/special mode has priority
  3. **Storehouse dump** - If hand holds resources AND cursor over storehouse
  4. **Pile siphon** - If cursor over resource pile (can start scooping)
  5. **Object grab** - If cursor over pickable entity (villager, rock, animal, rain cloud)
  6. **Ground drip** - Last resort, drop held object/resources at cursor position
  - Add hysteresis: 3 frames before mode switch (prevents jitter)
  - Add cooldown: 0.1s after throw before next grab (prevents spam)
  - Ensure deterministic raycast order and layer mask checks
- [x] Replace any direct `Input.*` usage: unify around New Input System actions delivered via `PlayerInput`/`InputActionReference`.
- [x] Create `HandCameraInputProfile` ScriptableObject capturing action references, invert toggles, sensitivity multipliers; bake into DOTS config.
- [x] Validate layer/mask assignments during bootstrap; log/warn on mismatch.
- [x] Provide DOTS bridge Mono (optional) that reads actions and writes to `DivineHandInput` component deterministically.
- [ ] Adopt central `HandInputRouterSystem` once integration TODO lands; ensure state transitions respect shared `HandInteractionState` and fixed-step timing.

### 2. Divine Hand Data & State Machine
- [x] Extend component structs: `DivineHandState` (entity, cursor position, aim, held entity, flags, timers), `DivineHandConfig` (pickup radius, lerp speeds, throw impulse curves), `DivineHandInput` (current action bits, charge).
- [ ] **Implement BW2 state machine exactly** (deterministic transitions): **In Progress** — command bridge & state promotion (`SlingshotAim`, `Dumping`, `Dragging`) implemented; deterministic dump/siphon flows still pending.
  - Empty → [RMB press on pickable] → Holding
  - Holding → [RMB release fast (<0.3s)] → Empty (gentle drop)
  - Holding → [hold >0.3s + mouse move] → SlingshotAim (charging)
  - SlingshotAim → [release] → Empty (throw with impulse)
  - Holding → [over storehouse with resources] → Dumping
  - Dumping → [complete transfer] → Empty
  - Include all timers: `cooldownAfterThrowSeconds` (0.1s), `minChargeSeconds` (0.3s), `maxChargeSeconds` (2.0s)
- [ ] **Hand cursor visual states** *(deferred: game-specific presentation)* (match BW2 exactly):
  - Open hand: default hovering state, can interact
  - Closed fist: grabbing/holding object firmly
  - Pointing finger: casting miracle or directing
  - Cupped hand: scooping resources from pile/rain
  - Pulsing/glowing: charging throw or miracle (visual intensity = charge %)
- [x] Emit hand events using DOTS buffers/singletons: `HandTypeChanged`, `HandAmountChanged`, `HandStateChanged` for HUD/analytics.
- [ ] Enforce resource type locking during siphon; guard cross-type operations with deterministic denies and feedback. **Partial** — pickup guard blocks mixed-resource grabs; siphon/dump enforcement outstanding.
- [ ] Support future extension points (e.g., miracles toggling modes) via config fields and event hooks.

### 3. Cursor Access & Highlighting
- [x] Keep hand entity aligned with terrain raycast (ground plane fallback). Update `DivineHandInputBridge` to write world/cursor positions each frame.
- [ ] Implement highlight system *(deferred: presentation-specific)*: when hovering a `HandPickable`, apply pooled highlight VFX (material swap, outline) and revert gracefully. Respect router priority (UI/higher handlers can override). **In Progress** — DOTS highlight component now mirrors router context; presentation/VFX hookup still pending.
- [ ] Provide distinct feedback for invalid actions *(deferred: presentation-specific)* (cursor colour change, deny SFX).
- [ ] Expose highlight info to presentation/companion systems *(deferred: presentation bridge)* (DOTS component describing highlight state). **In Progress** — `DivineHandHighlight` component populated each frame; consumers still to wire up.

### 4. Interaction Flow & Commands
- [ ] Make all interactions (pickup, hold, throw, siphon, dump) issue deterministic command buffers that integrate with registries (resource/storehouse) and `RainMiracleCommand`. **In Progress** — storehouse dump now deposits into inventory via `DivineHandCommand`; siphon/pile extraction still TODO.
- [ ] Record history entries for hand actions so rewind can replay events (similar to job history).
- [ ] Update rain cloud behaviour when held (velocity zero, moisture unaffected until release) and apply throw impulses using deterministic weighting.
- [ ] Ensure `DivineHandSystem` and related systems early-out correctly during playback/catch-up (`RewindState.Mode` check).
- [ ] Adopt central `HandInputRouterSystem` once integration TODO lands; ensure state transitions respect shared `HandInteractionState` and fixed-step timing.

### 5. BW2 Camera Parity *(deferred to game builds)*
> Baseline PureDOTS keeps deterministic camera transforms/logical routing; feel-matching remains with downstream titles.

- [x] **Pan (LMB - "Grab Land")**:
  - On LMB down over terrain: establish grab plane at raycast hit point
  - While dragging: camera follows inverse of mouse movement (land stays under cursor)
  - Behavior: smooth, responsive, no momentum/inertia
  - Height: maintains altitude offset from terrain during entire drag
  - UI blocking: pan disabled when pointer over UI elements
  - Release: immediate stop, no coasting or easing
- [x] **Orbit (MMB - "Spin Around Point")**:
  - On MMB down: raycast to terrain establishes locked pivot point (world space)
  - While dragging: yaw (horizontal) and pitch (vertical) rotation around locked pivot
  - Sensitivity: distance-scaled (closer = 1.5x faster, mid = 1.0x, far = 0.6x slower)
  - Pivot lock: pivot stays absolutely fixed in world space while MMB held
  - Pitch limits: -30° to +85° (prevents camera flipping upside down)
  - On release: unlocks pivot, current camera view becomes new base position
- [x] **Zoom (Scroll Wheel)**:
  - Direction: scroll up = closer, scroll down = farther (invertible in settings)
  - Target: zooms toward point under cursor (not pivot center or screen center)
  - During orbit: adjusts orbit radius while keeping pivot locked (no pivot movement)
  - Hand synergy: if divine hand visible, pivots XZ to hand cursor position during zoom
  - Range: 6m minimum to 220m maximum (enforce clamps)
  - Speed: exponential feel (6 units per tick, feels faster when farther)
- [x] **Terrain Collision**:
  - Camera clearance: minimum 2m above terrain at all times
  - Sphere cast: detect obstacles between pivot and desired camera position
  - Auto-adjust: pull camera closer if blocked, return when path clears
  - Smooth: gentle easing, no sudden pops or jarring movements
- [ ] **Anchor Snaps** *(deferred to game builds)*:
  - Space key → snap to temple with smooth lerp
  - C key → snap to creature with smooth lerp
  - Support designer-defined custom anchors (buildings, events)
  - Preserve pitch angle during snap (maintain player's chosen tilt)
  - Zoom inheritance: new pivot adopts zoom distance from previous view
- [ ] **Configuration Assets** *(deferred to game builds)*:
  - Build `CameraProfile` ScriptableObject with all sensitivity/speed/clamp parameters
  - Support multiple profiles per scene (different biomes, areas, story moments)
  - Expose invert toggles for all axes (pan X/Y, orbit X/Y, zoom)
  - Include BW2 reference profile as default template

### 6. Launch Modes & Future Hooks
- [ ] Implement three launch modes from truth source: simple toss, slingshot (charged arc), radial scatter (stub). Use config curves for impulse vs. charge.
- [ ] Add HUD indicators for charge/launch readiness; integrate with hand state events.
- [ ] Reserve extension points for future miracles/creature interactions (e.g., throw villager, drop miracle payloads).

### 7. Testing & Benchmarks
> Baseline tests rely on console/log assertions because we lack shared visual representations for rewind validation.

- [ ] Unit tests: state transitions, router priority resolution, highlight toggling, launch impulse calculations.
- [ ] Playmode tests: pickup capacity enforcement, storehouse dump flow, cross-type block, throw trajectories, camera orbit/pan/zoom invariants (positions/angles).
- [ ] Rewind tests: record → rewind → resume verifying hand state, camera pivot, highlight entity, and launch mode state all match original via console/log instrumentation.
- [ ] Performance tests: run at 120 FPS and 240 FPS verifying zero GC allocations, Burst compliance, and stable timing.
- [ ] Integration tests: once spatial grid/registries land, confirm hand queries align with grid results (hover detection).
- [ ] **BW2 Parity Validation** *(deferred to game builds)*:
  - Record side-by-side video (BW2 vs. our implementation) of same camera movements
  - Measure orbit sensitivity (degrees of rotation per pixel of mouse movement) at close/mid/far distances
  - Measure zoom speed (distance change per scroll tick)
  - Compare throw distances and arc trajectories for same charge times
  - Verify pan responsiveness (land stays under cursor during drag)
  - Validate hand state transitions match BW2 timing (grab, hold, release, throw)
- [ ] **Manual QA Checklist** *(deferred to game builds)*:
  - LMB pan feels smooth and responsive (land stays under cursor)
  - MMB orbit locks pivot correctly and releases on button up
  - Scroll zoom targets cursor position (not center of screen)
  - Hand cursor changes state appropriately (open/closed/pointing/cupped/charging)
  - Grabbing objects works on first try (95%+ success rate)
  - Throwing has satisfying arc and impact feel
  - Resource siphoning shows progress clearly with visual feedback
  - Storehouse dump triggers at correct distance (hover detection)
  - RMB priority router resolves conflicts correctly (no ambiguous actions)
  - Camera never clips through terrain or objects
  - Anchor snaps (Space/C keys) frame target smoothly with lerp

### 8. Tooling & Observability
- [ ] Debug overlay showing router winner, hand state, held entity, highlight target, camera pivot/radius; toggle via debug menu.
- [ ] Gizmos for hand raycast, camera pivot, zoom radius, highlight box.
- [ ] Extend HUD (DOTS data + hybrid UI) to display cargo amount, launch charge, current mode.
- [ ] Logging hooks for analytics (hand actions, camera mode toggles, throw stats).

### 9. Documentation & Designer Workflow
- [ ] Update `Docs/Guides/SceneSetup.md` with step-by-step hand/camera setup, required components, layer assignments.
- [ ] Expand `Docs/DesignNotes/RainMiraclesAndHand.md` to describe DOTS implementation, highlight/launch configuration, tuning advice.
- [ ] Document camera/hand tuning tips (sensitivity, zoom rates) for designers in plain language.
- [ ] Record progress milestones in `Docs/Progress.md`; link to truth sources and code changes.

## Future Extension Points
### Planned for Later
- **Creature interaction**: Train, praise, scold gestures with hand
- **Miracle casting**: Draw symbols with hand cursor to activate miracles
- **Villager pickup**: Special handling (can't throw hard, gentle drop only)
- **Building placement**: Drag-to-place mechanic with ghost preview
- **Camera paths**: Scripted camera movement for cutscenes and story moments
- **Photo mode**: Free camera with pause, filters, and screenshot tools
- **Multi-object selection**: Hold Ctrl + grab multiple entities at once
- **Smart camera framing**: Auto-focus on important events (battles, disasters)
- **Camera shake**: Impact feedback for explosions, creature stomps, etc.
- **Camera zones**: Restricted areas or forced viewpoints for story beats
- **Gesture recognition**: Advanced input patterns for complex interactions

### Architecture Hooks (Stub Now, Implement Later)
- Gesture recognition system interface (reserved state machine states)
- Extended hand state enum values (8-15 reserved for future modes)
- Custom interaction callback system via event buffers
- Presentation layer bridge hooks (highlight VFX, particle effects, HUD)
- Camera transition/lerp system for smooth anchor switching
- Input profile switching (mouse/gamepad/touch configurations)

## Success Criteria
### Core Parity Achieved When:
- Designer confirms "this feels exactly like BW2" after blind feel test
- All camera controls match BW2 reference video timing (±5% tolerance)
- Hand interactions work on first attempt 95%+ of time
- No user confusion about what hand can/can't pick up (clear visual feedback)
- Zero crashes or state machine deadlocks in 1-hour continuous play session
- RMB priority router resolves all conflicts deterministically with no ambiguity

### Ready for Designer Handoff When:
- All Workstreams 0-6 tasks complete
- BW2 parity validation passes (video comparison, sensitivity measurements)
- Rewind works flawlessly (record → rewind → catch-up → record cycle)
- Performance targets met (120 FPS minimum, zero GC allocations per frame)
- Configuration assets created with BW2 reference values
- Designer tuning tools functional (in-editor preview, real-time parameter adjustment)
- Documentation complete (setup guides, tuning tips, integration notes)

## Deliverables
- DOTS-compliant hand/camera systems delivering BW2 feel with deterministic behaviour.
- Centralized RMB router and input bridge utilities reusable by other systems.
- Highlight/feedback pipeline ready for presentation bridges.
- Comprehensive unit/playmode/performance/rewind test coverage.
- Updated documentation guiding designers on configuration and tuning.
- BW2 parity validation report with measurements and video evidence.
- Configuration asset templates with reference values.

## Dependencies & Links
- Registry rewrite for resource/storehouse interactions without allocations.
- Spatial services for efficient hover/target queries (future integration).
- Presentation bridges for HUD and VFX (highlight, charge meter).
- Pooled memory utilities (`NativeList`/ECB pools) to eliminate GC churn.
- Time engine & rewind for consistent behaviour across control schemes.

## Implementation Phases & Time Estimates

### Phase 1: Camera Parity Refinement (2 weeks)
- Fine-tune orbit sensitivity curves (distance-scaled feel)
- Perfect pan "grab land" plane intersection math
- Implement zoom-to-cursor with hand cursor synergy
- Add terrain collision with smooth pull-in/out
- Implement anchor snap system (temple/creature hotkeys)
- Create camera profile asset with BW2 reference values
- **Validation**: Side-by-side video comparison with BW2

### Phase 2: Hand State Machine & Interaction (2 weeks)
- Implement full state machine in DOTS (Empty/Holding/Aiming/Dumping)
- Add hover detection with highlight system
- Implement grab/hold/drop with smooth lerp follow
- Add slingshot charge mechanic with visual feedback
- Implement resource siphoning from piles
- Create storehouse dump interaction
- Add RMB priority router
- **Validation**: All interaction flows working, no state bugs

### Phase 3: Throw Physics & Feedback (1 week)
- Implement three throw modes (simple/slingshot/scatter)
- Add throw impulse calculation (charge * direction * mass)
- Create throw cooldown system
- Integrate with physics entities
- **Validation**: Throwing feels powerful and accurate

### Phase 4: Visual Feedback & Polish (1 week)
- Hand cursor state visuals (open/closed/pointing/cupped/charging)
- Highlight system for hoverable objects
- Invalid action feedback (cursor color, deny effect)
- Resource type indicators and capacity meter
- **Validation**: Clear visual communication of hand state

### Phase 5: Configuration & Tuning (1 week)
- Create HandCameraProfile ScriptableObject
- Add sensitivity curves for orbit/pan/zoom
- Expose throw impulse curves
- Build in-editor preview/test tools
- **Validation**: Designers can tune without code changes

### Phase 6: Rewind Integration & Testing (1 week)
- Implement hand state snapshotting
- Add camera pivot/distance/rotation history
- Test record → rewind → catch-up cycles
- Fix any non-determinism
- **Validation**: Rewind works flawlessly

### Phase 7: Advanced Features (2 weeks) - OPTIONAL/FUTURE
- Gesture recognition system
- Multi-object selection
- Smart camera framing
- Camera shake system
- **Validation**: Enhancements feel natural

## Next Steps & Implementation Order
1. **Recon & gap analysis** (Workstream 0) - 2-3 days
2. **Input routing + configuration overhaul** (Workstream 1) - 1 week
3. **Hand state machine + highlight** (Workstreams 2 & 3) - 2 weeks
4. **Interaction flow updates** (Workstream 4) - 1 week
5. **Camera parity implementation** (Workstream 5) - 2 weeks
6. **Launch modes & HUD** (Workstreams 6 & 8) - 1 week
7. **Testing, profiling, tooling** (Workstream 7) - 1 week
8. **Documentation and designer rollout** (Workstream 9) - 3-4 days

**Total Estimated Time**: 8-10 weeks for core BW2 parity (Phases 1-6)

Update this document as milestones complete; capture tuning learnings, BW2 comparison notes, and blockers so the team stays aligned.
