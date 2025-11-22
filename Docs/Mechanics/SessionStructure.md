# Session Structure & Win/Loss Conditions (Both Games)

## Overview

This document defines the **session lifecycle**, **campaign vs. sandbox modes**, **victory conditions**, **failure states**, and **session progression** for both Space4X and Godgame. It establishes what constitutes a "playable game session" and when/how sessions end.

---

## Core Concept

**Sessions are player-authored game states with defined start/end conditions.**

Key principles:
- **Session = single playthrough** (start → mid-game → end)
- **Serialized game state** allows player-modified scenarios (custom starts)
- **Multiple session types** (campaign missions, sandbox, scenarios, challenges)
- **Victory conditions** are configurable (per session type)
- **Failure states** trigger game-over (optional restart)
- **Persistent progression** (optional, campaign unlocks)

---

## Session Types

Both games support multiple session types with different structure/goals.

```csharp
public enum SessionType : byte
{
    Sandbox,        // Open-ended, no win/loss conditions
    Campaign,       // Linear mission sequence with objectives
    Scenario,       // Single mission with specific victory condition
    Challenge,      // Timed/scored challenge mode
    Tutorial        // Guided learning experience
}
```

### 1. Sandbox Mode

**Space4X & Godgame**

**Structure**:
- **Start**: Player configures initial state (galaxy size, starting resources, faction alignment, difficulty)
- **Mid-Game**: Indefinite gameplay, no prescribed objectives
- **End**: Player-triggered (quit/save) OR optional soft goals (reach population X, explore Y% of map)

**Victory Conditions**: None (or optional player-defined goals)

**Failure Conditions**: None (cannot lose sandbox)

**Save/Load**: Full state persistence, multiple save slots

**Use Cases**:
- Creative play (build elaborate villages, design perfect fleet compositions)
- Testing/experimentation (mod testing, balance exploration)
- Relaxed play (no pressure, play at own pace)

---

### 2. Campaign Mode

**Space4X & Godgame**

**Structure**:
- **Start**: Mission briefing, predefined starting state (specific faction, limited resources, scenario setup)
- **Mid-Game**: Complete mission objectives (destroy enemy fleet, reach population 500, survive 1000 ticks)
- **End**: Victory (all objectives complete) OR Failure (objectives failed, faction destroyed)

**Victory Conditions** (per mission):
- **Primary Objectives**: Must complete to win (destroy enemy stronghold, colonize 3 planets)
- **Secondary Objectives**: Optional bonus (no casualties, complete within time limit)
- **Success Rewards**: Unlock next mission, unlock tech/units, narrative progression

**Failure Conditions**:
- Primary objective failed (e.g., village destroyed before timer expires)
- Critical asset lost (e.g., flagship carrier destroyed, capital village razed)
- Time limit exceeded (optional per mission)

**Progression**:
- **Linear Campaign**: Mission 1 → Mission 2 → Mission 3 (unlock sequence)
- **Branching Campaign**: Player choices unlock different missions (diplomacy → peace path, combat → war path)
- **Persistent Resources**: Optional carry-over (survive mission with resources → start next mission with bonus)

**Save/Load**: Mid-mission saves allowed, restart mission option

---

### 3. Scenario Mode

**Space4X & Godgame**

**Structure**:
- **Start**: Curated starting state (historical battle, specific challenge, community-created scenario)
- **Mid-Game**: Complete scenario objectives
- **End**: Victory OR Failure (same as campaign, but standalone)

**Victory Conditions** (per scenario):
- Custom-defined (designer specifies via scenario JSON)
- Examples:
  - "Defend village from 10 waves of attackers"
  - "Mine 5000 ore before enemy fleet arrives"
  - "Convert enemy village to your alignment without combat"

**Failure Conditions**:
- Custom-defined (scenario-specific)
- Always include "all your entities destroyed" as baseline failure

**Scenario File Format** (JSON):
```json
{
  "scenarioName": "Battle of the Crimson Nebula",
  "sessionType": "Scenario",
  "initialState": {
    "playerFaction": "Human Empire",
    "playerStartingFleet": [
      { "carrierType": "Battleship", "modules": [...], "crew": 1500 }
    ],
    "enemyFactions": [
      { "name": "Pirate Armada", "alignment": { "moral": -0.8, "order": -0.6, "purity": 0.2 } }
    ]
  },
  "victoryConditions": [
    { "type": "DestroyAllEnemies", "required": true },
    { "type": "NoCasualties", "required": false, "bonusScore": 1000 }
  ],
  "failureConditions": [
    { "type": "FlagshipDestroyed" },
    { "type": "TimeLimit", "ticks": 5000 }
  ],
  "timeLimit": 5000,
  "difficulty": "Hard"
}
```

**Save/Load**: Mid-scenario saves, retry scenario option

**Use Cases**:
- Historical recreations (famous space battles, legendary village sieges)
- Community challenges (leaderboard scoring, speedrun competitions)
- Narrative vignettes (self-contained story moments)

---

### 4. Challenge Mode

**Space4X & Godgame**

**Structure**:
- **Start**: Standardized starting state (same for all players)
- **Mid-Game**: Achieve highest score OR fastest completion
- **End**: Time limit expires OR objective complete

**Victory Conditions**:
- **Score-Based**: Accumulate points (destroy enemies = +100, build structures = +50, etc.)
- **Time-Based**: Complete objective in shortest time (speedrun)
- **Survival**: Last as long as possible (endless waves)

**Failure Conditions**:
- Time limit expires (if score challenge)
- Eliminated (if survival challenge)

**Leaderboards**: Global/friend leaderboards for highest scores/fastest times

**Examples**:
- **Space4X**: "Mine the most ore in 1000 ticks" (score = total ore)
- **Godgame**: "Survive endless goblin raids" (score = ticks survived)

**Save/Load**: No saves (challenge restarts from scratch)

---

### 5. Tutorial Mode

**Space4X & Godgame**

**Structure**:
- **Start**: Guided introduction, minimal starting state
- **Mid-Game**: Step-by-step instructions (build first storehouse, mine first deposit)
- **End**: Tutorial complete (all steps done)

**Victory Conditions**:
- Complete all tutorial steps (build X, gather Y, cast Z miracle)

**Failure Conditions**:
- None (tutorial cannot be failed, only restarted)

**Save/Load**: No saves (tutorial auto-progresses)

---

## Session Lifecycle

### Session States

```csharp
public enum SessionState : byte
{
    NotStarted,     // Session exists but not loaded
    Loading,        // Assets/state loading
    Running,        // Active gameplay
    Paused,         // Time stopped (player menu)
    Victory,        // Win condition met
    Defeat,         // Loss condition met
    Ended           // Session terminated (quit without save)
}
```

### Session Flow

```
NotStarted
  ↓
Loading (spawn entities, apply scenario state)
  ↓
Running
  ↔ Paused (toggle via player input)
  ↓
Victory / Defeat (condition evaluation)
  ↓
Ended (cleanup, return to menu)
```

### Session Components

```csharp
// Singleton tracking session state
public struct SessionState : IComponentData
{
    public SessionType Type;
    public SessionState State;
    public ushort CurrentTick;
    public ushort SessionDurationTicks;          // Total ticks elapsed
    public ushort TimeLimitTicks;                // 0 = no limit
    public float Score;                          // For challenge mode
}

// Buffer of victory conditions for this session
public struct VictoryCondition : IBufferElementData
{
    public VictoryType Type;
    public bool Required;                        // Primary (required) vs. Secondary (optional)
    public float TargetValue;                    // Threshold (e.g., population >= 500)
    public float CurrentValue;                   // Progress tracker
    public bool Completed;
}

public enum VictoryType : byte
{
    // Universal
    SurviveTime,        // Survive X ticks
    ReachPopulation,    // Population >= X
    AccumulateScore,    // Score >= X

    // Space4X
    DestroyAllEnemies,
    ColonizePlanets,    // N planets colonized
    ControlSectors,     // N sectors under control
    AccumulateWealth,   // Resources >= X
    ResearchTech,       // Unlock N techs

    // Godgame
    BuildStructures,    // N buildings constructed
    ConvertVillages,    // N enemy villages converted to alignment
    CastMiracles,       // N miracles cast
    ProtectEntity,      // Specific entity survives
    AchieveAlignment    // Reach specific alignment vector

    // Custom
    Custom              // Designer-defined via script
}

// Buffer of failure conditions
public struct FailureCondition : IBufferElementData
{
    public FailureType Type;
    public Entity CriticalEntity;                // Entity whose destruction causes failure
    public float ThresholdValue;
    public bool Triggered;
}

public enum FailureType : byte
{
    // Universal
    AllEntitiesDestroyed,   // Total wipeout
    TimeLimitExpired,       // Ran out of time
    CriticalEntityLost,     // Flagship/capital destroyed

    // Space4X
    EmpireCollapsed,        // All planets lost
    EconomicCollapse,       // Resources/income <= 0

    // Godgame
    VillageDestroyed,       // Specific village razed
    PopulationExtinct,      // All villagers dead
    MoraleCritical,         // Morale <= 0

    // Custom
    Custom                  // Designer-defined
}
```

---

## Victory Condition Evaluation

```csharp
[BurstCompile]
public partial struct SessionVictoryEvaluationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var session = SystemAPI.GetSingleton<SessionState>();

        if (session.State != SessionState.Running)
            return; // Only evaluate during active gameplay

        var victoryConditions = SystemAPI.GetSingletonBuffer<VictoryCondition>();
        bool allRequiredComplete = true;

        foreach (var condition in victoryConditions)
        {
            if (condition.Completed)
                continue;

            bool conditionMet = EvaluateVictoryCondition(condition);

            if (conditionMet)
            {
                condition.Completed = true;
            }

            if (condition.Required && !condition.Completed)
            {
                allRequiredComplete = false;
            }
        }

        if (allRequiredComplete)
        {
            // Victory achieved!
            session.State = SessionState.Victory;
            TriggerVictorySequence();
        }
    }
}
```

**Example Condition Evaluation**:
```csharp
bool EvaluateVictoryCondition(VictoryCondition condition)
{
    switch (condition.Type)
    {
        case VictoryType.ReachPopulation:
            var villagerRegistry = GetSingletonBuffer<VillagerRegistryEntry>();
            condition.CurrentValue = villagerRegistry.Length;
            return condition.CurrentValue >= condition.TargetValue;

        case VictoryType.DestroyAllEnemies:
            var enemyQuery = GetEntityQuery(ComponentType.ReadOnly<Enemy>());
            condition.CurrentValue = enemyQuery.CalculateEntityCount();
            return condition.CurrentValue == 0;

        case VictoryType.SurviveTime:
            var session = GetSingleton<SessionState>();
            condition.CurrentValue = session.SessionDurationTicks;
            return condition.CurrentValue >= condition.TargetValue;

        case VictoryType.AccumulateWealth:
            var resources = GetSingletonBuffer<StorehouseInventoryEntry>();
            float totalResources = 0f;
            foreach (var entry in resources)
                totalResources += entry.Amount;
            condition.CurrentValue = totalResources;
            return condition.CurrentValue >= condition.TargetValue;

        default:
            return false;
    }
}
```

---

## Failure Condition Evaluation

```csharp
[BurstCompile]
public partial struct SessionFailureEvaluationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var session = SystemAPI.GetSingleton<SessionState>();

        if (session.State != SessionState.Running)
            return;

        var failureConditions = SystemAPI.GetSingletonBuffer<FailureCondition>();

        foreach (var condition in failureConditions)
        {
            if (condition.Triggered)
                continue;

            bool failed = EvaluateFailureCondition(condition);

            if (failed)
            {
                condition.Triggered = true;
                session.State = SessionState.Defeat;
                TriggerDefeatSequence();
                return; // Stop evaluation, game over
            }
        }
    }
}
```

**Example Failure Evaluation**:
```csharp
bool EvaluateFailureCondition(FailureCondition condition)
{
    switch (condition.Type)
    {
        case FailureType.CriticalEntityLost:
            return !Exists(condition.CriticalEntity); // Flagship destroyed

        case FailureType.TimeLimitExpired:
            var session = GetSingleton<SessionState>();
            return session.SessionDurationTicks >= session.TimeLimitTicks;

        case FailureType.AllEntitiesDestroyed:
            var playerEntities = GetEntityQuery(ComponentType.ReadOnly<PlayerOwned>());
            return playerEntities.CalculateEntityCount() == 0;

        case FailureType.PopulationExtinct:
            var villagerRegistry = GetSingletonBuffer<VillagerRegistryEntry>();
            return villagerRegistry.Length == 0;

        default:
            return false;
    }
}
```

---

## Session Configuration

Sessions are configured via **ScriptableObject profiles** or **JSON scenario files**.

### Session Profile (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "NewSessionProfile", menuName = "PureDOTS/Session Profile")]
public class SessionProfile : ScriptableObject
{
    [Header("Session Info")]
    public SessionType Type;
    public string SessionName;
    public string Description;
    public Difficulty Difficulty;

    [Header("Time")]
    public ushort TimeLimitTicks = 0; // 0 = no limit
    public float TimeScale = 1.0f;

    [Header("Victory Conditions")]
    public List<VictoryConditionData> VictoryConditions;

    [Header("Failure Conditions")]
    public List<FailureConditionData> FailureConditions;

    [Header("Initial State")]
    public string InitialStateJsonPath; // Path to scenario JSON
}

[Serializable]
public struct VictoryConditionData
{
    public VictoryType Type;
    public bool Required;
    public float TargetValue;
}

[Serializable]
public struct FailureConditionData
{
    public FailureType Type;
    public Entity CriticalEntity; // Optional
    public float ThresholdValue;
}

public enum Difficulty : byte
{
    VeryEasy,
    Easy,
    Normal,
    Hard,
    VeryHard,
    Nightmare
}
```

### Session Initialization

```csharp
public partial struct SessionInitializationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var session = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<SessionState>(session);
        state.EntityManager.AddBuffer<VictoryCondition>(session);
        state.EntityManager.AddBuffer<FailureCondition>(session);
    }

    public void OnUpdate(ref SystemState state)
    {
        var session = SystemAPI.GetSingleton<SessionState>();

        if (session.State != SessionState.Loading)
            return;

        // Load session profile
        var profile = LoadSessionProfile(); // From JSON/ScriptableObject

        // Initialize victory conditions
        var victoryBuffer = SystemAPI.GetSingletonBuffer<VictoryCondition>();
        foreach (var vcData in profile.VictoryConditions)
        {
            victoryBuffer.Add(new VictoryCondition
            {
                Type = vcData.Type,
                Required = vcData.Required,
                TargetValue = vcData.TargetValue,
                CurrentValue = 0f,
                Completed = false
            });
        }

        // Initialize failure conditions
        var failureBuffer = SystemAPI.GetSingletonBuffer<FailureCondition>();
        foreach (var fcData in profile.FailureConditions)
        {
            failureBuffer.Add(new FailureCondition
            {
                Type = fcData.Type,
                CriticalEntity = fcData.CriticalEntity,
                ThresholdValue = fcData.ThresholdValue,
                Triggered = false
            });
        }

        // Load initial state (spawn entities, etc.)
        LoadInitialState(profile.InitialStateJsonPath);

        // Transition to Running
        session.State = SessionState.Running;
    }
}
```

---

## Campaign Progression

**Campaign Mode** uses a **mission unlock system**.

```csharp
// Persistent player profile (saved between sessions)
public struct CampaignProgress : IComponentData
{
    public ushort CurrentMission;                // 0-indexed
    public ushort HighestUnlockedMission;
    public DynamicBuffer<MissionCompletionEntry> CompletedMissions;
}

public struct MissionCompletionEntry : IBufferElementData
{
    public ushort MissionId;
    public bool PrimaryCompleted;
    public bool SecondaryCompleted;
    public float CompletionTime;                 // Ticks taken
    public float Score;
}
```

**Mission Unlock Logic**:
```csharp
void OnMissionVictory(ushort missionId, bool secondaryCompleted)
{
    var campaign = GetSingleton<CampaignProgress>();
    var completedMissions = GetSingletonBuffer<MissionCompletionEntry>();

    // Record completion
    completedMissions.Add(new MissionCompletionEntry
    {
        MissionId = missionId,
        PrimaryCompleted = true,
        SecondaryCompleted = secondaryCompleted,
        CompletionTime = GetSession().SessionDurationTicks,
        Score = GetSession().Score
    });

    // Unlock next mission
    ushort nextMission = (ushort)(missionId + 1);
    if (nextMission > campaign.HighestUnlockedMission)
    {
        campaign.HighestUnlockedMission = nextMission;
        SaveCampaignProgress(); // Persist to disk
    }

    // Optional: Unlock rewards (tech, units, etc.)
    if (secondaryCompleted)
    {
        UnlockBonusReward(missionId);
    }
}
```

---

## Difficulty Scaling

Difficulty affects initial conditions and AI behavior.

```csharp
public struct DifficultyModifiers : IComponentData
{
    public Difficulty Level;

    // Resource modifiers
    public float PlayerResourceMultiplier;       // 1.0 = normal, 0.5 = hard (half resources)
    public float EnemyResourceMultiplier;        // 1.0 = normal, 2.0 = hard (double resources)

    // AI modifiers
    public float AIAggressionMultiplier;         // 1.0 = normal, 1.5 = hard (more aggressive)
    public float AIDecisionSpeedMultiplier;      // 1.0 = normal, 2.0 = hard (thinks faster)

    // Combat modifiers
    public float PlayerDamageMultiplier;         // 1.0 = normal, 0.8 = hard (player weaker)
    public float EnemyDamageMultiplier;          // 1.0 = normal, 1.2 = hard (enemies stronger)

    // Time modifiers
    public float TimeScaleMultiplier;            // 1.0 = normal, 1.5 = hard (less time to react)
}
```

**Difficulty Presets**:
```csharp
DifficultyModifiers GetDifficultyModifiers(Difficulty difficulty)
{
    return difficulty switch
    {
        Difficulty.VeryEasy => new DifficultyModifiers
        {
            PlayerResourceMultiplier = 2.0f,
            EnemyResourceMultiplier = 0.5f,
            AIAggressionMultiplier = 0.5f,
            PlayerDamageMultiplier = 1.5f,
            EnemyDamageMultiplier = 0.7f
        },
        Difficulty.Normal => new DifficultyModifiers
        {
            PlayerResourceMultiplier = 1.0f,
            EnemyResourceMultiplier = 1.0f,
            AIAggressionMultiplier = 1.0f,
            PlayerDamageMultiplier = 1.0f,
            EnemyDamageMultiplier = 1.0f
        },
        Difficulty.VeryHard => new DifficultyModifiers
        {
            PlayerResourceMultiplier = 0.5f,
            EnemyResourceMultiplier = 2.0f,
            AIAggressionMultiplier = 2.0f,
            PlayerDamageMultiplier = 0.7f,
            EnemyDamageMultiplier = 1.5f
        },
        _ => GetDifficultyModifiers(Difficulty.Normal)
    };
}
```

---

## Post-Session Flow

When session ends (Victory/Defeat), trigger cleanup sequence.

```csharp
public partial struct SessionEndSequenceSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var session = SystemAPI.GetSingleton<SessionState>();

        if (session.State == SessionState.Victory)
        {
            HandleVictory();
        }
        else if (session.State == SessionState.Defeat)
        {
            HandleDefeat();
        }
    }

    void HandleVictory()
    {
        // Display victory screen (via presentation bridge)
        ShowVictoryScreen();

        // Calculate final score
        float finalScore = CalculateFinalScore();

        // Record stats (playtime, resources, kills, etc.)
        RecordSessionStats();

        // Unlock next mission (if campaign)
        if (GetSession().Type == SessionType.Campaign)
        {
            UnlockNextMission();
        }

        // Optional: Save replay
        SaveReplayFile();

        // Transition to Ended state
        GetSession().State = SessionState.Ended;

        // Return to main menu after X seconds
        ScheduleReturnToMenu(5000); // 5 seconds
    }

    void HandleDefeat()
    {
        // Display defeat screen
        ShowDefeatScreen();

        // Offer retry option
        ShowRetryButton();

        // Optional: Save failure replay for analysis
        SaveReplayFile();

        GetSession().State = SessionState.Ended;
    }
}
```

---

## Rewind Integration

Sessions must support rewind during gameplay.

```csharp
// Rewind limits per session type
SessionType.Sandbox => Unlimited rewind
SessionType.Campaign => Limited rewind (consume rewind charges)
SessionType.Scenario => Unlimited rewind (learn/retry)
SessionType.Challenge => No rewind (fair competition)
SessionType.Tutorial => Unlimited rewind
```

**Rewind Charges** (Campaign Mode):
```csharp
public struct RewindCharges : IComponentData
{
    public ushort ChargesRemaining;
    public ushort MaxCharges;
    public ushort RechargeRate;                  // Ticks per charge
    public ushort TicksSinceLastRecharge;
}
```

---

## Open Questions / Design Decisions Needed

1. **Session Auto-Save**: Should sessions auto-save periodically (every 1000 ticks)?
   - *Suggestion*: Yes, auto-save every 1000 ticks in Sandbox/Campaign, no auto-save in Challenge

2. **Victory Bonuses**: Should secondary objectives grant tangible rewards (resources, units)?
   - *Suggestion*: Yes in Campaign (unlock bonuses), no in Sandbox/Scenario (just satisfaction)

3. **Defeat Penalty**: Does failing a mission lock progression or allow retry?
   - *Suggestion*: Allow unlimited retries, but record failure count in stats

4. **Leaderboard Scope**: Global leaderboards or friend-only?
   - *Suggestion*: Both options (player choice)

5. **Session Duration Display**: Show elapsed time (real-time) or tick count?
   - *Suggestion*: Both (tick count for determinism, real-time for player convenience)

6. **Mid-Session Quit**: Can player quit without saving (forfeit progress)?
   - *Suggestion*: Yes, with confirmation prompt ("Unsaved progress will be lost")

7. **Victory Cutscenes**: Should campaign missions have victory cinematics?
   - *Suggestion*: Optional (designer can add custom victory events via scenario JSON)

8. **Difficulty Change**: Can player change difficulty mid-session?
   - *Suggestion*: No (prevents exploits), only at session start or mission restart

---

## Implementation Notes

- **SessionState** singleton = tracks session state, time, score
- **VictoryCondition** buffer = win conditions
- **FailureCondition** buffer = loss conditions
- **SessionInitializationSystem** = loads profile, spawns entities
- **SessionVictoryEvaluationSystem** = checks win conditions each tick
- **SessionFailureEvaluationSystem** = checks loss conditions each tick
- **SessionEndSequenceSystem** = handles victory/defeat flow
- **CampaignProgress** component = persistent progression (saved)

---

## References

- **Scenario Runner**: [ScenarioRunnerEntryPoints.cs](../../Packages/com.moni.puredots/Runtime/Devtools/ScenarioRunnerEntryPoints.cs) - Headless scenario execution
- **Rewind System**: [RewindPatterns.md](../DesignNotes/RewindPatterns.md) - Rewind integration
- **Time System**: [TimeComponents.cs](../../Packages/com.moni.puredots/Runtime/Runtime/TimeComponents.cs) - Time control
- **Alignment System**: Victory conditions can reference alignment thresholds
- **Aggregate Decisions**: Campaign missions can require aggregate-level victories (e.g., "Empire controls 10 sectors")
