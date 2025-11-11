# Linking External Game Projects to PureDOTS

This guide explains how to link external game projects (`Space4x` and `Godgame`) into the PureDOTS project so both can be used in the same scene while maintaining architectural separation.

## Overview

**PureDOTS Project Structure:**
```
PureDOTS/
├── Packages/com.moni.puredots/  (Framework - PureDOTS package)
├── Assets/Scripts/Space4x/      (Space4X code - can be local or symlinked)
└── Assets/Scripts/Godgame/      (Godgame code - can be local or symlinked)
```

**External Project Structure:**
```
Space4x/                         (External project)
└── Assets/Scripts/              (Space4X game code)

Godgame/                         (External project)
└── Assets/Scripts/              (Godgame game code)
```

## Method 1: Symlinks/Junctions (Recommended for Windows)

### Step 1: Remove Local Code (if exists)

If you have local copies in PureDOTS:
1. **Backup first!** Copy `Assets/Scripts/Space4x/` and `Assets/Scripts/Godgame/` elsewhere
2. Delete `Assets/Scripts/Space4x/` (except `.meta` files - keep those)
3. Delete `Assets/Scripts/Godgame/` (except `.meta` files - keep those)

### Step 2: Create Symlinks

**Using PowerShell (Run as Administrator):**

```powershell
# Navigate to PureDOTS Assets/Scripts directory
cd "C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS\Assets\Scripts"

# Create junction for Space4X
New-Item -ItemType Junction -Path "Space4x" -Target "C:\Users\Moni\Documents\claudeprojects\unity\Space4x\Assets\Scripts"

# Create junction for Godgame
New-Item -ItemType Junction -Path "Godgame" -Target "C:\Users\Moni\Documents\claudeprojects\unity\Godgame\Assets\Scripts"
```

**Using Command Prompt (Run as Administrator):**

```cmd
cd C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS\Assets\Scripts

mklink /J Space4x C:\Users\Moni\Documents\claudeprojects\unity\Space4x\Assets\Scripts
mklink /J Godgame C:\Users\Moni\Documents\claudeprojects\unity\Godgame\Assets\Scripts
```

### Step 3: Refresh Unity

1. Return to Unity Editor
2. Unity will detect the symlinks and import the external code
3. Assembly definitions from external projects will be recognized

## Method 2: Package References (For Packages Only)

If external projects are structured as Unity packages, add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.moni.puredots": "file:Packages/com.moni.puredots",
    "com.moni.space4x": "file:../Space4x/Packages/com.moni.space4x",
    "com.moni.godgame": "file:../Godgame/Packages/com.moni.godgame"
  }
}
```

**Note:** This only works if external projects expose packages. For full project linking, use symlinks.

## Method 3: Assembly Definition References

If external projects compile to DLLs, reference them in assembly definitions:

```json
{
  "name": "Space4X",
  "references": [
    "PureDOTS.Runtime",
    "PureDOTS.Systems",
    "Space4X.External"  // External DLL reference
  ]
}
```

## Verification

After linking, verify:

1. **Check Unity Console:**
   - No missing script errors
   - External assemblies compile successfully

2. **Check Assembly Definitions:**
   - `Space4X.asmdef` references PureDOTS assemblies ✅
   - `Godgame.*.asmdef` references PureDOTS assemblies ✅
   - PureDOTS assemblies do NOT reference game assemblies ✅

3. **Check Namespaces:**
   - Space4X code uses `Space4X.*` namespaces
   - Godgame code uses `Godgame.*` namespaces
   - No cross-game dependencies

## Assembly Definition Setup

### Space4X Assembly (`Assets/Scripts/Space4x/Space4X.asmdef`)

```json
{
  "name": "Space4X",
  "rootNamespace": "Space4X",
  "references": [
    "Unity.Entities",
    "Unity.Entities.Hybrid",
    "Unity.Mathematics",
    "Unity.Burst",
    "Unity.Collections",
    "Unity.Transforms",
    "Unity.InputSystem",
    "PureDOTS.Runtime",
    "PureDOTS.Systems"
  ]
}
```

### Godgame Assembly (`Assets/Scripts/Godgame/Godgame.*.asmdef`)

```json
{
  "name": "Godgame.Runtime",  // Or Godgame.Authoring, etc.
  "rootNamespace": "Godgame",
  "references": [
    "Unity.Entities",
    "Unity.Entities.Hybrid",
    "Unity.Mathematics",
    "Unity.Burst",
    "Unity.Collections",
    "Unity.Transforms",
    "PureDOTS.Runtime",
    "PureDOTS.Systems"
  ]
}
```

**Important:** PureDOTS assemblies must NOT reference Space4X or Godgame assemblies!

## Scene Setup with External Projects

When external projects are linked:

1. **Root Scene** (PureDOTS):
   - Contains framework setup (`PureDotsConfigAuthoring`, `TimeControlsAuthoring`)
   - Uses PureDOTS package systems

2. **Space4X SubScene**:
   - Uses `Space4X.*` authoring components from external project
   - Systems from external Space4X project run automatically
   - Components from `Space4X.Runtime.Transport` namespace

3. **Godgame SubScene**:
   - Uses `Godgame.*` authoring components from external project
   - Systems from external Godgame project run automatically
   - Components from `Godgame.*` namespaces

## Troubleshooting

### Symlink Not Working

- **Issue:** Unity doesn't see external files
- **Fix:** Ensure symlink was created correctly, restart Unity

### Assembly Definition Errors

- **Issue:** External assemblies can't find PureDOTS
- **Fix:** Ensure external projects' `.asmdef` files reference `PureDOTS.Runtime` and `PureDOTS.Systems`

### Circular Dependencies

- **Issue:** PureDOTS trying to reference game assemblies
- **Fix:** Check PureDOTS `.asmdef` files - they must NOT reference Space4X or Godgame

### Missing Authoring Components

- **Issue:** Can't find `MiningVesselAuthoring` or `VillagerAuthoring`
- **Fix:** Ensure external projects are properly linked and their assemblies are compiled

## Best Practices

1. **Version Control:**
   - Don't commit symlinks to git (they're platform-specific)
   - Document symlink setup in README
   - Use `.gitignore` to exclude symlinked folders if needed

2. **Development Workflow:**
   - Edit code in external projects
   - PureDOTS automatically picks up changes
   - Test in PureDOTS scene

3. **Build Process:**
   - External projects should build independently
   - PureDOTS can reference them for demo scenes
   - Production builds may need different setup

4. **Separation:**
   - Keep game-specific code in external projects
   - PureDOTS only contains framework code
   - No cross-game dependencies

## Alternative: Git Submodules

If projects are in separate repos:

```bash
# In PureDOTS project
git submodule add <space4x-repo-url> Assets/Scripts/Space4x
git submodule add <godgame-repo-url> Assets/Scripts/Godgame

# Update submodules
git submodule update --init --recursive
```

This keeps projects in sync but requires git submodule management.








