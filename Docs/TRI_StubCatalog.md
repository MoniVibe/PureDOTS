# TRI Stub Catalog

Shared record of PureDOTS ahead-of-time stubs. Search for `[TRI-STUB]` as needed.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/LogisticsStubComponents.cs`
  - **Module**: Logistics / hauling / maintenance tickets.
  - **Types**: `LogisticsRoute`, `HaulRequest`, `MaintenanceTicket`.
  - **Intent**: Provide IDs for planners/job systems to reference before full schemas exist.
  - **Owner**: PureDOTS logistics spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/OperationsConceptStubComponents.cs`
  - **Module**: Exploration / threat intel / sample storage.
  - **Types**: `ExplorationOrder`, `ThreatSignature`, `IntelSample`.
  - **Intent**: Let scenario + telemetry code request operations data independent of rendering.
  - **Owner**: PureDOTS operations spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/AIConceptStubComponents.cs`
  - **Module**: Behavior tree + perception scaffolding.
  - **Types**: `BehaviorTreeHandle`, `BehaviorTaskState`, `BehaviorNodeState`, `PerceptionConfig`, `PerceptionStimulus`.
  - **Intent**: Shared AI planners/perception loops can define queries now, fill logic later.
  - **Owner**: PureDOTS AI spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/FighterSquadronStubComponents.cs`
  - **Module**: Fighter squadrons, formations, attack runs.
  - **Types**: `FighterSquadronTag`, `SquadronFormation`, `SquadronMember`, `FormationSlot`, `AttackRunTicket`, `AttackRunState`.
  - **Intent**: Provide consistent ECS schema for carriers/strike craft before full hangar ops ship.
  - **Owner**: PureDOTS vehicles/space combat spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/NavigationStubComponents.cs` (+ `NavigationStubSystems.cs`)
  - **Module**: Pathfinding/nav ticketing.
  - **Types**: `NavSurfaceId`, `PathfinderTicket`, `PathSolutionElement`, `PathResultState`; stub system `NavPlannerStubSystem`.
  - **Intent**: Allow games to issue nav requests safely while actual planner/render integration is pending.
  - **Owner**: PureDOTS navigation spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/MovementSpecStubComponents.cs`
  - **Module**: Movement specs/intents.
  - **Types**: `MovementSpec`, `MovementIntent`, `MovementSolutionState`.
  - **Intent**: Provide shared schema for future movement tuning + solution tracking.
  - **Owner**: PureDOTS movement spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/EconomyStubComponents.cs` + `EconomyStubSystems.cs` + `EconomyStubService.cs`
  - **Module**: Economy/production/inventory.
  - **Types**: `ResourceTypeId`, `MaterialProperty`, `ProductionRecipe`, `ProductionQueueEntry`, `FacilityState`, `FacilityInputElement`, `FacilityOutputElement`, `InventorySummary`; stub systems `ProductionSchedulerStubSystem`, `FacilityProcessingStubSystem`, `MarketPricingStubSystem`; service `EconomyServiceStub`.
  - **Intent**: Allow production/facility systems to compile and reference IDs before full economic behavior is implemented.
  - **Owner**: PureDOTS economy spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/NavigationServiceStub.cs`
  - **Module**: Navigation service API.
  - **Types**: `NavigationServiceStub` static helper exposing `RequestPath`, `CancelPath`.
  - **Intent**: Provide contract for games to request nav tickets before real planner integration.
  - **Owner**: PureDOTS navigation spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/CombatServiceStub.cs`
  - **Module**: Combat scheduling API.
  - **Types**: `CombatServiceStub` with `ScheduleEngagement`, `ReportDamage`, `GetThreatRating`.
  - **Intent**: Allow games to hook into combat pipeline signatures early.
  - **Owner**: PureDOTS combat spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/DiplomacyServiceStub.cs`
  - **Module**: Relation/diplomacy API.
  - **Types**: `DiplomacyServiceStub` with `ApplyRelationDelta`, `GetRelation`.
  - **Intent**: Provide relation interface until aggregate diplomacy ships.
  - **Owner**: PureDOTS diplomacy spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/TelemetryBridgeStub.cs`
  - **Module**: Telemetry/metrics bridge.
  - **Types**: `TelemetryBridgeStub` with `RecordMetric`, `RecordEvent`.
  - **Intent**: Let systems log metrics consistently before full telemetry wiring.
  - **Owner**: PureDOTS telemetry spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/SensorInterruptStubComponents.cs` + `SensorInterruptStubSystems.cs` + `SensorServiceStub.cs`
  - **Module**: Sensors/interrupts.
  - **Types**: `SensorChannelDef`, `SensorRigState`, `InterruptTicket`, `AlertTrigger`; system `SensorSamplingStubSystem`; service `SensorServiceStub`.
  - **Intent**: Allow agents to register rigs, raise interrupts, and keep ECS shapes stable before sensor logic ships.
  - **Owner**: PureDOTS sensor spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/TimeControlStubComponents.cs` + `TimeControlServiceStub.cs`
  - **Module**: Time control / playback.
  - **Types**: `TimeControlCommand`, `TimelineBookmark`, `PlaybackMarker`; service `TimeControlServiceStub`.
  - **Intent**: Provide contract for pause/resume/scrub/bookmark operations prior to final rewind UI/state machines.
  - **Owner**: PureDOTS time spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/NarrativeStubComponents.cs` + `NarrativeStubSystems.cs` + `NarrativeServiceStub.cs`
  - **Module**: Narrative/situation events.
  - **Types**: `SituationId`, `NarrativeEventTicket`, `DialogueChoice`; system `NarrativeDispatcherStubSystem`; service `NarrativeServiceStub`.
  - **Intent**: Let gameplay raise/track narrative events without full dispatcher.
  - **Owner**: PureDOTS narrative spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/TelemetryStubComponents.cs`
  - **Module**: Telemetry stream data.
  - **Types**: `TelemetryStreamId`, `TelemetrySample`.
  - **Intent**: Provide ECS representation of telemetry streams for future ingestion.
  - **Owner**: PureDOTS telemetry spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/SaveLoadStubComponents.cs` + `SaveLoadServiceStub.cs`
  - **Module**: Persistence/save-load.
  - **Types**: `SaveChunkTag`, `SnapshotHandle`, `DeserializationTicket`; service `SaveLoadServiceStub`.
  - **Intent**: Give systems a way to request saves/loads and tag chunks before persistence is built.
  - **Owner**: PureDOTS persistence spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/BehaviorInfluenceStubComponents.cs` + `BehaviorInfluenceStubSystems.cs` + `BehaviorServiceStub.cs`
  - **Module**: Behavior profiles / initiative / needs.
  - **Types**: `BehaviorProfileId`, `BehaviorModifier`, `InitiativeStat`, `NeedCategory`, `NeedSatisfaction`, `NeedRequestElement`; system `BehaviorInfluenceStubSystem`; service `BehaviorServiceStub`.
  - **Intent**: Let consumers reference behavior/initiative/needs contracts before real influence logic ships.
  - **Owner**: PureDOTS behavior spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/AggregateStubComponents.cs` + `AggregateStubSystems.cs` + `AggregateServiceStub.cs`
  - **Module**: Aggregate entities (bands/fleets/guilds).
  - **Types**: `AggregateHandle`, `AggregateArchetypeId`, `AggregateRole`, `AggregateFormationTicket`, `AggregateMembershipElement`, `FleetDescriptor`, `BandDescriptor`, `GuildDescriptor`; system `AggregateManagementStubSystem`; service `AggregateServiceStub`.
  - **Intent**: Provide shared IDs/contracts for group entities and membership management until full aggregate systems are implemented.
  - **Owner**: PureDOTS aggregates spine.

- **File**: `Packages/com.moni.puredots/Runtime/Stubs/CraftingStubComponents.cs` + `CraftingStubSystems.cs` + `CraftingServiceStub.cs`
  - **Module**: Crafting logic/material consumption/quality formulas.
  - **Types**: `CraftingJobTicket`, `CraftingMaterialEntry`, `CraftingFormulaParams`, `CraftingQualityState`, `CraftingResult`; system `CraftingLogicStubSystem`; service `CraftingServiceStub`.
  - **Intent**: Allow facilities and gameplay to reference crafting jobs/materials/quality before full logic lands.
  - **Owner**: PureDOTS crafting spine.
