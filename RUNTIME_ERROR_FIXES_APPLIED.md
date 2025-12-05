# Runtime Error Fixes - Applied Status

## Summary

Based on error reports, here's the status of fixes:

### ✅ Verified Fixed
1. **Space4XAffiliationComplianceSystem** - Guard exists (lines 133-139), no Assert found
2. **VillagerTargetingSystem** - Already calls `.Update(ref state)` on BufferLookup (line 67)
3. **RegistryHealthSystem** - Already calls `.Update(ref state)` on BufferLookup (line 75)

### ⚠️ Files Not Found (Need to be located)
4. **Space4XMutinySystem.cs** - Needs `RequireForUpdate<RewindState>()`
5. **UpdateVesselMovementAIJob** - Needs `[ReadOnly]` on ComponentLookup fields
6. **UpdateStrikeCraftBehaviorJob** - Needs `[ReadOnly]` on ComponentLookup fields
7. **BufferTypeHandle<ResourceRegistryEntry>** - Error suggests a system using IJobChunk, but not found

---

## Fix Instructions (Apply when files are found)

### 1. Space4XMutinySystem - RewindState Gate

**File:** `Assets/Scripts/Space4x/Registry/Space4XMutinySystem.cs` (or similar path)

**Fix:**
```csharp
using PureDOTS.Runtime.Components;

public partial struct Space4XMutinySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PureDOTS.Runtime.Components.RewindState>();
        // keep any other RequireForUpdate calls you already had
    }

    public void OnUpdate(ref SystemState state)
    {
        // safety belt in case bootstrap changes later
        if (!SystemAPI.HasSingleton<RewindState>())
            return;

        var rewindState = SystemAPI.GetSingleton<RewindState>();
        // existing logic...
    }
}
```

**Quick disable (if not needed yet):**
```csharp
public void OnUpdate(ref SystemState state)
{
    return; // TEMP: disable mutiny until rewind is fully wired
}
```

---

### 2. UpdateVesselMovementAIJob - [ReadOnly] Fix

**File:** Find the file containing `UpdateVesselMovementAIJob` struct

**Fix in job struct:**
```csharp
[BurstCompile]
public struct UpdateVesselMovementAIJob : IJobParallelFor
{
    [ReadOnly] public ComponentLookup<VesselStance> StanceLookup;
    [ReadOnly] public ComponentLookup<Formation> FormationLookup;
    [ReadOnly] public ComponentLookup<MovementPolicy> MovementLookup;
    // ... other fields ...
}
```

**Fix in system OnUpdate:**
```csharp
public void OnUpdate(ref SystemState state)
{
    var stanceLookup = state.GetComponentLookup<VesselStance>(true);
    var formationLookup = state.GetComponentLookup<Formation>(true);
    var movementLookup = state.GetComponentLookup<MovementPolicy>(true);

    var job = new UpdateVesselMovementAIJob
    {
        StanceLookup = stanceLookup,
        FormationLookup = formationLookup,
        MovementLookup = movementLookup,
        // ... other fields ...
    };

    state.Dependency = job.ScheduleParallel(count, 64, state.Dependency);
}
```

**If lookups are stored as fields:**
```csharp
ComponentLookup<VesselStance> _stanceLookup;

public void OnCreate(ref SystemState state)
{
    _stanceLookup = state.GetComponentLookup<VesselStance>(true);
}

public void OnUpdate(ref SystemState state)
{
    _stanceLookup.Update(ref state); // IMPORTANT: update before use

    var job = new UpdateVesselMovementAIJob
    {
        StanceLookup = _stanceLookup,
        // ...
    };
}
```

---

### 3. UpdateStrikeCraftBehaviorJob - [ReadOnly] Fix

**File:** Find the file containing `UpdateStrikeCraftBehaviorJob` struct

**Fix in job struct:**
```csharp
[BurstCompile]
public struct UpdateStrikeCraftBehaviorJob : IJobParallelFor
{
    [ReadOnly] public ComponentLookup<StrikeCraft> StrikeCraftLookup;
    [ReadOnly] public ComponentLookup<Movement> MovementLookup;
    // ... other fields ...
}
```

**Fix in system OnUpdate:**
```csharp
var strikeCraftLookup = state.GetComponentLookup<StrikeCraft>(true);
var movementLookup = state.GetComponentLookup<Movement>(true);

var job = new UpdateStrikeCraftBehaviorJob
{
    StrikeCraftLookup = strikeCraftLookup,
    MovementLookup = movementLookup,
    // ...
};
```

---

### 4. BufferTypeHandle<ResourceRegistryEntry> - ObjectDisposedException

**Error:** `Attempted to access BufferTypeHandle<ResourceRegistryEntry> which has been invalidated by a structural change.`

**Status:** No system found using `BufferTypeHandle<ResourceRegistryEntry>`. Current systems use:
- `ResourceRegistrySystem` - Uses `GetBuffer` directly (line 72)
- `VillagerTargetingSystem` - Uses `BufferLookup` with `.Update(ref state)` (line 67)
- `RegistryHealthSystem` - Uses `BufferLookup` with `.Update(ref state)` (line 75)

**If found, fix:**
```csharp
public partial struct SomeSystem : ISystem
{
    BufferTypeHandle<ResourceRegistryEntry> _entryHandle;

    public void OnCreate(ref SystemState state)
    {
        _entryHandle = state.GetBufferTypeHandle<ResourceRegistryEntry>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        // refresh after structural changes
        _entryHandle.Update(ref state);

        var job = new SomeJob
        {
            EntryHandle = _entryHandle,
            // ...
        };

        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
}
```

**Or get fresh handle each frame:**
```csharp
public void OnUpdate(ref SystemState state)
{
    var entryHandle = state.GetBufferTypeHandle<ResourceRegistryEntry>(true);

    var job = new SomeJob
    {
        EntryHandle = entryHandle,
    };

    state.Dependency = job.ScheduleParallel(_query, state.Dependency);
}
```

**Important:** Ensure structural changes (ECB playback, Add/RemoveComponent) happen BEFORE scheduling the job, not after.

---

## Notes

- Files referenced in errors were not found in current codebase search
- Errors suggest these files exist in a different location or branch
- Apply fixes when files are located
- AffiliationComplianceSystem is already fixed (guard exists, no Assert found)
- BufferLookup systems already have proper `.Update(ref state)` calls






