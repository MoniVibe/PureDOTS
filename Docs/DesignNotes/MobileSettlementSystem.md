# Mobile Settlement & Nomadic Colony Concepts

## Goals
- Allow villager ships (Godgame) and colonist fleets (Space4x) to operate as mobile settlements with upgradeable infrastructure.
- Support alignment/outlook-driven captain behavior, splinter group formation, and long-distance migration across islands, planets, and stars.
- Reuse existing systems (registries, industrial sectors, navigation, buffs, metrics) while adding travel-specific mechanics.

## Core Entities
- `MobileSettlement` component:
  - `SettlementId`, `FactionId`, `Population`, `CrewSize`, `CargoCapacity`.
  - `HomePortScope` reference for inheritance of taxes/loyalty.
  - `CurrentMode` (Exploring, Trading, Colonizing, Nomadic, Docked).
- `CaptainProfile` component:
  - `CaptainEntity`, `OutlookVector`, `AlignmentTriplet`, `Traits` (e.g., Explorer, Isolationist).
  - Influence on decision-making and morale buffs/debuffs.
- `ShipUpgradeState` buffer:
  - Installed modules (foundry, orchard, labs), tier levels, maintenance.
  - Hooks into production chains and industrial sector calculations.
- `SplinterGroup` component (for new colonies):
  - `ParentSettlementId`, `DestinationScope`, `DepartureTick`, `Manifest` (population/resources).

## Systems Overview
1. `MobileSettlementLifecycleSystem`:
   - Updates population, morale, logistics while at sea/space.
   - Applies consumption using resource/buff systems.
2. `MobileSettlementNavigationSystem`:
   - Integrates with `UniversalNavigationSystem` for long-range travel (ocean, interstellar lanes).
   - Supports multi-leg routes, hazards, danger weights.
3. `CaptainBehaviorSystem`:
   - Uses alignment/outlook to select goals (explore, trade, colonize, raid).
   - Emits `CaptainDecisionEvent` consumed by narrative/AI planners.
4. `ShipUpgradeSystem`:
   - Processes upgrade orders using production chains; affects tier, capacity, specialization tags.
   - Updates `IndustrialSectorSystem` when ship counts as mobile facility.
5. `SplinterGroupFormationSystem`:
   - Triggers when morale/ideology thresholds crossed or events fire.
   - Moves population/resources into `SplinterGroup` entity and schedules new settlement creation at destination.
6. `NomadModeSystem`:
   - Handles roaming behavior (no home port), dynamic trade and buff effects.
   - Adjusts metric engine scopes (nomadic band metrics).

## Integration Points
- **Registries**: mobile settlements register as villages/colonies; logistic and resource registries track cargo, markets.
- **Navigation**: uses danger layers (storms, pirates, nebulae). Travel time affects uptime and industrial throughput.
- **Industrial Sectors**: ship modules contribute facility scores; splinter colonies inherit industry level seeds.
- **Buff System**: captain traits grant buffs/debuffs (Explorer’s Zeal, Isolationist Penalty); storms apply travel debuffs.
- **Perception**: ships have sensor range influenced by upgrades/buffs.
- **Metric Engine**: track population, wealth, morale, route efficiency, splinter frequency.
- **Narrative Situations**: events for poaching wars, splinter decisions, colonization milestones.
- **Skill Progression**: crew gains xp from voyages; spillover applies when stationed to teach settlements.

## Splinter & Colonization Flow
1. Trigger (ideology mismatch, opportunity, crisis).
2. `SplinterGroup` entity created with manifest (people/resources/ships).
3. Assign destination (new island/planet/system).
4. `MobileSettlementNavigationSystem` routes splinter group; upon arrival:
   - Create new settlement scope with inherited culture/tech modifiers.
   - Register with industrial/metric services; apply buffs (founder’s enthusiasm).
   - Optionally keep parent link for diplomacy and trade routes.

## Upgrades & Modules
- Modules defined in production catalog (`ShipyardModule`, `HydroponicBay`, `Foundry`).
- Upgrades change `BaseTier`, `Capacity`, specialization tags, enabling new production recipes.
- Modules affect `FacilityScore` and `IndustryIndex` when counted as mobile facility.
- Apply upkeep costs (maintenance, crew) via resource system.

## Technical Considerations
- Represent ships as ECS entities with child companion entities for presentation.
- Use SoA storage for navigation state (route, waypoint index, ETA) to handle large fleets.
- Ensure structural changes (splinter creation, settlement spawn) go through `EntityCommandBuffer` and respect rewind.
- Keep captain behavior deterministic: use hashed seeds from settlement id + current tick.
- For interstellar travel, integrate with `UniversalNavigationSystem` 3D volumes.

## Presentation
- Leverage `PresentationGuidelines.md`: companion entity drives ship visuals, crew count UI, morale indicators.
- Use `VFXPoolingPlan.md` for wake effects, colonization fireworks, morale buffs.

## Testing
- Simulation tests: ship upgrades affecting industrial output, population carry capacity.
- Navigation tests: route hazards, arrival triggers, multi-leg journeys.
- Splinter formation: ensure new settlement inherits data correctly and parent metrics update.
- Determinism: record/playback voyages with buffs/events.
