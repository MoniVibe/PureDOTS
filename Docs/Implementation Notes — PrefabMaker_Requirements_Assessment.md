# Implementation Notes — PrefabMaker Requirements Assessment

**Project:** Space4X  
**Doc Path:** `Docs/PrefabMaker_Requirements_Assessment.md`  
**Phase:** Phase 1-3 (Core → High Value → Polish)  
**Spine:** DOTS 1.4 + PureDOTS time/rewind  
**Related Systems:** Carriers, Modules, Stations, Aggregates, Individuals, Alignment/Compliance, Construction Loop

---

## Prefab vs Data Matrix

| Thing | Prefab? | Why | Token Sockets? | Binding IDs | Notes |
|-------|---------|-----|----------------|-------------|-------|
| Hull Spec | NO | Data-driven hull definition (slots, mass, category) | N/A | HullId | Blob asset from HullCatalog SO |
| Hull Prefab | YES | Thin presentation token with sockets for module attachment | Socket_{MountType}_{Size}_{Index} | HullId | Required for module mounting |
| Module Spec | NO | Data-driven module definition (mount reqs, function, stats) | N/A | ModuleId | Blob asset from ModuleCatalog SO |
| Module Prefab | YES | Thin presentation token (visual only, no gameplay logic) | N/A | ModuleId | Optional, visual polish |
| Station Spec | NO | Data-driven station definition (refit flags, zone radius) | N/A | StationId | Blob asset from StationCatalog SO |
| Station Prefab | YES | Thin presentation token | N/A | StationId | Required for scene placement |
| Resource Spec | NO | Data-driven resource definition | N/A | ResourceId | Blob asset from ResourceCatalog SO |
| Resource Prefab | YES | Thin presentation token (pickup visual) | N/A | ResourceId | Optional, visual polish |
| Product Spec | NO | Data-driven product definition | N/A | ProductId | Blob asset from ProductCatalog SO |
| Product Prefab | YES | Thin presentation token | N/A | ProductId | Optional, visual polish |
| Aggregate Spec | NO | Data-driven aggregate definition (outlook/alignment profiles) | N/A | AggregateId | Blob asset from AggregateCatalog SO |
| Aggregate Prefab | YES | Thin presentation token (faction badge, banner) | N/A | AggregateId | Optional, UI element |
| Effect Spec | NO | Data-driven effect definition | N/A | EffectId | Blob asset from EffectCatalog SO |
| Effect Prefab | YES | Thin presentation token (VFX) | N/A | EffectId | Optional, visual FX |
| Individual Spec | NO | Data-driven individual definition (stats, traits, expertise) | N/A | IndividualId | Blob asset from IndividualCatalog SO |
| Individual Prefab | YES | Thin presentation token (portrait, model) | N/A | IndividualId | Optional, UI/visual |
| Augmentation Spec | NO | Data-driven augmentation definition | N/A | AugmentationId | Blob asset from AugmentationCatalog SO |
| Augmentation Prefab | YES | Thin presentation token (implant visual) | N/A | AugmentationId | Optional, visual polish |
| Manufacturer Spec | NO | Data-driven manufacturer definition | N/A | ManufacturerId | Blob asset from ManufacturerCatalog SO |
| Manufacturer Prefab | NO | Not needed (data-only) | N/A | N/A | Manufacturers are data, no visuals |

**Decision Rationale:**
- All specs are data (blobs). Prefabs are thin presentation tokens with sockets only where needed (hulls for module mounting).
- Individual entities, aggregates, effects are optional visuals (UI elements, VFX).
- Manufacturers are pure data (no prefabs needed).

---

## Schemas

### ECS Components (Runtime)

```csharp
// Hull Components (already exist)
public struct HullId : IComponentData { public FixedString64Bytes Value; }
public struct HullSocketTag : IComponentData { public MountType Type; public MountSize Size; }
public struct HangarCapacity : IComponentData { public int Value; }

// Module Components (already exist)
public struct ModuleId : IComponentData { public FixedString64Bytes Value; }
public struct MountRequirement : IComponentData { public MountType Type; public MountSize Size; }
public struct ModuleFunctionData : IComponentData { public ModuleFunction Function; public float Capacity; }

// Individual Entity Components (NEW - Phase 1)
public struct IndividualId : IComponentData { public FixedString64Bytes Value; }
public struct IndividualStats : IComponentData 
{ 
    public int Command, Tactics, Logistics, Diplomacy, Engineering, Resolve;
}
public struct PhysiqueFinesseWill : IComponentData 
{ 
    public int Physique, Finesse, Will;
    public byte PhysiqueInclination, FinesseInclination, WillInclination; // 1-10
    public float GeneralXP;
}
public struct ExpertiseEntry : IBufferElementData 
{ 
    public ExpertiseType Type; // CarrierCommand, Espionage, Logistics, Psionic, Beastmastery
    public byte Tier; // 0-255
}
public struct ServiceTrait : IBufferElementData 
{ 
    public FixedString32Bytes TraitId; // ReactorWhisperer, StrikeWingMentor, etc.
}
public struct PreordainProfile : IComponentData 
{ 
    public PreordainTrack Track; // CombatAce, LogisticsMaven, DiplomaticEnvoy, EngineeringSavant
}

// Module Quality/Rarity/Tier/Manufacturer (NEW - Phase 1)
public struct ModuleQuality : IComponentData { public float Value; } // 0-1
public struct ModuleRarity : IComponentData { public Rarity Value; } // Common, Uncommon, Heroic, Prototype
public struct ModuleTier : IComponentData { public byte Value; } // 0-255
public struct ModuleManufacturer : IComponentData { public FixedString64Bytes ManufacturerId; }

// Aggregate Outlook/Alignment Composition (NEW - Phase 1)
public struct AggregateType : IComponentData 
{ 
    public AggregateKind Kind; // Dynasty, Guild, Corporation, Army, Band
}
public struct AggregateOutlookProfile : IComponentData 
{ 
    public BlobAssetReference<OutlookProfileBlob> Profile;
}
public struct AggregateAlignmentProfile : IComponentData 
{ 
    public BlobAssetReference<AlignmentProfileBlob> Profile;
}
public struct AggregatePolicy : IComponentData 
{ 
    public float Aggression, TradeBias, Diplomacy;
    public byte DoctrineMissile, DoctrineLaser, DoctrineHangar;
    // ... (all policy fields from ComposedAggregateSpec)
}
```

### Blob Assets

```csharp
// Individual Spec Blob
public struct IndividualSpecBlob 
{ 
    public FixedString64Bytes Id;
    public IndividualStats Stats;
    public PhysiqueFinesseWill Attributes;
    public BlobArray<ExpertiseEntry> Expertise;
    public BlobArray<ServiceTraitEntry> Traits;
    public PreordainTrack PreordainTrack;
    public FixedString64Bytes TitleId;
    public FixedString64Bytes LineageId;
}

// Module Spec Blob (extend existing)
public struct ModuleSpecBlob 
{ 
    // ... existing fields ...
    public float Quality; // 0-1
    public Rarity Rarity;
    public byte Tier;
    public FixedString64Bytes ManufacturerId;
}

// Aggregate Spec Blob (extend existing)
public struct AggregateSpecBlob 
{ 
    // ... existing fields ...
    public FixedString64Bytes TemplateId;
    public FixedString64Bytes OutlookId;
    public FixedString64Bytes AlignmentId;
    public FixedString64Bytes PersonalityId;
    public FixedString64Bytes ThemeId;
    public AggregatePolicy Policy; // Resolved policy fields
}
```

### Authoring ScriptableObjects

```csharp
[CreateAssetMenu]
public class IndividualCatalogAuthoring : ScriptableObject 
{ 
    public List<IndividualEntryAuthoring> Individuals;
}

[Serializable]
public struct IndividualEntryAuthoring 
{ 
    public string id;
    public int command, tactics, logistics, diplomacy, engineering, resolve;
    public int physique, finesse, will;
    public byte physiqueInclination, finesseInclination, willInclination;
    public List<ExpertiseEntryAuthoring> expertise;
    public List<string> serviceTraits;
    public PreordainTrack preordainTrack;
    public string titleId;
    public string lineageId;
}

[CreateAssetMenu]
public class AugmentationCatalogAuthoring : ScriptableObject 
{ 
    public List<AugmentationEntryAuthoring> Augmentations;
}

[Serializable]
public struct AugmentationEntryAuthoring 
{ 
    public string id;
    public string slotId;
    public AugmentationType type; // Combat, Finesse, Will, General
    public float quality;
    public byte tier;
    public Rarity rarity;
    public string manufacturerId;
}

[CreateAssetMenu]
public class ManufacturerCatalogAuthoring : ScriptableObject 
{ 
    public List<ManufacturerEntryAuthoring> Manufacturers;
}

[Serializable]
public struct ManufacturerEntryAuthoring 
{ 
    public string id;
    public string name;
    public ManufacturerSignature Signature; // Fire rate, ammo type, damage profile, etc.
    public float Experience; // Manufacturer XP for legendary runs
}
```

### Baker Mapping

```csharp
public sealed class IndividualCatalogBaker : Baker<IndividualCatalogAuthoring> 
{
    public override void Bake(IndividualCatalogAuthoring src) 
    {
        using var bb = new BlobBuilder(Allocator.Temp);
        ref var root = ref bb.ConstructRoot<IndividualSpecBlob>();
        
        var arr = bb.Allocate(ref root.Entries, src.individuals.Count);
        for (int i = 0; i < src.individuals.Count; i++) 
        {
            var entry = src.individuals[i];
            arr[i] = new IndividualSpecEntry 
            { 
                Id = entry.id,
                Stats = new IndividualStats 
                { 
                    Command = entry.command,
                    Tactics = entry.tactics,
                    Logistics = entry.logistics,
                    Diplomacy = entry.diplomacy,
                    Engineering = entry.engineering,
                    Resolve = entry.resolve
                },
                Attributes = new PhysiqueFinesseWill 
                { 
                    Physique = entry.physique,
                    Finesse = entry.finesse,
                    Will = entry.will,
                    PhysiqueInclination = entry.physiqueInclination,
                    FinesseInclination = entry.finesseInclination,
                    WillInclination = entry.willInclination
                },
                // ... expertise, traits, preordain track ...
            };
        }
        
        var blob = bb.CreateBlobAssetReference<IndividualSpecBlob>(Allocator.Persistent);
        var e = GetEntity(TransformUsageFlags.None);
        AddComponent(e, new IndividualSpecRef { Blob = blob });
    }
}
```

---

## Systems & Ordering

### System Groups

**Initialization:**
- `IndividualCatalogInitializationSystem` - Creates IndividualSpecRef singleton from catalog
- `AugmentationCatalogInitializationSystem` - Creates AugmentationSpecRef singleton
- `ManufacturerCatalogInitializationSystem` - Creates ManufacturerSpecRef singleton
- `AggregateProfileResolutionSystem` - Resolves outlook/alignment profiles for aggregates

**FixedStep Simulation:**
- `IndividualStatAggregationSystem` - Aggregates individual stats to vessel/carrier level (NEW)
- `ModuleQualityApplicationSystem` - Applies quality/rarity/tier modifiers to module stats (NEW)
- `AggregatePolicyResolutionSystem` - Resolves aggregate policies from profiles (NEW)

**Presentation:**
- `IndividualVisualBindingSystem` - Binds individual visuals (portraits, models) (NEW)
- `ModuleVisualBindingSystem` - Binds module visuals (already exists, extend for quality/rarity visuals)

### Code Sketches

```csharp
[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct IndividualStatAggregationSystem : ISystem 
{
    [BurstCompile] 
    public void OnUpdate(ref SystemState s) 
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();
        
        new AggregateStatsJob { Ecb = ecb }.ScheduleParallel();
    }
    
    [BurstCompile]
    public partial struct AggregateStatsJob : IJobEntity 
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        void Execute([ChunkIndexInQuery] int ciq, Entity e, 
            in DynamicBuffer<CrewMember> crew, 
            ref VesselStats stats) 
        {
            // Aggregate Command, Tactics, Logistics, etc. from crew individuals
            // Store in VesselStats component
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct ModuleQualityApplicationSystem : ISystem 
{
    [BurstCompile] 
    public void OnUpdate(ref SystemState s) 
    {
        new ApplyQualityJob { }.ScheduleParallel();
    }
    
    [BurstCompile]
    public partial struct ApplyQualityJob : IJobEntity 
    {
        void Execute(Entity e, 
            in ModuleId id,
            in ModuleQuality quality,
            in ModuleRarity rarity,
            in ModuleTier tier,
            ref ModuleStatModifier modifier) 
        {
            // Apply quality/rarity/tier multipliers to module stats
            // Quality: 0-1 multiplier on base stats
            // Rarity: +5% per tier (Common=0%, Uncommon=5%, Heroic=10%, Prototype=15%)
            // Tier: +2% per tier level
        }
    }
}
```

**ECB Boundaries:**
- Structural changes (spawning/destroying prefabs, visual tokens) only at Begin/End Presentation via ECB.
- Simulation systems mutate components directly (stats, quality modifiers).

---

## Prefab Maker Tasks

### What to Generate

**Phase 1 (Critical - Agent A/Progression):**

1. **Individual Entity Prefabs:**
   - Generate prefabs in `Assets/Prefabs/Space4X/Individuals/Captains/`, `Officers/`, `Crew/`
   - Components: `IndividualIdAuthoring`, `IndividualStatsAuthoring`, `PhysiqueFinesseWillAuthoring`, `ExpertiseAuthoring`, `ServiceTraitsAuthoring`, `PreordainTrackAuthoring`, `TitleAuthoring`, `LineageAuthoring`
   - Visual: Placeholder capsule or portrait placeholder

2. **Module Quality/Rarity/Tier/Manufacturer:**
   - Extend `ModuleGenerator` to emit `ModuleQualityAuthoring`, `ModuleRarityAuthoring`, `ModuleTierAuthoring`, `ModuleManufacturerAuthoring`
   - Read from `ModuleCatalogAuthoring` extended fields

3. **Aggregate Outlook/Alignment Composition:**
   - Extend `AggregateGenerator` to emit `AggregateTypeAuthoring`, `AggregateOutlookProfileAuthoring`, `AggregateAlignmentProfileAuthoring`, `AggregatePolicyAuthoring`
   - Read from `AggregateCatalogAuthoring` extended fields (templateId, outlookId, alignmentId, etc.)

4. **Manufacturer Prefabs:**
   - Generate manufacturer catalog SO (no prefabs, data-only)
   - Create `ManufacturerCatalogAuthoring` with manufacturer definitions

**Phase 2 (High Value):**

5. **Facility Archetype/Tier Support:**
   - Extend `ModuleCatalogAuthoring` with `FacilityArchetype` and `FacilityTier` fields
   - Add `FacilityArchetypeAuthoring` and `FacilityTierAuthoring` components to module prefabs
   - Validate tier compatibility (Small–Massive on carriers/stations; Titanic only on megastructures)

6. **Individual Entity Relations:**
   - Add `LoyaltyScoresAuthoring`, `OwnershipStakesAuthoring`, `MentorshipAuthoring`, `PatronageWebAuthoring`, `SuccessionAuthoring` components
   - Generate relation data from catalog or templates

**Phase 3 (Polish):**

7. **Augmentation/Implant System:**
   - Create `AugmentationGenerator` for augmentation prefabs
   - Generate in `Assets/Prefabs/Space4X/Augmentations/`
   - Add `AugmentationAuthoring` component to individual entity prefabs

8. **Socket Layout Algorithm:**
   - Implement socket positioning algorithm (spherical/cylindrical distribution)
   - Allow manual override in catalog
   - Visualize sockets in editor (gizmos)

### Validation

**Catalog Validation:**
- All individual entries have valid stats (0-100 range)
- All module entries have valid quality (0-1), rarity (enum), tier (0-255)
- All aggregate entries have valid profile references (outlookId, alignmentId exist)
- All manufacturer entries have valid signature data

**Binding Validation:**
- All individual IDs have corresponding prefabs (if prefabs enabled)
- All module IDs have corresponding prefabs
- All aggregate IDs have corresponding prefabs

**Facility Tier Validation:**
- Module facility tier compatible with host hull category
- Small–Massive modules only on carriers/stations
- Titanic modules only on megastructures

**Idempotency:**
- Hash report: Catalog SO hash → blob hash → prefab hash
- CLI dry-run: `--validate-only` flag checks all catalogs without generating
- Re-run produces identical outputs (deterministic blob building, deterministic prefab generation)

---

## Determinism & Tests

### Test Names & Descriptions

1. **PrefabMaker_IndividualGeneration_EditMode** - Generates individual prefabs with correct components from catalog
2. **PrefabMaker_ModuleQualityGeneration_EditMode** - Generates module prefabs with quality/rarity/tier/manufacturer components
3. **PrefabMaker_AggregateProfileResolution_EditMode** - Resolves aggregate outlook/alignment profiles correctly
4. **PrefabMaker_Idempotency_EditMode** - Running generator twice produces identical prefab hashes
5. **IndividualStatAggregation_PlayMode** - Aggregates individual stats to vessel level correctly
6. **ModuleQualityApplication_PlayMode** - Applies quality/rarity/tier modifiers to module stats correctly
7. **Rewind_IndividualStats_PlayMode** - Individual stats rewind correctly (snapshot/restore)

**Rewind Check:**
- Individual stats, module quality, aggregate policies are components → snapshot via PureDOTS time spine
- Blob assets (IndividualSpec, ModuleSpec, AggregateSpec) are immutable → no rewind needed

---

## CI & Budgets

### Metrics to Export

**Per Scenario:**
- Prefab generation time: IndividualGenerator, ModuleGenerator, AggregateGenerator
- Prefab count: Generated prefabs per catalog
- Blob size: IndividualSpec blob, ModuleSpec blob, AggregateSpec blob
- Validation time: Catalog validation, binding validation

### Budgets

- **IndividualGenerator**: < 100ms per 1000 individuals
- **ModuleGenerator**: < 50ms per 1000 modules
- **AggregateGenerator**: < 50ms per 1000 aggregates
- **Total prefab generation**: < 5s for full catalog regeneration

### Gates

- Prefab generation exceeds 10s → fail CI
- Validation finds errors → fail CI
- Blob size exceeds 100MB → warn

---

## Risks & "Why Invalid" Checks

### Lint Rules

1. **Individual Stats Validation:**
   - Rule: `command`, `tactics`, etc. in range 0-100
   - Message: "Individual '{id}' has invalid stat '{stat}': {value} (must be 0-100)"

2. **Module Quality Validation:**
   - Rule: `quality` in range 0-1, `rarity` is valid enum, `tier` in range 0-255
   - Message: "Module '{id}' has invalid quality/rarity/tier: quality={quality}, rarity={rarity}, tier={tier}"

3. **Aggregate Profile Reference:**
   - Rule: `outlookId`, `alignmentId` reference existing profiles
   - Message: "Aggregate '{id}' references missing profile: {profileId}"

4. **Facility Tier Compatibility:**
   - Rule: Module facility tier compatible with host hull category
   - Message: "Module '{moduleId}' with tier {tier} incompatible with hull '{hullId}' category {category}"

5. **Socket Count Mismatch:**
   - Rule: Hull prefab socket count matches catalog slot count
   - Message: "Hull '{id}' prefab has {prefabSockets} sockets but catalog specifies {catalogSlots}"

6. **Manufacturer Reference:**
   - Rule: Module `manufacturerId` references existing manufacturer
   - Message: "Module '{id}' references missing manufacturer: {manufacturerId}"

### Design Risks

**Risk: Individual Stat Aggregation Performance**
- **Mitigation:** Cache aggregated stats, update only when crew changes
- **Fallback:** Aggregate stats only on-demand (not every tick)

**Risk: Module Quality Bloat**
- **Mitigation:** Store quality/rarity/tier as single packed struct (4 bytes total)
- **Fallback:** Remove quality if performance issues

**Risk: Aggregate Profile Resolution Complexity**
- **Mitigation:** Resolve profiles at bake time, store resolved policy in component
- **Fallback:** Simple tag-based aggregates (no profile composition)

---

## TODO(Design) Items

1. **Individual XP Progression:** How does XP accumulate/spend? Default: Defer to future system.
2. **Service Trait Application:** How do traits modify stats? Default: Simple multiplier per trait.
3. **Preordain Track Guidance:** How does preordain track guide career? Default: Defer to future system.
4. **Augmentation Installation:** How are augments installed at runtime? Default: Defer to future system.
5. **Manufacturer Legendary Runs:** How are legendary production runs triggered? Default: Defer to future system.

---

**End of Implementation Notes — PrefabMaker Requirements Assessment**

