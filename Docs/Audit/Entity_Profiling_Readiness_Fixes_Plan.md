# Entity Profiling Readiness Fixes Plan

## Status: 🟡 PLANNING - Ready for Implementation

## Overview
Address critical blockers preventing Entity Profiling System from being test-ready. Fixes archetype assignment, data flow gaps, opt-out mechanism, and adds comprehensive testing.

## Critical Issues to Fix

### 1. Archetype Assignment Not Guaranteed

**Problem**: Bootstrap creates `EntityProfile` with empty `ArchetypeName`, then `ArchetypeResolutionSystem` sets "Default" only if `VillagerId` exists. This creates a timing dependency and may fail if systems run out of order.

**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/Identity/EntityProfilingSystem.cs` (lines 38-70, 138-157)

**Fix**:
- Update `EntityProfilingBootstrapSystem` to set archetype name directly when creating `EntityProfile` for Godgame entities
- Use "Default" archetype name immediately in bootstrap for `VillagerId` entities
- Keep `ArchetypeResolutionSystem` logic as fallback for entities that somehow get through without archetype
- Ensure `CreatedTick` is always set when bootstrap creates `EntityProfile` (already done, verify)

**Implementation**:
```csharp
// In EntityProfilingBootstrapSystem, for VillagerId entities:
ecb.AddComponent(entity, new EntityProfile
{
    ArchetypeName = new FixedString64Bytes("Default"), // Set immediately
    Source = EntityProfileSource.Generated,
    CreatedTick = timeState.Tick,
    IsResolved = 0
});
```

### 2. Space4X Officer Stats Not Populated

**Problem**: `CreateIndividual` accepts `IndividualProfileData` but never populates the new `Command`, `Tactics`, etc. fields. Callers must manually set these fields, but there's no API to do so.

**Files**:
- `Packages/com.moni.puredots/Runtime/Identity/EntityProfilingService.cs` (lines 155-223)
- `Packages/com.moni.puredots/Runtime/Identity/EntityProfilingComponents.cs` (lines 85-117)

**Fix**:
- Add overloaded `CreateIndividual` method that accepts officer stats as parameters
- Or: Add helper method `CreateIndividualWithOfficerStats` that takes full profile
- Update `CreateIndividual` to accept optional `Space4XIndividualStats` parameter
- Ensure `OfficerStatsApplicationSystem` uses profile values (already done, verify default logic)

**Implementation**:
```csharp
public static void CreateIndividual(
    ref EntityManager entityManager, 
    Entity entity, 
    IndividualProfileData profile, 
    FixedString64Bytes archetypeName = default, 
    uint createdTick = 0,
    Space4XIndividualStats? officerStats = null)
{
    // ... existing code ...
    
    // If officer stats provided, populate profile
    if (officerStats.HasValue)
    {
        var stats = officerStats.Value;
        profile.Command = stats.Command;
        profile.Tactics = stats.Tactics;
        profile.Logistics = stats.Logistics;
        profile.Diplomacy = stats.Diplomacy;
        profile.Engineering = stats.Engineering;
        profile.Resolve = stats.Resolve;
    }
    
    // Store updated profile
    entityManager.SetComponentData(entity, profile);
}
```

### 3. Expertise and ServiceTrait Buffers Always Empty

**Problem**: `OfficerStatsApplicationSystem` creates empty buffers but never populates them. Plan called for seeding from profile data, but no fields exist in `IndividualProfileData`.

**Files**:
- `Packages/com.moni.puredots/Runtime/Identity/EntityProfilingComponents.cs` (lines 85-117)
- `Packages/com.moni.puredots/Runtime/Systems/Identity/EntityProfilingSystem.cs` (lines 710-720)

**Fix**:
- Add `FixedList64Bytes<FixedString32Bytes>` fields to `IndividualProfileData` for initial expertise types and service traits
- Or: Use `BlobArray` approach if fixed-size arrays are problematic
- Update `OfficerStatsApplicationSystem` to populate buffers from profile data
- Add helper method to convert string arrays to buffer entries

**Implementation**:
```csharp
// In IndividualProfileData:
public FixedList32Bytes<FixedString32Bytes> InitialExpertiseTypes; // Max 5 entries
public FixedList32Bytes<FixedString32Bytes> InitialServiceTraits; // Max 5 entries

// In OfficerStatsApplicationSystem:
if (profile.InitialExpertiseTypes.Length > 0)
{
    var expertiseBuffer = ecb.SetBuffer<ExpertiseEntry>(entity);
    for (int i = 0; i < profile.InitialExpertiseTypes.Length; i++)
    {
        // Parse expertise type and add with default tier
        expertiseBuffer.Add(new ExpertiseEntry 
        { 
            Type = profile.InitialExpertiseTypes[i], 
            Tier = 1 
        });
    }
}
```

**Alternative**: If `FixedList` is not available, use separate `half` fields for expertise count and `FixedString32Bytes` fields for each entry (max 5).

### 4. Hybrid Bootstrap Overwrites Manually Created Entities

**Problem**: Bootstrap checks for 6 components to skip profiling, but authored entities may have different component sets. Entities with custom stats but missing `DerivedAttributes` or `SocialStats` will be re-profiled, causing data conflicts.

**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/Identity/EntityProfilingSystem.cs` (lines 38-107)

**Fix**:
- Add opt-out component `SkipEntityProfiling : IComponentData` (empty struct)
- Update bootstrap to check for `SkipEntityProfiling` before creating `EntityProfile`
- Document that bakers should add `SkipEntityProfiling` if they manually set all profiling components
- Alternative: Check for `ProfileApplicationState.Phase == Complete` as opt-out signal

**Implementation**:
```csharp
// New component in EntityProfilingComponents.cs:
public struct SkipEntityProfiling : IComponentData { }

// In EntityProfilingBootstrapSystem:
foreach (var (villagerId, entity) in SystemAPI.Query<RefRO<VillagerId>>()
    .WithNone<EntityProfile, SkipEntityProfiling>() // Add opt-out check
    .WithEntityAccess())
{
    // ... existing bootstrap logic ...
}
```

### 5. CreatedTick May Be Stale

**Problem**: Bootstrap sets `CreatedTick` when creating `EntityProfile`, but if `EntityProfile` already exists (from manual creation), bootstrap doesn't update it. This could cause issues with tick-based calculations.

**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/Identity/EntityProfilingSystem.cs` (lines 38-107)

**Fix**:
- Bootstrap should update `CreatedTick` if `EntityProfile` exists but `CreatedTick == 0`
- Or: Only update if `CreatedTick == 0` to preserve manually set ticks
- Add comment explaining tick assignment strategy

**Implementation**:
```csharp
// In bootstrap, after checking for EntityProfile existence:
if (state.EntityManager.HasComponent<EntityProfile>(entity))
{
    var existingProfile = state.EntityManager.GetComponentData<EntityProfile>(entity);
    if (existingProfile.CreatedTick == 0)
    {
        existingProfile.CreatedTick = timeState.Tick;
        ecb.SetComponent(entity, existingProfile);
    }
    continue; // Skip creating new profile
}
```

### 6. Testing Infrastructure Missing

**Problem**: No Unity tests exist to verify profiling flow. Need edit-mode and playmode tests to ensure entities complete profiling phases.

**Files**:
- `Packages/com.moni.puredots/Runtime/Tests/` (new file)

**Fix**:
- Create `EntityProfilingServiceTests.cs` for edit-mode service API tests
- Create `EntityProfilingSystemTests.cs` for playmode integration tests
- Test cases:
  1. `CreateVillager` with archetype → verify `VillagerArchetypeAssignment` created
  2. Bootstrap creates `EntityProfile` for `VillagerId` entity → verify profiling phases complete
  3. `CreateIndividual` with officer stats → verify `Space4XIndividualStats` matches profile
  4. `SkipEntityProfiling` opt-out → verify bootstrap skips entity
  5. Profile completion check → verify `ProfileApplicationState.Phase == Complete`

**Implementation**:
```csharp
// Edit-mode test example:
[Test]
public void CreateVillager_SetsArchetypeAndTick()
{
    var world = new World("TestWorld");
    var entityManager = world.EntityManager;
    var entity = entityManager.CreateEntity();
    
    var profile = new VillagerProfileData 
    { 
        BasePhysique = 50f, 
        BaseFinesse = 50f, 
        BaseWill = 50f 
    };
    
    EntityProfilingService.CreateVillager(
        ref entityManager, 
        entity, 
        profile, 
        new FixedString64Bytes("Default"), 
        100u);
    
    Assert.IsTrue(entityManager.HasComponent<EntityProfile>(entity));
    var ep = entityManager.GetComponentData<EntityProfile>(entity);
    Assert.AreEqual("Default", ep.ArchetypeName.ToString());
    Assert.AreEqual(100u, ep.CreatedTick);
    
    world.Dispose();
}
```

## Implementation Order

1. **Phase 1 - Opt-Out Mechanism**: Add `SkipEntityProfiling` component and update bootstrap (prevents regressions)
2. **Phase 2 - Archetype Assignment**: Fix bootstrap to set archetype name immediately
3. **Phase 3 - Space4X Data Flow**: Extend `CreateIndividual` API and populate officer stats
4. **Phase 4 - Expertise/Traits**: Add fields to `IndividualProfileData` and populate buffers
5. **Phase 5 - CreatedTick Safety**: Update bootstrap to handle existing `EntityProfile`
6. **Phase 6 - Testing**: Create test suite and verify all fixes

## Testing Requirements

### Unit Tests (Edit Mode)
- Service API parameter validation
- Component existence checks
- Tick assignment logic
- Opt-out component behavior

### Integration Tests (Play Mode)
- Bootstrap system creates profiles for `VillagerId` entities
- All profiling phases complete in correct order
- `ProfileApplicationState.Phase == Complete` after all phases
- Space4X entities get officer stats from profile
- Expertise/trait buffers populated correctly
- Opt-out prevents profiling

### Regression Tests
- Existing `VillagerAuthoring` entities still work
- Entities with `SkipEntityProfiling` are not modified
- Manually created entities with full component sets complete profiling

## Dependencies

- `TimeState` singleton must exist
- `VillagerArchetypeCatalogComponent` singleton for archetype resolution
- `FixedList` or alternative fixed-size array type for expertise/traits
- Unity Test Framework for test execution

## Success Criteria

- All entities with `VillagerId` or `SimIndividualTag` get profiled automatically
- `ProfileApplicationState.Phase == Complete` for all profiled entities
- Space4X officer stats match profile data (not hardcoded 50)
- Expertise/trait buffers populated from profile when provided
- Opt-out mechanism prevents unwanted profiling
- All tests pass in Unity Test Runner

## Notes

- `CreatedTick` assignment in bootstrap is already correct (lines 59, 93), but verify it handles existing profiles
- `ArchetypeResolutionSystem` already sets "Default" for `VillagerId` entities (line 145), but bootstrap should do it earlier
- `OfficerStatsApplicationSystem` already reads from profile (line 685), but defaults to 50 if profile values are 0 - verify this is correct behavior
- Consider adding `[BurstCompile]` compatibility checks for `FixedList` usage in profile data

