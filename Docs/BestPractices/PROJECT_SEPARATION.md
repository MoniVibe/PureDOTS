# Project Separation Guide

**Last Updated**: 2025-12-01
**Purpose**: Prevent code mixups between PureDOTS, Space4X, and Godgame projects

---

## ⚠️ CRITICAL: Three Separate Projects

**Always verify which project you're working in before writing code.**

| Project | Path | Purpose | Code Location |
|---------|------|---------|---------------|
| **PureDOTS** | `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` | **Shared framework package** | `Packages/com.moni.puredots/` |
| **Space4X** | `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` | **Carrier 4X game** | `Assets/Projects/Space4X/` |
| **Godgame** | `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` | **God-game simulation** | `Assets/Projects/Godgame/` |

---

## PureDOTS Framework Rules

### ✅ What Belongs in PureDOTS

**PureDOTS is game-agnostic shared infrastructure:**

- ✅ Time/Rewind systems (`TimeState`, `RewindState`, `TickTimeState`)
- ✅ Registry infrastructure (Villager, Storehouse, Resource, Band, Miracle registries)
- ✅ Spatial partitioning (`SpatialGridConfig`, `SpatialGridState`)
- ✅ Telemetry & debug systems
- ✅ AI pipeline (sensors, utility scoring, steering, task resolution)
- ✅ Scenario runner (headless CLI)
- ✅ Generic authoring tools
- ✅ Best practices documentation

**Location:** `Packages/com.moni.puredots/`

### ❌ What Does NOT Belong in PureDOTS

**Game-specific code must stay in game projects:**

- ❌ Space4X carriers, modules, mining systems
- ❌ Godgame villagers, miracles, divine hand
- ❌ Game-specific input actions
- ❌ Game-specific presentation/camera code
- ❌ Game-specific mechanics

**If you see paths like these in PureDOTS workspace, they're artifacts:**
- `Assets/Projects/Space4X/` ❌ (should be in Space4X project)
- `Assets/Scripts/Space4x/` ❌ (should be in Space4X project)
- `Assets/Projects/Godgame/` ❌ (should be in Godgame project)

---

## Space4X Game Rules

### ✅ What Belongs in Space4X

**Space4X-specific game code:**

- ✅ Carrier systems (mining, hauling, fleet combat)
- ✅ Module system (carrier customization, refit/repair)
- ✅ Alignment/compliance (crew alignment, mutiny detection)
- ✅ Tech diffusion (research spreading)
- ✅ Space4X input actions (`Space4X.Input` namespace)
- ✅ Space4X camera controllers
- ✅ Space4X presentation code

**Location:** `C:\Users\Moni\Documents\claudeprojects\unity\Space4x\Assets/Projects/Space4X/`

### Integration Pattern

**Space4X consumes PureDOTS via package reference:**

```json
// Packages/manifest.json in Space4X project
{
  "dependencies": {
    "com.moni.puredots": "file:../../PureDOTS/Packages/com.moni.puredots"
  }
}
```

---

## Godgame Rules

### ✅ What Belongs in Godgame

**Godgame-specific game code:**

- ✅ Villager AI (game-specific behavior)
- ✅ Miracles (game-specific effects)
- ✅ Divine hand (game-specific interaction)
- ✅ Villages/Bands (game-specific aggregates)
- ✅ Godgame input actions (`Godgame.Input` namespace)
- ✅ Godgame camera controllers
- ✅ Godgame presentation code

**Location:** `C:\Users\Moni\Documents\claudeprojects\unity\Godgame\Assets/Projects/Godgame/`

### Integration Pattern

**Godgame consumes PureDOTS via package reference:**

```json
// Packages/manifest.json in Godgame project
{
  "dependencies": {
    "com.moni.puredots": "file:../../PureDOTS/Packages/com.moni.puredots"
  }
}
```

---

## Common Mixup Scenarios

### Scenario 1: Camera Code

**❌ Wrong:** Creating camera controller in PureDOTS workspace
```
PureDOTS/Assets/Scripts/Space4x/Camera/Space4XCameraController.cs  ❌
```

**✅ Correct:** Camera controller in Space4X project
```
Space4x/Assets/Scripts/Space4x/Camera/Space4XCameraController.cs  ✅
```

**PureDOTS provides:** Framework camera infrastructure (`CameraRigService`, `CameraRigState`)
**Game projects provide:** Game-specific camera controllers

### Scenario 2: Input Actions

**❌ Wrong:** Input actions in PureDOTS
```
PureDOTS/Assets/Input/GameplayActions.inputactions  ❌
```

**✅ Correct:** Input actions in game projects
```
Space4x/Assets/Input/GameplayActions.inputactions  ✅
Godgame/Assets/Input/GameplayActions.inputactions  ✅
```

**PureDOTS provides:** Input integration pattern (command components, input reading systems)
**Game projects provide:** Game-specific action maps

### Scenario 3: Game-Specific Systems

**❌ Wrong:** Game-specific system in PureDOTS
```csharp
// PureDOTS/Packages/com.moni.puredots/Runtime/Systems/CarrierMiningSystem.cs  ❌
namespace PureDOTS.Systems
{
    public partial struct CarrierMiningSystem : ISystem  // Space4X-specific!
    {
        // ...
    }
}
```

**✅ Correct:** Game-specific system in game project
```csharp
// Space4x/Assets/Projects/Space4X/Scripts/Systems/CarrierMiningSystem.cs  ✅
namespace Space4X.Systems
{
    public partial struct CarrierMiningSystem : ISystem
    {
        // Uses PureDOTS registries, but is Space4X-specific
    }
}
```

---

## Verification Checklist

**Before committing code, verify:**

- [ ] **Which project am I in?** Check current workspace path
- [ ] **Is this code game-agnostic?** (PureDOTS only)
- [ ] **Does this reference game-specific types?** (Should be in game project)
- [ ] **Is the file path correct?** (PureDOTS: `Packages/com.moni.puredots/`, Games: `Assets/Projects/{Game}/`)
- [ ] **Are namespaces correct?** (PureDOTS: `PureDOTS.*`, Space4X: `Space4X.*`, Godgame: `Godgame.*`)

---

## Quick Reference

### PureDOTS Framework
- **Path**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS`
- **Code**: `Packages/com.moni.puredots/`
- **Namespace**: `PureDOTS.*`
- **Rule**: Game-agnostic only

### Space4X Game
- **Path**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x`
- **Code**: `Assets/Projects/Space4X/`
- **Namespace**: `Space4X.*`
- **Rule**: Space4X-specific code

### Godgame
- **Path**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame`
- **Code**: `Assets/Projects/Godgame/`
- **Namespace**: `Godgame.*`
- **Rule**: Godgame-specific code

---

## Component-Level Classification

### Generic vs Game-Specific Components

**PureDOTS Framework Components**:
- ✅ Generic terminology: `ResourceSourceState`, `EntityMorale`, `ActorNeeds`, `StorehouseInventory`
- ✅ Domain-agnostic: `TimeState`, `Registry`, `Spatial`, `Telemetry`, `AI`, `Combat`
- ❌ Avoid game-specific terms: `Villager`, `Carrier`, `Miracle`, `Mining`, `Mutiny`

**Game Layer Components**:
- ✅ Game-specific types: `Space4X.Carrier`, `Godgame.Villager`, `Godgame.Miracle`
- ✅ Game-specific state: `Space4X.MiningPhase`, `Space4X.MutinyState`, `Godgame.MiracleState`
- ✅ Game-specific configs: `Space4X.MinerConfig`, `Godgame.VillagerConfig`

### Component Naming Patterns

**PureDOTS Pattern**: `{GenericDomain}{Type}`
```csharp
// ✅ PureDOTS
public struct ResourceSourceState : IComponentData { }
public struct EntityMorale : IComponentData { }
public struct ActorNeeds : IComponentData { }
```

**Game Layer Pattern**: `{GameName}.{GameSpecificType}`
```csharp
// ✅ Space4X
namespace Space4X.Runtime
{
    public struct Carrier : IComponentData { }
    public struct MiningPhase : IComponentData { }
}

// ✅ Godgame
namespace Godgame.Runtime
{
    public struct Villager : IComponentData { }
    public struct MiracleState : IComponentData { }
}
```

### Boundary Violation Examples

**❌ Wrong: Game-Specific Component in PureDOTS**
```csharp
// PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/VillagerComponents.cs  ❌
namespace PureDOTS.Runtime.Components
{
    public struct VillagerNeeds : IComponentData  // "Villager" is Godgame-specific!
    {
        // ...
    }
}
```

**✅ Correct: Generic Component in PureDOTS**
```csharp
// PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/ActorComponents.cs  ✅
namespace PureDOTS.Runtime.Components
{
    public struct ActorNeeds : IComponentData  // Generic "Actor" terminology
    {
        // ...
    }
}
```

**✅ Correct: Game-Specific Component in Game Project**
```csharp
// Godgame/Assets/Projects/Godgame/Scripts/Godgame/Runtime/VillagerComponents.cs  ✅
namespace Godgame.Runtime
{
    public struct VillagerNeeds : IComponentData  // Game-specific, correctly placed
    {
        // ...
    }
}
```

### Decision Framework for New Components

When creating a new component, ask:

1. **Is this concept game-specific?**
   - ✅ Yes → Game Layer (`Assets/Projects/{Game}/`)
   - ❌ No → Continue

2. **Can this be used by multiple games?**
   - ✅ Yes → PureDOTS Framework (`Packages/com.moni.puredots/`)
   - ❌ No → Game Layer

3. **Does this reference game-specific types?**
   - ✅ Yes → Game Layer
   - ❌ No → PureDOTS Framework

### Component Categories

| Category | PureDOTS | Game Layer |
|----------|----------|------------|
| **Tags** | `JobTag`, `ActiveTag` | `MiningTag`, `CarrierTag`, `MiracleTag` |
| **Configs** | `ResourceSourceConfig`, `StorehouseConfig` | `Space4X.MinerConfig`, `Godgame.VillagerConfig` |
| **State** | `ResourceSourceState`, `ConstructionSiteProgress` | `Space4X.MiningPhase`, `Godgame.MiracleState` |
| **Systems** | `ResourceFlowSystem`, `MovementSystem` | `Space4X.Space4XMiningSystem`, `Godgame.GodgameMiracleSystem` |
| **Authoring** | `ResourceAuthoring`, `StorehouseAuthoring` | `Space4X.CarrierAuthoring`, `Godgame.VillagerAuthoring` |

---

## Additional Resources

- [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) - Complete tri-project overview
- [PUREDOTS_INTEGRATION_SPEC.md](../PUREDOTS_INTEGRATION_SPEC.md) - How games integrate with PureDOTS
- [FoundationGuidelines.md](../FoundationGuidelines.md) - Coding standards
- [BOUNDARY_CLASSIFICATION.md](../ARCHITECTURE/BOUNDARY_CLASSIFICATION.md) - Complete component catalog
- [BOUNDARY_CONTRACT.md](../ARCHITECTURE/BOUNDARY_CONTRACT.md) - Detailed rules and examples
- [MIGRATION_PLAN.md](../ARCHITECTURE/MIGRATION_PLAN.md) - Step-by-step fixes for violations

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

