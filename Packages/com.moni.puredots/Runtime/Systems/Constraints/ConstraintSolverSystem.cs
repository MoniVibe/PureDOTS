using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Systems.Constraints
{
    /// <summary>
    /// Iterative constraint solver system.
    /// Solves constraints similar to physics constraints but on psychological/social state variables.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    public partial struct ConstraintSolverSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Solve focus constraints
            var focusQuery = state.GetEntityQuery(typeof(FocusConstraint));
            if (!focusQuery.IsEmpty)
            {
                var focusJob = new SolveFocusConstraintsJob
                {
                    DeltaTime = tickState.FixedDeltaTime,
                    CurrentTick = tickState.Tick
                };
                state.Dependency = focusJob.ScheduleParallel(focusQuery, state.Dependency);
            }

            // Solve energy constraints
            var energyQuery = state.GetEntityQuery(typeof(EnergyConstraint));
            if (!energyQuery.IsEmpty)
            {
                var energyJob = new SolveEnergyConstraintsJob
                {
                    DeltaTime = tickState.FixedDeltaTime,
                    CurrentTick = tickState.Tick
                };
                state.Dependency = energyJob.ScheduleParallel(energyQuery, state.Dependency);
            }

            // Solve moral constraints
            var moralQuery = state.GetEntityQuery(typeof(MoralConstraint));
            if (!moralQuery.IsEmpty)
            {
                var moralJob = new SolveMoralConstraintsJob
                {
                    DeltaTime = tickState.FixedDeltaTime,
                    CurrentTick = tickState.Tick
                };
                state.Dependency = moralJob.ScheduleParallel(moralQuery, state.Dependency);
            }
        }

        [BurstCompile]
        private partial struct SolveFocusConstraintsJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;

            public void Execute(ref FocusConstraint constraint)
            {
                // Apply cost if active
                if (constraint.IsActive)
                {
                    constraint.CurrentFocus -= constraint.CostRate * DeltaTime;
                }

                // Apply regeneration
                constraint.CurrentFocus += constraint.RegenRate * DeltaTime;

                // Clamp to valid range
                constraint.CurrentFocus = math.clamp(constraint.CurrentFocus, 0f, constraint.Capacity);

                constraint.LastUpdateTick = CurrentTick;
            }
        }

        [BurstCompile]
        private partial struct SolveEnergyConstraintsJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;

            public void Execute(ref EnergyConstraint constraint)
            {
                // Apply cost if active
                if (constraint.IsActive)
                {
                    constraint.CurrentEnergy -= constraint.CostRate * DeltaTime;
                }

                // Apply regeneration
                constraint.CurrentEnergy += constraint.RegenRate * DeltaTime;

                // Clamp to valid range
                constraint.CurrentEnergy = math.clamp(constraint.CurrentEnergy, 0f, constraint.Capacity);

                constraint.LastUpdateTick = CurrentTick;
            }
        }

        [BurstCompile]
        private partial struct SolveMoralConstraintsJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;

            public void Execute(ref MoralConstraint constraint)
            {
                // Restore toward balance target
                var deviation = constraint.MoralAlignment - constraint.BalanceTarget;
                var restoreAmount = math.sign(deviation) * constraint.RestoreRate * DeltaTime;
                constraint.MoralAlignment -= restoreAmount;

                // Apply deviation penalty
                var penalty = math.abs(deviation) * constraint.DeviationPenalty * DeltaTime;
                constraint.MoralAlignment = math.clamp(constraint.MoralAlignment, -1f, 1f);

                constraint.LastUpdateTick = CurrentTick;
            }
        }
    }
}

