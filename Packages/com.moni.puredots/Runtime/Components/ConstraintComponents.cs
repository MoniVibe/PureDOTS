using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Focus constraint for psychological state.
    /// Focus conservation: focus[i] -= cost; focus[i] += regen * dt; math.clamp(focus[i], 0, capacity)
    /// </summary>
    public struct FocusConstraint : IComponentData
    {
        public float CurrentFocus;          // Current focus level (0-capacity)
        public float Capacity;              // Maximum focus capacity
        public float RegenRate;              // Focus regeneration rate per second
        public float CostRate;               // Focus cost rate per second (when active)
        public bool IsActive;                // Whether constraint is active
        public uint LastUpdateTick;          // When constraint was last updated
    }

    /// <summary>
    /// Energy constraint for physical/psychological state.
    /// Similar to focus but for energy conservation.
    /// </summary>
    public struct EnergyConstraint : IComponentData
    {
        public float CurrentEnergy;          // Current energy level (0-capacity)
        public float Capacity;              // Maximum energy capacity
        public float RegenRate;              // Energy regeneration rate per second
        public float CostRate;               // Energy cost rate per second (when active)
        public bool IsActive;                // Whether constraint is active
        public uint LastUpdateTick;          // When constraint was last updated
    }

    /// <summary>
    /// Moral balance constraint for social/psychological state.
    /// Tracks moral alignment and balance constraints.
    /// </summary>
    public struct MoralConstraint : IComponentData
    {
        public float MoralAlignment;        // Moral alignment (-1 to 1, negative = evil, positive = good)
        public float BalanceTarget;          // Target balance (usually 0 for neutral)
        public float RestoreRate;            // Rate at which alignment restores toward target
        public float DeviationPenalty;       // Penalty for deviation from target
        public uint LastUpdateTick;          // When constraint was last updated
    }

    /// <summary>
    /// Constraint solver state tracking solver iterations.
    /// </summary>
    public struct ConstraintSolverState : IComponentData
    {
        public int IterationCount;           // Number of solver iterations
        public float ConvergenceThreshold;   // Convergence threshold
        public bool IsConverged;             // Whether solver has converged
        public uint LastSolveTick;           // When solver was last run
    }
}

