# Mechanic: Material Properties & Trait-Based Crafting

## Overview

**Status**: Concept
**Complexity**: High
**Category**: Economy / Manufacturing / Crafting

**One-line description**: A trait-based material property system where metals and materials are defined by physical properties (Hardness, Toughness, Ductility, etc.) with usage rules per item role, quality scaling, and substitution penalties enforced by a rule engine.

## Core Concept

Instead of hardcoded material lists for each item type, materials are defined by **measurable physical properties** (normalized 0.0-1.0 values). Each crafting role (Blade.Long, Mail.Ring, Spring, etc.) has **property requirements** that define acceptable ranges. Quality scaling affects the **result** without changing the metal's intrinsic nature. A rule engine validates material choices and applies **substitution penalties** for suboptimal combinations.

This system supports:
- **Realistic material behavior** (you can't make a good spring from copper)
- **Quality progression** (common steel blade → masterwork steel blade, same metal)
- **Material substitution** (bronze sword works, but penalty vs steel)
- **Tech progression** (unlocking stainless steel, titanium, tungsten)
- **Educational realism** (teaches real metallurgy concepts)

---

## Material Property Components

### 1. Material Definition

```csharp
// Core material properties component
public struct MaterialProperties : IComponentData
{
    public FixedString64Bytes MaterialName;      // "High-Carbon Steel", "Bronze", "Titanium"
    public MaterialCategory Category;            // Metal, Wood, Leather, Ceramic, Composite
    public TechTier RequiredTier;                // Tech tier required to work with this material

    // Physical Properties (normalized 0.0-1.0)
    public float Hardness;                       // Resist deformation/scratching (diamond = 1.0)
    public float Toughness;                      // Resist fracture/shattering (wrought iron = high)
    public float Ductility;                      // Deform without breaking (gold = 1.0, cast iron = 0.1)
    public float ElasticLimit;                   // Return to shape after stress (spring steel = high)
    public float Density;                        // Mass per volume (tungsten = 1.0, aluminum = 0.15)
    public float Temperable;                     // Benefit from heat treatment (steel = high, bronze = low)
    public float CorrosionResist;                // Resist rust/oxidation (stainless = high, iron = low)
    public float RedHardness;                    // Retain hardness at high temps (tungsten = high, steel = medium)

    // Derived Properties (calculated)
    public float TensileStrength;                // = (Hardness × 0.6) + (Ductility × 0.4)
    public float CompressiveStrength;            // = (Hardness × 0.7) + (Density × 0.3)
    public float ShearStrength;                  // = (Hardness × 0.5) + (Toughness × 0.5)
    public float FatigueResistance;              // = (ElasticLimit × 0.6) + (Toughness × 0.4)
}

public enum MaterialCategory : byte
{
    Metal,          // Iron, Steel, Bronze, Titanium
    Wood,           // Oak, Ash, Yew, Ironwood
    Leather,        // Hide, Tanned Leather, Exotic Hides
    Ceramic,        // Clay, Porcelain, Advanced Ceramics
    Composite,      // Laminated Wood, Fiber-Reinforced
    Crystal,        // Gemstones, Quartz, Magical Crystals
    Bone,           // Animal Bone, Dragon Bone
    Exotic          // Mithril, Adamantine, Star Metal
}

public enum TechTier : byte
{
    Primitive = 0,      // Stone, Bone, Wood
    Bronze = 1,         // Copper, Bronze, Basic Metallurgy
    Iron = 2,           // Wrought Iron, Basic Smithing
    Steel = 3,          // Med-Carbon Steel, Advanced Forging
    Advanced = 4,       // High-Carbon Steel, Spring Steel
    Industrial = 5,     // Stainless Steel, Tool Steel
    Modern = 6,         // Titanium, Advanced Alloys
    FutureTech = 7      // Tungsten, Exotic Materials
}
```

### 2. Crafting Role Requirements

```csharp
// Buffer defining what properties a crafting role needs
public struct CraftingRoleRequirements : IBufferElementData
{
    public FixedString64Bytes RoleID;            // "Blade.Long", "Axe.Head", "Mail.Ring", "Spring"
    public ItemPartType PartType;                // Which part of the item this applies to

    // Required Property Ranges (min/max acceptable values)
    public PropertyRange HardnessRange;          // e.g., Blade.Long = 0.6-1.0
    public PropertyRange ToughnessRange;         // e.g., Blade.Long = 0.4-1.0
    public PropertyRange DuctilityRange;         // e.g., Blade.Long = 0.3-0.8
    public PropertyRange ElasticLimitRange;      // e.g., Spring = 0.7-1.0
    public PropertyRange DensityRange;           // e.g., Projectile = 0.6-1.0 (heavy = more momentum)

    // Optimal Property Targets (for calculating quality)
    public float OptimalHardness;
    public float OptimalToughness;
    public float OptimalDuctility;
    public float OptimalElasticLimit;
    public float OptimalDensity;

    // Substitution Penalty Weights (how much each deviation hurts)
    public float HardnessPenaltyWeight;          // 0.0-1.0 (how critical is hardness)
    public float ToughnessPenaltyWeight;
    public float DuctilityPenaltyWeight;
    public float ElasticLimitPenaltyWeight;
    public float DensityPenaltyWeight;
}

public struct PropertyRange
{
    public float Min;                            // Below this = unusable
    public float Max;                            // Above this = wasted (or brittle)
    public float Ideal;                          // Perfect target value
}

public enum ItemPartType : byte
{
    // Weapons
    BladeEdge,          // Cutting edge of blade
    BladeSpine,         // Back of blade (needs toughness)
    WeaponHead,         // Axe/Hammer head
    WeaponHandle,       // Haft, grip
    Crossguard,         // Hand protection
    Pommel,             // Balance weight

    // Armor
    Plate,              // Solid armor plates
    MailRing,           // Chain mail rings
    Scale,              // Scale armor pieces
    Padding,            // Gambeson, soft armor

    // Tools
    ToolHead,           // Pickaxe, shovel head
    ToolHandle,         // Tool shaft

    // Mechanical
    Spring,             // Springs, leaf springs
    Fastener,           // Bolts, rivets, nails
    Bearing,            // Axles, pivot points
    Wheel,              // Wagon wheels, gears

    // Structural
    Frame,              // Wagon chassis, ship frame
    Panel,              // Hull plating, walls
    Reinforcement       // Bracing, supports
}
```

### 3. Material Validation & Penalties

```csharp
// Result of material validation for a crafting role
public struct MaterialValidationResult : IComponentData
{
    public bool IsUsable;                        // Can this material be used at all?
    public float QualityMultiplier;              // 0.0-1.0 (1.0 = perfect, 0.5 = severe penalty)
    public float SubstitutionPenalty;            // 0.0-1.0 (how much worse than ideal)
    public DynamicBuffer<PropertyViolation> Violations; // What's wrong with this choice
}

public struct PropertyViolation : IBufferElementData
{
    public PropertyType Property;                // Which property is wrong
    public ViolationType Type;                   // TooLow, TooHigh, SubOptimal
    public float Deviation;                      // How far from ideal (0.0-1.0)
    public float PenaltyContribution;            // How much this hurts quality
}

public enum PropertyType : byte
{
    Hardness,
    Toughness,
    Ductility,
    ElasticLimit,
    Density,
    Temperable,
    CorrosionResist,
    RedHardness
}

public enum ViolationType : byte
{
    TooLow,             // Below minimum acceptable
    TooHigh,            // Above maximum acceptable
    SubOptimal,         // Usable, but not ideal
    Incompatible        // Fundamentally wrong (e.g., brittle material for spring)
}
```

---

## Material Property Tables

### Metal Property Values (Normalized 0.0-1.0)

### Standard Materials (Common to Advanced)

| Material | Hardness | Toughness | Ductility | ElasticLimit | Density | Temperable | CorrosionResist | RedHardness | TechTier |
|----------|----------|-----------|-----------|--------------|---------|------------|-----------------|-------------|----------|
| **Copper** | 0.15 | 0.40 | 0.95 | 0.10 | 0.45 | 0.05 | 0.60 | 0.15 | Bronze |
| **Bronze** | 0.35 | 0.55 | 0.70 | 0.20 | 0.50 | 0.15 | 0.70 | 0.25 | Bronze |
| **Wrought Iron** | 0.40 | 0.75 | 0.80 | 0.30 | 0.40 | 0.30 | 0.10 | 0.30 | Iron |
| **Med-Carbon Steel** | 0.65 | 0.60 | 0.50 | 0.50 | 0.42 | 0.75 | 0.15 | 0.50 | Steel |
| **High-Carbon Steel** | 0.80 | 0.45 | 0.30 | 0.60 | 0.42 | 0.85 | 0.10 | 0.55 | Advanced |
| **Spring Steel** | 0.75 | 0.55 | 0.40 | 0.90 | 0.42 | 0.80 | 0.15 | 0.60 | Advanced |
| **Stainless Steel** | 0.70 | 0.65 | 0.45 | 0.55 | 0.43 | 0.60 | 0.95 | 0.65 | Industrial |
| **Titanium** | 0.75 | 0.85 | 0.35 | 0.70 | 0.25 | 0.50 | 0.98 | 0.75 | Modern |
| **Tungsten** | 0.95 | 0.35 | 0.10 | 0.50 | 1.00 | 0.40 | 0.85 | 0.95 | FutureTech |

### Exotic Materials (Rare, High Performance)

| Material | Hardness | Toughness | Ductility | ElasticLimit | Density | Temperable | CorrosionResist | RedHardness | TechTier |
|----------|----------|-----------|-----------|--------------|---------|------------|-----------------|-------------|----------|
| **Mithril** | 0.85 | 0.90 | 0.60 | 0.80 | 0.15 | 0.70 | 1.00 | 0.80 | Exotic-Low |
| **Adamantine** | 1.00 | 0.80 | 0.20 | 0.75 | 0.60 | 0.90 | 1.00 | 0.90 | Exotic-Low |
| **Star Metal** | 0.90 | 0.95 | 0.50 | 0.85 | 0.35 | 0.85 | 1.00 | 0.95 | Exotic-Mid |
| **Orichalcum** | 0.88 | 0.88 | 0.70 | 0.88 | 0.50 | 0.95 | 0.98 | 0.88 | Exotic-Mid |

### Endgame Materials (Intrinsically Superior)

**CRITICAL**: These materials are **fundamentally better** than even legendary-tier standard materials. A **Common Void Glass dagger beats a Legendary Steel dagger** due to intrinsic material superiority.

| Material | Hardness | Toughness | Ductility | ElasticLimit | Density | Temperable | CorrosionResist | RedHardness | TechTier | BaseDamageMultiplier |
|----------|----------|-----------|-----------|--------------|---------|------------|-----------------|-------------|----------|----------------------|
| **Void Glass** | 0.98 | 0.40 | 0.05 | 0.60 | 0.20 | 0.10 | 1.00 | 0.50 | Endgame | **3.0×** |
| **Crystal Adamant** | 1.00 | 0.95 | 0.30 | 0.90 | 0.45 | 0.80 | 1.00 | 0.98 | Endgame | **2.5×** |
| **Mythril Alloy** | 0.95 | 0.98 | 0.75 | 0.95 | 0.18 | 0.90 | 1.00 | 0.95 | Endgame | **2.8×** |
| **Elder Dragon Bone** | 0.85 | 0.92 | 0.15 | 0.75 | 0.25 | 0.40 | 0.95 | 0.85 | Endgame | **2.2×** |
| **Celestial Bronze** | 0.92 | 0.88 | 0.65 | 0.88 | 0.40 | 0.98 | 1.00 | 0.92 | Endgame | **2.6×** |
| **Umbral Iron** | 0.94 | 0.85 | 0.45 | 0.80 | 0.55 | 0.92 | 0.98 | 0.88 | Endgame | **2.4×** |
| **Primordial Ore** | 0.96 | 0.90 | 0.55 | 0.92 | 0.50 | 0.95 | 1.00 | 0.96 | Endgame | **2.7×** |

**Material Tier Damage Comparison**:
```
Common Steel Longsword:
- Base Damage: 20
- Material: Med-Carbon Steel (near-optimal for role)
- Quality: Common (0.9× performance)
- Final Damage: 20 × 0.95 × 0.9 = 17.1

Legendary Steel Longsword:
- Base Damage: 20
- Material: High-Carbon Steel (optimal for role)
- Quality: Legendary (1.5× performance)
- Final Damage: 20 × 0.95 × 1.5 = 28.5

Common Void Glass Dagger:
- Base Damage: 12 (dagger base)
- Material: Void Glass (3.0× intrinsic multiplier)
- Quality: Common (0.9× performance)
- Material Suitability: 0.88 (slightly brittle for impact, but devastating edge)
- Final Damage: 12 × 3.0 × 0.9 × 0.88 = 28.5 (equals Legendary Steel!)

Legendary Void Glass Dagger:
- Base Damage: 12
- Material: Void Glass (3.0× intrinsic multiplier)
- Quality: Legendary (1.5× performance)
- Material Suitability: 0.88
- Final Damage: 12 × 3.0 × 1.5 × 0.88 = 47.5 (crushes Legendary Steel!)

Common Mythril Alloy Longsword:
- Base Damage: 20
- Material: Mythril Alloy (2.8× intrinsic multiplier)
- Quality: Common (0.9× performance)
- Material Suitability: 0.98 (near-perfect for blades)
- Final Damage: 20 × 2.8 × 0.9 × 0.98 = 49.4 (beats Legendary Steel by 73%!)
```

**Key Takeaway**: Even poorly-crafted endgame materials exceed masterwork standard materials. This creates:
- **Material hunting endgame**: Finding rare materials becomes critical
- **Risk/reward tradeoffs**: Void Glass is devastating but brittle (low toughness 0.40)
- **Knowledge preservation**: Losing smiths who can work Void Glass is catastrophic

**Derived Property Formulas**:
```csharp
// Calculated during material initialization
TensileStrength = (Hardness × 0.6f) + (Ductility × 0.4f);
CompressiveStrength = (Hardness × 0.7f) + (Density × 0.3f);
ShearStrength = (Hardness × 0.5f) + (Toughness × 0.5f);
FatigueResistance = (ElasticLimit × 0.6f) + (Toughness × 0.4f);
```

### Non-Metal Materials (Examples)

| Material | Hardness | Toughness | Ductility | ElasticLimit | Density | Notes |
|----------|----------|-----------|-----------|--------------|---------|-------|
| **Oak Wood** | 0.25 | 0.60 | 0.40 | 0.30 | 0.35 | Good for hafts, shields |
| **Yew Wood** | 0.20 | 0.70 | 0.50 | 0.60 | 0.30 | Excellent for bows (high elastic) |
| **Leather** | 0.10 | 0.55 | 0.85 | 0.40 | 0.20 | Flexible armor, padding |
| **Dragon Bone** | 0.70 | 0.80 | 0.15 | 0.50 | 0.35 | Exotic, lightweight |
| **Ironwood** | 0.50 | 0.75 | 0.30 | 0.40 | 0.60 | Dense, hard wood |

---

## Crafting Role Requirements

### Blade Requirements

```csharp
// Long Blade (Sword, Longsword, Greatsword)
CraftingRoleRequirements BladeLong = new()
{
    RoleID = "Blade.Long",
    PartType = ItemPartType.BladeEdge,

    // Property Ranges
    HardnessRange = new PropertyRange { Min = 0.60f, Max = 0.95f, Ideal = 0.75f },
    ToughnessRange = new PropertyRange { Min = 0.40f, Max = 1.0f, Ideal = 0.60f },
    DuctilityRange = new PropertyRange { Min = 0.25f, Max = 0.70f, Ideal = 0.45f },
    ElasticLimitRange = new PropertyRange { Min = 0.30f, Max = 0.80f, Ideal = 0.55f },
    DensityRange = new PropertyRange { Min = 0.35f, Max = 0.60f, Ideal = 0.42f },

    // Optimal Targets
    OptimalHardness = 0.75f,
    OptimalToughness = 0.60f,
    OptimalDuctility = 0.45f,
    OptimalElasticLimit = 0.55f,
    OptimalDensity = 0.42f,

    // Penalty Weights (how critical each property is)
    HardnessPenaltyWeight = 0.35f,       // Very important (edge retention)
    ToughnessPenaltyWeight = 0.30f,      // Very important (resist breaking)
    DuctilityPenaltyWeight = 0.20f,      // Important (prevent brittleness)
    ElasticLimitPenaltyWeight = 0.10f,   // Moderately important (flex without bending)
    DensityPenaltyWeight = 0.05f         // Low importance (balance)
};

// Short Blade (Dagger, Knife)
CraftingRoleRequirements BladeShort = new()
{
    RoleID = "Blade.Short",
    PartType = ItemPartType.BladeEdge,

    HardnessRange = new PropertyRange { Min = 0.55f, Max = 0.95f, Ideal = 0.75f },
    ToughnessRange = new PropertyRange { Min = 0.30f, Max = 1.0f, Ideal = 0.50f },
    DuctilityRange = new PropertyRange { Min = 0.30f, Max = 0.80f, Ideal = 0.50f },
    ElasticLimitRange = new PropertyRange { Min = 0.25f, Max = 0.75f, Ideal = 0.50f },
    DensityRange = new PropertyRange { Min = 0.30f, Max = 0.55f, Ideal = 0.40f },

    // Less demanding than long blades (smaller = less stress)
    HardnessPenaltyWeight = 0.40f,
    ToughnessPenaltyWeight = 0.25f,
    DuctilityPenaltyWeight = 0.20f,
    ElasticLimitPenaltyWeight = 0.10f,
    DensityPenaltyWeight = 0.05f
};
```

### Axe/Hammer Requirements

```csharp
// Axe Head
CraftingRoleRequirements AxeHead = new()
{
    RoleID = "Axe.Head",
    PartType = ItemPartType.WeaponHead,

    HardnessRange = new PropertyRange { Min = 0.60f, Max = 0.90f, Ideal = 0.75f },
    ToughnessRange = new PropertyRange { Min = 0.50f, Max = 1.0f, Ideal = 0.70f }, // Higher toughness (impacts)
    DuctilityRange = new PropertyRange { Min = 0.20f, Max = 0.60f, Ideal = 0.40f },
    ElasticLimitRange = new PropertyRange { Min = 0.25f, Max = 0.70f, Ideal = 0.45f },
    DensityRange = new PropertyRange { Min = 0.40f, Max = 0.70f, Ideal = 0.50f },  // Heavier = more chopping power

    HardnessPenaltyWeight = 0.30f,
    ToughnessPenaltyWeight = 0.40f,      // Most critical (resist chipping)
    DuctilityPenaltyWeight = 0.15f,
    ElasticLimitPenaltyWeight = 0.05f,
    DensityPenaltyWeight = 0.10f
};

// Hammer Head
CraftingRoleRequirements HammerHead = new()
{
    RoleID = "Hammer.Head",
    PartType = ItemPartType.WeaponHead,

    HardnessRange = new PropertyRange { Min = 0.65f, Max = 0.95f, Ideal = 0.80f },
    ToughnessRange = new PropertyRange { Min = 0.60f, Max = 1.0f, Ideal = 0.75f }, // Very tough (repeated impacts)
    DuctilityRange = new PropertyRange { Min = 0.15f, Max = 0.50f, Ideal = 0.30f },
    ElasticLimitRange = new PropertyRange { Min = 0.30f, Max = 0.75f, Ideal = 0.50f },
    DensityRange = new PropertyRange { Min = 0.50f, Max = 1.0f, Ideal = 0.70f },   // Heavy = crushing power

    HardnessPenaltyWeight = 0.35f,
    ToughnessPenaltyWeight = 0.35f,
    DuctilityPenaltyWeight = 0.10f,
    ElasticLimitPenaltyWeight = 0.05f,
    DensityPenaltyWeight = 0.15f
};
```

### Armor Requirements

```csharp
// Mail Ring (Chain Mail)
CraftingRoleRequirements MailRing = new()
{
    RoleID = "Mail.Ring",
    PartType = ItemPartType.MailRing,

    HardnessRange = new PropertyRange { Min = 0.40f, Max = 0.80f, Ideal = 0.60f },
    ToughnessRange = new PropertyRange { Min = 0.50f, Max = 1.0f, Ideal = 0.70f }, // Must resist cutting
    DuctilityRange = new PropertyRange { Min = 0.50f, Max = 0.95f, Ideal = 0.70f }, // High ductility (deform, not break)
    ElasticLimitRange = new PropertyRange { Min = 0.30f, Max = 0.75f, Ideal = 0.50f },
    DensityRange = new PropertyRange { Min = 0.35f, Max = 0.60f, Ideal = 0.45f },

    HardnessPenaltyWeight = 0.20f,
    ToughnessPenaltyWeight = 0.30f,
    DuctilityPenaltyWeight = 0.35f,      // Most critical (rings must bend, not break)
    ElasticLimitPenaltyWeight = 0.10f,
    DensityPenaltyWeight = 0.05f
};

// Plate Armor
CraftingRoleRequirements ArmorPlate = new()
{
    RoleID = "Armor.Plate",
    PartType = ItemPartType.Plate,

    HardnessRange = new PropertyRange { Min = 0.55f, Max = 0.90f, Ideal = 0.70f },
    ToughnessRange = new PropertyRange { Min = 0.50f, Max = 1.0f, Ideal = 0.70f },
    DuctilityRange = new PropertyRange { Min = 0.30f, Max = 0.70f, Ideal = 0.50f },
    ElasticLimitRange = new PropertyRange { Min = 0.35f, Max = 0.75f, Ideal = 0.55f },
    DensityRange = new PropertyRange { Min = 0.40f, Max = 0.65f, Ideal = 0.50f },

    HardnessPenaltyWeight = 0.30f,
    ToughnessPenaltyWeight = 0.35f,
    DuctilityPenaltyWeight = 0.20f,
    ElasticLimitPenaltyWeight = 0.10f,
    DensityPenaltyWeight = 0.05f
};
```

### Mechanical Requirements

```csharp
// Spring
CraftingRoleRequirements Spring = new()
{
    RoleID = "Spring",
    PartType = ItemPartType.Spring,

    HardnessRange = new PropertyRange { Min = 0.65f, Max = 0.90f, Ideal = 0.75f },
    ToughnessRange = new PropertyRange { Min = 0.45f, Max = 0.80f, Ideal = 0.60f },
    DuctilityRange = new PropertyRange { Min = 0.25f, Max = 0.55f, Ideal = 0.40f },
    ElasticLimitRange = new PropertyRange { Min = 0.75f, Max = 1.0f, Ideal = 0.90f }, // CRITICAL (must return to shape)
    DensityRange = new PropertyRange { Min = 0.35f, Max = 0.55f, Ideal = 0.42f },

    HardnessPenaltyWeight = 0.20f,
    ToughnessPenaltyWeight = 0.20f,
    DuctilityPenaltyWeight = 0.10f,
    ElasticLimitPenaltyWeight = 0.45f,   // Absolutely critical
    DensityPenaltyWeight = 0.05f
};

// Fastener (Bolts, Rivets, Nails)
CraftingRoleRequirements Fastener = new()
{
    RoleID = "Fastener",
    PartType = ItemPartType.Fastener,

    HardnessRange = new PropertyRange { Min = 0.45f, Max = 0.85f, Ideal = 0.65f },
    ToughnessRange = new PropertyRange { Min = 0.40f, Max = 0.90f, Ideal = 0.65f },
    DuctilityRange = new PropertyRange { Min = 0.30f, Max = 0.80f, Ideal = 0.55f },
    ElasticLimitRange = new PropertyRange { Min = 0.25f, Max = 0.70f, Ideal = 0.45f },
    DensityRange = new PropertyRange { Min = 0.35f, Max = 0.65f, Ideal = 0.45f },

    HardnessPenaltyWeight = 0.25f,
    ToughnessPenaltyWeight = 0.30f,
    DuctilityPenaltyWeight = 0.25f,
    ElasticLimitPenaltyWeight = 0.15f,
    DensityPenaltyWeight = 0.05f
};
```

---

## Material Validation System

### Validation Algorithm

```csharp
public static MaterialValidationResult ValidateMaterialForRole(
    MaterialProperties material,
    CraftingRoleRequirements role)
{
    MaterialValidationResult result = new()
    {
        IsUsable = true,
        QualityMultiplier = 1.0f,
        SubstitutionPenalty = 0.0f,
        Violations = new DynamicBuffer<PropertyViolation>()
    };

    float totalPenalty = 0.0f;
    float totalWeight = 0.0f;

    // Check each property
    totalPenalty += EvaluateProperty(
        material.Hardness,
        role.HardnessRange,
        role.OptimalHardness,
        role.HardnessPenaltyWeight,
        PropertyType.Hardness,
        ref result
    );
    totalWeight += role.HardnessPenaltyWeight;

    totalPenalty += EvaluateProperty(
        material.Toughness,
        role.ToughnessRange,
        role.OptimalToughness,
        role.ToughnessPenaltyWeight,
        PropertyType.Toughness,
        ref result
    );
    totalWeight += role.ToughnessPenaltyWeight;

    // ... repeat for other properties

    // Calculate final multiplier
    result.SubstitutionPenalty = totalPenalty / totalWeight;
    result.QualityMultiplier = 1.0f - result.SubstitutionPenalty;

    // Check if material is fundamentally unusable
    if (result.QualityMultiplier < 0.3f)
    {
        result.IsUsable = false; // Too many violations
    }

    return result;
}

private static float EvaluateProperty(
    float materialValue,
    PropertyRange range,
    float optimalValue,
    float penaltyWeight,
    PropertyType propertyType,
    ref MaterialValidationResult result)
{
    // Check if property is within acceptable range
    if (materialValue < range.Min)
    {
        // Too low - calculate violation
        float deviation = (range.Min - materialValue) / range.Min;
        float penaltyContribution = deviation * penaltyWeight;

        result.Violations.Add(new PropertyViolation
        {
            Property = propertyType,
            Type = ViolationType.TooLow,
            Deviation = deviation,
            PenaltyContribution = penaltyContribution
        });

        // Severe penalty for being below minimum
        return penaltyContribution * 2.0f;
    }
    else if (materialValue > range.Max)
    {
        // Too high - can cause brittleness or waste
        float deviation = (materialValue - range.Max) / (1.0f - range.Max);
        float penaltyContribution = deviation * penaltyWeight * 0.5f; // Less severe than too low

        result.Violations.Add(new PropertyViolation
        {
            Property = propertyType,
            Type = ViolationType.TooHigh,
            Deviation = deviation,
            PenaltyContribution = penaltyContribution
        });

        return penaltyContribution;
    }
    else
    {
        // Within range - calculate deviation from optimal
        float deviation = math.abs(materialValue - optimalValue);
        float maxDeviation = math.max(
            math.abs(range.Max - optimalValue),
            math.abs(range.Min - optimalValue)
        );
        float normalizedDeviation = deviation / maxDeviation;

        if (normalizedDeviation > 0.15f) // Only record if deviation is significant
        {
            float penaltyContribution = normalizedDeviation * penaltyWeight * 0.3f; // Mild penalty

            result.Violations.Add(new PropertyViolation
            {
                Property = propertyType,
                Type = ViolationType.SubOptimal,
                Deviation = normalizedDeviation,
                PenaltyContribution = penaltyContribution
            });

            return penaltyContribution;
        }

        return 0.0f; // Near optimal, no penalty
    }
}
```

---

## Quality Scaling System

### Quality vs Material Nature

**CRITICAL PRINCIPLE**: Quality scaling affects the **result** of crafting, not the metal's intrinsic properties.

```csharp
public struct CraftedItemQuality : IComponentData
{
    public QualityTier Tier;                     // Poor, Common, Uncommon, Rare, Epic, Legendary
    public float QualityScore;                   // 0.0-1.0 (normalized quality)

    // Quality affects results, not material
    public float DurabilityMultiplier;           // Poor = 0.6, Legendary = 2.0
    public float PerformanceMultiplier;          // Poor = 0.7, Legendary = 1.5
    public float EdgeRetentionMultiplier;        // How long blade stays sharp
    public float FatigueResistanceMultiplier;    // How long before metal fatigue

    // Material properties remain constant
    public MaterialProperties BaseMaterial;      // Unchanged by quality
    public float MaterialQualityMultiplier;      // From material validation (substitution penalty)
}

public enum QualityTier : byte
{
    Poor = 0,           // 0.0-0.2: Rushed, flawed crafting
    Common = 1,         // 0.2-0.4: Standard crafting
    Uncommon = 2,       // 0.4-0.6: Skilled crafting
    Rare = 3,           // 0.6-0.75: Expert crafting
    Epic = 4,           // 0.75-0.9: Master crafting
    Legendary = 5       // 0.9-1.0: Legendary artisan
}

// Quality affects performance, not properties
private float CalculateEffectiveDamage(CraftedItemQuality quality, float baseDamage)
{
    // Quality improves performance
    float performanceBonus = quality.PerformanceMultiplier;

    // Material suitability affects performance
    float materialSuitability = quality.MaterialQualityMultiplier;

    // Combined formula
    float effectiveDamage = baseDamage * performanceBonus * materialSuitability;

    return effectiveDamage;
}

// Example: Common Bronze Sword vs Legendary Bronze Sword
// Material properties: Same (Bronze hardness = 0.35, toughness = 0.55)
// Quality multipliers: Different
//   - Common: Performance 0.9×, Durability 0.8×, Edge Retention 0.7×
//   - Legendary: Performance 1.5×, Durability 2.0×, Edge Retention 1.8×
// Result: Legendary sword lasts longer, cuts better, but still bronze (not magically harder)
```

### Quality Tier Multipliers

| Quality Tier | Durability | Performance | Edge Retention | Fatigue Resistance | Notes |
|--------------|------------|-------------|----------------|--------------------| ------|
| **Poor** | 0.6× | 0.7× | 0.5× | 0.6× | Flawed, rushed work |
| **Common** | 0.8× | 0.9× | 0.7× | 0.8× | Standard craftsmanship |
| **Uncommon** | 1.0× | 1.05× | 1.0× | 1.0× | Baseline (skilled work) |
| **Rare** | 1.3× | 1.2× | 1.3× | 1.3× | Expert craftsmanship |
| **Epic** | 1.6× | 1.35× | 1.6× | 1.6× | Master-level work |
| **Legendary** | 2.0× | 1.5× | 2.0× | 2.0× | Once-in-a-lifetime piece |

---

## Material Substitution Examples

### Example 1: Bronze Sword (Suboptimal Material)

**Material**: Bronze
**Role**: Blade.Long
**Tech Context**: Early game, before steel is unlocked

```
Property Evaluation:
- Hardness: 0.35 (Required: 0.60-0.95, Optimal: 0.75)
  - Violation: TooLow, Deviation: 0.42 (25% below minimum)
  - Penalty Contribution: 0.42 × 0.35 × 2.0 = 0.294 (severe)

- Toughness: 0.55 (Required: 0.40-1.0, Optimal: 0.60)
  - Violation: SubOptimal, Deviation: 0.083
  - Penalty Contribution: 0.083 × 0.30 × 0.3 = 0.007 (mild)

- Ductility: 0.70 (Required: 0.25-0.70, Optimal: 0.45)
  - Violation: TooHigh (at edge of range), Deviation: 0.25
  - Penalty Contribution: 0.25 × 0.20 × 0.5 = 0.025

- ElasticLimit: 0.20 (Required: 0.30-0.80, Optimal: 0.55)
  - Violation: TooLow, Deviation: 0.33
  - Penalty Contribution: 0.33 × 0.10 × 2.0 = 0.066

- Density: 0.50 (Required: 0.35-0.60, Optimal: 0.42)
  - Violation: SubOptimal, Deviation: 0.19
  - Penalty Contribution: 0.19 × 0.05 × 0.3 = 0.003

Total Penalty: 0.395
Material Quality Multiplier: 0.605

Result:
- IsUsable: true (above 0.3 threshold)
- Substitution Penalty: 39.5%
- Effect: Bronze sword works, but 40% worse than steel sword
- Edge retention: Poor (soft, needs frequent sharpening)
- Durability: Decent (tough, resists breaking)
```

### Example 2: High-Carbon Steel Sword (Optimal Material)

**Material**: High-Carbon Steel
**Role**: Blade.Long

```
Property Evaluation:
- Hardness: 0.80 (Required: 0.60-0.95, Optimal: 0.75)
  - Violation: SubOptimal, Deviation: 0.067
  - Penalty Contribution: 0.067 × 0.35 × 0.3 = 0.007

- Toughness: 0.45 (Required: 0.40-1.0, Optimal: 0.60)
  - Violation: SubOptimal, Deviation: 0.25
  - Penalty Contribution: 0.25 × 0.30 × 0.3 = 0.023

- Ductility: 0.30 (Required: 0.25-0.70, Optimal: 0.45)
  - Violation: SubOptimal, Deviation: 0.33
  - Penalty Contribution: 0.33 × 0.20 × 0.3 = 0.020

- ElasticLimit: 0.60 (Required: 0.30-0.80, Optimal: 0.55)
  - Violation: SubOptimal, Deviation: 0.091
  - Penalty Contribution: negligible

- Density: 0.42 (Required: 0.35-0.60, Optimal: 0.42)
  - Violation: None (perfect match)
  - Penalty Contribution: 0.0

Total Penalty: 0.050
Material Quality Multiplier: 0.95

Result:
- IsUsable: true
- Substitution Penalty: 5%
- Effect: Excellent sword material, near-perfect
- Edge retention: Excellent (hard, holds edge)
- Durability: Good (sufficient toughness)
```

### Example 3: Copper Spring (Unusable Material)

**Material**: Copper
**Role**: Spring

```
Property Evaluation:
- Hardness: 0.15 (Required: 0.65-0.90, Optimal: 0.75)
  - Violation: TooLow, Deviation: 0.77 (massively below minimum)
  - Penalty Contribution: 0.77 × 0.20 × 2.0 = 0.308

- Toughness: 0.40 (Required: 0.45-0.80, Optimal: 0.60)
  - Violation: TooLow, Deviation: 0.11
  - Penalty Contribution: 0.11 × 0.20 × 2.0 = 0.044

- Ductility: 0.95 (Required: 0.25-0.55, Optimal: 0.40)
  - Violation: TooHigh, Deviation: 0.73 (way too ductile)
  - Penalty Contribution: 0.73 × 0.10 × 0.5 = 0.037

- ElasticLimit: 0.10 (Required: 0.75-1.0, Optimal: 0.90)
  - Violation: TooLow, Deviation: 0.87 (CRITICAL FAILURE)
  - Penalty Contribution: 0.87 × 0.45 × 2.0 = 0.783 (massive)

- Density: 0.45 (Required: 0.35-0.55, Optimal: 0.42)
  - Violation: SubOptimal, Deviation: 0.071
  - Penalty Contribution: negligible

Total Penalty: 1.172
Material Quality Multiplier: -0.172 (capped at 0.0)

Result:
- IsUsable: false (below 0.3 threshold)
- Substitution Penalty: 117%
- Effect: CANNOT make a functional spring from copper
- Problem: Too soft, too ductile, no elastic memory
- Spring would permanently deform after first use
```

### Example 4: Spring Steel Spring (Perfect Material)

**Material**: Spring Steel
**Role**: Spring

```
Property Evaluation:
- Hardness: 0.75 (Required: 0.65-0.90, Optimal: 0.75)
  - Violation: None (perfect match)
  - Penalty Contribution: 0.0

- Toughness: 0.55 (Required: 0.45-0.80, Optimal: 0.60)
  - Violation: SubOptimal, Deviation: 0.083
  - Penalty Contribution: 0.083 × 0.20 × 0.3 = 0.005

- Ductility: 0.40 (Required: 0.25-0.55, Optimal: 0.40)
  - Violation: None (perfect match)
  - Penalty Contribution: 0.0

- ElasticLimit: 0.90 (Required: 0.75-1.0, Optimal: 0.90)
  - Violation: None (perfect match)
  - Penalty Contribution: 0.0

- Density: 0.42 (Required: 0.35-0.55, Optimal: 0.42)
  - Violation: None (perfect match)
  - Penalty Contribution: 0.0

Total Penalty: 0.005
Material Quality Multiplier: 0.995

Result:
- IsUsable: true
- Substitution Penalty: 0.5% (negligible)
- Effect: Perfect spring material
- Performance: Excellent elastic memory, long-lasting
```

### Example 5: Titanium Plate Armor (Advanced Material)

**Material**: Titanium
**Role**: Armor.Plate

```
Property Evaluation:
- Hardness: 0.75 (Required: 0.55-0.90, Optimal: 0.70)
  - Violation: SubOptimal, Deviation: 0.071
  - Penalty Contribution: 0.071 × 0.30 × 0.3 = 0.006

- Toughness: 0.85 (Required: 0.50-1.0, Optimal: 0.70)
  - Violation: TooHigh, Deviation: 0.21
  - Penalty Contribution: 0.21 × 0.35 × 0.5 = 0.037

- Ductility: 0.35 (Required: 0.30-0.70, Optimal: 0.50)
  - Violation: SubOptimal, Deviation: 0.30
  - Penalty Contribution: 0.30 × 0.20 × 0.3 = 0.018

- ElasticLimit: 0.70 (Required: 0.35-0.75, Optimal: 0.55)
  - Violation: SubOptimal, Deviation: 0.27
  - Penalty Contribution: 0.27 × 0.10 × 0.3 = 0.008

- Density: 0.25 (Required: 0.40-0.65, Optimal: 0.50)
  - Violation: TooLow, Deviation: 0.375 (lightweight problem)
  - Penalty Contribution: 0.375 × 0.05 × 2.0 = 0.038

Total Penalty: 0.107
Material Quality Multiplier: 0.893

Result:
- IsUsable: true
- Substitution Penalty: 10.7%
- Effect: Excellent armor, lightweight but slightly less protective than steel
- Advantage: Low density (0.25) = much lighter armor, better mobility
- Tradeoff: Slightly less impact resistance than heavier steel
```

---

## Crafting Integration

### Combined Quality Calculation

```csharp
public static float CalculateFinalItemQuality(
    MaterialProperties material,
    CraftingRoleRequirements role,
    CrafterStats crafter,
    RecipeQualityParameters recipe)
{
    // 1. Validate material for role
    MaterialValidationResult materialValidation = ValidateMaterialForRole(material, role);

    if (!materialValidation.IsUsable)
    {
        return 0.0f; // Cannot craft with this material
    }

    // 2. Calculate crafter quality (from existing system)
    float crafterQuality = CalculateCrafterQuality(crafter, recipe);

    // 3. Material quality contribution (30% weight)
    float materialContribution = materialValidation.QualityMultiplier * 0.30f;

    // 4. Crafter skill contribution (60% weight)
    float crafterContribution = crafterQuality * 0.60f;

    // 5. Recipe base quality (10% weight)
    float recipeContribution = recipe.BaseQuality * 0.10f;

    // 6. Combine all factors
    float finalQuality = materialContribution + crafterContribution + recipeContribution;

    // 7. Apply tech tier bonus (if material is higher tier than required)
    if (material.RequiredTier > recipe.MinimumTechTier)
    {
        int tierDifference = (int)material.RequiredTier - (int)recipe.MinimumTechTier;
        float techBonus = tierDifference * 0.05f; // +5% per tier above requirement
        finalQuality += techBonus;
    }

    return math.clamp(finalQuality, 0.0f, 1.0f);
}
```

### Example Combined Calculation

**Scenario**: Blacksmith crafting a longsword

**Inputs**:
- Material: High-Carbon Steel (QualityMultiplier: 0.95)
- Crafter: Expert Blacksmith (Skill: 0.75)
- Recipe: Longsword (Base Quality: 0.5, Difficulty: 0.6)

**Calculation**:
```
Material Contribution = 0.95 × 0.30 = 0.285
Crafter Contribution = 0.75 × 0.60 = 0.450
Recipe Contribution = 0.50 × 0.10 = 0.050
Tech Tier Bonus = 0 (steel is minimum tier for longswords)

Final Quality = 0.285 + 0.450 + 0.050 = 0.785 (Epic tier)
```

**Result**: Epic-tier High-Carbon Steel Longsword
- Durability: 1.6× base
- Performance: 1.35× base damage
- Edge Retention: 1.6× (stays sharp 60% longer)

---

## System Implementation

### Material Registry System

```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct MaterialRegistrySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Initialize material database
        InitializeMaterialDatabase(ref state);
    }

    private void InitializeMaterialDatabase(ref SystemState state)
    {
        // Create material entities
        CreateMaterialEntity(ref state, "Copper", new MaterialProperties
        {
            MaterialName = "Copper",
            Category = MaterialCategory.Metal,
            RequiredTier = TechTier.Bronze,
            Hardness = 0.15f,
            Toughness = 0.40f,
            Ductility = 0.95f,
            ElasticLimit = 0.10f,
            Density = 0.45f,
            Temperable = 0.05f,
            CorrosionResist = 0.60f,
            RedHardness = 0.15f
        });

        CreateMaterialEntity(ref state, "High-Carbon Steel", new MaterialProperties
        {
            MaterialName = "High-Carbon Steel",
            Category = MaterialCategory.Metal,
            RequiredTier = TechTier.Advanced,
            Hardness = 0.80f,
            Toughness = 0.45f,
            Ductility = 0.30f,
            ElasticLimit = 0.60f,
            Density = 0.42f,
            Temperable = 0.85f,
            CorrosionResist = 0.10f,
            RedHardness = 0.55f
        });

        // ... add all materials
    }

    private void CreateMaterialEntity(ref SystemState state, string name, MaterialProperties props)
    {
        Entity materialEntity = state.EntityManager.CreateEntity();

        // Calculate derived properties
        props.TensileStrength = (props.Hardness * 0.6f) + (props.Ductility * 0.4f);
        props.CompressiveStrength = (props.Hardness * 0.7f) + (props.Density * 0.3f);
        props.ShearStrength = (props.Hardness * 0.5f) + (props.Toughness * 0.5f);
        props.FatigueResistance = (props.ElasticLimit * 0.6f) + (props.Toughness * 0.4f);

        state.EntityManager.AddComponentData(materialEntity, props);
        state.EntityManager.SetName(materialEntity, name);
    }
}
```

### Material Validation System

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(CraftingResolutionSystem))]
public partial struct MaterialValidationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Validate pending crafting orders
        foreach (var (craftingOrder, entity) in SystemAPI.Query<RefRW<CraftingOrder>>().WithEntityAccess())
        {
            if (craftingOrder.ValueRO.Status != CraftingStatus.PendingValidation)
                continue;

            // Get material and role requirements
            MaterialProperties material = GetMaterial(ref state, craftingOrder.ValueRO.MaterialID);
            CraftingRoleRequirements role = GetRoleRequirements(ref state, craftingOrder.ValueRO.RoleID);

            // Validate material
            MaterialValidationResult result = ValidateMaterialForRole(material, role);

            // Update crafting order
            if (result.IsUsable)
            {
                craftingOrder.ValueRW.Status = CraftingStatus.Validated;
                craftingOrder.ValueRW.MaterialQualityMultiplier = result.QualityMultiplier;
            }
            else
            {
                craftingOrder.ValueRW.Status = CraftingStatus.InvalidMaterial;
                craftingOrder.ValueRW.FailureReason = "Material unsuitable for this role";

                // Log violations for player feedback
                foreach (var violation in result.Violations)
                {
                    LogMaterialViolation(entity, violation);
                }
            }
        }
    }
}
```

---

## Gameplay Examples

### Example 1: Bronze Age Weaponsmith

**Context**: Early game, only Bronze available

**Attempted Craft**: Longsword
**Available Materials**: Copper (terrible), Bronze (suboptimal)

**Player Choice**: Use Bronze despite 40% penalty
- **Reasoning**: Only option available
- **Result**: Functional bronze sword, requires frequent sharpening
- **Progression Hook**: Motivates tech research toward Iron Age

### Example 2: Medieval Blacksmith

**Context**: Mid game, Steel unlocked

**Attempted Craft**: Chain Mail
**Available Materials**: Wrought Iron, Med-Carbon Steel, High-Carbon Steel

**Material Comparison**:
```
Wrought Iron:
- Hardness: 0.40 (low, but acceptable for mail)
- Ductility: 0.80 (excellent, rings won't break)
- Quality Multiplier: 0.92 (8% penalty)
- Cost: Low

High-Carbon Steel:
- Hardness: 0.80 (too high for mail)
- Ductility: 0.30 (too low, rings will be brittle)
- Quality Multiplier: 0.65 (35% penalty)
- Cost: High

Med-Carbon Steel:
- Hardness: 0.65 (good)
- Ductility: 0.50 (acceptable)
- Quality Multiplier: 0.88 (12% penalty)
- Cost: Medium
```

**Optimal Choice**: Wrought Iron
- **Reasoning**: High ductility is critical for chain mail
- **Result**: Excellent chain mail that flexes without breaking
- **Lesson**: Harder ≠ Better (material must match role)

### Example 3: Industrial Engineer

**Context**: Late game, advanced materials unlocked

**Attempted Craft**: Precision Spring (for suspension system)
**Available Materials**: Med-Carbon Steel, Spring Steel, Titanium

**Material Comparison**:
```
Med-Carbon Steel:
- ElasticLimit: 0.50 (below 0.75 minimum)
- Quality Multiplier: 0.45 (55% penalty)
- Result: Barely functional, will permanently deform

Spring Steel:
- ElasticLimit: 0.90 (perfect for springs)
- Quality Multiplier: 0.995 (negligible penalty)
- Result: Perfect spring, excellent elastic memory

Titanium:
- ElasticLimit: 0.70 (below optimal)
- Quality Multiplier: 0.82 (18% penalty)
- Advantage: Lightweight, corrosion-resistant
- Result: Functional but suboptimal, good for weight-critical applications
```

**Player Choice Context**:
- **Ground Vehicle**: Choose Spring Steel (best performance)
- **Aircraft**: Choose Titanium (weight matters more than optimal elasticity)
- **Marine Environment**: Choose Titanium (corrosion resistance critical)

---

## Tech Progression Integration

### Material Unlocks by Tech Tier

| Tech Tier | Unlocked Materials | Typical Use Cases |
|-----------|-------------------|-------------------|
| **Primitive (0)** | Stone, Bone, Wood, Hide | Tools, primitive weapons, shelter |
| **Bronze (1)** | Copper, Bronze, Tin | Early metalworking, trade goods |
| **Iron (2)** | Wrought Iron, Cast Iron | Construction, agricultural tools, basic weapons |
| **Steel (3)** | Med-Carbon Steel | Quality weapons, armor, tools |
| **Advanced (4)** | High-Carbon Steel, Spring Steel | Precision tools, superior weapons, mechanical parts |
| **Industrial (5)** | Stainless Steel, Tool Steel | Industrial equipment, resistant components |
| **Modern (6)** | Titanium, Advanced Alloys | Aerospace, high-performance applications |
| **FutureTech (7)** | Tungsten, Exotic Materials | Extreme conditions, cutting-edge tech |

### Research Requirements

```csharp
public struct MaterialResearchRequirement : IComponentData
{
    public FixedString64Bytes MaterialName;
    public TechTier RequiredTier;
    public DynamicBuffer<ResearchPrerequisite> Prerequisites;
    public float ResearchCost;                   // Science/Research points
    public ushort ResearchTicks;                 // Time to unlock
}

// Example: Unlocking High-Carbon Steel
MaterialResearchRequirement HighCarbonSteelResearch = new()
{
    MaterialName = "High-Carbon Steel",
    RequiredTier = TechTier.Advanced,
    Prerequisites = new[]
    {
        new ResearchPrerequisite { Tech = "Blast Furnace", Level = 2 },
        new ResearchPrerequisite { Tech = "Advanced Metallurgy", Level = 1 },
        new ResearchPrerequisite { Tech = "Heat Treatment", Level = 2 }
    },
    ResearchCost = 5000f,
    ResearchTicks = 10000
};
```

---

## Endgame Material Knowledge System

### Knowledge Complexity & Loss

**CRITICAL DESIGN PRINCIPLE**: Endgame materials are **extremely difficult to work with**. Knowledge is fragile, easily lost, and requires rare expertise.

### 1. Material Working Knowledge Components

```csharp
// Individual's knowledge of working with a specific material
public struct MaterialWorkingKnowledge : IBufferElementData
{
    public FixedString64Bytes MaterialName;
    public float Proficiency;                    // 0.0-1.0 (how well they can work this material)
    public MaterialKnowledgeLevel Level;         // Novice, Apprentice, Journeyman, Master, Grandmaster
    public ushort TicksExperience;               // How long they've worked with this material
    public DynamicBuffer<MaterialTechnique> KnownTechniques; // Specific techniques mastered
    public bool CanTeach;                        // Can they teach others?
    public float TeachingEffectiveness;          // 0.0-1.0 (how well they teach)
}

public enum MaterialKnowledgeLevel : byte
{
    Novice = 0,         // 0.0-0.2: Ruins material frequently, low success rate
    Apprentice = 1,     // 0.2-0.4: Can make basic items with supervision
    Journeyman = 2,     // 0.4-0.6: Can work independently, occasional failures
    Master = 3,         // 0.6-0.8: High success rate, can teach apprentices
    Grandmaster = 4,    // 0.8-1.0: Peak mastery, can innovate new techniques
    Legendary = 5       // 1.0+: Once-in-a-generation expertise (with bonuses)
}

// Specific techniques for working with materials
public struct MaterialTechnique : IBufferElementData
{
    public FixedString64Bytes TechniqueName;    // "Void Glass Annealing", "Mythril Forging"
    public float Difficulty;                     // 0.0-1.0 (how hard to learn)
    public float FailureConsequence;             // 0.0-1.0 (how bad failures are)
    public DynamicBuffer<Entity> KnownBy;        // Which entities know this technique
    public bool IsLostKnowledge;                 // No living entity knows this anymore
}

// Material-specific working difficulty
public struct MaterialWorkingDifficulty : IComponentData
{
    public FixedString64Bytes MaterialName;
    public float BaseDifficulty;                 // 0.0-1.0 (how hard to learn)
    public float LearningCurve;                  // 0.0-1.0 (how steep the learning curve)
    public float FailureRate;                    // 0.0-1.0 (chance of ruining material)
    public float InjuryRisk;                     // 0.0-1.0 (chance of injuring crafter)
    public float MinimumProficiencyToAttempt;    // Below this = auto-fail
    public ushort TicksToLearnBasics;            // Apprentice supervision time
    public ushort TicksToMaster;                 // Total practice time to master
}
```

### 2. Material Working Difficulty Tables

| Material | BaseDifficulty | LearningCurve | FailureRate | InjuryRisk | MinProficiency | TicksToLearnBasics | TicksToMaster |
|----------|----------------|---------------|-------------|------------|----------------|-------------------|---------------|
| **Copper** | 0.10 | 0.15 | 0.05 | 0.01 | 0.0 | 500 | 5,000 |
| **Bronze** | 0.20 | 0.25 | 0.10 | 0.02 | 0.0 | 1,000 | 10,000 |
| **Wrought Iron** | 0.30 | 0.35 | 0.15 | 0.05 | 0.0 | 2,000 | 15,000 |
| **High-Carbon Steel** | 0.60 | 0.65 | 0.30 | 0.10 | 0.2 | 10,000 | 80,000 |
| **Titanium** | 0.75 | 0.80 | 0.40 | 0.15 | 0.4 | 20,000 | 150,000 |
| **Mithril** | 0.80 | 0.85 | 0.50 | 0.20 | 0.5 | 30,000 | 250,000 |
| **Void Glass** | 0.95 | 0.98 | 0.85 | 0.60 | 0.7 | 80,000 | 500,000 |
| **Crystal Adamant** | 0.92 | 0.95 | 0.75 | 0.40 | 0.65 | 60,000 | 400,000 |
| **Mythril Alloy** | 0.90 | 0.93 | 0.70 | 0.35 | 0.6 | 50,000 | 350,000 |
| **Elder Dragon Bone** | 0.88 | 0.90 | 0.65 | 0.50 | 0.6 | 45,000 | 300,000 |
| **Primordial Ore** | 0.94 | 0.96 | 0.80 | 0.55 | 0.7 | 70,000 | 450,000 |

**Time Context**:
- At 60 ticks/minute: 500,000 ticks = **8,333 minutes = 138 hours = 17 workdays**
- Void Glass mastery: **17 dedicated workdays of practice** (assuming no failures)
- With 85% failure rate: **~113 workdays of actual attempts** to reach mastery

**Failure Consequences**:
```csharp
public enum MaterialWorkingFailure : byte
{
    MinorFlaw,          // Item has reduced quality (-10% to -30%)
    MajorFlaw,          // Item has severe quality penalty (-40% to -60%)
    MaterialRuined,     // Material completely destroyed, no salvage
    CrafterInjured,     // Crafter takes damage, loses work time
    CatastrophicFailure // Material destroyed + crafter seriously injured + nearby damage
}

// Example: Void Glass failure consequences
// 60% injury risk, 85% failure rate
// Novice attempting Void Glass work:
// - 85% chance to ruin material (material lost)
// - 60% chance of injury per attempt
// - Expected outcome: Lose material, get injured, learn nothing
// - Required: Master supervision OR high intelligence/wisdom to learn from failures

// Grandmaster attempting Void Glass work:
// - 15% base failure rate × (1.0 - 0.85 proficiency) = 2.25% failure rate
// - 60% injury risk × (1.0 - 0.85 proficiency) = 9% injury risk
// - Still dangerous, but manageable
```

### 3. Knowledge Propagation & Loss

```csharp
// Tracks how many entities know each material working technique
public struct MaterialKnowledgeRegistry : IComponentData
{
    public FixedString64Bytes MaterialName;
    public ushort TotalMasters;                  // How many masters exist
    public ushort TotalJourneymen;
    public ushort TotalApprentices;
    public DynamicBuffer<Entity> AllPractitioners;
    public KnowledgeEndangermentLevel Endangerment;
    public Entity LastMaster;                    // If only one master remains
}

public enum KnowledgeEndangermentLevel : byte
{
    Widespread,         // 10+ masters, knowledge is safe
    Common,             // 5-10 masters, knowledge is stable
    Uncommon,           // 2-5 masters, knowledge is vulnerable
    Endangered,         // 1 master, knowledge could be lost
    CriticallyEndangered, // 0 masters, 1+ journeymen (incomplete knowledge)
    Lost                // No practitioners, knowledge extinct
}

// Knowledge loss events
public struct MaterialKnowledgeLossEvent : IComponentData
{
    public FixedString64Bytes MaterialName;
    public Entity LostPractitioner;              // Who died/forgot/retired
    public MaterialKnowledgeLevel LostLevel;     // What level of knowledge was lost
    public bool WasLastMaster;                   // Was this the last master?
    public KnowledgeEndangermentLevel NewStatus;
    public DynamicBuffer<MaterialTechnique> LostTechniques; // Techniques now lost
}
```

### 4. Teaching & Learning System

```csharp
// Teaching relationship for material knowledge
public struct MaterialMentorship : IComponentData
{
    public Entity Teacher;
    public Entity Student;
    public FixedString64Bytes MaterialName;
    public float ProgressToNextLevel;            // 0.0-1.0
    public ushort TicksSpentLearning;
    public ushort FailedAttempts;
    public ushort SuccessfulAttempts;
    public bool TeacherAvailable;                // Is teacher alive/willing?
}

// Learning rate calculation
public static float CalculateLearningRate(
    MaterialWorkingDifficulty materialDifficulty,
    float studentIntelligence,
    float studentWisdom,
    float teacherProficiency,
    float teacherEffectiveness,
    bool hasTeacher)
{
    float baseLearningRate = 0.001f; // 0.1% per tick baseline

    // Material difficulty penalty
    float difficultyModifier = 1.0f - (materialDifficulty.LearningCurve * 0.8f);
    // Void Glass: 1.0 - (0.98 × 0.8) = 0.216 (78% slower learning)

    // Intelligence & Wisdom bonus
    float intelligenceBonus = studentIntelligence * 0.5f; // Up to +50%
    float wisdomBonus = studentWisdom * 0.3f;             // Up to +30%

    // Teacher effectiveness (CRITICAL for endgame materials)
    float teacherModifier = 1.0f;
    if (hasTeacher)
    {
        teacherModifier = 1.0f + (teacherProficiency * teacherEffectiveness * 2.0f);
        // Master teacher (0.8 prof, 0.8 effectiveness): 1.0 + (0.8 × 0.8 × 2.0) = 2.28× faster
    }
    else
    {
        // Self-teaching penalty (very harsh for difficult materials)
        teacherModifier = 0.1f + (studentIntelligence * 0.2f); // 0.1× to 0.3× speed
        // Attempting Void Glass without teacher: ~90% slower learning
    }

    // Learning from failures (wisdom-dependent)
    float failureLearningRate = studentWisdom * 0.1f; // High wisdom learns from mistakes

    float finalLearningRate = baseLearningRate
                            × difficultyModifier
                            × (1.0f + intelligenceBonus + wisdomBonus)
                            × teacherModifier;

    return math.clamp(finalLearningRate, 0.0001f, 0.01f); // 0.01% to 1% per tick
}

// Example: Learning Void Glass
// Scenario 1: Apprentice with Master Teacher
// - Intelligence: 0.6, Wisdom: 0.5
// - Teacher: Proficiency 0.85, Effectiveness 0.7
// - Difficulty Modifier: 0.216
// - Intelligence Bonus: 0.3, Wisdom Bonus: 0.15
// - Teacher Modifier: 1 + (0.85 × 0.7 × 2.0) = 2.19×
// - Final Rate: 0.001 × 0.216 × 1.45 × 2.19 = 0.000686 per tick
// - Time to Journeyman: ~145,000 ticks (40 hours / 5 workdays)

// Scenario 2: Self-Teaching (No Master Exists)
// - Intelligence: 0.8, Wisdom: 0.7
// - Teacher Modifier: 0.1 + (0.8 × 0.2) = 0.26× (self-teaching penalty)
// - Final Rate: 0.001 × 0.216 × 1.65 × 0.26 = 0.000093 per tick
// - Time to Journeyman: ~1,075,000 ticks (298 hours / 37 workdays)
// - CRITICAL: 85% failure rate means ~247 workdays of actual attempts
// - Realistic outcome: Most give up or die trying
```

### 5. Knowledge Loss Examples

#### Example 1: Last Void Glass Master Dies

```
Initial State:
- Void Glass Masters: 1 (Grandmaster Thane, age 78)
- Void Glass Journeymen: 2 (can make basic items, 0.55 proficiency)
- Void Glass Apprentices: 1 (learning basics, 0.25 proficiency)

Event: Grandmaster Thane dies in dragon attack

Consequences:
- Knowledge Status: Endangered → Critically Endangered
- Lost Techniques: "Void Glass Perfect Annealing" (only Thane knew this)
- New Highest Proficiency: 0.55 (Journeyman Kara)
- Effect on Crafting:
  - Journeymen can still make Void Glass daggers
  - Quality cap reduced: Legendary → Rare (without perfect annealing)
  - Failure rate increases: 15% → 35% (without master's techniques)
  - Teaching effectiveness drops: No master to teach new apprentices

Recovery Options:
1. Journeyman Kara dedicates life to rediscovering lost techniques
   - Expected time: 300,000 ticks (83 hours / 10 workdays) to reach Master
   - Risk: 35% failure rate = high material waste
   - Risk: May never rediscover "Perfect Annealing" (was Thane's innovation)

2. Search for other Void Glass masters in distant lands
   - May find another master (rare)
   - May find ancient texts (Intelligence check to decipher)

3. Accept degraded knowledge
   - Continue making "good" Void Glass items instead of "legendary"
   - Knowledge permanently diminished
```

#### Example 2: Mythril Alloy Knowledge Becomes Endangered

```
Initial State:
- Mythril Alloy Masters: 3
- Mythril Alloy Journeymen: 8
- Material Status: Common (knowledge stable)

Disaster: Plague kills 2 of 3 masters, 5 journeymen

New State:
- Mythril Alloy Masters: 1 (Master Eldrin, age 54)
- Mythril Alloy Journeymen: 3
- Material Status: Endangered (single point of failure)

Player Options:
1. Protect Master Eldrin at all costs
   - Assign bodyguards
   - Keep him away from combat/danger
   - Prioritize his survival over others

2. Accelerate apprentice training
   - Master Eldrin takes 2 new apprentices
   - Reduces his crafting output (teaching time)
   - Investment in future knowledge security

3. Document knowledge in texts
   - Eldrin writes comprehensive manual
   - Requires high Intelligence to learn from text alone
   - Slower than in-person teaching, but preserves knowledge

4. Do nothing, accept risk
   - If Eldrin dies, knowledge drops to Critically Endangered
   - Remaining journeymen struggle to maintain proficiency
```

#### Example 3: Crystal Adamant Knowledge Lost, Then Rediscovered

```
Event Timeline:

Year 0: Crystal Adamant Knowledge Lost
- Last master killed in war
- No journeymen survive
- All practitioners dead
- Knowledge Status: Lost

Year 120: Ancient Ruins Discovered
- Party finds pre-war smithy
- Discovers Crystal Adamant text (partially damaged)
- Intelligence check to decipher: Difficulty 0.85

Outcome: Brilliant Scholar Succeeds (Intelligence 0.9)
- Deciphers 70% of techniques
- Missing: Critical annealing temperatures, alloy ratios
- Can attempt Crystal Adamant working, but:
  - Failure rate: 90% (vs 75% with full knowledge)
  - Quality cap: Uncommon (vs Epic with master knowledge)
  - No teacher, learning from trial and error

Rediscovery Process:
- Scholar experiments with 50 Crystal Adamant ingots
- 45 failures (90% failure rate)
- 5 successes (poor to common quality)
- Over 200,000 ticks (55 hours / 7 workdays)
- Proficiency raised to 0.35 (Apprentice level)
- Still missing critical techniques
- Knowledge Status: Critically Endangered (incomplete)

Long-term:
- Takes 3 generations of experimentation to fully rediscover
- Some techniques may never be recovered (lost innovations)
- Rediscovered knowledge may differ from original (new techniques developed)
```

### 6. Material Knowledge as Strategic Resource

**Gameplay Implications**:

1. **Knowledge Hoarding**: Masters refuse to teach (personal advantage)
   - Monopoly on high-value crafting
   - Can charge premium prices
   - Risk: Knowledge dies with them

2. **Knowledge Theft**: Spies steal material working techniques
   - Requires high espionage skill
   - May only get partial knowledge (incomplete)
   - Can trigger wars over stolen secrets

3. **Knowledge Trade**: Cultures trade material working knowledge
   - "We'll teach you Void Glass working for access to your Primordial Ore deposits"
   - Creates dependencies between cultures
   - Alliance through knowledge sharing

4. **Knowledge Preservation**: Cultures invest in documentation
   - Build libraries/archives
   - Train multiple masters (redundancy)
   - Create cultural traditions around critical materials

5. **Lost Knowledge Quests**: Players hunt for ancient knowledge
   - Explore ruins for forgotten techniques
   - Decipher ancient texts (Intelligence checks)
   - Find surviving masters in remote locations
   - Recover artifacts that hint at lost techniques

**Example Strategic Scenario**:

```
Player's Situation:
- Owns 50 Void Glass ingots (extremely rare)
- No Void Glass masters in civilization
- Nearest Void Glass master: 500km away, in enemy territory
- Ancient Void Glass text available (damaged, 60% complete)

Options:

1. Diplomatic Mission to Enemy
   - Risk: Rejected, imprisoned, or worse
   - Cost: Major concessions (territory, resources, alliances)
   - Benefit: Full knowledge, proper teaching

2. Espionage to Steal Knowledge
   - Risk: Caught = war, dead spy
   - Cost: Spy resources, time
   - Benefit: Partial knowledge (maybe 70% effective)

3. Self-Teaching from Text
   - Risk: 90% failure rate = lose 45 of 50 ingots
   - Cost: Massive material waste, time
   - Benefit: Independent knowledge (no foreign dependence)

4. Wait for Knowledge to Spread Naturally
   - Risk: Void Glass ingots sit unused for decades
   - Cost: Opportunity cost (enemies get stronger)
   - Benefit: Eventually knowledge may spread via trade/migration

5. Sell/Trade Void Glass Ingots
   - Risk: Lose access to endgame material
   - Cost: Strategic disadvantage
   - Benefit: Immediate resources (fund other development)
```

---

## Balance Considerations

### Material Cost vs Performance

**Design Principle**: Higher-tier materials should be:
1. **Harder to acquire** (rare resources, advanced tech)
2. **More expensive** to process (fuel, skilled labor)
3. **Significantly better** for specialized roles
4. **Not always optimal** for all roles (context matters)

**Example Progression**:
```
Bronze Sword:
- Material Cost: 10 copper, 2 tin
- Crafting Cost: 50 labor
- Quality Multiplier: 0.60 (40% penalty vs optimal)
- Damage: 12 (base 20 × 0.60)

Med-Carbon Steel Sword:
- Material Cost: 15 iron, 2 coal
- Crafting Cost: 100 labor
- Quality Multiplier: 0.92 (8% penalty vs optimal)
- Damage: 18.4 (base 20 × 0.92)

High-Carbon Steel Sword:
- Material Cost: 20 iron, 5 coal, 1 manganese
- Crafting Cost: 200 labor (requires expert smith)
- Quality Multiplier: 0.95 (5% penalty)
- Damage: 19 (base 20 × 0.95)
```

**Takeaway**: High-Carbon Steel is only 3% better than Med-Carbon, but costs 2× more. Player must decide if marginal improvement is worth the cost.

### Quality Tier Impact

```
Common Bronze Sword:
- Material: Bronze (0.60 multiplier)
- Quality: Common (0.9 performance multiplier)
- Final Damage: 20 × 0.60 × 0.9 = 10.8

Legendary Bronze Sword:
- Material: Bronze (0.60 multiplier)
- Quality: Legendary (1.5 performance multiplier)
- Final Damage: 20 × 0.60 × 1.5 = 18

Common High-Carbon Steel Sword:
- Material: High-Carbon Steel (0.95 multiplier)
- Quality: Common (0.9 performance multiplier)
- Final Damage: 20 × 0.95 × 0.9 = 17.1

Legendary High-Carbon Steel Sword:
- Material: High-Carbon Steel (0.95 multiplier)
- Quality: Legendary (1.5 performance multiplier)
- Final Damage: 20 × 0.95 × 1.5 = 28.5
```

**Takeaway**:
- Legendary Bronze (18) beats Common Steel (17.1) → Craftsmanship matters
- Legendary Steel (28.5) far exceeds Legendary Bronze (18) → Material + Craftsmanship = best

---

## Integration with Existing Systems

### Crafting Quality System Integration

From [CraftingQualitySystem.md](CraftingQualitySystem.md):

```csharp
// Enhanced CraftingOrder with material validation
public struct CraftingOrder : IBufferElementData
{
    public Entity Crafter;
    public BlobAssetReference<RecipeQualityParameters> Recipe;
    public FixedList32Bytes<MaterialStackRef> Materials;

    // NEW: Material validation results
    public float MaterialQualityMultiplier;      // From MaterialValidationSystem
    public DynamicBuffer<PropertyViolation> MaterialViolations;

    public ushort Priority;
    public float Progress;
    public CraftingStatus Status;
}

// Enhanced final quality calculation
float finalScore = baseQuality
                 + materialFactor * MaterialQualityScore  // Purity from original system
                 + crafterFactor * CrafterQualityModifier
                 + enhancementBonus
                 + materialSuitability * MaterialQualityMultiplier; // NEW: Material validation
```

### Item Quality Framework Integration

From [ItemQualityFramework.md](ItemQualityFramework.md):

```csharp
// Enhanced ItemPart with material properties
public struct ItemPart : IBufferElementData
{
    public PartTypeId PartType;
    public FixedString64Bytes Material;          // Material name
    public Entity MaterialEntity;                // NEW: Reference to material properties
    public byte QualityTier;
    public float Durability;
    public byte RarityWeight;

    // NEW: Material property snapshot (for performance)
    public float MaterialHardness;
    public float MaterialToughness;
    public float MaterialDensity;
}

// Durability calculation enhanced with material properties
public static float CalculatePartDurability(ItemPart part, float damageAmount)
{
    // Base durability from quality
    float baseDurability = part.Durability;

    // Material toughness resists damage
    float damageResistance = part.MaterialToughness;

    // Hardness affects wear rate
    float wearRate = 1.0f - (part.MaterialHardness * 0.5f);

    // Calculate effective damage
    float effectiveDamage = damageAmount * wearRate * (1.0f - damageResistance * 0.3f);

    return baseDurability - effectiveDamage;
}
```

---

## Future Extensions

### 1. Environmental Material Degradation

```csharp
public struct EnvironmentalDegradation : IComponentData
{
    public float CorrosionRate;                  // Based on MaterialProperties.CorrosionResist
    public float TemperatureDamage;              // Based on MaterialProperties.RedHardness
    public float HumidityDamage;                 // Iron rusts faster in wet climates
    public float SaltExposure;                   // Marine environments accelerate corrosion
}

// Example: Iron sword in swamp vs desert
// Swamp: High humidity + moderate temp = 2× corrosion rate
// Desert: Low humidity + high temp = 0.5× corrosion rate, but heat stress
```

### 2. Composite Materials

```csharp
public struct CompositeMaterial : IComponentData
{
    public Entity PrimaryMaterial;               // Core material (e.g., steel)
    public Entity SecondaryMaterial;             // Coating/layer (e.g., chrome plating)
    public float LayerThickness;                 // 0.0-1.0 (affects protection)

    // Blended properties
    public float EffectiveHardness;              // = Primary.Hardness × (1 - LayerThickness) + Secondary.Hardness × LayerThickness
    public float EffectiveCorrosionResist;       // = max(Primary.CorrosionResist, Secondary.CorrosionResist)
}

// Example: Chrome-plated steel
// - Core: Med-Carbon Steel (CorrosionResist: 0.15)
// - Coating: Chromium (CorrosionResist: 0.95)
// - Result: Excellent corrosion resistance, retains steel's toughness
```

### 3. Material Fatigue Tracking

```csharp
public struct MaterialFatigue : IComponentData
{
    public float CycleCount;                     // Number of stress cycles
    public float FatigueAccumulation;            // 0.0-1.0 (1.0 = critical failure imminent)
    public float StressAmplitude;                // Peak stress level per cycle

    // Fatigue resistance from material properties
    public float FatigueLimit;                   // = MaterialProperties.FatigueResistance
}

// Example: Spring after 10,000 compressions
// - Spring Steel: FatigueResistance 0.85, FatigueAccumulation: 0.12 (12%)
// - Med-Carbon Steel: FatigueResistance 0.60, FatigueAccumulation: 0.67 (67%, near failure)
```

### 4. Magical/Exotic Material Enhancements

```csharp
public struct ExoticMaterialProperties : IComponentData
{
    public MaterialProperties BaseProperties;
    public DynamicBuffer<MagicalAffinity> Affinities;
    public float ManaConduction;                 // How well it channels magic
    public float EnchantmentCapacity;            // Max enchantment strength it can hold
}

// Example: Mithril
// - Physical: Hardness 0.85, Toughness 0.90, Density 0.15 (lightweight)
// - Magical: ManaConduction 0.95, EnchantmentCapacity 0.90
// - Result: Accepts powerful enchantments, better than steel physically + magically
```

---

## Testing & Validation

### Unit Tests

```csharp
[Test]
public void ValidateMaterialForRole_BronzeSword_ReturnsSuboptimal()
{
    MaterialProperties bronze = CreateBronzeMaterial();
    CraftingRoleRequirements bladeLong = CreateBladeLongRequirements();

    MaterialValidationResult result = ValidateMaterialForRole(bronze, bladeLong);

    Assert.IsTrue(result.IsUsable, "Bronze should be usable for swords");
    Assert.IsTrue(result.SubstitutionPenalty > 0.3f, "Bronze should have significant penalty");
    Assert.IsTrue(result.QualityMultiplier < 0.7f, "Bronze quality should be below 70%");

    // Check for expected violations
    Assert.Contains(PropertyType.Hardness, result.Violations.Select(v => v.Property));
}

[Test]
public void ValidateMaterialForRole_CopperSpring_ReturnsUnusable()
{
    MaterialProperties copper = CreateCopperMaterial();
    CraftingRoleRequirements spring = CreateSpringRequirements();

    MaterialValidationResult result = ValidateMaterialForRole(copper, spring);

    Assert.IsFalse(result.IsUsable, "Copper should NOT be usable for springs");
    Assert.IsTrue(result.SubstitutionPenalty > 1.0f, "Copper spring penalty should exceed 100%");

    // Check for critical elastic limit violation
    PropertyViolation elasticViolation = result.Violations.First(v => v.Property == PropertyType.ElasticLimit);
    Assert.AreEqual(ViolationType.TooLow, elasticViolation.Type);
    Assert.IsTrue(elasticViolation.Deviation > 0.8f, "Elastic limit deviation should be severe");
}

[Test]
public void QualityScaling_LegendaryBronzeVsCommonSteel_CorrectDamage()
{
    // Legendary Bronze Sword
    float legendaryBronzeDamage = CalculateDamage(
        baseDamage: 20f,
        materialMultiplier: 0.60f,  // Bronze penalty
        qualityMultiplier: 1.5f     // Legendary
    );

    // Common Steel Sword
    float commonSteelDamage = CalculateDamage(
        baseDamage: 20f,
        materialMultiplier: 0.95f,  // Steel near-optimal
        qualityMultiplier: 0.9f     // Common
    );

    Assert.AreEqual(18f, legendaryBronzeDamage, 0.1f);
    Assert.AreEqual(17.1f, commonSteelDamage, 0.1f);
    Assert.IsTrue(legendaryBronzeDamage > commonSteelDamage,
                  "Legendary Bronze should beat Common Steel");
}
```

### Integration Tests

```csharp
[Test]
public void CraftingPipeline_BronzeSwordWithMasterSmith_ProducesRareTierItem()
{
    // Setup
    Entity crafter = CreateMasterBlacksmith(skillLevel: 0.85f);
    MaterialProperties bronze = GetMaterial("Bronze");
    RecipeQualityParameters swordRecipe = GetRecipe("Longsword");

    // Execute crafting
    CraftingOrder order = SubmitCraftingOrder(crafter, bronze, swordRecipe);
    TickCraftingSystem(order.TicksRequired);

    // Verify result
    Entity craftedSword = GetCraftedItem(order);
    CraftedItemQuality quality = GetComponent<CraftedItemQuality>(craftedSword);

    Assert.AreEqual(QualityTier.Rare, quality.Tier,
                    "Master smith with bronze should produce Rare tier");
    Assert.IsTrue(quality.QualityScore > 0.6f && quality.QualityScore < 0.75f);
}
```

---

## Documentation & Player Feedback

### In-Game Material Inspector

```csharp
public struct MaterialInspectorUI
{
    public void DisplayMaterialInfo(MaterialProperties material)
    {
        // Display material name and tier
        UI.Header($"{material.MaterialName} (Tier {material.RequiredTier})");

        // Display properties with visual bars
        UI.PropertyBar("Hardness", material.Hardness, color: GetPropertyColor(material.Hardness));
        UI.PropertyBar("Toughness", material.Toughness, color: GetPropertyColor(material.Toughness));
        UI.PropertyBar("Ductility", material.Ductility, color: GetPropertyColor(material.Ductility));
        UI.PropertyBar("Elastic Limit", material.ElasticLimit, color: GetPropertyColor(material.ElasticLimit));
        UI.PropertyBar("Density", material.Density, color: GetPropertyColor(material.Density));

        // Display derived properties
        UI.Separator();
        UI.Text($"Tensile Strength: {material.TensileStrength:F2}");
        UI.Text($"Fatigue Resistance: {material.FatigueResistance:F2}");

        // Display suitability for common roles
        UI.Separator();
        UI.Header("Suitability for Roles:");
        DisplayRoleSuitability(material, "Blade.Long");
        DisplayRoleSuitability(material, "Axe.Head");
        DisplayRoleSuitability(material, "Mail.Ring");
        DisplayRoleSuitability(material, "Spring");
    }

    private void DisplayRoleSuitability(MaterialProperties material, string roleID)
    {
        CraftingRoleRequirements role = GetRoleRequirements(roleID);
        MaterialValidationResult validation = ValidateMaterialForRole(material, role);

        if (!validation.IsUsable)
        {
            UI.Text($"❌ {roleID}: Unsuitable", color: Color.Red);
        }
        else if (validation.QualityMultiplier > 0.9f)
        {
            UI.Text($"✓✓✓ {roleID}: Excellent ({validation.QualityMultiplier:P0})", color: Color.Green);
        }
        else if (validation.QualityMultiplier > 0.7f)
        {
            UI.Text($"✓✓ {roleID}: Good ({validation.QualityMultiplier:P0})", color: Color.Yellow);
        }
        else
        {
            UI.Text($"✓ {roleID}: Suboptimal ({validation.QualityMultiplier:P0})", color: Color.Orange);
        }
    }
}
```

### Crafting Feedback System

```csharp
public void DisplayCraftingResult(CraftingOrder order, Entity craftedItem)
{
    CraftedItemQuality quality = GetComponent<CraftedItemQuality>(craftedItem);
    MaterialValidationResult materialValidation = order.MaterialValidation;

    // Display overall result
    UI.Header($"{quality.Tier} {order.Recipe.ItemName} Crafted!");
    UI.Text($"Quality Score: {quality.QualityScore:P0}");

    // Breakdown contributions
    UI.Separator();
    UI.Header("Quality Breakdown:");
    UI.Text($"Material Suitability: {materialValidation.QualityMultiplier:P0}");
    UI.Text($"Crafter Skill: {order.CrafterQualityContribution:P0}");
    UI.Text($"Recipe Difficulty: {order.RecipeContribution:P0}");

    // Material violations (if any)
    if (materialValidation.Violations.Length > 0)
    {
        UI.Separator();
        UI.Header("Material Issues:");
        foreach (var violation in materialValidation.Violations)
        {
            string message = FormatViolationMessage(violation);
            UI.Text($"⚠ {message}", color: Color.Orange);
        }
        UI.Text("\nTip: Use a more suitable material to increase quality.", color: Color.Gray);
    }
}

private string FormatViolationMessage(PropertyViolation violation)
{
    switch (violation.Type)
    {
        case ViolationType.TooLow:
            return $"{violation.Property} too low (penalty: {violation.PenaltyContribution:P0})";
        case ViolationType.TooHigh:
            return $"{violation.Property} too high (penalty: {violation.PenaltyContribution:P0})";
        case ViolationType.SubOptimal:
            return $"{violation.Property} suboptimal (penalty: {violation.PenaltyContribution:P0})";
        default:
            return $"{violation.Property} incompatible";
    }
}
```

---

*Last Updated: November 25, 2025*
*Document Owner: Crafting Systems Team*
