# Agent 1: BC1016 Fixes – Completed

- `BandFormationSystem.GoalToDescription` now uses cached `FixedString128Bytes` constants (no Burst string ctor).
- `SpellEffectExecutionSystem.ApplyShieldEffect` now uses cached `FixedString64Bytes` for the default shield buff id.

Status: DONE. Domain reload should no longer emit BC1016 for these sites.

