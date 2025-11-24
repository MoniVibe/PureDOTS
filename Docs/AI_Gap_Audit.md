# AI Gap Audit & Backlog

**Date**: 2025-01-XX (Created during AI gap closure planning)  
**Status**: Active  
**Purpose**: Catalog remaining gaps in PureDOTS AI pipeline and prioritize implementation work

## Summary

The PureDOTS AI pipeline (`AISystemGroup`) provides a modular framework for sensing, utility scoring, steering, and task resolution. Core integration is complete for Godgame villagers and Space4X vessels, but several gaps remain for advanced behaviors and production readiness.

## Critical Gaps

### 1. Virtual Sensor Readings for Internal Needs
**Severity**: High  
**Owner**: PureDOTS Core  
**Status**: Planned

**Problem**: Villagers currently use `VillagerUtilityScheduler` separately from the spatial AI pipeline. Internal needs (hunger, energy, morale) aren't represented as sensor readings, preventing unified utility scoring.

**Impact**: 
- Dual decision-making paths (spatial AI + needs scheduler) create inconsistency
- Can't use full AI pipeline for needs-based behaviors
- Harder to tune and debug

**Solution**:
- Create `AIVirtualSensorSystem` that populates `AISensorReading` buffers with internal state
- Map needs (hunger, energy, morale) to virtual sensor indices
- Update utility blobs to reference virtual sensor indices
- Deprecate `VillagerUtilityScheduler` in favor of unified pipeline

**Dependencies**: None  
**Estimated Effort**: 1-2 weeks

**Files Affected**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (add virtual sensor system)
- `Packages/com.moni.puredots/Runtime/Runtime/AI/AIComponents.cs` (add virtual sensor config)
- `Assets/Projects/Godgame/Scripts/Godgame/Authoring/VillagerAuthoring.cs` (update utility blob)
- `Packages/com.moni.puredots/Runtime/Systems/VillagerAISystem.cs` (remove dual path)

---

### 2. Miracle Detection
**Severity**: Medium  
**Owner**: PureDOTS Core  
**Status**: Planned

**Problem**: `AISensorCategory.Miracle` exists but detection logic is missing. Entities can't detect miracle effects (rain clouds, healing zones, etc.) via sensors.

**Impact**:
- Villagers can't react to miracles (flee fire, seek healing)
- Can't create utility curves for miracle-based behaviors
- Missing integration point for miracle framework

**Solution**:
- Add `ComponentLookup<MiracleDefinition>` and `ComponentLookup<MiracleRuntimeState>` to `AISensorCategoryFilter`
- Implement `MatchesCategory` logic for `AISensorCategory.Miracle`
- Update `ResolveCategory` to return `Miracle` when appropriate
- Add tests for miracle detection

**Dependencies**: Miracle components exist (`MiracleDefinition`, `MiracleRuntimeState`)  
**Estimated Effort**: 1 week

**Files Affected**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (add miracle lookups and matching)
- `Assets/Tests/Playmode/AIIntegrationTests.cs` (add miracle detection tests)

---

### 3. Conditional Compilation for Space4X Transport
**Severity**: Low  
**Owner**: PureDOTS Core  
**Status**: Inconsistent

**Problem**: `#if SPACE4X_TRANSPORT` directives still exist in `AISystems.cs`, contradicting earlier work that made Space4X always available. This creates compilation complexity and potential runtime issues.

**Impact**:
- Inconsistent codebase (some parts always include Space4X, others conditional)
- Risk of missing transport detection if define isn't set
- Harder to maintain

**Solution**:
- Remove all `#if SPACE4X_TRANSPORT` directives from `AISystems.cs`
- Ensure Space4X transport lookups are always compiled
- Verify no compilation errors without the define

**Dependencies**: None  
**Estimated Effort**: 1 day

**Files Affected**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (remove conditionals)

---

## Enhancement Gaps

### 4. Flow Field Integration with AI Steering
**Severity**: Medium  
**Owner**: PureDOTS Core  
**Status**: Partial

**Problem**: Flow field pathfinding exists (`FlowFieldBuildSystem`, `FlowFieldFollowSystem`) but isn't integrated with `AISteeringSystem`. Agents use basic steering without leveraging flow fields for crowd navigation.

**Impact**:
- Suboptimal pathfinding for large crowds
- Missing scalability benefits of flow fields
- Can't use flow fields for AI-driven movement

**Solution**:
- Integrate `FlowFieldState` with `AISteeringState`
- Update `AISteeringSystem` to sample flow field direction when available
- Blend flow field direction with local avoidance
- Add flow field layer selection based on AI goals

**Dependencies**: Flow field systems exist  
**Estimated Effort**: 1-2 weeks

**Files Affected**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (integrate flow fields)
- `Packages/com.moni.puredots/Runtime/Systems/Navigation/FlowFieldFollowSystem.cs` (ensure compatibility)

---

### 5. Multi-Goal Utility Bindings
**Severity**: Low  
**Owner**: Game Projects  
**Status**: Functional but Simplified

**Problem**: Current utility bindings (`VillagerAIUtilityBinding`, `VesselAIUtilityBinding`) use simple `FixedList32Bytes<Goal>` mappings. No support for:
- Conditional mappings (action → goal based on context)
- Priority overrides
- Goal state machines

**Impact**:
- Limited flexibility for complex behaviors
- Harder to implement multi-stage goals
- Bridge systems must handle all complexity

**Solution**:
- Create `AIUtilityBindingBlob` with richer mapping data
- Support conditional mappings (e.g., "if load > 0.9, action 0 → Returning")
- Add priority/weight fields to bindings
- Document advanced binding patterns

**Dependencies**: None  
**Estimated Effort**: 1 week

**Files Affected**:
- `Packages/com.moni.puredots/Runtime/Runtime/AI/AIComponents.cs` (add binding blob)
- `Assets/Projects/Godgame/Scripts/Godgame/Systems/GodgameVillagerAICommandBridgeSystem.cs` (use blob)
- `Assets/Scripts/Space4x/Systems/Space4XVesselAICommandBridgeSystem.cs` (use blob)

---

### 6. Performance Metrics & Validation
**Severity**: Medium  
**Owner**: PureDOTS Core  
**Status**: Missing

**Problem**: No performance metrics or validation framework for AI systems. Can't measure:
- Sensor update costs
- Utility scoring performance
- Command queue throughput
- Memory allocations

**Impact**:
- Can't validate performance targets (e.g., 10k agents < 1ms)
- Hard to optimize without data
- No regression detection

**Solution**:
- Add `AIPerformanceMetrics` singleton with counters
- Instrument each AI system with timing/allocations
- Create performance test harness (10k, 50k, 100k agents)
- Add telemetry hooks for HUD/debugging

**Dependencies**: Telemetry system exists  
**Estimated Effort**: 1 week

**Files Affected**:
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (add metrics)
- `Assets/Tests/Playmode/AIPerformanceTests.cs` (new file)

---

### 7. Sensor Visualization & Debugging
**Severity**: Low  
**Owner**: PureDOTS Core  
**Status**: Missing

**Problem**: No visual debugging tools for AI sensors. Designers can't see:
- Sensor ranges
- Detected entities
- Utility scores
- Command queue contents

**Impact**:
- Hard to tune sensor ranges
- Difficult to debug why agents don't detect targets
- No visual feedback for designers

**Solution**:
- Add gizmo drawer for sensor ranges (editor/runtime)
- Visualize detected entities with lines/spheres
- Debug overlay for utility scores and commands
- Scene view gizmos for AI state

**Dependencies**: Debug HUD exists  
**Estimated Effort**: 1 week

**Files Affected**:
- `Packages/com.moni.puredots/Editor/AISensorGizmos.cs` (new file)
- `Packages/com.moni.puredots/Runtime/Systems/Debug/AIDebugOverlaySystem.cs` (new file)

---

## Game-Specific Gaps

### 8. Godgame: Morale-Based Behaviors
**Severity**: Low  
**Owner**: Godgame  
**Status**: Planned

**Problem**: Villagers have morale but no AI behaviors that react to morale changes (e.g., low morale → seek entertainment, high morale → work harder).

**Solution**:
- Add morale virtual sensor
- Create utility curves for morale-based actions
- Update bridge system to handle morale goals

**Estimated Effort**: 3 days

---

### 9. Space4X: Escort & Formation Behaviors
**Severity**: Low  
**Owner**: Space4X  
**Status**: Planned

**Problem**: No support for escort drones or formation flying. Vessels can't coordinate movement.

**Solution**:
- Add formation/escort sensor categories
- Create utility curves for formation actions
- Implement formation steering in bridge system

**Estimated Effort**: 1 week

---

## Prioritization

### Short-Term (Next 2-3 Iterations)
1. **Virtual Sensor Readings** (High impact, unblocks needs-based AI)
2. **Miracle Detection** (Medium impact, unblocks miracle reactions)
3. **Remove Conditional Compilation** (Low effort, consistency)

### Medium-Term (Next Quarter)
4. **Flow Field Integration** (Performance/scalability)
5. **Performance Metrics** (Validation/optimization)
6. **Multi-Goal Bindings** (Flexibility)

### Long-Term (Future)
7. **Sensor Visualization** (Designer tooling)
8. **Game-Specific Behaviors** (Feature expansion)

## Success Metrics

- **Virtual Sensors**: 100% of villager needs handled by AI pipeline
- **Miracle Detection**: All miracle types detectable via sensors
- **Performance**: 10k agents < 1ms per AI system
- **Coverage**: All sensor categories have detection logic
- **Documentation**: Designer guide with advanced behavior recipes

## Dependencies & Blockers

- None critical. All gaps can be addressed independently.
- Virtual sensors depend on understanding current needs system (already documented).
- Miracle detection depends on miracle component availability (already exists).

## Related Documentation

- `Docs/Guides/AI_Integration_Guide.md` - Current integration guide
- `Docs/AI_Readiness_Implementation_Summary.md` - Previous implementation summary
- `Docs/TODO/VillagerSystems_TODO.md` - Villager system TODOs
- `Docs/TODO/Space4X_Frameworks_TODO.md` - Space4X TODOs

