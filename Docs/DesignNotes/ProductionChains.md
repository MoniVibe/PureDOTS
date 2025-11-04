# Production Chain Concepts

## Overview
- Provide a shared data-driven production pipeline spanning raw extraction, processing tiers, intermediate materials, and manufactured goods (ships, wagons, carriers).
- Support both Godgame and Space4x by abstracting resource categories, recipes, and logistics triggers.
- Tie into existing registries (Resource, Storehouse, LogisticsRequest) while remaining extensible for new services (economy, trade, tech).

## Data Model
- `ResourceTypeRegistry`: classifies resources into categories:
  - Extraction (ores, timber, fish, crops, livestock, stone, rare herbs)
  - Harvestables/Lootables (fallen loot, raid spoils, relics)
  - Processed Materials (ingots, planks, cloth, chemicals, alloys, food rations)
  - Components (hulls, rigging, wheels, weapon fittings, magical cores)
  - Final Products (ships, wagons, siege engines, carriers, trade goods)
- `ProductionRecipe` blob:
  - Inputs: `ResourceRef`, quantity, quality tier requirement
  - Outputs: `ResourceRef`, quantity, optional byproducts
  - Time cost, workforce requirement (`SkillTag`, `WorkCrewSize`)
  - Facility requirement (building type, service flags, environment)
  - Tech/culture requirements from shared services
- `ProductionChainDescriptor`:
  - Graph linking multiple `ProductionRecipe` nodes; supports alternate branches (e.g., timber -> plank -> wagon body; ore -> ingot -> armor plate).
  - Metadata for trade value, logistics priority, moral/faith modifiers.

## Systems
- `ExtractionSystemGroup`:
  - Handles raw gathering (mines, farms, fishing docks, hunting).
  - Emits `ResourceIncrement` events to the resource registry.
- `ProcessingSystemGroup`:
  - Runs recipe jobs in parallel, consuming inputs from storehouses, applying time/workforce costs, and producing intermediates.
  - Integrates with `VillagerJobSystems` for worker assignment and `Education service` for skill gating.
- `ManufacturingSystemGroup`:
  - Assembles intermediate components into final products (ships, wagons, carriers) and registers with `ConstructionRegistry` or `TransportRegistry`.
  - Supports queueing via `ProductionOrderBuffer` (requested by economy, trade, or military services).
- `LootIntegrationSystem`:
  - Converts battle loot or event rewards into resource entries/progression triggers; handles salvage recipes (break down ships into components).
- `LogisticsIntegrationSystem`:
  - Reads production orders, schedules delivery routes, and updates `LogisticsRequestRegistry`.

## Authoring
- ScriptableObject catalogs:
  - `ResourceCatalog` for defining categories, stack limits, spoilage rules.
  - `ProductionRecipeCatalog` grouped by facility type and tech tier.
  - `ProductionChainCatalog` describing canonical chains per faction/biome.
- Bakers translate catalogs into blob assets consumed by production systems.

## Services Integration
- `Economy/Trade`: adjusts production priorities, applies price multipliers, manages market demand.
- `Tech/Culture`: unlocks recipes, improves yields, introduces new chain branches.
- `Population Traits`: modifies worker efficiency, determines available skills.
- `Military`: requests equipment (weapons, armor, siege engines) and consumes outputs.
- `Narrative Situations`: trigger special recipes (festival goods, relief supplies, elite regalia) with unique effects.

## Analytics & Telemetry
- Track throughput per chain node (inputs/outputs per tick).
- Monitor workforce utilization, bottlenecks, spoilage.
- Emit events for chain completion (ship launched, convoy assembled) to analytics and narrative systems.

## Implementation Notes
- **Data Layout**: store production state in SoA buffers (separate arrays for timers, input counts, worker slots); use AoSoA batching for high-frequency recipes to improve Burst vectorization.
- **Registries**: mirror resource outputs in dedicated registries (`ProductionOrderRegistry`, `ManufacturedGoodsRegistry`) following the deterministic builder contract.
- **Authoring & Baking**: bakers translate catalog ScriptableObjects into blob assets; ensure conversion hooks validate recipe dependencies and facility requirements.
- **Behavior Trees/AI**: extend villager/job AI with nodes that query production orders, evaluate skill match, and reserve workstations.
- **Scheduler**: integrate with service scheduler to tick long-running productions deterministically across rewind.
- **Requirements & Gating**:
  - Skill levels: gate recipe access and yield modifiers behind worker skills provided by population traits/education services.
  - Facility capabilities: require building upgrades or module components before advanced recipes activate.
  - Resource quality: enforce minimum grade or blessed variants for elite products (relics, flagship hulls).
  - Culture/tech prerequisites: query shared services to confirm alignment or research milestones before production queues accept orders.
