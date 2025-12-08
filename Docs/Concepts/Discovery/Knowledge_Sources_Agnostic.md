# Knowledge Sources and Discovery (Agnostic Framework)

**Status:** Concept Design
**Category:** Core Discovery Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Knowledge Sources Framework** provides agnostic mechanics for one-time discoverable entities, objects, and places that grant permanent benefits or create persistent area effects. Games implement specific source types, interaction methods, and benefit catalogs while PureDOTS provides the discovery detection, interaction state tracking, and area effect systems.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Knowledge source component structure
- ✅ One-time interaction state machine
- ✅ Discovery detection framework
- ✅ Interaction duration tracking
- ✅ Benefit application framework
- ✅ Area effect propagation system
- ✅ Historical tracking (who discovered what, when)

**Game-Specific Aspects** (Implemented by Games):
- Source types (dragons, shrines, books, alien ruins)
- Benefit catalogs (skills, spells, stats, recipes, techs)
- Interaction types (instant, study, ritual, dialogue)
- Area effect types (culture pressure, fertility, blessings)
- Visual presentation (VFX, animations, audio)
- World generation placement rules

---

## Core Agnostic Components

### KnowledgeSource (Body ECS)
```csharp
/// <summary>
/// Agnostic knowledge source component
/// Games define type/benefit enums
/// </summary>
public struct KnowledgeSource : IComponentData
{
    public byte SourceTypeId;         // Game-defined enum
    public bool IsAvailable;
    public bool WasVisited;
    public float InteractionDuration; // Seconds (0 = instant)
    public byte BenefitTypeId;        // Game-defined enum
    public int BenefitValue;          // Skill points, spell ID, etc.
    public uint DiscoveryTick;
    public Entity DiscoveredBy;
}
```

### KnowledgeSourceInteraction (Body ECS)
```csharp
/// <summary>
/// Agnostic interaction state tracking
/// </summary>
public struct KnowledgeSourceInteraction : IComponentData
{
    public Entity Interactor;
    public float TimeRemaining;       // Countdown to completion
    public bool IsInteracting;
    public bool IsCompleted;
    public uint StartTick;
}
```

### LearnedKnowledgeHistory (Mind ECS Buffer)
```csharp
/// <summary>
/// Agnostic historical record of discoveries
/// </summary>
[InternalBufferCapacity(16)]
public struct LearnedKnowledgeHistory : IBufferElementData
{
    public Entity SourceEntity;
    public byte SourceTypeId;
    public byte BenefitTypeId;
    public int BenefitValue;
    public uint LearnedTick;
    public float3 DiscoveryLocation;
}
```

### AreaKnowledgeEffect (Mind ECS)
```csharp
/// <summary>
/// Agnostic persistent area effect
/// Games define effect type enums
/// </summary>
public struct AreaKnowledgeEffect : IComponentData
{
    public Entity Source;
    public byte EffectTypeId;         // Game-defined enum
    public float Radius;
    public float Strength;            // 0-1 or custom range
    public bool IsActive;
    public float3 Position;
}
```

---

## Agnostic Algorithms

### Discovery Detection
```csharp
/// <summary>
/// Detect when entity enters discovery radius
/// Agnostic: Games provide proximity values
/// </summary>
public static bool IsWithinDiscoveryRange(
    float3 entityPosition,
    float3 sourcePosition,
    float discoveryRadius)
{
    float distance = math.distance(entityPosition, sourcePosition);
    return distance <= discoveryRadius;
}
```

### Interaction Duration Tracking
```csharp
/// <summary>
/// Update interaction progress
/// Agnostic: Pure timer logic
/// </summary>
public static void UpdateInteractionProgress(
    ref KnowledgeSourceInteraction interaction,
    float deltaTime)
{
    if (!interaction.IsInteracting)
        return;

    interaction.TimeRemaining -= deltaTime;

    if (interaction.TimeRemaining <= 0f)
    {
        interaction.IsCompleted = true;
        interaction.IsInteracting = false;
    }
}
```

### Area Effect Strength Calculation
```csharp
/// <summary>
/// Calculate area effect strength at distance
/// Agnostic: Linear falloff from center
/// </summary>
public static float CalculateAreaEffectStrength(
    float3 targetPosition,
    float3 effectCenter,
    float effectRadius,
    float maxStrength)
{
    float distance = math.distance(targetPosition, effectCenter);

    if (distance >= effectRadius)
        return 0f;

    float falloff = 1f - (distance / effectRadius);
    return maxStrength * falloff;
}
```

### Historical Deduplication
```csharp
/// <summary>
/// Check if entity already learned from this source
/// Agnostic: Buffer scan
/// </summary>
public static bool HasLearnedFrom(
    DynamicBuffer<LearnedKnowledgeHistory> history,
    Entity source)
{
    for (int i = 0; i < history.Length; i++)
    {
        if (history[i].SourceEntity == source)
            return true;
    }
    return false;
}
```

---

## Extension Points for Games

### 1. Source Type Definitions
Games define knowledge source type enums:
```csharp
// Godgame example
public enum GodgameSourceType : byte
{
    // Living Beings
    Dragon,
    Spirit,
    OtherworldlyBeing,
    AncientMaster,
    Colony,

    // Objects
    SacredBook,
    Scroll,
    Runestone,
    Artifact,

    // Places
    Shrine,
    Statue,
    SacredGrove,
    AncientRuins,
    Temple,
}

// Space4X example
public enum Space4XSourceType : byte
{
    AlienRuins,
    DerelictShip,
    Monolith,
    ResearchStation,
    AncientLibrary,
    PrecursorArtifact,
}
```

### 2. Benefit Type Definitions
Games define benefit type enums:
```csharp
// Godgame example
public enum GodgameBenefitType : byte
{
    // Skills & Abilities
    SkillIncrease,
    SkillMastery,
    SpellUnlock,
    AbilityUnlock,

    // Stats
    StatBonus,
    AttributeBonus,

    // Knowledge
    RecipeUnlock,
    TacticUnlock,
    LoreUnlock,

    // Area Effects
    CulturePressure,
    FertilityBonus,
    BlessingAura,
}

// Space4X example
public enum Space4XBenefitType : byte
{
    TechUnlock,
    BlueprintUnlock,
    NavigationData,
    ResourceBonus,
    CrewBonus,
}
```

### 3. Interaction Type Definitions
Games define interaction methods:
```csharp
// Godgame example
public enum InteractionType : byte
{
    InstantDiscovery,    // Touch shrine, instant benefit
    Study,               // Read book over time (30 min)
    Ritual,              // Perform ritual at shrine (10 min)
    Dialogue,            // Talk to dragon/spirit (5 min)
    Meditation,          // Meditate at sacred grove (15 min)
}

// Space4X example
public enum InteractionType : byte
{
    QuickScan,           // Instant data download
    Experiment,          // Tinker with alien tech (60 min)
    Excavation,          // Dig through ruins (120 min)
    Translation,         // Decode alien text (45 min)
}
```

### 4. Area Effect Type Definitions
Games define persistent area effects:
```csharp
// Godgame example
public enum AreaEffectType : byte
{
    CulturePressure,     // Convert entities to culture
    FertilityBonus,      // Increase reproduction rate
    BlessingAura,        // Buff nearby entities
    CurseAura,           // Debuff nearby entities
    ManaRegeneration,    // Increase mana regen
}

// Space4X example
public enum AreaEffectType : byte
{
    ResearchBonus,       // Faster research near station
    ShieldBoost,         // Defense bonus near ruins
    NavigationHazard,    // Movement penalty near anomaly
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **DiscoveryDetectionSystem** (Body ECS, 60 Hz)
   - Detect entities entering discovery radius of knowledge sources
   - Create `KnowledgeSourceInteraction` component on discoverer
   - Trigger discovery VFX/audio/notifications

2. **InteractionProgressSystem** (Body ECS, 60 Hz)
   - Update `TimeRemaining` for active interactions
   - Handle interruptions (combat, movement)
   - Complete interactions when timer expires

3. **BenefitApplicationSystem** (Mind ECS, 1 Hz)
   - Apply benefits when interaction completes
   - Increase skills, unlock spells/recipes/techs
   - Add stats/attribute bonuses
   - Add to `LearnedKnowledgeHistory` buffer

4. **AreaEffectPropagationSystem** (Mind ECS, 1 Hz)
   - Detect entities in area effect radius
   - Apply culture pressure, fertility bonuses, etc.
   - Calculate strength based on distance falloff

5. **SourceStateManagementSystem** (Mind ECS, 1 Hz)
   - Mark sources as unavailable after use
   - Handle source disappearance (dragon departs, book crumbles)
   - Deactivate area effects when source is consumed

6. **WorldGenerationIntegration** (Aggregate ECS, 0.2 Hz)
   - Place knowledge sources during world generation
   - Define placement rules (shrines near rivers, ruins in mountains)
   - Ensure balanced distribution

---

## Data Contracts

Games must provide:
- Source type catalog (types, properties, interaction durations)
- Benefit type catalog (benefit types, values, application rules)
- Interaction type definitions (durations, requirements, interruption rules)
- Area effect type definitions (radius, strength, falloff curves)
- World generation rules (placement density, biome restrictions)

---

## Game-Specific Implementations

### Godgame (Fantasy World Discovery)
**Full Implementation:** [Knowledge_Sources_And_Discovery.md](../../../../Godgame/Docs/Concepts/Core/Knowledge_Sources_And_Discovery.md)

**Source Types:** Dragons, spirits, otherworldly beings, books, shrines, sacred groves
**Benefit Types:** Skills, spells, stats, recipes, culture pressure, fertility bonuses
**Interaction Types:** Instant, study (30 min), ritual (10 min), dialogue (5 min), meditation (15 min)
**Area Effects:** Culture pressure (500m), fertility bonus (300m), blessing auras

### Space4X (Sci-Fi Exploration)
**Implementation Reference:** TBD

**Source Types:** Alien ruins, derelict ships, monoliths, research stations, precursor artifacts
**Benefit Types:** Tech unlocks, blueprints, navigation data, resource bonuses
**Interaction Types:** Quick scan (instant), experiment (60 min), excavation (120 min)
**Area Effects:** Research bonus, shield boost, navigation hazards

---

## Performance Targets

**Body ECS (60 Hz) Budget:** 1-2 ms/frame
- Discovery detection: 1.0 ms (spatial queries)
- Interaction progress: 0.5 ms (timer updates)

**Mind ECS (1 Hz) Budget:** 20-30 ms/update
- Benefit application: 10 ms (skill/spell modifications)
- Area effect propagation: 10 ms (cultural influence, fertility)
- Source state management: 5 ms (availability updates)

**Optimization Strategies:**
- Spatial grid/octree for discovery detection (avoid N² checks)
- Batch benefit application (multiple discoveries per frame)
- LOD: Reduce area effect update frequency for distant sources
- Pool interaction components (reuse instead of create/destroy)

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Discovery range detection (distance threshold checks)
- ✅ Interaction duration tracking (timer countdown)
- ✅ Area effect strength calculation (distance falloff)
- ✅ Historical deduplication (prevent double learning)

### Integration Tests (Games)
- Discovery detection accuracy (enter/exit radius)
- Interaction completion (full duration vs interruption)
- Benefit application correctness (skill increase, spell unlock)
- Area effect propagation (culture conversion, fertility bonuses)
- One-time enforcement (cannot revisit for same benefit)

---

## Migration Notes

**New Components Required:**
- `KnowledgeSource` (Body ECS)
- `KnowledgeSourceInteraction` (Body ECS)
- `LearnedKnowledgeHistory` buffer (Mind ECS)
- `AreaKnowledgeEffect` (Mind ECS)
- Game-specific benefit components (skills, spells, stats)

**Integration with Existing Systems:**
- `SkillComponents` integration for skill increases
- `SpellKnowledge` integration for spell unlocks
- `CultureComponents` integration for cultural influence
- `WorldGeneration` integration for source placement

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layers (to be created)
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS coding patterns
- `Runtime/Skills/SkillComponents.cs` - Skill system integration

**Game Implementations:**
- `Godgame/Docs/Concepts/Core/Knowledge_Sources_And_Discovery.md` - Full game-side concept
- `Space4X/Docs/Concepts/Exploration/Discovery_System.md` - Space exploration variant (to be created)

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
