# PureDOTS Contracts

Simple breadcrumbs for shared systems. When you add or change a contract, update this file.

## TimeControlCommand v1

- Producer: Time control systems (PureDOTS)
- Consumer: Godgame, Space4x time/rewind systems
- Schema:
  - Type (TimeControlCommandType enum)
  - UintParam (tick count, target tick, etc.)
  - FloatParam (speed multiplier, etc.)
  - Scope (Global, LocalBubble, Territory, Player)
  - Source (Player, Miracle, Scenario, DevTool, Technology, System)
  - PlayerId (byte, 0 for SP)
  - SourceId (uint, origin entity ID)
  - Priority (byte, conflict resolution)
- Notes: Must be rewind-safe, Burst-safe. No strings. Processed by RewindCoordinatorSystem and TimeScaleCommandSystem.

## PhysicsCollisionEvent v1

- Producer: Physics/Collision systems (PureDOTS)
- Consumer: Godgame, Space4x combat/presentation
- Schema:
  - OtherEntity (Entity)
  - ContactPoint (float3)
  - ContactNormal (float3)
  - Impulse (float)
  - Tick (uint)
  - EventType (PhysicsCollisionEventType: Collision, TriggerEnter, TriggerExit)
- Notes: Must be rewind-safe, Burst-safe. No strings. Added to entities with RequiresPhysics component and PhysicsCollisionEventElement buffer.

## InputCommandLogEntry v1

- Producer: Input systems (PureDOTS)
- Consumer: Godgame, Space4x input handling
- Schema:
  - Tick (uint)
  - Type (byte)
  - FloatParam (float)
  - UintParam (uint)
- Notes: Ring buffer entry for time control command logging. Burst-safe.

## SpatialGridProvider v1

- Producer: Spatial systems (PureDOTS)
- Consumer: Godgame, Space4x spatial queries
- Interface: ISpatialGridProvider
- Implementations: HashedSpatialGridProvider, UniformSpatialGridProvider
- Notes: Provider pattern allows swapping spatial grid implementations. Config-driven selection.

## PhysicsProvider v1

- Producer: Physics systems (PureDOTS)
- Consumer: Godgame, Space4x physics/collision systems
- Interface: IPhysicsProvider
- Implementations: NoPhysicsProvider (ID=0), EntitiesPhysicsProvider (ID=1), HavokPhysicsProvider (ID=2, stub)
- Schema:
  - ProviderId (byte in PhysicsConfig: None=0, Entities=1, Havok=2)
  - Step(float deltaTime, ref PhysicsWorld world)
  - GetCollisionEvents(Allocator) -> NativeArray<CollisionEvent>
  - GetTriggerEvents(Allocator) -> NativeArray<TriggerEvent>
- Notes: Provider pattern allows swapping physics backends. Currently only Entities (Unity Physics) is fully implemented. PhysicsEventSystem processes events when ProviderId=Entities. Games select provider via PhysicsConfig.

## LaunchRequest v1

- Producer: Game adapters (Godgame slingshot, Space4x launchers)
- Consumer: PureDOTS launch queue systems
- Schema:
  - SourceEntity (Entity) - the launcher entity
  - PayloadEntity (Entity) - the object to launch
  - LaunchTick (uint) - scheduled tick for launch (0 = immediate)
  - InitialVelocity (float3) - velocity to apply at launch
  - Flags (byte) - optional flags (reserved)
- Notes: Written by game adapters only in Record mode. Burst-safe, no strings. Consumed by LaunchRequestIntakeSystem.

## LaunchQueueEntry v1

- Producer: PureDOTS LaunchRequestIntakeSystem
- Consumer: PureDOTS LaunchExecutionSystem
- Schema:
  - PayloadEntity (Entity)
  - ScheduledTick (uint)
  - InitialVelocity (float3)
  - State (LaunchEntryState enum: Pending, Launched, Consumed)
- Notes: Internal queue on launcher entities. Rewind-safe (state restored on rewind). Burst-safe.

## LauncherConfig v1

- Producer: Game authoring (bakers)
- Consumer: PureDOTS launch systems
- Schema:
  - MaxQueueSize (byte) - max pending launches
  - CooldownTicks (uint) - ticks between launches
  - DefaultSpeed (float) - default launch speed if not specified
- Notes: Singleton-like config per launcher entity. Set at bake time.

