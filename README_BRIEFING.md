# PureDOTS Project Briefing

> **See also**: `TRI_PROJECT_BRIEFING.md` for the complete tri-project overview covering PureDOTS, Space4X, and Godgame.

Welcome to the PureDOTS migration effort. This repository is a fresh Unity project configured with Entities 1.4 and mirrors the package environment from the legacy `godgame` repo. The long-term goal is to deliver the full GodGame experience using a pure DOTS architecture, guided by existing TruthSource documentation.

## Project Locations

| Project | Path |
|---------|------|
| **PureDOTS** | `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` |
| **Space4X** | `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` |
| **Godgame** | `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` |

⚠️ **Note**: The `projects/` subfolder inside PureDOTS is deprecated and should be ignored.

## Current State

- Packages: Manifest matches the legacy project (Entities, Burst, Collections, URP, Coplay, MCP).
- TODO: `PureDOTS_TODO.md` outlines first setup tasks (assemblies, bootstrap, system migration, authoring).
- No gameplay systems have been ported yet—this repo starts clean.

## Mission for Primary Agent

1. Establish core DOTS infrastructure:
   - Create runtime/system/authoring asmdefs.
   - Implement a custom world bootstrap with fixed-step, simulation, and presentation groups.
   - Seed baseline singleton components (time state, history settings) to anchor determinism/rewind.

2. Port reusable DOTS components/systems from the legacy project:
   - Components: resources, villagers, time, history, input.
   - Baker/authoring scripts for SubScene workflows.
   - Systems already DOTS-native (time step, rewind core, resource gathering).

3. Document and validate:
   - Update `PureDOTS_TODO.md` as tasks complete.
   - Note assumptions/deviations in `Docs/Progress.md` (create if absent).
   - Keep alignment with TruthSources for design intent.

## Key Principles

- Pure DOTS: avoid `WorldServices`/service locators; use singleton components, buffers, and systems.
- Determinism & rewind remain central—use existing `TimeState`, `RewindState`, etc. as references.
- Presentation should be hybrid-friendly but minimal—simulation logic belongs in DOTS.
- Salvage carefully: port DOTS-ready code; reimplement hybrid logic for the new architecture.
- Patterns: group/individual “patterns” (e.g., HardworkingVillage, ChaoticBand, OverstressedGroup) live in `Packages/com.moni.puredots/Runtime/Systems/Patterns/PatternSystem.cs` and write `GroupPatternModifiers` + `ActivePatternTag`. IDs are enums (see `Runtime/Patterns/PatternComponents.cs`) to stay Burst-safe—no FixedString construction in static contexts.
- Burst/static-init rule of thumb: do not construct `FixedStringXXBytes` or other managed-backed data in static fields/ctors for bursted systems. Use enum/int IDs in components; keep human-readable names in non-Burst helpers (debug-only).
- Input → ECS contract: camera/hand systems read `CameraInputState` + `CameraInputEdge`/`HandInputEdge` from ECS. `InputSnapshotBridge` (fed by `HandCameraInputRouter` or any other input) copies into those components in `CopyInputToEcsSystem`. Games can wire any input source as long as it writes those ECS components.

## Suggested Starting Checklist

- [ ] Set up `PureDOTS.Runtime`, `PureDOTS.Systems`, `PureDOTS.Authoring` asmdefs.
- [ ] Add/refine `PureDotsWorldBootstrap` for the new project.
- [ ] Port `TimeState`, `RewindState`, `HistorySettings` components and associated systems.
- [ ] Create a simple DOTS-only test SubScene to validate the loop.

Coordinate with TruthSources and the legacy repo for reference data and design constraints. The focus is laying a clean foundation—future agents can expand gameplay domains once the base is solid.

## Critical DOTS Patterns (Must Follow)

> **📚 Full Documentation**: See `Docs/FoundationGuidelines.md` for complete P0-P11 patterns with detailed examples.

### Quick Reference - Common Errors to Avoid

| Error | Cause | Fix |
|-------|-------|-----|
| CS1654 | foreach mutation | Use indexed `for` loop |
| EA0009 | Blob not by ref | `ref var x = ref blob.Value` |
| CS0266 | Implicit enum cast | `(byte)enum` or `(MyEnum)byte` |
| CS1031 | `ref readonly` (C# 12) | Use `ref` or `in` (C# 9) |
| CS0411 | Bad buffer type | Implement `IBufferElementData` |
| CS0311 | Bad authoring type | Inherit `MonoBehaviour` |
| BC1064 | Struct by value in Burst | Use `in` modifier |
| BC1016 | Managed code in Burst | Pre-define string constants |
| CS0103 | Missing import | `using Unity.Mathematics;` |

### Essential Patterns (Summary)

**P0: Verify dependencies exist before writing code**
```bash
grep -r "struct TypeName" --include="*.cs"
```

**P1: Buffer mutation - indexed access only**
```csharp
for (int i = 0; i < buffer.Length; i++) { var item = buffer[i]; item.Value = 5; buffer[i] = item; }
```

**P1: Blob access - always use ref**
```csharp
ref var catalog = ref blobRef.Value;
```

**P4: Blob parameters use `ref`, NOT `in`**
```csharp
void Process(ref ProjectileSpec spec) { }  // NOT 'in' for blob types
```

**P8: No managed strings in Burst**
```csharp
// Pre-define OUTSIDE Burst:
private static readonly FixedString64Bytes MyName = "name";
// Use in Burst:
var name = MyName;
```

**P9: Required imports**
```csharp
using Unity.Mathematics;                    // For math.*, half, float2
using Unity.Collections.LowLevel.Unsafe;    // For Unsafe.*
```

## Pre-Commit Checklist

Before marking any task complete:

- [ ] **Build passes**: Unity domain reload succeeds
- [ ] **Dependencies verified**: All types confirmed via grep
- [ ] **No foreach mutation**: Use indexed loops
- [ ] **Blob access uses ref**: All `blobRef.Value` with `ref`
- [ ] **Blob params use ref**: Not `in` for blob types
- [ ] **Buffer elements correct**: Implement `IBufferElementData`
- [ ] **Authoring inherits MonoBehaviour**: For Baker<T>
- [ ] **No managed strings in Burst**: Pre-define constants
- [ ] **Imports present**: `Unity.Mathematics`, etc.

## How to Adopt This Runtime in a New Game

1. **Read the Truth Sources**  
   - `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` – system groups, determinism contracts.  
   - `Docs/TruthSources/PlatformPerformance_TruthSource.md` – IL2CPP, Burst, worker policy.  
   - `Docs/TODO/SystemIntegration_TODO.md` – active integration tasks and shared components.  
   - Review domain TODOs (Spatial, Climate, Villagers, Resources, Miracles) for feature-specific expectations.

2. **Authoring Assets Required**  
   - **Minimum**: `PureDotsConfigAuthoring` GameObject with `PureDotsRuntimeConfig` asset containing `ResourceTypes` catalog (at least one entry)
   - **Optional**: `EnvironmentGridConfig` ScriptableObject (terrain bounds, grid resolution, climate defaults)  
   - **Optional**: `SpatialPartitionProfile` ScriptableObject (cell size, provider selection)  
   - **Optional**: `HandCameraInputProfile`, `ResourceTypeCatalog`, `VegetationSpeciesCatalog`, and registry-related profiles as needed  
   - Bake these via SubScenes/Bakers; see asset validation notes in `Docs/Guides/Authoring/` (added below).  
   - **See**: `Docs/QA/BootstrapAudit.md` for complete bootstrap coverage and authoring requirements

3. **Bootstrap Steps**  
   - ✅ **Automatic**: `PureDotsWorldBootstrap` creates world and system groups automatically (no MonoBehaviour required)
   - ✅ **Automatic**: `CoreSingletonBootstrapSystem` seeds all core singletons (time, rewind, spatial, registries, navigation)
   - ✅ **Automatic**: System groups are registered and sorted automatically
   - ✅ **Automatic**: `FixedStepSimulationSystemGroup` is configured with 60 FPS timestep
   - Include `link.xml` and Burst options per Platform Performance truth-source when creating builds

4. **Project Setup Checklist**  
   - Enable Burst, Entities Collections, and Jobs packages (already in manifest).  
   - Set Player Settings → Scripting Backend to IL2CPP for release builds; verify Managed Stripping Level is compatible with `link.xml`.  
   - Import core TODO files into your workflow (`PureDOTS_TODO.md`, domain TODOs) to track remaining work.  
   - Configure CI build/validation following `Docs/TODO/Utilities_TODO.md` guidelines (debug overlay, integration harness, deterministic replay).

5. **Where to Ask Questions**  
   - Start with `Docs/INDEX.md` (navigation hub) once present.  
   - Domain-specific questions: check design notes (`Docs/DesignNotes/*.md`) and QA checklists (`Docs/QA/*.md`).  
   - Update TODO checkboxes and design notes when new features land to keep the runtime reusable.
