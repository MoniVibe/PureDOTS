# Mining legacy Setup Guide

This document describes the mining loop legacy setup in SpawnerDemoScene.

## Overview

The scene includes two parallel mining systems:
1. **Ground Mining**: Villagers harvest ore and wood from resource nodes
2. **Space Mining**: Carrier deploys mining vessels to harvest ore from asteroids

## Scene Setup

### Ground Resource Nodes

#### Wood Nodes (for villagers)
- **ResourceNode_Wood1** at (8, 0, 8)
- **ResourceNode_Wood2** at (-8, 0, 8)
- **ResourceNode_Wood3** at (8, 0, -8)

**Required Component**: `ResourceSourceAuthoring`
- Resource Type ID: `wood`
- Initial Units: `200`
- Gather Rate Per Worker: `4`
- Max Simultaneous Workers: `3`
- Infinite: `false`
- Respawns: `true`
- Respawn Seconds: `45`
- Debug Gather Radius: `3`

#### Ore Nodes (for villagers)
- **OreNode1** at (-8, 0, -8)
- **OreNode2** at (-12, 0, -5)

**Required Component**: `ResourceSourceAuthoring`
- Resource Type ID: `ore`
- Initial Units: `150`
- Gather Rate Per Worker: `3`
- Max Simultaneous Workers: `2`
- Infinite: `false`
- Respawns: `true`
- Respawn Seconds: `60`
- Debug Gather Radius: `3`

### Space Asteroids (for mining vessels)

- **Asteroid_Ore1** at (20, 10, 20) - Scale: (2, 2, 2)
- **Asteroid_Ore2** at (25, 12, 25) - Scale: (1.5, 1.5, 1.5)
- **Asteroid_Ore3** at (22, 8, 23) - Scale: (1.8, 1.8, 1.8)

**Required Component**: `ResourceSourceAuthoring`
- Resource Type ID: `ore`
- Initial Units: `500` (larger than ground nodes)
- Gather Rate Per Worker: `8` (faster for space mining)
- Max Simultaneous Workers: `5`
- Infinite: `false`
- Respawns: `true`
- Respawn Seconds: `120`
- Debug Gather Radius: `5`

### Carrier and Mining Vessels

- **Carrier** at (18, 8, 18) - Scale: (2, 1, 3)
  - Parent of MiningVessel1 and MiningVessel2
  
- **MiningVessel1** - Child of Carrier, Local Position: (1, 0, 1)
- **MiningVessel2** - Child of Carrier, Local Position: (3, 0, 3)

**Note**: Currently mining vessels are placeholders. You'll need to create:
- Carrier authoring component (to manage mining vessels)
- Mining vessel authoring component (similar to VillagerAuthoring but for space)
- Systems to handle carrier mining vessel deployment and asteroid mining

## Quick Setup Steps

1. **Add ResourceSourceAuthoring to all resource nodes**:
   - Select each ResourceNode and OreNode GameObject
   - Add Component → `ResourceSourceAuthoring`
   - Configure properties as listed above

2. **Add ResourceSourceAuthoring to asteroids**:
   - Select each Asteroid GameObject
   - Add Component → `ResourceSourceAuthoring`
   - Configure properties as listed above (ore type, larger values)

3. **Ensure Storehouse accepts ore and wood**:
   - Select Storehouse GameObject
   - Verify `StorehouseAuthoring` component has capacity entries for both `ore` and `wood`

4. **Set up Carrier/Mining Vessel Systems** (if not yet implemented):
   - Create `CarrierAuthoring` component
   - Create `MiningVesselAuthoring` component (similar to VillagerAuthoring)
   - Create systems for:
     - Carrier deploying mining vessels
     - Mining vessels targeting asteroids
     - Mining vessels gathering from asteroids
     - Mining vessels returning to carrier with resources

## Expected Behavior

When systems are fully implemented:

1. **Villagers**:
   - Spawn from VillagerSpawner
   - Find nearby wood/ore nodes
   - Gather resources
   - Deposit at Storehouse
   - Repeat

2. **Mining Vessels**:
   - Deploy from Carrier
   - Target nearby asteroids
   - Gather ore from asteroids
   - Return to Carrier with ore
   - Carrier deposits ore to Storehouse (or separate space station)

## Resource Types

- `wood` - Ground resource, harvested by villagers
- `ore` - Available both on ground (for villagers) and in asteroids (for mining vessels)

