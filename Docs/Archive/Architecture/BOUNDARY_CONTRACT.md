# PureDOTS Framework vs Game Layer Boundary Contract

**Last Updated**: 2025-12-01
**Purpose**: Clear rules and examples for what belongs in PureDOTS vs game layers

---

## Core Principle

**PureDOTS is game-agnostic infrastructure. Game projects are game-specific implementations.**

---

## Dependency Direction

### ✅ Allowed Dependencies

```
Game Projects (Space4X, Godgame)
    ↓ depends on
PureDOTS Framework
    ↓ depends on
Unity DOTS Packages
```

### ❌ Forbidden Dependencies

```
PureDOTS Framework
    ↓ MUST NOT depend on
Game Projects (Space4X, Godgame)
```

**Enforcement**: PureDOTS packages must NOT reference game assemblies in `.asmdef` files.

---

## Classification Rules

### Rule 1: Generic vs Game-Specific Terminology

**PureDOTS Framework**:
- ✅ Generic terms: `Actor`, `Entity`, `Resource`, `Source`, `Sink`, `Storage`, `Combat`, `Movement`
- ✅ Domain-agnostic: `Time`, `Registry`, `Spatial`, `Telemetry`, `AI`, `Morale`
- ❌ Game-specific terms: `Villager`, `Carrier`, `Miracle`, `DivineHand`, `Mining`, `Mutiny`

**Game Layer**:
- ✅ Game-specific terms: `Space4X.Carrier`, `Godgame.Villager`, `Godgame.Miracle`
- ✅ Game mechanics: `Space4X.MiningPhase`, `Space4X.MutinyState`, `Godgame.MiracleState`

### Rule 2: Component Naming Patterns

#### PureDOTS Framework Components

**Pattern**: `{GenericDomain}{Type}`

| ✅ Correct | ❌ Wrong | Reason |
|------------|----------|--------|
| `ResourceSourceState` | `MinerState` | Generic resource source |
| `EntityMorale` | `VillagerMorale` | Generic morale system |
| `ActorNeeds` | `VillagerNeeds` | Generic needs system |
| `CombatHealth` | `VillagerHealth` | Generic health system |
| `StorehouseInventory` | `VillagerInventory` | Generic storage |

#### Game Layer Components

**Pattern**: `{GameName}.{GameSpecificType}`

| ✅ Correct | ❌ Wrong | Reason |
|------------|----------|--------|
| `Space4X.Carrier` | `Carrier` (in PureDOTS) | Game-specific type |
| `Godgame.Villager` | `Villager` (in PureDOTS) | Game-specific type |
| `Space4X.MiningPhase` | `MiningPhase` (in PureDOTS) | Game-specific state |
| `Godgame.MiracleState` | `MiracleState` (in PureDOTS) | Game-specific state |

### Rule 3: Component Categories

#### Tags & Marker Components

**PureDOTS**: Generic tags
```csharp
// ✅ PureDOTS
public struct JobTag : IComponentData { }
public struct ActiveTag : IComponentData { }
public struct StateTag : IComponentData { }
```

**Game Layer**: Game-specific tags
```csharp
// ✅ Space4X
public struct MiningTag : IComponentData { }
public struct CarrierTag : IComponentData { }

// ✅ Godgame
public struct MiracleTag : IComponentData { }
public struct VillagerTag : IComponentData { }
```

#### Config/Data Components

**PureDOTS**: Generic configs
```csharp
// ✅ PureDOTS
public struct ResourceSourceConfig : IComponentData
{
    public float GatherRatePerWorker;
    public int MaxSimultaneousWorkers;
}

public struct StorehouseConfig : IComponentData
{
    public float ShredRate;
    public float InputRate;
}
```

**Game Layer**: Game-specific configs
```csharp
// ✅ Space4X
public struct MinerConfig : IComponentData
{
    public float MiningEfficiency;
    public MiningToolType ToolType;
}

// ✅ Godgame
public struct VillagerConfig : IComponentData
{
    public float BaseMorale;
    public VillagerArchetype Archetype;
}
```

#### State Components

**PureDOTS**: Generic state
```csharp
// ✅ PureDOTS
public struct ResourceSourceState : IComponentData
{
    public float UnitsRemaining;
    public ResourceQualityTier QualityTier;
}

public struct ConstructionSiteProgress : IComponentData
{
    public float CurrentProgress;
    public float RequiredProgress;
}
```

**Game Layer**: Game-specific state
```csharp
// ✅ Space4X
public struct MiningPhase : IComponentData
{
    public MiningPhaseType Phase;
    public float Progress;
}

// ✅ Godgame
public struct MiracleState : IComponentData
{
    public MiracleLifecycleState Lifecycle;
    public float ChargePercent;
}
```

#### Systems

**PureDOTS**: Generic systems
```csharp
// ✅ PureDOTS
public partial struct ResourceFlowSystem : ISystem
{
    // Generic resource flow logic
}

public partial struct MovementSystem : ISystem
{
    // Generic movement logic
}
```

**Game Layer**: Game-specific systems
```csharp
// ✅ Space4X
public partial struct Space4XMiningSystem : ISystem
{
    // Space4X-specific mining execution
}

// ✅ Godgame
public partial struct GodgameMiracleSystem : ISystem
{
    // Godgame-specific miracle effects
}
```

### Rule 4: Authoring/MonoBehaviours

**PureDOTS**: Generic authoring
```csharp
// ✅ PureDOTS
public class ResourceAuthoring : MonoBehaviour
{
    // Generic resource authoring
}

public class StorehouseAuthoring : MonoBehaviour
{
    // Generic storehouse authoring
}
```

**Game Layer**: Game-specific authoring
```csharp
// ✅ Space4X
public class CarrierAuthoring : MonoBehaviour
{
    // Space4X carrier authoring
}

// ✅ Godgame
public class VillagerAuthoring : MonoBehaviour
{
    // Godgame villager authoring
}
```

### Rule 5: ScriptableObjects/Balance Data

**PureDOTS**: Framework catalogs
```csharp
// ✅ PureDOTS
public class ResourceTypeCatalog : ScriptableObject
{
    // Generic resource types
}

public class JobDefinitionCatalog : ScriptableObject
{
    // Generic job definitions
}
```

**Game Layer**: Game-specific catalogs
```csharp
// ✅ Space4X
public class ModuleCatalog : ScriptableObject
{
    // Space4X module definitions
}

// ✅ Godgame
public class MiracleCatalog : ScriptableObject
{
    // Godgame miracle definitions
}
```

---

## Decision Framework

### When Adding New Code, Ask:

1. **Is this concept game-specific?**
   - ✅ **Yes** → Game Layer (`Assets/Projects/{Game}/`)
   - ❌ **No** → Continue to question 2

2. **Can this be used by multiple games?**
   - ✅ **Yes** → PureDOTS Framework (`Packages/com.moni.puredots/`)
   - ❌ **No** → Game Layer

3. **Does this reference game-specific types?**
   - ✅ **Yes** → Game Layer
   - ❌ **No** → Continue to question 4

4. **Is this infrastructure/engine-level?**
   - ✅ **Yes** → PureDOTS Framework
   - ❌ **No** → Review again

### Examples

#### Example 1: New Mining System

**Question**: "Should mining system go in PureDOTS or Space4X?"

**Analysis**:
- Mining is Space4X-specific (Godgame doesn't have mining)
- Space4X has carriers, asteroids, mining phases
- **Decision**: ✅ **Space4X Game Layer**

**Implementation**:
```csharp
// ✅ Space4X
namespace Space4X.Systems
{
    public partial struct Space4XMiningSystem : ISystem { }
}

// ❌ NOT PureDOTS
namespace PureDOTS.Systems
{
    public partial struct MiningSystem : ISystem { }  // Wrong!
}
```

#### Example 2: New Morale System

**Question**: "Should morale system go in PureDOTS or games?"

**Analysis**:
- Morale applies to any entity (villagers, crew, etc.)
- Multiple games need morale
- **Decision**: ✅ **PureDOTS Framework** (generic)

**Implementation**:
```csharp
// ✅ PureDOTS
namespace PureDOTS.Runtime.Morale
{
    public struct EntityMorale : IComponentData { }
}

// ✅ Games use it
namespace Space4X.Runtime
{
    // Uses PureDOTS.EntityMorale
    public struct Space4XCrewAggregateData : IComponentData
    {
        // Aggregates EntityMorale from crew members
    }
}
```

#### Example 3: New Villager Component

**Question**: "Should villager component go in PureDOTS or Godgame?"

**Analysis**:
- "Villager" is Godgame-specific terminology
- Space4X uses "Crew" or "Individual"
- **Decision**: ✅ **Godgame Game Layer**

**Implementation**:
```csharp
// ✅ Godgame
namespace Godgame.Runtime
{
    public struct Villager : IComponentData { }
}

// ❌ NOT PureDOTS
namespace PureDOTS.Runtime.Components
{
    public struct Villager : IComponentData { }  // Wrong!
}
```

---

## Namespace Conventions

### PureDOTS Framework

**Pattern**: `PureDOTS.{Domain}.{Subdomain}`

```
PureDOTS.Runtime.Components
PureDOTS.Runtime.Registry
PureDOTS.Runtime.AI
PureDOTS.Runtime.Combat
PureDOTS.Runtime.Morale
PureDOTS.Systems
PureDOTS.Authoring
```

### Space4X Game

**Pattern**: `Space4X.{Domain}`

```
Space4X.Registry
Space4X.Systems
Space4X.Runtime
Space4X.Modules
Space4X.Knowledge
Space4X.Presentation
```

### Godgame

**Pattern**: `Godgame.{Domain}`

```
Godgame.Registry
Godgame.Systems
Godgame.Runtime
Godgame.Camera
Godgame.Environment
```

---

## Common Patterns

### Pattern 1: Registry Bridge

**PureDOTS**: Generic registry infrastructure
```csharp
// PureDOTS provides:
public interface IRegistryEntry { }
public struct ResourceRegistry : IComponentData { }
```

**Game Layer**: Game-specific bridge
```csharp
// Space4X implements:
public partial struct Space4XRegistryBridgeSystem : ISystem
{
    // Bridges Space4X.Carrier → PureDOTS registry
}

// Godgame implements:
public partial struct GodgameRegistryBridgeSystem : ISystem
{
    // Bridges Godgame.Villager → PureDOTS registry
}
```

### Pattern 2: Presentation Adapter

**PureDOTS**: Generic presentation bridge
```csharp
// PureDOTS provides:
public struct PresentationBinding : IComponentData { }
```

**Game Layer**: Game-specific presentation
```csharp
// Space4X implements:
public partial struct Space4XPresentationAdapterSystem : ISystem
{
    // Adapts Space4X components to presentation
}

// Godgame implements:
public partial struct GodgamePresentationAdapterSystem : ISystem
{
    // Adapts Godgame components to presentation
}
```

### Pattern 3: Input Handler

**PureDOTS**: Generic input pattern
```csharp
// PureDOTS provides:
public struct InputCommand : IComponentData { }
```

**Game Layer**: Game-specific input
```csharp
// Space4X implements:
public partial struct Space4XInputSystem : ISystem
{
    // Reads Space4X input actions → InputCommand
}

// Godgame implements:
public partial struct GodgameInputSystem : ISystem
{
    // Reads Godgame input actions → InputCommand
}
```

---

## Verification Checklist

Before committing code, verify:

- [ ] **Namespace correct?** PureDOTS uses `PureDOTS.*`, games use `{Game}.*`
- [ ] **No game references?** PureDOTS code doesn't reference `Space4X.*` or `Godgame.*`
- [ ] **Generic terminology?** PureDOTS uses generic terms (Actor, Entity, Resource)
- [ ] **Game-specific in games?** Game-specific types are in game projects
- [ ] **Dependencies correct?** PureDOTS `.asmdef` doesn't reference game assemblies
- [ ] **Location correct?** Code is in correct directory (`Packages/` vs `Assets/Projects/`)

---

## Examples: Correct vs Incorrect

### ✅ Correct: Generic Resource System

```csharp
// PureDOTS
namespace PureDOTS.Runtime.Components
{
    public struct ResourceSourceState : IComponentData
    {
        public float UnitsRemaining;
    }
}

// Space4X uses it
namespace Space4X.Systems
{
    public partial struct Space4XMiningSystem : ISystem
    {
        // Uses PureDOTS.ResourceSourceState
    }
}
```

### ❌ Incorrect: Game-Specific in PureDOTS

```csharp
// ❌ WRONG - In PureDOTS
namespace PureDOTS.Runtime.Components
{
    public struct MiningPhase : IComponentData  // Space4X-specific!
    {
        public MiningPhaseType Phase;
    }
}
```

### ✅ Correct: Game-Specific in Game Project

```csharp
// ✅ CORRECT - In Space4X
namespace Space4X.Systems
{
    public struct MiningPhase : IComponentData
    {
        public MiningPhaseType Phase;
    }
}
```

---

## Enforcement

### Automated Checks (Future)

Consider implementing:
- Linter rule: Check PureDOTS code doesn't reference game namespaces
- Build check: Verify `.asmdef` dependencies
- CI check: Run boundary validation on PRs

### Manual Review

Before merging PRs:
1. Check namespace matches location
2. Verify no game-specific types in PureDOTS
3. Confirm dependencies are correct

---

## Related Documents

- [BOUNDARY_CLASSIFICATION.md](BOUNDARY_CLASSIFICATION.md) - Complete component catalog
- [MIGRATION_PLAN.md](MIGRATION_PLAN.md) - Step-by-step fixes for violations
- [PROJECT_SEPARATION.md](../BestPractices/PROJECT_SEPARATION.md) - Project-level separation guide

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

