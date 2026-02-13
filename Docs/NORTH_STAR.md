# North Star

## Mission
Build **Space4X** as a 3D galaxy simulation of crewed capital ships where each carrier is a **village in space**: a hull, a society, and a chain of command.

## Project Relationship
- **PureDOTS**: shared deterministic ECS runtime and contracts.
- **Space4X**: flagship game expression of those contracts in a galaxy-scale setting.
- **Godgame**: sibling project proving the same runtime patterns in a different fantasy/society domain.

## Non-Negotiables
- **Headless is truth**: correctness is proven in headless artifacts, not in scene visuals.
- **Determinism first**: same seed + same inputs + same build => same outcomes.
- **Presentation is projection**: visuals follow simulation; visuals never become authority.
- **Carrier-as-village design**: combat, logistics, crew life, doctrine, and politics all matter on the same hull.
- **Debuggable by humans**: every major behavior must be inspectable via telemetry + Entities tooling (Archetypes, Systems, Journaling).

## Control Ladder
Player agency must remain coherent across this zoomable chain:

`God -> Admiral -> Captain -> Officer -> Pilot`

Each rung changes scope, not simulation truth.

## What “Good” Looks Like
- Galaxy movement feels large-scale and continuous.
- Carriers read as living organizations, not empty stat blocks.
- Orders propagate through command layers with visible consequences.
- Headless runs produce artifact proof for determinism, perf, and invariants.

## Scope Guard
Netplay is intentionally deferred. Keep architecture netplay-safe, but do not trade single-player determinism for premature multiplayer work.
