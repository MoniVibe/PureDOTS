# Foundation Guidelines

When extending this template for future projects:

1. **Keep configuration data asset-driven.** Prefer updating `PureDotsRuntimeConfig.asset` and `ResourceTypeCatalog.asset` over hardcoding values in scenes or systems. If new domains require configuration, create additional ScriptableObjects under `Assets/PureDOTS/Config` and bake them through authoring components.
2. **Use reusable prefabs as starting points.** Duplicate prefabs in `Assets/PureDOTS/Prefabs` instead of editing them directly; keep the originals as pristine references.
3. **Avoid scene-specific logic in core systems.** Systems within `PureDOTS.Systems` should only rely on components/buffers, not scene names or MonoBehaviours. Authoring scripts should remain thin and conversion-focused.
4. **Validate authoring data.** Follow the pattern established in `ResourceSourceAuthoring` and `PureDotsConfigAssets` by adding `OnValidate` hooks to catch misconfigurations early.
5. **Extend tests alongside features.** Add playmode or editmode tests to `Assets/Tests/` whenever new deterministic systems are introduced. Headless tests allow CI pipelines to catch regressions without loading sample scenes.
6. **Document new tooling.** Update `Docs/EnvironmentSetup.md`, `Docs/SystemOrdering/SystemSchedule.md`, and `Docs/TestingGuidelines.md` whenever new workflows or scripts are added so future teams remain aligned.

---

## Critical DOTS Coding Patterns (Priority Ordered)

These patterns are **mandatory** for all agents working on PureDOTS. Violations cause compile errors that block parallel work.

### P0: Verify Dependencies Before Writing Code

**Before writing any system that references a type or property:**

```bash
# Check if type exists
grep -r "struct TimeState" --include="*.cs"
grep -r "struct RewindState" --include="*.cs"

# Check if property exists
grep -r "ContributionScore" --include="*.cs" | head -5
```

**If not found:** Create the type/property FIRST, or flag as a blocker. Never assume types exist based on design documents alone.

### P1: Buffer Mutation - Use Indexed Access

```csharp
// ❌ COMPILE ERROR (CS1654/CS1657)
foreach (var item in buffer)
    item.Value = 5;

// ✅ CORRECT
for (int i = 0; i < buffer.Length; i++)
{
    var item = buffer[i];
    item.Value = 5;
    buffer[i] = item;
}
```

### P1: Blob Access - Always Use Ref

```csharp
// ❌ COMPILE ERROR (EA0001/EA0009)
var catalog = blobRef.Value;

// ✅ CORRECT
ref var catalog = ref blobRef.Value;
```

### P2: Enum Storage - Explicit Casts

```csharp
// Component stores byte, code uses enum
// ❌ COMPILE ERROR (CS0266)
component.ModeRaw = AvoidanceMode.Flee;

// ✅ CORRECT
component.ModeRaw = (byte)AvoidanceMode.Flee;
```

### P2: Rewind Guard - Check Before Mutation

```csharp
public void OnUpdate(ref SystemState state)
{
    var rewind = SystemAPI.GetSingleton<RewindState>();
    if (rewind.Mode != RewindMode.Record) return;
    // ... safe to mutate ...
}
```

### P3: C# Version - Unity Uses C# 9, NOT C# 12

```csharp
// ❌ COMPILE ERROR (CS1031, CS1001) - C# 12 syntax not supported
ref readonly var spec = ref FindSpec(...);
private static ref readonly ProjectileSpec FindSpec(...) { }

// ✅ CORRECT - Use C# 9 patterns
ref var spec = ref FindSpec(...);                    // For local variables
private static ref ProjectileSpec FindSpec(...) { }  // For return types

// For read-only parameters, use 'in' not 'ref readonly':
void Process(in ProjectileSpec spec) { }  // ✅ C# 9 compatible
```

### P4: Blob Parameters - Must Use `ref`, NOT `in`

Unity Entities Analyzer (EA0009) requires blob-stored types to use `ref` parameters:

```csharp
// ❌ COMPILE ERROR (EA0009) - 'in' not allowed for blob types
void Process(in ProjectileSpec spec) { }
int FindArc(in ShipLayoutBlob layout) { }

// ✅ CORRECT - Use 'ref' for all blob types
void Process(ref ProjectileSpec spec) { }
int FindArc(ref ShipLayoutBlob layout) { }

// Common blob types requiring 'ref':
// - ProjectileSpec, WeaponSpec (from catalogs)
// - ShipLayoutBlob, Curve1D
// - SpellEntry, LessonDefinitionBlob
// - Any type stored in BlobAssetReference<T>
```

### P5: Buffer Elements - Must Implement IBufferElementData

`DynamicBuffer<T>` can ONLY contain types implementing `IBufferElementData`:

```csharp
// ❌ COMPILE ERROR (CS0411 in generated code)
public struct ModuleState : IComponentData { }       // Wrong interface!
DynamicBuffer<ModuleState> modules;                  // Fails!

// ✅ CORRECT
[InternalBufferCapacity(8)]
public struct ModuleStateElement : IBufferElementData
{
    public float HP;
    public byte Destroyed;
}
DynamicBuffer<ModuleStateElement> modules;           // Works!
```

**Symptom**: CS0411 errors in `*__JobEntity_*.g.cs` generated files indicate wrong buffer element type.

### P6: Authoring Classes - Must Inherit MonoBehaviour

Unity DOTS `Baker<TAuthoringType>` requires the authoring type to inherit from `Component`:

```csharp
// ❌ COMPILE ERROR (CS0311)
public class MyCatalogAuthoring  // Missing inheritance!
{
    public class Baker : Baker<MyCatalogAuthoring> { }  // CS0311!
}

// ✅ CORRECT
public class MyCatalogAuthoring : MonoBehaviour
{
    public class Baker : Baker<MyCatalogAuthoring> { }  // Works!
}
```

### P7: Burst Parameters - Use `in` for Struct Parameters

When Burst calls helper methods, struct parameters should use `in`:

```csharp
// ❌ BURST ERROR (BC1064) - Structs by value in external calls
void ApplyModifiers(Entity entity, CombatStance stance) { }

// ✅ CORRECT - Use 'in' modifier
void ApplyModifiers(in Entity entity, in CombatStance stance) { }

// Primitives (int, float, uint) are fine by value
void Helper(in Entity e, int index, float value) { }  // OK
```

### P8: No Managed Code in Burst - String Operations Forbidden

**CRITICAL**: `String` operations are managed and CANNOT be used in Burst-compiled code:

```csharp
// ❌ BURST ERROR (BC1016) - All of these fail in Burst:
new FixedString64Bytes("literal");           // Managed constructor!
fixedString.ToString();                       // Managed method!
fixedString.Append("string");                // Managed overload!
someValue.ToString();                        // Managed!

// ✅ CORRECT - Pre-define constants outside Burst:
private static readonly FixedString64Bytes MyConstant = "my_value";
private static readonly FixedString32Bytes ReactorId = "reactor";

// Then use in Burst:
var name = MyConstant;                       // Just reference
if (moduleId.Equals(ReactorId)) { }          // Compare with constant
```

### P9: Building FixedStrings in Burst

When building dynamic strings in Burst, use ONLY Burst-compatible operations:

```csharp
// ✅ CORRECT - Burst-safe string building
FixedString64Bytes id = default;
id.Append((FixedString32Bytes)"prefix_");    // FixedString cast, not string
id.Append(tick);                              // Numbers work directly
id.Append('_');                               // Single chars work

// ❌ WRONG - All managed:
id.Append("string");                          // Managed!
id = new FixedString64Bytes("literal");       // Managed constructor!
```

### P10: Unity.Mathematics Import Required

When using `math.*`, `half`, `float2`, `float3`, etc.:

```csharp
// ❌ COMPILE ERROR (CS0103, CS0246)
var min = math.min(a, b);    // 'math' does not exist
half value = 0.5f;           // 'half' not found

// ✅ CORRECT - Add import at top of file:
using Unity.Mathematics;

// Now works:
var min = math.min(a, b);
half value = (half)0.5f;     // Note: explicit cast for half
```

### P11: Unsafe Operations - Require Import

When using `Unsafe.IsNullRef<T>()`, `Unsafe.NullRef<T>()`:

```csharp
// ❌ COMPILE ERROR (CS0103)
if (Unsafe.IsNullRef(ref spec)) { }  // 'Unsafe' does not exist

// ✅ CORRECT - Add import at top of file (OUTSIDE namespace):
using Unity.Collections.LowLevel.Unsafe;

// Now works:
if (Unsafe.IsNullRef(ref spec)) { }
return ref Unsafe.NullRef<ProjectileSpec>();
```

---

## Presentation / Camera Rules

### Simulation vs Presentation

Deterministic tick + rewind-safe sim lives in PureDOTS (Entities systems, TimeState, RewindState, registries, etc.).

Cameras, HUD, debug overlays, and tools live in game projects as non-deterministic presentation code.

### Access Pattern

Presentation can read from PureDOTS state via SystemAPI/queries or dedicated read-only components.

Presentation does not write directly to the sim; it issues commands or sets high-level intent in well-defined components if needed.

### Rewind & Time

Camera/HUD are not rewound; they follow the current sim state but are not recorded in time-travel history.

Any camera smoothing/lerping/easing uses Unity's Time.deltaTime or a presentation time source, not the fixed sim tick.

### Ownership

Camera rigs and HUDs belong to Space4X / Godgame repos, not to com.moni.puredots.

That's enough to prevent someone sneaking "GodHandSystem" into PureDOTS later.

### Shared Input Concepts

For tighter cross-game feel, PureDOTS defines an input "vocabulary", not specific implementations:

- **CameraMove** - 2D vector (X/Z plane movement)
- **CameraElevate** - float (up/down elevation)
- **CameraRotate** - 2D vector (yaw/pitch rotation)
- **CameraZoom** - float (zoom in/out)

Each game implements these using the new Input System in their own assets, mapping to whatever controls they want (WASD/QE/mouse in Space4X; drag/edge pan in Godgame).

---

## Pre-Commit Checklist

Before completing any task:

### Core Patterns
- [ ] **Build passes**: `dotnet build` or Unity domain reload succeeds
- [ ] **Dependencies exist**: All referenced types/properties verified via grep
- [ ] **No foreach mutation**: Buffer elements modified via indexed access only
- [ ] **Blob access uses ref**: All `blobRef.Value` accessed with `ref`
- [ ] **Explicit casts present**: Enum↔byte conversions have explicit casts
- [ ] **Rewind guards present**: Mutating systems check `RewindState.Mode`

### C# / Unity Compatibility
- [ ] **No `ref readonly`**: Use `ref` for returns, `in` for parameters (C# 9)
- [ ] **Blob params use `ref`**: Not `in` - EA0009 requires `ref` for blob types
- [ ] **Buffer elements correct**: Types in `DynamicBuffer<T>` implement `IBufferElementData`
- [ ] **Authoring inherits MonoBehaviour**: All Baker<T> authoring classes

### Burst Compliance
- [ ] **No managed strings in Burst**: No `new FixedString("literal")` or `.ToString()`
- [ ] **String constants pre-defined**: `static readonly FixedString64Bytes` outside Burst
- [ ] **Struct params use `in`**: Helper method parameters use `in Entity`, `in MyStruct`
- [ ] **Imports present**: `using Unity.Mathematics;`, `using Unity.Collections.LowLevel.Unsafe;`

### Presentation & Time
- [ ] **Presentation uses frame-time**: Camera & HUD code uses `Time.deltaTime`, not tick-time, unless deliberately aligned with deterministic tick (e.g., playback scrubbers)

---

## Common Error Quick Reference

| Error Code | Cause | Fix |
|------------|-------|-----|
| CS1654/CS1657 | Modifying foreach variable | Use indexed `for` loop |
| EA0001/EA0009 | Blob not accessed by ref | Use `ref var x = ref blob.Value` |
| CS0266 | Implicit enum↔byte | Add explicit cast `(byte)enum` |
| CS1031/CS1001 | `ref readonly` (C# 12) | Use `ref` or `in` (C# 9) |
| CS0411 | Buffer type inference | Check `IBufferElementData` interface |
| CS0311 | Baker authoring type | Inherit from `MonoBehaviour` |
| BC1064 | Struct by value in Burst | Use `in` modifier |
| BC1016 | Managed code in Burst | Pre-define string constants |
| CS0103 | `math`/`Unsafe` not found | Add using directive |
| CS0246 | `half` not found | `using Unity.Mathematics;` |

---

## Parallel Work Coordination

When multiple agents work in parallel:

1. **Shared types first**: Create components, enums, and structs before consumer systems
2. **Integration verification**: After parallel merges, run full build to catch dependency mismatches
3. **Communication**: Flag blockers immediately if dependencies are missing

---

## Documentation Sync Requirements

### Tri-Project Briefing

The `TRI_PROJECT_BRIEFING.md` file exists in **4 locations** and must stay synchronized:

- `C:\Users\Moni\Documents\claudeprojects\unity\TRI_PROJECT_BRIEFING.md` (Unity root)
- `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS\TRI_PROJECT_BRIEFING.md` (canonical)
- `C:\Users\Moni\Documents\claudeprojects\unity\Space4x\TRI_PROJECT_BRIEFING.md`
- `C:\Users\Moni\Documents\claudeprojects\unity\Godgame\TRI_PROJECT_BRIEFING.md`

**Sync command (run from unity folder):**
```bash
cp PureDOTS/TRI_PROJECT_BRIEFING.md . && cp PureDOTS/TRI_PROJECT_BRIEFING.md Space4x/ && cp PureDOTS/TRI_PROJECT_BRIEFING.md Godgame/
```

### When Adding New Error Patterns

1. Document pattern in this file (`Docs/FoundationGuidelines.md`)
2. Add to error table in `TRI_PROJECT_BRIEFING.md`
3. Sync briefing to all 4 locations
4. Update `README_BRIEFING.md` quick reference if needed

### Error Ownership by Project

| Project | Scope |
|---------|-------|
| **PureDOTS** | Core framework in `Packages/com.moni.puredots/` |
| **Space4X** | `C:\...\unity\Space4x\` - carrier, fleet, module systems |
| **Godgame** | `C:\...\unity\Godgame\` - villager, miracle, biome systems |

Game-specific errors should be fixed by the respective game team. Cross-project patterns should be documented here and synced.
