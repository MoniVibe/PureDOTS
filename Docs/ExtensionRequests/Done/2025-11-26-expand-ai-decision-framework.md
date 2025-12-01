# Extension Request: Expand AI - GOAP/Utility Decision Framework

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P1  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/AI/GOAP/GOAPComponents.cs` - AIGoal, AIAction, AIPlanner, UtilityOption, UtilityConfig, AIDirective, AISubordinate, AICommander, AIState, WorldStateFact, PlannedAction
- `Packages/com.moni.puredots/Runtime/Runtime/AI/GOAP/GOAPHelpers.cs` - Static helpers for GOAP planning and utility scoring
- `Packages/com.moni.puredots/Runtime/Systems/AI/GOAP/GOAPPlannerSystem.cs` - GOAPPlannerSystem, UtilityEvaluationSystem, DirectiveSystem

---

## Use Case

Current PureDOTS has basic state machines. Expand to support:

**Both Games:**
- **GOAP (Goal-Oriented Action Planning)**: Entities plan sequences to achieve goals
- **Utility AI**: Score multiple options, pick highest utility
- **Hierarchical decisions**: Village AI directs villager AI

**Godgame:**
- Villagers autonomously decide: work, eat, socialize, flee
- Villages allocate labor, request buildings, manage defense
- Bands choose formations, targets, retreat conditions

**Space4X:**
- Crew decide duties based on skills and needs
- Fleets plan routes, engagements, retreats
- Colonies prioritize construction, research, defense

---

## Proposed Expansion

### Requested Components

```csharp
// === GOAP ===
public struct AIGoal : IBufferElementData
{
    public FixedString32Bytes GoalId;     // "satisfy_hunger", "defend_village"
    public float Priority;                 // 0-100, higher = more urgent
    public float Insistence;               // How much it grows over time
    public bool IsActive;                  // Currently pursuing
    public Entity TargetEntity;            // Optional goal target
}

public struct AIAction : IBufferElementData
{
    public FixedString32Bytes ActionId;   // "eat_food", "attack_enemy"
    public FixedString32Bytes GoalId;     // Which goal this satisfies
    public float Cost;                     // Time/resource cost
    public float Utility;                  // How well it satisfies goal
    
    // Preconditions (simplified)
    public bool RequiresTarget;
    public bool RequiresResource;
    public FixedString32Bytes RequiredState;
}

public struct AIPlanner : IComponentData
{
    public FixedString32Bytes CurrentGoal;
    public FixedString32Bytes CurrentAction;
    public float PlanConfidence;           // 0-1, replan if low
    public uint PlanCreatedTick;
    public uint ReplanInterval;            // Ticks between replans
}

// === Utility AI ===
public struct UtilityOption : IBufferElementData
{
    public FixedString32Bytes OptionId;
    public float BaseScore;                // Inherent value
    public float NeedScore;                // From unmet needs
    public float OpportunityScore;         // From environment
    public float FinalScore;               // Combined
    public Entity TargetEntity;
}

public struct UtilityConfig : IComponentData
{
    public float NeedWeight;               // How much needs affect choice
    public float OpportunityWeight;        // How much environment affects
    public float RandomnessWeight;         // Prevent predictability
    public uint EvaluationInterval;        // Ticks between evaluations
}

// === Hierarchical AI ===
public struct AIDirective : IBufferElementData
{
    public Entity DirectingEntity;         // Village, fleet commander
    public FixedString32Bytes DirectiveType; // "gather_wood", "defend_point"
    public float Priority;
    public uint IssuedTick;
    public uint ExpiryTick;
}

public struct AISubordinate : IComponentData
{
    public Entity Commander;               // Who directs this entity
    public float Compliance;               // 0-1, how well they follow orders
    public bool HasStandingOrders;
}
```

### New Systems
- `GOAPPlannerSystem` - Creates action sequences for goals
- `UtilityEvaluationSystem` - Scores options, selects best
- `DirectiveSystem` - Propagates orders from commanders
- `AIDecisionIntegration` - Combines GOAP + Utility + Directives

---

## Example Usage

```csharp
// === Villager GOAP: Hungry villager finds food ===
// System adds goal:
goals.Add(new AIGoal { GoalId = "satisfy_hunger", Priority = 80 });

// Planner evaluates actions:
// - "eat_from_inventory" (utility 90, requires food in inventory)
// - "go_to_storehouse" (utility 70, always available)
// - "forage" (utility 50, requires being outside)

// Planner selects sequence: go_to_storehouse → take_food → eat

// === Fleet Utility: Choose engagement ===
var options = EntityManager.GetBuffer<UtilityOption>(fleetEntity);
// System scores:
// - "attack_enemy_fleet" (score 75, opportunity: weak enemy)
// - "retreat_to_base" (score 40, need: repairs)
// - "patrol_sector" (score 55, directive: patrol orders)
// Fleet attacks weak enemy

// === Village Directive: Labor allocation ===
// Village AI issues:
EntityManager.GetBuffer<AIDirective>(villagerEntity).Add(new AIDirective {
    DirectingEntity = villageEntity,
    DirectiveType = "gather_wood",
    Priority = 60
});
// Villager's planner incorporates directive into goal priorities
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/` directory
- Integration: Existing villager/crew state machines

**Breaking Changes:**
- Additive - existing state machines still work
- Games can adopt GOAP/Utility incrementally

---

## Review Notes

*(PureDOTS team use)*

