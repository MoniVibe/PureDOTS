using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Policies
{
    public enum TaxProfileSourceMode : byte
    {
        None = 0,
        SocietyAverage = 1,
        RulerOnly = 2,
        SocietyAndRulerBlend = 3
    }

    /// <summary>
    /// Tax policy component.
    /// Income brackets, business tax, transaction tax rates.
    /// </summary>
    public struct TaxPolicy : IComponentData
    {
        public Entity TargetEntity; // Village, Guild, faction, etc.
        public Entity RulerEntity;
        public Entity LeakageSinkEntity;

        public TaxProfileSourceMode ProfileMode;
        public float SocietyInfluence01;
        public float RulerInfluence01;
        public float ProfileElasticity;

        public float BaseIncomeTaxRate;
        public float BusinessProfitTaxRate;
        public float ExemptionFloor;
        public float Progressivity;
        public float ProgressivityIncomeScale;
        public float MaxEffectiveRate;

        public float BaseCompliance01;
        public float EnforcementStrength01;
        public float CorruptionLeakage01;
    }

    /// <summary>
    /// Optional progressive tax brackets. Thresholds should be configured in ascending order.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct TaxBracket : IBufferElementData
    {
        public float Threshold;
        public float MarginalRate;
    }

    /// <summary>
    /// Per-entity taxable income inputs for current tick.
    /// </summary>
    public struct TaxableIncome : IComponentData
    {
        public float PersonalIncome;
        public float BusinessIncome;
        public float DeductibleAmount;
        public float ExemptIncome;
        public uint LastAssessedTick;
    }

    /// <summary>
    /// Last tax assessment details for telemetry, audits, and unrest integration.
    /// </summary>
    public struct TaxAssessmentState : IComponentData
    {
        public float EffectiveRate;
        public float Compliance01;
        public float AssessedTax;
        public float CollectedTax;
        public float LeakageTax;
        public float TaxableBase;
        public uint LastAssessmentTick;
    }

    /// <summary>
    /// Runtime aggregate metrics for a tax policy per tick.
    /// </summary>
    public struct TaxPolicyRuntimeState : IComponentData
    {
        public uint LastUpdateTick;
        public uint AssessmentCount;
        public float TotalTaxableBase;
        public float TotalAssessedTax;
        public float TotalCollectedTax;
        public float TotalLeakageTax;
        public float AverageEffectiveRate;
        public float AverageCompliance01;
        public float AverageProfileFactor;
    }

    /// <summary>
    /// Budget policy component.
    /// Allocation percentages: education, welfare, military, etc.
    /// </summary>
    public struct BudgetPolicy : IComponentData
    {
        public Entity TargetEntity;
        public float EducationAllocation;
        public float WelfareAllocation;
        public float MilitaryAllocation;
        public float InfrastructureAllocation;
    }

    /// <summary>
    /// Tariff policy component.
    /// Import/export tariffs by GoodType.
    /// </summary>
    public struct TariffPolicy : IComponentData
    {
        public Entity TargetEntity;
        public float ImportTariffRate;
        public float ExportTariffRate;
    }

    /// <summary>
    /// Embargo policy component.
    /// Forbidden goods/partners, enforcement level.
    /// </summary>
    public struct EmbargoPolicy : IComponentData
    {
        public Entity TargetEntity;
        public float EnforcementLevel; // 0-1
    }

    /// <summary>
    /// Loan record component.
    /// Lender, borrower, principal, interest, payment schedule.
    /// </summary>
    public struct LoanRecord : IComponentData
    {
        public Entity Lender;
        public Entity Borrower;
        public float Principal;
        public float InterestRate;
        public float RemainingPrincipal;
        public float AccruedInterest;
        public uint NextPaymentTick;
        public uint MaturityTick;
    }

    /// <summary>
    /// Enforcement profile component.
    /// Patrol strength, corruption, legal severity.
    /// </summary>
    public struct EnforcementProfile : IComponentData
    {
        public Entity TargetEntity;
        public float PatrolStrength;
        public float Corruption;
        public float LegalSeverity;
    }
}
