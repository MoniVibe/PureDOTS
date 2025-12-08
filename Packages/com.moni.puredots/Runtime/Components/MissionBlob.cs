using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// BlobAsset structure for mission templates.
    /// Defines reusable mission patterns.
    /// </summary>
    public struct MissionTemplateBlob
    {
        public BlobString TemplateId;       // Template identifier
        public BlobArray<MissionNodeBlob> Nodes; // Nodes in this template
        public float BaseValue;             // Base value of this mission type
        public float BaseDuration;          // Base duration estimate
    }

    /// <summary>
    /// Mission node data in blob format.
    /// </summary>
    public struct MissionNodeBlob
    {
        public int NodeId;
        public MissionNodeType Type;
        public float3 TargetPosition;
        public float EstimatedDuration;
        public float Value;
        public int NextNodeId;
    }

    /// <summary>
    /// Catalog of mission templates.
    /// </summary>
    public struct MissionTemplateCatalogBlob
    {
        public BlobArray<MissionTemplateBlob> Templates;
    }
}

