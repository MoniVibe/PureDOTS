# Extension Request: Standardize Villager Stat Components

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Godgame  
**Priority**: P0  
**Assigned To**: TBD

---

## Use Case

Godgame currently mirrors its villager stat components into PureDOTS placeholder structs via a sync system to satisfy registry queries. To remove duplication and keep registries/AI utilities consistent across titles, PureDOTS needs to own canonical stat components (needs, mood, combat) that match the schemas Godgame and other games consume.

---

## Proposed Solution

**Extension Type**: New/updated components

**Details:**
- Add or confirm the following Burst-safe components under `PureDOTS.Runtime.Components`:
  - `VillagerNeeds`: `byte Food`, `byte Rest`, `byte Sleep`, `byte GeneralHealth`, `float Health`, `float MaxHealth`, `float Energy`.
  - `VillagerMood`: `float Mood`.
  - `VillagerCombatStats`: `float AttackDamage`, `float AttackSpeed`, `Entity CurrentTarget`.
- Include XML docs mirroring the Godgame structures to keep registry usage clear.
- Ship default bakers/tests to ensure fields are writable via `GetComponentRW` and align with `Godgame.VillagerPureDOTSSyncSystem` expectations.

---

## Impact Assessment

**Files/Systems Affected:**
- `Packages/com.moni.puredots/Runtime/Components/VillagerNeeds.cs`
- `Packages/com.moni.puredots/Runtime/Components/VillagerMood.cs`
- `Packages/com.moni.puredots/Runtime/Components/VillagerCombatStats.cs`
- Tests under `Packages/com.moni.puredots/Tests` verifying default values and registry compatibility

**Breaking Changes:**
- Additive if components already exist; if schemas differ, migration would be limited to component field renames in consuming games.

---

## Example Usage

```csharp
// Godgame sync system today
var src = SystemAPI.GetComponentRO<VillagerNeeds>(entity).ValueRO;
var dst = SystemAPI.GetComponentRW<PureDOTS.Runtime.Components.VillagerNeeds>(entity);
dst.ValueRW.Food = src.Food;
dst.ValueRW.Health = src.Health;
dst.ValueRW.MaxHealth = src.MaxHealth;
dst.ValueRW.Energy = src.Energy;
```

---

## Alternative Approaches Considered

- **Keep Godgame-only stat components with ad-hoc sync systems**
  - **Rejected:** duplicates schemas, adds maintenance overhead, and blocks other games from reusing stat utilities.
- **Use only combat-centric stats in PureDOTS**
  - **Rejected:** registry/AI systems need mood/needs data for scheduling and telemetry across games.

---

## Implementation Notes

- See `Godgame/Docs/PureDOTS_Stats_Foundation_Request.md` for deeper context and the original schemas.
- Keep components Burst-friendly and avoid managed defaults; include unit tests for serialization and registry consumption.
