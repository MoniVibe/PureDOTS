using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Village;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Computes village job preference weights based on outlook flags and alignment axes.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct VillageJobPreferenceSystem : ISystem
    {
        private ComponentLookup<VillageAlignmentState> _alignmentLookup;
        private ComponentLookup<VillageOutlook> _outlookLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _alignmentLookup = state.GetComponentLookup<VillageAlignmentState>(true);
            _outlookLookup = state.GetComponentLookup<VillageOutlook>(true);
            state.RequireForUpdate<VillageId>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<VillageId>>().WithNone<VillageJobPreferenceEntry>().WithEntityAccess())
            {
                ecb.AddBuffer<VillageJobPreferenceEntry>(entity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            _alignmentLookup.Update(ref state);
            _outlookLookup.Update(ref state);

            foreach (var (villageId, buffer, entity) in SystemAPI
                         .Query<RefRO<VillageId>, DynamicBuffer<VillageJobPreferenceEntry>>()
                         .WithEntityAccess())
            {
                ref readonly var alignment = ref _alignmentLookup.HasComponent(entity)
                    ? _alignmentLookup[entity]
                    : default;
                var outlookFlags = _outlookLookup.HasComponent(entity)
                    ? _outlookLookup[entity].Flags
                    : VillageOutlookFlags.None;

                buffer.Clear();

                AppendPreference(ref buffer, VillagerJob.JobType.Farmer, ComputeEssentialWeight(alignment, outlookFlags));
                AppendPreference(ref buffer, VillagerJob.JobType.Builder, ComputeBuilderWeight(alignment, outlookFlags));
                AppendPreference(ref buffer, VillagerJob.JobType.Gatherer, ComputeGathererWeight(alignment, outlookFlags));
                AppendPreference(ref buffer, VillagerJob.JobType.Hunter, ComputeHunterWeight(alignment, outlookFlags));
                AppendPreference(ref buffer, VillagerJob.JobType.Guard, ComputeGuardWeight(alignment, outlookFlags));
                AppendPreference(ref buffer, VillagerJob.JobType.Priest, ComputePriestWeight(alignment, outlookFlags));
                AppendPreference(ref buffer, VillagerJob.JobType.Merchant, ComputeMerchantWeight(alignment, outlookFlags));
                AppendPreference(ref buffer, VillagerJob.JobType.Crafter, ComputeCrafterWeight(alignment, outlookFlags));
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private static void AppendPreference(ref DynamicBuffer<VillageJobPreferenceEntry> buffer, VillagerJob.JobType jobType, float weight)
        {
            buffer.Add(new VillageJobPreferenceEntry
            {
                JobType = jobType,
                Weight = math.max(0.05f, weight)
            });
        }

        private static float ComputeEssentialWeight(in VillageAlignmentState alignment, VillageOutlookFlags flags)
        {
            var weight = 1f;
            weight += (-alignment.Materialism) * 0.5f; // ascetic villages prioritize essentials
            weight += math.saturate(alignment.Integrity) * 0.25f;
            if ((flags & VillageOutlookFlags.Ascetic) != 0)
            {
                weight += 0.5f;
            }
            if ((flags & VillageOutlookFlags.Materialistic) != 0)
            {
                weight -= 0.4f;
            }
            return weight;
        }

        private static float ComputeBuilderWeight(in VillageAlignmentState alignment, VillageOutlookFlags flags)
        {
            var weight = 1f;
            weight += math.saturate(alignment.Integrity) * 0.2f;
            if ((flags & VillageOutlookFlags.Expansionist) != 0)
            {
                weight += 0.4f;
            }
            return weight;
        }

        private static float ComputeGathererWeight(in VillageAlignmentState alignment, VillageOutlookFlags flags)
        {
            var weight = 1f;
            weight += (-alignment.Materialism) * 0.2f;
            return weight;
        }

        private static float ComputeHunterWeight(in VillageAlignmentState alignment, VillageOutlookFlags flags)
        {
            var weight = 1f;
            if ((flags & VillageOutlookFlags.Warlike) != 0)
            {
                weight += 0.5f;
            }
            return weight;
        }

        private static float ComputeGuardWeight(in VillageAlignmentState alignment, VillageOutlookFlags flags)
        {
            var weight = 1f;
            weight += math.saturate(-alignment.Integrity) * 0.2f; // corrupt guard up (enforcement)
            if ((flags & VillageOutlookFlags.Warlike) != 0)
            {
                weight += 0.8f;
            }
            return weight;
        }

        private static float ComputePriestWeight(in VillageAlignmentState alignment, VillageOutlookFlags flags)
        {
            var weight = 1f;
            if ((flags & VillageOutlookFlags.Spiritual) != 0)
            {
                weight += 0.6f;
            }
            return weight;
        }

        private static float ComputeMerchantWeight(in VillageAlignmentState alignment, VillageOutlookFlags flags)
        {
            var weight = 1f;
            weight += alignment.Materialism * 0.7f;
            if ((flags & VillageOutlookFlags.Materialistic) != 0)
            {
                weight += 0.8f;
            }
            if ((flags & VillageOutlookFlags.Ascetic) != 0)
            {
                weight -= 0.4f;
            }
            return weight;
        }

        private static float ComputeCrafterWeight(in VillageAlignmentState alignment, VillageOutlookFlags flags)
        {
            var weight = 1f;
            if ((flags & VillageOutlookFlags.Warlike) != 0)
            {
                weight += 0.5f;
            }
            if ((flags & VillageOutlookFlags.Materialistic) != 0)
            {
                weight += 0.3f;
            }
            return weight;
        }
    }
}
