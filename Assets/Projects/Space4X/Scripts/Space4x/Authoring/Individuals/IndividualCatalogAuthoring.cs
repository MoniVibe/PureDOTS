using Space4X.Individuals;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

namespace Space4X.Authoring.Individuals
{
    /// <summary>
    /// Authoring ScriptableObject for individual catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "IndividualCatalog", menuName = "Space4X/Individuals/Individual Catalog")]
    public class IndividualCatalogAuthoring : ScriptableObject
    {
        public List<IndividualEntryAuthoring> Individuals = new List<IndividualEntryAuthoring>();
    }

    /// <summary>
    /// Individual entry authoring data.
    /// </summary>
    [System.Serializable]
    public struct IndividualEntryAuthoring
    {
        public string Id;
        public int Command;
        public int Tactics;
        public int Logistics;
        public int Diplomacy;
        public int Engineering;
        public int Resolve;
        public int Physique;
        public int Finesse;
        public int Will;
        public byte PhysiqueInclination;
        public byte FinesseInclination;
        public byte WillInclination;
        public PreordainTrack PreordainTrack;
    }

    /// <summary>
    /// Baker for IndividualCatalogAuthoring.
    /// </summary>
    public sealed class IndividualCatalogBaker : Baker<IndividualCatalogAuthoring>
    {
        public override void Bake(IndividualCatalogAuthoring authoring)
        {
            using var bb = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var root = ref bb.ConstructRoot<IndividualSpecBlob>();

            var individuals = bb.Allocate(ref root.Entries, authoring.Individuals.Count);
            for (int i = 0; i < authoring.Individuals.Count; i++)
            {
                var entry = authoring.Individuals[i];
                individuals[i] = new IndividualSpecEntry
                {
                    Id = entry.Id,
                    Stats = new IndividualStats
                    {
                        Command = entry.Command,
                        Tactics = entry.Tactics,
                        Logistics = entry.Logistics,
                        Diplomacy = entry.Diplomacy,
                        Engineering = entry.Engineering,
                        Resolve = entry.Resolve
                    },
                    Attributes = new PhysiqueFinesseWill
                    {
                        Physique = entry.Physique,
                        Finesse = entry.Finesse,
                        Will = entry.Will,
                        PhysiqueInclination = entry.PhysiqueInclination,
                        FinesseInclination = entry.FinesseInclination,
                        WillInclination = entry.WillInclination
                    },
                    PreordainTrack = entry.PreordainTrack
                };
            }

            var blob = bb.CreateBlobAssetReference<IndividualSpecBlob>(Unity.Collections.Allocator.Persistent);
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new IndividualSpecRef { Blob = blob });
        }
    }

    /// <summary>
    /// Individual specification reference component.
    /// </summary>
    public struct IndividualSpecRef : IComponentData
    {
        public BlobAssetReference<IndividualSpecBlob> Blob;
    }

    /// <summary>
    /// Individual spec entry in blob.
    /// </summary>
    public struct IndividualSpecEntry
    {
        public FixedString64Bytes Id;
        public IndividualStats Stats;
        public PhysiqueFinesseWill Attributes;
        public PreordainTrack PreordainTrack;
    }

    /// <summary>
    /// Individual spec blob array wrapper.
    /// </summary>
    public struct IndividualSpecBlob
    {
        public BlobArray<IndividualSpecEntry> Entries;
    }
}

