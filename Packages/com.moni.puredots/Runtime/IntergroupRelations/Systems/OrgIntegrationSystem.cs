using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.IntergroupRelations
{
    /// <summary>
    /// Handles organization integration/mergers.
    /// When IntegrationProcess completes (ownership reaches 1.0), merges orgs and consolidates ownership.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OrgOwnershipSystem))]
    public partial struct OrgIntegrationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var currentTick = SystemAPI.GetSingleton<TimeState>().Tick;

            // Find orgs with complete ownership (share >= 1.0)
            var orgsToProcess = new NativeList<Entity>(Allocator.Temp);

            foreach (var (ownershipBuffer, orgEntity) in SystemAPI.Query<DynamicBuffer<OrgOwnership>>()
                .WithEntityAccess())
            {
                for (int i = 0; i < ownershipBuffer.Length; i++)
                {
                    var ownership = ownershipBuffer[i];
                    
                    if (ownership.Share >= 1f && SystemAPI.Exists(ownership.OwnerOrg))
                    {
                        // Check if integration process is active
                        var relation = GetRelation(state, ownership.OwnerOrg, orgEntity);
                        if (relation.HasValue && 
                            (relation.Value.Treaties & OrgTreatyFlags.IntegrationProcess) != 0)
                        {
                            orgsToProcess.Add(orgEntity);
                            break;
                        }
                    }
                }
            }

            // Process integrations
            foreach (var orgEntity in orgsToProcess)
            {
                var ownershipBuffer = SystemAPI.GetBuffer<OrgOwnership>(orgEntity);
                
                // Find the owner with 100% share
                Entity? ownerOrg = null;
                for (int i = 0; i < ownershipBuffer.Length; i++)
                {
                    if (ownershipBuffer[i].Share >= 1f)
                    {
                        ownerOrg = ownershipBuffer[i].OwnerOrg;
                        break;
                    }
                }

                if (!ownerOrg.HasValue)
                    continue;

                // Merge orgs: transfer members from target to owner
                if (SystemAPI.HasComponent<AggregateEntity>(orgEntity) && 
                    SystemAPI.HasComponent<AggregateEntity>(ownerOrg.Value))
                {
                    MergeAggregates(state, ecb, orgEntity, ownerOrg.Value);
                }

                // Update relation to Integrated
                var relationEntity = FindRelationEntity(state, ownerOrg.Value, orgEntity);
                if (relationEntity.HasValue && SystemAPI.HasComponent<OrgRelation>(relationEntity.Value))
                {
                    var relation = SystemAPI.GetComponentRW<OrgRelation>(relationEntity.Value);
                    relation.ValueRW.Kind = OrgRelationKind.Integrated;
                    relation.ValueRW.Treaties &= ~OrgTreatyFlags.IntegrationProcess;
                    relation.ValueRW.Attitude = math.max(relation.ValueRO.Attitude, 50f); // Ensure positive
                    relation.ValueRW.Trust = math.max(relation.ValueRO.Trust, 0.7f);
                }

                // Remove ownership buffer (fully integrated)
                ecb.RemoveComponent<OrgOwnership>(orgEntity);
                
                // Optionally remove the integrated org entity if it should be dissolved
                // For now, keep it but mark as integrated
                if (SystemAPI.HasComponent<OrgId>(orgEntity))
                {
                    var orgId = SystemAPI.GetComponent<OrgId>(orgEntity);
                    orgId.ParentOrgId = SystemAPI.GetComponent<OrgId>(ownerOrg.Value).Value;
                    ecb.SetComponent(orgEntity, orgId);
                }
            }
        }

        private static void MergeAggregates(SystemState state, EntityCommandBuffer ecb, Entity sourceOrg, Entity targetOrg)
        {
            // Transfer members from source to target
            if (SystemAPI.HasBuffer<AggregateMember>(sourceOrg) && 
                SystemAPI.HasBuffer<AggregateMember>(targetOrg))
            {
                var sourceMembers = SystemAPI.GetBuffer<AggregateMember>(sourceOrg);
                var targetMembers = SystemAPI.GetBuffer<AggregateMember>(targetOrg);

                for (int i = 0; i < sourceMembers.Length; i++)
                {
                    var member = sourceMembers[i];
                    if (SystemAPI.Exists(member.MemberEntity))
                    {
                        // Add member to target aggregate
                        targetMembers.Add(member);
                        
                        // Update member's aggregate membership if component exists
                        // Note: AggregateMembership component may not exist on all entities
                        // This is optional - the member will still be in the target aggregate's member list
                    }
                }

                // Clear source members
                sourceMembers.Clear();
            }

            // Update target aggregate member count
            if (SystemAPI.HasComponent<AggregateEntity>(targetOrg))
            {
                var aggregate = SystemAPI.GetComponentRW<AggregateEntity>(targetOrg);
                if (SystemAPI.HasBuffer<AggregateMember>(targetOrg))
                {
                    aggregate.ValueRW.MemberCount = (ushort)SystemAPI.GetBuffer<AggregateMember>(targetOrg).Length;
                }
            }
        }

        private static OrgRelation? GetRelation(SystemState state, Entity orgA, Entity orgB)
        {
            foreach (var relation in SystemAPI.Query<RefRO<OrgRelation>>()
                .WithAll<OrgRelationTag>())
            {
                if ((relation.ValueRO.OrgA == orgA && relation.ValueRO.OrgB == orgB) ||
                    (relation.ValueRO.OrgA == orgB && relation.ValueRO.OrgB == orgA))
                {
                    return relation.ValueRO;
                }
            }
            return null;
        }

        private static Entity? FindRelationEntity(SystemState state, Entity orgA, Entity orgB)
        {
            foreach (var (relation, entity) in SystemAPI.Query<RefRO<OrgRelation>>()
                .WithAll<OrgRelationTag>()
                .WithEntityAccess())
            {
                if ((relation.ValueRO.OrgA == orgA && relation.ValueRO.OrgB == orgB) ||
                    (relation.ValueRO.OrgA == orgB && relation.ValueRO.OrgB == orgA))
                {
                    return entity;
                }
            }
            return null;
        }
    }
}

