# Extension Request: Daily Routine / Activity Phases System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need time-based activity scheduling:
- **Space4X**: Shift rotations, maintenance windows, crew rest cycles
- **Godgame**: Villager work/rest/leisure cycles, sunrise/sunset routines

Routines partition the day into phases (Dawn, Morning, Noon, Afternoon, Dusk, Evening, Night, Midnight) and assign activities to each phase.

---

## Proposed Solution

**Extension Type**: New Components + System

### Components (`Packages/com.moni.puredots/Runtime/Runtime/AI/`)

```csharp
public enum DayPhase : byte
{
    Dawn = 0,        // 5:00 - 7:00
    Morning = 1,     // 7:00 - 11:00
    Noon = 2,        // 11:00 - 13:00
    Afternoon = 3,   // 13:00 - 17:00
    Dusk = 4,        // 17:00 - 19:00
    Evening = 5,     // 19:00 - 22:00
    Night = 6,       // 22:00 - 2:00
    Midnight = 7     // 2:00 - 5:00
}

public enum RoutineActivity : byte
{
    Sleep = 0,
    Wake = 1,
    Work = 2,
    Eat = 3,
    Leisure = 4,
    Socialize = 5,
    Worship = 6,
    Train = 7,
    Patrol = 8,
    Rest = 9
}

public struct EntityRoutine : IComponentData
{
    public DayPhase CurrentPhase;
    public RoutineActivity CurrentActivity;
    public RoutineActivity ScheduledActivity;  // What should be doing this phase
    public float PhaseStartTime;
    public bool IsInterrupted;                 // Doing something else
}

// Per-phase schedule (e.g., "Work at Dawn, Eat at Noon, Sleep at Night")
[InternalBufferCapacity(8)]
public struct RoutineSchedule : IBufferElementData
{
    public DayPhase Phase;
    public RoutineActivity Activity;
    public byte Priority;  // Higher = harder to interrupt
}

public struct RoutineConfig : IComponentData
{
    public float DawnHour;      // When dawn starts (5.0)
    public float DuskHour;      // When dusk starts (17.0)
    public float DayLength;     // Seconds per game day
}
```

### System

```csharp
// DailyRoutineSystem - Transitions phases, updates scheduled activities
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DailyRoutineSystem : ISystem { }

public static class RoutineHelpers
{
    public static DayPhase GetPhaseForTime(float hourOfDay, in RoutineConfig config);
    public static bool ShouldTransition(in EntityRoutine routine, float currentTime);
}
```

---

## Example Usage

```csharp
// Check if entity should be working
var routine = EntityManager.GetComponentData<EntityRoutine>(villagerEntity);
if (routine.CurrentActivity == RoutineActivity.Work && !routine.IsInterrupted)
{
    // Assign job task
}

// Interrupt routine for emergency
routine.IsInterrupted = true;
routine.CurrentActivity = RoutineActivity.Flee;
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/AI/`
- `DailyRoutineComponents.cs`
- `DailyRoutineSystem.cs`

---

## Review Notes

*(PureDOTS team use)*

