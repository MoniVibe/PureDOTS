# Bootstrap Profiles

## Purpose

Bootstrap profiles provide configurable scene initialization for Godgame and Space4x, seeding spatial/time registries and core singletons without hardcoding game-specific values.

## Usage

1. Create a ScriptableObject inheriting from `BootstrapProfile`
2. Configure registry bounds, spawn counts, initial entity placement
3. Attach to scene bootstrap GameObject
4. Profile is consumed by `BootstrapSystem` during scene initialization

## Example Profiles

- `GodgameVillageBootstrap` - Seeds village with storehouses, villagers, resource nodes
- `Space4xMiningBootstrap` - Seeds asteroids, carriers, mining vessels
- `PerformanceSoakBootstrap` - Seeds 50k entities for performance testing

## Neutrality

All profiles use theme-neutral component types (ResourceRegistry, SpatialGridConfig, etc.) so they work across both games.


