# Game Integration Guide

This guide explains how game projects (Space4X, Godgame, etc.) should interface with PureDOTS to ensure proper integration with the framework's systems.

## Overview

PureDOTS is designed to be game-agnostic. Game-specific logic should remain in game projects, while PureDOTS provides generic simulation infrastructure. This document outlines the patterns and requirements for integrating game entities with PureDOTS systems.

## Transport Unit Detection

### Purpose
The AI sensor system can detect transport units (ships, wagons, caravans, etc.) for pathfinding and interaction logic.

### Integration Steps

1. **Add the Transport Unit Tag**
   Add `TransportUnitTag` component to any entity that should be detected as a transport unit:

   ```csharp
   using PureDOTS.Runtime.Mobility;
   
   // In your authoring component or system
   entityManager.AddComponent<TransportUnitTag>(shipEntity);
   ```

2. **AI Sensor Configuration**
   Configure AI sensors to detect transport units by setting `AISensorConfig.PrimaryCategory` or `SecondaryCategory` to `AISensorCategory.TransportUnit`:

   ```csharp
   var sensorConfig = new AISensorConfig
   {
       PrimaryCategory = AISensorCategory.TransportUnit,
       Range = 50f,
       // ... other config
   };
   ```

### Example: Space4X Ships

For Space4X ships (MinerVessel, Carrier, Hauler, Freighter, Wagon), add `TransportUnitTag` to each ship prefab or entity:

```csharp
// In Space4X ship authoring or bootstrap
public void Bake(Baker<ShipAuthoring> baker)
{
    var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
    // ... other components
    
    // Add transport tag for AI detection
    baker.AddComponent<TransportUnitTag>(entity);
}
```

## Resource Lesson Configuration

### Purpose
When villagers harvest resources, they can learn lessons (knowledge progression). The lesson taught is now configurable per resource rather than hardcoded.

### Integration Steps

1. **Configure Resource Source**
   Set the `LessonId` field on `ResourceSourceConfig` to specify what lesson harvesting this resource teaches:

   ```csharp
   var resourceConfig = new ResourceSourceConfig
   {
       GatherRatePerWorker = 10f,
       MaxSimultaneousWorkers = 3,
       RespawnSeconds = 60f,
       LessonId = new FixedString64Bytes("lesson.harvest.iron_ore"), // Configure lesson here
       Flags = ResourceSourceConfig.FlagRespawns
   };
   ```

2. **Lesson ID Format**
   Use a consistent naming convention for lesson IDs. Examples:
   - `"lesson.harvest.iron_ore"`
   - `"lesson.harvest.legendary_alloy"`
   - `"lesson.harvest.ironoak"`
   - `"lesson.harvest.general"` (fallback)

3. **Empty Lesson ID**
   If `LessonId` is empty (default), villagers will not learn any lesson from harvesting that resource. This is useful for resources that don't provide knowledge progression.

### Example: Space4X Resources

For Space4X resources, configure the lesson ID in the resource authoring:

```csharp
// In Space4X resource authoring
public class MineralDepositAuthoring : MonoBehaviour
{
    public string LessonId = "lesson.harvest.iron_ore";
    
    void Bake(Baker<MineralDepositAuthoring> baker)
    {
        var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
        
        var config = new ResourceSourceConfig
        {
            GatherRatePerWorker = 15f,
            MaxSimultaneousWorkers = 5,
            RespawnSeconds = 120f,
            LessonId = new FixedString64Bytes(LessonId), // Use configured lesson
            Flags = ResourceSourceConfig.FlagRespawns
        };
        
        baker.AddComponent(entity, config);
    }
}
```

## General Integration Patterns

### Component Tags
PureDOTS uses tag components (empty structs implementing `IComponentData`) to mark entities for system processing. Examples:
- `TransportUnitTag` - Marks transport entities
- `VillagerId` - Marks villager entities
- `MiracleDefinition` - Marks miracle entities

### Component Lookups
Systems use `ComponentLookup<T>` for efficient component queries. Game projects should:
- Ensure required components exist on entities before systems run
- Use authoring components to add tags/components at bake time
- Avoid runtime component addition/removal unless necessary

### Spatial Grid Integration
Many PureDOTS systems rely on the spatial grid for proximity queries. Ensure entities have:
- `LocalTransform` component (for position)
- `SpatialGridResidency` component (added automatically by spatial systems)

### System Groups
PureDOTS organizes systems into groups. Game-specific systems should:
- Use appropriate system groups (`VillagerSystemGroup`, `ResourceSystemGroup`, etc.)
- Respect update order attributes (`[UpdateBefore]`, `[UpdateAfter]`)
- Follow the deterministic simulation pattern (check `RewindState.Mode`)

## Migration Notes

### From Space4X-Specific Code
If migrating from code that used `#if SPACE4X_TRANSPORT`:

1. **Remove conditional compilation** - No longer needed
2. **Add TransportUnitTag** - Add to all transport entities
3. **Update resource configs** - Set `LessonId` on `ResourceSourceConfig`

### Breaking Changes
- `ResourceSourceConfig` now includes `LessonId` field (defaults to empty)
- Transport detection now requires `TransportUnitTag` instead of specific component types
- Hardcoded lesson mapping removed (must configure `LessonId` per resource)

## Best Practices

1. **Use Authoring Components** - Configure tags and components at authoring time, not runtime
2. **Consistent Naming** - Use consistent lesson ID naming conventions across your game
3. **Documentation** - Document your lesson IDs and transport unit types in your game's documentation
4. **Testing** - Verify AI sensors detect your transport units and villagers learn lessons correctly

## Troubleshooting

### Transport Units Not Detected
- Verify `TransportUnitTag` is added to the entity
- Check `AISensorConfig` includes `AISensorCategory.TransportUnit`
- Ensure entity has `LocalTransform` and is registered in spatial grid

### Lessons Not Learned
- Verify `ResourceSourceConfig.LessonId` is set (not empty)
- Check lesson ID exists in your knowledge system
- Ensure `KnowledgeLessonEffectBlob` is configured correctly

## See Also

- `Docs/PUREDOTS_INTEGRATION_SPEC.md` - **Canonical integration & extension specification** (comprehensive guide covering interface patterns and extension process)
- `Docs/FOUNDATION_GAPS_QUICK_REFERENCE.md` - Foundation gap priorities
- `Docs/ORIENTATION_SUMMARY.md` - PureDOTS architecture overview
- `Packages/com.moni.puredots/Runtime/Runtime/MobilityComponents.cs` - Transport tag definition
- `Packages/com.moni.puredots/Runtime/Runtime/ResourceComponents.cs` - Resource config definition

**Note**: This guide is being absorbed into `PUREDOTS_INTEGRATION_SPEC.md`. For new integrations, refer to the canonical spec.

