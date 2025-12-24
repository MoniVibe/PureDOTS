# Stub Types (Intentional/Legacy)

List of stub/compat placeholders currently kept to unblock compilation. Replace with real implementations or remove when upstream features land.

| File | Purpose | Status |
| --- | --- | --- |
| Packages/com.moni.puredots/Runtime/Systems/Input/SelectionSystemStub.cs | Placeholder for selection flow | Pending real selection pipeline |
| Packages/com.moni.puredots/Runtime/Systems/Combat/FleetCommandSystemStub.cs | Fleet command hook stub | Pending real fleet command systems |
| Packages/com.moni.puredots/Runtime/Systems/Spatial/SpatialBuildSystemStub.cs | Spatial build placeholder | Pending spatial build implementation |
| Packages/com.moni.puredots/Runtime/Systems/SystemGroups/SystemGroupStubs.cs | Group placeholders | Keep until groups are defined |
| Packages/com.moni.puredots/Runtime/Components/Events/EventStubs.cs | Event placeholder types | Replace with real event schema |
| Packages/com.moni.puredots/Runtime/Components/Perception/PerceptionStubs.cs | Perception placeholder types | Replace with perception data/components |
| Packages/com.moni.puredots/Runtime/Components/Combat/CombatStateStubs.cs | Combat state placeholders | Replace with combat state components |
| Packages/com.moni.puredots/Runtime/Operations/ConceptStubComponents.cs | Operations concept stubs | Replace with real operations data |
| Packages/com.moni.puredots/Runtime/Logistics/Components/LogisticsStubComponents.cs | Logistics placeholder data | Replace with logistics components |
| Packages/com.moni.puredots/Runtime/Runtime/Systems/CoreStubSystems.cs | Core system placeholders | Remove when real systems exist |
| Packages/com.moni.puredots/Runtime/Runtime/Time/TimeMultiplayerStubs.cs | Multiplayer time placeholder | Replace with multiplayer time impl |
| Packages/com.moni.puredots/Runtime/Physics/CameraPhysicsStubs.cs | Camera physics placeholder | ✅ REMOVED (2025-12-XX) |
| Packages/com.moni.puredots/Runtime/Runtime/MetaRegistryStubs.cs | Meta registry placeholder | Replace when registry finalized |
| Packages/com.moni.puredots/Runtime/Systems/MetaRegistryStubSystems.cs | Meta registry systems placeholder | Remove when registry finalized |
# Stub Types Tracking

**Last Updated**: 2025-12-10
**Purpose**: Track temporary stub implementations that must be replaced with real types

---

## Stub Type Policy

### Canonical Rule
> If you add a stub in the canonical namespace, mark it `// STUB: REMOVE when real <Type>` and track it here. Delete the stub as soon as the real implementation lands. Never ship stubs alongside real types; before closing work, `grep -r "STUB:"` in `com.moni.puredots` and remove or `#if false` any leftovers.

### Why This Matters
- **Compilation Blocking**: Stubs mask missing dependencies and hide integration errors
- **Code Quality**: Real implementations follow DOTS patterns; stubs often violate them
- **Maintenance Burden**: Stubs accumulate technical debt and confuse future developers

## Current Stub Types

### None Currently Tracked
All stub types have been resolved. This section will be populated when new stubs are added.

## Historical Stubs (Resolved)

### Example Format (When Adding New Stubs)
```csharp
// File: Runtime/Systems/AI/StubReasoningSystem.cs
// Status: ACTIVE - Replace with GOAP implementation
// Added: 2025-12-01
// Owner: @agent_name
// Depends On: RealGoalPlanningSystem
// Notes: Basic if/else logic, no learning. Replace with utility-based planning.
public partial struct StubReasoningSystem : ISystem
{
    // STUB: REMOVE when real ReasoningSystem implemented
}
```

## Stub Cleanup Checklist

### Before Committing
- [ ] Run `grep -r "STUB:" --include="*.cs"` in `Packages/com.moni.puredots/`
- [ ] Verify no active stubs in production code
- [ ] Check that stub removal doesn't break compilation
- [ ] Update this tracking document

### During Code Review
- [ ] Reject PRs containing new stubs without tracking
- [ ] Verify stub dependencies are documented
- [ ] Ensure timeline for stub replacement is clear

## Common Stub Patterns

### Data Structure Stubs
```csharp
// ❌ Stub pattern
public struct StubAgentState : IComponentData
{
    public int PlaceholderField; // STUB: REMOVE when real agent state defined
}

// ✅ Real implementation
public struct AgentState : IComponentData
{
    public GoalType CurrentGoal;
    public UtilityScore GoalUtility;
    public DecisionContext Context;
}
```

### System Logic Stubs
```csharp
// ❌ Stub pattern
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // STUB: REMOVE when real decision logic implemented
    foreach (var entity in query) {
        // Dummy logic here
    }
}

// ✅ Real implementation
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var utility = SystemAPI.GetSingleton<UtilityCalculator>();
    foreach (var (entity, goals) in query) {
        var bestGoal = utility.Evaluate(goals);
        // Real decision logic
    }
}
```

## Stub Prevention Strategies

### 1. Contract-First Development
- Define component interfaces before implementation
- Use abstract base classes for system contracts
- Implement minimal viable types, not stubs

### 2. Parallel Development Coordination
- Shared types created first in dependency order
- Flag missing dependencies immediately
- Use integration tests to catch missing implementations

### 3. Incremental Implementation
- Implement core data structures completely
- Add system logic in layers (data → basic logic → advanced features)
- Never commit partial implementations as stubs

## Emergency Stub Protocol

### When Stubs Are Necessary
Rare cases where blocking work requires temporary stubs:

1. **Document the stub** with clear removal criteria
2. **Add to this tracking file** with owner and timeline
3. **Mark with distinctive comments** for easy grep finding
4. **Set removal deadline** (max 1 sprint)
5. **Create follow-up task** for implementation

### Emergency Stub Template
```csharp
// EMERGENCY STUB - REMOVE BY [DATE]
// Reason: Blocking parallel work on [FEATURE]
// Owner: @agent_name
// Replace with: [REAL_TYPE_NAME]
// Tracking: PureDOTS/Docs/StubTypes.md
public struct EmergencyStubType : IComponentData
{
    // Minimal fields to unblock compilation
}
```

## Metrics and Monitoring

### Stub Health Dashboard
- **Active Stubs**: `grep -r "STUB:" --include="*.cs" | wc -l`
- **Stub Files**: `grep -l "STUB:" --include="*.cs" | sort`
- **Oldest Stub**: Track by creation date
- **Stub Ownership**: Ensure all stubs have owners

### Weekly Review
- Audit all active stubs
- Escalate overdue removals
- Update timelines based on progress

## Success Criteria

### Zero Stub Policy
- **Target**: 0 active stubs in main branch
- **Definition**: No `STUB:` comments in production code
- **Exception**: Emergency stubs with 24-hour removal plans

### Quality Gates
- **CI Check**: Fail builds with active stubs
- **Code Review**: Reject PRs with undocumented stubs
- **Release**: No stubs in release branches

---

**Related Documents**:
- `FoundationGuidelines.md` - Coding standards
- `TRI_PROJECT_BRIEFING.md` - Project coordination rules
- `Docs/Progress.md` - Implementation status tracking



