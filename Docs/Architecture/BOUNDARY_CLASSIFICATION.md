# PureDOTS Framework vs Game Layer Classification

**Last Updated**: 2025-12-01
**Purpose**: Complete catalog of components classified by layer (PureDOTS Framework vs Game Layer)

---

## Overview

This document classifies all existing components, systems, and concepts into:
1. **PureDOTS Framework** - Generic, reusable building blocks (should stay)
2. **Game Layer** - Game-specific implementations (correctly placed)
3. **Boundary Violations** - Game-specific code incorrectly in PureDOTS (needs migration)

---

## PureDOTS Framework Components (Should Stay)

### Core Infrastructure

| Component | Namespace | Purpose | Why Framework-Level |
|-----------|-----------|---------|---------------------|
| `TimeState` | `PureDOTS.Runtime.Components` | Fixed-step simulation time | Generic time management |
| `RewindState` | `PureDOTS.Runtime.Components` | Rewind/replay state | Generic rewind infrastructure |
| `TickTimeState` | `PureDOTS.Runtime.Components` | Tick-based time tracking | Generic time tracking |
| `TimeControlCommand` | `PureDOTS.Runtime.Components` | Time control commands | Generic time control |

### Resource System (Generic)

| Component | Namespace | Purpose | Why Framework-Level |
|-----------|-----------|---------|---------------------|
| `ResourceTypeId` | `PureDOTS.Runtime.Components` | Resource type identifier | Generic resource type system |
| `ResourceSourceType` | `PureDOTS.Runtime.Components` | Source classification enum | Generic classification |
| `ResourceSourceConfig` | `PureDOTS.Runtime.Components` | Source configuration | Generic config pattern |
| `ResourceSourceState` | `PureDOTS.Runtime.Components` | Source runtime state | **Generic** - usable by any game |
| `StorehouseConfig` | `PureDOTS.Runtime.Components` | Storehouse configuration | Generic storage pattern |
| `StorehouseInventory` | `PureDOTS.Runtime.Components` | Inventory state | Generic inventory system |
| `ResourceChunkState` | `PureDOTS.Runtime.Components` | Physical resource chunk | Generic resource physics |
| `ConstructionSiteProgress` | `PureDOTS.Runtime.Components` | Construction progress | Generic construction pattern |
| `ResourceRegistry` | `PureDOTS.Runtime.Components` | Resource registry summary | Generic registry infrastructure |
| `ResourceRegistryEntry` | `PureDOTS.Runtime.Components` | Registry entry | Generic registry pattern |

### Registry Infrastructure (Generic)

| Component | Namespace | Purpose | Why Framework-Level |
|-----------|-----------|---------|---------------------|
| `IRegistryEntry` | `PureDOTS.Runtime.Registry` | Registry entry interface | Generic registry contract |
| `IRegistryFlaggedEntry` | `PureDOTS.Runtime.Registry` | Flagged entry interface | Generic registry pattern |
| `SpawnerConfig` | `PureDOTS.Runtime.Components` | Spawner configuration | Generic spawner pattern |
| `SpawnerState` | `PureDOTS.Runtime.Components` | Spawner runtime state | Generic spawner state |

### AI Framework (Generic)

| Component | Namespace | Purpose | Why Framework-Level |
|-----------|-----------|---------|---------------------|
| `SensorComponents` | `PureDOTS.Runtime.AI` | Sensor framework | Generic AI sensing |
| `UtilityComponents` | `PureDOTS.Runtime.AI` | Utility AI framework | Generic utility AI |
| `SteeringComponents` | `PureDOTS.Runtime.AI` | Steering behaviors | Generic movement AI |
| `GOAPComponents` | `PureDOTS.Runtime.AI.GOAP` | GOAP planning | Generic planning framework |
| `RoutineComponents` | `PureDOTS.Runtime.AI.Routine` | Routine behaviors | Generic routine system |

### Combat Framework (Generic)

| Component | Namespace | Purpose | Why Framework-Level |
|-----------|-----------|---------|---------------------|
| `HealthComponents` | `PureDOTS.Runtime.Combat` | Health/damage system | Generic combat health |
| `WeaponComponents` | `PureDOTS.Runtime.Combat` | Weapon definitions | Generic weapon system |
| `HazardComponents` | `PureDOTS.Runtime.Combat` | Hazard/damage sources | Generic hazard system |

### Morale System (Generic)

| Component | Namespace | Purpose | Why Framework-Level |
|-----------|-----------|---------|---------------------|
| `EntityMorale` | `PureDOTS.Runtime.Morale` | Morale state | **Generic** - morale applies to any entity |
| `MoraleBand` | `PureDOTS.Runtime.Morale` | Morale tier enum | Generic morale bands |
| `MoraleModifier` | `PureDOTS.Runtime.Morale` | Morale modifier | Generic modifier system |
| `MoraleConfig` | `PureDOTS.Runtime.Morale` | Morale configuration | Generic config |

**Note**: Morale system is generic and reusable. However, see violations section for `VillagerNeeds.Morale` field.

### Spatial & Environment (Generic)

| Component | Namespace | Purpose | Why Framework-Level |
|-----------|-----------|---------|---------------------|
| `SpatialGridConfig` | `PureDOTS.Runtime.Spatial` | Spatial partitioning config | Generic spatial system |
| `SpatialGridState` | `PureDOTS.Runtime.Spatial` | Spatial grid state | Generic spatial state |
| `CelestialComponents` | `PureDOTS.Runtime.Celestial` | Celestial mechanics | Generic celestial system |
| `EnvironmentComponents` | `PureDOTS.Runtime.Environment` | Environmental effects | Generic environment system |

### Camera Framework (Generic)

| Component | Namespace | Purpose | Why Framework-Level |
|-----------|-----------|---------|---------------------|
| `CameraRigService` | `PureDOTS.Runtime.Camera` | Camera rig service | Generic camera infrastructure |
| `CameraRigState` | `PureDOTS.Runtime.Camera` | Camera rig state | Generic camera state |
| `CameraRigApplier` | `PureDOTS.Runtime.Camera` | Camera rig applier | Generic camera system |
| `BW2StyleCameraController` | `PureDOTS.Runtime.Camera` | Reusable camera rig | Generic reusable rig |

---

## Game Layer Components (Correctly Placed)

### Space4X Components

| Component | Namespace | Purpose | Location |
|-----------|-----------|---------|----------|
| `Space4XColony` | `Space4X.Registry` | Colony entity | ✅ `Assets/Projects/Space4X/` |
| `Space4XFleet` | `Space4X.Registry` | Fleet entity | ✅ `Assets/Projects/Space4X/` |
| `Space4XLogisticsRoute` | `Space4X.Registry` | Logistics route | ✅ `Assets/Projects/Space4X/` |
| `Space4XAnomaly` | `Space4X.Registry` | Anomaly entity | ✅ `Assets/Projects/Space4X/` |
| `Space4XCrewComponents` | `Space4X.Runtime` | Crew-specific data | ✅ `Assets/Projects/Space4X/` |
| `Space4XAlignmentComponents` | `Space4X.Registry` | Alignment/compliance | ✅ `Assets/Projects/Space4X/` |
| `ModuleQualityComponents` | `Space4X.Modules` | Module system | ✅ `Assets/Projects/Space4X/` |
| `Space4XAbilityTypes` | `Space4X.Knowledge` | Ability system | ✅ `Assets/Projects/Space4X/` |

### Godgame Components

| Component | Namespace | Purpose | Location |
|-----------|-----------|---------|----------|
| `GodgameVillager` | `Godgame.Registry` | Villager mirror | ✅ `Assets/Projects/Godgame/` |
| `GodgameStorehouse` | `Godgame.Registry` | Storehouse mirror | ✅ `Assets/Projects/Godgame/` |
| `GodgameBand` | `Godgame.Registry` | Band mirror | ✅ `Assets/Projects/Godgame/` |
| `GodgameRegistrySnapshot` | `Godgame.Registry` | Registry snapshot | ✅ `Assets/Projects/Godgame/` |

---

## Boundary Violations (Need Migration)

### Critical Violations: Game-Specific Types in PureDOTS

#### 1. Villager Components (Godgame-Specific)

**Location**: `Packages/com.moni.puredots/Runtime/Runtime/VillagerComponents.cs`

**Violation**: Contains "Villager" which is Godgame-specific terminology.

| Component | Current Location | Issue | Target Location |
|-----------|------------------|-------|-----------------|
| `VillagerId` | `PureDOTS.Runtime.Components` | "Villager" is game-specific | Should be generic `ActorId` or move to Godgame |
| `VillagerNeeds` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `EntityNeeds` or move to Godgame |
| `VillagerAttributes` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `EntityAttributes` or move to Godgame |
| `VillagerBelief` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `EntityBelief` or move to Godgame |
| `VillagerReputation` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `EntityReputation` or move to Godgame |
| `VillagerJob` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `EntityJob` or move to Godgame |
| `VillagerMovement` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `EntityMovement` or move to Godgame |
| `VillagerCombatStats` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `EntityCombatStats` or move to Godgame |
| `VillagerRegistry` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `ActorRegistry` or move to Godgame |
| `VillagerRegistryEntry` | `PureDOTS.Runtime.Components` | "Villager" naming | Should be generic `ActorRegistryEntry` or move to Godgame |

**Recommendation**: 
- **Option A**: Rename to generic "Actor" or "Entity" terminology (e.g., `ActorNeeds`, `EntityJob`)
- **Option B**: Move to Godgame project if truly Godgame-specific

**Impact**: High - Many systems depend on these types.

#### 2. Miracle Components (Godgame-Specific)

**Location**: `Packages/com.moni.puredots/Runtime/Runtime/MiracleComponents.cs`

**Violation**: "Miracle" is Godgame-specific terminology. Space4X uses "Abilities" or "Tech".

| Component | Current Location | Issue | Target Location |
|-----------|------------------|-------|-----------------|
| `MiracleType` | `PureDOTS.Runtime.Components` | "Miracle" is Godgame-specific | Move to `Godgame.Runtime` |
| `MiracleDefinition` | `PureDOTS.Runtime.Components` | "Miracle" naming | Move to `Godgame.Runtime` |
| `MiracleRuntimeState` | `PureDOTS.Runtime.Components` | "Miracle" naming | Move to `Godgame.Runtime` |
| `MiracleRegistry` | `PureDOTS.Runtime.Components` | "Miracle" naming | Move to `Godgame.Runtime` |
| `MiracleRegistryEntry` | `PureDOTS.Runtime.Components` | "Miracle" naming | Move to `Godgame.Runtime` |

**Recommendation**: Move entire `MiracleComponents.cs` to `Assets/Projects/Godgame/Scripts/Godgame/Runtime/`

**Impact**: Medium - Godgame-specific, but may have dependencies.

#### 3. Divine Hand Components (Godgame-Specific)

**Location**: `Packages/com.moni.puredots/Runtime/Runtime/DivineHandComponents.cs`

**Violation**: "Divine Hand" is Godgame-specific player interaction mechanism.

| Component | Current Location | Issue | Target Location |
|-----------|------------------|-------|-----------------|
| `DivineHandTag` | `PureDOTS.Runtime.Components` | "Divine Hand" is Godgame-specific | Move to `Godgame.Runtime` |
| `DivineHandConfig` | `PureDOTS.Runtime.Components` | "Divine Hand" naming | Move to `Godgame.Runtime` |
| `DivineHandState` | `PureDOTS.Runtime.Components` | "Divine Hand" naming | Move to `Godgame.Runtime` |
| `HandPickable` | `PureDOTS.Runtime.Components` | Hand-specific | Move to `Godgame.Runtime` |

**Recommendation**: Move entire `DivineHandComponents.cs` to `Assets/Projects/Godgame/Scripts/Godgame/Runtime/`

**Impact**: Low - Clearly Godgame-specific.

#### 4. ResourceSourceState Reference to Villager

**Location**: `Packages/com.moni.puredots/Runtime/Runtime/ResourceComponents.cs` line 395

**Violation**: `ResourceActiveTicket` contains `Entity Villager;` field.

```csharp
public struct ResourceActiveTicket : IBufferElementData
{
    public Entity Villager;  // ❌ Should be generic "Entity Worker" or "Entity Actor"
    public uint TicketId;
    public float ReservedUnits;
}
```

**Recommendation**: Rename field to `Entity Worker` or `Entity Actor` to be generic.

**Impact**: Low - Single field rename.

---

## Name Collisions

### No Current Collisions Found

**Status**: ✅ No name collisions detected.

- `ResourceSourceState` exists only in PureDOTS (generic)
- Space4X does not define its own `ResourceSourceState`
- Games use PureDOTS `ResourceSourceState` correctly

---

## Design Documentation Review

### PatternBible.md

**Location**: `Packages/com.moni.puredots/Documentation/PatternBible.md`

**Status**: ✅ **Keep in PureDOTS**

**Reason**: This is conceptual documentation (pre-implementation patterns), not code. Contains game-specific examples but serves as a pattern library for all games.

**Recommendation**: Add note at top: "Patterns are conceptual and may reference game-specific examples. Implementation should be game-agnostic."

### VillagerDecisionMaking.md

**Location**: `Packages/com.moni.puredots/Documentation/DesignNotes/VillagerDecisionMaking.md`

**Status**: ⚠️ **Review Required**

**Reason**: May contain Godgame-specific "Villager" terminology. Should be reviewed to ensure it describes generic AI decision-making patterns.

**Recommendation**: Review and rename to `ActorDecisionMaking.md` or `EntityDecisionMaking.md` if generic, or move to Godgame docs if Villager-specific.

---

## Summary Statistics

| Category | Count | Status |
|----------|-------|--------|
| PureDOTS Framework Components | ~200+ | ✅ Correctly placed |
| Space4X Game Components | ~50+ | ✅ Correctly placed |
| Godgame Components | ~30+ | ✅ Correctly placed |
| **Boundary Violations** | **~15** | ❌ **Need Migration** |
| Name Collisions | 0 | ✅ None found |

---

## Next Steps

See [BOUNDARY_CONTRACT.md](BOUNDARY_CONTRACT.md) for rules and [MIGRATION_PLAN.md](MIGRATION_PLAN.md) for step-by-step fixes.

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

