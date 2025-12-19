# Sandbox Mode and Runtime Modding System

## Overview

Complete opt-in, moddable, and runtime-adjustable architecture enabling Garry's Mod-like creative freedom. All gameplay systems expose parameters for real-time modification, feature toggling, and persistent mod configurations. Players can enable/disable any feature, tweak any parameter, and create custom gameplay experiences during runtime.

**Key Principles**:
- **Everything is opt-in**: No forced features, all toggleable
- **Everything is moddable**: Expose all parameters for modification
- **Runtime modification**: Change values while game is running
- **Persistent configurations**: Save/load mod profiles
- **Hot-reload**: Apply changes without restart
- **Sandbox tools**: In-game UI for live tweaking
- **Cross-game**: Godgame and Space4X both support full modding

---

## Core Architecture

### Feature Toggle System

Every major gameplay feature has an opt-in toggle:

```csharp
public struct FeatureToggles : IComponentData
{
    // Combat features
    public bool EnableAccuracyDisruption;
    public bool EnableKnockbackSystem;
    public bool EnableDamageTypeModifiers;
    public bool EnableCriticalHits;
    public bool EnableMoraleSystem;

    // Social features
    public bool EnablePatienceSystem;
    public bool EnableCircadianRhythms;
    public bool EnableMemoryTapping;
    public bool EnableReputationSystem;
    public bool EnableRelationshipSystem;
    public bool EnableDialogueSystem;

    // Economic features
    public bool EnableResourceDepletion;
    public bool EnableEconomicSimulation;
    public bool EnableTradeRoutes;
    public bool EnableSupplyDemand;

    // Environmental features
    public bool EnableWeatherSystem;
    public bool EnableSeasonalChanges;
    public bool EnableDiseaseSystem;
    public bool EnableAgingSystem;

    // AI features
    public bool EnableAIBehaviors;
    public bool EnableAILearning;
    public bool EnableAIEmergentBehavior;
    public bool EnableAITelemetry;

    // Visualization features
    public bool EnableOverlays;
    public bool EnableParticleEffects;
    public bool EnableVisualFeedback;
    public bool EnableDebugVisualization;
}

public struct FeatureToggleRequest : IComponentData
{
    public FixedString64Bytes FeatureName;
    public bool NewState;               // Enable or disable
    public bool ApplyImmediately;
    public uint RequestTick;
}
```

### Runtime Parameter System

All gameplay parameters exposed for modification:

```csharp
public struct RuntimeParameter : IComponentData
{
    public FixedString64Bytes Category;        // "combat", "social", "economy"
    public FixedString64Bytes ParameterName;   // "damage_multiplier", "patience_decay_rate"
    public ParameterType Type;
    public float CurrentValue;
    public float DefaultValue;
    public float MinValue;
    public float MaxValue;
    public bool IsModified;                     // Track if changed from default
    public bool RequiresRestart;                // Some changes need restart
}

public enum ParameterType : byte
{
    Float = 0,
    Integer = 1,
    Boolean = 2,
    Enum = 3,
    Color = 4,
    Vector3 = 5,
    String = 6
}

public struct ParameterModification : IComponentData
{
    public FixedString64Bytes ParameterPath;   // "combat.accuracy.base_disruption_rate"
    public float NewValue;
    public uint ModifiedTick;
    public bool ApplyToExistingEntities;        // Retroactive or new only
}

[InternalBufferCapacity(128)]
public struct ParameterHistory : IBufferElementData
{
    public FixedString64Bytes ParameterName;
    public float OldValue;
    public float NewValue;
    public uint Tick;
}
```

### Mod Configuration Profiles

Save/load complete gameplay configurations:

```csharp
public struct ModProfile : IComponentData
{
    public FixedString128Bytes ProfileName;    // "Hardcore Mode", "Peaceful Sandbox"
    public FixedString256Bytes Description;
    public FixedString64Bytes Author;
    public uint CreatedTick;
    public uint LastModifiedTick;
    public uint ParameterCount;
    public uint FeatureToggleCount;
}

[InternalBufferCapacity(256)]
public struct ModParameter : IBufferElementData
{
    public FixedString64Bytes Path;            // Full parameter path
    public float Value;
    public ParameterType Type;
}

[InternalBufferCapacity(64)]
public struct ModFeatureToggle : IBufferElementData
{
    public FixedString64Bytes FeatureName;
    public bool Enabled;
}

public struct ModProfileRequest : IComponentData
{
    public FixedString128Bytes ProfileName;
    public ModProfileAction Action;
    public bool OverwriteExisting;
}

public enum ModProfileAction : byte
{
    Load = 0,
    Save = 1,
    Delete = 2,
    Export = 3,             // Export to file
    Import = 4,             // Import from file
    Reset = 5               // Reset to defaults
}
```

---

## Runtime Parameter Categories

### Combat Parameters

```csharp
public struct CombatParameterSet : IComponentData
{
    // Accuracy disruption (opt-in)
    public float DamageToDisruptionRatio;       // Default: 0.01 (100 damage = 1.0 disruption)
    public float KnockbackToDisruptionRatio;    // Default: 0.02 (50 knockback = 1.0 disruption)
    public float DisruptionDecayRate;           // Default: 0.1/sec
    public float MaxDisruptionDampening;        // Default: 0.95 (95% max)

    // Stability modifiers
    public float StrengthOffsetWeight;          // Default: 1.0
    public float MassOffsetWeight;              // Default: 1.0
    public float FocusOffsetWeight;             // Default: 1.0

    // Damage type multipliers (all runtime adjustable)
    public float PhysicalDamageMultiplier;      // Default: 1.0
    public float FireDamageMultiplier;          // Default: 1.0
    public float ColdDamageMultiplier;          // Default: 1.0
    public float LightningDamageMultiplier;     // Default: 1.0
    public float PoisonDamageMultiplier;        // Default: 1.0
    public float PsychicDamageMultiplier;       // Default: 1.0
    public float HolyDamageMultiplier;          // Default: 1.0
    public float VoidDamageMultiplier;          // Default: 1.0

    // Critical hit parameters
    public float BaseCritChance;                // Default: 0.05 (5%)
    public float BaseCritMultiplier;            // Default: 1.5
    public float MaxCritChance;                 // Default: 0.95 (95%)
    public float MaxCritMultiplier;             // Default: 5.0
}
```

### Social Parameters

```csharp
public struct SocialParameterSet : IComponentData
{
    // Patience system (opt-in)
    public float InitiativeToPatienceWeight;    // Default: 0.8 (80% inverse correlation)
    public float MinPatienceRating;             // Default: 0.1 (10% minimum)
    public float BaseWaitingThreshold;          // Default: 10s
    public float MaxWaitingThreshold;           // Default: 300s (5 min)
    public float PatienceDepletionMultiplier;   // Default: 1.0
    public float PatienceRegenMultiplier;       // Default: 1.0

    // Circadian rhythms (opt-in)
    public float EnergyImpactOnPerformance;     // Default: 0.5 (50% to 100% performance)
    public float SleepDebtPenaltyRate;          // Default: 0.1 (10% per hour deficit)
    public float MaxSleepDebtPenalty;           // Default: 0.5 (50% max penalty)
    public float CircadianFlexibility;          // Default: 1.0 (can shift schedules)

    // Memory tapping (opt-in)
    public float MemoryTapBaseFocusCost;        // Default: 2.0/sec
    public float MemoryTapMaxBonus;             // Default: 1.0 (100%)
    public float MemoryTapDurationMultiplier;   // Default: 1.0
    public float CharismaEffectMultiplier;      // Default: 1.0
    public float ParticipantScalingPower;       // Default: 0.5 (sqrt scaling)

    // Reputation
    public float ReputationDecayRate;           // Default: 0.01/day
    public float ReputationGainMultiplier;      // Default: 1.0
    public float ReputationLossMultiplier;      // Default: 1.5 (easier to lose)
}
```

### Economic Parameters

```csharp
public struct EconomicParameterSet : IComponentData
{
    // Resource system
    public float ResourceDepletionRate;         // Default: 1.0
    public float ResourceRegenerationRate;      // Default: 0.1
    public bool AllowInfiniteResources;         // Default: false (sandbox option)

    // Trade
    public float BaseTradeProfitMargin;         // Default: 0.2 (20%)
    public float TradeRouteEfficiencyMultiplier; // Default: 1.0
    public float SupplyDemandSensitivity;       // Default: 1.0

    // Production
    public float ProductionSpeedMultiplier;     // Default: 1.0
    public float ProductionCostMultiplier;      // Default: 1.0
    public float ProductionQualityVariance;     // Default: 0.2 (±20%)
}
```

### Environmental Parameters

```csharp
public struct EnvironmentalParameterSet : IComponentData
{
    // Weather (opt-in)
    public float WeatherChangeFrequency;        // Default: 1.0 (normal speed)
    public float WeatherSeverityMultiplier;     // Default: 1.0
    public bool AllowExtremeWeather;            // Default: true

    // Seasons (opt-in)
    public float SeasonLengthMultiplier;        // Default: 1.0
    public float SeasonalImpactMultiplier;      // Default: 1.0

    // Disease (opt-in)
    public float DiseaseSpreadRate;             // Default: 1.0
    public float DiseaseSeverity;               // Default: 1.0
    public bool AllowPandemics;                 // Default: true

    // Aging (opt-in)
    public float AgingSpeedMultiplier;          // Default: 1.0
    public bool AllowPermadeath;                // Default: true
}
```

---

## Sandbox Mode Tools

### In-Game Tweaker UI

Real-time parameter modification interface:

```csharp
public struct SandboxToolsState : IComponentData
{
    public bool IsActive;
    public SandboxToolMode CurrentMode;
    public Entity SelectedEntity;               // For entity-specific tweaking
    public FixedString64Bytes ActiveCategory;
}

public enum SandboxToolMode : byte
{
    ParameterTweaker = 0,       // Adjust numerical values
    FeatureToggles = 1,         // Enable/disable features
    EntitySpawner = 2,          // Spawn entities with custom parameters
    ScenarioBuilder = 3,        // Build custom scenarios
    DebugVisualization = 4,     // Visual debugging tools
    ProfileManager = 5          // Load/save configurations
}

public struct ParameterTweakerUI : IComponentData
{
    public FixedString64Bytes SearchFilter;
    public FixedString64Bytes CategoryFilter;
    public bool ShowModifiedOnly;
    public bool ShowAdvancedParameters;
    public TweakerSortMode SortMode;
}

public enum TweakerSortMode : byte
{
    Alphabetical = 0,
    Category = 1,
    MostRecent = 2,
    MostModified = 3
}

[InternalBufferCapacity(32)]
public struct ParameterSlider : IBufferElementData
{
    public FixedString64Bytes ParameterPath;
    public float CurrentValue;
    public float SliderMin;
    public float SliderMax;
    public float StepSize;                      // For discrete values
    public bool UseLogarithmicScale;            // For large ranges
}
```

### Hot-Reload System

Apply changes without restart:

```csharp
public struct HotReloadRequest : IComponentData
{
    public HotReloadTarget Target;
    public bool PreserveWorldState;             // Keep entities, just update rules
    public bool ShowReloadNotification;
}

public enum HotReloadTarget : byte
{
    Parameters = 0,         // Reload parameter values
    Features = 1,           // Reload feature toggles
    Systems = 2,            // Reload system configurations
    All = 3                 // Full hot-reload
}

[BurstCompile]
public partial struct HotReloadSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var request in SystemAPI.Query<RefRO<HotReloadRequest>>())
        {
            switch (request.ValueRO.Target)
            {
                case HotReloadTarget.Parameters:
                    ReloadAllParameters(ref state, request.ValueRO.PreserveWorldState);
                    break;

                case HotReloadTarget.Features:
                    ReloadFeatureToggles(ref state);
                    break;

                case HotReloadTarget.Systems:
                    ReloadSystemConfigurations(ref state);
                    break;

                case HotReloadTarget.All:
                    ReloadEverything(ref state, request.ValueRO.PreserveWorldState);
                    break;
            }
        }
    }

    private void ReloadAllParameters(ref SystemState state, bool preserveWorld)
    {
        // Get all runtime parameters
        foreach (var param in SystemAPI.Query<RefRW<RuntimeParameter>>())
        {
            // Apply new values to existing entities if requested
            if (preserveWorld)
            {
                ApplyParameterToExistingEntities(ref state, param.ValueRO);
            }

            // Update parameter value
            param.ValueRW.IsModified = param.ValueRO.CurrentValue != param.ValueRO.DefaultValue;
        }
    }
}
```

---

## Preset Mod Profiles

Common configurations players can load instantly:

```csharp
public static class DefaultModProfiles
{
    // Vanilla experience (all defaults)
    public static ModProfile Vanilla => new()
    {
        ProfileName = "Vanilla",
        Description = "Default game experience with all features balanced",
        Author = "PureDOTS Team"
    };

    // Hardcore difficulty
    public static ModProfile Hardcore => new()
    {
        ProfileName = "Hardcore",
        Description = "Brutal difficulty - high damage, fast depletion, permadeath",
        Author = "PureDOTS Team"
        // Parameters:
        // - damage_multiplier: 2.0
        // - resource_depletion_rate: 3.0
        // - allow_permadeath: true
        // - disease_severity: 2.0
        // - patience_depletion_multiplier: 2.0
    };

    // Peaceful sandbox
    public static ModProfile PeacefulSandbox => new()
    {
        ProfileName = "Peaceful Sandbox",
        Description = "No combat, infinite resources, creative building",
        Author = "PureDOTS Team"
        // Features disabled:
        // - combat_enabled: false
        // - resource_depletion: false
        // - disease_system: false
        // Parameters:
        // - allow_infinite_resources: true
        // - production_speed_multiplier: 5.0
    };

    // Roleplay focused
    public static ModProfile RoleplayFocus => new()
    {
        ProfileName = "Roleplay Focus",
        Description = "Enhanced social systems, reduced combat emphasis",
        Author = "PureDOTS Team"
        // Features enabled:
        // - patience_system: true
        // - circadian_rhythms: true
        // - memory_tapping: true
        // - dialogue_system: true
        // Parameters:
        // - combat_damage_multiplier: 0.5
        // - social_interaction_frequency: 2.0
    };

    // Speed run
    public static ModProfile SpeedRun => new()
    {
        ProfileName = "Speed Run",
        Description = "Accelerated gameplay for quick sessions",
        Author = "PureDOTS Team"
        // Parameters:
        // - production_speed_multiplier: 10.0
        // - movement_speed_multiplier: 3.0
        // - research_speed_multiplier: 5.0
        // - season_length_multiplier: 0.1
    };

    // Chaos mode
    public static ModProfile ChaosMode => new()
    {
        ProfileName = "Chaos Mode",
        Description = "Randomized parameters, unpredictable gameplay",
        Author = "PureDOTS Team"
        // Features:
        // - random_events: true
        // - extreme_weather: true
        // Parameters randomized each session
    };
}
```

---

## Runtime Modification Examples

### Example 1: Player Tweaks Combat Difficulty Mid-Game

```csharp
// Player finds combat too difficult, wants to reduce disruption impact
var modRequest = new ParameterModification
{
    ParameterPath = "combat.accuracy.damage_disruption_ratio",
    NewValue = 0.005f,                  // Half the default (0.01 → 0.005)
    ApplyToExistingEntities = true      // Affect current fights
};

// System applies immediately
ApplyParameterModification(modRequest);

// Result: All entities now experience 50% less accuracy disruption from damage
// 100 damage now causes 0.5 disruption instead of 1.0
// Change persists for this session, can be saved to profile
```

### Example 2: Enable Patience System After 10 Hours

```csharp
// Player wants more behavioral diversity after learning game
var featureRequest = new FeatureToggleRequest
{
    FeatureName = "patience_system",
    NewState = true,
    ApplyImmediately = true
};

// System hot-reloads, adds Patience component to all entities
EnableFeature(featureRequest);

// Result: All entities now have patience ratings calculated
// - High initiative scouts become impatient
// - Low initiative strategists become patient
// - Learning sessions affected by patience thresholds
// No restart required
```

### Example 3: Load "Peaceful Sandbox" Profile

```csharp
var profileRequest = new ModProfileRequest
{
    ProfileName = "Peaceful Sandbox",
    Action = ModProfileAction.Load,
    OverwriteExisting = true
};

// System loads profile, applies all changes
LoadModProfile(profileRequest);

// Result:
// Features disabled: combat, resource depletion, disease
// Parameters changed: infinite resources, 5× production speed
// Existing world state preserved, but rules changed
// Can still manually tweak individual parameters after loading
```

### Example 4: Create Custom "Diplomatic Victory" Mod

```csharp
// Player creates custom mod profile for diplomatic gameplay
var customProfile = new ModProfile
{
    ProfileName = "Diplomatic Victory",
    Description = "Win through relations, not combat",
    Author = "PlayerName"
};

// Configure features
DynamicBuffer<ModFeatureToggle> features = GetBuffer(customProfileEntity);
features.Add(new ModFeatureToggle { FeatureName = "combat_enabled", Enabled = false });
features.Add(new ModFeatureToggle { FeatureName = "dialogue_system", Enabled = true });
features.Add(new ModFeatureToggle { FeatureName = "reputation_system", Enabled = true });

// Configure parameters
DynamicBuffer<ModParameter> parameters = GetBuffer(customProfileEntity);
parameters.Add(new ModParameter { Path = "social.reputation_gain_multiplier", Value = 3.0f });
parameters.Add(new ModParameter { Path = "social.charisma_effect_multiplier", Value = 2.0f });
parameters.Add(new ModParameter { Path = "economic.trade_profit_margin", Value = 0.5f });

// Save profile
SaveModProfile(customProfile);

// Profile can be shared with other players, loaded anytime
```

### Example 5: Sandbox Tools - Spawn Custom Entity

```csharp
// Player uses sandbox tools to spawn custom warrior
var spawnRequest = new EntitySpawnRequest
{
    EntityType = "warrior",
    Position = playerClickPosition,

    // Custom parameters
    CustomStats = new EntityStatsOverride
    {
        BaseAccuracy = 0.95f,           // 95% accuracy (high)
        PhysicalStrength = 0.9f,        // Very strong
        InitiativeRating = 0.7f,        // High initiative
        PatienceRating = 0.24f,         // Low patience (auto-calculated if enabled)
        CharismaRating = 0.85f,         // Charismatic leader
        CircadianPattern = SleepPatternType.EarlyBird
    }
};

SpawnEntityWithOverrides(spawnRequest);

// Result: Custom warrior spawned with exact stats specified
// All features (patience, circadian, etc.) respect opt-in settings
// Entity participates in memory tapping, rallies, etc.
```

---

## Modding API

Expose C# API for external mods:

```csharp
public static class PureDotsModdingAPI
{
    // Parameter modification
    public static void SetParameter(string path, float value, bool applyToExisting = false);
    public static float GetParameter(string path);
    public static void ResetParameter(string path);
    public static void ResetAllParameters();

    // Feature toggles
    public static void EnableFeature(string featureName);
    public static void DisableFeature(string featureName);
    public static bool IsFeatureEnabled(string featureName);

    // Entity spawning
    public static Entity SpawnEntity(string entityType, float3 position, EntityStatsOverride stats = default);
    public static void ModifyEntity(Entity entity, EntityStatsOverride stats);
    public static void DeleteEntity(Entity entity);

    // Event hooks
    public static void RegisterOnParameterChanged(string path, Action<float, float> callback);
    public static void RegisterOnFeatureToggled(string feature, Action<bool> callback);
    public static void RegisterOnCombatStart(Action<Entity, Entity> callback);
    public static void RegisterOnEntitySpawned(Action<Entity> callback);

    // Profile management
    public static void LoadProfile(string profileName);
    public static void SaveProfile(string profileName, string description);
    public static void ExportProfile(string profileName, string filePath);
    public static void ImportProfile(string filePath);

    // Sandbox tools
    public static void OpenSandboxTools();
    public static void SetSandboxMode(SandboxToolMode mode);
    public static void ExecuteConsoleCommand(string command);
}
```

---

## Console Commands

Text-based modding interface:

```
# Parameter modification
set combat.damage_multiplier 2.0
set social.patience_depletion_multiplier 0.5
get combat.accuracy.base_disruption_rate
reset combat.*
reset_all

# Feature toggles
enable patience_system
disable combat_enabled
toggle circadian_rhythms
list_features

# Profile management
load_profile "Hardcore"
save_profile "My Custom Config" "Description here"
export_profile "My Custom Config" "C:/mods/custom.json"
import_profile "C:/mods/shared_config.json"
list_profiles

# Entity manipulation
spawn warrior 100,50,0 accuracy=0.95 strength=0.9
modify @selected accuracy=0.5
delete @selected
select_entity warrior_001

# Sandbox tools
sandbox_mode on
open_tweaker
open_spawner
show_debug_overlay resource_distribution
hide_debug_overlay

# Hot reload
hotreload parameters
hotreload features
hotreload all
```

---

## Persistent Configuration

Save/load mod configurations across sessions:

```csharp
public struct PersistentModConfig : IComponentData
{
    public FixedString128Bytes ActiveProfile;
    public bool AutoLoadOnStartup;
    public bool SaveParametersOnExit;
    public bool WarnOnParameterChange;          // Notify player of changes
}

public static class ModConfigPersistence
{
    // Save current configuration
    public static void SaveCurrentConfig(string filePath)
    {
        // Serialize FeatureToggles
        // Serialize all RuntimeParameters
        // Serialize active ModProfile
        // Write to JSON/binary file
    }

    // Load configuration
    public static void LoadConfig(string filePath)
    {
        // Read file
        // Deserialize FeatureToggles
        // Deserialize RuntimeParameters
        // Apply all changes with hot-reload
    }

    // Export for sharing
    public static void ExportShareableConfig(string profileName, string filePath)
    {
        // Human-readable JSON format
        // Include metadata (author, description, version)
        // Validate compatibility
    }
}
```

---

## Performance Considerations

**Hot-Reload Impact**:
```
Parameter Change:         <0.1ms (immediate)
Feature Toggle:           <10ms (add/remove components)
Profile Load:             <50ms (batch changes)
Entity Modification:      <0.05ms per entity
Full Hot-Reload:          <200ms (rebuild affected systems)
```

**Optimizations**:
- Batch parameter changes before applying
- Cache frequently accessed parameters
- Use component enablement for feature toggles (faster than add/remove)
- Defer retroactive changes to next frame
- Compress mod profiles using binary serialization

---

## Summary

**Opt-In Philosophy**:
- All features have toggles (combat, social, economic, environmental)
- Players choose complexity level
- Vanilla experience requires no configuration

**Runtime Modding**:
- All parameters exposed for modification
- Changes apply immediately with hot-reload
- No restart required for most changes
- Sandbox tools provide in-game tweaking UI

**Mod Profiles**:
- Save/load complete configurations
- Share profiles with other players
- Preset profiles for common playstyles
- Import/export to files

**Garry's Mod-Like Freedom**:
- Spawn entities with custom stats
- Modify world parameters in real-time
- Console commands for power users
- C# API for external mods
- Visual tweaker UI for accessibility

**Cross-Game Support**:
- Same modding system for Godgame and Space4X
- Parameters contextualized per game mode
- Shared profiles work across both games
- Mod compatibility validation

**Key Insight**: By making everything opt-in and runtime-moddable, players can craft their ideal experience - from hardcore survival to peaceful creative building - all within the same game framework.
