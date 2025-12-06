using UnityEngine;
using Unity.Entities;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Authoring.Economy.Ownership
{
    /// <summary>
    /// MonoBehaviour for authoring initial ownership.
    /// Fields: OwnerEntity (reference), Share, Rights (flags).
    /// Baker adds Ownership component.
    /// </summary>
    public class OwnershipAuthoring : MonoBehaviour
    {
        [Header("Ownership")]
        [SerializeField] private Entity _ownerEntity;
        [SerializeField, Range(0f, 1f)] private float _share = 1f;
        [SerializeField] private OwnershipRights _rights = OwnershipRights.Manage | OwnershipRights.Trade | OwnershipRights.Use;

        public Entity OwnerEntity => _ownerEntity;
        public float Share => _share;
        public OwnershipRights Rights => _rights;
    }

    /// <summary>
    /// Baker for OwnershipAuthoring.
    /// Adds Ownership component from authoring data.
    /// </summary>
    public class OwnershipBaker : Baker<OwnershipAuthoring>
    {
        public override void Bake(OwnershipAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Ownership
            {
                Owner = authoring.OwnerEntity,
                Share = authoring.Share,
                Rights = authoring.Rights
            });
        }
    }
}

