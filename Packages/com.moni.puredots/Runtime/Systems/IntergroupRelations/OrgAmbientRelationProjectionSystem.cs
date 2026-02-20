using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.IntergroupRelations;
using PureDOTS.Runtime.Social;
using PureDOTS.Runtime.Systems.Social;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.IntergroupRelations
{
    /// <summary>
    /// Projects recent personal relation changes into slow organization-level drift.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RelationInteractionSystem))]
    public partial struct OrgAmbientRelationProjectionSystem : ISystem
    {
        private BufferLookup<AggregateMembership> _membershipLookup;
        private ComponentLookup<OrgRelation> _orgRelationLookup;
        private ComponentLookup<OrgTag> _orgTagLookup;
        private EntityStorageInfoLookup _entityInfoLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _membershipLookup = state.GetBufferLookup<AggregateMembership>(true);
            _orgRelationLookup = state.GetComponentLookup<OrgRelation>(false);
            _orgTagLookup = state.GetComponentLookup<OrgTag>(true);
            _entityInfoLookup = state.GetEntityStorageInfoLookup();

            if (SystemAPI.TryGetSingletonEntity<OrgAmbientRelationConfig>(out var existingConfigEntity))
            {
                if (!state.EntityManager.HasComponent<OrgAmbientRelationState>(existingConfigEntity))
                {
                    state.EntityManager.AddComponentData(existingConfigEntity, new OrgAmbientRelationState { LastProjectionTick = 0u });
                }
                return;
            }

            if (!SystemAPI.TryGetSingletonEntity<OrgAmbientRelationConfig>(out _))
            {
                var entity = state.EntityManager.CreateEntity(typeof(OrgAmbientRelationConfig), typeof(OrgAmbientRelationState));
                state.EntityManager.SetComponentData(entity, OrgAmbientRelationConfig.Default);
                state.EntityManager.SetComponentData(entity, new OrgAmbientRelationState { LastProjectionTick = 0u });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewind) || rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var tick = SystemAPI.GetSingleton<TimeState>().Tick;
            var configEntity = SystemAPI.GetSingletonEntity<OrgAmbientRelationConfig>();
            var config = state.EntityManager.GetComponentData<OrgAmbientRelationConfig>(configEntity);
            var projectionState = state.EntityManager.GetComponentData<OrgAmbientRelationState>(configEntity);

            if (config.Enabled == 0)
            {
                return;
            }

            if (tick > 0 && projectionState.LastProjectionTick > 0 &&
                tick - projectionState.LastProjectionTick < math.max(1u, config.UpdateIntervalTicks))
            {
                return;
            }

            _membershipLookup.Update(ref state);
            _orgRelationLookup.Update(ref state);
            _orgTagLookup.Update(ref state);
            _entityInfoLookup.Update(ref state);

            var pairDeltas = new NativeHashMap<OrgPairKey, float>(128, Allocator.Temp);
            var standingDeltas = new NativeHashMap<EntityOrgPairKey, float>(256, Allocator.Temp);
            var internalDeltas = new NativeHashMap<Entity, OrgInternalDrift>(128, Allocator.Temp);
            var edgeMap = new NativeHashMap<OrgPairKey, Entity>(128, Allocator.Temp);

            foreach (var (relation, relationEntity) in SystemAPI.Query<RefRO<OrgRelation>>().WithEntityAccess())
            {
                if (!_entityInfoLookup.Exists(relationEntity))
                {
                    continue;
                }

                edgeMap[OrgPairKey.Create(relation.ValueRO.OrgA, relation.ValueRO.OrgB)] = relationEntity;
            }

            foreach (var (relations, memberships, sourceEntity) in SystemAPI.Query<DynamicBuffer<EntityRelation>, DynamicBuffer<AggregateMembership>>().WithEntityAccess())
            {
                if (!TryResolvePrimaryOrg(in memberships, out var sourceOrg, out var sourceLoyalty) || sourceOrg == Entity.Null)
                {
                    continue;
                }

                for (int i = 0; i < relations.Length; i++)
                {
                    var relation = relations[i];
                    if (relation.OtherEntity == Entity.Null || relation.LastInteractionTick == 0u)
                    {
                        continue;
                    }

                    if (!_entityInfoLookup.Exists(relation.OtherEntity) || !_membershipLookup.HasBuffer(relation.OtherEntity))
                    {
                        continue;
                    }

                    var age = tick >= relation.LastInteractionTick ? tick - relation.LastInteractionTick : 0u;
                    if (age > math.max(1u, config.RecentInteractionHorizonTicks))
                    {
                        continue;
                    }

                    var otherMemberships = _membershipLookup[relation.OtherEntity];
                    if (!TryResolvePrimaryOrg(in otherMemberships, out var targetOrg, out var targetLoyalty) || targetOrg == Entity.Null)
                    {
                        continue;
                    }

                    var recency = 1f - math.saturate((float)age / math.max(1f, config.RecentInteractionHorizonTicks));
                    var loyaltyWeight = math.saturate(sourceLoyalty) * math.saturate(targetLoyalty);
                    if (recency <= 0f || loyaltyWeight <= 0f)
                    {
                        continue;
                    }

                    var intensityNorm = math.clamp(relation.Intensity / 100f, -1f, 1f);
                    if (sourceOrg == targetOrg)
                    {
                        if (relation.Intensity > config.InternalConflictThreshold)
                        {
                            continue;
                        }

                        var severity = math.saturate((config.InternalConflictThreshold - relation.Intensity) / 100f);
                        var weight = severity * recency * loyaltyWeight;
                        AccumulateInternalDrift(ref internalDeltas, sourceOrg, new OrgInternalDrift
                        {
                            CohesionPenalty = weight * config.InternalCohesionPenaltyPerUnit,
                            CorruptionIncrease = weight * config.InternalCorruptionPerUnit,
                            PurityPenalty = weight * config.InternalPurityPenaltyPerUnit,
                            OrderPenalty = weight * config.InternalOrderPenaltyPerUnit,
                            VengefulIncrease = weight * config.InternalVengefulShiftPerUnit
                        });

                        var standingPenalty = -weight * config.InternalStandingPenaltyPerUnit;
                        if (math.abs(standingPenalty) >= 0.001f)
                        {
                            AccumulateStandingDelta(ref standingDeltas, sourceEntity, sourceOrg, standingPenalty);
                        }
                    }
                    else
                    {
                        if (!_orgTagLookup.HasComponent(sourceOrg) || !_orgTagLookup.HasComponent(targetOrg))
                        {
                            continue;
                        }

                        var delta = intensityNorm * recency * loyaltyWeight * config.ExternalAttitudePerUnit;
                        if (math.abs(delta) < 0.001f)
                        {
                            continue;
                        }

                        var key = OrgPairKey.Create(sourceOrg, targetOrg);
                        if (pairDeltas.TryGetValue(key, out var existing))
                        {
                            pairDeltas[key] = existing + delta;
                        }
                        else
                        {
                            pairDeltas.Add(key, delta);
                        }

                        var standingDelta = intensityNorm * recency * loyaltyWeight * config.ExternalStandingPerUnit;
                        if (math.abs(standingDelta) >= 0.001f)
                        {
                            AccumulateStandingDelta(ref standingDeltas, sourceEntity, targetOrg, standingDelta);
                        }
                    }
                }
            }

            var pairEntries = pairDeltas.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < pairEntries.Length; i++)
            {
                var key = pairEntries.Keys[i];
                var delta = math.clamp(pairEntries.Values[i], -config.MaxAttitudeDeltaPerTick, config.MaxAttitudeDeltaPerTick);
                if (math.abs(delta) < 0.0001f)
                {
                    continue;
                }

                Entity relationEntity;
                OrgRelation orgRelation;
                if (edgeMap.TryGetValue(key, out relationEntity) && _entityInfoLookup.Exists(relationEntity) && _orgRelationLookup.HasComponent(relationEntity))
                {
                    orgRelation = _orgRelationLookup[relationEntity];
                }
                else
                {
                    relationEntity = state.EntityManager.CreateEntity(typeof(OrgRelation), typeof(OrgRelationTag));
                    orgRelation = new OrgRelation
                    {
                        OrgA = key.OrgA,
                        OrgB = key.OrgB,
                        Kind = OrgRelationKind.Neutral,
                        Treaties = OrgTreatyFlags.None,
                        Attitude = 0f,
                        Trust = 0.5f,
                        Fear = 0f,
                        Respect = 0.5f,
                        Dependence = 0f,
                        EstablishedTick = tick,
                        LastUpdateTick = tick
                    };
                }

                orgRelation.Attitude = math.clamp(orgRelation.Attitude + delta, -100f, 100f);
                orgRelation.Kind = ResolveKind(orgRelation.Attitude);
                orgRelation.LastUpdateTick = tick;
                state.EntityManager.SetComponentData(relationEntity, orgRelation);
            }
            pairEntries.Dispose();

            var standingEntries = standingDeltas.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < standingEntries.Length; i++)
            {
                var key = standingEntries.Keys[i];
                if (!_entityInfoLookup.Exists(key.Entity) || !_entityInfoLookup.Exists(key.Org))
                {
                    continue;
                }

                var delta = math.clamp(standingEntries.Values[i], -config.MaxStandingDeltaPerTick, config.MaxStandingDeltaPerTick);
                if (math.abs(delta) < 0.0001f)
                {
                    continue;
                }

                ApplyEntityOrgStanding(ref state, key.Entity, key.Org, tick, delta);
            }
            standingEntries.Dispose();

            var internalEntries = internalDeltas.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < internalEntries.Length; i++)
            {
                var org = internalEntries.Keys[i];
                if (!_entityInfoLookup.Exists(org))
                {
                    continue;
                }

                var drift = internalEntries.Values[i];
                var maxStep = math.max(0.0001f, config.MaxInternalDeltaPerTick);

                if (!state.EntityManager.HasComponent<OrgPersona>(org))
                {
                    state.EntityManager.AddComponentData(org, new OrgPersona
                    {
                        VengefulForgiving = 0.5f,
                        CravenBold = 0.5f,
                        Cohesion = 0.5f,
                        LastUpdateTick = tick
                    });
                }

                if (!state.EntityManager.HasComponent<OrgAlignment>(org))
                {
                    state.EntityManager.AddComponentData(org, new OrgAlignment
                    {
                        Moral = 0f,
                        Order = 0f,
                        Purity = 0f
                    });
                }

                if (!state.EntityManager.HasComponent<OrgCorruption>(org))
                {
                    state.EntityManager.AddComponentData(org, new OrgCorruption
                    {
                        Level = 0f,
                        RecentPressure = 0f,
                        LastUpdateTick = tick
                    });
                }

                var persona = state.EntityManager.GetComponentData<OrgPersona>(org);
                persona.Cohesion = math.saturate(persona.Cohesion - math.min(drift.CohesionPenalty, maxStep));
                persona.VengefulForgiving = math.saturate(persona.VengefulForgiving + math.min(drift.VengefulIncrease, maxStep));
                persona.LastUpdateTick = tick;
                state.EntityManager.SetComponentData(org, persona);

                var alignment = state.EntityManager.GetComponentData<OrgAlignment>(org);
                alignment.Purity = math.clamp(alignment.Purity - math.min(drift.PurityPenalty, maxStep), -1f, 1f);
                alignment.Order = math.clamp(alignment.Order - math.min(drift.OrderPenalty, maxStep), -1f, 1f);
                state.EntityManager.SetComponentData(org, alignment);

                var corruption = state.EntityManager.GetComponentData<OrgCorruption>(org);
                var corruptionStep = math.min(math.max(0f, drift.CorruptionIncrease), math.max(0.0001f, config.MaxCorruptionDeltaPerTick));
                corruption.Level = math.saturate(corruption.Level + corruptionStep);
                corruption.RecentPressure = corruptionStep;
                corruption.LastUpdateTick = tick;
                state.EntityManager.SetComponentData(org, corruption);
            }
            internalEntries.Dispose();

            pairDeltas.Dispose();
            standingDeltas.Dispose();
            internalDeltas.Dispose();
            edgeMap.Dispose();

            projectionState.LastProjectionTick = tick;
            state.EntityManager.SetComponentData(configEntity, projectionState);
        }

        private static OrgRelationKind ResolveKind(float attitude)
        {
            if (attitude >= 50f) return OrgRelationKind.Allied;
            if (attitude >= 25f) return OrgRelationKind.Friendly;
            if (attitude <= -50f) return OrgRelationKind.Hostile;
            if (attitude <= -25f) return OrgRelationKind.Rival;
            return OrgRelationKind.Neutral;
        }

        private static bool TryResolvePrimaryOrg(
            in DynamicBuffer<AggregateMembership> memberships,
            out Entity org,
            out float loyalty)
        {
            org = Entity.Null;
            loyalty = 0f;
            float best = float.NegativeInfinity;

            for (int i = 0; i < memberships.Length; i++)
            {
                var membership = memberships[i];
                if (membership.AggregateEntity == Entity.Null)
                {
                    continue;
                }

                var score = membership.LoyaltyToAggregate;
                if (score <= best)
                {
                    continue;
                }

                best = score;
                org = membership.AggregateEntity;
                loyalty = math.saturate(score);
            }

            return org != Entity.Null;
        }

        private static void AccumulateInternalDrift(
            ref NativeHashMap<Entity, OrgInternalDrift> map,
            Entity org,
            in OrgInternalDrift delta)
        {
            if (map.TryGetValue(org, out var existing))
            {
                existing.CohesionPenalty += delta.CohesionPenalty;
                existing.CorruptionIncrease += delta.CorruptionIncrease;
                existing.PurityPenalty += delta.PurityPenalty;
                existing.OrderPenalty += delta.OrderPenalty;
                existing.VengefulIncrease += delta.VengefulIncrease;
                map[org] = existing;
            }
            else
            {
                map.Add(org, delta);
            }
        }

        private static void AccumulateStandingDelta(
            ref NativeHashMap<EntityOrgPairKey, float> map,
            Entity entity,
            Entity org,
            float delta)
        {
            var key = new EntityOrgPairKey { Entity = entity, Org = org };
            if (map.TryGetValue(key, out var existing))
            {
                map[key] = existing + delta;
            }
            else
            {
                map.Add(key, delta);
            }
        }

        private static void ApplyEntityOrgStanding(
            ref SystemState state,
            Entity entity,
            Entity org,
            uint tick,
            float delta)
        {
            if (!state.EntityManager.HasBuffer<EntityOrgStanding>(entity))
            {
                state.EntityManager.AddBuffer<EntityOrgStanding>(entity);
            }

            var standings = state.EntityManager.GetBuffer<EntityOrgStanding>(entity);
            var trustDelta = (int)math.round(delta * 0.5f);
            var familiarityDelta = (byte)(math.abs(delta) >= 0.25f ? 1 : 0);

            for (int i = 0; i < standings.Length; i++)
            {
                var entry = standings[i];
                if (entry.OrgEntity != org)
                {
                    continue;
                }

                entry.Score = (sbyte)math.clamp((int)entry.Score + (int)math.round(delta), -100, 100);
                entry.Trust = (byte)math.clamp((int)entry.Trust + trustDelta, 0, 100);
                entry.Familiarity = (byte)math.clamp(entry.Familiarity + familiarityDelta, 0, 100);
                entry.LastInteractionTick = tick;
                standings[i] = entry;
                return;
            }

            standings.Add(new EntityOrgStanding
            {
                OrgEntity = org,
                Score = (sbyte)math.clamp((int)math.round(delta), -100, 100),
                Trust = (byte)math.clamp(50 + trustDelta, 0, 100),
                Familiarity = familiarityDelta,
                LastInteractionTick = tick
            });
        }

        private struct OrgInternalDrift
        {
            public float CohesionPenalty;
            public float CorruptionIncrease;
            public float PurityPenalty;
            public float OrderPenalty;
            public float VengefulIncrease;
        }

        private struct EntityOrgPairKey : System.IEquatable<EntityOrgPairKey>
        {
            public Entity Entity;
            public Entity Org;

            public bool Equals(EntityOrgPairKey other)
            {
                return Entity.Equals(other.Entity) && Org.Equals(other.Org);
            }

            public override int GetHashCode()
            {
                return (int)math.hash(new uint4(
                    (uint)math.max(Entity.Index, 0),
                    (uint)Entity.Version,
                    (uint)math.max(Org.Index, 0),
                    (uint)Org.Version));
            }
        }

        private struct OrgPairKey : System.IEquatable<OrgPairKey>
        {
            public Entity OrgA;
            public Entity OrgB;

            public static OrgPairKey Create(Entity left, Entity right)
            {
                var leftHash = ((uint)math.max(left.Index, 0) * 486187739u) ^ (uint)left.Version;
                var rightHash = ((uint)math.max(right.Index, 0) * 486187739u) ^ (uint)right.Version;
                return leftHash <= rightHash
                    ? new OrgPairKey { OrgA = left, OrgB = right }
                    : new OrgPairKey { OrgA = right, OrgB = left };
            }

            public bool Equals(OrgPairKey other)
            {
                return OrgA.Equals(other.OrgA) && OrgB.Equals(other.OrgB);
            }

            public override int GetHashCode()
            {
                return (int)math.hash(new uint4(
                    (uint)math.max(OrgA.Index, 0),
                    (uint)OrgA.Version,
                    (uint)math.max(OrgB.Index, 0),
                    (uint)OrgB.Version));
            }
        }
    }
}
