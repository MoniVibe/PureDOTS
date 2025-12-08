# AgentSyncBus Specification

**Last Updated**: 2025-12-07  
**Purpose**: Define the responsibilities, invariants, message types, and extension rules for `AgentSyncBus` across Body/Mind/Aggregate ECS layers.

---

## Responsibilities & Invariants

- **Single authority**: Bus is created and owned by `AgentSyncBridgeCoordinator` (managed only).
- **GUID-only identity**: All messages carry `AgentGuid`; no `Entity` handles cross layers.
- **Deterministic cadence**: Body→Mind sync ~100 ms; Mind→Body sync ~250 ms; configurable but must be consistent per session.
- **Delta-compressed batches**: Bus batches and delta-compresses state to keep sync cost < 3 ms/frame.
- **Managed access**: Burst code must not touch the bus. Collect data in Burst, enqueue from managed systems.
- **Ordering**: Mapping → Body→Mind → Mind→Body → Intent resolution (see `MultiECS_Integration_Guide.md`).

---

## Access Patterns

- **Get bus**: Use `AgentSyncBridgeCoordinator.GetBusFromDefaultWorld()` (managed) or fetch the coordinator via `World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>()`.
- **Enqueue from managed systems**: Burst jobs write to temp native containers; managed wrapper enqueues into bus queues.
- **Read batches**: Use `Dequeue*Batch` APIs; dispose native lists after consumption.

---

## Message Types & Queues

| Message | Purpose | Direction | Queue Notes |
|---------|---------|-----------|-------------|
| `MindToBodyMessage` | Intents from cognition to simulation | Mind → Body | Batched per Mind→Body interval |
| `BodyToMindMessage` | State updates from simulation | Body → Mind | Batched per Body→Mind interval; delta-compressed |
| `Percept` | Sensor readings | Body → Mind | Batched with Body→Mind |
| `LimbCommand` | Limb activations | Mind → Body | Batched per Mind→Body interval |
| `AggregateIntentMessage` | Group-level intents | Aggregate → Mind | Batched per Aggregate tick |
| `ConsensusVoteMessage` | Voting payloads | Any → Aggregate/Mind | Batched; resolved to `ConsensusOutcomeMessage` |
| `ConsensusOutcomeMessage` | Resolved votes | Aggregate/Mind → Agents | Batched; consumed by cognitive systems |

---

## Lifecycle

1. **Initialization**: `AgentSyncBridgeCoordinator` constructs the bus during world bootstrap.
2. **Mapping**: `AgentMappingSystem` links Body `AgentSyncId` to Mind/Aggregate indices.
3. **Body→Mind**: `BodyToMindSyncSystem` gathers state/percepts, enqueues into bus (managed wrapper).
4. **Mind→Body**: `MindToBodySyncSystem` pulls cognitive outputs, enqueues intents/commands.
5. **Intent resolution**: `IntentResolutionSystem` applies resolved intents to Body ECS.

---

## Delta Compression & Ordering

- Body→Mind uses delta flags per field; unchanged fields are skipped.
- Messages are ordered by `TickNumber` inside each batch; do not rely on global ordering across queues.
- Keep `TickNumber` monotonic per direction; bridge systems stamp ticks if producer omits them.

---

## Extension Rules (New Message Types)

1. **Define struct** with `AgentGuid` + payload + `TickNumber` (and any flags for delta semantics).
2. **Add queue & APIs** to `AgentSyncBus` (`EnqueueX`, `DequeueXBatch`, count property).
3. **Bridge integration**: Update relevant bridge system to produce/consume the new batch and wire into cadence.
4. **Performance check**: Profile bus cost after adding; respect < 3 ms/frame target.

---

## Troubleshooting Quick Checks

- Bus null? Ensure `AgentSyncBridgeCoordinator` exists in the active world.
- Missing Mind link? Verify `AgentSyncId.MindEntityIndex >= 0` after mapping.
- Messages not arriving? Confirm producer enqueues from managed code and cadence intervals are not paused.
- Burst errors? Ensure Burst code only writes to temp containers; enqueuing must happen in managed systems.

---

## References

- `Docs/Architecture/ThreePillarECS_Architecture.md` – Layer responsibilities and communication rules.
- `Docs/Guides/MultiECS_Integration_Guide.md` – Integration cookbook and system ordering.
- `Docs/FoundationGuidelines.md` – Burst and determinism constraints (P0–P25 patterns).
