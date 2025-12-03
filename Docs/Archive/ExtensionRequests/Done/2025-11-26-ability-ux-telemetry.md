# Extension Request: Ability UX Telemetry (Casting Latency & Cancellation)

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Space4X  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

`Docs/TODO/4xdotsrequest.md` calls out “Capture ability UX telemetry hooks (casting latency, cancellation) once the HUD layer is ready.”  
Space4X miracles/abilities are authored via `Space4XMiracleAuthoring` which bakes the shared PureDOTS components (`MiracleDefinition`, `MiracleRuntimeState`, `MiracleCaster`, `MiracleTarget`). We currently track aggregate miracle energy/cooldown statistics, but we cannot measure player-facing UX metrics such as:

- Input-to-cast latency (time from `MiracleCastRequest` to `MiracleLifecycleState.Active`)
- Cancellations and their reasons (HUD cancellation, target invalidation, resource shortfall)
- Queue time when multiple miracles contend for the same caster
- Per-cast telemetry for HUD overlays (so UI can display “Cast stalled for 1.2s” badges)

Without official hooks from PureDOTS we have to instrument these states manually, duplicating logic that already lives inside the miracle runtime. This hinders tuning (design cannot see latency histograms) and makes it impossible to share metrics with Godgame/other teams.

---

## Proposed Solution

**Extension Type**: New telemetry events + registry fields

**Details:**
- Extend `MiracleRegistryEntry` with UX-facing counters:
  - `uint LastInputTick`
  - `uint CastStartTick`
  - `uint CancelTick`
  - `MiracleCancelReason CancelReason` (`None`, `UserCancelled`, `TargetInvalid`, `Interrupted`, `InsufficientResources`)
- Update `MiracleSystem/MiracleRuntimeSystem` to populate these ticks whenever input/cast/cancel transitions occur.
- Emit structured telemetry via a new `MiracleUxTelemetryBuffer` attached to the `TelemetryStream` entity:
  ```csharp
  public struct MiracleUxTelemetry : IBufferElementData {
      public FixedString64Bytes MiracleId;
      public uint InputTick;
      public uint ActivationTick;
      public uint CancelTick;
      public MiracleCancelReason CancelReason;
      public float LatencySeconds;
  }
  ```
- Add scalar metrics to `RegistryTelemetrySystem`:
  - `registry.miracles.castLatency.avg_ms`
  - `registry.miracles.castLatency.p95_ms`
  - `registry.miracles.cancellations.total`
  - `registry.miracles.cancellations.byReason.<reason>`
- Provide a `MiracleUxTelemetryBridgeSystem` template that gameplay projects can clone to forward the buffer into their HUD/tooling (Space4X will publish it to `space4x.miracles.*` metrics and show per-ability badges).
- Document the new telemetry keys and sample usage so HUD/presentation subsystems can bind latency/cancellation data without referencing gameplay entities directly.

---

## Impact Assessment

**Files/Systems Affected:**
- `Packages/com.moni.puredots/Runtime/Miracles/MiracleRegistryEntry.cs`
- `Packages/com.moni.puredots/Runtime/Miracles/MiracleSystem.cs`
- `Packages/com.moni.puredots/Runtime/Miracles/MiracleRuntimeState.cs`
- `Packages/com.moni.puredots/Runtime/Telemetry/TelemetryStream.cs` (add UX buffer)
- `Packages/com.moni.puredots/Runtime/Telemetry/RegistryTelemetrySystem.cs`
- Docs: `Docs/Guides/Miracles.md`, `Docs/Guides/Telemetry.md`
- NUnit/PlayMode tests verifying latency and cancellation counters.

**Breaking Changes:**
- Additive; existing miracles keep functioning. `MiracleRegistryEntry` grows a few fields but keeps binary layout by appending to the struct.

---

## Example Usage

```csharp
// Space4X HUD system
var uxTelemetry = SystemAPI.GetBuffer<MiracleUxTelemetry>(_telemetryEntity);
foreach (var evt in uxTelemetry)
{
    if (evt.CancelReason != MiracleCancelReason.None)
    {
        _hud.ShowCancellation(evt.MiracleId, evt.CancelReason);
    }
    else
    {
        _hud.UpdateLatency(evt.MiracleId, evt.LatencySeconds);
    }
}
```

Telemetry metrics such as `registry.miracles.castLatency.avg_ms` would automatically flow into the shared debug HUD, allowing design to spot slow casting loops and HUD designers to surface actionable feedback.

---

## Alternative Approaches Considered

- **Per-game instrumentation**: Space4X could track latency in gameplay systems, but this duplicates miracle runtime state, risks divergence on rewind, and does nothing for Godgame or future projects.
- **External profiling**: Capturing latency via Unity Profiler or custom timers is too coarse and doesn’t attribute issues to specific miracles/cancel reasons.

---

## Implementation Notes

- Ensure latency math uses `TickTimeState.FixedDeltaTime` so rewind playback reproduces the same seconds value.
- Consider capping the UX buffer length (e.g., 64 entries) and recycling old events each frame to avoid unbounded growth.
- Hook into command/log replay so latency & cancellation events appear in time-spine replays (valuable for QA triage).

---

## Review Notes

**Reviewer**:  
**Review Date**:  
**Decision**:  
**Notes**:
