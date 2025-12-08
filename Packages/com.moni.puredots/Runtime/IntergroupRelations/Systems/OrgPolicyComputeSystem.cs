using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using Unity.Collections;

namespace PureDOTS.Runtime.IntergroupRelations
{
    /// <summary>
    /// Derives OrgPolicyState interaction masks from OrgRelationKind and OrgTreatyFlags.
    /// Trade/logistics systems read AllowTrade, migration reads AllowMigration, etc.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OrgRelationInitSystem))]
    public partial struct OrgPolicyComputeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;
            
            var currentTick = timeState.Tick;

            var query = SystemAPI.QueryBuilder()
                .WithAll<OrgRelation, OrgRelationTag>()
                .Build();

            using var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var relation = SystemAPI.GetComponent<OrgRelation>(entity);

                // Check if policy state exists, create if not
                if (!SystemAPI.HasComponent<OrgPolicyState>(entity))
                {
                    state.EntityManager.AddComponent<OrgPolicyState>(entity);
                }

                var policy = SystemAPI.GetComponentRW<OrgPolicyState>(entity);

                // Compute interaction masks from relation kind and treaties
                var maskAtoB = ComputeInteractionMask(relation.Kind, relation.Treaties, true);
                var maskBtoA = ComputeInteractionMask(relation.Kind, relation.Treaties, false);

                policy.ValueRW.AToBMask = maskAtoB;
                policy.ValueRW.BToAMask = maskBtoA;
                policy.ValueRW.LastUpdateTick = currentTick;
            }
        }

        private static OrgInteractionMask ComputeInteractionMask(OrgRelationKind kind, OrgTreatyFlags treaties, bool isAtoB)
        {
            OrgInteractionMask mask = 0;

            // Base permissions from relation kind
            switch (kind)
            {
                case OrgRelationKind.Allied:
                case OrgRelationKind.Friendly:
                    mask |= OrgInteractionMask.AllowTrade | OrgInteractionMask.AllowMigration | 
                            OrgInteractionMask.AllowMarriage | OrgInteractionMask.AllowEmbassy | 
                            OrgInteractionMask.AllowAid;
                    break;

                case OrgRelationKind.Neutral:
                    mask |= OrgInteractionMask.AllowTrade | OrgInteractionMask.AllowEmbassy;
                    break;

                case OrgRelationKind.Rival:
                    mask |= OrgInteractionMask.AllowTrade; // Limited trade still possible
                    break;

                case OrgRelationKind.Hostile:
                    mask |= OrgInteractionMask.AllowEspionage; // Only espionage allowed
                    break;

                case OrgRelationKind.Shunned:
                    // No interactions allowed
                    break;

                case OrgRelationKind.Sanctioned:
                    // Trade blocked
                    break;

                case OrgRelationKind.Vassal:
                case OrgRelationKind.Overlord:
                    // Asymmetric permissions
                    if (kind == OrgRelationKind.Overlord && isAtoB)
                    {
                        mask |= OrgInteractionMask.AllowTrade | OrgInteractionMask.AllowMilitary | 
                                OrgInteractionMask.AllowEmbassy;
                    }
                    else if (kind == OrgRelationKind.Vassal && !isAtoB)
                    {
                        mask |= OrgInteractionMask.AllowTrade | OrgInteractionMask.AllowAid;
                    }
                    break;

                case OrgRelationKind.Integrated:
                    // Full permissions
                    mask |= OrgInteractionMask.AllowTrade | OrgInteractionMask.AllowMigration | 
                            OrgInteractionMask.AllowMarriage | OrgInteractionMask.AllowEmbassy | 
                            OrgInteractionMask.AllowMilitary | OrgInteractionMask.AllowAid;
                    break;
            }

            // Apply treaty flags
            if ((treaties & OrgTreatyFlags.TradeAgreement) != 0)
                mask |= OrgInteractionMask.AllowTrade;

            if ((treaties & OrgTreatyFlags.OpenBorders) != 0)
                mask |= OrgInteractionMask.AllowMigration | OrgInteractionMask.AllowMilitary;

            if ((treaties & OrgTreatyFlags.Sanctions) != 0)
                mask &= ~OrgInteractionMask.AllowTrade;

            if ((treaties & OrgTreatyFlags.Embargo) != 0)
                mask &= ~OrgInteractionMask.AllowTrade;

            if ((treaties & OrgTreatyFlags.ShunSocial) != 0)
                mask &= ~(OrgInteractionMask.AllowMigration | OrgInteractionMask.AllowMarriage);

            if ((treaties & OrgTreatyFlags.ShunReligious) != 0)
                mask &= ~OrgInteractionMask.AllowEmbassy;

            if ((treaties & OrgTreatyFlags.DefensivePact) != 0 || (treaties & OrgTreatyFlags.FullAlliance) != 0)
                mask |= OrgInteractionMask.AllowMilitary | OrgInteractionMask.AllowAid;

            return mask;
        }
    }
}

