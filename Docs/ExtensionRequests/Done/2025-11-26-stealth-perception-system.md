# Extension Request: Stealth & Perception System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Detection/DetectionComponents.cs` - VisibilityState, StealthStats, PerceptionStats, DetectionResult, VisibilityZone, DetectionConfig, AlertState, CoverPoint
- `Packages/com.moni.puredots/Runtime/Runtime/Detection/DetectionHelpers.cs` - Static helpers for stealth/perception calculation, detection checks, alert management

---

## Use Case

Stealth and perception mechanics are needed for:

**Godgame:**
- Spies infiltrating villages
- Assassins stalking targets  
- Thieves avoiding guards
- Ghosts and hidden entities
- Environmental concealment (fog, darkness, crowds)

**Space4X:**
- Cloaked ships
- Sensor ranges and detection
- Stealth bombers/scouts
- Electronic warfare

---

## Proposed Components

```csharp
// === Detection States ===
public enum VisibilityState : byte
{
    Exposed = 0,      // Fully visible (0% stealth bonus)
    Concealed = 1,    // Behind cover, in crowds (+25%)
    Hidden = 2,       // Actively sneaking (+50%)
    Invisible = 3     // Magical/tech invisibility (+75%)
}

// === Core Components ===
public struct StealthStats : IComponentData
{
    public float BaseStealthRating;     // Skill-based hiding ability
    public float EquipmentBonus;        // Cloaking device, shadow cloak
    public VisibilityState CurrentState;
    public float MovementPenalty;       // Running = harder to hide
    public float EnvironmentBonus;      // Darkness, fog, etc.
}

public struct PerceptionStats : IComponentData
{
    public float BasePerceptionRating;  // Detection ability
    public float EquipmentBonus;        // Night vision, sensors
    public float AlertnessLevel;        // 0=sleeping, 1=alert, 2=searching
    public float DetectionRadius;       // How far can detect
}

public struct DetectionResult : IBufferElementData
{
    public Entity DetectedEntity;
    public float Confidence;            // 0-1, how certain of detection
    public float3 LastKnownPosition;
    public uint DetectionTick;
    public bool IsCurrentlyVisible;     // Still in sight?
}

// === Environmental Modifiers ===
public struct VisibilityZone : IComponentData
{
    public float3 Center;
    public float Radius;
    public float StealthModifier;       // +0.3 = easier to hide, -0.3 = harder
    public FixedString32Bytes ZoneType; // "darkness", "fog", "crowd"
}

// === Detection Check Config ===
public struct DetectionConfig : IComponentData
{
    public float BaseSuccessChance;     // 50% baseline
    public float DistanceFalloff;       // Harder to detect at range
    public float MovementDetectionBonus;// Moving targets easier to spot
    public float AlertnessMultiplier;   // Alert guards detect better
    public uint CheckIntervalTicks;     // How often to roll detection
}
```

### New Systems
- `StealthStateSystem` - Updates visibility state based on environment
- `PerceptionCheckSystem` - Rolls detection against stealth
- `AlertnessSystem` - Entities become alert when detecting suspicious activity
- `VisibilityZoneSystem` - Tracks environmental modifiers

---

## Example Usage

```csharp
// === Spy attempting infiltration ===
// StealthStateSystem evaluates:
// - Base stealth: 70
// - Equipment: +10 (shadow cloak)
// - Environment: +20 (night time)
// - Movement: -15 (walking)
// Final stealth: 85

// === Guard perception check ===
// PerceptionCheckSystem rolls:
// - Base perception: 50
// - Equipment: +5 (torch)
// - Alertness: x1.0 (normal patrol)
// - Distance: -10 (30m away)
// Final perception: 45
// Roll: 85 vs 45 = spy remains undetected

// === Space4X: Cloaked ship ===
var stealth = new StealthStats {
    BaseStealthRating = 80,
    CurrentState = VisibilityState.Invisible,
    EquipmentBonus = 30 // Cloaking device
};
// Sensor sweep system uses same detection math
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Detection/` directory

**Breaking Changes:** None - new system

---

## Review Notes

*(PureDOTS team use)*

