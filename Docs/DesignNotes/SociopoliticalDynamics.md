# Sociopolitical Dynamics & Situation Tracking

## Goals
- Track ongoing macro situations (raids, blockades, sieges, hostilities, market disruptions) and sociocultural shifts (alignment drift, reputation changes, collective memories).
- Provide shared data for AI, narrative, economy, and presentation across games.
- Integrate with existing systems: narrative situations, metric engine, perception, economy, buffs, faction/guild framework.

## Core Data Structures
- `SituationRegistry` singleton:
  ```csharp
  public struct SituationEntry : IBufferElementData
  {
      public SituationId Id;
      public SituationType Type; // Raid, Blockade, Siege, MarketDisruption, CulturalShift, Hostility
      public Entity ScopeEntity; // settlement/faction/system
      public SituationStatus Status; // Active, Escalating, Resolving, Dormant
      public float StartTime;
      public float Duration;
      public float Intensity; // 0-1
      public SituationFlags Flags; // e.g., Visible, Secret, PlayerInvolved
  }
  ```
- `SituationActor` buffer linking involved factions/guilds/bands with roles (attacker, defender, ally, victim).
- `CollectiveMemory` component:
  - `MemoryId`, `Scope` (faction, guild, culture group), `Sentiment` (positive/negative), `DecayRate`, `OriginSituationId`.
- `AlignmentDrift` component on scope:
  - `DriftVector`, `LastUpdateTick`, `Sources` (situations contributing to drift).
- `ReputationLedger`:
  - Per faction/guild pair stats (trust, fear, respect), updated from situations.

## Situation Lifecycle Systems
1. `SituationSpawnSystem`:
   - Listens to events (raid command, market crash, blockade) and creates `SituationEntry` with actors.
   - Applies immediate buffs/debuffs (e.g., `SiegeSupplyPenalty`).
2. `SituationProgressSystem`:
   - Updates intensity based on contributing factors (combat victories, supply levels, economic metrics).
   - Transitions status (Active → Escalating → Resolving) with thresholds.
   - Emits `SituationProgressEvent` for UI/narrative.
3. `SituationResolutionSystem`:
   - Ends situation when conditions met (siege broken, blockade lifted, market stabilizes).
   - Applies aftermath buffs/debuffs, updates collective memories, reputation, metric snapshots.
4. `SituationDecaySystem`:
   - Handles lingering effects (reconstruction, recovery, cultural trauma) by spawning follow-up situations or memories.

## Sociocultural Tracking
- **Alignment & Outlook Drift**:
  - `AlignmentDriftSystem` reads situations and adjusts faction/guild alignment vectors using weighted contributions (e.g., repeated raids push towards militaristic outlook).
- **Reputation/Relations**:
  - `ReputationSystem` updates trust/fear/respect based on situation outcomes (successful defense raises respect, betrayal raises enmity).
- **Collective Memory**:
  - `CollectiveMemorySystem` aggregates memories among like-minded individuals (shared culture/guild/faction) with decay over time.
  - Memories influence narrative choices, buff triggers (e.g., revenge motivation), recruitment decisions.
- **Culture Drift**:
  - Works with `SkillProgression` and `Economy` (e.g., blockades causing scarcity shift culture towards austerity).
- **Mediation**:
  - Villages, guilds, elites, or famed individuals can mediate feuds via `MediationRequest` entries.
  - Participation requires both parties' consent; high-opportunism (chaotic/corrupt) sides may exploit mediation, reducing future willingness for rivals to participate.
  - Alignment compatibility gates mediation attempts (extreme opposites rarely agree).
  - Success reduces threat levels, improves relations, grants mediator reputation/influence bonuses, and creates positive memories.
  - Failure escalates hostilities, spreads negative memories, and may shift feud ownership to mediators while lowering mediation likelihood.
- **Opportunistic Betrayals**:
  - Rare “dark” event triggers (e.g., red weddings, surprise massacres, blood rituals) can replace or sabotage mediation/peace talks.
  - Triggered by alignment thresholds, high opportunism, or narrative events.
  - Spawn high-intensity situations (massacre, ritual) with severe reputation fallout, buffs/debuffs (fear, curse), and rapid escalation to total war or rebellion.
- **Belief Conflict & Crypto Worship**:
  - Corrupt spiritual authorities may forcibly convert others, generating `ConversionPressure` situations.
  - Targets may convert genuinely or become cryptos (secret believers), affecting loyalty metrics.
  - High crypto presence enables narrative events (self-immolation protests, divine trials) that trigger god-player choices (save, reject, punish) leading to buffs, conversions, or turmoil.
  - Crypto networks can foment rebellions, secret rituals, or undermine authorities.
- **Ideology Anchors**:
  - Certain entities possess `IdeologyAnchor` traits, resisting belief/alignment changes and preserving minority ideologies.
  - Anchors receive automatic popularity floor buffs to keep ideologies from vanishing entirely.
  - Provide storytelling hooks: anchors may become rally points, prophets, or stubborn holdouts influencing cultural drift calculations.
- **Contention Issues**:
  - Define issue catalog (dragon domestication, weapon supremacy, sacrifice ethics, military vs civilian production, migration policy, crop selection).
  - Issues trigger debates, duels, demonstrations with mood modifiers (win/lose debate, duel victory proving ideology).
  - Severity determined by popularity and impact; minor issues affect individual mood, major issues affect sociopolitical metrics and may escalate to events (hostile migrants, guild duels, forbidden magic retaliation).
  - Narrative levers: rebuffed mage turns to forbidden arts, martial guild masters duel for supremacy, winners absorb defeated guilds.

## Event Integration
- Situations emit timeline events for the metric engine and narrative system (`NarrativeSituations.md`).
- Buff system handles ongoing effects (siege attrition, embargo, morale boosts).
- Economy system reacts to market disruptions (price spikes, trade rerouting).
- Perception system affects visibility (blockades reduce info, rumors spread memories).
- Faction/guild system uses reputational deltas to trigger diplomacy changes.
- Presentation uses event streams for UI overlays (siege progress bars, blockade warnings).

## Authoring & Config
- `SituationDefinition` catalog mapping trigger conditions, intensity curves, default duration, follow-on events.
- `MemoryDefinition` specifying sentiment, decay, narrative hooks.
- `AlignmentImpactProfile` for each situation type (e.g., `Siege` pushes defenders towards `Defensive` alignment).
- `ReputationModifierProfile` for cross-faction relationship adjustments per situation outcome.

## Metric Engine Hooks
- Metrics to track:
  - `active_raid_count`, `blockade_duration`, `siege_supply_index`.
  - `reputation_score` per faction pair, `enmity_index`, `cultural_drift_rate`.
  - `market_disruption_value`, `hostility_intensity`.
- Cadence: per tick for critical states, daily/weekly for aggregated reports.

## Technical Considerations
- Use SoA storage for situation arrays (type, intensity, duration) for Burst updates.
- Sort situation buffers deterministically (id or start time) each update.
- Rewind: record situation events; recompute state deterministically on playback. Maintain minimal history for collective memories.
- Integrate with `SchedulerAndQueueing` for periodic checks and follow-up triggers.
- Avoid structural changes in hot paths; use enableable tags or buffers.

## Testing
- Unit tests for situation spawning, progression, resolution thresholds.
- Integration tests covering raid→siege transitions, blockade impact on economy, reputation adjustments.
- Narrative integration tests verifying situations feed into story arcs.
- Determinism: record/playback sequences of situations and confirm identical outcomes.
