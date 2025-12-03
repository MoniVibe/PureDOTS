# PureDOTS Integration & Extension Specification

**Note**: This document is superseded by `INTEGRATION_GUIDE.md` for day-to-day development. This spec remains for detailed extension procedures.

**Purpose**: Detailed reference for extending PureDOTS framework

This document serves as the single source of truth for:
- How game projects (Space4X, Godgame, etc.) integrate with PureDOTS systems
- How to extend PureDOTS with new features while maintaining game agnosticism
- Coordination guidelines for cross-project development

---

## Table of Contents

1. [Core Principles](#core-principles)
2. [Interface Patterns](#interface-patterns)
3. [Extension Conventions](#extension-conventions)
4. [Requesting Extensions](#requesting-extensions)
5. [Coordination Guidelines](#coordination-guidelines)
6. [Quick Reference](#quick-reference)

---

## Core Principles

### Game Agnosticism

PureDOTS must remain **game-agnostic**. All game-specific logic, themes, and content belong in game projects, not in PureDOTS core.

**Rules:**
- ❌ **Never** hardcode game-specific types (e.g., `MinerVessel`, `Carrier`) in PureDOTS
- ✅ **Use** generic tags and configurable data (e.g., `TransportUnitTag`, `LessonId` fields)
- ❌ **Never** use conditional compilation (`#if SPACE4X`, `#if GODGAME`) for game logic
- ✅ **Provide** extension points (enums, config fields, tag components) for games to customize

**Example Violations:**
```csharp
// ❌ BAD: Game-specific type in PureDOTS
if (entityManager.HasComponent<MinerVessel>(entity)) { ... }

// ✅ GOOD: Generic tag in PureDOTS
if (entityManager.HasComponent<TransportUnitTag>(entity)) { ... }
```

### Determinism & Rewind Compatibility

All PureDOTS systems must be **deterministic** and **rewind-safe**.

**Rules:**
- All systems must check `RewindState.Mode` before mutating state
- Systems should skip execution during `RewindMode.Playback`
- Use fixed-point math for deterministic calculations
- Avoid non-deterministic operations (random without seed, `Time.time`, etc.)

**Pattern:**
```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return; // Skip during playback
        
        // ... deterministic logic ...
    }
}
```

### Pure DOTS Architecture

PureDOTS uses **pure DOTS patterns** - no MonoBehaviour service locators.

**Rules:**
- Use singleton components + `DynamicBuffer<T>` for entity collections (Registry pattern)
- Use `ComponentLookup<T>` for efficient component queries
- Avoid `WorldServices` or static service locators
- Prefer `ISystem` (unmanaged) over `SystemBase` (managed) when possible

**Registry Pattern:**
```csharp
// Singleton component
public struct MyRegistry : IComponentData { }

// Buffer of entries
public struct MyEntry : IBufferElementData
{
    public Entity Entity;
    public int Index;
}

// System rebuilds registry each tick
[UpdateInGroup(typeof(MySystemGroup))]
public partial struct MyRegistrySystem : ISystem
{
    // Rebuilds buffer each tick
}
```

### Burst & IL2CPP Safety

PureDOTS systems should be **Burst-compatible** and **IL2CPP-safe**.

**Rules:**
- Use `[BurstCompile]` on systems and jobs
- Avoid reflection in jobs
- Use `FixedString` types instead of `string`
- Use `BlobAssetReference<T>` for large read-only data
- Avoid managed types in component data

---

## Interface Patterns

### Component Tags

PureDOTS uses **tag components** (empty structs implementing `IComponentData`) to mark entities for system processing.

**Common Tags:**
- `TransportUnitTag` - Marks transport entities (ships, wagons, caravans)
- `VillagerId` - Marks villager entities
- `MiracleDefinition` - Marks miracle entities
- `ResourceSourceConfig` - Marks resource nodes

**Usage:**
```csharp
using PureDOTS.Runtime.Mobility;

// In authoring component or system
entityManager.AddComponent<TransportUnitTag>(shipEntity);
```

**Adding New Tags:**
1. Create tag struct in appropriate `Runtime/Runtime/` folder
2. Add to relevant system's `ComponentLookup<T>`
3. Document in this guide

### AI Sensor Categories

The AI sensor system detects entities by category. Games can configure sensors to detect specific categories.

**Available Categories** (`AISensorCategory` enum):
- `None` (0) - No category
- `Villager` (1) - Villager entities
- `ResourceNode` (2) - Resource source entities
- `Storehouse` (3) - Storage building entities
- `TransportUnit` (4) - Transport entities (ships, wagons)
- `Miracle` (5) - Miracle entities
- `Custom0` (240) - Reserved for game-specific categories

**Configuring Sensors:**
```csharp
var sensorConfig = new AISensorConfig
{
    UpdateInterval = 0.5f,
    Range = 50f,
    MaxResults = 8,
    PrimaryCategory = AISensorCategory.TransportUnit,
    SecondaryCategory = AISensorCategory.ResourceNode
};
entityManager.AddComponent(entity, sensorConfig);
```

**Adding New Categories:**
1. Add enum value to `AISensorCategory` (use `Custom0`-`Custom15` for game-specific)
2. Update `MatchesCategory` in `AISensorCategoryFilter` (see [Extension Conventions](#adding-new-sensor-categories))
3. Add required `ComponentLookup<T>` to `AISensorUpdateSystem`

### Registry Integration

PureDOTS uses **registries** (singleton + buffer pattern) for entity collections.

**Available Registries:**
- `ResourceRegistry` + `DynamicBuffer<ResourceEntry>`
- `VillagerRegistry` + `DynamicBuffer<VillagerEntry>`
- `StorehouseRegistry` + `DynamicBuffer<StorehouseEntry>`
- `MiracleRegistry` + `DynamicBuffer<MiracleEntry>`
- `LogisticsRequestRegistry` + `DynamicBuffer<LogisticsRequestEntry>`
- `ConstructionRegistry` + `DynamicBuffer<ConstructionEntry>`

**How Entities Register:**
1. Add appropriate component (e.g., `ResourceSourceConfig`, `VillagerId`)
2. Registry system automatically picks up entity
3. Entry added to registry buffer each tick

**Querying Registries:**
```csharp
var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
var entries = SystemAPI.GetBuffer<ResourceEntry>(registryEntity);
for (int i = 0; i < entries.Length; i++)
{
    var entry = entries[i];
    // Use entry.Entity, entry.Index, etc.
}
```

### System Group Integration

PureDOTS organizes systems into **system groups**. Game-specific systems should use appropriate groups.

**Root Groups:**
- `InitializationSystemGroup` - Core singleton seeding
- `FixedStepSimulationSystemGroup` - Deterministic 60 FPS simulation
- `SimulationSystemGroup` - Variable-rate simulation
- `PresentationSystemGroup` - Visual updates

**Domain Groups:**
- `TimeSystemGroup` - Time state, tick advancement, rewind coordination
- `VillagerSystemGroup` - Villager AI, needs, jobs, movement
- `ResourceSystemGroup` - Resource gathering, processing, storage
- `EnvironmentSystemGroup` - Climate, moisture, temperature, wind
- `SpatialSystemGroup` - Spatial grid rebuilds, queries
- `AISystemGroup` - Shared AI pipeline (sensors, utility, steering)
- `HandSystemGroup` - Divine hand input, cursor, miracles
- `VegetationSystemGroup` - Plant growth, health, reproduction
- `ConstructionSystemGroup` - Building sites, progress
- `CombatSystemGroup` - Combat resolution
- `MiracleEffectSystemGroup` - Miracle effects

**Using System Groups:**
```csharp
[UpdateInGroup(typeof(VillagerSystemGroup))]
[UpdateAfter(typeof(VillagerNeedsSystem))]
public partial struct MyVillagerSystem : ISystem
{
    // Runs in VillagerSystemGroup after VillagerNeedsSystem
}
```

**Update Order Attributes:**
- `[UpdateBefore(typeof(OtherSystem))]` - Run before another system
- `[UpdateAfter(typeof(OtherSystem))]` - Run after another system
- `[UpdateInGroup(typeof(SystemGroup))]` - Place in system group

### Configuration Patterns

PureDOTS uses **data-driven configuration** via component fields.

**Common Patterns:**
- `FixedString64Bytes` fields for IDs (e.g., `LessonId`, `ResourceId`)
- `float` fields for rates/timings (e.g., `GatherRatePerWorker`, `RespawnSeconds`)
- `byte` flags for boolean options (e.g., `ResourceSourceConfig.Flags`)

**Example: Resource Lesson Configuration**
```csharp
var resourceConfig = new ResourceSourceConfig
{
    GatherRatePerWorker = 10f,
    MaxSimultaneousWorkers = 3,
    RespawnSeconds = 60f,
    LessonId = new FixedString64Bytes("lesson.harvest.iron_ore"), // Configurable lesson
    Flags = ResourceSourceConfig.FlagRespawns
};
```

**Best Practices:**
- Use consistent naming conventions for IDs (e.g., `"lesson.harvest.*"`)
- Document ID formats in game project documentation
- Empty IDs should be treated as "no effect" (e.g., empty `LessonId` = no learning)

### Spatial Grid Integration

Many PureDOTS systems rely on the **spatial grid** for proximity queries.

**Requirements:**
- Entities must have `LocalTransform` component (for position)
- `SpatialGridResidency` component added automatically by spatial systems

**Querying Spatial Grid:**
```csharp
var queryHelper = SystemAPI.GetSingleton<SpatialQueryHelper>();
var results = new NativeList<Entity>(Allocator.Temp);
queryHelper.QueryRadius(position, radius, ref results);
// Use results...
results.Dispose();
```

**When to Use Spatial vs. Registry:**
- **Spatial queries**: Position/radius-based searches (AI sensors, targeting, selection)
- **Registry queries**: Type/category-based searches (job assignment, resource gathering)
- **Combined**: Spatial query to narrow candidates, then filter by registry/component

### AI Pipeline Integration

PureDOTS provides a **shared AI pipeline** that can be consumed by any entity type.

**Pipeline Stages:**
1. **Sensors** (`AISensorUpdateSystem`) - Samples spatial grid, categorizes entities
2. **Scoring** (`AIUtilityScoringSystem`) - Evaluates actions using utility curves
3. **Steering** (`AISteeringSystem`) - Computes movement direction
4. **Task Resolution** (`AITaskResolutionSystem`) - Emits commands to domain systems

**Opting Into AI Pipeline:**
```csharp
// Add sensor config
entityManager.AddComponent(entity, new AISensorConfig
{
    UpdateInterval = 0.5f,
    Range = 10f,
    MaxResults = 8,
    PrimaryCategory = AISensorCategory.ResourceNode
});

// Add sensor state and buffer
entityManager.AddComponent(entity, new AISensorState());
entityManager.AddBuffer<AISensorReading>(entity);

// Add behaviour archetype (blob baked from ScriptableObject)
entityManager.AddComponent(entity, new AIBehaviourArchetype
{
    UtilityBlob = utilityBlobReference
});

// Add utility state
entityManager.AddComponent(entity, new AIUtilityState());

// Add steering config and state
entityManager.AddComponent(entity, new AISteeringConfig
{
    MaxSpeed = 5f,
    Acceleration = 10f,
    Responsiveness = 0.1f,
    DegreesOfFreedom = 2 // 2D planar movement
});
entityManager.AddComponent(entity, new AISteeringState());
entityManager.AddComponent(entity, new AITargetState());
```

**Consuming AI Commands:**
```csharp
var commands = SystemAPI.GetBuffer<AICommand>(queueEntity);
for (int i = 0; i < commands.Length; i++)
{
    var cmd = commands[i];
    // Execute action based on cmd.ActionIndex
    // Use cmd.TargetEntity and cmd.TargetPosition
}
```

See `Docs/Guides/AI_Integration_Guide.md` for comprehensive AI integration details.

---

## Extension Conventions

### When to Extend PureDOTS vs. Keep in Game Project

**Extend PureDOTS when:**
- Feature is **generic** and reusable across multiple games
- Feature provides **infrastructure** (systems, registries, utilities)
- Feature is **game-agnostic** (no game-specific themes/content)

**Keep in Game Project when:**
- Feature is **game-specific** (themes, content, rules)
- Feature is **content** (prefabs, assets, data)
- Feature is **presentation** (visuals, audio, UI)

**Examples:**
- ✅ **PureDOTS**: Generic `TransportUnitTag`, configurable `LessonId` field
- ❌ **Game Project**: Specific ship types (`MinerVessel`), hardcoded lesson mappings

### Adding New Components

**Naming Conventions:**
- `*Tag` - Empty tag components (e.g., `TransportUnitTag`)
- `*Config` - Configuration data (e.g., `ResourceSourceConfig`)
- `*State` - Runtime state (e.g., `AISensorState`)

**Placement:**
- Place in appropriate `Runtime/Runtime/` folder
- Group related components in same file (e.g., `MobilityComponents.cs`)

**Required Attributes:**
```csharp
[BurstCompile] // If used in Burst jobs
[Serializable] // If serialized in authoring
public struct MyComponent : IComponentData
{
    // Use FixedString instead of string
    public FixedString64Bytes MyId;
    // Use value types (float, int, byte, etc.)
    public float MyValue;
}
```

### Adding New Systems

**System Group Placement:**
- Choose appropriate system group (see [System Group Integration](#system-group-integration))
- Use `[UpdateInGroup(typeof(MySystemGroup))]` attribute
- Use `[UpdateBefore]` / `[UpdateAfter]` for ordering

**Rewind Guard Pattern:**
```csharp
[BurstCompile]
[UpdateInGroup(typeof(MySystemGroup))]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return; // Skip during playback
        
        // ... deterministic logic ...
    }
}
```

**Component Lookup Pattern:**
```csharp
[BurstCompile]
public partial struct MyJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<MyConfig> ConfigLookup;
    public ComponentLookup<MyState> StateLookup;
    
    public void Execute(Entity entity, ref MyComponent comp)
    {
        if (!ConfigLookup.HasComponent(entity))
            return;
        
        var config = ConfigLookup[entity];
        var state = StateLookup[entity];
        // ... logic ...
    }
}
```

### Adding New Sensor Categories

**Steps:**
1. **Add enum value** to `AISensorCategory` in `Packages/com.moni.puredots/Runtime/Runtime/AI/AIComponents.cs`
   ```csharp
   public enum AISensorCategory : byte
   {
       // ... existing categories ...
       MyNewCategory = 6, // Use next available number
       Custom0 = 240 // Reserved for game-specific (240-255)
   }
   ```

2. **Add ComponentLookup** to `AISensorCategoryFilter` and `AISensorUpdateSystem` in `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs`
   ```csharp
   internal struct AISensorCategoryFilter : ISpatialQueryFilter
   {
       // ... existing lookups ...
       [ReadOnly] public ComponentLookup<MyNewComponent> MyNewLookup;
   }
   ```

3. **Implement MatchesCategory case** in `AISensorCategoryFilter.Accept`
   ```csharp
   case AISensorCategory.MyNewCategory:
       if (MyNewLookup.HasComponent(entry.Entity))
       {
           return true;
       }
       break;
   ```

4. **Update system OnCreate** to initialize lookup
   ```csharp
   filter.MyNewLookup = SystemAPI.GetComponentLookup<MyNewComponent>(true);
   ```

5. **Update system OnUpdate** to update lookup
   ```csharp
   filter.MyNewLookup.Update(ref state);
   ```

**Documentation:**
- Add category to [AI Sensor Categories](#ai-sensor-categories) section
- Document required component type

### Adding New Job Types

**Steps:**
1. **Add enum value** to `VillagerJobType` (or equivalent) in appropriate component file
   ```csharp
   public enum VillagerJobType : byte
   {
       Gather = 0,
       Build = 1,
       Craft = 2,
       Combat = 3,
       MyNewJob = 4
   }
   ```

2. **Add behavior method** to `VillagerJobBehaviors` in `Packages/com.moni.puredots/Runtime/Runtime/Villager/JobBehaviors.cs`
   ```csharp
   public static void ExecuteMyNewJob(
       Entity entity,
       ref VillagerJob job,
       ref VillagerJobTicket ticket,
       ref VillagerJobProgress progress,
       // ... other parameters ...
       float deltaTime,
       uint currentTick)
   {
       // Implement job logic
   }
   ```

3. **Update switch statement** in `VillagerJobExecutionSystem.ExecuteJobJob` in `Packages/com.moni.puredots/Runtime/Systems/VillagerJobSystems.cs`
   ```csharp
   switch (job.Type)
   {
       case VillagerJobType.Gather:
           VillagerJobBehaviors.ExecuteGather(...);
           break;
       case VillagerJobType.MyNewJob:
           VillagerJobBehaviors.ExecuteMyNewJob(...);
           break;
   }
   ```

**Documentation:**
- Document job behavior in `JobBehaviors.cs` XML comments
- Update any job-related documentation

### Adding New Registries

**Steps:**
1. **Create registry singleton** component
   ```csharp
   public struct MyRegistry : IComponentData { }
   ```

2. **Create entry buffer element**
   ```csharp
   public struct MyEntry : IBufferElementData
   {
       public Entity Entity;
       public int Index;
       // ... other fields ...
   }
   ```

3. **Create registry system** that rebuilds buffer each tick
   ```csharp
   [UpdateInGroup(typeof(MySystemGroup))]
   public partial struct MyRegistrySystem : ISystem
   {
       public void OnUpdate(ref SystemState state)
       {
           var registryEntity = SystemAPI.GetSingletonEntity<MyRegistry>();
           var entries = SystemAPI.GetBuffer<MyEntry>(registryEntity);
           entries.Clear();
           
           // Query entities and populate entries
           foreach (var (entity, index) in SystemAPI.Query<Entity, MyComponent>().WithEntityAccess())
           {
               entries.Add(new MyEntry { Entity = entity, Index = index });
           }
       }
   }
   ```

4. **Register in RegistryDirectory** (see existing registry systems for pattern)

**Documentation:**
- Add registry to [Registry Integration](#registry-integration) section
- Document entry structure and usage

---

## Requesting Extensions

**Where to Submit Requests:**

Game teams should implement PureDOTS extensions directly when needed. See [Extension Conventions](#extension-conventions) for guidelines.

**Request Process:**

1. **Implement extension directly** following PureDOTS patterns
   - File name format: `YYYY-MM-DD-{short-description}.md`
   - Example: `2025-01-27-custom-sensor-categories.md`

2. **Follow extension conventions** (see [Extension Conventions](#extension-conventions)):
   - Use case (what game feature needs this)
   - Proposed solution (specific extension point needed)
   - Impact assessment (files/systems affected)
   - Example usage code
   - Alternative approaches considered

3. **Set status** to `[PENDING]` in the request document

4. **Commit and push** to the PureDOTS repository

5. **PureDOTS team** will review, approve/reject, and implement approved requests

**Request Status Labels:**
- `[PENDING]` - Awaiting review
- `[APPROVED]` - Approved, awaiting implementation
- `[IN PROGRESS]` - Currently being implemented
- `[COMPLETED]` - Implemented and merged
- `[REJECTED]` - Not approved (with reason)
- `[DEFERRED]` - Approved but deferred to future milestone

**See Also:**
- `Docs/INTEGRATION_GUIDE.md` - Quick reference for PureDOTS integration

**When to Request:**
- Before adding game-specific code to PureDOTS
- When existing extension points don't meet your needs
- When proposing new extension points (enums, config fields, tags)
- When unsure if a feature belongs in PureDOTS vs. game project

---

## Coordination Guidelines

### Communication

**Request Submission Process:**

Game teams should submit PureDOTS extension requests via:

1. **GitHub Issues** (Preferred)
   - Create an issue in the PureDOTS repository
   - Use the `[PureDOTS Extension]` label/tag in the title
   - Include:
     - **Use Case**: What game feature requires this extension
     - **Proposed Solution**: Specific extension point needed (new tag, enum value, config field, etc.)
     - **Impact**: Which systems/components would be affected
     - **Example Code**: Pseudo-code showing how the extension would be used
   
   **Example Issue Title:**
   ```
   [PureDOTS Extension] Add Custom0-15 sensor categories for game-specific entity detection
   ```

2. **Direct Communication** (For urgent/time-sensitive requests)
   - Contact PureDOTS maintainers directly
   - Follow up with GitHub issue for tracking

3. **Documentation Updates** (For minor clarifications)
   - Submit PR updating this guide with clarifications
   - Use for documenting discovered patterns or edge cases

**When to Submit a Request:**
- Before adding game-specific code to PureDOTS
- When proposing new extension points (enums, config fields, tags)
- When breaking changes are needed
- When unsure if a feature belongs in PureDOTS vs. game project
- When existing extension points don't meet your needs

**Request Review Process:**
- PureDOTS maintainers review requests for:
  - Game agnosticism (must be reusable across games)
  - Architectural fit (aligns with PureDOTS patterns)
  - Implementation feasibility
- Approved requests are prioritized and assigned
- Implementation follows patterns in this guide

### Documentation

**What to Document:**
- New extension points (enums, config fields, tags)
- Breaking changes (component changes, system renames)
- New patterns or conventions
- Migration notes for existing code

**Where to Document:**
- Update this guide (`PUREDOTS_INTEGRATION_SPEC.md`)
- Add examples to relevant sections
- Update `ORIENTATION_SUMMARY.md` if architecture changes

### Breaking Changes

**Handling Breaking Changes:**
- Document in this guide's "Migration Notes" section
- Provide migration examples (before/after code)
- Coordinate with game teams before implementing
- Consider deprecation period if possible

**Example Migration Note:**
```markdown
### Migration: Transport Detection (2025-01-27)

**Breaking Change**: Transport detection now uses `TransportUnitTag` instead of specific component types.

**Before:**
```csharp
#if SPACE4X_TRANSPORT
if (entityManager.HasComponent<MinerVessel>(entity)) { ... }
#endif
```

**After:**
```csharp
if (entityManager.HasComponent<TransportUnitTag>(entity)) { ... }
```

**Action Required**: Add `TransportUnitTag` to all transport entities in game projects.
```

### Versioning Notes

**Changelog Expectations:**
- Document significant changes in this guide
- Update version number and "Last Updated" date
- Add entries to relevant sections (Migration Notes, Extension Conventions)

**Version Format:**
- Major.Minor (e.g., 1.0, 1.1)
- Increment major for breaking changes
- Increment minor for new features/extensions

---

## Quick Reference

### Checklist: Adding a New Entity Type

- [ ] Create tag component (if needed for system detection)
- [ ] Add to appropriate registry (or create new registry)
- [ ] Add to spatial grid (ensure `LocalTransform` exists)
- [ ] Configure AI sensors (if entity should be detectable)
- [ ] Document in this guide

### Checklist: Adding a New System

- [ ] Choose appropriate system group
- [ ] Add `[UpdateInGroup]` attribute
- [ ] Add `[UpdateBefore]` / `[UpdateAfter]` if needed
- [ ] Implement rewind guard pattern
- [ ] Use `ComponentLookup<T>` for component queries
- [ ] Add `[BurstCompile]` if possible
- [ ] Document in this guide

### Checklist: Extending AI Sensors

- [ ] Add enum value to `AISensorCategory`
- [ ] Add `ComponentLookup<T>` to `AISensorCategoryFilter`
- [ ] Implement `MatchesCategory` case
- [ ] Update system `OnCreate` and `OnUpdate`
- [ ] Document category and required component

### Checklist: Adding a New Job Type

- [ ] Add enum value to `VillagerJobType`
- [ ] Add behavior method to `VillagerJobBehaviors`
- [ ] Update switch in `VillagerJobExecutionSystem`
- [ ] Document job behavior

### Common Pitfalls

**Pitfall**: Adding game-specific types to PureDOTS
- **Solution**: Use generic tags and configurable data instead

**Pitfall**: Forgetting rewind guard
- **Solution**: Always check `RewindState.Mode` before mutating state

**Pitfall**: Using `string` instead of `FixedString`
- **Solution**: Use `FixedString64Bytes` or `FixedString128Bytes` in components

**Pitfall**: Missing `[BurstCompile]` on systems
- **Solution**: Add `[BurstCompile]` to enable Burst compilation

**Pitfall**: Not updating lookups in `OnUpdate`
- **Solution**: Call `lookup.Update(ref state)` each frame

### Critical DOTS Patterns (Priority Ordered)

**P0: Verify Dependencies Before Writing Consumer Code**
```csharp
// Before writing a system that uses TimeState, GuildMember.ContributionScore, etc.:
// 1. Search: grep -r "struct TimeState" --include="*.cs"
// 2. If not found: CREATE IT FIRST or flag as blocker
// 3. Never assume types exist based on design docs alone
```

**P1: Buffer/Collection Mutation - NEVER Mutate Foreach Variables**
```csharp
// ❌ WRONG - C# doesn't allow mutating foreach iteration variables (CS1654/CS1657)
foreach (var item in buffer)
{
    item.Value = 5;  // COMPILE ERROR
}

// ✅ CORRECT - Use indexed access for mutation
for (int i = 0; i < buffer.Length; i++)
{
    var item = buffer[i];
    item.Value = 5;
    buffer[i] = item;
}

// ✅ ALSO CORRECT - Use ref if available
for (int i = 0; i < buffer.Length; i++)
{
    ref var item = ref buffer.ElementAt(i);
    item.Value = 5;
}
```

**P1: Blob Storage Access - ALWAYS Use Ref**
```csharp
// ❌ WRONG - Copies blob data, triggers EA0001
var weapons = catalog.Value.Weapons;
var lessonCatalog = lessonCatalogRef.Blob.Value;

// ✅ CORRECT - References blob directly
ref var weapons = ref catalog.Value.Weapons;
ref var lessonCatalog = ref lessonCatalogRef.Blob.Value;

// ❌ WRONG - Blob types in out parameters, triggers EA0009
bool TryFindProjectileSpec(out ProjectileSpec spec) { ... }

// ✅ CORRECT - Use ref parameters for blob types
bool TryFindProjectileSpec(ref ProjectileSpec spec) { ... }
// Or use 'in' for read-only access
float ExtractDamage(in ProjectileSpec spec) { ... }
```

**P2: Type Conversion - Explicit Casts for Storage Types**
```csharp
// Component uses byte for memory efficiency, code uses enum for type safety
public struct AvoidanceState : IComponentData { public byte ModeRaw; }

// ❌ WRONG - Implicit conversion fails (CS0266)
state.ModeRaw = AvoidanceMode.Flee;

// ✅ CORRECT - Explicit cast
state.ModeRaw = (byte)AvoidanceMode.Flee;
var mode = (AvoidanceMode)state.ModeRaw;
```

**P2: EntityManager vs EntityCommandBuffer**
```csharp
// ❌ WRONG - EntityManager.SetComponent doesn't exist for managed components
EntityManager.SetComponent(entity, managedComponent);

// ✅ CORRECT - Use EntityCommandBuffer for structural changes
var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
ecb.CreateCommandBuffer(state.WorldUnmanaged).SetComponent(entity, component);
```

**P3: IJobParallelFor Implementation**
```csharp
// ❌ WRONG - Job struct doesn't implement required interface
public struct MyJob { ... }
myJob.Schedule(count, batchSize, dependency); // CS0315

// ✅ CORRECT - Implement IJobParallelFor
public struct MyJob : IJobParallelFor
{
    public void Execute(int index) { ... }
}
```

### Pre-Commit Verification Checklist

Before marking any task complete, agents MUST:

1. **Build Check**: Run `dotnet build` or trigger Unity domain reload
2. **Dependency Check**: Verify all referenced types/properties exist:
   ```bash
   grep -r "struct TypeName" --include="*.cs"
   grep -r "PropertyName" --include="*.cs" | grep -v "//"
   ```
3. **Pattern Check**: Review code for:
   - [ ] No foreach mutation of buffer elements
   - [ ] All blob access uses `ref`
   - [ ] Explicit casts for enum↔byte conversions
   - [ ] Rewind guard on all mutating systems
4. **Integration Check**: If working in parallel with other agents, verify:
   - [ ] Shared types are created before consumers
   - [ ] Component properties match consumer expectations
   - [ ] Enum values exist before switch statements reference them

---

## See Also

- `Docs/ORIENTATION_SUMMARY.md` - PureDOTS architecture overview
- `Docs/FoundationGuidelines.md` - Foundation development guidelines
- `Docs/INTEGRATION_GUIDE.md` - Quick reference for PureDOTS integration
- `Docs/DesignNotes/SystemIntegration.md` - System integration patterns
- `Docs/Guides/AI_Integration_Guide.md` - Comprehensive AI integration guide
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` - AI sensor system reference
- `Packages/com.moni.puredots/Runtime/Runtime/Villager/JobBehaviors.cs` - Job behaviors reference

---

**This document should be updated as PureDOTS evolves and new extension patterns emerge.**

