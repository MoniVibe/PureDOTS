# Mechanic: Stealth Framework

## Overview

**Status**: Concept  
**Complexity**: Moderate  
**Category**: Systems Infrastructure

**One-line description**: Provide deterministic light-aware stealth, perception, and suspicion services that gameplay projects can extend for covert mechanics.

## Core Concept

PureDOTS ships reusable component contracts and systems so projects (Godgame, future titles) can stage stealth encounters without re-inventing core math. The framework separates **stealth profiles** (agent-facing data), **perception sensors** (observer-facing data), **environment probes** (light/sight sources), and **suspicion routing** (stateful escalation). Projects layer fantasy-specific content—blessings, tech, NPC behaviours—on top.

## How It Works

### Basic Rules

1. Agents with `StealthProfile` request light exposure data from sector probes each fixed tick.  
2. Observers with `PerceptionSensor` run deterministic opposed checks, using cached light modifiers plus vigilance state.  
3. Stealth events write suspicion deltas into `SuspicionScore` buffers; configured thresholds emit counterintelligence events.  
4. Optional `SpectralSight` and `IllusionProbe` components modify perception to reveal hidden or spectral entities.

### Parameters and Variables

| Parameter | Default | Range | Effect |
|-----------|---------|-------|--------|
| `StealthProfile.BaseScore` | 0.6 | 0-1 | Baseline stealth value prior to modifiers. |
| `PerceptionSensor.BaseScore` | 0.5 | 0-1 | Observer acuity before light or assist bonuses. |
| `LightExposure.Intensity` | 0.5 | 0-1 | Cached light level per probe sample. |
| `LightExposure.Modifier` | 0 | -0.4 to +0.2 | Applied to perception; darkness penalises up to -0.4, full light boosts +0.2. |
| `SuspicionScore.Value` | 0 | 0-1 | Accumulated suspicion; thresholds defined per project. |

### Edge Cases

- **Critical Success / Failure**: Deterministic roll bands trigger identity or motive exposure events.  
- **Conflicting Light Sources**: Combine via weighted average (configurable). Projects may override to use max/priority rules.  
- **Spectral Sight**: Entities halve darkness penalties and gain access to spectral query buffers.  
- **Illusion Layers**: Environment can register illusion volumes; perception queries cross-check to avoid false negatives.

## Player Interaction (Project Notes)

Projects consume the framework by:
- Authoring ScriptableObject configs (`StealthTuningConfig`) for light curves, suspicion decay, crit thresholds.  
- Creating bakers that attach `StealthProfile` or `PerceptionSensor` to relevant prefabs.  
- Bridging results into UI (suspicion meters, detection prompts) via event streams.

## Balance and Tuning

### Balance Goals

- Keep light influence significant but not absolute; tuning curves encourage environmental play.  
- Suspicion should build gradually, giving designers room for foreshadowing and recovery.  
- Framework remains genre-neutral—projects can plug in fantasy- or sci-fi-specific reactions.

### Tuning Knobs

1. **Light Attenuation Curve** (`StealthTuningConfig.LightCurve`).  
2. **Vigilance Assist Scaling** (group detection boosts).  
3. **Suspicion Decay** (per tick or triggered).  
4. **Critical Band Width** (P crit success/failure thresholds).  
5. **Probe Update Cadence** (trade-off between responsiveness and cost).

### Known Issues

- Requires performant light sampling; need integration with rendering or gameplay-authored probes.  
- Multiplayer/replay compatibility needs shared RNG seeds and state replication.

## Integration with Other Systems

| System | Interaction | Priority |
|--------|-------------|----------|
| Spatial Services | Provide probe positions and cached intensity values. | Critical |
| Event & Transition System | Emits stealth success/failure, suspicion escalations. | High |
| Alignment / Morale | Projects may map exposure to morale changes. | Medium |
| Presentation Bridges | Visual/Audio cues respond to stealth state. | Medium |

## Implementation Notes

- Implement `StealthProfile`, `PerceptionSensor`, `LightExposure`, `SuspicionScore`, `SpectralSight`, `IllusionProbe` components in PureDOTS runtime.  
- Add `StealthSystemGroup` with deterministic job scheduling: sample probes → compute light modifiers → run opposed checks → update suspicion and events.  
- Provide `StealthAuthoringBaker` templates to populate components from config assets.  
- Expose `StealthDebugHUD` (Entities Graphics overlay) showing light intensity, stealth value, suspicion drift for developers.

## Performance Considerations

- Cache probe results per sector/time slice to avoid redundant sampling across agents.  
- Batch opposed rolls using SIMD-friendly math; rely on `RandomNumberGenerator` seeded per tick for reproducibility.  
- Use pooled dynamic buffers for suspicion events to keep GC pressure zero.

## Testing Strategy

1. Deterministic unit tests verifying light modifier interpolation and opposed roll distribution.  
2. Scenario tests with multiple agents and observers to confirm suspicion thresholds and event ordering.  
3. Stress tests profiling hundreds of stealth checks under varying probe densities.

## Examples

- **Godgame**: Night-time covert action toggles city braziers off to suppress detection, using suspicion events to spawn inquisitions.  
- **Future RTS**: Scout drones deploy blackout fields to assist infiltrators; counterplay upgrades perception sensors.

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-10-31 | Initial draft | Captured reusable stealth framework expectations. |

---

*Last Updated: October 31, 2025*  
*Document Owner: Systems Team*
