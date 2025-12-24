# PureDOTS Combat System Audit

**Date**: 2025-01-21  
**Scope**: PureDOTS combat components vs vision requirements  
**Vision**: Godgame (Total War-style) + Space4X (Homeworld-style)

---

## Executive Summary

**Status**: Partial implementation with significant gaps

**What Exists**:
- ✅ Individual combat stats (BaseAttributes, CombatStats)
- ✅ Basic targeting system (TargetPriority, PotentialTarget)
- ✅ Projectile/weapon systems (WeaponMount, ProjectileEntity)
- ✅ Health/damage application (Health, Shield, DamageApplication)
- ✅ Module health tracking (ModuleHealth, ModuleState)
- ✅ Basic formations (BandFormationSystem, FormationAnchor)
- ✅ Combat state machine (CombatState, CombatStateData)
- ✅ Combat loop phases (CombatLoopPhase, CombatLoopState)

**What's Missing**:
- ❌ Squad-based formation combat (Total War-style)
- ❌ Morale wave propagation system
- ❌ Module targeting/subsystem disable 
- ❌ 3D positioning for space combat
- ❌ Formation-based tactics and maneuvers
- ❌ Squad cohesion affecting combat effectiveness
- ❌ Module-specific damage routing

---

## 1. Godgame Vision: Total War-Style Combat

### 1.1 Required: Squad-Based Formations

**Vision**: Units fight in formations (shield walls, phalanxes, skirmish lines). Formation integrity affects combat effectiveness.

**What Exists**:
```12:17:puredots/Packages/com.moni.puredots/Runtime/Runtime/Aggregates/BandComponents.cs
    public struct BandIdentity : IComponentData
    {
        public FixedString64Bytes BandName;
        public BandPurpose Purpose;
        public Entity LeaderEntity;
        public uint FormationTick;
    }
```

- `BandFormationSystem` - Forms bands from compatible entities
- `FormationAnchor` - Basic formation positioning
- `FormationElasticitySystem` - Adjusts spacing under threat

**What's Missing**:
- ❌ **Formation types** (ShieldWall, Phalanx, SkirmishLine, Wedge, etc.)
- ❌ **Formation integrity calculation** (how many members are in position)
- ❌ **Formation bonuses** (defense bonus for shield wall, attack bonus for wedge)
- ❌ **Formation breaking** (when integrity drops, units scatter)
- ❌ **Formation commands** (Hold Formation, Advance, Retreat, Flank)

**Gap**: Bands exist but lack tactical formation mechanics. No formation type definitions, integrity tracking, or formation-based combat bonuses.

---

### 1.2 Required: Morale Wave System

**Vision**: Morale propagates through formations. When one unit breaks, nearby units are affected. Morale waves can cascade through entire armies.

**What Exists**:
```26:29:puredots/Packages/com.moni.puredots/Runtime/Runtime/Aggregates/BandComponents.cs
    public struct BandAggregateStats : IComponentData
    {
        public ushort MemberCount;
        public float AverageMorale;
        public float AverageEnergy;
        public float AverageStrength;
    }
```

- `BandAggregateStats.AverageMorale` - Aggregate morale tracking
- `CombatStats.Morale` - Individual morale (yield threshold)

**What's Missing**:
- ❌ **Morale propagation system** (nearby units affect each other)
- ❌ **Morale wave events** (when unit breaks, propagate to neighbors)
- ❌ **Morale decay over time** (standing idle reduces morale)
- ❌ **Morale recovery** (victories, leadership bonuses)
- ❌ **Morale thresholds** (Routed, Shaken, Steady, Inspired)
- ❌ **Formation morale multiplier** (formations boost morale)

**Gap**: Morale exists as a stat but doesn't propagate or create cascading effects. No wave mechanics.

---

### 1.3 Required: Squad Cohesion in Combat

**Vision**: Units in cohesive squads fight better. Cohesion degrades under fire, affecting accuracy and damage output.

**What Exists**:
```125:134:puredots/Packages/com.moni.puredots/Runtime/Telemetry/BehaviorTelemetryDefinitions.cs
    public struct FleetCoreTelemetry : IComponentData
    {
        public float CohesionAccumulator;
        public uint CohesionSamples;
        public float MoraleAccumulator;
        public uint MoraleSamples;
        public float StrikeCraftLoadAccumulator;
        public uint StrikeCraftSamples;
        public byte CohesionOutOfRange;
    }
```

- `SquadCohesionComponents` - Squad cohesion tracking (found in grep results)
- `FormationElasticitySystem` - Adjusts formation under threat

**What's Missing**:
- ❌ **Combat cohesion calculation** (how well squad maintains formation during combat)
- ❌ **Cohesion → combat effectiveness** (cohesion affects accuracy, damage, defense)
- ❌ **Cohesion degradation** (damage taken reduces cohesion)
- ❌ **Cohesion recovery** (time, leadership, victories restore cohesion)
- ❌ **Cohesion thresholds** (Broken, Fragmented, Cohesive, Elite)

**Gap**: Cohesion exists for fleets but not integrated into ground combat effectiveness calculations.

---

### 1.4 Required: Formation-Based Tactics

**Vision**: Different formations enable different tactics (shield wall defends, wedge charges, skirmish harasses).

**What Exists**:
```6:13:puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/CombatManeuvers.cs
    public enum CombatManeuver : byte
    {
        None = 0,
        Strafe = 1,
        Kite = 2,
        JTurn = 3,
        Dive = 4,
        Disengage = 5
    }
```

- `CombatManeuver` - Maneuvers exist but are space-focused (Strafe, Kite, JTurn)
- `CombatLoopPhase` - Basic combat phases (Idle, Patrol, Intercept, Attack, Retreat)

**What's Missing**:
- ❌ **Ground formation tactics** (Charge, Hold, Flank, Encircle, Feint)
- ❌ **Tactic selection** (AI chooses tactics based on formation type)
- ❌ **Tactic effectiveness** (Charge vs ShieldWall, Flank vs Phalanx)
- ❌ **Tactic execution** (movement patterns, timing, coordination)

**Gap**: Maneuvers exist for space combat but not for ground formation tactics.

---

## 2. Space4X Vision: Homeworld-Style Combat

### 2.1 Required: Targetable Modules/Subsystems

**Vision**: Ships have modules (weapons, engines, shields) that can be individually targeted and disabled. Destroying engines stops movement, destroying weapons stops firing.

**What Exists**:
```71:83:puredots/Packages/com.moni.puredots/Runtime/Runtime/Space/ModuleComponents.cs
    public struct ShipModule : IComponentData
    {
        public ModuleFamily Family;
        public ModuleClass Class;
        public MountType RequiredMount;
        public MountSize RequiredSize;
        public FixedString64Bytes ModuleName;
        public float Mass;
        public float PowerRequired;
        public float PowerGeneration;
        public byte EfficiencyPercent;
        public ModuleState State;
    }
```

```54:61:puredots/Packages/com.moni.puredots/Runtime/Runtime/Space/ModuleComponents.cs
    public enum ModuleState : byte
    {
        Offline,
        Standby,
        Active,
        Damaged,
        Destroyed
    }
```

```118:138:puredots/Packages/com.moni.puredots/Runtime/Runtime/Space/ModuleComponents.cs
    public struct ModuleHealth : IComponentData
    {
        public const byte FlagRequiresRepair = 1 << 0;

        public byte Integrity;
        public byte FailureThreshold;
        public byte RepairPriority;
        public byte Flags;

        public bool NeedsRepair => (Flags & FlagRequiresRepair) != 0;

        public void MarkRepairRequested()
        {
            Flags |= FlagRequiresRepair;
        }

        public void ClearRepairRequested()
        {
            Flags &= unchecked((byte)~FlagRequiresRepair);
        }
    }
```

- `ShipModule` - Module definition with state
- `ModuleHealth` - Health tracking per module
- `ModuleState` - States include Offline, Damaged, Destroyed
- `CarrierModuleSlot` - Modules installed in slots

**What's Missing**:
- ❌ **Module targeting system** (select specific module as target)
- ❌ **Module hit detection** (projectiles hit specific modules, not just hull)
- ❌ **Module disable effects** (destroyed engine → no movement, destroyed weapon → no firing)
- ❌ **Module 3D positions** (where modules are on ship hull for hit detection)
- ❌ **Module visibility** (some modules exposed, some protected)
- ❌ **Module criticality** (destroying bridge disables command, destroying reactor destroys ship)

**Gap**: Modules exist with health/state but cannot be individually targeted. No module-specific damage routing or disable effects.

---

### 2.2 Required: 3D Positioning for Space Combat

**Vision**: Combat happens in 3D space. Ships can be above/below each other. Positioning matters (high ground advantage, flanking from below).

**What Exists**:
- `LocalTransform` - Standard Unity ECS transform (supports 3D)
- `CombatRange` - Range checks (MeleeRange, RangedMaxRange, AOERadius)

**What's Missing**:
- ❌ **3D combat range** (vertical engagement range separate from horizontal)
- ❌ **3D formation positioning** (ships above/below leader)
- ❌ **3D targeting** (aim up/down, not just horizontal)
- ❌ **3D movement** (ascend, descend, dive, climb)
- ❌ **3D advantage calculations** (high ground bonus, flanking from below)

**Gap**: 3D transforms exist but combat systems don't leverage 3D positioning. No vertical combat mechanics.

---

### 2.3 Required: Subsystem Disable Effects

**Vision**: When a module is destroyed, it affects ship capabilities. Destroyed engines = no movement. Destroyed weapons = no firing. Destroyed shields = no protection.

**What Exists**:
```157:163:puredots/Packages/com.moni.puredots/Runtime/Runtime/Ships/CarrierModuleComponents.cs
    public struct ModuleOperationalState : IComponentData
    {
        public byte IsOnline;
        public byte InCombat;
        public float LoadFactor;
    }
```

- `ModuleOperationalState` - Tracks if module is online
- `ModuleState.Destroyed` - Module can be destroyed

**What's Missing**:
- ❌ **Module → capability mapping** (Engine module → movement capability)
- ❌ **Capability disable system** (when module destroyed, disable capability)
- ❌ **Partial capability** (damaged engine = reduced speed, not stopped)
- ❌ **Capability recovery** (repair module → restore capability)
- ❌ **Emergency systems** (backup engines, emergency power)

**Gap**: Modules can be destroyed but destruction doesn't affect ship capabilities. No capability disable system.

---

### 2.4 Required: Module-Specific Damage Routing

**Vision**: When a projectile hits a ship, it hits a specific module (or hull). Damage is routed to that module's health, not just overall hull.

**What Exists**:
```114:156:puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/CombatComponents.cs
    public struct HitEvent : IBufferElementData
    {
        /// <summary>
        /// Entity that was hit.
        /// </summary>
        public Entity HitEntity;

        /// <summary>
        /// Entity that caused the hit (attacker).
        /// </summary>
        public Entity AttackerEntity;

        /// <summary>
        /// Hit position (world space).
        /// </summary>
        public float3 HitPosition;

        /// <summary>
        /// Hit normal (for impact effects).
        /// </summary>
        public float3 HitNormal;

        /// <summary>
        /// Damage amount.
        /// </summary>
        public float DamageAmount;

        /// <summary>
        /// Damage type.
        /// </summary>
        public DamageType DamageType;

        /// <summary>
        /// Weapon index that caused hit (if applicable).
        /// </summary>
        public byte WeaponIndex;

        /// <summary>
        /// Tick when hit occurred.
        /// </summary>
        public uint HitTick;
    }
```

- `HitEvent` - Tracks hit position and damage
- `DamageApplication` - Applies damage to entities
- `ModuleHealth` - Tracks module health

**What's Missing**:
- ❌ **Hit → module resolution** (determine which module was hit based on hit position)
- ❌ **Module hit detection** (raycast/geometry check for module bounds)
- ❌ **Module damage routing** (damage goes to module health, not hull)
- ❌ **Hull fallback** (if no module hit, damage goes to hull)
- ❌ **Module destruction threshold** (module destroyed when health reaches 0)

**Gap**: Hits are tracked but not routed to specific modules. No module hit detection or damage routing.

---

## 3. Shared Combat Systems

### 3.1 Targeting System

**What Exists**:
```10:46:puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/Targeting/TargetingComponents.cs
    public struct TargetPriority : IComponentData
    {
        /// <summary>
        /// Currently selected target entity.
        /// </summary>
        public Entity CurrentTarget;

        /// <summary>
        /// Threat score of current target.
        /// </summary>
        public float ThreatScore;

        /// <summary>
        /// Tick when target was last engaged.
        /// </summary>
        public uint LastEngagedTick;

        /// <summary>
        /// Tick when target was selected.
        /// </summary>
        public uint TargetSelectedTick;

        /// <summary>
        /// Strategy used for target selection.
        /// </summary>
        public TargetingStrategy Strategy;

        /// <summary>
        /// Whether to allow automatic target switching.
        /// </summary>
        public bool AllowAutoSwitch;

        /// <summary>
        /// Minimum ticks before allowing target switch.
        /// </summary>
        public uint TargetLockDuration;
    }
```

**Status**: ✅ Good foundation. Supports multiple targeting strategies.

**Missing for Vision**:
- ❌ **Module targeting** (target specific module on ship, not just ship)
- ❌ **Formation targeting** (target formation leader, formation center, weakest unit)
- ❌ **Priority target tags** (HighValueTargetTag exists but not used for formations)

---

### 3.2 Damage Application

**What Exists**:
```11:32:puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/DamageApplicationComponents.cs
    public struct DamageApplication : IComponentData
    {
        /// <summary>
        /// Damage amount to apply (already calculated).
        /// </summary>
        public float DamageAmount;

        /// <summary>
        /// Source entity that dealt damage.
        /// </summary>
        public Entity SourceEntity;

        /// <summary>
        /// Damage type flags.
        /// </summary>
        public DamageTypeFlags DamageType;

        /// <summary>
        /// Tick when damage should be applied.
        /// </summary>
        public uint ApplyTick;
    }
```

**Status**: ✅ Basic damage application exists.

**Missing for Vision**:
- ❌ **Formation damage sharing** (damage distributed across formation members)
- ❌ **Module damage routing** (damage goes to specific module)
- ❌ **Morale damage** (damage affects morale, not just health)

---

### 3.3 Combat State Machine

**What Exists**:
```9:70:puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/State/CombatStateComponents.cs
    public enum CombatState : byte
    {
        /// <summary>
        /// Not in combat, no target.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Moving toward target to engage.
        /// </summary>
        Approaching = 1,

        /// <summary>
        /// In combat range, actively fighting.
        /// </summary>
        Engaged = 2,

        /// <summary>
        /// Performing an attack action.
        /// </summary>
        Attacking = 3,

        /// <summary>
        /// Blocking or parrying incoming attacks.
        /// </summary>
        Defending = 4,

        /// <summary>
        /// Unable to act due to stun effect.
        /// </summary>
        Stunned = 5,

        /// <summary>
        /// Retreating from combat.
        /// </summary>
        Fleeing = 6,
```

**Status**: ✅ Good state machine for individual combatants.

**Missing for Vision**:
- ❌ **Formation combat states** (FormationEngaged, FormationBroken, FormationRouted)
- ❌ **Module operational states** (ModuleDestroyed, ModuleDamaged, ModuleOffline)

---

## 4. Priority Gaps (Must-Have for Vision)

### 4.1 Critical: Module Targeting & Disable (Space4X)

**Priority**: 🔴 Critical  
**Complexity**: High  
**Dependencies**: Module hit detection, capability system

**Required Components**:
- `ModuleTarget` - Target specific module (extends TargetPriority)
- `ModuleHitDetection` - Raycast/geometry check for module bounds
- `ModuleCapability` - Maps module → capability (Engine → Movement, Weapon → Firing)
- `CapabilityState` - Tracks if capability is enabled/disabled
- `ModuleDamageRouter` - Routes damage to modules based on hit position

**Required Systems**:
- `ModuleTargetingSystem` - Selects modules as targets
- `ModuleHitDetectionSystem` - Determines which module was hit
- `ModuleDamageRouterSystem` - Routes damage to module health
- `CapabilityDisableSystem` - Disables capabilities when modules destroyed

---

### 4.2 Critical: Formation Combat (Godgame)

**Priority**: 🔴 Critical  
**Complexity**: High  
**Dependencies**: Formation types, integrity tracking, morale propagation

**Required Components**:
- `FormationType` - Enum (ShieldWall, Phalanx, SkirmishLine, Wedge, etc.)
- `FormationIntegrity` - Tracks how many members are in position
- `FormationBonus` - Combat bonuses from formation (defense, attack, morale)
- `FormationTactic` - Current tactic (Charge, Hold, Flank, etc.)
- `SquadCohesion` - Cohesion level affecting combat effectiveness

**Required Systems**:
- `FormationCombatSystem` - Applies formation bonuses to combat
- `FormationIntegritySystem` - Calculates integrity from member positions
- `FormationTacticSystem` - Executes formation tactics
- `SquadCohesionSystem` - Tracks and updates cohesion during combat

---

### 4.3 High: Morale Wave Propagation (Godgame)

**Priority**: 🟡 High  
**Complexity**: Medium  
**Dependencies**: Morale system, spatial queries

**Required Components**:
- `MoraleWave` - Propagates morale changes to nearby units
- `MoraleThreshold` - Morale levels (Routed, Shaken, Steady, Inspired)
- `MoralePropagation` - Configuration for wave propagation

**Required Systems**:
- `MoraleWaveSystem` - Propagates morale changes through formations
- `MoraleDecaySystem` - Decays morale over time
- `MoraleRecoverySystem` - Recovers morale from victories/leadership

---

### 4.4 High: 3D Combat Positioning (Space4X)

**Priority**: 🟡 High  
**Complexity**: Medium  
**Dependencies**: 3D transforms (already exists)

**Required Components**:
- `CombatPosition3D` - 3D combat position (extends LocalTransform)
- `VerticalEngagementRange` - Vertical range for combat
- `3DAdvantage` - High ground/flanking bonuses

**Required Systems**:
- `3DCombatRangeSystem` - Calculates 3D engagement ranges
- `3DFormationSystem` - Positions ships in 3D formations
- `3DAdvantageSystem` - Calculates positioning advantages

---

## 5. Implementation Roadmap

### Phase 1: Foundation (Current)
- ✅ Individual combat stats
- ✅ Basic targeting
- ✅ Health/damage
- ✅ Module health tracking
- ✅ Basic formations

### Phase 2: Module Targeting (Space4X)
1. Module hit detection system
2. Module damage routing
3. Capability disable system
4. Module targeting UI/selection

### Phase 3: Formation Combat (Godgame)
1. Formation type definitions
2. Formation integrity calculation
3. Formation combat bonuses
4. Formation tactics system

### Phase 4: Morale & Cohesion (Godgame)
1. Morale wave propagation
2. Squad cohesion → combat effectiveness
3. Morale thresholds and states
4. Cohesion degradation/recovery

### Phase 5: 3D Combat (Space4X)
1. 3D engagement ranges
2. 3D formation positioning
3. 3D advantage calculations
4. Vertical movement mechanics

---

## 6. Recommendations

1. **Start with Module Targeting** (Space4X): Highest impact, enables tactical depth
2. **Add Formation Types** (Godgame): Foundation for formation combat
3. **Implement Morale Waves** (Godgame): Creates cascading effects
4. **Add 3D Positioning** (Space4X): Enhances space combat feel

**Avoid**:
- Don't add features that don't support the vision (e.g., complex individual duels)
- Don't duplicate systems (use shared combat components where possible)
- Don't optimize prematurely (get mechanics working first)

---

## 7. References

- `puredots/Docs/Concepts/Core/Combat_Mechanics_Core.md` - Core combat mechanics
- `puredots/Docs/Mechanics/CombatLoop.md` - Combat loop design
- `godgame/Docs/Concepts/Combat/Individual_Combat_System.md` - Individual combat vision
- `space4x/Assets/Scripts/Space4x/Registry/Space4XCaptainComponents.cs` - Captain orders

---

**Last Updated**: 2025-01-21  
**Next Review**: After Phase 2 completion



