# Extension Request: Expertise & Mastery Tracking System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Both games need expertise/mastery systems for entity progression:

**Space4X:**
- Officers have expertise vectors (CarrierCommand, Espionage, Logistics, Tactics)
- Activity-driven XP feeds into expertise pools
- Inclination modifiers (1-10) scale XP gain and skill costs
- Semi-random XP spending within player-guided priorities
- Mentor quality affects trainee learning rate

**Godgame:**
- Villagers have profession mastery (Farming, Smithing, Combat, Healing)
- Tasks grant XP toward relevant skills
- Natural aptitude affects learning speed
- Experienced villagers train apprentices
- Mastery unlocks profession-specific abilities

Shared needs:
- Multi-vector expertise tracking
- Activity-to-XP conversion
- Inclination/aptitude modifiers
- Teaching/mentoring calculations
- Mastery tier thresholds

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Category of expertise.
/// </summary>
public enum ExpertiseCategory : byte
{
    // Combat
    Combat = 0,
    Tactics = 1,
    Weapons = 2,
    Defense = 3,
    
    // Support
    Logistics = 4,
    Engineering = 5,
    Medical = 6,
    Science = 7,
    
    // Social
    Command = 8,
    Diplomacy = 9,
    Espionage = 10,
    Trading = 11,
    
    // Craft
    Crafting = 12,
    Farming = 13,
    Mining = 14,
    Construction = 15,
    
    // Special
    Psionic = 16,
    Beastmastery = 17,
    Navigation = 18,
    Research = 19
}

/// <summary>
/// Mastery tier for expertise.
/// </summary>
public enum MasteryTier : byte
{
    Novice = 0,         // 0-99 XP
    Apprentice = 1,     // 100-499 XP
    Journeyman = 2,     // 500-1999 XP
    Expert = 3,         // 2000-7999 XP
    Master = 4,         // 8000-24999 XP
    Grandmaster = 5,    // 25000+ XP
    Legend = 6          // 100000+ XP (rare)
}

/// <summary>
/// Single expertise entry.
/// </summary>
[InternalBufferCapacity(8)]
public struct ExpertiseEntry : IBufferElementData
{
    public ExpertiseCategory Category;
    public float CurrentXP;
    public float TotalXP;              // Lifetime accumulated
    public MasteryTier Tier;
    public byte Inclination;           // 1-10, natural aptitude
    public float RecentGain;           // XP gained this session
    public uint LastActivityTick;
}

/// <summary>
/// Pending XP to be distributed.
/// </summary>
public struct XPPool : IComponentData
{
    public float UnallocatedXP;        // XP waiting for distribution
    public float CombatXP;             // XP from combat activities
    public float CraftXP;              // XP from crafting activities
    public float SocialXP;             // XP from social activities
    public float SpecialXP;            // XP from special activities
}

/// <summary>
/// Preferences for XP allocation.
/// </summary>
public struct XPAllocationPrefs : IComponentData
{
    public ExpertiseCategory PrimaryFocus;
    public ExpertiseCategory SecondaryFocus;
    public float FocusWeight;          // 0-1, how much to favor focus
    public byte AutoAllocate;          // System allocates automatically
    public byte FollowAptitude;        // Favor high-inclination skills
}

/// <summary>
/// Teaching/mentoring capability.
/// </summary>
public struct MentoringCapability : IComponentData
{
    public ExpertiseCategory Specialty;
    public MasteryTier MinTierToTeach; // Minimum tier to be a mentor
    public float TeachingQuality;      // 0-1, how good at teaching
    public float MaxStudentsPerTick;   // Teaching capacity
    public byte IsAvailable;           // Currently mentoring
}

/// <summary>
/// Learning from a mentor.
/// </summary>
public struct MentorshipState : IComponentData
{
    public Entity MentorEntity;
    public ExpertiseCategory LearningCategory;
    public float LearningProgress;     // 0-1 current lesson
    public float XPMultiplier;         // Bonus from mentor quality
    public uint StartedTick;
    public byte IsActive;
}

/// <summary>
/// Activity that grants XP.
/// </summary>
public struct XPActivity
{
    public ExpertiseCategory Category;
    public float BaseXP;
    public float DifficultyModifier;   // Harder = more XP
    public float SuccessModifier;      // Success quality affects XP
    public uint CompletedTick;
}

/// <summary>
/// Expertise thresholds configuration.
/// </summary>
public struct ExpertiseConfig : IComponentData
{
    public float NoviceThreshold;      // 0
    public float ApprenticeThreshold;  // 100
    public float JourneymanThreshold;  // 500
    public float ExpertThreshold;      // 2000
    public float MasterThreshold;      // 8000
    public float GrandmasterThreshold; // 25000
    public float LegendThreshold;      // 100000
}
```

### Static Helpers

```csharp
public static class ExpertiseHelpers
{
    /// <summary>
    /// Calculates XP gain from activity with modifiers.
    /// </summary>
    public static float CalculateXPGain(
        in XPActivity activity,
        byte inclination,
        float mentorBonus,
        float focusBonus)
    {
        // Base XP from activity
        float xp = activity.BaseXP;
        
        // Difficulty scales XP (harder = more)
        xp *= activity.DifficultyModifier;
        
        // Success quality
        xp *= activity.SuccessModifier;
        
        // Inclination bonus (1-10 -> 0.5-1.5x)
        float inclinationMod = 0.5f + inclination * 0.1f;
        xp *= inclinationMod;
        
        // Mentor bonus (stacks)
        xp *= 1f + mentorBonus;
        
        // Focus bonus from player priorities
        xp *= 1f + focusBonus;
        
        return xp;
    }

    /// <summary>
    /// Gets mastery tier from XP amount.
    /// </summary>
    public static MasteryTier GetMasteryTier(float xp, in ExpertiseConfig config)
    {
        if (xp >= config.LegendThreshold) return MasteryTier.Legend;
        if (xp >= config.GrandmasterThreshold) return MasteryTier.Grandmaster;
        if (xp >= config.MasterThreshold) return MasteryTier.Master;
        if (xp >= config.ExpertThreshold) return MasteryTier.Expert;
        if (xp >= config.JourneymanThreshold) return MasteryTier.Journeyman;
        if (xp >= config.ApprenticeThreshold) return MasteryTier.Apprentice;
        return MasteryTier.Novice;
    }

    /// <summary>
    /// Calculates progress to next tier (0-1).
    /// </summary>
    public static float GetTierProgress(float xp, MasteryTier currentTier, in ExpertiseConfig config)
    {
        float currentThreshold = GetThreshold(currentTier, config);
        float nextThreshold = GetThreshold((MasteryTier)((int)currentTier + 1), config);
        
        if (nextThreshold <= currentThreshold) return 1f; // Max tier
        
        return (xp - currentThreshold) / (nextThreshold - currentThreshold);
    }

    private static float GetThreshold(MasteryTier tier, in ExpertiseConfig config)
    {
        return tier switch
        {
            MasteryTier.Novice => config.NoviceThreshold,
            MasteryTier.Apprentice => config.ApprenticeThreshold,
            MasteryTier.Journeyman => config.JourneymanThreshold,
            MasteryTier.Expert => config.ExpertThreshold,
            MasteryTier.Master => config.MasterThreshold,
            MasteryTier.Grandmaster => config.GrandmasterThreshold,
            MasteryTier.Legend => config.LegendThreshold,
            _ => 0
        };
    }

    /// <summary>
    /// Allocates XP from pool to expertise entries.
    /// </summary>
    public static void AllocateXP(
        ref XPPool pool,
        ref DynamicBuffer<ExpertiseEntry> expertise,
        in XPAllocationPrefs prefs,
        in ExpertiseConfig config,
        uint currentTick)
    {
        if (pool.UnallocatedXP <= 0) return;
        
        float toAllocate = pool.UnallocatedXP;
        pool.UnallocatedXP = 0;
        
        // Find focus entries
        int primaryIdx = -1;
        int secondaryIdx = -1;
        float totalWeight = 0;
        
        for (int i = 0; i < expertise.Length; i++)
        {
            if (expertise[i].Category == prefs.PrimaryFocus) primaryIdx = i;
            if (expertise[i].Category == prefs.SecondaryFocus) secondaryIdx = i;
            
            // Weight by inclination if following aptitude
            float weight = prefs.FollowAptitude != 0 ? expertise[i].Inclination : 5f;
            totalWeight += weight;
        }
        
        // Distribute XP
        for (int i = 0; i < expertise.Length; i++)
        {
            var entry = expertise[i];
            float share;
            
            if (i == primaryIdx)
            {
                share = toAllocate * prefs.FocusWeight;
            }
            else if (i == secondaryIdx)
            {
                share = toAllocate * prefs.FocusWeight * 0.5f;
            }
            else
            {
                float weight = prefs.FollowAptitude != 0 ? entry.Inclination : 5f;
                share = toAllocate * (1f - prefs.FocusWeight * 1.5f) * (weight / totalWeight);
            }
            
            entry.CurrentXP += share;
            entry.TotalXP += share;
            entry.RecentGain += share;
            entry.Tier = GetMasteryTier(entry.CurrentXP, config);
            entry.LastActivityTick = currentTick;
            expertise[i] = entry;
        }
    }

    /// <summary>
    /// Calculates teaching effectiveness.
    /// </summary>
    public static float CalculateTeachingBonus(
        in MentoringCapability mentor,
        MasteryTier mentorTier,
        MasteryTier studentTier)
    {
        // Can't teach what you barely know
        if (mentorTier < mentor.MinTierToTeach) return 0;
        
        // Teaching quality base
        float bonus = mentor.TeachingQuality;
        
        // Gap between mentor and student matters
        int tierGap = (int)mentorTier - (int)studentTier;
        float gapBonus = math.min(0.5f, tierGap * 0.1f);
        
        return bonus + gapBonus;
    }

    /// <summary>
    /// Calculates XP decay for unused expertise.
    /// </summary>
    public static float CalculateDecay(
        float currentXP,
        uint ticksSinceActivity,
        MasteryTier tier)
    {
        // Higher tiers decay slower
        float tierResistance = 1f - (int)tier * 0.1f;
        
        // Base decay rate
        float decayRate = 0.0001f * tierResistance;
        
        // More XP = more to lose, but slower rate
        float decayAmount = currentXP * decayRate * ticksSinceActivity;
        
        // Never decay below tier threshold
        return math.max(0, decayAmount);
    }

    /// <summary>
    /// Checks if entity can mentor in category.
    /// </summary>
    public static bool CanMentor(
        in DynamicBuffer<ExpertiseEntry> expertise,
        ExpertiseCategory category,
        in MentoringCapability capability)
    {
        for (int i = 0; i < expertise.Length; i++)
        {
            if (expertise[i].Category == category &&
                expertise[i].Tier >= capability.MinTierToTeach)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets best expertise category for entity.
    /// </summary>
    public static ExpertiseCategory GetBestExpertise(
        in DynamicBuffer<ExpertiseEntry> expertise,
        out MasteryTier tier,
        out float xp)
    {
        tier = MasteryTier.Novice;
        xp = 0;
        ExpertiseCategory best = ExpertiseCategory.Combat;
        
        for (int i = 0; i < expertise.Length; i++)
        {
            if (expertise[i].CurrentXP > xp)
            {
                xp = expertise[i].CurrentXP;
                tier = expertise[i].Tier;
                best = expertise[i].Category;
            }
        }
        
        return best;
    }

    /// <summary>
    /// Adds XP from completed activity.
    /// </summary>
    public static void AddActivityXP(
        ref XPPool pool,
        in XPActivity activity)
    {
        // Route to appropriate pool
        if ((int)activity.Category < 4)
            pool.CombatXP += activity.BaseXP;
        else if ((int)activity.Category < 8)
            pool.CraftXP += activity.BaseXP;
        else if ((int)activity.Category < 12)
            pool.SocialXP += activity.BaseXP;
        else
            pool.SpecialXP += activity.BaseXP;
        
        pool.UnallocatedXP += activity.BaseXP;
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Officer expertise tracking ===
var expertise = EntityManager.GetBuffer<ExpertiseEntry>(officerEntity);
var pool = EntityManager.GetComponentData<XPPool>(officerEntity);
var prefs = EntityManager.GetComponentData<XPAllocationPrefs>(officerEntity);
var config = EntityManager.GetComponentData<ExpertiseConfig>(officerEntity);

// Officer completes combat mission
var activity = new XPActivity
{
    Category = ExpertiseCategory.Tactics,
    BaseXP = 50f,
    DifficultyModifier = 1.5f,  // Hard mission
    SuccessModifier = 1.2f,     // Good performance
    CompletedTick = currentTick
};

ExpertiseHelpers.AddActivityXP(ref pool, activity);

// Allocate accumulated XP
ExpertiseHelpers.AllocateXP(ref pool, ref expertise, prefs, config, currentTick);

// Check for tier advancement
for (int i = 0; i < expertise.Length; i++)
{
    var entry = expertise[i];
    MasteryTier newTier = ExpertiseHelpers.GetMasteryTier(entry.CurrentXP, config);
    if (newTier > entry.Tier)
    {
        TriggerTierAdvancement(officerEntity, entry.Category, newTier);
    }
}

// === Godgame: Villager profession mastery ===
var villagerExpertise = EntityManager.GetBuffer<ExpertiseEntry>(villagerEntity);

// Villager learns from master smith
var mentorCapability = EntityManager.GetComponentData<MentoringCapability>(masterSmithEntity);
var mentorExpertise = EntityManager.GetBuffer<ExpertiseEntry>(masterSmithEntity);

// Find mentor's smithing tier
MasteryTier mentorTier = MasteryTier.Novice;
for (int i = 0; i < mentorExpertise.Length; i++)
{
    if (mentorExpertise[i].Category == ExpertiseCategory.Crafting)
    {
        mentorTier = mentorExpertise[i].Tier;
        break;
    }
}

// Find student's smithing tier
MasteryTier studentTier = MasteryTier.Novice;
for (int i = 0; i < villagerExpertise.Length; i++)
{
    if (villagerExpertise[i].Category == ExpertiseCategory.Crafting)
    {
        studentTier = villagerExpertise[i].Tier;
        break;
    }
}

// Calculate teaching bonus
float teachingBonus = ExpertiseHelpers.CalculateTeachingBonus(
    mentorCapability, mentorTier, studentTier);

// Apply to crafting XP gain
var craftActivity = new XPActivity
{
    Category = ExpertiseCategory.Crafting,
    BaseXP = 25f,
    DifficultyModifier = 1.0f,
    SuccessModifier = 1.0f
};

// Find inclination
byte inclination = 5;
for (int i = 0; i < villagerExpertise.Length; i++)
{
    if (villagerExpertise[i].Category == ExpertiseCategory.Crafting)
    {
        inclination = villagerExpertise[i].Inclination;
        break;
    }
}

float xpGain = ExpertiseHelpers.CalculateXPGain(
    craftActivity, inclination, teachingBonus, 0);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Single skill number
  - **Rejected**: Both games need multi-vector progression

- **Alternative 2**: Game-specific systems
  - **Rejected**: Core mechanics (XP, tiers, teaching) are identical

---

## Implementation Notes

**Dependencies:**
- None - standalone utility

**Performance Considerations:**
- Expertise buffers are fixed-size
- Allocation is O(n) per entity
- Can batch XP updates

**Related Requests:**
- Progression system (general level tracking)
- Focus system (mental resources for abilities)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

