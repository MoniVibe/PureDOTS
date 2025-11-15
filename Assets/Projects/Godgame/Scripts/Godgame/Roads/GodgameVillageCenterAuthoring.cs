using Godgame.Roads;
using Unity.Entities;
using UnityEngine;

namespace Godgame.Authoring
{
    [DisallowMultipleComponent]
    public sealed class GodgameVillageCenterAuthoring : MonoBehaviour
    {
        [SerializeField] private float roadRingRadius = 5f;

        private sealed class Baker : Unity.Entities.Baker<GodgameVillageCenterAuthoring>
        {
            public override void Bake(GodgameVillageCenterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var transform = authoring.transform;

                AddComponent(entity, new GodgameVillageCenter
                {
                    RoadRingRadius = Mathf.Max(1f, authoring.roadRingRadius),
                    BaseHeight = transform.position.y
                });
            }
        }
    }
}
