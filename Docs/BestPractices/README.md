# PureDOTS Best Practices

**Purpose**: Implementation-friendly guides for DOTS 1.4.x, C# 9, Unity Input System, and performance optimization.
**Audience**: Developers implementing PureDOTS systems
**Last Updated**: 2025-12-01

---

## ⚠️ CRITICAL: Project Separation

**Before writing any code, understand the tri-project structure:**

| Project | Path | Purpose | Code Location |
|---------|------|---------|---------------|
| **PureDOTS** | `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` | **Shared framework package** | `Packages/com.moni.puredots/` |
| **Space4X** | `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` | **Carrier 4X game** | `Assets/Projects/Space4X/` |
| **Godgame** | `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` | **God-game simulation** | `Assets/Projects/Godgame/` |

### Rules

**✅ PureDOTS Framework (Game-Agnostic):**
- Shared DOTS infrastructure (time, registries, spatial, telemetry)
- Generic systems usable by any game
- **NO game-specific types** (no Space4X carriers, no Godgame villagers)
- Located in `Packages/com.moni.puredots/`

**✅ Game Projects (Game-Specific):**
- Space4X: Mining, carriers, fleet combat, modules
- Godgame: Villagers, miracles, divine hand, villages
- Located in respective game project directories

**❌ NEVER:**
- Put game-specific code in PureDOTS workspace
- Reference Space4X/Godgame types from PureDOTS
- Create game folders in PureDOTS (`Assets/Projects/Space4X/` in PureDOTS is wrong!)

**See:** [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) for complete project overview and separation rules.

---

## Overview

This directory contains **practical, implementation-ready guides** covering:

✅ **DOTS 1.4.x** - Version-specific patterns and gotchas
✅ **C# 9** - New language features for ECS development
✅ **Unity Input System** - Integration with deterministic simulation
✅ **Burst Compilation** - Optimization techniques
✅ **Job System** - Parallel execution patterns
✅ **Memory Layout** - Cache-friendly component design
✅ **Entity Command Buffers** - Structural change patterns
✅ **Component Design** - Sizing and splitting strategies
✅ **Determinism** - Rewind-safe development checklist

---

## Quick Navigation

### ⚠️ Start Here: Project Context

- **[PROJECT_SEPARATION.md](PROJECT_SEPARATION.md)** ⚠️ **READ FIRST** - Understand PureDOTS vs Space4X vs Godgame separation

### Core Implementation Guides

1. **[DOTS_1_4_Patterns.md](DOTS_1_4_Patterns.md)** ⭐ **IMPLEMENTATION GUIDE**
   - ISystem vs SystemBase
   - SystemAPI.Query patterns
   - Aspect-oriented queries
   - Baker improvements
   - Entity relationships
   - Source generators

2. **[CSharp9_Features.md](CSharp9_Features.md)** ✅ Complete
   - Records for DTOs
   - Init-only setters
   - Pattern matching in systems
   - Target-typed new
   - Function pointers (Burst-compatible)

3. **[UnityInputSystem_ECS.md](UnityInputSystem_ECS.md)** ✅ Complete
   - Input System architecture
   - Command component pattern
   - Deterministic input handling
   - Multiplayer patterns
   - Testing input

### Performance Optimization

4. **[BurstOptimization.md](BurstOptimization.md)** ✅ Complete
   - Burst compilation checklist
   - SIMD vectorization
   - Common errors & fixes
   - Burst Inspector workflow

5. **[JobSystemPatterns.md](JobSystemPatterns.md)** ✅ Complete
   - IJob vs IJobChunk vs IJobEntity
   - ParallelWriter usage
   - Dependency optimization
   - Safety system tips

6. **[MemoryLayoutOptimization.md](MemoryLayoutOptimization.md)** ✅ Complete
   - Hot/medium/cold component sizing
   - Cache-line awareness (64-byte boundaries)
   - Companion entity pattern
   - Archetype design for chunk utilization

### Advanced Patterns

7. **[EntityCommandBuffers.md](EntityCommandBuffers.md)** ✅ Complete
   - When to use ECB vs EntityManager
   - Playback timing strategies
   - ParallelWriter patterns
   - Debugging ECB issues

8. **[ComponentDesignPatterns.md](ComponentDesignPatterns.md)** ✅ Complete
   - Component sizing guidelines
   - Tag vs data components
   - Shared components for config
   - Enable/Disable patterns

9. **[DeterminismChecklist.md](DeterminismChecklist.md)** ✅ Complete
   - Fixed timestep requirements
   - RNG seeding patterns
   - Float precision considerations
   - Rewind-safe system patterns

---

## How to Use These Guides

### For New Developers

**Onboarding Path:**
1. Read [FoundationGuidelines.md](../FoundationGuidelines.md) (coding standards)
2. Read [DOTS_1_4_Patterns.md](DOTS_1_4_Patterns.md) (implementation patterns)
3. Skim other guides as needed for specific tasks
4. Reference while coding (keep open in browser/editor)

### For Experienced Developers

**Reference Usage:**
- **Before implementing**: Check relevant best practice guide
- **During code review**: Verify patterns match guidelines
- **When debugging**: Consult "Common Pitfalls" sections
- **When optimizing**: Review performance guides

### For Code Reviews

**Checklist:**
- ✅ Follows DOTS 1.4.x patterns (ISystem, SystemAPI.Query)
- ✅ Components sized appropriately (hot < 128 bytes)
- ✅ Burst-compiled where applicable
- ✅ Deterministic (fixed timestep, seeded RNG)
- ✅ Proper ECB usage (ParallelWriter for parallel jobs)
- ✅ No managed allocations in hot paths

---

## Document Status

| Guide | Status | Priority | Notes |
|-------|--------|----------|-------|
| PROJECT_SEPARATION.md | ✅ Complete | **Critical** | ⚠️ Read first - prevents mixups |
| DOTS_1_4_Patterns.md | ✅ Complete | High | Foundation guide |
| CSharp9_Features.md | ✅ Complete | Medium | C# language features |
| UnityInputSystem_ECS.md | ✅ Complete | High | Critical for input handling |
| BurstOptimization.md | ✅ Complete | High | Performance critical |
| JobSystemPatterns.md | ✅ Complete | Medium | Parallel execution |
| MemoryLayoutOptimization.md | ✅ Complete | High | Cache optimization |
| EntityCommandBuffers.md | ✅ Complete | Medium | Structural changes |
| ComponentDesignPatterns.md | ✅ Complete | High | Component design |
| DeterminismChecklist.md | ✅ Complete | High | Rewind/replay safety |

**All guides complete!** See [FoundationGuidelines.md](../FoundationGuidelines.md) and [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) for additional coding patterns.

---

## Contributing to Best Practices

### Adding New Guides

1. **Identify need**: Is this pattern used across multiple systems?
2. **Draft outline**: Cover overview, patterns, examples, pitfalls
3. **Write content**: Code examples > theory
4. **Review with team**: Get feedback from domain experts
5. **Add to this README**: Update navigation and status

### Updating Existing Guides

**Update when:**
- ✅ Unity/DOTS version changes
- ✅ New pattern discovered by team
- ✅ Common pitfall identified
- ✅ Performance improvement found

**Update process:**
1. Edit guide markdown
2. Update "Last Updated" date
3. Note changes in git commit
4. Notify team via standup/chat

---

## Related Documentation

### Foundation Documents
- [PROJECT_SEPARATION.md](PROJECT_SEPARATION.md) ⚠️ **CRITICAL** - PureDOTS vs Space4X vs Godgame separation
- [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) - Critical DOTS coding patterns and tri-project overview
- [FoundationGuidelines.md](../FoundationGuidelines.md) - Core coding standards (P0-P17 patterns)
- [PUREDOTS_INTEGRATION_SPEC.md](../PUREDOTS_INTEGRATION_SPEC.md) - Integration guide

### Performance Documentation
- [PERFORMANCE_PLAN.md](../PERFORMANCE_PLAN.md) - Scale targets and budgets
- [QA/PerformanceBudgets.md](../QA/PerformanceBudgets.md) - Specific budgets
- [Guides/ComponentMigrationGuide.md](../Guides/ComponentMigrationGuide.md) - Hot/cold splitting

### Architecture Documentation
- [Architecture/PureDOTS_As_Framework.md](../Architecture/PureDOTS_As_Framework.md)
- [PresentationBridgeArchitecture.md](../PresentationBridgeArchitecture.md)

### Design Notes (Framework)
- [PureDOTS Framework DesignNotes](../../Packages/com.moni.puredots/Documentation/DesignNotes/)

---

## Pattern Examples by Category

### System Authoring

```csharp
// ISystem (default)
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var data in SystemAPI.Query<RefRW<MyData>>())
        {
            // Burst-compiled hot path
        }
    }
}

// SystemBase (special cases)
public partial class ManagedSystem : SystemBase
{
    private List<Thing> _managedState;

    protected override void OnUpdate()
    {
        // Managed code when necessary
    }
}
```

### Query Patterns

```csharp
// Basic query
foreach (var (transform, velocity) in
    SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
{
    transform.ValueRW.Position += velocity.ValueRO.Value;
}

// With filters
foreach (var ai in
    SystemAPI.Query<RefRW<AIState>>()
        .WithAll<EnemyTag>()
        .WithNone<Disabled>())
{
    // Only enabled enemies
}

// With entity access
foreach (var (health, entity) in
    SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
{
    if (health.ValueRO.Current <= 0)
    {
        state.EntityManager.DestroyEntity(entity);
    }
}
```

### Component Sizing

```csharp
// Hot path (< 128 bytes)
public struct VillagerNeedsHot : IComponentData
{
    public byte Hunger;     // 0-100
    public byte Thirst;     // 0-100
    public byte Energy;     // 0-100
}

// Cold path (companion entity)
public struct VillagerColdData : IComponentData
{
    public FixedString64Bytes Name;
    public long BirthTick;
    public FixedString128Bytes Biography;
}

// Reference to companion
public struct VillagerColdRef : IComponentData
{
    public Entity CompanionEntity;
}
```

### Deterministic Patterns

```csharp
// Use fixed timestep
float deltaTime = SystemAPI.Time.DeltaTime;  // Fixed

// Use seeded RNG
var rng = new Unity.Mathematics.Random(seed);
float randomValue = rng.NextFloat();

// Check rewind state
if (SystemAPI.GetSingleton<RewindState>().Mode == RewindMode.Record)
{
    // Only mutate during record
}
```

---

## Common Questions

### Q: ISystem or SystemBase?

**A:** Default to `ISystem` (Burst-compiled, no managed overhead). Only use `SystemBase` if you need managed state (List, Dictionary, GameObject references).

### Q: How do I query with EnableableComponent?

**A:** Queries filter enabled by default. Use `WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)` to include disabled.

### Q: What's the max component size for hot paths?

**A:** Target < 128 bytes. Use companion entities for larger cold data.

### Q: How do I ensure determinism?

**A:** Use fixed timestep, seeded RNG, avoid Unity APIs in simulation, check rewind state before mutations.

### Q: When should I use aspects?

**A:** When multiple systems access the same component groups with shared helper logic.

### Q: How do I avoid archetype fragmentation?

**A:** Batch entity creation by archetype. Pre-create archetypes with `CreateArchetype()`.

---

## Maintenance Schedule

**Quarterly Review** (Every 3 months):
- Check for Unity/DOTS version updates
- Review new patterns discovered by team
- Update examples with latest APIs
- Archive obsolete patterns

**Next Review**: 2025-03-01

---

## Feedback & Suggestions

**Found an issue?** Open a ticket or discuss in team chat.
**Have a pattern to add?** Submit a draft to this directory.
**Want to improve a guide?** PRs welcome!

---

*Maintainer: PureDOTS Framework Team*
*Last Updated: 2025-12-01*
