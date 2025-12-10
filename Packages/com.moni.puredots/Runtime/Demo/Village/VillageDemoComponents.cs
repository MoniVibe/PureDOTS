#if PUREDOTS_DEMO
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Demo.Village
{
    /// <summary>
    /// Demo-only components for the PureDOTS village demonstration.
    /// These components are used by demo systems to create and manage simple village entities.
    /// </summary>

    /// <summary>
    /// World-level tag to enable village demo presentation systems in host game worlds.
    /// Add this component to a world entity to enable VillageVisualSetupSystem.
    /// </summary>
    public struct VillageWorldTag : IComponentData { }

    /// <summary>
    /// Tag component identifying a village entity in the demo.
    /// </summary>
    public struct VillageTag : IComponentData { }

    /// <summary>
    /// Tag component identifying a villager entity in the demo.
    /// </summary>
    public struct VillagerTag : IComponentData { }

    /// <summary>
    /// Component defining a home lot with its position.
    /// </summary>
    public struct HomeLot : IComponentData
    {
        public float3 Position;
    }

    /// <summary>
    /// Component defining a work lot with its position.
    /// </summary>
    public struct WorkLot : IComponentData
    {
        public float3 Position;
    }

    /// <summary>
    /// Component storing a villager's home position.
    /// </summary>
    public struct VillagerHome : IComponentData
    {
        public float3 Position;
    }

    /// <summary>
    /// Component storing a villager's work position.
    /// </summary>
    public struct VillagerWork : IComponentData
    {
        public float3 Position;
    }

    /// <summary>
    /// Component tracking a villager's current state in their work/home cycle.
    /// Phase 0 = going to work, Phase 1 = going home.
    /// </summary>
    public struct VillagerState : IComponentData
    {
        public byte Phase;
    }
}
#endif

