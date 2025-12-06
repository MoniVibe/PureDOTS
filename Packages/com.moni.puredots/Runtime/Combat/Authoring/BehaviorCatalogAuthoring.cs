using System;
using System.Collections.Generic;
using PureDOTS.Runtime.Combat;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring.Combat
{
    /// <summary>
    /// Authoring ScriptableObject for creating behavior catalogs as blob assets.
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviorCatalog", menuName = "PureDOTS/Combat/Behavior Catalog", order = 100)]
    public sealed class BehaviorCatalogAuthoring : ScriptableObject
    {
        [Serializable]
        public sealed class BehaviorNodeDefinition
        {
            [Header("Identity")]
            public ushort id;
            public float skillRequirement = 0.5f;
            public ImplantFlags implantTag = ImplantFlags.None;

            [Header("Costs")]
            public float focusCost = 10f;
            public float staminaCost = 5f;
            public float baseWeight = 1f;

            [Header("Actions")]
            public List<ActionId> actions = new List<ActionId>();
        }

        [Header("Behavior Nodes")]
        public List<BehaviorNodeDefinition> nodes = new List<BehaviorNodeDefinition>();

        public BlobAssetReference<BehaviorCatalogBlob> CreateBlobAsset()
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<BehaviorCatalogBlob>();
                var nodeArray = builder.Allocate(ref root.Nodes, nodes.Count);

                for (int i = 0; i < nodes.Count; i++)
                {
                    var def = nodes[i];
                    var node = new BehaviorNode
                    {
                        Id = def.id,
                        SkillReq = def.skillRequirement,
                        ImplantTag = (byte)def.implantTag,
                        FocusCost = def.focusCost,
                        StaminaCost = def.staminaCost,
                        BaseWeight = def.baseWeight,
                        Actions = default
                    };

                    var actionCount = math.min(def.actions.Count, 64);
                    for (int j = 0; j < actionCount; j++)
                    {
                        node.Actions.Add((ushort)def.actions[j]);
                    }

                    nodeArray[i] = node;
                }

                return builder.CreateBlobAssetReference<BehaviorCatalogBlob>(Allocator.Persistent);
            }
        }
    }
}

