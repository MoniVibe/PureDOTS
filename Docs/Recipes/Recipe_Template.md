# Feature Implementation Recipe Template

**Category:** `cross-project-mechanic` | `puredots-infra-system` | `game-local-feature` | `ai-behavior-or-tree`  
**Feature Name:** [Brief name]  
**Contracts Touched:** [List contract names or "None if PureDOTS-only"]  
**Determinism:** [Required | Optional | Not needed]

---

## Context & Intent

**What problem does this solve?**  
[1-2 sentences]

**Which games/projects are affected?**  
- PureDOTS: [Yes/No - what changes]
- Godgame: [Yes/No - what adapters/authoring]
- Space4x: [Yes/No - what adapters/authoring]

**Skip sections that don't apply:**  
- [ ] No contracts needed (PureDOTS-only infra)
- [ ] No game adapters (PureDOTS-only)
- [ ] No ScenarioRunner test (not deterministic or too simple)
- [ ] No authoring (runtime-only systems)

---

## Step 1: Extract Shared Invariants

**Questions to answer:**
- What data must be shared? (entities, positions, states, events)
- What must be **rewind-safe**? (queues, scheduled actions, entity state)
- What is **pure presentation**? (VFX, audio, UI - stays in game project)
- What are the **producer/consumer** roles?

**Universal invariants:**
- [List what's shared across games]

**Game-specific variations:**
- Godgame: [What differs]
- Space4x: [What differs]

---

## Step 2: Define Contracts (if applicable)

**Skip if:** PureDOTS-only infrastructure that doesn't expose shared data structures.

**Add entries to `PureDOTS/Docs/Contracts.md`:**

```markdown
## ContractName v1

- Producer: [Who writes this]
- Consumer: [Who reads this]
- Schema:
  - Field1 (type) - description
  - Field2 (type) - description
- Notes: Burst-safe, rewind-safe, no strings, etc.
```

**Contracts to add:**
- [List each contract with brief description]

---

## Step 3: Implement PureDOTS Spine

### 3.1 Components & Buffers

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/<Feature>/`

**Components to create:**
- `[Feature]Config` - Configuration (set at bake time)
- `[Feature]State` - Runtime state
- `[Feature]Tag` - Tag component (if needed)

**Buffers to create:**
- `[Feature]Request` - Incoming requests (if adapters write to it)
- `[Feature]Entry` - Internal queue/registry entries (if needed)

**Code structure:**
```csharp
namespace PureDOTS.Runtime.<Feature>
{
    public struct <Feature>Config : IComponentData { ... }
    public struct <Feature>State : IComponentData { ... }
    public struct <Feature>Tag : IComponentData { }
    
    [InternalBufferCapacity(4)]
    public struct <Feature>Request : IBufferElementData { ... }
}
```

### 3.2 Systems

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/<Feature>/`

**Systems to create:**
- `[Feature]IntakeSystem` - Reads requests, validates, processes (if needed)
- `[Feature]ExecutionSystem` - Main logic/processing
- `[Feature]CleanupSystem` - Cleanup/compaction (if needed)

**System pattern:**
```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct <Feature>System : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return; // Only process in Record mode if deterministic
        
        // ... implementation ...
    }
}
```

### 3.3 Authoring (if applicable)

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/`

**Base authoring component:**
```csharp
public class <Feature>Authoring : MonoBehaviour
{
    // Configurable fields
    
    public class Baker : Baker<<Feature>Authoring>
    {
        public override void Bake(...)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<<Feature>Tag>(entity);
            AddComponent(entity, new <Feature>Config { ... });
            AddComponent(entity, new <Feature>State());
            // Add buffers if needed
        }
    }
}
```

**See also:** `[Recipe_AuthoringAndPrefabs.md](Recipe_AuthoringAndPrefabs.md)` for detailed authoring patterns, prefab setup, and SubScene organization.

---

## Step 4: Wire Game Adapters (if applicable)

**Skip if:** PureDOTS-only infrastructure.

### 4.1 Game-Specific Authoring

**Godgame:** `Godgame/Assets/Scripts/Godgame/Authoring/`

**Space4x:** `Space4x/Assets/Scripts/Space4x/Authoring/`

**Pattern:** Extend base authoring with game-specific config/tags.

### 4.2 Input/Command Adapters

**Location:** `<Game>/Assets/Scripts/<Game>/Adapters/<Feature>/`

**Pattern:**
1. Read game-specific input/AI commands
2. Translate to PureDOTS contract (buffer/component)
3. Write to PureDOTS data structure

### 4.3 Event/Effect Adapters

**Location:** `<Game>/Assets/Scripts/<Game>/Adapters/<Feature>/`

**Pattern:**
1. Query PureDOTS events/data
2. Translate to game-specific effects
3. Apply game-local behavior

---

## Step 5: Determinism & ScenarioRunner (if applicable)

**Skip if:** Not deterministic or too simple to warrant scenario testing.

**Scenario location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/`

**Test location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Tests/<Feature>/`

**Scenario structure:**
```json
{
  "scenarioId": "scenario.puredots.<feature>_demo",
  "seed": 12345,
  "runTicks": 300,
  "entityCounts": [...],
  "inputCommands": [...]
}
```

---

## Step 6: Integration & Testing Notes

**Files created/modified:**
- Contracts: `PureDOTS/Docs/Contracts.md`
- Components: `PureDOTS/.../Runtime/Runtime/<Feature>/`
- Systems: `PureDOTS/.../Runtime/Systems/<Feature>/`
- Authoring: `PureDOTS/.../Runtime/Authoring/` + game authoring
- Adapters: `<Game>/Assets/Scripts/<Game>/Adapters/<Feature>/`
- Scenarios: `PureDOTS/.../Runtime/Runtime/Scenarios/Samples/`
- Tests: `PureDOTS/.../Runtime/Tests/<Feature>/`

**Integration points:**
- [List any registries, providers, or other systems this integrates with]

**Testing checklist:**
- [ ] Contracts match implementation
- [ ] Systems compile and run
- [ ] Adapters wire correctly
- [ ] ScenarioRunner test passes (if applicable)
- [ ] Rewind safety verified (if deterministic)

---

## Quick Reference

**The Loop:**
1. Concept → Extract Invariants
2. Add/Update Contracts.md (if shared data)
3. Implement PureDOTS Spine
4. Wire Game Adapters (if cross-project)
5. (Optional) Add ScenarioRunner Test

**When to skip sections:**
- PureDOTS-only infra → Skip adapters, maybe contracts
- Game-local feature → Skip PureDOTS spine, use adapters only
- Simple tweak → One-paragraph sketch is enough

