# Group Morale Contract

## Purpose
Define one game-agnostic contract for how group-level morale drives:
- formation posture
- anchor behavior
- splinter/rejoin decisions
- goal commitment pressure

This avoids per-project drift between Godgame and Space4X style aggregates.

## Runtime Contract
`PureDOTS.Runtime.Groups.GroupMoraleContractComponents.cs`

Key types:
- `GroupMoraleContractProfile`
- `GroupMoraleContractState`
- `GroupAnchorContractState`
- `GroupGoalCommitmentContract`
- `GroupSplinterContractState`
- `GroupMoraleTransitionEvent`

Core policy helpers:
- `GroupMoraleContract.NormalizeMorale01`
- `GroupMoraleContract.ResolvePhase`
- `GroupMoraleContract.ResolveIntent`
- `GroupMoraleContract.ShouldSplit`
- `GroupMoraleContract.ShouldRejoin`

## Scale Conventions
- Contract signals are normalized to `0..1`.
- `NormalizeMorale01` accepts either normalized values or `0..1000` style values.

## Integration Plan
1. Feed `GroupMoraleContractState` from existing aggregate + threat/cohesion systems.
2. Keep project-specific flavor in adapter systems, not in the contract.
3. Emit `GroupMoraleTransitionEvent` for telemetry and narrative hooks.
4. Gate splinter execution off `ShouldSplit` and regroup off `ShouldRejoin`.

## Current Wiring
- `GroupMoraleContractEnsureSystem` seeds default profile/state/buffer on group entities with `GroupMetrics`.
- `GroupMoraleContractAdapterSystem` computes morale phase + intent from `GroupMetrics`, `GroupAggregate`, optional anchor/goal signals, and emits transition events on phase changes.
