# Documentation Consolidation & Organization Proposal

**Date**: 2025-12-01
**Purpose**: Consolidate scattered documentation, add implementation-friendly best practices, and establish maintainable structure

---

## Executive Summary

The documentation is **well-structured at a high level** but suffers from:
- **72 loose files** in root `Docs/` folder (should be ~20-30 focused docs)
- **Large temporary files** (`conole.md` = 206KB, likely console logs)
- **Multiple summary/progress files** that overlap or are outdated
- **Missing implementation guides** for DOTS 1.4.x, C# 9, Unity Input System
- **Scattered TODO files** across multiple directories

**Goal**: Reduce to ~30 core docs in root, archive/consolidate the rest, add implementation guides.

---

## Current State Analysis

### What's Working Well ✅

1. **Clear Organizational Structure**
   - Framework docs: `Packages/com.moni.puredots/Documentation/`
   - Game-specific: `Assets/Projects/{Godgame|Space4X}/Docs/`
   - Cross-cutting: `Docs/` (architecture, guides, mechanics)

2. **Strong Entry Points**
   - `INDEX.md` - comprehensive navigation
   - `FoundationGuidelines.md` - coding standards
   - `PUREDOTS_INTEGRATION_SPEC.md` - integration guide
   - `Mechanics/README.md` - cross-game mechanics catalog

3. **Good Conceptual Documentation**
   - PatternBible.md (50+ narrative patterns)
   - Mechanics system (12 cross-game mechanics)
   - Design notes well-organized in framework

### What Needs Attention ⚠️

#### 1. **Root Docs/ Folder Clutter** (72 files → target: ~30)

**Temporary/Log Files** (DELETE):
- `conole.md` (206KB) - appears to be console dump
- `consoleerror.md` (64KB) - error logs
- `pass.md` (11KB) - unclear purpose

**Progress/Status Files** (ARCHIVE → `Docs/Archive/Progress/`):
- `Implementation_*.md` (5 files)
- `Progress*.md` (2 files)
- `*_Summary.md` (10+ files)
- `*_Status.md` (4 files)
- `Readiness_Assessment.md`
- `ROADMAP_STATUS.md`

**Fix/Diagnostic Files** (ARCHIVE → `Docs/Archive/Fixes/`):
- `Camera_*_Fix.md` (3 files)
- `Mining_Demo_*.md` (6 files)
- `*_Troubleshooting.md` (3 files)
- `Fix_SubScene_And_Missing_Scripts.md`
- `Render_Pipeline_Fix.md`
- `SubScene_Migration_Fix.md`
- `Console_*.md` (3 files)

**One-Off Setup Guides** (CONSOLIDATE → `Docs/Guides/Setup/`):
- `Demo_Scenes_Implementation_Plan.md`
- `Coplay_SubScene_Setup_Prompt.md`
- `Mining_Demo_Setup*.md`
- `Resource_Setup_Critical.md`
- `ResourceTypeId_CRITICAL.md`

**Specialized Tools/Workflows** (MOVE → `Docs/Tools/`):
- `MCP_VFX_Graph_Tools.md` (39KB)
- `VFX_HELPER_FIXES.md`
- `WSL_Unity_MCP_Relay.md`

#### 2. **TODO Files Scattered** (Consolidate strategy needed)

**Current TODO files:**
- `PureDOTS_TODO.md` (root)
- `Docs/OUTSTANDING_TODOS_SUMMARY.md`
- `Docs/TODO/*.md` (10+ files)
- `Docs/FOUNDATION_GAPS_QUICK_REFERENCE.md`

**Recommendation**: Keep 2-3 focused TODO files:
1. `PureDOTS_TODO.md` - Active sprint/immediate work
2. `Docs/TODO/BACKLOG.md` - Consolidated backlog by domain
3. Archive completed TODOs to `Docs/Archive/CompletedWork/`

#### 3. **Missing Implementation Guides** (NEW CONTENT NEEDED)

The documentation lacks **implementation-friendly** best practices for:

**DOTS 1.4.x Specifics**:
- Version-specific API changes
- Migration from DOTS 1.3.x
- Entities 1.4 performance patterns
- Burst 1.8+ optimizations

**C# 9 Features**:
- Record types for data transfer objects
- Init-only setters for components
- Pattern matching in systems
- Target-typed new expressions
- Top-level statements (entry points)

**Unity Input System**:
- Integration with ECS
- Action maps and bindings
- Runtime rebinding patterns
- Multiplayer input handling

---

## Proposed Reorganization

### Phase 1: Clean Root Docs/ Folder (Immediate)

**Target Structure (30 core files):**

```
Docs/
├── INDEX.md                              # Navigation hub
├── FoundationGuidelines.md               # Core coding standards
├── PUREDOTS_INTEGRATION_SPEC.md          # Integration guide
├── ORIENTATION_SUMMARY.md                # Codebase overview
├── BehaviorAlignment_Summary.md          # Entity behaviors
├── CONCEPT_CAPTURE_METHODS.md            # Documentation patterns
├── DOCUMENTATION_ORGANIZATION_GUIDE.md   # This guide
├── NEW_PROJECT_QUICKSTART.md             # New project setup
├── PERFORMANCE_PLAN.md                   # Performance strategy
├── TECHNICAL_DEBT.md                     # Known issues
├── OUTSTANDING_TODOS_SUMMARY.md          # Active work summary
├── Vision.md                             # Project vision
├── ROADMAP.md                            # Strategic roadmap
├── TestingGuidelines.md                  # Testing standards
├── DependencyAudit.md                    # Dependencies
├── DeprecationList.md                    # Deprecated APIs
├── PresentationBridgeArchitecture.md     # Presentation layer
├── Streaming_Content.md                  # Content streaming
├── EnvironmentSetup.md                   # Dev environment
│
├── Architecture/                         # Core architecture docs
│   ├── PureDOTS_As_Framework.md
│   ├── Framework_Formalization_Summary.md
│   ├── GameDOTS_Separation.md
│   ├── DOTS_Alignment_Roadmap.md
│   └── DOTS_Sample_Gap_Analysis.md
│
├── Mechanics/                            # Cross-game mechanics
│   ├── README.md
│   └── [12 mechanic docs]
│
├── Guides/                               # How-to guides
│   ├── GettingStarted.md
│   ├── GameProject_Integration.md
│   ├── LinkingExternalGameProjects.md
│   ├── SceneSetup.md
│   ├── PhysicsValidation.md
│   ├── ComponentMigrationGuide.md
│   ├── GameIntegrationGuide.md
│   ├── PerformanceIntegrationRoadmap.md
│   ├── PhysicsVsSpatialGrid.md
│   ├── AI_Integration_Guide.md
│   ├── SpatialQueryUsage.md
│   ├── UsingPureDOTSEnvironmentAuthoring.md
│   ├── CameraIntegration.md
│   ├── CameraIntegrationGuide.md
│   └── Authoring/
│       └── EnvironmentAndSpatialValidation.md
│
├── BestPractices/                        # ⭐ NEW: Implementation guides
│   ├── README.md                         # Best practices index
│   ├── DOTS_1_4_Patterns.md             # DOTS 1.4.x specifics
│   ├── CSharp9_Features.md              # C# 9 usage guide
│   ├── UnityInputSystem_ECS.md          # Input System + ECS
│   ├── BurstOptimization.md             # Burst compilation
│   ├── JobSystemPatterns.md             # Job system best practices
│   ├── MemoryLayoutOptimization.md      # Cache-friendly layouts
│   ├── EntityCommandBuffers.md          # ECB patterns
│   ├── ComponentDesignPatterns.md       # Component sizing/splitting
│   └── DeterminismChecklist.md          # Deterministic code rules
│
├── QA/                                   # Quality assurance
│   ├── IntegrationTestChecklist.md
│   ├── PerformanceProfiles.md
│   ├── PerformanceBudgets.md
│   ├── BootstrapAudit.md
│   ├── IL2CPP_AOT_Audit.md
│   ├── TestingStrategy.md
│   ├── TelemetryEnhancementPlan.md
│   └── SinglePlayerRewind.md
│
├── TODO/                                 # Active TODOs
│   ├── BACKLOG.md                        # ⭐ NEW: Consolidated backlog
│   ├── [Domain-specific TODOs]
│   └── Done/                             # Completed items
│
├── ExtensionRequests/                    # Game → Framework requests
│   ├── README.md
│   └── TEMPLATE.md
│
├── CI/                                   # CI/CD automation
│   └── CI_AutomationPlan.md
│
├── Tools/                                # ⭐ NEW: Tool-specific docs
│   ├── MCP_VFX_Graph_Tools.md
│   ├── VFX_HELPER_FIXES.md
│   └── WSL_Unity_MCP_Relay.md
│
├── Archive/                              # ⭐ NEW: Historical docs
│   ├── Progress/                         # Progress reports
│   ├── Fixes/                            # Fixed issues
│   ├── Setup/                            # One-off setups
│   └── CompletedWork/                    # Finished TODOs
│
└── Roadmaps/                             # Strategic planning
    └── PureDOTS_MetaRoadmap.md
```

---

## Phase 2: Create New Implementation Guides

### 1. `BestPractices/DOTS_1_4_Patterns.md`

**Content:**
- Version-specific API changes from 1.3.x
- `ISystem` vs `SystemBase` usage patterns
- `SystemAPI.Query` best practices
- Aspect-oriented queries (new in 1.4)
- Source generators gotchas
- Baker patterns and entity relationships
- Structural changes and command buffers
- Blob asset authoring improvements
- Performance improvements in 1.4

### 2. `BestPractices/CSharp9_Features.md`

**Content:**
- **Records**: When to use for data transfer objects
- **Init-only setters**: Immutable component patterns
- **Target-typed new**: Cleaner component initialization
- **Pattern matching**: Switch expressions in systems
- **Top-level statements**: Bootstrap entry points
- **Function pointers**: Burst-compatible delegates
- **Module initializers**: Static init patterns
- **Covariant returns**: Type hierarchy helpers

### 3. `BestPractices/UnityInputSystem_ECS.md`

**Content:**
- Installing and configuring Input System
- Integration patterns with ECS
- Action maps in game projects
- Input handling in systems (deterministic)
- Separation: Input reading → Command buffer → Simulation
- Multiplayer input handling
- Runtime rebinding UI patterns
- Testing input in ECS tests
- Performance considerations

### 4. `BestPractices/BurstOptimization.md`

**Content:**
- Burst compilation checklist
- Common Burst errors and fixes
- SIMD vectorization patterns
- Loop optimization techniques
- Branch prediction hints
- Avoiding managed types
- Native containers best practices
- Function pointers for polymorphism
- Burst Inspector workflow

### 5. `BestPractices/JobSystemPatterns.md`

**Content:**
- IJob vs IJobChunk vs IJobEntity
- Parallel jobs with safety system
- Job dependencies and chains
- EntityCommandBuffer.ParallelWriter
- Job scheduling best practices
- Read/Write dependencies optimization
- Avoiding structural changes in jobs
- Testing job-based systems

### 6. `BestPractices/MemoryLayoutOptimization.md`

**Content:**
- Component size guidelines (hot/medium/cold)
- Cache line awareness (64-byte boundaries)
- Struct packing and alignment
- Archetype design for chunk utilization
- Hot/cold data splitting patterns
- Companion entity pattern
- Buffer element types
- Blob asset data layout
- Memory profiling workflow

### 7. `BestPractices/EntityCommandBuffers.md`

**Content:**
- When to use ECB vs direct EntityManager
- Playback timing and system ordering
- ParallelWriter patterns
- Avoiding duplicate commands
- Deferred structural changes
- Command buffer cleanup
- Debugging ECB issues
- Performance impact analysis

### 8. `BestPractices/ComponentDesignPatterns.md`

**Content:**
- Hot path component sizing
- Tag components vs data components
- Component vs DynamicBuffer
- Shared components for configuration
- Enable/Disable components
- Cleanup components
- Component lifecycle patterns
- Archetype stability considerations

### 9. `BestPractices/DeterminismChecklist.md`

**Content:**
- Fixed time step requirements
- RNG seeding patterns
- Avoiding Unity APIs in simulation
- Float precision considerations
- Sort stability requirements
- Collection iteration order
- Rewind-safe system patterns
- Testing determinism
- Replay verification

### 10. `BestPractices/README.md` (Index)

**Navigation hub linking to all best practices docs**

---

## Phase 3: Consolidate TODO Files

### Current State
- Multiple TODO files with overlapping concerns
- No clear distinction between active/backlog/done
- Hard to find what's actively being worked on

### Proposed Structure

**1. `PureDOTS_TODO.md` (Root)**
- **Current sprint work only** (2-3 weeks max)
- High-level tasks being actively worked
- Links to domain-specific TODOs for details

**2. `Docs/TODO/BACKLOG.md` (NEW)**
```markdown
# PureDOTS Backlog

## By Domain

### Framework Core
- [ ] Registry system optimization
- [ ] Rewind system enhancements
- ...

### Rendering
- [ ] LOD system improvements
- [ ] Aggregate rendering optimization
- ...

### AI & Behavior
- [ ] Utility AI enhancements
- [ ] Pathfinding optimization
- ...

### Resources & Economy
- [ ] Resource quality system
- [ ] Production chains
- ...

[etc by domain]
```

**3. Domain-Specific TODOs** (Keep focused)
- `SystemIntegration_TODO.md`
- `ClimateSystems_TODO.md`
- `VillagerSystems_TODO.md`
- `ResourcesFramework_TODO.md`
- etc.

**4. Archive Completed**
Move to `Docs/Archive/CompletedWork/{Year}/`

---

## Phase 4: Archive Strategy

### What to Archive

**Progress Reports** → `Docs/Archive/Progress/{Year}/`
- `Implementation_*.md`
- `Progress*.md`
- `*_Summary.md` (implementation summaries)
- `Readiness_Assessment.md`
- `ROADMAP_STATUS.md`

**Fixed Issues** → `Docs/Archive/Fixes/{Year}/`
- `Camera_*_Fix.md`
- `Mining_Demo_*.md`
- `*_Troubleshooting.md`
- `Render_Pipeline_Fix.md`
- `SubScene_Migration_Fix.md`
- Console error dumps

**Completed Setup Guides** → `Docs/Archive/Setup/{Year}/`
- One-off demo setups
- Migration guides (after migration complete)
- Critical setup docs (after resolved)

**Completed TODOs** → `Docs/Archive/CompletedWork/{Year}/`
- Finished TODO sections
- Milestone completion reports

### Archive File Format

Add header to archived files:
```markdown
> **ARCHIVED**: {Date}
> **Reason**: {Completed/Superseded/Obsolete}
> **Current Reference**: [Link to current doc if superseded]

[Original content below]
```

---

## Implementation Plan

### Week 1: Cleanup (2-3 hours)

**Day 1: Triage (1 hour)**
- [ ] Review all 72 root docs
- [ ] Categorize: Keep/Archive/Delete/Consolidate
- [ ] Create archive folders

**Day 2: Archive & Delete (1 hour)**
- [ ] Delete temp files (conole.md, consoleerror.md, pass.md)
- [ ] Move progress/fix files to Archive
- [ ] Move tool docs to Tools/
- [ ] Update INDEX.md references

**Day 3: Validate (30 min)**
- [ ] Check all links in INDEX.md
- [ ] Verify navigation paths
- [ ] Update README files

### Week 2: Best Practices Docs (8-10 hours)

**Day 1-2: DOTS & C# (4 hours)**
- [ ] Write `DOTS_1_4_Patterns.md`
- [ ] Write `CSharp9_Features.md`
- [ ] Write `BestPractices/README.md`

**Day 3: Input & Burst (2 hours)**
- [ ] Write `UnityInputSystem_ECS.md`
- [ ] Write `BurstOptimization.md`

**Day 4-5: Advanced Topics (4 hours)**
- [ ] Write `JobSystemPatterns.md`
- [ ] Write `MemoryLayoutOptimization.md`
- [ ] Write `EntityCommandBuffers.md`
- [ ] Write `ComponentDesignPatterns.md`
- [ ] Write `DeterminismChecklist.md`

### Week 3: TODO Consolidation (2-3 hours)

**Day 1: Consolidate (2 hours)**
- [ ] Create `TODO/BACKLOG.md`
- [ ] Merge overlapping TODOs
- [ ] Archive completed TODOs
- [ ] Update `PureDOTS_TODO.md` to sprint-only

**Day 2: Update Navigation (1 hour)**
- [ ] Update INDEX.md
- [ ] Update all README.md files
- [ ] Add navigation links between related docs

---

## Maintenance Guidelines

### Going Forward

**1. New Documents**
- Add to appropriate subfolder (not root unless high-level)
- Update INDEX.md immediately
- Link from relevant READMEs

**2. Progress Tracking**
- Use git commits for daily progress
- Write implementation summaries monthly
- Archive summaries after 6 months

**3. TODO Management**
- Keep `PureDOTS_TODO.md` to current sprint (2-3 weeks)
- Move to `BACKLOG.md` if not starting soon
- Archive to `CompletedWork/` when done

**4. Archive Policy**
- Archive progress reports after 6 months
- Archive fix docs after issue resolved + 3 months
- Keep cross-references when archiving
- Add archive notice headers

**5. Best Practices Updates**
- Review quarterly with Unity releases
- Update when DOTS version bumps
- Incorporate team learnings

---

## Benefits

### For Developers
✅ Clear navigation (30 vs 72 files in root)
✅ Implementation guides at hand (DOTS 1.4.x, C# 9, Input System)
✅ Easy to find active work (sprint TODO vs backlog)
✅ Best practices codified (not tribal knowledge)

### For New Team Members
✅ Faster onboarding (GettingStarted → BestPractices)
✅ Clear patterns to follow
✅ Historical context preserved but not cluttering

### For Project Health
✅ Maintainable structure (clear policies)
✅ Historical record preserved
✅ Active vs archived clearly separated
✅ Knowledge captured, not lost

---

## Risks & Mitigation

### Risk: Breaking Links
**Mitigation**: Use search-replace for moved files, validate all links before committing

### Risk: Losing Historical Context
**Mitigation**: Archive with headers, not delete; maintain archive index

### Risk: Best Practices Become Stale
**Mitigation**: Quarterly review process, version-track best practices docs

### Risk: Resistance to New Structure
**Mitigation**: Roll out incrementally, get team feedback, iterate

---

## Next Steps

1. **Review this proposal** with team
2. **Agree on archive strategy** (what to keep/delete)
3. **Schedule Week 1 cleanup** (pick a low-impact week)
4. **Assign best practices authoring** (domain experts write their sections)
5. **Set maintenance calendar** (quarterly reviews)

---

## Appendix A: Files to Delete

**Confirmed temporary/log files:**
- `Docs/conole.md` (206KB console dump)
- `Docs/consoleerror.md` (64KB error dump)
- `Docs/pass.md` (11KB unclear notes)

**Reason**: These appear to be console output captures or temporary notes, not documentation.

---

## Appendix B: Files to Archive

### Progress Reports → `Docs/Archive/Progress/2025/`
- `Implementation_Framework_Updates.md`
- `Implementation_Space4X_Progress.md`
- `Implementation_Status_Report.md`
- `Implementation_Sweep_PR_Summary.md`
- `Progress.md`
- `Progress_Report.md`
- `Readiness_Assessment.md`
- `ROADMAP_STATUS.md`
- `AI_Readiness_Implementation_Summary.md`
- `Console_Log_Analysis.md`
- `Console_Log_Summary.md`
- `Console_Warnings_Fixed_Summary.md`
- `Runtime_Errors_Fixed_Summary.md`
- `Camera_Refactor_Integration_Summary.md`

### Fix Documentation → `Docs/Archive/Fixes/2025/`
- `Camera_Background_Only_Fix.md`
- `Camera_Rendering_Fix.md`
- `Camera_Debugging_Guide.md`
- `Mining_Demo_Diagnosis.md`
- `Mining_Demo_Fix_Summary.md`
- `Mining_Demo_Quick_Fix.md`
- `Mining_Demo_Setup.md`
- `Mining_Demo_Setup_Instructions.md`
- `Mining_Loops_Demo_Status.md`
- `Entity_Baking_Troubleshooting.md`
- `Units_Not_Moving_Troubleshooting.md`
- `Profiler_Hiccup_Diagnosis_Guide.md`
- `Render_Pipeline_Fix.md`
- `SubScene_Migration_Fix.md`
- `Fix_SubScene_And_Missing_Scripts.md`

### Setup Guides → `Docs/Archive/Setup/2025/`
- `Demo_Scenes_Implementation_Plan.md`
- `Coplay_SubScene_Setup_Prompt.md`
- `Resource_Setup_Critical.md`
- `ResourceTypeId_CRITICAL.md`
- `Scene_Setup_Status.md`
- `Vessel_And_Villager_Movement_Setup.md`

### Specialized Analyses → `Docs/Archive/Analysis/2025/`
- `VillagerLoopAnalysis.md`
- `DependencyAudit.md` (if completed)
- `Adapters.md` (if superseded)
- `DeprecationList.md` (maintain in root, but archive old versions)

---

## Appendix C: Best Practices Content Outline

### DOTS 1.4.x Patterns (Detailed Outline)

```markdown
# DOTS 1.4.x Implementation Patterns

## Overview
- Version differences from 1.3.x
- When to upgrade
- Breaking changes summary

## System Authoring
### ISystem vs SystemBase
- When to use each
- Migration path from SystemBase
- Performance implications
- Ref vs value semantics

### SystemAPI.Query
- Syntax and patterns
- Performance characteristics
- Common gotchas
- Caching considerations

### Aspect-Oriented Queries (New in 1.4)
- Aspect definition patterns
- Reusable query logic
- Performance benefits
- Testing aspects

## Entity Creation & Baking
### Baker Patterns
- Basic baker structure
- Entity relationships in bakers
- Prefab baking
- Additional entities
- Dependencies between bakers

### Blob Asset Authoring
- ScriptableObject → Blob pipeline
- Blob builder patterns
- Sharing blobs across entities
- Blob asset disposal

## Structural Changes
### EntityCommandBuffer Best Practices
- Playback timing strategies
- ParallelWriter patterns
- Deferred vs immediate
- Debugging ECB issues

### Enable/Disable Components
- Archetype stability
- Performance implications
- Query filtering patterns

## Performance Patterns
### Chunk Iteration
- IJobChunk patterns
- Chunk-level operations
- EntityInQueryIndex usage

### Burst Compilation
- New Burst 1.8+ features
- Auto-vectorization hints
- SIMD intrinsics
- Burst Inspector workflow

## Source Generators
### Generated Code Patterns
- What gets generated
- Troubleshooting generator errors
- IDE integration
- Build-time implications

## Testing
### Entity Testing Framework
- Test world setup
- Deterministic testing
- Time manipulation
- Cleanup patterns

## Migration Guide
### From 1.3.x
- API renames
- Behavior changes
- Performance improvements
- Recommended order of operations

## Common Pitfalls
- Race conditions in jobs
- Archetype fragmentation
- Command buffer order
- World initialization
```

### C# 9 Features (Detailed Outline)

```markdown
# C# 9 Features for DOTS Development

## Overview
- Compatibility with Unity 2022.3+
- Which features work with Burst
- Which features work with IL2CPP

## Records
### Use Cases in DOTS
- Data transfer objects (authoring → runtime)
- Configuration structures
- Event/message types

### Examples
```csharp
// Configuration record
public record ResourceConfig(
    int MaxQuantity,
    float BaseWeight,
    bool IsPerishable
);

// DTO for baking
public record EntitySpawnRequest(
    float3 Position,
    quaternion Rotation,
    Entity Prefab
);
```

### Limitations
- Not for IComponentData (struct required)
- Reference types (not Burst-compatible)
- Useful in authoring/managed code only

## Init-Only Setters
### Component Patterns
```csharp
public struct ConfigurableComponent : IComponentData
{
    // Mutable hot data
    public float CurrentValue;

    // Config (set once)
    public float MaxValue { get; init; }
    public float MinValue { get; init; }
}
```

### Benefits
- Clear separation of config vs state
- Compile-time immutability
- Better intent communication

## Pattern Matching Enhancements
### Switch Expressions in Systems
```csharp
public partial struct AIDecisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (ai, entity) in
            SystemAPI.Query<RefRW<AIState>>().WithEntityAccess())
        {
            ai.ValueRW.NextAction = ai.ValueRO.CurrentState switch
            {
                AIStateType.Idle when ai.ValueRO.Hunger > 80 => ActionType.Eat,
                AIStateType.Idle when ai.ValueRO.Danger > 50 => ActionType.Flee,
                AIStateType.Fleeing when ai.ValueRO.Danger < 20 => ActionType.Idle,
                AIStateType.Attacking when ai.ValueRO.TargetValid => ActionType.Attack,
                _ => ActionType.Idle
            };
        }
    }
}
```

### Benefits
- Cleaner state machine logic
- Better compile-time checking
- Easier to read intent

## Target-Typed New
### Component Initialization
```csharp
// Before
var buffer = new DynamicBuffer<ResourceElement>();
var query = new EntityQuery(desc);

// After (C# 9)
DynamicBuffer<ResourceElement> buffer = new();
EntityQuery query = new(desc);
```

### EntityCommandBuffer Usage
```csharp
// Cleaner ECB commands
ecb.AddComponent(entity, new HealthComponent { Value = 100 });
// becomes
ecb.AddComponent(entity, new() { Value = 100 });
```

## Top-Level Statements
### Bootstrap Entry Points
```csharp
// BootstrapEntry.cs
using Unity.Entities;
using PureDOTS.Bootstrap;

// No class wrapper needed for simple entry points
BootstrapCoordinator.Initialize();
UnityEngine.Debug.Log("PureDOTS Bootstrap Complete");

// Helper methods below if needed
static void ConfigureDefaultWorld() { ... }
```

### Use Cases
- Simple bootstrap scripts
- Test runners
- Build automation scripts

## Function Pointers (Burst-Compatible)
### Polymorphic Behavior in Jobs
```csharp
[BurstCompile]
public unsafe struct PolymorphicJob : IJobChunk
{
    public delegate*<float, float> ProcessFunction;

    public void Execute(ArchetypeChunk chunk, ...)
    {
        var result = ProcessFunction(input);
    }
}

// Usage
[BurstCompile]
private static float SquareFunction(float x) => x * x;

new PolymorphicJob
{
    ProcessFunction = &SquareFunction
}.Schedule();
```

### Benefits vs Limitations
✅ Burst-compatible
✅ Zero allocation
✅ Inlineable
❌ No capturing (must be static)
❌ Unsafe context required

## Module Initializers
### Static Registration
```csharp
public static class SystemRegistry
{
    [ModuleInitializer]
    internal static void RegisterSystems()
    {
        // Runs before any code in assembly
        RegisterSystemType<VillagerAISystem>();
        RegisterSystemType<ResourceHarvestSystem>();
    }
}
```

### Use Cases
- System registration
- Static lookups initialization
- Registry population

## Covariant Returns
### Type Hierarchy Helpers
```csharp
public abstract class ComponentBaker<T> : MonoBehaviour
{
    public abstract IComponentData GetComponent();
}

public class VillagerBaker : ComponentBaker<VillagerId>
{
    // Covariant return (C# 9)
    public override VillagerId GetComponent()
        => new VillagerId { Value = id };
}
```

## Best Practices Summary
1. **Use records** for DTOs and config objects
2. **Use init setters** for component config fields
3. **Use pattern matching** for state machines
4. **Use target-typed new** for clarity
5. **Use function pointers** for polymorphism in Burst
6. **Use module initializers** for static setup
7. **Avoid** records/classes in hot paths (use structs)

## Compatibility Matrix
| Feature | Burst | IL2CPP | Managed | Notes |
|---------|-------|--------|---------|-------|
| Records | ❌ | ✅ | ✅ | Reference types only |
| Init setters | ✅ | ✅ | ✅ | Works on structs |
| Pattern matching | ✅ | ✅ | ✅ | Full support |
| Target-typed new | ✅ | ✅ | ✅ | Full support |
| Function pointers | ✅ | ✅ | ✅ | Burst-compatible |
| Module initializers | ❌ | ✅ | ✅ | Runs before Domain |
| Covariant returns | ❌ | ✅ | ✅ | Managed only |
```

### Unity Input System + ECS (Detailed Outline)

```markdown
# Unity Input System Integration with ECS

## Overview
- Why Input System vs legacy Input
- Version compatibility
- Package installation

## Architecture Pattern
### Separation of Concerns
```
Player Input Actions (managed)
    ↓
Input Reading System (managed, early in frame)
    ↓
Command Components (ECS)
    ↓
Simulation Systems (Burst, deterministic)
```

## Installation & Setup
### Package Manager
1. Install "Input System" package
2. Configure player settings
3. Set up action assets

### Project Settings
- Active Input Handling: Both (during migration) → New
- Background Behavior
- Update Mode

## Input Actions Setup
### Creating Action Maps
```csharp
// Generated C# class from .inputactions asset
public class GameplayActions : IInputActionCollection2
{
    public InputAction Move { get; }
    public InputAction Attack { get; }
    public InputAction UseAbility { get; }
}
```

### Action Types
- Value (continuous): Movement, look
- Button (discrete): Jump, attack
- Pass-Through (raw): Mouse delta

## ECS Integration Pattern
### Input Command Components
```csharp
// Command component (written by input system, read by simulation)
public struct MoveCommand : IComponentData
{
    public float2 Direction;
    public bool Sprint;
}

public struct AttackCommand : IComponentData, IEnableableComponent
{
    public float2 TargetDirection;
}
```

### Input Reading System (Managed)
```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
public partial class InputReadingSystem : SystemBase
{
    private GameplayActions _actions;

    protected override void OnCreate()
    {
        _actions = new GameplayActions();
        _actions.Enable();
    }

    protected override void OnUpdate()
    {
        // Read input from Unity Input System
        var moveInput = _actions.Move.ReadValue<Vector2>();
        var attackPressed = _actions.Attack.WasPressedThisFrame();

        // Write to ECS command components
        SystemAPI.SetSingleton(new MoveCommand
        {
            Direction = moveInput,
            Sprint = _actions.Sprint.IsPressed()
        });

        // Enable attack command if pressed
        if (attackPressed)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            SystemAPI.SetComponentEnabled<AttackCommand>(playerEntity, true);
        }
    }
}
```

### Simulation System (Burst)
```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var moveCommand = SystemAPI.GetSingleton<MoveCommand>();

        // Process move command deterministically
        foreach (var (transform, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>())
        {
            if (math.lengthsq(moveCommand.Direction) > 0.01f)
            {
                var dir = math.normalize(moveCommand.Direction);
                var speedMultiplier = moveCommand.Sprint ? 2f : 1f;
                transform.ValueRW.Position += new float3(dir, 0) *
                    speed.ValueRO.Value * speedMultiplier * SystemAPI.Time.DeltaTime;
            }
        }
    }
}
```

## Determinism Considerations
### Input Recording for Replay
```csharp
public struct InputHistory : IComponentData
{
    public BlobAssetReference<InputFrameBlob> RecordedInputs;
}

public struct InputFrameBlob
{
    public struct Frame
    {
        public uint Tick;
        public float2 MoveDirection;
        public bool AttackPressed;
    }

    public BlobArray<Frame> Frames;
}
```

### Replay System
```csharp
[BurstCompile]
public partial struct InputReplaySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ReplayMode>(out var replay) || !replay.IsReplaying)
            return;

        var history = SystemAPI.GetSingleton<InputHistory>();
        var currentTick = SystemAPI.GetSingleton<GameTick>().Value;

        // Read from recorded input instead of real input
        ref var inputs = ref history.RecordedInputs.Value;
        for (int i = 0; i < inputs.Frames.Length; i++)
        {
            if (inputs.Frames[i].Tick == currentTick)
            {
                SystemAPI.SetSingleton(new MoveCommand
                {
                    Direction = inputs.Frames[i].MoveDirection
                });
                break;
            }
        }
    }
}
```

## Multiplayer Patterns
### Client Input → Server Authority
```csharp
// Client: Read input, send to server
public partial class ClientInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var input = _actions.Move.ReadValue<Vector2>();

        // Send input to server (NOT directly to simulation)
        NetworkSend(new InputPacket
        {
            Tick = GetCurrentTick(),
            MoveDirection = input
        });
    }
}

// Server: Receive input, apply to simulation
[BurstCompile]
public partial struct ServerInputApplicationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Read from network buffer
        var inputBuffer = SystemAPI.GetSingletonBuffer<ReceivedInputPacket>();

        foreach (var packet in inputBuffer)
        {
            if (TryGetPlayerEntity(packet.PlayerId, out var entity))
            {
                SystemAPI.SetComponent(entity, new MoveCommand
                {
                    Direction = packet.MoveDirection
                });
            }
        }

        inputBuffer.Clear();
    }
}
```

## Runtime Rebinding
### Rebinding UI Pattern
```csharp
public class RebindUI : MonoBehaviour
{
    private GameplayActions _actions;

    public void StartRebind(InputAction action, int bindingIndex)
    {
        action.Disable();

        var rebind = action.PerformInteractiveRebinding(bindingIndex)
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                // Save new binding
                var json = action.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString("InputBindings", json);

                action.Enable();
                operation.Dispose();
            })
            .Start();
    }

    public void LoadBindings()
    {
        var json = PlayerPrefs.GetString("InputBindings", "");
        if (!string.IsNullOrEmpty(json))
        {
            _actions.LoadBindingOverridesFromJson(json);
        }
    }
}
```

## Testing Input
### Simulated Input in Tests
```csharp
[Test]
public void MovementSystem_RespondsToInput()
{
    // Setup
    var world = new World("TestWorld");
    var entity = world.EntityManager.CreateEntity(typeof(LocalTransform), typeof(MoveSpeed));

    // Inject simulated input
    var inputEntity = world.EntityManager.CreateEntity();
    world.EntityManager.AddComponentData(inputEntity, new MoveCommand
    {
        Direction = new float2(1, 0),
        Sprint = false
    });

    // Update movement system
    var movementSystem = world.GetOrCreateSystemManaged<MovementSystem>();
    movementSystem.Update();

    // Assert
    var transform = world.EntityManager.GetComponentData<LocalTransform>(entity);
    Assert.Greater(transform.Position.x, 0);

    world.Dispose();
}
```

## Performance Considerations
### Input Polling vs Events
- Polling (Update): Better for continuous input (movement)
- Events (callbacks): Better for discrete actions (button presses)
- Hybrid approach recommended

### Memory Allocations
- Cache action instances (don't recreate each frame)
- Dispose properly in OnDestroy
- Use value-type reads (ReadValue<T>) over object allocation

## Common Patterns
### Local Multiplayer
```csharp
// Per-player input
public struct PlayerInputComponent : IComponentData
{
    public int PlayerIndex;
}

public partial class MultiplayerInputSystem : SystemBase
{
    private GameplayActions[] _playerActions;

    protected override void OnCreate()
    {
        _playerActions = new GameplayActions[4]; // Up to 4 players
        for (int i = 0; i < 4; i++)
        {
            _playerActions[i] = new GameplayActions();
            _playerActions[i].devices = InputSystem.devices.Where(d => /* filter by player */);
        }
    }

    protected override void OnUpdate()
    {
        foreach (var (playerInput, moveCommand) in
            SystemAPI.Query<RefRO<PlayerInputComponent>, RefRW<MoveCommand>>())
        {
            var actions = _playerActions[playerInput.ValueRO.PlayerIndex];
            moveCommand.ValueRW.Direction = actions.Move.ReadValue<Vector2>();
        }
    }
}
```

### UI Navigation
```csharp
// Separate UI action map
public partial class UIInputSystem : SystemBase
{
    private GameplayActions.UIActions _uiActions;

    protected override void OnUpdate()
    {
        if (_uiActions.Submit.WasPressedThisFrame())
        {
            // Handle UI submit
        }
    }
}
```

## Migration from Legacy Input
### Parallel Support
```csharp
#if ENABLE_INPUT_SYSTEM
    // New Input System
    var move = _actions.Move.ReadValue<Vector2>();
#else
    // Legacy Input (fallback)
    var move = new Vector2(
        Input.GetAxis("Horizontal"),
        Input.GetAxis("Vertical")
    );
#endif
```

## Best Practices Summary
1. ✅ Read input in managed system (early frame)
2. ✅ Write to command components
3. ✅ Process commands in Burst systems (sim group)
4. ✅ Use enableable components for discrete actions
5. ✅ Record input for deterministic replay
6. ✅ Separate client input from server authority
7. ✅ Cache action instances (avoid allocations)
8. ✅ Test with simulated input
9. ❌ Don't read Input System directly in Burst systems
10. ❌ Don't mix input reading and simulation logic

## Troubleshooting
- **Input not working**: Check action is enabled
- **Input delayed**: Check system update order
- **Non-deterministic**: Ensure input goes through command components
- **Multiplayer desync**: Verify server authority pattern
```

---

## References
- Unity DOTS Manual: https://docs.unity3d.com/Packages/com.unity.entities@latest
- C# 9 Specification: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9
- Unity Input System: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- Burst Compiler: https://docs.unity3d.com/Packages/com.unity.burst@latest

---

*Maintainer: PureDOTS Documentation Team*
*Last Updated: 2025-12-01*
