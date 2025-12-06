# Cognitive Stats Integration Guide

**Last Updated**: 2025-01-27  
**Purpose**: Guide for integrating and using Wisdom/Intelligence stats in procedural learning systems

---

## Overview

The `CognitiveStats` component regulates procedural learning speed, reasoning depth, memory retention, and behavioral flexibility. It acts as a scalar modifier for cognitive and limbic systems, enabling agents with different cognitive capabilities.

### Core Stats

| Stat | Range | Function | Dominant Layer |
|------|-------|----------|----------------|
| **Intelligence** | 0-10 | Computational efficiency, problem-solving rate | Cognitive (planning, hypothesis testing) |
| **Wisdom** | 0-10 | Integrative, experience-based reasoning | Limbic/Emotional, Social/Observational |
| **Curiosity** | 0-10 | Exploration weight in procedural learning | Exploration/Discovery |
| **Focus** | 0-10 | Cognitive stamina (decays during reasoning) | Planning gates |

---

## Component Usage

### Adding CognitiveStats to Entities

**Via Authoring:**
```csharp
// Add CognitiveStatsAuthoring component in Unity Inspector
// Values are set in inspector (0-10 range)
```

**Via Code:**
```csharp
// Create default stats (all 5.0, Focus 10.0)
var stats = CognitiveStats.CreateDefaults();

// Or customize
var stats = new CognitiveStats
{
    Intelligence = 8.0f,
    Wisdom = 6.0f,
    Curiosity = 7.0f,
    Focus = 10.0f,
    MaxFocus = 10.0f,
    LastFocusDecayTick = 0
};

entityManager.AddComponent(entity, stats);
```

### Normalizing Stats for Formulas

All formulas use normalized 0-1 range. Use `CognitiveStats.Normalize()`:

```csharp
float intNorm = CognitiveStats.Normalize(stats.Intelligence); // 8.0 → 0.8
float wisNorm = CognitiveStats.Normalize(stats.Wisdom);     // 6.0 → 0.6
```

Or use the convenience properties:
```csharp
float intNorm = stats.IntelligenceNormalized;
float wisNorm = stats.WisdomNormalized;
```

---

## System Integration Points

### 1. Procedural Memory Reinforcement

**System**: `ProceduralMemoryReinforcementSystem`  
**Method**: `ReinforceAction()`

Apply Intelligence/Wisdom multipliers when reinforcing learned actions:

```csharp
// When action outcome is known
float successResult = actionSucceeded ? 1.0f : 0.0f;
float baseLearningRate = 0.1f; // Your base rate

ProceduralMemoryReinforcementSystem.ReinforceAction(
    ref memory,
    contextHash,
    actionId,
    successResult,
    baseLearningRate,
    in cognitiveStats  // Pass CognitiveStats
);

// Formula applied internally:
// effectiveRate = baseRate * (0.6 * Intelligence + 0.4 * Wisdom) * (1 + Curiosity * 0.5)
```

**Legacy Compatibility:**
```csharp
// Old signature still works (uses default stats)
ReinforceAction(ref memory, contextHash, actionId, successResult, learningRate);
```

---

### 2. Memory Decay

**System**: `ProceduralMemoryDecaySystem`  
**Runs**: Automatically in `LearningSystemGroup`

Applies Wisdom-based decay to memory scores:
- Formula: `memory.Value *= 1 - (0.01f / Wisdom)`
- Higher Wisdom = slower decay (better retention)
- Runs at 1Hz

**No manual integration needed** - system queries entities with both `ProceduralMemory` and `CognitiveStats`.

---

### 3. Exploration Probability

**System**: `ProceduralLearningSystem`  
**Integration**: Automatic (uses CognitiveStats if present)

Exploration probability is calculated as:
```csharp
float explorationChance = 0.1f + Curiosity * (1 - Focus / MaxFocus);
```

**Usage in Custom Systems:**
```csharp
if (CognitiveStatsLookup.HasComponent(entity))
{
    var stats = CognitiveStatsLookup[entity];
    float curiosityNorm = CognitiveStats.Normalize(stats.Curiosity);
    float focusRatio = stats.MaxFocus > 0f ? stats.Focus / stats.MaxFocus : 1f;
    float explorationChance = 0.1f + curiosityNorm * (1f - focusRatio);
    
    // Use explorationChance for decision-making
}
```

---

### 4. Focus Fatigue & Planning Gates

**System**: `FocusFatigueSystem`  
**Runs**: Automatically in `LearningSystemGroup`

**Check if agent can perform heavy planning:**
```csharp
if (FocusFatigueSystem.CanPerformHeavyPlanning(in cognitiveStats))
{
    // Perform expensive planning operations
    EvaluateDeepPlans();
}
else
{
    // Use cached/simplified plans
    UseCachedPlans();
}
```

**Focus Behavior:**
- Decays during active reasoning: `Focus -= DecayRate * DeltaTime`
- Regenerates when idle: `Focus += RegenRate * DeltaTime`
- Threshold: 2.0 (below this, planning is gated)

---

### 5. Affordance Discovery

**System**: `AffordanceDiscoverySystem`  
**Utility**: `CalculateObjectsToScan()`

Scale object scanning based on Intelligence:

```csharp
int objectsToScan = AffordanceDiscoverySystem.CalculateObjectsToScan(
    baseScanRate: 5.0f,
    deltaTime: timeState.FixedDeltaTime,
    in cognitiveStats
);

// Formula: ObjectsScanned = BaseScan * (0.5 + Intelligence)
// Higher Intelligence = more objects evaluated per cycle
```

**In Custom Affordance Systems:**
```csharp
float intNorm = CognitiveStats.Normalize(cognitiveStats.Intelligence);
float effectiveScanRate = baseScanRate * (0.5f + intNorm);
int objectsToScan = (int)math.ceil(effectiveScanRate * deltaTime);
```

---

### 6. Emotional Bias Damping

**System**: `EmotionalBiasSystem`  
**Runs**: Automatically in `MotivationSystemGroup`

**Apply Wisdom-based damping to emotional influence:**

```csharp
// Calculate damping factor
float dampingFactor = EmotionalBiasSystem.CalculateEmotionalDamping(in cognitiveStats);
// Formula: dampingFactor = 1 - Wisdom * 0.05f

// Apply to utility scores
float utilityScore = CalculateBaseUtility();
float emotionBias = GetEmotionBias();
float finalUtility = EmotionalBiasSystem.ApplyEmotionalBiasDamping(
    utilityScore,
    emotionBias,
    in cognitiveStats
);
```

**Manual Application:**
```csharp
float wisdomNorm = CognitiveStats.Normalize(cognitiveStats.Wisdom);
float dampingFactor = math.max(0f, 1f - wisdomNorm * 0.05f);
float dampedEmotionBias = emotionBias * dampingFactor;
float finalUtility = baseUtility * (1f + dampedEmotionBias);
```

---

## Query Patterns

### Optional CognitiveStats

Most systems make CognitiveStats optional. Use ComponentLookup:

```csharp
[BurstCompile]
private partial struct MyJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<CognitiveStats> CognitiveStatsLookup;
    
    public void Execute(Entity entity, ref MyComponent component)
    {
        // Use defaults if not present
        CognitiveStats stats = CognitiveStatsLookup.HasComponent(entity)
            ? CognitiveStatsLookup[entity]
            : CognitiveStats.CreateDefaults();
        
        // Use stats in calculations
        float intNorm = CognitiveStats.Normalize(stats.Intelligence);
        // ...
    }
}
```

### Required CognitiveStats

For systems that require CognitiveStats, add it to the query:

```csharp
var query = SystemAPI.QueryBuilder()
    .WithAll<ProceduralMemory, CognitiveStats>()
    .Build();
```

---

## Formula Reference

### Learning Rate Multiplier
```
EffectiveLearningRate = BaseRate * (0.6 * Intelligence + 0.4 * Wisdom) * (1 + Curiosity * 0.5)
```

### Memory Decay
```
memory.Value *= 1 - (0.01f / Wisdom)
```

### Exploration Probability
```
ExplorationChance = 0.1f + Curiosity * (1 - Focus / MaxFocus)
```

### Affordance Scan Rate
```
ObjectsScanned = BaseScan * (0.5 + Intelligence)
```

### Emotional Bias Damping
```
EmotionInfluence *= 1 - Wisdom * 0.05f
```

### Action Optimization Error Margin
```
errorMargin *= 1 / (1 + Intelligence)
```

---

## Example: Custom Learning System

```csharp
[BurstCompile]
[UpdateInGroup(typeof(LearningSystemGroup))]
public partial struct CustomLearningSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var cognitiveStatsLookup = state.GetComponentLookup<CognitiveStats>(true);
        cognitiveStatsLookup.Update(ref state);
        
        var job = new CustomLearningJob
        {
            CognitiveStatsLookup = cognitiveStatsLookup,
            CurrentTick = SystemAPI.GetSingleton<TickTimeState>().Tick
        };
        
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
    
    [BurstCompile]
    private partial struct CustomLearningJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<CognitiveStats> CognitiveStatsLookup;
        public uint CurrentTick;
        
        public void Execute(Entity entity, ref MyMemory memory)
        {
            // Get stats (optional)
            CognitiveStats stats = CognitiveStatsLookup.HasComponent(entity)
                ? CognitiveStatsLookup[entity]
                : CognitiveStats.CreateDefaults();
            
            // Check focus for heavy operations
            if (!FocusFatigueSystem.CanPerformHeavyPlanning(in stats))
            {
                return; // Skip expensive operations
            }
            
            // Use Intelligence for planning depth
            float intNorm = CognitiveStats.Normalize(stats.Intelligence);
            int plansToEvaluate = (int)(basePlans * intNorm);
            
            // Use Wisdom for bias correction
            float wisNorm = CognitiveStats.Normalize(stats.Wisdom);
            float biasReduction = wisNorm * 0.01f;
            memory.Bias *= (1f - biasReduction);
            
            // Use Curiosity for exploration
            float curNorm = CognitiveStats.Normalize(stats.Curiosity);
            if (ShouldExplore(curNorm))
            {
                ExploreNewOptions();
            }
        }
    }
}
```

---

## Performance Notes

- **Burst-Safe**: All formulas use multiplication-only, no branching per stat
- **Deterministic**: Stats are recomputed every N ticks (not per frame)
- **SIMD-Friendly**: All operations are numeric, no string operations
- **Default Values**: Entities without CognitiveStats use defaults (Intelligence=5.0, Wisdom=5.0, etc.)

---

## Related Systems

- `ProceduralLearningSystem` - Core learning loop (uses Curiosity/Focus)
- `ProceduralMemoryReinforcementSystem` - Action outcome reinforcement (uses Intelligence/Wisdom)
- `ProceduralMemoryDecaySystem` - Memory decay (uses Wisdom)
- `FocusFatigueSystem` - Cognitive stamina management
- `AffordanceDiscoverySystem` - Object scanning (uses Intelligence)
- `EmotionalBiasSystem` - Emotional influence damping (uses Wisdom)

---

## See Also

- `CognitiveStats.cs` - Component definition
- `CognitiveStatsAuthoring.cs` - Authoring component
- `CognitiveLearningFramework.md` - Overall cognitive system architecture

