# Extension Request: Expand Telemetry - Analytics & Balancing Tools

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Telemetry/Analytics/AnalyticsComponents.cs` - TelemetryHistory, TelemetryTrend, TelemetryAnomalyConfig, TelemetryAnomaly, BalanceMetric, PlayerAction, PlayerSession, AggregatedMetric, MetricCorrelation, AnalyticsConfig
- `Packages/com.moni.puredots/Runtime/Runtime/Telemetry/Analytics/AnalyticsHelpers.cs` - Static helpers for trend calculation, anomaly detection, balance analysis
- `Packages/com.moni.puredots/Runtime/Systems/Telemetry/Analytics/TelemetryAnalyticsSystem.cs` - TelemetryHistorySystem, TelemetryTrendSystem, AnomalyDetectionSystem, BalanceAnalysisSystem, PlayerBehaviorSystem

---

## Use Case

Current telemetry emits metrics. Expand to support:

**Both Games:**
- **Historical trends**: Track metrics over time (not just current)
- **Anomaly detection**: Alert when metrics are outliers
- **Balance analysis**: Identify overpowered/underpowered systems
- **Player behavior**: Track what players do for UX insights

**Dev Tools:**
- Live graphs in editor
- Export for external analysis
- A/B testing infrastructure

---

## Proposed Expansion

### Current State
- `TelemetryStream` singleton buffer
- `TelemetryMetric` per-frame values

### Requested Expansion

```csharp
// Historical tracking
public struct TelemetryHistory : IBufferElementData
{
    public FixedString32Bytes MetricKey;
    public float Value;
    public uint RecordedTick;
}

public struct TelemetryTrend : IComponentData
{
    public FixedString32Bytes MetricKey;
    public float CurrentValue;
    public float AverageValue;          // Rolling average
    public float MinValue;              // In tracking window
    public float MaxValue;
    public float Velocity;              // Rate of change
    public float Acceleration;          // Change in velocity
    public bool IsAnomaly;              // Outside normal range
}

// Anomaly detection
public struct TelemetryAnomalyConfig : IComponentData
{
    public float StandardDeviationThreshold; // 2.0 = flag if > 2 std devs
    public uint MinSamplesForAnomaly;        // Need N samples first
    public uint TrackingWindowTicks;         // How far back to track
}

public struct TelemetryAnomaly : IBufferElementData
{
    public FixedString32Bytes MetricKey;
    public float ExpectedValue;
    public float ActualValue;
    public float DeviationMagnitude;
    public uint DetectedTick;
    public FixedString64Bytes Context;    // "villager_deaths spike after patch"
}

// Balance analysis
public struct BalanceMetric : IBufferElementData
{
    public FixedString32Bytes Category;   // "combat", "economy", "progression"
    public FixedString32Bytes MetricId;   // "warrior_winrate", "gold_income"
    public float TargetValue;             // Designer intent
    public float ActualValue;             // Measured
    public float Deviation;               // How far from target
    public FixedString64Bytes Suggestion; // "Consider nerfing warrior damage"
}

// Player behavior
public struct PlayerAction : IBufferElementData
{
    public uint Tick;
    public FixedString32Bytes ActionType; // "build", "miracle", "attack"
    public FixedString32Bytes Target;     // What was acted on
    public float Duration;                // How long action took
    public bool WasSuccessful;
}

public struct PlayerSession : IComponentData
{
    public uint SessionStartTick;
    public uint TotalActions;
    public float AverageActionInterval;
    public FixedString32Bytes MostCommonAction;
    public float EngagementScore;         // Derived metric
}
```

### New Systems
- `TelemetryHistorySystem` - Records metric history
- `TelemetryTrendSystem` - Calculates trends, velocity
- `AnomalyDetectionSystem` - Flags outliers
- `BalanceAnalysisSystem` - Compares to targets
- `PlayerBehaviorSystem` - Tracks player actions

---

## Example Usage

```csharp
// === Check for economy anomaly ===
var anomalies = EntityManager.GetBuffer<TelemetryAnomaly>(telemetryEntity);
foreach (var anomaly in anomalies)
{
    Debug.LogWarning($"Anomaly: {anomaly.MetricKey} - {anomaly.Context}");
}

// === Balance dashboard ===
var balanceMetrics = EntityManager.GetBuffer<BalanceMetric>(telemetryEntity);
foreach (var metric in balanceMetrics)
{
    if (math.abs(metric.Deviation) > 0.2f) // >20% off target
    {
        BalanceDashboard.Flag(metric);
    }
}

// === Export for analysis ===
TelemetryExporter.ExportToCSV("session_data.csv", historyBuffer);
```

---

## Impact Assessment

**Files/Systems Affected:**
- Expand: `Packages/com.moni.puredots/Runtime/Telemetry/`
- New: History, trend, anomaly systems

**Breaking Changes:**
- Additive - existing telemetry unchanged
- New features opt-in

---

## Review Notes

*(PureDOTS team use)*

