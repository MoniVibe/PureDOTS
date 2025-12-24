# Agent: Deception & Deception Detection

## Scope
Implement deception system for entities lying, deceiving, and misleading others, with detection mechanics based on clarity, language proficiency, and detection skills.

## Stub Files to Implement

### Deception System (3 files)
- `Runtime/Stubs/DeceptionServiceStub.cs` → `Runtime/Deception/DeceptionService.cs`
- `Runtime/Stubs/DeceptionStubComponents.cs` → `Runtime/Deception/DeceptionComponents.cs`
- `Runtime/Stubs/DeceptionStubSystems.cs` → `Systems/Deception/DeceptionSystems.cs`

**Requirements:**
- Deception types: Lie, Mislead, ConcealIntent, FalseIdentity, FabricateEvidence
- Deception attempts: active deception being performed with success chance
- Deception state: current deception status and level
- Deception detection: detection attempts based on insight and clarity
- Deception history: record of past deceptions (successful/failed, detected/undetected)
- Deception consequences: successful deception affects reputation/relations, failed deception damages trust

## Reference Documentation
- `Docs/Concepts/Core/Miscommunication_System.md` - Miscommunication system (includes deception)
- `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` - Section 4.3 (Intent Communication & Deception)

## Implementation Notes
- Deception success depends on: deceiver's deception skill, target's insight, language proficiency, clarity
- Detection chance: receiver's insight increases detection, sender's deception skill decreases detection
- Language barriers: easier to lie in foreign language (-20% detection)
- General signs: harder to detect deception (-30% detection)
- Successful deception: affects reputation and relations
- Failed deception: damages trust and reputation

## Dependencies
- `CommunicationComponents` - Communication clarity affects deception
- `LanguageComponents` - Language proficiency affects deception
- `ReputationComponents` - Deception affects reputation
- `RelationComponents` - Deception affects relations

## Integration Points
- Communication system: deception attempts during communication
- Language system: language proficiency affects deception success
- Reputation system: deception affects reputation
- Relations system: deception affects trust and relations

