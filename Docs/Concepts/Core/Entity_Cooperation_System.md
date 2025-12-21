# Entity Cooperation System

## Overview

Comprehensive cooperation system where entities pool resources, coordinate actions, and support each other across multiple domains. Magic users channel mana to powerful allies, archers coordinate volleys, researchers collaborate on discoveries, mechanics care for their pilots, and band members develop shared sign languages. Cooperation efficiency depends on skills, cohesion, relations, and communication clarity.

**Key Principles**:
- **Resource pooling**: Mana, firepower, knowledge, labor shared between cooperators
- **Skill synergy**: Combined skills produce better results than individuals
- **Cohesion-dependent**: Quality scales with group cohesion (like formations)
- **Multi-layered**: Cooperation happens at different organizational levels
- **Care relationships**: Entities care about cooperators' wellbeing (mutual support)
- **Communication-based**: Shared languages, sign systems, protocols enable coordination
- **Cross-game**: Magic circles (Godgame), flight crews (Space4X), both use same framework
- **Deterministic**: Same cooperators + same task = same outcome

---

## Core Cooperation Components

### Cooperation Definition

```csharp
public struct Cooperation : IComponentData
{
    public Entity Coordinator;              // Who organizes the cooperation
    public CooperationType Type;
    public CooperationPhase Phase;
    public float Cohesion;                  // 0.0 to 1.0 (coordination quality)
    public float EfficiencyBonus;           // Combined bonus from cooperation
    public uint ParticipantCount;
    public uint TargetParticipantCount;
    public uint StartedTick;
    public bool IsActive;
}

public enum CooperationType : byte
{
    // Magic cooperation
    ManaPooling = 0,            // Pool mana for powerful caster
    RitualCasting = 1,          // Coordinated ritual magic
    SpellAmplification = 2,     // Amplify another's spell

    // Combat cooperation
    CoordinatedVolley = 10,     // Archers/guns fire together
    FocusFire = 11,             // Concentrate on single target
    SuppressiveFire = 12,       // Pin down enemies
    CoveringFire = 13,          // Protect advancing allies
    CrossfireSetup = 14,        // Create kill zone

    // Formation cooperation (already covered)
    FormationMaintenance = 20,  // Phalanx, shield wall, etc.

    // Production cooperation
    CollaborativeResearch = 30, // Multiple researchers
    CollaborativeCrafting = 31, // Multiple craftsmen
    AssemblyLine = 32,          // Organized production
    QualityControl = 33,        // Inspection and refinement

    // Facility cooperation
    CrewCoordination = 40,      // Ship/facility crew
    OperatorPilotLink = 41,     // Operator guides pilot
    HangarOperations = 42,      // Hangar crew coordination
    BridgeOfficers = 43,        // Bridge crew coordination

    // Social cooperation
    MusicEnsemble = 50,         // Band/orchestra
    SharedLanguage = 51,        // Develop custom signs/language
    CompanionSupport = 52,      // Companions helping each other

    // Support cooperation
    MutualCare = 60,            // Care for each other's wellbeing
    ResourceSharing = 61,       // Share food, supplies
    EmotionalSupport = 62,      // Morale and comfort
    ProtectiveWatch = 63        // Guard each other
}

public enum CooperationPhase : byte
{
    Forming = 0,                // Gathering participants
    Coordinating = 1,           // Establishing coordination
    Active = 2,                 // Actively cooperating
    Degrading = 3,              // Cohesion failing
    Dissolved = 4               // Cooperation ended
}

[InternalBufferCapacity(32)]
public struct CooperationParticipant : IBufferElementData
{
    public Entity ParticipantEntity;
    public CooperationRole Role;
    public float SkillLevel;                // Relevant skill for this cooperation
    public float ContributionAmount;        // How much they contribute
    public float CohesionContribution;      // How much they add to cohesion
    public bool IsActive;
}

public enum CooperationRole : byte
{
    Coordinator = 0,        // Organizes the cooperation
    Primary = 1,            // Main contributor
    Secondary = 2,          // Supporting contributor
    Support = 3,            // Auxiliary support
    Observer = 4            // Learning by watching
}
```

### Cooperation Cohesion

```csharp
public struct CooperationCohesion : IComponentData
{
    public float CurrentCohesion;           // 0.0 to 1.0
    public float BaseCohesion;              // From skills and relations
    public float CohesionDecayRate;         // Loss per second
    public float CohesionRegenRate;         // Recovery rate

    // Cohesion modifiers
    public float SkillSynergyBonus;         // Bonus from complementary skills
    public float RelationBonus;             // Bonus from good relations
    public float CommunicationClarity;      // Bonus from clear communication
    public float ExperienceBonus;           // Bonus from working together before

    // Efficiency scaling
    public float EfficiencyMultiplier;      // How much cohesion boosts output
}

[BurstCompile]
public partial struct CooperationCohesionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (cohesion, cooperation, participants) in SystemAPI.Query<
            RefRW<CooperationCohesion>,
            RefRO<Cooperation>,
            DynamicBuffer<CooperationParticipant>>())
        {
            // Calculate skill synergy
            float skillSynergy = CalculateSkillSynergy(participants);
            cohesion.ValueRW.SkillSynergyBonus = skillSynergy;

            // Calculate relation bonus
            float relationBonus = CalculateRelationBonus(participants);
            cohesion.ValueRW.RelationBonus = relationBonus;

            // Calculate communication clarity
            float commClarity = CalculateCommunicationClarity(participants);
            cohesion.ValueRW.CommunicationClarity = commClarity;

            // Base cohesion formula
            cohesion.ValueRW.BaseCohesion = math.clamp(
                (skillSynergy * 0.4f) +         // 40% from skills
                (relationBonus * 0.3f) +        // 30% from relations
                (commClarity * 0.2f) +          // 20% from communication
                (cohesion.ValueRO.ExperienceBonus * 0.1f), // 10% from experience
                0f,
                1f
            );

            // Apply to current cohesion
            if (cooperation.ValueRO.Phase == CooperationPhase.Active)
            {
                // Maintain cohesion
                cohesion.ValueRW.CurrentCohesion = math.lerp(
                    cohesion.ValueRO.CurrentCohesion,
                    cohesion.ValueRO.BaseCohesion,
                    SystemAPI.Time.DeltaTime * cohesion.ValueRO.CohesionRegenRate
                );
            }

            // Calculate efficiency multiplier
            cohesion.ValueRW.EfficiencyMultiplier = CalculateEfficiencyMultiplier(
                cohesion.ValueRO.CurrentCohesion,
                cooperation.ValueRO.Type
            );
        }
    }

    private float CalculateSkillSynergy(DynamicBuffer<CooperationParticipant> participants)
    {
        // Skills complement each other when diverse but high quality
        float avgSkill = 0f;
        float skillVariance = 0f;
        int count = 0;

        foreach (var participant in participants)
        {
            if (participant.IsActive)
            {
                avgSkill += participant.SkillLevel;
                count++;
            }
        }

        avgSkill /= math.max(1, count);

        // Calculate variance (diverse skills = higher synergy)
        foreach (var participant in participants)
        {
            if (participant.IsActive)
            {
                float diff = participant.SkillLevel - avgSkill;
                skillVariance += diff * diff;
            }
        }

        skillVariance /= math.max(1, count);

        // Synergy = high average + moderate variance
        float synergyScore = avgSkill * (1f + (skillVariance * 0.5f));
        return math.clamp(synergyScore, 0f, 1f);
    }

    private float CalculateRelationBonus(DynamicBuffer<CooperationParticipant> participants)
    {
        // Average relations between all participants
        float totalRelations = 0f;
        int relationCount = 0;

        for (int i = 0; i < participants.Length; i++)
        {
            if (!participants[i].IsActive) continue;

            for (int j = i + 1; j < participants.Length; j++)
            {
                if (!participants[j].IsActive) continue;

                float relation = GetRelationValue(
                    participants[i].ParticipantEntity,
                    participants[j].ParticipantEntity
                );

                totalRelations += relation; // -1.0 to 1.0
                relationCount++;
            }
        }

        float avgRelation = relationCount > 0 ? totalRelations / relationCount : 0f;
        return math.clamp((avgRelation + 1f) / 2f, 0f, 1f); // Normalize to 0-1
    }

    private float CalculateEfficiencyMultiplier(float cohesion, CooperationType type)
    {
        // Different cooperation types scale differently with cohesion
        float baseMultiplier = type switch
        {
            CooperationType.ManaPooling => 1f + (cohesion * 1.0f),         // Up to 2× efficiency
            CooperationType.RitualCasting => 1f + (cohesion * 2.0f),       // Up to 3× efficiency
            CooperationType.CoordinatedVolley => 1f + (cohesion * 0.8f),   // Up to 1.8× efficiency
            CooperationType.CollaborativeResearch => 1f + (cohesion * 1.5f), // Up to 2.5× efficiency
            CooperationType.CollaborativeCrafting => 1f + (cohesion * 1.2f), // Up to 2.2× efficiency
            CooperationType.CrewCoordination => 1f + (cohesion * 1.0f),    // Up to 2× efficiency
            CooperationType.MutualCare => 1f + (cohesion * 0.5f),          // Up to 1.5× efficiency
            _ => 1f + (cohesion * 0.5f)
        };

        return baseMultiplier;
    }
}
```

---

## Magic Cooperation

### Mana Pooling

Multiple magic users channel mana to a more powerful caster:

```csharp
public struct ManaPooling : IComponentData
{
    public Entity PrimaryCaster;            // Receives pooled mana
    public float PooledMana;                // Total mana available
    public float CastSpeedBonus;            // Faster casting with more mana
    public float EfficiencyBonus;           // Less mana waste
    public uint ContributorCount;
}

[InternalBufferCapacity(8)]
public struct ManaContributor : IBufferElementData
{
    public Entity ContributorEntity;
    public float ManaContributionRate;      // Per second
    public float ChannelingEfficiency;      // 0.0 to 1.0 (skill-based)
    public bool IsChanneling;
}

[BurstCompile]
public partial struct ManaPoolingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (pooling, contributors, cohesion) in SystemAPI.Query<
            RefRW<ManaPooling>,
            DynamicBuffer<ManaContributor>,
            RefRO<CooperationCohesion>>())
        {
            float totalManaThisFrame = 0f;
            int activeContributors = 0;

            // Collect mana from all contributors
            for (int i = 0; i < contributors.Length; i++)
            {
                if (!contributors[i].IsChanneling) continue;

                // Get contributor's mana pool
                var contributorMana = GetManaComponent(contributors[i].ContributorEntity);

                if (contributorMana.CurrentMana > 0f)
                {
                    // Calculate contribution this frame
                    float contribution = contributors[i].ManaContributionRate * deltaTime;
                    contribution = math.min(contribution, contributorMana.CurrentMana);

                    // Apply channeling efficiency
                    float efficiency = contributors[i].ChannelingEfficiency;
                    float effectiveMana = contribution * efficiency;

                    // Apply cohesion bonus
                    effectiveMana *= cohesion.ValueRO.EfficiencyMultiplier;

                    // Deduct from contributor
                    DeductMana(contributors[i].ContributorEntity, contribution);

                    totalManaThisFrame += effectiveMana;
                    activeContributors++;
                }
            }

            // Add pooled mana
            pooling.ValueRW.PooledMana += totalManaThisFrame;
            pooling.ValueRW.ContributorCount = (uint)activeContributors;

            // Calculate bonuses
            // Cast speed increases with more contributors (diminishing returns)
            pooling.ValueRW.CastSpeedBonus = math.sqrt(activeContributors) * 0.3f; // Up to +30% per contributor

            // Efficiency bonus from cohesion
            pooling.ValueRW.EfficiencyBonus = cohesion.ValueRO.CurrentCohesion * 0.5f; // Up to +50%
        }
    }
}
```

### Ritual Casting

Coordinated ritual magic requiring precise synchronization:

```csharp
public struct RitualCasting : IComponentData
{
    public FixedString64Bytes RitualName;
    public RitualPhase CurrentPhase;
    public float SynchronizationLevel;      // 0.0 to 1.0 (how in-sync)
    public float RitualPower;               // Total power of ritual
    public float TimeInPhase;
    public float RequiredCohesion;          // Minimum cohesion to succeed
}

public enum RitualPhase : byte
{
    Preparation = 0,        // Setting up circle, gathering mana
    Invocation = 1,         // Synchronized chanting/casting
    Channeling = 2,         // Maintaining power flow
    Climax = 3,             // Peak power moment
    Completion = 4,         // Ritual succeeds
    Failure = 5             // Ritual fails (cohesion lost)
}

[BurstCompile]
public partial struct RitualCastingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (ritual, cohesion, participants) in SystemAPI.Query<
            RefRW<RitualCasting>,
            RefRO<CooperationCohesion>,
            DynamicBuffer<CooperationParticipant>>())
        {
            // Check cohesion requirement
            if (cohesion.ValueRO.CurrentCohesion < ritual.ValueRO.RequiredCohesion)
            {
                // Cohesion too low, ritual fails
                ritual.ValueRW.CurrentPhase = RitualPhase.Failure;
                ApplyRitualBacklash(participants); // Dangerous to fail
                continue;
            }

            // Calculate synchronization from cohesion
            ritual.ValueRW.SynchronizationLevel = cohesion.ValueRO.CurrentCohesion;

            // Calculate ritual power
            float totalPower = 0f;
            foreach (var participant in participants)
            {
                if (participant.IsActive)
                {
                    totalPower += participant.ContributionAmount;
                }
            }

            // Apply cohesion multiplier (rituals benefit greatly from cohesion)
            ritual.ValueRW.RitualPower = totalPower * cohesion.ValueRO.EfficiencyMultiplier;

            // Advance ritual phase
            ritual.ValueRW.TimeInPhase += SystemAPI.Time.DeltaTime;
            UpdateRitualPhase(ref ritual.ValueRW, ritual.ValueRO.TimeInPhase);
        }
    }
}
```

---

## Combat Cooperation

### Coordinated Volley

Archers/bombardiers fire together for devastating effect:

```csharp
public struct CoordinatedVolley : IComponentData
{
    public Entity VolleyCommander;
    public float3 TargetPosition;
    public Entity TargetEntity;
    public float ChargeProgress;            // 0.0 to 1.0 (readying shots)
    public float VolleyPowerMultiplier;     // Bonus from coordination
    public uint ReadyCount;                 // Shooters ready to fire
    public uint TotalShooters;
    public bool FireOnCommand;
}

[InternalBufferCapacity(16)]
public struct VolleyShooter : IBufferElementData
{
    public Entity ShooterEntity;
    public bool IsReady;                    // Ready to fire
    public float AccuracyBonus;             // From volley coordination
    public float ReloadTime;
}

[BurstCompile]
public partial struct CoordinatedVolleySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (volley, shooters, cohesion) in SystemAPI.Query<
            RefRW<CoordinatedVolley>,
            DynamicBuffer<VolleyShooter>,
            RefRO<CooperationCohesion>>())
        {
            // Count ready shooters
            int readyCount = 0;
            foreach (var shooter in shooters)
            {
                if (shooter.IsReady) readyCount++;
            }

            volley.ValueRW.ReadyCount = (uint)readyCount;
            volley.ValueRW.TotalShooters = (uint)shooters.Length;

            // Calculate charge progress (how many are ready)
            volley.ValueRW.ChargeProgress = (float)readyCount / math.max(1, shooters.Length);

            // Calculate volley power multiplier from cohesion
            // High cohesion = simultaneous fire = devastating impact
            float simultaneityBonus = cohesion.ValueRO.CurrentCohesion * 0.5f; // Up to +50%
            float volumeBonus = math.sqrt(readyCount) * 0.2f; // Diminishing returns

            volley.ValueRW.VolleyPowerMultiplier = 1f + simultaneityBonus + volumeBonus;

            // Apply accuracy bonus to all shooters
            for (int i = 0; i < shooters.Length; i++)
            {
                var shooter = shooters[i];
                // Coordinated fire is more accurate
                shooter.AccuracyBonus = cohesion.ValueRO.CurrentCohesion * 0.3f; // Up to +30%
                shooters[i] = shooter;
            }

            // Fire on command
            if (volley.ValueRO.FireOnCommand && volley.ValueRO.ChargeProgress > 0.7f)
            {
                ExecuteVolley(volley.ValueRO, shooters);
            }
        }
    }

    private void ExecuteVolley(in CoordinatedVolley volley, DynamicBuffer<VolleyShooter> shooters)
    {
        // All ready shooters fire simultaneously
        foreach (var shooter in shooters)
        {
            if (shooter.IsReady)
            {
                FireProjectile(
                    shooter.ShooterEntity,
                    volley.TargetPosition,
                    volley.VolleyPowerMultiplier,
                    shooter.AccuracyBonus
                );
            }
        }
    }
}
```

---

## Production Cooperation

### Collaborative Research

Researchers working together on discoveries:

```csharp
public struct CollaborativeResearch : IComponentData
{
    public FixedString64Bytes ResearchTopic;
    public float ResearchProgress;          // 0.0 to 1.0
    public float ResearchSpeed;             // Progress per hour
    public float DiscoveryQuality;          // Quality of final result
    public uint ResearcherCount;
}

[InternalBufferCapacity(8)]
public struct Researcher : IBufferElementData
{
    public Entity ResearcherEntity;
    public float ResearchSkill;             // Intelligence + knowledge
    public float ContributionRate;          // Per hour
    public float SpecializationMatch;       // How well skills match topic
    public bool IsActivelyResearching;
}

[BurstCompile]
public partial struct CollaborativeResearchSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (research, researchers, cohesion) in SystemAPI.Query<
            RefRW<CollaborativeResearch>,
            DynamicBuffer<Researcher>,
            RefRO<CooperationCohesion>>())
        {
            float totalContribution = 0f;
            float avgSkill = 0f;
            int activeCount = 0;

            // Calculate combined research speed
            foreach (var researcher in researchers)
            {
                if (!researcher.IsActivelyResearching) continue;

                // Contribution = skill × specialization match
                float contribution = researcher.ResearchSkill * researcher.SpecializationMatch;
                totalContribution += contribution;
                avgSkill += researcher.ResearchSkill;
                activeCount++;
            }

            research.ValueRW.ResearcherCount = (uint)activeCount;

            if (activeCount > 0)
            {
                avgSkill /= activeCount;

                // Research speed formula:
                // BaseSpeed = TotalContribution
                // Cohesion multiplies effectiveness (collaboration vs duplication)
                float collaborationBonus = cohesion.ValueRO.EfficiencyMultiplier;

                // Diminishing returns with more researchers (communication overhead)
                float scalingFactor = math.sqrt(activeCount) / activeCount; // Penalty for large teams

                research.ValueRW.ResearchSpeed = totalContribution * collaborationBonus * scalingFactor;

                // Quality depends on cohesion and average skill
                // High cohesion + high skill = breakthrough discoveries
                // Low cohesion = conflicting theories, wasted effort
                research.ValueRW.DiscoveryQuality = (avgSkill * 0.6f) + (cohesion.ValueRO.CurrentCohesion * 0.4f);

                // Apply progress
                research.ValueRW.ResearchProgress += research.ValueRO.ResearchSpeed * deltaTime;
            }
        }
    }
}
```

### Collaborative Crafting

Multiple craftsmen working on item creation:

```csharp
public struct CollaborativeCrafting : IComponentData
{
    public FixedString64Bytes ItemName;
    public CraftingPhase CurrentPhase;
    public float CraftProgress;             // 0.0 to 1.0
    public float FinalQuality;              // 0.0 to 1.0 (or higher for masterwork)
    public uint CraftsmanCount;
}

public enum CraftingPhase : byte
{
    Planning = 0,           // Designing the item
    MaterialPrep = 1,       // Preparing materials
    Assembly = 2,           // Putting it together
    Refinement = 3,         // Fine details
    QualityControl = 4,     // Inspection and finishing
    Completed = 5
}

[InternalBufferCapacity(4)]
public struct Craftsman : IBufferElementData
{
    public Entity CraftsmanEntity;
    public float CraftingSkill;
    public float SpecializationBonus;       // Bonus for this item type
    public float QualityContribution;       // Impact on final quality
    public bool IsWorking;
}

[BurstCompile]
public partial struct CollaborativeCraftingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (crafting, craftsmen, cohesion) in SystemAPI.Query<
            RefRW<CollaborativeCrafting>,
            DynamicBuffer<Craftsman>,
            RefRO<CooperationCohesion>>())
        {
            float totalSkill = 0f;
            float minSkill = 1f;
            float maxSkill = 0f;
            int activeCount = 0;

            // Analyze craftsman skills
            foreach (var craftsman in craftsmen)
            {
                if (!craftsman.IsWorking) continue;

                float effectiveSkill = craftsman.CraftingSkill * (1f + craftsman.SpecializationBonus);
                totalSkill += effectiveSkill;
                minSkill = math.min(minSkill, effectiveSkill);
                maxSkill = math.max(maxSkill, effectiveSkill);
                activeCount++;
            }

            crafting.ValueRW.CraftsmanCount = (uint)activeCount;

            if (activeCount > 0)
            {
                float avgSkill = totalSkill / activeCount;

                // Quality calculation:
                // High cohesion = skills complement each other
                // Low cohesion = conflicting approaches, inconsistent quality

                if (cohesion.ValueRO.CurrentCohesion > 0.7f)
                {
                    // High cohesion: quality approaches max skill
                    crafting.ValueRW.FinalQuality = math.lerp(avgSkill, maxSkill, cohesion.ValueRO.CurrentCohesion);
                }
                else if (cohesion.ValueRO.CurrentCohesion > 0.4f)
                {
                    // Medium cohesion: quality is average
                    crafting.ValueRW.FinalQuality = avgSkill * cohesion.ValueRO.EfficiencyMultiplier;
                }
                else
                {
                    // Low cohesion: quality approaches min skill (weakest link)
                    crafting.ValueRW.FinalQuality = math.lerp(minSkill, avgSkill, cohesion.ValueRO.CurrentCohesion);
                }

                // Progress speed
                float craftSpeed = totalSkill * cohesion.ValueRO.EfficiencyMultiplier;
                crafting.ValueRW.CraftProgress += craftSpeed * SystemAPI.Time.DeltaTime;
            }
        }
    }
}
```

---

## Facility and Crew Cooperation

### Operator-Pilot Link

Operators guide pilots using sensors and communications:

```csharp
public struct OperatorPilotLink : IComponentData
{
    public Entity Operator;
    public Entity Pilot;
    public float LinkQuality;               // Communication clarity
    public float SensorDataQuality;         // How good sensor info is
    public float GuidanceBonus;             // Bonus to pilot from operator

    // Operator contributions
    public float ThreatAwarenessBonus;      // Operator spots threats
    public float NavigationBonus;           // Operator provides routes
    public float TargetingBonus;            // Operator locks targets
}

[BurstCompile]
public partial struct OperatorPilotLinkSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (link, cohesion) in SystemAPI.Query<
            RefRW<OperatorPilotLink>,
            RefRO<CooperationCohesion>>())
        {
            // Link quality based on cohesion and communication
            link.ValueRW.LinkQuality = cohesion.ValueRO.CurrentCohesion *
                                       cohesion.ValueRO.CommunicationClarity;

            // Operator provides bonuses based on their sensor skill and link quality
            var operatorSkills = GetEntitySkills(link.ValueRO.Operator);

            float sensorSkill = operatorSkills.SensorOperationSkill;
            float tacticSkill = operatorSkills.TacticalAnalysisSkill;

            // Bonuses scale with link quality
            link.ValueRW.ThreatAwarenessBonus = sensorSkill * link.ValueRO.LinkQuality * 0.4f;
            link.ValueRW.NavigationBonus = tacticSkill * link.ValueRO.LinkQuality * 0.3f;
            link.ValueRW.TargetingBonus = (sensorSkill + tacticSkill) * 0.5f * link.ValueRO.LinkQuality * 0.5f;

            // Apply bonuses to pilot
            ApplyOperatorBonuses(link.ValueRO.Pilot, link.ValueRO);
        }
    }
}
```

### Hangar Crew Operations

Coordinated hangar operations for rapid deployment:

```csharp
public struct HangarOperations : IComponentData
{
    public Entity HangarBay;
    public float OperationalEfficiency;     // 0.0 to 1.0
    public float DeploymentSpeed;           // Seconds to deploy unit
    public float MaintenanceQuality;        // Quality of repairs/prep
    public uint CrewMemberCount;
}

[InternalBufferCapacity(12)]
public struct HangarCrewMember : IBufferElementData
{
    public Entity CrewEntity;
    public HangarRole Role;
    public float RoleSkill;
    public float Fatigue;                   // 0.0 to 1.0 (affects performance)
    public bool IsOnDuty;
}

public enum HangarRole : byte
{
    DeckChief = 0,          // Coordinates operations
    Mechanic = 1,           // Repairs and maintenance
    Armorer = 2,            // Weapons loading
    Refueler = 3,           // Fuel/energy replenishment
    Launcher = 4,           // Launch operations
    Recovery = 5,           // Recovery operations
    Inspector = 6           // Quality control
}

[BurstCompile]
public partial struct HangarOperationsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (hangar, crew, cohesion) in SystemAPI.Query<
            RefRW<HangarOperations>,
            DynamicBuffer<HangarCrewMember>,
            RefRO<CooperationCohesion>>())
        {
            // Check crew composition (need all roles for optimal operation)
            var rolesFilled = CountFilledRoles(crew);
            float compositionScore = (float)rolesFilled / 7f; // 7 roles total

            // Calculate efficiency from cohesion, composition, and fatigue
            float avgFatigue = CalculateAverageFatigue(crew);
            float fatigueModifier = 1f - (avgFatigue * 0.5f); // Fatigue reduces efficiency up to 50%

            hangar.ValueRW.OperationalEfficiency = cohesion.ValueRO.CurrentCohesion *
                                                   compositionScore *
                                                   fatigueModifier;

            // Deployment speed (faster with higher efficiency)
            float baseDeploymentTime = 60f; // 60 seconds base
            hangar.ValueRW.DeploymentSpeed = baseDeploymentTime / (1f + hangar.ValueRO.OperationalEfficiency);

            // Maintenance quality (affects deployed unit condition)
            float mechanicSkill = GetRoleSkillAverage(crew, HangarRole.Mechanic);
            float inspectorSkill = GetRoleSkillAverage(crew, HangarRole.Inspector);

            hangar.ValueRW.MaintenanceQuality = ((mechanicSkill + inspectorSkill) * 0.5f) *
                                                cohesion.ValueRO.CurrentCohesion;
        }
    }

    private int CountFilledRoles(DynamicBuffer<HangarCrewMember> crew)
    {
        bool[] rolesFilled = new bool[7];
        foreach (var member in crew)
        {
            if (member.IsOnDuty)
            {
                rolesFilled[(int)member.Role] = true;
            }
        }

        int count = 0;
        for (int i = 0; i < 7; i++)
        {
            if (rolesFilled[i]) count++;
        }
        return count;
    }
}
```

---

## Mutual Care and Support

### Care Relationships

Entities care about each other's wellbeing, creating support networks:

```csharp
public struct CareRelationship : IComponentData
{
    public Entity CareGiver;
    public Entity CareReceiver;
    public float CareLevel;                 // 0.0 to 1.0 (how much they care)
    public CareType Type;
    public float SupportProvided;           // Tangible support given
}

public enum CareType : byte
{
    Mutual = 0,             // Both care for each other
    Protective = 1,         // One protects the other
    Mentorship = 2,         // Teacher-student care
    Professional = 3,       // Crew caring for crew
    Familial = 4,           // Family bonds
    Companionship = 5       // Friends/companions
}

[InternalBufferCapacity(8)]
public struct CareAction : IBufferElementData
{
    public CareActionType Action;
    public Entity Target;
    public float Magnitude;
    public uint PerformedTick;
}

public enum CareActionType : byte
{
    // Physical care
    ProvideFood = 0,
    ProvideShelter = 1,
    ProvideHealing = 2,
    ShareResources = 3,

    // Emotional care
    OfferComfort = 10,
    BoostMorale = 11,
    ListenToProblems = 12,
    ProvideEncouragement = 13,

    // Professional care
    CheckEquipment = 20,        // Mechanic checking pilot's mech
    EnsureRest = 21,            // Pilot ensuring mechanic sleeps
    ShareKnowledge = 22,        // Teaching
    ProtectFromDanger = 23      // Bodyguarding
}

public struct MutualCareBond : IComponentData
{
    public Entity EntityA;
    public Entity EntityB;
    public float BondStrength;              // 0.0 to 1.0
    public float MutualityScore;            // How balanced the care is

    // Care metrics
    public float EntityACareForB;
    public float EntityBCareForA;

    // Effects
    public float MoraleBonus;               // Mutual care boosts morale
    public float StressReduction;           // Care reduces stress
    public float PerformanceBonus;          // Caring relationships improve performance
}

[BurstCompile]
public partial struct MutualCareSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (bond, cohesion) in SystemAPI.Query<
            RefRW<MutualCareBond>,
            RefRO<CooperationCohesion>>())
        {
            // Calculate bond strength from mutual care
            bond.ValueRW.BondStrength = (bond.ValueRO.EntityACareForB +
                                        bond.ValueRO.EntityBCareForA) * 0.5f;

            // Calculate mutuality (balanced care is better)
            float careImbalance = math.abs(bond.ValueRO.EntityACareForB -
                                          bond.ValueRO.EntityBCareForA);
            bond.ValueRW.MutualityScore = 1f - careImbalance;

            // Effects of mutual care
            bond.ValueRW.MoraleBonus = bond.ValueRO.BondStrength * 0.3f; // Up to +30% morale
            bond.ValueRW.StressReduction = bond.ValueRO.MutualityScore * 0.4f; // Up to -40% stress
            bond.ValueRW.PerformanceBonus = (bond.ValueRO.BondStrength *
                                            bond.ValueRO.MutualityScore) * 0.2f; // Up to +20%

            // Apply effects
            ApplyCareBonuses(bond.ValueRO.EntityA, bond.ValueRO);
            ApplyCareBonuses(bond.ValueRO.EntityB, bond.ValueRO);
        }
    }
}
```

### Care Example: Mechanic and Pilot

```csharp
// Mechanic cares about pilot surviving
var mechanicCare = new CareRelationship
{
    CareGiver = mechanicEntity,
    CareReceiver = pilotEntity,
    CareLevel = 0.8f,                   // Deeply cares
    Type = CareType.Professional
};

// Mechanic performs care actions:
// - Checks mech equipment thoroughly (better maintenance)
// - Ensures pilot gets rest before missions
// - Provides extra armor/shields
// - Worries when pilot is in danger

// Pilot cares about mechanic's wellbeing
var pilotCare = new CareRelationship
{
    CareGiver = pilotEntity,
    CareReceiver = mechanicEntity,
    CareLevel = 0.7f,
    Type = CareType.Professional
};

// Pilot performs care actions:
// - Shares extra rations
// - Protects mechanics during base assaults
// - Brings back salvage/resources for mechanics
// - Ensures mechanics sleep and aren't overworked

// Mutual care bond
var mutualBond = new MutualCareBond
{
    EntityA = mechanicEntity,
    EntityB = pilotEntity,
    EntityACareForB = 0.8f,
    EntityBCareForA = 0.7f,
    BondStrength = 0.75f,               // Average care
    MutualityScore = 0.9f,              // Well-balanced
    MoraleBonus = 0.225f,               // +22.5% morale
    PerformanceBonus = 0.135f           // +13.5% performance
};

// Result: Both perform better when working together
// Mechanic: Better maintenance quality, faster repairs
// Pilot: Higher morale in combat, fights harder to survive
```

---

## Social Cooperation

### Shared Language Development

Companions develop custom sign languages or codes:

```csharp
public struct SharedLanguage : IComponentData
{
    public FixedString64Bytes LanguageName;
    public LanguageComplexity Complexity;
    public float Vocabulary;                // 0.0 to 1.0 (how much they've developed)
    public float CommunicationSpeed;        // Faster with more practice
    public uint SpeakerCount;
}

public enum LanguageComplexity : byte
{
    BasicSigns = 0,         // Simple gestures (danger, food, wait)
    ModerateCodes = 1,      // Tactical hand signals
    ComplexLanguage = 2,    // Full sign language
    CipherLanguage = 3      // Encrypted communication
}

[InternalBufferCapacity(16)]
public struct LanguageSpeaker : IBufferElementData
{
    public Entity SpeakerEntity;
    public float Proficiency;               // How well they know this language
    public float ContributionToVocabulary;  // How much they've added
    public uint TimesUsed;
}

[BurstCompile]
public partial struct SharedLanguageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (language, speakers, cohesion) in SystemAPI.Query<
            RefRW<SharedLanguage>,
            DynamicBuffer<LanguageSpeaker>,
            RefRO<CooperationCohesion>>())
        {
            // Language develops over time through use and cohesion
            float totalContribution = 0f;
            float avgProficiency = 0f;
            int speakerCount = 0;

            foreach (var speaker in speakers)
            {
                totalContribution += speaker.ContributionToVocabulary;
                avgProficiency += speaker.Proficiency;
                speakerCount++;
            }

            language.ValueRW.SpeakerCount = (uint)speakerCount;

            if (speakerCount > 0)
            {
                avgProficiency /= speakerCount;

                // Vocabulary grows with contributions and cohesion
                float vocabularyGrowth = totalContribution * cohesion.ValueRO.CurrentCohesion;
                language.ValueRW.Vocabulary = math.min(1f, language.ValueRO.Vocabulary + vocabularyGrowth);

                // Communication speed improves with proficiency
                language.ValueRW.CommunicationSpeed = avgProficiency * (1f + language.ValueRO.Vocabulary);
            }
        }
    }
}
```

### Music Ensemble Cooperation

Band members cooperating on performances:

```csharp
public struct MusicEnsemble : IComponentData
{
    public FixedString64Bytes EnsembleName;
    public float SynchronizationLevel;      // How in-sync musicians are
    public float PerformanceQuality;        // Overall performance quality
    public float AudienceMoraleBonus;       // Morale bonus to listeners
    public uint MusicianCount;
}

[InternalBufferCapacity(8)]
public struct Musician : IBufferElementData
{
    public Entity MusicianEntity;
    public MusicInstrument Instrument;
    public float MusicalSkill;
    public float TimingAccuracy;            // How well they keep time
    public bool IsPlaying;
}

public enum MusicInstrument : byte
{
    Drums = 0,          // Rhythm section
    Strings = 1,        // Melody
    Wind = 2,           // Harmony
    Voice = 3,          // Vocals
    Brass = 4,          // Power
    Percussion = 5      // Accents
}

[BurstCompile]
public partial struct MusicEnsembleSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (ensemble, musicians, cohesion) in SystemAPI.Query<
            RefRW<MusicEnsemble>,
            DynamicBuffer<Musician>,
            RefRO<CooperationCohesion>>())
        {
            float totalSkill = 0f;
            float avgTiming = 0f;
            int activeCount = 0;

            foreach (var musician in musicians)
            {
                if (!musician.IsPlaying) continue;

                totalSkill += musician.MusicalSkill;
                avgTiming += musician.TimingAccuracy;
                activeCount++;
            }

            ensemble.ValueRW.MusicianCount = (uint)activeCount;

            if (activeCount > 0)
            {
                avgTiming /= activeCount;

                // Synchronization from cohesion and timing
                ensemble.ValueRW.SynchronizationLevel = (cohesion.ValueRO.CurrentCohesion +
                                                         avgTiming) * 0.5f;

                // Performance quality from skill and synchronization
                float avgSkill = totalSkill / activeCount;
                ensemble.ValueRW.PerformanceQuality = (avgSkill * 0.6f) +
                                                      (ensemble.ValueRO.SynchronizationLevel * 0.4f);

                // Audience morale bonus scales with quality
                ensemble.ValueRW.AudienceMoraleBonus = ensemble.ValueRO.PerformanceQuality * 0.5f;
            }
        }
    }
}
```

---

## Cross-Layer Cooperation

### Multi-Tier Support Network

Cooperation across organizational layers (operators → pilots → mechanics):

```csharp
public struct MultiTierCooperation : IComponentData
{
    public CooperationTier Tier;
    public Entity ParentCooperation;        // Link to higher tier
    public uint ChildCooperationCount;      // Number of subordinate cooperations
    public float CrossTierEfficiency;       // How well tiers coordinate
}

public enum CooperationTier : byte
{
    Strategic = 0,      // Command level
    Operational = 1,    // Officer level
    Tactical = 2,       // Squad level
    Individual = 3      // Personal level
}

[InternalBufferCapacity(8)]
public struct CooperationDependency : IBufferElementData
{
    public Entity DependentEntity;          // Who depends on this cooperation
    public DependencyType Type;
    public float DependencyStrength;        // How much they rely on it
}

public enum DependencyType : byte
{
    RequiresSupport = 0,    // Cannot function without
    ImprovedBy = 1,         // Better with support
    EnabledBy = 2,          // Unlocked by cooperation
    SynergizesWith = 3      // Mutual benefit
}

// Example: Pilot depends on multiple cooperations
// - Operator-Pilot link (sensor data, guidance)
// - Hangar crew (maintenance, deployment)
// - Squadron formation (combat coordination)
// - Mutual care with mechanic (morale, equipment quality)

[BurstCompile]
public partial struct CrossLayerCooperationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Strategic tier affects operational tier
        // Operational tier affects tactical tier
        // Tactical tier affects individual performance

        // Calculate cascading bonuses from all tiers
        foreach (var multiTier in SystemAPI.Query<RefRW<MultiTierCooperation>>())
        {
            // Get bonuses from parent tier
            if (multiTier.ValueRO.ParentCooperation != Entity.Null)
            {
                var parentBonus = GetCooperationBonus(multiTier.ValueRO.ParentCooperation);

                // Cross-tier efficiency determines how much parent bonus transfers
                float transferredBonus = parentBonus * multiTier.ValueRO.CrossTierEfficiency;

                ApplyCrossTierBonus(multiTier.ValueRO, transferredBonus);
            }
        }
    }
}
```

---

## Example Scenarios

### Scenario 1: Magic Circle (Ritual Casting)

```csharp
// 5 mages attempting high-level ritual
var ritualCoop = new Cooperation
{
    Type = CooperationType.RitualCasting,
    ParticipantCount = 5
};

// Participants:
// - Archmage (leader, skill 0.95)
// - 2 High Mages (skill 0.8, 0.75)
// - 2 Apprentices (skill 0.4, 0.35)

// Relations:
// - Archmage + High Mages: 0.8 (long-time colleagues)
// - High Mages + Apprentices: 0.6 (teacher-student)
// - Apprentices: 0.3 (rivals)

// Cohesion calculation:
// Skill synergy: 0.65 (high average, high variance)
// Relation bonus: 0.65 (mixed relations)
// Communication: 0.9 (all speak Ancient Arcane)
// Result cohesion: 0.73

// Ritual power:
// Base power: 250 mana combined
// Efficiency multiplier: 1 + (0.73 × 2.0) = 2.46×
// Final power: 615 mana equivalent

// If apprentice rivalry causes cohesion to drop to 0.55:
// Efficiency multiplier: 1 + (0.55 × 2.0) = 2.1×
// Final power: 525 mana (90 mana lost from poor cohesion)

// If cohesion drops below 0.6 (required threshold):
// Ritual FAILS, backfire damages all participants
```

### Scenario 2: Coordinated Archer Volley

```csharp
// 20 archers preparing volley fire
var volleyCoop = new Cooperation
{
    Type = CooperationType.CoordinatedVolley,
    ParticipantCount = 20
};

// Commander skill: 0.8 (good tactical leader)
// Archer skills: Average 0.6, range 0.4 to 0.8
// Relations: All from same village, average 0.75
// Communication: All speak native language, 1.0 clarity

// Cohesion: 0.82 (high)

// Volley bonuses:
// Simultaneity bonus: 0.82 × 0.5 = +41%
// Volume bonus: sqrt(20) × 0.2 = +89%
// Total multiplier: 2.3×

// Individual arrow damage: 50
// Volley damage: 50 × 2.3 = 115 per arrow
// Total volley: 115 × 20 = 2,300 damage
// (vs 1,000 damage if firing individually)

// Accuracy bonus: 0.82 × 0.3 = +24.6% accuracy
// Result: Devastating synchronized strike
```

### Scenario 3: Collaborative Crafting (Mixed Skills)

```csharp
// 3 blacksmiths crafting legendary sword
var craftingCoop = new Cooperation
{
    Type = CooperationType.CollaborativeCrafting,
    ParticipantCount = 3
};

// Craftsman A: Master smith (skill 0.95)
// Craftsman B: Journeyman (skill 0.6)
// Craftsman C: Apprentice (skill 0.3)

// Relations: All 0.7 (good working relationship)
// Cohesion: 0.75 (high)

// Quality calculation:
// High cohesion (0.75 > 0.7): Quality approaches max skill
// Quality = lerp(avgSkill, maxSkill, cohesion)
// Quality = lerp(0.62, 0.95, 0.75)
// Quality = 0.87 (Excellent)

// If cohesion was low (0.3):
// Low cohesion (0.3 < 0.4): Quality approaches min skill
// Quality = lerp(minSkill, avgSkill, cohesion)
// Quality = lerp(0.3, 0.62, 0.3)
// Quality = 0.40 (Poor - apprentice's mistakes drag down masters)

// Result: Cohesion matters more than individual skill for groups
```

### Scenario 4: Pilot-Mechanic Mutual Care

```csharp
// Pilot and mechanic with strong mutual care bond
var careBond = new MutualCareBond
{
    EntityA = pilotEntity,
    EntityB = mechanicEntity,
    EntityACareForB = 0.85f,        // Pilot deeply cares about mechanic
    EntityBCareForA = 0.90f,        // Mechanic deeply cares about pilot
    BondStrength = 0.875f,
    MutualityScore = 0.95f          // Very balanced
};

// Effects:
// Morale bonus: 0.875 × 0.3 = +26.25% morale (both)
// Stress reduction: 0.95 × 0.4 = -38% stress (both)
// Performance bonus: 0.875 × 0.95 × 0.2 = +16.625% (both)

// Mechanic behaviors:
// - Prioritizes pilot's mech for repairs (+20% speed)
// - Uses higher quality parts (+15% quality)
// - Double-checks systems (+10% reliability)
// - Worries when pilot is in danger (stress when pilot HP low)

// Pilot behaviors:
// - Brings back salvage for mechanic (+resources)
// - Shares extra rations (+wellbeing)
// - Fights more carefully (+10% survival focus)
// - Protects mechanic during base assaults (+bodyguard)

// Combined result:
// - Pilot: Higher morale, better maintained mech, fights to survive
// - Mechanic: Higher morale, steady resource flow, protected
// - Both perform ~17% better when working together
```

### Scenario 5: Multi-Layer Cooperation (Squadron)

```csharp
// Strategic: Fleet command coordinates squadrons
// Operational: Squadron leader coordinates pilots
// Tactical: Pilots coordinate with operators
// Individual: Pilot cares about mechanic

// Pilot receives bonuses from ALL layers:

// 1. Fleet command (Strategic tier)
//    - Mission parameters: +10% objective clarity
//    - Resource allocation: +5% supply bonus
//    Cross-tier efficiency: 0.7
//    Transferred bonus: +10.5%

// 2. Squadron leader (Operational tier)
//    - Formation bonuses: +30% (from formation system)
//    - Tactical coordination: +15%
//    Cross-tier efficiency: 0.9
//    Transferred bonus: +40.5%

// 3. Operator link (Tactical tier)
//    - Sensor data: +40% threat awareness
//    - Targeting: +50% accuracy
//    - Navigation: +30% route efficiency
//    Direct bonus: +120%

// 4. Mechanic care (Individual tier)
//    - Mech quality: +15%
//    - Morale: +26%
//    - Performance: +17%
//    Direct bonus: +58%

// Total effective bonus: ~229%
// (Cascading effects multiply effectiveness)
```

---

## Integration with Other Systems

**Relations System**: Cohesion scales with relations between cooperators

**Communication System**: Clarity affects cooperation effectiveness, shared languages enable better coordination

**Patience System**: Impatient entities struggle with long cooperative tasks (research, formations)

**Formations**: Formations are specialized cooperation with combat bonuses

**Combat Mechanics**: Cooperation bonuses stack with accuracy, damage, defense

**Memory Tapping**: Shared memories are a form of spiritual cooperation

**Circadian Rhythms**: Energy levels affect cooperation participation and quality

**Sandbox Modding**: All cooperation parameters are runtime-moddable and opt-in

---

## Performance Targets

```
Cohesion Calculation:      <0.15ms per cooperation (20 members)
Mana Pooling:             <0.1ms per pool (8 contributors)
Volley Coordination:      <0.2ms per volley (20 shooters)
Research Collaboration:   <0.1ms per project (8 researchers)
Care Relationship:        <0.05ms per bond
Cross-Layer Propagation:  <0.3ms per tier cascade
────────────────────────────────────────
Total (100 cooperations): <80ms per frame
```

---

## Summary

**Cooperation Types**: Magic (mana pooling, rituals), Combat (volleys, focus fire), Production (research, crafting), Facility (crews, operators), Social (bands, languages), Care (mutual support)

**Cohesion Mechanics**: Skills (40%) + Relations (30%) + Communication (20%) + Experience (10%) = Cohesion quality

**Efficiency Scaling**: Different cooperation types scale 1.5× to 3× effectiveness with perfect cohesion

**Multi-Layer**: Cooperation cascades across organizational tiers (strategic → operational → tactical → individual)

**Care Relationships**: Entities care about cooperators' wellbeing, creating morale and performance bonuses

**Key Insight**: A phalanx is 400 warriors cooperating as one. A ritual circle is 5 mages channeling as one. A hangar crew is 12 specialists operating as one. A pilot and mechanic caring for each other perform 17% better together. Cooperation quality depends more on cohesion than individual skill - strangers with high skills produce poor results, while friends with moderate skills excel.
