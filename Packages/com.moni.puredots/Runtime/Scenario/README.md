# Scenario Authoring API

This package provides the Scenario Authoring API for creating deterministic scenarios in the Unity Editor.

## Components

- **IScenarioBuilder** - Interface for building scenarios
- **ScenarioBuilder** - Implementation backing editor actions with ScenarioRunner
- **ScenarioAction** - Buffer element storing editor actions
- **EditorWorldBootstrap** - Bootstrap for separate editor ECS world
- **EditorGizmoSystem** - Selection and placement management
- **PreviewSimulationSystem** - Parallel preview simulation
- **BlobHotReloadSystem** - Live blob asset updates

## Quick Start

```csharp
using PureDOTS.Runtime.Scenario;

// Get editor world
var editorWorld = EditorWorldBootstrap.GetOrCreateEditorWorld();

// Create builder
var builder = new ScenarioBuilder(editorWorld.EntityManager);

// Add entities
builder.AddEntity(prefabEntity, position);

// Save scenario
builder.SaveScenario("Assets/Scenarios/MyScenario.json");
```

See `Docs/Guides/WorldEditorAndAnalyticsGuide.md` for detailed usage.

