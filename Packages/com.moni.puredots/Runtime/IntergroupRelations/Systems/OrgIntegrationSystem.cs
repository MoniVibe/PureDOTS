using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
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
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var currentTick = timeState.Tick;

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
                        var relation = GetRelation(ref state, ownership.OwnerOrg, orgEntity);
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
                if (SystemAPI.HasComponent<PureDOTS.Runtime.Aggregate.AggregateEntity>(orgEntity) && 
                    SystemAPI.HasComponent<PureDOTS.Runtime.Aggregate.AggregateEntity>(ownerOrg.Value))
                {
                    MergeAggregates(ref state, ecb, orgEntity, ownerOrg.Value);
                }

                // Update relation to Integrated
                var relationEntity = FindRelationEntity(ref state, ownerOrg.Value, orgEntity);
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

        private static void MergeAggregates(ref SystemState state, EntityCommandBuffer ecb, Entity sourceOrg, Entity targetOrg)
        {
            // Transfer members from source to target
            var memberBufferLookup = state.GetBufferLookup<PureDOTS.Runtime.Aggregate.AggregateMember>(false);
            memberBufferLookup.Update(ref state);
            
            if (memberBufferLookup.HasBuffer(sourceOrg) && 
                memberBufferLookup.HasBuffer(targetOrg))
            {
                var sourceMembers = memberBufferLookup[sourceOrg];
                var targetMembers = memberBufferLookup[targetOrg];

                for (int i = 0; i < sourceMembers.Length; i++)
                {
                    var member = sourceMembers[i];
                    if (state.EntityManager.Exists(member.MemberEntity))
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
            var aggregateLookup = state.GetComponentLookup<PureDOTS.Runtime.Aggregate.AggregateEntity>(false);
            aggregateLookup.Update(ref state);
            if (aggregateLookup.HasComponent(targetOrg))
            {
                var aggregate = aggregateLookup[targetOrg];
                if (memberBufferLookup.HasBuffer(targetOrg))
                {
                    aggregate.MemberCount = (ushort)memberBufferLookup[targetOrg].Length;
                    aggregateLookup[targetOrg] = aggregate;
                }
            }
        }

        private static OrgRelation? GetRelation(ref SystemState state, Entity orgA, Entity orgB)
        {
            var query = state.EntityManager.CreateEntityQuery(typeof(OrgRelation), typeof(OrgRelationTag));
            var relations = query.ToComponentDataArray<OrgRelation>(Allocator.Temp);
            
            for (int i = 0; i < relations.Length; i++)
            {
                var relation = relations[i];
                if ((relation.OrgA == orgA && relation.OrgB == orgB) ||
                    (relation.OrgA == orgB && relation.OrgB == orgA))
                {
                    relations.Dispose();
                    query.Dispose();
                    return relation;
                }
            }
            
            relations.Dispose();
            query.Dispose();
            return null;
        }

        private static Entity? FindRelationEntity(ref SystemState state, Entity orgA, Entity orgB)
        {
            var query = state.EntityManager.CreateEntityQuery(typeof(OrgRelation), typeof(OrgRelationTag));
            var relations = query.ToComponentDataArray<OrgRelation>(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < relations.Length; i++)
            {
                var relation = relations[i];
                if ((relation.OrgA == orgA && relation.OrgB == orgB) ||
                    (relation.OrgA == orgB && relation.OrgB == orgA))
                {
                    var result = entities[i];
                    relations.Dispose();
                    entities.Dispose();
                    query.Dispose();
                    return result;
                }
            }
            
            relations.Dispose();
            entities.Dispose();
            query.Dispose();
            return null;
        }
    }
}

