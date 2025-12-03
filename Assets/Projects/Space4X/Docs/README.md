# Space4X Documentation

**Project**: 4X Space Strategy Game
**Framework**: PureDOTS
**Last Updated**: 2025-11-27

---

## Overview

This folder contains **Space4X-specific** documentation - game mechanics, systems, and design concepts unique to the 4X space strategy game.

**What is Space4X?**
- 4X gameplay (eXplore, eXpand, eXploit, eXterminate)
- Space fleet combat and logistics
- Colony management and expansion
- Diplomacy and compliance systems
- Orbital mechanics and celestial phenomena

---

## Documentation Structure

### [Systems/](Systems/)
Technical system designs specific to Space4X mechanics.

**Core Systems**:
- **Camera Systems** - Space4X-specific camera control and rendering
- **[Modular Hull System](Systems/ModularHullSystem.md)** - Flexible vessel customization with mass/power budgets and module slots
- **[Warrior Pilot Last Stand](Systems/WarriorPilotLastStand.md)** - Kamikaze boarding mechanics for warrior cultures
- **[Misc Vessels & Megastructures](Systems/MiscVesselsAndMegastructures.md)** - Utility vessels, infrastructure, and megastructures
- **Fleet Systems** - Fleet management, combat, and logistics (to be added)
- **Colony Systems** - Colony management and production (to be added)
- **Diplomacy Systems** - Faction relations and compliance (to be added)

### [Concepts/](Concepts/)
High-level game design concepts and mechanics.

---

## Space4X-Specific Systems

### Camera & Rendering
Located in: `Systems/`

- **CameraIntegrationArchitecture.md** - Space4X camera architecture
- **CameraIntegrationSuggestions.md** - Camera integration patterns

**Summary**: Space4X requires specialized camera controls for 3D space navigation, zooming from tactical to strategic views, and following fleets across vast distances.

### [Future Systems]

As the Space4X project develops, additional system documentation will be added here for:

- **Fleet Command** - Fleet formation, movement, and combat
- **Colony Management** - Resource production, population, and development
- **Orbital Mechanics** - Planetary orbits, solar phenomena, gravity wells
- **Jump Drive Systems** - FTL travel and micro-jumps
- **Diplomacy & Compliance** - Trade agreements, sanctions, border enforcement
- **Technology Tree** - Research and tech progression

---

## Integration with PureDOTS Framework

Space4X **uses** PureDOTS framework systems and **extends** them with game-specific logic.

### Cross-Game Mechanics

Space4X shares several mechanics with Godgame through the **[Cross-Game Mechanics](../../../Docs/Mechanics/)** system. These mechanics have thematic variations for sci-fi/space (Space4X) vs medieval/divine (Godgame):

- **Miracles & Abilities** - Tactical abilities (orbital strike, shield boost, EMP burst)
- **Underground Spaces** - Hollow asteroids, station sublevels, hidden pirate bases
- **Rogue Orbiters** - Rogue planets, extragalactic comets, alien megastructures
- **Special Days** - Fleet days, solar flares, planetary conjunctions
- **Instance Portals** - Anomalous rifts to derelict hulks, alien fortresses
- **Runewords & Synergies** - Core combinations and tech upgrade combos
- **Entertainment & Performers** - Fleet morale officers, holographic entertainment
- **Wonder Construction** - Megastructure building (orbital rings, Dyson swarms)
- **Limb & Organ Grafting** ⚠️ - Cybernetic enhancement, alien organ transplants (Mature)
- **Memories & Lessons** - Fleet tactical memories and combat protocol lessons
- **Consciousness Transference** ⚠️ - Neural override, consciousness upload, hive mind assimilation (Mature)
- **Death Continuity & Undead Origins** ⚠️ - Cyborg reanimation from dead marines, ghost signals in ship systems (Mature)

See [Cross-Game Mechanics Documentation](../../../Docs/Mechanics/README.md) for implementation details.

### Framework Systems Used

| Framework System | Space4X Usage |
|-----------------|---------------|
| **Border Patrol & Ambush** | Fleet patrols, intercepts, cloaked ambushes |
| **Celestial Mechanics** | Solar storms, planetary shadows, radiation |
| **Anchored Characters** | Important captains/aces always rendered |
| **Resource System** | Fuel, minerals, crew supplies |
| **Skill Progression** | Captain experience and specialization |
| **Buff System** | Ship buffs, debuffs, module effects |
| **Registry System** | Persistent tracking of carriers, captains, colonies |

### Space4X-Specific Extensions

| Extension | Purpose |
|-----------|---------|
| **Jump Capability** | Micro-jump flanking maneuvers |
| **Orbital Components** | Planetary orbits and gravity |
| **Sensor/Radar Systems** | Detection ranges, sensor shadows |
| **Carrier Modules** - Ship modules and customization |
| **Compliance System** | Border violations and sanctions |

---

## Key Space4X Concepts

### Core Loop

```
Explore space →
Establish colonies →
Expand territory →
Encounter rivals →
Diplomatic or military conflict →
Research tech, build fleets →
Repeat
```

### Fleet Lifecycle

```
Design → Build → Deploy →
Patrol/Explore → Engage Enemies →
Resupply/Repair → Upgrade →
Retire or Destroy
```

### Diplomatic Cycle

```
First Contact → Trade Agreements →
Border Tensions → Compliance Violations →
Sanctions or War → Peace Treaty →
Repeat
```

---

## Design Pillars

1. **Strategic Depth**: Meaningful choices at tactical and strategic levels
2. **Emergent Conflict**: Wars arise from border friction, not scripted events
3. **Fleet Personalities**: Captains/crews have traits, not generic units
4. **Scale**: From single-ship tactical combat to galaxy-spanning strategy
5. **Determinism**: Perfect replay, networked multiplayer

---

## Quick Reference

### Common Space4X Components

```csharp
// Jump Capability (Micro-jumps)
public struct JumpCapability : IComponentData {
    public float Range;
    public float Cooldown;
    public float EnergyCost;
    public uint LastJumpTick;
}

// Orbital Mechanics
public struct OrbitalComponent : IComponentData {
    public Entity OrbitCenter; // Star/Planet
    public float OrbitRadius;
    public float OrbitSpeed;
    public float CurrentAngle;
}

// Sensor/Radar
public struct SensorComponent : IComponentData {
    public float DetectionRadius;
    public float ActiveSweepCooldown;
}

// Carrier Module
public struct CarrierModule : IComponentData {
    public ModuleType Type; // Weapons, Shields, Hangar, etc.
    public float Integrity; // 0-1 (damaged modules lose effectiveness)
}
```

### Common Space4X Systems

```csharp
// Space4X Jump Planner
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class Space4X_JumpPlannerSystem : SystemBase { ... }

// Orbital Update
[UpdateInGroup(typeof(TransformSystemGroup))]
public partial class OrbitalUpdateSystem : SystemBase { ... }

// Sensor Detection
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SensorDetectionSystem : SystemBase { ... }
```

---

## Development Guidelines

### Adding New Space4X Systems

1. **Check Framework First**: Can you use/configure an existing framework system?
2. **Document in Systems/**: Create `.md` doc with:
   - System purpose
   - Component design
   - System logic
   - Integration with framework (Border Patrol, Celestial, etc.)
3. **Keep Game-Agnostic Parts Separate**: If your system has generic parts, propose moving them to framework
4. **Follow DOTS Patterns**: See [PureDOTS DataOrientedPractices.md](../../../Packages/com.moni.puredots/Documentation/DesignNotes/DataOrientedPractices.md)

### Testing Space4X Systems

- **Unit Tests**: `Assets/Projects/Space4X/Tests/`
- **Scenario Tests**: Use PureDOTS ScenarioRunner for integration tests
- **3D Navigation**: Test camera, movement, and pathfinding in 3D space
- **Performance**: Space4X targets 1000+ ships at 60 FPS

---

## Example: Border Patrol in Space4X

**Framework**: PureDOTS provides generic `BorderPatrolSystem`
**Space4X Extension**: `Space4X_JumpPlannerSystem` adds micro-jump flanking

```csharp
// Generic patrol (framework)
PatrolBehaviorSystem calculates intercept points

// Space4X-specific (game)
Space4X_JumpPlannerSystem:
  - If direct path crosses solar storm → find jump point
  - Jump behind enemy fleet (outside sensor cone)
  - Execute micro-jump (instant position change)
```

---

## See Also

- [PureDOTS Framework Docs](../../../Packages/com.moni.puredots/Documentation/)
- [Root Documentation Index](../../../Docs/INDEX.md)
- [Tri-Project Briefing](../../../TRI_PROJECT_BRIEFING.md)
- [Godgame Docs](../../Godgame/Docs/)

---

**Game Designer**: [TBD]
**Technical Lead**: [TBD]
**Documentation Maintainer**: Space4X Team
