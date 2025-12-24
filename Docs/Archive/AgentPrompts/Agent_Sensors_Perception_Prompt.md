# Agent: Sensors & Perception Systems

## Status: ✅ STUBS CREATED - Ready for Implementation

## Scope
Implement signal field system, sense organs, medium context filtering, stealth detection, and perception channel integration for the perception pipeline.

## Stub Files to Implement

### Signal Field System (3 files)
- ✅ `Runtime/Stubs/SignalFieldStub.cs` → `Runtime/Perception/SignalFieldService.cs`
- ✅ `Runtime/Stubs/SignalFieldStubComponents.cs` → `Runtime/Perception/SignalFieldComponents.cs`
- ✅ `Runtime/Stubs/SignalFieldStubSystems.cs` → `Systems/Perception/SignalFieldSystem.cs`

**Requirements:**
- Emit smell/sound/EM signals into spatial grid cells
- Signal field cells store signal strength per channel
- Signal decay over time
- Entities sample signal field at their position
- Signal levels converted to confidence based on acuity/noise floor

**Note**: `SensorySignalEmitter` and `SignalFieldCell` may already exist in `PerceptionComponents.cs` - verify

### Sense Organ System (3 files)
- ✅ `Runtime/Stubs/SenseOrganStub.cs` → `Runtime/Perception/SenseOrganService.cs`
- ✅ `Runtime/Stubs/SenseOrganStubComponents.cs` → `Runtime/Perception/SenseOrganComponents.cs`
- ✅ `Runtime/Stubs/SenseOrganStubSystems.cs` → `Systems/Perception/SenseOrganSystem.cs`

**Requirements:**
- Model individual sense organs (eyes, ears, sensors) with per-organ properties
- Sense organ state buffer: organ type, channels, gain, condition, noise floor, range multiplier
- Apply organ properties to detection calculations
- Model damaged/blinded senses (condition < 1)

**Note**: `SenseOrganState` buffer may already exist in `PerceptionComponents.cs` - verify

### Medium Context System (3 files)
- ✅ `Runtime/Stubs/MediumContextStub.cs` → `Runtime/Perception/MediumContextService.cs`
- ✅ `Runtime/Stubs/MediumContextStubComponents.cs` → `Runtime/Perception/MediumContextComponents.cs`
- ✅ `Runtime/Stubs/MediumContextStubSystems.cs` → `Systems/Perception/MediumContextSystem.cs`

**Requirements:**
- Medium types: Vacuum, Gas, Liquid, Solid, Mixed
- Filter detection channels by medium type (sound requires gas/liquid, EM blocked by obstacles)
- Space4X: vacuum outside hull = no hearing/smell, gas inside = hearing works
- Medium properties: sound speed, attenuation, diffusivity, flow vector, turbulence

### Stealth Detection System (3 files)
- ✅ `Runtime/Stubs/StealthDetectionStub.cs` → `Runtime/Perception/StealthDetectionService.cs`
- ✅ `Runtime/Stubs/StealthDetectionStubComponents.cs` → `Runtime/Perception/StealthDetectionComponents.cs`
- ✅ `Runtime/Stubs/StealthDetectionStubSystems.cs` → `Systems/Perception/StealthDetectionSystem.cs`

**Requirements:**
- Stealth levels: Exposed (0%), Concealed (25%), Hidden (50%), Invisible (75%)
- Environmental modifiers: light, terrain, movement speed
- Stealth check formula: StealthCheck vs PerceptionCheck
- Success levels: Remains undetected, Suspicious, Spotted, Exposed

### Perception Channel Integration (3 files)
- ✅ `Runtime/Stubs/PerceptionChannelStub.cs` → `Runtime/Perception/PerceptionChannelService.cs`
- ✅ `Runtime/Stubs/PerceptionChannelStubComponents.cs` → `Runtime/Perception/PerceptionChannelComponents.cs`
- ✅ `Runtime/Stubs/PerceptionChannelStubSystems.cs` → `Systems/Perception/PerceptionChannelSystem.cs`

**Requirements:**
- Integrate `PerceptionChannel` enum with detection systems
- Channels: Vision, Hearing, Smell, EM, Gravitic, Exotic, Paranormal
- Channel-based detection using spatial queries
- Channel-specific behaviors (smell diffuses, sound propagates, EM blocked)

**Note**: `SenseCapability`, `SensorSignature`, `PerceivedEntity` already exist in `PerceptionComponents.cs` - verify if systems are stubbed or real

## Reference Documentation
- `Docs/Concepts/Core/Perception_Action_Intent_Summary.md` - Perception pipeline specification
- `AI_PERFORMANCE_SENSORS_COMMS_COOPERATION_SUMMARY.md` - Section 3 (Sensors & Perception)
- `Runtime/Runtime/Perception/PerceptionComponents.cs` - Existing perception components

## Implementation Notes
- Verify existing components before creating stubs
- Signal field uses spatial grid for efficient updates
- Sense organs are optional enhancement (can be skipped initially)
- Medium context is critical for Space4X (vacuum vs pressurized)
- Stealth detection integrates with existing perception system

## Dependencies
- `PerceptionComponents.cs` - Existing perception components
- `SenseCapability` component
- `SensorSignature` component
- `PerceivedEntity` buffer
- Spatial grid for signal field

## Integration Points
- `PerceptionUpdateSystem` - Integrates with perception pipeline
- `AISensorUpdateSystem` - Uses perception data
- Spatial grid - Signal field storage
- Medium system - Filters channels by medium type

