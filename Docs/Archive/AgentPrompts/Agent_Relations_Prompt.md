# Agent: Relations Systems

## Status: ✅ STUBS CREATED - Ready for Implementation

## Scope
Implement relation update, decay, faction relations, personal relation formation, trust system, and grudge system for entity relationship management.

## Stub Files to Implement

### Relation Update System (3 files)
- ✅ `Runtime/Stubs/RelationUpdateStub.cs` → `Runtime/Relations/RelationUpdateService.cs`
- ✅ `Runtime/Stubs/RelationUpdateStubComponents.cs` → `Runtime/Relations/RelationUpdateComponents.cs`
- ✅ `Runtime/Stubs/RelationUpdateStubSystems.cs` → `Systems/Relations/RelationUpdateSystem.cs`

**Requirements:**
- Update relations based on interactions (positive/negative events)
- Relation event: source entity, target entity, relation delta, event type
- Relation change: helping = +relation, attacking = -relation, betrayal = major -relation
- Interaction outcome enum: Positive, Negative, Neutral, Betrayal

**Note**: `EntityRelationComponents.cs` has `InteractionOutcome` enum but no update system

### Relation Decay System (3 files)
- ✅ `Runtime/Stubs/RelationDecayStub.cs` → `Runtime/Relations/RelationDecayService.cs`
- ✅ `Runtime/Stubs/RelationDecayStubComponents.cs` → `Runtime/Relations/RelationDecayComponents.cs`
- ✅ `Runtime/Stubs/RelationDecayStubSystems.cs` → `Systems/Relations/RelationDecaySystem.cs`

**Requirements:**
- Decay relations over time if entities don't interact (familiarity fades, intensity decreases)
- Relation decay config: decay rate per day, decay threshold
- Decay calculation: relation value decreases over time without interaction
- Decay stops at neutral (0) or minimum relation value

**Note**: `EntityRelationComponents.cs` has `RelationConfig` with `DecayRatePerDay` but no system

### Faction Relations (3 files)
- ✅ `Runtime/Stubs/FactionRelationStub.cs` → `Runtime/Relations/FactionRelationService.cs`
- ✅ `Runtime/Stubs/FactionRelationStubComponents.cs` → `Runtime/Relations/FactionRelationComponents.cs`
- ✅ `Runtime/Stubs/FactionRelationStubSystems.cs` → `Systems/Relations/FactionRelationSystem.cs`

**Requirements:**
- Track faction-level relations (alliance, neutral, hostile, at war, vassal, overlord)
- Faction relation: faction A, faction B, relation score, relation type
- Faction relationship: relation type enum (Alliance, Neutral, Hostile, AtWar, Vassal, Overlord)
- Update faction relations based on aggregate member interactions

**Note**: `CombatComponents.cs` has `FactionRelationships` - verify if systems are stubbed or real

### Personal Relation Formation (3 files)
- ✅ `Runtime/Stubs/PersonalRelationFormationStub.cs` → `Runtime/Relations/PersonalRelationFormationService.cs`
- ✅ `Runtime/Stubs/PersonalRelationFormationStubComponents.cs` → `Runtime/Relations/PersonalRelationFormationComponents.cs`
- ✅ `Runtime/Stubs/PersonalRelationFormationStubSystems.cs` → `Systems/Relations/PersonalRelationFormationSystem.cs`

**Requirements:**
- Form personal relations from interactions (shared experiences → friendship, betrayal → enemy, family bonds)
- Relation formation event: source entity, target entity, event type, relation type
- Personal relation types: Friend, Enemy, Rival, Ally, Family, Mentor, Student
- Formation triggers: shared experiences, betrayal, family events, teaching/learning

**Note**: `PersonalRelationComponents.cs` has `PersonalRelationType` enum but no formation system

### Trust System (3 files)
- ✅ `Runtime/Stubs/TrustSystemStub.cs` → `Runtime/Relations/TrustSystemService.cs`
- ✅ `Runtime/Stubs/TrustSystemStubComponents.cs` → `Runtime/Relations/TrustSystemComponents.cs`
- ✅ `Runtime/Stubs/TrustSystemStubSystems.cs` → `Systems/Relations/TrustSystemSystem.cs`

**Requirements:**
- Calculate trust levels based on reliability (keeps promises = +trust, betrays = -trust)
- Trust level: trust score (0-100), trust events buffer
- Trust event: source entity, target entity, event type (PromiseKept, PromiseBroken, Betrayal)
- Trust calculation: reliability score affects trust level

**Note**: `PersonalRelationComponents.cs` has `Trust` field but no calculation system

### Grudge System (Godgame) (3 files)
- ✅ `Runtime/Stubs/GrudgeSystemStub.cs` → `Runtime/Relations/GrudgeSystemService.cs`
- ✅ `Runtime/Stubs/GrudgeSystemStubComponents.cs` → `Runtime/Relations/GrudgeSystemComponents.cs`
- ✅ `Runtime/Stubs/GrudgeSystemStubSystems.cs` → `Systems/Relations/GrudgeSystemSystem.cs`

**Requirements:**
- Track grudges (betrayals, murders, thefts) and drive revenge-seeking behavior
- Grudge: source entity, target entity, grudge type, grudge intensity, grudge event
- Grudge types: Betrayal, Murder, Theft, Insult, Harm
- Grudge events: grudge created, grudge resolved, revenge taken
- Revenge seeking: entities with grudges prioritize revenge actions

**Note**: `GrudgeHelpers.cs` exists - verify if systems are stubbed or real

## Reference Documentation
- `Docs/Concepts/Core/Entity_Relations.md` - Entity relations system
- `Runtime/Runtime/Social/EntityRelationComponents.cs` - Existing relation components
- `Runtime/Runtime/Social/PersonalRelationComponents.cs` - Existing personal relation components
- `Runtime/Runtime/Social/GrudgeHelpers.cs` - May already exist

## Implementation Notes
- Verify existing components before creating stubs
- Relations are bidirectional (entity A's relation with entity B)
- Relation updates should be event-driven (not polling)
- Relation decay should be throttled (not every tick)
- Trust and grudges are separate from general relations

## Dependencies
- `EntityRelationComponents.cs` - Existing relation components
- `PersonalRelationComponents.cs` - Existing personal relation components
- `GrudgeHelpers.cs` - May already exist
- Event system for relation events

## Integration Points
- Combat system: combat interactions affect relations
- Communication system: communication affects relations
- Deception system: deception affects trust
- Family system: family events affect personal relations

