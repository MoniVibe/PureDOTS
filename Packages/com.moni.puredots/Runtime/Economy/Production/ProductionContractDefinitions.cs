using System;
using PureDOTS.Runtime.Alignment;

namespace PureDOTS.Runtime.Economy.Production
{
    [Serializable]
    public struct ManufacturingAlignment
    {
        public float Moral;
        public float Order;
        public float Purity;
    }

    [Serializable]
    public struct ManufacturingBehaviorProfile
    {
        public float Compliance;
        public float Caution;
        public float FormationAdherence;
        public float RiskTolerance;
        public float Aggression;
        public float Patience;
    }

    [Serializable]
    public struct ManufacturingAxisValue
    {
        public EthicAxis Axis;
        public float Value;
    }

    public static class ManufacturingContractDefaults
    {
        public const float FanaticThreshold = 0.75f;
        public const float FanaticSecondaryThreshold = 0.35f;

        public static readonly ManufacturingAlignment NeutralAlignment = new ManufacturingAlignment
        {
            Moral = 0f,
            Order = 0f,
            Purity = 0f
        };

        public static readonly ManufacturingBehaviorProfile NeutralBehavior = new ManufacturingBehaviorProfile
        {
            Compliance = 0.5f,
            Caution = 0.5f,
            FormationAdherence = 0.5f,
            RiskTolerance = 0.5f,
            Aggression = 0.5f,
            Patience = 0.5f
        };
    }

    [Serializable]
    public struct ManufacturerDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public float AssemblyPurityBase;
        public float XenoAffinityBase;
        public ManufacturingAlignment Alignment;
        public ManufacturingBehaviorProfile Behavior;
        public ManufacturingAxisValue[] OutlookAxes;
        public int RaceId;
        public int CultureId;
    }

    [Serializable]
    public struct OrganDefinition
    {
        public string Id;
        public string DisplayName;
        public string SlotType;
        public string ManufacturerId;
        public float Quality;
        public float Efficiency;
        public float Precision;
        public float Stability;
        public float Cooling;
        public float PowerDraw;
        public float Reliability;
    }

    [Serializable]
    public struct ModuleOrganSlotDefinition
    {
        public string SlotType;
        public int Count;
    }

    [Serializable]
    public struct ModuleFamilyDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public ModuleOrganSlotDefinition[] OrganSlots;
        public bool CustomMadeDefault;
    }

    [Serializable]
    public struct ConsumableDefinition
    {
        public string Id;
        public string DisplayName;
        public string Category;
        public int Charges;
        public string ManufacturerId;
        public float Quality;
    }

    [Serializable]
    public struct CrewRoleDefinition
    {
        public string Id;
        public string DisplayName;
        public string Role;
        public string[] Traits;
        public ManufacturingAlignment Alignment;
        public ManufacturingAxisValue[] OutlookAxes;
        public ManufacturingBehaviorProfile Behavior;
        public int RaceId;
        public int CultureId;
    }

    [Serializable]
    public struct ShipChassisDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string ManufacturerId;
        public string Role;
        public int SegmentSlotCount;
        public int ModuleSocketCount;
        public float BaseMassTons;
        public float BaseIntegrity;
    }

    [Serializable]
    public struct HullSegmentDefinition
    {
        public string Id;
        public string DisplayName;
        public string SegmentType;
        public string ManufacturerId;
        public int ModuleSocketCount;
        public float MassTons;
        public float IntegrityBonus;
        public float TurnRateMultiplier;
        public float AccelerationMultiplier;
        public float DecelerationMultiplier;
        public float MaxSpeedMultiplier;
    }

    [Serializable]
    public struct ShipModelDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string ChassisId;
        public string ManufacturerId;
        public string BlueprintId;
        public string[] DefaultSegmentIds;
        public string[] DefaultModuleIds;
        public string DefaultStaffingProfileId;
    }

    public static class FacilityAttachmentSlotTypeIds
    {
        public const string Production = "production";
        public const string Utility = "utility";
        public const string Power = "power";
    }

    public static class FacilityLimbTypeIds
    {
        public const string Production = "production";
        public const string Power = "power";
        public const string Cargo = "cargo";
        public const string Automation = "automation";
        public const string Head = "head";
        public const string Training = "training";
        public const string Relations = "relations";
        public const string Legal = "legal";
        public const string Executive = "executive";
    }

    [Serializable]
    public struct FacilityFamilyDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string FacilityClass;
    }

    [Serializable]
    public struct FacilityAttachmentSlotDefinition
    {
        public string SlotType;
        public int Count;
        public float MaxMassTons;
    }

    [Serializable]
    public struct FacilityOrganSlotDefinition
    {
        public string SlotType;
        public int Count;
    }

    [Serializable]
    public struct FacilityHullDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string FacilityFamilyId;
        public string ManufacturerId;
        public float BaseMassTons;
        public float BaseIntegrity;
        public float BaseQuality01;
        public FacilityAttachmentSlotDefinition[] AttachmentSlots;
        public FacilityOrganSlotDefinition[] OrganSlots;
    }

    [Serializable]
    public struct FacilityOrganDefinition
    {
        public string Id;
        public string DisplayName;
        public string SlotType;
        public string ManufacturerId;
        public float Quality;
        public float Efficiency;
        public float Throughput;
        public float Stability;
        public float PowerDraw;
        public float Reliability;
    }

    [Serializable]
    public struct FacilityLimbDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string LimbType;
        public string ManufacturerId;
        public FacilityOrganSlotDefinition[] OrganSlots;
        public string[] SupportedProcessIds;
        public string[] Tags;
        public int ProcessSlots;
        public int ParallelChainSlots;
        public float ThroughputMultiplier;
        public float QualityMultiplier;
        public float PowerDraw;
        public float PowerCapacityBonus;
        public float CargoCapacityBonus;
        public float MassTons;
        public float Quality01;
        public bool CustomMadeDefault;
    }

    [Serializable]
    public struct FacilityProcessInputDefinition
    {
        public string ResourceId;
        public float Quantity;
        public float MinPurity01;
        public float MinQuality01;
    }

    [Serializable]
    public struct FacilityProcessOutputDefinition
    {
        public string ResourceId;
        public float Quantity;
        public float QualityFloor01;
        public bool IsByproduct;
    }

    [Serializable]
    public struct FacilityProcessDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public ProductionStage Stage;
        public float BaseTimeSeconds;
        public float PowerCost;
        public float LaborCost;
        public int MinTechTier;
        public string[] AllowedLimbIds;
        public FacilityProcessInputDefinition[] Inputs;
        public FacilityProcessOutputDefinition[] Outputs;
    }

    [Serializable]
    public struct FacilityConstructionCostDefinition
    {
        public string ResourceId;
        public float UnitsRequired;
    }

    [Serializable]
    public struct FacilityInvestmentDefinition
    {
        public float InitialCapitalCredits;
        public float PermitCostCredits;
        public float MaintenanceBudgetPerSecond;
        public float PayrollBudgetPerSecond;
        public float PayrollVariance01;
        public float EmployerTaxRate01;
        public float EmployeeTaxWithholding01;
        public FacilityConstructionCostDefinition[] ResourceCosts;
    }

    [Serializable]
    public struct FacilityStaffRoleDefinition
    {
        public string RoleId;
        public int MinCount;
        public int MaxCount;
        public float WagePerSecond;
        public float SkillRequirement01;
    }

    [Serializable]
    public struct FacilityStaffingDefinition
    {
        public string DefaultStaffingProfileId;
        public FacilityStaffRoleDefinition[] Roles;
    }

    [Serializable]
    public struct FacilityModelDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string FacilityFamilyId;
        public string HullId;
        public string ManufacturerId;
        public string BlueprintId;
        public string[] DefaultLimbIds;
        public string[] DefaultProcessIds;
        public FacilityInvestmentDefinition Investment;
        public FacilityStaffingDefinition Staffing;
        public float ConstructionTimeSeconds;
    }
}
