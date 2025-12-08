using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>Fallback economy group stub.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EconomySystemGroup : ComponentSystemGroup { }

    /// <summary>Fallback event group stub.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EventSystemGroup : ComponentSystemGroup { }

    /// <summary>Fallback physics group stub (distinct from Unity.Physics).</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PhysicsSystemGroup : ComponentSystemGroup { }

    /// <summary>Fallback presentation wrapper to avoid namespace ambiguity.</summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial class PureDotsPresentationSystemGroup : ComponentSystemGroup { }
}

namespace PureDOTS.Systems.Environment
{
    /// <summary>Stub to satisfy references when climate feedback is stripped.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ClimateFeedbackSystem : ISystem
    { }
}

namespace PureDOTS.Systems.Emotion
{
    /// <summary>Stub emotion system for compile-time references.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EmotionSystem : ISystem
    { }
}

namespace PureDOTS.Systems.Economy
{
    /// <summary>Stub investment decision system for compile-time references.</summary>
    [UpdateInGroup(typeof(EconomySystemGroup))]
    public partial struct InvestmentDecisionSystem : ISystem
    { }
}

namespace PureDOTS.Systems.Combat
{
    /// <summary>Stub fleet command system for compile-time references.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct FleetCommandSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
    }
}

namespace PureDOTS.Systems.Selection
{
    /// <summary>Stub selection system for compile-time references.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SelectionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
    }
}

namespace PureDOTS.Systems.Bridges
{
    /// <summary>Stub bridge for Mind-to-Body sync in the Systems namespace.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct MindToBodySyncSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
    }
}
