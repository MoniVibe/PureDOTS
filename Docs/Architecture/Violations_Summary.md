# Architecture Violations Summary

## Critical Violations: Game-Specific Code in PureDOTS Package

### 1. Transport Components (CRITICAL)

**Files:**
- `Packages/com.moni.puredots/Runtime/Runtime/Transport/TransportComponents.cs`

**Violations:**
- `MinerVessel` component - Game-specific concept
- `Hauler` component - Game-specific concept  
- `Freighter` component - Game-specific concept
- `Wagon` component - Game-specific concept
- `MinerVesselRegistry` - Registry for game-specific component
- `HaulerRegistry` - Registry for game-specific component
- `FreighterRegistry` - Registry for game-specific component
- `WagonRegistry` - Registry for game-specific component

**Systems Affected:**
- `TransportRegistrySystem` - Queries for game-specific components
- `CoreSingletonBootstrapSystem` - Creates registries for game-specific components

**Registry Kind Enum:**
- `Packages/com.moni.puredots/Runtime/Runtime/Registry/RegistryUtilities.cs`
  - `RegistryKind.MinerVessel`
  - `RegistryKind.Hauler`
  - `RegistryKind.Freighter`
  - `RegistryKind.Wagon`

**Impact:**
- PureDOTS package cannot be reused for other games
- Game-specific logic leaks into framework code
- Violates separation of concerns

### 2. Villager Components (QUESTIONABLE)

**Status:** Needs Review

**Files:**
- `Packages/com.moni.puredots/Runtime/Runtime/Components/Villager*.cs`

**Consideration:**
- "Villager" might be considered generic enough for a framework
- However, if Space4X is the only game using it, it should be moved
- Check if other planned games need villagers

**Recommendation:**
- If villager concept is game-specific → Move to Space4X
- If villager concept is framework-level → Keep in PureDOTS but document as framework concept

## Migration Plan

### Phase 1: Identify Dependencies

1. Find all systems using `MinerVessel`, `Hauler`, `Freighter`, `Wagon`
2. Find all authoring components that create these entities
3. Find all systems that query these components

### Phase 2: Create Space4X Transport Components

1. Create `Assets/Scripts/Space4x/Runtime/TransportComponents.cs`
2. Move `MinerVessel`, `Hauler`, `Freighter`, `Wagon` to Space4X namespace
3. Move registry types to Space4X
4. Update namespaces

### Phase 3: Move Systems

1. Move `TransportRegistrySystem` to Space4X
2. Or refactor to be generic (query by component type, not hardcoded)
3. Update `CoreSingletonBootstrapSystem` to not create game-specific registries

### Phase 4: Update References

1. Update all Space4X systems that reference moved components
2. Update authoring components
3. Update assembly definitions if needed

### Phase 5: Clean Up PureDOTS

1. Remove game-specific components from PureDOTS
2. Remove game-specific registry kinds
3. Update documentation

## Prevention

### Automated Checks

1. **Assembly Definition Validation:**
   - PureDOTS assemblies must NOT reference Space4X
   - Compile-time check already in place ✅

2. **Namespace Validation:**
   - No `using Space4X` in PureDOTS package
   - Can add custom analyzer or Roslyn check

3. **Component Naming Convention:**
   - PureDOTS: Generic names (Villager, Resource, Storehouse)
   - Space4X: Game-specific names (Vessel, Miner, Carrier)

4. **Code Review Checklist:**
   - [ ] Is this component/system game-specific?
   - [ ] Does it reference Space4X concepts?
   - [ ] Could another game reuse this code?
   - [ ] Is it in the correct assembly?

## Current State

✅ **Good Separation:**
- `VesselMovement` - Correctly in Space4X
- `VesselAIState` - Correctly in Space4X
- `VesselMovementSystem` - Correctly in Space4X
- Space4X systems correctly reference PureDOTS

❌ **Bad Separation:**
- `MinerVessel` - Should be in Space4X
- `TransportRegistrySystem` - Should be in Space4X or made generic
- Game-specific registries in PureDOTS bootstrap

## Next Steps

1. **Immediate:** Document violations (this file) ✅
2. **Short-term:** Plan migration strategy
3. **Medium-term:** Execute migration
4. **Long-term:** Add automated checks to prevent future violations




