# Boundary Violation Migration Plan

**Last Updated**: 2025-12-01
**Purpose**: Step-by-step fixes for boundary violations identified in [BOUNDARY_CLASSIFICATION.md](BOUNDARY_CLASSIFICATION.md)

---

## Migration Strategy

**Approach**: Phased migration to minimize breaking changes.

**Priority Order**:
1. **Low Impact**: Single field renames, isolated components
2. **Medium Impact**: Component moves with clear dependencies
3. **High Impact**: Core component renames affecting many systems

---

## Phase 1: Low Impact Fixes

### Fix 1: Rename `ResourceActiveTicket.Villager` Field

**Violation**: `Packages/com.moni.puredots/Runtime/Runtime/ResourceComponents.cs` line 395

**Current Code**:
```csharp
public struct ResourceActiveTicket : IBufferElementData
{
    public Entity Villager;  // ❌ Game-specific term
    public uint TicketId;
    public float ReservedUnits;
}
```

**Target Code**:
```csharp
public struct ResourceActiveTicket : IBufferElementData
{
    public Entity Worker;  // ✅ Generic term
    public uint TicketId;
    public float ReservedUnits;
}
```

**Steps**:
1. Rename field `Villager` → `Worker` in `ResourceComponents.cs`
2. Find all usages: `grep -r "ResourceActiveTicket" --include="*.cs"`
3. Update all references: `ticket.Villager` → `ticket.Worker`
4. Build and test

**Breaking Changes**: None (field rename only affects internal usage)

**Estimated Time**: 30 minutes

---

## Phase 2: Medium Impact - Move Godgame-Specific Components

### Fix 2: Move Miracle Components to Godgame

**Violation**: `Packages/com.moni.puredots/Runtime/Runtime/MiracleComponents.cs`

**Current Location**: `Packages/com.moni.puredots/Runtime/Runtime/MiracleComponents.cs`

**Target Location**: `Assets/Projects/Godgame/Scripts/Godgame/Runtime/MiracleComponents.cs`

**Steps**:
1. **Create target directory**:
   ```bash
   mkdir -p Assets/Projects/Godgame/Scripts/Godgame/Runtime
   ```

2. **Move file**:
   ```bash
   mv Packages/com.moni.puredots/Runtime/Runtime/MiracleComponents.cs \
      Assets/Projects/Godgame/Scripts/Godgame/Runtime/MiracleComponents.cs
   ```

3. **Update namespace**:
   ```csharp
   // Change from:
   namespace PureDOTS.Runtime.Components
   
   // To:
   namespace Godgame.Runtime
   ```

4. **Find all usages**:
   ```bash
   grep -r "PureDOTS.Runtime.Components.Miracle" --include="*.cs"
   grep -r "using PureDOTS.Runtime.Components" --include="*.cs" | grep -i miracle
   ```

5. **Update references**:
   - PureDOTS code: Remove Miracle references (shouldn't exist)
   - Godgame code: Update to `using Godgame.Runtime;`
   - Space4X code: Remove if present (shouldn't exist)

6. **Update `.asmdef` files**:
   - Ensure Godgame assembly references PureDOTS
   - Ensure PureDOTS assembly does NOT reference Godgame

7. **Build and test**:
   - Verify PureDOTS builds without Miracle types
   - Verify Godgame builds with Miracle types in new location
   - Run Godgame tests

**Breaking Changes**: 
- Godgame code needs namespace updates
- Any PureDOTS code referencing Miracle types will break (shouldn't exist)

**Estimated Time**: 2-3 hours

---

### Fix 3: Move Divine Hand Components to Godgame

**Violation**: `Packages/com.moni.puredots/Runtime/Runtime/DivineHandComponents.cs`

**Current Location**: `Packages/com.moni.puredots/Runtime/Runtime/DivineHandComponents.cs`

**Target Location**: `Assets/Projects/Godgame/Scripts/Godgame/Runtime/DivineHandComponents.cs`

**Steps**:
1. **Move file**:
   ```bash
   mv Packages/com.moni.puredots/Runtime/Runtime/DivineHandComponents.cs \
      Assets/Projects/Godgame/Scripts/Godgame/Runtime/DivineHandComponents.cs
   ```

2. **Update namespace**:
   ```csharp
   // Change from:
   namespace PureDOTS.Runtime.Components
   
   // To:
   namespace Godgame.Runtime
   ```

3. **Find all usages**:
   ```bash
   grep -r "DivineHand" --include="*.cs"
   ```

4. **Update references**:
   - PureDOTS code: Remove DivineHand references
   - Godgame code: Update to `using Godgame.Runtime;`

5. **Build and test**

**Breaking Changes**: 
- Godgame code needs namespace updates
- PureDOTS code referencing DivineHand will break (shouldn't exist)

**Estimated Time**: 1-2 hours

---

## Phase 3: High Impact - Rename Villager Components

### Fix 4: Rename Villager Components to Generic Terms

**Violation**: `Packages/com.moni.puredots/Runtime/Runtime/VillagerComponents.cs`

**Strategy**: Rename to generic "Actor" or "Entity" terminology.

**Option A: Rename to "Actor"** (Recommended)
- `VillagerId` → `ActorId`
- `VillagerNeeds` → `ActorNeeds`
- `VillagerAttributes` → `ActorAttributes`
- `VillagerJob` → `ActorJob`
- `VillagerMovement` → `ActorMovement`
- `VillagerRegistry` → `ActorRegistry`
- etc.

**Option B: Rename to "Entity"**
- `VillagerId` → `EntityId`
- `VillagerNeeds` → `EntityNeeds`
- etc.

**Option C: Move to Godgame** (Alternative)
- Move entire `VillagerComponents.cs` to Godgame
- Rename to `Godgame.Villager` components

**Recommended Approach**: **Option A** (Rename to "Actor")

**Steps** (Option A):

1. **Create migration script**:
   ```bash
   # Find all Villager component usages
   grep -r "Villager" --include="*.cs" | grep -v "Godgame" | grep -v "Space4X"
   ```

2. **Rename file**:
   ```bash
   mv Packages/com.moni.puredots/Runtime/Runtime/VillagerComponents.cs \
      Packages/com.moni.puredots/Runtime/Runtime/ActorComponents.cs
   ```

3. **Update component names** (in `ActorComponents.cs`):
   - `VillagerId` → `ActorId`
   - `VillagerNeeds` → `ActorNeeds`
   - `VillagerAttributes` → `ActorAttributes`
   - `VillagerBelief` → `ActorBelief`
   - `VillagerReputation` → `ActorReputation`
   - `VillagerJob` → `ActorJob`
   - `VillagerMovement` → `ActorMovement`
   - `VillagerCombatStats` → `ActorCombatStats`
   - `VillagerRegistry` → `ActorRegistry`
   - `VillagerRegistryEntry` → `ActorRegistryEntry`
   - etc.

4. **Update all usages** (automated find/replace):
   ```bash
   # In PureDOTS codebase
   find Packages/com.moni.puredots -name "*.cs" -exec sed -i 's/VillagerId/ActorId/g' {} \;
   find Packages/com.moni.puredots -name "*.cs" -exec sed -i 's/VillagerNeeds/ActorNeeds/g' {} \;
   # ... repeat for each component
   ```

5. **Update Godgame code**:
   - Godgame can create `Godgame.Villager` wrapper components if needed
   - Or use `Actor` components directly (preferred)

6. **Update documentation**:
   - Update all docs referencing "Villager" to "Actor"
   - Update `VillagerDecisionMaking.md` → `ActorDecisionMaking.md`

7. **Build and test**:
   - Full rebuild of PureDOTS
   - Full rebuild of Godgame
   - Run all tests

**Breaking Changes**: 
- All PureDOTS code using Villager components breaks
- All Godgame code using Villager components breaks
- Requires comprehensive update

**Estimated Time**: 4-6 hours

**Alternative (Option C - Move to Godgame)**:

If renaming is too disruptive, move to Godgame:

1. **Move file**:
   ```bash
   mv Packages/com.moni.puredots/Runtime/Runtime/VillagerComponents.cs \
      Assets/Projects/Godgame/Scripts/Godgame/Runtime/VillagerComponents.cs
   ```

2. **Update namespace**:
   ```csharp
   namespace Godgame.Runtime
   ```

3. **Update all usages**:
   - PureDOTS: Remove Villager references (create generic alternatives)
   - Godgame: Update to `using Godgame.Runtime;`

**Estimated Time**: 3-4 hours

---

## Migration Checklist

### Pre-Migration

- [ ] Backup current codebase
- [ ] Create feature branch: `boundary-cleanup`
- [ ] Review all violations in [BOUNDARY_CLASSIFICATION.md](BOUNDARY_CLASSIFICATION.md)
- [ ] Notify team of breaking changes

### Phase 1: Low Impact

- [ ] Fix 1: Rename `ResourceActiveTicket.Villager` → `Worker`
- [ ] Build and test PureDOTS
- [ ] Commit: `fix: rename ResourceActiveTicket.Villager to Worker`

### Phase 2: Medium Impact

- [ ] Fix 2: Move Miracle components to Godgame
- [ ] Fix 3: Move Divine Hand components to Godgame
- [ ] Build and test PureDOTS (should build without errors)
- [ ] Build and test Godgame (should build with new namespaces)
- [ ] Commit: `refactor: move Miracle and DivineHand components to Godgame`

### Phase 3: High Impact

- [ ] Fix 4: Rename Villager components (choose Option A, B, or C)
- [ ] Update all PureDOTS code
- [ ] Update all Godgame code
- [ ] Update documentation
- [ ] Full rebuild and test
- [ ] Commit: `refactor: rename Villager components to Actor`

### Post-Migration

- [ ] Update [BOUNDARY_CLASSIFICATION.md](BOUNDARY_CLASSIFICATION.md) (mark violations as fixed)
- [ ] Update [BOUNDARY_CONTRACT.md](BOUNDARY_CONTRACT.md) if needed
- [ ] Create PR and review
- [ ] Merge after approval

---

## Risk Mitigation

### Breaking Changes

**Mitigation**:
- Use feature branch for all changes
- Incremental commits per fix
- Comprehensive testing after each phase
- Team review before merging

### Dependency Issues

**Mitigation**:
- Verify `.asmdef` files don't create circular dependencies
- Ensure PureDOTS doesn't reference game assemblies
- Test build in isolation (PureDOTS only)

### Test Coverage

**Mitigation**:
- Run all PureDOTS tests after each fix
- Run all Godgame tests after each fix
- Run all Space4X tests (should be unaffected)
- Manual testing of affected systems

---

## Rollback Plan

If migration causes critical issues:

1. **Revert commits**:
   ```bash
   git revert <commit-hash>
   ```

2. **Restore from backup** if needed

3. **Document issues** for future attempt

---

## Success Criteria

Migration is complete when:

- ✅ All violations in [BOUNDARY_CLASSIFICATION.md](BOUNDARY_CLASSIFICATION.md) are marked as fixed
- ✅ PureDOTS builds without errors
- ✅ PureDOTS has no references to game-specific types
- ✅ Game projects build successfully
- ✅ All tests pass
- ✅ Documentation updated

---

## Related Documents

- [BOUNDARY_CLASSIFICATION.md](BOUNDARY_CLASSIFICATION.md) - Complete violation catalog
- [BOUNDARY_CONTRACT.md](BOUNDARY_CONTRACT.md) - Rules and examples

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

