# AI Implementation Backlog

**Last Updated**: 2025-01-XX (Created during AI gap closure planning)  
**Status**: Active  
**Source**: `Docs/AI_Gap_Audit.md`

This backlog prioritizes AI gap closure work for PureDOTS, Godgame, and Space4X projects.

## Backlog Items

### P0: Critical Foundation (Next 2-3 Iterations)

#### AI-001: Virtual Sensor Readings for Internal Needs
**Priority**: P0  
**Owner**: PureDOTS Core  
**Estimate**: 1-2 weeks  
**Status**: Planned

**Scope**:
- Create `AIVirtualSensorSystem` that runs before `AIUtilityScoringSystem`
- Populate `AISensorReading` buffers with internal state (hunger, energy, morale)
- Map needs to virtual sensor indices (e.g., sensor 0 = hunger, sensor 1 = energy)
- Update `GodgameAIAssetHelpers` to reference virtual sensors in utility curves
- Deprecate `VillagerUtilityScheduler` usage in `VillagerAISystem`
- Add tests validating virtual sensor population

**Acceptance Criteria**:
- [ ] `AIVirtualSensorSystem` populates sensor readings for all villager needs
- [ ] Utility scoring uses virtual sensors instead of separate scheduler
- [ ] Tests validate virtual sensor values match needs component values
- [ ] Documentation updated with virtual sensor usage

**Dependencies**: None  
**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AIVirtualSensorSystem.cs` (new)
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

### Sprint 3+ (Future)
5. AI-005: Multi-Goal Bindings (flexibility)
6. AI-006: Sensor Visualization (tooling)
7. AI-007: Morale Behaviors (game-specific)
8. AI-008: Formation Behaviors (game-specific)

## Success Metrics

- **Virtual Sensors**: 100% of villager needs handled by AI pipeline
- **Miracle Detection**: All miracle types detectable via sensors
- **Performance**: 10k agents < 1ms per AI system
- **Coverage**: All sensor categories have detection logic
- **Documentation**: Designer guide with advanced behavior recipes

## Notes

- Items are sized for 1-2 week sprints
- Dependencies are clearly marked
- Acceptance criteria ensure testable deliverables
- Game-specific items (AI-007, AI-008) can be done in parallel with core work

