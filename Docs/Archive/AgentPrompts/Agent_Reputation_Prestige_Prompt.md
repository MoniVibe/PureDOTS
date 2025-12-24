# Agent: Reputation & Prestige

## Scope
Implement reputation system (how entities are perceived) and prestige system (achievement-based reputation) with gossip propagation and tier progression.

## Stub Files to Implement

### Reputation System (3 files)
- `Runtime/Stubs/ReputationServiceStub.cs` → `Runtime/Reputation/ReputationService.cs`
- `Runtime/Stubs/ReputationStubComponents.cs` → `Runtime/Reputation/ReputationComponents.cs`
- `Runtime/Stubs/ReputationStubSystems.cs` → `Systems/Reputation/ReputationSystems.cs`

**Requirements:**
- Reputation domains: Trading, Combat, Diplomacy, Magic, Crafting, General
- Reputation tiers: Hated, Hostile, Unfriendly, Neutral, Friendly, Honored, Exalted
- Reputation events: actions affect reputation scores
- Reputation spread: gossip propagation through witnesses
- Reputation decay: unused reputation decays over time

### Prestige System (3 files)
- `Runtime/Stubs/PrestigeServiceStub.cs` → `Runtime/Prestige/PrestigeService.cs`
- `Runtime/Stubs/PrestigeStubComponents.cs` → `Runtime/Prestige/PrestigeComponents.cs`
- `Runtime/Stubs/PrestigeStubSystems.cs` → `Systems/Prestige/PrestigeSystems.cs`

**Requirements:**
- Prestige tiers: Unknown → Known → Notable → Renowned → Famous → Legendary → Mythic
- Prestige score: accumulates from achievements
- Prestige unlocks: tier-based unlocks (options, opportunities)
- Prestige stress: high prestige creates pressure/stress
- Notoriety: negative prestige (infamy)
- Prestige decay: prestige decays slowly over time

## Reference Documentation
- `Docs/Concepts/Core/Reputation_System.md` - Reputation system design
- `Docs/Archive/ExtensionRequests/Done/2025-11-26-prestige-reputation-system.md` - Extension request
- `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` - Section 6.1 (Godgame Integration)

## Implementation Notes
- Reputation is per-observer (entity A's reputation with entity B)
- Reputation spreads through witnesses (gossip propagation)
- Prestige is global (entity's overall achievement-based reputation)
- Prestige affects leadership elections and unlocks
- Reputation and prestige are separate systems (reputation = perception, prestige = achievement)

## Dependencies
- `EntityRelationComponents` - Relations affect reputation
- `CommunicationComponents` - Communication affects gossip spread
- Event system for reputation events

## Integration Points
- Relations system: reputation affects relations
- Communication system: gossip propagation
- Leadership system: prestige affects leadership elections
- Unlock system: prestige unlocks options

