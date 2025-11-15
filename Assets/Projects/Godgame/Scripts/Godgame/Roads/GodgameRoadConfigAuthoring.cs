using Godgame.Roads;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;

namespace Godgame.Authoring
{
    [DisallowMultipleComponent]
    public sealed class GodgameRoadConfigAuthoring : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private string roadDescriptorKey = "godgame.road.segment";
        [SerializeField] private string handleDescriptorKey = "godgame.road.handle";
        [SerializeField] private float defaultRoadWidth = 2.5f;
        [SerializeField] private float initialStretchLength = 6f;
        [SerializeField] private float roadMeshBaseLength = 4f;

        [Header("Handles")]
        [SerializeField] private float handleMass = 0.1f;
        [SerializeField] private float handleFollowLerp = 0.25f;

        [Header("Auto Build / Heat")]
        [SerializeField] private float heatCellSize = 4f;
        [SerializeField] private float heatDecayPerSecond = 0.25f;
        [SerializeField] private float heatBuildThreshold = 6f;
        [SerializeField] private float autoBuildLength = 8f;

        private sealed class Baker : Unity.Entities.Baker<GodgameRoadConfigAuthoring>
        {
            public override void Bake(GodgameRoadConfigAuthoring authoring)
            {
                if (!PresentationKeyUtility.TryParseKey(authoring.roadDescriptorKey, out var roadHash, out _))
                {
                    Debug.LogWarning($"GodgameRoadConfigAuthoring: invalid road descriptor key '{authoring.roadDescriptorKey}'.");
                    return;
                }

                Hash128 handleHash = default;
                if (!string.IsNullOrWhiteSpace(authoring.handleDescriptorKey))
                {
                    PresentationKeyUtility.TryParseKey(authoring.handleDescriptorKey, out handleHash, out _);
                }

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GodgameRoadConfig
                {
                    DefaultRoadWidth = Mathf.Max(0.5f, authoring.defaultRoadWidth),
                    InitialStretchLength = Mathf.Max(1f, authoring.initialStretchLength),
                    RoadMeshBaseLength = Mathf.Max(0.1f, authoring.roadMeshBaseLength),
                    HandleMass = Mathf.Max(0.01f, authoring.handleMass),
                    HandleFollowLerp = Mathf.Clamp01(authoring.handleFollowLerp),
                    HeatCellSize = Mathf.Max(0.5f, authoring.heatCellSize),
                    HeatDecayPerSecond = Mathf.Max(0.01f, authoring.heatDecayPerSecond),
                    HeatBuildThreshold = Mathf.Max(0.1f, authoring.heatBuildThreshold),
                    AutoBuildLength = Mathf.Max(1f, authoring.autoBuildLength),
                    RoadDescriptor = roadHash,
                    HandleDescriptor = handleHash
                });
            }
        }
    }
}
