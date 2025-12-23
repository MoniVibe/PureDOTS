# Entity Stats & Archetypes (Canonical)
**Status**: Draft (authoritative direction)  
**Category**: Core / Identity / Progression  
**Applies To**: Godgame, Space4X, shared PureDOTS  

---

## 0) Goals
- **Blank-by-default**: entities only carry stat modules they need; systems must query required components.
- **Two-layer model**: **universal core stats** (cross-game) + **domain stats** (game/scenario-specific).
- **LOD-safe**: stats must support individual ↔ aggregate collapse without changing the conceptual API.
- **Archetypes are data**: archetypes are tags + weights + policy biases, not “hard classes”.
- **Capability-gated flexibility**: any entity (person, building, flora, item) can opt into the same systems by attaching capability tags.

---

## 0.1 Blank Entity Contract (PureDOTS baseline)
- Every runtime entity is just an **Entity ID** until components are attached.
- **No assumptions** about biology, mortality, mobility, or agency.
- Everything—alignment, loyalties, skills, mood, sentience, locomotion—is optional.
- Systems must branch on **capabilities/tags**, never on prefab/category names.

### Minimal structural components
| Component | Purpose | Optional |
|-----------|---------|----------|
| `EntityName` (`PureDOTS.Runtime.Identity`) | Human-readable handles | Optional but recommended |
| `EntityKind` (`PureDOTS.Runtime.Identity`) | Semantic kind (Villager, SentientBuilding, Spirit) | Optional |
| `EntityStableId` (`PureDOTS.Runtime.Identity`) | Stable identity for save/load + determinism | Optional but recommended |
| `DynamicBuffer<CapabilityTag>` (`PureDOTS.Runtime.Identity`) | Declares abilities (IsUndead, IsBuilding, Sentient, CanCast, etc.) | Optional |
| `EntityEventLogState` + `DynamicBuffer<EntityEventLogEntry>` (`PureDOTS.Runtime.Observability`) | Per-entity ring buffer (“why did this change?”) | Optional |
| `LocalTransform` + `SpatialIndexedTag` (`Unity.Transforms` + `PureDOTS.Runtime.Spatial`) | Spatial presence + spatial grid indexing | Optional |
| `EntityOwner` + `DynamicBuffer<EntityMembership>` + `EntitySeat` (`PureDOTS.Runtime.Identity`) | Ownership/membership/seats | Optional |
| `EntityLifecycle` (`PureDOTS.Runtime.Identity`) | Lifecycle state (alive/dormant/destroyed) | Optional |
| `InventoryCapacity` + `DynamicBuffer<InventorySlot>` (`PureDOTS.Runtime.Identity`) | Container/inventory slots | Optional |
| `RelationBuffer` | Social/ownership ties | Optional |
| `Stat modules` | Core + domain stats (sections below) | Optional |

> New archetypes (sentient buildings, emotional swords, flora alliances) are created by attaching the right capability tags + stat modules; no engine code changes required.

**Authoring hooks**
- `EntityBasicsAuthoring` (PureDOTS Authoring/Identity): name/kind/capability tags
- `EntityStableIdAuthoring` (PureDOTS Authoring/Identity): stable identity
- `EntityEventLogAuthoring` (PureDOTS Authoring/Observability): per-entity event log ring buffer
- `SpatialPresenceAuthoring` (PureDOTS Authoring/Spatial): opt-in spatial grid indexing
- `EntityOwnershipAuthoring` (PureDOTS Authoring/Identity): owners/memberships/seats
- `EntityRelationsAuthoring` (PureDOTS Authoring/Social): enable relations module + seed ties
- `AuthorityCapabilityAuthoring` (PureDOTS Authoring/Identity): authority capability flags
- `EntityIntentAuthoring` (PureDOTS Authoring/Identity): intent queue
- `EntityResourceBatteriesAuthoring` (PureDOTS Authoring/Identity): energy/heat/integrity pools
- `EntityLifecycleAuthoring` / `EntityInventoryAuthoring` (PureDOTS Authoring/Identity): lifecycle + inventory
- `BlankEntityPreset` + `BlankEntityPresetAuthoring`: one-stop editor preset to stamp blank entity defaults

See also: `Concepts/Core/Capabilities_And_Affordances_System.md`.

### Collective Aggregate module (PureDOTS.Runtime.Aggregates)
- `CollectiveAggregate` component: declarative shell for any population aggregate (villages, ship crews, guilds, bands, dynasties). Tracks owner, anchor, lifecycle state (Active/Recovering/Abandoned/Corpse), counts, and pending queues.
- Buffers:
  - `CollectiveAggregateMember`: member entities + role labels + membership flags.
  - `CollectiveWorkOrder`, `CollectiveHaulingRoute`, `CollectiveConstructionApproval`: intrinsic work/hauling/approval queues—attach once per aggregate, regardless of scenario.
  - `CollectiveSocialVenue`: hearth/market/shrine/council slots discoverable by AI.
  - `CollectiveAggregateHistoryEntry`: time-sliced history retained even after abandonment (corpse stage) so UI/telemetry can inspect late.
- `CollectiveAggregateCorpseWindow`: keeps a deserted collective as a “corpse” with temporary history before the entity despawns entirely.
- Applies to both games: Godgame settlements attach it automatically; Space4X carriers can reuse it to model crews without inventing another membership system.
- Abandonment flow: when `MemberCount` drops to 0, set `State = CollectiveAggregateState.Corpse`, keep history buffers for `ExpireTick`, then cleanup. Systems query `CollectiveAggregateState` instead of scenario-specific booleans.

---

## 1) Universal Core Stats (shared contract)

### 1.1 Primary attributes (XP / growth drivers)
Canonical across games:
- **Physique**: physical capacity; feeds “might” development and some survivability.
- **Finesse**: precision/speed; feeds accuracy, mobility, technical dexterity.
- **Will**: mental fortitude/discipline; feeds magic/psionics/leadership resilience.
- **Wisdom**: general learning/cross-discipline; can also act as a global gain modifier.

### 1.2 Pools (progression resources)
PureDOTS concept docs assume **4 pools**:
- `PhysiqueXP`, `FinesseXP`, `WillXP`, `WisdomXP` (Wisdom is general pool + gain modifier lens).

### 1.3 Derived attributes (computed, cached, or overridden)
Common derived set:
- **Strength**
- **Agility**
- **Intelligence**
- **WisdomDerived** (optional if Wisdom is both attribute + pool)

Default derivation (reference-quality; games may override weights):
- `Strength = 0.8 * Physique + 0.2 * WeaponMastery`
- `Agility = 0.8 * Finesse + 0.2 * Acrobatics`
- `Intelligence = 0.6 * Will + 0.4 * Education`
- `WisdomDerived = 0.6 * Will + 0.4 * Lore`

### 1.4 Universal resource: Focus
**Focus** is an engine-level "battery-like" resource; games may *present* it as stamina/mana/concentration.
- drains by action intensity
- restores with rest
- regen/efficiency modified by attributes/skills/policies

### 1.5 Trait Axis Catalog (data-driven personality/alignment/behavior)

**Core principle:** All personality, alignment, behavior, and outlook traits are represented as **sparse axis-value pairs**, not hardcoded enums. This enables games and players to add custom axes without code changes.

#### 1.5.1 Axis Definition Structure

Each trait axis is defined by:
- **AxisId** (`FixedString32`): Unique identifier (e.g., `"LawfulChaotic"`, `"Cohesion"`, `"Xenophobia"`)
- **DisplayName**: Human-readable label for UI
- **ValueRange**: Min/max bounds (typically `-100` to `+100` for bipolar axes, `0` to `100` for unipolar)
- **SemanticTags**: Optional tags (`Alignment`, `Behavior`, `Outlook`, `Stat`, `Communication`, `Cooperation`) for system filtering
- **TriadSemantics** (optional): For bipolar axes, labels for negative/neutral/positive poles (e.g., `["Chaotic", "Neutral", "Lawful"]`)

**Storage:**
- Per-entity: `DynamicBuffer<TraitAxisValue>` where `TraitAxisValue : IBufferElementData { FixedString32 AxisId; float Value; }`
- Catalog reference: `TraitAxisSet : IComponentData { BlobAssetReference<TraitAxisCatalogBlob> Catalog; }` (optional; points to definition set)

**Blank-by-default:** Entities only store axes they have non-default values for. Systems query via helper API: `TraitAxisLookup.TryGetValue(entity, "LawfulChaotic", out value)`.

#### 1.5.2 Canonical Default Axes (PureDOTS starter set)

**Alignment axes** (bipolar, -100 to +100):
- `LawfulChaotic`: Lawful (+100) ↔ Neutral (0) ↔ Chaotic (-100)
- `GoodEvil`: Good (+100) ↔ Neutral (0) ↔ Evil (-100)
- `CorruptPure`: Corrupt (-100) ↔ Neutral (0) ↔ Pure (+100)

**Behavior axes** (bipolar, -100 to +100):
- `VengefulForgiving`: Vengeful (-100) ↔ Neutral (0) ↔ Forgiving (+100)
- `BoldCraven`: Bold (+100) ↔ Neutral (0) ↔ Craven (-100)
- `CooperativeCompetitive`: Cooperative (+100) ↔ Reserved (0) ↔ Competitive (-100)
- `WarlikePeaceful`: Warlike (+100) ↔ Neutral (0) ↔ Peaceful (-100)

**Outlook axes** (bipolar or unipolar, varies):
- `XenophobiaXenophilia`: Xenophobic (-100) ↔ Neutral (0) ↔ Xenophilic (+100)
- `AuthoritarianEgalitarian`: Authoritarian (-100) ↔ Neutral (0) ↔ Egalitarian (+100)
- `SpiritualMaterialist`: Spiritual (+100) ↔ Neutral (0) ↔ Materialist (-100)
- `MightBalanceMagic`: Might (+100) ↔ Balance (0) ↔ Magic (-100)

**Stat axes** (unipolar, 0 to 100, or derived from P/F/W pools):
- `Physique`: Physical capacity (0-100, typically derived from PhysiqueXP pool)
- `Finesse`: Precision/speed (0-100, typically derived from FinesseXP pool)
- `Will`: Mental fortitude (0-100, typically derived from WillXP pool)
- `Wisdom`: General learning/cross-discipline (0-100, typically derived from WisdomXP pool + gain modifier)

**Communication/Cooperation axes** (unipolar or bipolar, game-specific):
- `Cohesion`: Group coordination strength (0-100; high = silent cooperation, low = requires explicit comms)
- `ChaosTendency`: Intent churn / unpredictability (0-100; high = frequent replanning, ignores orders)
- `CommAptitude`: Communication clarity/reliability (0-100; affects message decode success, noise floor)

#### 1.5.3 Extending the Catalog (games + players)

**Game-level extensions:**
- Author a `TraitAxisCatalogBlob` asset with additional axes (e.g., Space4X adds `"CommandRating"`, `"TacticalAcumen"`).
- Systems query axes by ID; unknown axes return default (0 or neutral).

**Player/modder extensions:**
- Same mechanism: add axis definitions to catalog assets.
- Systems that consume traits must be **axis-agnostic** (query by ID, not hardcoded enum checks).
- Example: a mod adds `"HonorDishonor"` axis; existing utility scoring systems can reference it if authored profiles include it.

**Semantic tags enable filtering:**
- Systems can query "all axes tagged `Alignment`" or "all axes tagged `Behavior`" for bulk operations.
- Tags are metadata only; systems still query by `AxisId` for specific values.

#### 1.5.4 System Integration (how systems consume axes)

**Utility scoring (AI decisions):**
- Utility curves reference axes by ID: `Weight("Cohesion") * CohesionValue` affects "share-intent vs act-local" decisions.
- Example: High `Cohesion` → lower intent lag, more shared world-state, optional silent intent replication.

**Intent commitment (anti-thrashing):**
- `ChaosTendency` axis affects commitment timers: `CommitmentDuration = BaseDuration * (1.0 - ChaosTendency / 100.0)`.
- High chaos → shorter commitments, more frequent replanning.

**Communication clarity:**
- `CommAptitude` axis modifies message decode success, noise floor, and broadcast range.
- `Cohesion` axis reduces reliance on explicit comms (high cohesion = silent cooperation).

**Cooperation/group behavior:**
- `CooperativeCompetitive` axis biases group formation, resource sharing, and order obedience.
- `Cohesion` axis affects attention radius and shared-memory participation.

**Skill cost modifiers (soft gating):**
- Alignment/outlook axes feed into skill unlock costs (see `Alignment_Based_Skill_Gating.md`).
- Example: `"Cruel"` talents cost less for entities with negative `GoodEvil` values.

**Helper API pattern:**
```csharp
// PureDOTS.Runtime.Stats.TraitAxisLookup
public static bool TryGetValue(Entity entity, FixedString32 axisId, out float value);
public static float GetValueOrDefault(Entity entity, FixedString32 axisId, float defaultValue = 0f);
public static NativeList<TraitAxisValue> GetAllAxes(Entity entity, Allocator allocator);
public static NativeList<TraitAxisValue> GetAxesByTag(Entity entity, TraitAxisTag tag, Allocator allocator);
```

**System query example:**
```csharp
// In utility scoring system
float cohesion = TraitAxisLookup.GetValueOrDefault(entity, "Cohesion", 50f);
float chaos = TraitAxisLookup.GetValueOrDefault(entity, "ChaosTendency", 0f);
float commitmentMultiplier = 1.0f - (chaos / 100.0f);
float cooperationWeight = cohesion / 100.0f; // High cohesion = more group coordination
```

#### 1.5.5 Authoring Hooks

- **`TraitAxisCatalogAsset`** (ScriptableObject): Defines axis definitions with IDs, ranges, tags, triad semantics.
- **`TraitAxisSetAuthoring`** (MonoBehaviour): Assigns catalog reference + initial trait values to entities.
- **`TraitAxisValueAuthoring`** (MonoBehaviour): Per-entity initial trait assignments (sparse; only non-defaults).

**Baker pattern:**
- `TraitAxisSetAuthoring` bakes `TraitAxisSet` component + `DynamicBuffer<TraitAxisValue>` with initial values.
- Systems read from buffers at runtime; no hardcoded enum dependencies.

#### 1.5.6 Dynamic Trait Drift (actions shape personality over time)

**Core principle:** Trait axis values are **not static**; they drift over time based on actions, intents, and context. Entities evolve based on what they do, not just what they start as.

**Semantic meanings (canonical defaults):**
- **`LawfulChaotic`**: Predictability and adherence to rules/procedure. Lawful entities follow protocols, chaotic entities are unpredictable and rule-breaking.
- **`CorruptPure`**: Selfishness vs selflessness. Corrupt entities prioritize personal gain, pure entities prioritize others' welfare. Also reflects "means" choices (dirty methods vs clean methods).
- **`GoodEvil`**: Willingness to exploit/harm others for gain or pleasure. Good entities avoid harm, evil entities actively exploit. Nuance: good vengeful entities may slay targets, evil vengeful entities may torture them.

**Action Footprints (data-driven):**
Each action type emits **axis deltas** that modify trait values:
- **Base footprint**: Action type (e.g., "Kill", "Torture", "Mercy", "Theft", "Charity") has default axis deltas.
- **Intent modifiers**: The entity's intent (e.g., "ProtectOthers", "PersonalGain", "Duty", "Revenge") scales or flips deltas.
- **Context modifiers**: Target classification (e.g., "Innocent", "Threat", "Criminal", "ManEatingBeast") further adjusts deltas.

**Example footprints:**
- **Kill (base)**: `GoodEvil = -10`, `CorruptPure = -5`, `LawfulChaotic = -3`
- **Kill + Intent(ProtectOthers) + Context(ManEatingBeast)**: `GoodEvil = +2`, `CorruptPure = +5`, `LawfulChaotic = +3` (lawful duty, pure motive)
- **Kill + Intent(PersonalGain) + Context(Innocent)**: `GoodEvil = -20`, `CorruptPure = -15`, `LawfulChaotic = -10` (evil, corrupt, chaotic)
- **Torture (base)**: `GoodEvil = -25`, `CorruptPure = -20`, `LawfulChaotic = -5`
- **Mercy (base)**: `GoodEvil = +5`, `CorruptPure = +3`, `LawfulChaotic = +2`
- **Charity (base)**: `GoodEvil = +3`, `CorruptPure = +8`, `LawfulChaotic = +1`

**Drift mechanics:**
- **Immediate application**: Action footprints apply deltas immediately after action completion.
- **Decay over time**: Trait values drift toward neutral (0) at a configurable rate (e.g., `-0.1 per day` for alignment axes, `-0.05 per day` for behavior axes). This prevents permanent lock-in.
- **Resistance**: Entities with extreme values (near -100 or +100) resist drift more strongly (harder to change once entrenched).
- **Intent inertia**: Repeated actions with the same intent accumulate faster (entities become "more themselves" through consistent behavior).

**Storage:**
- **`ActionFootprintBlob`**: Blob asset defining action type → axis delta mappings.
- **`IntentModifierBlob`**: Blob asset defining intent → axis delta multipliers/scalars.
- **`ContextModifierBlob`**: Blob asset defining target classification → axis delta adjustments.
- **`TraitDriftState`**: Optional component tracking drift history, resistance factors, and decay rates per axis.

**System integration:**
- **Action completion events** trigger footprint application (via event stream or direct system call).
- **`TraitDriftSystem`**: Applies action footprints, applies decay, enforces resistance, updates `DynamicBuffer<TraitAxisValue>`.
- **AI systems** read current trait values via `TraitAxisLookup`; values influence utility scoring, intent commitment, comm clarity, etc.

**Example flow:**
```csharp
// Entity kills a man-eating beast to protect villagers
var action = ActionType.Kill;
var intent = EntityIntent.ProtectOthers;
var context = TargetClassification.ManEatingBeast;

// Lookup footprints
var baseFootprint = ActionFootprintCatalog.GetFootprint(action);
var intentMod = IntentModifierCatalog.GetModifier(intent);
var contextMod = ContextModifierCatalog.GetModifier(context);

// Apply deltas
TraitDriftSystem.ApplyFootprint(entity, baseFootprint, intentMod, contextMod);
// Result: GoodEvil += 2, CorruptPure += 5, LawfulChaotic += 3
// Entity becomes slightly more good, pure, and lawful

// Later: Entity tortures a prisoner for information
action = ActionType.Torture;
intent = EntityIntent.ExtractInformation;
context = TargetClassification.Prisoner;

// Apply deltas
TraitDriftSystem.ApplyFootprint(entity, baseFootprint, intentMod, contextMod);
// Result: GoodEvil -= 15, CorruptPure -= 12, LawfulChaotic -= 3
// Entity becomes more evil, corrupt, and chaotic (but intent mitigates some of the "evil" penalty)
```

**Authoring:**
- **`ActionFootprintAsset`**: ScriptableObject defining action type → axis deltas.
- **`IntentModifierAsset`**: ScriptableObject defining intent → axis multipliers.
- **`ContextModifierAsset`**: ScriptableObject defining target classification → axis adjustments.
- **`TraitDriftConfig`**: Global config for decay rates, resistance curves, drift caps.

---

## 2) Universal Combat Envelope (minimal, composable)
Combat-facing stats exist as **derived envelopes**; games may keep richer stats locally.

Suggested baseline fields (either as a single component or split modules):
- **Attack**: to-hit / engagement success
- **Defense**: dodge/block envelope
- **MaxHealth / CurrentHealth**
- **Stamina / CurrentStamina**
- **Mana / CurrentMana** *(optional; only when capability requires)*

Reference derivations (Godgame docs already use these as defaults):
- `Attack = 0.7 * Agility + 0.3 * Strength` *(or use Finesse directly if you skip Agility)*
- `Defense = 0.6 * Agility + ArmorTerm`
- `MaxHealth = 50 + 0.6 * Strength + 0.4 * Will`
- `Stamina = Strength / 10`
- `MaxMana = 0.5 * Will + 0.5 * Intelligence`

---

## 3) Needs / Condition Stats (module, not universal)
**Needs** are a module frequently used in Godgame and optionally used for Space4X crew/colonists:
- Food (0–100)
- Rest (0–100)
- Sleep (0–100)
- GeneralHealth (0–100) *(distinct from combat HP)*

Rule: if a game does not simulate needs, omit the module; systems must degrade gracefully.

---

## 4) Resistances & Modifiers (module)
Resistances and multipliers are always optional modules:
- Resistances by damage channel (e.g., Physical/Fire/Cold/Poison/Magic/Lightning/Holy/Dark)
- HealBonus multiplier
- SpellDuration multiplier
- SpellIntensity multiplier

Representation preference:
- use fixed enum IDs + compact storage (avoid string keys in runtime hot paths)

---

## 5) Domain Stats (game-specific, layered on top)

### 5.1 Godgame domain stats (examples)
Commonly referenced in Godgame docs:
- **Social**: Fame, Wealth, Reputation, Glory, Renown
- **Mood/Morale**: Mood, stress, cohesion (often linked to productivity, rebellion risk)
- **Limb/implant**: per-limb health and implant modifiers (optional depth)

### 5.2 Space4X domain stats (examples)
- `EntityOwner`, `DynamicBuffer<EntityMembership>`, `EntitySeat`, `EntitySeatAssignment`: general ownership/membership/seat scaffolding usable by both games (villager belongs to multiple bands; sentient building hosts priests; artifact slots into socket).
- `AuthorityCapabilities` + `AuthoritySeat`: tie alignment/outlook rules to explicit capability gates (override ROE, purge crew, declare emergency). Governance systems check capabilities, not prefab names.
- `EntityIntentQueue` + `DynamicBuffer<EntityIntent`: universal intent/event queue (orders, rituals, pulses) that even items or flora can use.
- `EnergyPool`, `HeatState`, `IntegrityState`: general resource batteries beyond Focus; can power mana batteries, fel-electric generators, sentient buildings.
- `EntityLifecycle` and `InventoryCapacity`/`InventorySlot`: blank entities can sleep/hibernate/die/be destroyed, and they can hold or contain other entities/items.
- `EntityEventLogState` ring buffer: local observability for “why did this change?” without shipping everything to teleporters.
- `SpatialPresenceAuthoring` opt-in: shared spatial indexing for Godgame villagers, Space4X officers, sentient mushrooms, artifacts.
- `BlankEntityPreset` asset + authoring: editor-friendly way for designers to stamp identity, stable ids, capabilities, modules, observability, intents, and spatial presence in one place.
Space4X officer/crew docs define a domain set:
- **OfficerStats**: Command, Tactics, Logistics, Diplomacy, Engineering, Resolve
- **Expertise**: typed expertise tiers (buffer)
- **ServiceTraits**: trait flags / entries (buffer)
- **PreordainProfile**: career track nudges (CombatAce, LogisticsMaven, DiplomaticEnvoy, EngineeringSavant, …)

Rule: domain stats must be readable by presentation + AI, but should not be required by core PureDOTS systems unless explicitly modularized.

### 5.3 Abstract / non-biological entities
Examples: sentient buildings, conscious items, flora-fauna hybrids, AI swarms.
- Attach only the modules they need (e.g., skip Physique/Finesse but keep Social stats or Focus).
- Capability tags (`IsStructure`, `IsItem`, `IsSentient`, `CanHostSpirit`) inform systems which calculators to run.
- Skills, relations, loyalties, archetypes can exist on any host entity.

---

## 6) Archetypes (canonical definition)

### 6.1 What an “archetype” is (and isn’t)
An archetype is:
- a **data-authored bundle** of starting modules + weights + caps + policy biases
- a **soft guidance** package (cost modifiers, unlock weights), not a hard class

An archetype is not:
- a rigid class that prevents learning other paths

### 6.2 Archetype facets (recommended)
Archetypes can be composed from multiple facets (all optional):
- **NatureArchetype**: "what kind of entity is this?" (civilian, soldier, monk, pirate, botanist, ace pilot, …)
- **RoleArchetype**: task/combat role preferences (Offense/Defense/Support/Utility/Research/Logistics)
- **CareerTrack**: long-arc progression nudges (Space4X preordain profiles; Godgame village roles)
- **CapabilityLocks**: explicit capability tags (MagicCapable/PsionicCapable/RoboticCapable/…)
- **ProfileBias**: initial trait axis values (see section 1.5). Archetypes seed `DynamicBuffer<TraitAxisValue>` with initial alignment/outlook/behavior axes (e.g., `LawfulChaotic = +80`, `Cohesion = 60`, `ChaosTendency = 20`). Games/players can extend axes beyond the canonical set.

### 6.3 Archetype output (what systems consume)
Archetypes should primarily output:
- **progression weights** (auto-spend biases, preferred next nodes)
- **cost modifiers** (soft gating by alignment/outlook/archetype, sampled from trait axes via `TraitAxisLookup`)
- **policy defaults** (risk tolerance, obedience, aggression, etc., derived from trait axis values like `BoldCraven`, `CooperativeCompetitive`, `ChaosTendency`)
- **starting skills/tiers** *(when required by scenario authoring)*
- **capability sets** (auto-applied tags when the archetype is chosen; e.g., `UndeadSpellblade` adds `IsUndead`, `CanCast` capabilities)
- **trait axis seeds** (initial values for alignment/behavior/outlook axes; systems query these at runtime via `TraitAxisLookup`)

---

## 7) Capability tags & affordances (glue for everything)
- Capability tags are lightweight components/bitsets declaring what systems may execute (e.g., `Capability.CanSwim`, `Capability.HasCirculatorySystem`, `Capability.AcceptsFocusResource`).
- Affordances describe **what can act upon the entity** (e.g., “can be wielded”, “can host population”, “accepts rituals”).
- Both are data-authored and extendable per scenario; gameplay checks capabilities instead of prefab IDs.
- Recommended storage: `DynamicBuffer<CapabilityTag>` (small) or bitset masks for hot paths.

See: `Concepts/Core/Capabilities_And_Affordances_System.md`.

---

## 8) Adoption per game (current status)

### 8.1 Godgame
- Already authors full individuals via template stats (core + domain modules + relations).
- Leans heavily on needs, social stats, limb/implant modules.
- Action item: migrate template schema to PureDOTS `IndividualDefinition` once available (reuse blank entity contract).

### 8.2 Space4X
- Currently tracks crew as aggregates; only promoted officers become explicit entities.
- Officers already expose domain stats (Command/Tactics/etc.) plus preordain profiles and traits.
- Action item: introduce a shared `IndividualDefinition`/baker so promoted officers use the same core modules as Godgame (even if many fields stay default/empty). Aggregated crew keep summary stats + capability tags until promoted.

---

## 9) Cross-doc truth sources (existing)
- PureDOTS progression: `puredots/Docs/Concepts/Core/Skill_And_Attribute_Progression.md`
- PureDOTS skill design note: `puredots/Packages/com.moni.puredots/Documentation/DesignNotes/SkillProgressionSystem.md`
- Profile/archetype facet framing: `puredots/Docs/Concepts/Core/Entity_Profile_Schema.md`
- Godgame stat schema: `godgame/Docs/Individual_Template_Stats.md`, `godgame/Docs/Individual_Stats_Requirements.md`
- Space4X officer/archetype framing: `space4x/Docs/Conceptualization/Mechanics/AceOfficerProgression.md`, `space4x/Docs/PureDOTS_Request_Space4xStats.md`


