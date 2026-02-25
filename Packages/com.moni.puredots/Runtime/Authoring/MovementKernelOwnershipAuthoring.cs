using PureDOTS.Runtime.Movement;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Authoring
{
    /// <summary>
    /// Opts an entity into MovementKernel ownership.
    /// Owned entities use kernel-captured pose as authoritative transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MovementKernelOwnershipAuthoring : MonoBehaviour
    {
    }

    public sealed class MovementKernelOwnershipBaker : Baker<MovementKernelOwnershipAuthoring>
    {
        public override void Bake(MovementKernelOwnershipAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<MovementKernelOwned>(entity);
            AddComponent(entity, new MovementKernelPose
            {
                Position = default,
                Rotation = Unity.Mathematics.quaternion.identity,
                Scale = 1f,
                CapturedTick = 0
            });
        }
    }
}
