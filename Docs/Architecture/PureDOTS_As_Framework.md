# PureDOTS as Environmental Framework

## Overview

PureDOTS is formalized as an **environmental daemon** - a framework service that game projects depend on. It provides deterministic simulation infrastructure, registry systems, spatial partitioning, and authoring tools that games build upon.

## Architecture Model

```
┌─────────────────────────────────────────────────────────┐
│                    PureDOTS Framework                   │
│              (Environmental Daemon/Service)             │
│                                                         │
│  ┌─────────────────────────────────────────────────┐  │
│  │  Core Services                                  │  │
│  │  - Time & Rewind System                         │  │
│  │  - Registry Infrastructure                      │  │
│  │  - Spatial Grid System                          │  │
│  │  - Resource Management                          │  │
│  │  - Deterministic Simulation Groups              │  │
│  └─────────────────────────────────────────────────┘  │
│                                                         │
│  ┌─────────────────────────────────────────────────┐  │
│  │  Authoring & Tooling                            │  │
│  │  - Baker Components                             │  │
│  │  - Editor Tools                                 │  │
│  │  - Validation Systems                           │  │
│  └─────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                        ▲
                        │ depends on
                        │
        ┌───────────────┴───────────────┐
        │                               │
┌───────┴────────┐            ┌─────────┴────────┐
│   Space4X      │            │     Godgame      │
│   Game Project │            │   Game Project   │
│                │            │                  │
│ - Vessels      │            │ - Villagers      │
│ - Carriers     │            │ - Buildings      │
│ - Mining       │            │ - Farming        │
└────────────────┘            └──────────────────┘
```

## Formal Dependency Model

### PureDOTS Package

**Package Identity:**
- **Name**: `com.moni.puredots`
- **Display Name**: Pure DOTS Core Framework
- **Type**: Unity Package (UPM)
- **Distribution**: Git repository or local file reference

**Package Structure:**
```
com.moni.puredots/
├── package.json              # Package manifest
├── README.md                 # Package documentation
├── CHANGELOG.md              # Version history
├── Runtime/
│   ├── Runtime/             # Core components
│   ├── Systems/             # Framework systems
│   ├── Authoring/           # Baker components
│   ├── Config/              # Configuration assets
│   └── Input/               # Input handling
└── Editor/                  # Editor tooling
```

### Game Project Integration

**Game projects reference PureDOTS via `Packages/manifest.json`:**

```json
{
  "dependencies": {
    "com.moni.puredots": "file:../PureDOTS/Packages/com.moni.puredots"
  }
}
```

**Or via Git (for versioned releases):**

```json
{
  "dependencies": {
    "com.moni.puredots": "https://github.com/moni/puredots.git?path=/Packages/com.moni.puredots#v1.0.0"
  }
}
```

## Framework Contract

### What PureDOTS Provides

#### 1. Core Infrastructure

- **Time System**: `TimeState`, `TimeTickSystem`, deterministic tick management
- **Rewind System**: `RewindState`, history recording, playback support
- **World Bootstrap**: `PureDotsWorldBootstrap` - initializes world and system groups
- **Registry Infrastructure**: Generic registry patterns, metadata, health monitoring
- **Spatial Grid**: Spatial partitioning, cell-based queries, residency tracking

#### 2. Authoring Components

- **Configuration**: `PureDotsConfigAuthoring`, `TimeSettingsAuthoring`, `HistorySettingsAuthoring`
- **Resources**: `ResourceSourceAuthoring`, `StorehouseAuthoring`
- **Generic Entities**: `VillagerAuthoring` (framework example)
- **Environment**: `EnvironmentGridConfigAuthoring`, `ClimateProfileAuthoring`

#### 3. Systems & Groups

- **Simulation Groups**: `TimeSystemGroup`, `VillagerSystemGroup`, `ResourceSystemGroup`, `SpatialSystemGroup`
- **Core Systems**: Time management, registry updates, spatial grid maintenance
- **Framework Systems**: Generic systems that games can extend

#### 4. Editor Tooling

- **Validation**: Scene validation, asset validation
- **Setup Tools**: Menu items for scene setup, configuration
- **Debugging**: Debug HUDs, diagnostic tools

### What Games Must Provide

#### 1. Game-Specific Components

- **Domain Components**: Game-specific entity components (Vessels, Buildings, etc.)
- **Game Systems**: Systems that implement game logic using PureDOTS infrastructure
- **Authoring Components**: Bakers for game-specific entities

#### 2. Assembly Definitions

- **Game Assemblies**: Must reference PureDOTS assemblies
- **No Reverse Dependencies**: PureDOTS must NOT reference game assemblies

#### 3. Configuration

- **Runtime Config**: Reference `PureDotsRuntimeConfig` asset
- **Time Settings**: Configure time step, history settings
- **System Groups**: Register game systems in appropriate groups

## Integration Pattern

### Step 1: Add PureDOTS Dependency

**In game project's `Packages/manifest.json`:**

```json
{
  "dependencies": {
    "com.moni.puredots": "file:../PureDOTS/Packages/com.moni.puredots"
  }
}
```

### Step 2: Create Game Assembly

**`Assets/Scripts/GameName.asmdef`:**

```json
{
  "name": "GameName.Runtime",
  "rootNamespace": "GameName",
  "references": [
    "Unity.Entities",
    "Unity.Mathematics",
    "Unity.Burst",
    "Unity.Collections",
    "Unity.Transforms",
    "PureDOTS.Runtime",
    "PureDOTS.Systems"
  ]
}
```

### Step 3: Implement Game Systems

**Example: `Assets/Scripts/Systems/GameMovementSystem.cs`**

```csharp
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace GameName.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TimeSystemGroup))]
    public partial struct GameMovementSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            // Use PureDOTS time system
            // Implement game-specific movement logic
        }
    }
}
```

### Step 4: Create Game Authoring

**Example: `Assets/Scripts/Authoring/GameEntityAuthoring.cs`**

```csharp
using PureDOTS.Authoring;
using Unity.Entities;
using UnityEngine;

namespace GameName.Authoring
{
    public class GameEntityAuthoring : MonoBehaviour
    {
        // Game-specific authoring data
    }

    public class GameEntityBaker : Baker<GameEntityAuthoring>
    {
        public override void Bake(GameEntityAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            // Add game-specific components
            // Can reference PureDOTS components via using statements
        }
    }
}
```

### Step 5: Setup Scene

1. Add `PureDotsConfigAuthoring` to root scene
2. Reference `PureDotsRuntimeConfig` asset
3. Create SubScenes for game entities
4. PureDOTS systems auto-register via `PureDotsWorldBootstrap`

## Versioning Strategy

### Semantic Versioning

**Format**: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes to framework API
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

**Example**: `1.2.3`

### Version Tags

- **Git Tags**: `v1.0.0`, `v1.1.0`, etc.
- **Package Version**: Updated in `package.json`
- **Changelog**: Documented in `CHANGELOG.md`

### Game Project Locking

**Game projects can lock to specific versions:**

```json
{
  "dependencies": {
    "com.moni.puredots": "https://github.com/moni/puredots.git?path=/Packages/com.moni.puredots#v1.0.0"
  }
}
```

**Or use file reference for development:**

```json
{
  "dependencies": {
    "com.moni.puredots": "file:../PureDOTS/Packages/com.moni.puredots"
  }
}
```

## Framework API Surface

### Public API (Games Can Use)

#### Components
- `TimeState`, `RewindState`, `HistorySettings`
- `ResourceSourceState`, `Storehouse`, `ResourceRegistry`
- `SpatialGridConfig`, `SpatialGridState`
- `RegistryMetadata`, `RegistryHealth`

#### Systems
- `TimeTickSystem`, `RewindCoordinatorSystem`
- `ResourceRegistrySystem`, `StorehouseRegistrySystem`
- `SpatialGridBuildSystem`, `SpatialGridUpdateSystem`
- `PureDotsWorldBootstrap`

#### Authoring
- `PureDotsConfigAuthoring`, `TimeSettingsAuthoring`
- `ResourceSourceAuthoring`, `StorehouseAuthoring`
- `VillagerAuthoring` (framework example)

### Internal API (Framework Only)

- Package internals (not exposed to games)
- Internal system implementations
- Editor-only code

## Extension Points

### For Game Projects

1. **System Groups**: Games can create custom system groups
2. **Components**: Games add their own components
3. **Registries**: Games can create custom registries using framework patterns
4. **Authoring**: Games create their own bakers

### For PureDOTS Framework

1. **Extension Interfaces**: Framework can expose interfaces for games to implement
2. **Event Systems**: Framework can emit events games subscribe to
3. **Configuration Assets**: Games can extend config ScriptableObjects
4. **Partial Classes**: Framework can use partial classes for game extensions

## Testing Strategy

### Framework Tests (PureDOTS)

- Unit tests for framework systems
- Integration tests for framework components
- Located in: `PureDOTS/Assets/Tests/`

### Game Tests (Game Projects)

- Game-specific tests
- Can reference PureDOTS for test utilities
- Located in: `GameProject/Assets/Tests/`

## Distribution Methods

### Method 1: Local Development (Current)

```json
{
  "com.moni.puredots": "file:../PureDOTS/Packages/com.moni.puredots"
}
```

**Use Case**: Active development, changes sync immediately

### Method 2: Git Repository

```json
{
  "com.moni.puredots": "https://github.com/moni/puredots.git?path=/Packages/com.moni.puredots#v1.0.0"
}
```

**Use Case**: Versioned releases, stable builds

### Method 3: Local Package Server

```json
{
  "com.moni.puredots": "http://localhost:4873/com.moni.puredots/-/1.0.0.tgz"
}
```

**Use Case**: Internal package registry, CI/CD

## Best Practices

### For PureDOTS Framework

1. **API Stability**: Maintain backward compatibility between minor versions
2. **Documentation**: Document all public APIs
3. **Versioning**: Follow semantic versioning strictly
4. **Testing**: Comprehensive test coverage for framework code
5. **No Game Dependencies**: Framework must never depend on game code

### For Game Projects

1. **Version Locking**: Lock to specific PureDOTS versions for production
2. **Assembly Definitions**: Properly reference PureDOTS assemblies
3. **Namespace Separation**: Use game-specific namespaces
4. **Extension Over Modification**: Extend framework, don't modify it
5. **Framework Updates**: Test thoroughly when updating PureDOTS version

## Migration Path

### From Embedded Code to Package

1. **Current State**: Space4X code embedded in PureDOTS project
2. **Target State**: Space4X as external project referencing PureDOTS package

**Steps:**
1. Create/verify PureDOTS package structure
2. Create Space4X project (if doesn't exist)
3. Move Space4X code from PureDOTS to Space4X project
4. Reference PureDOTS package in Space4X's `manifest.json`
5. Update assembly definitions
6. Test integration

## Formal Contract Checklist

### PureDOTS Framework Must:

- [x] Provide stable public API
- [x] Maintain backward compatibility (within major version)
- [x] Not depend on game-specific code
- [x] Document all public APIs
- [x] Version releases following semantic versioning
- [x] Provide comprehensive tests
- [x] Support multiple game projects simultaneously

### Game Projects Must:

- [x] Reference PureDOTS via package manifest
- [x] Create proper assembly definitions
- [x] Use game-specific namespaces
- [x] Not modify PureDOTS package code
- [x] Lock to specific versions for production
- [x] Test integration when updating PureDOTS

## Examples

### Space4X Project Setup

**`Space4X/Packages/manifest.json`:**
```json
{
  "dependencies": {
    "com.moni.puredots": "file:../../PureDOTS/Packages/com.moni.puredots"
  }
}
```

**`Space4X/Assets/Scripts/Space4X.asmdef`:**
```json
{
  "name": "Space4X",
  "references": [
    "PureDOTS.Runtime",
    "PureDOTS.Systems"
  ]
}
```

### Godgame Project Setup

**`Godgame/Packages/manifest.json`:**
```json
{
  "dependencies": {
    "com.moni.puredots": "file:../../PureDOTS/Packages/com.moni.puredots"
  }
}
```

**`Godgame/Assets/Scripts/Godgame.asmdef`:**
```json
{
  "name": "Godgame.Runtime",
  "references": [
    "PureDOTS.Runtime",
    "PureDOTS.Systems"
  ]
}
```

## Summary

PureDOTS is a **formal Unity package** that provides:
- Environmental services (time, rewind, spatial grid)
- Framework infrastructure (registries, systems, authoring)
- Stable API contract for games to build upon

Game projects:
- Reference PureDOTS as a dependency
- Build game-specific code on top of framework
- Maintain strict separation (no reverse dependencies)

This formal structure enables:
- Independent development of games
- Version management and updates
- Reusability across multiple projects
- Clear architectural boundaries








