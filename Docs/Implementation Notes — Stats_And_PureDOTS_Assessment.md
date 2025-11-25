# Implementation Notes — Stats And PureDOTS Assessment

**Project:** Godgame  
**Doc Path:** `Docs/Stats_And_PureDOTS_Assessment.md`  
**Phase:** Implementation Complete — Future Expansion  
**Spine:** DOTS 1.4 + PureDOTS time/rewind  
**Related Systems:** Villagers, Stats, Registry Bridge, Time Integration, Spatial Grid, Telemetry

---

## Prefab vs Data Matrix

| Thing | Prefab? | Why | Token Sockets? | Binding IDs | Notes |
|-------|---------|-----|----------------|-------------|-------|
| Villager Template | NO | Data-driven villager definition (stats, personality, needs) | N/A | VillagerId | Blob asset from VillagerTemplate SO |
| Villager Prefab | YES | Thin presentation token (model, portrait) | N/A | VillagerId | Required for scene placement |
| Stat Component | NO | Runtime ECS component (attributes, combat stats, needs) | N/A | N/A | Pure data, no prefab |
| Stat Spec | NO | Template stat definition (base values, modifiers) | N/A | N/A | Part of VillagerTemplate blob |
| Registry Mirror | NO | PureDOTS registry bridge component | N/A | N/A | Data-only, no prefab |
| Telemetry Metric | NO | Runtime telemetry data | N/A | N/A | Buffer element, no prefab |

**Decision Rationale:**
- All stats are data (components). Villager prefabs are thin presentation tokens.
- Registry mirrors and telemetry are pure data (no prefabs needed).
- Stat specs are part of template blobs, not separate entities.

---

## Schemas

### ECS Components (Runtime)

```csharp
// Core Attributes (already exist)
public struct VillagerAttributes : IComponentData 
{ 
    public int Physique, Finesse, Will, Wisdom; // 0-100
}

// Derived Attributes (already exist)
public struct VillagerDerivedAttributes : IComponentData 
{ 
    public int Strength, Agility, Intelligence; // 0-100
}

// Social Stats (already exist)
public struct VillagerSocialStats : IComponentData 
{ 
    public float Fame, Wealth, Reputation, Glory, Renown;
}

// Combat Stats (already exist)
public struct VillagerCombatStats : IComponentData 
{ 
    public int Attack, Defense, MaxHealth, CurrentHealth;
    public int Stamina, CurrentStamina;
    public int MaxMana, CurrentMana;
    public float AttackDamage, AttackSpeed;
    public Entity CurrentTarget;
}

// Needs (already exist)
public struct VillagerNeeds : IComponentData 
{ 
    public float Food, Rest, Sleep, GeneralHealth;
    public float Health, MaxHealth, Energy; // PureDOTS compatibility
}

// Mood (already exist)
public struct VillagerMood : IComponentData 
{ 
    public float Mood; // 0-100
}

// Resistances (already exist)
public struct VillagerResistances : IComponentData 
{ 
    public float Physical, Fire, Cold, Poison, Magic, Lightning, Holy, Dark;
}

// Modifiers (already exist)
public struct VillagerModifiers : IComponentData 
{ 
    public float HealBonus, SpellDurationModifier, SpellIntensityModifier;
}

// Personality (already exist)
public struct VillagerPersonality : IComponentData 
{ 
    public sbyte VengefulScore, BoldScore; // -100 to +100
}

// Alignment (already exist)
public struct VillagerAlignment : IComponentData 
{ 
    public sbyte MoralAxis, OrderAxis, PurityAxis; // -100 to +100
}

// Outlook (already exist)
public struct VillagerOutlook : IComponentData 
{ 
    public byte OutlookTypes; // Flags
    public BlobArray<float> OutlookValues;
    public byte FanaticFlags;
}

// Limbs (already exist)
public struct VillagerLimb : IBufferElementData 
{ 
    public FixedString64Bytes LimbId;
    public float Health;
    public byte InjuryFlags;
}

// Implants (already exist)
public struct VillagerImplant : IBufferElementData 
{ 
    public FixedString64Bytes ImplantId;
    public FixedString64Bytes AttachedToLimb;
    public float Quality;
}

// Registry Mirror (already exists)
public struct GodgameVillager : IComponentData 
{ 
    public FixedString64Bytes VillagerId;
    public float Health, MaxHealth, Energy;
    public float Mood;
    public float AttackDamage, AttackSpeed;
    public Entity CurrentTarget;
}
```

### Blob Assets

```csharp
// Villager Template Blob (already exists)
public struct VillagerTemplateBlob 
{ 
    public FixedString64Bytes Id;
    public VillagerAttributes Attributes;
    public VillagerDerivedAttributes DerivedAttributes;
    public VillagerSocialStats SocialStats;
    public VillagerCombatStats BaseCombatStats;
    public VillagerNeeds BaseNeeds;
    public VillagerResistances Resistances;
    public VillagerModifiers Modifiers;
    public VillagerPersonality Personality;
    public VillagerAlignment Alignment;
    public VillagerOutlook Outlook;
    public BlobArray<LimbEntry> Limbs;
    public BlobArray<ImplantEntry> Implants;
}
```

### Authoring ScriptableObjects

```csharp
[CreateAssetMenu]
public class VillagerTemplate : ScriptableObject 
{ 
    // All stat fields from IndividualTemplate
    public int physique, finesse, will, wisdom;
    public int strength, agility, intelligence;
    public float fame, wealth, reputation, glory, renown;
    public int baseAttack, baseDefense, baseHealth, baseStamina, baseMana;
    public float food, rest, sleep, generalHealth;
    public Dictionary<string, float> resistances;
    public float healBonus, spellDurationModifier, spellIntensityModifier;
    public sbyte vengefulScore, boldScore;
    public string alignmentId;
    public List<string> outlookIds;
    public List<LimbReference> limbs;
    public List<ImplantReference> implants;
    public bool isUndead, isSummoned;
}
```

### Baker Mapping

```csharp
public sealed class VillagerAuthoringBaker : Baker<VillagerAuthoring> 
{
    public override void Bake(VillagerAuthoring src) 
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        
        // Add all stat components from template
        AddComponent(entity, new VillagerAttributes 
        { 
            Physique = src.template.physique,
            Finesse = src.template.finesse,
            Will = src.template.will,
            Wisdom = src.template.wisdom
        });
        
        AddComponent(entity, new VillagerDerivedAttributes 
        { 
            Strength = src.template.strength,
            Agility = src.template.agility,
            Intelligence = src.template.intelligence
        });
        
        // ... (all other stat components) ...
        
        // Add PureDOTS compatibility components (for registry bridge)
        AddComponent(entity, new VillagerNeeds 
        { 
            Health = src.template.baseHealth,
            MaxHealth = src.template.baseHealth,
            Energy = src.template.baseStamina
        });
        
        AddComponent(entity, new VillagerMood { Mood = 50f });
        
        AddComponent(entity, new VillagerCombatStats 
        { 
            AttackDamage = 0f,
            AttackSpeed = 1f,
            CurrentTarget = Entity.Null
        });
    }
}
```

---

## Systems & Ordering

### System Groups

**Initialization:**
- `VillagerStatInitializationSystem` - Ensures stats have valid defaults (already exists)
- `VillagerPureDOTSSyncSystem` - Syncs Godgame components to PureDOTS components (already exists)

**FixedStep Simulation:**
- `VillagerStatCalculationSystem` - Calculates derived combat stats from attributes (already exists)
- `VillagerNeedsSystem` - Manages need decay over time (already exists)
- `GodgameVillagerSyncSystem` - Syncs villager data to registry mirror (already exists)

**Presentation:**
- `VillagerVisualBindingSystem` - Binds villager visuals (model, portrait) (already exists)

### Code Sketches

```csharp
[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(VillagerStatCalculationSystem))]
public partial struct VillagerPureDOTSSyncSystem : ISystem 
{
    [BurstCompile] 
    public void OnUpdate(ref SystemState s) 
    {
        new SyncJob { }.ScheduleParallel();
    }
    
    [BurstCompile]
    public partial struct SyncJob : IJobEntity 
    {
        void Execute(Entity e, 
            in VillagerNeeds needs,
            in VillagerMood mood,
            in VillagerCombatStats combat,
            ref PureDOTSVillagerNeeds pureDOTSNeeds,
            ref PureDOTSVillagerMood pureDOTSMood,
            ref PureDOTSVillagerCombatStats pureDOTSCombat) 
        {
            // Sync Godgame components to PureDOTS components
            pureDOTSNeeds.Health = needs.Health;
            pureDOTSNeeds.MaxHealth = needs.MaxHealth;
            pureDOTSNeeds.Energy = needs.Energy;
            
            pureDOTSMood.Mood = mood.Mood;
            
            pureDOTSCombat.AttackDamage = combat.AttackDamage;
            pureDOTSCombat.AttackSpeed = combat.AttackSpeed;
            pureDOTSCombat.CurrentTarget = combat.CurrentTarget;
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct VillagerStatCalculationSystem : ISystem 
{
    [BurstCompile] 
    public void OnUpdate(ref SystemState s) 
    {
        new CalculateStatsJob { }.ScheduleParallel();
    }
    
    [BurstCompile]
    public partial struct CalculateStatsJob : IJobEntity 
    {
        void Execute(Entity e, 
            in VillagerAttributes attrs,
            ref VillagerDerivedAttributes derived,
            ref VillagerCombatStats combat) 
        {
            // Calculate derived attributes
            derived.Strength = (int)(attrs.Physique * 0.8f + combat.Attack * 0.2f);
            derived.Agility = (int)(attrs.Finesse * 0.8f + combat.AttackSpeed * 0.2f);
            derived.Intelligence = (int)(attrs.Will * 0.6f + attrs.Wisdom * 0.4f);
            
            // Calculate combat stats (if auto-calc)
            if (combat.Attack == 0) 
                combat.Attack = (int)(attrs.Physique * 0.5f + attrs.Finesse * 0.3f + attrs.Will * 0.2f);
            
            if (combat.Defense == 0) 
                combat.Defense = (int)(attrs.Physique * 0.4f + attrs.Finesse * 0.4f + attrs.Will * 0.2f);
        }
    }
}
```

**ECB Boundaries:**
- Structural changes (spawning/destroying villagers, visual tokens) only at Begin/End Presentation via ECB.
- Simulation systems mutate components directly (stats, needs, mood).

---

## Prefab Maker Tasks

### What to Generate

**Villager Prefabs (Already Complete):**
- Generate prefabs in `Assets/Prefabs/Godgame/Villagers/`
- Components: `VillagerIdAuthoring`, all stat authoring components from template
- Visual: Placeholder capsule or villager model

**Stat Component Generation (Already Complete):**
- `PrefabGenerator.GenerateIndividualPrefab` transfers all template stats to authoring component
- All stat categories covered: Attributes, Derived Attributes, Social Stats, Combat Stats, Needs, Resistances, Modifiers, Personality, Alignment, Outlook, Limbs, Implants

### Validation

**Template Validation:**
- All stat values in valid ranges (0-100 for attributes, -100 to +100 for personality/alignment)
- All resistance values in valid range (0-1)
- All modifier values non-negative

**Component Validation:**
- All required stat components present on villager entities
- PureDOTS compatibility components present (for registry bridge)

**Idempotency:**
- Hash report: Template SO hash → blob hash → prefab hash
- CLI dry-run: `--validate-only` flag checks all templates without generating
- Re-run produces identical outputs (deterministic blob building, deterministic prefab generation)

---

## Determinism & Tests

### Test Names & Descriptions

1. **VillagerStats_ComponentCreation_EditMode** - All stat components created correctly from template
2. **VillagerStatCalculation_AutoCalc_EditMode** - Auto-calculation produces correct derived stats
3. **VillagerStatCalculation_Override_EditMode** - Override values respected (non-zero = override)
4. **VillagerNeeds_Decay_EditMode** - Needs decay over time correctly
5. **VillagerPureDOTSSync_RegistryBridge_PlayMode** - PureDOTS components sync correctly for registry bridge
6. **Rewind_VillagerStats_PlayMode** - Villager stats rewind correctly (snapshot/restore)

**Rewind Check:**
- All stat components support rewind via PureDOTS time spine
- Registry mirror components snapshot/restore correctly
- Telemetry metrics are not rewound (one-way stream)

---

## CI & Budgets

### Metrics to Export

**Per Scenario:**
- Stat calculation time: VillagerStatCalculationSystem
- Needs decay time: VillagerNeedsSystem
- Sync time: VillagerPureDOTSSyncSystem
- Villager entity count: Active villagers in world
- Registry bridge time: GodgameVillagerSyncSystem

### Budgets

- **VillagerStatCalculationSystem**: < 0.5ms per 1000 villagers
- **VillagerNeedsSystem**: < 1ms per 1000 villagers
- **VillagerPureDOTSSyncSystem**: < 0.5ms per 1000 villagers
- **Villager entity count**: < 10,000 active villagers
- **Registry bridge time**: < 2ms per 1000 villagers

### Gates

- Stat calculation exceeds 2ms → fail CI
- Needs decay exceeds 5ms → fail CI
- Villager entity count exceeds 20,000 → warn
- Registry bridge time exceeds 5ms → warn

---

## Risks & "Why Invalid" Checks

### Lint Rules

1. **Stat Range Validation:**
   - Rule: Attributes in range 0-100, personality/alignment in range -100 to +100
   - Message: "Villager '{id}' has invalid stat '{stat}': {value} (must be {min}-{max})"

2. **Resistance Range Validation:**
   - Rule: Resistances in range 0-1
   - Message: "Villager '{id}' has invalid resistance '{type}': {value} (must be 0-1)"

3. **Modifier Validation:**
   - Rule: Modifiers non-negative
   - Message: "Villager '{id}' has invalid modifier '{mod}': {value} (must be >= 0)"

4. **PureDOTS Component Missing:**
   - Rule: Villager entities have PureDOTS compatibility components for registry bridge
   - Message: "Villager '{id}' missing PureDOTS compatibility components (add VillagerNeeds, VillagerMood, VillagerCombatStats)"

5. **Template Reference Missing:**
   - Rule: VillagerAuthoring references valid VillagerTemplate
   - Message: "VillagerAuthoring references missing template: {templateId}"

### Design Risks

**Risk: Stat Calculation Performance**
- **Mitigation:** Cache calculated stats, update only when attributes change
- **Fallback:** Simple stat = attribute (no derived calculation)

**Risk: Needs Decay Performance**
- **Mitigation:** Batch needs decay, update only active villagers
- **Fallback:** Coarse needs decay (update every N ticks)

**Risk: Registry Bridge Overhead**
- **Mitigation:** Sync only changed stats, batch sync operations
- **Fallback:** Reduce sync frequency (every N ticks)

---

## TODO(Design) Items

1. **Social Stats Tracking:** How do social stats change? Default: Defer to future system (Wealth_And_Social_Dynamics).
2. **Resistance Application:** How are resistances applied in combat? Default: Defer to future system (Individual_Combat_System).
3. **Modifier Application:** How are modifiers applied in healing/spells? Default: Defer to future system (Miracle_System_Vision).
4. **Limb System:** How are limbs injured/damaged? Default: Defer to future system (Individual_Combat_System).
5. **XP Progression:** How does XP accumulate/spend? Default: Defer to future system (Individual_Progression_System).

---

## PureDOTS Compliance Summary

### Registry Bridge Pattern
- ✅ Mirror components (`GodgameVillager`) implemented
- ✅ Sync systems (`GodgameVillagerSyncSystem`) implemented
- ✅ Bridge system (`GodgameRegistryBridgeSystem`) uses `DeterministicRegistryBuilder<T>`
- ✅ Registry directory properly registers buffers
- ✅ Telemetry publishes metrics via `TelemetryStream`

### Time Integration
- ✅ Uses PureDOTS `TimeState` and `RewindState` singletons
- ✅ `TimeControlSystem` processes commands without forking time systems
- ✅ No custom time systems (follows PureDOTS time spine)

### Spatial Grid Integration
- ✅ Uses `SpatialGridResidency` and `SpatialIndexedTag` from PureDOTS
- ✅ `GodgameSpatialIndexingSystem` adds spatial tags to runtime-spawned entities
- ✅ Registry bridge marks continuity with `CellId`/`SpatialVersion`

### Telemetry Integration
- ✅ Publishes metrics via `TelemetryStream` singleton buffer
- ✅ Uses `TelemetryMetric` elements with proper keys/units
- ✅ Batches metrics per frame
- ✅ No managed allocations in telemetry systems

### Component Integration
- ✅ `VillagerPureDOTSSyncSystem` syncs Godgame components to PureDOTS components
- ✅ PureDOTS compatibility components added during baking
- ✅ Registry bridge reads PureDOTS components correctly

**Overall PureDOTS Compliance: 6/6 compliant (100%)**

---

**End of Implementation Notes — Stats And PureDOTS Assessment**

