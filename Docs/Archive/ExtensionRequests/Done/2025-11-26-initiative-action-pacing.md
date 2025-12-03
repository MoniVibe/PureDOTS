# Extension Request: Initiative / Action Pacing System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need action pacing that determines how quickly entities act:
- **Space4X**: Ship action order in combat, crew reaction times
- **Godgame**: Villager work speed, combat turn order, response times

Initiative affects who acts first, how often they can act, and urgency of responses.

---

## Proposed Solution

**Extension Type**: New Components + System

### Components (`Packages/com.moni.puredots/Runtime/Runtime/AI/`)

```csharp
public struct EntityInitiative : IComponentData
{
    public float BaseInitiative;      // Innate speed (40-120, 100 = average)
    public float CurrentInitiative;   // After modifiers
    public float ActionCooldown;      // Time until next action allowed
    public float LastActionTime;      // When entity last acted
    public byte Urgency;              // 0-100, boosts priority when high
}

public struct InitiativeConfig : IComponentData
{
    public float BaseActionInterval;   // Default time between actions
    public float UrgencyBoostMax;      // Max speed boost from urgency
    public float MinActionInterval;    // Fastest possible action rate
}
```

### System

```csharp
// InitiativeSystem - Updates cooldowns, determines action readiness
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct InitiativeSystem : ISystem { }

public static class InitiativeHelpers
{
    public static bool CanAct(in EntityInitiative init, float currentTime);
    public static float GetActionInterval(in EntityInitiative init, in InitiativeConfig config);
    public static int CompareInitiative(in EntityInitiative a, in EntityInitiative b); // For sorting
}
```

---

## Example Usage

```csharp
// Check if entity can act this frame
var init = EntityManager.GetComponentData<EntityInitiative>(entity);
if (InitiativeHelpers.CanAct(init, currentTime))
{
    // Perform action
    init.LastActionTime = currentTime;
    EntityManager.SetComponentData(entity, init);
}

// Sort combatants by initiative for turn order
combatants.Sort((a, b) => InitiativeHelpers.CompareInitiative(
    EntityManager.GetComponentData<EntityInitiative>(a),
    EntityManager.GetComponentData<EntityInitiative>(b)));
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/AI/`
- `InitiativeComponents.cs`
- `InitiativeSystem.cs`

---

## Review Notes

*(PureDOTS team use)*

