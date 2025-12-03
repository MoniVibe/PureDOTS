using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    public struct DivineHandCommand : IComponentData
    {
        public DivineHandCommandType Type;
        public Entity TargetEntity;
        public float3 TargetPosition;
        public float3 TargetNormal;
        public float TimeSinceIssued;
    }
}
