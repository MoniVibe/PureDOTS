#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PureDOTS.Runtime.Miracles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring.Miracles
{
    /// <summary>
    /// ScriptableObject asset for miracle catalog data.
    /// </summary>
    [CreateAssetMenu(fileName = "MiracleCatalog", menuName = "PureDOTS/Miracles/Miracle Catalog", order = 100)]
    public sealed class MiracleCatalogAsset : ScriptableObject
    {
        [Serializable]
        public class MiracleSpecData
        {
            [Header("Identity")]
            public MiracleId id = MiracleId.None;
            
            [Header("Timing")]
            [Min(0f)]
            public float baseCooldownSeconds = 10f;
            [Min(0f)]
            public float basePrayerCost = 0f; // Not enforced in MVP
            
            [Header("Area")]
            [Min(0f)]
            public float baseRadius = 10f;
            [Min(0f)]
            public float maxRadius = 50f;
            
            [Header("Charges")]
            [Range(1, 10)]
            public byte maxCharges = 1;
            
            [Header("Properties")]
            [Range(1, 3)]
            public byte tier = 1; // 1=small, 2=medium, 3=epic
            
            [Header("Dispensation")]
            [Tooltip("Bitmask: 1=Sustained, 2=Throw")]
            public byte allowedDispenseModes = 3; // Both by default
            
            [Header("Targeting")]
            public TargetingMode targetingMode = TargetingMode.Point;
            
            [Header("Category")]
            public MiracleCategory category = MiracleCategory.Weather;
        }
        
        public List<MiracleSpecData> specs = new();
    }
    
    /// <summary>
    /// MonoBehaviour authoring component that references a miracle catalog asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiracleCatalogAuthoring : MonoBehaviour
    {
        [Tooltip("Reference to the ScriptableObject catalog that defines miracle data.")]
        public MiracleCatalogAsset catalog;
        
        [Tooltip("Global cooldown scale multiplier (for difficulty tuning).")]
        [Min(0.1f)]
        public float globalCooldownScale = 1f;
        
        class Baker : Unity.Entities.Baker<MiracleCatalogAuthoring>
        {
            public override void Bake(MiracleCatalogAuthoring authoring)
            {
                if (authoring.catalog == null)
                {
                    Debug.LogWarning("[MiracleCatalogBaker] Missing catalog reference.");
                    return;
                }
                
                var catalog = authoring.catalog;
                if (catalog.specs == null || catalog.specs.Count == 0)
                {
                    Debug.LogWarning($"[MiracleCatalogBaker] No specs defined in {catalog.name}.");
                    return;
                }
                
                // Build blob data
                using var builder = new BlobBuilder(Allocator.Temp);
                ref var catalogBlob = ref builder.ConstructRoot<MiracleCatalogBlob>();
                var specsArray = builder.Allocate(ref catalogBlob.Specs, catalog.specs.Count);
                
                for (int i = 0; i < catalog.specs.Count; i++)
                {
                    var src = catalog.specs[i];
                    specsArray[i] = new MiracleSpec
                    {
                        Id = src.id,
                        BaseCooldownSeconds = math.max(0f, src.baseCooldownSeconds),
                        BasePrayerCost = math.max(0f, src.basePrayerCost),
                        BaseRadius = math.max(0f, src.baseRadius),
                        MaxRadius = math.max(src.baseRadius, src.maxRadius),
                        MaxCharges = (byte)math.clamp(src.maxCharges, 1, 10),
                        Tier = (byte)math.clamp(src.tier, 1, 3),
                        AllowedDispenseModes = src.allowedDispenseModes,
                        TargetingMode = src.targetingMode,
                        Category = src.category
                    };
                }
                
                var blobAsset = builder.CreateBlobAssetReference<MiracleCatalogBlob>(Allocator.Persistent);
                var entity = GetEntity(TransformUsageFlags.None);
                AddBlobAsset(ref blobAsset, out _);
                AddComponent(entity, new MiracleConfigState
                {
                    Catalog = blobAsset,
                    GlobalCooldownScale = math.max(0.1f, authoring.globalCooldownScale)
                });
            }
        }
    }
}
#endif












