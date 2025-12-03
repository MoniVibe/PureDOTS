# Feature Implementation Recipes

Quick-start templates for implementing new mechanics, AI behaviors, and systems across PureDOTS, Godgame, and Space4x.

## Recipe Types

### `cross-project-mechanic`
**When:** Feature spans PureDOTS + at least one game (e.g., launch/slingshot, combat, resource gathering)  
**Pattern:** Contracts → PureDOTS spine → Game adapters → ScenarioRunner  
**Example:** [Slingshot/Launch Mechanic](ContractFirst_FeatureRecipe.md)

### `puredots-infra-system`
**When:** Core infrastructure/registry/system living only in PureDOTS (e.g., telemetry, registries, provider interfaces)  
**Pattern:** Contracts (if exposing shared data) → PureDOTS implementation → Tests  
**Example:** [LightSource Registry](Recipe_LightSourceRegistry.md)

### `game-local-feature`
**When:** Feature only affects one game but uses adapters/registries cleanly (e.g., Godgame-specific villager behaviors)  
**Pattern:** PureDOTS adapters → Game-specific implementation (minimal PureDOTS changes)  
**Example:** *(TBD - add when needed)*

### `ai-behavior-or-tree`
**When:** Agent behavior, decision loops, or utility/GOAP/BT nodes (e.g., villager job behaviors, fleet targeting)  
**Pattern:** Contracts (if shared) → PureDOTS behavior spine → Game-specific behavior adapters  
**Example:** [Villager Job Behavior](Recipe_VillagerJobBehavior.md) *(coming soon)*

### `authoring-pattern`
**When:** Creating authoring components, bakers, prefabs, or SubScenes (applies to all feature types)  
**Pattern:** MonoBehaviour authoring → Baker → Config/State components → Prefab → SubScene  
**Example:** [Authoring & Prefabs](Recipe_AuthoringAndPrefabs.md)

---

## The Workflow

**Start from the template, then specialize.**

1. **Pick a recipe type** (see above)
2. **Clone** `[Recipe_Template.md](Recipe_Template.md)`
3. **Fill in** the relevant sections (skip what doesn't apply)
4. **Implement** following the recipe steps
5. **Verify** contracts match implementation, tests pass

---

## Usage Rules

**Use a recipe when:**
- Feature **touches PureDOTS plus at least one game**
- Feature is **core infrastructure** (registries, providers, shared systems)
- Feature needs **determinism guarantees** (rewind-safe, ScenarioRunner tests)

**Skip the full recipe for:**
- Tiny, one-off tweaks (a **one-paragraph sketch** loosely following the template is enough)
- Pure presentation changes (VFX, audio, UI - stays in game project)
- Bug fixes or refactors (unless they touch contracts)

**When in doubt:**
- Start from the template but **don't overfill it** - this is a thinking aid, not paperwork
- If you're spending more than 5 minutes on the recipe doc, you're overthinking it

---

## Available Recipes

### Worked Examples

- **[Slingshot/Launch Mechanic](../ContractFirst_FeatureRecipe.md)** (`cross-project-mechanic`)
  - Complete end-to-end example with contracts, PureDOTS spine, adapters, and ScenarioRunner

- **[LightSource Registry](Recipe_LightSourceRegistry.md)** (`puredots-infra-system`)
  - Simple registry pattern example (no contracts, no adapters, PureDOTS-only)

- **[Authoring & Prefabs](Recipe_AuthoringAndPrefabs.md)** (`authoring-pattern`)
  - Complete guide to authoring components, bakers, prefabs, and SubScenes

### Template

- **[Recipe Template](Recipe_Template.md)** - Start here for new features

---

## How to Add a New Recipe

1. **Implement the feature** following the template
2. **Create a new recipe doc** in this folder: `Recipe_<FeatureName>.md`
3. **Fill in** the template sections with your actual implementation details
4. **Add a link** to this README under "Available Recipes"
5. **Update** `ContractFirst_FeatureRecipe.md` or this README if the new recipe reveals a pattern worth documenting

**Time budget:** 5 minutes to create the recipe doc after implementation is done.

---

## See Also

- `[PureDOTS/Docs/Contracts.md](../Contracts.md)` - Contract definitions
- `[PureDOTS/Docs/INTEGRATION_GUIDE.md](../INTEGRATION_GUIDE.md)` - Integration patterns
- `[TRI_PROJECT_BRIEFING.md](../../../TRI_PROJECT_BRIEFING.md)` - Project overview

