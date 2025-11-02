# Creating a Dual Mining Demo Scene

This guide explains how to create a scene that showcases both **Space4X** (vessel mining) and **godgame** (villager mining) loops side-by-side while maintaining strict Game/DOTS separation conventions.

## Project Structure Options

### Option 1: External Projects (Recommended)

The PureDOTS project can reference external game projects:
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x`
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame`

These external projects contain their own game-specific code and can be linked into PureDOTS using symlinks/junctions.

### Option 2: Embedded Code (Current)

Currently, Space4X code exists locally in `Assets/Scripts/Space4x/`. This works for development but external projects are preferred for proper separation.

## Linking External Projects

To use code from external game projects, create symlinks (Windows junctions) to link the external project folders into PureDOTS:

## Architecture Overview

```
Root Scene (Main.unity)
├── PureDOTS Framework Setup
│   ├── PureDotsConfig GameObject (framework config)
│   └── TimeControls GameObject (framework controls)
│
├── Space4X SubScene (Space4X_Entities.unity)
│   ├── Mining Vessels (MiningVesselAuthoring from external Space4X project)
│   ├── Carriers (CarrierAuthoring from external Space4X project)
│   └── Asteroids (ResourceSourceAuthoring from PureDOTS)
│
└── Godgame SubScene (Godgame_Entities.unity)
    ├── Villagers (VillagerAuthoring from external Godgame project)
    ├── Storehouses (StorehouseAuthoring from PureDOTS)
    └── Resource Nodes (ResourceSourceAuthoring from PureDOTS)
```

**Code Sources:**
- **PureDOTS Framework**: `Packages/com.moni.puredots/` (local package)
- **Space4X Game Code**: External project at `C:\Users\Moni\Documents\claudeprojects\unity\Space4x`
- **Godgame Code**: External project at `C:\Users\Moni\Documents\claudeprojects\unity\Godgame`

**Setup Required:** Link external projects via symlinks (see `LinkingExternalGameProjects.md`)

## Key Principles

### ✅ CORRECT Separation

1. **PureDOTS Framework** (Root Scene):
   - `PureDotsConfigAuthoring` - Framework configuration
   - `TimeControlsAuthoring` - Framework time controls
   - Generic systems from PureDOTS package

2. **Space4X Game** (Space4X SubScene):
   - `MiningVesselAuthoring` - Game-specific vessel authoring
   - `CarrierAuthoring` - Game-specific carrier authoring
   - Uses `Space4X.Runtime.Transport` components
   - Systems: `VesselMovementSystem`, `VesselAISystem`, etc.

3. **Godgame** (Godgame SubScene):
   - `VillagerAuthoring` - From external Godgame project
   - `StorehouseAuthoring` - Framework storehouse authoring (PureDOTS)
   - Uses `Godgame.*` components from external project
   - Systems: `VillagerMovementSystem`, `VillagerAISystem`, etc. from external Godgame project

### ❌ VIOLATIONS to Avoid

- Don't put Space4X-specific components in the godgame SubScene
- Don't put godgame-specific components in the Space4X SubScene
- Don't mix game-specific authoring in the root scene
- Don't create cross-game dependencies

## Prerequisites

**Before creating the scene, link external projects:**

1. **Link External Projects** (See `LinkingExternalGameProjects.md` for details):
   ```powershell
   # Run PowerShell as Administrator
   cd "C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS\Assets\Scripts"
   New-Item -ItemType Junction -Path "Space4x" -Target "C:\Users\Moni\Documents\claudeprojects\unity\Space4x\Assets\Scripts"
   New-Item -ItemType Junction -Path "Godgame" -Target "C:\Users\Moni\Documents\claudeprojects\unity\Godgame\Assets\Scripts"
   ```

2. **Verify in Unity:**
   - External project assemblies compile
   - No missing script errors
   - Both projects' authoring components are available

## Step-by-Step Setup

### 1. Create Root Scene

1. Create new scene: `Assets/Scenes/MiningDemo_Dual.unity`
2. Add framework setup:
   - Run menu: `Space4X > Fix Complete Scene Setup`
   - This creates:
     - `PureDotsConfig` GameObject with `PureDotsConfigAuthoring`
     - SubScene structure (if needed)

### 2. Create Space4X SubScene

1. In Hierarchy, right-click → **New Sub Scene** → **Empty Scene**
2. Name it `Space4X_Entities` and save as `Assets/Scenes/SubScenes/Space4X_Entities.unity`
3. **Open the SubScene** (double-click to edit it)
4. Add Space4X entities:
   - Mining Vessels (GameObjects with `MiningVesselAuthoring`)
   - Carriers (GameObjects with `CarrierAuthoring`)
   - Asteroids (GameObjects with `ResourceSourceAuthoring` with `resourceTypeId="ore"`)
5. **Close the SubScene** (click "←" arrow to return to root)
6. Ensure SubScene component has `AutoLoadScene = true`

### 3. Create Godgame SubScene

1. In Hierarchy, right-click → **New Sub Scene** → **Empty Scene**
2. Name it `Godgame_Entities` and save as `Assets/Scenes/SubScenes/Godgame_Entities.unity`
3. **Open the SubScene** (double-click to edit it)
4. Add godgame entities:
   - Villagers (GameObjects with `VillagerAuthoring`)
   - Storehouses (GameObjects with `StorehouseAuthoring`)
   - Resource Nodes (GameObjects with `ResourceSourceAuthoring` with `resourceTypeId="wood"`)
5. **Close the SubScene** (click "←" arrow to return to root)
6. Ensure SubScene component has `AutoLoadScene = true`

### 4. Spatial Organization

**Recommended Layout:**
```
                    Root Scene View
┌─────────────────────────────────────────────┐
│                                             │
│  ┌─────────────────┐  ┌─────────────────┐  │
│  │  Space4X Zone   │  │  Godgame Zone    │  │
│  │                 │  │                 │  │
│  │  Vessels        │  │  Villagers       │  │
│  │  Carriers       │  │  Storehouses     │  │
│  │  Asteroids      │  │  Trees           │  │
│  │                 │  │                 │  │
│  └─────────────────┘  └─────────────────┘  │
│                                             │
└─────────────────────────────────────────────┘
```

- **Space4X Zone**: X = -20 to 0, Z = -10 to 10
- **Godgame Zone**: X = 0 to 20, Z = -10 to 10
- Keep zones separated to avoid visual confusion

### 5. Run Automated Setup

Use the helper menu:
- `Space4X > Setup Dual Mining Demo Scene`

This will:
- Create both SubScenes if missing
- Add authoring components to GameObjects
- Configure resource types correctly
- Set up storehouses/carriers with proper capacity

## Component Mapping

### Space4X Components (Space4X SubScene Only)

| GameObject Component | ECS Component | System |
|---------------------|---------------|--------|
| `MiningVesselAuthoring` | `MinerVessel` + `VesselMovement` + `VesselAIState` | `VesselMovementSystem`, `VesselAISystem` |
| `CarrierAuthoring` | `Carrier` | `VesselDepositSystem` |
| `ResourceSourceAuthoring` (ore) | `ResourceSourceState` | `ResourceRegistrySystem` (PureDOTS) |

### Godgame Components (Godgame SubScene Only)

| GameObject Component | ECS Component | System |
|---------------------|---------------|--------|
| `VillagerAuthoring` | `VillagerMovement` + `VillagerAIState` | `VillagerMovementSystem`, `VillagerAISystem` |
| `StorehouseAuthoring` | `Storehouse` | `ResourceDepositSystem` (PureDOTS) |
| `ResourceSourceAuthoring` (wood) | `ResourceSourceState` | `ResourceRegistrySystem` (PureDOTS) |

### Shared Framework Components (Both SubScenes)

- `ResourceSourceAuthoring` - Framework component (PureDOTS)
- `ResourceRegistrySystem` - Framework system (PureDOTS)
- `TimeState`, `RewindState` - Framework singletons (PureDOTS)

## Testing the Setup

### Play Mode Checklist

1. **Before Play Mode:**
   - ✅ Both SubScenes are **CLOSED** (not open for editing)
   - ✅ SubScene components have `AutoLoadScene = true`
   - ✅ `PureDotsConfig` GameObject exists in root scene

2. **During Play Mode:**
   - Check console for initialization messages
   - Verify both systems are running:
     - `[VesselMovementSystem] Found X vessels`
     - `[VillagerMovementSystem] Found X villagers`
   - Watch entities move and gather resources

3. **Visual Verification:**
   - Space4X zone: Vessels should mine asteroids and deposit to carriers
   - Godgame zone: Villagers should gather wood and deposit to storehouses

## Troubleshooting

### Entities Not Appearing

- **Issue**: SubScene is open during Play Mode
- **Fix**: Close the SubScene (click "←" in Hierarchy)

### Systems Not Running

- **Issue**: Missing `PureDotsConfigAuthoring` in root scene
- **Fix**: Run `Space4X > Fix Complete Scene Setup`

### Components Not Converting

- **Issue**: GameObjects not in a SubScene
- **Fix**: Move GameObjects into the appropriate SubScene

### Wrong Namespace/Assembly

- **Issue**: Using `PureDOTS.Runtime.Transport` in Space4X
- **Fix**: Use `Space4X.Runtime.Transport` in Space4X SubScene only

## Best Practices

1. **SubScene Organization:**
   - One SubScene per game domain
   - Keep root scene minimal (framework only)
   - Use descriptive SubScene names

2. **Naming Conventions:**
   - Space4X entities: `MiningVessel_01`, `Carrier_Main`, `Asteroid_Ore_01`
   - Godgame entities: `Villager_01`, `Storehouse_Main`, `Tree_01`

3. **Resource Type IDs:**
   - Space4X: Use `"ore"` for asteroids
   - Godgame: Use `"wood"` for trees
   - Both: Can share resource types (PureDOTS framework handles this)

4. **Documentation:**
   - Comment SubScenes with their purpose
   - Use GameObject names that indicate their game domain

## Example Scene Structure

```
MiningDemo_Dual (Root Scene)
├── PureDotsConfig (PureDotsConfigAuthoring)
├── TimeControls (TimeControlsAuthoring)
├── MainCamera
├── Directional Light
│
├── Space4X_SubScene (SubScene component)
│   └── References: Assets/Scenes/SubScenes/Space4X_Entities.unity
│
└── Godgame_SubScene (SubScene component)
    └── References: Assets/Scenes/SubScenes/Godgame_Entities.unity
```

## Automated Setup Tool

### Prerequisites Check

Before running setup, verify external projects are linked:
- **`Space4X > Check External Project Links`** - Verifies symlinks are set up correctly

### Setup Scene

Use the menu item:
- **`Space4X > Setup Dual Mining Demo Scene`**

This tool will:
1. Create root scene if needed
2. Create both SubScenes
3. Add framework setup (PureDotsConfig, TimeControls)
4. Create sample entities in each SubScene
5. Configure resource types correctly
6. Set up storehouses/carriers with capacity

**Note:** The setup tool will use:
- Space4X authoring components from external project (if linked) or local code
- Godgame authoring components from external project (if linked) or PureDOTS framework components

## Next Steps

After setup:
1. Customize entity positions in their respective SubScenes
2. Adjust resource amounts and gather rates
3. Add more entities as needed
4. Test both mining loops independently

