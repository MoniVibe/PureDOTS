# World Aggregate Profile System

This package provides aggregate world metrics computed from simulation data for visualization and feedback.

## Components

- **WorldAggregateProfile** - Singleton storing aggregate metrics (Population, EnergyFlux, Harmony, Chaos)
- **HistoryBuffer** - Buffer storing profile deltas for visualization
- **SumPopulationSystem** - Aggregates population counts
- **AverageMoralitySystem** - Computes average morality/harmony
- **EnergyBalanceSystem** - Tracks energy flux
- **WorldEventTriggerSystem** - Detects major events and triggers profile spikes

## Quick Start

```csharp
using PureDOTS.Runtime.Aggregate;

// Read aggregate profile
var profile = SystemAPI.GetSingleton<WorldAggregateProfile>();
float harmony = profile.Harmony; // 0-1
float chaos = profile.Chaos;      // 0-1
```

See `Docs/Guides/WorldEditorAndAnalyticsGuide.md` for detailed usage.

