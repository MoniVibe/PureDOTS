# Motion Physics Design

**Status:** Design Concept
**Category:** Core - Physics / Movement
**Audience:** Implementers / Designers / Architects
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

**Purpose:** Separate absolute speed (meters/sec) from responsiveness and perception (how fast it feels). Enable "big is fast" without "big feels agile" - titans can have higher top speeds while feeling heavy/slow, fighters can feel agile while having lower absolute speeds.

**Key Principle:** Speed perception is driven by optic flow, acceleration, and animation cues - not just absolute velocity. Use motion profiles to tune absolute limits (sim layer) separately from feel parameters (perception layer).

---

## Core Concepts

### The Paradox

**Desired Behavior:**
- **Fighter:** Lower top speed, but feels fast/agile
- **Titan:** Higher top speed, but feels heavy/slow

**Challenge:** Absolute speed (m/s) vs perceived speed (feel) are different things.

**Solution:** Three-layer approach:
1. **Sim Layer:** Absolute limits (Vmax, Amax, αmax) - physics constraints
2. **Control Layer:** Responsiveness (acceleration, turn rate, braking) - gameplay feel
3. **Perception Layer:** Visual/audio cues (FOV, optic flow, animation) - subjective speed

---

## 1. Sim Layer: Make "Big is Fast" Possible Without "Big Feels Agile"

### Legged / Ground Motion

**Use dimensionless speed so bigger bodies can have higher absolute top speed while still moving with a "slower cadence":**

**Froude Number:**
```
Fr = v² / (g × l)
```

Where:
- `v` = velocity (m/s)
- `g` = gravitational acceleration (~9.81 m/s²)
- `l` = leg length / hip height (meters)

**Key Insight:** Holding similar `Fr` across sizes implies `v ∝ √l`: larger creatures can be faster in m/s, even if their gait feels heavier/slower in frequency.

**Example:**
- **Fighter (l = 0.9m):** v ≈ 3 m/s (Fr ≈ 1.0, comfortable run)
- **Titan (l = 3.0m):** v ≈ 5.5 m/s (Fr ≈ 1.0, same gait "feel", but 83% faster absolute)

**Result:** Titan gets larger `l` → higher max `v`, but lower stride frequency (slow "thump") is natural.

**Reference:** [Wikipedia - Froude Number](https://en.wikipedia.org/wiki/Froude_number)

---

### Ships / 6DOF Motion

**Make "cumbersome" primarily about angular/linear acceleration limits:**

**Moment of Inertia:**
- Moment of inertia grows with mass and distance squared: `I = Σ(m × r²)`
- Angular acceleration follows `τ = I × α` (torque = moment of inertia × angular acceleration)
- Bigger bodies resist turning (higher `I` → lower `α` for same `τ`)

**Example:**
- **Fighter:** Small mass, compact → low `I` → high `α` (agile turns)
- **Titan:** Large mass, spread out → high `I` → low `α` (sluggish turns)

**Result:** Titan can have higher top speed, but far lower angular acceleration / turn rate / braking.

**Reference:** [Wikipedia - Moment of Inertia](https://en.wikipedia.org/wiki/Moment_of_inertia)

---

## 2. Control Layer: Top Speed is NOT the "Feel"

**Give every mover a small "motion model":**

```csharp
/// <summary>
/// Motion profile defining absolute limits and responsiveness.
/// </summary>
public struct MotionProfile : IComponentData
{
    // Absolute limits (sim layer)
    public float Vmax;           // Maximum velocity (m/s) - absolute cap
    public float Amax;           // Maximum linear acceleration (m/s²)
    public float Dmax;           // Maximum braking/deceleration (m/s²)
    public float ωmax;           // Maximum angular speed (rad/s)
    public float αmax;           // Maximum angular acceleration (rad/s²)
    public float Jmax;           // Maximum jerk (m/s³) - optional, for weighty starts/stops
}
```

**Time-Constant Approach:**

Time to reach 90% of max speed:
```
t₉₀ ≈ 2.3 × Vmax / Amax
```

**Example Target Ratios** (to satisfy "fighter can't outpace titan at full speed, but feels agile"):

```
Vtitan ≈ 1.5–3× Vfighter      (absolute speed advantage)
Afighter ≈ 5–20× Atitan        (acceleration advantage)
αfighter ≈ 10–50× αtitan       (turn rate advantage)
```

**Result:**
- **Fighter wins:** Maneuverability and reachability (quick turns, fast acceleration)
- **Titan wins:** Straight-line top speed (can outrun fighter in open space)

---

## 3. Perception Layer: Make Fast Things Look Fast and Heavy Things Feel Heavy

**Human speed perception is heavily driven by optic flow (texture/object motion across the view).** Visual flow is used to perceive/regulate speed, and changing texture/flow properties measurably changes perceived speed.

**Reference:**
- [Springer Link - Optic Flow and Speed Perception](https://link.springer.com/article/10.1007/s00221-007-0894-3)
- [Nature - Speed Perception](https://www.nature.com/articles/35012068)

---

### Fighter "Feels Fast"

**Higher FOV / wider GFOV and stronger peripheral motion → stronger speed sensation:**

- Higher FOV (wider field of view) increases perceived speed
- More peripheral motion (textures moving across view) enhances speed perception
- Perceived speed is sensitive to GFOV/FOV manipulations in simulator studies

**Additional cues:**
- More near-field parallax (camera closer, more nearby detail)
- Motion trails / streak VFX
- Faster animation cadence (quick steps, rapid movements)

**Reference:** [ATS International Journal - FOV and Speed Perception](https://atsinternationaljournal.com/)

---

### Titan "Feels Heavy"

**Lower camera responsiveness and reduced optic flow cues:**

- Lower camera responsiveness (smoothing/lag)
- Lower FOV (narrower field of view)
- Fewer near-field cues (camera further, less detail)
- Animation/pose: emphasize ease-in/ease-out and follow-through (overshoot + settle) to convey inertia/weight

**Audio/FX:**
- Slow, bassy footfalls / engine spool
- Delayed stop "settle" VFX (momentum continues, then settles)
- Heavy impact sounds (weighty steps)

**Reference:** [Adobe - Ease-in/Ease-out Animation](https://www.adobe.com/creativecloud/animation/discover/ease-in-ease-out.html)

---

## 4. Practical DOTS Implementation Pattern

### MotionProfile Component

```csharp
/// <summary>
/// Motion profile with absolute limits and feel parameters.
/// </summary>
public struct MotionProfile : IComponentData
{
    // Absolute limits (sim layer - used by physics/movement systems)
    public float Vmax;           // Maximum velocity (m/s)
    public float Amax;           // Maximum linear acceleration (m/s²)
    public float Dmax;           // Maximum braking/deceleration (m/s²)
    public float ωmax;           // Maximum angular speed (rad/s)
    public float αmax;           // Maximum angular acceleration (rad/s²)
    public float Jmax;           // Maximum jerk (m/s³) - optional
    
    // Feel parameters (perception layer - used by presentation systems)
    public float CameraFOVBias;          // FOV multiplier (fighter: 1.2×, titan: 0.8×)
    public float TrailIntensity;         // Motion trail intensity (fighter: 1.0, titan: 0.2)
    public float StepCadenceMultiplier;  // Animation cadence (fighter: 1.5×, titan: 0.6×)
    public float CameraResponsiveness;   // Camera smoothing (fighter: 0.9, titan: 0.5)
    
    // Locomotion mode
    public LocomotionMode Mode;          // Ground, 6DOF, etc.
}

public enum LocomotionMode : byte
{
    Ground = 0,
    SixDOF = 1,
    Flying = 2,
    Swimming = 3
}
```

**Usage:**

```csharp
// Sim systems use only caps/limits
public void ApplyMovement(ref MotionProfile profile, ref PhysicsVelocity velocity, float3 desiredDir)
{
    var currentSpeed = math.length(velocity.Linear);
    var desiredSpeed = profile.Vmax;
    
    // Accelerate toward desired speed (clamped by Amax)
    var accelNeeded = desiredSpeed - currentSpeed;
    var accel = math.clamp(accelNeeded / fixedDt, -profile.Dmax, profile.Amax);
    
    velocity.Linear += desiredDir * accel * fixedDt;
    velocity.Linear = math.clamp(velocity.Linear, -profile.Vmax, profile.Vmax);
}

// Presentation systems use only feel params
public void UpdateCameraFOV(ref MotionProfile profile, ref Camera camera)
{
    var baseFOV = 60f;
    camera.fieldOfView = baseFOV * profile.CameraFOVBias;
}

public void UpdateAnimationSpeed(ref MotionProfile profile, ref Animator animator)
{
    animator.speed = profile.StepCadenceMultiplier;
}
```

---

### Example Profiles

**Fighter Profile:**
```csharp
var fighterProfile = new MotionProfile
{
    // Absolute limits (lower top speed, high acceleration)
    Vmax = 10f,          // m/s
    Amax = 20f,          // m/s² (very responsive)
    Dmax = 25f,          // m/s² (quick braking)
    ωmax = 4f,           // rad/s (fast turning)
    αmax = 15f,          // rad/s² (agile)
    Jmax = 50f,          // m/s³ (quick response)
    
    // Feel params (fast perception)
    CameraFOVBias = 1.2f,           // Wider FOV
    TrailIntensity = 1.0f,          // Strong trails
    StepCadenceMultiplier = 1.5f,   // Fast animation
    CameraResponsiveness = 0.9f,    // Responsive camera
    Mode = LocomotionMode.Ground
};
```

**Titan Profile:**
```csharp
var titanProfile = new MotionProfile
{
    // Absolute limits (higher top speed, low acceleration)
    Vmax = 25f,          // m/s (faster than fighter!)
    Amax = 2f,           // m/s² (sluggish)
    Dmax = 3f,           // m/s² (slow braking)
    ωmax = 0.5f,         // rad/s (slow turning)
    αmax = 0.3f,         // rad/s² (cumbersome)
    Jmax = 2f,           // m/s³ (weighty)
    
    // Feel params (heavy perception)
    CameraFOVBias = 0.8f,           // Narrower FOV
    TrailIntensity = 0.2f,          // Weak trails
    StepCadenceMultiplier = 0.6f,   // Slow animation
    CameraResponsiveness = 0.5f,    // Laggy camera
    Mode = LocomotionMode.Ground
};
```

**Result:**
- Titan top speed (25 m/s) > Fighter top speed (10 m/s) ✅
- Fighter acceleration (20 m/s²) > Titan acceleration (2 m/s²) ✅
- Fighter turn rate (15 rad/s²) > Titan turn rate (0.3 rad/s²) ✅
- Fighter feels fast (high FOV, trails, quick animation) ✅
- Titan feels heavy (low FOV, smooth camera, slow animation) ✅

---

## Quick Checklist

**To match desired paradox ("fighter can't outpace titan at full speed, but feels agile"):**

✅ **Titan top speed > fighter top speed** (absolute scale)

✅ **Titan accel/turn/brake ≪ fighter** (responsiveness)

✅ **Fighter camera/optic-flow cues stronger; titan cues damped** (perceived speed)

✅ **Titan animation uses slow-in/out + follow-through** (weight)

---

## Integration Points

### Physics Systems

- Movement systems read `MotionProfile.Vmax`, `Amax`, `αmax` for constraints
- Steering systems use `ωmax`, `αmax` for turn rate limits
- Braking systems use `Dmax` for deceleration

### Presentation Systems

- Camera systems read `CameraFOVBias`, `CameraResponsiveness` for FOV/smoothing
- Animation systems read `StepCadenceMultiplier` for playback speed
- VFX systems read `TrailIntensity` for motion trail strength

### AI Systems

- Pathfinding uses `Vmax` for time estimates
- Steering behaviors use `Amax`, `αmax` for maneuver planning
- Decision systems consider `Vmax` vs `Amax` tradeoffs (speed vs agility)

---

## Related Documentation

- **Perception System:** `Docs/Concepts/Core/Perception_Action_Intent_Summary.md` - Perception channels and sensors
- **AI Behavior Contracts:** `Docs/Concepts/Core/AI_Behavior_Contracts_Advisory.md` - AI system contracts
- **Wikipedia - Froude Number:** https://en.wikipedia.org/wiki/Froude_number
- **Wikipedia - Moment of Inertia:** https://en.wikipedia.org/wiki/Moment_of_inertia

---

**For Implementers:** Use `MotionProfile` component to separate sim limits from feel params  
**For Designers:** Tune acceleration/turn rates for responsiveness, FOV/animation for perception  
**For Architects:** Three-layer approach (sim/control/perception) enables independent tuning

