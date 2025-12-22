# Simulation LOD & Environment Fields System

**Status:** Draft  
**Category:** Core - Environmental Simulation Architecture  
**Scope:** Cross-Project (PureDOTS Kernel for Godgame + Space4X)  
**Created:** 2025-12-21  
**Last Updated:** 2025-12-21

---

## Purpose

**Primary Goal:** Define a layered, LOD-based approach to environmental simulation (fluids, physics, climate, biology) that scales to millions of entities while maintaining "game-real" believability without requiring full-scale CFD or GCM models.

**Secondary Goals:**
- Create a single queryable Environment Field API (shared kernel across projects)
- Enable tiered fluid simulation (gameplay vs visuals)
- Support buoyancy, drag, locomotion, schooling, predator-prey interactions
- Integrate climate/seasonal systems with biome/ecology
- Enforce Simulation LOD everywhere to prevent scope creep

**Key Principle:** Full planet-scale CFD (Navier–Stokes everywhere) is too ambitious; "game-real" fluids/biology/climate are achievable with layered approximations that still yield lifelike behavior.

---

## Core Concept

**Environment Field API:** A single source of truth for environmental queries (gravity, medium type, fluid properties, temperature, light). Entities query this rather than maintaining scattered boolean flags.

**Tiered Simulation:** Different LOD tiers (Tier 0 near camera → Tier 3 offscreen) use different simulation methods. Gameplay-critical systems use cheap approximations; visual richness uses higher-fidelity solvers only where visible.

**Medium-Driven Locomotion:** Entities resolve locomotion mode (grounded, swimming, free-float, climbing) from environment queries each tick, enabling smooth transitions when environment changes.

**Design Philosophy:** Avoid global monolithic solvers. Split gameplay mechanics from visual effects. Use aggregate models for distant entities, individual physics only when near camera or important.

---

## Order of Execution (Implementation Priority)

### Order 1: Environment Kernel (Foundation)

**Goal:** Create shared queryable Environment Field API

**API Structure:**
```csharp
// Single source of truth for environment queries
EnvironmentField {
    // Spatial structure: grid for outdoors, compartment-graph for interiors
    GravityVector (float3)              // Direction and magnitude
    MediumType (enum)                    // Gas / Liquid / Vacuum / Solid
    FluidDensity ρ (float)              // For buoyancy calculations
    FlowVelocity (float3)                // Current/flow direction and speed
    Viscosity (float, optional)          // For drag calculations
    Temperature (float)                  // For climate/biome effects
    Salinity (float, optional)           // For buoyancy/biome variation
    Light/Irradiance (float)            // From star/season model
    Pressure (float, optional)           // For depth/altitude effects
}
```

**Query Pattern:**
```csharp
// Entities query environment at their position each tick
var env = EnvironmentField.Query(position);
var medium = env.MediumType;
var density = env.FluidDensity;
var current = env.FlowVelocity;
```

**Critical Rule:** Entities **MUST** query environment field. Never scatter `isUnderwater`, `isZeroG`, `isVacuum` booleans on entities. Those come from environment queries or logic/presentation will desync.

**Scope:** Shared kernel usable by Godgame (terrestrial environments) and Space4X (ship interiors, planetary surfaces, space).

---

### Order 2: Fluid Model Ladder (Tiered Approach)

**Goal:** Choose appropriate fluid model for each use case (gameplay vs visuals)

**Tier A — Gameplay Water Volumes (Cheap, Everywhere)**

**Purpose:** Static or slowly-varying water surfaces/volumes + currents from environment field

**Properties:**
- Low computational cost
- Suitable for all entities
- Supports: buoyancy, swimming speed, drifting, drowning, underwater predation

**Implementation:**
- Precomputed or slowly-updated water volumes (not dynamic simulation)
- Currents read from Environment Field (FlowVelocity)
- Surface collision detection for entry/exit

**Use Cases:**
- Ocean/sea gameplay mechanics
- River currents affecting swimming
- Flooding affecting locomotion
- Underwater predation mechanics

---

**Tier B — Shallow-Water Heightfields (Terrain Water, Flooding)**

**Purpose:** Represent water as heightmap over terrain (columns), not full 3D volume

**Properties:**
- Based on shallow-water equations (cheap, appropriate for terrain-scale water)
- Heightfield representation (water height per terrain column)
- Supports: rivers, tides, flood plains, coastal changes, "water height" biosculpting

**Implementation:**
- Heightmap-based water level
- Flow between columns based on height differences
- Terrain integration (water follows terrain topology)
- Active set updates (only update cells with activity)
- Conduit graph for underground tunnels (sparse, graph-based)

**Use Cases:**
- Terrain water simulation (rivers, lakes, flooding)
- Coastal dynamics (tides, erosion)
- Player water manipulation (biosculpting, damming)
- Underground tunnels and siphoning

**Reference:** Shallow-water equations are specifically designed for this use case (heightfields, not full 3D).

**See:** `Docs/Concepts/Core/Fluid_Dynamics_System.md` for detailed implementation specification.

---

**Tier C — Stable Fluids for Visual Richness (Smoke/Eddies)**

**Purpose:** Visual effects when swirls/eddies desired without strict physical accuracy

**Properties:**
- Stable, visually-oriented solver (Stam's "Real-Time Fluid Dynamics for Games")
- Stability + speed prioritized over physical accuracy
- Reasonable grid sizes (32³ to 128³ max)

**Implementation:**
- Stam-style stable fluid solver
- Advection, diffusion, pressure projection
- Velocity field visualization (particles, smoke, foam)

**Use Cases:**
- Visual water effects (splashes, eddies, swirls)
- Smoke/cloud effects
- Atmospheric visual effects

**Reference:** Stam's classic approach prioritizes stability and speed for games, not scientific accuracy.

**Critical Rule:** This is **visual only**. Gameplay mechanics (buoyancy, swimming) use Tier A/B, not Tier C.

---

**Tier D — Local SPH/FLIP for Hero Moments**

**Purpose:** Particle-based fluids only near camera / special interactions

**Properties:**
- SPH (Smoothed Particle Hydrodynamics) or FLIP (Fluid Implicit Particle) solver
- High computational cost (particles × interactions)
- **Only** for close-up, hero moments, cutscenes

**Implementation:**
- Local simulation volume (around camera/important entity)
- Particle-based fluid representation
- High visual fidelity

**Use Cases:**
- Close-up water interactions (hand in water, detailed splash)
- Special effects (fountain, waterfall detail)
- Cutscene-quality fluid rendering

**Critical Rule:** **Never** attempt particle fluids at planetary scales. Only local volumes.

---

**Recurring Error to Avoid:** Trying to solve one global fluid model that handles gameplay + visuals + everything. **Split it:** Tier A/B for gameplay, Tier C/D for visuals, never mix.

---

### Order 3: Buoyancy + Drag in ECS (Practical Implementation)

**Goal:** Implement buoyancy and drag using Archimedes' principle and drag equations

**Buoyancy Core:**

**Archimedes' Principle:**
```
F_buoyant = ρ_fluid × g × V_displaced
```

Where:
- `ρ_fluid` = fluid density (from Environment Field)
- `g` = gravity magnitude (from Environment Field)
- `V_displaced` = volume of entity submerged in fluid

**DOTS Implementation:**
- Query environment field for `ρ` and `g` at entity position
- Calculate submerged volume (entity bounds × submersion fraction)
- Apply upward impulse: `PhysicsVelocity.ApplyLinearImpulse(upward * F_buoyant)`

---

**Drag Core:**

**Standard Drag Equation:**
```
D = C_d × (ρ × V² × A) / 2
```

Where:
- `C_d` = drag coefficient (entity property, species-dependent)
- `ρ` = fluid density (from Environment Field)
- `V` = relative velocity (entity velocity - flow velocity)
- `A` = cross-sectional area (entity property)

**DOTS Implementation:**
- Query environment field for `ρ` and `FlowVelocity`
- Calculate relative velocity: `v_rel = entity_velocity - flow_velocity`
- Apply drag force opposite to velocity direction
- Use Unity Physics extensions: `PhysicsVelocity.ApplyLinearImpulse(drag_force)`

**Reference:** Unity DOTS sample "BuoyancySystem" demonstrates this exact pattern: depth → upward impulse + water drag.

---

**Design Hook for Species Differences:**

**Fish:**
- Higher thrust coefficient
- Lower drag coefficient (streamlined)
- Neutral buoyancy target (density ≈ 1.0)
- Result: Efficient swimmers, maintain depth easily

**Amphibians:**
- Lower thrust coefficient
- Buoyancy varies (can float or sink)
- Higher fatigue rate
- Result: Capable swimmers but tire quickly

**Land Animals:**
- Poor thrust coefficient (inefficient in water)
- High drag coefficient (not streamlined)
- Result: Struggle to swim, prefer to avoid deep water

**Implementation:** Species-specific stat blobs (thrust_coefficient, drag_coefficient, buoyancy_target) drive behavior. No per-limb CFD needed.

**Recurring Error to Avoid:** Per-limb hydrodynamics for everyone. Keep it simple: actuator-strength → thrust, let drag/buoyancy handle the "physics flavor."

---

### Order 4: Medium-Driven Locomotion Modes

**Goal:** Resolve locomotion mode from environment + anatomy each tick

**Locomotion Mode Resolution:**

Each tick, entity queries environment field and resolves mode:

**Grounded:**
- Entry: Contact with solid surface (ground check)
- Physics: Contact-based forces, friction
- Exit: No contact (falling/jumping) or medium change

**Swimming:**
- Entry: MediumType == Liquid, submerged
- Physics: Fluid thrust + buoyancy + drag (Order 3)
- Exit: Surface exit or medium change

**FreeFloat (Zero-G):**
- Entry: MediumType == Vacuum OR gravity ≈ 0
- Physics: Impulse-based movement + thruster forces
- Exit: Gravity restored or contact with surface

**Climbing / Wall-Push:**
- Entry: Contact with vertical surface + appropriate anatomy
- Physics: Contact impulses, adhesion forces
- Exit: Contact lost or anatomy insufficient

---

**Transitions:**

**Example: Compartment Flooding (Space4X)**
1. Entity in compartment, LocomotionMode = FreeFloat
2. Compartment floods (MediumType changes: Vacuum → Liquid)
3. Next tick: Environment query returns MediumType = Liquid
4. LocomotionMode resolves to Swimming
5. Buoyancy + drag activate automatically

**Example: Underwater to Surface (Godgame)**
1. Entity swimming underwater
2. Moves toward surface (Environment query: MediumType = Liquid, depth decreasing)
3. Surface contact detected
4. LocomotionMode resolves to Grounded
5. Buoyancy deactivates (no longer submerged)

**Result:** Believable transitions when environments change dynamically (flooding, depressurization, surface entry/exit).

---

### Order 5: Schooling and Swarm Movement (Boids)

**Goal:** Implement Reynolds' boids model for fish schools, bird flocks

**Boids Core Algorithm:**

Three behavioral rules per agent:
1. **Separation:** Steer away from nearby neighbors (avoid collision)
2. **Alignment:** Steer toward average heading of nearby neighbors
3. **Cohesion:** Steer toward average position of nearby neighbors

**Local Perception:**
- Each agent perceives neighbors within perception radius
- Weighted combination of three rules determines steering force
- No central control (emergent behavior)

**Reference:** Reynolds' distributed behavioral model is the baseline for natural-looking flocking/swarming.

---

**Scalability Optimization:**

**Aggregate Approach:**
- At distance: Run boids on **school entities** (aggregates), not individuals
- School entity moves as single unit using boids rules
- Near camera/interest: Expand school to individual agents, run boids per-agent

**Spatial Hashing:**
- Use spatial grid/hash for neighbor search (O(1) lookup per cell)
- Each agent only searches its cell + adjacent cells
- Prevents O(N²) neighbor search

**Implementation:**
```
Tier 2 (Mid-distance): School aggregate uses boids (school moves as unit)
Tier 1 (Near-distance): School expands, individuals use boids
Tier 0 (Close-up): Full individual boids + collision avoidance
```

**Recurring Error to Avoid:** O(N²) neighbor search. Always use spatial hashing / grid bins for neighbor queries.

---

### Order 6: Predator–Prey Interactions (Ecology)

**Goal:** Rich ecology without per-frame micro-simulation

**Event-Driven Approach:**

**Detection (Sensors):**
- Vision: Line-of-sight check (prey visible?)
- Proximity: Water surface proximity (birds diving for fish)
- Density: Prey density in area (school detected?)

**Opportunity Resolution:**
- Close-up (Tier 0): Animation/presentation layer for kill/strike
- Mid-distance (Tier 1): Probability-based resolution (chance of successful strike)
- Far (Tier 2): Aggregate resolution (biomass transfer + fear signal)

**Far-Tier Resolution:**
```
IF predator_near_prey_school:
    strike_success_rate = f(predator_strength, prey_defense, density)
    biomass_transferred = strike_success_rate × prey_biomass_per_individual
    prey_school.size -= biomass_transferred / average_individual_biomass
    fear_signal_propagates (nearby_prey_flee_boost)
```

**Result:** Ecology remains rich ("flying eats fish" lifelike behavior) without turning combat into per-frame micro-simulation of mouth physics globally.

---

### Order 7: Climate Fields (Seasons, Day/Night, Orbit/Tilt)

**Goal:** Integrate star parameters → planet flux → climate fields → biomes

**Star → Planet Flux:**

**Inverse-Square Law:**
```
F = L / (4π r²)
```

Where:
- `F` = received flux (energy per area per time)
- `L` = stellar luminosity
- `r` = distance from star

**Star Class & Luminosity:**
- Spectral class correlates with temperature and luminosity
- Use as content-facing presets (K-class star = cooler, dimmer; O-class = hot, bright)

---

**Seasons (Axial Tilt):**

**Obliquity Effect:**
- Axial tilt (obliquity) changes where/when insolation hits
- Root cause of seasons
- Affects how extreme seasons are (high tilt = extreme seasons)

**Irradiance Calculation:**
```
Irradiance(latitude, dayOfYear, timeOfDay) = f(orbit, tilt, star_flux)
```

**Tidally Locked Planets:**
- Climate behavior becomes "permanent dayside/nightside"
- Circulation and nightside cooling dominate outcomes
- No day/night cycle (fixed insolation pattern)

---

**Game-Real Climate Model (Recommended):**

**Avoid:** Full General Circulation Model (GCM). Too complex, years of tuning, overkill for games.

**Use:** Simple energy-balance / relaxation model

**Components:**
1. Compute `Irradiance(lat, dayOfYear, timeOfDay)` from orbit + tilt
2. Energy-balance model updates:
   - Surface temperature
   - Humidity
   - Ice fraction
   - Ocean temperature (if needed)
3. Drive biome + underwater temperature/oxygen fields from climate

**Climate → Environment Field:**
- Climate model updates `Temperature` in Environment Field
- Biome selection based on temperature + humidity + altitude
- Underwater fields (temperature, oxygen) derived from surface climate

**Result:** Believable seasons, day/night cycles, biome variation without GCM complexity.

---

### Order 8: Simulation LOD Enforcement (Critical)

**Goal:** Enforce LOD tiers everywhere to prevent scope creep

**Tier Definitions:**

**Tier 0 (Near / Active):**
- Detailed buoyancy (individual calculations)
- Individual entities (not aggregates)
- Schooling on individual agents (boids per-agent)
- Local currents (detailed flow)
- Close combat animations (event-driven predator-prey)

**Tier 1 (Mid-Distance):**
- Kinematic + simplified forces (approximate buoyancy)
- Schools as aggregates (boids on school entities)
- Coarse currents (simplified flow)
- Probability-based interactions

**Tier 2 (Far):**
- Pure aggregates (biomass, population counts)
- No individual physics
- Resource flows (abstracted)
- Aggregate predator-prey (biomass transfer only)

**Tier 3 (Offscreen):**
- Ledger updates only (rates, counts)
- Seasonal migrations (timeline-based)
- Climate drift (coarse updates)
- No simulation, just state updates

---

**LOD Transitions:**

**Entity Tier Assignment:**
- Distance from camera/interest
- Entity importance (hero entities always Tier 0)
- Player focus (selected entities = Tier 0)

**Tier Transition Rules:**
- Smooth transitions (lerp values when changing tiers)
- Invariant preservation (Tier 1 aggregate must match Tier 0 sum when expanded)
- No silent breaks (system must validate invariants)

---

**Recurring Error to Avoid:** Adding one "cool detail" that only exists in Tier 0 and silently breaks invariants when entities move tiers. **All systems must respect LOD tiers.**

**Example Anti-Pattern:**
```
// BAD: Detailed water physics only in Tier 0
IF tier == 0:
    detailed_buoyancy()
    detailed_drag()
ELSE:
    simple_kinematic()  // Invariant broken: entities behave differently when moving tiers
```

**Correct Pattern:**
```
// GOOD: Appropriate model for each tier
IF tier == 0:
    detailed_buoyancy()
ELIF tier == 1:
    approximate_buoyancy()  // Cheaper, but consistent
ELSE:
    aggregate_physics()  // Aggregate behavior matches sum of individuals
```

---

## Technical Architecture

### Environment Field Implementation

**Spatial Structure:**

**Outdoors (Grid-Based):**
- 3D grid or octree for spatial queries
- Each cell contains EnvironmentField data
- Efficient for large-scale terrain

**Interiors (Compartment-Graph):**
- Graph of connected compartments (rooms, ship sections)
- Each compartment has EnvironmentField data
- Supports different environments per compartment (vacuum vs atmosphere)

**Query API:**
```csharp
public static EnvironmentField Query(float3 position, SpatialIndex index)
{
    // Resolve spatial structure (grid cell or compartment)
    var cell = index.ResolveCell(position);
    return cell.EnvironmentData;
}
```

---

### DOTS Component Structure

**Environment Field Component:**
```csharp
public struct EnvironmentFieldCell : IComponentData
{
    public float3 GravityVector;
    public MediumType Type;  // Gas, Liquid, Vacuum, Solid
    public float FluidDensity;
    public float3 FlowVelocity;
    public float Viscosity;
    public float Temperature;
    public float Salinity;
    public float Irradiance;
    public float Pressure;
}
```

**Entity Locomotion State:**
```csharp
public struct LocomotionMode : IComponentData
{
    public LocomotionType CurrentMode;  // Grounded, Swimming, FreeFloat, Climbing
    public uint LastEnvironmentQueryTick;
}
```

**Entity Buoyancy State:**
```csharp
public struct BuoyancyState : IComponentData
{
    public float SubmergedVolume;
    public float BuoyancyForce;
    public float DragCoefficient;
    public float ThrustCoefficient;
    public float BuoyancyTarget;  // Neutral buoyancy density
}
```

**School Aggregate:**
```csharp
public struct SchoolAggregate : IComponentData
{
    public int MemberCount;
    public float3 AveragePosition;
    public float3 AverageVelocity;
    public float Biomass;
}

public struct SchoolMembership : IBufferElementData
{
    public Entity MemberEntity;
    public float ContributionWeight;
}
```

---

### System Execution Order

```
1. ClimateUpdateSystem (Tier 3: coarse climate updates)
   ↓
2. EnvironmentFieldUpdateSystem (update grid/compartments from climate + sources)
   ↓
3. LocomotionModeResolutionSystem (query environment, resolve mode)
   ↓
4. BuoyancyDragSystem (Tier 0/1: apply forces based on environment)
   ↓
5. BoidsSystem (Tier 0/1: update individual/school positions)
   ↓
6. PredatorPreyInteractionSystem (event-driven, sensor-based)
   ↓
7. AggregateUpdateSystem (Tier 2/3: update biomass, populations)
   ↓
8. LODTransitionSystem (assign tiers, validate invariants)
```

---

## Integration Points

### With Existing Systems

**Locomotion System:**
- LocomotionMode drives locomotion physics
- Environment queries inform pathfinding (avoid water if can't swim)

**Combat System:**
- Predator-prey interactions trigger combat events
- Medium affects combat (underwater = different weapon effectiveness)

**Ecology System:**
- Climate → biome selection
- Temperature/oxygen fields affect species distribution

**Physics System:**
- Buoyancy/drag applied via Unity Physics extensions
- Environment field informs collision resolution

---

## Performance Considerations

### Scalability Targets

**Entity Count:**
- Tier 0: ~1000 individual entities (detailed physics)
- Tier 1: ~10,000 entities (simplified physics or aggregates)
- Tier 2: ~100,000 aggregates (population counts)
- Tier 3: Unlimited (ledger only)

**Spatial Queries:**
- Environment field queries: O(1) with spatial index
- Neighbor searches: O(N) with spatial hashing (not O(N²))

**Fluid Simulation:**
- Tier A/B: Negligible cost (precomputed or heightfield)
- Tier C: ~1-5ms for 64³ grid (stable fluids)
- Tier D: Local only, ~10-50ms for small particle count

---

## Design Principles

### Core Principles

1. **Single Source of Truth:** Environment Field is authoritative. Entities query, don't cache booleans.

2. **Tiered Simulation:** Different methods for different needs. Gameplay uses cheap approximations, visuals use higher-fidelity only when visible.

3. **Medium-Driven:** Locomotion resolves from environment each tick. No hardcoded modes.

4. **Aggregate Models:** Distant entities become aggregates. Individual physics only when near/important.

5. **LOD Enforcement:** All systems must respect tiers. No Tier-0-only features that break invariants.

---

## Recurring Errors to Avoid (Summary)

1. ❌ Scattering `isUnderwater` / `isZeroG` booleans on entities
   ✅ Query Environment Field each tick

2. ❌ One global fluid model for gameplay + visuals
   ✅ Split: Tier A/B for gameplay, Tier C/D for visuals

3. ❌ Per-limb hydrodynamics for everyone
   ✅ Simple: actuator-strength → thrust, drag/buoyancy handle physics

4. ❌ O(N²) neighbor search for boids
   ✅ Spatial hashing / grid bins

5. ❌ Full GCM for climate
   ✅ Simple energy-balance / relaxation model

6. ❌ Tier-0-only features that break invariants
   ✅ All systems respect LOD tiers

---

## Open Questions

1. **Environment Field Update Frequency:** How often does climate update Environment Field? Every frame? Every second? Per-tier?

2. **School Expansion Threshold:** At what distance/importance does school aggregate expand to individuals?

3. **Buoyancy Volume Calculation:** How to compute submerged volume efficiently? Bounds approximation? Signed distance fields?

4. **Climate Model Tuning:** What energy-balance model complexity is sufficient? Simple relaxation or more sophisticated?

5. **LOD Tier Assignment:** Distance-based only, or also importance-based? How to handle player focus?

6. **Compartment Flooding Speed:** How fast do compartments flood/depressurize? Instant? Gradual? Event-driven?

---

## Related Documentation

- **Environmental Systems:** `Docs/Concepts/Core/Environmental_Systems.md` (existing environmental concepts)
- **Locomotion System:** `Docs/Concepts/Core/Locomotion_System.md` (locomotion mechanics)
- **General Forces System:** `Docs/Concepts/Core/General_Forces_System.md` (force application patterns)
- **Forces Integration Guide:** `Docs/Concepts/Core/Forces_Integration_Guide.md` (Unity Physics integration)

---

**For Implementers:** 
- Start with Order 1 (Environment Kernel). This is the foundation for everything else.
- Implement Order 3 (Buoyancy + Drag) using Unity Physics extensions (reference: Unity DOTS BuoyancySystem sample).
- Use spatial hashing from the start (don't optimize later).
- Enforce LOD tiers from the beginning (don't add detail without tier support).

**For Designers:** 
- Focus on "game-real" believability, not scientific accuracy.
- Tier A/B fluids provide gameplay depth without visual complexity.
- Climate model should drive biomes/ecology, not be a goal in itself.
- LOD enforcement prevents scope creep - resist the urge to add Tier-0-only details.

---

**Last Updated:** 2025-12-21  
**Status:** Draft - Core architecture defined, awaiting implementation decisions

