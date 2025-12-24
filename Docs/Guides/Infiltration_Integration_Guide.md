# Infiltration System Integration Guide

This guide explains how to integrate infiltration and espionage mechanics into game projects (Space4X, Godgame).

## Overview

PureDOTS provides a complete infiltration system that handles:
1. **Infiltration Progress** - Agents build infiltration levels over time (Contact → Embedded → Trusted → Influential → Subverted)
2. **Suspicion Tracking** - Risky activities increase suspicion, which decays over time
3. **Detection** - Counter-intelligence detects infiltrators based on suspicion and cover quality
4. **Intel Gathering** - Agents automatically gather intelligence based on infiltration level
5. **Cover Management** - Cover identities degrade over time and with suspicion
6. **Investigations** - Organizations investigate suspicious agents
7. **Extraction** - Exposed agents can extract with success chance based on plan quality

## Architecture

```
Infiltration Systems:
├─ InfiltrationProgressSystem (updates progress, handles level-ups)
├─ SuspicionTrackingSystem (tracks and decays suspicion)
├─ InfiltrationDetectionSystem (detects exposed agents)
├─ IntelGatheringSystem (gathers intel based on level)
├─ CoverDegradationSystem (degrades cover over time)
├─ InvestigationSystem (organizations investigate suspects)
└─ ExtractionExecutionSystem (executes extraction plans)

Game Integration Points:
├─ Start infiltration (spy band missions, individual assignments)
├─ Record risky activities (theft, sabotage, assassination)
├─ Listen for interrupts (exposure, extraction events)
├─ Query intel (UI, strategy decisions)
└─ Setup counter-intelligence (organizations defend against spies)
```

## Starting Infiltration

### When to Call StartInfiltration

Call `InfiltrationService.StartInfiltration` when:
- A spy band begins an infiltration mission
- An individual spy is assigned to infiltrate an organization
- A double agent begins deep cover work

**Example (Godgame - Spy Band Mission):**
```csharp
// In spy band mission system
var spyLeader = GetSpyBandLeader(bandEntity);
var targetVillage = mission.TargetVillage;

InfiltrationService.StartInfiltration(
    spy: spyLeader,
    targetOrganization: targetVillage,
    method: InfiltrationMethod.Cultural, // Posing as traveler
    coverStrength: CalculateCoverStrength(spyLeader),
    startTick: timeState.Tick,
    ref state);
```

**Example (Space4X - Fleet Infiltration):**
```csharp
// In fleet command system
var spyCarrier = GetSpyCarrier(fleetEntity);
var targetFaction = mission.TargetFaction;

InfiltrationService.StartInfiltration(
    spy: spyCarrier,
    targetOrganization: targetFaction,
    method: InfiltrationMethod.Conscription, // Joined fleet
    coverStrength: 0.8f, // Good cover documents
    startTick: timeState.Tick,
    ref state);
```

### Creating Cover Identity

Before starting infiltration, create a `CoverIdentity` component:

```csharp
// In authoring or mission setup
var cover = new CoverIdentity
{
    CoverName = (FixedString64Bytes)"Marcus the Merchant",
    CoverRole = (FixedString64Bytes)"Trader",
    Credibility = 0.8f, // How believable (0-1)
    Authenticity = 0.75f, // How authentic (0-1)
    Depth = 0.7f, // Backstory detail (0-1)
    CoverEstablishedTick = timeState.Tick,
    CreatedTick = timeState.Tick,
    LastVerifiedTick = timeState.Tick,
    HasDocuments = 1, // Forged credentials
    HasContacts = 1 // Supporting network
};

entityManager.AddComponent(spyEntity, cover);
```

## Recording Risky Activities

### When to Call RecordRiskyActivity

Call `InfiltrationService.RecordRiskyActivity` when spy performs:
- **Theft** (activityRisk: 0.3-0.4) - Stealing items, documents, resources
- **Sabotage** (activityRisk: 0.5-0.7) - Destroying supplies, infrastructure
- **Assassination** (activityRisk: 0.8-0.9) - Killing targets
- **Intel Extraction** (activityRisk: 0.2-0.3) - Accessing secret documents
- **Info Gathering** (activityRisk: 0.1-0.2) - Passive observation, surveillance

**Example (Theft Mission):**
```csharp
// In theft mission completion system
if (theftMission.Success)
{
    // Record risky activity
    InfiltrationService.RecordRiskyActivity(
        spy: spyEntity,
        activityRisk: 0.35f, // Moderate risk
        ref state);

    // Award stolen items
    AwardStolenItems(spyEntity, theftMission.StolenItems);
}
```

**Example (Assassination Mission):**
```csharp
// In assassination system
if (assassinationCompleted)
{
    InfiltrationService.RecordRiskyActivity(
        spy: assassinEntity,
        activityRisk: 0.85f, // Very high risk
        ref state);

    // High suspicion gain - may trigger investigation
}
```

## Listening for Interrupts

### Interrupt Types

The infiltration system emits these interrupts:

- `InterruptType.InfiltrationExposed` - Agent's cover blown, extraction needed
- `InterruptType.InfiltrationExtractionStarted` - Extraction plan activated
- `InterruptType.InfiltrationExtractionCompleted` - Agent successfully extracted
- `InterruptType.InfiltrationExtractionFailed` - Agent captured
- `InterruptType.IntelGathered` - New intelligence gathered (low priority)

**Example (Interrupt Handler):**
```csharp
[BurstCompile]
[UpdateInGroup(typeof(InterruptSystemGroup))]
public partial struct InfiltrationInterruptHandler : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (interrupts, entity) in
            SystemAPI.Query<DynamicBuffer<Interrupt>>().WithEntityAccess())
        {
            for (int i = 0; i < interrupts.Length; i++)
            {
                var interrupt = interrupts[i];
                if (interrupt.IsProcessed != 0)
                    continue;

                switch (interrupt.Type)
                {
                    case InterruptType.InfiltrationExposed:
                        HandleExposure(entity, interrupt, ref state);
                        interrupt.IsProcessed = 1;
                        interrupts[i] = interrupt;
                        break;

                    case InterruptType.InfiltrationExtractionFailed:
                        HandleCapture(entity, interrupt, ref state);
                        interrupt.IsProcessed = 1;
                        interrupts[i] = interrupt;
                        break;

                    case InterruptType.IntelGathered:
                        HandleIntelGathered(entity, interrupt, ref state);
                        interrupt.IsProcessed = 1;
                        interrupts[i] = interrupt;
                        break;
                }
            }
        }
    }

    private void HandleExposure(Entity spy, Interrupt interrupt, ref SystemState state)
    {
        // Trigger diplomatic consequences
        var infiltration = SystemAPI.GetComponent<InfiltrationState>(spy);
        var targetOrg = infiltration.TargetOrganization;
        
        // Damage relations
        DamageRelations(spy, targetOrg, -50);
        
        // Notify mission system
        NotifyMissionFailed(spy, "Cover blown");
    }
}
```

## Querying Gathered Intel

### Reading Intel Buffer

Query the `GatheredIntel` buffer to display intel in UI or make strategy decisions:

```csharp
// In UI system or strategy AI
if (SystemAPI.HasBuffer<GatheredIntel>(spyEntity))
{
    var intelBuffer = SystemAPI.GetBuffer<GatheredIntel>(spyEntity);
    
    foreach (var intel in intelBuffer)
    {
        if (intel.IsStale != 0)
            continue; // Skip stale intel

        switch (intel.Type)
        {
            case IntelType.Military:
                // Display military intel (troop counts, positions)
                DisplayMilitaryIntel(intel);
                break;
            case IntelType.Economic:
                // Display economic intel (resources, trade routes)
                DisplayEconomicIntel(intel);
                break;
            case IntelType.Technological:
                // Display tech intel (research, blueprints)
                DisplayTechIntel(intel);
                break;
        }
    }
}
```

### Intel Availability by Level

- **Contact**: Military intel only (public data)
- **Embedded**: Military, Economic, Political (local intel)
- **Trusted**: + Social (communications intercept)
- **Influential**: + Technological (secret access)
- **Subverted**: All types (command-level access)

## Setting Up Counter-Intelligence

### Adding CounterIntelligence to Organizations

Organizations (villages, guilds, factions) need `CounterIntelligence` to detect spies:

```csharp
// In organization authoring or setup
var counterIntel = new CounterIntelligence
{
    DetectionRate = 0.01f, // Base detection chance per tick
    SuspicionGrowth = 0.05f, // How fast suspicion builds
    SuspicionDecayRate = 0.01f, // Natural suspicion decay
    InvestigationPower = 0.5f, // Effectiveness of investigations
    SecurityLevel = 5, // 0-10 overall security tier
    LastSweepTick = 0,
    ActiveMeasures = 0 // Active counter-intel operations
};

entityManager.AddComponent(villageEntity, counterIntel);
```

**Security Level Guidelines:**
- 0-2: Low security (easy to infiltrate)
- 3-5: Medium security (standard)
- 6-8: High security (difficult infiltration)
- 9-10: Maximum security (very difficult, frequent sweeps)

## Extraction Planning

### Creating Extraction Plans

When infiltration starts, create an extraction plan:

```csharp
// In mission setup or when infiltration begins
var extractionPlan = new ExtractionPlan
{
    SafeHouseEntity = safeHouseEntity, // Where to flee
    ExfilContactEntity = contactEntity, // Who helps extract
    ExtractionPoint = safeHousePosition,
    ExfilPosition = safeHousePosition,
    SuccessChance = 0f, // Calculated by system
    PlanQuality = 7, // 0-10, how well planned
    PlannedExtractionTick = timeState.Tick + 1000, // When to extract
    Status = ExtractionStatus.Planned,
    IsActivated = 0
};

entityManager.AddComponent(spyEntity, extractionPlan);
```

### Emergency Extraction

When agent is exposed, trigger emergency extraction:

```csharp
// In interrupt handler or detection system
if (infiltration.IsExposed != 0 && infiltration.IsExtracting == 0)
{
    InfiltrationService.Extract(
        spy: spyEntity,
        targetOrganization: infiltration.TargetOrganization,
        extractionPoint: nearestSafeHouse,
        exfilContact: contactEntity,
        planQuality: 5, // Lower quality for emergency
        plannedTick: timeState.Tick,
        ref state);
}
```

## Integration Checklist

### For Spy Band Missions (Godgame)

- [ ] Call `StartInfiltration` when spy band begins infiltration mission
- [ ] Create `CoverIdentity` for spy leader before infiltration
- [ ] Call `RecordRiskyActivity` when band performs theft/sabotage/assassination
- [ ] Listen for `InfiltrationExposed` interrupt to trigger extraction
- [ ] Query `GatheredIntel` buffer for mission reports
- [ ] Handle `InfiltrationExtractionFailed` interrupt (agent captured)

### For Fleet Infiltration (Space4X)

- [ ] Call `StartInfiltration` when spy carrier joins enemy fleet
- [ ] Set `CounterIntelligence` on faction entities
- [ ] Call `RecordRiskyActivity` when spy performs tech theft or sabotage
- [ ] Listen for intel interrupts to update intel database
- [ ] Query intel for fleet composition and strategy decisions
- [ ] Handle extraction interrupts for diplomatic consequences

## Performance Notes

- Infiltration systems are throttled (progress every 10 ticks, suspicion every 5 ticks, detection every 20 ticks)
- Intel gathering runs every 50 ticks
- Cover degradation runs every 100 ticks
- Investigations run every 30 ticks
- All systems respect `RewindState` and skip during playback

## Related Systems

- **Deception System**: `DisguiseIdentity` works alongside `CoverIdentity`
- **Relations System**: Exposure damages relations between spy employer and target
- **Communication System**: Intercepted communications increase suspicion
- **Reputation System**: Cover credibility affects reputation checks

## Example: Complete Infiltration Mission Flow

```csharp
// 1. Mission Setup
var spy = CreateSpy();
var targetVillage = GetTargetVillage();

// Create cover identity
var cover = new CoverIdentity { /* ... */ };
entityManager.AddComponent(spy, cover);

// Start infiltration
InfiltrationService.StartInfiltration(
    spy, targetVillage, InfiltrationMethod.Cultural, 0.8f, timeState.Tick, ref state);

// 2. Mission Execution (over time)
// InfiltrationProgressSystem automatically increases level
// IntelGatheringSystem automatically gathers intel

// When spy performs theft:
InfiltrationService.RecordRiskyActivity(spy, 0.35f, ref state);

// 3. Exposure Handling
// InfiltrationDetectionSystem detects exposure, emits interrupt
// Interrupt handler triggers extraction:
InfiltrationService.Extract(spy, targetVillage, safeHouse, contact, 7, timeState.Tick, ref state);

// 4. Intel Usage
var intelBuffer = SystemAPI.GetBuffer<GatheredIntel>(spy);
// Use intel for strategy decisions, UI display, etc.
```

## Troubleshooting

**Suspicion stays at zero:**
- Ensure `CounterIntelligence` component exists on target organization
- Call `RecordRiskyActivity` when spy performs actions
- Check that `SuspicionTrackingSystem` is running

**Intel not gathering:**
- Verify infiltration level is above `Contact`
- Check that `IntelGatheringSystem` is running
- Ensure `GatheredIntel` buffer exists (created automatically)

**Cover not degrading:**
- Verify `CoverIdentity` component exists on spy
- Check that `CoverDegradationSystem` is running
- Cover degrades slowly (every 100 ticks)

**Investigations not starting:**
- Ensure `CounterIntelligence` exists on organization
- Suspicion must be > 0.5 to trigger investigation
- Check that `InvestigationSystem` is running



