# Space4X Frameworks TODO

Source: `Space4x/Docs/TODO/4xdotsrequest.md` (Space4X backlog). Track DOTS-side work needed for Space4X mechanics; keep this file in sync with upstream requests.

Meta: Status values = Planned | In Progress | Done. Update Owner + LastUpdated when touching a section. Keep completed items in place (mark Status: Done) so history stays visible.

Agent routing (single source of truth): when told to "proceed work", start here.
- Agent A (Implementation): begin at **Next Up**; take the first Planned item, set Status → In Progress with your name/date. On completion, set Status → Done, update LastUpdated, and drop PR link in Progress.md under the Space4X heading.
- Agent B (Error & Glue): scan sections marked In Progress or blocked; pick up integration/ordering/testing gaps, update Status/Owner/LastUpdated, and note hand-off expectations in this file.
- Agent C (Documentation): mirror changes by updating this file and Progress.md; ensure any new systems/authoring/tests are documented here with Status/Owner/LastUpdated before closing the slice.

## Next Up (refresh each planning cycle)
- Phase 1 slice: Modules + degradation/repairs baseline (slot data, refit system, health/repair queue) — Owner: Agent A (Implementation) — In Progress 2025-11-22
- Phase 1 slice: Compliance path (crew aggregation, Affiliation buffers, compliance ordering + breach routing) — Owner: TBD
- Phase 1 slice: Mining deposits/harvest node queue and waypoint/highway scaffolding — Owner: TBD

## Core Mechanics & Compliance
- Status: Planned | Owner: TBD | LastUpdated: 2025-02-06
- Wire alignment data on crew prefabs: `AlignmentTriplet`, `RaceId`, `CultureId`, `DynamicBuffer<EthicAxisValue>`, `DynamicBuffer<OutlookEntry>`, crew aggregates with `DynamicBuffer<TopOutlook>`, `RacePresence`, `CulturePresence`.
- Populate `DynamicBuffer<AffiliationTag>` for crews, fleets, colonies, factions; derive loyalty from morale/contract on spawn.
- Add `DoctrineAuthoring` baker mapping to `DoctrineProfile`, `DynamicBuffer<DoctrineAxisExpectation>`, `DynamicBuffer<DoctrineOutlookExpectation>`.
- Implement `CrewAggregationSystem` (recompute weighted alignments/outlooks) feeding compliance buffers.
- Integrate `Space4XAffiliationComplianceSystem` ordering (after aggregation, before command systems); route `ComplianceBreach` to AI/planning (mutiny/desertion command buffers) and suspicion deltas to intel/alert systems.
- Hook suspicion decay routing: feed `SuspicionScore` deltas into telemetry/UI surfaces.

## Modules, Health, Repairs
- Status: In Progress | Owner: Agent A (Implementation) | LastUpdated: 2025-11-22
- Module slot framework: `CarrierModuleSlot` buffer + `ModuleStatModifier` components; module entities parented to carriers.
- Systems: `CarrierModuleRefitSystem` (time-based swap, archetype transition, refit gating), `ModuleStatAggregationSystem` (child stat aggregation), `ModuleBakingSystem` (authoring -> blob).
- Gating: check `RefitFacility` proximity; tech/crew scaling for refit time; field vs station swap rules.
- Component degradation: per-module `ComponentHealth` buffer with degradation sources, failure states, repair priority.
- Systems: `ComponentDegradationSystem`, `FieldRepairSystem` (capped repairs outside stations), `StationRepairSystem`, `ComponentFailureSystem`.
- Repair queue/priorities; critical system auto-prioritization; manual override path.
- Progress (2025-11-22, Agent A): Added module slot/health/repair components with aggregation, degradation, repair queue, refit gating, power-budget gating, and console/singleton telemetry plus catalog/loadout authoring to spawn module entities. Unit tests cover aggregation/refit/repair flows. ScenarioRunner smoke (`Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/space4x_modules_smoke.json`) exercises over-budget refit without prefabs. Follow-ups: consume catalog in scenarios, HUD telemetry, catalog-ID refits, crew skill modifiers, combat stat aggregation, and station-vs-field facility checks/presentation coverage.

## Mining Deposits & Harvest Nodes
- Status: Planned | Owner: TBD | LastUpdated: 2025-02-06
- Deposit entities with richness/type/regeneration/hazard/max nodes; buffer of `HarvestNode` attachment points and deterministic queueing.
- Systems: `DepositRegenerationSystem`, `HarvestNodeAssignmentSystem`, `HarvestNodeQueueSystem`, `DepositDepletionSystem`.
- Deterministic ordering: process requests by (Priority, RequestTick, EntityIndex); register deposits in spatial grid for proximity queries.

## Crew Progression
- Status: Planned | Owner: TBD | LastUpdated: 2025-02-06
- Components: `CrewSkills`, `SkillExperienceGain`, `HazardResistance`, `SkillChangeLogEntry`.
- Systems: `CrewExperienceSystem`, `SkillModifierSystem`, `HazardResistanceSystem`; mining system multiplies rate by skill; telemetry `space4x.skills.*`.
- Hazard mitigation: reduce `HazardDamageEvent` amounts using `HazardResistance`; tests exist upstream, ensure parity.
- Breeding/Cloning (deferred): `CrewGrowthSettings/State/Telemetry`, `Space4XCrewGrowthSystem`, authoring; defaults disabled, currently logs telemetry only.

## Navigation, Waypoints, Interception
- Status: In Progress | Owner: Agent C | LastUpdated: 2025-02-07
- Progress: deterministic mobility path queue with gateway traversal, blocked/disabled guards, and rendezvous/interception event stream; follow-up needed for maintenance/degradation and ownership reconfiguration.
- Waypoints/infrastructure: `Waypoint`, `HyperHighway`, `Gateway` components; systems for registration, pathfinding, maintenance/degradation, ownership reconfiguration; maintenance resource tracking in blob data.
- Fleet interception/rendezvous: broadcast position/velocity, intercept pathfinding, tech-gated interception vs static rendezvous, spatial queries for nearest fleets; command log + telemetry `space4x.intercept.*`; ensure sample authoring seeds `InterceptCapability`.

## Economy & Spoilage
- Status: In Progress | Owner: Agent C | LastUpdated: 2025-02-07
- Progress: batch pricing tracks smoothed inflow/outflow with elasticity + time-scale aware spoilage; trade opportunity surfacing added (best supply/demand pairs by price spread) with a sample scenario (`space4x_trade_opportunities.json`); trade routing now issues logistics requests from surfaced opportunities and a lightweight fulfillment stub advances them for registry telemetry. Full transport assignment/pacing + station inventory pipeline remain.
- Supply/demand pricing: `StationInventory` buffer with inflow/outflow, base/current price; `SupplyDemandModifier` elasticity.
- Systems: `InventoryFlowTrackingSystem`, `DynamicPricingSystem`, `TradeOpportunitySystem`.
- Spoilage: FIFO inventory batches with `CreationTick` and `SpoilageRate`; `ResourceSpoilageSystem` + `FIFOConsumptionSystem`; consumables degrade (e.g., 2%/tick), durables excluded.

## Tech Diffusion & Time Control
- Status: In Progress | Owner: Agent C | LastUpdated: 2025-02-07
- Progress: tech diffusion components/system added (source bootstrap, distance-weighted spread, time-scale aware rates); upgrade application + registry/time-control audits remain open.
- Tech diffusion: `TechLevel`, `TechDiffusionState` components; `TechDiffusionSystem`, `TechUpgradeApplicationSystem`; hybrid distance/time formula with tech level multiplier; spatial queries from core worlds.
- Time control: ensure Space4X systems honor `TimeState.TimeScale` (pause/1x/2x/5x/10x); mark time-independent systems appropriately for UI/presentation.

## Authoring & Tooling
- Status: Planned | Owner: TBD | LastUpdated: 2025-02-06
- Enum registry generation: `EthicAxisId`, `OutlookId`, `AffiliationType` shared across authoring/narrative/DOTS.
- Inspector helpers/validation: doctrine min/max ranges, fanatic conviction caps; baker validation for crews.
- Sample micro scene: captain + crew + faction doctrine to validate mutiny/desertion flows.

## Integration Hooks & Telemetry
- Status: Planned | Owner: TBD | LastUpdated: 2025-02-06
- AI planner: consume `ComplianceBreach` to spawn mutiny/desertion/independence tickets.
- Telemetry: extend registry bridge snapshot with breach counts/mean suspicion; `Space4XComplianceTelemetrySystem` writes `space4x.compliance.*`.
- Narrative triggers: forward breach events to narrative/quest/bark systems.

## Testing
- Status: Planned | Owner: TBD | LastUpdated: 2025-02-06
- Edit-mode NUnit: synthetic alignments/ethics into compliance system; assert breach type/severity scaling with loyalty; spy suspicion behavior.
- Runtime assertions: aggregation/compliance guard rails for missing doctrine/affiliation data.
- Module/degradation tests: refit sequence, stat aggregation, failure/repair flows.
- Spoilage/economy tests: FIFO consumption, price elasticity, trade opportunity detection.
- Interception tests: broadcast tick/velocity/residency updates; request→path→command-log flow.

## Phasing (requested priority)
- Phase 1: Module system, component degradation, mining deposit/harvest nodes, waypoints/infrastructure.
- Phase 2: Supply & demand economy, resource spoilage, tech diffusion.
- Phase 3: Fleet interception, crew experience/skills, crew breeding/cloning (deferred).
