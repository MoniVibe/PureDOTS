# Implementation Notes — Item System Summary For Advisor

**Project:** Godgame  
**Doc Path:** `Docs/Item_System_Summary_For_Advisor.md`  
**Phase:** Phase 3 Implementation — Architecture Review  
**Spine:** DOTS 1.4 + PureDOTS time/rewind  
**Related Systems:** Materials, Equipment, Tools, Buildings, Production Chains, Quality/Rarity/Tech Tier

---

## Prefab vs Data Matrix

| Thing | Prefab? | Why | Token Sockets? | Binding IDs | Notes |
|-------|---------|-----|----------------|-------------|-------|
| Material Spec | NO | Data-driven material definition (traits, stats, usage) | N/A | MaterialId | Blob asset from MaterialCatalog SO |
| Material Prefab | YES | Thin presentation token (pickup visual, icon) | N/A | MaterialId | Optional, visual polish |
| Equipment Spec | NO | Data-driven equipment definition (stats, material reqs) | N/A | EquipmentId | Blob asset from EquipmentCatalog SO |
| Equipment Prefab | YES | Thin presentation token (weapon/armor visual) | N/A | EquipmentId | Optional, visual polish |
| Tool Spec | NO | Data-driven tool definition (production inputs, quality derivation) | N/A | ToolId | Blob asset from ToolCatalog SO |
| Tool Prefab | YES | Thin presentation token (tool visual) | N/A | ToolId | Optional, visual polish |
| Building Spec | NO | Data-driven building definition (health, desirability, materials) | N/A | BuildingId | Blob asset from BuildingCatalog SO |
| Building Prefab | YES | Thin presentation token with sockets for material attachment | Socket_Material_{Index} | BuildingId | Required for scene placement |
| Production Recipe | NO | Data-driven recipe definition (inputs, outputs, time, workforce) | N/A | RecipeId | Blob asset from RecipeCatalog SO |
| Production Recipe Prefab | NO | Not needed (data-only) | N/A | N/A | Recipes are data, no visuals |
| Quality Profile | NO | Data-driven quality calculation (weights, min/max bounds) | N/A | N/A | Part of ToolSpec blob |
| Rarity Enum | NO | Data enum (Common, Uncommon, Rare, Epic, Legendary) | N/A | N/A | Part of item spec |
| Tech Tier | NO | Data value (0-10) gates availability | N/A | N/A | Part of item spec |
| Material Attribute | NO | Data-driven attribute definition (name, value, skill threshold) | N/A | AttributeId | Part of MaterialSpec blob |

**Decision Rationale:**
- All specs are data (blobs). Prefabs are thin presentation tokens.
- Production recipes are pure data (no prefabs needed).
- Quality/rarity/tech tier are data fields, not separate entities.

---

## Schemas

### ECS Components (Runtime)

```csharp
// Material Components
public struct MaterialId : IComponentData { public FixedString64Bytes Value; }
public struct MaterialSpecRef : IComponentData { public BlobAssetReference<MaterialSpec> Blob; }
public struct MaterialQuality : IComponentData { public float Value; } // 0-100
public struct MaterialPurity : IComponentData { public float Value; } // 0-100, for extracted materials
public struct MaterialRarity : IComponentData { public Rarity Value; } // Common, Uncommon, Rare, Epic, Legendary
public struct MaterialTechTier : IComponentData { public byte Value; } // 0-10

// Equipment Components
public struct EquipmentId : IComponentData { public FixedString64Bytes Value; }
public struct EquipmentSpecRef : IComponentData { public BlobAssetReference<EquipmentSpec> Blob; }
public struct EquipmentQuality : IComponentData { public float Value; } // 0-100, derived from materials
public struct EquipmentRarity : IComponentData { public Rarity Value; } // Derived from materials + craftsman
public struct EquipmentTechTier : IComponentData { public byte Value; } // Required tier to craft/use
public struct EquipmentStats : IComponentData 
{ 
    public float Damage, Armor, BlockChance, CritChance, CritDamage;
    public float Weight, Encumbrance, Durability;
}

// Tool Components
public struct ToolId : IComponentData { public FixedString64Bytes Value; }
public struct ToolSpecRef : IComponentData { public BlobAssetReference<ToolSpec> Blob; }
public struct ToolQuality : IComponentData { public float Value; } // 0-100, derived from quality formula
public struct ToolRarity : IComponentData { public Rarity Value; } // Derived from materials + craftsman
public struct ToolTechTier : IComponentData { public byte Value; } // Required tier to craft/use
public struct ProductionInput : IBufferElementData 
{ 
    public FixedString64Bytes MaterialId;
    public float Quantity;
    public float MinPurity;
    public float MinQuality;
    public byte MinTechTier; // NEW
}

// Building Components
public struct BuildingId : IComponentData { public FixedString64Bytes Value; }
public struct BuildingSpecRef : IComponentData { public BlobAssetReference<BuildingSpec> Blob; }
public struct BuildingQuality : IComponentData { public float Value; } // Derived from materials
public struct BuildingStats : IComponentData 
{ 
    public float Health, MaxHealth;
    public float Desirability;
}

// Material Attribute Buffer
public struct MaterialAttribute : IBufferElementData 
{ 
    public FixedString64Bytes AttributeId; // IncreasedDurability, SharpEdge, etc.
    public float Value;
    public bool IsPercentage;
}
```

### Blob Assets

```csharp
// Material Spec Blob
public struct MaterialSpecBlob 
{ 
    public FixedString64Bytes Id;
    public MaterialCategory Category; // Raw, Extracted, Producible, Luxury
    public MaterialUsageFlags Usage; // Building, Armor, Weapon, Tool, etc.
    public MaterialTraitsFlags Traits; // Ductile, Hard, Flammable, etc.
    public MaterialStats Stats; // Hardness, Toughness, Density, MeltingPoint, Conductivity
    public float BaseQuality; // 0-100
    public float Purity; // 0-100, for extracted materials
    public Rarity Rarity; // Common, Uncommon, Rare, Epic, Legendary
    public byte TechTier; // 0-10, required tier to extract/use
    public BlobArray<MaterialAttributeEntry> PossibleAttributes; // Attributes skilled craftsmen can add
}

// Equipment Spec Blob
public struct EquipmentSpecBlob 
{ 
    public FixedString64Bytes Id;
    public EquipmentType Type; // Weapon, Armor, Tool, Accessory
    public SlotKind Slot; // Hand, Body, Head, Feet, Accessory
    public EquipmentStats BaseStats; // Base damage, armor, etc.
    public MaterialRequirements MaterialReqs; // Required material traits/stats
    public float BaseDurability;
    public byte RequiredTechTier; // 0-10, gates crafting/use
}

// Tool Spec Blob
public struct ToolSpecBlob 
{ 
    public FixedString64Bytes Id;
    public BlobArray<ProductionInputEntry> ProductionInputs; // Materials needed
    public FixedString64Bytes ProducedFrom; // Parent material/tool
    public QualityDerivation QualityDerivation; // Weights for quality calculation
    public float BaseQuality; // 0-100
    public float MinQuality, MaxQuality; // Bounds
    public byte RequiredTechTier; // 0-10, gates crafting
    public BlobArray<MaterialAttributeEntry> PossibleAttributes;
}

// Quality Derivation Blob
public struct QualityDerivation 
{ 
    public float PurityWeight; // Default 0.4
    public float QualityWeight; // Default 0.3
    public float CraftsmanSkillWeight; // Default 0.2
    public float ForgeQualityWeight; // Default 0.1
    public float BaseQualityMultiplier; // Default 1.0
}
```

### Authoring ScriptableObjects

```csharp
[CreateAssetMenu]
public class MaterialCatalog : ScriptableObject 
{ 
    public List<MaterialEntryAuthoring> Materials;
}

[Serializable]
public struct MaterialEntryAuthoring 
{ 
    public string id;
    public MaterialCategory category;
    public MaterialUsageFlags usage;
    public MaterialTraitsFlags traits;
    public MaterialStatsAuthoring stats;
    public float baseQuality; // 0-100
    public float purity; // 0-100, for extracted materials
    public Rarity rarity; // Common, Uncommon, Rare, Epic, Legendary
    public byte techTier; // 0-10
    public List<MaterialAttributeAuthoring> possibleAttributes;
}

[CreateAssetMenu]
public class EquipmentCatalog : ScriptableObject 
{ 
    public List<EquipmentEntryAuthoring> Equipment;
}

[Serializable]
public struct EquipmentEntryAuthoring 
{ 
    public string id;
    public EquipmentType type;
    public SlotKind slot;
    public EquipmentStatsAuthoring baseStats;
    public MaterialRequirementsAuthoring materialReqs;
    public float baseDurability;
    public byte requiredTechTier; // 0-10
}

[CreateAssetMenu]
public class ToolCatalog : ScriptableObject 
{ 
    public List<ToolEntryAuthoring> Tools;
}

[Serializable]
public struct ToolEntryAuthoring 
{ 
    public string id;
    public List<ProductionInputAuthoring> productionInputs;
    public string producedFrom;
    public QualityDerivationAuthoring qualityDerivation;
    public float baseQuality; // 0-100
    public float minQuality, maxQuality;
    public byte requiredTechTier; // 0-10
    public List<MaterialAttributeAuthoring> possibleAttributes;
}
```

### Baker Mapping

```csharp
public sealed class MaterialCatalogBaker : Baker<MaterialCatalog> 
{
    public override void Bake(MaterialCatalog src) 
    {
        using var bb = new BlobBuilder(Allocator.Temp);
        ref var root = ref bb.ConstructRoot<MaterialSpecBlob>();
        
        var arr = bb.Allocate(ref root.Entries, src.materials.Count);
        for (int i = 0; i < src.materials.Count; i++) 
        {
            var entry = src.materials[i];
            arr[i] = new MaterialSpecEntry 
            { 
                Id = entry.id,
                Category = entry.category,
                Usage = entry.usage,
                Traits = entry.traits,
                Stats = entry.stats.ToBlob(),
                BaseQuality = entry.baseQuality,
                Purity = entry.purity,
                Rarity = entry.rarity,
                TechTier = entry.techTier,
                // ... possibleAttributes ...
            };
        }
        
        var blob = bb.CreateBlobAssetReference<MaterialSpecBlob>(Allocator.Persistent);
        var e = GetEntity(TransformUsageFlags.None);
        AddComponent(e, new MaterialSpecRef { Blob = blob });
    }
}
```

---

## Systems & Ordering

### System Groups

**Initialization:**
- `MaterialCatalogInitializationSystem` - Creates MaterialSpecRef singleton from catalog
- `EquipmentCatalogInitializationSystem` - Creates EquipmentSpecRef singleton
- `ToolCatalogInitializationSystem` - Creates ToolSpecRef singleton
- `BuildingCatalogInitializationSystem` - Creates BuildingSpecRef singleton

**FixedStep Simulation:**
- `QualityCalculationSystem` - Calculates item quality from materials + craftsman + forge (NEW)
- `RarityAssignmentSystem` - Assigns rarity based on quality thresholds + material rarity (NEW)
- `TechTierValidationSystem` - Validates tech tier requirements for crafting/use (NEW)
- `ProductionChainValidationSystem` - Validates production chains (cycle detection, input validation) (NEW)
- `MaterialAttributeApplicationSystem` - Applies material attributes based on craftsman skill (NEW)
- `EquipmentStatCalculationSystem` - Calculates equipment stats from materials + quality (already exists, extend)

**Presentation:**
- `ItemVisualBindingSystem` - Binds item visuals (materials, equipment, tools) (already exists)

### Code Sketches

```csharp
[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct QualityCalculationSystem : ISystem 
{
    [BurstCompile] 
    public void OnUpdate(ref SystemState s) 
    {
        new CalculateQualityJob { }.ScheduleParallel();
    }
    
    [BurstCompile]
    public partial struct CalculateQualityJob : IJobEntity 
    {
        void Execute(Entity e, 
            in ToolId id,
            in ToolSpecRef specRef,
            in DynamicBuffer<ProductionInput> inputs,
            ref ToolQuality quality) 
        {
            var spec = specRef.Blob.Value;
            var derivation = spec.QualityDerivation;
            
            // Calculate quality from inputs
            float materialPurity = 0f, materialQuality = 0f;
            foreach (var input in inputs) 
            {
                // Lookup material spec, accumulate purity/quality
            }
            
            // TODO(Design): Get craftsman skill and forge quality from context
            float craftsmanSkill = 50f; // Placeholder
            float forgeQuality = 50f; // Placeholder
            
            float calculated = (
                materialPurity * derivation.PurityWeight +
                materialQuality * derivation.QualityWeight +
                craftsmanSkill * derivation.CraftsmanSkillWeight +
                forgeQuality * derivation.ForgeQualityWeight
            ) * derivation.BaseQualityMultiplier;
            
            quality.Value = math.clamp(calculated, spec.MinQuality, spec.MaxQuality);
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct RarityAssignmentSystem : ISystem 
{
    [BurstCompile] 
    public void OnUpdate(ref SystemState s) 
    {
        new AssignRarityJob { }.ScheduleParallel();
    }
    
    [BurstCompile]
    public partial struct AssignRarityJob : IJobEntity 
    {
        void Execute(Entity e, 
            in ToolQuality quality,
            in DynamicBuffer<ProductionInput> inputs,
            ref ToolRarity rarity) 
        {
            // Base rarity from quality thresholds
            Rarity baseRarity = QualityToRarity(quality.Value);
            
            // Max material rarity
            Rarity maxMaterialRarity = Rarity.Common;
            foreach (var input in inputs) 
            {
                // Lookup material rarity, track max
            }
            
            // Can't exceed material rarity
            rarity.Value = (Rarity)math.min((byte)baseRarity, (byte)maxMaterialRarity);
        }
        
        static Rarity QualityToRarity(float quality) 
        {
            if (quality >= 90) return Rarity.Legendary;
            if (quality >= 70) return Rarity.Epic;
            if (quality >= 50) return Rarity.Rare;
            if (quality >= 30) return Rarity.Uncommon;
            return Rarity.Common;
        }
    }
}
```

**ECB Boundaries:**
- Structural changes (spawning/destroying items, visual tokens) only at Begin/End Presentation via ECB.
- Simulation systems mutate components directly (quality, rarity, stats).

---

## Prefab Maker Tasks

### What to Generate

**Phase 1: Core Fields**

1. **Add Quality/Rarity/Tech Tier to Base Template:**
   - Extend `PrefabTemplate` base class with `quality`, `rarity`, `techTier` fields
   - Update all template types (Material, Equipment, Tool, Building) to include these fields

2. **Material Prefabs:**
   - Generate prefabs in `Assets/Prefabs/Godgame/Materials/`
   - Components: `MaterialIdAuthoring`, `MaterialQualityAuthoring`, `MaterialPurityAuthoring`, `MaterialRarityAuthoring`, `MaterialTechTierAuthoring`
   - Visual: Placeholder sphere

3. **Equipment Prefabs:**
   - Generate prefabs in `Assets/Prefabs/Godgame/Equipment/`
   - Components: `EquipmentIdAuthoring`, `EquipmentQualityAuthoring`, `EquipmentRarityAuthoring`, `EquipmentTechTierAuthoring`, `EquipmentStatsAuthoring`
   - Visual: Placeholder cube

4. **Tool Prefabs:**
   - Generate prefabs in `Assets/Prefabs/Godgame/Tools/`
   - Components: `ToolIdAuthoring`, `ToolQualityAuthoring`, `ToolRarityAuthoring`, `ToolTechTierAuthoring`, `ProductionInputAuthoring` buffer
   - Visual: Placeholder capsule

**Phase 2: Quality System**

5. **Unified Quality Calculation:**
   - Create `CalculateItemQuality()` function for all item types
   - Update quality calculation for Materials, Tools, Equipment
   - Add quality display in UI

**Phase 3: Rarity System**

6. **Rarity Assignment:**
   - Convert `rarity` from float to enum in all templates
   - Implement `CalculateRarity()` function
   - Add rarity propagation through production chains
   - Add rarity display in UI

**Phase 4: Tech Tier System**

7. **Tech Tier Integration:**
   - Add `techTier` to `MaterialTemplate`
   - Add `requiredTechTier` to `ToolTemplate` and `EquipmentTemplate`
   - Add `minTechTier` to `ProductionInput`
   - Add tech tier validation in production chains
   - Add tech tier display in UI

**Phase 5: Production Chain Validation**

8. **Cycle Detection:**
   - Implement cycle detection (A → B → A)
   - Validate all inputs exist
   - Validate tech tier requirements

**Phase 6: Material Attributes**

9. **Attribute Application:**
   - Decide on enum vs string-based attributes (TODO(Design))
   - Implement attribute application system
   - Implement attribute stacking rules
   - Implement attribute conflict resolution

### Validation

**Catalog Validation:**
- All material entries have valid quality (0-100), rarity (enum), tech tier (0-10)
- All equipment entries have valid stats, required tech tier
- All tool entries have valid production inputs, quality derivation weights
- All production chains have no cycles
- All production inputs reference existing materials

**Binding Validation:**
- All material IDs have corresponding prefabs
- All equipment IDs have corresponding prefabs
- All tool IDs have corresponding prefabs

**Idempotency:**
- Hash report: Catalog SO hash → blob hash → prefab hash
- CLI dry-run: `--validate-only` flag checks all catalogs without generating
- Re-run produces identical outputs (deterministic blob building, deterministic prefab generation)

---

## Determinism & Tests

### Test Names & Descriptions

1. **QualityCalculation_Deterministic_EditMode** - Quality calculation produces identical results with same inputs
2. **RarityAssignment_QualityThresholds_EditMode** - Rarity assignment follows quality thresholds correctly
3. **TechTierValidation_Gating_EditMode** - Tech tier gates crafting/use correctly
4. **ProductionChain_CycleDetection_EditMode** - Cycle detection catches circular dependencies
5. **MaterialAttributeApplication_SkillThresholds_EditMode** - Attributes apply based on craftsman skill thresholds
6. **QualityPropagation_ProductionChain_PlayMode** - Quality propagates through production chain correctly
7. **Rewind_ItemQuality_PlayMode** - Item quality rewinds correctly (snapshot/restore)

**Rewind Check:**
- Item quality, rarity, tech tier are components → snapshot via PureDOTS time spine
- Blob assets (MaterialSpec, EquipmentSpec, ToolSpec) are immutable → no rewind needed

---

## CI & Budgets

### Metrics to Export

**Per Scenario:**
- Quality calculation time: QualityCalculationSystem
- Rarity assignment time: RarityAssignmentSystem
- Production chain validation time: ProductionChainValidationSystem
- Item entity count: Materials, Equipment, Tools in world

### Budgets

- **QualityCalculationSystem**: < 1ms per 1000 items
- **RarityAssignmentSystem**: < 0.5ms per 1000 items
- **ProductionChainValidationSystem**: < 10ms for full catalog validation
- **Item entity count**: < 50,000 active items

### Gates

- Quality calculation exceeds 5ms → fail CI
- Production chain validation finds cycles → fail CI
- Item entity count exceeds 100,000 → warn

---

## Risks & "Why Invalid" Checks

### Lint Rules

1. **Quality Range Validation:**
   - Rule: `quality` in range 0-100, `minQuality <= maxQuality`
   - Message: "Tool '{id}' has invalid quality range: {min} > {max}"

2. **Rarity Enum Validation:**
   - Rule: `rarity` is valid enum value
   - Message: "Material '{id}' has invalid rarity: {rarity}"

3. **Tech Tier Range Validation:**
   - Rule: `techTier` in range 0-10
   - Message: "Material '{id}' has invalid tech tier: {tier} (must be 0-10)"

4. **Production Input Reference:**
   - Rule: `ProductionInput.materialId` references existing material
   - Message: "Tool '{id}' references missing material: {materialId}"

5. **Production Chain Cycle:**
   - Rule: No cycles in production chain (A → B → A)
   - Message: "Production chain has cycle: {cyclePath}"

6. **Tech Tier Requirement:**
   - Rule: Tool `requiredTechTier` >= max of input `minTechTier`
   - Message: "Tool '{id}' requires tech tier {tier} but has input requiring tier {inputTier}"

### Design Risks

**Risk: Quality Calculation Performance**
- **Mitigation:** Cache quality calculations, update only when inputs change
- **Fallback:** Simple quality = material quality average (no craftsman/forge)

**Risk: Rarity Propagation Complexity**
- **Mitigation:** Simple max-of-inputs rule for MVP
- **Fallback:** No rarity propagation (rarity from material only)

**Risk: Tech Tier Gating Complexity**
- **Mitigation:** Simple tier check at crafting time
- **Fallback:** No tech tier gating (all items available)

---

## TODO(Design) Items

1. **Quality Display:** Should quality affect item naming? (e.g., "Crude" vs "Masterwork") Default: No naming changes for MVP.
2. **Quality Tiers:** Should quality have tiers? (Poor/Common/Good/Excellent/Masterwork) Default: No tiers, use raw 0-100.
3. **Rarity Assignment:** How should rarity be assigned? Default: Quality thresholds + material rarity max.
4. **Rarity Propagation:** How should rarity propagate? Default: Max of input rarities.
5. **Tech Tier Scope:** Should tech tier gate material extraction, tool crafting, or both? Default: Both.
6. **Tech Tier Unlock:** Should tech tier be per-village or global? Default: Global unlock.
7. **Material Attributes:** Should attributes be enum-based or string-based? Default: Enum-based for type safety.
8. **Attribute Application:** Should attributes be deterministic or probabilistic? Default: Deterministic if skill met.
9. **Attribute Stacking:** Should attributes stack? Default: Yes, additive stacking.
10. **Attribute Conflicts:** Should attributes conflict? (e.g., Flammable vs Fireproof) Default: Yes, conflicts prevent both.

---

**End of Implementation Notes — Item System Summary For Advisor**

