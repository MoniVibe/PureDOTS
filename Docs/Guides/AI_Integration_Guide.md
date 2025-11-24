# AI Integration Guide

This guide explains how to integrate entities (villagers, vessels, NPCs) with the shared PureDOTS AI pipeline.

## Overview

PureDOTS provides a modular AI framework (`AISystemGroup`) that handles:
1. **Sensing** - Spatial queries to detect nearby entities
2. **Utility Scoring** - Evaluate actions based on sensor readings and archetype curves
3. **Steering** - Calculate movement direction and velocity
4. **Task Resolution** - Emit commands for domain-specific systems to consume

Entities opt into this pipeline by adding AI components during authoring, and domain-specific bridge systems translate generic AI commands into game-specific behavior.

## Architecture

```
AISystemGroup Pipeline:
├─ AISensorUpdateSystem (samples spatial grid, populates sensor buffers)
├─ AIUtilityScoringSystem (scores actions based on sensor readings + archetype blobs)
├─ AISteeringSystem (calculates movement direction)
└─ AITaskResolutionSystem (emits AICommand buffer)

Domain Bridge Systems:
├─ GodgameVillagerAICommandBridgeSystem (consumes AICommand → VillagerAIState)
└─ Space4XVesselAICommandBridgeSystem (consumes AICommand → VesselAIState)
```

## Adding AI to an Entity

### Step 1: Add AI Components During Authoring

In your authoring component's Baker, add:

```csharp
// 1. Create AI utility archetype blob
var blobBuilder = new BlobBuilder(Allocator.Temp);
ref var root = ref blobBuilder.ConstructRoot<AIUtilityArchetypeBlob>();
var actions = blobBuilder.Allocate(ref root.Actions, 2); // 2 actions

// Define action 0
ref var action0 = ref actions[0];
var factors0 = blobBuilder.Allocate(ref action0.Factors, 1);
factors0[0] = new AIUtilityCurveBlob
{
    SensorIndex = 0, // Which sensor reading to use
    Threshold = 0.3f, // Minimum value to trigger
    Weight = 2f, // Priority multiplier
    ResponsePower = 2f, // Curve shape (1=linear, 2=quadratic)
    MaxValue = 1f
};

var utilityBlob = blobBuilder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Temp);
blobBuilder.Dispose();
AddBlobAsset(ref utilityBlob, out _);

// 2. Add sensor configuration
AddComponent(entity, new AISensorConfig
{
    UpdateInterval = 0.5f, // Update sensors every 0.5 seconds
    Range = 30f, // Detection range
    MaxResults = 8, // Max entities to track
    PrimaryCategory = AISensorCategory.ResourceNode,
    SecondaryCategory = AISensorCategory.Storehouse
});

AddComponent(entity, new AISensorState());
AddBuffer<AISensorReading>(entity);

// 3. Add behavior archetype
AddComponent(entity, new AIBehaviourArchetype
{
    UtilityBlob = utilityBlob
});

AddComponent(entity, new AIUtilityState());
AddBuffer<AIActionState>(entity);

// 4. Add steering configuration
AddComponent(entity, new AISteeringConfig
{
    MaxSpeed = 3f,
    Acceleration = 8f,
    Responsiveness = 0.5f,
    DegreesOfFreedom = 2, // 2D or 3D movement
    ObstacleLookAhead = 2f
});

AddComponent(entity, new AISteeringState());
AddComponent(entity, new AITargetState());

// 5. Add utility binding (maps action indices to domain-specific goals)
var binding = new YourEntityAIUtilityBinding();
binding.Goals.Add(YourGoal.Action1); // Action 0 maps to Action1
binding.Goals.Add(YourGoal.Action2); // Action 1 maps to Action2
AddComponent(entity, binding);
```

### Step 2: Create a Bridge System

Create a system that consumes `AICommand` and updates your entity's state:

```csharp
[UpdateInGroup(typeof(YourSystemGroup))]
[UpdateAfter(typeof(PureDOTS.Systems.AI.AITaskResolutionSystem))]
public partial struct YourEntityAICommandBridgeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var queueEntity = SystemAPI.GetSingletonEntity<AICommandQueueTag>();
        var commands = SystemAPI.GetBuffer<AICommand>(queueEntity);

        foreach (var command in commands)
        {
            if (!HasComponent<YourEntityState>(command.Agent))
                continue;

            var goal = MapActionToGoal(command.Agent, command.ActionIndex);
            var entityState = GetComponent<YourEntityState>(command.Agent);
            entityState.CurrentGoal = goal;
            entityState.TargetEntity = command.TargetEntity;
            SetComponent(command.Agent, entityState);
        }
    }
}
```

### Step 3: Define Sensor Categories (if needed)

If your entities need to detect custom entity types, extend `AISensorCategory`:

```csharp
public enum AISensorCategory : byte
{
    // ... existing categories ...
    YourCustomType = 6,
    Custom1 = 241
}
```

Then update `AISensorCategoryFilter` in `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` to recognize your component types.

## Examples

### Godgame Villagers

See `Assets/Projects/Godgame/Scripts/Godgame/Authoring/VillagerAuthoring.cs`:
- Adds all AI components during baking
- Creates 4-action utility blob (SatisfyHunger, Rest, ImproveMorale, Work)
- Maps actions to `VillagerAIState.Goal` via `VillagerAIUtilityBinding`
- Bridge system: `GodgameVillagerAICommandBridgeSystem`

### Space4X Vessels

See `Assets/Scripts/Space4x/Authoring/VesselAuthoring.cs`:
- Adds all AI components during baking
- Creates 2-action utility blob (Mining, Returning)
- Maps actions to `VesselAIState.Goal` via `VesselAIUtilityBinding`
- Bridge system: `Space4XVesselAICommandBridgeSystem`

## Sensor Categories

Available categories:
- `Villager` - Detects entities with `VillagerId`
- `ResourceNode` - Detects entities with `ResourceSourceConfig`
- `Storehouse` - Detects entities with `StorehouseConfig`
- `TransportUnit` - Detects `MinerVessel`, `Carrier`, `Hauler`, `Freighter`, `Wagon` (Space4X only)
- `Miracle` - Reserved for future miracle detection

## Utility Blob Design

Actions are scored by summing weighted utility curves. Each curve samples a sensor reading:

```
Action Score = Σ (Curve(sensor_value) * weight)
```

Where `Curve(x) = pow(max((x - threshold) / maxValue, 0), responsePower)`

Example: An action with threshold=0.3, weight=2, responsePower=2 will:
- Score 0 if sensor reading < 0.3
- Score quadratically higher as reading approaches 1.0
- Multiply final score by 2

## System Execution Order

The AI pipeline runs in this order:
1. `AISensorUpdateSystem` - Populates sensor readings
2. `AIUtilityScoringSystem` - Scores actions, selects best
3. `AISteeringSystem` - Calculates movement
4. `AITaskResolutionSystem` - Emits commands
5. Your bridge system - Consumes commands, updates entity state

Ensure your bridge system runs after `AITaskResolutionSystem` using `[UpdateAfter]`.

## Limitations & Future Work

- **Needs-based scoring**: Currently, villagers use `VillagerUtilityScheduler` for needs (hunger/energy/morale) because these aren't spatial entities. Future: create virtual sensor readings for internal state.
- **TransportUnit detection**: Requires `SPACE4X_TRANSPORT` define or Space4X assembly reference. Consider making this more generic.
- **Miracle detection**: Not yet implemented - needs miracle component integration.

## Testing

See `Assets/Tests/Playmode/AIIntegrationTests.cs` for examples of:
- Creating entities with AI components
- Validating utility bindings
- Testing command queue creation

## Troubleshooting

**Entities not finding targets:**
- Ensure `AISensorConfig` has correct `PrimaryCategory`/`SecondaryCategory`
- Verify spatial grid is populated (`SpatialGridState.TotalEntries > 0`)
- Check sensor update interval isn't too long

**Commands not being consumed:**
- Verify bridge system runs after `AITaskResolutionSystem`
- Check bridge system queries for correct component requirements
- Ensure `RewindState.Mode == RewindMode.Record`

**Utility scores always zero:**
- Verify utility blob is created and assigned
- Check sensor readings are being populated (`AISensorReading` buffer length > 0)
- Ensure `SensorIndex` in curves matches sensor reading indices

---

## Advanced Behavior Recipes

This section provides step-by-step recipes for creating advanced AI behaviors without modifying core systems.

### Recipe 1: Multi-Stage Goals (e.g., Gather → Deliver → Rest)

**Use Case**: Create behaviors that require multiple sequential actions (e.g., villager gathers resources, delivers to storehouse, then rests).

**Approach**: Use multiple actions with different sensor dependencies, and handle state transitions in your bridge system.

```csharp
// In your utility blob helper:
var actions = builder.Allocate(ref root.Actions, 3);

// Action 0: Gather (prioritizes nearby resources)
factors0[0] = new AIUtilityCurveBlob
{
    SensorIndex = 0, // Resource node sensor reading
    Threshold = 0.1f,
    Weight = 1.5f,
    ResponsePower = 1.5f,
    MaxValue = 1f
};

// Action 1: Deliver (prioritizes when inventory is full AND storehouse nearby)
// Note: This requires checking inventory state in bridge system
factors1[0] = new AIUtilityCurveBlob
{
    SensorIndex = 1, // Storehouse sensor reading
    Threshold = 0.2f,
    Weight = 2f, // Higher priority when inventory full
    ResponsePower = 2f,
    MaxValue = 1f
};

// Action 2: Rest (prioritizes low energy)
factors2[0] = new AIUtilityCurveBlob
{
    SensorIndex = 2, // Energy virtual sensor (when implemented)
    Threshold = 0.3f,
    Weight = 1f,
    ResponsePower = 1f,
    MaxValue = 1f
};

// In your bridge system:
public void OnUpdate(ref SystemState state)
{
    foreach (var command in commands)
    {
        var inventory = GetComponent<VillagerInventory>(command.Agent);
        var needs = GetComponent<VillagerNeeds>(command.Agent);
        
        // Override action 1 (Deliver) if inventory not full
        if (command.ActionIndex == 1 && inventory.TotalWeight < inventory.Capacity * 0.8f)
        {
            continue; // Skip this command, let AI re-evaluate
        }
        
        // Override action 2 (Rest) if energy is high
        if (command.ActionIndex == 2 && needs.EnergyFloat > 50f)
        {
            continue;
        }
        
        // Apply command...
    }
}
```

**Key Points**:
- Use sensor readings for spatial targets (resources, storehouses)
- Use bridge system to add context checks (inventory state, needs)
- Let AI pipeline handle spatial selection, bridge handles state validation

---

### Recipe 2: Morale-Based Behaviors (Godgame)

**Use Case**: Villagers seek entertainment when morale is low, work harder when morale is high.

**Approach**: Create utility curves that respond to morale virtual sensor (when implemented).

```csharp
// Action 0: Seek Entertainment (prioritizes low morale)
factors0[0] = new AIUtilityCurveBlob
{
    SensorIndex = 3, // Morale virtual sensor (0 = low morale, 1 = high morale)
    Threshold = 0.4f, // Trigger when morale below 40%
    Weight = 2f, // High priority
    ResponsePower = 2f, // Quadratic - urgent when very low
    MaxValue = 1f
};

// Action 1: Work (prioritizes high morale + job availability)
// Use multiple factors for complex scoring
var factors1 = builder.Allocate(ref action1.Factors, 2);
factors1[0] = new AIUtilityCurveBlob
{
    SensorIndex = 3, // Morale (inverted - high morale = high utility)
    Threshold = 0.6f, // Prefer when morale above 60%
    Weight = 1.5f,
    ResponsePower = 1.5f,
    MaxValue = 1f
};
factors1[1] = new AIUtilityCurveBlob
{
    SensorIndex = 4, // Job availability sensor
    Threshold = 0.1f,
    Weight = 1f,
    ResponsePower = 1f,
    MaxValue = 1f
};
// Action score = (morale_factor * 1.5) + (job_factor * 1.0)
```

**Key Points**:
- Use multiple factors per action for complex scoring
- Invert sensor values if needed (high morale → high work utility)
- Tune thresholds and weights to balance behaviors

---

### Recipe 3: Escort & Formation Behaviors (Space4X)

**Use Case**: Drones escort carriers, maintaining formation positions.

**Approach**: Add escort target sensor category and create formation utility curves.

```csharp
// Step 1: Add escort sensor category (in AIComponents.cs)
public enum AISensorCategory : byte
{
    // ... existing ...
    EscortTarget = 6, // Detects entities with EscortTargetTag
}

// Step 2: Update AISensorCategoryFilter to detect escort targets
case AISensorCategory.EscortTarget:
    if (EscortTargetLookup.HasComponent(entry.Entity))
        return true;
    break;

// Step 3: Create utility blob with escort action
// Action 0: Escort (prioritizes nearby escort targets)
factors0[0] = new AIUtilityCurveBlob
{
    SensorIndex = 0, // Escort target sensor reading
    Threshold = 0.2f, // Maintain 20% of max range
    Weight = 3f, // Very high priority
    ResponsePower = 2f,
    MaxValue = 1f
};

// Step 4: In bridge system, calculate formation offset
public void OnUpdate(ref SystemState state)
{
    foreach (var command in commands)
    {
        if (command.ActionIndex == 0) // Escort action
        {
            var escortTarget = command.TargetEntity;
            var targetTransform = GetComponent<LocalTransform>(escortTarget);
            var agentTransform = GetComponent<LocalTransform>(command.Agent);
            
            // Calculate formation offset (e.g., 10m to the right)
            var formationOffset = new float3(10f, 0f, 0f);
            var desiredPosition = targetTransform.Position + formationOffset;
            
            var aiState = GetComponent<VesselAIState>(command.Agent);
            aiState.TargetPosition = desiredPosition;
            SetComponent(command.Agent, aiState);
        }
    }
}
```

**Key Points**:
- Add custom sensor categories for game-specific entity types
- Use bridge system to calculate formation positions
- Sensor readings provide target selection, bridge provides positioning

---

### Recipe 4: Threat-Aware Behaviors (Flee vs. Fight)

**Use Case**: Entities flee from strong threats but attack weak ones.

**Approach**: Use threat sensor with strength evaluation, create separate flee/attack actions.

```csharp
// Action 0: Flee (prioritizes strong threats nearby)
factors0[0] = new AIUtilityCurveBlob
{
    SensorIndex = 0, // Threat sensor reading (normalized threat strength)
    Threshold = 0.7f, // Flee when threat strength > 70%
    Weight = 5f, // Very high priority - survival first
    ResponsePower = 3f, // Cubic - urgent when very strong
    MaxValue = 1f
};

// Action 1: Attack (prioritizes weak threats)
factors1[0] = new AIUtilityCurveBlob
{
    SensorIndex = 0, // Same threat sensor, but inverted logic
    Threshold = 0.3f, // Attack when threat strength < 30%
    Weight = 2f,
    ResponsePower = 1.5f,
    MaxValue = 1f
};

// In bridge system, invert threat sensor for attack action
// (or create separate sensor reading for threat weakness)
```

**Key Points**:
- Use threshold differences to create behavior boundaries
- High `ResponsePower` creates sharp transitions (flee vs. attack)
- Consider creating separate sensor readings for different perspectives (threat strength vs. weakness)

---

### Recipe 5: Time-of-Day Behaviors

**Use Case**: Villagers work during day, rest at night.

**Approach**: Use virtual sensor for time-of-day, create time-aware utility curves.

```csharp
// Action 0: Work (prioritizes daytime + job availability)
var factors0 = builder.Allocate(ref action0.Factors, 2);
factors0[0] = new AIUtilityCurveBlob
{
    SensorIndex = 5, // Time-of-day virtual sensor (0 = midnight, 1 = noon)
    Threshold = 0.3f, // Prefer daytime (0.3 = 7am, 0.7 = 7pm)
    Weight = 1.5f,
    ResponsePower = 2f, // Strong preference for midday
    MaxValue = 1f
};
factors0[1] = new AIUtilityCurveBlob
{
    SensorIndex = 4, // Job availability
    Threshold = 0.1f,
    Weight = 1f,
    ResponsePower = 1f,
    MaxValue = 1f
};

// Action 1: Rest (prioritizes nighttime)
factors1[0] = new AIUtilityCurveBlob
{
    SensorIndex = 5, // Time-of-day (inverted - night = high utility)
    Threshold = 0.7f, // Prefer nighttime (after 7pm)
    Weight = 2f,
    ResponsePower = 2f,
    MaxValue = 1f
};
```

**Key Points**:
- Virtual sensors can represent any internal state (time, weather, etc.)
- Combine multiple factors for complex behaviors
- Use thresholds to create time windows (e.g., work 7am-7pm)

---

## Debugging Tips

### Visualizing Sensor Ranges

**In Editor**: Add gizmo drawer to visualize sensor ranges:
```csharp
[DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
static void DrawSensorRange(AISensorConfig config, Transform transform, GizmoType gizmoType)
{
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(transform.position, config.Range);
}
```

**In Runtime**: Use debug overlay to show sensor readings:
```csharp
// In your debug system:
var readings = GetBuffer<AISensorReading>(entity);
for (int i = 0; i < readings.Length; i++)
{
    var reading = readings[i];
    var targetPos = GetComponent<LocalTransform>(reading.Target).Position;
    Debug.DrawLine(entityPos, targetPos, Color.green);
}
```

### Inspecting Utility Scores

Add debug logging to `AIUtilityScoringSystem`:
```csharp
// Log top 3 actions for selected entity
var actions = GetBuffer<AIActionState>(entity);
for (int i = 0; i < math.min(3, actions.Length); i++)
{
    Debug.Log($"Action {i}: Score={actions[i].Score}, SensorIndex={actions[i].SensorIndex}");
}
```

### Command Queue Inspection

Check command queue contents:
```csharp
var queueEntity = SystemAPI.GetSingletonEntity<AICommandQueueTag>();
var commands = SystemAPI.GetBuffer<AICommand>(queueEntity);
Debug.Log($"Command queue: {commands.Length} commands");
foreach (var cmd in commands)
{
    Debug.Log($"  Agent={cmd.Agent}, Action={cmd.ActionIndex}, Target={cmd.TargetEntity}");
}
```

### Performance Profiling

Enable performance metrics (when implemented):
```csharp
var metrics = SystemAPI.GetSingleton<AIPerformanceMetrics>();
Debug.Log($"Sensor Update: {metrics.SensorUpdateTimeMs}ms");
Debug.Log($"Utility Scoring: {metrics.UtilityScoringTimeMs}ms");
Debug.Log($"Steering: {metrics.SteeringTimeMs}ms");
```

---

## Performance Considerations

### Sensor Update Frequency

**Rule of Thumb**: Update sensors every 0.5-1.0 seconds for most entities. Reduce frequency for:
- Static entities (buildings, resources)
- Low-priority entities (idle villagers)
- Large crowds (use spatial caching)

```csharp
AISensorConfig
{
    UpdateInterval = 0.5f, // Good default
    Range = 30f, // Balance detection vs. performance
    MaxResults = 8, // Limit to reduce processing
}
```

### Utility Scoring Complexity

**Rule of Thumb**: Keep actions to 2-5 per entity. Each action can have 1-3 factors.

**Performance Impact**:
- 10k entities × 3 actions × 2 factors = 60k curve evaluations per frame
- Target: < 1ms for utility scoring at 10k entities

**Optimization Tips**:
- Use `ResponsePower = 1f` (linear) when possible (faster than quadratic)
- Limit `MaxResults` in sensor config
- Cache utility scores for multiple ticks when state doesn't change

### Steering Calculations

**Rule of Thumb**: Steering runs every frame. Keep calculations simple.

**Performance Impact**:
- 10k entities × steering calc = 10k operations per frame
- Target: < 0.5ms for steering at 10k entities

**Optimization Tips**:
- Use 2D steering when possible (faster than 3D)
- Cache obstacle queries (don't query every frame)
- Use flow fields for crowd navigation (shared calculations)

### Memory Allocations

**Rule of Thumb**: Zero allocations per frame in AI systems.

**Common Allocations**:
- `NativeList` creation in `OnUpdate` → Use `NativeList` fields, reuse
- `BlobAssetReference` lookups → Cache in system state
- Buffer resizing → Pre-allocate buffers during authoring

**Check for Allocations**:
```csharp
// In Unity Profiler, check "GC Alloc" column
// Should be 0 B for AI systems
```

---

## Tuning Guidelines

### Utility Curve Parameters

**Threshold**: Minimum sensor value to trigger action
- Low threshold (0.1-0.3): Action triggers early, more frequent
- High threshold (0.7-0.9): Action triggers late, less frequent
- **Tuning**: Start at 0.5, adjust based on behavior frequency

**Weight**: Priority multiplier for action
- Low weight (0.5-1.0): Low priority, only when other actions unavailable
- High weight (2.0-5.0): High priority, overrides other actions
- **Tuning**: Start at 1.0, increase if action never selected

**ResponsePower**: Curve shape (1 = linear, 2 = quadratic, 3 = cubic)
- Linear (1.0): Gradual response, smooth transitions
- Quadratic (2.0): Sharp response, clear priorities
- Cubic (3.0): Very sharp, binary-like behavior
- **Tuning**: Start at 1.5, increase for sharper transitions

**MaxValue**: Normalization value for sensor reading
- Usually 1.0 (sensor readings are normalized 0-1)
- Adjust if sensor readings use different range

### Sensor Range Tuning

**Rule of Thumb**: 
- Short range (10-20m): Fast decisions, local awareness
- Medium range (30-50m): Balanced awareness vs. performance
- Long range (50-100m): Strategic decisions, higher cost

**Tuning Process**:
1. Start with medium range (30m)
2. Increase if entities miss targets
3. Decrease if performance issues
4. Use different ranges for different categories (e.g., threats = long range, resources = medium range)

### Action Count Optimization

**Rule of Thumb**:
- Simple entities: 2-3 actions (e.g., gather, deliver)
- Complex entities: 4-6 actions (e.g., gather, deliver, rest, socialize)
- Avoid: > 8 actions (hard to tune, performance impact)

**Tuning Process**:
1. Start with essential actions only
2. Add actions incrementally
3. Remove actions that are never selected
4. Combine similar actions if possible

---

## Common Patterns

### Pattern 1: Priority Override

**Problem**: Action A should always override action B when condition X is true.

**Solution**: Use very high weight for action A, or handle in bridge system:
```csharp
// In bridge system:
if (needs.Health < 0.2f && command.ActionIndex != FLEE_ACTION)
{
    // Override with flee
    command.ActionIndex = FLEE_ACTION;
}
```

### Pattern 2: Cooldown Behavior

**Problem**: Action should have cooldown (e.g., can't rest immediately after resting).

**Solution**: Track last action time in bridge system:
```csharp
var lastActionTime = GetComponent<AILastActionTime>(entity);
if (command.ActionIndex == REST_ACTION && 
    currentTime - lastActionTime.RestTime < REST_COOLDOWN)
{
    continue; // Skip command
}
```

### Pattern 3: Context-Dependent Scoring

**Problem**: Action utility depends on multiple conditions (e.g., gather only if inventory has space).

**Solution**: Use multiple factors OR handle in bridge system:
```csharp
// Option 1: Multiple factors (preferred)
factors0[0] = hungerFactor;
factors0[1] = inventorySpaceFactor; // Virtual sensor for inventory

// Option 2: Bridge system validation (fallback)
if (command.ActionIndex == GATHER_ACTION && inventory.IsFull)
{
    continue; // Skip, let AI re-evaluate
}
```

---

## Next Steps

1. **Virtual Sensors**: Implement virtual sensor system for internal needs (see `Docs/AI_Gap_Audit.md`)
2. **Miracle Detection**: Add miracle detection to sensor system
3. **Performance Metrics**: Add performance tracking and validation framework
4. **Visualization Tools**: Create editor gizmos and runtime debug overlays

See `Docs/AI_Backlog.md` for prioritized implementation items.

