using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Systems.Performance;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Performance
{
    /// <summary>
    /// Ensures TierProfileSettings exists and applies the active profile to the shared cadence + universal budgets.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct TierProfileBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Ensure singleton exists early so downstream systems can require it.
            if (!SystemAPI.HasSingleton<TierProfileSettings>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, TierProfileSettings.CreateDefaults(TierProfileId.Mid));
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // No-op; settings are updated by authoring or runtime config in the future.
        }
    }

    /// <summary>
    /// Applies TierProfileSettings into MindCadenceSettings + UniversalPerformanceBudget.
    /// This makes the policy knobs authoritative without duplicating systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(MindCadenceBootstrapSystem))]
    public partial struct TierProfileApplySystem : ISystem
    {
        private uint _lastAppliedVersion;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TierProfileSettings>();
            state.RequireForUpdate<MindCadenceSettings>();
            state.RequireForUpdate<UniversalPerformanceBudget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var profile = SystemAPI.GetSingleton<TierProfileSettings>();
            if (profile.Version == _lastAppliedVersion)
            {
                return;
            }

            // Drive cadence from Tier0 (full) defaults. Per-entity tier can further gate.
            var cadence = SystemAPI.GetSingletonRW<MindCadenceSettings>();
            cadence.ValueRW.SensorCadenceTicks = profile.Tier0SensorCadenceTicks;
            cadence.ValueRW.EvaluationCadenceTicks = profile.Tier0EvaluationCadenceTicks;
            cadence.ValueRW.ResolutionCadenceTicks = profile.Tier0ResolutionCadenceTicks;

            // Aggregate universal budgets: Tier0 + Tier1 are the main consumers, Tier2 is best-effort, Tier3 is 0.
            var budget = SystemAPI.GetSingletonRW<UniversalPerformanceBudget>();
            budget.ValueRW.MaxPerceptionChecksPerTick =
                profile.Tier0MaxPerceptionChecksPerTick + profile.Tier1MaxPerceptionChecksPerTick + profile.Tier2MaxPerceptionChecksPerTick;
            budget.ValueRW.MaxTacticalDecisionsPerTick =
                profile.Tier0MaxTacticalDecisionsPerTick + profile.Tier1MaxTacticalDecisionsPerTick + profile.Tier2MaxTacticalDecisionsPerTick;

            _lastAppliedVersion = profile.Version;
        }
    }

    /// <summary>
    /// Ensures AIFidelityTier exists for entities that participate in AI/perception pipelines.
    /// Structural change is restricted to initialization.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AIFidelityTierEnsureSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<RewindState>(out var rewind) && rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var tick = SystemAPI.GetSingleton<TimeState>().Tick;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<SenseCapability>>()
                         .WithNone<AIFidelityTier>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new AIFidelityTier
                {
                    Tier = AILODTier.Tier1_Reduced,
                    LastChangeTick = tick,
                    ReasonMask = 0
                });
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRO<AIBehaviourArchetype>>()
                         .WithNone<AIFidelityTier>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new AIFidelityTier
                {
                    Tier = AILODTier.Tier1_Reduced,
                    LastChangeTick = tick,
                    ReasonMask = 0
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// Assigns AI tiers using low-cost interest heuristics (no distance queries yet).
    /// Tier0 = has a presentation companion (usually near player); otherwise Tier1.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SpatialSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(PerceptionSystemGroup))]
    public partial struct AIInterestTierAssignmentSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TierProfileSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewind) || rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var time = SystemAPI.GetSingleton<TimeState>();
            var profile = SystemAPI.GetSingleton<TierProfileSettings>();
            var hysteresis = profile.TierHysteresisTicks == 0 ? 1u : profile.TierHysteresisTicks;

            foreach (var (tier, entity) in SystemAPI.Query<RefRW<AIFidelityTier>>().WithEntityAccess())
            {
                var current = tier.ValueRO;
                if (time.Tick - current.LastChangeTick < hysteresis)
                {
                    continue;
                }

                // Heuristic interest: visible companion implies high interest.
                var desired = SystemAPI.HasComponent<CompanionPresentation>(entity)
                    ? AILODTier.Tier0_Full
                    : AILODTier.Tier1_Reduced;

                if (desired == current.Tier)
                {
                    continue;
                }

                current.Tier = desired;
                current.LastChangeTick = time.Tick;
                current.ReasonMask = (byte)(desired == AILODTier.Tier0_Full ? 1 : 0);
                tier.ValueRW = current;
            }
        }
    }
}


