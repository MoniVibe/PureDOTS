# AI Implementation Backlog

**Last Updated**: 2025-12-21 (Refreshed: medium-first sensing, group-cache comms MVP, governance baseline)  
**Status**: Active  
**Source**: `Docs/AI_Gap_Audit.md`

This backlog prioritizes AI gap closure work for PureDOTS, Godgame, and Space4X projects.

Also see:
- `Docs/Architecture/Scalability_Contract.md` (million+ guardrails; hot/cold; anti-pattern bans)
- `Docs/Architecture/Senses_And_Comms_Medium_First.md` (signals-in-medium; comms ride channels; interrupt-driven minds)
- `Docs/Concepts/Core/Information_Propagation.md` (claims/evidence/beliefs + storage topology)
- `Docs/Concepts/Core/Authority_And_Command_Hierarchies.md` (mayor/captain + delegates; refusal ladders)

## Backlog Items

### P0: Critical Foundation (Next 2-3 Iterations)

#### AI-001: Virtual Sensor Readings for Internal Needs
**Priority**: P0  
**Owner**: PureDOTS Core  
**Estimate**: 1-2 weeks  
**Status**: Implemented in PureDOTS (verify ordering + downstream adoption)

**Scope**:
- Ensure `AIVirtualSensorSystem` runs before `AIUtilityScoringSystem` (explicit ordering)
- Populate `AISensorReading` buffers with internal state (hunger, energy, morale)
- Map needs to virtual sensor indices (e.g., sensor 0 = hunger, sensor 1 = energy)
- Update `GodgameAIAssetHelpers` to reference virtual sensors in utility curves
- Deprecate `VillagerUtilityScheduler` usage in `VillagerAISystem`
- Add tests validating virtual sensor population

**Acceptance Criteria**:
- [x] `AIVirtualSensorSystem` exists and populates need readings (Hunger/Energy/Morale)
- [ ] Utility scoring deterministically sees virtual sensors in the same tick (ordering proof)
- [ ] Tests validate virtual sensor values match need component values
- [ ] Godgame villager AI no longer depends on legacy scheduler dual-path

**Dependencies**: None  
**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AIVirtualSensorSystem.cs`
- `Packages/com.moni.puredots/Runtime/Runtime/AI/AIComponents.cs` (add virtual sensor config)
- `Assets/Projects/Godgame/Scripts/Godgame/Systems/GodgameAIAssetHelpers.cs` (update utility blob)
- `Packages/com.moni.puredots/Runtime/Systems/VillagerAISystem.cs` (remove dual path)

---

#### AI-002: Miracle Detection
**Priority**: P0  
**Owner**: PureDOTS Core  
**Estimate**: 1 week  
**Status**: Planned

**Scope**:
- Add `ComponentLookup<MiracleDefinition>` and `ComponentLookup<MiracleRuntimeState>` to `AISensorCategoryFilter`
- Implement `MatchesCategory` logic for `AISensorCategory.Miracle`
- Update `ResolveCategory` to return `Miracle` when entity has miracle components
- Remove conditional compilation (`#if SPACE4X_TRANSPORT`) from `AISystems.cs`
- Add tests for miracle detection

**Acceptance Criteria**:
- [ ] `AISensorCategory.Miracle` correctly identifies miracle entities
- [ ] Sensor readings include miracle entities when category is set
- [ ] All conditional compilation removed from AI systems
- [ ] Tests validate miracle detection accuracy

**Dependencies**: Miracle components exist  
**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (add miracle lookups, remove conditionals)
- `Assets/Tests/Playmode/AIIntegrationTests.cs` (add miracle tests)

---

### P1: Performance & Scalability (Next Quarter)

#### AI-009: Profile → Policy Unification (Authority + Compliance)
**Priority**: P1  
**Owner**: PureDOTS Core + game integration  
**Estimate**: 1-2 weeks  
**Status**: Planned

**Scope**:
- Define a minimal shared “policy” facet derived from profile + dynamic state:
  - obedience, risk tolerance, consensus appetite
  - mutiny pressure threshold, order refusal bias, ROE strictness, friendly-fire inhibition (governance/combat safety)
- Ensure it works for **individuals and aggregates** (crew mass, village population).
- Add telemetry metrics/events for the derived policy values (proof-oriented, small payloads).
- Document the canonical schema and Space4X↔PureDOTS mapping rules.

**Acceptance Criteria**:
- [ ] Policy derivation is deterministic and produces stable metrics in headless runs.
- [ ] Both games can consume the policy fields without introducing new type/namespace collisions.
- [ ] At least one headless proof line exists for a policy-driven behavior (e.g., council vs executive approval, order refused due to ROE, mutiny pressure threshold crossed).

**References**:
- `Docs/Concepts/Core/Entity_Profile_Schema.md`

---

#### AI-010: Authority Seats & Delegation (Villages + Ships)
**Priority**: P1  
**Owner**: PureDOTS Core + game integration  
**Estimate**: 1-2 weeks  
**Status**: Planned

**Scope**:
- Model authority seats (mayor/captain + delegates/officers) with deterministic succession.
- MVP governance baseline:
  - **Village**: single executive **Mayor** + delegates (steward/marshal/quartermaster).
  - **Council/quorum**: later extension driven by Authority axis; do not block MVP.
- Ensure every macro-issued order is attributable (issuer seat + occupant entity).
- Add an order validation outcome path for domain officers (refuse / delay / escalate) driven by morale/cohesion and ROE/friendly-fire constraints.
- Add a bounded authority response ladder to refusal/dissent (clarify/negotiate → coerce/punish → extreme purge/decimation as rare, scenario-scaled options).
- Gate extreme enforcement behind **discretion flags on named leaders** (captains/officers/overseers), and add internal “radical scrutiny” signals for aggregates (so enforcement can target catalysts instead of the whole group).
- Drive at least one macro→micro handoff per game:
  - Godgame: village construction priority + patrol assignment
  - Space4X: captain intent → sortie assignment (even if simplified)

**Acceptance Criteria**:
- [ ] Seat occupancy is data-driven and supports missing seats.
- [ ] Orders always carry issuer attribution.
- [ ] Headless proof lines confirm both handoffs happened end-to-end.
- [ ] Headless proof line confirms at least one deterministic order refusal/delay/escalation (with telemetry reasons).

**References**:
- `Docs/Concepts/Core/Authority_And_Command_Hierarchies.md`

---

#### AI-011: Mutiny/Coup/Defection Outcome Resolver
**Priority**: P1  
**Owner**: Space4X + PureDOTS social/politics integration  
**Estimate**: 1 week  
**Status**: Planned

**Scope**:
- Implement a compact 3-way split model (loyalists / rebels / neutrals) for mutiny/coup events.
- Resolve at least one flexible outcome path:
  - loyalist mutiny (replace captain, stay in faction)
  - renegade captain (defect)
- Support scenario constraints/overrides (bias or force certain outcomes).
- Keep “attack friendlies” as a high-friction path unless scenarios explicitly enable betrayal.

**Acceptance Criteria**:
- [ ] Mutiny resolves deterministically given the same inputs.
- [ ] Telemetry captures the chosen outcome and the key reason tokens/weights (small, structured).
- [ ] Headless proof line confirms an outcome was resolved.

**References**:
- `space4x/Docs/Concepts/Politics/Rebellion_Mechanics_System.md`

---

#### AI-012: Group Cache Communications MVP (Village-First)
**Priority**: P1  
**Owner**: PureDOTS Core + Godgame integration  
**Estimate**: 1 week  
**Status**: Planned

**Scope**:
- Implement the MVP “group knowledge cache” storage layer (village/ship/squad blackboard).
- Start with villages:
  - micro actors post observations/reports into the group cache (bounded, tick-stamped).
  - mayor + delegates read the cache to drive priorities (avoid per-individual memory for MVP).
- Support a tiny predicate set (2–4 claim types) and a deterministic staleness/eviction policy.
- Make miscommunication first-class even in MVP: low integrity can still produce wrong entries.

**Acceptance Criteria**:
- [ ] Village group cache receives at least: `ThreatSeen`, `ResourceSeen`, `StockpileLow` (or equivalent), with confidence/staleness.
- [ ] Mayor uses group cache entries to choose one macro action (e.g., patrol priority increases on threats).
- [ ] Bounded memory: cache has caps + deterministic eviction (no unbounded growth).
- [ ] Headless proof line + telemetry confirms cache activity and a macro decision driven by it.

**References**:
- `Docs/Concepts/Core/Information_Propagation.md`
- `Docs/Architecture/Senses_And_Comms_Medium_First.md`

#### AI-003: Flow Field Integration with AI Steering
**Priority**: P1  
**Owner**: PureDOTS Core  
**Estimate**: 1-2 weeks  
**Status**: Planned

**Scope**:
- Integrate `FlowFieldState` with `AISteeringState`
- Update `AISteeringSystem` to sample flow field direction when `FlowFieldAgentTag` present
- Blend flow field direction with local avoidance from `LocalSteeringSystem`
- Add flow field layer selection based on AI goals (e.g., resource layer for gathering)
- Ensure compatibility with existing steering logic

**Acceptance Criteria**:
- [ ] Agents with flow fields use flow direction in steering calculations
- [ ] Flow field direction blends correctly with local avoidance
- [ ] Layer selection works based on AI goals
- [ ] Performance remains acceptable with flow fields enabled

**Dependencies**: Flow field systems exist  
**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (integrate flow fields)
- `Packages/com.moni.puredots/Runtime/Systems/Navigation/FlowFieldFollowSystem.cs` (ensure compatibility)

---

#### AI-004: Performance Metrics & Validation Framework
**Priority**: P1  
**Owner**: PureDOTS Core  
**Estimate**: 1 week  
**Status**: Planned

**Scope**:
- Create `AIPerformanceMetrics` singleton component
- Instrument `AISensorUpdateSystem`, `AIUtilityScoringSystem`, `AISteeringSystem`, `AITaskResolutionSystem` with timing
- Track memory allocations per system
- Create performance test harness (10k, 50k, 100k agents)
- Add telemetry hooks for HUD/debugging

**Acceptance Criteria**:
- [ ] Performance metrics singleton tracks timing/allocations for all AI systems
- [ ] Test harness validates performance targets (10k agents < 1ms per system)
- [ ] Telemetry exposes metrics to debug HUD
- [ ] Documentation includes performance tuning guidelines

**Dependencies**: Telemetry system exists  
**Files**:
- `Packages/com.moni.puredots/Runtime/Runtime/AI/AIPerformanceMetrics.cs` (new)
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (add metrics)
- `Assets/Tests/Playmode/AIPerformanceTests.cs` (new)

---

### P2: Flexibility & Tooling (Future)

#### AI-005: Multi-Goal Utility Bindings
**Priority**: P2  
**Owner**: Game Projects  
**Estimate**: 1 week  
**Status**: Planned

**Scope**:
- Create `AIUtilityBindingBlob` with richer mapping data
- Support conditional mappings (e.g., "if load > 0.9, action 0 → Returning")
- Add priority/weight fields to bindings
- Update bridge systems to use blob-based bindings
- Document advanced binding patterns

**Acceptance Criteria**:
- [ ] `AIUtilityBindingBlob` supports conditional mappings
- [ ] Bridge systems use blob-based bindings
- [ ] Documentation includes advanced binding examples
- [ ] Tests validate conditional mapping logic

**Dependencies**: None  
**Files**:
- `Packages/com.moni.puredots/Runtime/Runtime/AI/AIComponents.cs` (add binding blob)
- `Assets/Projects/Godgame/Scripts/Godgame/Systems/GodgameVillagerAICommandBridgeSystem.cs` (use blob)
- `Assets/Scripts/Space4x/Systems/Space4XVesselAICommandBridgeSystem.cs` (use blob)

---

#### AI-006: Sensor Visualization & Debugging Tools
**Priority**: P2  
**Owner**: PureDOTS Core  
**Estimate**: 1 week  
**Status**: Planned

**Scope**:
- Add gizmo drawer for sensor ranges (editor/runtime)
- Visualize detected entities with lines/spheres
- Debug overlay for utility scores and commands
- Scene view gizmos for AI state

**Acceptance Criteria**:
- [ ] Sensor ranges visible in scene view
- [ ] Detected entities visualized with connections
- [ ] Debug overlay shows utility scores and commands
- [ ] Gizmos toggleable via debug menu

**Dependencies**: Debug HUD exists  
**Files**:
- `Packages/com.moni.puredots/Editor/AISensorGizmos.cs` (new)
- `Packages/com.moni.puredots/Runtime/Systems/Debug/AIDebugOverlaySystem.cs` (new)

---

### P3: Game-Specific Features (Future)

#### AI-007: Godgame Morale-Based Behaviors
**Priority**: P3  
**Owner**: Godgame  
**Estimate**: 3 days  
**Status**: Planned

**Scope**:
- Add morale virtual sensor (via AI-001)
- Create utility curves for morale-based actions (seek entertainment, work harder)
- Update bridge system to handle morale goals

**Acceptance Criteria**:
- [ ] Villagers react to low morale (seek entertainment)
- [ ] High morale increases work priority
- [ ] Utility curves tuned for morale behaviors

**Dependencies**: AI-001 (Virtual Sensors)  
**Files**:
- `Assets/Projects/Godgame/Scripts/Godgame/Systems/GodgameAIAssetHelpers.cs` (add morale curves)
- `Assets/Projects/Godgame/Scripts/Godgame/Systems/GodgameVillagerAICommandBridgeSystem.cs` (handle morale goals)

---

#### AI-008: Space4X Escort & Formation Behaviors
**Priority**: P3  
**Owner**: Space4X  
**Estimate**: 1 week  
**Status**: Planned

**Scope**:
- Add formation/escort sensor categories
- Create utility curves for formation actions
- Implement formation steering in bridge system

**Acceptance Criteria**:
- [ ] Vessels can detect escort targets
- [ ] Formation utility curves prioritize escort actions
- [ ] Bridge system handles formation steering

**Dependencies**: None  
**Files**:
- `Packages/com.moni.puredots/Runtime/Runtime/AI/AIComponents.cs` (add formation categories)
- `Assets/Scripts/Space4x/Systems/Space4XVesselAICommandBridgeSystem.cs` (add formation logic)

---

## Implementation Order

### Sprint 1 (Weeks 1-2)
1. AI-001: Virtual Sensor Readings (foundation for needs-based AI)
2. AI-002: Miracle Detection (unblocks miracle reactions)

### Sprint 2 (Weeks 3-4)
3. AI-003: Flow Field Integration (performance/scalability)
4. AI-004: Performance Metrics (validation framework)

### Sprint 3 (Weeks 5-6)
5. AI-009: Profile → Policy Unification (authority + compliance)
6. AI-010: Authority Seats & Delegation (villages + ships)

### Sprint 4 (Weeks 7-8)
7. AI-011: Mutiny/Coup/Defection Outcome Resolver

### Sprint 5+ (Future)
8. AI-005: Multi-Goal Bindings (flexibility)
9. AI-006: Sensor Visualization (tooling)
10. AI-007: Morale Behaviors (game-specific)
11. AI-008: Formation Behaviors (game-specific)

## Success Metrics

- **Virtual Sensors**: 100% of villager needs handled by AI pipeline
- **Miracle Detection**: All miracle types detectable via sensors
- **Performance**: 10k agents < 1ms per AI system
- **Governance**: authority seats issue attributable orders; council vs executive mode is profile-driven
- **Mutiny outcomes**: at least one mutiny event resolves with deterministic outcome + telemetry evidence
- **Coverage**: All sensor categories have detection logic
- **Documentation**: Designer guide with advanced behavior recipes

## Notes

- Items are sized for 1-2 week sprints
- Dependencies are clearly marked
- Acceptance criteria ensure testable deliverables
- Game-specific items (AI-007, AI-008) can be done in parallel with core work
