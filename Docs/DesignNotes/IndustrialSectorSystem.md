# Industrial Sector System Concepts

## Goals
- Extend facility-level simulation with settlement-wide industrial sectors (Heavy, Light, Agriculture, etc.) that aggregate performance, disparity, and knowledge spillover.
- Provide deterministic metrics for AI management, economic systems, and player feedback.
- Ensure data-driven integration with production chains, skill progression, and scheduler infrastructure.

## Facility Metrics
- Facility component additions:
  - `FacilityTier` (I–VIII), aligns with tech unlocks.
  - `ChiefLevel` (0–100), the lead’s proficiency.
  - `WorkforceSkill` (0–100), average crew competence.
  - `Uptime` (0–1), availability factoring maintenance & inputs.
  - `Capacity` (units/time).
  - `SpecializationTags` (`DynamicBuffer<FacilityTag>`).
- Derived multipliers:
  - `ChiefEffect = 1 + 0.6 * (ChiefLevel / 100)`
  - `WorkforceEffect = 1 + 0.4 * (WorkforceSkill / 100)`
  - `UptimeEffect = Uptime`
  - `TierEffect = pow(1.25f, BaseTier - 1)`
  - `Specialization` bonus if facility tags match current orders.
- Effective Facility Score:
  ```
  FacilityScore = pow(Capacity, 0.85f)
                * TierEffect
                * ChiefEffect
                * WorkforceEffect
                * UptimeEffect
                * Specialization;
  ```

## Industry Aggregation
- Group facilities by settlement + industry category.
- Weighted metrics:
  - Weight per facility = normalized `pow(Capacity, 0.5f)` (soft emphasis on large plants).
  - Compute trimmed weighted mean of `FacilityScore` (trim top/bottom 10% when sample size ≥ 10).
  - Star bonus: let `Outlier = maxScore / Base`. Apply `StarBonus = min(0.25f, 0.08f * (Outlier - 1))`.
  - `IndustryIndex = Base * (1 + max(0, StarBonus))`.
- Map to industry level (1–10):
  ```
  IndustryLevel = round(clamp(1, 10,
                     log(IndustryIndex / K) / log(1.6f)));
  ```
  - `K` chosen in data config so early settlements land ~Level 3–4.
- Disparity:
  - `TopQ = 80th percentile FacilityScore`.
  - `BottomQ = 20th percentile`.
  - `Disparity = (TopQ - BottomQ) / (TopQ + ε)`.

## Knowledge Spillover
- Per tick (weekly cadence via scheduler):
  ```
  Spillover = 0.02 * Proximity * Collaboration * (IndustryLevel / 10);
  workforce_skill += Spillover * (avg_top_quartile_skill - workforce_skill);
  chief_level     += 0.4 * Spillover * (avg_top_quartile_chief - chief_level);
  ```
- `Proximity`: 1.0 same settlement, 0.6 same province, 0.3 distant.
- `Collaboration`: policy-driven (0–1).
- Apply increases via `SchedulerAndQueueing` tasks to ensure deterministic updates.

## Management Pressure & Actions
- Management pressure score:
  ```
  Pressure = 0.5 * Disparity
           + 0.3 * max(0, TargetLevel - IndustryLevel)
           + 0.2 * ContractRisk;
  ```
- Thresholds trigger AI decisions (queued tasks/events):
  - Tier 1 (≥0.2): training weeks, maintenance blitz, minor tooling upgrades.
  - Tier 2 (≥0.35): headhunt chiefs, performance contracts, mentor programs.
  - Tier 3 (≥0.5): consolidation, license tech, regional policy boosts.
- Actions implemented as scheduled commands affecting facilities (modify components, spawn events, adjust economy).

## Hiring Model
- Headhunting pulls chief candidates from distributions biased by settlement renown & industry level.
- Costs include gold/influence, relocation, non-compete risk; success may trigger culture mismatch debuff.
- Managed via event system + scheduler (timed contract adjustments).

## Orders & Competitiveness
- Define `QualityScore = normalize(TierEffect * ChiefEffect * WorkforceEffect)`.
- Contract success when `QualityScore * UptimeEffect >= OrderQualityDemand`.
- Industry level influences contract availability & pricing; integrate with economy/trade service.

## UI & Feedback
- Provide industry dashboard:
  - `IndustryLevel`, `IndustryIndex`, trend arrows.
  - Disparity gauge (Healthy/Gap/Crisis).
  - Highlight star facility, top/bottom quartile.
  - Recommendations & forecast (time to next level under current policies).
- Hook into narrative situations (poaching, tooling recall, guild charters).

## Data Representation
- Extend registries with:
  - `FacilityRegistryEntry` storing new metrics.
  - `IndustryRegistry` per settlement/category with aggregated stats, star facility id, disparity.

## Technical Considerations
- **SoA storage** for facility metrics to support Burst calculations.
- **Parallel aggregation** via `IJobChunk` or `IJobParallelFor` grouped by settlement/category.
- **Trimmed mean** implemented deterministically: sort by score using stable index, slice by integer thresholds.
- **Scheduler integration** for spillover and management actions (weekly cadence).
- **Rewind**: record industry snapshots or recompute from facility history each playback.
- **Economy hooks**: adjust resource consumption for upgrades, headhunting costs.
- **Space4x adaptation**: facilities include mobile shipyards, orbital stations; proximity uses fleet logistics graphs.

## Authoring & Config
- `IndustryConfig` ScriptableObject:
  - `K` constant, star bonus cap, spillover rate, pressure thresholds.
  - Policy modifiers (collaboration boosts, renown effects).
- `FacilityTagDefinition` for specialization mapping to product categories.
- Bakers translate configurations into blobs consumed by runtime systems.

## Testing
- Unit tests for facility score calculation and trimmed mean logic.
- Integration tests verifying industry level shifts as facilities upgrade/downgrade.
- Simulation tests for spillover convergence with/without collaboration.
- Determinism/rewind tests covering management actions and hiring events.
