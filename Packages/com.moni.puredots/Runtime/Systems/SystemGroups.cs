using Unity.Entities;
using Unity.Physics.Systems;
using PureDOTS.Runtime.Threading;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Custom system group for time management systems.
    /// Runs first in InitializationSystemGroup.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [ThreadRole(ThreadRoleType.MainOrchestrator)]
    public partial class TimeSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Environment simulation group; runs after physics and before spatial indexing.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [ThreadRole(ThreadRoleType.Logic)]
    public partial class EnvironmentSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Spatial systems run after environment state updates and before gameplay logic.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnvironmentSystemGroup))]
    [UpdateBefore(typeof(GameplaySystemGroup))]
    [ThreadRole(ThreadRoleType.Logic)]
    public partial class SpatialSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Hot path system group - runs every tick, no throttling.
    /// Contains systems that process many entities with simple math (movement, steering).
    /// Must be tiny, branch-light, data tight. No allocations, no pathfinding calls.
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup), OrderFirst = true)]
    public partial class HotPathSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Warm path system group - throttled, staggered updates.
    /// Contains systems that do local pathfinding, group decisions, replanning.
    /// Throttled (K queries/tick), staggered updates, local A* only.
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(HotPathSystemGroup))]
    public partial class WarmPathSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Cold path system group - long intervals, event-driven.
    /// Contains systems that do strategic planning, graph building, multi-modal routing.
    /// Event-driven or long intervals (50-200 ticks), strategic planning.
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(WarmPathSystemGroup))]
    public partial class ColdPathSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Event-driven system group that processes events first.
    /// Runs before other simulation groups to handle event triggers.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class EventSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Modifier system group for processing modifier events and applying modifiers.
    /// Runs after EventSystemGroup and before GameplaySystemGroup.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EventSystemGroup))]
    [UpdateBefore(typeof(GameplaySystemGroup))]
    public partial class ModifierSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Hot path modifier group - runs every tick (60Hz) for active modifier math.
    /// </summary>
    [UpdateInGroup(typeof(ModifierSystemGroup), OrderFirst = true)]
    public partial class ModifierHotPathGroup : ComponentSystemGroup { }

    /// <summary>
    /// Cold path modifier group - throttled updates (0.2-1Hz) for expiry/cleanup.
    /// </summary>
    [UpdateInGroup(typeof(ModifierSystemGroup))]
    [UpdateAfter(typeof(ModifierHotPathGroup))]
    public partial class ModifierColdPathGroup : ComponentSystemGroup { }

    /// <summary>
    /// Cognitive system group with adaptive tick rate (0.5-5Hz).
    /// Runs at lower frequency than physics for cognitive AI processing.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class CognitiveSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Reflex system group - runs at 60Hz for instant sensor→action reactive mapping.
    /// Pure reactive layer with no learning, lowest latency.
    /// </summary>
    [UpdateInGroup(typeof(HotPathSystemGroup))]
    public partial class ReflexSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Learning system group - runs at 1Hz for procedural learning and pattern extraction.
    /// Handles procedural memory, affordance detection, causal chains, context hashing.
    /// </summary>
    [UpdateInGroup(typeof(CognitiveSystemGroup))]
    public partial class LearningSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Motivation system group - runs at 0.2Hz for emotion and motivation updates.
    /// Handles limbic modulation and emotion-driven learning.
    /// </summary>
    [UpdateInGroup(typeof(CognitiveSystemGroup))]
    [UpdateAfter(typeof(LearningSystemGroup))]
    public partial class MotivationSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Economy system group - parent group for all economy systems.
    /// Contains BodyEconomySystemGroup (60Hz), MindEconomySystemGroup (1Hz), AggregateEconomySystemGroup (0.2Hz).
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial class EconomySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Body economy system group - runs at 60Hz for physical resource extraction and logistics.
    /// Handles production, mining, hauling at fixed timestep.
    /// </summary>
    [UpdateInGroup(typeof(EconomySystemGroup), OrderFirst = true)]
    [ThreadRole(ThreadRoleType.Logic)]
    public partial class BodyEconomySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Mind economy system group - runs at 1Hz for investment and strategic decisions.
    /// Handles investment evaluation, risk assessment, trade negotiations.
    /// </summary>
    [UpdateInGroup(typeof(EconomySystemGroup))]
    [UpdateAfter(typeof(BodyEconomySystemGroup))]
    [ThreadRole(ThreadRoleType.Logic)]
    public partial class MindEconomySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Aggregate economy system group - runs at 0.2Hz for market simulation and macro-economy.
    /// Handles market equilibrium, empire wealth aggregation, tax collection.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(HistorySystemGroup))]
    [ThreadRole(ThreadRoleType.Logic)]
    public partial class AggregateEconomySystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Shared AI systems that feed data into gameplay domains.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(VillagerSystemGroup))]
    public partial class AISystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Tactical system group for formation commands, group morale, and tactical AI.
    /// Runs at 1-5 Hz (throttled) after spatial systems and before individual villager systems.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(SpatialSystemGroup))]
    [UpdateBefore(typeof(VillagerSystemGroup))]
    [ThreadRole(ThreadRoleType.Logic)]
    public partial class TacticalSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// High level gameplay simulation group containing domain-specific subgroups.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(TransportPhaseGroup))]
    [ThreadRole(ThreadRoleType.Logic)]
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
    /// System group for power network management.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial class PowerSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for miracle effect processing.
    /// </summary>
    /// <remarks>See Docs/TruthSources/RuntimeLifecycle_TruthSource.md for canonical ordering expectations.</remarks>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ResourceSystemGroup))]
    public partial class MiracleEffectSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for perception systems.
    /// Runs after spatial grid, before AI systems.
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    public partial class PerceptionSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for interrupt handling.
    /// Runs after perception/combat/group logic, before AI/GOAP systems.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(AISystemGroup))]
    public partial class InterruptSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for group decision systems.
    /// Runs after group membership, before interrupt handling.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(InterruptSystemGroup))]
    public partial class GroupDecisionSystemGroup : ComponentSystemGroup { }

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
    /// System group for micro collision regime (< 100m objects).
    /// Uses Newtonian rigid-body impact physics.
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial class MicroCollisionSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for meso collision regime (100m - 10km objects).
    /// Uses cratering / momentum transfer physics.
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial class MesoCollisionSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for macro collision regime (> 10km objects, moons, planets).
    /// Uses hydrodynamic approximation (SPH or energy map).
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial class MacroCollisionSystemGroup : ComponentSystemGroup { }

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
    /// Predictive simulation system group for multiplayer rollback networking.
    /// Currently empty - reserves system group slots for future rollback networking.
    /// Systems marked with [UpdateInGroup(typeof(PredictedSimulationSystemGroup))] will replay
    /// buffered inputs ahead of the authoritative tick for client-side prediction.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LateSimulationSystemGroup))]
    public partial class PredictedSimulationSystemGroup : ComponentSystemGroup { }

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
    [ThreadRole(ThreadRoleType.RenderingIO)]
    public partial class PureDotsPresentationSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// [DEPRECATED] Old PresentationSystemGroup - use Unity.Entities.PresentationSystemGroup or PureDotsPresentationSystemGroup instead.
    /// This type is kept for compatibility but should not be used in new code.
    /// </summary>
    [System.Obsolete("Use Unity.Entities.PresentationSystemGroup or PureDOTS.Systems.PureDotsPresentationSystemGroup instead. See Docs/FoundationGuidelines.md for policy.")]
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial class PresentationSystemGroup : ComponentSystemGroup { }
}
