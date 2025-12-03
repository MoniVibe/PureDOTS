using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    public struct DivineHandTag : IComponentData { }

    /// <summary>
    /// Tag component marking an entity as being held by a divine hand.
    /// </summary>
    public struct HandHeldTag : IComponentData
    {
        public Entity Holder;
    }

    public struct DivineHandState : IComponentData
    {
        public float3 CursorPosition;
        public float3 CursorNormal;
        public Entity HoveredEntity;
        
        // Reintroduced fields used by Godgame miracle systems
        public Entity HeldEntity;
        public ushort HeldResourceTypeIndex;
        public float HeldAmount;
    }
}
