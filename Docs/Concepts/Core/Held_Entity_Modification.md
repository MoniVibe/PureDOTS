# Held Entity Modification System

**Status:** Concept Design
**Category:** Debug / Development Tool
**Complexity:** Low
**Scope:** All entities with agency
**Created:** 2025-01-21
**Last Updated:** 2025-01-21

---

## Overview

**Core Concept:** Entities that are currently held by the player (via divine hand or similar interaction) can be modified in their outlooks, behaviors, archetypes, and alignments. This modification system is gated behind a debug flag to allow on-the-fly entity modification during development, testing, and debugging.

**Purpose:**
- **Development:** Quickly test different entity configurations without restarting
- **Debugging:** Modify entity properties to reproduce specific scenarios
- **Testing:** Validate behavior changes with different alignment/outlook combinations
- **Prototyping:** Rapid iteration on entity personality and cultural traits

**Gating:** All modification features are disabled by default and require a debug flag to enable.

---

## Core Concept

### Held Entity Detection

**Held Entity Components:**
```csharp
// PureDOTS Core
public struct HeldByPlayer : IComponentData
{
    public Entity Holder;                   // Player/camera/hand entity
    public float3 LocalOffset;              // Local offset from holder
    public float3 HoldStartPosition;        // World position when picked up
    public float HoldStartTime;             // Time when pickup started
}

// Godgame-specific
public struct HandHeldTag : IComponentData
{
    public Entity Holder;                   // Hand entity holding this item
}

// Space4X-specific
public struct GrabbedTag : IComponentData
{
    public Entity Holder;                   // Hand entity holding this entity
}
```

**Detection:**
- System checks for `HeldByPlayer`, `HandHeldTag`, or `GrabbedTag` components
- If entity has any of these components, it is considered "held"
- Modification UI/commands only available for held entities

---

## Modifiable Properties

### 1. Alignments

**Component:** `EntityAlignment` (PureDOTS) or `VillagerAlignment` (Godgame)

**Modifiable Fields:**
```csharp
public struct EntityAlignment : IComponentData
{
    public float Moral;      // -100 (Evil) ↔ +100 (Good)
    public float Order;      // -100 (Chaotic) ↔ +100 (Lawful)
    public float Purity;    // -100 (Corrupt) ↔ +100 (Pure)
    public float Strength;  // 0..1 (how resistant to change)
}
```

**Modification Options:**
- **Direct Value Input:** Set exact values for Moral, Order, Purity (-100 to +100)
- **Preset Buttons:** Quick-set common alignments (Lawful Good, Chaotic Evil, True Neutral, etc.)
- **Slider Controls:** Adjust values with sliders for fine-tuning
- **Strength Adjustment:** Modify alignment strength (how resistant to change)

**Validation:**
- Values clamped to [-100, +100] range
- Strength clamped to [0, 1] range

---

### 2. Outlooks

**Component:** `EntityOutlook` (PureDOTS) or `VillagerOutlook` (Godgame)

**Modifiable Fields:**
```csharp
public struct EntityOutlook : IComponentData
{
    public OutlookType Primary;      // Strongest cultural lens
    public OutlookType Secondary;    // Secondary outlook
    public OutlookType Tertiary;     // Tertiary outlook
}

public enum OutlookType : byte
{
    None = 0,
    Warlike = 1,
    Peaceful = 2,
    Spiritual = 3,
    Materialistic = 4,
    Scholarly = 5,
    Pragmatic = 6,
    Xenophobic = 7,
    Egalitarian = 8,
    Authoritarian = 9
}
```

**Modification Options:**
- **Dropdown Menus:** Select Primary, Secondary, Tertiary outlooks from dropdown
- **Preset Combinations:** Quick-set common outlook combinations
- **Clear Outlook:** Set any outlook to `None` to remove it
- **Swap Outlooks:** Swap Primary ↔ Secondary, Secondary ↔ Tertiary

**Validation:**
- Outlooks cannot be duplicated (Primary ≠ Secondary ≠ Tertiary)
- At least one outlook should be set (Primary recommended)

---

### 3. Behaviors (Personality Axes)

**Component:** `PersonalityAxes` (PureDOTS) or `VillagerBehavioralPersonality` (Godgame)

**Modifiable Fields:**
```csharp
public struct PersonalityAxes : IComponentData
{
    public float VengefulForgiving;  // -100 (Vengeful) ↔ +100 (Forgiving)
    public float CravenBold;         // -100 (Craven) ↔ +100 (Bold)
    // Future axes:
    // public float TrustingParanoid;
    // public float SelfishAltruistic;
}
```

**Modification Options:**
- **Slider Controls:** Adjust VengefulForgiving and CravenBold with sliders
- **Preset Buttons:** Quick-set common personalities (Vengeful Bold, Forgiving Craven, etc.)
- **Direct Value Input:** Set exact values for each axis (-100 to +100)

**Validation:**
- Values clamped to [-100, +100] range

---

### 4. Archetypes

**Component:** `EntityArchetype` or similar (game-specific)

**Modifiable Fields:**
```csharp
// Example archetype component (game-specific)
public struct EntityArchetype : IComponentData
{
    public FixedString64Bytes ArchetypeId;  // "Warrior", "Scholar", "Merchant", etc.
    public float ArchetypeWeight;           // 0..1 (how strongly archetype applies)
    public ArchetypeFlags Flags;            // Archetype-specific flags
}
```

**Modification Options:**
- **Archetype Selection:** Choose from available archetypes (dropdown or list)
- **Weight Adjustment:** Modify how strongly archetype applies (0..1)
- **Flag Toggles:** Enable/disable archetype-specific flags
- **Clear Archetype:** Remove archetype (set to None/null)

**Note:** Archetypes are game-specific and may not exist in all games. Modification system should gracefully handle missing archetype components.

---

## Debug Flag Gating

### Debug Flag Component

```csharp
/// <summary>
/// Global debug flags for development tools.
/// </summary>
public struct DebugFlags : IComponentData
{
    public byte Flags;
    
    // Debug feature flags
    public const byte FlagEnableHeldEntityModification = 1 << 0;
    public const byte FlagEnableEntityInspector = 1 << 1;
    public const byte FlagEnablePerformanceOverlay = 1 << 2;
    // ... other debug flags
    
    public bool EnableHeldEntityModification => (Flags & FlagEnableHeldEntityModification) != 0;
}
```

### Flag Activation

**Methods:**
1. **Command Line:** `--enable-held-entity-modification`
2. **Runtime Toggle:** Debug menu checkbox (if debug menu exists)
3. **Code:** `SystemAPI.SetComponent(singletonEntity, new DebugFlags { Flags = DebugFlags.FlagEnableHeldEntityModification })`

**Default State:**
- **Disabled by default** (flag = 0)
- Modification UI/commands hidden when disabled
- Modification attempts ignored when disabled

---

## Modification Interface

### UI Requirements (When Flag Enabled)

**Display Conditions:**
- Only show when entity is held (`HeldByPlayer` component present)
- Only show when debug flag is enabled
- Hide when entity is released

**UI Elements:**
1. **Alignment Section:**
   - Moral slider (-100 to +100)
   - Order slider (-100 to +100)
   - Purity slider (-100 to +100)
   - Strength slider (0 to 1)
   - Preset buttons (Lawful Good, Chaotic Evil, etc.)

2. **Outlook Section:**
   - Primary dropdown (OutlookType enum)
   - Secondary dropdown (OutlookType enum)
   - Tertiary dropdown (OutlookType enum)
   - "Clear" buttons for each outlook
   - "Swap" buttons (Primary ↔ Secondary, etc.)

3. **Behavior Section:**
   - VengefulForgiving slider (-100 to +100)
   - CravenBold slider (-100 to +100)
   - Preset buttons (Vengeful Bold, Forgiving Craven, etc.)

4. **Archetype Section (if applicable):**
   - Archetype dropdown/list
   - Weight slider (0 to 1)
   - Flag toggles (if applicable)

5. **Action Buttons:**
   - "Apply Changes" (commit modifications)
   - "Reset to Original" (restore original values)
   - "Close" (hide UI, keep changes)

---

## Modification System

### Component Modification Flow

```csharp
[BurstCompile]
[UpdateInGroup(typeof(DebugSystemGroup))]
public partial struct HeldEntityModificationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Check if modification is enabled
        if (!SystemAPI.TryGetSingleton<DebugFlags>(out var debugFlags))
        {
            return; // No debug flags, skip
        }
        
        if (!debugFlags.EnableHeldEntityModification)
        {
            return; // Modification disabled, skip
        }
        
        // Find all held entities
        foreach (var (heldByPlayer, entity) in
            SystemAPI.Query<RefRO<HeldByPlayer>>()
                .WithEntityAccess())
        {
            // Check if entity has modification request
            if (SystemAPI.HasComponent<EntityModificationRequest>(entity))
            {
                var request = SystemAPI.GetComponent<EntityModificationRequest>(entity);
                ApplyModifications(ref state, entity, request);
                
                // Remove request after processing
                SystemAPI.SetComponent(entity, new EntityModificationRequest { IsProcessed = true });
            }
        }
    }
    
    private void ApplyModifications(ref SystemState state, Entity entity, EntityModificationRequest request)
    {
        // Modify alignment
        if (request.ModifyAlignment && SystemAPI.HasComponent<EntityAlignment>(entity))
        {
            var alignment = SystemAPI.GetComponent<EntityAlignment>(entity);
            
            if (request.SetMoral.HasValue)
                alignment.Moral = request.SetMoral.Value;
            if (request.SetOrder.HasValue)
                alignment.Order = request.SetOrder.Value;
            if (request.SetPurity.HasValue)
                alignment.Purity = request.SetPurity.Value;
            if (request.SetStrength.HasValue)
                alignment.Strength = request.SetStrength.Value;
            
            SystemAPI.SetComponent(entity, alignment);
        }
        
        // Modify outlook
        if (request.ModifyOutlook && SystemAPI.HasComponent<EntityOutlook>(entity))
        {
            var outlook = SystemAPI.GetComponent<EntityOutlook>(entity);
            
            if (request.SetPrimaryOutlook.HasValue)
                outlook.Primary = request.SetPrimaryOutlook.Value;
            if (request.SetSecondaryOutlook.HasValue)
                outlook.Secondary = request.SetSecondaryOutlook.Value;
            if (request.SetTertiaryOutlook.HasValue)
                outlook.Tertiary = request.SetTertiaryOutlook.Value;
            
            SystemAPI.SetComponent(entity, outlook);
        }
        
        // Modify behavior
        if (request.ModifyBehavior && SystemAPI.HasComponent<PersonalityAxes>(entity))
        {
            var behavior = SystemAPI.GetComponent<PersonalityAxes>(entity);
            
            if (request.SetVengefulForgiving.HasValue)
                behavior.VengefulForgiving = request.SetVengefulForgiving.Value;
            if (request.SetCravenBold.HasValue)
                behavior.CravenBold = request.SetCravenBold.Value;
            
            SystemAPI.SetComponent(entity, behavior);
        }
        
        // Modify archetype (game-specific)
        if (request.ModifyArchetype && SystemAPI.HasComponent<EntityArchetype>(entity))
        {
            var archetype = SystemAPI.GetComponent<EntityArchetype>(entity);
            
            if (request.SetArchetypeId.Length > 0)
                archetype.ArchetypeId = request.SetArchetypeId;
            if (request.SetArchetypeWeight.HasValue)
                archetype.ArchetypeWeight = request.SetArchetypeWeight.Value;
            
            SystemAPI.SetComponent(entity, archetype);
        }
    }
}

/// <summary>
/// Modification request component (temporary, removed after processing).
/// </summary>
public struct EntityModificationRequest : IComponentData
{
    public bool IsProcessed;
    
    // Alignment modifications
    public bool ModifyAlignment;
    public float? SetMoral;
    public float? SetOrder;
    public float? SetPurity;
    public float? SetStrength;
    
    // Outlook modifications
    public bool ModifyOutlook;
    public OutlookType? SetPrimaryOutlook;
    public OutlookType? SetSecondaryOutlook;
    public OutlookType? SetTertiaryOutlook;
    
    // Behavior modifications
    public bool ModifyBehavior;
    public float? SetVengefulForgiving;
    public float? SetCravenBold;
    
    // Archetype modifications
    public bool ModifyArchetype;
    public FixedString64Bytes SetArchetypeId;
    public float? SetArchetypeWeight;
}
```

---

## Original Value Preservation

### Backup Component

```csharp
/// <summary>
/// Stores original values before modification (for reset functionality).
/// </summary>
public struct EntityOriginalValues : IComponentData
{
    public EntityAlignment OriginalAlignment;
    public EntityOutlook OriginalOutlook;
    public PersonalityAxes OriginalBehavior;
    public EntityArchetype OriginalArchetype;  // If applicable
    public bool HasBackup;
}
```

**Backup Creation:**
- Created when first modification is requested
- Stores original component values
- Used for "Reset to Original" functionality

**Reset Functionality:**
```csharp
public void ResetToOriginal(ref SystemState state, Entity entity)
{
    if (!SystemAPI.HasComponent<EntityOriginalValues>(entity))
    {
        return; // No backup, cannot reset
    }
    
    var original = SystemAPI.GetComponent<EntityOriginalValues>(entity);
    
    if (SystemAPI.HasComponent<EntityAlignment>(entity))
        SystemAPI.SetComponent(entity, original.OriginalAlignment);
    if (SystemAPI.HasComponent<EntityOutlook>(entity))
        SystemAPI.SetComponent(entity, original.OriginalOutlook);
    if (SystemAPI.HasComponent<PersonalityAxes>(entity))
        SystemAPI.SetComponent(entity, original.OriginalBehavior);
    if (SystemAPI.HasComponent<EntityArchetype>(entity))
        SystemAPI.SetComponent(entity, original.OriginalArchetype);
}
```

---

## Validation and Constraints

### Alignment Validation

```csharp
public static EntityAlignment ValidateAlignment(EntityAlignment alignment)
{
    alignment.Moral = math.clamp(alignment.Moral, -100f, 100f);
    alignment.Order = math.clamp(alignment.Order, -100f, 100f);
    alignment.Purity = math.clamp(alignment.Purity, -100f, 100f);
    alignment.Strength = math.clamp(alignment.Strength, 0f, 1f);
    return alignment;
}
```

### Outlook Validation

```csharp
public static EntityOutlook ValidateOutlook(EntityOutlook outlook)
{
    // Ensure no duplicate outlooks
    if (outlook.Primary == outlook.Secondary && outlook.Primary != OutlookType.None)
    {
        outlook.Secondary = OutlookType.None;
    }
    if (outlook.Primary == outlook.Tertiary && outlook.Primary != OutlookType.None)
    {
        outlook.Tertiary = OutlookType.None;
    }
    if (outlook.Secondary == outlook.Tertiary && outlook.Secondary != OutlookType.None)
    {
        outlook.Tertiary = OutlookType.None;
    }
    
    return outlook;
}
```

### Behavior Validation

```csharp
public static PersonalityAxes ValidateBehavior(PersonalityAxes behavior)
{
    behavior.VengefulForgiving = math.clamp(behavior.VengefulForgiving, -100f, 100f);
    behavior.CravenBold = math.clamp(behavior.CravenBold, -100f, 100f);
    return behavior;
}
```

---

## Integration Points

### Hand Interaction System

- **Detection:** System checks for `HeldByPlayer` component to identify held entities
- **UI Display:** Modification UI shown when entity is held and flag is enabled
- **Release Handling:** Modification UI hidden when entity is released

### Entity Systems

- **Alignment Systems:** Modified alignments affect relation calculations, behavior selection
- **Outlook Systems:** Modified outlooks affect cultural expressions, tech preferences
- **Behavior Systems:** Modified behaviors affect risk tolerance, combat stance
- **Archetype Systems:** Modified archetypes affect starting modules, weights, caps

### Aggregate Systems

- **Aggregate Recalculation:** Modified individual entities may trigger aggregate recalculation
- **Cascade Effects:** Changes to held entity may affect aggregate averages (if entity is member)

---

## Usage Examples

### Example 1: Testing Alignment Effects

```
1. Pick up villager entity
2. Enable debug flag (--enable-held-entity-modification)
3. Modification UI appears
4. Change alignment: Moral = +80, Order = +70, Purity = +60
5. Apply changes
6. Release entity
7. Observe: Villager now behaves as Lawful Good Pure entity
8. Test: Check relations with other entities (should be more positive with Good entities)
```

### Example 2: Testing Outlook Combinations

```
1. Pick up villager entity
2. Enable debug flag
3. Modification UI appears
4. Change outlooks: Primary = Warlike, Secondary = Spiritual, Tertiary = Authoritarian
5. Apply changes
6. Release entity
7. Observe: Villager now prefers combat, values faith, respects hierarchy
8. Test: Check tech preferences (should prefer combat techs, spiritual buildings)
```

### Example 3: Testing Behavior Extremes

```
1. Pick up villager entity
2. Enable debug flag
3. Modification UI appears
4. Change behavior: VengefulForgiving = -90, CravenBold = +85
5. Apply changes
6. Release entity
7. Observe: Villager is extremely vengeful but also very bold
8. Test: Provoke entity (should seek revenge aggressively, not retreat)
```

### Example 4: Rapid Prototyping

```
1. Pick up entity
2. Enable debug flag
3. Try different alignment/outlook/behavior combinations
4. Apply changes, release, observe behavior
5. Pick up again, modify, repeat
6. Quickly iterate through many combinations to find interesting behaviors
```

---

## Performance Considerations

### System Update Frequency

- **Modification System:** Only runs when debug flag is enabled
- **Update Rate:** Once per frame (or every N frames if performance is concern)
- **Early Exit:** System exits immediately if flag is disabled

### Component Access

- **Sparse Access:** Only accesses components for held entities (small subset)
- **Batch Processing:** Can batch multiple modification requests if needed
- **Component Lookup:** Uses `SystemAPI.HasComponent` checks before modification

### UI Rendering

- **Conditional Rendering:** UI only rendered when entity is held and flag is enabled
- **Lazy Initialization:** UI components created on-demand, destroyed when not needed

---

## Security and Safety

### Debug-Only Feature

- **Default Disabled:** Feature disabled by default (requires explicit flag)
- **No Production Impact:** Should never be enabled in production builds
- **Build Guards:** Consider `#if UNITY_EDITOR || DEVELOPMENT_BUILD` guards

### Validation

- **Value Clamping:** All values clamped to valid ranges
- **Component Existence:** Checks for component existence before modification
- **Null Checks:** Validates entity existence before operations

### Reversibility

- **Original Backup:** Original values stored for reset functionality
- **Reset Capability:** "Reset to Original" button restores original values
- **No Permanent Changes:** Modifications are reversible (unless explicitly saved)

---

## Future Extensions

### Additional Modifiable Properties

- **Stats:** Modify individual stats (Physique, Finesse, Agility, Intellect, Will, Social, Faith)
- **Relations:** Modify relations with other entities (for testing)
- **Inventory:** Modify inventory items (for testing)
- **Skills:** Modify skill levels (for testing)

### Preset Library

- **Save Presets:** Save common modification combinations as presets
- **Load Presets:** Quick-load presets for rapid testing
- **Preset Sharing:** Export/import presets for team sharing

### Batch Modification

- **Multi-Entity Selection:** Modify multiple held entities at once
- **Apply to All:** Apply same modifications to all entities in selection
- **Randomize:** Randomize properties within valid ranges

---

## Related Documentation

- **Hand Interaction System:** `Docs/Concepts/Core/Hand_Anchor_Components_Summary.md` - How entities are held
- **Divine Hand State Machine:** `Docs/Mechanics/DivineHandStateMachine.md` - Hand interaction states
- **Alignment Framework:** `Docs/Concepts/Meta/Generalized_Alignment_Framework.md` - Alignment system
- **Entity Relations:** `godgame/Docs/Concepts/Implemented/Villagers/Entity_Relations_And_Interactions.md` - How alignments affect relations

---

## Summary

The Held Entity Modification System provides a debug-enabled tool for modifying entity properties (alignments, outlooks, behaviors, archetypes) while entities are held by the player. This enables rapid iteration, testing, and debugging of entity behavior without requiring game restarts or entity respawning.

**Key Features:**
1. **Debug Flag Gating:** All features disabled by default, require explicit flag
2. **Held Entity Detection:** Only modifies entities currently held by player
3. **Comprehensive Modification:** Supports alignments, outlooks, behaviors, archetypes
4. **Original Value Backup:** Stores original values for reset functionality
5. **Validation:** All values validated and clamped to valid ranges
6. **Reversible:** Modifications can be reset to original values

**Use Cases:**
- Development: Quick testing of different entity configurations
- Debugging: Reproduce specific scenarios with modified entities
- Testing: Validate behavior changes with different property combinations
- Prototyping: Rapid iteration on entity personality and cultural traits

---

**For Implementers:** Create modification UI that only appears when entity is held and debug flag is enabled. Use component modification system to apply changes safely.

**For Designers:** Use this tool to quickly test different entity configurations and observe emergent behaviors.

**For QA:** Use this tool to create specific test scenarios with modified entity properties.

---

**Last Updated:** 2025-01-21
**Status:** Concept Design - Ready for implementation





