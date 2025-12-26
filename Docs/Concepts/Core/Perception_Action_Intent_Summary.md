# Perception, Action, and Intent Systems Summary

**Status:** Active - Core Systems
**Category:** Core - AI Pipeline
**Audience:** Implementers / Architects / Designers
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

**Purpose:** Unified perception, action, and intent pipeline for cross-game AI (Godgame villagers, Space4X vessels, creatures). Provides sensing, utility-based decision-making, and interrupt-driven behavior changes.

**Architecture:**
- **Perception:** Channel-based detection (Vision, Hearing, Smell, EM, Gravitic, Paranormal) using spatial queries, signal fields, and sensor signatures
- **Actions:** Utility-based scoring system with blob-defined curves, spatial sensor readings, and virtual sensors for internal needs
- **Intent:** Interrupt-driven intent selection (Idle, MoveTo, Attack, Flee, etc.) that bridges perception/actions to domain-specific behavior

**Key Design Principles:**
- **Data-Driven:** Behaviors defined by blob assets (utility curves), not hardcoded logic
- **Channel-Based:** Flexible perception channels work across games (villagers use Vision/Hearing, ships use EM/Gravitic)
- **Interrupt-Driven:** Agents react to perception deltas, comm receipts, threats via interrupts (reduces polling)
- **Modular:** Entities opt into AI pipeline via components; bridge systems translate generic commands to game-specific goals

## Contract Tightening (Read First)

**Ownership rules:**
- **Perception systems** are the only writers of `PerceivedEntity` / `SignalPerceptionState`.
- **Targeting system** is the only writer of `CurrentTarget` / `TargetPosition`.
- **Intent system** is the only writer of `EntityIntent` / `UtilityDecisionState`.

**Commitment rules:**
- Intent switches only when `(newScore >= oldScore * SwitchThreshold)` **and** `MinCommitTicks` elapsed.
- Interrupts can pre-empt only if `InterruptPriority >= CurrentIntentPriority`.

**Cadence rules:**
- Perception updates are **budgeted**; no world scans in hot path.
- Utility scoring is throttled via `UtilityConfig.ReconsiderInterval`.

**Fallback rules:**
- If no viable action meets `MinViableScore`, keep `EntityIntent.Mode = Idle`.
- On invalid targets, emit `LostTarget` interrupt and clear the target in the same tick.

---

## Canonical Perception Pipeline (Entities 1.4+)

Perception data must flow through the canonical stack below. `PerceivedEntity` buffers and `SignalPerceptionState`
are the ONLY approved sensory inputs for AI systems.

1. **PerceptionSignalFieldUpdateSystem** – emitters write smell/sound/EM data into the spatial signal field.
2. **PerceptionSignalSamplingSystem** – entities sample the signal field into `SignalPerceptionState`.
3. **PerceptionUpdateSystem** – spatial queries populate `DynamicBuffer<PerceivedEntity>` per sensor cadence.
4. **AISensorUpdateSystem** – derives `AISensorReading` buffers (category-aware, normalized scores).
5. **AIVirtualSensorSystem** – injects internal need “virtual sensors” ahead of utility scoring.

> AI systems MUST read from `PerceivedEntity` / `SignalPerceptionState` / `AISensorReading`. Any direct use of
> `DetectedEntity` or legacy `SensorConfig` is considered a regression.

---

## File Mapping

### Core Perception Components
- `Packages/com.moni.puredots/Runtime/Stubs/SignalFieldStubComponents.cs`
  - `PerceptionChannel` (enum with flags: Vision, Hearing, Smell, EM, Gravitic, Exotic, Paranormal, Proximity)
  - `SensorySignalEmitter` (emission config: channels, smell/sound/EM strength)
  - `SignalFieldCell` (buffer: smell/sound/EM levels per spatial cell)
  - `SignalFieldConfig` (decay rates, emission scale, max strength, sampling falloff)
  - `SignalFieldState` (last update tick, version)
  - `SignalPerceptionState` (signal field sampling results: smell/sound/EM levels and confidence)
  - `SignalPerceptionThresholds` (interrupt thresholds + cooldown)
- `Packages/com.moni.puredots/Runtime/Stubs/PerceptionChannelStubComponents.cs`
  - `SenseCapability` (sensor config: enabled channels, range, FOV, acuity, update interval)
  - `SensorSignature` (per-channel detectability: visual, auditory, olfactory, EM, gravitic, exotic, paranormal signatures)
  - `PerceivedEntity` (buffer: target entity, detected channels, confidence, distance, direction, threat level, relationship)
- `Packages/com.moni.puredots/Runtime/Stubs/SenseOrganStubComponents.cs`
  - `SenseOrganState` (buffer: organ type, channels, gain, condition, noise floor, range multiplier)
  - `SenseOrganType` (Eye, Ear, Nose, EMSuite, GraviticArray, ExoticSensor, ParanormalOrgan)
- `Packages/com.moni.puredots/Runtime/Runtime/Perception/PerceptionComponents.cs`
  - `PerceptionState` (tracking: last update tick, perceived count, highest threat, nearest entity)
  - `PerceivedRelationKind` / `PerceivedRelationFlags` (relation classification helpers)
  - `ObstacleGridConfig` / `ObstacleGridCell` / `ObstacleTag` / `ObstacleHeight` / `ObstacleGridRebuildRequest`
- `Packages/com.moni.puredots/Runtime/Runtime/Perception/SensorSignatureModifierComponents.cs`
  - `SensorSignatureModifier` (cloaks, camo, emission dampening)

### Legacy Sensor Components (Deprecated – guarded by `LegacySensorSystemEnabled`)
- `Packages/com.moni.puredots/Runtime/Runtime/AI/SensorComponents.cs`
  - `DetectionType` (enum: Sight, Sound, Smell, Proximity, Radar, Psychic)
  - `DetectedEntity` (buffer: target, distance, direction, detection type, confidence, threat level, relationship)
  - `SensorConfig` (legacy config: range, FOV, detection mask, update interval, max tracked targets)
  - `SensorState` (legacy state: last update tick, detection count, highest threat)
  - `Detectable` (marker: visibility, audibility, threat level, category)
> **Deprecated:** `SensorUpdateSystem` + `DetectedEntity` performed O(N²) scans and are now disabled by default via
> `SimulationFeatureFlags.LegacySensorSystemEnabled`. Enable only for small demo scenes (< 1k entities) and migrate
> gameplay code to the canonical pipeline above.

### Action/Utility Components
- `Packages/com.moni.puredots/Runtime/Runtime/AI/UtilityComponents.cs`
  - `ActionType` (enum: None, Idle, Move, Gather, Deliver, Attack, Defend, Flee, Rest, Eat, Drink, Socialize, Work, Patrol, Guard, Explore, Follow, Custom)
  - `ActionScore` (buffer: action type, custom action ID, utility score, target entity, target position, priority modifier, cooldown)
  - `UtilityCurveRef` (blob reference to utility curves)
  - `UtilityCurveBlob` (blob: array of curve definitions)
  - `UtilityCurveDefinition` (curve type, slope, exponent, shifts, min/max values)
  - `UtilityDecisionState` (current action, score, target, selected tick, min duration, interrupted flag)
  - `UtilityConfig` (reconsider interval, switch threshold, random factor, min viable score)

### Intent/Interrupt Components
- `Packages/com.moni.puredots/Runtime/Runtime/Interrupts/InterruptComponents.cs`
  - `InterruptType` (enum: UnderAttack, NewThreatDetected, ResourceSpotted, NewOrder, LowHealth, etc.)
  - `InterruptPriority` (enum: Low, Normal, High, Urgent, Critical)
  - `Interrupt` (buffer: type, priority, source entity, timestamp, target entity, target position, payload value/ID, processed flag)
  - `EntityIntent` (intent mode, target entity/position, triggering interrupt, intent set tick, priority, valid flag)
  - `IntentMode` (enum: Idle, MoveTo, Attack, Flee, UseAbility, ExecuteOrder, Gather, Build, Defend, Patrol, Follow, Custom0-3)
  - `InterruptUtils` (static helpers: Emit, EmitCombat, EmitPerception, EmitOrder)

### Perception Systems
- `Packages/com.moni.puredots/Runtime/Systems/Perception/PerceptionUpdateSystem.cs`
  - `PerceptionUpdateSystem` (updates perception state using channel-based detection, integrates with spatial grid)
  - `PerceptionSignalFieldUpdateSystem` (updates signal field cells with emitter contributions)
  - `PerceptionSignalSamplingSystem` (samples signal field for entities with SenseCapability)

### AI Pipeline Systems
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs`
  - `AISensorUpdateSystem` (samples spatial grid, populates sensor readings, category filtering)
  - `AIVirtualSensorSystem` (populates virtual sensor readings for internal needs)
  - `AIUtilityScoringSystem` (scores actions using utility curves and sensor readings)
  - `AISteeringSystem` (calculates movement direction and velocity)
  - `AITaskResolutionSystem` (emits AICommand buffer)

### Intent/Interrupt Systems
- `Packages/com.moni.puredots/Runtime/Systems/Interrupts/InterruptHandlerSystem.cs`
  - `InterruptHandlerSystem` (processes interrupts, updates EntityIntent)

### Bridge Systems
- `Assets/Projects/Godgame/Scripts/Godgame/Systems/GodgameVillagerAICommandBridgeSystem.cs` (consumes AICommand → VillagerAIState)
- `Assets/Scripts/Space4x/Systems/VesselAISystem.cs` (Space4X vessel AI command bridge)

### Documentation
- `Docs/Guides/AI_Integration_Guide.md` (developer guide for integrating entities with AI pipeline)
- `Docs/Mechanics/AIBehaviorModules.md` (architecture and module design)
- `Docs/Mechanics/IntelVisibilitySystem.md` (Space4X-specific radar/intel system)
- `Docs/Concepts/Stealth/Infiltration_Detection_Agnostic.md` (stealth and detection concepts)
- `Docs/Concepts/Core/AI_Optimization_Methodologies.md` (attention/interrupts, commitment, confidence-aware decisions)

---

## Specifications

### Perception System

#### Channel-Based Detection

**Perception Channels (Flags Enum):**
```csharp
PerceptionChannel Vision = 1 << 0;      // Visual (line of sight)
PerceptionChannel Hearing = 1 << 1;     // Auditory (sound)
PerceptionChannel Smell = 1 << 2;       // Olfactory
PerceptionChannel EM = 1 << 3;          // Electromagnetic (radar/optical)
PerceptionChannel Gravitic = 1 << 4;    // Gravitational detection
PerceptionChannel Exotic = 1 << 5;      // Exotic physics
PerceptionChannel Paranormal = 1 << 6;  // Magical/psychic
PerceptionChannel Proximity = 1 << 7;   // Generic proximity
// Custom channels (bits 8-31) reserved for game extensions
```

**Game-Specific Channel Mappings:**
- **Godgame:** Vision, Hearing, Smell, Paranormal (detect miracles, divine presence)
- **Space4X:** EM, Gravitic, Exotic (radar, mass detection, exotic sensors)

#### SenseCapability Configuration

```csharp
public struct SenseCapability : IComponentData
{
    public PerceptionChannel EnabledChannels;  // Bitmask of enabled channels
    public float Range;                        // Detection range (meters)
    public float FieldOfView;                  // FOV degrees (360 = omnidirectional)
    public float Acuity;                       // 0-1, affects confidence calculations
    public float UpdateInterval;               // Minimum time between updates (seconds)
    public byte MaxTrackedTargets;             // Max entities to track simultaneously
    public byte Flags;                         // Capability flags
}
```

#### Sensor Signature (Detectability)

```csharp
public struct SensorSignature : IComponentData
{
    public float VisualSignature;        // 0 = invisible, 1 = normal, >1 = conspicuous
    public float AuditorySignature;      // 0 = silent, 1 = normal, >1 = noisy
    public float OlfactorySignature;     // 0 = odorless, 1 = normal, >1 = strong smell
    public float EMSignature;            // 0 = stealth, 1 = normal, >1 = high emissions
    public float GraviticSignature;      // 0 = no gravity, 1 = normal, >1 = massive
    public float ExoticSignature;        // 0 = undetectable, 1 = normal, >1 = exotic
    public float ParanormalSignature;    // 0 = mundane, 1 = normal, >1 = magical
}
```

#### Sensor Signature Modifiers (Cloaks / Emissions)

`SensorSignatureModifier` applies per-channel multipliers to `SensorSignature` for
cloaks, camo, emission dampening, decoys, or environmental masking. It is applied
by `PerceptionUpdateSystem` before detection tests, keeping the core detection math
intact and data-driven.

#### PerceivedEntity Buffer

```csharp
[InternalBufferCapacity(16)]
public struct PerceivedEntity : IBufferElementData
{
    public Entity TargetEntity;              // Perceived entity
    public PerceptionChannel DetectedChannels; // Which channels detected this
    public float Confidence;                  // 0-1, detection confidence
    public float Distance;                    // Distance to target
    public float3 Direction;                  // Direction to target (normalized)
    public uint FirstDetectedTick;            // When first detected
    public uint LastSeenTick;                 // When last seen/updated
    public byte ThreatLevel;                  // 0-255 threat level
    public sbyte Relationship;                // -128 = enemy, 0 = neutral, +127 = ally
}
```

#### Signal Field System

**Purpose:** Efficiently track smell/sound/EM emissions in spatial grid cells (avoids per-entity queries).

**Components:**
- `SensorySignalEmitter` (on entities that emit signals)
- `SignalFieldCell` (buffer on spatial grid entity, one per cell)
- `SignalFieldConfig` (decay rates, emission scale, max strength)

**Flow:**
1. Emitters update signal field cells (smell/sound/EM strength)
2. Cells decay over time
3. Entities with `SenseCapability` sample signal field using multi-cell neighborhood sampling:
   - Sampling radius calculated from `SenseCapability.Range` (capped by `MaxSamplingRadiusCells`)
   - Tier-based multiplier applied if `AIFidelityTier` component exists
   - Distance-based falloff applied: `weight = pow(1f - distance / maxDistance, SamplingFalloffExponent)`
   - Accumulated weighted signal levels clamped to `MaxStrength`
4. Signal levels converted to confidence based on acuity/noise floor

**Multi-Cell Sampling:**
- Default: 5x5 neighborhood (radiusCells=2)
- High-tier entities sample more cells (1.5x multiplier) for fidelity
- Low-tier entities sample fewer cells (0.5-0.75x multiplier) for performance
- Falloff exponent (default 1.5f) controls how quickly signal strength decreases with distance

#### Sense Organs (Optional Enhancement)

**Purpose:** Model individual sense organs (eyes, ears, sensors) with per-organ properties.

```csharp
[InternalBufferCapacity(4)]
public struct SenseOrganState : IBufferElementData
{
    public SenseOrganType OrganType;      // Eye, Ear, Nose, EMSuite, GraviticArray, etc.
    public PerceptionChannel Channels;    // Which channels this organ supports
    public float Gain;                    // Organ sensitivity (0-1)
    public float Condition;               // Organ health (0-1, affects acuity)
    public float NoiseFloor;              // Minimum signal level to detect
    public float RangeMultiplier;         // Multiplies base range
}
```

**Benefits:**
- Model damaged/blinded senses (condition < 1)
- Per-organ acuity (keen senses = higher gain)
- Noise floor for poor-quality sensors

#### Detection Rules (Phase 1: Simple, Phase 2: Advanced)

**Current (Phase 1):**
- Range check (distance <= range)
- FOV check (for Vision/EM channels, dot product with forward direction)
- Signature-based confidence (signature × range decay × acuity - noise floor)
- Medium filtering (channels filtered by medium type: gas, liquid, vacuum, solid)

**Line-of-Sight Policy:**
- **Priority order:** Physics raycast → Obstacle grid → Confidence penalty
- Vision/EM channels require LOS; other channels don't
- Obstacle grid is deterministic fallback for headless/non-physics scenarios
- "LOS unknown" case reduces confidence by 50% but doesn't block detection
- Implemented via `ObstacleGridUtilities.CheckLOS` using Bresenham/DDA line stepping

**Future (Phase 2):**
- Channel-specific behaviors (smell diffuses, sound propagates, EM blocked by obstacles)
- Pluggable detection rules (blob assets for custom detection logic)

### Action System

#### Action Types

```csharp
public enum ActionType : byte
{
    None = 0,
    Idle = 1,
    Move = 2,
    Gather = 3,
    Deliver = 4,
    Attack = 5,
    Defend = 6,
    Flee = 7,
    Rest = 8,
    Eat = 9,
    Drink = 10,
    Socialize = 11,
    Work = 12,
    Patrol = 13,
    Guard = 14,
    Explore = 15,
    Follow = 16,
    Custom = 255  // Use CustomActionId for game-specific actions
}
```

#### Utility Scoring

**Architecture:**
- Actions scored using utility curves (blob assets)
- Each action has multiple factors (weighted utility curves)
- Each factor samples a sensor reading (spatial or virtual)
- Best-scoring action selected (with random factor for variation)

**Utility Curve Formula:**
```
score = pow(max((sensor_value - threshold) / maxValue, 0), responsePower) * weight
```

**Example:**
- Action "SatisfyHunger" with threshold=0.3, weight=2.0, responsePower=2.0
- If hunger sensor reading = 0.8 (high hunger):
  - score = pow(max((0.8 - 0.3) / 1.0, 0), 2.0) * 2.0
  - score = pow(0.5, 2.0) * 2.0 = 0.25 * 2.0 = 0.5
- If hunger sensor reading = 1.0 (critical hunger):
  - score = pow(0.7, 2.0) * 2.0 = 0.49 * 2.0 = 0.98

**Virtual Sensors (Internal Needs):**
- `AIVirtualSensorSystem` populates sensor readings for internal state (hunger, energy, morale)
- Enables unified utility scoring for spatial targets AND internal needs
- Maps needs to sensor indices (e.g., sensor 0 = hunger, sensor 1 = energy)

#### ActionScore Buffer

```csharp
[InternalBufferCapacity(8)]
public struct ActionScore : IBufferElementData
{
    public ActionType ActionType;
    public byte CustomActionId;        // For Custom action type
    public float Score;                // Utility score (higher = better)
    public Entity TargetEntity;        // Target entity (if applicable)
    public float3 TargetPosition;      // Target position (if applicable)
    public float PriorityModifier;     // Multiplies base score
    public float CooldownRemaining;    // Cooldown before can choose again
}
```

#### UtilityDecisionState

```csharp
public struct UtilityDecisionState : IComponentData
{
    public ActionType CurrentAction;           // Currently selected action
    public byte CurrentCustomActionId;
    public float CurrentScore;                 // Score of current action
    public Entity CurrentTarget;               // Target entity
    public float3 CurrentTargetPosition;       // Target position
    public uint ActionSelectedTick;            // When action was selected
    public uint MinActionDurationTicks;        // Minimum duration before reconsider
    public bool Interrupted;                   // Whether action was interrupted
}
```

### Intent System

#### Interrupt Types

**Combat Interrupts:**
- `UnderAttack`, `TookDamage`, `LostTarget`, `TargetDestroyed`, `WeaponReady`, `OutOfAmmo`

**Perception Interrupts:**
- `NewThreatDetected`, `LostThreat`, `AllyInDanger`, `ResourceSpotted`, `ObjectiveSpotted`
- `SmellSignalDetected`, `SoundSignalDetected`, `EMSignalDetected`

**Group/Order Interrupts:**
- `NewOrder`, `OrderCancelled`, `ObjectiveChanged`, `GroupFormed`, `GroupDisbanded`, `LeaderChanged`

**State Interrupts:**
- `LowHealth`, `LowResources`, `StatusEffectApplied`, `StatusEffectRemoved`, `AbilityReady`, `AbilityFailed`

#### Interrupt Buffer

```csharp
[InternalBufferCapacity(8)]
public struct Interrupt : IBufferElementData
{
    public InterruptType Type;
    public InterruptPriority Priority;         // Low, Normal, High, Urgent, Critical
    public Entity SourceEntity;                // Entity that caused/emitted interrupt
    public uint Timestamp;                     // Tick when emitted
    public Entity TargetEntity;                // Optional target entity
    public float3 TargetPosition;              // Optional target position
    public float PayloadValue;                 // Optional numeric payload
    public FixedString32Bytes PayloadId;       // Optional string payload
    public byte IsProcessed;                   // Whether interrupt has been handled
}
```

#### EntityIntent Component

```csharp
public struct EntityIntent : IComponentData
{
    public IntentMode Mode;                    // Desired behavior mode
    public Entity TargetEntity;                // Optional target entity
    public float3 TargetPosition;              // Optional target position
    public InterruptType TriggeringInterrupt;  // Interrupt that triggered this intent
    public uint IntentSetTick;                 // When intent was set
    public InterruptPriority Priority;         // Intent priority (from interrupt)
    public byte IsValid;                       // Whether intent is still valid
}
```

#### Intent Modes

```csharp
public enum IntentMode : byte
{
    Idle = 0,
    MoveTo = 1,
    Attack = 2,
    Flee = 3,
    UseAbility = 4,
    ExecuteOrder = 5,
    Gather = 6,
    Build = 7,
    Defend = 8,
    Patrol = 9,
    Follow = 10,
    Custom0 = 100,
    Custom1 = 101,
    Custom2 = 102,
    Custom3 = 103
}
```

---

## How It Works

### Perception Pipeline

1. **PerceptionUpdateSystem** (every N ticks, based on UpdateInterval):
   - Collects all entities with `SensorSignature` or `Detectable` component
   - For each entity with `SenseCapability`:
     - Queries spatial grid for nearby entities (within range)
     - For each enabled channel, evaluates detection:
       - Range check
       - FOV check (for Vision/EM)
       - Signature × range decay × acuity - noise floor
       - Medium filtering (channels filtered by medium type)
     - Adds `PerceivedEntity` to buffer if detected on any channel
   - Updates `PerceptionState` (perceived count, highest threat, nearest entity)

2. **PerceptionSignalFieldUpdateSystem** (every tick):
   - Updates `SignalFieldCell` buffer on spatial grid entity
   - For each entity with `SensorySignalEmitter`:
     - Quantizes position to grid cell
     - Adds smell/sound/EM strength to cell (with decay)
   - Decays cells over time based on config

3. **PerceptionSignalSamplingSystem** (every N ticks):
   - For each entity with `SenseCapability`:
     - Samples signal field at entity position
     - Converts signal levels to confidence (acuity × level - noise floor)
     - Updates `SignalPerceptionState` (smell/sound/EM levels and confidence)

4. **PerceptionToInterruptBridgeSystem**:
   - Monitors `PerceivedEntity` buffer changes
   - Emits interrupts when new threats/resources detected

### AI Pipeline (Sensing → Scoring → Steering → Commands)

1. **AISensorUpdateSystem** (every N ticks, based on sensor cadence):
   - Queries spatial grid for entities with `AISensorConfig`
   - Filters by category (Villager, ResourceNode, Storehouse, TransportUnit, Miracle)
   - Populates `AISensorReading` buffer with normalized scores (0-1)

2. **AIVirtualSensorSystem** (every N ticks):
   - Reads internal state (needs, health, morale) for entities
   - Populates virtual sensor readings (maps needs to sensor indices)
   - Enables unified utility scoring for spatial AND internal targets

3. **AIUtilityScoringSystem** (every N ticks, based on think interval):
   - For each entity with `AIBehaviourArchetype` (utility blob reference):
     - Evaluates all actions using utility curves
     - Each action score = sum of (curve(sensor_value) × weight)
     - Selects best-scoring action
     - Updates `UtilityDecisionState` (current action, score, target)

4. **AISteeringSystem** (every tick):
   - For each entity with `UtilityDecisionState`:
     - Calculates desired direction toward target
     - Applies obstacle avoidance
     - Updates `AISteeringState` (desired velocity)

5. **AITaskResolutionSystem** (every N ticks):
   - Emits `AICommand` buffer entries (one per agent)
   - Commands contain: agent entity, action index, target entity/position

6. **Bridge Systems** (domain-specific):
   - Consume `AICommand` buffer
   - Map action index to domain-specific goal (e.g., `VillagerAIState.Goal`)
   - Update entity state (target entity, target position, goal)

### Intent Pipeline (Interrupt-Driven)

1. **Interrupt Emission** (various systems):
   - Perception systems emit interrupts on new threats/resources
   - Combat systems emit interrupts on damage/weapon ready
   - Order systems emit interrupts on new orders
   - Status systems emit interrupts on low health/resources

2. **InterruptHandlerSystem** (every tick):
   - Processes `Interrupt` buffer entries (highest priority first)
   - Maps interrupt type to `IntentMode`
   - Updates `EntityIntent` component (mode, target, priority)
   - Marks interrupts as processed

3. **Intent Consumption** (domain-specific systems):
   - Read `EntityIntent` component
   - Execute intent (move to target, attack, flee, etc.)
   - Validate intent is still valid (target still exists, priority still high)

---

## Integration Points

### Spatial Grid

- **Perception queries spatial grid** for nearby entities (`SpatialQueryHelper.GetEntitiesWithinRadius`)
- **Signal field stored on grid entity** (`SignalFieldCell` buffer, one per cell)
- **Sensor readings use spatial queries** (category filtering, range checks)

### Communication System

- **Interrupts can be emitted from comm receipts** (new message, order received)
- **Intent mode ExecuteOrder** triggers order execution via communication system
- **Group intent** (squad leader) distributed via communication links

### Time System

- **Sensor updates respect rewind state** (only update in Record mode)
- **Interrupt timestamps** use `TimeState.Tick` for determinism
- **Action cooldowns** use tick-based timing

### Medium System

- **Channels filtered by medium type** (Vision doesn't work in vacuum, Hearing doesn't work in vacuum)
- **Signal emissions filtered by medium** (`MediumUtilities.FilterChannels`)
- **Perception confidence affected by medium** (e.g., sound propagates better in water)

---

## Gaps and Limitations

### Perception Gaps

1. **Line-of-Sight Queries:** Phase 1 uses simple FOV checks; Phase 2 needs actual raycast occlusion queries
2. **Channel-Specific Behaviors:** Smell diffusion, sound propagation, EM blocking not yet implemented
3. **Miracle Detection:** `AISensorCategory.Miracle` exists but detection logic missing
4. **Persistent Tracking:** "Lost track" logic (remember last-seen position when entity leaves range) not implemented

### Action/Utility Gaps

1. **Virtual Sensors Incomplete:** `AIVirtualSensorSystem` exists but needs full needs integration (hunger, energy, morale mapped to sensor indices)
2. **Action State Machines:** Multi-stage goals (Gather → Deliver → Rest) require bridge system logic, not built into utility system
3. **Context-Aware Scoring:** Actions can't conditionally check state (e.g., "Deliver only if inventory full") without bridge system overrides
4. **Commitment/Anti-Thrashing:** Actions flip-flop if scores change slightly; need commitment timer/abort cost (see `AI_Optimization_Methodologies.md`)

### Intent/Interrupt Gaps

1. **Intent Validation:** No automatic validation that intent is still valid (target destroyed, priority changed)
2. **Intent Interruption:** Low-priority intents don't automatically yield to high-priority interrupts
3. **Intent Persistence:** Intents don't have "commitment" (agents switch intents too easily)

### Performance Gaps

1. **Sensor Update Frequency:** All sensors update at same cadence; should use LOD (near camera = frequent, far = rare)
2. **Signal Field Scaling:** Signal field updates all cells every tick; should use active set (only update cells with emitters or recent activity)
3. **Utility Scoring Cost:** All actions scored every think interval; should cache scores, only re-evaluate on sensor changes

---

## Malpractices and Anti-Patterns

### Perception Anti-Patterns

❌ **Per-Entity Raycasts:** Don't raycast from every entity to every target. Use spatial queries + optional LOS cache.

❌ **Hardcoded Detection Logic:** Don't hardcode "villagers detect via vision, ships detect via EM." Use `SenseCapability.EnabledChannels` and channel-based detection.

❌ **Polling Perception:** Don't constantly check "is threat nearby?" Use interrupts (`NewThreatDetected`) to react to perception changes.

✅ **Use Spatial Queries:** Query spatial grid for nearby entities, then filter by category/signature.

✅ **Channel-Based Detection:** Use `PerceptionChannel` flags and `SensorSignature` for flexible detection.

✅ **Signal Field for Emissions:** Use signal field for smell/sound/EM emissions (efficient grid-based approach).

### Action/Utility Anti-Patterns

❌ **Hardcoded Action Selection:** Don't write "if hunger > 0.8, eat." Use utility curves and blob assets.

❌ **Spatial-Only Scoring:** Don't only score actions based on spatial entities. Use virtual sensors for internal needs.

❌ **Constant Re-Evaluation:** Don't score all actions every tick. Use think intervals and only re-evaluate on sensor changes.

✅ **Data-Driven Curves:** Define utility curves in blob assets, reference via `UtilityCurveRef`.

✅ **Unified Sensor Readings:** Use both spatial sensors (nearby resources) and virtual sensors (hunger, energy) in same utility evaluation.

✅ **Throttled Decisions:** Use `ThinkInterval` and `MinActionDurationTicks` to prevent flip-flopping.

### Intent/Interrupt Anti-Patterns

❌ **Polling for State Changes:** Don't constantly check "is health low?" Emit `LowHealth` interrupt when threshold crossed.

❌ **Direct State Modification:** Don't directly modify entity behavior from interrupt. Update `EntityIntent`, let systems consume intent.

❌ **Ignoring Priority:** Don't process interrupts in arbitrary order. Sort by priority (Critical > Urgent > High > Normal > Low).

✅ **Event-Driven Intents:** Emit interrupts on state changes (perception deltas, damage, orders), let `InterruptHandlerSystem` update intents.

✅ **Intent as Contract:** `EntityIntent` is a contract between interrupt system and behavior systems. Don't bypass it.

✅ **Priority-Aware Processing:** Process interrupts by priority, higher priority intents override lower priority.

---

## Performance Characteristics

### Perception Performance

- **Spatial Queries:** O(log n) per query (spatial grid), cached results per sensor
- **Channel Detection:** O(1) per channel per target (simple math, no loops)
- **Signal Field:** O(emitters) update cost, O(1) sampling cost
- **Update Frequency:** Throttled by `UpdateInterval` (typically 0.5-2.0 seconds)

### Action/Utility Performance

- **Utility Scoring:** O(actions × factors) per entity, typically 4-8 actions × 1-3 factors = 4-24 operations
- **Virtual Sensors:** O(1) per need (direct reads), O(needs) total
- **Think Frequency:** Throttled by `ThinkInterval` (typically 1-5 seconds)

### Intent/Interrupt Performance

- **Interrupt Processing:** O(interrupts × priority levels), typically 0-8 interrupts per entity
- **Intent Updates:** O(1) per entity (single component write)
- **Update Frequency:** Every tick (but interrupts are rare, only emitted on state changes)

---

## Recommendations

### Phase 1 (Current State)

✅ **Perception channels working** (Vision, Hearing, EM, Gravitic, etc.)  
✅ **Signal field for emissions** (smell/sound/EM)  
✅ **Utility scoring with blob assets** (data-driven actions)  
✅ **Interrupt-driven intents** (event-driven behavior changes)  
⚠️ **Virtual sensors incomplete** (needs full needs integration)  
⚠️ **LOS queries missing** (simple FOV checks only)

### Phase 2 (Near-Term Improvements)

1. **Complete Virtual Sensors:** Map all needs (hunger, energy, morale, health) to sensor indices, update utility blobs
2. **LOS Queries:** Add raycast-based occlusion checks for Vision/EM channels
3. **Intent Validation:** Automatic validation that intent targets still exist, priority still high
4. **Commitment/Anti-Thrashing:** Add commitment timer to actions, prevent flip-flopping (see `AI_Optimization_Methodologies.md`)

### Phase 3 (Long-Term Enhancements)

1. **Channel-Specific Behaviors:** Smell diffusion, sound propagation, EM blocking
2. **Persistent Tracking:** "Lost track" logic (remember last-seen position)
3. **Simulation LOD:** Sensor updates based on distance to camera (near = frequent, far = rare)
4. **Confidence-Aware Decisions:** Store confidence on perceptions, low confidence → scout/clarify (see `AI_Optimization_Methodologies.md`)

---

## Related Documentation

- **AI Integration Guide:** `Docs/Guides/AI_Integration_Guide.md` - Developer guide for integrating entities with AI pipeline
- **AI Behavior Modules:** `Docs/Mechanics/AIBehaviorModules.md` - Architecture and module design
- **AI Optimization Methodologies:** `Docs/Concepts/Core/AI_Optimization_Methodologies.md` - Attention/interrupts, commitment, confidence-aware decisions
- **Spatial Grid Summary:** `Docs/Concepts/Core/Spatial_Grid_System_Summary.md` - Spatial query foundation
- **Communication System:** `Docs/Concepts/Core/Communication_And_Language_System.md` - Order distribution via comms

---

**Miracle Detection Contract:**
- All miracle effect entities MUST have `LocalTransform` component (spatial residency)
- All miracle effect entities MUST have `SensorSignature` OR `SensorySignalEmitter` (at least one required)
- **Direct detection:** Use `SensorSignature` for visible/contact miracles (e.g., Fireball with Vision channel)
- **Field detection:** Use `SensorySignalEmitter` for ambient/area miracles (e.g., Food miracle with Smell channel)
- Validation enforced by `MiracleDetectabilityBootstrapSystem` (debug builds only)

**For Implementers:** Focus on completing virtual sensors, implementing intent validation, and ensuring miracles are detectable  
**For Architects:** Review interrupt-driven architecture, consider commitment/anti-thrashing for action stability  
**For Designers:** Use utility curves for tuning behavior, design interrupt types for reactive behaviors
