# Entity Profile Glossary (Locked Terms)
**Status**: Locked (authoritative; aligns with current project vision)  
**Category**: Core / Identity / AI  
**Applies To**: Space4X, Godgame, PureDOTS

This glossary is the single source of truth for **profile-related terms** to avoid ambiguity across docs, code, and discussions.

---

## Core Terms (use these meanings)

### Profile
An entity's **stable identity layer** used for decision making and narrative weight.  
Profile facets include **Alignment**, **Outlook (ideology axes)**, **Behavior (temperament)**, and **Identity Anchors**.

### Alignment (moral tri-axis)
The universal moral vector used across games:
- **Order/Law** (lawful vs chaotic)
- **Moral/Good** (prosocial vs exploitative)
- **Purity/Integrity** (clean ideals vs corruption/shortcuts)

Values are continuous and typically normalized to `[-1..+1]`.

### Outlook (ideology axes)
Higher-level **ideological axes** used as a decision lever for culture and long-term behavior.  
Canonical axes:
- **Authority**: authoritarian ↔ egalitarian  
- **Military**: militarist ↔ pacifist  
- **Economic**: materialist ↔ spiritual  
- **Tolerance**: xenophilic ↔ xenophobic  
- **Expansion**: expansionist ↔ isolationist

Outlooks are **not** alert states and **not** loyalty stance.

### Behavior (temperament / response pattern)
The "how" layer, not ideology. Examples:
- **Boldness**, **Conviction**, **Selflessness**
- Optional: **Patience**, **Vengeful/Forgiving**, **Honesty/Deception**

Behavior informs **risk tolerance**, **aggression**, **obedience friction**, etc.

### Stance (operational posture / alert regimen)
Fast-changing, situation-driven **operational posture** for captains and crews.  
Used for **heightened alert states** and changes in regimen (patrol → alert → combat posture, etc.).

Stance is **not ideology** and **not a worldview**. It is a current regimen state that can
also reflect loyalty posture (loyalist/opportunist/mutinous) without being a permanent belief system.

### Archetype (stats)
When we say **"archetype"** in this project, we mean the **primary stat emphasis**:
- **Physique**
- **Finesse**
- **Will**
- **Wisdom**

This is the default meaning in design discussions and planning.

---

## Identity Anchors (supporting facets)

### Race / Culture
Identity anchors that bias behavior and policy, but are **not** alignment or outlook.

### Policy (derived)
Derived numbers used in hot-path systems (obedience bias, risk tolerance, aggression bias, etc.).
Policy is **derived from Profile + Dynamic State**.

### Dynamic State (not part of profile)
Short-lived or frequently updated data (morale, fatigue, cohesion, stress, grievances).
These should not be confused with Profile or Outlook.

---

## Disambiguation (do not mix these)

### Stance vs Outlook
- **Stance**: operational alert posture, changes quickly.
- **Outlook**: ideological axes, changes slowly.

### Alignment vs Outlook
- **Alignment**: core moral tri-axis (Order/Moral/Purity).
- **Outlook**: cultural/ideological axes (Authority/Military/etc.).

### Behavior vs Outlook
- **Behavior**: temperament/response patterns.
- **Outlook**: ideology/worldview.

---

## Naming Rules (for clarity)

Avoid calling unrelated systems "archetype" without a prefix. Use:
- **StatArchetype** = Physique/Finesse/Will/Wisdom emphasis (default meaning).
- **ProfileArchetype** = data-authored bundle of biases/caps/weights (if needed).
- **ECSArchetype** = Unity ECS archetype (component set).
- **AIArchetype** = AI behavior family (villager/crew/carrier/etc.).
- **FacilityArchetype** = facility production/role category.
- **RenderArchetypeKey** = render semantic key (not a profile archetype).

If a doc or system uses "ArchetypeId" without a prefix, treat it as **ambiguous** until renamed
or clarified in that context.

---

## Current Code Mapping (Space4X / PureDOTS)

- **Alignment**: `Space4X.Registry.AlignmentTriplet`
- **Outlook (ideology axes)**: `Space4X.Registry.EthicAxisValue`
- **Stance (Space4X)**: `Space4X.Registry.StanceId`, `StanceEntry`, `TopStance`
- **Stance (PureDOTS)**: `PureDOTS.Runtime.Alignment.Stance` / `StanceEntry` / `TopStance`

---

## Intent
This glossary supersedes older ambiguous usage. If another doc conflicts, defer to this glossary.
