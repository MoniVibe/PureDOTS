# IL2CPP/AOT Compliance Audit

_Last updated: 2025-01-27_

## Purpose

This document catalogs IL2CPP/AOT hazards in PureDOTS runtime code, lists required `[Preserve]` attributes and `link.xml` entries, and provides a build checklist for IL2CPP validation.

## Reflection Usage Analysis

### Known Reflection Usage

#### 1. Bootstrap System Discovery
**Location**: `CoreSingletonBootstrapSystem.cs`  
**Usage**: Bootstrap systems may use reflection to discover registration methods  
**Status**: DOTS codegen handles ISystem preservation automatically  
**Action**: Verify Unity.Entities codegen preserves all ISystem types

#### 2. Enum Reflection (Debug Console)
**Location**: `RuntimeConfigConsoleBehaviour.cs`, debug command handlers  
**Usage**: `Enum.Parse()`, `Enum.GetValues()` for job types, resource types, miracle types  
**Risk**: Medium - enums may be stripped if not referenced statically  
**Action**: Add to `link.xml` (see below)

#### 3. Runtime Config Types
**Location**: `PureDotsRuntimeConfig`, `ResourceTypeCatalog`, `HistorySettingsData`  
**Usage**: ScriptableObject serialization may require reflection  
**Risk**: Low - Unity serialization system handles this  
**Action**: Verify ScriptableObject types are preserved

#### 4. Component Lookups
**Location**: Various systems using `ComponentLookup<T>`  
**Usage**: Generic type parameters  
**Risk**: Low - DOTS handles component type registration  
**Action**: No action needed (handled by DOTS)

### Preserved Types & Assemblies

#### Components Requiring Preservation

Add to `link.xml`:

```xml
<linker>
  <!-- Bootstrap and System Registration -->
  <assembly fullname="PureDOTS.Runtime">
    <type fullname="PureDOTS.Systems.CoreSingletonBootstrapSystem" preserve="all" />
    <type fullname="PureDOTS.Runtime.Registry.SystemRegistry" preserve="all" />
  </assembly>

  <!-- Runtime Config Types -->
  <assembly fullname="PureDOTS.Runtime">
    <type fullname="PureDOTS.Authoring.PureDotsRuntimeConfig" preserve="all" />
    <type fullname="PureDOTS.Authoring.ResourceTypeCatalog" preserve="all" />
    <type fullname="PureDOTS.Authoring.HistorySettingsData" preserve="all" />
    <type fullname="PureDOTS.Authoring.TimeSettingsData" preserve="all" />
  </assembly>

  <!-- Enum Types (for debug console) -->
  <assembly fullname="PureDOTS.Runtime">
    <type fullname="PureDOTS.Runtime.Components.VillagerJob" preserve="all" />
    <type fullname="PureDOTS.Runtime.Components.MiracleType" preserve="all" />
    <type fullname="PureDOTS.Runtime.Components.ResourceTier" preserve="all" />
    <type fullname="PureDOTS.Runtime.Components.HandState" preserve="all" />
    <type fullname="PureDOTS.Runtime.Components.RewindMode" preserve="all" />
  </assembly>

  <!-- Presentation Bridge Types -->
  <assembly fullname="PureDOTS.Runtime">
    <type fullname="PureDOTS.Runtime.Presentation.PresentationRegistryAsset" preserve="all" />
    <type fullname="PureDOTS.Runtime.Presentation.PresentationDescriptor" preserve="all" />
  </assembly>

  <!-- Unity.Entities assemblies (preserve all for now) -->
  <assembly fullname="Unity.Entities" preserve="all" />
  <assembly fullname="Unity.Entities.Hybrid" preserve="all" />
</linker>
```

### Burst-Compatible Code Audit

#### ✅ Burst-Safe Patterns

- All job structs use blittable types only
- No managed allocations in hot paths
- No reflection inside Burst jobs
- Generic parameters resolved at compile time

#### ⚠️ Areas Requiring Attention

1. **System State Queries**
   - `SystemAPI.HasSingleton<T>()` is Burst-safe
   - `SystemAPI.GetSingleton<T>()` is Burst-safe
   - All current usage verified safe

2. **Component Lookups**
   - `ComponentLookup<T>` is Burst-safe
   - Update calls must be outside Burst jobs (correctly implemented)

3. **Dynamic Buffer Access**
   - `DynamicBuffer<T>` is Burst-safe
   - All buffer operations verified safe

## IL2CPP Build Checklist

### Pre-Build Validation

- [ ] Verify `link.xml` exists at `Assets/Config/Linker/link.xml`
- [ ] Run EditMode tests to catch compilation errors
- [ ] Verify Burst compilation succeeds (check `Library/Bee/tmp/il2cppOutput/`)

### Player Settings

- [ ] **Scripting Backend**: IL2CPP
- [ ] **Api Compatibility Level**: `.NET Standard 2.1`
- [ ] **Managed Stripping Level**: Low (increase after validation)
- [ ] **Allow 'Unsafe' Code**: Enabled for PureDOTS.Runtime assembly
- [ ] **Burst AOT Compilation**: Enabled

### Build Steps

1. **Clean Build**
   ```bash
   # Clean Library and Temp directories
   rm -rf Library/ Temp/
   ```

2. **IL2CPP Build**
   ```bash
   "$UNITY_PATH" -batchmode -quit -projectPath PureDOTS \
     -buildTarget Windows64 \
     -executeMethod BuildScript.BuildIL2CPP \
     -logFile build/Logs/IL2CPP_Build.log
   ```

3. **Validate Build**
   - Check build log for `MissingMethodException` or `MissingTypeException`
   - Run bootstrap smoke test in IL2CPP build
   - Verify Burst compilation succeeded

### Post-Build Validation

- [ ] Run bootstrap scene in IL2CPP build
- [ ] Verify core systems initialize correctly
- [ ] Test rewind/playback functionality
- [ ] Check debug console enum parsing works
- [ ] Validate presentation bridge spawns work

### Common IL2CPP Issues & Fixes

#### Issue: MissingMethodException on enum reflection
**Symptom**: `Enum.Parse()` fails at runtime  
**Fix**: Add enum type to `link.xml` (see above)

#### Issue: MissingTypeException on ScriptableObject
**Symptom**: Asset loading fails  
**Fix**: Add ScriptableObject type to `link.xml`

#### Issue: Burst compilation fails
**Symptom**: Build succeeds but runtime crashes  
**Fix**: Check `Library/Bee/tmp/il2cppOutput/BurstDebugInformation_DoNotShip/` for errors

#### Issue: Generic type not found
**Symptom**: `MissingMethodException` on generic method call  
**Fix**: Add explicit static constructor or dummy usage to force code generation

## Automated Validation

### CI Integration

See `Docs/CI/CI_AutomationPlan.md` for automated IL2CPP build steps.

### Manual Testing

1. **Smoke Test**
   - Load bootstrap scene
   - Verify singletons created
   - Check debug HUD displays

2. **Integration Test**
   - Run playmode test suite in IL2CPP build
   - Verify registry systems work
   - Test spatial queries

3. **Stress Test**
   - Spawn 10k entities
   - Run for 1000 ticks
   - Monitor for crashes/errors

## Type Registration Requirements

### ISystem Types

Unity.Entities codegen automatically preserves ISystem types. No manual action needed.

### Component Types

DOTS handles component type registration. No manual action needed.

### Authoring Types

ScriptableObject types used at runtime must be preserved in `link.xml`.

### Enum Types

Enums used with reflection (debug console, serialization) must be preserved.

## Future Work

- [ ] Add automated IL2CPP build to CI pipeline
- [ ] Create runtime type registration helper for dynamic types
- [ ] Document platform-specific quirks (iOS, Android, Console)
- [ ] Add IL2CPP-specific performance profiling

## References

- `Docs/TruthSources/PlatformPerformance_TruthSource.md` - Platform policies
- `Docs/CI/CI_AutomationPlan.md` - Build automation
- Unity IL2CPP Documentation: https://docs.unity3d.com/Manual/IL2CPP.html
