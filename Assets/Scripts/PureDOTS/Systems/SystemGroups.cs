using Unity.Entities;
using Unity.Physics.Systems;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Custom system group for time management systems.
    /// Runs first in InitializationSystemGroup.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class TimeSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Environment simulation group; runs after physics and before spatial indexing.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuildPhysicsWorld))]
    public partial class EnvironmentSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Spatial systems run after environment state updates and before gameplay logic.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnvironmentSystemGroup))]
    [UpdateBefore(typeof(GameplaySystemGroup))]
    public partial class SpatialSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Shared AI systems that feed data into gameplay domains.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(SpatialSystemGroup))]
    [UpdateBefore(typeof(VillagerSystemGroup))]
    public partial class AISystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// High level gameplay simulation group containing domain-specific subgroups.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpatialSystemGroup))]
    public partial class GameplaySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for rewind/history recording.
    /// Runs after simulation to capture state.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class HistorySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Fixed-step job systems for villagers. Runs inside FixedStepSimulation before high-level AI.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class VillagerJobFixedStepGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for villager AI and behavior.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(AISystemGroup))]
    public partial class VillagerSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for resource management.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(VillagerSystemGroup))]
    public partial class ResourceSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for miracle effect processing.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial class MiracleEffectSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for combat systems.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuildPhysicsWorld))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial class CombatSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for divine hand interaction.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuildPhysicsWorld))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial class HandSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for vegetation systems.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial class VegetationSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for construction systems.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial class ConstructionSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Late simulation group for cleanup and state recording.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class LateSimulationSystemGroup : ComponentSystemGroup { }
}
