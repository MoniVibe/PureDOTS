# Mechanic: Haul Loop

## Overview

**Status**: Concept  
**Complexity**: Moderate  
**Category**: Economy / Logistics

**One-line description**: Carriers shuttle mined resources, trade goods, and construction materials between extraction sites, stations, and colonies to keep the empire supplied.

## Core Concept

The haul loop is the fourth foundational priority, binding mining output to construction and trade demands. Carriers act as configurable logistics platforms, executing serialized routing plans that players can mod pre-launch. Efficient hauling maintains station build queues, feeds colonization efforts, and underwrites combat readiness through steady supply.

## How It Works

### Basic Rules

1. Define haul routes linking origin points (mines, trade hubs) to destinations (stations, shipyards, colonies).
2. Assign carriers with appropriate cargo modules and schedule cadence (continuous shuttle, convoy, on-demand).
3. Execute routes, transferring cargo according to priority tiers and clearing holds so mining and trade loops remain unblocked.

### Parameters and Variables

| Parameter | Default Value | Range | Effect |
|-----------|---------------|-------|--------|
| RouteLength | 2 jumps | 1-10 jumps | Impacts travel time and fuel usage.
| CargoPriority | Balanced | Low–Critical | Determines delivery order when capacity constrained.
| TransitRisk | 0.1 | 0-1 | Influences need for escorts or stealth modules.
| TransferRate | 1.0 | 0.2-3.0 | Throughput per tick at loading/unloading.
| ScheduleCadence | Continuous | Continuous–Batch | Controls whether carriers wait for full loads.

### Edge Cases

- **Bottlenecks**: Station queues overflow when hauling lags; triggers alerts for player intervention or auto-reassignment.
- **Route Disruption**: Combat or anomalies along a route force rerouting, integrating closely with exploration intel.
- **Overdelivery**: Excess stockpiles degrade if storage caps exceeded, encouraging distributed logistics planning.

## Player Interaction

### Player Decisions

- Selecting which resources get priority when capacity is limited.
- Choosing between resilient convoys with escorts or agile single-carrier shuttles.
- Deciding when to spin up temporary forward depots to shorten routes.

### Skill Expression

Veteran players dynamically rebalance routes, anticipate combat threats, and leverage serialized configuration to script contingencies that keep stations supplied even during crises.

### Feedback to Player

- Visual: Route overlays, congestion heatmaps, and capacity indicators on carriers and stations.
- Numerical: Supply dashboards showing inflow/outflow rates, backlog timers, and resource deficits.
- Audio: Alerts for stalled deliveries or route blockages.

## Balance and Tuning

### Balance Goals

- Hauling should be essential but not tedious; success depends on strategic planning rather than micromanagement.
- The loop must surface choke points that encourage combat or station construction responses.

### Tuning Knobs

1. **Transfer Rate Scaling**: Adjust how upgrades or crew quality boost throughput.
2. **Transit Risk Multipliers**: Increase or decrease hazard impact per sector.
3. **Storage Limits**: Tune station capacity to create meaningful but manageable pressure.

### Known Issues

- TBD until logistics simulations expose dominant bottlenecks.

## Integration with Other Systems

| System/Mechanic | Type of Interaction | Priority |
|-----------------|---------------------|----------|
| Mining Loop | Supplies raw material, requires empty holds | Critical |
| Combat Loop | Provides escorts and route security | High |
| Exploration Loop | Updates safe paths and warns of disruptions | High |

### Emergent Possibilities

- Player-created modular carriers that switch between hauling and combat roles depending on doctrine triggers.
- Dynamic trade agreements with NPC factions once diplomacy layers exist, using haul routes as the enforcement mechanism.

## Implementation Notes

### Technical Approach

- Represent routes as serialized graphs so modders can predefine or alter initial logistics networks.
- Use DOTS command buffers to queue load/unload operations, ensuring deterministic sequencing under heavy entity counts.
- Integrate with registry telemetry to track supply metrics outlined in `Docs/TODO/Space4x_PureDOTS_Integration_TODO.md`.

### Performance Considerations

- Batch route calculations per tick, reusing spatial partition for pathfinding.
- Avoid per-entity coroutine logic; rely on state machines encoded in components for Burst efficiency.

### Testing Strategy

1. Unit tests for load/unload sequencing and capacity handling.
2. Scenario tests that simulate route disruption and automatic rerouting.
3. Stress tests with thousands of concurrent routes validating throughput at the million-entity target.

## Examples

### Example Scenario 1

**Setup**: Mining carriers fill holds faster than stations can receive.  
**Action**: Player creates a secondary route to a refinery station closer to the belt.  
**Result**: Congestion clears, mining uptime stays high, and hauling network stabilizes.

### Example Scenario 2

**Setup**: Player-modified start spawns long-haul trade convoy with high TransitRisk sectors.  
**Action**: Escorts rerouted from combat loop secure the trade lane while exploration scouts alternate safe corridors.  
**Result**: Supply chain persists despite elevated danger, at the cost of reduced frontier combat coverage.

## References and Inspiration

- **EVE Online** hauling logistics and convoy gameplay.
- **Supreme Commander** mass/fabricator transport mechanics.

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-10-31 | Initial draft | Defined foundational haul loop |

---

*Last Updated: October 31, 2025*  
*Document Owner: Design Team*
