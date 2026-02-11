# PureDOTS Outlook/Profile Scenario Plan

**Status**: Draft (nightly)
**Scope**: Headless scenario showcases for profile/outlook/decision weights

## Goals
- Validate Profile -> Policy -> Decision Weight wiring with deterministic headless runs.
- Exercise both individual profiles (named actors) and aggregate profiles (crew/village).
- Keep scenario inputs small, reproducible, and telemetry-friendly.

## Profile Fixtures (entity profiles)
Use these reusable fixtures across scenarios. Each fixture includes alignment triplet, outlook axes, and behavior axes.

- **Civic Steward**
  - Alignment: Moral +0.6, Order +0.4, Purity +0.2
  - Outlook axes: Authority -0.5, Military -0.4, Economic +0.3, Tolerance +0.6, Expansion -0.2
  - Behavior axes: Boldness -0.2, Conviction +0.3, Selflessness +0.6
- **Militant Autocrat**
  - Alignment: Moral -0.2, Order +0.7, Purity +0.1
  - Outlook axes: Authority +0.8, Military +0.7, Economic +0.1, Tolerance -0.4, Expansion +0.5
  - Behavior axes: Boldness +0.6, Conviction +0.7, Selflessness -0.3
- **Trader Consortium**
  - Alignment: Moral +0.1, Order +0.2, Purity -0.1
  - Outlook axes: Authority +0.1, Military -0.5, Economic +0.8, Tolerance +0.4, Expansion +0.3
  - Behavior axes: Boldness +0.1, Conviction +0.2, Selflessness -0.1
- **Spiritual Isolationist**
  - Alignment: Moral +0.4, Order +0.2, Purity +0.7
  - Outlook axes: Authority +0.2, Military -0.6, Economic -0.7, Tolerance -0.4, Expansion -0.8
  - Behavior axes: Boldness -0.3, Conviction +0.5, Selflessness +0.2
- **Opportunist Mercenary**
  - Alignment: Moral -0.3, Order -0.2, Purity -0.1
  - Outlook axes: Authority -0.2, Military +0.5, Economic +0.4, Tolerance -0.2, Expansion +0.2
  - Behavior axes: Boldness +0.5, Conviction -0.1, Selflessness -0.5

## Outlook Axes Grid (targets)
Use these as anchors when authoring scenario tags or expected deltas:
- Authority: Egalitarian (-) <-> Authoritarian (+)
- Military: Pacifist (-) <-> Militarist (+)
- Economic: Spiritualist (-) <-> Materialist (+)
- Tolerance: Xenophobic (-) <-> Xenophilic (+)
- Expansion: Isolationist (-) <-> Expansionist (+)

## Decision Weight Surfaces (to verify)
Each scenario should demonstrate at least one of these weight surfaces:
- **ComplianceWeight**: Authority, Conviction, Morale, Grievance
- **AggressionWeight**: Military, Boldness, Selflessness, ROE
- **NegotiationWeight**: Economic, Tolerance, Military, Selflessness
- **RiskWeight**: Boldness, Military, Conviction, MoraleLow
- **PunishmentWeight**: Authority, Conviction, Selflessness, Cohesion

## Scenario Showcases (headless)
All scenario paths are **PureDOTS-only** and live under the package samples directory.

1. **headless_time_rewind_short**
   - Scenario rel: `puredots/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/headless_time_rewind_short.json`
   - Focus: Rewind profile integrity + decision weights under time pressure.
   - Profiles used: Civic Steward vs Militant Autocrat
   - Expected signals: policy.* metrics stable across rewind; event.rewind_enter/exit; compliance/negotiation weights shift with morale.

2. **worldgen_headless_smoke**
   - Scenario rel: `puredots/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/worldgen_headless_smoke.json`
   - Focus: Aggregate profile seeding and outlook tag derivation under worldgen load.
   - Profiles used: Trader Consortium vs Spiritual Isolationist
   - Expected signals: aggregate outlook tags derived from axis values; policy fields populated; no missing profile components.

Optional follow-ups (only if needed for a broader sweep):
- `puredots_firing_range_smoke.json` (combat aggression/ROE weight surface)
- `puredots_weapon_spawner_smoke.json` (risk and compliance under repeated triggers)

## Telemetry Proof Points
- `policy.*` metrics for each weight surface (compliance, aggression, negotiation, risk, punishment).
- `event.profile_axis_shift` or equivalent when axis changes occur.
- `event.mutiny_pressure_crossed` or refusal outcomes when thresholds are met.
- `scenario.exit_reason` and `invariants.json` present for validity.

## Run Matrix (nightly target)
- Run each showcase with seed 7 and 11, repeat 1.
- Validate stable decision weights across reruns unless explicitly randomized by scenario.
