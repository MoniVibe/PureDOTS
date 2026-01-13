# Combat Damage Component Schema (v0)

Owner: PureDOTS (shared schema). Do not place under C:\dev\Tri.

## Intent

Define the ECS schema that implements the Combat Damage and Integrity Contract. This is
data-only and deterministic, and it stays game-agnostic.

See also:
- [Combat Damage and Integrity Contract](Combat_Damage_Contract.md)
- [Core Combat Mechanics](../Core/Combat_Mechanics_Core.md)

## Existing Components to Reuse (PureDOTS)

From `PureDOTS.Runtime.Ships`:
- `HullState` (legacy hull HP, can map to segment hull if needed)
- `ShieldArcState` (legacy 8-arc shield HP)
- `ArmorDegradeState` (legacy 8-arc armor ablation)
- `ModuleRuntimeState` / `ModuleRuntimeStateElement`
- `ModuleRef`
- `HitEvent` (input hit events)
- `ModuleDamageEvent`
- `DamageKind`, `DamageRulesBlob`, `RefitRepairRulesBlob`

From `PureDOTS.Runtime.Identity`:
- `IntegrityState` (generic health)
- `EnergyPool`, `HeatState` (thermal and energy)

From `PureDOTS.Runtime.Identity` and `PureDOTS.Runtime.Agency`:
- `EntitySeat`, `EntitySeatAssignment`, `AuthoritySeat*` (seat authority)

This schema extends the above while keeping backward compatibility where practical.

## New or Extended Components (Proposed)

### Damage Event Stream (Superset of HitEvent)

Use `HitEvent` as input, then normalize into a richer `DamageEvent` + payload buffer.

```csharp
public struct DamageEvent : IBufferElementData
{
    public uint EventId;
    public uint Tick;
    public Entity Source;
    public Entity Target;
    public ushort SegmentHint;
    public float3 ImpactPositionLocal;
    public float3 ImpactNormalLocal;
    public float3 IncomingDirectionLocal;
    public float Impulse;
    public float Heat;
    public float SpreadRadius;
    public DamageEventFlags Flags;
    public ushort PayloadStart;
    public ushort PayloadCount;
}

[System.Flags]
public enum DamageEventFlags : byte
{
    None = 0,
    IsExplosion = 1 << 0,
    IsBeam = 1 << 1,
    IsPiercing = 1 << 2,
    IgnoresFriendlyFire = 1 << 3
}

public struct DamagePayloadElement : IBufferElementData
{
    public ushort DamageTypeIndex;
    public float Amount;
    public float Penetration;
    public float Bypass;
}
```

### Damage Type Catalog

```csharp
public struct DamageTypeDef
{
    public FixedString64Bytes Id;
    public DamageTypeFlags Flags;
}

[System.Flags]
public enum DamageTypeFlags : byte
{
    None = 0,
    Thermal = 1 << 0,
    EM = 1 << 1,
    Explosive = 1 << 2,
    Kinetic = 1 << 3,
    Corrosive = 1 << 4,
    Radiation = 1 << 5
}

public struct DamageTypeCatalogBlob
{
    public BlobArray<DamageTypeDef> Types;
}

public struct DamageTypeIndex : IComponentData
{
    public BlobAssetReference<DamageTypeCatalogBlob> Catalog;
}
```

### Segment Mapping

```csharp
public struct DamageSegmentDefinition : IBufferElementData
{
    public ushort SegmentId;
    public float3 LocalCenter;
    public float3 LocalExtents;
    public ushort ShieldProfileId;
    public ushort ArmorProfileId;
    public ushort HullProfileId;
    public byte Flags;
}

public struct DamageSegmentState : IBufferElementData
{
    public ushort SegmentId;
    public float ShieldStrength;
    public float ArmorIntegrity;
    public float HullIntegrity;
    public byte Flags;
    public uint LastDamageTick;
}

public struct DamageSegmentFlags
{
    public const byte Breached = 1 << 0;
    public const byte Vented = 1 << 1;
    public const byte OnFire = 1 << 2;
}

public struct ModuleSegmentLink : IComponentData
{
    public ushort SegmentId;
}
```

### Layer Profiles (Blobs)

```csharp
public struct DamageResistanceElement
{
    public ushort DamageTypeIndex;
    public float Resistance;
    public float Hardness;
    public float SeepThrough;
}

public struct ShieldProfileBlob
{
    public float MaxStrength;
    public float RegenPerSecond;
    public float CoverageAngleDeg;
    public byte CoverageMode;
    public BlobArray<DamageResistanceElement> Resistances;
}

public struct ArmorProfileBlob
{
    public float MaxIntegrity;
    public float AblationPerDamage;
    public BlobArray<DamageResistanceElement> Resistances;
}

public struct HullProfileBlob
{
    public float MaxIntegrity;
    public float BreachThreshold;
    public BlobArray<DamageResistanceElement> Resistances;
}

public struct DamageProfileIndex : IComponentData
{
    public BlobAssetReference<DamageProfileCatalogBlob> Catalog;
}

public struct DamageProfileCatalogBlob
{
    public BlobArray<ShieldProfileBlob> Shields;
    public BlobArray<ArmorProfileBlob> Armors;
    public BlobArray<HullProfileBlob> Hulls;
}
```

### Shield Holes

```csharp
public struct ShieldHoleState : IBufferElementData
{
    public ushort SegmentId;
    public float3 LocalDirection;
    public float AngleDeg;
    public float RemainingSeconds;
}
```

### Module Integrity and Faults

```csharp
public struct ModuleIntegrity : IComponentData
{
    public IntegrityState Integrity;
    public float FaultThreshold;
    public float CriticalThreshold;
}

public struct ModuleDamageSensitivity : IComponentData
{
    public ushort DamageTypeIndex;
    public float Multiplier;
}

public struct ModuleFaultState : IComponentData
{
    public byte IsFaulted;
    public byte IsDestroyed;
    public uint LastFaultTick;
}
```

### Combat Reaction Hooks (Data Only)

```csharp
public enum CombatPosture : byte
{
    Hold = 0,
    Cautious = 1,
    Aggressive = 2,
    Desperate = 3,
    Retreat = 4
}

public struct CombatPostureState : IComponentData
{
    public CombatPosture Value;
    public float RiskTolerance;
    public float RetreatThreshold;
}

public struct DamageControlPolicy : IComponentData
{
    public float ShieldPriority;
    public float RepairPriority;
    public float WeaponsPriority;
    public float VentingThreshold;
}
```

## Mapping Legacy Components

- `HitEvent` can be adapted into `DamageEvent` with a single payload entry and a default `DamageTypeIndex`.
- `ShieldArcState` and `ArmorDegradeState` can map to segments by treating arcs as 8 fixed segments.
- `HullState` can mirror `DamageSegmentState.HullIntegrity` for backward systems until full migration.

## System Order (Expected)

1) Hit intake -> normalize to `DamageEvent` + payload buffer.
2) Segment routing -> assign `SegmentId`.
3) Shield mitigation -> apply coverage, holes, power gating.
4) Armor mitigation -> resist/penetration/ablation.
5) Hull integrity -> breach detection and secondary events.
6) Module integrity -> spill-through + fault emission.
7) Regeneration/repair -> shield regen and integrity recovery.

## Notes

- All IDs are indices into catalogs or blobs. No runtime string lookups.
- All time is ticks or fixed delta time; no wall-clock usage.
