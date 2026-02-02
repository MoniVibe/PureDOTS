using System;
using Unity.Entities;

namespace PureDOTS.Runtime.Modules
{
    public enum ModulePosture : byte
    {
        Off = 0,
        Standby = 1,
        Online = 2,
        Emergency = 3
    }

    public enum ModuleCapabilityKind : byte
    {
        None = 0,
        ThrustAuthority = 1,
        TurnAuthority = 2
    }

    public enum EngineClass : byte
    {
        Civilian = 0,
        Military = 1,
        Capital = 2,
        Experimental = 3,
        Other = 255
    }

    public enum EngineFuelType : byte
    {
        Chemical = 0,
        Ion = 1,
        Fusion = 2,
        Antimatter = 3,
        Exotic = 4,
        Other = 255
    }

    public enum EngineIntakeType : byte
    {
        None = 0,
        Scoop = 1,
        Ramjet = 2,
        ReactorFeed = 3,
        Other = 255
    }

    public enum EngineVectoringMode : byte
    {
        Fixed = 0,
        Gimbaled = 1,
        Vectored = 2,
        Omnidirectional = 3,
        Other = 255
    }

    public struct ModuleSpecBlob
    {
        public float PowerDrawOff;
        public float PowerDrawStandby;
        public float PowerDrawOnline;
        public float PowerDrawEmergency;
        public float TauColdToOnline;
        public float TauWarmToOnline;
        public float TauOnlineToStandby;
        public float TauStandbyToOff;
        public float MaxOutput;
        public float RampRateLimit;
        public ModuleCapabilityKind Capability;
    }

    public struct ModuleSpec : IComponentData
    {
        public BlobAssetReference<ModuleSpecBlob> Spec;
    }

    public struct ModuleRuntimeState : IComponentData
    {
        public ModulePosture Posture;
        public float NormalizedOutput;
        public float TargetOutput;
        public float TimeInState;
    }

    public struct ModulePowerRequest : IComponentData
    {
        public float RequestedPower;
    }

    public struct ModulePowerAllocation : IComponentData
    {
        public float AllocatedPower;
        public float SupplyRatio;
    }

    [Flags]
    public enum ModuleCommandFlags : byte
    {
        None = 0,
        Posture = 1 << 0,
        TargetOutput = 1 << 1,
        LockPosture = 1 << 2
    }

    [InternalBufferCapacity(2)]
    public struct ModuleCommand : IBufferElementData
    {
        public ModulePosture Posture;
        public float TargetOutput;
        public ModuleCommandFlags Flags;
    }

    [InternalBufferCapacity(4)]
    public struct ModuleAttachment : IBufferElementData
    {
        public Entity Module;
    }

    public struct ModuleOwner : IComponentData
    {
        public Entity Owner;
    }

    public struct ModulePowerSupply : IComponentData
    {
        public float AvailablePower;
    }

    public struct ModulePowerBudget : IComponentData
    {
        public float TotalRequested;
        public float TotalAllocated;
        public float SupplyRatio;
    }

    public struct ModuleCapabilityOutput : IComponentData
    {
        public float ThrustAuthority;
        public float TurnAuthority;
    }

    public struct EngineProfile : IComponentData
    {
        public EngineClass Class;
        public EngineFuelType FuelType;
        public EngineIntakeType IntakeType;
        public EngineVectoringMode VectoringMode;
        public float TechLevel; // 0-1
        public float Quality; // 0-1
        public float ThrustScalar; // <= 0 uses auto scale
        public float TurnScalar; // <= 0 uses auto scale
        public float ResponseRating; // 0-1
        public float EfficiencyRating; // 0-1
        public float BoostRating; // 0-1
        public float VectoringRating; // 0-1

        public static EngineProfile Default => new EngineProfile
        {
            Class = EngineClass.Civilian,
            FuelType = EngineFuelType.Chemical,
            IntakeType = EngineIntakeType.None,
            VectoringMode = EngineVectoringMode.Fixed,
            TechLevel = 0.5f,
            Quality = 0.5f,
            ThrustScalar = 0f,
            TurnScalar = 0f,
            ResponseRating = 0.5f,
            EfficiencyRating = 0.5f,
            BoostRating = 0.5f,
            VectoringRating = 0.5f
        };
    }

    public struct EnginePerformanceOutput : IComponentData
    {
        public float ThrustAuthority;
        public float TurnAuthority;
        public float Response;
        public float Efficiency;
        public float Boost;
        public float TechLevel;
        public float Quality;
        public float Vectoring;
    }

    public struct EngineeringCohesion : IComponentData
    {
        public float Value;
    }

    public struct NavigationCohesion : IComponentData
    {
        public float Value;
    }

    public struct BridgeTechLevel : IComponentData
    {
        public float Value;
    }
}
