using PureDOTS.Runtime.Culture;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Authoring.Culture
{
    /// <summary>
    /// Authoring component for cultural doctrine.
    /// </summary>
    public class CulturalDoctrineAuthoring : MonoBehaviour
    {
        public CulturalDoctrineAsset doctrineAsset;
    }

    /// <summary>
    /// Baker for cultural doctrine authoring.
    /// </summary>
    public class CulturalDoctrineBaker : Baker<CulturalDoctrineAuthoring>
    {
        public override void Bake(CulturalDoctrineAuthoring authoring)
        {
            if (authoring.doctrineAsset == null)
            {
                return;
            }

            var entity = GetEntity(authoring, TransformUsageFlags.None);

            // Build blob asset from ScriptableObject
            var blobRef = BuildCulturalDoctrineBlob(authoring.doctrineAsset);

            if (blobRef.IsCreated)
            {
                AddComponent(entity, new CulturalDoctrineReference
                {
                    Doctrine = blobRef
                });
            }
        }

        private BlobAssetReference<CulturalDoctrineBlob> BuildCulturalDoctrineBlob(CulturalDoctrineAsset asset)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var doctrine = ref builder.ConstructRoot<CulturalDoctrineBlob>();

            // Allocate and copy archetype name
            var archetypeNameBytes = System.Text.Encoding.UTF8.GetBytes(asset.archetypeName ?? "Default");
            var archetypeNameBlob = builder.Allocate(ref doctrine.ArchetypeName, archetypeNameBytes.Length);
            for (int i = 0; i < archetypeNameBytes.Length; i++)
            {
                archetypeNameBlob[i] = archetypeNameBytes[i];
            }

            doctrine.SoulHarvestBias = asset.soulHarvestBias;
            doctrine.HolyEntityMoraleBonus = asset.holyEntityMoraleBonus;
            doctrine.DeviationMultiplier = asset.deviationMultiplier;
            doctrine.IgnoreMoraleDecayOnGrudge = asset.ignoreMoraleDecayOnGrudge;
            doctrine.DeadEnemyAttackWeightBonus = asset.deadEnemyAttackWeightBonus;

            var blobRef = builder.CreateBlobAssetReference<CulturalDoctrineBlob>(Allocator.Persistent);
            builder.Dispose();
            return blobRef;
        }
    }
}

