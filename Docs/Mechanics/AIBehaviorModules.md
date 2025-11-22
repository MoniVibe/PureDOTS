# AI Behavior Module Framework (Shared)

## Overview

The AI Behavior Module Framework is a **composable, data-driven AI system** that handles autonomous decision-making for entities in both Space4X and Godgame. It uses a **modular architecture** where behaviors are built from reusable components: Sensors (perceive world), Utilities (evaluate options), Steering (execute movement), and Tasks (perform actions).

This document defines the universal AI framework that works for villagers, crew, carriers, creatures, and aggregates.

---

## Core Concept

**AI entities are NOT hardcoded scripts.** Instead:
- **Behaviors are data** (ScriptableObject profiles + blob assets)
- **Sensors provide input** (detect threats, resources, allies)
- **Utility functions score options** (which action is best right now?)
- **Steering executes movement** (pathfinding, collision avoidance)
- **Tasks perform actions** (gather, attack, heal, trade)
- **State machines track progress** (idle → working → returning)

Both games share the same AI spine. Game-specific logic is **configuration**, not code.

---

## AI Entity Components

### 1. Core AI Components

```csharp
// Base AI agent identity
public struct AIAgent : IComponentData
{
    public AIArchetype Archetype;                // Villager, Crew, Carrier, Creature, etc.
    public AIBehaviorMode Mode;                  // Current behavior mode
    public Entity BehaviorProfile;               // Reference to behavior config
    public float DecisionCooldown;               // Ticks until next decision
    public ushort ThinkInterval;                 // How often to re-evaluate (10-60 ticks)
}

public enum AIArchetype : byte
{
    // Godgame
    Villager,       // Individual villager
    Creature,       // Animal, monster
    Band,           // Combat group aggregate

    // Space4X
    Crew,           // Individual crew member
    Carrier,        // Individual ship
    Fleet,          // Ship group aggregate

    // Shared
    Aggregate       // Generic collective entity
}

public enum AIBehaviorMode : byte
{
    Idle,           // No task, waiting
    Working,        // Executing task
    Traveling,      // Moving to destination
    Fleeing,        // Escape from threat
    Attacking,      // Combat engagement
    Gathering,      // Resource collection
    Trading,        // Economic activity
    Socializing,    // Interaction with others
    Resting         // Recovery, morale boost
}
```

### 2. Sensor Components

Sensors detect entities and conditions in the world.

```csharp
// Buffer of detected entities
public struct AISensorReading : IBufferElementData
{
    public Entity DetectedEntity;
    public SensorType Type;                      // What sensor detected this
    public float Distance;                       // How far away
    public float3 Position;                      // Last known position
    public ushort DetectionTick;                 // When detected
    public float ThreatLevel;                    // 0.0-1.0 (how dangerous)
    public float Desirability;                   // 0.0-1.0 (how attractive)
}

public enum SensorType : byte
{
    Vision,         // Line-of-sight detection
    Hearing,        // Audio detection (footsteps, noise)
    Smell,          // Scent detection (creatures)
    Sensor,         // Tech sensors (Space4X radar/gravimetric)
    Registry,       // Query from registry (perfect knowledge)
    Memory          // Remembered from past detection
}
```

```csharp
// Sensor configuration (per agent)
public struct AISensorConfig : IComponentData
{
    public float VisionRange;                    // Max distance for Vision
    public float VisionAngle;                    // FOV in degrees (360 = omnidirectional)
    public float HearingRange;
    public float SmellRange;
    public float SensorRange;                    // Space4X tech sensors
    public SensorUpdateRate UpdateRate;
}

public enum SensorUpdateRate : byte
{
    VeryFast,       // Every 5 ticks (expensive, combat)
    Fast,           // Every 10 ticks
    Normal,         // Every 20 ticks (default)
    Slow,           // Every 60 ticks (background awareness)
    OnDemand        // Only when requested (manual scan)
}
```

### 3. Utility Scoring Components

Utility functions evaluate potential actions and select the best one.

```csharp
// Buffer of action options evaluated this decision cycle
public struct AIUtilityOption : IBufferElementData
{
    public ActionType Action;
    public Entity Target;                        // Target entity (resource, enemy, etc.)
    public float3 Destination;                   // Target location
    public float UtilityScore;                   // 0.0-1.0 (higher = better)
    public float ConfidenceLevel;                // 0.0-1.0 (certainty in decision)
}

public enum ActionType : byte
{
    // Universal
    Idle,
    MoveTo,
    Flee,

    // Godgame
    GatherResource,
    DeliverResource,
    BuildStructure,
    AttackEnemy,
    HealAlly,
    Socialize,
    Rest,
    Worship,

    // Space4X
    MineDeposit,
    HaulCargo,
    CombatEngage,
    Surveysector,
    ConstructStation,
    TradeWithStation,
    Refit,
    Dock,

    // Aggregate-specific
    FormGroup,
    SplitGroup,
    CoordinateAttack,
    Retreat
}
```

```csharp
// Utility curve configuration (defines how scores are calculated)
public struct AIUtilityCurve : IComponentData
{
    public ActionType Action;
    public CurveType Curve;                      // Linear, Exponential, Sigmoid, etc.
    public float MinInput;                       // Input range start
    public float MaxInput;                       // Input range end
    public float OutputMultiplier;               // Scale output score
}

public enum CurveType : byte
{
    Linear,         // y = x
    Exponential,    // y = x^2
    Logarithmic,    // y = log(x)
    Sigmoid,        // y = 1/(1+e^-x) (S-curve)
    Inverse,        // y = 1 - x
    Constant        // y = fixed value
}
```

### 4. Steering Components

Steering handles movement physics and pathfinding.

```csharp
public struct AISteeringState : IComponentData
{
    public float3 Velocity;                      // Current velocity
    public float3 DesiredVelocity;               // Target velocity
    public float MaxSpeed;                       // Top speed
    public float MaxAcceleration;                // How fast can change direction
    public float RotationSpeed;                  // Turning rate
    public SteeringMode Mode;
}

public enum SteeringMode : byte
{
    None,           // No steering (stationary or external physics)
    Seek,           // Move toward target
    Flee,           // Move away from target
    Wander,         // Random wandering
    FollowPath,     // Follow waypoint path
    Flock,          // Group movement (cohesion, separation, alignment)
    Orbit           // Circle around target
}
```

```csharp
// Path following (waypoint queue)
public struct AIPathWaypoint : IBufferElementData
{
    public float3 Position;
    public float ArrivalRadius;                  // Distance to consider "reached"
    public WaypointType Type;
}

public enum WaypointType : byte
{
    Standard,       // Normal waypoint
    Pause,          // Stop and wait at this point
    Jump,           // FTL jump point (Space4X)
    Highway,        // Highway gate (Space4X)
    Teleport        // Instant travel (magic portal, wormhole)
}
```

### 5. Task Execution Components

Tasks represent discrete actions the AI is performing.

```csharp
public struct AITaskState : IComponentData
{
    public ActionType CurrentAction;
    public Entity TargetEntity;
    public float3 TargetPosition;
    public TaskPhase Phase;
    public ushort TicksInPhase;
    public float ProgressPercent;                // 0.0-1.0
}

public enum TaskPhase : byte
{
    Planning,       // Evaluating if task is possible
    Preparing,      // Acquiring resources/tools
    Traveling,      // Moving to task location
    Executing,      // Performing task
    Completing,     // Finishing up
    Failed,         // Task cannot complete
    Aborted         // Task canceled externally
}
```

---

## AI System Architecture

### System Execution Order

```
FixedStepSimulationSystemGroup
  └─ AISystemGroup (custom group)
      ├─ AISensorUpdateSystem          [Reads world, populates sensor buffers]
      ├─ AIUtilityEvaluationSystem     [Scores actions, selects best]
      ├─ AITaskPlanningSystem          [Validates selected action, creates task]
      ├─ AISteeringSystem              [Calculates movement toward task target]
      ├─ AITaskExecutionSystem         [Performs task logic]
      └─ AIStateCleanupSystem          [Removes completed tasks, resets cooldowns]
```

---

## 1. AISensorUpdateSystem

**Responsibility**: Populate `AISensorReading` buffer with detected entities.

**Logic**:
```csharp
[BurstCompile]
public partial struct AISensorUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
{
        var timeState = SystemAPI.GetSingleton<TimeState>();

        foreach (var (agent, sensorConfig, sensorBuffer, transform) in
                 SystemAPI.Query<RefRO<AIAgent>, RefRO<AISensorConfig>, DynamicBuffer<AISensorReading>, RefRO<LocalTransform>>())
        {
            // Throttle updates based on UpdateRate
            if (agent.ValueRO.DecisionCooldown > 0)
                continue;

            sensorBuffer.Clear(); // Clear old readings

            // Vision sensor: spatial grid query within range
            var nearbyEntities = SpatialQuery.GetEntitiesWithinRadius(
                transform.ValueRO.Position,
                sensorConfig.ValueRO.VisionRange
            );

            foreach (var detected in nearbyEntities)
            {
                // Filter by FOV angle
                float3 dirToTarget = math.normalize(detected.Position - transform.ValueRO.Position);
                float3 forward = math.forward(transform.ValueRO.Rotation);
                float angle = math.degrees(math.acos(math.dot(dirToTarget, forward)));

                if (angle > sensorConfig.ValueRO.VisionAngle / 2)
                    continue; // Outside FOV

                // Line-of-sight check (raycast, optional for performance)
                if (!HasLineOfSight(transform.ValueRO.Position, detected.Position))
                    continue;

                // Add to sensor buffer
                sensorBuffer.Add(new AISensorReading
                {
                    DetectedEntity = detected.Entity,
                    Type = SensorType.Vision,
                    Distance = math.distance(transform.ValueRO.Position, detected.Position),
                    Position = detected.Position,
                    DetectionTick = timeState.CurrentTick,
                    ThreatLevel = CalculateThreat(agent.ValueRO, detected),
                    Desirability = CalculateDesirability(agent.ValueRO, detected)
                });
            }

            // Registry sensor: query known resources/entities (perfect knowledge)
            if (sensorConfig.ValueRO.SensorRange > 0)
            {
                // Example: query resource registry
                var resourceRegistry = SystemAPI.GetSingletonBuffer<ResourceRegistryEntry>();
                foreach (var resource in resourceRegistry)
                {
                    if (math.distance(transform.ValueRO.Position, resource.Position) > sensorConfig.ValueRO.SensorRange)
                        continue;

                    sensorBuffer.Add(new AISensorReading
                    {
                        DetectedEntity = resource.Entity,
                        Type = SensorType.Registry,
                        Distance = math.distance(transform.ValueRO.Position, resource.Position),
                        Position = resource.Position,
                        DetectionTick = timeState.CurrentTick,
                        ThreatLevel = 0f,
                        Desirability = 0.8f // Resources are desirable
                    });
                }
            }
        }
    }
}
```

**Threat Calculation** (example):
```csharp
float CalculateThreat(AIAgent agent, DetectedEntity detected)
{
    // Check if detected entity is hostile
    if (!HasComponent<Alignment>(detected.Entity))
        return 0f;

    var myAlignment = GetComponent<Alignment>(agent.Entity);
    var theirAlignment = GetComponent<Alignment>(detected.Entity);

    // Alignment mismatch = threat
    float alignmentDelta = math.distance(myAlignment.MoralAxis, theirAlignment.MoralAxis);
    alignmentDelta += math.distance(myAlignment.OrderAxis, theirAlignment.OrderAxis);
    alignmentDelta += math.distance(myAlignment.PurityAxis, theirAlignment.PurityAxis);

    // Check if they have weapons/combat capability
    float combatThreat = HasComponent<CombatCapability>(detected.Entity) ? 0.5f : 0f;

    return math.clamp(alignmentDelta / 6f + combatThreat, 0f, 1f);
}
```

---

## 2. AIUtilityEvaluationSystem

**Responsibility**: Evaluate all possible actions, score them, select best option.

**Logic**:
```csharp
[BurstCompile]
public partial struct AIUtilityEvaluationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (agent, sensorBuffer, utilityBuffer, transform) in
                 SystemAPI.Query<RefRW<AIAgent>, DynamicBuffer<AISensorReading>, DynamicBuffer<AIUtilityOption>, RefRO<LocalTransform>>())
        {
            // Throttle decisions
            if (agent.ValueRO.DecisionCooldown > 0)
            {
                agent.ValueRW.DecisionCooldown--;
                continue;
            }

            utilityBuffer.Clear();

            // Evaluate each possible action based on archetype
            switch (agent.ValueRO.Archetype)
            {
                case AIArchetype.Villager:
                    EvaluateVillagerActions(ref agent.ValueRW, sensorBuffer, utilityBuffer, transform.ValueRO);
                    break;

                case AIArchetype.Carrier:
                    EvaluateCarrierActions(ref agent.ValueRW, sensorBuffer, utilityBuffer, transform.ValueRO);
                    break;

                case AIArchetype.Creature:
                    EvaluateCreatureActions(ref agent.ValueRW, sensorBuffer, utilityBuffer, transform.ValueRO);
                    break;
            }

            // Select highest scoring action
            if (utilityBuffer.Length == 0)
            {
                // No valid actions, go idle
                agent.ValueRW.Mode = AIBehaviorMode.Idle;
                agent.ValueRW.DecisionCooldown = agent.ValueRO.ThinkInterval;
                continue;
            }

            AIUtilityOption bestOption = utilityBuffer[0];
            for (int i = 1; i < utilityBuffer.Length; i++)
            {
                if (utilityBuffer[i].UtilityScore > bestOption.UtilityScore)
                    bestOption = utilityBuffer[i];
            }

            // Commit to best action (AITaskPlanningSystem will create task)
            agent.ValueRW.Mode = GetModeForAction(bestOption.Action);
            agent.ValueRW.DecisionCooldown = agent.ValueRO.ThinkInterval;

            // Store selected option (task system reads this)
            utilityBuffer.Clear();
            utilityBuffer.Add(bestOption); // Only keep best option
        }
    }
}
```

**Example: Villager Action Evaluation**
```csharp
void EvaluateVillagerActions(
    ref AIAgent agent,
    DynamicBuffer<AISensorReading> sensors,
    DynamicBuffer<AIUtilityOption> options,
    LocalTransform transform)
{
    // Action: GatherResource
    foreach (var sensor in sensors)
    {
        if (!IsResource(sensor.DetectedEntity))
            continue;

        float distance = sensor.Distance;
        float distanceScore = 1f - math.clamp(distance / 100f, 0f, 1f); // Closer = better
        float desirability = sensor.Desirability;

        float utilityScore = distanceScore * 0.6f + desirability * 0.4f;

        options.Add(new AIUtilityOption
        {
            Action = ActionType.GatherResource,
            Target = sensor.DetectedEntity,
            Destination = sensor.Position,
            UtilityScore = utilityScore,
            ConfidenceLevel = 0.8f
        });
    }

    // Action: Flee (if threat detected)
    float maxThreat = 0f;
    Entity mostDangerousEntity = Entity.Null;
    foreach (var sensor in sensors)
    {
        if (sensor.ThreatLevel > maxThreat)
        {
            maxThreat = sensor.ThreatLevel;
            mostDangerousEntity = sensor.DetectedEntity;
        }
    }

    if (maxThreat > 0.5f)
    {
        // Flee from threat
        float3 fleeDirection = math.normalize(transform.Position - GetPosition(mostDangerousEntity));
        float3 fleeDestination = transform.Position + fleeDirection * 50f;

        options.Add(new AIUtilityOption
        {
            Action = ActionType.Flee,
            Target = mostDangerousEntity,
            Destination = fleeDestination,
            UtilityScore = maxThreat, // Higher threat = higher priority to flee
            ConfidenceLevel = 1.0f
        });
    }

    // Action: Rest (if low energy)
    var villagerNeeds = GetComponent<VillagerNeeds>(agent.Entity);
    if (villagerNeeds.Energy < 30f)
    {
        float restUrgency = 1f - (villagerNeeds.Energy / 100f);

        options.Add(new AIUtilityOption
        {
            Action = ActionType.Rest,
            Target = Entity.Null,
            Destination = transform.Position, // Rest in place
            UtilityScore = restUrgency,
            ConfidenceLevel = 1.0f
        });
    }

    // Action: Socialize (if lonely)
    if (villagerNeeds.Morale < 50f)
    {
        // Find nearest friendly villager
        Entity nearestAlly = FindNearestAlly(sensors);
        if (nearestAlly != Entity.Null)
        {
            float socialUrgency = 1f - (villagerNeeds.Morale / 100f);

            options.Add(new AIUtilityOption
            {
                Action = ActionType.Socialize,
                Target = nearestAlly,
                Destination = GetPosition(nearestAlly),
                UtilityScore = socialUrgency * 0.7f, // Lower priority than survival
                ConfidenceLevel = 0.6f
            });
        }
    }
}
```

**Example: Carrier Action Evaluation** (Space4X)
```csharp
void EvaluateCarrierActions(
    ref AIAgent agent,
    DynamicBuffer<AISensorReading> sensors,
    DynamicBuffer<AIUtilityOption> options,
    LocalTransform transform)
{
    var carrier = GetComponent<Carrier>(agent.Entity);

    // Action: MineDeposit (if carrier has Mining role)
    if (carrier.ActiveRole == CarrierRole.Mining)
    {
        foreach (var sensor in sensors)
        {
            if (!IsDeposit(sensor.DetectedEntity))
                continue;

            var deposit = GetComponent<Deposit>(sensor.DetectedEntity);

            float richness = deposit.YieldRemaining / deposit.YieldMax;
            float distance = sensor.Distance;
            float distanceScore = 1f - math.clamp(distance / 500f, 0f, 1f);

            float utilityScore = richness * 0.7f + distanceScore * 0.3f;

            options.Add(new AIUtilityOption
            {
                Action = ActionType.MineDeposit,
                Target = sensor.DetectedEntity,
                Destination = sensor.Position,
                UtilityScore = utilityScore,
                ConfidenceLevel = 0.9f
            });
        }
    }

    // Action: CombatEngage (if hostile detected and carrier has weapons)
    if (carrier.ActiveRole == CarrierRole.Combat)
    {
        foreach (var sensor in sensors)
        {
            if (sensor.ThreatLevel < 0.3f)
                continue; // Not hostile enough

            var carrierStats = GetCarrierStats(agent.Entity);
            float combatPower = carrierStats.TotalDPS / 1000f; // Normalize
            float enemyPower = EstimateEnemyPower(sensor.DetectedEntity);

            float powerRatio = combatPower / math.max(enemyPower, 0.1f);
            float engageChance = math.clamp(powerRatio, 0f, 1f);

            if (engageChance < 0.3f)
            {
                // Too weak, flee instead
                options.Add(new AIUtilityOption
                {
                    Action = ActionType.Flee,
                    Target = sensor.DetectedEntity,
                    Destination = transform.Position - math.normalize(sensor.Position - transform.Position) * 200f,
                    UtilityScore = sensor.ThreatLevel,
                    ConfidenceLevel = 0.8f
                });
            }
            else
            {
                // Strong enough, engage
                options.Add(new AIUtilityOption
                {
                    Action = ActionType.CombatEngage,
                    Target = sensor.DetectedEntity,
                    Destination = sensor.Position,
                    UtilityScore = sensor.ThreatLevel * engageChance,
                    ConfidenceLevel = engageChance
                });
            }
        }
    }

    // Action: SurveySector (if Exploration role and in unsurveyed sector)
    if (carrier.ActiveRole == CarrierRole.Exploration)
    {
        var sectorVisibility = GetSectorVisibility(transform.Position);
        if (sectorVisibility == VisibilityLevel.Unknown || sectorVisibility == VisibilityLevel.Stale)
        {
            options.Add(new AIUtilityOption
            {
                Action = ActionType.SurveyS ector,
                Target = Entity.Null,
                Destination = transform.Position, // Survey current sector
                UtilityScore = 0.9f, // High priority for explorers
                ConfidenceLevel = 1.0f
            });
        }
    }
}
```

---

## 3. AITaskPlanningSystem

**Responsibility**: Convert selected utility option into executable task.

**Logic**:
```csharp
public partial struct AITaskPlanningSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (utilityBuffer, taskState, agent) in
                 SystemAPI.Query<DynamicBuffer<AIUtilityOption>, RefRW<AITaskState>, RefRO<AIAgent>>())
        {
            if (utilityBuffer.Length == 0)
                continue; // No action selected

            var selectedOption = utilityBuffer[0]; // Best action from evaluation system

            // Create task from selected option
            taskState.ValueRW.CurrentAction = selectedOption.Action;
            taskState.ValueRW.TargetEntity = selectedOption.Target;
            taskState.ValueRW.TargetPosition = selectedOption.Destination;
            taskState.ValueRW.Phase = TaskPhase.Planning;
            taskState.ValueRW.TicksInPhase = 0;
            taskState.ValueRW.ProgressPercent = 0f;

            // Validate task is possible (resources available, target still exists, etc.)
            if (!ValidateTask(taskState.ValueRO, agent.ValueRO))
            {
                taskState.ValueRW.Phase = TaskPhase.Failed;
                continue;
            }

            // Transition to next phase
            taskState.ValueRW.Phase = GetNextPhase(selectedOption.Action);
        }
    }
}
```

**Phase Determination**:
```csharp
TaskPhase GetNextPhase(ActionType action)
{
    switch (action)
    {
        case ActionType.GatherResource:
        case ActionType.MineDeposit:
        case ActionType.CombatEngage:
            return TaskPhase.Traveling; // Must move to target first

        case ActionType.Rest:
        case ActionType.Idle:
            return TaskPhase.Executing; // Can execute immediately

        case ActionType.Flee:
            return TaskPhase.Executing; // Start fleeing immediately

        default:
            return TaskPhase.Preparing; // Default: prepare before acting
    }
}
```

---

## 4. AISteeringSystem

**Responsibility**: Calculate movement toward task target.

**Logic**:
```csharp
[BurstCompile]
public partial struct AISteeringSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.GetSingleton<TimeState>().FixedDeltaTime;

        foreach (var (steering, taskState, transform) in
                 SystemAPI.Query<RefRW<AISteeringState>, RefRO<AITaskState>, RefRW<LocalTransform>>())
        {
            if (taskState.ValueRO.Phase != TaskPhase.Traveling)
                continue; // Not moving

            float3 targetPos = taskState.ValueRO.TargetPosition;
            float3 currentPos = transform.ValueRO.Position;

            float distance = math.distance(currentPos, targetPos);

            // Reached destination
            if (distance < 1f)
            {
                steering.ValueRW.DesiredVelocity = float3.zero;
                continue;
            }

            // Calculate desired velocity
            float3 direction = math.normalize(targetPos - currentPos);
            steering.ValueRW.DesiredVelocity = direction * steering.ValueRO.MaxSpeed;

            // Apply steering (blend current velocity toward desired)
            float3 steeringForce = steering.ValueRO.DesiredVelocity - steering.ValueRO.Velocity;
            steeringForce = math.clamp(steeringForce, -steering.ValueRO.MaxAcceleration, steering.ValueRO.MaxAcceleration);

            steering.ValueRW.Velocity += steeringForce * deltaTime;
            steering.ValueRW.Velocity = math.clamp(steering.ValueRW.Velocity, -steering.ValueRO.MaxSpeed, steering.ValueRO.MaxSpeed);

            // Update position
            transform.ValueRW.Position += steering.ValueRO.Velocity * deltaTime;

            // Update rotation to face movement direction
            if (math.lengthsq(steering.ValueRO.Velocity) > 0.01f)
            {
                float3 forward = math.normalize(steering.ValueRO.Velocity);
                quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());
                transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, targetRotation, steering.ValueRO.RotationSpeed * deltaTime);
            }
        }
    }
}
```

**Flocking Behavior** (for groups like bands/fleets):
```csharp
float3 CalculateFlockingSteering(Entity agent, DynamicBuffer<AISensorReading> sensors)
{
    float3 cohesion = float3.zero;    // Move toward group center
    float3 separation = float3.zero;  // Avoid crowding neighbors
    float3 alignment = float3.zero;   // Match group velocity

    int neighborCount = 0;

    foreach (var sensor in sensors)
    {
        if (sensor.Type != SensorType.Vision || sensor.Distance > 20f)
            continue;

        if (!IsSameGroup(agent, sensor.DetectedEntity))
            continue;

        neighborCount++;

        // Cohesion: average position
        cohesion += sensor.Position;

        // Separation: push away from close neighbors
        if (sensor.Distance < 5f)
        {
            float3 away = GetPosition(agent) - sensor.Position;
            separation += math.normalize(away) / sensor.Distance; // Closer = stronger push
        }

        // Alignment: average velocity
        var neighborVelocity = GetComponent<AISteeringState>(sensor.DetectedEntity).Velocity;
        alignment += neighborVelocity;
    }

    if (neighborCount == 0)
        return float3.zero;

    cohesion /= neighborCount;
    cohesion = math.normalize(cohesion - GetPosition(agent)); // Direction to center

    alignment /= neighborCount;
    alignment = math.normalize(alignment); // Average direction

    // Weighted combination
    return cohesion * 0.3f + separation * 0.5f + alignment * 0.2f;
}
```

---

## 5. AITaskExecutionSystem

**Responsibility**: Perform task-specific logic (gather, attack, etc.).

**Logic**:
```csharp
public partial struct AITaskExecutionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (taskState, agent, transform) in
                 SystemAPI.Query<RefRW<AITaskState>, RefRO<AIAgent>, RefRO<LocalTransform>>())
        {
            if (taskState.ValueRO.Phase != TaskPhase.Executing)
                continue;

            taskState.ValueRW.TicksInPhase++;

            switch (taskState.ValueRO.CurrentAction)
            {
                case ActionType.GatherResource:
                    ExecuteGatherResource(ref taskState.ValueRW, agent.ValueRO, transform.ValueRO);
                    break;

                case ActionType.AttackEnemy:
                    ExecuteAttackEnemy(ref taskState.ValueRW, agent.ValueRO, transform.ValueRO);
                    break;

                case ActionType.Rest:
                    ExecuteRest(ref taskState.ValueRW, agent.ValueRO);
                    break;

                case ActionType.MineDeposit:
                    ExecuteMineDeposit(ref taskState.ValueRW, agent.ValueRO, transform.ValueRO);
                    break;

                // ... other actions
            }
        }
    }
}
```

**Example: Execute Gather Resource**
```csharp
void ExecuteGatherResource(ref AITaskState task, AIAgent agent, LocalTransform transform)
{
    // Check if target still exists
    if (task.TargetEntity == Entity.Null || !Exists(task.TargetEntity))
    {
        task.Phase = TaskPhase.Failed;
        return;
    }

    // Check distance to target
    var targetPos = GetPosition(task.TargetEntity);
    if (math.distance(transform.Position, targetPos) > 2f)
    {
        // Not close enough, transition back to Traveling
        task.Phase = TaskPhase.Traveling;
        return;
    }

    // Gather resource over time (e.g., 100 ticks)
    const ushort gatherDuration = 100;
    task.ProgressPercent = (float)task.TicksInPhase / gatherDuration;

    if (task.TicksInPhase >= gatherDuration)
    {
        // Gathering complete, add resource to inventory
        var villagerJob = GetComponent<VillagerJobTicket>(agent.Entity);
        // ... add resource to carry buffer ...

        task.Phase = TaskPhase.Completing;
    }
}
```

**Example: Execute Attack Enemy**
```csharp
void ExecuteAttackEnemy(ref AITaskState task, AIAgent agent, LocalTransform transform)
{
    if (task.TargetEntity == Entity.Null || !Exists(task.TargetEntity))
    {
        task.Phase = TaskPhase.Completed;
        return; // Enemy destroyed or fled
    }

    var targetPos = GetPosition(task.TargetEntity);
    float distance = math.distance(transform.Position, targetPos);

    // Get attack range from combat component
    var combat = GetComponent<CombatCapability>(agent.Entity);

    if (distance > combat.AttackRange)
    {
        // Too far, chase
        task.Phase = TaskPhase.Traveling;
        task.TargetPosition = targetPos; // Update destination
        return;
    }

    // Attack every N ticks (attack speed)
    if (task.TicksInPhase % combat.AttackSpeed == 0)
    {
        // Deal damage to target
        var targetHealth = GetComponentRW<Health>(task.TargetEntity);
        targetHealth.ValueRW.Current -= combat.Damage;

        if (targetHealth.ValueRO.Current <= 0)
        {
            // Enemy defeated
            task.Phase = TaskPhase.Completing;
        }
    }
}
```

---

## Behavior Profiles (Data-Driven Configuration)

AI behaviors are configured via ScriptableObject profiles.

```csharp
// ScriptableObject asset
[CreateAssetMenu(fileName = "NewAIBehaviorProfile", menuName = "PureDOTS/AI/Behavior Profile")]
public class AIBehaviorProfile : ScriptableObject
{
    public AIArchetype Archetype;
    public ushort ThinkInterval = 20; // Ticks between decisions

    [Header("Sensors")]
    public float VisionRange = 30f;
    public float VisionAngle = 120f;
    public float HearingRange = 50f;
    public SensorUpdateRate SensorRate = SensorUpdateRate.Normal;

    [Header("Steering")]
    public float MaxSpeed = 5f;
    public float MaxAcceleration = 2f;
    public float RotationSpeed = 3f;

    [Header("Utility Curves")]
    public List<UtilityCurveData> UtilityCurves;

    [Header("Aggression")]
    public float AggressionLevel = 0.5f;      // 0.0 = pacifist, 1.0 = always attack
    public float FleeThreshold = 0.3f;        // Threat level to trigger flee
}

[Serializable]
public struct UtilityCurveData
{
    public ActionType Action;
    public AnimationCurve Curve; // Unity AnimationCurve for visual editing
    public float Multiplier;
}
```

**Baker** converts profile to blob:
```csharp
public class AIBehaviorProfileAuthoring : MonoBehaviour
{
    public AIBehaviorProfile Profile;

    class Baker : Baker<AIBehaviorProfileAuthoring>
    {
        public override void Bake(AIBehaviorProfileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new AIAgent
            {
                Archetype = authoring.Profile.Archetype,
                Mode = AIBehaviorMode.Idle,
                ThinkInterval = authoring.Profile.ThinkInterval
            });

            AddComponent(entity, new AISensorConfig
            {
                VisionRange = authoring.Profile.VisionRange,
                VisionAngle = authoring.Profile.VisionAngle,
                HearingRange = authoring.Profile.HearingRange,
                UpdateRate = authoring.Profile.SensorRate
            });

            AddComponent(entity, new AISteeringState
            {
                MaxSpeed = authoring.Profile.MaxSpeed,
                MaxAcceleration = authoring.Profile.MaxAcceleration,
                RotationSpeed = authoring.Profile.RotationSpeed
            });

            // Add buffers
            AddBuffer<AISensorReading>(entity);
            AddBuffer<AIUtilityOption>(entity);
            AddBuffer<AIPathWaypoint>(entity);

            // Bake utility curves to blob (omitted for brevity)
        }
    }
}
```

---

## Aggregate AI Decision-Making

Aggregates (bands, fleets, villages) use the same framework but with different considerations.

**Aggregate Utility Example** (Band combat decision):
```csharp
void EvaluateBandActions(
    ref AIAgent agent,
    DynamicBuffer<AISensorReading> sensors,
    DynamicBuffer<AIUtilityOption> options,
    LocalTransform transform)
{
    var band = GetComponent<Band>(agent.Entity);
    var bandMembers = GetBuffer<BandMemberEntry>(agent.Entity);

    // Action: CoordinateAttack (if enemy detected)
    foreach (var sensor in sensors)
    {
        if (sensor.ThreatLevel < 0.4f)
            continue;

        // Calculate band combat power vs. enemy
        float bandPower = CalculateBandCombatPower(bandMembers);
        float enemyPower = EstimateEnemyPower(sensor.DetectedEntity);

        float powerRatio = bandPower / math.max(enemyPower, 0.1f);

        if (powerRatio > 1.5f)
        {
            // Overwhelming advantage, attack
            options.Add(new AIUtilityOption
            {
                Action = ActionType.CoordinateAttack,
                Target = sensor.DetectedEntity,
                Destination = sensor.Position,
                UtilityScore = math.min(powerRatio / 2f, 1f),
                ConfidenceLevel = 0.9f
            });
        }
        else if (powerRatio < 0.5f)
        {
            // Outnumbered, retreat
            options.Add(new AIUtilityOption
            {
                Action = ActionType.Retreat,
                Target = sensor.DetectedEntity,
                Destination = CalculateRetreatPosition(transform.Position, sensor.Position),
                UtilityScore = (1f - powerRatio) * 0.8f,
                ConfidenceLevel = 0.95f
            });
        }
    }

    // Action: SplitGroup (if band too large, low cohesion)
    if (bandMembers.Length > 20 && band.Cohesion < 0.4f)
    {
        options.Add(new AIUtilityOption
        {
            Action = ActionType.SplitGroup,
            Target = Entity.Null,
            Destination = transform.Position,
            UtilityScore = 1f - band.Cohesion, // Low cohesion = high split urgency
            ConfidenceLevel = 0.7f
        });
    }
}
```

---

## Rewind Integration

AI state must save/restore for rewind compatibility.

```csharp
public struct AIHistorySample : IBufferElementData
{
    public ushort Tick;
    public AIBehaviorMode Mode;
    public TaskPhase TaskPhase;
    public ActionType CurrentAction;
    public Entity TargetEntity;
    public float3 TargetPosition;
}
```

**Recording**:
```csharp
var history = GetBuffer<AIHistorySample>(agent);
history.Add(new AIHistorySample
{
    Tick = timeState.CurrentTick,
    Mode = aiAgent.Mode,
    TaskPhase = taskState.Phase,
    CurrentAction = taskState.CurrentAction,
    TargetEntity = taskState.TargetEntity,
    TargetPosition = taskState.TargetPosition
});
```

**Playback**:
```csharp
var sample = FindSampleForTick(history, rewindState.PlaybackTick);
aiAgent.Mode = sample.Mode;
taskState.Phase = sample.TaskPhase;
taskState.CurrentAction = sample.CurrentAction;
taskState.TargetEntity = sample.TargetEntity;
taskState.TargetPosition = sample.TargetPosition;
```

---

## Open Questions / Design Decisions Needed

1. **Sensor LOD (Level of Detail)**: Should distant entities use lower-fidelity sensors (save performance)?
   - *Suggestion*: Yes - entities >100 units away update at Slow rate, <20 units at Fast rate

2. **Utility Curve Authoring**: AnimationCurve in Unity editor OR code-defined curves?
   - *Suggestion*: AnimationCurve for designer friendliness, baked to blob for runtime

3. **Task Interruption**: Can higher-priority task interrupt current task?
   - *Suggestion*: Yes - if new utility score >1.5x current task score, abort and switch

4. **Path Caching**: Should pathfinding results be cached for repeated queries?
   - *Suggestion*: Yes - cache path for 100 ticks, revalidate if target moves >10 units

5. **Group Coordination**: How do band/fleet members synchronize (all attack same target)?
   - *Suggestion*: Aggregate entity broadcasts "focus target" to members, individual AI respects it

6. **Fleeing Behavior**: Should fleeing entities path around obstacles or straight-line away?
   - *Suggestion*: Straight-line for performance, add obstacle avoidance if collision imminent

7. **Resting Duration**: Fixed duration OR until needs restored?
   - *Suggestion*: Until needs restored (energy >80%), with max cap of 200 ticks

8. **Aggression Inheritance**: Do aggregate entities inherit aggression from members OR have independent value?
   - *Suggestion*: Average of members, updated when composition changes

---

## Implementation Notes

- **AISystemGroup** = custom system group, runs after input, before presentation
- **AISensorUpdateSystem** = spatial queries, populates sensor buffers
- **AIUtilityEvaluationSystem** = scores actions, selects best
- **AITaskPlanningSystem** = creates tasks from selected actions
- **AISteeringSystem** = movement physics
- **AITaskExecutionSystem** = action-specific logic
- **AIStateCleanupSystem** = removes completed tasks
- All systems respect `RewindState.Mode` (skip during Playback)

---

## References

- **Villager Jobs**: [VillagerJobs_DOTS.md](../DesignNotes/VillagerJobs_DOTS.md) - Job assignment integration
- **Carrier Architecture**: [CarrierArchitecture.md](CarrierArchitecture.md) - Carrier role AI decisions
- **Combat Loop**: [CombatLoop.md](CombatLoop.md) - Combat AI integration
- **Alignment System**: Alignment affects threat calculation
- **Spatial Grid**: Sensor queries use spatial partitioning
- **Registry System**: Registry sensor type for perfect knowledge
