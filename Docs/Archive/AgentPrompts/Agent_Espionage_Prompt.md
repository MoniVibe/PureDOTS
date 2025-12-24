# Agent: Espionage & Infiltration

## Scope
Implement infiltration system for spies and double agents infiltrating organizations, building infiltration levels, managing cover identities, and gathering intel.

## Stub Files to Implement

### Infiltration System (3 files)
- `Runtime/Stubs/InfiltrationServiceStub.cs` → `Runtime/Infiltration/InfiltrationService.cs`
- `Runtime/Stubs/InfiltrationStubComponents.cs` → `Runtime/Infiltration/InfiltrationComponents.cs`
- `Runtime/Stubs/InfiltrationStubSystems.cs` → `Systems/Infiltration/InfiltrationSystems.cs`

**Requirements:**
- Infiltration levels: Contact → Embedded → Trusted → Influential → Subverted
- Infiltration progress: activities increase infiltration level over time
- Suspicion tracking: suspicious activities increase suspicion level
- Detection checks: counter-intelligence detects infiltration based on suspicion
- Cover identity: spy maintains cover identity with credibility
- Extraction plan: plan and execute extraction when exposed
- Gathered intel: buffer of intelligence collected (military, economic, political, technological, social)
- Investigation: organizations investigate potential spies

## Reference Documentation
- `Docs/Concepts/Stealth/Infiltration_Detection_Agnostic.md` - Infiltration system design
- `Docs/Archive/ExtensionRequests/Done/2025-11-26-espionage-infiltration-utilities.md` - Extension request
- `Runtime/Runtime/AI/Infiltration/InfiltrationComponents.cs` - May already exist, verify and move to Stubs if needed

## Implementation Notes
- Infiltration is a long-term process (days/weeks)
- Suspicion decays over time if no suspicious activities
- Counter-intelligence measures increase detection chance
- Cover identity credibility affects suspicion gain rate
- Extraction can be planned or emergency (immediate)
- Intel gathering rate depends on infiltration level

## Dependencies
- `EntityRelationComponents` - Relations affect infiltration success
- `CommunicationComponents` - Communication affects suspicion
- `ReputationComponents` - Reputation affects cover credibility
- Spatial queries for detection checks

## Integration Points
- Relations system: good relations help infiltration
- Communication system: communication patterns affect suspicion
- Reputation system: reputation affects cover credibility
- Stealth system: stealth affects detection chances

