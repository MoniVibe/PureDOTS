#if UNITY_EDITOR
using System;
using PureDOTS.Runtime.Space;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Authoring.Space
{
    [DisallowMultipleComponent]
    public sealed class CarrierModuleLoadoutAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct ModuleDefinition
        {
            public ModuleFamily family;
            public ModuleClass moduleClass;
            public string moduleName;
            public float mass;
            public float powerRequired;
            public float powerGeneration;
            [Range(0, 100)] public byte efficiencyPercent;
            public ModuleState state;
            [Range(0, 100)] public byte failureThreshold;
            [Range(0, 10)] public byte repairPriority;
            public float passiveDegradationPerSecond;
            public float activeDegradationPerSecond;
            public float cargoCapacity;
            public float miningRate;
            public float repairRateBonus;
        }

        [Serializable]
        public struct SlotDefinition
        {
            public MountType mountType;
            public MountSize mountSize;
            public ModuleDefinition module;
        }

        [Header("Refit / Repair")]
        public float fieldRefitRate = 3f;
        public float stationRefitRate = 8f;
        public bool atRefitFacilityOnStart = true;
        [Min(0f)] public float maxPowerOutput = 12f;

        [Header("Slots")]
        public SlotDefinition[] slots = Array.Empty<SlotDefinition>();

        [Header("Telemetry")]
        [Tooltip("Log when power budget is exceeded on bake to aid validation.")]
        public bool logPowerBudget = true;
    }

    public sealed class CarrierModuleLoadoutBaker : Baker<CarrierModuleLoadoutAuthoring>
    {
        public override void Bake(CarrierModuleLoadoutAuthoring authoring)
        {
            var carrier = GetEntity(authoring, TransformUsageFlags.Dynamic);

            AddComponent(carrier, new CarrierRefitState
            {
                FieldRefitRate = math.max(0f, authoring.fieldRefitRate),
                StationRefitRate = math.max(0f, authoring.stationRefitRate),
                AtRefitFacility = authoring.atRefitFacilityOnStart
            });

            AddComponent<CarrierModuleStatTotals>(carrier);
            AddComponent(carrier, new CarrierPowerBudget
            {
                MaxPowerOutput = math.max(0f, authoring.maxPowerOutput),
                CurrentDraw = 0f,
                CurrentGeneration = 0f,
                OverBudget = false
            });
            var slotsBuffer = AddBuffer<CarrierModuleSlot>(carrier);
            AddBuffer<ModuleRepairTicket>(carrier);
            AddBuffer<CarrierModuleRefitRequest>(carrier);

            if (authoring.slots == null || authoring.slots.Length == 0)
            {
                return;
            }

            for (byte i = 0; i < authoring.slots.Length; i++)
            {
                var slotDef = authoring.slots[i];
                var moduleEntity = CreateModuleEntity(carrier, slotDef.module, slotDef.mountType, slotDef.mountSize);

                slotsBuffer.Add(new CarrierModuleSlot
                {
                    SlotIndex = i,
                    Type = slotDef.mountType,
                    Size = slotDef.mountSize,
                    InstalledModule = moduleEntity
                });
            }

            if (authoring.logPowerBudget && TryCalculatePower(authoring.slots, out var draw, out var generation) && draw > authoring.maxPowerOutput)
            {
                UnityEngine.Debug.LogWarning($"CarrierModuleLoadoutAuthoring: Power draw {draw:F2} exceeds max output {authoring.maxPowerOutput:F2} on '{authoring.name}'. Refit/repair will respect over-budget gating at runtime.", authoring);
            }
        }

        private Entity CreateModuleEntity(Entity carrier, CarrierModuleLoadoutAuthoring.ModuleDefinition def, MountType mountType, MountSize mountSize)
        {
            var module = CreateAdditionalEntity(TransformUsageFlags.Dynamic);

            AddComponent(module, new Parent { Value = carrier });
            AddComponent(module, new ShipModule
            {
                Family = def.family,
                Class = def.moduleClass,
                RequiredMount = mountType,
                RequiredSize = mountSize,
                ModuleName = new FixedString64Bytes(string.IsNullOrWhiteSpace(def.moduleName) ? def.moduleClass.ToString() : def.moduleName.Trim()),
                Mass = math.max(0f, def.mass),
                PowerRequired = math.max(0f, def.powerRequired),
                PowerGeneration = math.max(0f, def.powerGeneration),
                EfficiencyPercent = (byte)math.clamp((int)def.efficiencyPercent, 0, 100),
                State = def.state
            });

            AddComponent(module, new ModuleStatModifier
            {
                Mass = math.max(0f, def.mass),
                PowerDraw = math.max(0f, def.powerRequired),
                PowerGeneration = math.max(0f, def.powerGeneration),
                CargoCapacity = math.max(0f, def.cargoCapacity),
                MiningRate = math.max(0f, def.miningRate),
                RepairRateBonus = math.max(0f, def.repairRateBonus)
            });

            AddComponent(module, new ModuleHealth
            {
                Integrity = 100,
                FailureThreshold = (byte)math.clamp((int)def.failureThreshold, 0, 100),
                RepairPriority = (byte)math.clamp((int)def.repairPriority, 0, 10),
                Flags = 0
            });

            AddComponent(module, new ModuleDegradation
            {
                PassivePerSecond = math.max(0f, def.passiveDegradationPerSecond),
                ActivePerSecond = math.max(0f, def.activeDegradationPerSecond),
                CombatMultiplier = 1f
            });

            return module;
        }

        private bool TryCalculatePower(CarrierModuleLoadoutAuthoring.SlotDefinition[] slots, out float draw, out float generation)
        {
            draw = 0f;
            generation = 0f;

            foreach (var slot in slots)
            {
                var module = slot.module;
                draw += math.max(0f, module.powerRequired);
                generation += math.max(0f, module.powerGeneration);
            }

            return true;
        }
    }
}
#endif
