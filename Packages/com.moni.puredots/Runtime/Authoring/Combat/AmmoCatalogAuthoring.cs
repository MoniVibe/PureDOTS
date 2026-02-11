using System;
using System.Collections.Generic;
using PureDOTS.Runtime.Combat;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Entities.Hybrid;
#endif

namespace PureDOTS.Authoring.Combat
{
    [CreateAssetMenu(fileName = "AmmoCatalog", menuName = "PureDOTS/Space4X/Ammo Catalog")]
    public sealed class AmmoCatalogAsset : ScriptableObject
    {
        public List<AmmoCatalogEntry> Entries = new();
    }

    [Serializable]
    public struct AmmoCatalogEntry
    {
        public string AmmoId;
        public float DamageMultiplier;
        public float SpeedMultiplier;
        public float LifetimeMultiplier;
        public float TurnRateMultiplier;
        public float SeekRadiusMultiplier;
        public float AoERadiusMultiplier;
        public float ChainRangeMultiplier;
        public float PierceBonus;
        public float KnockbackMultiplier;
        public bool OverrideDamageType;
        public DamageType DamageTypeOverride;
        public DamageFlags DamageFlags;
        public List<AmmoEffectOpEntry> OnHitAdd;
    }

    [Serializable]
    public struct AmmoEffectOpEntry
    {
        public EffectOpKind Kind;
        public float Magnitude;
        public float Duration;
        public float Aux;
        public uint StatusId;
    }

    /// <summary>
    /// Authoring component for ammo catalog.
    /// </summary>
    public sealed class AmmoCatalogAuthoring : MonoBehaviour
    {
        public AmmoCatalogAsset Catalog;
    }

#if UNITY_EDITOR
    public sealed class AmmoCatalogBaker : Baker<AmmoCatalogAuthoring>
    {
        public override void Bake(AmmoCatalogAuthoring authoring)
        {
            if (authoring.Catalog == null || authoring.Catalog.Entries == null || authoring.Catalog.Entries.Count == 0)
            {
                Debug.LogWarning("[AmmoCatalogBaker] No catalog assigned or empty entries; skipping blob creation.");
                return;
            }

            var entity = GetEntity(TransformUsageFlags.None);

            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AmmoCatalogBlob>();
            var array = builder.Allocate(ref root.Ammunition, authoring.Catalog.Entries.Count);

            for (int i = 0; i < authoring.Catalog.Entries.Count; i++)
            {
                var entry = authoring.Catalog.Entries[i];
                var id = string.IsNullOrWhiteSpace(entry.AmmoId) ? $"ammo.{i}" : entry.AmmoId.Trim().ToLowerInvariant();

                ref var spec = ref array[i];
                spec = new AmmoSpec
                {
                    Id = new FixedString32Bytes(id),
                    DamageMultiplier = entry.DamageMultiplier,
                    SpeedMultiplier = entry.SpeedMultiplier,
                    LifetimeMultiplier = entry.LifetimeMultiplier,
                    TurnRateMultiplier = entry.TurnRateMultiplier,
                    SeekRadiusMultiplier = entry.SeekRadiusMultiplier,
                    AoERadiusMultiplier = entry.AoERadiusMultiplier,
                    ChainRangeMultiplier = entry.ChainRangeMultiplier,
                    PierceBonus = entry.PierceBonus,
                    KnockbackMultiplier = entry.KnockbackMultiplier,
                    DamageTypeOverride = entry.OverrideDamageType ? (byte)entry.DamageTypeOverride : (byte)255,
                    DamageFlags = entry.DamageFlags
                };

                var effectCount = entry.OnHitAdd != null ? entry.OnHitAdd.Count : 0;
                var effects = builder.Allocate(ref spec.OnHitAdd, effectCount);
                for (int e = 0; e < effectCount; e++)
                {
                    var op = entry.OnHitAdd[e];
                    effects[e] = new EffectOp
                    {
                        Kind = op.Kind,
                        Magnitude = op.Magnitude,
                        Duration = op.Duration,
                        Aux = op.Aux,
                        StatusId = op.StatusId
                    };
                }
                AmmoSpecSanitizer.Sanitize(ref spec);
            }

            var blob = builder.CreateBlobAssetReference<AmmoCatalogBlob>(Allocator.Persistent);
            AddBlobAsset(ref blob, out _);
            AddComponent(entity, new AmmoCatalog { Catalog = blob });
        }
    }
#endif
}
