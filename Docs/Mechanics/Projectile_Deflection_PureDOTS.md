# PureDOTS Projectile Deflection Model (v0)
Date: 2026-02-09
Owner: shonh
Status: draft

## Purpose
Define the shared PureDOTS deflection model (decision + resolution) so Space4x and Godgame can skin it without re-implementing timing, accuracy, and deflection logic.

## Scope
- Deflection as a shared interaction primitive (shields, armor glances, active manipulation, intercepts, control/sway).
- Decision logic for dodge vs deflect vs intercept vs ignore.
- Performance guardrails for large threat volumes.

Out of scope:
- Detailed physics equations (see `Docs/Concepts/Combat/Projectile_Deflection_Agnostic.md`).
- Game-specific VFX, theme, or ability trees.

## Cross-Domain Intent
- "Master mage" == apex operator/ship.
- "Firing range" == skill-expression testbed (archers, gunners, pilots).
- PureDOTS owns timing/accuracy/deflection primitives; games only tune parameters.

## Entity Assumptions
- Projectiles are entities with a lightweight threat profile and predictable impact windows.
- Deflection sources are entities or systems with explicit capabilities and costs.

## Deflection Sources (Shared Interface)
- Barrier/Shield: area or tethered bubble; passive absorb/deflect.
- Active Deflect: limb/weapon manipulation with timing windows.
- Intercept: projectile vs projectile or drone swarm.
- Control/Sway: ECW/mana alters projectile allegiance or trajectory.
- Armor Glance: collision-phase deflect/attenuate on impact.

## Threat Evaluation
Compute per projectile:
- impact_time_ms
- threat_score (damage * hit_probability * exposure_window)
- tags (kinetic, energy, magic, ecw, explosive, piercing)
- dodge_difficulty, deflect_resistance, control_resistance

Ignore threshold:
- If threat_score < shield_margin or armor_tolerance and resource_cost is non-trivial, do nothing.
- Enables realistic "don't waste ammo/energy on negligible threats."

## Action Selection (Dodge vs Deflect vs Intercept vs Ignore)
For each threat, evaluate:
- success_prob
- risk_reduction
- resource_cost
- timing_window viability

Utility (conceptual):
- (risk_reduction * success_prob) - resource_cost

Higher finesse entities:
- Better success_prob prediction and tighter timing windows.

## Resolution Pipeline (Layered)
Ordered resolution per threat:
1) Barrier/Shield
2) Active Deflect or Control/Sway
3) Intercept (drone/projectile)
4) Armor Glance (on impact)

Outcome types:
- nullify
- redirect
- sway/control
- glance (damage attenuated)
- fail

## Timing/Accuracy Primitives (PureDOTS)
Base parameters (per role/seat):
- reaction_time_ms
- windup_ms
- window_ms
- recovery_ms
- prequeue_ms
- arc_deg

Deflection telemetry:
- deflect_accuracy_pct
- deflect_timing_error_ms

Cadence/variance:
- cadence_s
- cadence_variance

Tracking:
- track_stability
- alignment_error_deg

## Projectile Diversity
Projectile threat profile fields (examples):
- maneuverability
- seek_strength
- signature
- deflect_resistance
- dodge_difficulty
- control_resistance
- phase/arc (line, homing, arcing, piercing)

This makes some projectiles easier to dodge but harder to deflect (and vice versa).

## Cost Model (Shared)
Costs are proportional to projectile threat and deflection type:
- mana_cost
- heat_cost
- stamina_cost
- charge_cost

## Performance Guardrails
- Cap threats per tick (top N by threat_score).
- Use windowed deflection, not per-projectile physics.
- Cache arc checks unless facing changes.
- If budget exceeded, collapse to volley-level deflection.

## Telemetry (Base Keys)
- threat_ignored_rate
- dodge_chosen_rate
- deflect_chosen_rate
- intercept_chosen_rate
- deflect_success_rate
- dodge_success_rate
- damage_prevented
- prediction_error_ms
- resource_cost_per_prevented_damage

Games may add tags, but keep base keys stable.

## Data Sketch (Components)
Deflection primitives (PureDOTS data):
- `DeflectionProfile`: reaction timing, mode biases, action caps.
- `DeflectionBudget`: energy, mana, focus, ammo.
- `DeflectionIntent`: selected response per tick.
- `DeflectionRequest`: queued actions for resolvers.
- `DeflectionEvent`: resolution outcome (success/fail).

Projectile control primitives:
- `ProjectileControlRequest`: redirect/hijack/disrupt intent.
- `ProjectileControlState`: active control parameters.
- `ProjectileSignature`: identifiers for ECM or mana sway.

Threat sketch (for planners, not physics):
- threat_score
- impact_time_ms
- dodge_difficulty
- deflect_resistance
- control_resistance
- tags[]

## Integration Notes
- Use `Projectile_Deflection_Agnostic.md` for physics math and force modeling.
- The decision layer should be deterministic under fixed seeds.
- Don't fork this model in game layers; only tune parameters and tags.
