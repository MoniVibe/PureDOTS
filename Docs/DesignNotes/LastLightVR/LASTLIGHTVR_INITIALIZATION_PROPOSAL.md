# LastLightVR Project Initialization Proposal

**Created**: 2025-11-26
**Purpose**: Propose structure and initialization plan for LastLightVR project
**Based On**: Patterns from Space4X, Godgame, and PureDOTS

---

## Executive Summary

LastLightVR will be the **fourth project** in the tri-project ecosystem (soon to be quad-project):

```
┌─────────────────────────────────────────────────────────────────┐
│                    QUAD-PROJECT ECOSYSTEM                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐      │
│  │ Space4X  │  │ Godgame  │  │LastLight │  │ Future?  │      │
│  │ (Carrier │  │  (God-   │  │   VR     │  │          │      │
│  │   4X)    │  │  game)   │  │ (VR Sim) │  │          │      │
│  └─────┬────┘  └─────┬────┘  └─────┬────┘  └──────────┘      │
│        │             │             │                           │
│        └─────────────┼─────────────┘                           │
│                      │                                         │
│                ┌─────▼─────┐                                   │
│                │  PureDOTS │                                   │
│                │ Framework │                                   │
│                └───────────┘                                   │
└─────────────────────────────────────────────────────────────────┘
```

**Recommended Approach**: **Hybrid** structure combining:
- Space4X's **vision-first** simplicity
- Godgame's **rich categorization** (adopted as needed)
- PureDOTS **extension request** workflow

---

## 1. Project Location & Setup

### 1.1 Recommended Path

```
C:\Users\Moni\Documents\claudeprojects\unity\LastLightVR\
```

**Rationale**: Matches existing project naming convention (PascalCase, no spaces)

### 1.2 Unity Project Structure

```
LastLightVR/
├── Assets/
│   ├── LastLightVR/          # Game-specific code
│   │   ├── Scripts/
│   │   ├── Scenes/
│   │   ├── Prefabs/
│   │   └── Config/
│   └── Tests/
├── Packages/
│   └── manifest.json          # Reference PureDOTS package
├── Docs/                      # ← CONCEPT DOCUMENTATION
├── ProjectSettings/
└── AGENTS.md
```

### 1.3 PureDOTS Integration

**Packages/manifest.json**:
```json
{
  "dependencies": {
    "com.moni.puredots": "file:../../PureDOTS/Packages/com.moni.puredots",
    "com.unity.entities": "1.4.2",
    // ... other dependencies
  }
}
```

---

## 2. Documentation Structure (Proposed)

### 2.1 Phase 1: Minimal Viable Structure

**Initial Setup (Week 1)**:

```
LastLightVR/Docs/
├── INDEX.md
├── PROJECT_SETUP.md
├── Progress.md
├── Conceptualization/
│   ├── README.md
│   ├── GameVision.md
│   ├── CorePillars.md
│   ├── DesignPrinciples.md
│   ├── Features/
│   │   └── _TEMPLATE.md
│   └── Mechanics/
│       └── _TEMPLATE.md
└── TODO/
    └── Phase1_Initialization_TODO.md
```

**What Each File Does**:

- **INDEX.md** - Navigation hub for all documentation
- **PROJECT_SETUP.md** - Unity setup, dependencies, MCP tools
- **Progress.md** - Running log of milestones and work
- **GameVision.md** - High-level vision ("What is LastLightVR?")
- **CorePillars.md** - 3-5 fundamental design pillars
- **DesignPrinciples.md** - Decision-making principles
- **_TEMPLATE.md** - Templates for features and mechanics

### 2.2 Phase 2: Category Expansion

**As Complexity Grows (Month 2+)**:

```
LastLightVR/Docs/
├── Conceptualization/
│   ├── README.md              # Add status dashboard
│   ├── GameVision.md
│   ├── CorePillars.md
│   ├── DesignPrinciples.md
│   ├── _Templates/            # Move templates here
│   │   ├── Feature.md
│   │   ├── Mechanic.md
│   │   ├── System.md
│   │   └── Experience.md
│   ├── Core/                  # ← Categories emerge
│   ├── VRInteraction/
│   ├── Environment/
│   ├── AI_NPCs/
│   ├── Narrative/
│   ├── Progression/
│   ├── Multiplayer/           # If applicable
│   └── Implemented/           # Archive completed concepts
└── TODO/
    ├── Phase1_Initialization_TODO.md
    ├── Phase2_CoreMechanics_TODO.md
    └── Integration_TODO.md
```

**Rationale**:
- Start simple (Space4X style)
- Let categories emerge organically
- Don't prematurely organize (YAGNI principle)

### 2.3 Phase 3: Integration & Truth Sources

**As Implementation Begins**:

```
LastLightVR/Docs/
├── Conceptualization/         # (as above)
├── TruthSources/              # If needed (complex systems)
│   ├── VR_Input_TruthSource.md
│   └── Spatial_Audio_TruthSource.md
├── Guides/
│   ├── VR_Setup_Guide.md
│   └── LastLightVR_PureDOTS_Entity_Mapping.md
└── QA/
    └── VR_TestingChecklist.md
```

---

## 3. Initial Documents (Draft Content)

### 3.1 GameVision.md (Template)

```markdown
# LastLightVR - Game Vision

**Last Updated**: 2025-11-26
**Status**: Draft

---

## Elevator Pitch

[30-second description of the game]

Example:
> LastLightVR is a VR survival experience where you are the last beacon of
> civilization in a post-collapse world. Navigate ruins, manage scarce
> resources, and preserve knowledge for future generations—all in room-scale VR.

---

## Core Experience

**What makes this game unique?**
- [Unique selling point 1]
- [Unique selling point 2]
- [Unique selling point 3]

**Target Player**:
- Persona: [Who is this for?]
- Play session: [How long? What context?]
- Skill level: [Casual? Hardcore? VR-native?]

---

## Genre & Inspiration

**Primary Genre**: [VR Survival / VR Sim / VR Strategy / etc.]

**Inspirations**:
- [Game/Media 1] - What we're taking from it
- [Game/Media 2] - What we're taking from it

---

## Scope & Platform

**Platform**: VR (Quest 2/3, PCVR, Index, etc.)
**Scale**: [Indie scope? AA? Focused prototype?]
**Performance Target**: [90fps? 120fps? Entity count?]

---

## High-Level Pillars

*Note: Detailed pillars in CorePillars.md*

1. **Pillar 1** - [One sentence]
2. **Pillar 2** - [One sentence]
3. **Pillar 3** - [One sentence]

---

## Anti-Goals

What this game is NOT:
- ❌ [Anti-goal 1]
- ❌ [Anti-goal 2]

---

## Success Criteria

**Minimum Viable Experience**:
- [ ] [Core loop 1 playable]
- [ ] [VR interaction feeling good]
- [ ] [Performance hitting target]

**1.0 Vision**:
- [ ] [Full feature set]
- [ ] [Polish level]

---

## See Also
- [CorePillars.md](CorePillars.md)
- [DesignPrinciples.md](DesignPrinciples.md)
```

### 3.2 CorePillars.md (Template)

```markdown
# LastLightVR - Core Pillars

**Last Updated**: 2025-11-26

These pillars guide all design decisions. When in doubt, return to these.

---

## Pillar 1: [Name]

**Definition**: [What this means]

**In Practice**:
- Feature decisions: [How this guides features]
- Mechanic design: [How this affects mechanics]
- VR interaction: [How this shapes VR design]

**Examples**:
- ✅ [Something that aligns]
- ❌ [Something that violates]

---

## Pillar 2: [Name]

[Same structure]

---

## Pillar 3: [Name]

[Same structure]

---

## Pillar Conflicts & Resolution

Sometimes pillars conflict. Here's how we prioritize:

**Scenario**: [Pillar A] vs [Pillar B]
- **Resolution**: [How we decide]

---

## Evolution Log

| Date | Change | Rationale |
|------|--------|-----------|
| 2025-11-26 | Initial pillars | [Why these three] |
```

### 3.3 DesignPrinciples.md (Template)

```markdown
# LastLightVR - Design Principles

**Last Updated**: 2025-11-26

Tactical guidelines for day-to-day design decisions.

---

## VR-Specific Principles

### Comfort First
- **Rule**: Never sacrifice comfort for spectacle
- **Why**: VR sickness destroys the experience
- **Examples**:
  - ✅ Teleport + smooth locomotion options
  - ❌ Forced camera movement

### Physical Interaction Over UI
- **Rule**: If it can be grabbed, it should be grabbable
- **Why**: Immersion breaks with floating menus
- **Examples**:
  - ✅ Pick up item, inspect in hand, pocket it
  - ❌ Menu wheel for inventory management

---

## Gameplay Principles

### Scarcity Creates Tension
- **Rule**: [Your principle]
- **Why**: [Reasoning]
- **Examples**: [Concrete cases]

---

## Technical Principles

### Performance Is Non-Negotiable
- **Rule**: 90fps minimum, target 120fps
- **Why**: VR requires rock-solid framerate
- **Trade-offs**:
  - Reduce entity count before reducing framerate
  - LOD aggressively
  - Burst-compile everything

---

## Integration with PureDOTS

### When to Extend PureDOTS
- ✅ Need: Generic spatial query system (could be used by other VR games)
- ✅ Need: VR hand tracking components (reusable)
- ❌ Need: LastLightVR-specific narrative system (game-specific)

### Extension Request Threshold
If 2+ future projects could use it → Extension request
If only LastLightVR needs it → Game-specific implementation

---

## Decision Templates

**Template: New Feature**
1. Does it align with CorePillars? (All 3?)
2. Does it respect VR comfort?
3. Does it fit scope? (MVP? 1.0? Post-1.0?)
4. Implementation cost? (Days? Weeks? Months?)
5. Decision: [Go / No-Go / Defer]

**Template: VR Interaction**
1. Can it be physical? (vs UI-based)
2. Is it comfortable? (tested in headset?)
3. Is it intuitive? (can players discover it?)
4. Fallback option? (accessibility)
5. Decision: [Implement / Iterate / Reject]

---

## See Also
- [GameVision.md](GameVision.md)
- [CorePillars.md](CorePillars.md)
```

---

## 4. Recommended File Naming

### 4.1 Conventions

| Type | Convention | Example |
|------|------------|---------|
| Vision Docs | PascalCase.md | `GameVision.md` |
| Concepts | PascalCase_With_Underscores.md | `VR_Inventory_System.md` |
| Features | PascalCase.md | `HandTracking.md` |
| Mechanics | PascalCase.md | `ResourceScavenging.md` |
| Extension Requests | YYYY-MM-DD-kebab-case.md | `2025-11-26-vr-hand-tracking.md` |

### 4.2 Status Markers

**In Concept Docs**:
```markdown
**Status**: Draft | In Review | Approved | In Development | Implemented | On Hold
**Priority**: P0 (Critical) | P1 (High) | P2 (Medium) | P3 (Low)
**VR Tested**: Yes | No | Partial
```

**WIP Flags** (use Godgame's pattern):
```markdown
<WIP> - Work in progress
<NEEDS VR TEST> - Needs hands-on testing in headset
<NEEDS SPEC> - Specification needed
<COMFORT RISK> - Potential VR comfort issue
<PERFORMANCE RISK> - May impact framerate
```

---

## 5. Workflow: From Concept to Implementation

### 5.1 New Concept Flow

```
1. Idea emerges
   ↓
2. Quick note in appropriate category (or Features/)
   └─ Status: Draft
   ↓
3. Flesh out using template
   └─ Add examples, edge cases, VR considerations
   ↓
4. Review against CorePillars & DesignPrinciples
   └─ Status: In Review
   ↓
5. If approved, create TODO task
   └─ Status: Approved
   ↓
6. If needs PureDOTS extension, file request
   └─ PureDOTS/Docs/ExtensionRequests/YYYY-MM-DD-feature.md
   ↓
7. Implementation begins
   └─ Status: In Development
   ↓
8. Complete & verify
   └─ Status: Implemented
   └─ Move to Implemented/ or mark [COMPLETED]
```

### 5.2 VR-Specific Workflow

```
Concept → Paper Design → VR Prototype → Comfort Test → Iterate
                                              │
                                              ├─ Pass → Implement
                                              └─ Fail → Redesign or Reject
```

**Key Point**: VR features MUST be tested in headset before full implementation.

---

## 6. Integration with Tri-Project Ecosystem

### 6.1 TRI_PROJECT_BRIEFING.md Update

Add LastLightVR to the project table:

```markdown
| Project | Path | Purpose |
|---------|------|---------|
| **PureDOTS** | `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` | Shared DOTS framework |
| **Space4X** | `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` | Carrier 4X strategy |
| **Godgame** | `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` | God-game simulation |
| **LastLightVR** | `C:\Users\Moni\Documents\claudeprojects\unity\LastLightVR` | VR survival/simulation |
```

### 6.2 Extension Request Discipline

**When to File Extension Request**:
- ✅ VR-agnostic spatial systems (other projects could use)
- ✅ Hand tracking components (reusable)
- ✅ VR comfort/performance utilities (other VR projects)
- ❌ LastLightVR narrative systems (game-specific)
- ❌ LastLightVR-specific UI (not generalizable)

**Example Good Request**:
> "VR Hand IK solver components - could be used by any VR game on PureDOTS"

**Example Bad Request**:
> "LastLightVR story trigger system - only useful for our narrative"

---

## 7. VR-Specific Considerations

### 7.1 Performance Constraints

**VR Requirements**:
- **Minimum**: 90fps (11.1ms frame time)
- **Target**: 120fps (8.3ms frame time)
- **Absolute Max**: 144fps (6.9ms frame time)

**Implications**:
- More aggressive than flatscreen games
- Burst-compile EVERYTHING
- Aggressive LOD and culling
- Limit particle effects

### 7.2 VR Comfort Documentation

Create `Docs/QA/VR_Comfort_Checklist.md`:

```markdown
# VR Comfort Checklist

Run this checklist for EVERY new VR interaction.

## Camera Movement
- [ ] No forced camera rotation
- [ ] Optional smooth locomotion (off by default)
- [ ] Teleport available
- [ ] Vignette option for sensitive players

## Physical Interaction
- [ ] Hand IK feels natural
- [ ] Grab interactions respond instantly
- [ ] No "swimming" feeling (hand lag)
- [ ] Release is intuitive

## UI & Menus
- [ ] Text readable at arm's length
- [ ] No pixel shimmer (resolution issues)
- [ ] Menus anchored to world or hand (not head)

## Scale & Proportions
- [ ] Player height calibrated
- [ ] Objects at realistic scale
- [ ] Distances feel accurate

## Testing Panel
- [ ] Tested by VR-sensitive person
- [ ] Tested in 30+ minute session
- [ ] No reported discomfort
```

### 7.3 PureDOTS VR Extensions (Potential)

Anticipated extension requests:

| Feature | Priority | Description |
|---------|----------|-------------|
| VR Hand Tracking | P0 | Hand position, rotation, grip state |
| VR Spatial Audio | P1 | 3D audio source components |
| VR Comfort Settings | P1 | Vignette, snap turn, etc. |
| VR UI Canvas System | P2 | World-space UI anchoring |
| VR Object Grabbing | P0 | Grab, release, throw physics |

---

## 8. Immediate Action Items

### 8.1 Week 1: Foundation

**Day 1**:
- [ ] Create `LastLightVR/` Unity project
- [ ] Setup PureDOTS package reference
- [ ] Create `Docs/` folder structure (Phase 1)
- [ ] Write `INDEX.md`
- [ ] Write `PROJECT_SETUP.md`

**Day 2-3**:
- [ ] Draft `GameVision.md` (elevator pitch, scope)
- [ ] Draft `CorePillars.md` (3-5 pillars)
- [ ] Draft `DesignPrinciples.md` (VR-specific + gameplay)

**Day 4-5**:
- [ ] Create templates (`_TEMPLATE.md` for Features, Mechanics)
- [ ] Create `Phase1_Initialization_TODO.md`
- [ ] Write first concept doc (core mechanic?)
- [ ] Update `TRI_PROJECT_BRIEFING.md` with LastLightVR

**Day 6-7**:
- [ ] VR test Unity setup
- [ ] Create first VR scene
- [ ] Document initial setup in `PROJECT_SETUP.md`

### 8.2 Week 2-4: Core Concepts

- [ ] Write 3-5 core mechanic concepts
- [ ] File first extension request (if needed)
- [ ] Create VR comfort checklist
- [ ] Establish GitHub/version control workflow

---

## 9. Open Questions

### For Discussion:

1. **Project Scope**: Indie game? Prototype? Full release?
2. **VR Platform**: Quest-native? PCVR? Both?
3. **Multiplayer**: Solo only? Co-op? Async?
4. **PureDOTS Fit**: How much can/should be shared vs. game-specific?
5. **Timeline**: MVP in 3 months? 6 months? Longer?
6. **Team Size**: Solo? Small team? Collaborators?

### Technical Questions:

1. **Unity Version**: Match PureDOTS (Unity 6? 2022 LTS?)
2. **Entities Version**: 1.4.2 (locked) or allow newer?
3. **VR SDK**: OpenXR? Unity XR Interaction Toolkit?
4. **Input System**: Unity Input System (already used by PureDOTS)

---

## 10. Success Metrics

### Documentation Success:

- [ ] Vision docs clearly communicate game concept
- [ ] Teammates can onboard using docs alone
- [ ] Extension requests successfully processed by PureDOTS team
- [ ] Concepts → Implementation pipeline is smooth

### Integration Success:

- [ ] PureDOTS package integration works
- [ ] Extension requests are game-agnostic
- [ ] VR-specific code cleanly separated
- [ ] Other projects can reference our VR patterns

---

## 11. See Also

- [CONCEPT_CAPTURE_METHODS.md](CONCEPT_CAPTURE_METHODS.md) - Patterns analysis
- [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) - Ecosystem overview
- [PUREDOTS_INTEGRATION_SPEC.md](PUREDOTS_INTEGRATION_SPEC.md) - Integration guide
- [Godgame Concepts README](../../Godgame/Docs/Concepts/README.md) - Example structure
- [Space4X Conceptualization README](../../Space4x/Docs/Conceptualization/README.md) - Example structure

---

## Appendix: Directory Structure (Full)

**Complete proposed structure (Phase 1-3 combined)**:

```
LastLightVR/
├── Assets/
│   ├── LastLightVR/
│   │   ├── Scripts/
│   │   ├── Scenes/
│   │   ├── Prefabs/
│   │   ├── Config/
│   │   └── VR/
│   └── Tests/
│       ├── EditMode/
│       └── PlayMode/
├── Packages/
│   └── manifest.json
├── Docs/
│   ├── INDEX.md
│   ├── PROJECT_SETUP.md
│   ├── Progress.md
│   ├── AGENTS.md
│   ├── Conceptualization/
│   │   ├── README.md
│   │   ├── GameVision.md
│   │   ├── CorePillars.md
│   │   ├── DesignPrinciples.md
│   │   ├── _Templates/
│   │   │   ├── Feature.md
│   │   │   ├── Mechanic.md
│   │   │   ├── System.md
│   │   │   └── Experience.md
│   │   ├── Core/
│   │   ├── VRInteraction/
│   │   ├── Environment/
│   │   ├── AI_NPCs/
│   │   ├── Narrative/
│   │   ├── Progression/
│   │   ├── Multiplayer/
│   │   └── Implemented/
│   ├── Guides/
│   │   ├── VR_Setup_Guide.md
│   │   └── LastLightVR_PureDOTS_Entity_Mapping.md
│   ├── QA/
│   │   ├── VR_Comfort_Checklist.md
│   │   └── VR_TestingGuidelines.md
│   ├── TODO/
│   │   ├── Phase1_Initialization_TODO.md
│   │   ├── Phase2_CoreMechanics_TODO.md
│   │   └── Integration_TODO.md
│   └── TruthSources/
│       ├── VR_Input_TruthSource.md
│       └── Spatial_Audio_TruthSource.md
├── ProjectSettings/
└── README.md
```

---

**Created**: 2025-11-26
**Status**: PROPOSAL - Ready for review and initialization
**Next Steps**: Review, approve, and execute Week 1 action items
