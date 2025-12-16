# AI Behavior Spec Template

_This template captures TRI's "Mind / Body / Aggregate" pillar contract for a single AI behavior running inside one Entities runtime. Duplicate this file into each game's `Docs/AI/Behaviors/ai.<domain>.<behavior>.md`, then fill in the concrete details (catalog IDs, registry names, cadence tick counts, telemetry signals, etc.). All cadence values must be expressed as integer tick divisors (no Hz)._ 

## 1. Identity & Scope
- **Behavior Id**: `ai.<domain>.<behavior>`
- **Domain / Feature**: _What gameplay pillar does this behavior live under?_ (e.g., Space4X mining loop, Godgame logistics)
- **Summary**: _One-paragraph description of what the agents are doing and why._
- **Primary Entities / Components**: _List the core archetypes, singletons, and buffers this behavior touches._
- **Supported Modes**: _Headless / presentation / replay. Headless contract is always authoritative._
- **Dependencies**: _Other systems, registries, or catalogs this behavior assumes are present._

## 2. Pillar Cadence Contract
| Pillar | SystemGroup slice | Cadence (ticks) | Gate Condition | Notes |
|--------|-------------------|-----------------|----------------|-------|
| Body | `_SystemGroupName_` | `BodyCadenceTicks (usually 1)` | `CadenceGate.ShouldRun(time.Tick, BodyCadenceTicks)` *or* `Always run` | _Movement/execution_ |
| Mind | `_MindGroupName_` | `MindCadenceTicks` | `CadenceGate.ShouldRun(..., MindCadenceTicks)` | _Intent planning and task resolution_ |
| Aggregate | `_AggregateGroupName_` | `AggregateCadenceTicks` | `CadenceGate.ShouldRun(..., AggregateCadenceTicks)` | _Telemetry / policy rollups_ |

Explain why each cadence was chosen and how pillars stay deterministic when cadences differ.

## 3. Body Pillar (Execution)
- **Systems**: _List the Body pillar systems that execute intent (movement, interaction, harvesting, delivery, etc.)._
- **Queries / Filters**: _Key components/tags required to participate (e.g., `GameWorldTag`, `RuntimeMode.IsHeadless`, `MiningVessel`)._
- **Inputs**: _Components produced by Mind/Aggregate pillars that Body consumes (orders, assignments, stats, etc.)._
- **Outputs**: _State written by Body (resource buffers, telemetry samples, job phase updates)._ 
- **Invariants**: _List numeric/logic invariants Body must maintain (capacity bounds, cooldown adherence, etc.)._
- **Failure Handling**: _Describe recovery when targets disappear, resources deplete, or stores fill._

## 4. Mind Pillar (Goals / Intent)
- **Cadence**: `MindCadenceTicks = ___` and explicit `CadenceGate` usage at the top of all Mind systems.
- **Systems**: _Enumerate the planning / decision systems and what they compute._
- **Goal Graph**: _Outline the state transitions (Idle → Acquire Target → Execute → Deliver → Idle)._ Visuals optional.
- **Focus / Stats / Needs**: _Document which stats gate intent selection (focus, stamina, efficiency) and how they feed the cadenced planning._
- **Edge Cases**: _How Mind reacts to missing targets, saturated storehouses, or conflicting intents._

## 5. Aggregate Pillar (Rollups)
- **Cadence**: `AggregateCadenceTicks = ___`.
- **Systems**: _Telemetry, registry bridges, compliance systems triggered by this behavior._
- **Telemetry Signals**: _List the singletons/buffers recording throughput (e.g., `MiningTelemetry`, `StorehouseDeliveryLog`)._
- **Policies**: _Describe any thresholds or alerts Aggregate raises that feed back into Mind/Body._

## 6. Data & Component Contracts
| Component / Buffer | Pillar | Access | Description | Notes |
|--------------------|--------|--------|-------------|-------|
| `ComponentName` | Mind/Body/Aggregate | RO/RW | _What data it holds_ | _Authoring source / scenario knobs_ |

Call out any new components/bakers the behavior introduces and where they should live (PureDOTS vs game glue).

## 7. Cadence Gate Example
Provide copy-ready sample code showing the correct early-return pattern:
```csharp
[BurstCompile]
public partial struct ExampleMindSystem : ISystem
{
    private const int MindCadenceTicks = ___;

    public void OnUpdate(ref SystemState state)
    {
        if (!CadenceGate.ShouldRun(ref state, MindCadenceTicks))
        {
            return;
        }

        // Mind logic
    }
}
```
Mention any synchronization assumptions (e.g., Mind cadences align to registry refresh cadences).

## 8. Headless & Testing Checklist
- **Golden Scenario(s)**: `_scenario_id` (CLI invocation + expected report fields/invariants).
- **Stress Scenario(s)**: `_scenario_id` (scale/perf focus).
- **Automated Tests**: _List NUnit / scenario tests required. State whether determinism hashes are compared._
- **Invariants & Telemetry**: _Call out every counter that must move for PR acceptance (e.g., `oreInHold`, `oreDeposited`, `storehouseInventory`)._

## 9. Presentation / Hand-off Notes
- _List the tags, logs, or TODO hooks the presentation lane can pick up later (no rendering work here)._ 
- _Document any state exposure requirements (e.g., `StorehouseDropoffAssignment` for UI lines)._ 

## 10. Implementation Checklist
Numbered list of concrete work items (systems, components, scenarios, tests, docs, telemetry) that must land before the behavior is considered complete.

---
**Authoring Notes**
- Keep docs ASCII-only and concise.
- Cross-link to TRI briefings or other specs when necessary instead of duplicating content.
- When cloning this template, replace every placeholder with concrete values; empty sections fail headless review.
