# Godgame Prefab Creation Checklist

This document provides step-by-step instructions for creating all prefabs needed for the Godgame legacy.

**Related Documentation:**
- [Space4X Prefab Checklist](./Space4X_PrefabChecklist.md) - For comparison with Space4X's data-only entity prefabs
- [Godgame Prefab Build Summary](./Godgame_PrefabBuild_Summary.md) - Implementation summary and next steps

## Overview

Each prefab should be created in Unity Editor by:
1. Creating an empty GameObject
2. Adding the required authoring components
3. Configuring component properties
4. Adding placeholder visual mesh (optional but recommended)
5. Saving as prefab in the appropriate directory

## Prefab Directory Structure

```
Godgame/Assets/Prefabs/
├── Systems/          (Bootstrap, Camera, Divine Hand)
├── Villagers/       (Civilian, Combat variants)
├── Buildings/       (VillageCenter, House, Storehouse, WorshipSite, ConstructionSite)
├── Resources/       (Resource nodes, Processors)
├── Vegetation/      (Trees, Crops, Shrubs)
└── Factions/        (Band spawners, Patrol anchors, Diplomacy markers)
```

---

## 1. Bootstrap Prefabs

### 1.1 SimulationBootstrap.prefab
**Location:** `Godgame/Assets/Prefabs/Systems/`

**Components:**
- `PureDotsConfigAuthoring` - Assign a `PureDotsRuntimeConfig` asset (create if needed)
- `TimeSettingsAuthoring` - Configure time settings
- `HistorySettingsAuthoring` - Configure history/rewind settings
- `SceneSpawnAuthoring` - Assign a `SceneSpawnProfileAsset` (created later)

**Settings:**
- `PureDotsConfigAuthoring.config`: Create or assign `Godgame/Assets/Data/GodgameConfig.asset`
- `TimeSettingsAuthoring.fixedDeltaTime`: 0.016667 (60 FPS)
- `TimeSettingsAuthoring.defaultSpeedMultiplier`: 1.0
- `TimeSettingsAuthoring.pauseOnStart`: false
- `SceneSpawnAuthoring.profile`: Assign after creating SceneSpawnProfile

**Placeholder Visual:** Optional - simple cube mesh with `PlaceholderVisualAuthoring` (Kind: Crate)

---

### 1.2 CameraRig.prefab
**Location:** `Godgame/Assets/Prefabs/Systems/`

**Components:**
- `BW2StyleCameraController` (from `PureDOTS.Runtime.Camera`)
- `HandCameraInputRouter` (from `PureDOTS.Runtime.Input`)
- `Camera` (Unity default)
- `PlaceholderVisualAuthoring` (optional debug gizmo)

**Settings:**
- Configure camera position/rotation as needed
- `BW2StyleCameraController.inputRouter`: Reference to HandCameraInputRouter component

**Note:** This prefab should be instantiated in the scene at runtime, not baked into SubScene.

---

### 1.3 DivineHand.prefab
**Location:** `Godgame/Assets/Prefabs/Systems/`

**Components:**
- `DivineHandAuthoring`
- `DivineHandInputBridge` (runtime MonoBehaviour, not baked)

**DivineHandAuthoring Settings:**
- `pickupRadius`: 8.0
- `maxGrabDistance`: 60.0
- `holdLerp`: 0.25
- `throwImpulse`: 20.0
- `throwChargeMultiplier`: 12.0
- `holdHeightOffset`: 4.0
- `cooldownAfterThrowSeconds`: 0.35
- `minChargeSeconds`: 0.15
- `maxChargeSeconds`: 1.25
- `hysteresisFrames`: 3
- `heldCapacity`: 500
- `siphonUnitsPerSecond`: 50.0
- `dumpUnitsPerSecond`: 150.0
- `initialCursorWorldPosition`: (0, 12, 0)
- `initialAimDirection`: (0, -1, 0)

**Note:** `DivineHandInputBridge` is a runtime MonoBehaviour that should be added to the scene GameObject that hosts the DivineHand entity, not baked into the prefab.

**Placeholder Visual:** Optional - small sphere with `PlaceholderVisualAuthoring` (Kind: Miracle)

---

## 2. Villager Prefabs

### 2.1 Villager.prefab (Civilian)
**Location:** `Godgame/Assets/Prefabs/Villagers/`

**Components:**
- `VillagerAuthoring`
- `PlaceholderVisualAuthoring`
- `CapsuleCollider` (for physics interaction)
- Mesh renderer with simple capsule mesh

**VillagerAuthoring Settings:**
- `villagerId`: -1 (auto-assigned)
- `factionId`: 0 (neutral/player faction)
- `initialHealth`: 100
- `maxHealth`: 100
- `initialHunger`: 20
- `initialEnergy`: 80
- `initialMorale`: 75
- `baseSpeed`: 3.0
- `visionRange`: 20
- `hearingRange`: 15
- `initialJob`: None
- `initialDiscipline`: Unassigned
- `initialDisciplineLevel`: 0
- `initialMood`: 50
- `moodChangeRate`: 1.0
- `startAvailableForJobs`: true
- `isCombatCapable`: false

**PlaceholderVisualAuthoring Settings:**
- `kind`: Crate (or create custom villager visual)
- `baseScale`: 1.0
- `baseColor`: Light skin tone (0.85, 0.75, 0.65, 1.0)

---

### 2.2 Villager_Combat.prefab
**Location:** `Godgame/Assets/Prefabs/Villagers/`

**Components:** Same as Villager.prefab

**VillagerAuthoring Settings:** Copy from Villager.prefab, then:
- `isCombatCapable`: true
- `attackDamage`: 15
- `attackSpeed`: 1.5
- `defenseRating`: 20
- `attackRange`: 2.5
- `initialJob`: Guard (if available) or None

**PlaceholderVisualAuthoring Settings:**
- `baseColor`: Slightly darker/more saturated (0.7, 0.6, 0.5, 1.0) to differentiate

---

### 2.3 VillagerSpawner.prefab
**Location:** `Godgame/Assets/Prefabs/Villagers/`

**Components:**
- `VillagerSpawnerAuthoring`
- `PlaceholderVisualAuthoring` (optional - invisible sphere)

**VillagerSpawnerAuthoring Settings:**
- `villagerPrefab`: Reference to `Villager.prefab`
- `initialPopulation`: 4
- `spawnRadius`: 10
- `maxPopulation`: 50
- `reproductionRate`: 0.01

**PlaceholderVisualAuthoring Settings:**
- `kind`: Crate
- `baseScale`: 0.5 (small spawn indicator)

---

## 3. Village Building Prefabs

### 3.1 VillageCenter.prefab
**Location:** `Godgame/Assets/Prefabs/Buildings/`

**Components:**
- `VillageCenterAuthoring`
- `PlaceholderVisualAuthoring`
- Simple mesh (cube or custom building mesh)

**VillageCenterAuthoring Settings:**
- `villageId`: -1 (auto-assigned)
- `factionId`: 0
- `maxPopulation`: 50
- `spawnRadius`: 20
- `villagerPrefab`: Reference to `Villager.prefab`
- `initialAlignment`: 50
- `initialCohesion`: 50
- `initialInitiative`: 50
- `residencyQuota`: 100
- `residencyRange`: 30

**PlaceholderVisualAuthoring Settings:**
- `kind`: Crate
- `baseScale`: 2.0 (larger than houses)
- `baseColor`: (0.9, 0.8, 0.7, 1.0) - beige/tan

---

### 3.2 House.prefab
**Location:** `Godgame/Assets/Prefabs/Buildings/`

**Components:**
- `HousingAuthoring`
- `PlaceholderVisualAuthoring`
- Simple cube mesh

**HousingAuthoring Settings:**
- `maxResidents`: 4
- `restBonusMultiplier`: 1.2
- `comfortLevel`: 50
- `temperatureBonus`: 5
- `energyRestoreRate`: 2.0
- `moraleRestoreRate`: 0.5

**PlaceholderVisualAuthoring Settings:**
- `kind`: Crate
- `baseScale`: 1.5
- `baseColor`: (0.7, 0.6, 0.5, 1.0) - brown

---

### 3.3 Storehouse.prefab
**Location:** `Godgame/Assets/Prefabs/Buildings/`

**Components:**
- `StorehouseAuthoring`
- `PlaceholderVisualAuthoring`
- Simple cube mesh

**StorehouseAuthoring Settings:**
- `shredRate`: 1.0
- `maxShredQueueSize`: 8
- `inputRate`: 10.0
- `outputRate`: 10.0
- `capacities`: Add entries for:
  - "Wood": 1000
  - "Stone": 1000
  - "Food": 500
  - "Tools": 100

**PlaceholderVisualAuthoring Settings:**
- `kind`: Barrel
- `baseScale`: 1.8
- `baseColor`: (0.6, 0.5, 0.4, 1.0) - dark brown

---

### 3.4 WorshipSite.prefab
**Location:** `Godgame/Assets/Prefabs/Buildings/`

**Components:**
- `WorshipSiteAuthoring`
- `PlaceholderVisualAuthoring`
- Simple mesh (cylinder or custom shrine mesh)

**WorshipSiteAuthoring Settings:**
- `manaGenerationRate`: 1.0
- `influenceRange`: 10
- `maxMana`: 100
- `isActive`: true
- `maxWorshippers`: 5
- `worshipBonusMultiplier`: 1.5
- `canStoreMana`: true
- `storageCapacity`: 1000

**PlaceholderVisualAuthoring Settings:**
- `kind`: Miracle
- `baseScale`: 1.5
- `baseColor`: (0.6, 0.85, 1.2, 1.0) - light blue
- `miracleBaseIntensity`: 1.0
- `miraclePulseAmplitude`: 0.35
- `miraclePulseSpeed`: 2.5
- `miracleGlowColor`: (0.6, 0.85, 1.2, 1.0)

---

### 3.5 ConstructionSite.prefab (Optional)
**Location:** `Godgame/Assets/Prefabs/Buildings/`

**Components:**
- `ConstructionSiteAuthoring`
- `PlaceholderVisualAuthoring`
- Simple mesh (half-built appearance)

**ConstructionSiteAuthoring Settings:**
- `cost`: Add entries for required resources:
  - "Wood": 50 units
  - "Stone": 30 units
  - "Food": 10 units (optional)
- `requiredProgress`: 100.0
- `currentProgress`: 0.0
- `completionPrefab`: Reference to the finished building prefab (e.g., House.prefab)
- `destroySiteOnComplete`: true
- `siteIdOverride`: 0 (auto-assigned)

**PlaceholderVisualAuthoring Settings:**
- `kind`: Crate
- `baseScale`: 1.0
- `baseColor`: (0.5, 0.5, 0.5, 0.7) - semi-transparent gray

---

## 4. Resource & Logistics Prefabs

### 4.1 Resource_Wood.prefab
**Location:** `Godgame/Assets/Prefabs/Resources/`

**Components:**
- `ResourceSourceAuthoring`
- `PlaceholderVisualAuthoring`
- Simple mesh (tree stump or log)

**ResourceSourceAuthoring Settings:**
- `resourceTypeId`: "Wood"
- `initialUnits`: 100
- `gatherRatePerWorker`: 2.0
- `maxSimultaneousWorkers`: 3
- `debugGatherRadius`: 3
- `infinite`: false
- `respawns`: true
- `respawnSeconds`: 60
- `handUprootAllowed`: true

**PlaceholderVisualAuthoring Settings:**
- `kind`: Crate
- `baseScale`: 1.0
- `baseColor`: (0.4, 0.3, 0.2, 1.0) - dark brown

---

### 4.2 Resource_Stone.prefab
**Location:** `Godgame/Assets/Prefabs/Resources/`

**Components:** Same as Resource_Wood.prefab

**ResourceSourceAuthoring Settings:**
- `resourceTypeId`: "Stone"
- `initialUnits`: 150
- `gatherRatePerWorker`: 1.5
- `maxSimultaneousWorkers`: 2
- `debugGatherRadius`: 2.5
- `respawnSeconds`: 120

**PlaceholderVisualAuthoring Settings:**
- `baseColor`: (0.6, 0.6, 0.6, 1.0) - gray

---

### 4.3 Resource_Food.prefab
**Location:** `Godgame/Assets/Prefabs/Resources/`

**Components:** Same as Resource_Wood.prefab

**ResourceSourceAuthoring Settings:**
- `resourceTypeId`: "Food"
- `initialUnits`: 50
- `gatherRatePerWorker`: 3.0
- `maxSimultaneousWorkers`: 4
- `debugGatherRadius`: 2
- `respawnSeconds`: 30

**PlaceholderVisualAuthoring Settings:**
- `baseColor`: (0.8, 0.6, 0.4, 1.0) - orange/yellow

---

### 4.4 ResourceProcessor.prefab (Optional)
**Location:** `Godgame/Assets/Prefabs/Resources/`

**Components:**
- `ResourceProcessorAuthoring`
- `PlaceholderVisualAuthoring`

**Settings:**
- Configure processing recipes and rates as needed

**PlaceholderVisualAuthoring Settings:**
- `kind`: Barrel
- `baseScale`: 1.5
- `baseColor`: (0.5, 0.5, 0.5, 1.0) - gray

---

## 5. Vegetation & Environment Prefabs

### 5.1 Tree.prefab
**Location:** `Godgame/Assets/Prefabs/Vegetation/`

**Components:**
- `VegetationAuthoring`
- `PlaceholderVisualAuthoring`
- Simple mesh (capsule or custom tree)

**VegetationAuthoring Settings:**
- `useCatalog`: true
- `catalog`: Reference to `VegetationSpeciesCatalog` asset
- `catalogSpeciesIndex`: 0 (Tree)
- `initialStage`: Seedling
- `growthRate`: 0.5
- `maxHealth`: 100
- `initialHealth`: 50
- `initialWaterLevel`: 50
- `initialLightLevel`: 75
- `initialSoilQuality`: 60
- `resourceTypeId`: "Wood"
- `productionRate`: 1.0
- `maxProductionCapacity`: 10
- `harvestCooldown`: 60
- `waterConsumptionRate`: 0.1
- `nutrientConsumptionRate`: 0.05
- `energyProductionRate`: 0.2
- `reproductionCooldown`: 300
- `spreadRange`: 5
- `spreadChance`: 0.1
- `maxOffspringRadius`: 2
- `frostResistance`: 0.5
- `droughtResistance`: 0.5

**PlaceholderVisualAuthoring Settings:**
- `kind`: Vegetation
- `baseScale`: 1.0
- `baseColor`: (0.2, 0.6, 0.2, 1.0) - green
- `seedlingScale`: 0.25
- `growingScale`: 0.55
- `matureScale`: 1.0
- `fruitingScale`: 1.15
- `dyingScale`: 0.8
- `deadScale`: 0.45
- `scaleLerpSeconds`: 0.15

---

### 5.2 Crop.prefab
**Location:** `Godgame/Assets/Prefabs/Vegetation/`

**Components:** Same as Tree.prefab

**VegetationAuthoring Settings:**
- `catalogSpeciesIndex`: 3 (Crop)
- `initialStage`: Seedling
- `growthRate`: 1.0 (faster than trees)
- `resourceTypeId`: "Food"
- `harvestCooldown`: 30 (shorter than trees)

**PlaceholderVisualAuthoring Settings:**
- `baseColor`: (0.4, 0.7, 0.2, 1.0) - lighter green
- `baseScale`: 0.5 (smaller than trees)

---

### 5.3 Shrub.prefab
**Location:** `Godgame/Assets/Prefabs/Vegetation/`

**Components:** Same as Tree.prefab

**VegetationAuthoring Settings:**
- `catalogSpeciesIndex`: 1 (Shrub)
- `initialStage`: Mature
- `growthRate`: 0.8
- `resourceTypeId`: "Wood" (or custom)

**PlaceholderVisualAuthoring Settings:**
- `baseColor`: (0.3, 0.5, 0.2, 1.0) - darker green
- `baseScale`: 0.7

---

### 5.4 ClimateGrid.prefab
**Location:** `Godgame/Assets/Prefabs/Vegetation/`

**Components:**
- `ClimateProfile` (ScriptableObject reference)
- `EnvironmentGridConfig`
- `PlaceholderVisualAuthoring` (optional - invisible)

**Settings:**
- Configure grid dimensions and cell size
- Assign climate profile asset

---

### 5.5 RainCloud.prefab
**Location:** `Godgame/Assets/Prefabs/Vegetation/`

**Components:**
- `RainCloudAuthoring`
- `PlaceholderVisualAuthoring`

**RainCloudAuthoring Settings:**
- Configure cloud movement, moisture generation, and lifetime

**PlaceholderVisualAuthoring Settings:**
- `kind`: Miracle
- `baseScale`: 3.0 (large)
- `baseColor`: (0.5, 0.5, 0.7, 0.8) - semi-transparent gray-blue

---

### 5.6 RainMiracle.prefab
**Location:** `Godgame/Assets/Prefabs/Vegetation/`

**Components:**
- `RainMiracleAuthoring`
- `PlaceholderVisualAuthoring`

**Settings:**
- Configure miracle parameters (area, duration, intensity)

**PlaceholderVisualAuthoring Settings:**
- `kind`: Miracle
- `baseScale`: 2.0
- `baseColor`: (0.4, 0.6, 1.0, 1.0) - bright blue

---

## 6. Faction & Band Prefabs

### 6.1 BandSpawner.prefab
**Location:** `Godgame/Assets/Prefabs/Factions/`

**Components:**
- `SceneSpawnAuthoring`
- `PlaceholderVisualAuthoring` (optional)

**SceneSpawnAuthoring Settings:**
- `profile`: Reference to band spawn profile (created separately)
- `seedOverride`: 0 (use profile seed)
- `seedOffset`: 0

**Note:** Create a separate `SceneSpawnProfileAsset` for band spawning with combat villager prefabs.

---

### 6.2 PatrolAnchor.prefab
**Location:** `Godgame/Assets/Prefabs/Factions/`

**Components:**
- `SpawnerAuthoring` (if available) or custom waypoint component
- `PlaceholderVisualAuthoring`

**Settings:**
- Configure patrol waypoints and radius
- Link to band spawner

**PlaceholderVisualAuthoring Settings:**
- `kind`: Crate
- `baseScale`: 0.3 (small marker)
- `baseColor`: (1.0, 0.0, 0.0, 0.5) - semi-transparent red

---

### 6.3 DiplomacyMarker.prefab
**Location:** `Godgame/Assets/Prefabs/Factions/`

**Components:**
- `CultureAuthoring`
- `AreaEffectAuthoring`
- `FactionAuthoring`
- `PlaceholderVisualAuthoring`

**Settings:**
- Configure faction ID and relations
- Set influence area and effects

**PlaceholderVisualAuthoring Settings:**
- `kind`: Miracle
- `baseScale`: 1.0
- `baseColor`: (1.0, 1.0, 0.0, 0.7) - semi-transparent yellow

---

## 7. Support Assets

### 7.1 VegetationSpeciesCatalog.asset
**Location:** `Godgame/Assets/Data/`

**Creation:**
1. Right-click in `Godgame/Assets/Data/`
2. Create → PureDOTS → Vegetation Species Catalog
3. Add species entries:
   - Tree (index 0)
   - Shrub (index 1)
   - Grass (index 2)
   - Crop (index 3)
   - Flower (index 4)
   - Fungus (index 5)

**Settings:** Configure growth rates, resource yields, and environmental needs per species.

---

### 7.2 SceneSpawnProfile.asset
**Location:** `Godgame/Assets/Data/`

**Creation:**
1. Right-click in `Godgame/Assets/Data/`
2. Create → PureDOTS → Scene Spawn Profile
3. Name it `GodgameDemoSpawnProfile`

**Entries to Add:**
1. **Villagers** - Point placement, count: 4-8 per village
2. **VillageCenter** - Point placement, count: 1 per village
3. **Houses** - RandomCircle placement, count: 3-5 per village, radius: 15
4. **Storehouse** - Point placement, count: 1 per village
5. **WorshipSite** - Point placement, count: 1 per village
6. **Resource_Wood** - RandomCircle placement, count: 5-8, radius: 30
7. **Resource_Stone** - RandomCircle placement, count: 3-5, radius: 30
8. **Resource_Food** - RandomCircle placement, count: 4-6, radius: 30
9. **Tree** - RandomCircle placement, count: 10-15, radius: 50
10. **Crop** - Grid placement, grid: 5x5, spacing: 2x2
11. **Shrub** - RandomCircle placement, count: 8-12, radius: 40

**Settings:**
- `seed`: 1 (or randomize for variation)
- Configure each entry's placement mode, count, and spread

---

### 7.3 PureDotsRuntimeConfig.asset
**Location:** `Godgame/Assets/Data/`

**Creation:**
1. Right-click in `Godgame/Assets/Data/`
2. Create → PureDOTS → PureDOTS Runtime Config
3. Name it `GodgameConfig`

**Settings:**
- Configure Time, History, and Pooling settings
- Add Resource Types catalog with entries:
  - "Wood"
  - "Stone"
  - "Food"
  - "Tools"
  - "Mana"
- Add Recipe Catalog (optional) for resource processing

---

## 8. Presentation Assets

### 8.1 VillagerVisual.prefab
**Location:** `Godgame/Assets/Prefabs/Presentation/` (create directory)

**Components:**
- `PlaceholderVisualAuthoring`
- Simple capsule mesh
- Basic material

**Settings:**
- Use as visual reference for villager prefabs
- Can be linked or duplicated for consistency

---

### 8.2 BuildingVisual.prefab
**Location:** `Godgame/Assets/Prefabs/Presentation/`

**Components:**
- `PlaceholderVisualAuthoring`
- Simple cube mesh
- Basic material

**Settings:**
- Base template for building visuals

---

## Validation Steps

After creating all prefabs:

1. **Check Component References:**
   - Verify all prefab references are assigned
   - Ensure ScriptableObject assets are linked

2. **Test Baking:**
   - Create a test SubScene
   - Add a few prefabs
   - Convert to SubScene
   - Verify no baking errors

3. **Runtime Test:**
   - Create a test scene with SimulationBootstrap
   - Instantiate prefabs
   - Verify systems recognize entities
   - Check placeholder visuals render

4. **Documentation:**
   - Update this checklist with any custom settings
   - Note any prefab variants created
   - Document resource type IDs used

---

## Notes

- All prefabs should use `TransformUsageFlags.Dynamic` for runtime entities
- Placeholder visuals are optional but recommended for debugging
- Resource type IDs must match entries in ResourceTypeIndex catalog
- Faction IDs should be consistent across related prefabs
- Village IDs can be auto-assigned (-1) or manually set for specific villages

---

## Quick Reference: Component Namespaces

- `PureDOTS.Authoring.*` - Authoring components
- `PureDOTS.Runtime.Components.*` - Runtime ECS components
- `PureDOTS.Runtime.Camera.*` - Camera systems
- `PureDOTS.Runtime.Input.*` - Input systems
- `PureDOTS.Runtime.Rendering.*` - Rendering components

---

**Last Updated:** [Current Date]
**Status:** Initial Checklist Created

