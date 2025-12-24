# Agent: Intent & AI Systems

## Scope
Implement the core intent system that bridges perception/interrupts to action selection for the AI pipeline.

## Stub Files to Implement

### Intent System (3 files)
- `Runtime/Stubs/IntentServiceStub.cs` → `Runtime/Intent/IntentService.cs`
- `Runtime/Stubs/IntentStubComponents.cs` → `Runtime/Intent/IntentComponents.cs`
- `Runtime/Stubs/IntentStubSystems.cs` → `Systems/Intent/IntentSystems.cs`

**Requirements:**
- `EntityIntent` component: mode, target entity/position, triggering interrupt, priority
- `IntentMode` enum: Idle, MoveTo, Attack, Flee, UseAbility, ExecuteOrder, Gather, Build, Defend, Patrol, Follow
- `Interrupt` buffer: type, priority, source, timestamp, target, payload
- Intent processing: process interrupts, update intent based on priority
- Intent validation: check if intent is still valid (target exists, priority still high)
- Interrupt handling: map interrupt types to intent modes

## Reference Documentation
- `Docs/Concepts/Core/Perception_Action_Intent_Summary.md` - Full intent pipeline specification
- `Runtime/Runtime/Interrupts/InterruptComponents.cs` - May already exist, verify
- `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` - Section 1.1 (AI Pipeline)

## Implementation Notes
- Intent is the bridge between perception/interrupts and action execution
- Interrupts drive intent changes (reactive behavior)
- Intent priority determines which intent wins when multiple interrupts occur
- Intent validation prevents stale intents (target destroyed, etc.)
- Systems should read `EntityIntent` and execute accordingly

## Dependencies
- `Interrupt` buffer (may already exist)
- `InterruptType` and `InterruptPriority` enums
- `EntityIntent` component (may already exist in `Perception_Action_Intent_Summary.md`)
- Bridge to `AICommand` buffer for action execution

## Integration Points
- `InterruptHandlerSystem` processes interrupts → updates `EntityIntent`
- Domain-specific systems read `EntityIntent` → execute actions
- Perception systems emit interrupts on new threats/resources
- Combat systems emit interrupts on damage/weapon ready

