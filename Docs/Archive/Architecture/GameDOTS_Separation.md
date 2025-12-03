# Game/DOTS Separation Convention

## Overview

This project maintains a strict separation between:
- **PureDOTS Package** (`Packages/com.moni.puredots/`): Generic, reusable DOTS framework code (environmental daemon)
- **Game Projects** (External): Space4X, Godgame, etc. - Game-specific implementations that reference PureDOTS

**PureDOTS is a formal Unity package** that game projects depend on. Games reference PureDOTS via `Packages/manifest.json` and build their game-specific code on top of the framework.

## Rules

### PureDOTS Package Rules

✅ **ALLOWED:**
- Generic DOTS systems and utilities
- Framework components (TimeState, RewindState, HistorySettings, etc.)
- Registry patterns and infrastructure
- Spatial grid systems
- Generic authoring components
- Editor tooling for DOTS

❌ **FORBIDDEN:**
- Game-specific components (Vessels, Miners, Haulers, etc.)
- Game-specific systems that reference game concepts
- Any reference to `Space4X` namespace
- Game-specific authoring components
- Dependencies on game assemblies

### Space4X Game Rules

✅ **ALLOWED:**
- Game-specific components (VesselMovement, VesselAIState, etc.)
- Game-specific systems (VesselMovementSystem, VesselAISystem, etc.)
- Game-specific authoring (VesselAuthoring, CarrierAuthoring, etc.)
- References to PureDOTS package assemblies

❌ **FORBIDDEN:**
- Modifying PureDOTS package internals
- Direct access to PureDOTS private/internal APIs
- Creating systems in PureDOTS namespace

## Dependency Direction

```
Space4X (Game)
    ↓ depends on
PureDOTS (Framework)
    ↓ depends on
Unity Packages
```

**NEVER reverse this direction!**

## Assembly Definitions

- `PureDOTS.Runtime.asmdef` - Must NOT reference Space4X
- `PureDOTS.Systems.asmdef` - Must NOT reference Space4X  
- `Space4X.asmdef` - CAN reference PureDOTS.Runtime and PureDOTS.Systems

## Current Violations

### ❌ VIOLATION: Game-Specific Components in PureDOTS

**Location:** `Packages/com.moni.puredots/Runtime/Runtime/Transport/TransportComponents.cs`

**Issues:**
- `MinerVessel` - Game-specific concept, should be in Space4X
- `Hauler` - Game-specific concept, should be in Space4X
- `Freighter` - Game-specific concept, should be in Space4X
- `Wagon` - Game-specific concept, should be in Space4X

**Impact:**
- `TransportRegistrySystem` in PureDOTS queries for game-specific components
- Makes PureDOTS package non-reusable for other games
- Violates separation of concerns

**Fix Required:**
1. Move `MinerVessel`, `Hauler`, `Freighter`, `Wagon` components to Space4X
2. Move `TransportRegistrySystem` to Space4X or make it generic/configurable
3. Keep only generic transport registry infrastructure in PureDOTS

## Enforcement

### Compile-Time Checks

✅ Assembly definitions prevent reverse dependencies (Space4X → PureDOTS is OK, PureDOTS → Space4X is blocked)

### Runtime Checks

- Systems should not query for components they don't own
- Authoring components should be in appropriate assemblies

### Code Review Checklist

- [ ] No `using Space4X` in PureDOTS package
- [ ] No game-specific component types in PureDOTS
- [ ] No game-specific system logic in PureDOTS
- [ ] All game code is in `Assets/Scripts/Space4x/`
- [ ] All framework code is in `Packages/com.moni.puredots/`

## Migration Guide

When moving code from PureDOTS to Space4X:

1. **Move Component File:**
   - Copy from `Packages/com.moni.puredots/Runtime/Runtime/...`
   - To `Assets/Scripts/Space4x/Runtime/...`
   - Update namespace from `PureDOTS.Runtime.*` to `Space4X.Runtime`

2. **Update Systems:**
   - Update namespace references
   - Update `using` statements
   - Update assembly references if needed

3. **Update Authoring:**
   - Move authoring components to Space4X
   - Update `GetEntity()` calls if needed

4. **Clean Up:**
   - Remove from PureDOTS
   - Update any PureDOTS systems that depended on moved code
   - Make PureDOTS systems generic or move them too

