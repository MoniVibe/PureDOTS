# Agent: Cooperation Systems

## Status: ✅ STUBS CREATED - Ready for Implementation

## Scope
Implement cooperation systems for magic circles, coordinated combat, mutual care, crew coordination, production cooperation, and group knowledge sharing.

## Stub Files to Implement

### Magic Circle Cooperation (Godgame) (3 files)
- ✅ `Runtime/Stubs/MagicCircleStub.cs` → `Runtime/Cooperation/MagicCircleService.cs`
- ✅ `Runtime/Stubs/MagicCircleStubComponents.cs` → `Runtime/Cooperation/MagicCircleComponents.cs`
- ✅ `Runtime/Stubs/MagicCircleStubSystems.cs` → `Systems/Cooperation/MagicCircleSystem.cs`

**Requirements:**
- Coordinate multiple casters pooling mana
- Magic circle: primary caster, pooled mana, cast speed bonus, efficiency bonus
- Circle member buffer: contributor entity, mana contribution rate, channeling efficiency
- Ritual casting: coordinated ritual magic requiring synchronization
- Ritual phases: Preparation → Invocation → Channeling → Climax → Completion

### Coordinated Combat (Godgame) (3 files)
- ✅ `Runtime/Stubs/CoordinatedCombatStub.cs` → `Runtime/Cooperation/CoordinatedCombatService.cs`
- ✅ `Runtime/Stubs/CoordinatedCombatStubComponents.cs` → `Runtime/Cooperation/CoordinatedCombatComponents.cs`
- ✅ `Runtime/Stubs/CoordinatedCombatStubSystems.cs` → `Systems/Cooperation/CoordinatedCombatSystem.cs`

**Requirements:**
- Coordinate simultaneous attacks (archers volley together, shield wall advances together)
- Volley coordinator: volley commander, target position, charge progress, volley power multiplier
- Volley shooter buffer: shooter entity, ready status, accuracy bonus
- Simultaneity bonus: cohesion × 0.5 (up to +50%)
- Volume bonus: sqrt(readyCount) × 0.2 (diminishing returns)

### Mutual Care (Godgame) (3 files)
- ✅ `Runtime/Stubs/MutualCareStub.cs` → `Runtime/Cooperation/MutualCareService.cs`
- ✅ `Runtime/Stubs/MutualCareStubComponents.cs` → `Runtime/Cooperation/MutualCareComponents.cs`
- ✅ `Runtime/Stubs/MutualCareStubSystems.cs` → `Systems/Cooperation/MutualCareSystem.cs`

**Requirements:**
- Entities prioritize helping allies (healing wounded, protecting vulnerable, rescuing downed)
- Care relationship: caregiver, care receiver, care level (0-1), type (Mutual/Protective/Mentorship/etc.)
- Care action buffer: action type (ProvideFood/Healing/Comfort/etc.), target, magnitude
- Mutual care bond: bond strength, mutuality score, morale bonus, stress reduction, performance bonus

### Crew Coordination (Space4X) (3 files)
- ✅ `Runtime/Stubs/CrewCoordinationStub.cs` → `Runtime/Cooperation/CrewCoordinationService.cs`
- ✅ `Runtime/Stubs/CrewCoordinationStubComponents.cs` → `Runtime/Cooperation/CrewCoordinationComponents.cs`
- ✅ `Runtime/Stubs/CrewCoordinationStubSystems.cs` → `Systems/Cooperation/CrewCoordinationSystem.cs`

**Requirements:**
- Coordinate crew tasks (pilot-operator communication, hangar operations, multi-tier support)
- Crew member: crew entity, crew role, crew task
- Operator-pilot link: operator, pilot, link quality, sensor data quality, guidance bonus
- Hangar operations: hangar bay, operational efficiency, deployment speed, maintenance quality
- Multi-tier support: strategic → operational → tactical → individual

### Production Cooperation (Godgame) (3 files)
- ✅ `Runtime/Stubs/ProductionCooperationStub.cs` → `Runtime/Cooperation/ProductionCooperationService.cs`
- ✅ `Runtime/Stubs/ProductionCooperationStubComponents.cs` → `Runtime/Cooperation/ProductionCooperationComponents.cs`
- ✅ `Runtime/Stubs/ProductionCooperationStubSystems.cs` → `Systems/Cooperation/ProductionCooperationSystem.cs`

**Requirements:**
- Coordinate production chains (blacksmiths work with miners, builders coordinate with haulers)
- Production team: team entity, production role
- Collaborative crafting: item name, current phase, craft progress, final quality
- Crafting phases: Planning → MaterialPrep → Assembly → Refinement → QualityControl → Completed
- Quality calculation: high cohesion → quality approaches max skill

### Group Knowledge Sharing (3 files)
- ✅ `Runtime/Stubs/GroupKnowledgeStub.cs` → `Runtime/Cooperation/GroupKnowledgeService.cs`
- ✅ `Runtime/Stubs/GroupKnowledgeStubComponents.cs` → `Runtime/Cooperation/GroupKnowledgeComponents.cs`
- ✅ `Runtime/Stubs/GroupKnowledgeStubSystems.cs` → `Systems/Cooperation/GroupKnowledgeSystem.cs`

**Requirements:**
- Share knowledge within groups (band knows where resources are, fleet shares intel)
- Group knowledge: knowledge entity, knowledge type, knowledge content
- Knowledge diffusion: knowledge spreads within group over time
- Knowledge types: locations, threats, resources, tactics

**Note**: `GroupKnowledgeComponents.cs` exists - verify if systems are stubbed or real

## Reference Documentation
- `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` - Section 5 (Cooperation Systems)
- `Docs/Concepts/Core/Entity_Cooperation_System.md` - Cooperation framework
- `Runtime/Runtime/Knowledge/GroupKnowledgeComponents.cs` - May already exist

## Implementation Notes
- Cooperation cohesion calculation: skill synergy (40%) + relation bonus (30%) + communication clarity (20%) + experience (10%)
- Efficiency multipliers vary by cooperation type (mana pooling: up to 2×, ritual casting: up to 3×)
- Cohesion decays/regen over time
- Cooperation phases: Forming → Coordinating → Active → Degrading → Dissolved

## Dependencies
- `EntityRelationComponents` - Relations affect cohesion
- `CommunicationComponents` - Communication clarity affects cohesion
- `GroupKnowledgeComponents` - May already exist
- `ManaComponents` - For magic circle cooperation

## Integration Points
- Relations system: relations affect cooperation cohesion
- Communication system: communication clarity affects cohesion
- Combat system: coordinated combat affects combat effectiveness
- Production system: production cooperation affects output quality

