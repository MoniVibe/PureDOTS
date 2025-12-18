# Remaining Technical Debt

_Last updated: 2025-01-27_

This document catalogs remaining technical debt items across PureDOTS, organized by priority and category.

## High Priority (Blocking or Critical)

### 1. Presentation Bridge Implementation
**Status**: Core MVP implemented, testing pending  
**Location**: `Docs/TODO/PresentationBridge_TODO.md`

**Issues**:
- ~~Companion entity sync systems not implemented~~ ✅ `PresentationSpawnSystem` and `PresentationRecycleSystem` exist
- ~~Presentation bridges for render meshes incomplete~~ ✅ Basic spawn/recycle with Entities Graphics support
- ~~No testing for rewind tolerance~~ ✅ Added rewind guards to spawn/recycle systems
- ~~Minimal authoring tooling~~ ✅ `PresentationRegistryAsset` and `PresentationRegistryAuthoring` exist
- Validation tests missing (rewind-safe presentation tests)
- Sample authoring guide missing (`Docs/Guides/Authoring/`)

**Impact**: Core presentation bridge functional, ready for visual iteration

**Effort**: 2-3 weeks (core complete, testing/documentation pending)

### 2. Compilation Health Issues
**Status**: Known issues documented  
**Location**: `PureDOTS_TODO.md` section 10

**Issues**:
- `StreamingValidatorTests` references need restoration after asmdef updates
- NetCode 1.8 physics regression with `PhysicsWorldHistory.Clone` overloads (needs shim/patch)
- Duplicate `StreamingCoordinatorBootstrapSystem` definition causing namespace collisions
- Missing generic comparer in `StreamingLoaderSystem` for `IComparer<StreamingSectionCommand>`

**Impact**: Compilation errors, test failures

**Effort**: 1-2 days

### 3. Terraforming Hooks Integration
**Status**: Core hooks integrated  
**Location**: `Docs/TODO/SystemIntegration_TODO.md` section 6

**Issues**:
- ~~`TerrainVersion` increment triggers not confirmed across all consumers~~ ✅ Added TerrainVersion tracking to FlowFieldConfig, all environment grids already have LastTerrainVersion
- ~~Integration missing~~ ✅ Created TerrainChangeProcessorSystem, wired FlowFieldBuildSystem to check terrain version
- Existing TODOs don't reference terraforming contract (partial - TerraformingPrototype_TODO references hooks)
- Integration tests missing for terrain change propagation (future work)

**Impact**: Terraforming system can now integrate cleanly when implemented

**Effort**: 1 week (core hooks complete, testing pending)

## Medium Priority (Important but Not Blocking)

### 4. Reusable AI Behavior Modules
**Status**: Partially documented, not implemented  
**Location**: `Docs/TODO/SystemIntegration_TODO.md` section 10

**Issues**:
- Generic AI modules (sensors, scoring, steering, task selectors) not implemented
- Systems use custom AI logic instead of shared modules
- Documentation exists but no implementation pattern

**Impact**: Code duplication, harder to maintain consistent AI behavior

**Effort**: 2-3 weeks

### 5. Meta Registry Implementation
**Status**: Stubs exist, no implementation  
**Location**: `Runtime/Runtime/MetaRegistryStubs.cs`, `Docs/DesignNotes/MetaRegistryRoadmap.md`

**Issues**:
- `FactionRegistry`, `ClimateHazardRegistry`, `AreaEffectRegistry`, `CultureAlignmentRegistry` are empty stubs
- Systems disabled (no functionality)
- Roadmap exists but implementation not started

**Impact**: Missing high-level game systems (factions, hazards, area effects, culture)

**Effort**: 12 weeks (phased rollout)

### 6. Integration Test Coverage Gaps
**Status**: Some tests exist, gaps remain  
**Location**: `Docs/TODO/SystemIntegration_TODO.md` section 7

**Issues**:
- Hand siphon + miracle token deterministic resolver test missing
- Hand dump to storehouse after miracle charge test missing
- Environment debug overlay aggregator not implemented
- Nightly performance suite automation incomplete

**Impact**: Integration bugs may go undetected

**Effort**: 1-2 weeks

### 7. Content Neutrality Validation
**Status**: Manual review only  
**Location**: `Docs/TODO/SystemIntegration_TODO.md` section 11

**Issues**:
- No automated lint/tests ensuring shared layer naming stays neutral
- Risk of theme-specific terminology creeping into shared code
- Manual review process established but not automated

**Impact**: Future modules may violate neutrality, requiring refactors

**Effort**: 1 week (lint tooling)

### 8. Slices & Ownership Definition
**Status**: Not defined  
**Location**: `Docs/TODO/SystemIntegration_TODO.md` section 12

**Issues**:
- Meta-system slices not defined (`Runtime Core`, `Data Authoring`, `Tooling/Telemetry`, `QA/Validation`)
- No ownership or contact cadence documented
- TODO tasks not tagged with slice responsibility
- No governance process established

**Impact**: Unclear ownership, potential for conflicting changes

**Effort**: 1 week (definition + documentation)

## Lower Priority (Nice to Have)

### 9. Migration Plan & Asset Salvage
**Status**: Not started  
**Location**: `PureDOTS_TODO.md` section 7

**Issues**:
- No inventory of legacy assets worth porting
- Hard dependencies not identified
- Deprecation list not established

**Impact**: May duplicate work or miss useful legacy code

**Effort**: 1-2 weeks

### 10. Save/Load Determinism Tests
**Status**: Not implemented  
**Location**: `Docs/TODO/Utilities_TODO.md` section 4

**Issues**:
- Save/load system not implemented
- Determinism tests for serialization missing
- Snapshot comparison tools not built

**Impact**: Cannot validate persistence correctness

**Effort**: 2-3 weeks

### 11. Platform-Specific Quirks Documentation
**Status**: Not documented  
**Location**: `Docs/TODO/Utilities_TODO.md` section 4

**Issues**:
- Physics differences across platforms not documented
- Input device variations not captured
- DOTS runtime quirks per platform not recorded

**Impact**: Platform-specific bugs may surprise developers

**Effort**: Ongoing (as issues discovered)

### 12. Crash Reporting Integration
**Status**: Not prepared  
**Location**: `Docs/TODO/Utilities_TODO.md` section 4

**Issues**:
- Unity Cloud Diagnostics integration not prepared
- Sentry or equivalent not integrated
- Error reporting pipeline missing

**Impact**: Production bugs harder to diagnose

**Effort**: 1 week

### 13. Agent Workflow Protocol
**Status**: Not enforced  
**Location**: `PureDOTS_TODO.md` section 11

**Issues**:
- Three-agent cadence (Implementation, Error & Glue, Documentation) not enforced
- Hand-off process not standardized
- File citation requirements not tracked

**Impact**: Inefficient workflow, potential for missed follow-ups

**Effort**: Process definition (1 day) + enforcement (ongoing)

## Domain-Specific Debt

### 14. Villager Systems SoA Refactor
**Status**: Roadmap exists, not started  
**Location**: `Docs/TODO/VillagerSystems_TODO.md` section 2

**Issues**:
- Component layout optimization pending (reduce `VillagerNeeds` size, consolidate tags)
- Inventory/buffer refactoring pending (move to companion entities)
- System refactoring pending (Burst jobs, modular behaviors)

**Impact**: Performance not optimal, harder to scale

**Effort**: 4-6 weeks (phased)

### 15. Villager Local Steering System
**Status**: Not implemented  
**Location**: `Docs/TODO/VillagerSystems_TODO.md` section 2

**Issues**:
- Obstacle avoidance not implemented
- Separation forces (Reynolds steering) missing
- Static obstacle detection via grid not implemented

**Impact**: Villagers cluster, don't avoid obstacles

**Effort**: 1-2 weeks

### 16. Miracles Framework Completion
**Status**: Partial implementation  
**Location**: `Docs/TODO/MiraclesFramework_TODO.md`

**Issues**:
- Many miracle types not implemented
- Gesture recognition optional (hotkeys implemented)
- Targeting & area display incomplete
- Presentation polish pending

**Impact**: Limited miracle gameplay

**Effort**: 6-8 weeks (remaining work)

### 17. Terraforming Prototype
**Status**: Architecture recommendations only  
**Location**: `Docs/TODO/TerraformingPrototype_TODO.md`

**Issues**:
- Prototype not started
- Many open questions unanswered
- MVP scope undefined

**Impact**: Terraforming feature cannot be implemented

**Effort**: 6-12 months (prototype phase)

## Testing & Validation Debt

### 18. Deterministic Replay Harness
**Status**: Design documented, not implemented  
**Location**: `Docs/QA/TestingStrategy.md`

**Issues**:
- Snapshot system not implemented
- Replay harness not built
- Cross-platform determinism validation missing

**Impact**: Cannot validate deterministic execution

**Effort**: 2-3 weeks

### 19. Nightly Stress Suite
**Status**: Design documented, not automated  
**Location**: `Docs/QA/TestingStrategy.md`

**Issues**:
- 100k entity soak test not automated
- Long-run determinism test not implemented
- Memory leak detection incomplete

**Impact**: Performance regressions may go undetected

**Effort**: 1-2 weeks

### 20. Regression Scene Automation
**Status**: Design documented, not implemented  
**Location**: `Docs/QA/TestingStrategy.md`

**Issues**:
- Villager loop regression scene not automated
- Miracle rain regression scene not automated
- Resource delivery regression scene not automated

**Impact**: End-to-end functionality may break silently

**Effort**: 1-2 weeks

## Performance & Telemetry Debt

### 21. External Dashboard Integration
**Status**: Plan exists, not implemented  
**Location**: `Docs/QA/TelemetryEnhancementPlan.md`

**Issues**:
- Grafana/InfluxDB integration not implemented
- Telemetry export system not built
- ~~JSON Lines format not implemented~~ ✅ `PerformanceTelemetryExportSystem` writes NDJSON via `PUREDOTS_PERF_TELEMETRY_PATH`

**Impact**: No long-term trend analysis

**Effort**: 1-2 weeks

### 22. Automated Profiling Harness
**Status**: Plan exists, not implemented  
**Location**: `Docs/QA/TelemetryEnhancementPlan.md`

**Issues**:
- Profiling script not created
- CI integration missing
- Baseline comparison not automated

**Impact**: Performance regressions may go undetected

**Effort**: 1 week

### 23. Job Scheduling Instrumentation
**Status**: Plan exists, not implemented  
**Location**: `Docs/QA/TelemetryEnhancementPlan.md`

**Issues**:
- Worker thread utilization tracking missing
- Job completion time metrics not collected
- Dependency chain depth not measured

**Impact**: Bottlenecks harder to diagnose

**Effort**: 1 week

## Build & CI Debt

### 24. IL2CPP Build Automation
**Status**: Checklist exists, not automated  
**Location**: `Docs/CI/CI_AutomationPlan.md`

**Issues**:
- IL2CPP build script not created
- CI integration missing
- Validation steps not automated

**Impact**: AOT regressions may reach production

**Effort**: 1 week

### 25. Test Coverage Reporting
**Status**: Plan exists, not integrated  
**Location**: `Docs/CI/CI_AutomationPlan.md`

**Issues**:
- Coverage collection not configured
- HTML reports not generated
- Baseline comparison missing

**Impact**: Coverage trends unknown

**Effort**: 1 week

### 26. AssetBundle/Addressable Builds
**Status**: Plan exists, not implemented  
**Location**: `Docs/CI/CI_AutomationPlan.md`

**Issues**:
- Content build automation missing
- Bundle validation not implemented
- CDN upload not automated

**Impact**: Content updates require manual builds

**Effort**: 1-2 weeks

## Summary by Priority

**High Priority** (3 items, ~4-5 weeks):
- Presentation Bridge Implementation
- Compilation Health Issues
- Terraforming Hooks Integration

**Medium Priority** (5 items, ~18-20 weeks):
- Reusable AI Behavior Modules
- Meta Registry Implementation
- Integration Test Coverage Gaps
- Content Neutrality Validation
- Slices & Ownership Definition

**Lower Priority** (5 items, ~6-8 weeks):
- Migration Plan & Asset Salvage
- Save/Load Determinism Tests
- Platform-Specific Quirks Documentation
- Crash Reporting Integration
- Agent Workflow Protocol

**Domain-Specific** (4 items, ~18-24 weeks):
- Villager Systems SoA Refactor
- Villager Local Steering System
- Miracles Framework Completion
- Terraforming Prototype

**Testing & Validation** (3 items, ~4-6 weeks):
- Deterministic Replay Harness
- Nightly Stress Suite
- Regression Scene Automation

**Performance & Telemetry** (3 items, ~3-4 weeks):
- External Dashboard Integration
- Automated Profiling Harness
- Job Scheduling Instrumentation

**Build & CI** (3 items, ~3-4 weeks):
- IL2CPP Build Automation
- Test Coverage Reporting
- AssetBundle/Addressable Builds

**Total Estimated Effort**: ~56-71 weeks (approximately 1-1.5 years)

## Recommendations

1. **Immediate Focus**: Address compilation health issues (2-3 days) to unblock development
2. **Short-term**: Complete presentation bridge implementation (2-3 weeks) to enable visual iteration
3. **Medium-term**: Implement reusable AI modules and meta registries (staged rollout)
4. **Long-term**: Establish governance, complete domain-specific refactors, build comprehensive testing infrastructure

## References

- `PureDOTS_TODO.md` - Original project TODO
- `Docs/TODO/` - Domain-specific TODOs
- `Docs/QA/TestingStrategy.md` - Testing gaps
- `Docs/QA/TelemetryEnhancementPlan.md` - Telemetry gaps
- `Docs/CI/CI_AutomationPlan.md` - CI automation gaps
- `Docs/DesignNotes/MetaRegistryRoadmap.md` - Meta registry implementation plan
