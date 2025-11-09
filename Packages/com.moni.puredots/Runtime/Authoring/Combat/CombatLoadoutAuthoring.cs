#if UNITY_EDITOR
using PureDOTS.Runtime.Combat;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatLoadoutAuthoring : MonoBehaviour
    {
        public float patrolRadius = 25f;
        public float engagementRange = 5f;
        public float weaponCooldownSeconds = 3f;
        public float retreatThreshold = 0.2f;
    }

    public sealed class CombatLoadoutBaker : Baker<CombatLoadoutAuthoring>
    {
        public override void Bake(CombatLoadoutAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new CombatLoopConfig
            {
                PatrolRadius = authoring.patrolRadius,
                EngagementRange = authoring.engagementRange,
                WeaponCooldownSeconds = authoring.weaponCooldownSeconds,
                RetreatThreshold = authoring.retreatThreshold
            });

            AddComponent(entity, new CombatLoopState
            {
                Phase = CombatLoopPhase.Idle,
                PhaseTimer = 0f,
                WeaponCooldown = 0f,
                Target = Entity.Null,
                LastKnownTargetPosition = float3.zero
            });
        }
    }
}
#endif
