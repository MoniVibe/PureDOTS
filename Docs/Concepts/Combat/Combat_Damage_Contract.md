# Combat Damage and Integrity Contract (v0)

Owner: PureDOTS (shared contract). Do not place under C:\dev\Tri.

## Intent

Define a no-illusion, headless-provable combat damage model that is local-impact based,
layered (shield/armor/hull/modules), and deterministic across Space4X and Godgame.

See also: [Core Combat Mechanics](../Core/Combat_Mechanics_Core.md), [Combat Damage Component Schema](Combat_Damage_Component_Schema.md).

## Scope

This contract covers:
- Damage routing by local impact, not a global health pool.
- Layered mitigation (shields/armor/hull), spill-through, and module integrity.
- Behavior reactions at individual and aggregate levels during combat.
- Deterministic design rolls for armor/shields/modules.

## Combat Spine (Behavior -> Damage)

Sense -> Evaluate -> Commit -> Engage -> Impact -> Resolve -> React -> Recover

Sense: perception and comms provide target and threat data.
Evaluate: intent + profile + cohesion select goals and risk posture.
Commit: set combat posture, rules of engagement, and target priorities.
Engage: execute maneuver/weapon use via movement + power constraints.
Impact: hits generate localized damage events.
Resolve: shields/armor/hull/modules process damage deterministically.
React: tactics update from damage feedback and morale/cohesion.
Recover: repair/refit, shield regen, and damage control.

## Contract Statements (Machine-Provable)

- No global HP: every hit resolves to a segment; only that segment (and declared spill) is damaged.
- Layer order is authoritative: Shield -> Armor -> Hull -> Modules.
- Coverage truth: if a layer does not cover the impact arc, it cannot mitigate.
- Power gating: shield strength and regen are bounded by allocated power.
- Seat authority: weapon/shield operations require seats or explicit automation cover.
- Damage types are explicit and resolved per layer; no universal damage number.
- Penetration/bypass are explicit; if a layer is bypassed it does not absorb.
- Damage results are deterministic given hit + state; no runtime randomness.
- Module damage only occurs from segment-local spill-through or explicit internal events.
- Repairs and refits must consume time and resources; no instant restoration.

## Local Impact Model (Segments)

Each combat entity defines a segment map:
- SegmentId: stable identifier (data-driven, not runtime-generated).
- SegmentBounds: local AABB/OBB or volume for hit routing.
- SegmentState: shield/armor/hull refs + integrity + flags (breached, vented).

Hit resolution:
1) Map impact position to SegmentId.
2) Fetch SegmentState and resolve layers in order.
3) Apply spill-through to modules in the same segment (and optional neighbors).

## Layers

Shields:
- Coverage models: bubble, arc, wrap, or directional segments.
- Can allow or block friendly fire explicitly.
- Can create "holes" on overload or explosive hits (coverage drops locally).
- Regen is power-gated and tech-dependent.

Armor:
- Types: ablative, reactive, adaptive, reflective, brittle, composite.
- Provides resistances per damage type and may define seep-through ratio.
- Can be refit (replace or convert type) via production.

Hull:
- Baseline structural integrity per segment.
- Breach thresholds trigger secondary effects (pressure loss, fires, module faults).

Modules:
- Integrity per module; in-segment modules receive spill-through damage.
- Specific modules may have sensitivities (heat, EMP, shock).

## Damage Types (Agnostic Defaults)

Define damage as a vector of typed amounts; examples:
- Kinetic: high penetration, strong structural impact, spall risk.
- Explosive: area effect, shock load, can create shield holes.
- Thermal: heats armor/shields/modules; reduces regen and efficiency.
- EM/Disruption: reduces power effectiveness, can disable electronics.
- Corrosive: damages armor/hull over time; increases seep-through.
- Radiation: affects crew/morale and sensitive modules.

Games can add more types, but all must map to resist/pen/bypass tables.

Damage events also carry:
- ImpactNormal and IncomingDirection (for angle-based effects).
- Impulse and Heat payloads (for knockback and thermal load).

## Formulae (Deterministic)

Variables per layer:
- D: incoming damage (per type).
- C: coverage [0..1] at impact arc.
- B: bypass [0..1] from weapon vs layer.
- R: resistance [0..1] (tech + health scaled).
- P: penetration [0..1] (weapon vs hardness).
- S: spill-through [0..1] (layer property).
- A: impact angle factor [0..1] from dot(-IncomingDirection, ImpactNormal).

P = clamp(P * A, 0..1)
EffectiveMitigation = clamp(R - P, 0..1)
Mitigated = D * C * (1 - B) * EffectiveMitigation
Unmitigated = D - Mitigated
LayerDamage = Unmitigated * C * (1 - B)
DamageOut = Unmitigated * S

Notes:
- For shields, LayerDamage drains shield strength; holes reduce C locally.
- For armor, LayerDamage ablates; remaining DamageOut may seep into hull.
- For hull, DamageOut feeds module spill-through or internal effects.

## Module Damage and Cascades

Modules define:
- Integrity (health) and fault thresholds.
- Sensitivity multipliers per damage type.
- Failure mode: Degraded -> Offline -> Catastrophic.

Cascades:
- Reactor/weapon failures can emit internal damage events in same segment.
- Fires and heat apply overtime effects until extinguished or vented.

## Behavior and Performance (Individual + Aggregate)

Individual (pilot/crew):
- Profile biases clearance, aggression, and target stickiness.
- Morale/health affects aim precision, reaction time, and risk tolerance.

Ship-level (aggregate):
- Combat posture selects power focus and target priorities.
- Damage control shifts resources to shields/repairs or weapon output.

Wing/Fleet (aggregate):
- Cohesion/comms quality gates coordinated maneuvers and formation tightness.
- Leadership profiles bias engagement range and retreat thresholds.

All levels are bounded by physical limits; no level can bypass actuation or power limits.

## Design and Tech Determinism

Armor/shield/module specs are generated by deterministic design rolls:
- Seed = DesignId + TechTier + DesignerStats.
- Rolls occur at design time, not runtime.
- Revisions replace specs; no random drift in live combat.

## Repair and Refit

- Repairs consume time + resources and occur at facilities or via onboard teams.
- Refit swaps armor/shield types and updates segment specs.
- Emergency field repairs are limited and reduce quality.

## Telemetry Hooks (v0)

- DamageIn/Out per layer and per segment.
- Shield coverage/holes and regen rate.
- Module fault counts and downtime.
- Breach events and secondary effects.

## Headless Proof Slices (v0)

1) SHIELD_HOLE: explosive hit creates a local hole; follow-up passes only through that arc.
2) ARMOR_PEN: same hit vs different armor types yields different hull damage.
3) SEGMENT_LOCALITY: hit in segment A does not damage modules in segment B.

## Repo Split

PureDOTS:
- Segment/Layer data, damage routing, mitigation, spill-through, repair hooks.
- Deterministic design spec schema and roll rules.

Space4X/Godgame:
- Concrete armor/shield tech, weapon catalogs, and balance tables.
