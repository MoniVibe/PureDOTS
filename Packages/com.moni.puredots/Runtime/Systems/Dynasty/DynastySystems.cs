using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using PureDOTS.Runtime.Dynasty;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Succession;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Lifecycle;
using PureDOTS.Runtime.Social;
using PureDOTS.Runtime.Stats;
using PureDOTS.Runtime.Economy.Wealth;
using Unity.Mathematics;

namespace PureDOTS.Systems.Dynasty
{
    /// <summary>
    /// System that handles dynasty succession when leaders die.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PureDOTS.Systems.Lifecycle.MortalitySystem))]
    public partial struct DynastySuccessionSystem : ISystem
    {
        private ComponentLookup<DynastyMember> _dynastyMemberLookup;
        private ComponentLookup<SuccessionEvent> _successionEventLookup;
        private ComponentLookup<DynastySuccessionPreferences> _successionPreferencesLookup;
        private ComponentLookup<SocialStanding> _socialStandingLookup;
        private ComponentLookup<IndividualStats> _individualStatsLookup;
        private ComponentLookup<PureDOTS.Runtime.Individual.IndividualStats> _altIndividualStatsLookup;
        private ComponentLookup<VillagerReputation> _villagerReputationLookup;
        private ComponentLookup<VillagerWealth> _villagerWealthLookup;
        private ComponentLookup<PhysiqueFinesseWill> _physiqueLookup;
        private BufferLookup<DynastyLineage> _dynastyLineageLookup;
        private ComponentLookup<SuccessionCrisis> _successionCrisisLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _dynastyMemberLookup = state.GetComponentLookup<DynastyMember>(true);
            _successionEventLookup = state.GetComponentLookup<SuccessionEvent>(true);
            _successionPreferencesLookup = state.GetComponentLookup<DynastySuccessionPreferences>(true);
            _socialStandingLookup = state.GetComponentLookup<SocialStanding>(true);
            _individualStatsLookup = state.GetComponentLookup<IndividualStats>(true);
            _altIndividualStatsLookup = state.GetComponentLookup<PureDOTS.Runtime.Individual.IndividualStats>(true);
            _villagerReputationLookup = state.GetComponentLookup<VillagerReputation>(true);
            _villagerWealthLookup = state.GetComponentLookup<VillagerWealth>(true);
            _physiqueLookup = state.GetComponentLookup<PhysiqueFinesseWill>(true);
            _dynastyLineageLookup = state.GetBufferLookup<DynastyLineage>(true);
            _successionCrisisLookup = state.GetComponentLookup<SuccessionCrisis>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            _dynastyMemberLookup.Update(ref state);
            _successionEventLookup.Update(ref state);
            _successionPreferencesLookup.Update(ref state);
            _socialStandingLookup.Update(ref state);
            _individualStatsLookup.Update(ref state);
            _altIndividualStatsLookup.Update(ref state);
            _villagerReputationLookup.Update(ref state);
            _villagerWealthLookup.Update(ref state);
            _physiqueLookup.Update(ref state);
            _dynastyLineageLookup.Update(ref state);
            _successionCrisisLookup.Update(ref state);

            // Succession reads death buffers that may be touched by prior lifecycle jobs this frame.
            // Force completion to avoid safety contention in mixed Run/Schedule paths.
            state.Dependency.Complete();

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            // Process deaths to detect leader/founder deaths
            var deathJob = new ProcessLeaderDeathsJob
            {
                Ecb = ecb,
                CurrentTick = timeState.Tick,
                DynastyMemberLookup = _dynastyMemberLookup,
                SuccessionEventLookup = _successionEventLookup
            };
            deathJob.Run();

            // Process succession for dynasties that need it
            var successionJob = new ProcessSuccessionJob
            {
                Ecb = ecb,
                CurrentTick = timeState.Tick,
                DynastyMemberLookup = _dynastyMemberLookup,
                SuccessionPreferencesLookup = _successionPreferencesLookup,
                SocialStandingLookup = _socialStandingLookup,
                IndividualStatsLookup = _individualStatsLookup,
                AltIndividualStatsLookup = _altIndividualStatsLookup,
                VillagerReputationLookup = _villagerReputationLookup,
                VillagerWealthLookup = _villagerWealthLookup,
                PhysiqueLookup = _physiqueLookup,
                DynastyLineageLookup = _dynastyLineageLookup,
                SuccessionCrisisLookup = _successionCrisisLookup
            };
            successionJob.Run();
        }

        partial struct ProcessLeaderDeathsJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            public uint CurrentTick;
            [ReadOnly]
            public ComponentLookup<DynastyMember> DynastyMemberLookup;
            [ReadOnly]
            public ComponentLookup<SuccessionEvent> SuccessionEventLookup;

            void Execute(Entity entity, in DynamicBuffer<DeathEvent> deathEvents)
            {
                for (int i = 0; i < deathEvents.Length; i++)
                {
                    var deathEvent = deathEvents[i];
                    Entity deadEntity = deathEvent.DeadEntity;

                    // Validate that deceased was a dynasty member
                    if (!DynastyMemberLookup.HasComponent(deadEntity))
                        continue;

                    var dynastyMember = DynastyMemberLookup[deadEntity];
                    
                    // Validate dynasty entity is valid
                    if (dynastyMember.DynastyEntity == Entity.Null)
                        continue;

                    // Only process if deceased was Founder or Heir
                    if (dynastyMember.Rank != DynastyRank.Founder && dynastyMember.Rank != DynastyRank.Heir)
                        continue;

                    var newEvent = new SuccessionEvent
                    {
                        DeceasedEntity = deadEntity,
                        SuccessorEntity = Entity.Null,
                        TypeUsed = SuccessionType.Primogeniture,
                        OccurredTick = CurrentTick,
                        ResolvedTick = 0,
                        WasContested = 0,
                        WasSuccessful = 0
                    };

                    // Check if succession event already exists (prevent duplicates)
                    if (SuccessionEventLookup.HasComponent(dynastyMember.DynastyEntity))
                    {
                        var existingEvent = SuccessionEventLookup[dynastyMember.DynastyEntity];
                        if (existingEvent.WasSuccessful == 0)
                        {
                            // Unresolved succession already in flight.
                            if (existingEvent.DeceasedEntity == deadEntity)
                            {
                                continue;
                            }
                            continue;
                        }

                        // Overwrite resolved succession with the new event.
                        Ecb.SetComponent(dynastyMember.DynastyEntity, newEvent);
                        continue;
                    }

                    // Mark dynasty for succession processing
                    Ecb.AddComponent(dynastyMember.DynastyEntity, newEvent);
                }
            }
        }

        partial struct ProcessSuccessionJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            public uint CurrentTick;
            [ReadOnly]
            public ComponentLookup<DynastyMember> DynastyMemberLookup;
            [ReadOnly]
            public ComponentLookup<DynastySuccessionPreferences> SuccessionPreferencesLookup;
            [ReadOnly]
            public ComponentLookup<SocialStanding> SocialStandingLookup;
            [ReadOnly]
            public ComponentLookup<IndividualStats> IndividualStatsLookup;
            [ReadOnly]
            public ComponentLookup<PureDOTS.Runtime.Individual.IndividualStats> AltIndividualStatsLookup;
            [ReadOnly]
            public ComponentLookup<VillagerReputation> VillagerReputationLookup;
            [ReadOnly]
            public ComponentLookup<VillagerWealth> VillagerWealthLookup;
            [ReadOnly]
            public ComponentLookup<PhysiqueFinesseWill> PhysiqueLookup;
            [ReadOnly]
            public BufferLookup<DynastyLineage> DynastyLineageLookup;
            [ReadOnly]
            public ComponentLookup<SuccessionCrisis> SuccessionCrisisLookup;

            void Execute(
                Entity entity,
                ref DynastyIdentity identity,
                ref SuccessionEvent successionEvent,
                in DynastySuccessionRules rules,
                in DynastySuccessionPreferences preferences,
                ref DynamicBuffer<DynastyMemberEntry> members)
            {
                // Skip if already resolved
                if (successionEvent.WasSuccessful != 0)
                    return;

                // Validate that deceased entity was actually a dynasty member
                bool deceasedWasMember = false;
                for (int i = 0; i < members.Length; i++)
                {
                    if (members[i].MemberEntity == successionEvent.DeceasedEntity)
                    {
                        deceasedWasMember = true;
                        break;
                    }
                }
                if (!deceasedWasMember)
                {
                    // Invalid succession event - remove component to prevent infinite reprocessing
                    // This can happen if a SuccessionEvent was created for an entity that was
                    // removed from the dynasty before the succession job ran
                    Ecb.RemoveComponent<SuccessionEvent>(entity);
                    return;
                }

                // Build heir candidates and select best one
                Entity selectedHeir = Entity.Null;
                float bestSuitability = -1f;
                byte bestPriority = byte.MaxValue;
                int strongClaims = 0;
                float claimSum = 0f;
                float claimVariance = 0f;
                int eligibleCandidates = 0;

                byte deceasedGeneration = ResolveGeneration(entity, successionEvent.DeceasedEntity);
                
                for (int i = 0; i < members.Length; i++)
                {
                    var member = members[i];
                    // Skip the deceased entity
                    if (member.MemberEntity == successionEvent.DeceasedEntity)
                        continue;

                    // Check if member meets requirements
                    bool isBloodline = member.LineageStrength >= rules.MinLineageStrength;
                    if (rules.RequiresBloodline != 0 && !isBloodline)
                        continue;

                    // Calculate suitability based on succession type
                    float suitability = CalculateSuitability(member.MemberEntity, member.LineageStrength, preferences);
                    byte priority = (byte)member.Rank;
                    bool isDesignated = member.Rank == DynastyRank.Heir;
                    byte candidateGeneration = ResolveGeneration(entity, member.MemberEntity);
                    byte generationalDistance = (byte)math.abs(candidateGeneration - deceasedGeneration);
                    float legitimacy = math.saturate(member.LineageStrength);
                    float claim = SuccessionHelpers.CalculateClaimStrength(
                        isBloodline,
                        generationalDistance,
                        legitimacy,
                        isDesignated);

                    if (claim >= preferences.ClaimDisputeThreshold)
                    {
                        strongClaims++;
                    }

                    claimSum += claim;
                    claimVariance += claim * claim;
                    eligibleCandidates++;

                    // Apply succession type selection logic
                    bool isCandidate = false;
                    switch (rules.SuccessionType)
                    {
                        case SuccessionType.Primogeniture:
                        case SuccessionType.Seniority:
                            // Lower priority (rank) = better
                            if (selectedHeir == Entity.Null || priority < bestPriority ||
                                (priority == bestPriority && suitability > bestSuitability + preferences.MeritTieBreak))
                            {
                                isCandidate = true;
                                bestPriority = priority;
                            }
                            break;
                        case SuccessionType.Ultimogeniture:
                            // Higher priority = better (youngest)
                            if (selectedHeir == Entity.Null || priority > bestPriority ||
                                (priority == bestPriority && suitability > bestSuitability + preferences.MeritTieBreak))
                            {
                                isCandidate = true;
                                bestPriority = priority;
                            }
                            break;
                        case SuccessionType.Designated:
                            // Prefer designated heirs
                            if (isDesignated || selectedHeir == Entity.Null)
                            {
                                isCandidate = true;
                            }
                            break;
                        case SuccessionType.Meritocratic:
                            // Best suitability
                            if (suitability > bestSuitability)
                            {
                                isCandidate = true;
                                bestSuitability = suitability;
                            }
                            break;
                        default:
                            // Default to suitability
                            if (suitability > bestSuitability)
                            {
                                isCandidate = true;
                                bestSuitability = suitability;
                            }
                            break;
                    }

                    if (isCandidate)
                    {
                        selectedHeir = member.MemberEntity;
                        if (rules.SuccessionType == SuccessionType.Meritocratic || 
                            rules.SuccessionType == SuccessionType.Random)
                        {
                            bestSuitability = suitability;
                        }
                    }
                }

                if (eligibleCandidates > 1 && strongClaims > 1)
                {
                    successionEvent.WasContested = 1;

                    if (!SuccessionCrisisLookup.HasComponent(entity))
                    {
                        float intensity = CalculateCrisisIntensity(eligibleCandidates, claimSum, claimVariance);
                        Ecb.AddComponent(entity, new SuccessionCrisis
                        {
                            SubjectEntity = entity,
                            ClaimantCount = (byte)math.min(255, strongClaims),
                            Intensity = intensity,
                            StartTick = CurrentTick,
                            DeadlineTick = CurrentTick + 1000,
                            RequiresVote = (byte)(rules.SuccessionType == SuccessionType.Elective ? 1 : 0),
                            RequiresCombat = (byte)(rules.SuccessionType == SuccessionType.Meritocratic ? 1 : 0)
                        });
                    }
                }

                if (selectedHeir != Entity.Null)
                {
                    // Update succession event
                    successionEvent.SuccessorEntity = selectedHeir;
                    successionEvent.TypeUsed = rules.SuccessionType;
                    successionEvent.ResolvedTick = CurrentTick;
                    successionEvent.WasSuccessful = 1;

                    // Determine new rank for successor
                    DynastyRank newRank = DynastyRank.Heir;
                    if (identity.FounderEntity == successionEvent.DeceasedEntity)
                    {
                        // New founder
                        identity.FounderEntity = selectedHeir;
                        newRank = DynastyRank.Founder;
                    }

                    // Update member rank in buffer
                    for (int i = 0; i < members.Length; i++)
                    {
                        if (members[i].MemberEntity == selectedHeir)
                        {
                            var entry = members[i];
                            entry.Rank = newRank;
                            members[i] = entry;
                            break;
                        }
                    }

                    // Update DynastyMember component on successor entity
                    if (DynastyMemberLookup.HasComponent(selectedHeir))
                    {
                        var member = DynastyMemberLookup[selectedHeir];
                        member.Rank = newRank;
                        Ecb.SetComponent(selectedHeir, member);
                    }

                    // Demote previous heir if they exist and weren't selected
                    Entity previousHeir = Entity.Null;
                    for (int i = 0; i < members.Length; i++)
                    {
                        if (members[i].MemberEntity != selectedHeir && 
                            members[i].Rank == DynastyRank.Heir &&
                            members[i].MemberEntity != successionEvent.DeceasedEntity)
                        {
                            previousHeir = members[i].MemberEntity;
                            var entry = members[i];
                            entry.Rank = DynastyRank.Noble; // Demote to Noble
                            members[i] = entry;
                            break;
                        }
                    }

                    // Update previous heir's component if demoted
                    if (previousHeir != Entity.Null && DynastyMemberLookup.HasComponent(previousHeir))
                    {
                        var member = DynastyMemberLookup[previousHeir];
                        member.Rank = DynastyRank.Noble;
                        Ecb.SetComponent(previousHeir, member);
                    }
                }
            }

            private byte ResolveGeneration(Entity dynastyEntity, Entity member)
            {
                if (!DynastyLineageLookup.HasBuffer(dynastyEntity))
                {
                    return 0;
                }

                var lineage = DynastyLineageLookup[dynastyEntity];
                for (int i = 0; i < lineage.Length; i++)
                {
                    if (lineage[i].MemberEntity == member)
                    {
                        return lineage[i].Generation;
                    }
                }

                return 0;
            }

            private float CalculateSuitability(Entity member, float lineageStrength, in DynastySuccessionPreferences preferences)
            {
                float warlike = 0.5f;
                float materialist = 0.5f;
                float intellect = 0.5f;
                float diplomacy = 0.5f;
                float spiritual = 0.5f;
                float prestige = 0.5f;
                float wealth = 0.5f;

                if (IndividualStatsLookup.HasComponent(member))
                {
                    var stats = IndividualStatsLookup[member];
                    warlike = math.saturate(((float)stats.Command + (float)stats.Tactics + (float)stats.Resolve) / 300f);
                    materialist = math.saturate(((float)stats.Logistics + (float)stats.Engineering) / 200f);
                    intellect = math.saturate(((float)stats.Diplomacy + (float)stats.Engineering) / 200f);
                    diplomacy = math.saturate((float)stats.Diplomacy / 100f);
                    if (PhysiqueLookup.HasComponent(member))
                    {
                        var physique = PhysiqueLookup[member];
                        spiritual = math.saturate((float)physique.Will / 10f);
                    }
                }
                else if (AltIndividualStatsLookup.HasComponent(member))
                {
                    var stats = AltIndividualStatsLookup[member];
                    warlike = math.saturate((stats.Physique + stats.Agility) / 20f);
                    materialist = math.saturate(stats.Intellect / 10f);
                    intellect = math.saturate(stats.Intellect / 10f);
                    diplomacy = math.saturate(stats.Social / 10f);
                    spiritual = math.saturate(stats.Faith / 10f);
                }

                if (VillagerReputationLookup.HasComponent(member))
                {
                    var reputation = VillagerReputationLookup[member];
                    float gloryScore = math.saturate((reputation.Glory + reputation.Renown) / 200f);
                    warlike = math.saturate((warlike + gloryScore) * 0.5f);

                    float renownScore = math.saturate((reputation.Renown + reputation.Honor) / 200f);
                    prestige = math.saturate(math.max(prestige, renownScore));
                }

                if (VillagerWealthLookup.HasComponent(member))
                {
                    var wealthData = VillagerWealthLookup[member];
                    float wealthTierMax = math.max(1f, (float)WealthTier.UltraHigh);
                    float wealthTierScore = math.saturate((float)wealthData.Tier / wealthTierMax);
                    wealth = wealthTierScore;
                    materialist = math.saturate((materialist + wealthTierScore) * 0.5f);
                }

                if (SocialStandingLookup.HasComponent(member))
                {
                    var standing = SocialStandingLookup[member];
                    prestige = math.saturate(math.max(prestige, (standing.Reputation + 100) / 200f));
                }

                float weightSum = preferences.LineageWeight + preferences.WarlikeWeight +
                                 preferences.MaterialistWeight + preferences.IntellectWeight +
                                 preferences.DiplomacyWeight + preferences.SpiritualWeight +
                                 preferences.WealthWeight + preferences.PrestigeWeight;

                if (weightSum <= 0f)
                {
                    return lineageStrength;
                }

                float score = 0f;
                score += lineageStrength * preferences.LineageWeight;
                score += warlike * preferences.WarlikeWeight;
                score += materialist * preferences.MaterialistWeight;
                score += intellect * preferences.IntellectWeight;
                score += diplomacy * preferences.DiplomacyWeight;
                score += spiritual * preferences.SpiritualWeight;
                score += wealth * preferences.WealthWeight;
                score += prestige * preferences.PrestigeWeight;

                return math.saturate(score / weightSum);
            }

            private static float CalculateCrisisIntensity(int candidateCount, float claimSum, float claimVarianceSum)
            {
                if (candidateCount <= 1)
                {
                    return 0f;
                }

                float avgClaim = claimSum / math.max(1, candidateCount);
                float variance = 0f;
                if (candidateCount > 0)
                {
                    variance = (claimVarianceSum / candidateCount) - avgClaim * avgClaim;
                }

                float countFactor = math.min(1f, candidateCount * 0.2f);
                float contestedFactor = 1f - math.sqrt(math.max(0f, variance));
                return math.saturate(countFactor * contestedFactor);
            }
        }
    }

    /// <summary>
    /// System that tracks dynasty lineage when new members are born.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DynastyLineageTrackingSystem : ISystem
    {
        private ComponentLookup<DynastyMember> _dynastyMemberLookup;
        private BufferLookup<DynastyMemberEntry> _dynastyMemberEntryLookup;
        private BufferLookup<DynastyLineage> _dynastyLineageLookup;
        private BufferLookup<EntityRelation> _entityRelationLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _dynastyMemberLookup = state.GetComponentLookup<DynastyMember>(true);
            _dynastyMemberEntryLookup = state.GetBufferLookup<DynastyMemberEntry>(false);
            _dynastyLineageLookup = state.GetBufferLookup<DynastyLineage>(true);
            _entityRelationLookup = state.GetBufferLookup<EntityRelation>(false); // Writable for bidirectional relation creation
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            _dynastyMemberLookup.Update(ref state);
            _dynastyMemberEntryLookup.Update(ref state);
            _dynastyLineageLookup.Update(ref state);
            _entityRelationLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecbParallel = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            // Process death events
            var deathJob = new ProcessDeathEventsJob
            {
                Ecb = ecbParallel,
                CurrentTick = timeState.Tick,
                DynastyMemberLookup = _dynastyMemberLookup,
                DynastyMemberEntryLookup = _dynastyMemberEntryLookup
            };
            deathJob.ScheduleParallel();

            // Process birth events (needs single-threaded ECB for service calls)
            var birthJob = new ProcessBirthEventsJob
            {
                Ecb = ecb,
                CurrentTick = timeState.Tick,
                DynastyMemberLookup = _dynastyMemberLookup,
                DynastyLineageLookup = _dynastyLineageLookup,
                EntityRelationLookup = _entityRelationLookup
            };
            birthJob.Run();

            // Track new lineage entries (births handled separately)
            var trackJob = new TrackLineageJob
            {
                Ecb = ecbParallel,
                CurrentTick = timeState.Tick
            };
            trackJob.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ProcessDeathEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public uint CurrentTick;
            [ReadOnly]
            public ComponentLookup<DynastyMember> DynastyMemberLookup;
            [NativeDisableParallelForRestriction]
            public BufferLookup<DynastyMemberEntry> DynastyMemberEntryLookup;

            void Execute([EntityIndexInQuery] int index, DynamicBuffer<DeathEvent> deathEvents)
            {
                for (int i = 0; i < deathEvents.Length; i++)
                {
                    var deathEvent = deathEvents[i];
                    Entity deadEntity = deathEvent.DeadEntity;

                    // Check if deceased entity is a dynasty member
                    if (!DynastyMemberLookup.HasComponent(deadEntity))
                        continue;

                    var dynastyMember = DynastyMemberLookup[deadEntity];
                    Entity dynastyEntity = dynastyMember.DynastyEntity;

                    if (dynastyEntity == Entity.Null)
                        continue;

                    // Remove DynastyMember component
                    Ecb.RemoveComponent<DynastyMember>(index, deadEntity);

                    // Remove from dynasty member buffer
                    if (DynastyMemberEntryLookup.HasBuffer(dynastyEntity))
                    {
                        var members = DynastyMemberEntryLookup[dynastyEntity];
                        for (int j = members.Length - 1; j >= 0; j--)
                        {
                            if (members[j].MemberEntity == deadEntity)
                            {
                                members.RemoveAt(j);
                                break;
                            }
                        }
                    }

                    // If founder/leader died, mark for succession (handled by DynastySuccessionSystem)
                }
            }
        }

        [BurstCompile]
        partial struct ProcessBirthEventsJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            public uint CurrentTick;
            [ReadOnly]
            public ComponentLookup<DynastyMember> DynastyMemberLookup;
            [ReadOnly]
            public BufferLookup<DynastyLineage> DynastyLineageLookup;
            [NativeDisableParallelForRestriction]
            public BufferLookup<EntityRelation> EntityRelationLookup;

            void Execute(
                Entity entity,
                in LifecycleState lifecycle)
            {
                // Check if this entity was just born (BirthTick matches current tick)
                if (lifecycle.BirthTick != CurrentTick)
                    return;

                // IMPORTANT: This system requires that game-side systems (genealogy/lifecycle) create
                // Parent EntityRelation entries on the newborn entity BEFORE this handler runs.
                // If parent relations don't exist, the entity will not be added to any dynasty.
                // Game-side systems should create bidirectional Parent/Child relations when entities are born.

                // Find parents via EntityRelation buffers
                Entity parentA = Entity.Null;
                Entity parentB = Entity.Null;
                byte generation = 0;

                if (EntityRelationLookup.HasBuffer(entity))
                {
                    var relations = EntityRelationLookup[entity];
                    for (int i = 0; i < relations.Length; i++)
                    {
                        if (relations[i].Type == RelationType.Parent)
                        {
                            if (parentA == Entity.Null)
                                parentA = relations[i].OtherEntity;
                            else if (parentB == Entity.Null)
                                parentB = relations[i].OtherEntity;
                        }
                    }
                }

                // If no parents found, entity cannot be added to dynasty
                // This is expected if game-side systems haven't created parent relations yet
                if (parentA == Entity.Null && parentB == Entity.Null)
                {
                    return;
                }

                // Fallback: Ensure bidirectional Parent/Child relations exist
                if (parentA != Entity.Null)
                {
                    EnsureBidirectionalRelation(parentA, entity);
                }
                if (parentB != Entity.Null)
                {
                    EnsureBidirectionalRelation(parentB, entity);
                }

                // Calculate generation from parent lineage
                if (parentA != Entity.Null && DynastyMemberLookup.HasComponent(parentA))
                {
                    var parentDynasty = DynastyMemberLookup[parentA];
                    if (DynastyLineageLookup.HasBuffer(parentDynasty.DynastyEntity))
                    {
                        var lineage = DynastyLineageLookup[parentDynasty.DynastyEntity];
                        for (int i = 0; i < lineage.Length; i++)
                        {
                            if (lineage[i].MemberEntity == parentA)
                            {
                                generation = (byte)(lineage[i].Generation + 1);
                                break;
                            }
                        }
                    }
                }

                // If at least one parent is in a dynasty, add child to that dynasty
                if (parentA != Entity.Null && DynastyMemberLookup.HasComponent(parentA))
                {
                    var parentDynasty = DynastyMemberLookup[parentA];
                    float lineageStrength = DynastyService.CalculateLineageStrength(
                        generation,
                        parentA != Entity.Null && DynastyMemberLookup.HasComponent(parentA) ? DynastyMemberLookup[parentA].LineageStrength : 0f,
                        parentB != Entity.Null && DynastyMemberLookup.HasComponent(parentB) ? DynastyMemberLookup[parentB].LineageStrength : 0f);

                    DynastyService.TrackLineage(
                        ref Ecb,
                        parentDynasty.DynastyEntity,
                        entity,
                        parentA,
                        parentB,
                        CurrentTick,
                        generation);

                    DynastyService.AddMember(
                        ref Ecb,
                        parentDynasty.DynastyEntity,
                        entity,
                        DynastyRank.Member,
                        lineageStrength,
                        CurrentTick);
                }
                else if (parentB != Entity.Null && DynastyMemberLookup.HasComponent(parentB))
                {
                    var parentDynasty = DynastyMemberLookup[parentB];
                    float lineageStrength = DynastyService.CalculateLineageStrength(
                        generation,
                        parentA != Entity.Null && DynastyMemberLookup.HasComponent(parentA) ? DynastyMemberLookup[parentA].LineageStrength : 0f,
                        parentB != Entity.Null && DynastyMemberLookup.HasComponent(parentB) ? DynastyMemberLookup[parentB].LineageStrength : 0f);

                    DynastyService.TrackLineage(
                        ref Ecb,
                        parentDynasty.DynastyEntity,
                        entity,
                        parentA,
                        parentB,
                        CurrentTick,
                        generation);

                    DynastyService.AddMember(
                        ref Ecb,
                        parentDynasty.DynastyEntity,
                        entity,
                        DynastyRank.Member,
                        lineageStrength,
                        CurrentTick);
                }
            }

            void EnsureBidirectionalRelation(Entity parent, Entity child)
            {
                // Ensure buffers exist - add via ECB if missing (will be available next frame)
                bool childBufferExists = EntityRelationLookup.HasBuffer(child);
                bool parentBufferExists = EntityRelationLookup.HasBuffer(parent);
                
                if (!childBufferExists)
                {
                    Ecb.AddBuffer<EntityRelation>(child);
                }
                if (!parentBufferExists)
                {
                    Ecb.AddBuffer<EntityRelation>(parent);
                }

                // If buffers don't exist yet, relations will be created next frame when buffers are available
                // This is acceptable since birth handlers run every frame
                if (!childBufferExists || !parentBufferExists)
                {
                    return;
                }

                // Check if relations already exist
                var childRelations = EntityRelationLookup[child];
                bool hasParentRelation = false;
                for (int i = 0; i < childRelations.Length; i++)
                {
                    if (childRelations[i].OtherEntity == parent && childRelations[i].Type == RelationType.Parent)
                    {
                        hasParentRelation = true;
                        break;
                    }
                }

                var parentRelations = EntityRelationLookup[parent];
                bool hasChildRelation = false;
                for (int i = 0; i < parentRelations.Length; i++)
                {
                    if (parentRelations[i].OtherEntity == child && parentRelations[i].Type == RelationType.Child)
                    {
                        hasChildRelation = true;
                        break;
                    }
                }

                // Create missing relations (single-threaded job allows direct buffer modification)
                if (!hasParentRelation)
                {
                    var childBuffer = EntityRelationLookup[child];
                    childBuffer.Add(new EntityRelation
                    {
                        OtherEntity = parent,
                        Type = RelationType.Parent,
                        Intensity = 80, // High positive for parent-child
                        InteractionCount = 0,
                        FirstMetTick = CurrentTick,
                        LastInteractionTick = CurrentTick,
                        Trust = 90,
                        Familiarity = 100,
                        Respect = 70,
                        Fear = 0
                    });
                }

                if (!hasChildRelation)
                {
                    var parentBuffer = EntityRelationLookup[parent];
                    parentBuffer.Add(new EntityRelation
                    {
                        OtherEntity = child,
                        Type = RelationType.Child,
                        Intensity = 80, // High positive for parent-child
                        InteractionCount = 0,
                        FirstMetTick = CurrentTick,
                        LastInteractionTick = CurrentTick,
                        Trust = 90,
                        Familiarity = 100,
                        Respect = 50,
                        Fear = 0
                    });
                }
            }
        }

        [BurstCompile]
        partial struct TrackLineageJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public uint CurrentTick;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int index,
                in DynastyIdentity identity,
                ref DynamicBuffer<DynastyLineage> lineage)
            {
                // Track new births in the dynasty
                // Calculate generation based on parent lineage
                // Update lineage strength for new members
                // Birth events are now handled by ProcessBirthEventsJob
            }
        }
    }

    /// <summary>
    /// System that updates dynasty reputation and prestige.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DynastyReputationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            foreach (var (prestige, wealth, members, entity) in SystemAPI
                .Query<RefRW<DynastyPrestige>, RefRW<PureDOTS.Runtime.Dynasty.DynastyWealth>, DynamicBuffer<DynastyMemberEntry>>()
                .WithEntityAccess())
            {
                float totalWealth = wealth.ValueRO.TotalWealth;
                float averageReputation = prestige.ValueRO.DynastyReputation;
                DynastyService.UpdateDynastyReputation(
                    entity,
                    ref prestige.ValueRW,
                    members,
                    totalWealth,
                    averageReputation,
                    timeState.Tick);
            }
        }
    }

    /// <summary>
    /// System that aggregates dynasty wealth from members.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DynastyWealthAggregationSystem : ISystem
    {
        private ComponentLookup<PureDOTS.Runtime.Economy.Wealth.VillagerWealth> _villagerWealthLookup;
        private ComponentLookup<PureDOTS.Runtime.Economy.Wealth.DynastyWealth> _dynastyWalletLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _villagerWealthLookup = state.GetComponentLookup<PureDOTS.Runtime.Economy.Wealth.VillagerWealth>(true);
            _dynastyWalletLookup = state.GetComponentLookup<PureDOTS.Runtime.Economy.Wealth.DynastyWealth>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            _villagerWealthLookup.Update(ref state);
            _dynastyWalletLookup.Update(ref state);

            foreach (var (wealth, members, entity) in SystemAPI
                .Query<RefRW<PureDOTS.Runtime.Dynasty.DynastyWealth>, DynamicBuffer<DynastyMemberEntry>>()
                .WithEntityAccess())
            {
                float totalWealth = 0f;
                int activeMemberCount = 0;

                for (int i = 0; i < members.Length; i++)
                {
                    var memberEntity = members[i].MemberEntity;
                    if (memberEntity != Entity.Null && _villagerWealthLookup.HasComponent(memberEntity))
                    {
                        var memberWealth = _villagerWealthLookup[memberEntity];
                        totalWealth += memberWealth.Balance;
                        activeMemberCount++;
                    }
                }

                float sharedWealth = 0f;
                if (_dynastyWalletLookup.HasComponent(entity))
                {
                    sharedWealth = _dynastyWalletLookup[entity].Balance;
                }

                wealth.ValueRW.TotalWealth = totalWealth;
                wealth.ValueRW.SharedWealth = sharedWealth;
                wealth.ValueRW.AverageWealth = activeMemberCount > 0 ? totalWealth / activeMemberCount : 0f;
                wealth.ValueRW.LastUpdatedTick = timeState.Tick;
            }
        }
    }
}
