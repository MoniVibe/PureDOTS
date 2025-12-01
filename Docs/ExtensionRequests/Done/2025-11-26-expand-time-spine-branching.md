# Extension Request: Expand Time Spine - Branching & What-If Simulation

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P1  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Time/Branching/BranchingComponents.cs` - TimelineBranch, WhatIfRequest, WhatIfModification, WhatIfResult, TimeSpineConfig, BranchSnapshot, BranchEntityDelta, BranchMergeRequest, MergeSelection, BranchComparison, BranchMembership
- `Packages/com.moni.puredots/Runtime/Runtime/Time/Branching/BranchingHelpers.cs` - Static helpers for branch management, divergence calculation, merge operations
- `Packages/com.moni.puredots/Runtime/Systems/Time/Branching/TimelineBranchSystem.cs` - TimelineBranchSystem, WhatIfSimulationSystem, BranchMergeSystem

---

## Use Case

The current time spine supports rewind/resim. Expand to support:

**Godgame:**
- "What-if" simulation: Preview miracle effects before committing
- Timeline branching: Save world states, explore alternate histories
- Prophecy system: Peek into probable futures

**Space4X:**
- Battle simulation: Preview fleet engagement outcomes
- Strategic planning: Test different fleet compositions
- Campaign branching: Explore alternate victory paths

---

## Proposed Expansion

### Current State
- `TimeState` singleton with tick tracking
- `RewindState` for replay
- Snapshot/command log for determinism

### Requested Expansion

```csharp
// Branch management
public struct TimelineBranch : IBufferElementData
{
    public uint BranchId;
    public uint ParentBranchId;
    public uint ForkTick;             // When this branch diverged
    public uint HeadTick;             // Latest tick in this branch
    public FixedString64Bytes Label;  // "Main", "What-If Miracle", "Battle Preview"
    public bool IsSimulation;         // True = temporary preview, don't persist
    public bool IsActive;             // Currently simulating this branch
}

public struct WhatIfRequest : IComponentData
{
    public uint SourceTick;           // Fork from this tick
    public uint SimDuration;          // How many ticks to simulate
    public Entity TriggerEntity;      // What to test (miracle, fleet order)
    public FixedString32Bytes Label;
}

public struct WhatIfResult : IComponentData
{
    public uint BranchId;
    public bool Completed;
    public float SuccessProbability;  // 0-1 based on multiple runs
    public uint CasualtyEstimate;     // Predicted losses
    public float ResourceDelta;       // Net resource change
}

// Time spine expansion
public struct TimeSpineConfig : IComponentData
{
    public uint MaxBranches;          // Limit concurrent branches
    public uint MaxSnapshotsPerBranch;
    public uint SimulationSpeedMultiplier; // Run previews faster
    public bool AllowParallelBranches;    // Multiple what-ifs at once
}
```

### New Systems
- `TimelineBranchSystem` - Creates/manages branches
- `WhatIfSimulationSystem` - Runs fast-forward previews
- `BranchMergeSystem` - Commits or discards preview results

---

## Example Usage

```csharp
// === Godgame: Preview miracle effect ===
EntityManager.AddComponentData(miracleEntity, new WhatIfRequest {
    SourceTick = currentTick,
    SimDuration = 300,  // 10 seconds preview
    TriggerEntity = miracleEntity,
    Label = "Rain Miracle Preview"
});

// After simulation completes
var result = EntityManager.GetComponentData<WhatIfResult>(miracleEntity);
if (result.SuccessProbability > 0.8f)
{
    // Good outcome - commit the miracle
    TimelineHelpers.CommitBranch(result.BranchId);
}
else
{
    // Bad outcome - discard
    TimelineHelpers.DiscardBranch(result.BranchId);
}

// === Space4X: Battle preview ===
EntityManager.AddComponentData(fleetEntity, new WhatIfRequest {
    SourceTick = currentTick,
    SimDuration = 1800,  // 60 second battle
    TriggerEntity = engagementEntity,
    Label = "Fleet Engagement"
});
```

---

## Impact Assessment

**Files/Systems Affected:**
- Expand: `Packages/com.moni.puredots/Runtime/Time/TimeComponents.cs`
- Expand: `Packages/com.moni.puredots/Runtime/Systems/Time/TimeControlSystem.cs`
- New: `TimelineBranchSystem.cs`, `WhatIfSimulationSystem.cs`

**Breaking Changes:** 
- Should be additive to existing time spine
- Existing rewind functionality preserved

---

## Review Notes

*(PureDOTS team use)*

