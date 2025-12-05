using PureDOTS.Runtime.Platform;
using PureDOTS.Runtime.Platform.Blobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Platform
{
    /// <summary>
    /// Validates platform loadouts against hull constraints and computes aggregated stats.
    /// Enforces constraints for Mass/Hardpoint/Voxel layout modes.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LoadoutValidationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HullDefRegistry>();
            state.RequireForUpdate<ModuleDefRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hullRegistry = SystemAPI.GetSingleton<HullDefRegistry>();
            var moduleRegistry = SystemAPI.GetSingleton<ModuleDefRegistry>();

            if (!hullRegistry.Registry.IsCreated || !moduleRegistry.Registry.IsCreated)
            {
                return;
            }

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            var moduleSlotsLookup = state.GetBufferLookup<PlatformModuleSlot>(false);
            moduleSlotsLookup.Update(ref state);

            foreach (var (hullRef, kind, entity) in SystemAPI.Query<RefRO<PlatformHullRef>, RefRO<PlatformKind>>().WithEntityAccess())
            {
                if (!moduleSlotsLookup.HasBuffer(entity))
                {
                    continue;
                }

                var moduleSlots = moduleSlotsLookup[entity];
                var hullId = hullRef.ValueRO.HullId;
                ref var hullRegistryBlob = ref hullRegistry.Registry.Value;
                
                if (hullId < 0 || hullId >= hullRegistryBlob.Hulls.Length)
                {
                    continue;
                }

                ref var hullDef = ref hullRegistryBlob.Hulls[hullId];
                ref var moduleRegistryBlob = ref moduleRegistry.Registry.Value;

                var platformEntityRef = entity;
                ValidateAndUpdateLoadout(
                    ref state,
                    ref ecb,
                    ref platformEntityRef,
                    in hullDef,
                    ref moduleSlots,
                    in kind.ValueRO,
                    ref moduleRegistryBlob);
            }
        }

        [BurstCompile]
        private static void ValidateAndUpdateLoadout(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            ref Entity platformEntity,
            in HullDef hullDef,
            ref DynamicBuffer<PlatformModuleSlot> moduleSlots,
            in PlatformKind kind,
            ref ModuleDefRegistryBlob moduleRegistry)
        {
            var stats = new PlatformAggregatedStats();
            var totalMass = 0f;
            var totalVolume = 0f;
            var totalPowerDraw = 0f;
            var moduleCount = 0;
            var validSlotIndices = new NativeHashSet<short>(16, Allocator.Temp);
            var validCellIndices = new NativeHashSet<int>(16, Allocator.Temp);
            var needsUpdate = false;

            for (int i = 0; i < moduleSlots.Length; i++)
            {
                var slot = moduleSlots[i];
                
                if (slot.ModuleId < 0 || slot.ModuleId >= moduleRegistry.Modules.Length)
                {
                    if (slot.State != ModuleSlotState.Destroyed)
                    {
                        slot.State = ModuleSlotState.Offline;
                        moduleSlots[i] = slot;
                        needsUpdate = true;
                    }
                    continue;
                }

                ref var moduleDef = ref moduleRegistry.Modules[slot.ModuleId];
                var isValid = true;

                switch (hullDef.LayoutMode)
                {
                    case PlatformLayoutMode.MassOnly:
                        if (slot.SlotIndex != -1 || slot.CellIndex != -1)
                        {
                            isValid = false;
                        }
                        break;

                    case PlatformLayoutMode.Hardpoint:
                        if (slot.SlotIndex < 0 || slot.SlotIndex >= hullDef.HardpointCount)
                        {
                            isValid = false;
                        }
                        else if (validSlotIndices.Contains(slot.SlotIndex))
                        {
                            isValid = false;
                        }
                        else
                        {
                            validSlotIndices.Add(slot.SlotIndex);
                        }
                        break;

                    case PlatformLayoutMode.VoxelHull:
                        if (slot.CellIndex < 0 || slot.CellIndex >= hullDef.VoxelCellCount)
                        {
                            isValid = false;
                        }
                        else if (validCellIndices.Contains(slot.CellIndex))
                        {
                            isValid = false;
                        }
                        else
                        {
                            validCellIndices.Add(slot.CellIndex);
                        }
                        break;
                }

                if (!isValid)
                {
                    if (slot.State != ModuleSlotState.Destroyed)
                    {
                        slot.State = ModuleSlotState.Offline;
                        moduleSlots[i] = slot;
                        needsUpdate = true;
                    }
                    continue;
                }

                if (slot.State == ModuleSlotState.Installed || slot.State == ModuleSlotState.Damaged)
                {
                    totalMass += moduleDef.Mass;
                    totalVolume += moduleDef.Volume;
                    totalPowerDraw += moduleDef.PowerDraw;
                    moduleCount++;

                    if (moduleDef.Category == ModuleCategory.Engine)
                    {
                        stats.MaxThrust += ExtractThrust(ref moduleDef.CapabilityPayload);
                    }
                    else if (moduleDef.Category == ModuleCategory.Shield)
                    {
                        stats.ShieldStrength += ExtractShieldStrength(ref moduleDef.CapabilityPayload);
                        stats.ShieldCoverage += ExtractShieldCoverage(ref moduleDef.CapabilityPayload);
                    }
                    else if (moduleDef.Category == ModuleCategory.Hangar)
                    {
                        stats.HangarCapacity += ExtractHangarCapacity(ref moduleDef.CapabilityPayload);
                    }
                }
            }

            if (hullDef.LayoutMode == PlatformLayoutMode.MassOnly)
            {
                if (totalMass > hullDef.MassCapacity || moduleCount > hullDef.MaxModuleCount)
                {
                    needsUpdate = true;
                }
            }

            stats.TotalMass = totalMass;
            stats.PowerConsumed = totalPowerDraw;
            stats.MaxHP = hullDef.BaseHP;

            if (!state.EntityManager.HasComponent<PlatformAggregatedStats>(platformEntity))
            {
                ecb.AddComponent(platformEntity, stats);
            }
            else
            {
                ecb.SetComponent(platformEntity, stats);
            }

            validSlotIndices.Dispose();
            validCellIndices.Dispose();
        }

        [BurstCompile]
        private static float ExtractThrust(ref BlobArray<byte> payload)
        {
            if (payload.Length < 4)
                return 0f;
            var bytes = new uint4(payload[0], payload[1], payload[2], payload[3]);
            return math.asfloat(bytes.x);
        }

        [BurstCompile]
        private static float ExtractShieldStrength(ref BlobArray<byte> payload)
        {
            if (payload.Length < 4)
                return 0f;
            var bytes = new uint4(payload[0], payload[1], payload[2], payload[3]);
            return math.asfloat(bytes.x);
        }

        [BurstCompile]
        private static float ExtractShieldCoverage(ref BlobArray<byte> payload)
        {
            if (payload.Length < 8)
                return 0f;
            var bytes = new uint4(payload[4], payload[5], payload[6], payload[7]);
            return math.asfloat(bytes.x);
        }

        [BurstCompile]
        private static float ExtractHangarCapacity(ref BlobArray<byte> payload)
        {
            if (payload.Length < 4)
                return 0f;
            var bytes = new uint4(payload[0], payload[1], payload[2], payload[3]);
            return math.asfloat(bytes.x);
        }
    }
}

