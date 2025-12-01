# LastLightVR - Concept Summary & Action Plan

**Created**: 2025-11-26
**Status**: Ready for initialization
**Purpose**: Executive summary of LastLightVR concept and next steps

---

## Quick Overview

**LastLightVR** ("Last Light") is a co-op VR last-stand simulator combining:
- **Space Pirate Trainer** (VR combat feel)
- **XCOM/Darkest Dungeon** (tactical sacrifice, permadeath)
- **Tower Defense** (wave-based attacks on multiple fronts)

**Core Hook**: You are the beacon - a reverse Eye of Sauron - defending the last light in a world of darkness. Make brutal real-time decisions about who lives and who dies while physically fighting alongside your defenders.

---

## What Makes This Special

### 1. The Fantasy
> "You are the will of a blinding beacon that holds back an ocean of demons"

- Not a traditional hero - you ARE the settlement's will
- Possess/control different defenders mid-battle
- Every decision has permanent consequences

### 2. The Signature Mechanic: "Declare Last Stand"

**Physical VR action**:
- Slam your weapon into the beacon
- Ring a massive war bell
- Raise a relic to the sky

**Mechanical effect**:
- Huge buffs to defenders in that area (damage, morale, slow-mo)
- But defenders there will likely die permanently
- Beacon is drained → global penalties for next missions

**Emotional impact**:
> "It's the nuke button you can press often—but you really, really don't want to"

### 3. Emergent Stories
> "Remember when we sacrificed the smith and his whole squad so the kids could evacuate?"

Every session creates memorable moments of sacrifice and heroism.

---

## Core Gameplay Loop

### Meta Layer (Between Battles)

```
Settlement Management (Hub/Map View)
    ↓
Assign villagers to jobs (farmer, smith, guard, etc.)
    ↓
Craft/upgrade equipment from gathered resources
    ↓
Expand outward → build outposts to gather more resources
    ↓
Darkness attacks → Choose which location to defend
    ↓
Pick defenders & loadout for the mission
    ↓
[Drop into VR Combat]
```

### Combat Layer (VR Mission)

```
Enter VR as one defender
    ↓
Demons attack along 2-3 lanes toward:
  - Buildings (shelter, smithy, storehouse)
  - The beacon itself
    ↓
Node-based movement:
  - Choose position (gate, wall, rooftop)
  - Choose stance (cautious/normal/reckless)
  - Character auto-moves, you fight in VR
    ↓
Issue high-level squad orders:
  - "Squad A defend smithy"
  - "Archers fall back"
  - "Everyone to the beacon!"
    ↓
Make real-time sacrifices:
  - Which building can we afford to lose?
  - Who evacuates and who holds the line?
    ↓
Optional: Declare Last Stand (once per mission)
  - Huge buffs + high chance of permanent deaths
    ↓
Mission ends → survivors bring back resources
    ↓
Count the dead → they're gone forever
    ↓
Tales of the fallen → small permanent bonuses
```

---

## Key Design Pillars (Proposed)

### Pillar 1: Meaningful Sacrifice
- Every defender death matters
- No "reloading" or "undoing" bad decisions
- Players feel the weight of their choices
- **Example**: Sacrificing the veteran smith to save a group of children should feel heroic AND tragic

### Pillar 2: Visceral VR Combat
- Combat feels like Space Pirate Trainer
- Physical aiming, blocking, parrying, spellcasting
- Clear enemy telegraphs and projectiles
- **Anti-goal**: NOT a slow tactical game - it's intense and physically engaging

### Pillar 3: Tactical Clarity Under Pressure
- Players make hard decisions in real-time
- But those decisions should feel informed, not random
- VR-friendly information design (no overwhelming HUD)
- **Example**: You should clearly see "smithy has 3 defenders vs 20 demons" and know that's a losing fight

### Pillar 4: Comfortable VR
- No forced camera movement
- No stick locomotion (node-based movement instead)
- Steady 90fps minimum (target 120fps)
- Clear, readable visuals (no pixel shimmer or tiny text)

### Pillar 5: Multiplayer Cooperation & Tension
- Co-op players share defenders and resources
- Arguing over Last Stand usage is a feature
- Stories emerge from group decisions
- **Future**: PvPvE competitive beacons

---

## MVP Scope (Recommended)

### What's IN the First Prototype

**Map**:
- ✅ One settlement around the beacon
- ✅ 2-3 attack lanes
- ✅ 3 key buildings: shelter, smithy, storehouse
- ✅ The beacon (center, must not fall)

**Defenders**:
- ✅ Peasant conscript (pitchfork, low morale)
- ✅ Guard (sword/greatsword)
- ✅ Archer/crossbowman
- ✅ Simple mage (1-2 spells)

**Enemies**:
- ✅ 2-3 demon/monster types with clear behaviors
- ✅ Wave-based attacks
- ✅ Some target buildings, some target beacon

**Core Mechanics**:
- ✅ Node-based movement (choose position + stance)
- ✅ VR combat (shoot/swing/block/cast)
- ✅ Simple squad orders (defend X, fall back)
- ✅ Last Stand mechanic (once per mission)
- ✅ Permadeath (defenders die permanently)

**Meta Progression**:
- ✅ Simple resource gathering (survivors bring back resources)
- ✅ Upgrade armory (better gear for defenders)
- ✅ "Tales of the fallen" (small bonuses from dead heroes)

**Multiplayer**:
- ✅ Single-player first
- ✅ 2-player co-op (same beacon, shared resources)

### What's NOT in v1 (But Could Be Later)

- ❌ Full village expansion/building system
- ❌ Complex skill trees per defender
- ❌ Multiple settlement types
- ❌ PvP or competing beacons
- ❌ Complex crafting chains
- ❌ 3-4+ player co-op (start with 2)
- ❌ Procedural generation (handcraft first map)

---

## Technical Strategy

### Unity Setup
- **Unity 2022/2023 LTS or Unity 6 LTS**
- **Entities 1.4.2** (version locked with PureDOTS)
- **OpenXR + XR Interaction Toolkit**

### Architecture Approach

```
┌─────────────────────────────────────────────────┐
│           VR Layer (GameObjects)                │
│  - Player rig, hands, weapons, spells           │
│  - VR UI, interactions                          │
│  - URP VR rendering                             │
└───────────────────┬─────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────┐
│      Simulation Layer (DOTS/Entities)           │
│  - Defender AI                                  │
│  - Enemy hordes (lots of entities)              │
│  - Pathfinding, combat resolution               │
│  - Resource/settlement simulation               │
└───────────────────┬─────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────┐
│        PureDOTS Framework (Shared)              │
│  - Time/tick system                             │
│  - Registry infrastructure                      │
│  - Spatial partitioning                         │
│  - Telemetry/debug                              │
└─────────────────────────────────────────────────┘
```

### Key Technical Decisions

**1. VR Rendering**: Classic GameObjects + URP
- **NOT** Entities Graphics (VR support is shaky)
- Keep player-facing stuff in GameObject-land

**2. Simulation**: DOTS/Entities for hordes
- Use Entities for:
  - Enemy AI (hundreds of demons)
  - Defender AI when off-screen
  - Settlement simulation
  - Pathfinding
- Convert to GameObjects for rendering near player

**3. Multiplayer**: Start simple, expand later
- **v1**: Local single-player
- **v2**: 2-player co-op (same room or networked)
- **v3**: 3-4 player co-op
- Consider server-authoritative for hordes

**4. Performance Targets**:
- **Minimum**: 90fps
- **Target**: 120fps
- **Entity budget**: ~500-1000 fully simulated near player
- **Distant units**: LOD/imposters/abstract simulation

---

## PureDOTS Integration Opportunities

### Potential Extension Requests

Based on the concept, these could be game-agnostic and worth requesting from PureDOTS:

1. **VR Hand Tracking Components** (P0)
   - Hand position, rotation, grip state
   - Could be used by any VR game on PureDOTS

2. **Node-Based Movement System** (P1)
   - Choose destination + stance → auto-pathing
   - Could be useful for RTS/strategy games too

3. **Squad Command Components** (P2)
   - High-level orders (defend, retreat, focus target)
   - Reusable for any game with AI squads

4. **VR Comfort Settings** (P1)
   - Vignette, snap turn, comfort options
   - Any VR game would benefit

5. **Last Stand / Heroic Moment System** (P2)
   - Temporary buff + permanent consequence framework
   - Could generalize to other "ultimate ability" patterns

### Game-Specific (Keep in LastLightVR)

- Beacon mechanics (reverse Eye of Sauron visual)
- Specific demon/monster types
- Settlement/darkness narrative systems
- LastLight-specific UI/UX

---

## Documentation Structure (To Create)

Following the patterns from [NEW_PROJECT_QUICKSTART.md](NEW_PROJECT_QUICKSTART.md):

```
LastLightVR/Docs/
├── INDEX.md
├── PROJECT_SETUP.md
├── Progress.md
├── Conceptualization/
│   ├── README.md
│   ├── GameVision.md                    ← Expand ChatGPT pitch
│   ├── CorePillars.md                   ← Define 5 pillars above
│   ├── DesignPrinciples.md              ← VR-specific principles
│   ├── Features/
│   │   └── _TEMPLATE.md
│   └── Mechanics/
│       ├── LastStandMechanic.md         ← Signature mechanic
│       ├── NodeBasedMovement.md
│       ├── DefenderManagement.md
│       ├── ResourceGatheringLoop.md
│       └── MultiplayerStructure.md
├── Guides/
│   ├── VR_Setup_Guide.md
│   └── LastLightVR_PureDOTS_Entity_Mapping.md
├── QA/
│   ├── VR_Comfort_Checklist.md
│   └── VR_TestingGuidelines.md
└── TODO/
    ├── Phase1_Initialization_TODO.md
    ├── Phase2_CoreMechanics_TODO.md
    └── Integration_TODO.md
```

---

## Immediate Next Steps (Week 1)

### Day 1-2: Project Setup
- [ ] Create `LastLightVR/` Unity project
- [ ] Setup PureDOTS package reference (Entities 1.4.2)
- [ ] Create `Docs/` folder structure (minimal)
- [ ] Write `INDEX.md`
- [ ] Write `PROJECT_SETUP.md`

### Day 3-4: Vision Documents
- [ ] **GameVision.md** - Expand the ChatGPT pitch into full vision
- [ ] **CorePillars.md** - Document the 5 pillars
- [ ] **DesignPrinciples.md** - VR comfort + gameplay principles

### Day 5-6: First Mechanics
- [ ] **LastStandMechanic.md** - Full spec of signature mechanic
- [ ] **NodeBasedMovement.md** - Spec the VR-friendly movement
- [ ] Create `Phase1_Initialization_TODO.md`

### Day 7: VR Prototype
- [ ] Setup XR Interaction Toolkit
- [ ] Create first VR scene
- [ ] Test node-based movement concept in VR
- [ ] Document findings in `Progress.md`

---

## Week 2-4: Core Mechanics

- [ ] Write mechanic docs for:
  - Defender Management
  - Resource Gathering Loop
  - Multiplayer Structure
- [ ] Create first VR combat prototype:
  - Space Pirate Trainer-style shooting
  - 1-2 enemy types
  - Test "declare last stand" physical interaction
- [ ] File first PureDOTS extension request (VR hand tracking?)
- [ ] Create VR Comfort Checklist

---

## Success Criteria

### Concept Success
- ✅ Vision docs clearly communicate the game fantasy
- ✅ Teammates/friends can understand the concept from docs alone
- ✅ Core pillars guide design decisions

### Prototype Success
- ✅ VR combat feels good (Space Pirate Trainer quality)
- ✅ Node-based movement works and feels comfortable
- ✅ "Declare Last Stand" physical interaction is satisfying
- ✅ Playtesters create at least one memorable "sacrifice" story

### Technical Success
- ✅ PureDOTS integration works
- ✅ VR rendering hits 90fps with ~100 entities
- ✅ Extension requests are game-agnostic and approved

---

## Key Risks & Mitigations

### Risk 1: VR Cognitive Overload
**Symptom**: Players confused, overwhelmed, motion sick
**Mitigation**:
- Start with 2 lanes max (not 3+)
- Implement slow-mo tactical mode
- Clear visual/audio feedback for squad status

### Risk 2: DOTS + VR Rendering Issues
**Symptom**: Entities Graphics doesn't work in VR
**Mitigation**:
- Use Entities for simulation only
- Convert to GameObjects for VR rendering
- Test early with small scene

### Risk 3: Multiplayer Complexity
**Symptom**: Networked hordes tank performance
**Mitigation**:
- Build single-player first
- Add 2-player local/networked co-op second
- Server-authoritative sim for enemies

### Risk 4: Permadeath Feels Bad
**Symptom**: Players rage-quit after losing favorite defenders
**Mitigation**:
- Clear telegraphing of danger
- "Tales of the fallen" make deaths feel meaningful
- Retreat/evacuate options always available

---

## Resources & References

### Created Documents
- [LastLightVR_ConceptTranscript.md](DesignNotes/LastLightVR_ConceptTranscript.md) - Full ChatGPT conversation
- [LASTLIGHTVR_INITIALIZATION_PROPOSAL.md](LASTLIGHTVR_INITIALIZATION_PROPOSAL.md) - Detailed setup guide
- [NEW_PROJECT_QUICKSTART.md](NEW_PROJECT_QUICKSTART.md) - Fast-track guide
- [CONCEPT_CAPTURE_METHODS.md](CONCEPT_CAPTURE_METHODS.md) - Documentation patterns

### Ecosystem Documentation
- [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) - Overall architecture
- [PUREDOTS_INTEGRATION_SPEC.md](PUREDOTS_INTEGRATION_SPEC.md) - Integration patterns
- [Godgame Concepts README](../../Godgame/Docs/Concepts/README.md) - Example structure
- [Space4X Conceptualization README](../../Space4x/Docs/Conceptualization/README.md) - Example structure

---

## The North Star

Keep coming back to this:

> "Remember when we sacrificed the smith and his whole squad so the kids could evacuate?"

If playtests create stories like this, the game is working.

If players feel:
- **Weight** - decisions matter, deaths are permanent
- **Tension** - real-time sacrifices under pressure
- **Heroism** - glorious last stands are earned, not spammed
- **Connection** - they remember the names of the fallen

Then you've captured the fantasy.

---

**Created**: 2025-11-26
**Status**: READY - All foundation documents created
**Next Action**: Create Unity project and begin Week 1 checklist

---

## Quick Start Command

Ready to begin? Here's your first command:

```bash
# Navigate to workspace
cd C:\Users\Moni\Documents\claudeprojects\unity\

# Create LastLightVR Unity project via Unity Hub
# Then:

cd LastLightVR
mkdir -p Docs/Conceptualization/Features Docs/Conceptualization/Mechanics Docs/TODO

# Copy this summary for reference
cp ../PureDOTS/Docs/LASTLIGHTVR_CONCEPT_SUMMARY.md Docs/

# You're ready to roll!
```
