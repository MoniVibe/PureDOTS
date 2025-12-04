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

            foreach (var (hullRef, moduleSlots, kind, entity) in SystemAPI.Query<RefRO<PlatformHullRef>, DynamicBuffer<PlatformModuleSlot>, RefRO<PlatformKind>>().WithEntityAccess())
            {
                var hullId = hullRef.ValueRO.HullId;
                ref var hullRegistryBlob = ref hullRegistry.Registry.Value;
                
                if (hullId < 0 || hullId >= hullRegistryBlob.Hulls.Length)
                {
                    continue;
                }

                ref var hullDef = ref hullRegistryBlob.Hulls[hullId];
                ref var moduleRegistryBlob = ref moduleRegistry.Registry.Value;

                ValidateAndUpdateLoadout(
                    ref state,
                    ref ecb,
                    entity,
                    in hullDef,
                    in moduleSlots,
                    in kind.ValueRO,
                    ref moduleRegistryBlob);
            }
        }

        [BurstCompile]
        private static void ValidateAndUpdateLoadout(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity platformEntity,
            in HullDef hullDef,
            in DynamicBuffer<PlatformModuleSlot> moduleSlots,
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
                        stats.MaxThrust += ExtractThrust(moduleDef.CapabilityPayload);
                    }
                    else if (moduleDef.Category == ModuleCategory.Shield)
                    {
                        stats.ShieldStrength += ExtractShieldStrength(moduleDef.CapabilityPayload);
                        stats.ShieldCoverage += ExtractShieldCoverage(moduleDef.CapabilityPayload);
                    }
                    else if (moduleDef.Category == ModuleCategory.Hangar)
                    {
                        stats.HangarCapacity += ExtractHangarCapacity(moduleDef.CapabilityPayload);
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

            if (!SystemAPI.HasComponent<PlatformAggregatedStats>(platformEntity))
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
        private static float ExtractThrust(BlobArray<byte> payload)
        {
            if (payload.Length < 4)
                return 0f;
            return math.asfloat(new uint4(payload[0], payload[1], payload[2], payload[3]));
        }

        [BurstCompile]
        private static float ExtractShieldStrength(BlobArray<byte> payload)
        {
            if (payload.Length < 4)
                return 0f;
            return math.asfloat(new uint4(payload[0], payload[1], payload[2], payload[3]));
        }

        [BurstCompile]
        private static float ExtractShieldCoverage(BlobArray<byte> payload)
        {
            if (payload.Length < 8)
                return 0f;
            return math.asfloat(new uint4(payload[4], payload[5], payload[6], payload[7]));
        }

        [BurstCompile]
        private static float ExtractHangarCapacity(BlobArray<byte> payload)
        {
            if (payload.Length < 4)
                return 0f;
            return math.asfloat(new uint4(payload[0], payload[1], payload[2], payload[3]));
        }
    }
}

