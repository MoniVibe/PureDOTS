# Workplace Camaraderie & Proximity Preferences

**Status**: Concept  
**Category**: Core / Social / Movement  
**Related Systems**: Relations, Communication, Cooperation, Locomotion  
**Applies To**: Godgame, Space4X (shared social behavior)

---

## Overview

Entities with high relations prefer working in proximity and engaging in casual conversation while performing tasks. High cohesion groups may sing work songs when observed closely. Low relations produce opposite behavior: entities avoid proximity and remain silent.

**Key Principle**: Relations influence movement preferences and social behavior during work, creating visible workplace culture and camaraderie.

---

## Core Concepts

### Proximity Preference
- **High relations** → prefer working near each other
- **Low relations** → avoid proximity, maintain distance
- **Neutral relations** → no preference (work efficiency only)

### Casual Communication
- Entities chat while working (if relations allow)
- Communication is **ambient** (not task-critical)
- Frequency scales with relation strength

### Work Songs
- High cohesion groups sing together
- Triggered when:
  - Group cohesion > threshold (e.g., 0.7)
  - Entities are in proximity
  - Player is zoomed close enough to hear
- Cultural/contextual (different songs for different work types)

### Opposite Behavior (Low Relations)
- Avoid proximity
- No chatting
- Silent work
- May actively move away if too close

---

## Work Types (Examples)

### Haulers
- Prefer hauling routes that pass near friends
- Chat while carrying loads
- May coordinate routes to maximize social time

### Builders
- Walk to construction sites together
- Chat while building
- Coordinate work positions to stay near friends

### Farmers
- Work adjacent plots when possible
- Chat during planting/harvesting
- Share tools/help when nearby

### Miners
- Prefer working same shaft/area as friends
- Chat during breaks and work
- Coordinate shifts to overlap with friends

### Foresters
- Work in same grove when possible
- Chat while chopping/planting
- Share knowledge about tree locations

**General Rule**: Any entity type that can work together will prefer proximity when relations are high.

---

## Data Model

### Proximity Preference Component

```csharp
public struct ProximityPreference : IComponentData
{
    public float RelationThreshold;          // Minimum relation to prefer proximity (default: 0.3)
    public float PreferredDistance;           // Ideal distance from friends (default: 2-5 units)
    public float AvoidanceDistance;          // Distance to maintain from disliked entities (default: 5-10 units)
    public float ProximityWeight;             // How much proximity matters vs efficiency (0-1)
    public bool IsActive;                    // Whether preference is currently active
}
```

### Workplace Camaraderie Component

```csharp
public struct WorkplaceCamaraderie : IComponentData
{
    public float ChatFrequency;              // How often to chat (0-1, relation-based)
    public float LastChatTime;               // When last chat occurred
    public float ChatCooldown;               // Minimum time between chats
    public bool CanSingWorkSongs;            // Whether group can sing
    public float CohesionThreshold;          // Minimum cohesion for work songs (default: 0.7)
}
```

### Work Song State

```csharp
public struct WorkSongState : IComponentData
{
    public FixedString64Bytes SongId;        // Which song is being sung
    public WorkSongType SongType;            // Type of work song
    public float SongProgress;               // Progress through song (0-1)
    public uint ParticipantCount;            // How many are singing
    public bool IsActive;                    // Whether song is currently active
}

public enum WorkSongType : byte
{
    Hauling = 0,
    Building = 1,
    Farming = 2,
    Mining = 3,
    Forestry = 4,
    General = 5
}
```

---

## Movement Integration

### Pathfinding Modifier

When pathfinding to work location, entities consider:

1. **Direct efficiency** (shortest path)
2. **Proximity bonus** (paths that pass near friends)
3. **Avoidance penalty** (paths that pass near disliked entities)

**Formula**:
```
PathScore = BaseEfficiency - (ProximityWeight × FriendProximityBonus) + (AvoidanceWeight × EnemyProximityPenalty)
```

### Position Selection

When multiple work positions are available:

1. **High relations**: Choose position near friends
2. **Low relations**: Choose position away from disliked entities
3. **Neutral**: Choose most efficient position

### Dynamic Adjustment

Entities periodically reassess proximity:
- If friend moves away, consider following (if task allows)
- If disliked entity approaches, consider moving away
- Balance between social preference and task efficiency

---

## Communication Integration

### Casual Chat System

```csharp
public struct CasualChat : IComponentData
{
    public Entity ChatPartner;              // Who they're chatting with
    public float ChatDuration;               // How long chat lasts
    public float ChatStartTime;              // When chat started
    public ChatTopic Topic;                 // What they're talking about
    public bool IsActive;
}

public enum ChatTopic : byte
{
    Work = 0,                // Talking about the work
    Personal = 1,            // Personal stories
    Gossip = 2,              // About others
    Plans = 3,               // Future plans
    Complaints = 4,          // Venting
    Jokes = 5                // Humor
}
```

### Chat Triggers

Chats occur when:
- Entities are in proximity (within chat range)
- Relations > threshold (e.g., 0.3)
- Not on cooldown
- Both entities are working (not idle)
- Work allows chatting (not dangerous/urgent)

### Chat Frequency

```
ChatFrequency = RelationStrength × WorkCompatibility × (1 - Urgency)
```

- **High relations** → more frequent chats
- **Compatible work** → easier to chat (both doing similar tasks)
- **Urgent work** → less chatting (focus on task)

---

## Work Songs System

### Activation Conditions

Work songs activate when:
1. **Group cohesion** > threshold (default: 0.7)
2. **Entities in proximity** (within song range, e.g., 10 units)
3. **Player zoom level** close enough (presentation layer)
4. **Work type compatible** (all doing similar work)
5. **Cultural context** allows (some cultures may not sing)

### Song Selection

- Song type matches work type (hauling song for haulers, etc.)
- Cultural variation (different songs per culture/faction)
- Group size affects song choice (solo vs group songs)

### Song Mechanics

- **Synchronization**: High cohesion = better synchronization
- **Duration**: Songs loop while conditions are met
- **Quality**: Cohesion + relation strength affects quality
- **Effects**: Work songs provide small morale bonus to participants

### Presentation

- **Audio**: Play work song audio when zoomed close
- **Visual**: Optional mouth/gesture animations
- **UI**: Optional subtitle/lyrics display

---

## Low Relations Behavior

### Avoidance

Entities with low relations:
- **Maintain distance**: Prefer positions away from disliked entities
- **Pathfinding**: Avoid routes that pass near enemies
- **Position selection**: Choose work positions that maximize distance

### Silence

- **No chatting**: Won't initiate or respond to casual chat
- **Minimal communication**: Only task-critical communication
- **No work songs**: Won't participate in group singing

### Active Avoidance

If disliked entity approaches too close:
- **Move away**: Relocate to different work area (if task allows)
- **Request reassignment**: Ask for different task/location (if authority system allows)
- **Tension**: May escalate to conflict if forced proximity persists

---

## Integration with Existing Systems

### Relations System
- **Reads relation values** to determine proximity preferences
- **Updates relations** based on positive/negative social interactions
- **Tracks relation history** (recent interactions affect current behavior)

### Communication System
- **Uses communication channels** for casual chat
- **Respects language barriers** (can't chat without shared language)
- **Applies communication clarity** to chat success

### Cooperation System
- **Proximity preference** affects who works together
- **High cohesion** enables work songs
- **Camaraderie** contributes to cooperation cohesion

### Locomotion System
- **Pathfinding** considers proximity preferences
- **Position selection** balances efficiency vs social preference
- **Dynamic movement** adjusts based on friend/enemy positions

### Task System
- **Work assignments** can consider relation preferences
- **Task flexibility** allows entities to choose positions
- **Task urgency** overrides social preferences when needed

---

## Performance Considerations

### Proximity Queries

- Use **spatial partitioning** to find nearby entities efficiently
- **Cache friend/enemy lists** per entity (update periodically, not every frame)
- **Limit proximity checks** to entities in same work area

### Chat Frequency

- **Cooldown system** prevents chat spam
- **Batch chat events** (multiple chats per frame if needed)
- **LOD system** (only process chats for visible/important entities)

### Work Songs

- **Group-based** (one song per group, not per entity)
- **Spatial culling** (only play songs for groups near player)
- **Audio pooling** (reuse audio sources)

---

## Configuration (Policy-Driven)

### Proximity Preferences

```yaml
ProximityPreferencePolicy:
  relation_threshold: 0.3          # Minimum relation to prefer proximity
  preferred_distance_min: 2.0     # Minimum preferred distance
  preferred_distance_max: 5.0     # Maximum preferred distance
  avoidance_distance_min: 5.0     # Minimum avoidance distance
  avoidance_distance_max: 10.0    # Maximum avoidance distance
  proximity_weight_default: 0.3    # Default weight for proximity vs efficiency
```

### Chat System

```yaml
ChatPolicy:
  chat_range: 5.0                 # Maximum distance for chatting
  chat_cooldown_min: 30.0         # Minimum seconds between chats
  chat_cooldown_max: 120.0        # Maximum seconds between chats
  chat_duration_min: 5.0          # Minimum chat duration (seconds)
  chat_duration_max: 20.0         # Maximum chat duration (seconds)
  relation_threshold: 0.3         # Minimum relation to chat
```

### Work Songs

```yaml
WorkSongPolicy:
  cohesion_threshold: 0.7         # Minimum cohesion to sing
  song_range: 10.0                # Maximum distance for group singing
  min_participants: 2            # Minimum entities needed for song
  morale_bonus: 0.1               # Morale bonus from singing (0-1)
  cultural_variation: true        # Different songs per culture
```

---

## Example Scenarios

### Scenario 1: Haulers with High Relations

```
Entity A (Hauler) and Entity B (Hauler) have relation: 0.8

Behavior:
- Both prefer routes that pass near each other
- When in proximity, chat frequency: 0.8 (very frequent)
- Chat topics: Work, Personal, Jokes
- If cohesion > 0.7 and both hauling: Work song activates
- Work efficiency: Slightly reduced (social time), but morale increased
```

### Scenario 2: Builders Going to Construction Together

```
Entity A (Builder) and Entity B (Builder) have relation: 0.6

Behavior:
- Both walk to construction site together (same path)
- Chat while walking and building
- Choose adjacent work positions
- Coordinate tool sharing
- Work songs if group cohesion high
```

### Scenario 3: Low Relations (Avoidance)

```
Entity A (Farmer) and Entity B (Farmer) have relation: -0.4

Behavior:
- Entity A chooses farm plot away from Entity B
- No chatting (relation below threshold)
- If Entity B approaches: Entity A moves away
- Silent work, minimal interaction
- Work efficiency: Normal (no social overhead)
```

### Scenario 4: Work Song Group

```
5 Miners working together:
- Relations: All > 0.5 (friends)
- Cohesion: 0.75 (high)
- Proximity: All within 10 units
- Work type: All mining

Result:
- Mining work song activates
- All 5 miners sing together
- Synchronization: 0.75 (good)
- Morale bonus: +0.1 to all participants
- Player hears song when zoomed close
```

---

## Design Principles

1. **Relations drive behavior**: High relations = proximity + chatting, low relations = avoidance + silence
2. **Work efficiency balance**: Social preferences don't completely override efficiency
3. **Emergent culture**: Workplace culture emerges from relation patterns
4. **Presentation-aware**: Work songs only play when player can observe
5. **Performance-conscious**: Efficient queries and batching for scale
6. **Policy-driven**: All thresholds and behaviors configurable

---

## Open Questions

- Should proximity preferences affect task assignment (assign friends to same area)?
- How to handle mixed relations (entity likes A but dislikes B, both nearby)?
- Should work songs have gameplay effects beyond morale (e.g., work speed bonus)?
- How to handle cultural differences in work song styles?
- Should entities remember good/bad work experiences and adjust relations?

---

**Last Updated**: 2025-12-20  
**Status**: Concept



