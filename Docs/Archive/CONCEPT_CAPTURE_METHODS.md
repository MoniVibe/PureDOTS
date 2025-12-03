# Concept Capture Methods Across Projects

**Created**: 2025-11-26
**Purpose**: Document existing concept capture patterns across PureDOTS, Godgame, and Space4X to inform new project setup

---

## Overview

The tri-project ecosystem uses a **layered documentation approach** to capture concepts at different levels of abstraction and implementation readiness:

```
┌─────────────────────────────────────────────────────────────────┐
│                    CONCEPT HIERARCHY                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Game Projects (Space4X, Godgame, LastLightVR)                │
│  ├── Conceptualization/Concepts - Blue-sky game design        │
│  ├── Mechanics - Detailed mechanic specifications             │
│  └── Extension Requests → PureDOTS                             │
│                          ↓                                      │
│  PureDOTS Framework                                            │
│  ├── DesignNotes - System architecture designs                │
│  ├── ExtensionRequests - Game → Framework requests            │
│  └── Integration - Framework usage patterns                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. PureDOTS Framework Documentation

**Location**: `PureDOTS/Docs/`

### 1.1 Design Notes (`Docs/DesignNotes/`)

**Purpose**: Architectural designs for framework systems
**Scope**: Game-agnostic, reusable DOTS systems

**Structure**:
```
Docs/DesignNotes/
├── BuffSystem.md
├── GuildCurriculumSystem.md
├── RewindPatterns.md
├── RegistryDomainPlan.md
└── SystemExecutionOrder.md
```

**Characteristics**:
- Technical architecture focus
- Implementation patterns and data structures
- System integration concerns
- DOTS-specific patterns (Burst, Jobs, ECS)

**Example**: `GuildCurriculumSystem.md`
```markdown
# Guild Curriculum & Thematic Teaching System

## Concept
Guilds are thematic organizations that teach knowledge...

## Technical Architecture
### Guild Curriculum Components
[C# code examples, component schemas]

## System Integration
[How it fits into PureDOTS pipeline]
```

### 1.2 Extension Requests (`Docs/ExtensionRequests/`)

**Purpose**: Track game team requests for framework features
**Scope**: Game needs → Framework extensions

**Workflow**:
1. Game team identifies need (e.g., "Space4X needs custom sensor categories")
2. Create request: `YYYY-MM-DD-{description}.md`
3. Use `TEMPLATE.md` for structure
4. PureDOTS team reviews (APPROVED/REJECTED/DEFERRED)
5. Implementation → Move to `Done/`

**Template Structure**:
```markdown
# Extension Request: {Brief Description}

**Status**: [PENDING/APPROVED/IN PROGRESS/COMPLETED]
**Submitted**: YYYY-MM-DD
**Game Project**: {Space4X / Godgame / Both}
**Priority**: {P0 / P1 / P2}

## Use Case
[What game feature needs this?]

## Proposed Solution
[Extension Type: New Tag / Enum / Component / System]

## Impact Assessment
[Files affected, breaking changes]

## Example Usage
[Code showing how games would use it]

## Alternative Approaches Considered
[Why other solutions don't work]
```

**Key Files**:
- `README.md` - Submission process
- `TEMPLATE.md` - Request template
- `Done/` - Completed requests (archive & reference)

### 1.3 Integration Documentation (`Docs/Integration/`)

**Purpose**: How games integrate with PureDOTS
**Scope**: Usage patterns, integration guides

**Key Files**:
- `GAME_INTEGRATION_GUIDE.md` - Integration patterns
- `PUREDOTS_INTEGRATION_SPEC.md` - Canonical integration spec

### 1.4 Mechanics (`Docs/Mechanics/`)

**Purpose**: Cross-game mechanical concepts
**Scope**: Shared game mechanics applicable to multiple projects

**Structure**:
```
Docs/Mechanics/
├── _TEMPLATE.md
├── CombatLoop.md
├── ConstructionLoop.md
├── ExplorationLoop.md
├── MiningLoop.md
└── ResourceChains.md
```

---

## 2. Godgame Conceptualization

**Location**: `Godgame/Docs/Concepts/`

### 2.1 Structure

```
Docs/Concepts/
├── _Templates/         # Feature.md, Mechanic.md, System.md, Experience.md
├── Buildings/
├── Combat/
├── Core/
├── Economy/
├── Experiences/
├── Implemented/        # Completed features
├── Interaction/
├── Meta/
├── Military/
├── Miracles/
├── Progression/
├── Resources/
├── UI_UX/
├── Villagers/
├── World/
├── README.md          # Status dashboard, guidelines
├── QUICK_START.md
└── WIP_FLAGS.md       # Uncertainty markers
```

### 2.2 Philosophy

**From `README.md`**:
> "Living collection of game design ideas (**what the game should be**). Implementation details live in Truth Source docs."

**Key Principles**:
1. **Dreaming vs Building**: Concepts can be wild, Truth Sources ground in reality
2. **Status Tracking**: Draft → In Review → Approved → In Development → Implemented
3. **WIP Flags**: Mark uncertain sections `<WIP>`, `<NEEDS SPEC>`, `<FOR REVIEW>`
4. **Bidirectional Links**: Cross-reference related concepts
5. **Truth Source Integration**: Link concepts to implementation contracts

### 2.3 Status Dashboard

Tracks concept counts by category and status:

| Category | Draft | In Review | Approved | Implemented | Total |
|----------|-------|-----------|----------|-------------|-------|
| Core | 1 | 0 | 0 | 0 | 1 |
| Villagers | 2 | 0 | 0 | 0 | 2 |
| ... | ... | ... | ... | ... | ... |

### 2.4 Templates

- `Feature.md` - High-level feature concept
- `Mechanic.md` - Specific game mechanic
- `System.md` - Technical system design
- `Experience.md` - Player experience flow

---

## 3. Space4X Conceptualization

**Location**: `Space4x/Docs/Conceptualization/`

### 3.1 Structure

```
Docs/Conceptualization/
├── CorePillars.md        # Fundamental design pillars
├── DesignPrinciples.md   # Decision-making principles
├── GameVision.md         # Overarching vision
├── Features/
│   └── _TEMPLATE.md
├── Mechanics/
│   ├── _TEMPLATE.md
│   ├── MiningLoop.md
│   ├── HaulLoop.md
│   ├── CombatLoop.md
│   ├── ExplorationLoop.md
│   ├── ResourceChains.md
│   ├── TechProgression.md
│   ├── AceOfficerProgression.md
│   ├── LineageAndAggregates.md
│   └── [50+ mechanic docs]
└── README.md
```

### 3.2 Philosophy

**From `README.md`**:
> "High-level concept docs. Start with GameVision.md and CorePillars.md to align on intent. Move actionable items to Docs/TODO when ready for implementation."

**Lighter Structure**:
- Fewer categories (Features, Mechanics vs. Godgame's 14 categories)
- Focus on vision alignment before diving into mechanics
- Mechanic docs are more detailed and implementation-ready
- Direct pipeline to TODO tracking

### 3.3 Key Documents

1. **GameVision.md** - "Carrier-first 4X strategy game"
2. **CorePillars.md** - Design pillars (e.g., "Carrier as protagonist")
3. **DesignPrinciples.md** - Guiding principles for feature decisions

---

## 4. Comparison Matrix

| Aspect | PureDOTS | Godgame | Space4X |
|--------|----------|---------|---------|
| **Focus** | Framework systems | Game design concepts | Game vision & mechanics |
| **Abstraction** | Technical architecture | Player experiences + systems | Mechanics + vision |
| **Structure** | DesignNotes, ExtensionRequests | 14 categories + templates | Vision docs + Mechanics |
| **Status Tracking** | Extension request lifecycle | 5-stage status per concept | Lightweight (move to TODO) |
| **Templates** | Extension request template | 4 templates (Feature, Mechanic, System, Experience) | 2 templates (Feature, Mechanic) |
| **Workflow** | Game → Request → Review → Implement | Draft → Review → Approve → Develop | Vision → Mechanic → TODO → Implement |
| **Philosophy** | Game-agnostic contracts | "Dream big, build real" | "Vision first, then detail" |

---

## 5. Common Patterns

### 5.1 File Naming

- **PureDOTS DesignNotes**: `PascalCaseSystem.md` (e.g., `GuildCurriculumSystem.md`)
- **Extension Requests**: `YYYY-MM-DD-kebab-case.md` (e.g., `2025-11-26-aggregate-stats-utilities.md`)
- **Godgame Concepts**: `PascalCase_With_Underscores.md` (e.g., `Heal_Miracle.md`)
- **Space4X Mechanics**: `PascalCase.md` (e.g., `MiningLoop.md`)

### 5.2 Status Markers

**Godgame Style** (Explicit statuses):
```markdown
**Status:** Draft | In Review | Approved | In Development | Implemented | On Hold | Archived
```

**Extension Request Style**:
```markdown
**Status**: [PENDING] | [APPROVED] | [IN PROGRESS] | [COMPLETED] | [REJECTED] | [DEFERRED]
```

**WIP Flags** (Godgame):
```markdown
<WIP> - Work in progress
<NEEDS SPEC> - Specification needed
<FOR REVIEW> - Needs design review
<CLARIFICATION NEEDED: question here>
```

### 5.3 Cross-Referencing

All projects emphasize bidirectional links:
- Concepts → Truth Sources → Implementation
- Game concepts → Extension requests → Framework systems
- Related mechanics link to each other

---

## 6. Key Success Factors

### 6.1 Godgame's Strengths

1. **Rich categorization** - 14 categories organize diverse concepts
2. **Status dashboard** - At-a-glance progress tracking
3. **WIP flags** - Explicit uncertainty marking
4. **Template variety** - Different templates for different abstraction levels
5. **"Dream big" culture** - Encourages exploration without technical constraints

### 6.2 Space4X's Strengths

1. **Vision clarity** - Start with high-level vision docs
2. **Mechanic detail** - Mechanics are implementation-ready
3. **Lightweight process** - Fewer barriers to documentation
4. **Direct TODO pipeline** - Clear path from concept to implementation

### 6.3 PureDOTS's Strengths

1. **Extension request workflow** - Formal game → framework communication
2. **Game-agnostic focus** - Ensures reusability
3. **Technical depth** - DesignNotes are architecture-ready
4. **Archive system** - Completed requests preserved for reference

---

## 7. Recommendations for New Projects

When starting a new project (e.g., **LastLightVR**):

### 7.1 Minimal Viable Structure

```
NewProject/
└── Docs/
    ├── Conceptualization/
    │   ├── README.md
    │   ├── GameVision.md
    │   ├── CorePillars.md
    │   ├── DesignPrinciples.md
    │   ├── Features/
    │   │   └── _TEMPLATE.md
    │   └── Mechanics/
    │       └── _TEMPLATE.md
    └── INDEX.md
```

**Rationale**: Start with Space4X's lightweight structure, expand to Godgame's category system as complexity grows.

### 7.2 Growth Path

**Phase 1: Vision** (Week 1)
- Write GameVision.md
- Define CorePillars.md
- Establish DesignPrinciples.md

**Phase 2: Core Mechanics** (Weeks 2-4)
- Identify 3-5 core mechanics
- Write mechanic docs in `Mechanics/`
- Identify PureDOTS extension needs

**Phase 3: Categorization** (Month 2+)
- As concepts grow, split into categories (à la Godgame)
- Implement status dashboard
- Establish templates for recurring patterns

**Phase 4: Integration** (Ongoing)
- File extension requests to PureDOTS
- Link concepts to Truth Sources
- Track implementation status

### 7.3 Hybrid Approach

**Adopt from Godgame**:
- Status tracking per concept
- WIP flags for uncertainty
- Implemented/ folder for archives

**Adopt from Space4X**:
- Vision-first approach
- Lightweight initial structure
- Direct TODO pipeline

**Adopt from PureDOTS**:
- Extension request workflow
- Technical architecture focus (when needed)
- Game-agnostic thinking (what could be shared?)

---

## 8. Documentation Principles (Universal)

### DO ✅

1. **Be specific** - Concrete numbers, examples, edge cases
2. **Mark uncertainty** - Use WIP flags, status markers
3. **Link bidirectionally** - Update related docs when adding references
4. **Check what exists** - Read Truth Sources before designing
5. **Version control** - Commit frequently, meaningful messages
6. **Update indexes** - Keep README dashboards current

### DON'T ❌

1. **Assume systems exist** - Verify before referencing
2. **Design in isolation** - Consider system interactions
3. **Over-commit early** - It's OK to have multiple options
4. **Ignore implementation** - Check what's built before designing
5. **Orphan documents** - Always link from parent README
6. **Skip status updates** - Keep status markers current

---

## 9. Cross-Project Patterns

### 9.1 Game → Framework Flow

```
Game Concept (Space4X/Godgame/LastLightVR)
    ↓
Identify Framework Need
    ↓
Extension Request (PureDOTS/Docs/ExtensionRequests/)
    ↓
Review & Approval
    ↓
DesignNote (PureDOTS/Docs/DesignNotes/)
    ↓
Implementation (PureDOTS package)
    ↓
Game Integration (via PUREDOTS_INTEGRATION_SPEC)
    ↓
Concept Status: Implemented
```

### 9.2 Mechanic Lifecycle

```
Draft Concept → In Review → Approved
    ↓
Truth Source Contract Created
    ↓
In Development → Component/System Implementation
    ↓
Test & Validate
    ↓
Implemented → Move to Implemented/ or mark [COMPLETED]
    ↓
Document Deviations (if any)
```

---

## 10. See Also

- [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) - Tri-project architecture
- [PUREDOTS_INTEGRATION_SPEC.md](PUREDOTS_INTEGRATION_SPEC.md) - Integration patterns
- [Godgame Concepts README](../../Godgame/Docs/Concepts/README.md)
- [Space4X Conceptualization README](../../Space4x/Docs/Conceptualization/README.md)

---

**Last Updated**: 2025-11-26
**Maintainer**: Tri-Project Documentation Team
