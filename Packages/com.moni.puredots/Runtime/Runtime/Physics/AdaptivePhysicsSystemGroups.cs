using Unity.Entities;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Light mass physics system group (60 Hz).
    /// Full rigid-body simulation for entities < 10⁴ kg.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class LightMassPhysicsGroup : ComponentSystemGroup { }

    /// <summary>
    /// Medium mass physics system group (6 Hz).
    /// Simplified inertia update for entities 10⁴–10⁸ kg.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LightMassPhysicsGroup))]
    public partial class MediumMassPhysicsGroup : ComponentSystemGroup { }

    /// <summary>
    /// Heavy mass physics system group (0.6 Hz).
    /// Analytic orbit/drag integration for entities > 10⁸ kg.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MediumMassPhysicsGroup))]
    public partial class HeavyMassPhysicsGroup : ComponentSystemGroup { }
}

