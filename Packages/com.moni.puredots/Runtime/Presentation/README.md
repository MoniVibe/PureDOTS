# Presentation Systems

This package provides presentation layer systems for world tree morphing and statistics UI.

## Components

- **WorldTreeMorphSystem** - Procedural tree mesh morphing based on aggregate profile
- **WorldTreeTag** - Tag marking world tree entities
- **WorldTreeMeshBlob** - BlobAsset for tree mesh parameters
- **StatisticsUISystem** - UI coordination for statistics dashboard
- **StatisticsUITag** - Tag marking statistics UI entities
- **StatisticsUIData** - Data component for UI state

## Quick Start

```csharp
using PureDOTS.Runtime.Presentation;

// World tree morphing happens automatically when:
// 1. WorldAggregateProfile exists
// 2. WorldTreeMorphSystem is enabled

// Statistics UI coordination
var uiEntity = entityManager.CreateEntity();
entityManager.AddComponent<StatisticsUITag>(uiEntity);
```

See `Docs/Guides/WorldEditorAndAnalyticsGuide.md` for detailed usage.

