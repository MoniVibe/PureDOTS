# Mechanic: Mining Loop

## Overview

**Status**: Concept  
**Complexity**: Moderate  
**Category**: Economy

**One-line description**: Carriers extract raw resources from celestial deposits and feed the empire’s first production chain.

## Core Concept

Mining is the foundational loop that bankrolls every other directive. Carriers prospect, deploy extraction rigs, and maintain throughput from resource-rich bodies back to staging stations. Because the initial game state is serialized for player-driven modification, deposit definitions, carrier loadouts, and rig efficiencies must be data-driven and easy to extend.

## How It Works

### Basic Rules

1. Identify a deposit (asteroid, moon seam, orbital debris belt) within carrier range.
2. Assign a carrier to deploy appropriate extraction equipment and begin harvesting at a rate governed by deposit richness, rig quality, and carrier staffing.
3. Store extracted ore in carrier holds or linked drop-off drones until a haul directive transfers it to a station or refinery.

### Parameters and Variables

| Parameter | Default Value | Range | Effect |
|-----------|---------------|-------|--------|
| DepositRichness | 1.0 | 0.1-5.0 | Multiplies base extraction per tick.
| RigEfficiency | 1.0 | 0.2-2.0 | Captures equipment tier and maintenance state.
| HazardLevel | 0.0 | 0-1 | Drives attrition risk and required support modules.
| ExtractionTick | 5s | 1s-60s | Interval for yield and hazard evaluation.
| CarrierHoldCapacity | 1000 | 200-10000 | Dictates when the haul loop must pick up goods.

### Edge Cases

- **Depletion**: Deposits decline over time; when richness < threshold, carriers auto-suspend and request reassignment.
- **Hazards**: High hazard deposits require escort or specialised modules; failure inflicts component degradation or morale penalties.
- **Overcrowding**: Multiple carriers on the same deposit share richness via diminishing returns to encourage territorial planning.

## Player Interaction

### Player Decisions

- Choosing which deposits to activate first to fuel construction timelines.
- Balancing rig quality against maintenance and up-front costs.
- Scheduling escorts or countermeasures for hazardous sites.

### Skill Expression

Experienced players anticipate depletion curves, rotate carriers before downtime, and layer exploration intel to locate richer seams faster than rivals.

### Feedback to Player

- Visual: Deposit overlays indicating richness, remaining yield, and hazard state.
- Numerical: Carrier dashboards showing extraction per tick and time-to-fill for holds.
- Audio: Drills, alarms, or hazard warnings cueing intervention needs.

## Balance and Tuning

### Balance Goals

- Mining output must reliably bootstrap the economy without trivialising later resource hunts.
- Hazards introduce meaningful risk so combat and support loops remain relevant around mining sites.

### Tuning Knobs

1. **Richness Distribution**: Adjust galaxy generation curves to control early scarcity vs abundance.
2. **Hazard Scaling**: Increase attrition or required escort strength in contested regions.
3. **Rig Upkeep**: Drift maintenance costs to gate runaway carrier fleets.

### Known Issues

- TBD once prototype data reveals throughput imbalances.

## Integration with Other Systems

| System/Mechanic | Type of Interaction | Priority |
|-----------------|---------------------|----------|
| Haul Loop | Consumes mined resources and clears holds | High |
| Combat Loop | Protects mining carriers and contests deposits | High |
| Exploration Loop | Locates new deposits and hazards | High |

### Emergent Possibilities

- Hazard zones that spawn pirate interest, forcing combat choices to secure premium ores.
- Logistics bottlenecks that push players to build forward refineries on-the-fly.

## Implementation Notes

### Technical Approach

- Represent deposits as serialized entities with richness curves so players can mod initial placements.
- Use DOTS dynamic buffers on carriers to track active rigs, extraction rates, and hazard exposure for Burst-friendly simulation.
- Mining systems should run before hauling logistics each tick so resource availability updates feeding downstream commands.

### Performance Considerations

- Batch deposit evaluations per sector to keep the one-million-entity target feasible.
- Reuse alignment and morale data to influence hazard response without extra components.

### Testing Strategy

1. Unit tests for extraction rate decay and depletion handling.
2. Simulation tests verifying multi-carrier diminishing returns.
3. Stress tests measuring performance when thousands of carriers mine simultaneously.

## Examples

### Example Scenario 1

**Setup**: Carrier squad finds a rich asteroid belt with moderate hazards.  
**Action**: Deploy high-tier rigs and assign a combat escort to suppress pirate spawns.  
**Result**: Output spikes early, but escort upkeep shifts priorities once hazards escalate.

### Example Scenario 2

**Setup**: Player-modified start seeds sparse deposits but grants advanced rigs.  
**Action**: Single carrier rotates between deposits, timing hauls precisely.  
**Result**: Compact operation remains competitive through efficiency rather than volume.

## References and Inspiration

- **Sins of a Solar Empire**: Long-haul mining that demands defence.  
- **Factorio**: Resource depletion driving expansion.

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-10-31 | Initial draft | Captured foundational mining loop vision |

---

*Last Updated: October 31, 2025*  
*Document Owner: Design Team*
