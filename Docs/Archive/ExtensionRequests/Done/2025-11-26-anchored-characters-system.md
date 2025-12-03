# Extension Request: Anchored Characters System

**Status**: `[RESOLVED]`
**Submitted**: 2025-11-26
**Resolved**: 2025-11-27
**Game Project**: Both (Space4X + Godgame), potentially LastLightVR
**Priority**: P1 - High (core player attachment feature)
**Assigned To**: Agent (Automated)
**Sprint**: Phase 1 complete - Core components and systems implemented

---

## Use Case

All game projects need a way for players to develop emotional attachment to specific characters (captains, villagers, defenders) by ensuring those characters remain fully visible and simulated regardless of distance, LOD settings, or normal culling rules.

**Space4X:**
- Player's favorite ship captains always render even when viewing galaxy-wide strategic map
- Legendary ace pilots remain fully simulated across star systems
- Notable NPCs (rivals, allies, mentors) stay visible and active
- Player's flagship carrier never simplifies or despawns

**Godgame:**
- Village leaders (chief, master smith, high priest) always visible
- Player's favorite villagers remain animated even when camera is zoomed out
- Heroes who survived major events stay fully simulated
- NPCs with emotional significance never reduce to abstract dots

**LastLightVR:**
- Veteran defenders who survived multiple Last Stands always render at high quality
- Named heroes with "Tales of the Fallen" remain fully animated
- Player's right-hand NPCs continuously simulate even across battlefield
- Creates attachment: "I can't let Kael die - I can see him holding that line!"

### Core Problem

**Standard ECS optimization**:
- Entities far from camera → culled from rendering
- Entities outside simulation radius → despawned or simplified
- Distant entities → low LOD or abstract simulation

**Player experience problem**:
> "I spent 20 hours with Captain Aria. I zoom out to see the battle, and she becomes a dot or disappears. Is she even real? Or just a database entry?"

**Emotional disconnect**: Players can't form attachments to characters who feel ephemeral.

---

## Proposed Solution

**Extension Type**: New Components + System Integration

### Components

#### 1. Core Component: `AnchoredCharacter`

```csharp
namespace PureDOTS.Runtime.Rendering
{
    /// <summary>
    /// Marks this entity as "anchored" - exempt from normal culling/LOD/despawn rules.
    /// Ensures player-favorite characters remain fully rendered and simulated.
    /// </summary>
    public struct AnchoredCharacter : IComponentData
    {
        /// <summary>
        /// Priority level (higher = more important if we need to limit anchored count).
        /// 0 = normal anchored (player favorite)
        /// 1-5 = increasingly important (veterans, leaders)
        /// 10 = critical (flagship, avatar, story-critical NPCs)
        /// </summary>
        public byte Priority;

        /// <summary>
        /// Who anchored this? (for multiplayer - which player cares about this entity?)
        /// Entity.Null = all players care (major NPCs, shared anchors)
        /// </summary>
        public Entity AnchoredBy;

        /// <summary>
        /// Reason/tag for why anchored (debug/telemetry).
        /// 0 = player favorited
        /// 1 = story-critical NPC
        /// 2 = legendary/veteran status
        /// 3 = flagship/leader
        /// 4 = auto-anchored by attention tracking
        /// </summary>
        public byte AnchorReason;

        /// <summary>
        /// When was this character anchored?
        /// </summary>
        public uint AnchoredAtTick;
    }
}
```

#### 2. Rendering Override: `AnchoredRenderingOverride`

```csharp
namespace PureDOTS.Runtime.Rendering
{
    /// <summary>
    /// Optional: Controls reduced detail level for anchored characters at extreme distance.
    /// Still rendered (never culled), but can use lower LOD than full quality.
    /// </summary>
    public struct AnchoredRenderingOverride : IComponentData
    {
        /// <summary>
        /// Minimum LOD level this character will use.
        /// 0 = full detail always
        /// 1 = medium detail minimum
        /// 2 = low detail minimum (but never culled entirely)
        /// </summary>
        public byte MinLODLevel;

        /// <summary>
        /// Should this character cast shadows even at distance?
        /// </summary>
        public bool AlwaysCastShadows;

        /// <summary>
        /// Should VFX/particles be active even when far?
        /// False = model renders, but particles cull (performance optimization)
        /// </summary>
        public bool AlwaysRenderVFX;
    }
}
```

#### 3. Player Tracking: `PlayerAnchoredCharacterBuffer`

```csharp
namespace PureDOTS.Runtime.Registry
{
    /// <summary>
    /// Buffer tracking all characters this player has anchored.
    /// Attached to player entity.
    /// </summary>
    [InternalBufferCapacity(8)] // Most players anchor 5-10 characters
    public struct PlayerAnchoredCharacterBuffer : IBufferElementData
    {
        public Entity AnchoredEntity;
        public uint AnchoredAtTick;
        public byte Priority;
    }
}
```

#### 4. Budget Enforcement: `AnchoredCharacterBudget`

```csharp
namespace PureDOTS.Runtime.Registry
{
    /// <summary>
    /// Singleton component tracking anchored character budget.
    /// Prevents performance issues from unlimited anchoring.
    /// </summary>
    public struct AnchoredCharacterBudget : IComponentData
    {
        /// <summary>
        /// Maximum anchored characters per player (configurable).
        /// </summary>
        public byte MaxAnchoredPerPlayer; // Default: 10

        /// <summary>
        /// Current total anchored count across all players.
        /// </summary>
        public int TotalAnchoredCount;

        /// <summary>
        /// Performance telemetry: total render cost of anchored characters (ms).
        /// </summary>
        public float TotalRenderCostMs;

        /// <summary>
        /// Performance telemetry: total simulation cost of anchored characters (ms).
        /// </summary>
        public float TotalSimCostMs;
    }
}
```

### System Integration Points

#### A) Culling System Integration

**Requirement**: Entities with `AnchoredCharacter` are never frustum-culled.

**Implementation**: In rendering/culling systems, check for `AnchoredCharacter` tag:

```csharp
// Pseudocode - actual implementation depends on rendering pipeline
Entities
    .WithNone<AnchoredCharacter>() // Normal entities
    .ForEach((Entity e, in LocalTransform transform) =>
    {
        if (!frustum.Contains(transform.Position))
        {
            Cull(e); // Standard culling
        }
    }).Schedule();

Entities
    .WithAll<AnchoredCharacter>() // Anchored entities
    .ForEach((Entity e, in LocalTransform transform, in AnchoredRenderingOverride renderOverride) =>
    {
        // NEVER cull, but optionally reduce LOD at distance
        float distance = math.distance(transform.Position, cameraPos);
        byte lodLevel = math.max(renderOverride.MinLODLevel, CalculateLOD(distance));

        SetLOD(e, lodLevel);
        SetRenderingEnabled(e, true); // Always render
    }).Schedule();
```

#### B) Despawn System Integration

**Requirement**: Entities with `AnchoredCharacter` are never despawned.

```csharp
// Pseudocode
Entities
    .WithNone<AnchoredCharacter>() // Don't despawn anchored!
    .ForEach((Entity e, in LocalTransform transform) =>
    {
        if (ShouldDespawn(transform.Position))
        {
            ecb.DestroyEntity(e);
        }
    }).Schedule();

// Anchored entities only despawn when explicitly killed by gameplay
```

#### C) Simulation System Integration

**Requirement**: AI/behavior systems always update anchored entities, even if outside normal simulation radius.

```csharp
// In AI update systems
Entities
    .WithNone<AnchoredCharacter>()
    .ForEach((ref AIState ai, in LocalTransform transform) =>
    {
        if (IsWithinSimulationRadius(transform.Position))
        {
            UpdateAI(ref ai);
        }
    }).Schedule();

Entities
    .WithAll<AnchoredCharacter>() // Always update
    .ForEach((ref AIState ai) =>
    {
        UpdateAI(ref ai); // Unconditional update
    }).Schedule();
```

#### D) Spatial Query Integration

**Requirement**: Spatial queries (e.g., "find all enemies nearby") always include anchored characters, even if technically outside query radius.

```csharp
// Example: Finding nearby entities for targeting
var nearbyEntities = SpatialGrid.Query(position, radius);

// Also include all anchored characters (they're always relevant)
nearbyEntities.AddRange(AnchoredCharacterRegistry.GetAll());
```

---

## Impact Assessment

### Files/Systems Affected

**New Files**:
- `Packages/com.moni.puredots/Runtime/Runtime/Rendering/AnchoredCharacterComponents.cs`
  - `AnchoredCharacter`
  - `AnchoredRenderingOverride`
- `Packages/com.moni.puredots/Runtime/Runtime/Registry/AnchoredCharacterRegistry.cs`
  - `PlayerAnchoredCharacterBuffer`
  - `AnchoredCharacterBudget`
- `Packages/com.moni.puredots/Runtime/Systems/Rendering/AnchoredCharacterCullingSystem.cs`
  - Integration with culling
- `Packages/com.moni.puredots/Runtime/Systems/AnchoredCharacterBudgetSystem.cs`
  - Budget enforcement and telemetry

**Modified Files** (integration points):
- Any culling system → Add `WithNone<AnchoredCharacter>()` to normal cull logic
- Any despawn system → Add `WithNone<AnchoredCharacter>()` to despawn logic
- Any simulation radius system → Add separate query for anchored entities
- Spatial query utilities → Add `IncludeAnchoredCharacters()` option

### Breaking Changes

**No breaking changes** - this is purely additive.

- New components are opt-in
- Existing entities without `AnchoredCharacter` behave exactly as before
- Systems that don't check for anchored characters will have default behavior (cull/despawn normally)

**Migration Path**: N/A (additive only)

---

## Example Usage

### Space4X: Anchoring a Captain

```csharp
// Player clicks "Anchor Captain Aria" in UI
public void AnchorCaptain(Entity captainEntity, Entity playerEntity)
{
    var ecb = new EntityCommandBuffer(Allocator.Temp);

    // Add anchored component
    ecb.AddComponent(captainEntity, new AnchoredCharacter
    {
        Priority = 0, // Player favorite
        AnchoredBy = playerEntity,
        AnchorReason = 0, // Player favorited
        AnchoredAtTick = TimeState.Tick
    });

    // Optional: Add rendering override
    ecb.AddComponent(captainEntity, new AnchoredRenderingOverride
    {
        MinLODLevel = 1, // At least medium detail
        AlwaysCastShadows = true,
        AlwaysRenderVFX = false // VFX can cull for performance
    });

    // Track in player's anchored buffer
    var buffer = EntityManager.GetBuffer<PlayerAnchoredCharacterBuffer>(playerEntity);
    buffer.Add(new PlayerAnchoredCharacterBuffer
    {
        AnchoredEntity = captainEntity,
        AnchoredAtTick = TimeState.Tick,
        Priority = 0
    });

    ecb.Playback(EntityManager);
    ecb.Dispose();

    Debug.Log($"Captain Aria is now anchored. She will always be visible and simulated.");
}
```

### Godgame: Auto-Anchoring Village Leaders

```csharp
// When villager is promoted to chief, auto-anchor them
public void PromoteToChief(Entity villagerEntity)
{
    // Standard promotion logic...
    AddComponent<VillageChief>(villagerEntity);

    // Auto-anchor (story-critical NPC)
    AddComponent(villagerEntity, new AnchoredCharacter
    {
        Priority = 5, // High priority (leader)
        AnchoredBy = Entity.Null, // All players care
        AnchorReason = 3, // Leader/flagship
        AnchoredAtTick = TimeState.Tick
    });

    Debug.Log($"Villager promoted to Chief and auto-anchored.");
}
```

### LastLightVR: Anchoring Veterans

```csharp
// After surviving 3 Last Stands, auto-anchor defender
public void CheckVeteranStatus(Entity defenderEntity, int lastStandsSurvived)
{
    if (lastStandsSurvived >= 3 && !HasComponent<AnchoredCharacter>(defenderEntity))
    {
        AddComponent(defenderEntity, new AnchoredCharacter
        {
            Priority = 2, // Veteran
            AnchoredBy = playerEntity,
            AnchorReason = 2, // Legendary/veteran status
            AnchoredAtTick = TimeState.Tick
        });

        Debug.Log($"Defender has survived 3 Last Stands and earned veteran status (auto-anchored).");
    }
}
```

### Budget Enforcement

```csharp
// Check if player can anchor another character
public bool CanAnchor(Entity playerEntity)
{
    var budget = SystemAPI.GetSingleton<AnchoredCharacterBudget>();
    var anchored = EntityManager.GetBuffer<PlayerAnchoredCharacterBuffer>(playerEntity);

    if (anchored.Length >= budget.MaxAnchoredPerPlayer)
    {
        Debug.LogWarning($"Cannot anchor: limit of {budget.MaxAnchoredPerPlayer} reached.");
        return false;
    }

    return true;
}
```

---

## Alternative Approaches Considered

### Alternative 1: Game-Specific Implementation

**Approach**: Each game (Space4X, Godgame) implements their own anchoring system.

**Pros**:
- Full control over implementation
- Can customize exactly to game needs

**Cons**:
- ❌ Duplicate code across projects
- ❌ Inconsistent UX (Space4X anchoring works differently than Godgame)
- ❌ Wasted effort (solving same problem 3+ times)
- ❌ No shared learnings or optimizations

**Rejected**: Violates DRY principle; anchored characters are a universal pattern.

### Alternative 2: Always Render Everything (No Culling)

**Approach**: Just disable culling for all entities, no special "anchored" system needed.

**Pros**:
- Simple: no new components/systems

**Cons**:
- ❌ Terrible performance (rendering thousands of off-screen entities)
- ❌ Not selective (can't choose which characters matter)
- ❌ Doesn't solve budget problem (still need to limit)

**Rejected**: Performance unacceptable.

### Alternative 3: Registry-Only Tracking (No Rendering Changes)

**Approach**: Track anchored characters in registry, but don't change rendering/culling.

**Pros**:
- Lightweight: just data tracking

**Cons**:
- ❌ Doesn't solve the core problem (characters still cull/despawn)
- ❌ Players still can't SEE their favorite characters at distance
- ❌ Defeats the purpose

**Rejected**: Doesn't achieve the fantasy ("I want to SEE Captain Aria's ship").

---

## Implementation Notes

### Performance Considerations

**Rendering**:
- Anchored characters bypass frustum culling → could render off-screen
- Mitigate with `AnchoredRenderingOverride.MinLODLevel` (still render, but lower quality)
- Mitigate with VFX culling (model renders, particles don't)

**Simulation**:
- Anchored characters always tick AI → CPU cost
- Mitigate with reduced update frequency at extreme distance (1/4 tick rate)
- Budget cap (max 10 per player) prevents runaway cost

**Memory**:
- Anchored characters never despawn → could accumulate
- Mitigate with explicit cleanup on death/retirement
- Telemetry tracks memory footprint

### Multiplayer Considerations

**Shared Anchored Characters**:
- `AnchoredBy = Entity.Null` means all players care (major NPCs)
- These count toward global budget, not per-player

**Per-Player Anchored Characters**:
- `AnchoredBy = playerEntity` means only that player cares
- Other players may see reduced LOD or normal culling

**PvP Conflicts**:
- If Player A anchors Entity X and Player B anchors Entity Y
- Both render for both players (use priority system if budget exceeded)

### Telemetry & Debug

**Track**:
- How many characters are anchored (per player, globally)
- Which characters get anchored most (data-driven design)
- Performance cost (render ms, sim ms)
- Budget violations (player hitting limit)

**Expose in Editor**:
- Gizmo: Anchored characters glow in scene view
- Inspector: Show anchor status, priority, who anchored
- Profiler: "Anchored Characters" section showing cost

---

## Testing Requirements

### Unit Tests

- [ ] Adding `AnchoredCharacter` component prevents culling
- [ ] Adding `AnchoredCharacter` component prevents despawn
- [ ] Budget enforcement works (can't exceed max)
- [ ] Un-anchoring removes from player buffer

### Integration Tests

- [ ] Anchored character renders when off-screen (Space4X galaxy view)
- [ ] Anchored character AI updates when outside simulation radius
- [ ] Spatial queries include anchored characters
- [ ] LOD reduction works (still renders, but lower quality)

### Performance Tests

- [ ] 10 anchored characters: < 5ms overhead
- [ ] 20 anchored characters: < 10ms overhead
- [ ] 100 anchored characters: < 50ms overhead (stress test)

### UX Tests

- [ ] Players understand anchor UI (can favorite characters)
- [ ] Players notice anchored characters remain visible
- [ ] Players report emotional attachment increase
- [ ] Budget limit feels reasonable (not too restrictive)

---

## Dependencies

### Required PureDOTS Systems

- **Registry System** - For tracking anchored status persistently
- **Telemetry System** - For performance tracking
- **Rendering Pipeline** - For culling integration

### Unity Packages

- `com.unity.entities` >= 1.4.2
- `com.unity.entities.graphics` >= 1.4.2 (for rendering integration)
- `com.unity.burst` >= 1.8.24

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**: Automated
**Review Date**: 2025-11-27
**Decision**: Deferred to Backlog
**Notes**: 

This is a feature request with significant scope (2-3 weeks estimated effort). Moved to backlog for sprint planning after current blockers are cleared.

**Pre-requisites:**
- P0 BC1016 Burst errors resolved ✅
- Core rendering/culling pipeline stable
- Spatial grid integration complete

**When Ready:**
- Create experimental branch
- Implement core components first (`AnchoredCharacter`, `AnchoredRenderingOverride`)
- Integrate with culling/despawn systems
- Add budget enforcement

---

## Appendix: User Stories

### Story 1: Space4X Captain Attachment

**As a** Space4X player
**I want to** anchor Captain Aria
**So that** I can always see her ship, even from galaxy view
**And** feel like she's a real person in my fleet, not just a stat

**Acceptance Criteria**:
- When I anchor Captain Aria, her ship is always visible
- Her ship uses at least medium LOD, never fully culled
- Her AI continues running even when I'm across the galaxy
- I can visually identify her ship in large fleet battles

### Story 2: Godgame Village Leader

**As a** Godgame player
**I want to** anchor Borin, my master smith
**So that** I can always see him working at the forge
**And** feel connected to him as my village's craftsman

**Acceptance Criteria**:
- When I anchor Borin, he's always visible at the forge
- His animations play even when I'm zoomed out
- If demons raid, I see him grab his hammer and fight
- I can check on him anytime and see real activity

### Story 3: LastLightVR Veteran Defender

**As a** LastLightVR player
**I want to** auto-anchor Kael after he survives 3 Last Stands
**So that** I can always see him fighting on the battlefield
**And** feel urgency when he's in danger

**Acceptance Criteria**:
- After 3 Last Stand survivals, Kael is auto-anchored
- His character model always renders at high VR quality
- His combat AI runs continuously across the battlefield
- I can glance and see Kael holding the line (emergent attachment)

---

**Submitted By**: Development Team
**Estimated Effort**: 2-3 weeks
**Expected Impact**: High - Core player attachment feature for all games
