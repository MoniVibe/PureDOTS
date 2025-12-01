# Godgame Documentation

**Project**: Divine Intervention God-Game
**Framework**: PureDOTS
**Last Updated**: 2025-11-27

---

## Overview

This folder contains **Godgame-specific** documentation - game mechanics, systems, and design concepts unique to the god-game project.

**What is Godgame?**
- Divine intervention gameplay (player is a god)
- Village/villager simulation
- Social dynamics, radicalization, miracles
- Medieval/fantasy setting

---

## Documentation Structure

### [Systems/](Systems/)
Technical system designs specific to Godgame mechanics.

**Core Systems**:
- **Radical/Rebellion Systems** - Radicalization, rebellion, and response mechanics
- **Miracle Systems** - Divine intervention mechanics (rain, blessings, etc.)
- **Villager Systems** - Villager jobs, AI, and behavior
- **Social Systems** - Social dynamics and politics
- **Crisis Systems** - Elite crises and challenges
- **Settlement Systems** - Mobile settlements and expansion
- **Industry Systems** - Industrial production chains
- **Narrative Systems** - Story and situation generation

### [Concepts/](Concepts/)
High-level game design concepts and mechanics.

---

## Godgame-Specific Systems

### Radical & Rebellion
Located in: `Systems/`

- **RadicalAggregatesSystem.md** - How villagers form radical groups
- **RadicalMovementExamples.md** - Example scenarios
- **RadicalResponseStrategies.md** - How to respond to radicals
- **RadicalSystemQuickReference.md** - Quick reference guide

**Summary**: Villagers can become radicalized based on dissatisfaction, forming opposition groups. Player must balance appeasement vs suppression.

### Miracles & Divine Intervention
Located in: `Systems/`

- **RainMiraclesAndHand.md** - Divine hand mechanics and miracles

**Summary**: Player (god) can intervene with miracles like rain, blessings, or curses. Costs miracle points, affects villager faith.

### Villager Management
Located in: `Systems/`

- **VillagerJobs_DOTS.md** - Villager job assignment and execution
- **SociopoliticalDynamics.md** - Social structures and power dynamics

**Summary**: Villagers have jobs, social roles, and political alignments. They form factions, compete for power, and influence village direction.

### Settlement & Industry
Located in: `Systems/`

- **MobileSettlementSystem.md** - Settlement movement and nomadic mechanics
- **IndustrialSectorSystem.md** - Industrial zones and production
- **EliteCrisisSystem.md** - Elite challenges and power struggles

**Summary**: Settlements can move (nomadic), develop industries, and face crises from elite factions.

### Narrative
Located in: `Systems/`

- **NarrativeSituations.md** - Procedural narrative generation

**Summary**: Events and situations emerge from simulation, creating stories.

---

## Integration with PureDOTS Framework

Godgame **uses** PureDOTS framework systems and **extends** them with game-specific logic.

### Framework Systems Used

| Framework System | Godgame Usage |
|-----------------|---------------|
| **Guild Curriculum** | Villages teach crafts, faith, combat skills |
| **Buff System** | Blessings, curses, moods |
| **Skill Progression** | Villager skill growth |
| **Celestial Mechanics** | Day/night affects crops, moods |
| **Border Patrol & Ambush** | Border defense against raids |
| **Resource System** | Food, materials, faith points |

### Godgame-Specific Extensions

| Extension | Purpose |
|-----------|---------|
| **Radical Components** | Track radicalization levels |
| **Miracle Components** | Store miracle cooldowns, costs |
| **Villager Personality** | Alignment, traits, preferences |
| **Faith System** | Villager faith in player (god) |

---

## Key Godgame Concepts

### Core Loop

```
Player observes villagers →
Identifies needs/crises →
Intervenes (miracles, guidance) OR lets nature take its course →
Villagers respond (faith changes, actions) →
Emergent stories and challenges →
Repeat
```

### Villager Lifecycle

```
Birth → Childhood (learn from parents) →
Adulthood (work, socialize, form beliefs) →
Elder (teach, lead, or radicalize) →
Death (legacy: items, knowledge, tales)
```

### Radicalization Pipeline

```
Content Villager →
Dissatisfied (unmet needs) →
Questioning (lose faith in god/system) →
Radicalized (join opposition) →
Active Rebellion (undermine village) OR
Redemption (player intervenes successfully)
```

---

## Design Pillars

1. **Emergent Narrative**: Stories arise from simulation, not scripted
2. **Moral Ambiguity**: No "right" answer - appease vs suppress, mercy vs justice
3. **Consequence**: Every miracle/decision has costs and ripple effects
4. **Simulation Depth**: Villagers are individuals with beliefs, not abstract units

---

## Quick Reference

### Common Godgame Components

```csharp
// Radicalization
public struct RadicalAlignment : IComponentData {
    public float AlignmentLevel; // 0 = loyal, 1 = fully radicalized
}

// Faith in Player (God)
public struct VillagerFaith : IComponentData {
    public float FaithLevel; // 0 = atheist, 1 = devout
}

// Miracle Cooldowns
public struct MiracleState : IComponentData {
    public float RainCooldown;
    public float BlessingCooldown;
}

// Job Assignment
public struct VillagerJob : IComponentData {
    public JobType CurrentJob; // Farmer, Guard, Priest, etc.
}
```

### Common Godgame Systems

```csharp
// Radicalization Progression
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class RadicalizationProgressionSystem : SystemBase { ... }

// Miracle Application
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class MiracleApplicationSystem : SystemBase { ... }

// Villager Job Execution
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class VillagerJobExecutionSystem : SystemBase { ... }
```

---

## Development Guidelines

### Adding New Godgame Systems

1. **Check Framework First**: Can you use/configure an existing framework system?
2. **Document in Systems/**: Create `.md` doc with:
   - System purpose
   - Component design
   - System logic
   - Integration points with framework
3. **Keep Game-Agnostic Parts Separate**: If your system has generic parts, propose moving them to framework
4. **Follow DOTS Patterns**: See [PureDOTS DataOrientedPractices.md](../../../Packages/com.moni.puredots/Documentation/DesignNotes/DataOrientedPractices.md)

### Testing Godgame Systems

- **Unit Tests**: `Assets/Projects/Godgame/Tests/`
- **Scenario Tests**: Use PureDOTS ScenarioRunner for integration tests
- **Playtests**: Emergence requires actual play sessions, not just automated tests

---

## See Also

- [PureDOTS Framework Docs](../../../Packages/com.moni.puredots/Documentation/)
- [Root Documentation Index](../../../Docs/INDEX.md)
- [Tri-Project Briefing](../../../TRI_PROJECT_BRIEFING.md)
- [Space4X Docs](../../Space4X/Docs/)

---

**Game Designer**: [TBD]
**Technical Lead**: [TBD]
**Documentation Maintainer**: Godgame Team
