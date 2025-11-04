# Narrative Situation Concepts

## Overview
- **Situations** are multi-step narrative experiences composed of linked events, choices, and outcomes.
- Each situation tracks progression state, branching options, and timers; outcomes can spawn follow-up situations or modify world services (diplomacy, economy, morale).

## Data Model
- `SituationDefinition` (blob):
  - `BlobArray<SituationNode>` nodes with text keys, action references, and outcome modifiers.
  - `BlobArray<SituationEdge>` edges describing choice conditions, required services (e.g., faction standing), and outcome targets.
  - Metadata: category (crisis, opportunity, ritual), urgency, recurrence rules.
- `SituationInstance` component:
  - `SituationId DefinitionId`, `NodeId CurrentNode`, `float TimeRemaining`, flags (blocking, optional).
  - Dynamic buffer `SituationChoice` with available choice ids for the current node.
- `SituationPayload` dynamic buffer for injecting entities (villagers, elites) or rewards into the situation’s context.

## Execution Flow
1. `SituationSpawnerSystem` reads triggers (service events, timers, player actions) and instantiates `SituationInstance` entities referencing blob definitions.
2. `SituationProgressSystem` advances timers, evaluates guard conditions for available choices, and emits `SituationEvent` records to the event broker.
3. When a player/AI selects a choice, `SituationResolutionSystem` applies outcomes:
   - Modify services (adjust faction standing, assign quests, spawn elites).
   - Queue follow-up situations or conclude the thread.
   - Emit narrative analytics for telemetry.
4. Situations integrate with `StateMachineFramework` for branching nodes and with `EventSystemConcepts` for logging transitions.

## Authoring & Tooling
- Author via `SituationDefinition` ScriptableObjects; baker emits blob assets.
- Provide editor preview of branching graphs, choice conditions, and outcome modifiers.
- Support localization keys and cinematic hooks (cutscenes, UI prompts).

## Integration Targets
- Diplomacy/faction service: adjust relations, treaties, shared wars.
- Elite governance: marry elites, shift succession lines, trigger coups.
- Education/tech: unlock research, grant knowledge boosts.
- Military/conflict: spawn raids, enforce ceasefires, trigger rebellions.
- Telemetry: log choices for analytics dashboards.
