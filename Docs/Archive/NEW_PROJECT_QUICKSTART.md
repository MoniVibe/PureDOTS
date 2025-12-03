# New Project Quick Start Guide

**Purpose**: Fast-track initialization of new game projects in the PureDOTS ecosystem
**Audience**: Developers starting a new game project (Space4X, Godgame, LastLightVR, Future projects)

---

## 🚀 5-Minute Setup

### Step 1: Create Unity Project

```bash
# Navigate to workspace
cd C:\Users\Moni\Documents\claudeprojects\unity\

# Create Unity project (via Unity Hub or CLI)
# Name format: PascalCase (e.g., LastLightVR, NextProject)
```

### Step 2: Link PureDOTS Package

**Edit `Packages/manifest.json`**:
```json
{
  "dependencies": {
    "com.moni.puredots": "file:../../PureDOTS/Packages/com.moni.puredots",
    "com.unity.entities": "1.4.2",
    "com.unity.burst": "1.8.24",
    "com.unity.collections": "2.6.2",
    "com.unity.mathematics": "1.3.2",
    "com.unity.physics": "1.0.16",
    "com.unity.inputsystem": "1.7.0"
  }
}
```

⚠️ **CRITICAL**: Use Entities **1.4.2** (NOT 1.5+). Version lock required.

### Step 3: Create Docs Folder

```bash
cd YourProject
mkdir -p Docs/Conceptualization/Features Docs/Conceptualization/Mechanics Docs/TODO
```

### Step 4: Copy Templates

**From PureDOTS, copy these starter files**:

```bash
# Copy vision docs template
cp ../PureDOTS/Docs/NEW_PROJECT_QUICKSTART.md Docs/

# Create INDEX.md
cat > Docs/INDEX.md << 'EOF'
# Documentation Index

- **Agent entry point**: `AGENTS.md`
- **Progress**: `Docs/Progress.md`
- **Vision**: `Docs/Conceptualization/GameVision.md`
- **Core Pillars**: `Docs/Conceptualization/CorePillars.md`
- **Active TODOs**: `Docs/TODO/Phase1_Initialization_TODO.md`
EOF
```

### Step 5: Write Vision Docs

**Create these 3 files** (templates in [LASTLIGHTVR_INITIALIZATION_PROPOSAL.md](LASTLIGHTVR_INITIALIZATION_PROPOSAL.md)):

1. `Docs/Conceptualization/GameVision.md` - What is the game?
2. `Docs/Conceptualization/CorePillars.md` - What are the 3-5 pillars?
3. `Docs/Conceptualization/DesignPrinciples.md` - How do we make decisions?

### Step 6: Update Tri-Project Briefing

**Edit `TRI_PROJECT_BRIEFING.md`**:
```markdown
| Project | Path | Purpose |
|---------|------|---------|
| **YourProject** | `C:\Users\Moni\Documents\claudeprojects\unity\YourProject` | [Brief description] |
```

---

## 📋 Documentation Checklist

### Week 1: Foundation

- [ ] Unity project created
- [ ] PureDOTS package linked
- [ ] Docs folder structure created
- [ ] `INDEX.md` created
- [ ] `PROJECT_SETUP.md` written (Unity setup, dependencies)
- [ ] `GameVision.md` drafted (elevator pitch, scope, pillars)
- [ ] `CorePillars.md` drafted (3-5 pillars)
- [ ] `DesignPrinciples.md` drafted (decision guidelines)
- [ ] `Progress.md` started (session log)
- [ ] `Phase1_Initialization_TODO.md` created
- [ ] TRI_PROJECT_BRIEFING.md updated

### Week 2-4: Concepts

- [ ] 3-5 core mechanic concepts written
- [ ] Templates created (`Features/_TEMPLATE.md`, `Mechanics/_TEMPLATE.md`)
- [ ] Category structure emerges (if needed)
- [ ] First extension request filed (if PureDOTS needs extending)

---

## 🗂️ Folder Structure Options

### Option A: Minimal (Space4X Style)

**Best for**: Small/focused projects, prototypes, early exploration

```
Docs/
├── INDEX.md
├── Progress.md
├── Conceptualization/
│   ├── README.md
│   ├── GameVision.md
│   ├── CorePillars.md
│   ├── DesignPrinciples.md
│   ├── Features/
│   └── Mechanics/
└── TODO/
```

**Pros**: Simple, low overhead
**Cons**: Less organization as project grows

### Option B: Categorized (Godgame Style)

**Best for**: Complex projects, multiple subsystems, long-term development

```
Docs/
├── INDEX.md
├── Progress.md
├── Conceptualization/
│   ├── README.md              # Status dashboard
│   ├── GameVision.md
│   ├── CorePillars.md
│   ├── DesignPrinciples.md
│   ├── _Templates/
│   ├── Core/
│   ├── Combat/
│   ├── Economy/
│   ├── Progression/
│   ├── UI_UX/
│   └── Implemented/
└── TODO/
```

**Pros**: Scalable, organized
**Cons**: More overhead, can be premature

### Recommendation: Start with A, evolve to B

---

## 📝 File Templates

### GameVision.md (Minimal)

```markdown
# [Project Name] - Game Vision

**Last Updated**: YYYY-MM-DD

## Elevator Pitch
[30-second description]

## Core Experience
- [What makes this unique?]
- [Target player]
- [Play session]

## Scope
- **Platform**: [PC / Console / VR / Mobile]
- **Scale**: [Prototype / Indie / AA]
- **Timeline**: [3 months? 6 months? 1 year?]

## High-Level Pillars
1. [Pillar 1] - [One sentence]
2. [Pillar 2] - [One sentence]
3. [Pillar 3] - [One sentence]

## See Also
- [CorePillars.md](CorePillars.md)
```

### CorePillars.md (Minimal)

```markdown
# [Project Name] - Core Pillars

**Last Updated**: YYYY-MM-DD

## Pillar 1: [Name]

**Definition**: [What this means]

**In Practice**:
- ✅ [Aligns with pillar]
- ❌ [Violates pillar]

---

## Pillar 2: [Name]
[Same structure]

---

## Pillar 3: [Name]
[Same structure]
```

### DesignPrinciples.md (Minimal)

```markdown
# [Project Name] - Design Principles

**Last Updated**: YYYY-MM-DD

## Gameplay Principles

### [Principle Name]
- **Rule**: [The principle]
- **Why**: [Reasoning]
- **Examples**:
  - ✅ [Good example]
  - ❌ [Bad example]

---

## Technical Principles

### Performance Standards
- **Target FPS**: [30 / 60 / 90 / 120]
- **Entity Count**: [Target entity budget]
- **Platform**: [Constraints]

---

## Integration with PureDOTS

### When to Extend PureDOTS
- ✅ [Example of good extension]
- ❌ [Example of game-specific feature]
```

---

## 🔗 Extension Request Workflow

### When to File Extension Request

**DO file request when**:
- ✅ Feature is game-agnostic (2+ projects could use it)
- ✅ Extends framework capabilities (not game logic)
- ✅ Improves developer experience (tooling, debugging)

**DON'T file request when**:
- ❌ Feature is game-specific (only your project needs it)
- ❌ Already exists in PureDOTS (check first!)
- ❌ Can be implemented in game layer

### How to File Request

1. **Create file**: `PureDOTS/Docs/ExtensionRequests/YYYY-MM-DD-feature-name.md`
2. **Use template**: Copy from `ExtensionRequests/TEMPLATE.md`
3. **Fill sections**:
   - Use case (why you need it)
   - Proposed solution (what to add)
   - Impact assessment (what changes)
   - Example usage (how to use it)
4. **Commit & push**
5. **Wait for review** (PureDOTS team tags: APPROVED/REJECTED/DEFERRED)

---

## 🎯 Best Practices

### Documentation

**DO ✅**:
- Write vision docs first (before coding)
- Keep concepts separate from implementation
- Mark uncertainty with `<WIP>` flags
- Link related concepts bidirectionally
- Update status markers as work progresses

**DON'T ❌**:
- Document implementation details in concept docs
- Assume systems exist (verify first!)
- Orphan documents (always link from INDEX.md)
- Let docs go stale (update regularly)

### Code Organization

**DO ✅**:
- Keep game code in `Assets/YourProject/`
- Follow DOTS patterns (Burst, Jobs, ECS)
- Check PureDOTS types exist before using them
- Write tests for core systems
- Document deviations from concepts

**DON'T ❌**:
- Put game logic in PureDOTS package
- Duplicate PureDOTS functionality
- Modify PureDOTS directly (file extension request instead)
- Ignore DOTS coding patterns (see TRI_PROJECT_BRIEFING.md)

### Integration

**DO ✅**:
- Reference PureDOTS via package
- Follow integration spec (PUREDOTS_INTEGRATION_SPEC.md)
- File extension requests for shared needs
- Coordinate with other game teams

**DON'T ❌**:
- Fork PureDOTS (breaks updates)
- Work around limitations (file request instead)
- Upgrade Entities version independently (locked to 1.4.2)

---

## 📚 Essential Reading

**Before Starting**:
1. [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) - Ecosystem architecture
2. [CONCEPT_CAPTURE_METHODS.md](CONCEPT_CAPTURE_METHODS.md) - Documentation patterns
3. [PUREDOTS_INTEGRATION_SPEC.md](PUREDOTS_INTEGRATION_SPEC.md) - Integration guide

**During Development**:
1. [FoundationGuidelines.md](../FoundationGuidelines.md) - Coding guidelines
2. [ExtensionRequests/README.md](ExtensionRequests/README.md) - Extension workflow
3. Other game projects' docs (learn from existing patterns)

**Reference**:
- [Godgame Concepts README](../../Godgame/Docs/Concepts/README.md)
- [Space4X Conceptualization README](../../Space4x/Docs/Conceptualization/README.md)
- [LASTLIGHTVR_INITIALIZATION_PROPOSAL.md](LASTLIGHTVR_INITIALIZATION_PROPOSAL.md) (detailed example)

---

## ⚡ Quick Commands

```bash
# Create new concept
cd Docs/Conceptualization/Mechanics
touch NewMechanic.md
echo "# New Mechanic\n\n**Status**: Draft\n**Created**: $(date +%Y-%m-%d)" > NewMechanic.md

# Check if PureDOTS type exists
grep -r "struct TypeName" ../../PureDOTS/Packages --include="*.cs"

# Build verification
dotnet build

# Run tests
# (Unity) Test Runner window
# (CLI) Unity -batchmode -projectPath . -runTests -testPlatform editmode
```

---

## 🆘 Common Issues

### Issue: Build errors after linking PureDOTS

**Solution**: Check Entities version = 1.4.2 exactly
```bash
grep "com.unity.entities" Packages/manifest.json
# Should show: "com.unity.entities": "1.4.2"
```

### Issue: Concepts feel overwhelming

**Solution**: Start minimal (GameVision, CorePillars, DesignPrinciples only)
- Don't create categories until you have 5+ concepts
- Don't use templates until patterns emerge
- Focus on vision first, organization later

### Issue: Unsure if feature should be PureDOTS extension

**Solution**: Ask:
1. Could 2+ other projects use this?
2. Is it game-agnostic?
3. Does it extend framework capabilities?

If YES to all 3 → Extension request
If NO to any → Game-specific implementation

---

## 🎓 Learning Path

**Day 1**: Read ecosystem docs
- TRI_PROJECT_BRIEFING.md
- CONCEPT_CAPTURE_METHODS.md
- One other game project's docs (Godgame or Space4X)

**Day 2-3**: Write vision
- GameVision.md
- CorePillars.md
- DesignPrinciples.md

**Week 1**: Setup & first concept
- Unity project + PureDOTS integration
- Docs folder structure
- First mechanic concept

**Week 2-4**: Core concepts
- 3-5 core mechanics
- Category structure (if needed)
- First extension request (if needed)

**Month 2+**: Scale up
- Expand categories
- Implement concepts
- File extension requests as needed

---

## ✅ Ready to Start?

1. **Choose project name** (PascalCase, no spaces)
2. **Create Unity project**
3. **Run through Week 1 checklist** (above)
4. **Read essential docs** (TRI_PROJECT_BRIEFING, PUREDOTS_INTEGRATION_SPEC)
5. **Write vision docs** (GameVision, CorePillars, DesignPrinciples)
6. **Start creating!**

---

**Created**: 2025-11-26
**Last Updated**: 2025-11-26
**Maintainer**: Tri-Project Documentation Team
