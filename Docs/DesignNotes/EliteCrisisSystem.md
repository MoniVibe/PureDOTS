# Elite Crisis & Political Domino Concepts

## Goals
- Model elite/ruler actions (assassinations, marriages, betrayals, feuds) and their cascading effects on security, diplomacy, economy, and unrest.
- Integrate with factions/guilds, sociopolitical dynamics, buffs, economy, and narrative systems.
- Provide data-driven hooks for AI decision-making and player feedback.

## Core Components
- `EliteRegistry` (extension of elite governance plan):
  - `EliteProfile`: family/house id, rank, influence, loyalty, personal outlook.
  - `EliteStatus`: alive, under protection, exiled, deceased.
- `PoliticalEvent` buffer:
  ```csharp
  public struct PoliticalEvent : IBufferElementData
  {
      public PoliticalEventType Type; // AssassinationAttempt, MarriageAlliance, Betrayal, FeudEscalation, Coup
      public Entity PrimaryElite;
      public Entity SecondaryElite;
      public Entity ScopeEntity; // settlement/system
      public float Timestamp;
      public PoliticalEventFlags Flags;
  }
  ```
- `SecurityState` component on settlements/systems:
  - `AlertLevel`, `SecuritySpending`, `ParanoiaIndex`, `LockdownStatus`.
- `TensionState` component:
  - `ThreatLevel`, `CivilUnrest`, `RebellionRisk`, `Morale`.
- `WarRiskState` on faction pairs:
  - `AggressionScore`, `FeudIntensity`, `TotalWarThreshold`.

## Event Flow
1. **Elite Action Detection** (`EliteActionSystem`):
   - Monitors narrative situations, sociopolitical dynamics, player/AI commands.
   - Creates `PoliticalEvent` entries with context (targets, outcomes).
2. **Security Response System**:
   - On assassination attempt or coup: increase `AlertLevel`, apply security buffs, trigger lockdown events (affecting trade/navigation).
   - Adjust `SecuritySpending`, spawn `SecurityTask` in scheduler.
3. **Diplomatic Impact System**:
   - Marriages: reduce threat level, improve relations between factions/guilds; apply buffs (`AllianceStability`).
   - Betrayals: increase `AggressionScore`, apply debuffs (`TrustErosion`).
   - Feuds escalate `FeudIntensity`; when threshold exceeded, create `SituationEntry` (war, raid) via sociopolitical system.
4. **Economy Impact System**:
   - Security lockdown increases operational costs, reduces market throughput.
   - Marriage alliances may reduce tariffs or improve trade routes.
5. **Unrest & Rebellion System**:
   - High `ParanoiaIndex` + `CivilUnrest` → spawn `RebellionSituation` in sociopolitical registry.
   - Apply buffs/debuffs to populations (fear, defiance). Tie into skill progression (resistance training) and industrial output penalties.

## Cascading Effects
- `AssassinationAttempt` → Security lockdown (buff), trade disruption (`EconomySystem`), fear buffs/debuffs, potential rebellion if prolonged.
- `MarriageAlliance` → reduces threat, improves diplomacy, may create `CollectiveMemory` positive sentiment, decreases `WarRisk`.
- `Betrayal` → increases `TensionState`, `WarRisk`, triggers buff (`Vengeance`), risk of feud escalating to war.
- `Feud` → escalate via situation system; if intensity > threshold, triggers `TotalWar` event impacting multiple systems.
- `TotalWar` → interacts with `SociopoliticalDynamics` (sieges, blockades), economic shocks, may create `Rebellion` if war fatigue high.
- `Mediation` → equal or higher-ranking elite intervenes:
  - Success: reduces feud intensity, improves relations, grants mediator reputation/influence buff, may spawn positive `CollectiveMemory`.
  - Failure: increases tension, damages mediator reputation, potentially escalates to betrayal or war.
  - Godgame ritual: treated as miniature ritual granting the god-player intervention options (miracle spend, favor checks) to influence success, apply buffs, or impose sanctions on participants.
- `DynastySanction` → xenophobic patriarch/matriarch disowns family members for cross-cultural marriages:
  - Triggers feud/conflict with disowned branch, spawns scandals impacting elite reputation.
  - Wealth disparity (materialist), belief divergence (spiritual), glory/strength expectations (warlike lawful/chaotic) drive conflicts.
  - Forced marriages may backfire (runaway lovers, suicide), leading to narrative scenarios and collective memories.

## Integration Points
- **Buff System**: apply status effects (`Lockdown`, `ParanoidAuthorities`, `AllianceHarmony`, `Vengeance`).
- **SociopoliticalDynamics**: elite events spawn situations (siege, blockade, rebellion) and feed collective memories.
- **EconomySystem**: adjust security spending, tariffs, market disruptions.
- **MetricEngine**: track `security_alert_level`, `threat_index`, `war_risk`, `rebellion_probability`.
- **FactionAndGuildSystem**: adjust diplomacy relations, compliance; update guild collaboration (alliances or schisms).
- **NarrativeSituations**: create stories around assassination, marriages, coups, rebellions.
- **MobileSettlementSystem**: captains respond to political events (avoid lockdown sectors, exploit trade opportunities).

## Authoring & Config
- `PoliticalEventDefinition` catalog with trigger conditions, base effects, thresholds.
- `SecurityPolicyProfile` controlling alert escalation, spending response, lockdown severity.
- `RebellionProfile` specifying risk factors, thresholds, effects.
- `MarriageAllianceProfile` linking factions/guilds, reduction percentages.

## Technical Considerations
- Buffers sorted by timestamp to process in order; use hashed ids for deterministic RNG (assassination success).
- Security spending/performance tracked via economy scheduler; ensure budget updates deterministic.
- Lockdown toggles use enableable components on trade routes/navigation nodes.
- Rewind: record political events; reapply security state/buffs consistently.
- Performance: use dirty flags to recalc war risk/tension only when events occur.

## Testing
- Unit tests for event triggers and resulting state changes.
- Integration tests: assassination leading to lockdown, trade penalty, eventual rebellion.
- Diplomacy tests: marriage reduces war risk, maintains relations.
- War escalation scenario: feud → betrayal → total war → rebellion, verifying cascades.
