using Unity.Entities;
using Unity.Physics.Systems;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Custom system group for time management systems.
    /// Runs first in InitializationSystemGroup.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class TimeSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Environment simulation group; runs after physics and before spatial indexing.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EnvironmentSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Spatial systems run after environment state updates and before gameplay logic.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnvironmentSystemGroup))]
    [UpdateBefore(typeof(GameplaySystemGroup))]
    public partial class SpatialSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Shared AI systems that feed data into gameplay domains.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(VillagerSystemGroup))]
    public partial class AISystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// High level gameplay simulation group containing domain-specific subgroups.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(TransportPhaseGroup))]
    public partial class GameplaySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for rewind/history recording.
    /// Runs after simulation to capture state.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(HistoryPhaseGroup))]
    public partial class HistorySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Fixed-step job systems for villagers. Runs inside FixedStepSimulation before high-level AI.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class VillagerJobFixedStepGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for villager AI and behavior.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(AISystemGroup))]
    public partial class VillagerSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for resource management.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(VillagerSystemGroup))]
    public partial class ResourceSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for miracle effect processing.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial class MiracleEffectSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for combat systems.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial class CombatSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for divine hand interaction.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial class HandSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for vegetation systems.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class VegetationSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for construction systems.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial class ConstructionSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// High-priority system group for camera/input handling. Executes at the end of InitializationSystemGroup
    /// so input is processed before the SimulationSystemGroup begins.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class CameraInputSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Late simulation group for cleanup and state recording.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class LateSimulationSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// PureDOTS presentation system group for rendering/UI bridge systems.
    /// Runs under Unity's PresentationSystemGroup for proper frame-time execution.
    /// Consumes simulation data for visualization. Guarded by PresentationRewindGuardSystem.
    /// </summary>
    /// <remarks>
    /// This group provides logical organization for PureDOTS presentation systems.
    /// All systems in this group ultimately run in Unity's PresentationSystemGroup.
    /// See Docs/FoundationGuidelines.md for presentation system group policy.
    /// </remarks>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial class PureDotsPresentationSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// [DEPRECATED] Old PresentationSystemGroup - use Unity.Entities.PresentationSystemGroup or PureDotsPresentationSystemGroup instead.
    /// This type is kept for compatibility but should not be used in new code.
    /// </summary>
    [System.Obsolete("Use Unity.Entities.PresentationSystemGroup or PureDOTS.Systems.PureDotsPresentationSystemGroup instead. See Docs/FoundationGuidelines.md for policy.")]
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial class PresentationSystemGroup : ComponentSystemGroup { }
}
