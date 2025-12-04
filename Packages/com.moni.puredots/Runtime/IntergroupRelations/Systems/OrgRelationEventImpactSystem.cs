using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.IntergroupRelations
{
    /// <summary>
    /// Applies event deltas to organization relations.
    /// Updates Attitude/Trust/Fear/Respect/Dependence and toggles treaties based on event outcomes.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OrgRelationInitSystem))]
    public partial struct OrgRelationEventImpactSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTick = SystemAPI.GetSingleton<TimeState>().Tick;

            // Process all relation events
            foreach (var (relationEvent, entity) in SystemAPI.Query<RefRO<OrgRelationEvent>>()
                .WithEntityAccess())
            {
                // Find the relation entity for this org pair
                Entity? relationEntity = FindRelationEntity(state, relationEvent.ValueRO.SourceOrg, relationEvent.ValueRO.TargetOrg);
                
                if (!relationEntity.HasValue)
                {
                    // Relation doesn't exist yet, create it first
                    relationEntity = CreateRelationEntity(state, relationEvent.ValueRO.SourceOrg, relationEvent.ValueRO.TargetOrg);
                }

                if (relationEntity.HasValue && SystemAPI.HasComponent<OrgRelation>(relationEntity.Value))
                {
                    var relation = SystemAPI.GetComponentRW<OrgRelation>(relationEntity.Value);

                    // Apply deltas with persona-based modifiers
                    ApplyEventDeltas(ref relation, relationEvent.ValueRO, state);

                    // Update relation kind based on new attitude
                    relation.ValueRW.Kind = DetermineRelationKind(relation.ValueRO.Attitude);

                    relation.ValueRW.LastUpdateTick = currentTick;
                }

                // Remove event component after processing
                state.EntityManager.RemoveComponent<OrgRelationEvent>(entity);
            }
        }

        private static void ApplyEventDeltas(ref RefRW<OrgRelation> relation, OrgRelationEvent evt, SystemState state)
        {
            // Get source org persona for modifier calculation
            float personaModifier = 1f;
            if (SystemAPI.HasComponent<OrgPersona>(evt.SourceOrg))
            {
                var persona = SystemAPI.GetComponent<OrgPersona>(evt.SourceOrg);
                
                // Vengeful orgs amplify negative events, forgiving orgs reduce them
                if (evt.AttitudeDelta < 0f)
                {
                    personaModifier = 0.5f + persona.VengefulForgiving * 0.5f; // 0.5x to 1.0x
                }
                else
                {
                    personaModifier = 1f - persona.VengefulForgiving * 0.3f; // 0.7x to 1.0x
                }
            }

            // Apply attitude delta with persona modifier
            relation.ValueRW.Attitude = math.clamp(
                relation.ValueRO.Attitude + evt.AttitudeDelta * personaModifier, 
                -100f, 100f);

            // Apply trust delta
            relation.ValueRW.Trust = math.clamp(
                relation.ValueRO.Trust + evt.TrustDelta, 
                0f, 1f);

            // Apply fear delta
            relation.ValueRW.Fear = math.clamp(
                relation.ValueRO.Fear + evt.FearDelta, 
                0f, 1f);

            // Apply respect delta
            relation.ValueRW.Respect = math.clamp(
                relation.ValueRO.Respect + evt.RespectDelta, 
                0f, 1f);

            // Apply dependence delta
            relation.ValueRW.Dependence = math.clamp(
                relation.ValueRO.Dependence + evt.DependenceDelta, 
                0f, 1f);

            // Update treaties
            relation.ValueRW.Treaties |= evt.TreatyFlagsToAdd;
            relation.ValueRW.Treaties &= ~evt.TreatyFlagsToRemove;
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

        private static Entity CreateRelationEntity(SystemState state, Entity orgA, Entity orgB)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var currentTick = SystemAPI.GetSingleton<TimeState>().Tick;

            var relationEntity = ecb.CreateEntity();
            ecb.AddComponent(relationEntity, new OrgRelationTag());

            // Initialize with neutral baseline
            ecb.AddComponent(relationEntity, new OrgRelation
            {
                OrgA = orgA,
                OrgB = orgB,
                Kind = OrgRelationKind.Neutral,
                Treaties = OrgTreatyFlags.None,
                Attitude = 0f,
                Trust = 0.5f,
                Fear = 0f,
                Respect = 0.5f,
                Dependence = 0f,
                EstablishedTick = currentTick,
                LastUpdateTick = currentTick
            });

            return relationEntity;
        }

        private static OrgRelationKind DetermineRelationKind(float attitude)
        {
            if (attitude >= 50f)
                return OrgRelationKind.Allied;
            if (attitude >= 25f)
                return OrgRelationKind.Friendly;
            if (attitude <= -50f)
                return OrgRelationKind.Hostile;
            if (attitude <= -25f)
                return OrgRelationKind.Rival;
            return OrgRelationKind.Neutral;
        }
    }
}

