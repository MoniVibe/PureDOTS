# Foundation Gaps Quick Reference

**Last Updated**: 2025-01-27  
**Purpose**: Quick reference for immediate foundation work priorities

## Immediate Action Items (P0)

### 1. AI Virtual Sensors (AI-001)
**File**: `Packages/com.moni.puredots/Runtime/Systems/AI/AIVirtualSensorSystem.cs` (new)  
**Status**: Planned  
**Estimate**: 1-2 weeks

**What**: Create system that populates `AISensorReading` buffers with internal villager needs (hunger, energy, morale) as virtual sensor readings.

**Why**: Currently `VillagerAISystem` uses dual path - both AI pipeline and legacy `VillagerUtilityScheduler`. Virtual sensors unify this.

**Dependencies**: None  
**Blocks**: AI-007 (Morale behaviors)

**Acceptance**:
- [ ] `AIVirtualSensorSystem` populates sensor readings for all villager needs
- [ ] `VillagerAISystem` uses virtual sensors instead of scheduler
- [ ] Tests validate virtual sensor values match needs component values

---

### 2. AI Miracle Detection (AI-002)
**File**: `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (modify)  
**Status**: Planned  
**Estimate**: 1 week

**What**: Add miracle component lookups to `AISensorCategoryFilter`, implement `AISensorCategory.Miracle` detection, remove conditional compilation.

**Why**: Enables AI to react to miracles (e.g., villagers flee from disasters, seek beneficial miracles).

**Dependencies**: Miracle components exist  
**Blocks**: None

**Acceptance**:
- [ ] `AISensorCategory.Miracle` correctly identifies miracle entities
- [ ] Sensor readings include miracle entities when category is set
- [ ] All conditional compilation removed from AI systems

---

### 3. Villager Job Behavior Stubs
**Files**: 
- `Packages/com.moni.puredots/Runtime/Systems/VillagerJobSystems.cs` (modify)
- `Packages/com.moni.puredots/Runtime/Runtime/Villager/JobBehaviors.cs` (flesh out)

**Status**: Scaffolding exists, needs implementation  
**Estimate**: 2-3 weeks

**What**: Implement actual job behavior logic for `GatherJobBehavior`, `BuildJobBehavior`, `CraftJobBehavior`, `CombatJobBehavior`. Feed archetype catalog data into AI selection.

**Why**: Currently villagers can be assigned jobs but behaviors are stubs - they don't actually perform work.

**Dependencies**: `VillagerArchetypeCatalog` exists  
**Blocks**: Villager gameplay loop

**Acceptance**:
- [ ] Gather job actually collects resources from nodes
- [ ] Build job progresses construction sites
- [ ] Craft job processes materials
- [ ] Combat job engages enemies
- [ ] Archetype catalog data influences job selection

---

### 4. Presentation Bridge Testing
**Files**: 
- `Assets/Tests/Playmode/PresentationBridgeTests.cs` (new)
- `Docs/Guides/Authoring/PresentationBridge.md` (new)

**Status**: Core implementation exists, testing missing  
**Estimate**: 1 week

**What**: Create validation tests for rewind-safe presentation, write sample authoring guide.

**Why**: Ensures presentation doesn't break determinism during rewind.

**Dependencies**: Presentation bridge systems exist  
**Blocks**: Presentation confidence

**Acceptance**:
- [ ] Tests validate presentation spawn/recycle during rewind
- [ ] Tests validate companion entity sync during rewind
- [ ] Authoring guide with examples

---

## Next Quarter Priorities (P1)

### 5. Flow Field Integration (AI-003)
**File**: `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (modify)  
**Status**: Planned  
**Estimate**: 1-2 weeks

**What**: Integrate `FlowFieldState` with `AISteeringSystem`, blend flow field direction with local avoidance.

**Why**: Enables scalable pathfinding for 100k+ agents.

**Dependencies**: Flow field systems exist  
**Blocks**: Large-scale villager simulations

---

### 6. Resources Framework - Chunk/Pile System
**Files**: 
- `Packages/com.moni.puredots/Runtime/Systems/ResourceSystems.cs` (add)
- `Packages/com.moni.puredots/Runtime/Runtime/Resource/ResourceComponents.cs` (add)

**Status**: Planned  
**Estimate**: 3-4 weeks

**What**: Implement resource chunk physics, pile merging, hand siphon mechanics.

**Why**: Complete resource interaction loop (gather → chunk → pile → storehouse).

**Dependencies**: Resource registry exists  
**Blocks**: Resource gameplay loop

---

### 7. Climate Systems - Biome Determination
**File**: `Packages/com.moni.puredots/Runtime/Systems/Environment/BiomeDeterminationSystem.cs` (complete)  
**Status**: Stubbed, needs completion  
**Estimate**: 1-2 weeks

**What**: Complete biome determination logic based on temperature, moisture, elevation.

**Why**: Enables climate-driven gameplay (vegetation growth, resource availability).

**Dependencies**: Climate grids exist  
**Blocks**: Climate-driven features

---

## Foundation Health Checklist

Before starting new features, verify:

- [ ] All systems have rewind guards (`RewindState.Mode` checks)
- [ ] All registries update spatial sync (`RegistrySpatialSyncSystem`)
- [ ] All systems use registry lookups (not ad-hoc queries)
- [ ] All AI systems use shared pipeline (sensors → utility → steering → commands)
- [ ] All authoring has validation (baker errors caught early)
- [ ] All systems are Burst-compatible (where possible)
- [ ] All systems respect `TimeState.TimeScale` (pause/playback)

## Quick Links

- **Full Orientation**: `Docs/ORIENTATION_SUMMARY.md`
- **AI Backlog**: `Docs/AI_Backlog.md`
- **Villager TODO**: `Docs/TODO/VillagerSystems_TODO.md`
- **Resources TODO**: `Docs/TODO/ResourcesFramework_TODO.md`
- **Space4X TODO**: `Docs/TODO/Space4X_Frameworks_TODO.md`
- **Technical Debt**: `Docs/TECHNICAL_DEBT.md`
- **Outstanding TODOs**: `Docs/OUTSTANDING_TODOS_SUMMARY.md`

