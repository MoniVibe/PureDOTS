using PureDOTS.Runtime.Platform;
using PureDOTS.Runtime.Platform.Blobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Platform
{
    /// <summary>
    /// Tracks reactor modules per segment and aggregates reactor state.
    /// Marks segments with ReactorPresent flag.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlatformReactorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ModuleDefRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var moduleRegistry = SystemAPI.GetSingleton<ModuleDefRegistry>();

            if (!moduleRegistry.Registry.IsCreated)
            {
                return;
            }

            ref var moduleRegistryBlob = ref moduleRegistry.Registry.Value;

            foreach (var (moduleSlots, segmentStates) in SystemAPI.Query<
                DynamicBuffer<PlatformModuleSlot>,
                DynamicBuffer<PlatformSegmentState>>())
            {
                UpdateReactorFlags(
                    ref segmentStates,
                    in moduleSlots,
                    ref moduleRegistryBlob);
            }
        }

        [BurstCompile]
        private static void UpdateReactorFlags(
            ref DynamicBuffer<PlatformSegmentState> segmentStates,
            in DynamicBuffer<PlatformModuleSlot> moduleSlots,
            ref ModuleDefRegistryBlob moduleRegistry)
        {
            var segmentHasReactor = new NativeHashMap<int, bool>(segmentStates.Length, Allocator.Temp);

            for (int i = 0; i < moduleSlots.Length; i++)
            {
                var slot = moduleSlots[i];
                if (slot.State == ModuleSlotState.Destroyed || slot.ModuleId < 0)
                {
                    continue;
                }

                if (slot.ModuleId >= moduleRegistry.Modules.Length)
                {
                    continue;
                }

                ref var moduleDef = ref moduleRegistry.Modules[slot.ModuleId];
                if (moduleDef.Category == ModuleCategory.Utility)
                {
                    segmentHasReactor[slot.SegmentIndex] = true;
                }
            }

            for (int i = 0; i < segmentStates.Length; i++)
            {
                var segmentState = segmentStates[i];
                var segmentIndex = segmentState.SegmentIndex;

                if (segmentHasReactor.TryGetValue(segmentIndex, out var hasReactor) && hasReactor)
                {
                    segmentState.Status |= SegmentStatusFlags.ReactorPresent;
                }
                else
                {
                    segmentState.Status &= ~SegmentStatusFlags.ReactorPresent;
                }

                segmentStates[i] = segmentState;
            }
        }
    }
}

