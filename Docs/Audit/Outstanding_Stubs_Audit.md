# Outstanding Stubs Audit

**Date**: 2025-01-21  
**Scope**: Combat, Sensors, Cooperation, Relations  
**Purpose**: Identify all stub systems/components needed to flesh out vision

---

## Executive Summary

**Existing Stubs**: 40+ stub files across multiple domains  
**Outstanding Stubs**: ~25 combat stubs, ~8 sensor stubs, ~12 cooperation stubs, ~10 relation stubs

**Pattern**: Stubs follow `*Stub.cs` (service stubs) and `*StubComponents.cs` + `*StubSystems.cs` (component/system stubs) naming.

---

## 1. Combat Stubs

### 1.1 Existing Combat Stubs

**Found**:
- ✅ `CombatServiceStub.cs` - Basic engagement/damage/threat rating stubs

**Status**: Minimal - only service-level stub exists

---

### 1.2 Required Combat Stubs (From Audit)

#### Formation Combat (Godgame)

**Missing Stubs**:
- ❌ `FormationCombatStub.cs` - Stub formation bonuses (defense/attack/morale multipliers)
- ❌ `FormationCombatStubComponents.cs` - FormationType, FormationIntegrity, FormationBonus components
- ❌ `FormationCombatStubSystems.cs` - Applies fake formation bonuses to combat stats

**Purpose**: Feed fake formation bonuses while real integrity math lands. Enables downstream systems (AI, telemetry) to wire up.

---

#### Morale Wave (Godgame)

**Missing Stubs**:
- ❌ `MoraleWaveStub.cs` - Stub morale propagation service
- ❌ `MoraleWaveStubComponents.cs` - MoraleWave, MoraleThreshold, MoralePropagation components
- ❌ `MoraleWaveStubSystems.cs` - Emits scripted morale waves (unit breaks → propagate to neighbors)

**Purpose**: Emit fake morale waves so downstream telemetry/AI can integrate before true propagation exists.

---

#### Module Targeting (Space4X)

**Missing Stubs**:
- ❌ `ModuleTargetingStub.cs` - Stub module selection service
- ❌ `ModuleTargetingStubComponents.cs` - ModuleTarget, ModuleHitDetection components
- ❌ `ModuleTargetingStubSystems.cs` - Selects fake module targets per hit

**Purpose**: Select pretend subsystems per hit, lets presentation/UI iterate before geometric hit detection exists.

---

#### Module Damage Routing (Space4X)

**Missing Stubs**:
- ❌ `ModuleDamageRouterStub.cs` - Stub damage routing service
- ❌ `ModuleDamageRouterStubComponents.cs` - ModuleDamageRouter, ModuleHitResult components
- ❌ `ModuleDamageRouterStubSystems.cs` - Routes damage to fake module buckets

**Purpose**: Route damage to synthetic module health pools, proves capability disable logic without real hull layouts.

---

#### Capability Disable (Space4X)

**Missing Stubs**:
- ❌ `CapabilityDisableStub.cs` - Stub capability disable service
- ❌ `CapabilityDisableStubComponents.cs` - ModuleCapability, CapabilityState components
- ❌ `CapabilityDisableStubSystems.cs` - Toggles movement/fire/shield flags when modules "fail"

**Purpose**: Disable capabilities (movement, firing, shields) when stubbed modules "destroyed", unblocks downstream command logic.

---

#### 3D Formation (Space4X)

**Missing Stubs**:
- ❌ `Formation3DStub.cs` - Stub 3D positioning service
- ❌ `Formation3DStubComponents.cs` - CombatPosition3D, VerticalEngagementRange, 3DAdvantage components
- ❌ `Formation3DStubSystems.cs` - Assigns fake vertical offsets and advantage multipliers

**Purpose**: Assign canned vertical offsets and advantage multipliers for fleets until spatial math ready.

---

#### Squad Cohesion (Godgame)

**Missing Stubs**:
- ❌ `CohesionEffectStub.cs` - Stub cohesion → combat effectiveness service
- ❌ `CohesionEffectStubComponents.cs` - SquadCohesion, CohesionThreshold components
- ❌ `CohesionEffectStubSystems.cs` - Scales attack/defense with fake cohesion curves

**Purpose**: Scale combat effectiveness (accuracy, damage, defense) with fake cohesion values, enables balance passes.

---

#### Formation Tactics (Godgame)

**Missing Stubs**:
- ❌ `FormationTacticStub.cs` - Stub tactic execution service
- ❌ `FormationTacticStubComponents.cs` - FormationTactic, TacticState components
- ❌ `FormationTacticStubSystems.cs` - Drives fake Charge/Hold/Flank states

**Purpose**: Drive canned tactic states so animation/AI layers can integrate prior to full planner.

---

#### Combat State Extensions

**Missing Stubs**:
- ❌ `CombatStateFormationStub.cs` - Stub formation combat states (FormationEngaged, FormationBroken, FormationRouted)
- ❌ `CombatStateModuleStub.cs` - Stub module operational states (ModuleDestroyed, ModuleDamaged, ModuleOffline)

**Purpose**: Extend existing CombatState enum with formation/module states for state machine integration.

---

## 2. Sensor Stubs

### 2.1 Existing Sensor Stubs

**Found**:
- ✅ `SensorServiceStub.cs` - Basic sensor rig registration and interrupt submission
- ✅ `SensorInterruptStubComponents.cs` - Sensor interrupt components
- ✅ `SensorInterruptStubSystems.cs` - Sensor interrupt systems

**Status**: Basic sensor service exists, but perception pipeline stubs missing

---

### 2.2 Required Sensor Stubs

#### Signal Field System

**Missing Stubs**:
- ❌ `SignalFieldStub.cs` - Stub signal field service (smell/sound/EM emissions)
- ❌ `SignalFieldStubComponents.cs` - SensorySignalEmitter, SignalFieldCell, SignalFieldConfig components
- ❌ `SignalFieldStubSystems.cs` - Updates fake signal field cells, samples for entities

**Purpose**: Emit fake smell/sound/EM signals into spatial grid, enables perception sampling without real field diffusion.

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 3.2 - Signal Field System

---

#### Sense Organ System

**Missing Stubs**:
- ❌ `SenseOrganStub.cs` - Stub sense organ service (eyes, ears, sensors)
- ❌ `SenseOrganStubComponents.cs` - SenseOrganState buffer (organ type, channels, gain, condition)
- ❌ `SenseOrganStubSystems.cs` - Applies organ properties to detection calculations

**Purpose**: Model individual sense organs with per-organ properties (gain, condition, noise floor) for granular detection.

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 3.3 - Sense Organs

---

#### Medium Context System

**Missing Stubs**:
- ❌ `MediumContextStub.cs` - Stub medium type service (Vacuum/Gas/Liquid/Solid)
- ❌ `MediumContextStubComponents.cs` - MediumType, MediumProperties components
- ❌ `MediumContextStubSystems.cs` - Filters channels by medium (sound requires gas/liquid, EM blocked by obstacles)

**Purpose**: Filter detection channels by medium type (Space4X: vacuum outside hull = no hearing/smell, gas inside = hearing works).

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 3.4 - Medium Context

---

#### Stealth Detection System

**Missing Stubs**:
- ❌ `StealthDetectionStub.cs` - Stub stealth vs perception checks
- ❌ `StealthDetectionStubComponents.cs` - StealthLevel, StealthModifiers components
- ❌ `StealthDetectionStubSystems.cs` - Rolls stealth checks vs perception, applies environmental modifiers

**Purpose**: Stealth levels (Exposed/Concealed/Hidden/Invisible) vs perception checks with environmental modifiers (light, terrain, movement).

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 3.5 - Stealth & Perception

---

#### Perception Channel Integration

**Missing Stubs**:
- ❌ `PerceptionChannelStub.cs` - Stub channel-based detection service
- ❌ `PerceptionChannelStubComponents.cs` - SenseCapability, SensorSignature, PerceivedEntity components (may already exist, verify)
- ❌ `PerceptionChannelStubSystems.cs` - Channel-based detection using spatial queries

**Purpose**: Integrate PerceptionChannel enum with detection systems (Vision/Hearing/Smell/EM/Gravitic/Paranormal).

**Note**: Components may already exist in `PerceptionComponents.cs` - verify if systems are stubbed or real.

---

## 3. Cooperation Stubs

### 3.1 Existing Cooperation Stubs

**Found**:
- ✅ `AggregateServiceStub.cs` - Basic aggregate formation
- ✅ `AggregateStubComponents.cs` - Aggregate components
- ✅ `AggregateStubSystems.cs` - Aggregate systems
- ✅ `BandFormationSystem.cs` - Real band formation (not stub)

**Status**: Basic aggregation exists, but cooperation-specific stubs missing

---

### 3.2 Required Cooperation Stubs

#### Magic Circle Cooperation (Godgame)

**Missing Stubs**:
- ❌ `MagicCircleStub.cs` - Stub magic circle service (mana pooling, rituals)
- ❌ `MagicCircleStubComponents.cs` - MagicCircle, CircleMember, ManaPool components
- ❌ `MagicCircleStubSystems.cs` - Coordinates mana pooling, ritual casting

**Purpose**: Coordinate multiple casters pooling mana and casting rituals together.

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 5.1 - Magic Circles

---

#### Coordinated Combat (Godgame)

**Missing Stubs**:
- ❌ `CoordinatedCombatStub.cs` - Stub coordinated volleys/service
- ❌ `CoordinatedCombatStubComponents.cs` - VolleyCoordinator, VolleyMember components
- ❌ `CoordinatedCombatStubSystems.cs` - Coordinates simultaneous attacks (volleys, shield walls)

**Purpose**: Coordinate simultaneous attacks (archers volley together, shield wall advances together).

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 5.2 - Coordinated Volleys

---

#### Mutual Care (Godgame)

**Missing Stubs**:
- ❌ `MutualCareStub.cs` - Stub mutual care service (healing, protection)
- ❌ `MutualCareStubComponents.cs` - CareRelationship, CarePriority components
- ❌ `MutualCareStubSystems.cs` - Entities prioritize helping allies (healing, protection, rescue)

**Purpose**: Entities prioritize helping allies (healing wounded, protecting vulnerable, rescuing downed).

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 5.3 - Mutual Care

---

#### Crew Coordination (Space4X)

**Missing Stubs**:
- ❌ `CrewCoordinationStub.cs` - Stub crew coordination service
- ❌ `CrewCoordinationStubComponents.cs` - CrewMember, CrewRole, CrewTask components
- ❌ `CrewCoordinationStubSystems.cs` - Coordinates crew tasks (pilot-operator links, hangar operations)

**Purpose**: Coordinate crew tasks (pilot-operator communication, hangar operations, multi-tier support).

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 5.4 - Crew Coordination

---

#### Production Cooperation (Godgame)

**Missing Stubs**:
- ❌ `ProductionCooperationStub.cs` - Stub production cooperation service
- ❌ `ProductionCooperationStubComponents.cs` - ProductionTeam, ProductionRole components
- ❌ `ProductionCooperationStubSystems.cs` - Coordinates production tasks (blacksmiths + miners, builders + haulers)

**Purpose**: Coordinate production chains (blacksmiths work with miners, builders coordinate with haulers).

**Vision Reference**: `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` Section 5.5 - Production Cooperation

---

#### Group Knowledge Sharing

**Missing Stubs**:
- ❌ `GroupKnowledgeStub.cs` - Stub group knowledge sharing service
- ❌ `GroupKnowledgeStubComponents.cs` - GroupKnowledge, KnowledgeDiffusion components (may exist, verify)
- ❌ `GroupKnowledgeStubSystems.cs` - Shares knowledge within groups (locations, threats, resources)

**Purpose**: Share knowledge within groups (band knows where resources are, fleet shares intel).

**Note**: `GroupKnowledgeComponents.cs` exists - verify if systems are stubbed or real.

---

## 4. Relations Stubs

### 4.1 Existing Relations Stubs

**Found**:
- ✅ `DiplomacyServiceStub.cs` - Basic diplomacy service
- ✅ `PersonalRelationComponents.cs` - Real personal relations (not stub)
- ✅ `EntityRelationComponents.cs` - Real entity relations (not stub)

**Status**: Basic relation components exist, but relation update/decay stubs missing

---

### 4.2 Required Relations Stubs

#### Relation Update System

**Missing Stubs**:
- ❌ `RelationUpdateStub.cs` - Stub relation update service
- ❌ `RelationUpdateStubComponents.cs` - RelationEvent, RelationChange components
- ❌ `RelationUpdateStubSystems.cs` - Updates relations based on interactions (positive/negative events)

**Purpose**: Update relations based on interactions (helping = +relation, attacking = -relation, betrayal = major -relation).

**Vision Reference**: `EntityRelationComponents.cs` has InteractionOutcome enum but no update system.

---

#### Relation Decay System

**Missing Stubs**:
- ❌ `RelationDecayStub.cs` - Stub relation decay service
- ❌ `RelationDecayStubComponents.cs` - RelationDecayConfig components
- ❌ `RelationDecayStubSystems.cs` - Decays unused relations over time (familiarity fades)

**Purpose**: Decay relations over time if entities don't interact (familiarity fades, intensity decreases).

**Vision Reference**: `EntityRelationComponents.cs` has RelationConfig with DecayRatePerDay but no system.

---

#### Faction Relations

**Missing Stubs**:
- ❌ `FactionRelationStub.cs` - Stub faction relation service
- ❌ `FactionRelationStubComponents.cs` - FactionRelation, FactionRelationship components (may exist, verify)
- ❌ `FactionRelationStubSystems.cs` - Updates faction-level relations (alliance, war, vassal)

**Purpose**: Track faction-level relations (alliance, neutral, hostile, at war, vassal, overlord).

**Note**: `CombatComponents.cs` has `FactionRelationships` - verify if systems are stubbed or real.

---

#### Personal Relation Formation

**Missing Stubs**:
- ❌ `PersonalRelationFormationStub.cs` - Stub personal relation formation service
- ❌ `PersonalRelationFormationStubComponents.cs` - RelationFormationEvent components
- ❌ `PersonalRelationFormationStubSystems.cs` - Forms personal relations (family, friends, rivals) from interactions

**Purpose**: Form personal relations from interactions (shared experiences → friendship, betrayal → enemy, family bonds).

**Vision Reference**: `PersonalRelationComponents.cs` has PersonalRelationType enum but no formation system.

---

#### Trust System

**Missing Stubs**:
- ❌ `TrustSystemStub.cs` - Stub trust calculation service
- ❌ `TrustSystemStubComponents.cs` - TrustLevel, TrustEvent components
- ❌ `TrustSystemStubSystems.cs` - Calculates trust based on reliability (keeps promises, betrays, etc.)

**Purpose**: Calculate trust levels based on reliability (keeps promises = +trust, betrays = -trust).

**Vision Reference**: `PersonalRelationComponents.cs` has Trust field but no calculation system.

---

#### Grudge System (Godgame)

**Missing Stubs**:
- ❌ `GrudgeSystemStub.cs` - Stub grudge tracking service
- ❌ `GrudgeSystemStubComponents.cs` - Grudge, GrudgeEvent components (may exist, verify)
- ❌ `GrudgeSystemStubSystems.cs` - Tracks grudges (betrayals, murders, thefts) and revenge seeking

**Purpose**: Track grudges (betrayals, murders, thefts) and drive revenge-seeking behavior.

**Note**: `GrudgeHelpers.cs` exists - verify if systems are stubbed or real.

---

## 5. Summary: Outstanding Stubs

### Combat Stubs (25 total)

**Formation Combat**:
- `FormationCombatStub.cs` + Components + Systems (3 files)

**Morale**:
- `MoraleWaveStub.cs` + Components + Systems (3 files)

**Module Targeting**:
- `ModuleTargetingStub.cs` + Components + Systems (3 files)

**Module Damage**:
- `ModuleDamageRouterStub.cs` + Components + Systems (3 files)

**Capability Disable**:
- `CapabilityDisableStub.cs` + Components + Systems (3 files)

**3D Formation**:
- `Formation3DStub.cs` + Components + Systems (3 files)

**Cohesion**:
- `CohesionEffectStub.cs` + Components + Systems (3 files)

**Tactics**:
- `FormationTacticStub.cs` + Components + Systems (3 files)

**State Extensions**:
- `CombatStateFormationStub.cs` (1 file)
- `CombatStateModuleStub.cs` (1 file)

---

### Sensor Stubs (8 total)

- `SignalFieldStub.cs` + Components + Systems (3 files)
- `SenseOrganStub.cs` + Components + Systems (3 files)
- `MediumContextStub.cs` + Components + Systems (3 files)
- `StealthDetectionStub.cs` + Components + Systems (3 files)
- `PerceptionChannelStub.cs` + Components + Systems (3 files)

**Note**: Some components may already exist - verify before creating stubs.

---

### Cooperation Stubs (12 total)

- `MagicCircleStub.cs` + Components + Systems (3 files)
- `CoordinatedCombatStub.cs` + Components + Systems (3 files)
- `MutualCareStub.cs` + Components + Systems (3 files)
- `CrewCoordinationStub.cs` + Components + Systems (3 files)
- `ProductionCooperationStub.cs` + Components + Systems (3 files)
- `GroupKnowledgeStub.cs` + Components + Systems (3 files)

**Note**: `GroupKnowledgeComponents.cs` exists - verify if systems are stubbed or real.

---

### Relations Stubs (10 total)

- `RelationUpdateStub.cs` + Components + Systems (3 files)
- `RelationDecayStub.cs` + Components + Systems (3 files)
- `FactionRelationStub.cs` + Components + Systems (3 files)
- `PersonalRelationFormationStub.cs` + Components + Systems (3 files)
- `TrustSystemStub.cs` + Components + Systems (3 files)
- `GrudgeSystemStub.cs` + Components + Systems (3 files)

**Note**: Many relation components exist - verify if systems are stubbed or real before creating.

---

## 6. Verification Checklist

Before creating stubs, verify:

1. **Components Exist?** Check if components already exist in `Runtime/Runtime/`:
   - `PerceptionComponents.cs` - SenseCapability, SensorSignature, PerceivedEntity
   - `GroupKnowledgeComponents.cs` - GroupKnowledge, KnowledgeDiffusion
   - `GrudgeHelpers.cs` - Grudge components
   - `CombatComponents.cs` - FactionRelationships

2. **Systems Exist?** Check if systems already exist in `Runtime/Systems/`:
   - Perception systems (PerceptionUpdateSystem, etc.)
   - Relation systems (RelationUpdateSystem, etc.)
   - Cooperation systems (MagicCircleSystem, etc.)

3. **Stubs vs Real?** If components/systems exist, determine if they're:
   - Real implementations (keep, don't stub)
   - Partial implementations (stub missing parts)
   - Empty shells (replace with stubs)

---

## 7. Implementation Priority

### Phase 1: Critical Combat Stubs
1. FormationCombatStub (enables formation combat)
2. ModuleTargetingStub (enables module targeting)
3. ModuleDamageRouterStub (enables module damage)
4. CapabilityDisableStub (enables capability disable)

### Phase 2: High-Value Stubs
5. MoraleWaveStub (enables morale propagation)
6. CohesionEffectStub (enables cohesion effects)
7. SignalFieldStub (enables signal-based perception)
8. RelationUpdateStub (enables relation updates)

### Phase 3: Enhancement Stubs
9. FormationTacticStub (enables tactics)
10. Formation3DStub (enables 3D positioning)
11. MagicCircleStub (enables magic cooperation)
12. CoordinatedCombatStub (enables coordinated attacks)

---

## 8. Stub Pattern

**Service Stub Pattern**:
```csharp
// [TRI-STUB] This is an ahead-of-time stub. Safe to compile, does nothing at runtime.
using Unity.Entities;

namespace PureDOTS.Runtime.<Domain>
{
    public static class <Domain>ServiceStub
    {
        public static void <Method>(in Entity entity, <params>) { }
        public static <ReturnType> <Method>(in Entity entity) => default;
    }
}
```

**Component Stub Pattern**:
```csharp
// [TRI-STUB] Stub components for <purpose>
using Unity.Entities;

namespace PureDOTS.Runtime.<Domain>
{
    public struct <Component>Stub : IComponentData
    {
        // Minimal fields to satisfy compile-time dependencies
    }
}
```

**System Stub Pattern**:
```csharp
// [TRI-STUB] Stub system for <purpose>
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.<Domain>
{
    [BurstCompile]
    [UpdateInGroup(typeof(<SystemGroup>))]
    public partial struct <System>Stub : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
    }
}
```

---

## 9. Additional Stubs from Documentation Scan

### 9.1 Ritual System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Ritual_Magic_System.md`

**Missing Stubs**:
- ❌ `RitualSystemStub.cs` - Stub ritual service (phase progression, intensity, completion)
- ❌ `RitualSystemStubComponents.cs` - Ritual, RitualPhase, RitualParticipant components
- ❌ `RitualSystemStubSystems.cs` - Drives ritual phases, concentration checks, completion bonuses

**Purpose**: Universal ritual system (war chants, epic songs, rallying speeches, magic rituals, ship formations). Phase-based progression with intensity curves.

---

### 9.2 Memory & History System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Memory_And_History_Integration.md`

**Missing Stubs**:
- ❌ `MemorySystemStub.cs` - Stub memory service (canonical records, interpretations, claims)
- ❌ `MemorySystemStubComponents.cs` - Memory, CanonicalRecord, MemoryInterpretation, Claim components
- ❌ `MemorySystemStubSystems.cs` - Creates canonical records, derives interpretations, propagates claims

**Purpose**: Single canonical history per major event, interpreted differently by holders. Hot/cold split, handles, lazy derivations.

---

### 9.3 Teaching & Learning System Stubs

**Documentation**: `puredots/Docs/Mechanics/MemoriesAndLessons.md`

**Missing Stubs**:
- ❌ `TeachingSystemStub.cs` - Stub teaching service (language, spells, crafting recipes)
- ❌ `TeachingSystemStubComponents.cs` - TeachingSession, Lesson, StudentProgress components
- ❌ `TeachingSystemStubSystems.cs` - Coordinates teaching sessions, tracks progress, completes lessons

**Purpose**: Entities teach languages, spells, crafting recipes to others. Progress tracking, proficiency levels, teaching efficiency.

---

### 9.4 Reputation System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Reputation_System.md`

**Missing Stubs**:
- ❌ `ReputationSystemStub.cs` - Stub reputation service (domain scores, witness spreading)
- ❌ `ReputationSystemStubComponents.cs` - EntityReputation, ReputationDomain, ReputationEvent components
- ❌ `ReputationSystemStubSystems.cs` - Updates reputation scores, spreads through witnesses, affects opportunities

**Purpose**: Tracks how entities are perceived (trading, combat, diplomacy, magic domains). Action-based, spreads organically, affects opportunities.

---

### 9.5 Authority & Command System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Authority_And_Command_Hierarchies.md`

**Missing Stubs**:
- ❌ `AuthoritySystemStub.cs` - Stub authority service (seats, delegation, orders)
- ❌ `AuthoritySystemStubComponents.cs` - AuthorityBody, AuthoritySeat, AuthoritySeatOccupant, AuthorityDelegation components
- ❌ `AuthoritySystemStubSystems.cs` - Manages authority seats, delegates orders, tracks mutiny/coup

**Purpose**: Who decides for aggregates (villages, ships). Authority seats, delegation rules, order issuance, mutiny/coup logic.

---

### 9.6 Tactical Commands System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Tactical_Commands_And_Formations.md`

**Missing Stubs**:
- ❌ `TacticalCommandSystemStub.cs` - Stub tactical command service (orders, compliance, formation training)
- ❌ `TacticalCommandSystemStubComponents.cs` - TacticalCommand, CommandCompliance, FormationTraining components
- ❌ `TacticalCommandSystemStubSystems.cs` - Issues commands, tracks compliance, trains formations

**Purpose**: Leaders issue orders (follow, attack, defend, formations) through communication. Formation training, cohesion mechanics, relation-based coordination.

---

### 9.7 Biodeck & Biosculpting System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Biodeck_And_Biosculpting_System.md`

**Missing Stubs**:
- ❌ `BiodeckSystemStub.cs` - Stub biodeck service (climate sculpting, biome classification)
- ❌ `BiodeckSystemStubComponents.cs` - BioDeckModule, BioDeckCell, BioSculptCommand components
- ❌ `BiodeckSystemStubSystems.cs` - Applies sculpt commands, updates climate, classifies biomes

**Purpose**: Terraformable "nanosoil" surface (Startopia-style). Climate field sculpting, biome classification with hysteresis, vegetation as patches.

---

### 9.8 Genealogy Mixing System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Genealogy_Mixing_System.md`

**Missing Stubs**:
- ❌ `GenealogySystemStub.cs` - Stub genealogy service (mixing, trait inheritance, hybrid creation)
- ❌ `GenealogySystemStubComponents.cs` - Genealogy, GenealogyComposition, TraitInheritance components
- ❌ `GenealogySystemStubSystems.cs` - Mixes genealogies, inherits traits, creates hybrid entities

**Purpose**: Mix genealogies to create hybrid entities (cat people, dragonkin, dryads). Composable genetic building blocks, trait inheritance, hybrid stabilization.

---

### 9.9 Morality & Outlook System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Morality_Reputation_And_Outlook_Shifts.md`

**Missing Stubs**:
- ❌ `MoralitySystemStub.cs` - Stub morality service (moral vector, ethics stances, appraisal)
- ❌ `MoralitySystemStubComponents.cs` - MoralVector, EthicsStance, MoralEvent, Outlook components
- ❌ `MoralitySystemStubSystems.cs` - Appraises moral events, updates outlook, applies culture weights

**Purpose**: Culture-weighted norm appraisal of events. Moral vector (Care/Harm, Fairness/Cheating, etc.), ethics stances, reputation vs outlook split.

---

### 9.10 Exploration & Discovery System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Exploration_And_Discovery.md`

**Missing Stubs**:
- ❌ `ExplorationSystemStub.cs` - Stub exploration service (layered exploration, discovery packets)
- ❌ `ExplorationSystemStubComponents.cs` - ExplorationSite, DiscoveryPacket, KnowledgeClaim components
- ❌ `ExplorationSystemStubSystems.cs` - Runs exploration layers, generates discovery packets, updates knowledge stores

**Purpose**: Multi-layer exploration (broad scan → surface scan → expeditions → archaeological digs). Knowledge ownership, diffusion, mastery, decay.

---

### 9.11 Miracle Framework Stubs

**Documentation**: `puredots/Docs/Mechanics/MiracleFramework.md`

**Missing Stubs**:
- ❌ `MiracleSystemStub.cs` - Stub miracle service (rain, fire, lightning, heal, time effects)
- ❌ `MiracleSystemStubComponents.cs` - MiracleRequest, RainCloudComponent, FirePulse, LightningArc, HealPulse, TimeDistortion components
- ❌ `MiracleSystemStubSystems.cs` - Validates miracles, spawns effect entities, resolves effects

**Purpose**: Deterministic miracle command framework (rain, water burst, fire, lightning, heal, time effects). Resource hooks, validation, execution.

---

### 9.12 Information Propagation System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Information_Propagation.md`

**Missing Stubs**:
- ❌ `InformationPropagationStub.cs` - Stub information service (claims, evidence, beliefs, networks)
- ❌ `InformationPropagationStubComponents.cs` - ClaimInstance, EvidenceEvent, BeliefOverlay, NetworkConfig components
- ❌ `InformationPropagationStubSystems.cs` - Propagates claims through networks, updates beliefs, manages evidence

**Purpose**: Perceived reality system (true/false/unknown), fog-of-war persistence, rumors/hearsay, multi-network comms, deception/verification.

---

### 9.13 Dialogue Content System Stubs

**Documentation**: `puredots/Docs/Concepts/Core/Dialogue_Content_System.md`

**Missing Stubs**:
- ❌ `DialogueSystemStub.cs` - Stub dialogue service (topics, statements, responses, influence)
- ❌ `DialogueSystemStubComponents.cs` - SocialDialogue, TacticalDialogue, StrategicDialogue, PersonalDialogue, TransactionalDialogue components
- ❌ `DialogueSystemStubSystems.cs` - Generates dialogue topics, processes statements, applies influence

**Purpose**: Defines what entities communicate about (topics, statements, questions, responses). Profile sharing, intent communication, influence attempts, deception checks.

---

## 10. Updated Summary: All Outstanding Stubs

### Combat Stubs (25 files)
- Formation Combat (3 files)
- Morale Waves (3 files)
- Module Targeting (3 files)
- Module Damage Routing (3 files)
- Capability Disable (3 files)
- 3D Formation (3 files)
- Cohesion Effects (3 files)
- Formation Tactics (3 files)
- State Extensions (2 files)

### Sensor Stubs (8 files)
- Signal Field (3 files)
- Sense Organs (3 files)
- Medium Context (3 files)
- Stealth Detection (3 files)
- Perception Channel Integration (3 files)

### Cooperation Stubs (12 files)
- Magic Circles (3 files)
- Coordinated Combat (3 files)
- Mutual Care (3 files)
- Crew Coordination (3 files)
- Production Cooperation (3 files)
- Group Knowledge (3 files)

### Relations Stubs (10 files)
- Relation Updates (3 files)
- Relation Decay (3 files)
- Faction Relations (3 files)
- Personal Relation Formation (3 files)
- Trust System (3 files)
- Grudge System (3 files)

### Additional System Stubs (39 files)
- Ritual System (3 files)
- Memory & History (3 files)
- Teaching & Learning (3 files)
- Reputation System (3 files)
- Authority & Command (3 files)
- Tactical Commands (3 files)
- Biodeck & Biosculpting (3 files)
- Genealogy Mixing (3 files)
- Morality & Outlook (3 files)
- Exploration & Discovery (3 files)
- Miracle Framework (3 files)
- Information Propagation (3 files)
- Dialogue Content (3 files)

**Total Outstanding Stubs**: ~94 stub files across all domains

---

## 5. Power, Resources, Production, Vegetation, Climate Stubs

### 5.1 Power Generation & Storage

**Documentation**: `Docs/Concepts/Core/Power_And_Battery_System.md`

**Existing Implementation**:
- ✅ `PowerSourceComponents.cs` - PowerSourceState, PowerSourceDefBlob, various source types (Solar, Wind, Reactor, etc.)
- ✅ `PowerConsumerComponents.cs` - PowerConsumerState, PowerConsumerDefBlob
- ✅ `PowerNetworkComponents.cs` - PowerNetworkRef, PowerNetwork
- ✅ `PowerInfrastructureComponents.cs` - Power infrastructure tracking
- ✅ `PowerFlowSolveSystem.cs` - Power flow calculation
- ✅ `PowerSourceUpdateSystem.cs` - Generator updates
- ✅ `PowerNetworkBuildSystem.cs` - Network construction

**Missing Stubs** (from Power_And_Battery_System.md):
- ❌ `PowerBatteryStub.cs` - Battery storage service stub
- ❌ `PowerBatteryStubComponents.cs` - PowerBattery component (MaxCapacity, CurrentStored, ChargeRate, DischargeRate, SelfDischargeRate, ChargeEfficiency, DischargeEfficiency, Health, CycleCount, TechLevel)
- ❌ `PowerBatteryStubSystems.cs` - Battery charging/discharging/self-discharge systems
- ❌ `PowerBankStub.cs` - Power bank service stub (for weapon/shield surge capacity)
- ❌ `PowerBankStubComponents.cs` - PowerBankRequirement, WeaponPowerDemand, ShieldPowerDemand, PowerBank component
- ❌ `PowerBankStubSystems.cs` - Power bank charge/discharge for weapons/shields
- ❌ `PowerDistributionStub.cs` - Distribution loss calculation service stub
- ❌ `PowerDistributionStubComponents.cs` - PowerDistribution component (InputPower, TransmissionLoss, OutputPower, DistributionEfficiency, ConduitDamage, TechLevel)
- ❌ `PowerDistributionStubSystems.cs` - Distribution loss calculation system

**Purpose**: Power system has generation and consumption, but missing battery storage, power banks for surge capacity, and distribution loss modeling.

**Stub Count**: 9 files (3 battery + 3 power bank + 3 distribution)

---

### 5.2 Resource Logistics & Transport

**Documentation**: `Docs/Concepts/Core/Resource_Logistics_And_Transport.md`

**Existing Implementation**:
- ✅ `ResourceComponents.cs` - Basic resource tracking (ResourceTypeId, ResourceSourceState, StorehouseConfig, ResourceChunkState)
- ✅ `ResourceProcessorComponents.cs` - ResourceProcessorConfig, ResourceProcessorState, ProcessingStationRegistry
- ✅ `ResourceRecipeComponents.cs` - Recipe system
- ✅ `ResourceRegistrySystem.cs` - Resource registry
- ✅ `ResourceProcessingSystem.cs` - Processing system
- ✅ `StorehouseLedgerComponents.cs` - Storehouse inventory tracking

**Missing Stubs** (from Resource_Logistics_And_Transport.md):
The documentation describes a comprehensive logistics kernel with:
- NodeStore (logistics endpoints: tiles, districts, settlements, stations, warehouses, mobile transports)
- ContainerStore (storage modules/compartments)
- BatchStore (resource lots with quality, rarity, decay, legality, ownership)
- OrderStore (intent to move quantities)
- ShipmentStore (execution instances)
- RouteStore (planned itineraries)
- ServiceStore (service nodes: docks, loaders, customs, refuel, repair, gatejump)
- KnowledgeStore (faction knowledge for routing decisions)
- Routing system (edge-based graph with cost-term stack)
- Reservation system (inventory, capacity, service reservations)
- Behavior profiles & policy modules

**Missing Stubs**:
- ❌ `ResourceLogisticsStub.cs` - Logistics kernel service stub
- ❌ `ResourceLogisticsStubComponents.cs` - NodeId, ContainerId, BatchId, OrderId, ShipmentId, RouteId, ServiceId, NodeStore, ContainerStore, BatchStore, OrderStore, ShipmentStore, RouteStore, ServiceStore components
- ❌ `ResourceLogisticsStubSystems.cs` - Order planning, routing, reservation, dispatch, transit, delivery systems
- ❌ `ResourceRoutingStub.cs` - Routing service stub
- ❌ `ResourceRoutingStubComponents.cs` - EdgeId, EdgeState, RouteCache, CostTermStack components
- ❌ `ResourceRoutingStubSystems.cs` - Route computation, cache invalidation, cost calculation systems
- ❌ `ResourceReservationStub.cs` - Reservation service stub
- ❌ `ResourceReservationStubComponents.cs` - InventoryReservation, CapacityReservation, ServiceReservation components
- ❌ `ResourceReservationStubSystems.cs` - Reservation creation, TTL/cancellation, release systems

**Purpose**: Current resource system handles basic resource tracking and processing, but missing the full logistics transport system (multi-hop supply chains, routing, reservations, knowledge-driven decisions).

**Stub Count**: 9 files (3 logistics + 3 routing + 3 reservation)

---

### 5.3 Production & Crafting

**Documentation**: `Docs/Concepts/Production/Crafting_And_Construction.md` (mostly stub)

**Existing Implementation**:
- ✅ `ProductionComponents.cs` - BusinessProduction, ProductionJob, BusinessInventory
- ✅ `ProductionJobProgressSystem.cs` - Job progress tracking
- ✅ `ProductionJobSchedulingSystem.cs` - Job scheduling
- ✅ `ProductionJobCompletionSystem.cs` - Job completion
- ✅ `ProductionQualitySystem.cs` - Quality calculation
- ✅ `CraftingServiceStub.cs` - Basic crafting service stub exists
- ✅ `CraftingStubComponents.cs` - Crafting stub components exist
- ✅ `CraftingStubSystems.cs` - Crafting stub systems exist

**Missing Stubs** (from Crafting_And_Construction.md):
The documentation is mostly a stub, but identifies key concepts:
- Recipe systems (what can be crafted from what)
- Construction progress (multi-step building)
- Quality variation (crafted items have quality levels)
- Material requirements (need specific resources)
- Crafting skills and expertise

**Status**: Production system has basic job tracking, but crafting/construction system is mostly stubbed. Documentation is incomplete, so stubs are already in place.

**Stub Count**: 0 files (already stubbed)

---

### 5.4 Vegetation

**Documentation**: `Docs/Concepts/Core/Environmental_Systems.md` (mentions vegetation)

**Existing Implementation**:
- ✅ `VegetationComponents.cs` - Comprehensive vegetation system (VegetationId, VegetationLifecycle, VegetationHealth, VegetationProduction, VegetationConsumption, VegetationReproduction, VegetationSeasonal, VegetationSpawnConfig, VegetationHarvestCommand)
- ✅ `VegetationGrowthSystem.cs` - Growth simulation
- ✅ `VegetationHarvestSystem.cs` - Harvest processing
- ✅ `VegetationReproductionSystem.cs` - Reproduction/spreading
- ✅ `VegetationHealthSystem.cs` - Health tracking
- ✅ `VegetationDecaySystem.cs` - Decay simulation
- ✅ `VegetationSpawnSystem.cs` - Spawn management
- ✅ `VegetationEnvironmentComponents.cs` - Environmental integration
- ✅ `VegetationNeedsComponents.cs` - Needs tracking
- ✅ `VegetationSpeciesBlob.cs` - Species definitions

**Missing Stubs**: None identified - vegetation system appears comprehensive.

**Stub Count**: 0 files

---

### 5.5 Climate & Weather

**Documentation**: `Docs/Concepts/Core/Environmental_Systems.md`

**Existing Implementation**:
- ✅ `ClimateComponents.cs` - ClimateVector, ClimateGridRuntimeCell, ClimateControlSource
- ✅ `ClimateProfileComponents.cs` - Climate profile definitions
- ✅ `WeatherComponents.cs` - Weather state
- ✅ `MoistureComponents.cs` - Moisture tracking
- ✅ `WindComponents.cs` - Wind vectors
- ✅ `ClimateStateUpdateSystem.cs` - Climate state updates
- ✅ `ClimateOscillationSystem.cs` - Climate oscillation
- ✅ `ClimateControlSystem.cs` - Climate control (miracles, structures)
- ✅ `GlobalClimateSystem.cs` - Global climate simulation

**Missing Stubs**: None identified - climate system appears comprehensive.

**Stub Count**: 0 files

---

### Summary: Power, Resources, Production, Vegetation, Climate

**Total Missing Stubs**: 18 files
- Power Generation & Storage: 9 files
- Resource Logistics & Transport: 9 files
- Production & Crafting: 0 files (already stubbed)
- Vegetation: 0 files (complete)
- Climate: 0 files (complete)

**Key Findings**:
1. **Power**: Missing battery storage, power banks (for weapon/shield surge), and distribution loss modeling
2. **Resources**: Missing comprehensive logistics transport system (routing, reservations, multi-hop supply chains)
3. **Production**: Already stubbed, documentation incomplete
4. **Vegetation**: Complete implementation
5. **Climate**: Complete implementation

---

**Total Outstanding Stubs**: ~112 stub files across all domains (94 previous + 18 new)

---

## 6. Additional System Stubs from Extended Documentation Scan

### 6.1 Agency & Sentience System Stubs

**Documentation**: `Docs/Concepts/Core/Agency_And_Sentience.md`

**Missing Stubs**:
- ❌ `AgencySystemStub.cs` - Stub agency service (control contests, domain arbitration)
- ❌ `AgencySystemStubComponents.cs` - AgencySelf, ControlLink, ResolvedControl, AgencyDomain components
- ❌ `AgencySystemStubSystems.cs` - Resolves control contests per domain, applies pressure/resistance

**Purpose**: Layered arbitration for who controls what (self vs controller). Domains (SelfBody, Movement, Work, Combat, Communications). Pressure vs resistance contests.

**Stub Count**: 3 files

---

### 6.2 Arguments System Stubs

**Documentation**: `Docs/Concepts/Core/Arguments_System.md`

**Missing Stubs**:
- ❌ `ArgumentsSystemStub.cs` - Stub argument service (decision protocols, relationship deltas)
- ❌ `ArgumentsSystemStubComponents.cs` - ArgumentSession, ArgumentParticipant, ArgumentOption, ArgumentOutcome components
- ❌ `ArgumentsSystemStubSystems.cs` - Runs argument sessions, computes utilities, resolves conflicts

**Purpose**: Bounded decision protocols for conflicting intents/orders. Produces selected options, relationship deltas, escalation events. Only leaders/advisors argue.

**Stub Count**: 3 files

---

### 6.3 Bay & Platform Combat System Stubs

**Documentation**: `Docs/Concepts/Core/Bay_And_Platform_Combat.md`

**Missing Stubs**:
- ❌ `BayCombatStub.cs` - Stub bay/platform combat service (combat positions, firing arcs)
- ❌ `BayCombatStubComponents.cs` - CombatPosition, FiringArc, BayState, BayOccupant components
- ❌ `BayCombatStubSystems.cs` - Manages bay states, assigns positions, validates firing arcs

**Purpose**: Parent entities (carriers, ships, fortifications) provide combat positions for child entities (mechs, crews, turrets). Firing arcs, bay states (Closed/Opening/Open/Closing/Damaged).

**Stub Count**: 3 files

---

### 6.4 Capabilities & Affordances System Stubs

**Documentation**: `Docs/Concepts/Core/Capabilities_And_Affordances_System.md`

**Missing Stubs**:
- ❌ `CapabilitiesSystemStub.cs` - Stub capabilities service (anatomy-driven capabilities, affordances)
- ❌ `CapabilitiesSystemStubComponents.cs` - MobilityCapability, ManipulationCapability, Affordance, TraversalEdge components
- ❌ `CapabilitiesSystemStubSystems.cs` - Computes capabilities from anatomy, matches affordances, validates traversal

**Purpose**: Anatomy-driven capabilities (limbs/stats determine what's possible). World-authored affordances (ladders, ledges, climb surfaces). Navigation graph with traversal edges.

**Stub Count**: 3 files

---

### 6.5 Catalytic Crystal System Stubs

**Documentation**: `Docs/Concepts/Core/Catalytic_Crystal_System.md`

**Missing Stubs**:
- ❌ `CatalyticCrystalStub.cs` - Stub crystal service (growth, spread, mutation, corruption)
- ❌ `CatalyticCrystalStubComponents.cs` - CatalyticCrystal, CrystalField, CrystalGrowth, CrystalMutation components
- ❌ `CatalyticCrystalStubSystems.cs` - Crystal growth/spread, mutation mechanics, environmental corruption

**Purpose**: Self-propagating crystalline material (Tiberium-like). Spreads autonomously, high energy density, corrupts environment. Exponential growth, mutation mechanics, containment strategies.

**Stub Count**: 3 files

---

### 6.6 Conflict Resolution System Stubs

**Documentation**: `Docs/Concepts/Core/Conflict_Resolution.md`

**Missing Stubs**:
- ❌ `ConflictResolutionStub.cs` - Stub conflict service (multi-party conflicts, negotiations, treaties)
- ❌ `ConflictResolutionStubComponents.cs` - Conflict, Party, Side, Representative, Proposal, Treaty components
- ❌ `ConflictResolutionStubSystems.cs` - Conflict state machine, negotiation protocols, treaty enforcement

**Purpose**: Moddable framework for conflicts between any entities. Implicit → explicit progression, non-violent resolution, escalation/de-escalation, multi-party via coalitions, aftermath artifacts.

**Stub Count**: 3 files

---

### 6.7 Diggable Terrain System Stubs

**Documentation**: `Docs/Concepts/Core/Diggable_Terrain_System.md`

**Missing Stubs**:
- ❌ `DiggableTerrainStub.cs` - Stub terrain modification service (chunked terrain, diff-based edits)
- ❌ `DiggableTerrainStubComponents.cs` - TerrainChunk, TerrainVoxelRuntime, TerrainChunkDirty, DigCommand components
- ❌ `DiggableTerrainStubSystems.cs` - Terrain edits, navigation updates, versioned path invalidation

**Purpose**: Diggable terrain with efficient navigation updates. Chunked terrain with diff-based edits, derived navigation graphs, versioned path invalidation. Avoids "rebake hell".

**Stub Count**: 3 files

---

### 6.8 Entity Lifecycle System Stubs

**Documentation**: `Docs/Concepts/Core/Entity_Lifecycle.md`

**Missing Stubs**:
- ❌ `EntityLifecycleStub.cs` - Stub lifecycle service (creation, aging, death, post-death, resurrection)
- ❌ `EntityLifecycleStubComponents.cs` - IdentityRecord, EmbodimentState, AnatomyInstance, SoulRecord, NeuralStack, EstateCase components
- ❌ `EntityLifecycleStubSystems.cs` - Lifecycle transitions, death outcomes, resurrection mechanics

**Purpose**: Engine-agnostic lifecycle (birth/death/resurrection). Identity persistence, optional embodiments, anatomy detail, anchors (soul/stack/remains), estate/bank.

**Stub Count**: 3 files

---

### 6.9 Locomotion System Stubs

**Documentation**: `Docs/Concepts/Core/Locomotion_System.md`

**Missing Stubs**:
- ❌ `LocomotionSystemStub.cs` - Stub locomotion service (multi-modal movement, runtime switching)
- ❌ `LocomotionSystemStubComponents.cs` - LocomotionMode, LocomotionCapability, LocomotionState, MovementDirectionality components
- ❌ `LocomotionSystemStubSystems.cs` - Mode switching, capability validation, movement execution

**Purpose**: Multi-modal locomotion (walk/run/fly/hover/glide). Runtime switching, directionality (mono/bi/planar/volumetric), entity-agnostic, physics-integrated.

**Stub Count**: 3 files

---

### 6.10 Luck Stat System Stubs

**Documentation**: `Docs/Concepts/Core/Luck_Stat_System.md`

**Missing Stubs**:
- ❌ `LuckSystemStub.cs` - Stub luck service (roll modification, lifetime impact)
- ❌ `LuckSystemStubComponents.cs` - LuckStat, LuckModifier components
- ❌ `LuckSystemStubSystems.cs` - Applies luck to rolls, manages modifiers, calculates effective luck

**Purpose**: Player-modifiable stat (-100 to +100) influencing all random rolls. Universal roll modifier, lifetime impact, flexible integration.

**Stub Count**: 3 files

---

### 6.11 Mana-Powered Infrastructure System Stubs

**Documentation**: `Docs/Concepts/Core/Mana_Powered_Infrastructure_And_Energy_Alignment.md`

**Missing Stubs**:
- ❌ `ManaInfrastructureStub.cs` - Stub mana energy service (mana as power, alignment variants)
- ❌ `ManaInfrastructureStubComponents.cs` - ManaSource, ManaConsumer, ManaBattery, EnergyAlignment components
- ❌ `ManaInfrastructureStubSystems.cs` - Mana generation/consumption, alignment interactions, conversion

**Purpose**: Mana as first-class energy resource (parallel to power). Alignment variants (Fel/Mana/Arcane). Mana-powered buildings, batteries, infrastructure. Alignment compatibility/conversion/conflicts.

**Stub Count**: 3 files

---

### 6.12 Mobile Cities & Land Carriers System Stubs

**Documentation**: `Docs/Concepts/Core/Mobile_Cities_And_Land_Carriers.md`

**Missing Stubs**:
- ❌ `MobileCityStub.cs` - Stub mobile city service (mobile platforms, aggregate movement)
- ❌ `MobileCityStubComponents.cs` - MobileCityPlatform, BuildingSlot, MobileCityState components
- ❌ `MobileCityStubSystems.cs` - Platform movement, building attachment, aggregate mobility

**Purpose**: Mobile settlements (treads, walker limbs, hover, anti-grav). Tech-gated progression, aggregate movement, land carriers with combat bays.

**Stub Count**: 3 files

---

### 6.13 Patience & Circadian Systems Stubs

**Documentation**: `Docs/Concepts/Core/Patience_And_Circadian_Systems.md`

**Missing Stubs**:
- ❌ `PatienceSystemStub.cs` - Stub patience service (patience-initiative relationship, activity thresholds)
- ❌ `PatienceSystemStubComponents.cs` - Patience, PatienceActivity, CircadianRhythm, SleepPattern components
- ❌ `PatienceSystemStubSystems.cs` - Patience depletion, circadian effects, sleep scheduling

**Purpose**: Patience inversely tied to initiative. Circadian rhythms (night owls, early birds). Behavioral consequences (impatient = rash decisions, patient = miss opportunities).

**Stub Count**: 3 files

---

### 6.14 Rival Gods Diplomacy System Stubs

**Documentation**: `Docs/Concepts/Core/Rival_Gods_Diplomacy_System.md`

**Missing Stubs**:
- ❌ `RivalGodsStub.cs` - Stub god diplomacy service (pantheon relations, domain costs, power unlocks)
- ❌ `RivalGodsStubComponents.cs` - GodEntity, GodDomain, GodRelation, MiracleDomainCost components
- ❌ `RivalGodsStubSystems.cs` - Manages god relations, miracle costs, relation progression, power unlocks

**Purpose**: Meta-layer where natural phenomena controlled by rival deities. Using miracles costs mana and affects relations. Relation progression unlocks powers (Interloper → Tolerated → Accepted → Allied → Conjoined).

**Stub Count**: 3 files

---

### 6.15 Sentient Flora-Fauna Hybrids System Stubs

**Documentation**: `Docs/Concepts/Core/Sentient_Flora_Fauna_Hybrids.md`

**Missing Stubs**:
- ❌ `SentientHybridStub.cs` - Stub hybrid service (propagation, sentience awakening, reproduction)
- ❌ `SentientHybridStubComponents.cs` - HybridEntity, HybridPropagation, SentienceAwakening, HybridReproduction components
- ❌ `SentientHybridStubSystems.cs` - Vegetation propagation, sentience awakening, hybrid reproduction

**Purpose**: Sentient plant-animal hybrids (ents, treants, dryads). Propagate vegetation, vegetation awakens as sentient hybrids, hybrids reproduce. Self-propagating populations.

**Stub Count**: 3 files

---

### 6.16 Skill & Attribute Progression System Stubs

**Documentation**: `Docs/Concepts/Core/Skill_And_Attribute_Progression.md`

**Missing Stubs**:
- ❌ `SkillProgressionStub.cs` - Stub skill progression service (use-based, training, teaching, XP-based)
- ❌ `SkillProgressionStubComponents.cs` - SkillTrack, ExperiencePool, ProgressionLOD, Mastery components
- ❌ `SkillProgressionStubSystems.cs` - XP gain, skill progression, mastery evaluation, synergy calculation

**Purpose**: Hybrid simulation progression (use-based + training + teaching + XP). Continuous values + tier milestones. Soft-gated specialization, drift over time, partial respec.

**Stub Count**: 3 files

---

### 6.17 Additional Mechanics Stubs

**From `Docs/Mechanics/` folder**:

#### Carrier Architecture (Space4X)
- ❌ `CarrierArchitectureStub.cs` + Components + Systems (3 files)
- **Purpose**: Ship classifications (Capital Ships, Regular Ships, Vessels/Crafts), mount-based modules, weapon research/manufacturing

#### Death Continuity & Undead Origins
- ❌ `DeathContinuityStub.cs` + Components + Systems (3 files)
- **Purpose**: Death registry, corpse tracking, undead creation from corpses, spirit manifestation, continuity

#### Diplomacy Dynamics
- ❌ `DiplomacyDynamicsStub.cs` + Components + Systems (3 files)
- **Purpose**: Outlook-driven affinities, passive diplomatic drift, ambassadors, treaties, trade agreements

#### Divine Hand State Machine (Godgame)
- ❌ `DivineHandStub.cs` + Components + Systems (3 files)
- **Purpose**: Unified state machine for hand interactions (pickup, throw, miracles, siphoning, forcing)

#### Entity Hierarchy
- ❌ `EntityHierarchyStub.cs` + Components + Systems (3 files)
- **Purpose**: Ownership layers, asset ownership, allegiance & splintering, multi-layer allegiances

#### Facility Archetypes
- ❌ `FacilityArchetypesStub.cs` + Components + Systems (3 files)
- **Purpose**: Shared vs host-specific archetypes, tier scaling (Small → Titanic), facility slots

#### Floating Islands & Rogue Orbiters
- ❌ `FloatingIslandsStub.cs` + Components + Systems (3 files)
- **Purpose**: Temporary mobile locations (floating islands Godgame, rogue orbiters Space4X), limited-time exploration

#### Instance Portals & Procedural Dungeons
- ❌ `InstancePortalsStub.cs` + Components + Systems (3 files)
- **Purpose**: Portal spawning, procedural instance generation, isolated challenge zones, completion conditions

#### Intel & Visibility System (Space4X)
- ❌ `IntelVisibilityStub.cs` + Components + Systems (3 files)
- **Purpose**: Fog-of-war, intel decay, sensor mechanics, information warfare, sector visibility

#### Limb & Organ Grafting
- ❌ `LimbGraftingStub.cs` + Components + Systems (3 files)
- **Purpose**: Surgical grafting, property inheritance, compatibility, social consequences, body horror

#### Material Properties System
- ❌ `MaterialPropertiesStub.cs` + Components + Systems (3 files)
- **Purpose**: Trait-based material properties (Hardness, Toughness, Ductility), crafting role requirements, substitution penalties

#### Runewords & Synergies
- ❌ `RunewordsStub.cs` + Components + Systems (3 files)
- **Purpose**: Socket systems, runeword formation, enchantment synergies, consumable combos, augment set bonuses

#### Stealth Framework
- ❌ `StealthFrameworkStub.cs` + Components + Systems (3 files)
- **Purpose**: Deterministic light-aware stealth, perception sensors, suspicion routing, spectral sight

#### Tech Progression & Diffusion
- ❌ `TechProgressionStub.cs` + Components + Systems (3 files)
- **Purpose**: Research sources, diffusion mechanics, diplomatic sharing, tech adoption

#### Underground Spaces & Hidden Bases
- ❌ `UndergroundSpacesStub.cs` + Components + Systems (3 files)
- **Purpose**: Terrain layers, excavation mechanics, underground settlement types, access points

#### Wonder Construction
- ❌ `WonderConstructionStub.cs` + Components + Systems (3 files)
- **Purpose**: Multi-stage monument construction, professional workers, manufactured resources, long-term projects

**Mechanics Stub Count**: 18 systems × 3 files = 54 files

---

### Summary: Extended Documentation Scan

**Total Additional Missing Stubs**: 75 files
- Core Concepts: 16 systems × 3 files = 48 files
- Mechanics: 18 systems × 3 files = 54 files
- (Some overlap with previous sections - counted separately for completeness)

**Key Findings**:
1. **Agency & Control**: Missing control contest system for multi-entity control
2. **Arguments & Decisions**: Missing bounded decision protocols for conflicts
3. **Combat Extensions**: Missing bay/platform combat, capabilities/affordances
4. **World Systems**: Missing diggable terrain, underground spaces, floating islands
5. **Lifecycle**: Missing comprehensive lifecycle system (birth/death/resurrection)
6. **Progression**: Missing skill progression, luck stat, tech diffusion
7. **Diplomacy**: Missing conflict resolution, rival gods, diplomacy dynamics
8. **Special Systems**: Missing catalytic crystals, sentient hybrids, mana infrastructure

---

**Total Outstanding Stubs**: ~187 stub files across all domains (112 previous + 75 new)

---

## 7. Intent, Espionage, Ambition, Glory, Family, Deceit Stubs

### 7.1 Intent System Stubs

**Documentation**: `Docs/Concepts/Core/Perception_Action_Intent_Summary.md`

**Existing Implementation**:
- ✅ `EntityIntent` component exists in Perception_Action_Intent_Summary.md
- ✅ `IntentMode` enum (Idle, MoveTo, Attack, Flee, UseAbility, ExecuteOrder, etc.)
- ✅ `Interrupt` buffer (InterruptType, InterruptPriority)
- ✅ Basic intent components in AmbitionStubComponents.cs (IntentState)

**Missing Stubs**:
- ❌ `IntentServiceStub.cs` - Intent service (set intent, clear intent, validate intent)
- ❌ `IntentStubComponents.cs` - Full EntityIntent component, IntentMode enum, Interrupt buffer
- ❌ `IntentStubSystems.cs` - Intent processing, interrupt handling, intent validation

**Purpose**: Core AI intent system - entities declare what they want to do (intent) based on interrupts and goals. Intent drives action selection.

---

### 7.2 Espionage & Infiltration Stubs

**Documentation**: `Docs/Concepts/Stealth/Infiltration_Detection_Agnostic.md`, `Docs/Archive/ExtensionRequests/Done/2025-11-26-espionage-infiltration-utilities.md`

**Existing Implementation**:
- ✅ `InfiltrationComponents.cs` in Runtime/AI/Infiltration/ (InfiltrationState, CounterIntelligence, CoverIdentity, ExtractionPlan, GatheredIntel, Investigation)
- ✅ Infiltration helpers and algorithms documented

**Missing Stubs**:
- ❌ `InfiltrationServiceStub.cs` - Infiltration service (start infiltration, level up, detect exposure, extract)
- ❌ `InfiltrationStubComponents.cs` - InfiltrationState, CounterIntelligence, CoverIdentity, ExtractionPlan, GatheredIntel, Investigation (move from Runtime to Stubs)
- ❌ `InfiltrationStubSystems.cs` - Infiltration progress, suspicion tracking, detection checks, extraction execution

**Purpose**: Spies and double agents infiltrate organizations, build infiltration levels (Contact → Embedded → Trusted → Influential → Subverted), manage cover identities, gather intel, and extract when exposed.

---

### 7.3 Ambition, Desire, Wish Stubs

**Documentation**: `Docs/Features/Motivation.md`

**Existing Implementation**:
- ✅ `AmbitionServiceStub.cs` - Ambition service exists
- ✅ `AmbitionStubComponents.cs` - AmbitionState, DesireElement, IntentState, TaskElement
- ✅ `AmbitionStubSystems.cs` - AmbitionFlowSystem (ambition → desire → intent → tasks)

**Status**: ✅ **ALREADY STUBBED** - Ambition/Desire system is fully stubbed

**Purpose**: Motivation system with 5 layers (Dreams, Aspirations, Desires, Ambitions, Wishes). Entities have goals that drive behavior. Initiative and loyalty affect goal selection.

---

### 7.4 Glory, Renown, Reputation Stubs

**Documentation**: `Docs/Concepts/Core/Reputation_System.md`, `Docs/Archive/ExtensionRequests/Done/2025-11-26-prestige-reputation-system.md`

**Existing Implementation**:
- ✅ Reputation system documented (EntityReputation, ReputationDomain, ReputationEvent, ReputationSpreadSystem)
- ✅ Prestige system documented (Prestige, PrestigeTier, ReputationScore, PrestigeStress, Notoriety)

**Missing Stubs**:
- ❌ `ReputationServiceStub.cs` - Reputation service (modify reputation, spread gossip, calculate reputation tier)
- ❌ `ReputationStubComponents.cs` - EntityReputation, ReputationDomain, ReputationEvent, ReputationTier
- ❌ `ReputationStubSystems.cs` - Reputation spread, gossip propagation, reputation decay
- ❌ `PrestigeServiceStub.cs` - Prestige service (add prestige, calculate tier, check unlocks)
- ❌ `PrestigeStubComponents.cs` - Prestige, PrestigeTier, ReputationScore, PrestigeStress, Notoriety, PrestigeUnlock, PrestigeEvent
- ❌ `PrestigeStubSystems.cs` - Prestige decay, tier calculation, unlock checking, stress management

**Purpose**: 
- **Reputation**: How entities are perceived by others (Trading, Combat, Diplomacy, Magic, Crafting domains). Reputation spreads through witnesses and gossip.
- **Prestige**: Achievement-based prestige (Unknown → Known → Notable → Renowned → Famous → Legendary → Mythic). Prestige unlocks options and affects opportunities.
- **Glory/Renown**: Fame from achievements, affects leadership elections, unlocks options.

---

### 7.5 Family & Dynasty Stubs

**Documentation**: `Docs/Concepts/Core/Genealogy_Mixing_System.md`, `Docs/Concepts/Core/Entity_Lifecycle.md`, `Docs/Concepts/Politics/Leadership_And_Succession.md`

**Existing Implementation**:
- ✅ Genealogy system documented (Genealogy, GenealogyComposition, trait inheritance)
- ✅ Entity lifecycle documented (birth, death, resurrection, inheritance)
- ✅ Leadership succession documented (dynasty members, bloodline, inheritance)

**Missing Stubs**:
- ❌ `FamilyServiceStub.cs` - Family service (create family, add member, remove member, calculate relationships)
- ❌ `FamilyStubComponents.cs` - FamilyIdentity, FamilyMember, FamilyRelation, FamilyTree
- ❌ `FamilyStubSystems.cs` - Family relationship calculation, inheritance tracking, family tree updates
- ❌ `DynastyServiceStub.cs` - Dynasty service (create dynasty, track lineage, calculate dynasty prestige)
- ❌ `DynastyStubComponents.cs` - DynastyIdentity, DynastyMember, DynastyLineage, DynastyPrestige
- ❌ `DynastyStubSystems.cs` - Dynasty succession, lineage tracking, dynasty reputation

**Purpose**: 
- **Family**: Track family relationships (parent, child, sibling, spouse). Family members share reputation, inheritance flows through family.
- **Dynasty**: Extended family lineages that control aggregates (villages, empires). Dynasty members compete for leadership seats. Dynasty prestige affects succession and unlocks.

---

### 7.6 Deceit & Deception Stubs

**Documentation**: `Docs/Concepts/Core/Miscommunication_System.md`

**Existing Implementation**:
- ✅ Miscommunication system documented (MiscommunicationEvent, MiscommunicationSeverity, MiscommunicationType, deception detection)

**Missing Stubs**:
- ❌ `DeceptionServiceStub.cs` - Deception service (attempt deception, detect deception, calculate deception success)
- ❌ `DeceptionStubComponents.cs` - DeceptionAttempt, DeceptionState, DeceptionDetection, DeceptionHistory
- ❌ `DeceptionStubSystems.cs` - Deception resolution, detection checks, deception consequences

**Purpose**: Entities can lie, deceive, and mislead others. Deception attempts can be detected based on clarity, language proficiency, and detection skills. Successful deception affects reputation and relations. Failed deception damages trust.

---

**Total Outstanding Stubs**: ~200 stub files across all domains (187 previous + 13 new)

---

**Last Updated**: 2025-01-21  
**Next Review**: After Phase 1 stub creation

