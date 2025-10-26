# Miracles Framework TODO (BW2 Parity)

> **Generalisation Guideline**: Treat miracle casting as a reusable effect system. Miracle behaviours should be defined through data (profiles, payload configs) rather than game-specific code branches.

## Goal
- Build a reusable miracles framework matching Black & White 2 behaviour: miracles are throwable (token) or sustained (RMB hold) hand interactions with deterministic area effects.
- Support many miracle types sharing the same lifecycle template (pickup → aim → release/hold → effect) with configuration-driven payloads.
- Ensure miracles integrate with existing DOTS systems (hand router, registries, spatial grid, rewind) and are easy for designers to author/tune.
- Keep alignment with `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and integration guidance in `Docs/TODO/SystemIntegration_TODO.md`.

## Plain-Language Primer
- Miracles are “spells” the player can cast via the divine hand. In BW2, you pick up glowing tokens (one-time effects) or hold RMB to sustain an effect around the hand.
- Common flow: acquire miracle → aim/target → release/maintain → apply effect on terrain/entities → consume tokens/prayer power → log history for rewind.
- We need a single DOTS template that handles targeting, casting, effects, and cleanup so adding new miracles is mostly data/config.

## Reference Behaviour (BW2)

### Casting Flow
1. **Gesture Recognition**: Player draws symbol with mouse (e.g., circle for heal, zigzag for lightning)
2. **Mana Check**: System verifies sufficient prayer power/mana available
3. **Token Creation**: Miracle appears as glowing token/orb in divine hand
4. **Charge/Hold**: Hold RMB to charge miracle (increases potency/area)
5. **Delivery**: Release to throw token, or sustain by holding over target area

### Miracle Categories
**Offensive Miracles** (Instant Impact):
- Fireball, Lightning, Meteor, Volcano - damage, destruction, fire

**Defensive Miracles** (Sustained/Buff):
- Shield, Heal, Forest - protection, restoration, growth

**Environmental Miracles** (Area Effects):
- Rain, Earthquake, Freeze, Tornado - terrain modification, weather

**Utility Miracles**:
- Teleport, Flock of Birds, Food/Wood - movement, scouting, resources

### Power Levels (BW2 System)
- **Basic**: Low cost, small area, weak effect (Level 1)
- **Increased**: Medium cost (1.5x), larger area (1.5x), stronger effect (2.0x intensity)
- **Extreme**: High cost (2.5x), massive area (2.5x), devastating effect (3.5x intensity)

**Charge Mechanic**:
- Hold miracle token to charge (0-100%)
- Charge time determines power level: <30% = Basic, 30-70% = Increased, >70% = Extreme
- Visual feedback: Token glows brighter, pulses faster, grows larger

### Token & Sustained Mechanics
- **Token miracles** (Fireball, Lightning, Boulder): pick up token → throw → impact area-of-effect.
- **Sustained miracles** (Water, Food, Shield): hold RMB → energy flows from hand into area; effect persists while holding, drains prayer.
- **Area targeting**: ring highlight showing radius; effect strength falls off from center.
- **Prayer Power cost**: casting consumes prayer; sustained miracles drain over time.
- **Alignment effects**: some miracles push alignment to good/evil based on usage.
- **Feedback**: audio/visual cues, charging glow, area decals, cooldown indicators.

## Workstreams & Tasks

### 0. Recon & Requirements Gathering
- [ ] Review existing rain miracle, divine hand, and RMB router to identify reusable patterns.
- [ ] Catalogue BW2 miracle types (token vs. sustained) and categorize effects (damage, heal, growth, weather, projectile).
- [ ] Document legacy truth-source expectations (alignment impact, prayer costs, targeting rules).
- [ ] Identify data/assets needed (visuals, effect curves, audio) and note missing infrastructure.
- [ ] Confirm rewind behaviour for miracles (history events, deterministic effect reapplication).
- [x] Audit current miracle systems for ad-hoc pooling (ECB allocations, instantiates) and map to shared `Nx.Pooling` plan.

### 1. Core Architecture & Data
- [ ] **Define MiracleProfile ScriptableObject** with all parameters:
  - MiracleType enum (Rain, Fire, Heal, Shield, Lightning, etc.)
  - DisplayName, Icon, Description
  - BaseCost, CostPerSecond (for sustained types)
  - Gesture pattern (point array for recognition)
  - Charge time thresholds (ChargeTimeBasic, ChargeTimeIncreased, ChargeTimeExtreme)
  - Effect prefab references (VFX, projectile, area effect)
  - Behavior flags (IsThrowable, IsSustained, IsInstant)
  - Target filter (Ground, Units, Buildings, All)
  - Area of effect curves (radius by power level: Basic/Increased/Extreme arrays)
  - Damage/heal amounts by power level (intensity arrays)
  - Special parameters (duration, travel speed, chain count, etc.)
- [ ] **Build MiracleCatalogBlob** for runtime access:
  - BlobArray of MiracleDefinitionBlob structs
  - Each definition contains all profile data in Burst-friendly format
  - Include Entity references to TokenPrefab and EffectPrefab
- [ ] **Add shared miracle components** in `MiracleComponents.cs`:
  - `MiracleCatalog` - Singleton with BlobAssetReference to catalog
  - `MiracleToken` - Token entity: MiracleTypeId, PowerLevel (0-2), ChargePercent, OwnerHand, CreationTick, ManaInvested, BehaviorFlags
  - `MiracleEffect` - Effect instance: MiracleTypeId, PowerLevel, EffectCenter, EffectRadius, Duration, Intensity, StartTick, RemainingDuration, TargetMask
  - `MiracleState` - Per-token state enum (Inactive, Held, Thrown, Active, Depleted) with StateTimer, Velocity, Target
- [ ] Plan for pooled miracle token/effect entities using shared pooling utilities.
- [ ] Review high-frequency miracle data for SoA compliance (separate arrays for position/intensity/state as needed).
- [ ] Express miracle behaviours (damage, heal, growth, weather) via payload data definitions to keep the runtime agnostic to game theme.
- [ ] **Add payload components** in `MiraclePayloads.cs` (per miracle type):
  - `FireballPayload` - ExplosionRadius, BurnDuration, IgnitionChance
  - `HealPayload` - HealPerSecond, MaxHealPerTarget, TargetTypes
  - `ShieldPayload` - AbsorptionAmount, ShieldDuration, ShieldedEntity
  - `LightningPayload` - ChainCount, ChainRadius, DamagePerBolt
  - (Add more as miracle types expand)
- [ ] **Create PrayerPower/Mana system** in `PrayerPowerComponents.cs`:
  - `PrayerPower` singleton - CurrentMana, MaxMana, RegenRate
  - `PrayerPowerSystem` - Handles regeneration from worship, buildings
- [ ] Update authoring/bakers to convert profiles into runtime blobs.
- [x] Provide pooling configuration for miracle token/effect prefabs via shared pooling settings (prewarm counts per miracle type).

### 2. Input & Casting Flow
- [ ] **Update RMB router priority** for miracles:
  1. UI elements (always win)
  2. **Active miracle gesture** (blocks other actions)
  3. **Miracle channeling** (sustained delivery)
  4. Modal tools
  5. Storehouse dump
  6. Pile siphon
  7. Object grab
  8. Ground drip
- [ ] **Extend divine hand states** (add to HandState enum):
  - `CastingMiracle` - Gesture recognition active
  - `HoldingMiracle` - Miracle token in hand, charging
  - `ChannelingMiracle` - Sustained miracle active (rain, heal)
- [x] Reference shared `HandInputRouterSystem` and `HandInteractionState` from integration TODO once centralised router lands.
- [ ] Integrate miracles with RMB router: add handlers for token pickup, sustain hold, release actions.
- [ ] **Implement core miracle systems**:
  - `MiracleTokenSpawnSystem` - Creates tokens from gesture/hotkey commands
  - `MiracleChargeSystem` - Updates charge percent, determines power level (0=Basic, 1=Increased, 2=Extreme)
  - `MiracleThrowSystem` - Handles token physics when thrown (reuses hand throw logic)
  - `MiracleImpactSystem` - Triggers effect on collision/landing
  - `MiracleSustainSystem` - Manages continuous effects, drains mana per second
  - `MiracleEffectCleanupSystem` - Removes expired effects and tokens
- [ ] Add charge mechanic for throwables (hold duration modifies power).
- [ ] Ensure deterministic transitions (state machine with distinct states: Idle, HoldingToken, Charging, Sustaining, Cooldown).
- [ ] Provide UI/HUD hooks for miracle selection, cost, charge meter.
- [x] Replace transient `NativeQueue`/ECB usage in miracle systems with pooled equivalents (borrowed + returned each tick).

## Dependencies & Shared Infrastructure
- Sample environment grids (sunlight, temperature, wind, moisture, solar radiation) through shared helpers to drive miracle payloads (rain, fire, shield heat dissipation).
- Use spatial grid registry entries for miracle targeting (neutral resource registry, villager registry, miracle neutral registry).
- When miracles spawn autonomous agents (rain clouds, shields, drones), wire them through the shared `AISystemGroup` modules so sensors/utility/steering reuse the core pipeline instead of bespoke logic.
- Follow central hand/router contracts (`HandInputRouterSystem`, `HandInteractionState`, `RmbContext`) and rewind guards when adding new miracle handlers.

### 2.5. Gesture Recognition (NEW)
- [ ] **Implement GestureRecognitionSystem**:
  - Capture mouse points while RMB held (sample every 0.05s)
  - Normalize path to 0-1 coordinates
  - Match against stored patterns (dot product similarity or distance matching)
  - Threshold >0.85 = recognized, spawn appropriate miracle token
  - Fail <0.85 = deny feedback, consume partial mana
- [ ] **Define gesture patterns** (2D mouse paths):
  - Rain: Wavy horizontal line (~~~~)
  - Fire: Sharp zigzag (^^^)
  - Heal: Circle (O)
  - Lightning: Vertical zigzag (|/|\|)
  - Shield: Square box (□)
  - Earthquake: Spiral inward (spiral)
- [ ] **Add simplified alternative for Phase 1**:
  - Hotkey shortcuts: Rain=R, Fire=F, Heal=H, Shield=S (testing/accessibility)
  - Full gestures: Implement later once core framework stable
  - Allow both methods (config toggle)
- [ ] **Visual feedback**: Ghost trail showing mouse path, recognition success/fail indicator.

### 3. Targeting & Area Display
- [ ] Implement `MiracleTargetingSystem`:
  - Compute target position under cursor (terrain raycast).
  - Clamp to valid areas (within influence ring, allowed terrain types).
  - Update area highlight (mesh or gizmo) with radius/falloff visual.
  - Handle sustained miracles tethered to hand position vs. token projectiles.
- [ ] Integrate with spatial grid to find affected entities/terrain cells.
- [ ] Add configurable targeting filters (include/exclude villages, enemies, vegetation).

### 4. Effect Processing Framework
- [ ] Create effect processors using ECS systems:
  - `MiracleDamageSystem` (damage/ignite entities within radius).
  - `MiracleHealSystem` (heal villagers/creatures).
  - `MiracleGrowthSystem` (boost vegetation growth/moisture).
  - `MiracleSpawnSystem` (spawn clouds, tokens, creatures).
  - `MiracleWeatherSystem` (change climate, start rainfall).
  - `MiracleShieldSystem` (apply buffs to entities within area).
- [ ] Effects triggered via `MiracleEffectPayload` buffer entries (deterministic command pattern).
- [ ] Support timed effects (sustain) vs. instant pulses.
- [ ] Ensure all effects record history events for rewind; replay regenerates same results.

### 5. Tokens & Resource Economy
- [ ] Implement token spawning system (e.g., from shrines, villagers).
- [ ] Define prayer resource component (global pool) and integrate with casting costs.
- [ ] Add cooldown manager (per miracle) preventing immediate recast.
- [ ] Track alignment shifts per use; update alignment system accordingly.
- [ ] Provide analytics (miracles cast, prayer spent, alignment impact).

### 6. Presentation & Feedback
- [ ] Integrate with presentation bridge for:
  - Area highlights (projection, glow)
  - Casting hand VFX (charging, sustained beam)
  - Impact effects (explosion, rain, shield bubble)
  - Audio cues (start, loop, end)
- [ ] Provide config for designers to assign VFX/SFX per miracle.
- [ ] Add HUD indicators (miracle selection wheel, resource cost display).

### 7. Testing & Validation
- [ ] **Unit tests** in `MiracleFrameworkTests.cs`:
  - Gesture pattern matching (known mouse paths → expected miracle recognition)
  - Mana cost calculations (power level → correct mana deduction)
  - Area effect radius calculations (power level → area size)
  - Token physics (deterministic throw trajectory reproduction)
  - Effect application logic (correct entities affected by target mask)
  - Charge threshold logic (charge time → power level assignment)
- [ ] **Playmode tests**:
  - Full miracle flow (gesture/hotkey → charge → throw → impact → effect → cleanup)
  - Sustained miracle (hold → channel → mana drain → release → cleanup)
  - Mana depletion and regeneration over time
  - Multiple simultaneous effects (no conflicts, deterministic ordering)
  - Spatial query accuracy (affects correct entities within radius)
  - Power level scaling (Basic vs. Increased vs. Extreme all work correctly)
- [ ] **Rewind tests** in `MiracleRewindTests.cs`:
  - Record miracle cast → rewind to before cast → verify state matches
  - Active effect during rewind → restores correctly with remaining duration
  - Gesture recognition deterministic on replay (if implemented)
  - Token physics identical on replay (same trajectory, impact point)
  - Multiple miracles in sequence replay correctly
- [ ] **Performance tests**:
  - 10 simultaneous miracle effects with 10k entities (measure frame time)
  - Gesture recognition latency (<50ms from complete to recognition)
  - Effect application time (<1ms for 100k entities with spatial grid)
  - Memory footprint (zero GC allocations per frame)
  - Token spawning overhead (should be negligible)
- [ ] Alignment impact tests (good vs. evil uses).

### 8. Tooling & Designer Workflow
- [ ] Miracle authoring inspector (ScriptableObject) with validation for cost/effect settings.
- [ ] In-editor preview for area highlight and effect radius.
- [ ] Debug overlay showing active miracles, area overlays, prayer drain.
- [ ] Documentation updates (new `Docs/DesignNotes/MiraclesFramework.md`, update SceneSetup guides).

## Miracle Types (Initial Parity Set)
1. **Fireball (Token)** – projectile damage + ignite.
2. **Lightning (Token)** – instant strike, chain damage.
3. **Water/Rain (Sustain)** – hydrates area, extinguish fires.
4. **Food (Sustain)** – spawns food resources.
5. **Shield (Sustain)** – protective bubble reducing damage.
6. **Heal (Token/Sustain)** – heal villagers/creatures.
7. **Boulder (Token)** – throw rock causing impact damage.
8. **Disaster (Token)** – meteor/hurricane (alignment penalty).
9. **Earthquake (Token)** - screen shake, structural damage, unit panic.
10. **Forest (Instant)** - instantly grows trees in area.
11. **Freeze (Token)** - slow enemy movement, ice terrain.
12. **Tornado (Token)** - moving vortex that picks up objects.

## Configuration Reference (BW2 Values)

### Mana Costs (Basic Level)
```
Rain: 100 mana (initial) + 10/s sustained
Fireball: 150 mana instant
Heal: 80 mana (initial) + 15/s sustained
Shield: 120 mana (initial) + 20/s sustained
Lightning: 200 mana instant
Earthquake: 250 mana instant
Forest: 180 mana instant
Food/Wood: 100 mana instant
Meteor: 300 mana instant
Freeze: 140 mana instant
```

### Power Level Multipliers
```
Basic (0-1s charge): 1.0x cost, 1.0x area, 1.0x intensity
Increased (1-2.5s): 1.5x cost, 1.5x area, 2.0x intensity
Extreme (2.5-4s): 2.5x cost, 2.5x area, 3.5x intensity
```

### Charge Times & Thresholds
```
Basic: 0-1 second (0-30% charge)
Increased: 1-2.5 seconds (30-70% charge)
Extreme: 2.5-4 seconds (70-100% charge)
```

### Effect Durations
```
Sustained miracles (rain, heal, shield): Until mana depletes or player cancels
Temporary effects (burn, freeze, buff): 10-30 seconds
Instant miracles (fireball, lightning): Immediate impact, no duration
Area denial (earthquake rubble): 60-120 seconds
```

### Effect Areas (Basic Level)
```
Fireball: 8m radius
Heal: 10m radius
Shield: 12m radius (single building)
Lightning: 5m radius (initial), 3m chain radius
Earthquake: 20m radius
Forest: 15m radius (spawns 5-10 trees)
Rain: 25m radius per cloud
```

## File Structure
```
Assets/Scripts/PureDOTS/
  Runtime/
    MiracleComponents.cs         (EXPAND - add catalog, token, effect, state)
    MiraclePayloads.cs          (NEW - payload structs per miracle type)
    PrayerPowerComponents.cs    (NEW - mana system components)
  
  Systems/
    MiracleTokenSpawnSystem.cs         (NEW - creates tokens from gestures/commands)
    MiracleChargeSystem.cs             (NEW - charge management, power level determination)
    MiracleThrowSystem.cs              (NEW - token physics when thrown)
    MiracleImpactSystem.cs             (NEW - triggers effects on collision)
    MiracleSustainSystem.cs            (NEW - sustained effect management)
    MiracleEffectCleanupSystem.cs      (NEW - cleanup expired effects/tokens)
    MiracleGestureRecognitionSystem.cs (NEW - optional gesture matching)
    
    Effects/
      FireballEffectSystem.cs          (NEW - explosive damage + fire)
      HealEffectSystem.cs              (NEW - area healing)
      ShieldEffectSystem.cs            (NEW - damage absorption buff)
      LightningEffectSystem.cs         (NEW - chain damage)
      EarthquakeEffectSystem.cs        (NEW - structural damage, panic)
      ForestEffectSystem.cs            (NEW - instant tree spawning)
      FreezeEffectSystem.cs            (NEW - movement slow, ice)
      FoodEffectSystem.cs              (NEW - resource spawning)
      RainEffectSystem.cs              (REFACTOR existing rain miracle)
  
  Authoring/
    MiracleProfileAsset.cs       (NEW - ScriptableObject definition)
    MiracleCatalogAuthoring.cs   (NEW - blob baker for catalog)
  
  Config/
    MiracleCatalog.cs            (NEW - catalog asset container)
  
  Tests/
    MiracleFrameworkTests.cs     (NEW - unit tests)
    GestureRecognitionTests.cs   (NEW - gesture pattern tests)
    MiracleRewindTests.cs        (NEW - determinism validation)
```

## Open Questions
1. Should gestures be required at launch, or hotkeys sufficient (accessibility concern)?
2. How many miracle types at launch minimum (8? 12? full BW2 set of 15+)?
3. Do creatures cast miracles, or player-only system?
4. Should miracles have cooldowns beyond mana cost (prevent spam)?
5. Can miracles be interrupted/canceled mid-charge (return partial mana)?
6. Do we need combo miracles (combine two types for special effect)?
7. Should there be miracle tiers/unlocks (progression system)?
8. How do miracles interact with each other (fire + rain = steam/cancel)?
9. Should alignment shifts be per-cast or cumulative over time?
10. Do we need miracle "charges" (limited uses) or pure mana-based system?
11. How are miracle tokens created/supplied (shrines, villagers, auto-regeneration)? Are they finite?
12. Do we allow multiple sustained miracles to overlap in one area, or enforce exclusivity per type?
13. Must certain miracles consume physical resources (wood, ore, animals) in addition to prayer, and how is that routed through the resource framework?
14. What are the global limits on simultaneously active miracles (per type vs. global cap)?
15. How is prayer generation/regeneration exposed to players (UI, metrics) and what happens when the pool is empty?
16. Should gestures support accessibility alternatives (controller, keyboard shortcuts) at parity launch?
17. Do miracles permanently alter terrain/biome state (e.g., forest miracle planting trees) or produce temporary effects only?
18. How do miracles interact with creatures—can creatures learn/cast them, or are they player-only?
19. Do we need multiplayer/network hooks (authoritative casting, anti-cheat) for future modes?
20. How are miracle cooldowns communicated (UI, token glow) and can players queue casts during cooldown?

## Dependencies & Links
- **Divine hand system** (see `DivineHandCamera_TODO.md`) - holding, throwing, channeling states
- **Spatial grid** (see `SpatialServices_TODO.md`) - target queries, area effect entity selection
- **Vegetation systems** (see `VegetationSystems_TODO.md`) - rain/growth miracle integration
- **Prayer power/worship system** - mana source and regeneration (future system)
- **VFX/presentation layer** - visual feedback, particle effects, audio
- **Rewind system** - history buffers, deterministic replay
- **Villager/resource systems** - affected by miracle effects (heal, damage, spawn)
- **Registry rewrite** - efficient entity lookups for targeted miracles
- **Alignment/prayer economy** - moral consequences, resource costs

## BW2 Parity Checklist
- [ ] Miracles activated by mouse gestures or hotkeys
- [ ] Tokens appear in divine hand as glowing orbs
- [ ] Charging increases power level (Basic/Increased/Extreme with visual feedback)
- [ ] Throwable tokens fly in arc, impact at landing point
- [ ] Sustained miracles follow hand cursor while held
- [ ] Mana/prayer power costs and regeneration system
- [ ] Visual feedback clear for charge level and effect area (ring highlights)
- [ ] Multiple miracles available (8+ types minimum)
- [ ] Effects scale with power level (area, intensity, duration multiply)
- [ ] Miracles integrate with gameplay (damage enemies, heal allies, spawn resources, modify terrain)
- [ ] Alignment shifts based on miracle usage (good vs. evil)
- [ ] Token pickup/drop works like BW2 (can cancel by dropping)

## Success Criteria

### BW2 Parity Achieved When:
- Designer confirms gestures (or hotkeys) feel responsive like BW2
- Charge mechanic matches BW2 timing/feedback (visual glow, pulse)
- Throwing tokens has same arc feel as BW2 objects
- Effects produce expected gameplay results (correct damage, healing, spawning)
- Visual/audio feedback matches BW2 quality and impact

### Template System Complete When:
- 8+ miracle types implemented and working at all 3 power levels
- Adding new miracle takes <4 hours following template pattern
- All miracles share core code paths (no duplication)
- Configuration drives behavior (no hardcoded values in systems)
- Documentation clear enough for designer to create miracle without engineer

### Ready for Release When:
- All Workstreams 0-8 tasks complete
- Rewind works flawlessly with all miracle types (deterministic replay)
- Performance targets met (60 FPS with 10+ simultaneous active miracles)
- Mana system balanced (costs feel fair, regeneration appropriate)
- Designer documentation complete (authoring guide, tuning reference)
- Integration with divine hand seamless (no state machine bugs)
- Spatial grid integration working (area queries scale to 100k entities)

## Implementation Phases & Estimates

### Phase 1: Core Miracle Framework (2 weeks)
**Goal**: Template system with 2-3 miracle types working

**Tasks**:
- Design `MiracleProfile` ScriptableObject with all shared parameters
- Create `MiracleCatalog` blob builder system
- Implement `MiracleToken` and `MiracleEffect` components
- Build `MiracleTokenSpawnSystem` (creates tokens from commands)
- Build `MiracleChargeSystem` (updates charge, determines power level)
- Implement `MiracleThrowSystem` (physics for thrown tokens)
- Implement `MiracleImpactSystem` (triggers effect on collision)
- Add divine hand states: `HoldingMiracle`, `ChannelingMiracle`, `CastingMiracle`
- Test with Rain (existing, refactored) and Heal (new simple miracle)

**Validation**: Two miracle types work end-to-end (spawn → charge → throw → impact → effect)

### Phase 2: Gesture Recognition (1-2 weeks)
**Goal**: Mouse pattern matching OR hotkey system for Phase 1

**Tasks**:
- Implement gesture capture system (mouse path recording)
- Build pattern matching algorithm (normalized path comparison)
- Create gesture library for all miracle types
- Add visual feedback (ghost trail, recognition success/fail)
- Integrate with divine hand RMB hold (gesture mode)
- Add hotkey fallback for testing/accessibility

**Validation**: 90%+ gesture recognition accuracy, feels responsive (OR hotkeys work perfectly)

### Phase 3: Mana/Prayer Power System (1 week)
**Goal**: Resource management for miracles

**Tasks**:
- Create `PrayerPower` singleton (current mana, max, regen rate)
- Implement `PrayerPowerSystem` (regeneration from worship, buildings)
- Add mana cost deduction on miracle cast (check before spawning token)
- Add mana drain for sustained miracles (per second cost)
- Create UI bridge for mana meter display
- Add "insufficient mana" feedback (visual/audio deny)

**Validation**: Mana costs balance gameplay, regeneration feels fair

### Phase 4: Effect Template Expansion (2-3 weeks)
**Goal**: Implement 6-8 core miracle types using template

**Tasks**:
- **Fireball**: Throwable explosive with area damage, fire entity spawning
- **Heal**: Sustained area heal for villagers/creatures
- **Shield**: Buff that absorbs damage for buildings/units
- **Lightning**: Instant strike with chain damage logic (spatial grid neighbor traversal)
- **Earthquake**: Screen shake, structural damage, unit panic/flee behavior
- **Forest**: Instant tree spawning in area (integration with vegetation spawn)
- **Freeze**: Slow enemy movement, ice terrain visual effect
- **Food**: Spawn food piles for villagers

Each miracle follows template pattern:
- Create `[Type]EffectSystem` implementing template interface
- Add `[Type]Payload` component for specialized data
- Configure `MiracleProfile` asset with all parameters
- Test at all 3 power levels (Basic/Increased/Extreme)

**Validation**: All miracles work correctly at all 3 power levels, no crashes

### Phase 5: Spatial Integration (1 week)
**Goal**: Efficient target selection and area queries

**Tasks**:
- Miracles query spatial grid for affected entities (no linear scans)
- Area effects use radius queries with target mask filtering
- Chain effects (lightning) use spatial neighbor traversal
- Shield placement checks building/unit proximity
- Teleport validates target location occupancy
- Optimize for 100k+ entities

**Validation**: Miracles scale to 100k entities without performance degradation

### Phase 6: Rewind & Determinism (1 week)
**Goal**: Perfect rewind compatibility

**Tasks**:
- Add miracle state to history buffers (MiracleHistorySample)
- Implement snapshot/restore for active effects
- Test record → rewind → replay cycles
- Validate gesture recognition deterministic (if implemented)
- Ensure token physics deterministic (reproducible trajectories)
- Fix any floating-point drift in effect calculations
- Verify spatial grid queries return same results on replay

**Validation**: Rewind replays miracles identically (pixel-perfect on replay)

### Phase 7: VFX & Polish (1-2 weeks)
**Goal**: BW2-quality visual/audio feedback

**Tasks**:
- Token VFX (glowing orbs, power level glow intensity)
- Impact VFX (explosions, healing auras, shield domes)
- Gesture trail rendering (mouse path visualization)
- Charge indicators (pulse, color shift, size scaling)
- Audio integration (cast sound, impact, sustain loop, deny feedback)
- Camera shake for powerful miracles (earthquake, meteor)
- Area highlight decals (ring showing effect radius)

**Validation**: Miracles feel impactful and satisfying to cast

### Phase 8: Configuration & Balancing (1 week)
**Goal**: Designer-friendly tuning

**Tasks**:
- Create `MiracleProfile` assets for all miracle types
- Expose all parameters in inspector (costs, areas, intensities, durations)
- Add power level curves (AnimationCurve for area/intensity vs. charge)
- Build miracle test scene (spawn miracle tokens directly for testing)
- Document configuration workflow for designers
- Balance costs, cooldowns, effects through playtesting

**Validation**: Designers can create and tune miracles without code changes

**Total Estimated Time:** 9-12 weeks for complete BW2 parity + 8-12 miracle types

## System Template Pattern (For New Miracles)

All miracle effect systems follow this pattern for consistency:

```csharp
[BurstCompile]
[UpdateInGroup(typeof(MiracleEffectSystemGroup))]
public partial struct [MiracleType]EffectSystem : ISystem
{
  public void OnUpdate(ref SystemState state)
  {
    var timeState = SystemAPI.GetSingleton<TimeState>();
    var rewindState = SystemAPI.GetSingleton<RewindState>();
    
    // Skip during playback
    if (rewindState.Mode != RewindMode.Record)
      return;
    
    // 1. Query active effects of this type
    foreach (var (effect, payload, entity) in 
             SystemAPI.Query<RefRW<MiracleEffect>, RefRO<[Type]Payload>>()
                      .WithEntityAccess()
                      .Where(e => e.MiracleTypeId == MiracleType.[Type]))
    {
      // 2. Query affected entities using spatial grid
      var targets = SpatialQuery.GetEntitiesWithinRadius(
        effect.EffectCenter, 
        effect.EffectRadius,
        effect.TargetMask);
      
      // 3. Apply miracle-specific logic (damage, heal, buff, spawn, etc.)
      foreach (var target in targets)
      {
        Apply[Type]Effect(target, effect.Intensity, payload);
      }
      
      // 4. Update duration, mark for cleanup if expired
      effect.ValueRW.RemainingDuration -= timeState.FixedDeltaTime;
      if (effect.ValueRO.RemainingDuration <= 0)
      {
        // Mark for cleanup or destroy immediately
        state.EntityManager.DestroyEntity(entity);
      }
    }
  }
}
```

**Adding a new miracle requires**:
1. Create `[Type]Payload` component (specialized data)
2. Create `[Type]EffectSystem` following template above
3. Create `MiracleProfile` asset with all parameters
4. Add gesture pattern (if using gestures)
5. Create token/effect prefabs with VFX
6. ~4 hours total for experienced developer

## Next Steps & Implementation Order
1. **Recon & requirements** (Workstream 0) - 2-3 days
2. **Core architecture & components** (Workstream 1) - 1.5-2 weeks
3. **Core miracle systems** (Workstream 2) - 2 weeks
4. **Gesture recognition OR hotkeys** (Workstream 2.5) - 1-2 weeks
5. **Targeting & area display** (Workstream 3) - 1 week
6. **First 2-3 effect systems** (Workstream 4, partial) - 1 week
7. **Mana/prayer system** (Workstream 5, partial) - 1 week
8. **Expand to 8+ miracle types** (Workstream 4, complete) - 2-3 weeks
9. **Spatial integration optimization** (inline with Workstream 4) - ongoing
10. **Presentation & feedback polish** (Workstream 6) - 1-2 weeks
11. **Testing & rewind validation** (Workstream 7) - 1-2 weeks
12. **Tooling & designer workflow** (Workstream 8) - 1 week
13. **Documentation & handoff** - 3-4 days

**Recommended order**: Implement hotkeys first (Phase 1), defer full gestures until after core framework proven (Phase 2 optional).

Track progress in `Docs/Progress.md` and update this TODO as tasks complete. Capture BW2 comparison videos and tuning notes for future reference.
