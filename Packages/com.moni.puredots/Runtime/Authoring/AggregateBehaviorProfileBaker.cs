using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring
{
    public class AggregateBehaviorProfileBaker : Baker<AggregateBehaviorProfileAsset>
    {
        public override void Bake(AggregateBehaviorProfileAsset authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var buildData = authoring.ToBuildData();
            var blob = BuildBlob(buildData);
            AddComponent(entity, new AggregateBehaviorProfile { Blob = blob });
        }

        private static BlobAssetReference<AggregateBehaviorProfileBlob> BuildBlob(AggregateBehaviorProfileBlob.BuildData data)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var profile = ref builder.ConstructRoot<AggregateBehaviorProfileBlob>();

            profile.CollectiveNeedWeight = data.CollectiveNeedWeight;
            profile.PersonalAmbitionWeight = data.PersonalAmbitionWeight;
            profile.EmergencyOverrideWeight = data.EmergencyOverrideWeight;
            profile.DisciplineResistanceWeight = data.DisciplineResistanceWeight;
            profile.ShortageThreshold = data.ShortageThreshold;
            profile.ConscriptionWeight = data.ConscriptionWeight;
            profile.DefenseEmergencyWeight = data.DefenseEmergencyWeight;
            profile.InitiativeIntervalTicks = data.InitiativeIntervalTicks;
            profile.InitiativeJitterTicks = data.InitiativeJitterTicks;
            profile.AllowConscriptionOverrides = data.AllowConscriptionOverrides;

            var lawKeys = data.LawfulnessComplianceCurve != null && data.LawfulnessComplianceCurve.length > 0
                ? data.LawfulnessComplianceCurve.keys
                : new[] { new Keyframe(-1f, 1f), new Keyframe(1f, 1f) };
            var lawArray = builder.Allocate(ref profile.LawfulnessComplianceCurve.Keys, lawKeys.Length);
            for (int i = 0; i < lawKeys.Length; i++)
            {
                lawArray[i] = new float2(lawKeys[i].time, lawKeys[i].value);
            }

            var chaosKeys = data.ChaosFreedomCurve != null && data.ChaosFreedomCurve.length > 0
                ? data.ChaosFreedomCurve.keys
                : new[] { new Keyframe(-1f, 1f), new Keyframe(1f, 1f) };
            var chaosArray = builder.Allocate(ref profile.ChaosFreedomCurve.Keys, chaosKeys.Length);
            for (int i = 0; i < chaosKeys.Length; i++)
            {
                chaosArray[i] = new float2(chaosKeys[i].time, chaosKeys[i].value);
            }

            var blobRef = builder.CreateBlobAssetReference<AggregateBehaviorProfileBlob>(Allocator.Persistent);
            builder.Dispose();
            return blobRef;
        }
    }
}
