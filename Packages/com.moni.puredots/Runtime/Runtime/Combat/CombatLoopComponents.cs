using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    public enum CombatLoopPhase : byte
    {
        Idle = 0,
        Patrol = 1,
        Intercept = 2,
        Attack = 3,
        Retreat = 4
    }

    public struct CombatLoopConfig : IComponentData
    {
        public float PatrolRadius;
        public float EngagementRange;
        public float WeaponCooldownSeconds;
        public float RetreatThreshold;
    }

    public struct CombatLoopState : IComponentData
    {
        public CombatLoopPhase Phase;
        public float PhaseTimer;
        public float WeaponCooldown;
        public Entity Target;
        public float3 LastKnownTargetPosition;
    }
}
